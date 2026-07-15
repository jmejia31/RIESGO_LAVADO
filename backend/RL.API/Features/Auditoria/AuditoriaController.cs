using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Auditoria.Application;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Security;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RL.API.Features.Auditoria
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class AuditoriaController : ControllerBase
    {
        private readonly IAuditoriaService _service;

        public AuditoriaController(IAuditoriaService service)
        {
            _service = service;
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
                var (datos, total) = await _service.ObtenerBitacoraPaginadaAsync(pagina, limite, buscar, accion, modulo, tabla, fechaInicio, fechaFin);
                return Ok(new AuditoriaPaginadoDto
                {
                    Datos = datos,
                    TotalRegistros = total
                });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al obtener la bitácora");
                return StatusCode(500, new
                {
                    success = false,
                    mensaje = "Ocurrió un error interno al consultar la bitácora.",
                    traceId = HttpContext.TraceIdentifier
                });
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
            await _service.RegistrarExportacionAsync(dto, usuarioId, ip);
            return Ok(new { success = true, mensaje = "Auditoría de exportación registrada." });
        }

    }
}
