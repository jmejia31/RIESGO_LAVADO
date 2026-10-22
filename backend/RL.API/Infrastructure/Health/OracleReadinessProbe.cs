using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using RL.API.Infrastructure.Database;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RL.API.Infrastructure.Health;

/// <summary>
/// Readiness probe de Oracle. Realiza una consulta mínima de solo lectura y nunca devuelve
/// al cliente mensajes, cadenas de conexión, SQL ni detalles de excepciones.
/// </summary>
public sealed class OracleReadinessProbe : ISystemReadinessProbe
{
    private readonly OracleDbContext _dbContext;
    private readonly ILogger<OracleReadinessProbe> _logger;
    private readonly int _timeoutSeconds;

    public OracleReadinessProbe(
        OracleDbContext dbContext,
        IOptions<HealthProbeOptions> options,
        ILogger<OracleReadinessProbe> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeoutSeconds = options.Value.GetEffectiveOracleTimeoutSeconds();
    }

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        var totalTimer = Stopwatch.StartNew();
        var connectionCreateMs = 0L;
        var openAsyncMs = 0L;
        var executeScalarMs = 0L;
        var stage = "CREATE_CONNECTION";
        var stageTimer = Stopwatch.StartNew();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

        try
        {
            Oracle.ManagedDataAccess.Client.OracleConnection connection;
            stageTimer.Restart();
            try
            {
                connection = _dbContext.CreateConnection();
            }
            finally
            {
                connectionCreateMs = stageTimer.ElapsedMilliseconds;
            }
            using (connection)
            {
                stage = "OPEN_CONNECTION";
                stageTimer.Restart();
                try
                {
                    await connection.OpenAsync(timeoutCts.Token);
                }
                finally
                {
                    openAsyncMs = stageTimer.ElapsedMilliseconds;
                }

                stage = "CREATE_COMMAND";
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1 FROM DUAL";
                command.CommandTimeout = _timeoutSeconds;

                stage = "EXECUTE_SCALAR";
                stageTimer.Restart();
                object? result;
                try
                {
                    result = await command.ExecuteScalarAsync(timeoutCts.Token);
                }
                finally
                {
                    executeScalarMs = stageTimer.ElapsedMilliseconds;
                }

                stage = "VALIDATE_RESULT";
                var isReady = result is not null && Convert.ToInt32(result) == 1;
                totalTimer.Stop();
                _logger.LogDebug(
                    "Oracle readiness completed. Stage={ReadinessStage} ConnectionCreateMs={ConnectionCreateMs} OpenAsyncMs={OpenAsyncMs} ExecuteScalarMs={ExecuteScalarMs} TotalReadinessMs={TotalReadinessMs}",
                    stage,
                    connectionCreateMs,
                    openAsyncMs,
                    executeScalarMs,
                    totalTimer.ElapsedMilliseconds);
                return isReady;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            totalTimer.Stop();
            _logger.LogWarning(
                "Oracle readiness cancelled. ORACLE_READINESS_FAILURE_STAGE={ORACLE_READINESS_FAILURE_STAGE} READINESS_CANCELLATION_SOURCE={READINESS_CANCELLATION_SOURCE} CONNECTION_CREATE_MS={CONNECTION_CREATE_MS} OPEN_ASYNC_MS={OPEN_ASYNC_MS} EXECUTE_SCALAR_MS={EXECUTE_SCALAR_MS} TOTAL_READINESS_MS={TOTAL_READINESS_MS}",
                stage,
                "INTERNAL_3S_TIMEOUT",
                connectionCreateMs,
                openAsyncMs,
                executeScalarMs,
                totalTimer.ElapsedMilliseconds);
            return false;
        }
        catch (OperationCanceledException)
        {
            totalTimer.Stop();
            _logger.LogInformation(
                "Oracle readiness cancelled. ORACLE_READINESS_FAILURE_STAGE={ORACLE_READINESS_FAILURE_STAGE} READINESS_CANCELLATION_SOURCE={READINESS_CANCELLATION_SOURCE} CONNECTION_CREATE_MS={CONNECTION_CREATE_MS} OPEN_ASYNC_MS={OPEN_ASYNC_MS} EXECUTE_SCALAR_MS={EXECUTE_SCALAR_MS} TOTAL_READINESS_MS={TOTAL_READINESS_MS}",
                stage,
                "CALLER",
                connectionCreateMs,
                openAsyncMs,
                executeScalarMs,
                totalTimer.ElapsedMilliseconds);
            throw;
        }
        catch (OracleException ex)
        {
            totalTimer.Stop();
            _logger.LogWarning(
                "Oracle readiness failed. ORACLE_READINESS_FAILURE_STAGE={ORACLE_READINESS_FAILURE_STAGE} ORACLE_EXCEPTION_NUMBER={ORACLE_EXCEPTION_NUMBER} ORACLE_ERROR_CODE={ORACLE_ERROR_CODE} ORACLE_ERRORS_COUNT={ORACLE_ERRORS_COUNT} CONNECTION_CREATE_MS={CONNECTION_CREATE_MS} OPEN_ASYNC_MS={OPEN_ASYNC_MS} EXECUTE_SCALAR_MS={EXECUTE_SCALAR_MS} TOTAL_READINESS_MS={TOTAL_READINESS_MS} READINESS_CANCELLATION_SOURCE={READINESS_CANCELLATION_SOURCE} EXCEPTION_TYPE={EXCEPTION_TYPE} HRESULT={HRESULT} MESSAGE_SANITIZED={MESSAGE_SANITIZED}",
                stage,
                ex.Number,
                FormatOracleCode(ex.Number),
                ex.Errors.Count,
                connectionCreateMs,
                openAsyncMs,
                executeScalarMs,
                totalTimer.ElapsedMilliseconds,
                GetCancellationSource(cancellationToken, timeoutCts.Token),
                ex.GetType().Name,
                ex.HResult,
                ReadinessDiagnosticSanitizer.Sanitize(ex.Message));

            var errorIndex = 0;
            foreach (OracleError error in ex.Errors)
            {
                _logger.LogWarning(
                    "Oracle readiness provider error. ERROR_INDEX={ERROR_INDEX} ORACLE_ERROR_NUMBER={ORACLE_ERROR_NUMBER} ORACLE_ERROR_CODE={ORACLE_ERROR_CODE} MESSAGE_SANITIZED={MESSAGE_SANITIZED}",
                    errorIndex++,
                    error.Number,
                    FormatOracleCode(error.Number),
                    ReadinessDiagnosticSanitizer.Sanitize(error.Message));
            }

            LogInnerExceptions(ex);
            return false;
        }
        catch (Exception ex)
        {
            totalTimer.Stop();
            _logger.LogWarning(
                "Oracle readiness failed. ORACLE_READINESS_FAILURE_STAGE={ORACLE_READINESS_FAILURE_STAGE} EXCEPTION_TYPE={EXCEPTION_TYPE} HRESULT={HRESULT} MESSAGE_SANITIZED={MESSAGE_SANITIZED} CONNECTION_CREATE_MS={CONNECTION_CREATE_MS} OPEN_ASYNC_MS={OPEN_ASYNC_MS} EXECUTE_SCALAR_MS={EXECUTE_SCALAR_MS} TOTAL_READINESS_MS={TOTAL_READINESS_MS} READINESS_CANCELLATION_SOURCE={READINESS_CANCELLATION_SOURCE}",
                stage,
                ex.GetType().Name,
                ex.HResult,
                ReadinessDiagnosticSanitizer.Sanitize(ex.Message),
                connectionCreateMs,
                openAsyncMs,
                executeScalarMs,
                totalTimer.ElapsedMilliseconds,
                GetCancellationSource(cancellationToken, timeoutCts.Token));
            LogInnerExceptions(ex);
            return false;
        }
    }

    private void LogInnerExceptions(Exception exception)
    {
        var inner = exception.InnerException;
        for (var depth = 1; inner is not null && depth <= 3; depth++, inner = inner.InnerException)
        {
            _logger.LogWarning(
                "Oracle readiness inner exception. INNER_DEPTH={INNER_DEPTH} INNER_EXCEPTION_TYPE={INNER_EXCEPTION_TYPE} INNER_EXCEPTION_HRESULT={INNER_EXCEPTION_HRESULT} INNER_EXCEPTION_MESSAGE_SANITIZED={INNER_EXCEPTION_MESSAGE_SANITIZED}",
                depth,
                inner.GetType().Name,
                inner.HResult,
                ReadinessDiagnosticSanitizer.Sanitize(inner.Message));
        }
    }

    private static string GetCancellationSource(CancellationToken callerToken, CancellationToken linkedToken) =>
        callerToken.IsCancellationRequested
            ? "CALLER"
            : linkedToken.IsCancellationRequested ? "INTERNAL_3S_TIMEOUT" : "NONE";

    private static string FormatOracleCode(int number) => $"ORA-{number:D5}";
}

internal static class ReadinessDiagnosticSanitizer
{
    private static readonly Regex SensitiveKeyValue = new(
        """(?i)(password|pwd|user\s*id|uid|token|secret|jwt|connection\s*string|connectionstring)\s*[:=]\s*(?:"[^"]*"|'[^']*'|[^;\s,]+)""",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex BearerToken = new(
        @"(?i)\bBearer\s+[^\s,;]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ConnectionDescriptor = new(
        @"(?i)(Data\s+Source|Server|Host)\s*=\s*[^;]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TnsDescriptor = new(
        @"(?i)\(DESCRIPTION\s*=.*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    private static readonly Regex HostReference = new(
        @"(?i)\b(host|server)\s+[^\s,;]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex IpAddress = new(
        @"\b(?:\d{1,3}\.){3}\d{1,3}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static string Sanitize(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        var sanitized = SensitiveKeyValue.Replace(message, "$1=[REDACTED]");
        sanitized = BearerToken.Replace(sanitized, "Bearer [REDACTED]");
        sanitized = ConnectionDescriptor.Replace(sanitized, "$1=[REDACTED]");
        sanitized = TnsDescriptor.Replace(sanitized, "[TNS_DESCRIPTOR_REDACTED]");
        sanitized = HostReference.Replace(sanitized, "$1 [REDACTED]");
        sanitized = IpAddress.Replace(sanitized, "[IP_REDACTED]");
        return sanitized.Length <= 512 ? sanitized : sanitized[..512];
    }
}
