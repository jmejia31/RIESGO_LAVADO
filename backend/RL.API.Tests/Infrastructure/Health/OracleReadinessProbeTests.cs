using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RL.API.Infrastructure.Database;
using RL.API.Infrastructure.Health;
using Xunit;

namespace RL.API.Tests.Infrastructure.Health;

public sealed class OracleReadinessProbeTests
{
    [Theory]
    [InlineData("Password=Sup3rSecret; Pwd=AltSecret; User Id=analyst; Uid=other; Token=abc; Secret=jwt-secret; JWT=header.payload.sig", "Sup3rSecret")]
    [InlineData("Authorization: Bearer abc.def.ghi", "abc.def.ghi")]
    [InlineData("(DESCRIPTION=(ADDRESS=(HOST=oracle.internal)(PORT=1521)))", "oracle.internal")]
    [InlineData("Data Source=oracle.internal:1521/service;Pooling=true", "oracle.internal")]
    public void Sanitizer_RedactsDatosSensibles(string input, string secret)
    {
        var sanitized = ReadinessDiagnosticSanitizer.Sanitize(input);

        Assert.DoesNotContain(secret, sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sup3rSecret", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("AltSecret", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("analyst", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("oracle.internal", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IsReadyAsync_CuandoFallaConexion_RetornaNotReadyYRegistraEtapaSegura()
    {
        var logger = new CapturingLogger<OracleReadinessProbe>();
        var probe = new OracleReadinessProbe(
            new OracleDbContext("this is not a valid Oracle connection string; Password=do-not-log"),
            Options.Create(new HealthProbeOptions { OracleTimeoutSeconds = 3 }),
            logger);

        var result = await probe.IsReadyAsync();

        Assert.False(result);
        var warning = Assert.Single(logger.Messages.Where(message => message.Contains("Oracle readiness failed", StringComparison.Ordinal)));
        Assert.Contains("ORACLE_READINESS_FAILURE_STAGE=CREATE_CONNECTION", warning, StringComparison.Ordinal);
        Assert.DoesNotContain("do-not-log", warning, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IsReadyAsync_OracleListenerLocalInaccesible_RetornaNotReadyYRegistraErrorsSanitizados()
    {
        var logger = new CapturingLogger<OracleReadinessProbe>();
        var probe = new OracleReadinessProbe(
            new OracleDbContext("Data Source=127.0.0.1:1/unused;User Id=diagnostic-user;Password=diagnostic-password;Connection Timeout=1"),
            Options.Create(new HealthProbeOptions { OracleTimeoutSeconds = 3 }),
            logger);

        var result = await probe.IsReadyAsync();

        Assert.False(result);
        var warning = Assert.Single(logger.Messages.Where(message => message.Contains("ORACLE_EXCEPTION_NUMBER=", StringComparison.Ordinal)));
        Assert.Contains("ORACLE_READINESS_FAILURE_STAGE=OPEN_CONNECTION", warning, StringComparison.Ordinal);
        Assert.Contains("ORACLE_ERRORS_COUNT=", warning, StringComparison.Ordinal);
        Assert.Contains("ORACLE_ERROR_CODE=ORA-", warning, StringComparison.Ordinal);
        Assert.Contains(logger.Messages, message => message.Contains("ORACLE_ERROR_NUMBER=", StringComparison.Ordinal));
        Assert.DoesNotContain("diagnostic-user", string.Join('\n', logger.Messages), StringComparison.Ordinal);
        Assert.DoesNotContain("diagnostic-password", string.Join('\n', logger.Messages), StringComparison.Ordinal);
        Assert.DoesNotContain("Data Source", string.Join('\n', logger.Messages), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", string.Join('\n', logger.Messages), StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessProbe_MantieneSqlTimeoutFactoryYCancellationContract()
    {
        var repositoryRoot = FindRepositoryRoot();
        var probeSource = File.ReadAllText(Path.Combine(repositoryRoot, "backend/RL.API/Infrastructure/Health/OracleReadinessProbe.cs"));
        var contextSource = File.ReadAllText(Path.Combine(repositoryRoot, "backend/RL.API/Infrastructure/Database/OracleDbContext.cs"));

        Assert.Contains("command.CommandText = \"SELECT 1 FROM DUAL\";", probeSource, StringComparison.Ordinal);
        Assert.Contains("command.CommandTimeout = _timeoutSeconds;", probeSource, StringComparison.Ordinal);
        Assert.Contains("timeoutCts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));", probeSource, StringComparison.Ordinal);
        Assert.Contains("await command.ExecuteScalarAsync(timeoutCts.Token)", probeSource, StringComparison.Ordinal);
        Assert.Contains("var isReady = result is not null && Convert.ToInt32(result) == 1;", probeSource, StringComparison.Ordinal);
        Assert.Contains("return isReady;", probeSource, StringComparison.Ordinal);
        Assert.Contains("when (!cancellationToken.IsCancellationRequested)", probeSource, StringComparison.Ordinal);
        Assert.Matches(@"catch \(OperationCanceledException\)\s*\{", probeSource);
        Assert.Contains("throw;", probeSource, StringComparison.Ordinal);
        Assert.Contains("INTERNAL_3S_TIMEOUT", probeSource, StringComparison.Ordinal);
        Assert.Contains("CALLER", probeSource, StringComparison.Ordinal);
        Assert.Contains("_dbContext.CreateConnection()", probeSource, StringComparison.Ordinal);
        Assert.Contains("=> new(_connectionString)", contextSource, StringComparison.Ordinal);
        Assert.Equal(3, new HealthProbeOptions().GetEffectiveOracleTimeoutSeconds());
    }

    [Fact]
    public void Sanitizer_TruncaMensajesLargosDespuesDeRedactar()
    {
        var sanitized = ReadinessDiagnosticSanitizer.Sanitize(new string('x', 600));

        Assert.Equal(512, sanitized.Length);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "RIESGO_LAVADO.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root not found for source-contract test.");
    }
}
