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
[Route("api/matrices-riesgos/riesgos")]
[Produces("application/json")]
public sealed class MatricesRiesgosGestionController : ControllerBase
{
    private readonly IMatricesRiesgosGestionService _service;

    public MatricesRiesgosGestionController(IMatricesRiesgosGestionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInactivos = false) =>
        Responder(await _service.ListarRiesgosAsync(incluirInactivos));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id) =>
        Responder(await _service.ObtenerRiesgoAsync(id));

    [HttpPost]
    [AuditRequired("Creación de riesgo maestro")]
    public async Task<IActionResult> Crear([FromBody] RiesgoGuardarDto dto) =>
        Responder(await _service.CrearRiesgoAsync(dto, ObtenerUsuarioId(), ObtenerIp()));

    [HttpPut("{id:long}")]
    [AuditRequired("Actualización de riesgo maestro")]
    public async Task<IActionResult> Actualizar(long id, [FromBody] RiesgoGuardarDto dto) =>
        Responder(await _service.ActualizarRiesgoAsync(id, dto, ObtenerUsuarioId(), ObtenerIp()));

    private long ObtenerUsuarioId() => Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private string? ObtenerIp()
    {
        string? forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor)) return forwardedFor.Split(',')[0].Trim();
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
