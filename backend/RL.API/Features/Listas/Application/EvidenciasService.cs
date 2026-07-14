using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RL.API.Features.Listas.Contracts;
using RL.API.Features.Listas.Persistence;
using RL.API.Services;
using System.IO.Compression;

namespace RL.API.Features.Listas.Application;

public sealed class EvidenciasService : IEvidenciasService
{
    private const int DefaultEvidenceMaxMb = 10;
    private const int MaxTextoSeguimiento = 1000;
    private const string DefaultEvidenceTypesText = "PDF, imágenes, Word, Excel";
    private const string DefaultEvidenceStoragePath = "App_Data/Evidencias";
    private const string LegacyEvidenceStoragePath = "Uploads/Evidencias";

    private static readonly Dictionary<string, string[]> DefaultEvidenceMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new[] { "application/pdf" },
        [".png"] = new[] { "image/png" },
        [".jpg"] = new[] { "image/jpeg", "image/pjpeg" },
        [".jpeg"] = new[] { "image/jpeg", "image/pjpeg" },
        [".doc"] = new[] { "application/msword", "application/octet-stream" },
        [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip", "application/octet-stream" },
        [".xls"] = new[] { "application/vnd.ms-excel", "application/octet-stream" },
        [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream" }
    };

    private static readonly Dictionary<string, byte[][]> EvidenceSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".doc"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        [".xls"] = new[] { new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        [".docx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 }, new byte[] { 0x50, 0x4B, 0x07, 0x08 } },
        [".xlsx"] = new[] { new byte[] { 0x50, 0x4B, 0x03, 0x04 }, new byte[] { 0x50, 0x4B, 0x05, 0x06 }, new byte[] { 0x50, 0x4B, 0x07, 0x08 } }
    };

    private readonly IListasRepository _repo;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public EvidenciasService(IListasRepository repo, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _repo = repo;
        _configuration = configuration;
        _environment = environment;
    }

    public EvidenciaPoliticaDto ObtenerPolitica()
    {
        var maximoMb = ObtenerMaximoMb();
        var mimeTypesPermitidos = ObtenerMimeTypesPermitidos();
        return new EvidenciaPoliticaDto(
            maximoMb,
            ObtenerMaximoBytes(),
            mimeTypesPermitidos.Keys.OrderBy(k => k).ToArray(),
            ObtenerTiposPermitidosTexto());
    }

    public string? ValidarArchivos(List<IFormFile>? archivos)
    {
        // Proceso de validación documental: controla nombre, tamaño, extensión,
        // MIME y firma real del archivo antes de permitir su almacenamiento.
        if (archivos == null || archivos.Count == 0) return null;

        var maximoMb = ObtenerMaximoMb();
        var maximoBytes = ObtenerMaximoBytes();
        var mimeTypesPermitidos = ObtenerMimeTypesPermitidos();

        foreach (var file in archivos)
        {
            var nombreOriginal = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(nombreOriginal))
                return "El nombre del archivo de evidencia no es válido.";

            if (nombreOriginal.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return $"El archivo {nombreOriginal} contiene caracteres no permitidos en el nombre.";

            if (file.Length <= 0)
                return $"El archivo {nombreOriginal} está vacío.";

            if (file.Length > maximoBytes)
                return $"El archivo {nombreOriginal} supera el límite de {maximoMb} MB.";

            var extension = Path.GetExtension(nombreOriginal);
            if (string.IsNullOrWhiteSpace(extension) || !mimeTypesPermitidos.TryGetValue(extension, out var mimeTypes))
                return $"El archivo {nombreOriginal} tiene una extensión no permitida.";

            extension = extension.ToLowerInvariant();
            var contentType = file.ContentType?.Trim();
            if (string.IsNullOrWhiteSpace(contentType))
                return $"No se pudo identificar el tipo de contenido del archivo {nombreOriginal}.";

            if (!Array.Exists(mimeTypes, mime => string.Equals(mime, contentType, StringComparison.OrdinalIgnoreCase)))
                return $"El archivo {nombreOriginal} tiene un tipo de contenido no permitido ({contentType}).";

            if (DebeValidarFirmaArchivo() && !TieneFirmaPermitida(file, extension))
                return $"El archivo {nombreOriginal} no coincide con la firma real esperada para {extension}.";
        }

        return null;
    }

    public async Task GuardarArchivosAsync(long detalleId, List<IFormFile>? archivos, long usuarioId)
    {
        // Proceso de almacenamiento seguro: conserva el nombre original en metadata
        // y guarda el archivo físico con GUID para evitar rutas públicas o nombres manipulables.
        if (archivos == null || archivos.Count == 0) return;

        var uploadDir = ObtenerDirectorioEvidencias();
        Directory.CreateDirectory(uploadDir);

        foreach (var file in archivos)
        {
            var nombreOriginal = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid():N}{extension}";
            var filePath = ObtenerRutaFisicaSegura(uniqueName, permitirLegacy: false);
            if (filePath == null)
                throw new InvalidOperationException("No se pudo resolver una ruta segura para guardar la evidencia.");

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            await _repo.GuardarEvidenciaMetaAsync(detalleId, nombreOriginal, file.ContentType.Trim(), uniqueName, usuarioId);
        }
    }

    public async Task<ServiceResult> RegistrarSeguimientoAsync(string noDocumento, string? motivoIngreso, List<IFormFile>? archivos, long usuarioId)
    {
        motivoIngreso = motivoIngreso?.Trim();
        if (string.IsNullOrWhiteSpace(motivoIngreso))
            return ServiceResult.BadRequest("El comentario de seguimiento es obligatorio.");
        if (motivoIngreso.Length > MaxTextoSeguimiento)
            return ServiceResult.BadRequest("El comentario de seguimiento no debe superar los 1000 caracteres.");

        var positivoId = await _repo.ObtenerPositivoIdPorDocumentoAsync(noDocumento);
        if (!positivoId.HasValue)
            return ServiceResult.NotFound("No se encontró un registro positivo activo para este documento.");

        var errorArchivo = ValidarArchivos(archivos);
        if (errorArchivo != null)
            return ServiceResult.BadRequest(errorArchivo);

        var detalleId = await _repo.RegistrarSeguimientoAsync(positivoId.Value, motivoIngreso, usuarioId);
        await GuardarArchivosAsync(detalleId, archivos, usuarioId);

        return ServiceResult.Ok("Seguimiento y evidencia registrados correctamente.");
    }

    public async Task<ServiceResult> ActualizarSeguimientoAsync(long detalleId, string? motivoIngreso, List<IFormFile>? archivos, long usuarioId)
    {
        motivoIngreso = motivoIngreso?.Trim();
        if (string.IsNullOrWhiteSpace(motivoIngreso))
            return ServiceResult.BadRequest("El comentario de seguimiento es obligatorio.");
        if (motivoIngreso.Length > MaxTextoSeguimiento)
            return ServiceResult.BadRequest("El comentario de seguimiento no debe superar los 1000 caracteres.");

        var errorArchivo = ValidarArchivos(archivos);
        if (errorArchivo != null)
            return ServiceResult.BadRequest(errorArchivo);

        var ok = await _repo.ActualizarSeguimientoAsync(detalleId, motivoIngreso, usuarioId);
        if (!ok)
            return ServiceResult.NotFound("No se encontró el seguimiento a actualizar.");

        await GuardarArchivosAsync(detalleId, archivos, usuarioId);
        return ServiceResult.Ok("Seguimiento actualizado correctamente.");
    }

    public async Task<ServiceResult<EvidenciaDescargaDto>> DescargarEvidenciaAsync(long evidenciaId, long usuarioId)
    {
        // Proceso de visualización/descarga: resuelve la ruta física segura,
        // audita el acceso sensible y entrega bytes sin exponer rutas directas.
        var meta = await _repo.ObtenerEvidenciaPorIdAsync(evidenciaId);
        if (meta == null)
            return ServiceResult<EvidenciaDescargaDto>.NotFound("Evidencia no encontrada.");

        var filePath = ObtenerRutaFisicaSegura(meta.Value.Ruta, permitirLegacy: true);
        if (filePath == null || !File.Exists(filePath))
            return ServiceResult<EvidenciaDescargaDto>.NotFound("El archivo físico no existe en el servidor.");

        var dataJson = JsonConvert.SerializeObject(new { NombreArchivo = meta.Value.Nombre, NombreFisico = meta.Value.Ruta, TipoOperacion = "DESCARGA_VISUALIZACION" });
        await _repo.RegistrarAuditoriaVisualizacionAsync(evidenciaId, dataJson, usuarioId);

        var bytes = await File.ReadAllBytesAsync(filePath);
        return ServiceResult<EvidenciaDescargaDto>.Ok(new EvidenciaDescargaDto(bytes, meta.Value.Mime, meta.Value.Nombre));
    }

    public async Task<ServiceResult> EliminarEvidenciaAsync(long evidenciaId, string? motivoEliminacion, long usuarioId)
    {
        // Proceso de eliminación lógica: exige motivo, conserva el archivo físico
        // y actualiza solo la metadata para mantener evidencia trazable.
        motivoEliminacion = motivoEliminacion?.Trim();
        if ((motivoEliminacion?.Length ?? 0) > MaxTextoSeguimiento)
            return ServiceResult.BadRequest("El motivo de eliminacion no debe superar los 1000 caracteres.");
        if (string.IsNullOrWhiteSpace(motivoEliminacion))
            return ServiceResult.BadRequest("El motivo de eliminación es obligatorio.");

        var meta = await _repo.ObtenerEvidenciaPorIdAsync(evidenciaId);
        if (meta == null)
            return ServiceResult.NotFound("Evidencia no encontrada en la base de datos.");

        var ok = await _repo.EliminarEvidenciaMetaAsync(evidenciaId, usuarioId, motivoEliminacion);
        if (!ok)
            return ServiceResult.BadRequest("No se pudo eliminar el registro de evidencia.");

        var filePath = ObtenerRutaFisicaSegura(meta.Value.Ruta, permitirLegacy: true);
        if (filePath != null && File.Exists(filePath))
        {
            Serilog.Log.Information("Evidencia inactivada lógicamente; archivo físico conservado: {FilePath}", filePath);
        }

        return ServiceResult.Ok("Evidencia eliminada correctamente.");
    }

    public async Task<ServiceResult> EliminarSeguimientoAsync(long detalleId, string? motivoEliminacion, long usuarioId)
    {
        motivoEliminacion = motivoEliminacion?.Trim();
        if ((motivoEliminacion?.Length ?? 0) > MaxTextoSeguimiento)
            return ServiceResult.BadRequest("El motivo de eliminacion no debe superar los 1000 caracteres.");
        if (string.IsNullOrWhiteSpace(motivoEliminacion))
            return ServiceResult.BadRequest("El motivo de eliminación es obligatorio.");

        var ok = await _repo.EliminarSeguimientoLogicoAsync(detalleId, usuarioId, motivoEliminacion);
        return ok
            ? ServiceResult.Ok("Seguimiento eliminado correctamente.")
            : ServiceResult.NotFound("No se encontró el seguimiento o ya fue eliminado.");
    }

    public Task RegistrarReporteImpresoAsync(string noDocumento, string dataJson, long usuarioId)
    {
        return _repo.RegistrarAuditoriaReporteImpresoAsync(noDocumento, dataJson, usuarioId);
    }

    private int ObtenerMaximoMb()
    {
        var maximoMb = _configuration.GetValue<int?>("Evidencias:MaxFileSizeMb") ?? DefaultEvidenceMaxMb;
        return maximoMb > 0 ? maximoMb : DefaultEvidenceMaxMb;
    }

    private long ObtenerMaximoBytes()
    {
        return ObtenerMaximoMb() * 1024L * 1024L;
    }

    private Dictionary<string, string[]> ObtenerMimeTypesPermitidos()
    {
        var configurados = _configuration
            .GetSection("Evidencias:AllowedMimeTypes")
            .Get<Dictionary<string, string[]>>();

        if (configurados == null || configurados.Count == 0)
            return new Dictionary<string, string[]>(DefaultEvidenceMimeTypes, StringComparer.OrdinalIgnoreCase);

        return configurados
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Value != null && kv.Value.Length > 0)
            .ToDictionary(
                kv => kv.Key.StartsWith(".") ? kv.Key.ToLowerInvariant() : $".{kv.Key.ToLowerInvariant()}",
                kv => kv.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private string ObtenerTiposPermitidosTexto()
    {
        return _configuration["Evidencias:AllowedTypesText"] ?? DefaultEvidenceTypesText;
    }

    private string ObtenerDirectorioEvidencias()
    {
        return ResolverRutaAlmacenamiento(_configuration["Evidencias:StoragePath"] ?? DefaultEvidenceStoragePath);
    }

    private string ObtenerDirectorioEvidenciasLegacy()
    {
        return ResolverRutaAlmacenamiento(_configuration["Evidencias:LegacyStoragePath"] ?? LegacyEvidenceStoragePath);
    }

    private string ResolverRutaAlmacenamiento(string ruta)
    {
        return Path.GetFullPath(Path.IsPathRooted(ruta)
            ? ruta
            : Path.Combine(_environment.ContentRootPath, ruta));
    }

    private string? ObtenerRutaFisicaSegura(string nombreFisico, bool permitirLegacy)
    {
        if (string.IsNullOrWhiteSpace(nombreFisico)) return null;

        var soloNombre = Path.GetFileName(nombreFisico);
        if (!string.Equals(soloNombre, nombreFisico, StringComparison.Ordinal))
            return null;

        var rutaPrincipal = ConstruirRutaDentroDeBase(ObtenerDirectorioEvidencias(), soloNombre);
        if (rutaPrincipal == null || !permitirLegacy || File.Exists(rutaPrincipal))
            return rutaPrincipal;

        return ConstruirRutaDentroDeBase(ObtenerDirectorioEvidenciasLegacy(), soloNombre);
    }

    private static string? ConstruirRutaDentroDeBase(string directorioBase, string nombreFisico)
    {
        var baseFullPath = Path.GetFullPath(directorioBase);
        var candidatePath = Path.GetFullPath(Path.Combine(baseFullPath, nombreFisico));
        var baseWithSeparator = baseFullPath.EndsWith(Path.DirectorySeparatorChar)
            ? baseFullPath
            : baseFullPath + Path.DirectorySeparatorChar;

        return candidatePath.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase)
            ? candidatePath
            : null;
    }

    private bool DebeValidarFirmaArchivo()
    {
        return _configuration.GetValue<bool?>("Evidencias:ValidateFileSignature") ?? true;
    }

    private static bool TieneFirmaPermitida(IFormFile file, string extension)
    {
        if (!EvidenceSignatures.TryGetValue(extension, out var signatures))
            return false;

        Span<byte> buffer = stackalloc byte[8];
        using var stream = file.OpenReadStream();
        var bytesRead = stream.Read(buffer);
        if (bytesRead == 0) return false;

        foreach (var signature in signatures)
        {
            if (bytesRead < signature.Length) continue;

            var match = true;
            for (var i = 0; i < signature.Length; i++)
            {
                if (buffer[i] != signature[i])
                {
                    match = false;
                    break;
                }
            }

            if (match) return TieneEstructuraOfficeOpenXml(file, extension);
        }

        return false;
    }

    private static bool TieneEstructuraOfficeOpenXml(IFormFile file, string extension)
    {
        if (extension != ".docx" && extension != ".xlsx")
            return true;

        try
        {
            using var stream = file.OpenReadStream();
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var hasContentTypes = zip.GetEntry("[Content_Types].xml") != null;
            var hasExpectedEntry = extension == ".docx"
                ? zip.GetEntry("word/document.xml") != null
                : zip.GetEntry("xl/workbook.xml") != null;

            return hasContentTypes && hasExpectedEntry;
        }
        catch
        {
            return false;
        }
    }
}
