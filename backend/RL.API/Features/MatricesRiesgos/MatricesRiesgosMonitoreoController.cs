using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos;

[ApiController]
[Authorize]
[ModuloAuthorize(10)]
[Route("api/matrices-riesgos/monitoreo")]
[Produces("application/json")]
public sealed class MatricesRiesgosMonitoreoController : ControllerBase
{
    private readonly IMatricesRiesgosMonitoreoService _service;

    public MatricesRiesgosMonitoreoController(IMatricesRiesgosMonitoreoService service) => _service = service;

    [HttpGet("evaluaciones/{evaluacionId:long}/alertas")]
    public async Task<IActionResult> ListarAlertas(long evaluacionId) => Responder(await _service.ListarAlertasAsync(evaluacionId));

    [HttpPost("alertas")]
    [AuditRequired("Creación de señal de alerta")]
    public async Task<IActionResult> CrearAlerta([FromBody] SenalAlertaGuardarDto dto) =>
        Responder(await _service.CrearAlertaAsync(dto, UsuarioId(), Ip()));

    [HttpPut("alertas/{id:long}/estado")]
    [AuditRequired("Cambio de estado de señal de alerta")]
    public async Task<IActionResult> CambiarEstadoAlerta(long id, [FromBody] SenalAlertaEstadoDto dto) =>
        Responder(await _service.CambiarEstadoAlertaAsync(id, dto, UsuarioId(), Ip()));

    [HttpGet("evaluaciones/{evaluacionId:long}/automonitoreo")]
    public async Task<IActionResult> ListarAutomonitoreo(long evaluacionId) =>
        Responder(await _service.ListarAutomonitoreoAsync(evaluacionId));

    [HttpPost("automonitoreo")]
    [AuditRequired("Registro de automonitoreo de evaluación")]
    public async Task<IActionResult> RegistrarAutomonitoreo([FromBody] AutomonitoreoGuardarDto dto) =>
        Responder(await _service.RegistrarAutomonitoreoAsync(dto, UsuarioId(), Ip()));

    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen() => Responder(await _service.ObtenerResumenOperativoAsync());

    private long UsuarioId() => Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private string? Ip()
    {
        string? forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded)) return forwarded.Split(',')[0].Trim();
        string? realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        return !string.IsNullOrWhiteSpace(realIp) ? realIp.Trim() : HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private IActionResult Responder(ServiceResult result) => result.Success
        ? Ok(new { success = true, mensaje = result.Message })
        : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
    private IActionResult Responder<T>(ServiceResult<T> result) => result.Success
        ? Ok(new { success = true, datos = result.Data, mensaje = result.Message })
        : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
}
