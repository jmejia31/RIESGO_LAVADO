using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RL.API.DTOs;
using RL.API.Security;
using RL.API.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ListasController : ControllerBase
    {
        private readonly IListasService _listasService;
        private readonly IEvidenciasService _evidenciasService;
        private readonly ICoincidenciasService _coincidenciasService;

        public ListasController(
            IListasService listasService,
            IEvidenciasService evidenciasService,
            ICoincidenciasService coincidenciasService)
        {
            _listasService = listasService;
            _evidenciasService = evidenciasService;
            _coincidenciasService = coincidenciasService;
        }

        // Este controlador funciona como frontera HTTP del módulo de listas.
        // Las validaciones críticas, evidencias, auditoría y calificaciones se delegan a servicios.

        [HttpGet("evidencias/politica")]
        [ModuloAuthorize(4)]
        public IActionResult ObtenerPoliticaEvidencias()
        {
            var politica = _evidenciasService.ObtenerPolitica();
            return Ok(new
            {
                success = true,
                datos = new
                {
                    maximoMb = politica.MaximoMb,
                    maximoBytes = politica.MaximoBytes,
                    extensionesPermitidas = politica.ExtensionesPermitidas,
                    tiposPermitidosTexto = politica.TiposPermitidosTexto
                }
            });
        }

        [HttpGet("juridicas")]
        [ModuloAuthorize(4)]
        public async Task<IActionResult> ObtenerJuridicas()
        {
            var result = await _listasService.ObtenerJuridicasAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("naturales")]
        [ModuloAuthorize(4)]
        public async Task<IActionResult> ObtenerNaturales()
        {
            var result = await _listasService.ObtenerNaturalesAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("empleados")]
        [ModuloAuthorize(4)]
        public async Task<IActionResult> ObtenerEmpleados()
        {
            var result = await _listasService.ObtenerEmpleadosAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("naturales/{numeroIdentificacion}/detalle")]
        [ModuloAuthorize(4)]
        public async Task<IActionResult> ObtenerDetalleNatural(string numeroIdentificacion)
        {
            var result = await _listasService.ObtenerDetalleNaturalAsync(numeroIdentificacion);
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("empleados/{numeroIdentificacion}/detalle")]
        [ModuloAuthorize(4)]
        public async Task<IActionResult> ObtenerDetalleEmpleado(string numeroIdentificacion)
        {
            var result = await _listasService.ObtenerDetalleEmpleadoAsync(numeroIdentificacion);
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("tipos-documento")]
        [ModuloAuthorize(4)]
        public async Task<IActionResult> ObtenerTiposDocumento()
        {
            var result = await _listasService.ObtenerTiposDocumentoAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("tipos-listas-cautela")]
        [ModuloAuthorize(4, 6, 7)]
        public async Task<IActionResult> ObtenerTiposListasCautela()
        {
            var result = await _listasService.ObtenerTiposListasCautelaAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("resumen")]
        [ModuloAuthorize(7)]
        public async Task<IActionResult> ObtenerResumenListas()
        {
            try
            {
                var result = await _listasService.ObtenerResumenListasAsync();
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenListas");
                return Error500(ex);
            }
        }

        [HttpGet("{id}/exportar")]
        [ModuloAuthorize(7)]
        [AuditRequired("Exportación de lista de cautela")]
        public async Task<IActionResult> ObtenerDetalleListaParaExportar(int id)
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _listasService.ObtenerDetalleListaParaExportarAsync(id, usuarioId, ip);
                return Ok(new { success = true, datos = result.Data });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerDetalleListaParaExportar para {Id}", id);
                return Error500(ex);
            }
        }

        [HttpPost("tipos-listas-cautela")]
        [ModuloAuthorize(6)]
        [AuditRequired("Creación de tipo de lista de cautela")]
        public async Task<IActionResult> CrearTipoListaCautela([FromBody] TipoListaCautelaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos o incompletos." });

            var result = await _listasService.CrearTipoListaCautelaAsync(dto, ObtenerUsuarioId());
            return result.Success
                ? Ok(new { success = true, mensaje = result.Message, datos = result.Data })
                : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
        }

        [HttpPut("tipos-listas-cautela/{id}")]
        [ModuloAuthorize(6)]
        [AuditRequired("Edición de tipo de lista de cautela")]
        public async Task<IActionResult> ActualizarTipoListaCautela(int id, [FromBody] TipoListaCautelaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos o incompletos." });

            var result = await _listasService.ActualizarTipoListaCautelaAsync(id, dto, ObtenerUsuarioId());
            return Responder(result);
        }

        [HttpDelete("tipos-listas-cautela/{id}")]
        [ModuloAuthorize(6)]
        [AuditRequired("Eliminación de tipo de lista de cautela")]
        public async Task<IActionResult> EliminarTipoListaCautela(int id)
        {
            try
            {
                var result = await _listasService.EliminarTipoListaCautelaAsync(id, ObtenerUsuarioId());
                return Responder(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al eliminar tipo de lista de cautela {Id}", id);
                return BadRequest(new { success = false, mensaje = "No se puede eliminar el tipo de lista porque está siendo referenciado por otros registros." });
            }
        }

        [HttpPost("positivos")]
        [ModuloAuthorize(4)]
        [AuditRequired("Registro de positivo en monitoreo de listas")]
        public async Task<IActionResult> RegistrarPositivo([FromBody] RegistrarPositivoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos" });

            var result = await _listasService.RegistrarPositivoAsync(dto, ObtenerUsuarioId());
            return Responder(result);
        }

        [HttpGet("positivos/{noDocumento}")]
        [ModuloAuthorize(4)]
        public async Task<IActionResult> ObtenerPositivoPorDocumento(string noDocumento)
        {
            var result = await _listasService.ObtenerPositivoPorDocumentoAsync(noDocumento);
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("positivos/{noDocumento}/seguimientos")]
        [ModuloAuthorize(4)]
        public async Task<IActionResult> ObtenerSeguimientos(string noDocumento, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            var result = await _listasService.ObtenerSeguimientosAsync(noDocumento, desde, hasta);
            return result.Success
                ? Ok(new { success = true, datos = result.Data })
                : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
        }

        [HttpPost("positivos/{noDocumento}/seguimientos")]
        [ModuloAuthorize(4)]
        [AuditRequired("Creación de seguimiento y evidencias")]
        public async Task<IActionResult> RegistrarSeguimiento(
            string noDocumento,
            [FromForm] string motivoIngreso,
            [FromForm] List<IFormFile>? archivos)
        {
            var result = await _evidenciasService.RegistrarSeguimientoAsync(noDocumento, motivoIngreso, archivos, ObtenerUsuarioId());
            return Responder(result);
        }

        [HttpGet("evidencias/{evidenciaId}")]
        [ModuloAuthorize(4)]
        [AuditRequired("Visualizacion o descarga de evidencia")]
        public async Task<IActionResult> DescargarEvidencia(long evidenciaId)
        {
            var result = await _evidenciasService.DescargarEvidenciaAsync(evidenciaId, ObtenerUsuarioId());
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });

            return File(result.Data.Bytes, result.Data.Mime, result.Data.Nombre);
        }

        [HttpPut("seguimientos/{detalleId}")]
        [ModuloAuthorize(4)]
        [AuditRequired("Edición de seguimiento y evidencias")]
        public async Task<IActionResult> ActualizarSeguimiento(
            long detalleId,
            [FromForm] string motivoIngreso,
            [FromForm] List<IFormFile>? archivos)
        {
            var result = await _evidenciasService.ActualizarSeguimientoAsync(detalleId, motivoIngreso, archivos, ObtenerUsuarioId());
            return Responder(result);
        }

        [HttpDelete("evidencias/{evidenciaId}")]
        [ModuloAuthorize(4)]
        [AuditRequired("Eliminación lógica de evidencia")]
        public async Task<IActionResult> EliminarEvidencia(long evidenciaId, [FromBody] MotivoEliminacionDto? dto)
        {
            var result = await _evidenciasService.EliminarEvidenciaAsync(evidenciaId, dto?.MotivoEliminacion, ObtenerUsuarioId());
            return Responder(result);
        }

        [HttpDelete("seguimientos/{detalleId}")]
        [ModuloAuthorize(4)]
        [AuditRequired("Eliminación lógica de seguimiento")]
        public async Task<IActionResult> EliminarSeguimiento(long detalleId, [FromBody] MotivoEliminacionDto? dto)
        {
            var result = await _evidenciasService.EliminarSeguimientoAsync(detalleId, dto?.MotivoEliminacion, ObtenerUsuarioId());
            return Responder(result);
        }

        [HttpPost("positivos/{noDocumento}/reporte-impreso")]
        [ModuloAuthorize(4)]
        [AuditRequired("Impresion o generacion de reporte")]
        public async Task<IActionResult> RegistrarReporteImpreso(string noDocumento, [FromBody] System.Text.Json.JsonElement data)
        {
            await _evidenciasService.RegistrarReporteImpresoAsync(noDocumento, data.ToString(), ObtenerUsuarioId());
            return Ok(new { success = true, mensaje = "Auditoría de reporte impreso registrada." });
        }

        [HttpGet("coincidencias-patrono/resumen")]
        [ModuloAuthorize(8)]
        public async Task<IActionResult> ObtenerResumenCoincidenciasPatrono()
        {
            try
            {
                var result = await _coincidenciasService.ObtenerResumenPatronoAsync();
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenCoincidenciasPatrono");
                return Error500(ex);
            }
        }

        [HttpGet("coincidencias-patrono/detalle")]
        [ModuloAuthorize(8)]
        public async Task<IActionResult> ObtenerDetalleCoincidenciasPatrono([FromQuery] string fecha)
        {
            try
            {
                var result = await _coincidenciasService.ObtenerDetallePatronoAsync(fecha);
                return result.Success
                    ? Ok(new { success = true, datos = result.Data })
                    : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerDetalleCoincidenciasPatrono para la fecha {Fecha}", fecha);
                return Error500(ex);
            }
        }

        public record CalificarRequest(int TipoCalificacionId);

        [HttpPut("coincidencias-patrono/{id}/calificar")]
        [ModuloAuthorize(8)]
        [AuditRequired("Calificacion de coincidencia de patrono")]
        public async Task<IActionResult> CalificarCoincidencia(long id, [FromBody] CalificarRequest body)
        {
            try
            {
                if (body is null)
                    return BadRequest(new { success = false, mensaje = "El cuerpo de la solicitud es requerido." });

                var result = await _coincidenciasService.CalificarAsync(id, body.TipoCalificacionId, ObtenerUsuarioId(), esEmpleado: false);
                return Responder(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al calificar la coincidencia {Id}", id);
                return Error500(ex);
            }
        }

        [HttpGet("coincidencias-patrono/resumen-match")]
        [ModuloAuthorize(8)]
        public async Task<IActionResult> ObtenerResumenMatchLista([FromQuery] long dataId, [FromQuery] string nombre)
        {
            try
            {
                var result = await _coincidenciasService.ObtenerResumenMatchListaAsync(dataId, nombre);
                return result.Success
                    ? Ok(new { success = true, detalle = result.Data })
                    : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenMatchLista para dataId {DataId} y nombre {Nombre}", dataId, nombre);
                return Error500(ex);
            }
        }

        [HttpGet("coincidencias-empleado/resumen-match")]
        [ModuloAuthorize(9)]
        public async Task<IActionResult> ObtenerResumenMatchListaEmpleado([FromQuery] long dataId, [FromQuery] string nombre)
        {
            try
            {
                var result = await _coincidenciasService.ObtenerResumenMatchListaAsync(dataId, nombre);
                return result.Success
                    ? Ok(new { success = true, detalle = result.Data })
                    : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenMatchListaEmpleado para dataId {DataId} y nombre {Nombre}", dataId, nombre);
                return Error500(ex);
            }
        }

        [HttpGet("coincidencias-empleado/resumen")]
        [ModuloAuthorize(9)]
        public async Task<IActionResult> ObtenerResumenCoincidenciasEmpleado()
        {
            try
            {
                var result = await _coincidenciasService.ObtenerResumenEmpleadoAsync();
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenCoincidenciasEmpleado");
                return Error500(ex);
            }
        }

        [HttpGet("coincidencias-empleado/detalle")]
        [ModuloAuthorize(9)]
        public async Task<IActionResult> ObtenerDetalleCoincidenciasEmpleado([FromQuery] string fecha)
        {
            try
            {
                var result = await _coincidenciasService.ObtenerDetalleEmpleadoAsync(fecha);
                return result.Success
                    ? Ok(new { success = true, datos = result.Data })
                    : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerDetalleCoincidenciasEmpleado para la fecha {Fecha}", fecha);
                return Error500(ex);
            }
        }

        [HttpPut("coincidencias-empleado/{id}/calificar")]
        [ModuloAuthorize(9)]
        [AuditRequired("Calificacion de coincidencia de empleado")]
        public async Task<IActionResult> CalificarCoincidenciaEmpleado(long id, [FromBody] CalificarRequest body)
        {
            try
            {
                if (body is null)
                    return BadRequest(new { success = false, mensaje = "El cuerpo de la solicitud es requerido." });

                var result = await _coincidenciasService.CalificarAsync(id, body.TipoCalificacionId, ObtenerUsuarioId(), esEmpleado: true);
                return Responder(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al calificar la coincidencia de empleado {Id}", id);
                return Error500(ex);
            }
        }

        public class UploadCautelaRequest
        {
            public IFormFile Archivo { get; set; } = null!;
            public int TipoListaCautelaId { get; set; }
        }

        [HttpPost("cautela/upload")]
        [ModuloAuthorize(7)]
        [AuditRequired("Carga de lista de cautela")]
        public async Task<IActionResult> UploadCautela([FromForm] UploadCautelaRequest request)
        {
            try
            {
                var result = await _listasService.ProcesarCargaCautelaAsync(request.Archivo, request.TipoListaCautelaId, ObtenerUsuarioId());
                return Responder(result);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en UploadCautela para el tipoListaCautelaId {Id}", request?.TipoListaCautelaId);
                return StatusCode(500, new
                {
                    success = false,
                    mensaje = "Ocurrió un error al procesar la carga del archivo.",
                    traceId = HttpContext.TraceIdentifier
                });
            }
        }

        private long ObtenerUsuarioId()
        {
            return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        }

        private IActionResult Responder(ServiceResult result)
        {
            return result.Success
                ? Ok(new { success = true, mensaje = result.Message })
                : StatusCode(result.StatusCode, new { success = false, mensaje = result.Message });
        }

        private IActionResult Error500(Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                mensaje = "Ocurrió un error interno al procesar la solicitud.",
                traceId = HttpContext.TraceIdentifier
            });
        }
    }
}
