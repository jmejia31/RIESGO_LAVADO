using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Auditoria.Application;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Core.Security;
using RL.API.Infrastructure.Http;
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
        [HttpGet("entidad")]
        [ModuloAuthorize(5)]
        public async Task<IActionResult> ObtenerHistorialEntidad(
            [FromQuery] string tabla,
            [FromQuery] string registroId,
            [FromQuery] string? registroAlternativo = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int limite = 25)
        {
            if (string.IsNullOrWhiteSpace(tabla) || string.IsNullOrWhiteSpace(registroId))
                return BadRequest(new { success = false, mensaje = "La tabla y el identificador de entidad son obligatorios." });

            try
            {
                var (datos, total) = await _service.ObtenerPorEntidadPaginadaAsync(tabla.Trim(), registroId.Trim(), registroAlternativo?.Trim(), pagina, limite);
                return Ok(new AuditoriaPaginadoDto { Datos = datos, TotalRegistros = total });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al obtener el historial de auditorÃ­a de {Tabla}/{RegistroId}", tabla, registroId);
                return StatusCode(500, new { success = false, mensaje = "OcurriÃ³ un error interno al consultar el historial.", traceId = HttpContext.TraceIdentifier });
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
            var ip = HttpContext.GetClientIp();
            await _service.RegistrarExportacionAsync(dto, usuarioId, ip);
            return Ok(new { success = true, mensaje = "Auditoría de exportación registrada." });
        }

    }
}
