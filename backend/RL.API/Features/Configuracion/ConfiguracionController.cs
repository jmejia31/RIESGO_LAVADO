using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Core.Security;
using RL.API.Infrastructure.Http;
using RL.API.Infrastructure.Files;
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
        var validationError = FileUploadPolicyValidator.Validate(archivo, FileUploadPolicies.Images());
        if (validationError is not null)
            return BadRequest(new { success = false, mensaje = validationError });

        var extension = Path.GetExtension(Path.GetFileName(archivo.FileName)).ToLowerInvariant();
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var nombreUnico = $"{Guid.NewGuid()}{extension}";
        var rutaCompleta = FileUploadPolicyValidator.ResolveSafePath(uploadsFolder, nombreUnico);
        if (rutaCompleta is null)
            return BadRequest(new { success = false, mensaje = "No se pudo resolver una ruta segura para guardar la imagen." });

        string journalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias", ".configuration-image-operations");
        var journal = new DurableFileOperationJournal(uploadsFolder, journalPath);
        await using var operation = await journal.BeginAsync(
            DurableFileOperationType.ConfigurationImageUpload,
            nombreUnico,
            nombreUnico);
        await using (var stream = new FileStream(operation.PendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
        {
            await archivo.CopyToAsync(stream);
            stream.Flush(flushToDisk: true);
        }

        var urlRelativa = $"/uploads/{nombreUnico}";
        await _service.RegistrarCargaImagenAsync(
            Path.GetFileName(archivo.FileName),
            nombreUnico,
            urlRelativa,
            archivo.ContentType,
            archivo.Length,
            ObtenerUsuarioId(),
            ObtenerIp());

        if (System.IO.File.Exists(rutaCompleta))
            throw new IOException("The generated configuration image path is already occupied.");
        System.IO.File.Move(operation.PendingPath, rutaCompleta);
        await operation.CompleteAsync();

        return Ok(new { success = true, url = urlRelativa });
    }
    private long ObtenerUsuarioId()
    {
        return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private string? ObtenerIp() => HttpContext.GetClientIp();
}
