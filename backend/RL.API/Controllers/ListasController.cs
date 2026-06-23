using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Repositories;
using RL.API.DTOs;
using System;
using System.IO;
using Microsoft.AspNetCore.Http;
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
        private const long MaxEvidenceFileBytes = 10 * 1024 * 1024;
        private static readonly Dictionary<string, string[]> AllowedEvidenceMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new[] { "application/pdf" },
            [".png"] = new[] { "image/png" },
            [".jpg"] = new[] { "image/jpeg", "image/pjpeg" },
            [".jpeg"] = new[] { "image/jpeg", "image/pjpeg" },
            [".doc"] = new[] { "application/msword", "application/octet-stream" },
            [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip", "application/octet-stream" },
            [".xls"] = new[] { "application/vnd.ms-excel", "application/octet-stream" },
            [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream" }
        };

        private readonly IListasRepository _repo;
        private readonly IAuditoriaRepository _auditoriaRepo;

        public ListasController(IListasRepository repo, IAuditoriaRepository auditoriaRepo)
        {
            _repo = repo;
            _auditoriaRepo = auditoriaRepo;
        }

        private static string? ValidarArchivosEvidencia(List<IFormFile>? archivos)
        {
            if (archivos == null || archivos.Count == 0) return null;

            foreach (var file in archivos)
            {
                var nombreOriginal = Path.GetFileName(file.FileName);
                if (string.IsNullOrWhiteSpace(nombreOriginal))
                    return "El nombre del archivo de evidencia no es valido.";

                if (nombreOriginal.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return $"El archivo {nombreOriginal} contiene caracteres no permitidos en el nombre.";

                if (file.Length <= 0)
                    return $"El archivo {nombreOriginal} esta vacio.";

                if (file.Length > MaxEvidenceFileBytes)
                    return $"El archivo {nombreOriginal} supera el limite de 10 MB.";

                var extension = Path.GetExtension(nombreOriginal);
                if (string.IsNullOrWhiteSpace(extension) || !AllowedEvidenceMimeTypes.TryGetValue(extension, out var mimeTypes))
                    return $"El archivo {nombreOriginal} tiene una extension no permitida.";

                var contentType = file.ContentType?.Trim();
                if (string.IsNullOrWhiteSpace(contentType))
                    return $"No se pudo identificar el tipo de contenido del archivo {nombreOriginal}.";

                if (!Array.Exists(mimeTypes, mime => string.Equals(mime, contentType, StringComparison.OrdinalIgnoreCase)))
                    return $"El archivo {nombreOriginal} tiene un tipo de contenido no permitido ({contentType}).";
            }

            return null;
        }

        private async Task GuardarArchivosEvidenciaAsync(long detalleId, List<IFormFile>? archivos, long usuarioId)
        {
            if (archivos == null || archivos.Count == 0) return;

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Evidencias");
            Directory.CreateDirectory(uploadDir);

            foreach (var file in archivos)
            {
                var nombreOriginal = Path.GetFileName(file.FileName);
                var extension = Path.GetExtension(nombreOriginal);
                var uniqueName = $"{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadDir, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                await _repo.GuardarEvidenciaMetaAsync(detalleId, nombreOriginal, file.ContentType, uniqueName, usuarioId);
            }
        }

        [HttpGet("juridicas")]
        public async Task<IActionResult> ObtenerJuridicas()
        {
            var result = await _repo.ObtenerJuridicasAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("naturales")]
        public async Task<IActionResult> ObtenerNaturales()
        {
            var result = await _repo.ObtenerNaturalesAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("empleados")]
        public async Task<IActionResult> ObtenerEmpleados()
        {
            var result = await _repo.ObtenerEmpleadosAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("naturales/{numeroIdentificacion}/detalle")]
        public async Task<IActionResult> ObtenerDetalleNatural(string numeroIdentificacion)
        {
            var result = await _repo.ObtenerDetalleNaturalAsync(numeroIdentificacion);
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("empleados/{numeroIdentificacion}/detalle")]
        public async Task<IActionResult> ObtenerDetalleEmpleado(string numeroIdentificacion)
        {
            var result = await _repo.ObtenerDetalleEmpleadoAsync(numeroIdentificacion);
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("tipos-documento")]
        public async Task<IActionResult> ObtenerTiposDocumento()
        {
            var result = await _repo.ObtenerTiposDocumentoAsync();
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("tipos-listas-cautela")]
        public async Task<IActionResult> ObtenerTiposListasCautela()
        {
            var result = await _repo.ObtenerTiposListasCautelaAsync();
            return Ok(new { success = true, datos = result });
        }

        [AllowAnonymous]
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumenListas()
        {
            try
            {
                var result = await _repo.ObtenerResumenListasAsync();
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenListas");
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }

        [HttpGet("{id}/exportar")]
        public async Task<IActionResult> ObtenerDetalleListaParaExportar(int id)
        {
            try
            {
                var result = await _repo.ObtenerDetalleListaParaExportarAsync(id);
                var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var auditoria = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    Accion = "EXPORTACION_EXCEL",
                    TipoListaCautelaId = id,
                    CantidadRegistros = result.Count
                });
                await _auditoriaRepo.RegistrarAsync("DNP_IHSS.LISTA_CAUTELA", id.ToString(), "VER", null, auditoria, usuarioId, null, ip, "ExportacionListas");
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerDetalleListaParaExportar para {Id}", id);
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }

        [HttpPost("tipos-listas-cautela")]
        public async Task<IActionResult> CrearTipoListaCautela([FromBody] TipoListaCautelaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos o incompletos." });

            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var newId = await _repo.CrearTipoListaCautelaAsync(dto.Descripcion, dto.TipoArchivo, dto.CantidadColumnas, usuarioId);

            if (newId > 0)
            {
                return Ok(new { success = true, mensaje = "Tipo de lista creado exitosamente.", datos = new { tipoListaCautelaId = newId, descripcion = dto.Descripcion, tipoArchivo = dto.TipoArchivo, cantidadColumnas = dto.CantidadColumnas } });
            }
            return BadRequest(new { success = false, mensaje = "No se pudo crear el tipo de lista." });
        }

        [HttpPut("tipos-listas-cautela/{id}")]
        public async Task<IActionResult> ActualizarTipoListaCautela(int id, [FromBody] TipoListaCautelaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos o incompletos." });

            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ok = await _repo.ActualizarTipoListaCautelaAsync(id, dto.Descripcion, dto.TipoArchivo, dto.CantidadColumnas, usuarioId);

            if (ok)
            {
                return Ok(new { success = true, mensaje = "Tipo de lista actualizado exitosamente." });
            }
            return NotFound(new { success = false, mensaje = "No se encontró el tipo de lista a actualizar o no hubo cambios." });
        }

        [HttpDelete("tipos-listas-cautela/{id}")]
        public async Task<IActionResult> EliminarTipoListaCautela(int id)
        {
            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            try
            {
                var ok = await _repo.EliminarTipoListaCautelaAsync(id, usuarioId);
                if (ok)
                {
                    return Ok(new { success = true, mensaje = "Tipo de lista eliminado exitosamente." });
                }
                return NotFound(new { success = false, mensaje = "No se encontró el tipo de lista o ya fue eliminado." });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al eliminar tipo de lista de cautela {Id}", id);
                return BadRequest(new { success = false, mensaje = "No se puede eliminar el tipo de lista porque está siendo referenciado por otros registros." });
            }
        }


        [HttpPost("positivos")]
        public async Task<IActionResult> RegistrarPositivo([FromBody] RegistrarPositivoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, mensaje = "Datos inválidos" });

            var creadoPorId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ok = await _repo.RegistrarPositivoAsync(dto, creadoPorId);
            return ok 
                ? Ok(new { success = true, mensaje = "Motivo registrado exitosamente." })
                : BadRequest(new { success = false, mensaje = "No se pudo registrar el motivo." });
        }

        [HttpGet("positivos/{noDocumento}")]
        public async Task<IActionResult> ObtenerPositivoPorDocumento(string noDocumento)
        {
            var result = await _repo.ObtenerPositivoPorDocumentoAsync(noDocumento);
            return Ok(new { success = true, datos = result });
        }

        [HttpGet("positivos/{noDocumento}/seguimientos")]
        public async Task<IActionResult> ObtenerSeguimientos(string noDocumento, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
                return BadRequest(new { success = false, mensaje = "La fecha desde no puede ser mayor que la fecha hasta." });

            var result = await _repo.ObtenerSeguimientosAsync(noDocumento, desde, hasta);
            return Ok(new { success = true, datos = result });
        }

        [HttpPost("positivos/{noDocumento}/seguimientos")]
        public async Task<IActionResult> RegistrarSeguimiento(
            string noDocumento,
            [FromForm] string motivoIngreso,
            [FromForm] List<IFormFile>? archivos)
        {
            if (string.IsNullOrWhiteSpace(motivoIngreso))
                return BadRequest(new { success = false, mensaje = "El comentario de seguimiento es obligatorio." });

            var positivoId = await _repo.ObtenerPositivoIdPorDocumentoAsync(noDocumento);
            if (!positivoId.HasValue)
                return NotFound(new { success = false, mensaje = "No se encontró un registro positivo activo para este documento." });

            var errorArchivo = ValidarArchivosEvidencia(archivos);
            if (errorArchivo != null)
                return BadRequest(new { success = false, mensaje = errorArchivo });

            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // 1. Registrar el seguimiento
            long detalleId = await _repo.RegistrarSeguimientoAsync(positivoId.Value, motivoIngreso, usuarioId);

            // 2. Guardar archivos de evidencia físicamente y sus metadatos
            await GuardarArchivosEvidenciaAsync(detalleId, archivos, usuarioId);

            return Ok(new { success = true, mensaje = "Seguimiento y evidencia registrados correctamente." });
        }

        [HttpGet("evidencias/{evidenciaId}")]
        public async Task<IActionResult> DescargarEvidencia(long evidenciaId)
        {
            var meta = await _repo.ObtenerEvidenciaPorIdAsync(evidenciaId);
            if (meta == null)
                return NotFound(new { success = false, mensaje = "Evidencia no encontrada." });

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Evidencias");
            var filePath = Path.Combine(uploadDir, meta.Value.Ruta);

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { success = false, mensaje = "El archivo físico no existe en el servidor." });

            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Registrar auditoría de visualización (VER)
            var dataJson = Newtonsoft.Json.JsonConvert.SerializeObject(new { NombreArchivo = meta.Value.Nombre, Ruta = meta.Value.Ruta });
            await _repo.RegistrarAuditoriaVisualizacionAsync(evidenciaId, dataJson, usuarioId);

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, meta.Value.Mime, meta.Value.Nombre);
        }

        [HttpPut("seguimientos/{detalleId}")]
        public async Task<IActionResult> ActualizarSeguimiento(
            long detalleId,
            [FromForm] string motivoIngreso,
            [FromForm] List<IFormFile>? archivos)
        {
            if (string.IsNullOrWhiteSpace(motivoIngreso))
                return BadRequest(new { success = false, mensaje = "El comentario de seguimiento es obligatorio." });

            var errorArchivo = ValidarArchivosEvidencia(archivos);
            if (errorArchivo != null)
                return BadRequest(new { success = false, mensaje = errorArchivo });

            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // 1. Actualizar el seguimiento
            bool ok = await _repo.ActualizarSeguimientoAsync(detalleId, motivoIngreso, usuarioId);
            if (!ok)
                return NotFound(new { success = false, mensaje = "No se encontró el seguimiento a actualizar." });

            // 2. Guardar nuevos archivos de evidencia físicamente y sus metadatos
            await GuardarArchivosEvidenciaAsync(detalleId, archivos, usuarioId);

            return Ok(new { success = true, mensaje = "Seguimiento actualizado correctamente." });
        }

        [HttpDelete("evidencias/{evidenciaId}")]
        public async Task<IActionResult> EliminarEvidencia(long evidenciaId)
        {
            var meta = await _repo.ObtenerEvidenciaPorIdAsync(evidenciaId);
            if (meta == null)
                return NotFound(new { success = false, mensaje = "Evidencia no encontrada en la base de datos." });

            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // 1. Eliminar de base de datos y auditar
            bool ok = await _repo.EliminarEvidenciaMetaAsync(evidenciaId, usuarioId);
            if (!ok)
                return BadRequest(new { success = false, mensaje = "No se pudo eliminar el registro de evidencia." });

            // 2. Conservar el archivo fisico; la eliminacion es solo logica.
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Evidencias");
            var filePath = Path.Combine(uploadDir, meta.Value.Ruta);

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    Serilog.Log.Information("Evidencia inactivada logicamente; archivo fisico conservado: {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error al eliminar archivo físico de evidencia: {FilePath}", filePath);
                }
            }

            return Ok(new { success = true, mensaje = "Evidencia eliminada correctamente." });
        }

        [HttpDelete("seguimientos/{detalleId}")]
        public async Task<IActionResult> EliminarSeguimiento(long detalleId)
        {
            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            bool ok = await _repo.EliminarSeguimientoLogicoAsync(detalleId, usuarioId);
            return ok 
                ? Ok(new { success = true, mensaje = "Seguimiento eliminado correctamente." })
                : NotFound(new { success = false, mensaje = "No se encontró el seguimiento o ya fue eliminado." });
        }

        [HttpPost("positivos/{noDocumento}/reporte-impreso")]
        public async Task<IActionResult> RegistrarReporteImpreso(string noDocumento, [FromBody] System.Text.Json.JsonElement data)
        {
            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            string dataJson = data.ToString();
            await _repo.RegistrarAuditoriaReporteImpresoAsync(noDocumento, dataJson, usuarioId);
            return Ok(new { success = true, mensaje = "Auditoría de reporte impreso registrada." });
        }

        [HttpGet("coincidencias-patrono/resumen")]
        public async Task<IActionResult> ObtenerResumenCoincidenciasPatrono()
        {
            try
            {
                var result = await _repo.ObtenerResumenCoincidenciasPatronoAsync();
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenCoincidenciasPatrono");
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }

        [HttpGet("coincidencias-patrono/detalle")]
        public async Task<IActionResult> ObtenerDetalleCoincidenciasPatrono([FromQuery] string fecha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fecha))
                    return BadRequest(new { success = false, mensaje = "El parámetro fecha es obligatorio (formato YYYY-MM-DD)." });

                var result = await _repo.ObtenerDetalleCoincidenciasPatronoAsync(fecha);
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerDetalleCoincidenciasPatrono para la fecha {Fecha}", fecha);
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }

        public record CalificarRequest(int TipoCalificacionId);

        [HttpPut("coincidencias-patrono/{id}/calificar")]
        public async Task<IActionResult> CalificarCoincidencia(long id, [FromBody] CalificarRequest body)
        {
            try
            {
                if (body is null)
                    return BadRequest(new { success = false, mensaje = "El cuerpo de la solicitud es requerido." });

                var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                bool ok = await _repo.CalificarCoincidenciaAsync(id, body.TipoCalificacionId, usuarioId);
                if (ok)
                {
                    return Ok(new { success = true, mensaje = "Coincidencia calificada exitosamente." });
                }
                return NotFound(new { success = false, mensaje = "No se encontró el registro de coincidencia especificado." });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al calificar la coincidencia {Id}", id);
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }

        [HttpGet("coincidencias-patrono/resumen-match")]
        public async Task<IActionResult> ObtenerResumenMatchLista([FromQuery] long dataId, [FromQuery] string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre))
                    return BadRequest(new { success = false, mensaje = "El parámetro nombre es requerido." });

                var detail = await _repo.ObtenerResumenMatchListaAsync(dataId, nombre);
                return Ok(new { success = true, detalle = detail });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenMatchLista para dataId {DataId} y nombre {Nombre}", dataId, nombre);
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }

        [HttpGet("coincidencias-empleado/resumen")]
        public async Task<IActionResult> ObtenerResumenCoincidenciasEmpleado()
        {
            try
            {
                var result = await _repo.ObtenerResumenCoincidenciasEmpleadoAsync();
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerResumenCoincidenciasEmpleado");
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }

        [HttpGet("coincidencias-empleado/detalle")]
        public async Task<IActionResult> ObtenerDetalleCoincidenciasEmpleado([FromQuery] string fecha)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fecha))
                    return BadRequest(new { success = false, mensaje = "El parámetro fecha es obligatorio (formato YYYY-MM-DD)." });

                var result = await _repo.ObtenerDetalleCoincidenciasEmpleadoAsync(fecha);
                return Ok(new { success = true, datos = result });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en ObtenerDetalleCoincidenciasEmpleado para la fecha {Fecha}", fecha);
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }

        [HttpPut("coincidencias-empleado/{id}/calificar")]
        public async Task<IActionResult> CalificarCoincidenciaEmpleado(long id, [FromBody] CalificarRequest body)
        {
            try
            {
                if (body is null)
                    return BadRequest(new { success = false, mensaje = "El cuerpo de la solicitud es requerido." });

                var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                // Usa el mismo método del repositorio que ya apunta a RL_CALIFICACIONES_COINCIDENCIAS
                bool ok = await _repo.CalificarCoincidenciaAsync(id, body.TipoCalificacionId, usuarioId);
                if (ok)
                {
                    return Ok(new { success = true, mensaje = "Coincidencia calificada exitosamente." });
                }
                return NotFound(new { success = false, mensaje = "No se encontró el registro de coincidencia especificado." });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al calificar la coincidencia de empleado {Id}", id);
                return StatusCode(500, new { success = false, mensaje = ex.Message, detalle = ex.ToString() });
            }
        }
        public class UploadCautelaRequest
        {
            public Microsoft.AspNetCore.Http.IFormFile Archivo { get; set; } = null!;
            public int TipoListaCautelaId { get; set; }
        }

        [HttpPost("cautela/upload")]
        public async Task<IActionResult> UploadCautela([FromForm] UploadCautelaRequest request)
        {
            try
            {
                var result = await _repo.ValidarArchivoCautelaAsync(request.Archivo, request.TipoListaCautelaId);
                if (!result.EsValido)
                {
                    return BadRequest(new { success = false, mensaje = result.Mensaje });
                }
                var usuarioId = Convert.ToInt64(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                
                // Si la validación es exitosa, procesamos el archivo según la extensión
                var extension = System.IO.Path.GetExtension(request.Archivo.FileName).ToLower();
                (bool Success, string Mensaje) processResult;

                if (extension == ".xml")
                {
                    processResult = await _repo.ProcesarArchivoXmlOnuAsync(request.Archivo, request.TipoListaCautelaId, usuarioId);
                }
                else if (extension == ".xls" || extension == ".xlsx")
                {
                    var descripcion = await _repo.ObtenerDescripcionListaAsync(request.TipoListaCautelaId);
                    // Si es Lista Engel, usamos su formato específico.
                    // Para PEPS, Extraditables y cualquier otra lista nueva, usamos el formato PEPS por defecto.
                    if (descripcion.ToUpper().Contains("ENGEL"))
                    {
                        processResult = await _repo.ProcesarArchivoExcelEngelAsync(request.Archivo, request.TipoListaCautelaId, usuarioId);
                    }
                    else
                    {
                        processResult = await _repo.ProcesarArchivoExcelPepsAsync(request.Archivo, request.TipoListaCautelaId, usuarioId);
                    }
                }
                else
                {
                    processResult = await _repo.ProcesarArchivoCsvOfacAsync(request.Archivo, request.TipoListaCautelaId, usuarioId);
                }

                if (!processResult.Success)
                {
                    return BadRequest(new { success = false, mensaje = processResult.Mensaje });
                }

                return Ok(new { success = true, mensaje = processResult.Mensaje });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error en UploadCautela para el tipoListaCautelaId {Id}", request?.TipoListaCautelaId);
                return StatusCode(500, new { success = false, mensaje = "Ocurrió un error al procesar la carga del archivo.", detalle = ex.Message });
            }
        }
    }
}
