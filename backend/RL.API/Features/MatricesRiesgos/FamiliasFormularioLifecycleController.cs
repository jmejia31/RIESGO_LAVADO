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
[Route("api/matrices-riesgos/familias")]
[Produces("application/json")]
public sealed class FamiliasFormularioLifecycleController : ControllerBase
{
    private readonly FamiliasFormularioLifecycleService _service;
    private readonly ILogger<FamiliasFormularioLifecycleController> _logger;

    public FamiliasFormularioLifecycleController(
        FamiliasFormularioLifecycleService service,
        ILogger<FamiliasFormularioLifecycleController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPut("{id:long}/activar")]
    [Authorize(Roles = SystemRoles.Administrador)]
    [AuditRequired("Activación de familia de formulario")]
    public async Task<IActionResult> ActivarFamiliaFormulario(long id)
    {
        try
        {
            return Responder(await _service.ActivarFamiliaFormularioAsync(id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al activar la familia de formulario ID {Id}", id);
            return Error500();
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = SystemRoles.Administrador)]
    [AuditRequired("Eliminación segura de familia de formulario vacía")]
    public async Task<IActionResult> EliminarFamiliaFormulario(long id)
    {
        try
        {
            return Responder(await _service.EliminarFamiliaFormularioAsync(id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar la familia de formulario ID {Id}", id);
            return Error500();
        }
    }

    [HttpPut("{id:long}/archivar")]
    [Authorize(Roles = SystemRoles.Administrador)]
    [AuditRequired("Archivado administrativo de familia de formulario")]
    public async Task<IActionResult> ArchivarFamiliaFormulario(long id, [FromBody] JustificacionFamiliaDto dto)
    {
        try { return Responder(await _service.ArchivarFamiliaFormularioAsync(id, dto?.Motivo)); }
        catch (Exception ex) { _logger.LogError(ex, "Error al archivar la familia de formulario ID {Id}", id); return Error500(); }
    }

    [HttpPut("{id:long}/restaurar")]
    [Authorize(Roles = SystemRoles.Administrador)]
    [AuditRequired("Restauración administrativa de familia de formulario")]
    public async Task<IActionResult> RestaurarFamiliaFormulario(long id)
    {
        try { return Responder(await _service.RestaurarFamiliaFormularioAsync(id)); }
        catch (Exception ex) { _logger.LogError(ex, "Error al restaurar la familia de formulario ID {Id}", id); return Error500(); }
    }

    [HttpGet("{id:long}/purga-precheck")]
    [Authorize(Roles = SystemRoles.Administrador)]
    public async Task<IActionResult> ObtenerPurgaPrecheck(long id)
    {
        try
        {
            ServiceResult<FamiliaFormularioPurgaPrecheckDto> result = await _service.ObtenerPurgaPrecheckAsync(id);
            return result.Success ? Ok(new { success = true, datos = result.Data }) : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error al evaluar la purga de la familia de formulario ID {Id}", id); return Error500(); }
    }

    private IActionResult Responder(ServiceResult result)
    {
        return result.Success
            ? Ok(new { success = true, mensaje = result.Message })
            : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
    }

    private IActionResult Error500()
    {
        return StatusCode(500, new
        {
            success = false,
            mensaje = "Error interno en el módulo de Matrices de Riesgos.",
            traceId = HttpContext.TraceIdentifier
        });
    }
}

public sealed class JustificacionFamiliaDto
{
    public string? Motivo { get; set; }
}
