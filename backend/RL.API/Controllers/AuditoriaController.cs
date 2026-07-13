using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Repositories;
using RL.API.DTOs;
using RL.API.Security;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RL.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AuditoriaController : ControllerBase
    {
        private readonly IAuditoriaRepository _repo;

        public AuditoriaController(IAuditoriaRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        [ModuloAuthorize(5)]
        public async Task<IActionResult> ObtenerBitacora(
            [FromQuery] int pagina = 1,
            [FromQuery] int limite = 10,
            [FromQuery] string? buscar = null,
            [FromQuery] string? accion = null,
            [FromQuery] string? modulo = null,
            [FromQuery] string? tabla = null,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var (datos, total) = await _repo.ObtenerBitacoraPaginadaAsync(pagina, limite, buscar, accion, modulo, tabla, fechaInicio, fechaFin);
                return Ok(new AuditoriaPaginadoDto
                {
                    Datos = datos,
                    TotalRegistros = total
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, mensaje = "Error al obtener la bitácora: " + ex.Message });
            }
        }
        [HttpPost("exportacion")]
        [ModuloAuthorize(4, 5, 7, 8, 9)]
        [AuditRequired("Exportación Excel/PDF o generación de reporte")]
        public async Task<IActionResult> RegistrarExportacion([FromBody] RegistrarExportacionAuditoriaDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Tabla) || string.IsNullOrWhiteSpace(dto.RegistroId))
                return BadRequest(new { success = false, mensaje = "Datos de auditoría inválidos." });

            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var accionDetalle = ObtenerAccionDetalle(dto);
            var datos = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Accion = accionDetalle,
                dto.Detalle
            });

            await _repo.RegistrarAsync(dto.Tabla, dto.RegistroId, "VER", null, datos, usuarioId, null, ip, dto.Modulo);
            return Ok(new { success = true, mensaje = "Auditoría de exportación registrada." });
        }

        private static string ObtenerAccionDetalle(RegistrarExportacionAuditoriaDto dto)
        {
            if (dto.Detalle.TryGetValue("accion", out var accion) && accion != null)
            {
                var accionTexto = accion.ToString();
                if (!string.IsNullOrWhiteSpace(accionTexto))
                    return accionTexto;
            }

            if (dto.Detalle.TryGetValue("tipoReporte", out var tipoReporte) &&
                tipoReporte?.ToString()?.Contains("PDF", StringComparison.OrdinalIgnoreCase) == true)
                return "GENERACION_REPORTE_PDF";

            if (dto.Detalle.TryGetValue("archivo", out var archivo) &&
                archivo?.ToString()?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true)
                return "EXPORTACION_PDF";

            return "EXPORTACION_EXCEL";
        }
    }
}
