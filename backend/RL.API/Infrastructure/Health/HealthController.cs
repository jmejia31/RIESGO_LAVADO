using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace RL.API.Infrastructure.Health;

/// <summary>
/// Endpoints operativos para orquestadores, balanceadores y monitoreo de infraestructura.
/// No requieren autenticación y exponen únicamente el estado agregado mínimo.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly ISystemReadinessProbe _readinessProbe;

    public HealthController(ISystemReadinessProbe readinessProbe)
    {
        _readinessProbe = readinessProbe;
    }

    /// <summary>
    /// Liveness: confirma exclusivamente que el proceso HTTP está vivo.
    /// No consulta Oracle ni servicios externos.
    /// </summary>
    [HttpGet("/healthz")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(HealthProbeResponse), StatusCodes.Status200OK)]
    public ActionResult<HealthProbeResponse> Liveness() =>
        Ok(HealthProbeResponse.Healthy());

    /// <summary>
    /// Readiness: confirma que las dependencias indispensables están disponibles.
    /// Una dependencia no disponible produce 503 sin revelar detalles internos.
    /// </summary>
    [HttpGet("/readyz")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(HealthProbeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthProbeResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthProbeResponse>> Readiness(CancellationToken cancellationToken)
    {
        var isReady = await _readinessProbe.IsReadyAsync(cancellationToken);
        var response = isReady
            ? HealthProbeResponse.Healthy()
            : HealthProbeResponse.Unhealthy();

        return isReady
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}

public sealed record HealthProbeResponse(
    [property: JsonProperty("status")] string Status)
{
    public static HealthProbeResponse Healthy() => new("Healthy");
    public static HealthProbeResponse Unhealthy() => new("Unhealthy");
}
