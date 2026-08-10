using Microsoft.Extensions.Options;
using RL.API.Infrastructure.Database;

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
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_timeoutSeconds));

        try
        {
            using var connection = _dbContext.CreateConnection();
            await connection.OpenAsync(timeoutCts.Token);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM DUAL";
            command.CommandTimeout = _timeoutSeconds;

            var result = await command.ExecuteScalarAsync(timeoutCts.Token);
            return result is not null && Convert.ToInt32(result) == 1;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Readiness Oracle agotó el tiempo máximo configurado.");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Readiness Oracle no disponible. Tipo de excepción: {ExceptionType}",
                ex.GetType().Name);
            return false;
        }
    }
}
