using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
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
[ModuloAuthorize(10)]
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
    // 1. ADMINISTRACIÓN DEL CICLO DE VIDA DEL FORMULARIO
    // ============================================================

    [HttpPost("formularios/borrador")]
    [Authorize(Roles = SystemRoles.Administrador)]
    [AuditRequired("Creación de borrador de formulario")]
    public async Task<IActionResult> CrearBorradorFormulario(
        [FromQuery] long familiaId,
        [FromQuery] string codigoFormulario,
        [FromBody] JsonDocument jsonConfig)
    {
        try
        {
            var result = await _service.CrearBorradorFormularioAsync(
                familiaId,
                codigoFormulario,
                jsonConfig.RootElement.GetRawText(),
                ObtenerUsuarioId());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear borrador de formulario.");
            return Error500(ex);
        }
    }

    [HttpPost("formularios/{id:long}/clonar")]
    [Authorize(Roles = SystemRoles.Administrador)]
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
    [Authorize(Roles = SystemRoles.Administrador)]
    [AuditRequired("Actualización de borrador de formulario")]
    public async Task<IActionResult> ActualizarBorradorFormulario(long id, [FromBody] JsonDocument jsonConfig)
    {
        try
        {
            var result = await _service.ActualizarBorradorFormularioAsync(id, jsonConfig.RootElement.GetRawText(), ObtenerUsuarioId());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar borrador de formulario ID {Id}", id);
            return Error500(ex);
        }
    }

    [HttpPost("formularios/{id:long}/publicar")]
    [Authorize(Roles = SystemRoles.Administrador)]
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
    [Authorize(Roles = SystemRoles.Administrador)]
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
    // 2. EVALUACIONES E HISTORIAL
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

    // ============================================================
    // 3. VINCULACIÓN DE EVIDENCIAS
    // ============================================================

    [HttpPost("evidencias/vinculos")]
    [AuditRequired("Vínculo genérico de evidencia")]
    public async Task<IActionResult> VincularEvidencia([FromBody] VincularEvidenciaDto dto)
    {
        try
        {
            return Responder(await _service.VincularEvidenciaAsync(dto, ObtenerUsuarioId(), ObtenerIp()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al vincular evidencia {EvidenciaId}", dto.EvidenciaId);
            return Error500(ex);
        }
    }

    [HttpGet("evaluaciones/{id:long}/flujos")]
    public async Task<IActionResult> ObtenerFlujosEvaluacion(long id) =>
        Responder(await _service.ObtenerFlujosEvaluacionAsync(id));

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

    [HttpDelete("evidencias/{id:long}")]
    [AuditRequired("Eliminación de archivo de evidencia física huérfana")]
    public async Task<IActionResult> EliminarEvidencia(long id)
    {
        try
        {
            return Responder(await _service.EliminarEvidenciaAsync(id, ObtenerUsuarioId(), ObtenerIp()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar la evidencia ID {Id}", id);
            return Error500(ex);
        }
    }

    // ============================================================
    // 4. REPORTES Y METODOLOGÍA CON CONTRATOS NEUTROS
    // ============================================================

    [HttpGet("consolidado")]
    public async Task<IActionResult> ObtenerConsolidado()
    {
        try
        {
            var result = await _service.ObtenerConsolidadoTipadoAsync();
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener matriz consolidada tipada.");
            return Error500(ex);
        }
    }

    [HttpGet("metodologia/vigente")]
    public async Task<IActionResult> ObtenerMetodologiaVigente()
    {
        try
        {
            var result = await _service.ObtenerMetodologiaDinamicaVigenteAsync();
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener metodología dinámica vigente.");
            return Error500(ex);
        }
    }

    private long ObtenerUsuarioId()
    {
        return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private string? ObtenerIp()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.Trim();
        }

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
