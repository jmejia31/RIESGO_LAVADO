using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Repositories;
using RL.API.Security;
using System.Security.Claims;

namespace RL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionRepository _repo;
    private readonly IAuditoriaRepository _auditoriaRepo;

    public ConfiguracionController(IConfiguracionRepository repo, IAuditoriaRepository auditoriaRepo)
    {
        _repo = repo;
        _auditoriaRepo = auditoriaRepo;
    }

    [HttpGet("sistema")]
    [AllowAnonymous]
    public async Task<IActionResult> Sistema()
    {
        var config = await _repo.ObtenerConfigSistemaAsync();
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
    public async Task<IActionResult> GuardarSistema([FromBody] Models.ConfigSistema config)
    {
        if (config == null) return BadRequest(new { success = false, mensaje = "Datos inválidos" });

        var anterior = await _repo.ObtenerConfigSistemaAsync();
        var ok = await _repo.GuardarConfigSistemaAsync(config);
        if (ok)
        {
            await _auditoriaRepo.RegistrarAsync(
                "RL_CONFIG_SISTEMA",
                "1",
                "UPDATE",
                Newtonsoft.Json.JsonConvert.SerializeObject(anterior),
                Newtonsoft.Json.JsonConvert.SerializeObject(config),
                ObtenerUsuarioId(),
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                "Configuracion");
        }
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo actualizar la configuración" });

        return Ok(new { success = true, mensaje = "Configuración actualizada exitosamente" });
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginSlides()
    {
        var slides = await _repo.ObtenerSlidesAsync();
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
        var slides = await _repo.ObtenerTodosSlidesAsync();
        return Ok(new { success = true, datos = slides });
    }

    [HttpPost("slides")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    public async Task<IActionResult> CrearSlide([FromBody] Models.LoginSlide slide)
    {
        if (slide == null) return BadRequest(new { success = false, mensaje = "Datos inválidos" });
        
        var ok = await _repo.CrearSlideAsync(slide);
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo crear el slide" });

        await RegistrarAuditoriaSlideAsync("INSERT", slide.Id.ToString(), null, slide);
        return Ok(new { success = true, mensaje = "Slide creado exitosamente" });
    }

    [HttpPut("slides/{id}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    public async Task<IActionResult> ActualizarSlide(int id, [FromBody] Models.LoginSlide slide)
    {
        if (slide == null) return BadRequest(new { success = false, mensaje = "Datos inválidos" });
        var anterior = (await _repo.ObtenerTodosSlidesAsync()).FirstOrDefault(s => s.Id == id);
        slide.Id = id;

        var ok = await _repo.ActualizarSlideAsync(slide);
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo actualizar el slide" });

        await RegistrarAuditoriaSlideAsync("UPDATE", id.ToString(), anterior, slide);
        return Ok(new { success = true, mensaje = "Slide actualizado exitosamente" });
    }

    [HttpDelete("slides/{id}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    public async Task<IActionResult> EliminarSlide(int id)
    {
        var anterior = (await _repo.ObtenerTodosSlidesAsync()).FirstOrDefault(s => s.Id == id);
        var ok = await _repo.EliminarSlideAsync(id);
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo eliminar el slide o no existe" });

        await RegistrarAuditoriaSlideAsync("DELETE", id.ToString(), anterior, null);
        return Ok(new { success = true, mensaje = "Slide eliminado exitosamente" });
    }

    [HttpPost("slides/upload")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
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
        return Ok(new { success = true, url = urlRelativa });
    }

    private long ObtenerUsuarioId()
    {
        return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private async Task RegistrarAuditoriaSlideAsync(string accion, string registroId, Models.LoginSlide? anterior, Models.LoginSlide? nuevo)
    {
        await _auditoriaRepo.RegistrarAsync(
            "RL_LOGIN_SLIDES",
            registroId,
            accion,
            anterior == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(anterior),
            nuevo == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(nuevo),
            ObtenerUsuarioId(),
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            "Configuracion");
    }
}
