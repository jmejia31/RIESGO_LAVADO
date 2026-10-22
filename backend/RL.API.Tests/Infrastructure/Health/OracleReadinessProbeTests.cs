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
}
