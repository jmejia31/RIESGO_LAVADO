using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Security;
using RL.API.Services;

namespace RL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class CatalogosController : ControllerBase
{
    private readonly ICatalogoService _service;

    public CatalogosController(ICatalogoService service)
    {
        _service = service;
    }

    [HttpGet("roles")]
    [ModuloAuthorize(2)]
    public async Task<IActionResult> Roles()
    {
        var roles = await _service.ObtenerRolesAsync();
        return Ok(new { success = true, datos = roles.Select(r => new { rolId = r.Key, rolNombre = r.Value }) });
    }

    [HttpGet("dominios")]
    [ModuloAuthorize(2)]
    public async Task<IActionResult> Dominios()
    {
        var doms = await _service.ObtenerDominiosAsync();
        return Ok(new { success = true, datos = doms.Select(d => new { domId = d.Key, domNombre = d.Value }) });
    }

    [HttpGet("modulos")]
    public async Task<IActionResult> Modulos()
    {
        var mods = await _service.ObtenerModulosAsync();
        return Ok(new { 
            success = true, 
            datos = mods.Select(m => new { 
                modId = m.ModId, 
                modNombre = m.ModNombre, 
                modDescripcion = m.ModDescripcion, 
                modRuta = m.ModRuta, 
                modIcono = m.ModIcono, 
                modSeccion = m.ModSeccion 
            }) 
        });
    }
}
