using System.IO.Compression;
using System.Xml;
using Microsoft.AspNetCore.Http;

namespace RL.API.Infrastructure.Files;

public sealed record FileUploadPolicy(
    long MaxBytes,
    IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedMimeTypes);

public static class FileUploadPolicies
{
    public const long ListImportMaxBytes = 128L * 1024 * 1024;
    public const long EvidenceMaxBytes = 10L * 1024 * 1024;

    public static FileUploadPolicy Evidence(long? maxBytes = null,
        IReadOnlyDictionary<string, string[]>? configuredMimeTypes = null)
    {
        var mimeTypes = configuredMimeTypes ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".png"] = ["image/png"],
            [".jpg"] = ["image/jpeg", "image/pjpeg"],
            [".jpeg"] = ["image/jpeg", "image/pjpeg"],
            [".doc"] = ["application/msword", "application/octet-stream"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip", "application/octet-stream"],
            [".xls"] = ["application/vnd.ms-excel", "application/octet-stream"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream"]
        };

        return CreatePolicy(maxBytes ?? EvidenceMaxBytes, mimeTypes);
    }

    public static FileUploadPolicy ListImport(long? maxBytes = null) => CreatePolicy(
        maxBytes ?? ListImportMaxBytes,
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".csv"] = ["text/csv", "application/csv", "application/vnd.ms-excel", "application/octet-stream"],
            [".xml"] = ["application/xml", "text/xml", "application/octet-stream"],
            [".xls"] = ["application/vnd.ms-excel", "application/octet-stream"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/zip", "application/octet-stream"]
        });

    public static FileUploadPolicy Images(long? maxBytes = null) => CreatePolicy(
        maxBytes ?? EvidenceMaxBytes,
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ["image/jpeg", "image/pjpeg"],
            [".jpeg"] = ["image/jpeg", "image/pjpeg"],
            [".png"] = ["image/png"],
            [".gif"] = ["image/gif"],
            [".webp"] = ["image/webp"]
        });

    private static FileUploadPolicy CreatePolicy(long maxBytes, IReadOnlyDictionary<string, string[]> mimeTypes) => new(
        maxBytes,
        mimeTypes
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is { Length: > 0 })
            .ToDictionary(
                pair => NormalizeExtension(pair.Key),
                pair => (IReadOnlySet<string>)new HashSet<string>(pair.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase));

    internal static string NormalizeExtension(string extension) =>
        extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
}

public static class FileUploadPolicyValidator
{
    public static string? Validate(IFormFile? file, FileUploadPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (file is null || file.Length <= 0)
            return "El archivo está vacío o no fue seleccionado.";
        if (policy.MaxBytes <= 0 || file.Length > policy.MaxBytes)
            return "El archivo supera el límite de tamaño permitido.";

        var safeName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(safeName)
            || !string.Equals(safeName, file.FileName, StringComparison.Ordinal)
            || file.FileName.IndexOfAny(['/', '\\']) >= 0
            || safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "El nombre del archivo no es válido.";

        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        if (!policy.AllowedMimeTypes.TryGetValue(extension, out var allowedMimeTypes))
            return "La extensión del archivo no está permitida.";

        var mimeType = file.ContentType?.Trim();
        if (string.IsNullOrWhiteSpace(mimeType) || !allowedMimeTypes.Contains(mimeType))
            return "El tipo de contenido del archivo no está permitido.";

        return ValidateContent(file, extension);
    }

    public static string? ResolveSafePath(string rootDirectory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory) || string.IsNullOrWhiteSpace(fileName))
            return null;

        var safeName = Path.GetFileName(fileName);
        if (!string.Equals(safeName, fileName, StringComparison.Ordinal)
            || fileName.IndexOfAny(['/', '\\']) >= 0
            || safeName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return null;

        var root = Path.GetFullPath(rootDirectory);
        var rootPrefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, safeName));
        return path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ? path : null;
    }

    private static string? ValidateContent(IFormFile file, string extension)
    {
        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[12];
        var read = stream.Read(header);
        stream.Position = 0;

        var valid = extension switch
        {
            ".pdf" => StartsWith(header[..read], [0x25, 0x50, 0x44, 0x46]),
            ".png" => HasPngStructure(stream, header[..read]),
            ".jpg" or ".jpeg" => HasJpegStructure(stream, header[..read]),
            ".gif" => HasGifStructure(stream, header[..read]),
            ".webp" => HasWebpStructure(stream, header[..read]),
            ".doc" or ".xls" => StartsWith(header[..read], [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]),
            ".docx" => HasOpenXmlStructure(stream, "word/document.xml"),
            ".xlsx" => HasOpenXmlStructure(stream, "xl/workbook.xml"),
            ".xml" => IsSafeXml(stream),
            ".csv" => IsTextData(stream),
            _ => false
        };

        return valid ? null : "El contenido del archivo no coincide con el formato declarado.";
    }

    private static bool StartsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> signature) =>
        source.Length >= signature.Length && source[..signature.Length].SequenceEqual(signature);

    private static bool HasOpenXmlStructure(Stream stream, string expectedEntry)
    {
        try
        {
            using var bufferedStream = new MemoryStream();
            stream.CopyTo(bufferedStream);
            bufferedStream.Position = 0;
            using var archive = new ZipArchive(bufferedStream, ZipArchiveMode.Read, leaveOpen: true);
            var contentTypes = archive.GetEntry("[Content_Types].xml");
            var document = archive.GetEntry(expectedEntry);
            return contentTypes is not null
                && document is not null
                && IsSafeXmlEntry(contentTypes)
                && IsSafeXmlEntry(document);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool IsSafeXmlEntry(ZipArchiveEntry entry)
    {
        const long MaxXmlEntryBytes = 1024 * 1024;
        if (entry.Length <= 0 || entry.Length > MaxXmlEntryBytes) return false;

        try
        {
            using var stream = entry.Open();
            return IsSafeXml(stream);
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException)
        {
            return false;
        }
    }

    private static bool IsSafeXml(Stream stream)
    {
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(stream, settings);
            while (reader.Read()) { }
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private static bool IsTextData(Stream stream)
    {
        var buffer = new byte[8192];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var index = 0; index < read; index++)
            {
                var value = buffer[index];
                if (value == 0 || (value < 0x20 && value is not (0x09 or 0x0A or 0x0D)))
                    return false;
            }
        }

        return true;
    }

    private static bool HasPngStructure(Stream stream, ReadOnlySpan<byte> header)
    {
        if (!StartsWith(header, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return false;
        stream.Position = Math.Max(0, stream.Length - 12);
        Span<byte> tail = stackalloc byte[12];
        return stream.Read(tail) == tail.Length
            && tail[4..8].SequenceEqual("IEND"u8)
            && tail[8] == 0 && tail[9] == 0 && tail[10] == 0 && tail[11] == 0;
    }

    private static bool HasJpegStructure(Stream stream, ReadOnlySpan<byte> header)
    {
        if (!StartsWith(header, [0xFF, 0xD8, 0xFF]) || stream.Length < 4) return false;
        stream.Position = stream.Length - 2;
        Span<byte> tail = stackalloc byte[2];
        return stream.Read(tail) == 2 && tail[0] == 0xFF && tail[1] == 0xD9;
    }

    private static bool HasGifStructure(Stream stream, ReadOnlySpan<byte> header)
    {
        if (!StartsWith(header, "GIF87a"u8) && !StartsWith(header, "GIF89a"u8)) return false;
        stream.Position = stream.Length - 1;
        return stream.ReadByte() == 0x3B;
    }

    private static bool HasWebpStructure(Stream stream, ReadOnlySpan<byte> header)
    {
        if (header.Length < 12 || !header[..4].SequenceEqual("RIFF"u8) || !header[8..12].SequenceEqual("WEBP"u8)) return false;
        Span<byte> size = stackalloc byte[4];
        stream.Position = 4;
        if (stream.Read(size) != 4) return false;
        var declaredLength = BitConverter.ToUInt32(size) + 8L;
        return declaredLength == stream.Length;
    }
}
