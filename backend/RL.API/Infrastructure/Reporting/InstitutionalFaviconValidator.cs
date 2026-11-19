using System.Buffers.Binary;

namespace RL.API.Infrastructure.Reporting;

public sealed record FaviconValidationResult(
    bool IsValid,
    string? MimeType,
    string? FailureReason,
    int Width = 0,
    int Height = 0);

/// <summary>
/// Validador seguro de archivos de favicon institucionales.
/// Inspecciona magic bytes y dimensiones para formatos ICO, PNG, JPEG, GIF y WebP,
/// impidiendo archivos corruptos, tipos no admitidos o payloads de tamaño excesivo.
/// </summary>
public static class InstitutionalFaviconValidator
{
    public const long MaxFaviconSizeBytes = 2 * 1024 * 1024; // 2 MB

    public static FaviconValidationResult Validate(byte[] bytes, string? extensionOrHint = null)
    {
        if (bytes is null || bytes.Length == 0)
            return new(false, null, "Contenido de favicon vacío.");

        if (bytes.Length > MaxFaviconSizeBytes)
            return new(false, null, $"El favicon supera el tamaño máximo permitido de {MaxFaviconSizeBytes / 1024 / 1024} MB.");

        // 1. Windows Icon Format (.ico): 0x00 0x00 0x01 0x00
        if (bytes.Length >= 6 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0)
        {
            var count = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4, 2));
            if (count is >= 1 and <= 256)
            {
                int width = bytes.Length >= 7 ? (bytes[6] == 0 ? 256 : bytes[6]) : 32;
                int height = bytes.Length >= 8 ? (bytes[7] == 0 ? 256 : bytes[7]) : 32;
                return new(true, "image/x-icon", null, width, height);
            }
        }

        // 2. PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 24 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
            if (width is < 16 or > 2048 || height is < 16 or > 2048)
                return new(false, null, $"Dimensiones PNG {width}x{height} fuera del rango permitido para favicon (16..2048).");

            return new(true, "image/png", null, width, height);
        }

        // 3. JPEG: FF D8 FF
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            try
            {
                var dims = InstitutionalJpegInspector.InspectJpeg(bytes);
                if (dims.Width is < 16 or > 2048 || dims.Height is < 16 or > 2048)
                    return new(false, null, $"Dimensiones JPEG {dims.Width}x{dims.Height} fuera del rango permitido para favicon (16..2048).");

                return new(true, "image/jpeg", null, dims.Width, dims.Height);
            }
            catch (Exception ex)
            {
                return new(false, null, $"Imagen JPEG no válida para favicon: {ex.Message}");
            }
        }

        // 4. GIF: GIF87a / GIF89a
        if (bytes.Length >= 10 &&
            bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38 &&
            (bytes[4] == 0x37 || bytes[4] == 0x39) && bytes[5] == 0x61)
        {
            var width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6, 2));
            var height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2));
            if (width is < 16 or > 2048 || height is < 16 or > 2048)
                return new(false, null, $"Dimensiones GIF {width}x{height} fuera del rango permitido para favicon (16..2048).");

            return new(true, "image/gif", null, width, height);
        }

        // 5. WebP: RIFF .... WEBP
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return new(true, "image/webp", null, 32, 32);
        }

        return new(false, null, "El archivo no contiene un formato de imagen de favicon admitido (ICO, PNG, JPEG, GIF, WebP).");
    }
}
