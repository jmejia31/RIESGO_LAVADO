using System.Buffers.Binary;
using System.IO.Compression;

namespace RL.API.Infrastructure.Reporting;

public sealed record FaviconConversionResult(
    byte[] IcoBytes,
    int PrimaryWidth,
    int PrimaryHeight,
    int ImageCount);

/// <summary>
/// Convertidor institucional de imágenes a formato Favicon ICO multirresolución optimizado.
/// Transforma cualquier imagen admitida (PNG, JPEG, WebP, ICO) en un recurso estándar
/// con proporciones cuadradas normalizadas (contain), centrado sin deformación,
/// preservación estricta de transparencia y tamaños optimizados (16x16, 32x32, 48x48).
/// </summary>
public static class InstitutionalFaviconConverter
{
    private static readonly int[] StandardFaviconSizes = [16, 32, 48];

    public static byte[] ConvertToFaviconIco(byte[] inputBytes, string? extensionOrHint = null)
    {
        var result = Convert(inputBytes, extensionOrHint);
        return result.IcoBytes;
    }

    public static FaviconConversionResult Convert(byte[] inputBytes, string? extensionOrHint = null)
    {
        if (inputBytes is null || inputBytes.Length == 0)
            throw new ArgumentException("El archivo de imagen está vacío.", nameof(inputBytes));

        if (inputBytes.Length > InstitutionalFaviconValidator.MaxFaviconSizeBytes)
            throw new ArgumentException($"El archivo supera el tamaño máximo permitido de {InstitutionalFaviconValidator.MaxFaviconSizeBytes / 1024 / 1024} MB.", nameof(inputBytes));

        var validation = InstitutionalFaviconValidator.Validate(inputBytes, extensionOrHint);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.FailureReason ?? "El archivo no contiene un formato de imagen admitido.");

        // Caso 1: Si ya es un archivo ICO válido estructurado, verificar integridad y retornar
        if (validation.MimeType == "image/x-icon" && inputBytes.Length >= 6 && inputBytes[0] == 0 && inputBytes[1] == 0 && inputBytes[2] == 1 && inputBytes[3] == 0)
        {
            var count = BinaryPrimitives.ReadUInt16LittleEndian(inputBytes.AsSpan(4, 2));
            if (count >= 1)
            {
                return new(inputBytes, validation.Width, validation.Height, count);
            }
        }

        // Caso 2: Extraer matriz RGBA y dimensiones reales validadas
        var (sourceRgba, srcWidth, srcHeight) = ExtractRgba(inputBytes, validation.MimeType!, validation.Width, validation.Height);

        if (srcWidth <= 0 || srcHeight <= 0)
            throw new InvalidOperationException("No se pudieron determinar las dimensiones de la imagen.");

        // Normalizar a lienzo cuadrado manteniendo proporciones (contain) y centrado
        var (squareRgba, squareSize) = ContainInSquare(sourceRgba, srcWidth, srcHeight);

        // Generar representaciones PNG para cada tamaño de icono (16x16, 32x32, 48x48)
        var pngStreams = new List<(int Size, byte[] PngBytes)>();
        foreach (var size in StandardFaviconSizes)
        {
            byte[] resizedRgba = ResampleBilinear(squareRgba, squareSize, squareSize, size, size);
            byte[] png = EncodePng(resizedRgba, size, size);
            pngStreams.Add((size, png));
        }

        // Empaquetar en formato ICO multirresolución estándar
        byte[] icoBytes = PackageAsIco(pngStreams);

        // Validar el archivo ICO resultante con el validador estricto institucional
        var outputCheck = InstitutionalFaviconValidator.Validate(icoBytes, ".ico");
        if (!outputCheck.IsValid)
            throw new InvalidOperationException($"La conversión produjo un ICO inválido: {outputCheck.FailureReason}");

        return new(icoBytes, outputCheck.Width, outputCheck.Height, pngStreams.Count);
    }

    private static (byte[] Rgba, int Width, int Height) ExtractRgba(byte[] bytes, string mimeType, int validatedWidth, int validatedHeight)
    {
        // Nunca sustituir la imagen del usuario por un rectángulo de color fijo.
        // La interfaz convierte JPEG/WebP a PNG RGBA real antes de subirlos.
        if (mimeType != "image/png")
            throw new InvalidOperationException("Este formato necesita normalización real a PNG antes de la conversión. No se generan iconos sintéticos.");

        try
        {
            var decoded = InstitutionalPngDecoder.DecodePngRgba(bytes);
            if (decoded.Width != validatedWidth || decoded.Height != validatedHeight)
                throw new InvalidOperationException("Las dimensiones decodificadas no coinciden con el archivo validado.");
            return decoded;
        }
        catch (Exception exception) when (exception is InvalidOperationException or InvalidDataException or IOException or OverflowException or ArgumentOutOfRangeException)
        {
            throw new InvalidOperationException("No se pudo decodificar el PNG de forma íntegra; se rechaza sin sustituir sus píxeles.", exception);
        }
    }

    private static (byte[] Rgba, int Size) ContainInSquare(byte[] srcRgba, int width, int height)
    {
        if (width == height)
            return (srcRgba, width);

        int size = Math.Max(width, height);
        var square = new byte[size * size * 4];
        // square es inicializado con ceros (completamente transparente: R=0, G=0, B=0, A=0)

        int offsetX = (size - width) / 2;
        int offsetY = (size - height) / 2;

        for (int y = 0; y < height; y++)
        {
            int srcOffset = y * width * 4;
            int dstOffset = ((offsetY + y) * size + offsetX) * 4;
            Array.Copy(srcRgba, srcOffset, square, dstOffset, width * 4);
        }

        return (square, size);
    }

    private static byte[] ResampleBilinear(byte[] src, int srcW, int srcH, int dstW, int dstH)
    {
        if (srcW == dstW && srcH == dstH)
            return (byte[])src.Clone();

        var dst = new byte[dstW * dstH * 4];
        float xRatio = (float)srcW / dstW;
        float yRatio = (float)srcH / dstH;

        for (int dstY = 0; dstY < dstH; dstY++)
        {
            float srcY = (dstY + 0.5f) * yRatio - 0.5f;
            int y0 = Math.Clamp((int)MathF.Floor(srcY), 0, srcH - 1);
            int y1 = Math.Clamp(y0 + 1, 0, srcH - 1);
            float yLerp = Math.Clamp(srcY - y0, 0f, 1f);

            for (int dstX = 0; dstX < dstW; dstX++)
            {
                float srcX = (dstX + 0.5f) * xRatio - 0.5f;
                int x0 = Math.Clamp((int)MathF.Floor(srcX), 0, srcW - 1);
                int x1 = Math.Clamp(x0 + 1, 0, srcW - 1);
                float xLerp = Math.Clamp(srcX - x0, 0f, 1f);

                int idx00 = (y0 * srcW + x0) * 4;
                int idx10 = (y0 * srcW + x1) * 4;
                int idx01 = (y1 * srcW + x0) * 4;
                int idx11 = (y1 * srcW + x1) * 4;

                float w00 = (1f - xLerp) * (1f - yLerp);
                float w10 = xLerp * (1f - yLerp);
                float w01 = (1f - xLerp) * yLerp;
                float w11 = xLerp * yLerp;

                int dstIdx = (dstY * dstW + dstX) * 4;
                for (int c = 0; c < 4; c++)
                {
                    float val = src[idx00 + c] * w00 +
                                src[idx10 + c] * w10 +
                                src[idx01 + c] * w01 +
                                src[idx11 + c] * w11;
                    dst[dstIdx + c] = (byte)Math.Clamp((int)MathF.Round(val), 0, 255);
                }
            }
        }

        return dst;
    }

    private static byte[] EncodePng(byte[] rgba, int width, int height)
    {
        using var ms = new MemoryStream();
        // Firma PNG
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.Slice(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.Slice(4, 4), height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 6;  // color type 6: RGBA
        ihdr[10] = 0; // compression
        ihdr[11] = 0; // filter
        ihdr[12] = 0; // interlace
        WriteChunk(ms, "IHDR"u8, ihdr);

        // IDAT
        var uncompressedScanlines = new byte[height * (1 + width * 4)];
        int srcOffset = 0;
        int dstOffset = 0;
        for (int y = 0; y < height; y++)
        {
            uncompressedScanlines[dstOffset++] = 0; // tipo de filtro 0 (None)
            Array.Copy(rgba, srcOffset, uncompressedScanlines, dstOffset, width * 4);
            srcOffset += width * 4;
            dstOffset += width * 4;
        }

        using (var compressedMs = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressedMs, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(uncompressedScanlines, 0, uncompressedScanlines.Length);
            }
            WriteChunk(ms, "IDAT"u8, compressedMs.ToArray());
        }

        // IEND
        WriteChunk(ms, "IEND"u8, []);

        return ms.ToArray();
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(len, data.Length);
        stream.Write(len);

        stream.Write(type);
        if (data.Length > 0)
            stream.Write(data);

        var crcBuffer = new byte[type.Length + data.Length];
        type.CopyTo(crcBuffer);
        if (data.Length > 0)
            data.CopyTo(crcBuffer.AsSpan(type.Length));

        var crc = InstitutionalFaviconValidator.ComputeCrc32(crcBuffer);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static byte[] PackageAsIco(List<(int Size, byte[] PngBytes)> images)
    {
        using var ms = new MemoryStream();
        // ICONDIR (6 bytes)
        ms.WriteByte(0); ms.WriteByte(0); // Reserved = 0
        ms.WriteByte(1); ms.WriteByte(0); // Type = 1 (Icon)
        Span<byte> countBytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(countBytes, (ushort)images.Count);
        ms.Write(countBytes);

        int currentOffset = 6 + images.Count * 16;
        for (int i = 0; i < images.Count; i++)
        {
            var (size, png) = images[i];
            Span<byte> entry = stackalloc byte[16];
            entry[0] = (byte)(size >= 256 ? 0 : size); // Width
            entry[1] = (byte)(size >= 256 ? 0 : size); // Height
            entry[2] = 0; // Color count
            entry[3] = 0; // Reserved
            BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(4, 2), 1); // Planes = 1
            BinaryPrimitives.WriteUInt16LittleEndian(entry.Slice(6, 2), 32); // Bit count = 32
            BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(8, 4), (uint)png.Length); // Bytes in res
            BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(12, 4), (uint)currentOffset); // Offset
            ms.Write(entry);
            currentOffset += png.Length;
        }

        // Image payloads
        foreach (var (_, png) in images)
        {
            ms.Write(png);
        }

        return ms.ToArray();
    }
}
