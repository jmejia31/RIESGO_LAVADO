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
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var (datos, total) = await _repo.ObtenerBitacoraPaginadaAsync(pagina, limite, buscar, accion, modulo, fechaInicio, fechaFin);
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
    }
}
