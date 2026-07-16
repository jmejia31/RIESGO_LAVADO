using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Core.Security;
using RL.API.Shared.Results;
using System.Security.Claims;

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
            _logger.LogError(ex, "Error al obtener la metodología vigente de Matrices de Riesgos");
            return Error500(ex);
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> ObtenerDashboard()
    {
        try
        {
            var result = await _service.ObtenerDashboardAsync();
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard de Matrices de Riesgos");
            return Error500(ex);
        }
    }

    [HttpGet("reportes")]
    public async Task<IActionResult> ObtenerReporte(
        [FromQuery] string? buscar = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? sujetoTipo = null,
        [FromQuery] string? nivelResidual = null,
        [FromQuery] string? modeloVersion = null,
        [FromQuery] string? responsable = null,
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null)
    {
        try
        {
            var result = await _service.ObtenerReporteAsync(new MatrizRiesgoReporteFiltroDto
            {
                Buscar = buscar,
                Estado = estado,
                SujetoTipo = sujetoTipo,
                NivelResidual = nivelResidual,
                ModeloVersion = modeloVersion,
                Responsable = responsable,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reporte de Matrices de Riesgos");
            return Error500(ex);
        }
    }

    [HttpGet("reportes/exportar")]
    [AuditRequired("Exportación de reporte de matrices de riesgos")]
    public async Task<IActionResult> ExportarReporte(
        [FromQuery] string formato = "EXCEL",
        [FromQuery] string? buscar = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? sujetoTipo = null,
        [FromQuery] string? nivelResidual = null,
        [FromQuery] string? modeloVersion = null,
        [FromQuery] string? responsable = null,
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null)
    {
        try
        {
            var result = await _service.ExportarReporteAsync(new MatrizRiesgoReporteFiltroDto
            {
                Buscar = buscar,
                Estado = estado,
                SujetoTipo = sujetoTipo,
                NivelResidual = nivelResidual,
                ModeloVersion = modeloVersion,
                Responsable = responsable,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            }, formato, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());

            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });

            return File(result.Data.Contenido, result.Data.ContentType, result.Data.NombreArchivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar reporte de Matrices de Riesgos");
            return Error500(ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? buscar = null,
        [FromQuery] string? estado = null,
        [FromQuery] string? sujetoTipo = null,
        [FromQuery] DateTime? fechaInicio = null,
        [FromQuery] DateTime? fechaFin = null)
    {
        try
        {
            var result = await _service.ListarAsync(new MatrizRiesgoFiltroDto
            {
                Buscar = buscar,
                Estado = estado,
                SujetoTipo = sujetoTipo,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            });
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar Matrices de Riesgos");
            return Error500(ex);
        }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id)
    {
        try
        {
            var result = await _service.ObtenerAsync(id);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener matriz de riesgos {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpPost]
    [AuditRequired("Creación de matriz de riesgos")]
    public async Task<IActionResult> Crear([FromBody] MatrizRiesgoCrearRequestDto dto)
    {
        try
        {
            var result = await _service.CrearAsync(dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear matriz de riesgos");
            return Error500(ex);
        }
    }

    [HttpPut("{id:long}")]
    [AuditRequired("Actualizacion de matriz de riesgos")]
    public async Task<IActionResult> Actualizar(long id, [FromBody] MatrizRiesgoCrearRequestDto dto)
    {
        try
        {
            var result = await _service.ActualizarAsync(id, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar matriz de riesgos {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpPost("{id:long}/calcular")]
    [AuditRequired("Cálculo de matriz de riesgos")]
    public async Task<IActionResult> Calcular(long id, [FromBody] MatrizRiesgoCalcularRequestDto dto)
    {
        try
        {
            var result = await _service.CalcularAsync(id, dto ?? new MatrizRiesgoCalcularRequestDto(), esRecalculo: false, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al calcular matriz de riesgos {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpPost("{id:long}/recalcular")]
    [AuditRequired("Recálculo de matriz de riesgos")]
    public async Task<IActionResult> Recalcular(long id, [FromBody] MatrizRiesgoCalcularRequestDto dto)
    {
        try
        {
            var result = await _service.CalcularAsync(id, dto ?? new MatrizRiesgoCalcularRequestDto(), esRecalculo: true, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al recalcular matriz de riesgos {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpPut("{id:long}/estado")]
    [AuditRequired("Cambio de estado de matriz de riesgos")]
    public async Task<IActionResult> CambiarEstado(long id, [FromBody] MatrizRiesgoCambiarEstadoRequestDto dto)
    {
        try
        {
            var result = await _service.CambiarEstadoAsync(id, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado de matriz de riesgos {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpPut("{id:long}/eliminar")]
    [AuditRequired("Eliminación lógica de matriz de riesgos")]
    public async Task<IActionResult> EliminarMatriz(long id, [FromBody] MatrizRiesgoInactivarRequestDto dto)
    {
        try
        {
            var result = await _service.EliminarMatrizAsync(id, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar matriz de riesgos {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpGet("{id:long}/historial")]
    public async Task<IActionResult> ObtenerHistorial(long id)
    {
        try
        {
            var result = await _service.ObtenerHistorialAsync(id);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de matriz de riesgos {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpGet("{id:long}/planes")]
    public async Task<IActionResult> ListarPlanes(long id)
    {
        try
        {
            var result = await _service.ListarPlanesAsync(id);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar planes de acción de matriz {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpPost("{id:long}/planes")]
    [AuditRequired("Creación de plan de acción de matriz de riesgos")]
    public async Task<IActionResult> CrearPlan(long id, [FromBody] MatrizRiesgoPlanAccionRequestDto dto)
    {
        try
        {
            var result = await _service.CrearPlanAsync(id, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear plan de acción de matriz {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpPut("{id:long}/planes/{planId:long}")]
    [AuditRequired("Actualización de plan de acción de matriz de riesgos")]
    public async Task<IActionResult> ActualizarPlan(long id, long planId, [FromBody] MatrizRiesgoPlanAccionRequestDto dto)
    {
        try
        {
            var result = await _service.ActualizarPlanAsync(id, planId, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar plan {PlanId} de matriz {MatrizId}", planId, id);
            return Error500(ex);
        }
    }

    [HttpPut("{id:long}/planes/{planId:long}/estado")]
    [AuditRequired("Cambio de estado de plan de acción de matriz de riesgos")]
    public async Task<IActionResult> CambiarEstadoPlan(long id, long planId, [FromBody] MatrizRiesgoPlanEstadoRequestDto dto)
    {
        try
        {
            var result = await _service.CambiarEstadoPlanAsync(id, planId, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar estado del plan {PlanId} de matriz {MatrizId}", planId, id);
            return Error500(ex);
        }
    }

    [HttpPut("{id:long}/planes/{planId:long}/inactivar")]
    [AuditRequired("Inactivación de plan de acción de matriz de riesgos")]
    public async Task<IActionResult> InactivarPlan(long id, long planId, [FromBody] MatrizRiesgoInactivarRequestDto dto)
    {
        try
        {
            var result = await _service.InactivarPlanAsync(id, planId, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inactivar plan {PlanId} de matriz {MatrizId}", planId, id);
            return Error500(ex);
        }
    }

    [HttpGet("{id:long}/evidencias")]
    public async Task<IActionResult> ListarEvidencias(long id)
    {
        try
        {
            var result = await _service.ListarEvidenciasAsync(id);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar evidencias de matriz {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpPost("{id:long}/evidencias")]
    [Consumes("multipart/form-data")]
    [AuditRequired("Carga de evidencia de matriz de riesgos")]
    public async Task<IActionResult> CargarEvidencia(long id, [FromForm] long? controlId, [FromForm] long? planId, [FromForm] IFormFile? archivo)
    {
        try
        {
            var result = await _service.CargarEvidenciaAsync(id, controlId, planId, archivo, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar evidencia de matriz {MatrizId}", id);
            return Error500(ex);
        }
    }

    [HttpGet("{id:long}/evidencias/{evidenciaId:long}/descargar")]
    [AuditRequired("Descarga de evidencia de matriz de riesgos")]
    public async Task<IActionResult> DescargarEvidencia(long id, long evidenciaId)
    {
        try
        {
            var result = await _service.DescargarEvidenciaAsync(id, evidenciaId, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });

            return File(result.Data.Contenido, result.Data.ContentType, result.Data.NombreArchivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al descargar evidencia {EvidenciaId} de matriz {MatrizId}", evidenciaId, id);
            return Error500(ex);
        }
    }

    [HttpPut("{id:long}/evidencias/{evidenciaId:long}/inactivar")]
    [AuditRequired("Eliminación lógica de evidencia de matriz de riesgos")]
    public async Task<IActionResult> InactivarEvidencia(long id, long evidenciaId, [FromBody] MatrizRiesgoInactivarRequestDto dto)
    {
        try
        {
            var result = await _service.InactivarEvidenciaAsync(id, evidenciaId, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inactivar evidencia {EvidenciaId} de matriz {MatrizId}", evidenciaId, id);
            return Error500(ex);
        }
    }

    [HttpGet("criterios")]
    public async Task<IActionResult> ListarCriterios([FromQuery] bool incluirInactivos = false)
    {
        try
        {
            var result = await _service.ListarCriteriosAsync(incluirInactivos);
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar criterios de Matrices de Riesgos");
            return Error500(ex);
        }
    }

    [HttpPost("criterios")]
    [AuditRequired("Creacion de criterio de matriz de riesgos")]
    public async Task<IActionResult> CrearCriterio([FromBody] MatrizRiesgoCriterioRequestDto dto)
    {
        try
        {
            var result = await _service.CrearCriterioAsync(dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear criterio de Matrices de Riesgos");
            return Error500(ex);
        }
    }

    [HttpPut("criterios/{criterioId:long}")]
    [AuditRequired("Actualizacion de criterio de matriz de riesgos")]
    public async Task<IActionResult> ActualizarCriterio(long criterioId, [FromBody] MatrizRiesgoCriterioRequestDto dto)
    {
        try
        {
            var result = await _service.ActualizarCriterioAsync(criterioId, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar criterio de Matrices de Riesgos {CriterioId}", criterioId);
            return Error500(ex);
        }
    }

    [HttpPut("criterios/{criterioId:long}/inactivar")]
    [AuditRequired("Inactivacion de criterio de matriz de riesgos")]
    public async Task<IActionResult> InactivarCriterio(long criterioId, [FromBody] MatrizRiesgoInactivarRequestDto dto)
    {
        try
        {
            var result = await _service.InactivarCriterioAsync(criterioId, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inactivar criterio de Matrices de Riesgos {CriterioId}", criterioId);
            return Error500(ex);
        }
    }

    [HttpPut("criterios/{criterioId:long}/eliminar")]
    [AuditRequired("Eliminacion de criterio de matriz de riesgos")]
    public async Task<IActionResult> EliminarCriterio(long criterioId, [FromBody] MatrizRiesgoInactivarRequestDto dto)
    {
        try
        {
            var result = await _service.EliminarCriterioAsync(criterioId, dto, ObtenerUsuarioId(), ObtenerUsuarioEmail(), ObtenerIp());
            return Responder(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar criterio de Matrices de Riesgos {CriterioId}", criterioId);
            return Error500(ex);
        }
    }

    private long ObtenerUsuarioId()
    {
        return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private string? ObtenerUsuarioEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
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
            mensaje = "Error interno en Matrices de Riesgos.",
            traceId = HttpContext.TraceIdentifier
        });
    }
}
