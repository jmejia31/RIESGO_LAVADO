namespace RL.API.Infrastructure.Health;

/// <summary>
/// Contrato de readiness del API. Debe comprobar únicamente dependencias indispensables
/// para atender tráfico sin exponer información técnica al consumidor HTTP.
/// </summary>
public interface ISystemReadinessProbe
{
    Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
}
