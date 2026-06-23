using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Repositories;
using RL.API.DTOs;
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
        public async Task<IActionResult> RegistrarExportacion([FromBody] RegistrarExportacionAuditoriaDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Tabla) || string.IsNullOrWhiteSpace(dto.RegistroId))
                return BadRequest(new { success = false, mensaje = "Datos de auditoria invalidos." });

            var usuarioId = Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var datos = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                Accion = "EXPORTACION_EXCEL",
                dto.Detalle
            });

            await _repo.RegistrarAsync(dto.Tabla, dto.RegistroId, "VER", null, datos, usuarioId, null, ip, dto.Modulo);
            return Ok(new { success = true, mensaje = "Auditoria de exportacion registrada." });
        }
    }
}
