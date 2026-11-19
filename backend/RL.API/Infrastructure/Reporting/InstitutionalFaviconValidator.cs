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
/// Inspecciona la firma binaria, integridad estructural de chunks/segmentos,
/// dimensiones reales (16..2048 px), límites de recursos (<= 2MB, <= 4M píxeles)
/// y finalización válida para formatos ICO, PNG, JPEG, GIF y WebP,
/// impidiendo archivos corruptos, truncados o tipos no admitidos.
/// </summary>
public static class InstitutionalFaviconValidator
{
    public const long MaxFaviconSizeBytes = 2 * 1024 * 1024; // 2 MB
    public const int MinDimension = 16;
    public const int MaxDimension = 2048;
    public const long MaxPixelCount = 4_194_304; // 2048 x 2048 píxeles

    private static readonly uint[] CrcTable = InitializeCrcTable();

    private static uint[] InitializeCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
            {
                c = (c & 1) != 0 ? (0xEDB88320 ^ (c >> 1)) : (c >> 1);
            }
            table[i] = c;
        }
        return table;
    }

    public static uint ComputeCrc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        for (int i = 0; i < data.Length; i++)
        {
            crc = CrcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        }
        return ~crc;
    }

    public static FaviconValidationResult Validate(byte[] bytes, string? extensionOrHint = null)
    {
        if (bytes is null || bytes.Length == 0)
            return new(false, null, "Contenido de favicon vacío.");

        if (bytes.Length > MaxFaviconSizeBytes)
            return new(false, null, $"El favicon supera el tamaño máximo permitido de {MaxFaviconSizeBytes / 1024 / 1024} MB.");

        // 1. Windows Icon Format (.ico): 0x00 0x00 0x01 0x00
        if (bytes.Length >= 6 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0)
        {
            return ValidateIco(bytes);
        }

        // 2. PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return ValidatePng(bytes);
        }

        // 3. JPEG: FF D8 FF
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return ValidateJpeg(bytes);
        }

        // 4. GIF: GIF87a / GIF89a
        if (bytes.Length >= 6 &&
            bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38 &&
            (bytes[4] == 0x37 || bytes[4] == 0x39) && bytes[5] == 0x61)
        {
            return ValidateGif(bytes);
        }

        // 5. WebP: RIFF .... WEBP
        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return ValidateWebp(bytes);
        }

        return new(false, null, "El archivo no contiene un formato de imagen de favicon admitido (ICO, PNG, JPEG, GIF, WebP).");
    }

    private static FaviconValidationResult ValidatePng(byte[] bytes)
    {
        if (bytes.Length < 8 + 12 + 13 + 12)
            return new(false, null, "Archivo PNG truncado (longitud inferior al mínimo estructural).");

        var offset = 8;
        int width = 0;
        int height = 0;
        bool hasIhdr = false;
        bool hasIdat = false;
        bool hasIend = false;

        while (offset + 8 <= bytes.Length)
        {
            var chunkLength = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            if (chunkLength < 0 || chunkLength > MaxFaviconSizeBytes)
                return new(false, null, "Longitud de chunk PNG inválida o desbordada.");

            var chunkType = bytes.AsSpan(offset + 4, 4);
            var totalChunkSize = checked(chunkLength + 12);

            if (offset + totalChunkSize > bytes.Length)
                return new(false, null, "Chunk PNG truncado antes de completar sus datos o CRC.");

            var expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + 8 + chunkLength, 4));
            var actualCrc = ComputeCrc32(bytes.AsSpan(offset + 4, chunkLength + 4));
            if (expectedCrc != actualCrc)
                return new(false, null, "Integridad de chunk PNG corrupta (falla de verificación CRC).");

            if (!hasIhdr)
            {
                if (!chunkType.SequenceEqual("IHDR"u8))
                    return new(false, null, "El primer chunk del PNG debe ser obligatoriamente IHDR.");

                if (chunkLength != 13)
                    return new(false, null, "Longitud del chunk IHDR de PNG inválida (debe ser 13 bytes).");

                var dataSpan = bytes.AsSpan(offset + 8, chunkLength);
                width = BinaryPrimitives.ReadInt32BigEndian(dataSpan.Slice(0, 4));
                height = BinaryPrimitives.ReadInt32BigEndian(dataSpan.Slice(4, 4));
                var bitDepth = dataSpan[8];
                var colorType = dataSpan[9];
                var compression = dataSpan[10];
                var filter = dataSpan[11];
                var interlace = dataSpan[12];

                if (width is < MinDimension or > MaxDimension || height is < MinDimension or > MaxDimension)
                    return new(false, null, $"Dimensiones PNG {width}x{height} fuera del rango permitido para favicon ({MinDimension}..{MaxDimension}).");

                if ((long)width * height > MaxPixelCount)
                    return new(false, null, "El recuento de píxeles del PNG excede el límite máximo de memoria.");

                if (compression != 0 || filter != 0 || interlace > 1)
                    return new(false, null, "Parámetros de compresión o filtrado de PNG inválidos.");

                hasIhdr = true;
            }
            else
            {
                if (chunkType.SequenceEqual("IHDR"u8))
                    return new(false, null, "Chunk IHDR duplicado en archivo PNG.");

                if (chunkType.SequenceEqual("IDAT"u8))
                {
                    if (chunkLength > 0)
                        hasIdat = true;
                }
                else if (chunkType.SequenceEqual("IEND"u8))
                {
                    if (chunkLength != 0)
                        return new(false, null, "El chunk IEND de PNG debe tener longitud cero.");

                    if (!hasIdat)
                        return new(false, null, "El archivo PNG finaliza sin contener datos de imagen (IDAT ausente).");

                    hasIend = true;
                    offset += totalChunkSize;
                    break;
                }
            }

            offset += totalChunkSize;
        }

        if (!hasIend)
            return new(false, null, "Archivo PNG incompleto o truncado (chunk IEND no alcanzado).");

        return new(true, "image/png", null, width, height);
    }

    private static FaviconValidationResult ValidateJpeg(byte[] bytes)
    {
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            return new(false, null, "Firma JPEG ausente o archivo truncado.");

        bool hasEoi = false;
        for (int i = bytes.Length - 2; i >= Math.Max(2, bytes.Length - 1024); i--)
        {
            if (bytes[i] == 0xFF && bytes[i + 1] == 0xD9)
            {
                hasEoi = true;
                break;
            }
        }
        if (!hasEoi)
            return new(false, null, "Archivo JPEG incompleto o truncado (marcador EOI 0xFF 0xD9 ausente).");

        try
        {
            var (width, height, _) = InstitutionalJpegInspector.InspectJpeg(bytes);
            if (width is < MinDimension or > MaxDimension || height is < MinDimension or > MaxDimension)
                return new(false, null, $"Dimensiones JPEG {width}x{height} fuera del rango permitido para favicon ({MinDimension}..{MaxDimension}).");

            if ((long)width * height > MaxPixelCount)
                return new(false, null, "El recuento de píxeles del JPEG excede el límite máximo de memoria.");

            return new(true, "image/jpeg", null, width, height);
        }
        catch (Exception ex)
        {
            return new(false, null, $"Estructura JPEG corrupta o truncada: {ex.Message}");
        }
    }

    private static FaviconValidationResult ValidateIco(byte[] bytes)
    {
        if (bytes.Length < 6)
            return new(false, null, "Archivo ICO truncado (encabezado incompleto).");

        if (bytes[0] != 0 || bytes[1] != 0 || bytes[2] != 1 || bytes[3] != 0)
            return new(false, null, "Firma de archivo ICO inválida (debe ser 0x00 0x00 0x01 0x00).");

        var imageCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(4, 2));
        if (imageCount is < 1 or > 256)
            return new(false, null, $"Número de imágenes en ICO fuera de rango permitido (1..256): {imageCount}.");

        var requiredHeaderSize = checked(6 + imageCount * 16);
        if (bytes.Length < requiredHeaderSize)
            return new(false, null, "Archivo ICO truncado (el directorio de imágenes excede la longitud del archivo).");

        int bestWidth = 0;
        int bestHeight = 0;
        bool hasValidEntry = false;

        for (int i = 0; i < imageCount; i++)
        {
            var entryOffset = 6 + i * 16;
            int entryWidth = bytes[entryOffset] == 0 ? 256 : bytes[entryOffset];
            int entryHeight = bytes[entryOffset + 1] == 0 ? 256 : bytes[entryOffset + 1];

            var bytesInRes = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(entryOffset + 8, 4));
            var imageOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(entryOffset + 12, 4));

            if (bytesInRes == 0)
                return new(false, null, $"Entrada {i} de ICO con tamaño de recurso vacío.");

            if (imageOffset < requiredHeaderSize)
                return new(false, null, $"Entrada {i} de ICO con desplazamiento de imagen inválido ({imageOffset} < {requiredHeaderSize}).");

            if ((long)imageOffset + bytesInRes > bytes.Length)
                return new(false, null, $"Entrada {i} de ICO con datos fuera de los límites del archivo (offset {imageOffset} + size {bytesInRes} > {bytes.Length}).");

            var imageData = bytes.AsSpan((int)imageOffset, (int)bytesInRes);

            if (imageData.Length >= 8 &&
                imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47 &&
                imageData[4] == 0x0D && imageData[5] == 0x0A && imageData[6] == 0x1A && imageData[7] == 0x0A)
            {
                var pngResult = ValidatePng(imageData.ToArray());
                if (!pngResult.IsValid)
                    return new(false, null, $"Entrada {i} de ICO contiene PNG corrupto: {pngResult.FailureReason}");

                if (bestWidth == 0)
                {
                    bestWidth = pngResult.Width;
                    bestHeight = pngResult.Height;
                }
                hasValidEntry = true;
            }
            else if (imageData.Length >= 40 && BinaryPrimitives.ReadUInt32LittleEndian(imageData.Slice(0, 4)) == 40)
            {
                var bmpWidth = BinaryPrimitives.ReadInt32LittleEndian(imageData.Slice(4, 4));
                var bmpHeight = BinaryPrimitives.ReadInt32LittleEndian(imageData.Slice(8, 4)) / 2;
                var planes = BinaryPrimitives.ReadUInt16LittleEndian(imageData.Slice(12, 2));
                var bitCount = BinaryPrimitives.ReadUInt16LittleEndian(imageData.Slice(14, 2));

                if (planes != 1)
                    return new(false, null, $"Entrada {i} de ICO contiene cabecera BMP con planos inválidos ({planes} != 1).");

                if (bitCount is not (1 or 4 or 8 or 16 or 24 or 32))
                    return new(false, null, $"Entrada {i} de ICO contiene profundidad de color BMP no soportada ({bitCount} bpp).");

                var resolvedWidth = bmpWidth > 0 ? bmpWidth : entryWidth;
                var resolvedHeight = bmpHeight > 0 ? bmpHeight : entryHeight;

                if (resolvedWidth is < MinDimension or > MaxDimension || resolvedHeight is < MinDimension or > MaxDimension)
                    return new(false, null, $"Dimensiones ICO {resolvedWidth}x{resolvedHeight} fuera del rango permitido ({MinDimension}..{MaxDimension}).");

                if (bestWidth == 0)
                {
                    bestWidth = resolvedWidth;
                    bestHeight = resolvedHeight;
                }
                hasValidEntry = true;
            }
            else
            {
                return new(false, null, $"Entrada {i} de ICO no contiene un formato de imagen interno válido (PNG o BITMAPINFOHEADER).");
            }
        }

        if (!hasValidEntry)
            return new(false, null, "El archivo ICO no contiene ninguna entrada de imagen válida.");

        return new(true, "image/x-icon", null, bestWidth, bestHeight);
    }

    private static FaviconValidationResult ValidateGif(byte[] bytes)
    {
        if (bytes.Length < 13)
            return new(false, null, "Archivo GIF truncado (encabezado incompleto).");

        if (bytes[0] != 0x47 || bytes[1] != 0x49 || bytes[2] != 0x46 || bytes[3] != 0x38 ||
            (bytes[4] != 0x37 && bytes[4] != 0x39) || bytes[5] != 0x61)
        {
            return new(false, null, "Firma GIF inválida (debe ser GIF87a o GIF89a).");
        }

        var width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(6, 2));
        var height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(8, 2));

        if (width is < MinDimension or > MaxDimension || height is < MinDimension or > MaxDimension)
            return new(false, null, $"Dimensiones GIF {width}x{height} fuera del rango permitido para favicon ({MinDimension}..{MaxDimension}).");

        if ((long)width * height > MaxPixelCount)
            return new(false, null, "El recuento de píxeles del GIF excede el límite máximo de memoria.");

        bool hasTrailer = false;
        for (int i = bytes.Length - 1; i >= Math.Max(0, bytes.Length - 16); i--)
        {
            if (bytes[i] == 0x3B)
            {
                hasTrailer = true;
                break;
            }
        }

        if (!hasTrailer)
            return new(false, null, "Archivo GIF incompleto o truncado (terminador 0x3B ausente).");

        bool hasImageDescriptor = false;
        for (int i = 10; i < bytes.Length - 9; i++)
        {
            if (bytes[i] == 0x2C)
            {
                hasImageDescriptor = true;
                break;
            }
        }

        if (!hasImageDescriptor)
            return new(false, null, "El archivo GIF no contiene descriptor de imagen (0x2C ausente).");

        return new(true, "image/gif", null, width, height);
    }

    private static FaviconValidationResult ValidateWebp(byte[] bytes)
    {
        if (bytes.Length < 20)
            return new(false, null, "Archivo WebP truncado (contenedor RIFF incompleto).");

        if (bytes[0] != 0x52 || bytes[1] != 0x49 || bytes[2] != 0x46 || bytes[3] != 0x46 ||
            bytes[8] != 0x57 || bytes[9] != 0x45 || bytes[10] != 0x42 || bytes[11] != 0x50)
        {
            return new(false, null, "Firma WebP inválida (contenedor RIFF/WEBP ausente).");
        }

        var riffPayloadSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4, 4));
        var expectedTotalSize = checked((long)riffPayloadSize + 8);
        if (bytes.Length < expectedTotalSize)
            return new(false, null, $"Archivo WebP truncado: longitud real ({bytes.Length}) menor a la declarada en RIFF ({expectedTotalSize}).");

        var chunkFourCc = bytes.AsSpan(12, 4);
        var chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16, 4));

        if (12 + 8 + chunkLength > bytes.Length)
            return new(false, null, "Chunk principal de WebP truncado antes de completar sus datos.");

        int width;
        int height;

        if (chunkFourCc.SequenceEqual("VP8 "u8))
        {
            if (chunkLength < 10 || bytes.Length < 30)
                return new(false, null, "Chunk VP8 de WebP demasiado corto.");

            if (bytes[23] != 0x9D || bytes[24] != 0x01 || bytes[25] != 0x2A)
                return new(false, null, "Código de inicio de cuadro clave VP8 inválido (0x9D 0x01 0x2A esperado).");

            width = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(26, 2)) & 0x3FFF;
            height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28, 2)) & 0x3FFF;
        }
        else if (chunkFourCc.SequenceEqual("VP8L"u8))
        {
            if (chunkLength < 5 || bytes.Length < 25)
                return new(false, null, "Chunk VP8L de WebP demasiado corto.");

            if (bytes[20] != 0x2F)
                return new(false, null, "Firma de chunk VP8L inválida (0x2F esperado).");

            var b1 = bytes[21];
            var b2 = bytes[22];
            var b3 = bytes[23];
            var b4 = bytes[24];

            width = (b1 | ((b2 & 0x3F) << 8)) + 1;
            height = (((b2 >> 6) | (b3 << 2) | ((b4 & 0xF) << 10))) + 1;
        }
        else if (chunkFourCc.SequenceEqual("VP8X"u8))
        {
            if (chunkLength < 10 || bytes.Length < 30)
                return new(false, null, "Chunk VP8X de WebP demasiado corto.");

            var w0 = bytes[24];
            var w1 = bytes[25];
            var w2 = bytes[26];
            width = (w0 | (w1 << 8) | (w2 << 16)) + 1;

            var h0 = bytes[27];
            var h1 = bytes[28];
            var h2 = bytes[29];
            height = (h0 | (h1 << 8) | (h2 << 16)) + 1;
        }
        else
        {
            return new(false, null, $"Formato o FourCC WebP no soportado o desconocido ({System.Text.Encoding.ASCII.GetString(chunkFourCc)}).");
        }

        if (width is < MinDimension or > MaxDimension || height is < MinDimension or > MaxDimension)
            return new(false, null, $"Dimensiones WebP {width}x{height} fuera del rango permitido para favicon ({MinDimension}..{MaxDimension}).");

        if ((long)width * height > MaxPixelCount)
            return new(false, null, "El recuento de píxeles del WebP excede el límite máximo de memoria.");

        return new(true, "image/webp", null, width, height);
    }
}
