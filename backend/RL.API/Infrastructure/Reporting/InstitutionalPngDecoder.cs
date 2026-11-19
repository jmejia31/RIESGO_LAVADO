using System.Buffers.Binary;
using System.IO.Compression;

namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Decodifica PNG proporcionados por la parametrización institucional.
/// No contiene logos predeterminados ni recursos de imagen incorporados.
/// </summary>
internal static class InstitutionalPngDecoder
{
    internal static (byte[] Rgb, int Width, int Height) DecodePngForBranding(byte[] bytes)
    {
        var decoded = Decode(bytes);
        return (decoded.Rgb.ToArray(), decoded.Width, decoded.Height);
    }

    internal static (byte[] Rgba, int Width, int Height) DecodePngRgba(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < signature.Length || !bytes.AsSpan(0, signature.Length).SequenceEqual(signature))
            throw new InvalidOperationException("La imagen configurada no es un PNG válido.");

        var offset = signature.Length;
        var idat = new MemoryStream();
        int width = 0;
        int height = 0;
        byte bitDepth = 0;
        byte colorType = 0;
        byte interlace = 0;

        while (offset + 12 <= bytes.Length)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4)));
            var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var dataStart = offset + 8;
            var dataEnd = checked(dataStart + length);
            if (dataEnd + 4 > bytes.Length)
                throw new InvalidOperationException("El PNG configurado contiene un chunk truncado.");

            if (type == "IHDR")
            {
                width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(dataStart, 4)));
                height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(dataStart + 4, 4)));
                bitDepth = bytes[dataStart + 8];
                colorType = bytes[dataStart + 9];
                interlace = bytes[dataStart + 12];
            }
            else if (type == "IDAT")
            {
                idat.Write(bytes, dataStart, length);
            }
            else if (type == "IEND")
            {
                break;
            }

            offset = dataEnd + 4;
        }

        if (width <= 0 || height <= 0 || bitDepth != 8 || (colorType != 2 && colorType != 6) || interlace != 0)
            throw new InvalidOperationException("El PNG configurado usa una variante no soportada para el procesamiento de favicon.");

        idat.Position = 0;
        using var decoded = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress, leaveOpen: true))
            zlib.CopyTo(decoded);

        var bytesPerPixel = colorType == 6 ? 4 : 3;
        var scanlineLength = checked(width * bytesPerPixel);
        var expectedLength = checked(height * (scanlineLength + 1));
        var scanlines = decoded.ToArray();
        if (scanlines.Length != expectedLength)
            throw new InvalidOperationException("El PNG configurado no pudo decodificarse con sus dimensiones declaradas.");

        var previous = new byte[scanlineLength];
        var current = new byte[scanlineLength];
        var rgba = new byte[checked(width * height * 4)];
        var rgbaOffset = 0;
        for (var row = 0; row < height; row++)
        {
            var filter = scanlines[row * (scanlineLength + 1)];
            scanlines.AsSpan(row * (scanlineLength + 1) + 1, scanlineLength).CopyTo(current);
            Unfilter(current, previous, filter, bytesPerPixel);
            for (var pixel = 0; pixel < width; pixel++)
            {
                var source = pixel * bytesPerPixel;
                rgba[rgbaOffset++] = current[source];
                rgba[rgbaOffset++] = current[source + 1];
                rgba[rgbaOffset++] = current[source + 2];
                rgba[rgbaOffset++] = colorType == 6 ? current[source + 3] : (byte)255;
            }

            (previous, current) = (current, previous);
        }

        return (rgba, width, height);
    }

    private static InstitutionalPng Decode(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < signature.Length || !bytes.AsSpan(0, signature.Length).SequenceEqual(signature))
            throw new InvalidOperationException("La imagen configurada no es un PNG válido.");

        var offset = signature.Length;
        var idat = new MemoryStream();
        int width = 0;
        int height = 0;
        byte bitDepth = 0;
        byte colorType = 0;
        byte interlace = 0;

        while (offset + 12 <= bytes.Length)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4)));
            var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var dataStart = offset + 8;
            var dataEnd = checked(dataStart + length);
            if (dataEnd + 4 > bytes.Length)
                throw new InvalidOperationException("El PNG configurado contiene un chunk truncado.");

            if (type == "IHDR")
            {
                width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(dataStart, 4)));
                height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(dataStart + 4, 4)));
                bitDepth = bytes[dataStart + 8];
                colorType = bytes[dataStart + 9];
                interlace = bytes[dataStart + 12];
            }
            else if (type == "IDAT")
            {
                idat.Write(bytes, dataStart, length);
            }
            else if (type == "IEND")
            {
                break;
            }

            offset = dataEnd + 4;
        }

        if (width <= 0 || height <= 0 || bitDepth != 8 || (colorType != 2 && colorType != 6) || interlace != 0)
            throw new InvalidOperationException("El PNG configurado usa una variante no soportada para el embedding documental.");

        idat.Position = 0;
        using var decoded = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress, leaveOpen: true))
            zlib.CopyTo(decoded);

        var bytesPerPixel = colorType == 6 ? 4 : 3;
        var scanlineLength = checked(width * bytesPerPixel);
        var expectedLength = checked(height * (scanlineLength + 1));
        var scanlines = decoded.ToArray();
        if (scanlines.Length != expectedLength)
            throw new InvalidOperationException("El PNG configurado no pudo decodificarse con sus dimensiones declaradas.");

        var previous = new byte[scanlineLength];
        var current = new byte[scanlineLength];
        var rgb = new byte[checked(width * height * 3)];
        var rgbOffset = 0;
        for (var row = 0; row < height; row++)
        {
            var filter = scanlines[row * (scanlineLength + 1)];
            scanlines.AsSpan(row * (scanlineLength + 1) + 1, scanlineLength).CopyTo(current);
            Unfilter(current, previous, filter, bytesPerPixel);
            for (var pixel = 0; pixel < width; pixel++)
            {
                var source = pixel * bytesPerPixel;
                var alpha = colorType == 6 ? current[source + 3] : (byte)255;
                rgb[rgbOffset++] = CompositeOnWhite(current[source], alpha);
                rgb[rgbOffset++] = CompositeOnWhite(current[source + 1], alpha);
                rgb[rgbOffset++] = CompositeOnWhite(current[source + 2], alpha);
            }

            (previous, current) = (current, previous);
        }

        return new InstitutionalPng(rgb, width, height);
    }

    private static void Unfilter(byte[] current, byte[] previous, byte filter, int bytesPerPixel)
    {
        for (var index = 0; index < current.Length; index++)
        {
            var left = index >= bytesPerPixel ? current[index - bytesPerPixel] : (byte)0;
            var up = previous[index];
            var upLeft = index >= bytesPerPixel ? previous[index - bytesPerPixel] : (byte)0;
            current[index] = filter switch
            {
                0 => current[index],
                1 => unchecked((byte)(current[index] + left)),
                2 => unchecked((byte)(current[index] + up)),
                3 => unchecked((byte)(current[index] + ((left + up) / 2))),
                4 => unchecked((byte)(current[index] + Paeth(left, up, upLeft))),
                _ => throw new InvalidOperationException($"Filtro PNG no soportado: {filter}.")
            };
        }
    }

    private static byte Paeth(byte left, byte up, byte upLeft)
    {
        var estimate = left + up - upLeft;
        var leftDistance = Math.Abs(estimate - left);
        var upDistance = Math.Abs(estimate - up);
        var upLeftDistance = Math.Abs(estimate - upLeft);
        return leftDistance <= upDistance && leftDistance <= upLeftDistance
            ? left
            : upDistance <= upLeftDistance ? up : upLeft;
    }

    private static byte CompositeOnWhite(byte channel, byte alpha) =>
        (byte)Math.Clamp((channel * alpha + 255 * (255 - alpha) + 127) / 255, 0, 255);

    private sealed record InstitutionalPng(byte[] Rgb, int Width, int Height);
}
