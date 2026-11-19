using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Core.Security;
using RL.API.Infrastructure.Http;
using RL.API.Infrastructure.Files;
using RL.API.Infrastructure.Reporting;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;

namespace RL.API.Features.Configuracion;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionService _service;
    private readonly IWebHostEnvironment? _environment;

    public ConfiguracionController(IConfiguracionService service, IWebHostEnvironment? environment = null)
    {
        _service = service;
        _environment = environment;
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
            colorEncabezadoReportes = config.ColorEncabezadoReportes,
            timeoutSesion = config.TimeoutSesion,
            acuerdoLegal = config.AcuerdoLegal,
            maxIntentos = config.MaxIntentos,
            validezClaveTemp = config.ValidezClaveTemp
        }});
    }

    // Endpoint institucional para resolver el logotipo configurado del sistema (archivo local o URL HTTPS validada).
    // Proporciona los bytes de imagen normalizados y seguros contra CORS para los generadores de reportes.
    [HttpGet("sistema/logo-reportes")]
    [AllowAnonymous]
    public async Task<IActionResult> LogoReportes([FromServices] IInstitutionalBrandingResolver? brandingResolver = null)
    {
        Response.Headers["Cache-Control"] = "no-store";

        if (brandingResolver is not null)
        {
            var snapshot = await brandingResolver.ResolveAsync(HttpContext.RequestAborted).ConfigureAwait(false);
            if (!snapshot.HasLogo || snapshot.LogoEffectiveBytes.Length == 0)
            {
                Response.Headers["X-Logo-Status"] = snapshot.Source;
                return NoContent();
            }

            Response.Headers["X-Logo-Status"] = "available";
            Response.Headers["X-Logo-Source"] = snapshot.Source;
            return File(snapshot.LogoEffectiveBytes, snapshot.LogoMimeType);
        }

        if (_environment is null) return NoContent();

        var configuration = await _service.ObtenerConfigSistemaAsync().ConfigureAwait(false);
        var logoUrl = configuration?.LogoUrl?.Trim();
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            Response.Headers["X-Logo-Status"] = "not-configured";
            return NoContent();
        }

        if (!logoUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers["X-Logo-Status"] = "resolver-required-for-non-local";
            return NoContent();
        }

        var fileName = logoUrl["/uploads/".Length..];
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.Contains('/')
            || fileName.Contains('\\')
            || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            Response.Headers["X-Logo-Status"] = "invalid-file-name";
            return NoContent();
        }

        var path = InstitutionalBrandingStorage.ResolveLocalAsset(_environment, logoUrl);
        if (path is null)
        {
            Response.Headers["X-Logo-Status"] = "configured-logo-not-local";
            return NoContent();
        }

        var mime = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => null
        };
        if (mime is null)
        {
            Response.Headers["X-Logo-Status"] = "unsupported-media-type";
            return NoContent();
        }

        var file = new FileInfo(path);
        if (file.Length <= 0 || file.Length > 10L * 1024 * 1024)
        {
            Response.Headers["X-Logo-Status"] = "invalid-file-size";
            return NoContent();
        }

        Response.Headers["X-Logo-Status"] = "available";
        return PhysicalFile(path, mime);
    }

    [HttpGet("sistema/logo-reportes/estado")]
    [AllowAnonymous]
    public async Task<IActionResult> EstadoLogoReportes([FromServices] IInstitutionalBrandingResolver? brandingResolver = null)
    {
        var configuration = await _service.ObtenerConfigSistemaAsync().ConfigureAwait(false);
        var logoUrl = configuration?.LogoUrl?.Trim();
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return Ok(new
            {
                configurado = false,
                disponible = false,
                tipo = "SIN_LOGO",
                estado = "not-configured",
                mensaje = "No hay logotipo configurado en el sistema."
            });
        }

        var esLocal = logoUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || logoUrl.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase);
        var esHttps = logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var tipo = esLocal ? "LOGO_LOCAL" : (esHttps ? "LOGO_HTTPS" : "LOGO_INVALIDO");

        if (brandingResolver is not null)
        {
            var snapshot = await brandingResolver.ResolveAsync(HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(new
            {
                configurado = true,
                disponible = snapshot.HasLogo,
                tipo,
                estado = snapshot.Source,
                mimeType = snapshot.HasLogo ? snapshot.LogoMimeType : null,
                ancho = snapshot.HasLogo ? snapshot.LogoWidth : 0,
                alto = snapshot.HasLogo ? snapshot.LogoHeight : 0,
                relacionAspecto = snapshot.HasLogo ? snapshot.LogoAspectRatio : 1m,
                mensaje = snapshot.HasLogo ? "Logotipo disponible y validado." : "El logotipo configurado no se pudo recuperar o no es válido."
            });
        }

        return Ok(new { configurado = true, disponible = false, tipo, estado = "resolver-unavailable", mensaje = "Resolver no disponible." });
    }

    [HttpGet("sistema/favicon")]
    [AllowAnonymous]
    public async Task<IActionResult> Favicon(
        [FromServices] IInstitutionalLogoDownloader? logoDownloader = null,
        CancellationToken cancellationToken = default)
    {
        Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";

        var configuration = await _service.ObtenerConfigSistemaAsync().ConfigureAwait(false);
        var iconoUrl = configuration?.IconoUrl?.Trim();
        if (string.IsNullOrWhiteSpace(iconoUrl))
        {
            Response.Headers["X-Favicon-Status"] = "not-configured";
            return NoContent();
        }

        if (iconoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            iconoUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
            iconoUrl.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
            iconoUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            iconoUrl.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers["X-Favicon-Status"] = "invalid-scheme";
            return NoContent();
        }

        // Caso 1: URL HTTPS remota
        if (iconoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (logoDownloader is null)
            {
                Response.Headers["X-Favicon-Status"] = "downloader-unavailable";
                return NoContent();
            }

            var downloadResult = await logoDownloader.DownloadFaviconAsync(iconoUrl, cancellationToken).ConfigureAwait(false);
            if (!downloadResult.Success || downloadResult.Bytes is null || downloadResult.Bytes.Length == 0)
            {
                Response.Headers["X-Favicon-Status"] = downloadResult.FailureReason ?? "download-failed";
                return NoContent();
            }

            Response.Headers["X-Favicon-Status"] = "available";
            Response.Headers["X-Favicon-Source"] = "configured-https-url";
            return File(downloadResult.Bytes, downloadResult.MimeType ?? "image/x-icon");
        }

        // Caso 2: Archivo local
        if (iconoUrl.Contains("..") || iconoUrl.Contains(':') || iconoUrl.Contains('\\'))
        {
            Response.Headers["X-Favicon-Status"] = "invalid-path";
            return NoContent();
        }

        if (_environment is null)
        {
            Response.Headers["X-Favicon-Status"] = "storage-unavailable";
            return NoContent();
        }

        var path = InstitutionalBrandingStorage.ResolveLocalAsset(_environment, iconoUrl);
        if (path is null || !System.IO.File.Exists(path))
        {
            Response.Headers["X-Favicon-Status"] = "not-found";
            return NoContent();
        }

        var fileInfo = new FileInfo(path);
        if (fileInfo.Length <= 0 || fileInfo.Length > InstitutionalFaviconValidator.MaxFaviconSizeBytes)
        {
            Response.Headers["X-Favicon-Status"] = "invalid-file-size";
            return NoContent();
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var validation = InstitutionalFaviconValidator.Validate(bytes, Path.GetExtension(path));
        if (!validation.IsValid)
        {
            Response.Headers["X-Favicon-Status"] = validation.FailureReason ?? "invalid-content";
            return NoContent();
        }

        Response.Headers["X-Favicon-Status"] = "available";
        Response.Headers["X-Favicon-Source"] = "configured-local-file";
        return File(bytes, validation.MimeType!);
    }

    [HttpGet("sistema/favicon/estado")]
    [AllowAnonymous]
    public async Task<IActionResult> EstadoFavicon(
        [FromServices] IInstitutionalLogoDownloader? logoDownloader = null,
        CancellationToken cancellationToken = default)
    {
        var configuration = await _service.ObtenerConfigSistemaAsync().ConfigureAwait(false);
        var iconoUrl = configuration?.IconoUrl?.Trim();
        if (string.IsNullOrWhiteSpace(iconoUrl))
        {
            return Ok(new
            {
                configurado = false,
                disponible = false,
                tipo = "SIN_FAVICON",
                estado = "not-configured",
                mensaje = "No hay favicon configurado en el sistema."
            });
        }

        var esLocal = iconoUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
                      iconoUrl.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase) ||
                      (!iconoUrl.Contains("://") && !iconoUrl.StartsWith("//"));
        var esHttps = iconoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var tipo = esLocal ? "FAVICON_LOCAL" : (esHttps ? "FAVICON_HTTPS" : "FAVICON_INVALIDO");

        if (!esLocal && !esHttps)
        {
            return Ok(new
            {
                configurado = true,
                disponible = false,
                tipo,
                estado = "invalid-scheme",
                mensaje = "El favicon configurado tiene un esquema no permitido."
            });
        }

        if (esHttps)
        {
            if (logoDownloader is null)
                return Ok(new { configurado = true, disponible = false, tipo, estado = "downloader-unavailable", mensaje = "Downloader no disponible." });

            var res = await logoDownloader.DownloadFaviconAsync(iconoUrl, cancellationToken).ConfigureAwait(false);
            return Ok(new
            {
                configurado = true,
                disponible = res.Success,
                tipo,
                estado = res.Success ? "configured-https-url" : (res.FailureReason ?? "download-failed"),
                mimeType = res.Success ? res.MimeType : null,
                ancho = res.Width,
                alto = res.Height,
                mensaje = res.Success ? "Favicon HTTPS validado y disponible." : res.FailureReason
            });
        }

        if (_environment is null)
            return Ok(new { configurado = true, disponible = false, tipo, estado = "storage-unavailable", mensaje = "Almacenamiento no disponible." });

        var path = InstitutionalBrandingStorage.ResolveLocalAsset(_environment, iconoUrl);
        if (path is null || !System.IO.File.Exists(path))
            return Ok(new { configurado = true, disponible = false, tipo, estado = "not-found", mensaje = "Archivo de favicon local no encontrado." });

        var bytes = await System.IO.File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var val = InstitutionalFaviconValidator.Validate(bytes, Path.GetExtension(path));
        return Ok(new
        {
            configurado = true,
            disponible = val.IsValid,
            tipo,
            estado = val.IsValid ? "configured-local-file" : (val.FailureReason ?? "invalid-content"),
            mimeType = val.IsValid ? val.MimeType : null,
            ancho = val.Width,
            alto = val.Height,
            mensaje = val.IsValid ? "Favicon local validado y disponible." : val.FailureReason
        });
    }

    [HttpPut("sistema")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    [AuditRequired("Cambio de configuracion del sistema")]
    public async Task<IActionResult> GuardarSistema([FromBody] ConfigSistema config)
    {
        if (config == null) return BadRequest(new { success = false, mensaje = "Datos inválidos" });

        bool ok;
        try
        {
            ok = await _service.GuardarConfigSistemaAsync(config, ObtenerUsuarioId(), ObtenerIp());
        }
        catch (InvalidOperationException exception)
            when (exception.Message.StartsWith("B06_BRANDING_SCHEMA=NOT_READY", StringComparison.Ordinal))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                codigo = "B06_BRANDING_SCHEMA=NOT_READY",
                mensaje = "La configuracion institucional no puede publicarse hasta aplicar la migracion B06 en la base de datos autorizada."
            });
        }
        if (!ok) return BadRequest(new { success = false, mensaje = "No se pudo actualizar la configuración" });

        InstitutionalBrandingResolver.ClearCache();
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
        if (_environment is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, mensaje = "El almacenamiento institucional no está disponible." });

        var uploadsFolder = InstitutionalBrandingStorage.GetUploadsDirectory(_environment);
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

        var urlRelativa = InstitutionalBrandingStorage.BuildPublicUploadUrl(nombreUnico);
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

    [HttpPost("sistema/favicon/upload")]
    [Authorize(Roles = "ADMINISTRADOR")]
    [ModuloAuthorize(3)]
    [AuditRequired("Carga de favicon institucional")]
    public async Task<IActionResult> SubirFavicon(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return BadRequest(new { success = false, mensaje = "No se ha proporcionado ningún archivo para el favicon." });

        if (archivo.Length > InstitutionalFaviconValidator.MaxFaviconSizeBytes)
            return BadRequest(new { success = false, mensaje = $"El archivo de favicon supera el tamaño máximo permitido de {InstitutionalFaviconValidator.MaxFaviconSizeBytes / 1024 / 1024} MB." });

        if (_environment is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false, mensaje = "El almacenamiento institucional no está disponible." });

        var extension = Path.GetExtension(Path.GetFileName(archivo.FileName)).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".ico" or ".webp"))
            return BadRequest(new { success = false, mensaje = $"Extensión '{extension}' no admitida para favicon. Use PNG, JPG, JPEG, WebP o ICO." });

        using var memoryStream = new MemoryStream();
        await archivo.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        // 1. Validar el contenido subido
        var validation = InstitutionalFaviconValidator.Validate(bytes, extension);
        if (!validation.IsValid)
            return BadRequest(new { success = false, mensaje = validation.FailureReason ?? "El archivo de imagen no es válido para favicon." });

        // 2. Convertir y normalizar a favicon ICO multirresolución optimizado
        byte[] convertedBytes;
        FaviconConversionResult conversionResult;
        try
        {
            conversionResult = InstitutionalFaviconConverter.Convert(bytes, extension);
            convertedBytes = conversionResult.IcoBytes;
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, mensaje = $"Error al procesar y convertir el favicon: {ex.Message}" });
        }

        // 3. Validar el resultado convertido
        var outputValidation = InstitutionalFaviconValidator.Validate(convertedBytes, ".ico");
        if (!outputValidation.IsValid)
            return BadRequest(new { success = false, mensaje = $"El favicon convertido no superó la verificación de seguridad: {outputValidation.FailureReason}" });

        // 4. Almacenamiento seguro y persistente
        var uploadsFolder = InstitutionalBrandingStorage.GetUploadsDirectory(_environment);
        Directory.CreateDirectory(uploadsFolder);

        var nombreUnico = $"favicon-{Guid.NewGuid():N}.ico";
        var rutaCompleta = FileUploadPolicyValidator.ResolveSafePath(uploadsFolder, nombreUnico);
        if (rutaCompleta is null)
            return BadRequest(new { success = false, mensaje = "No se pudo resolver una ruta segura para guardar el favicon." });

        string journalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Evidencias", ".configuration-image-operations");
        var journal = new DurableFileOperationJournal(uploadsFolder, journalPath);
        await using var operation = await journal.BeginAsync(
            DurableFileOperationType.ConfigurationImageUpload,
            nombreUnico,
            nombreUnico);

        await using (var stream = new FileStream(operation.PendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
        {
            await stream.WriteAsync(convertedBytes);
            await stream.FlushAsync();
        }

        var urlRelativa = InstitutionalBrandingStorage.BuildPublicUploadUrl(nombreUnico);
        await _service.RegistrarCargaImagenAsync(
            Path.GetFileName(archivo.FileName),
            nombreUnico,
            urlRelativa,
            "image/x-icon",
            convertedBytes.Length,
            ObtenerUsuarioId(),
            ObtenerIp());

        if (System.IO.File.Exists(rutaCompleta))
            throw new IOException("The generated configuration image path is already occupied.");
        System.IO.File.Move(operation.PendingPath, rutaCompleta);
        await operation.CompleteAsync();

        return Ok(new
        {
            success = true,
            url = urlRelativa,
            mimeType = "image/x-icon",
            width = outputValidation.Width,
            height = outputValidation.Height,
            status = "ready-to-save",
            mensaje = "Favicon convertido correctamente. Guarde la configuración para publicarlo."
        });
    }
    private long ObtenerUsuarioId()
    {
        return Convert.ToInt64(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    }

    private string? ObtenerIp() => HttpContext.GetClientIp();
}
