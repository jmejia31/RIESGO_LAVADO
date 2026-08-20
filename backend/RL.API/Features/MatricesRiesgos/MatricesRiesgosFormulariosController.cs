using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Core.Security;
using RL.API.Features.MatricesRiesgos.Application;

namespace RL.API.Features.MatricesRiesgos;

/// <summary>
/// Expone lecturas puntuales de versiones de formulario para que el cliente
/// pueda reabrir exactamente el VER_JSON persistido de una versión concreta.
/// </summary>
[ApiController]
[Authorize]
[ModuloAuthorize(10)]
[Route("api/matrices-riesgos/formularios")]
[Produces("application/json")]
public sealed class MatricesRiesgosFormulariosController : ControllerBase
{
    private readonly IMatricesRiesgosAppService _service;

    public MatricesRiesgosFormulariosController(IMatricesRiesgosAppService service)
    {
        _service = service;
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> ObtenerVersionFormulario(long id)
    {
        var result = await _service.ObtenerVersionFormularioAsync(id);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new
            {
                success = false,
                mensaje = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            datos = result.Data,
            mensaje = result.Message
        });
    }
}
