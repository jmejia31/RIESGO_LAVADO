using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Core.Security;
using RL.API.Shared.Results;

namespace RL.API.Features.MatricesRiesgos;

[ApiController]
[Authorize]
[ModuloAuthorize(10)] // Modulo 10 de Matrices de Riesgos en el SGRLA
[Route("api/matrices-riesgos")]
[Produces("application/json")]
public sealed class MatricesRiesgosController : ControllerBase
{
    private readonly IMatricesRiesgosAppService _service;
    private readonly ILogger<MatricesRiesgosController> _logger;

    public MatricesRiesgosController(IMatricesRiesgosAppService service, ILogger<MatricesRiesgosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ============================================================
    // 1. ENDPOINTS DE ADMINISTRACIÓN DEL CICLO DE VIDA DEL FORMULARIO
    // ============================================================

    [HttpPost("formularios/borrador")]
    [Authorize(Roles = "ADMIN, DBA, RIESGOS_ADMIN")]
    [AuditRequired("Creación de borrador de formulario")]
    public async Task<IActionResult> CrearBorradorFormulario([FromQuery] long familiaId, [FromQuery] string codigoFormulario, [FromBody] string jsonConfig)
    {
        try
        {
            var result = await _service.CrearBorradorFormularioAsync(familiaId, codigoFormulario, jsonConfig, ObtenerUsuarioId());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear borrador de formulario.");
            return Error500(ex);
        }
    }

    [HttpPost("formularios/{id:long}/clonar")]
    [Authorize(Roles = "ADMIN, DBA, RIESGOS_ADMIN")]
    [AuditRequired("Clonación de versión de formulario")]
    public async Task<IActionResult> ClonarVersionFormulario(long id)
    {
        try
        {
            var result = await _service.ClonarVersionFormularioAsync(id, ObtenerUsuarioId());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al clonar versión de formulario ID {Id}", id);
            return Error500(ex);
        }
    }

    [HttpPut("formularios/{id:long}")]
    [Authorize(Roles = "ADMIN, DBA, RIESGOS_ADMIN")]
    [AuditRequired("Actualización de borrador de formulario")]
    public async Task<IActionResult> ActualizarBorradorFormulario(long id, [FromBody] string jsonConfig)
    {
        try
        {
            var result = await _service.ActualizarBorradorFormularioAsync(id, jsonConfig, ObtenerUsuarioId());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar borrador de formulario ID {Id}", id);
            return Error500(ex);
        }
    }

    [HttpPost("formularios/{id:long}/publicar")]
    [Authorize(Roles = "ADMIN, DBA, RIESGOS_ADMIN")]
    [AuditRequired("Publicación y vigencia de versión de formulario")]
    public async Task<IActionResult> PublicarVersionFormulario(long id)
    {
        try
        {
            var result = await _service.PublicarVersionFormularioAsync(id, ObtenerUsuarioId());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al publicar versión de formulario ID {Id}", id);
            return Error500(ex);
        }
    }

    [HttpPut("formularios/{id:long}/estado")]
    [Authorize(Roles = "ADMIN, DBA, RIESGOS_ADMIN")]
    [AuditRequired("Cambio de vigencia de versión de formulario")]
    public async Task<IActionResult> CambiarEstadoVigenciaFormulario(long id, [FromQuery] bool vigente)
    {
        try
        {
            var result = await _service.CambiarEstadoVigenciaFormularioAsync(id, vigente, ObtenerUsuarioId());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar vigencia de versión de formulario ID {Id}", id);
            return Error500(ex);
        }
    }

    [HttpGet("formularios/historial")]
    public async Task<IActionResult> ListarHistorialVersionesFormulario([FromQuery] string familiaCodigo)
    {
        try
        {
            var result = await _service.ListarHistorialVersionesFormularioAsync(familiaCodigo);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar historial de versiones del formulario.");
            return Error500(ex);
        }
    }

    // ============================================================
    // 2. ENDPOINTS OPERATIVOS DE EVALUACIONES E HISTORIAL
    // ============================================================

    [HttpGet("formulario/version-vigente")]
    public async Task<IActionResult> ObtenerVersionVigenteFormulario([FromQuery] string familiaCodigo = "MATRIZ_RIESGOS_LAFT")
    {
        try
        {
            var result = await _service.ObtenerVersionVigenteFormularioAsync(familiaCodigo);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la versión de formulario vigente.");
            return Error500(ex);
        }
    }

    [HttpGet("evaluaciones/{id:long}")]
    public async Task<IActionResult> ObtenerEvaluacion(long id)
    {
        try
        {
            var result = await _service.ObtenerEvaluacionAsync(id);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la evaluación ID {Id}", id);
            return Error500(ex);
        }
    }

    [HttpGet("evaluaciones")]
    public async Task<IActionResult> ListarEvaluacionesPaginadas([FromQuery] ConsultaEvaluacionPaginadaDto filtro)
    {
        try
        {
            var result = await _service.ListarEvaluacionesPaginadasAsync(filtro);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar evaluaciones paginadas.");
            return Error500(ex);
        }
    }

    [HttpPost("evaluaciones")]
    [AuditRequired("Creación y cálculo de evaluación de riesgo")]
    public async Task<IActionResult> CrearEvaluacion([FromBody] EvaluacionRiesgoDto dto)
    {
        try
        {
            var result = await _service.CrearEvaluacionAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear evaluación de riesgo.");
            return Error500(ex);
        }
    }

    [HttpPut("evaluaciones/{id:long}")]
    [AuditRequired("Actualización y recálculo de evaluación de riesgo")]
    public async Task<IActionResult> ActualizarEvaluacion(long id, [FromBody] EvaluacionRiesgoDto dto)
    {
        if (id != dto.EvaId)
        {
            return BadRequest(new { success = false, mensaje = "El ID de la ruta no coincide con el ID del cuerpo." });
        }

        try
        {
            var result = await _service.ActualizarEvaluacionAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar evaluación ID {Id}", id);
            return Error500(ex);
        }
    }

    [HttpPost("evaluaciones/{id:long}/transiciones")]
    [AuditRequired("Transición de estado de la máquina de estados")]
    public async Task<IActionResult> TransicionarEstadoEvaluacion(long id, [FromQuery] string nuevoEstado, [FromQuery] string? motivo)
    {
        try
        {
            var result = await _service.TransicionarEstadoEvaluacionAsync(id, nuevoEstado, motivo, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al transicionar estado para la evaluación ID {Id}", id);
            return Error500(ex);
        }
    }

    [HttpGet("evaluaciones/{id:long}/revisiones")]
    public async Task<IActionResult> ObtenerRevisionesEvaluacion(long id)
    {
        try
        {
            var result = await _service.ObtenerRevisionesEvaluacionAsync(id);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de revisiones ID {Id}", id);
            return Error500(ex);
        }
    }

    // ============================================================
    // 3. ENDPOINTS DE VINCULACIÓN DE EVIDENCIAS TIPO DURO
    // ============================================================

    [HttpPost("evidencias/cargar")]
    [AuditRequired("Carga física de archivo de evidencia al servidor")]
    public async Task<IActionResult> CargarEvidencia(IFormFile archivo)
    {
        try
        {
            var result = await _service.CargarArchivoEvidenciaFisicaAsync(archivo, ObtenerUsuarioId());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar archivo de evidencia.");
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/riesgo")]
    [AuditRequired("Vinculación de evidencia a Riesgo")]
    public async Task<IActionResult> VincularEvidenciaRiesgo([FromBody] AsociarEvidenciaRiesgoDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaRiesgoAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/evaluacion")]
    [AuditRequired("Vinculación de evidencia a Evaluación")]
    public async Task<IActionResult> VincularEvidenciaEvaluacion([FromBody] AsociarEvidenciaEvaluacionDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaEvaluacionAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/control")]
    [AuditRequired("Vinculación de evidencia a Control")]
    public async Task<IActionResult> VincularEvidenciaControl([FromBody] AsociarEvidenciaControlDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaControlAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/plan")]
    [AuditRequired("Vinculación de evidencia a Plan de Acción")]
    public async Task<IActionResult> VincularEvidenciaPlan([FromBody] AsociarEvidenciaPlanDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaPlanAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/actividad")]
    [AuditRequired("Vinculación de evidencia a Actividad de Plan")]
    public async Task<IActionResult> VincularEvidenciaActividad([FromBody] AsociarEvidenciaActividadDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaActividadAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/alerta")]
    [AuditRequired("Vinculación de evidencia a Alerta")]
    public async Task<IActionResult> VincularEvidenciaAlerta([FromBody] AsociarEvidenciaAlertaDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaAlertaAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/automonitoreo")]
    [AuditRequired("Vinculación de evidencia a Automonitoreo")]
    public async Task<IActionResult> VincularEvidenciaAutomonitoreo([FromBody] AsociarEvidenciaAutomonitoreoDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaAutomonitoreoAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/revision")]
    [AuditRequired("Vinculación de evidencia a Revision de Evaluación")]
    public async Task<IActionResult> VincularEvidenciaRevision([FromBody] AsociarEvidenciaRevisionDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaRevisionAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpPost("evidencias/vincular/aprobacion")]
    [AuditRequired("Vinculación de evidencia a Aprobacion de Formulario")]
    public async Task<IActionResult> VincularEvidenciaAprobacion([FromBody] AsociarEvidenciaAprobacionDto dto)
    {
        try
        {
            var result = await _service.VincularEvidenciaAprobacionAsync(dto, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            return Error500(ex);
        }
    }

    [HttpDelete("evidencias/{id:long}")]
    [AuditRequired("Eliminación de archivo de evidencia física huérfana")]
    public async Task<IActionResult> EliminarEvidencia(long id)
    {
        try
        {
            var result = await _service.EliminarEvidenciaAsync(id, ObtenerUsuarioId(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar la evidencia ID {Id}", id);
            return Error500(ex);
        }
    }

    // ============================================================
    // 4. REPORTES CONSOLIDADOS Y AUXILIARES
    // ============================================================

    [HttpGet("consolidado")]
    public async Task<IActionResult> ObtenerConsolidado()
    {
        try
        {
            var result = await _service.ObtenerConsolidadoMatricesAsync();
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener matriz consolidada.");
            return Error500(ex);
        }
    }

    [HttpGet("metodologia/vigente")]
    public async Task<IActionResult> ObtenerMetodologiaVigente()
    {
        try
        {
            var result = await _service.ObtenerMetodologiaVigenteAsync();
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener metodología vigente.");
            return Error500(ex);
        }
    }

    // ============================================================
    // METODOS AUXILIARES DE SEGURIDAD E IP
    // ============================================================

    private long ObtenerUsuarioId()
    {
        return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private string? ObtenerIp()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp.Trim();

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private IActionResult Responder(ServiceResult result)
    {
        return result.Success
            ? Ok(new { success = true, mensaje = result.Message })
            : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
    }

    private IActionResult Responder<T>(ServiceResult<T> result)
    {
        return result.Success
            ? Ok(new { success = true, datos = result.Data, mensaje = result.Message })
            : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
    }

    private IActionResult Error500(Exception ex)
    {
        return StatusCode(500, new
        {
            success = false,
            mensaje = "Error interno en el módulo de Matrices de Riesgos.",
            traceId = HttpContext.TraceIdentifier
        });
    }
}
