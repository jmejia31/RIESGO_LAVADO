using System.Buffers.Binary;
using System.IO.Compression;

namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Recurso institucional único para los generadores de documentos del backend.
/// El PNG se incorpora como recurso embebido desde assets/institutional y no se
/// obtiene desde Internet ni desde una ruta de ejecución mutable.
/// </summary>
public static class InstitutionalLogoAsset
{
    private static readonly Lazy<InstitutionalPng> Loaded = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public const string FileName = "ihss-logo.png";
    public const string Sha256 = "D57799214B441EA834320A1069D46A6930590ED4325F5A5EFAFCC65D115B7543";

    public static byte[] PngBytes => Loaded.Value.Bytes.ToArray();
    public static int Width => Loaded.Value.Width;
    public static int Height => Loaded.Value.Height;
    public static decimal AspectRatio => Width / (decimal)Height;

    /// <summary>
    /// Devuelve RGB comprimido y dimensiones para un XObject PDF válido.
    /// El decodificador admite el PNG RGBA/RGB de la fuente canónica y aplica
    /// los filtros definidos por la especificación PNG antes de comprimirlo.
    /// </summary>
    public static (byte[] Rgb, int Width, int Height) GetPdfRgb()
    {
        var png = Loaded.Value;
        return (png.Rgb.ToArray(), png.Width, png.Height);
    }

    private static InstitutionalPng Load()
    {
        var assembly = typeof(InstitutionalLogoAsset).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(FileName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
            throw new InvalidOperationException($"No se encontró el recurso embebido {FileName}.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"No se pudo abrir el recurso embebido {resourceName}.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();
        if (!Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).Equals(Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"La integridad de {FileName} no coincide con el SHA-256 autorizado.");

        return Decode(bytes);
    }

    private static InstitutionalPng Decode(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (bytes.Length < signature.Length || !bytes.AsSpan(0, signature.Length).SequenceEqual(signature))
            throw new InvalidOperationException("El logo institucional no es un PNG válido.");

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
                throw new InvalidOperationException("El PNG institucional contiene un chunk truncado.");

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
            throw new InvalidOperationException("El PNG institucional usa una variante no soportada para el embedding documental.");

        idat.Position = 0;
        using var decoded = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress, leaveOpen: true))
            zlib.CopyTo(decoded);

        var bytesPerPixel = colorType == 6 ? 4 : 3;
        var scanlineLength = checked(width * bytesPerPixel);
        var expectedLength = checked(height * (scanlineLength + 1));
        var scanlines = decoded.ToArray();
        if (scanlines.Length != expectedLength)
            throw new InvalidOperationException("El PNG institucional no pudo decodificarse con sus dimensiones declaradas.");

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
                rgb[rgbOffset++] = current[source];
                rgb[rgbOffset++] = current[source + 1];
                rgb[rgbOffset++] = current[source + 2];
            }

            (previous, current) = (current, previous);
        }

        return new InstitutionalPng(bytes, rgb, width, height);
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

    private sealed record InstitutionalPng(byte[] Bytes, byte[] Rgb, int Width, int Height);
}
