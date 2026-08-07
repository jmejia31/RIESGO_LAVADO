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
[Route("api/matrices-riesgos/mitigacion")]
[Produces("application/json")]
public sealed class MatricesRiesgosMitigacionController : ControllerBase
{
    private readonly IMatricesRiesgosMitigacionService _service;

    public MatricesRiesgosMitigacionController(IMatricesRiesgosMitigacionService service) => _service = service;

    [HttpGet("evaluaciones/{evaluacionId:long}/controles")]
    public async Task<IActionResult> ListarControles(long evaluacionId) => Responder(await _service.ListarControlesAsync(evaluacionId));

    [HttpPost("controles")]
    [AuditRequired("Creación de control de riesgo")]
    public async Task<IActionResult> CrearControl([FromBody] ControlRiesgoGuardarDto dto) =>
        Responder(await _service.CrearControlAsync(dto, UsuarioId(), Ip()));

    [HttpPut("controles/{id:long}")]
    [AuditRequired("Actualización de control de riesgo")]
    public async Task<IActionResult> ActualizarControl(long id, [FromBody] ControlRiesgoGuardarDto dto) =>
        Responder(await _service.ActualizarControlAsync(id, dto, UsuarioId(), Ip()));

    [HttpGet("controles/{controlId:long}/evaluaciones")]
    public async Task<IActionResult> ListarEvaluacionesControl(long controlId) =>
        Responder(await _service.ListarEvaluacionesControlAsync(controlId));

    [HttpPost("controles/{controlId:long}/evaluaciones")]
    [AuditRequired("Evaluación de efectividad de control")]
    public async Task<IActionResult> EvaluarControl(long controlId, [FromBody] EvaluacionControlGuardarDto dto) =>
        Responder(await _service.RegistrarEvaluacionControlAsync(controlId, dto, UsuarioId(), Ip()));

    [HttpGet("evaluaciones/{evaluacionId:long}/planes")]
    public async Task<IActionResult> ListarPlanes(long evaluacionId) => Responder(await _service.ListarPlanesAsync(evaluacionId));

    [HttpPost("planes")]
    [AuditRequired("Creación de plan de mitigación")]
    public async Task<IActionResult> CrearPlan([FromBody] PlanMitigacionGuardarDto dto) =>
        Responder(await _service.CrearPlanAsync(dto, UsuarioId(), Ip()));

    [HttpPut("planes/{id:long}")]
    [AuditRequired("Actualización de plan de mitigación")]
    public async Task<IActionResult> ActualizarPlan(long id, [FromBody] PlanMitigacionGuardarDto dto) =>
        Responder(await _service.ActualizarPlanAsync(id, dto, UsuarioId(), Ip()));

    [HttpGet("planes/{planId:long}/actividades")]
    public async Task<IActionResult> ListarActividades(long planId) => Responder(await _service.ListarActividadesAsync(planId));

    [HttpPost("actividades")]
    [AuditRequired("Creación de actividad de mitigación")]
    public async Task<IActionResult> CrearActividad([FromBody] ActividadPlanGuardarDto dto) =>
        Responder(await _service.CrearActividadAsync(dto, UsuarioId(), Ip()));

    [HttpPut("actividades/{id:long}")]
    [AuditRequired("Actualización de actividad de mitigación")]
    public async Task<IActionResult> ActualizarActividad(long id, [FromBody] ActividadPlanGuardarDto dto) =>
        Responder(await _service.ActualizarActividadAsync(id, dto, UsuarioId(), Ip()));

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
