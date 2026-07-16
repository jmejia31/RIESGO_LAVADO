using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Core.Security;
using System.Security.Claims;

namespace RL.API.Features.Configuracion;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionService _service;

    public ConfiguracionController(IConfiguracionService service)
    {
        _service = service;
    }

    [HttpGet("sistema")]
    [AllowAnonymous]
    public async Task<IActionResult> Sistema()
    {
        var config = await _service.ObtenerConfigSistemaAsync();
        if (config == null) return NotFound(new { success = false, mensaje = "Configuración no encontrada" });

        return Ok(new { success = true, datos = new {
            nombreInstitucion = config.NombreInstitucion,
            nombreSistema = config.NombreSistema,
            logoUrl = config.LogoUrl,
            iconoUrl = config.IconoUrl,
            colorPrimario = config.ColorPrimario,
            colorSecundario = config.ColorSecundario,
            timeoutSesion = config.TimeoutSesion,
            acuerdoLegal = config.AcuerdoLegal,
            maxIntentos = config.MaxIntentos
        }});
    }

    [HttpPut("sistema")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    [AuditRequired("Cambio de configuracion del sistema")]
    public async Task<IActionResult> GuardarSistema([FromBody] ConfigSistema config)
    {
        if (config == null) return BadRequest(new { success = false, mensaje = "Datos inválidos" });

        var ok = await _service.GuardarConfigSistemaAsync(config, ObtenerUsuarioId(), ObtenerIp());
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo actualizar la configuración" });

        return Ok(new { success = true, mensaje = "Configuración actualizada exitosamente" });
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginSlides()
    {
        var slides = await _service.ObtenerSlidesAsync();
        return Ok(new { success = true, datos = slides.Select(s => new {
            id = s.Id,
            imagenUrl = s.ImagenUrl,
            titulo = s.Titulo,
            descripcion = s.Descripcion,
            orden = s.Orden,
            activo = s.Activo,
            imagenIcono = s.ImagenIcono
        })});
    }

    [HttpGet("slides")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    public async Task<IActionResult> TodosSlides()
    {
        var slides = await _service.ObtenerTodosSlidesAsync();
        return Ok(new { success = true, datos = slides });
    }

    [HttpPost("slides")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    [AuditRequired("Creación de slide de configuración")]
    public async Task<IActionResult> CrearSlide([FromBody] LoginSlide slide)
    {
        if (slide == null) return BadRequest(new { success = false, mensaje = "Datos inválidos" });
        
        var ok = await _service.CrearSlideAsync(slide, ObtenerUsuarioId(), ObtenerIp());
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo crear el slide" });

        return Ok(new { success = true, mensaje = "Slide creado exitosamente" });
    }

    [HttpPut("slides/{id}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    [AuditRequired("Edición de slide de configuración")]
    public async Task<IActionResult> ActualizarSlide(int id, [FromBody] LoginSlide slide)
    {
        if (slide == null) return BadRequest(new { success = false, mensaje = "Datos inválidos" });
        var ok = await _service.ActualizarSlideAsync(id, slide, ObtenerUsuarioId(), ObtenerIp());
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo actualizar el slide" });

        return Ok(new { success = true, mensaje = "Slide actualizado exitosamente" });
    }

    [HttpDelete("slides/{id}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    [AuditRequired("Eliminación de slide de configuración")]
    public async Task<IActionResult> EliminarSlide(int id)
    {
        var ok = await _service.EliminarSlideAsync(id, ObtenerUsuarioId(), ObtenerIp());
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo eliminar el slide o no existe" });

        return Ok(new { success = true, mensaje = "Slide eliminado exitosamente" });
    }

    [HttpPost("slides/upload")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    [AuditRequired("Carga de imagen de configuracion")]
    public async Task<IActionResult> SubirImagen(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { success = false, mensaje = "No se ha seleccionado ningún archivo o el archivo está vacío" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { success = false, mensaje = "Tipo de archivo no permitido. Solo se permiten imágenes (jpg, jpeg, png, gif, webp)" });

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var nombreUnico = $"{Guid.NewGuid()}{extension}";
        var rutaCompleta = Path.Combine(uploadsFolder, nombreUnico);

        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        var urlRelativa = $"/uploads/{nombreUnico}";
        await _service.RegistrarCargaImagenAsync(
            archivo.FileName,
            nombreUnico,
            urlRelativa,
            archivo.ContentType,
            archivo.Length,
            ObtenerUsuarioId(),
            ObtenerIp());

        return Ok(new { success = true, url = urlRelativa });
    }

    private long ObtenerUsuarioId()
    {
        return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private string? ObtenerIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
