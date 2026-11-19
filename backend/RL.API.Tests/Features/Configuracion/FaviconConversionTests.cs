using System.Buffers.Binary;
using System.IO.Compression;
using RL.API.Infrastructure.Reporting;
using Xunit;

namespace RL.API.Tests.Features.Configuracion;

public class FaviconConversionTests
{
    private static byte[] CrearPngRgba(int width, int height, bool conTransparencia = false)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4, 4), height);
        ihdr[8] = 8;  // bit depth 8
        ihdr[9] = 6;  // color type 6: RGBA
        ihdr[10] = 0; // deflate
        ihdr[11] = 0; // filter none
        ihdr[12] = 0; // no interlace
        EscribirChunk(ms, "IHDR"u8, ihdr);

        // IDAT
        var rawRowLength = 1 + width * 4;
        var rawData = new byte[rawRowLength * height];
        for (int y = 0; y < height; y++)
        {
            var rowStart = y * rawRowLength;
            rawData[rowStart] = 0; // Filter none
            for (int x = 0; x < width; x++)
            {
                var p = rowStart + 1 + x * 4;
                rawData[p] = 0x1E;     // R
                rawData[p + 1] = 0x3A; // G
                rawData[p + 2] = 0x8A; // B
                // Si conTransparencia es true, hacer las esquinas transparentes
                if (conTransparencia && (x < 4 || x >= width - 4 || y < 4 || y >= height - 4))
                {
                    rawData[p + 3] = 0x00; // Alpha 0 (transparente)
                }
                else
                {
                    rawData[p + 3] = 0xFF; // Alpha 255 (opaco)
                }
            }
        }

        using (var compressedMs = new MemoryStream())
        {
            using (var zlib = new ZLibStream(compressedMs, CompressionLevel.Fastest, leaveOpen: true))
            {
                zlib.Write(rawData, 0, rawData.Length);
            }
            EscribirChunk(ms, "IDAT"u8, compressedMs.ToArray());
        }

        // IEND
        EscribirChunk(ms, "IEND"u8, []);
        return ms.ToArray();
    }

    private static void EscribirChunk(MemoryStream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
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

    [Fact]
    public void FC_01_PngCuadradoValido_GeneraFaviconIcoValidoMultirresolucion()
    {
        var input = FaviconEndpointTests.CrearPngValido(64, 64);
        var result = InstitutionalFaviconConverter.Convert(input, ".png");

        Assert.NotNull(result);
        Assert.True(result.IcoBytes.Length > 0);
        Assert.Equal(3, result.ImageCount); // 16x16, 32x32, 48x48

        var validation = InstitutionalFaviconValidator.Validate(result.IcoBytes, ".ico");
        Assert.True(validation.IsValid);
        Assert.Equal("image/x-icon", validation.MimeType);
    }

    [Fact]
    public void FC_02_PngHorizontalValido_NormalizaLienzoCuadradoSinDeformacion()
    {
        // 120 x 40 (proporción 3:1)
        var input = FaviconEndpointTests.CrearPngValido(120, 40);
        var result = InstitutionalFaviconConverter.Convert(input, ".png");

        Assert.NotNull(result);
        var validation = InstitutionalFaviconValidator.Validate(result.IcoBytes, ".ico");
        Assert.True(validation.IsValid);
        Assert.Equal("image/x-icon", validation.MimeType);
    }

    [Fact]
    public void FC_03_PngVerticalValido_NormalizaLienzoCuadradoSinDeformacion()
    {
        // 40 x 120 (proporción 1:3)
        var input = FaviconEndpointTests.CrearPngValido(40, 120);
        var result = InstitutionalFaviconConverter.Convert(input, ".png");

        Assert.NotNull(result);
        var validation = InstitutionalFaviconValidator.Validate(result.IcoBytes, ".ico");
        Assert.True(validation.IsValid);
        Assert.Equal("image/x-icon", validation.MimeType);
    }

    [Fact]
    public void FC_04_JpgDirectoSinDecodificador_RechazaEnLugarDeCrearColorFalso()
    {
        var input = FaviconEndpointTests.CrearJpegValido(64, 64);
        // Una imagen no decodificada no puede convertirse en un cuadrado azul ficticio.
        // El flujo Angular normaliza estos archivos a PNG RGBA auténtico.
        Assert.Throws<InvalidOperationException>(() =>
            InstitutionalFaviconConverter.Convert(input, ".jpg"));
    }

    [Fact]
    public void FC_05_JpegDirectoSinDecodificador_RechazaEnLugarDeCrearColorFalso()
    {
        var input = FaviconEndpointTests.CrearJpegValido(80, 80);
        // Una imagen no decodificada no puede convertirse en un cuadrado azul ficticio.
        // El flujo Angular normaliza estos archivos a PNG RGBA auténtico.
        Assert.Throws<InvalidOperationException>(() =>
            InstitutionalFaviconConverter.Convert(input, ".jpeg"));
    }

    [Fact]
    public void FC_06_PngTransparente_PreservaCanalAlfa()
    {
        var input = CrearPngRgba(48, 48, conTransparencia: true);
        var result = InstitutionalFaviconConverter.Convert(input, ".png");

        Assert.NotNull(result);
        var validation = InstitutionalFaviconValidator.Validate(result.IcoBytes, ".ico");
        Assert.True(validation.IsValid);
        Assert.Equal("image/x-icon", validation.MimeType);
    }

    [Fact]
    public void FC_07_WebpDirectoSinDecodificador_RechazaEnLugarDeCrearColorFalso()
    {
        var input = FaviconEndpointTests.CrearWebpValido(64, 64);
        // Una imagen no decodificada no puede convertirse en un cuadrado azul ficticio.
        // El flujo Angular normaliza estos archivos a PNG RGBA auténtico.
        Assert.Throws<InvalidOperationException>(() =>
            InstitutionalFaviconConverter.Convert(input, ".webp"));
    }

    [Fact]
    public void FC_08_IcoValido_PreservaEstructuraCompatibilidad()
    {
        var input = FaviconEndpointTests.CrearIcoValido(32, 32);
        var result = InstitutionalFaviconConverter.Convert(input, ".ico");

        Assert.NotNull(result);
        var validation = InstitutionalFaviconValidator.Validate(result.IcoBytes, ".ico");
        Assert.True(validation.IsValid);
        Assert.Equal("image/x-icon", validation.MimeType);
    }

    [Fact]
    public void FC_09_ImagenCorrupta_LanzaExcepcion()
    {
        var corruptBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x00, 0x00, 0xFF, 0xFF };
        Assert.Throws<InvalidOperationException>(() =>
            InstitutionalFaviconConverter.Convert(corruptBytes, ".png"));
    }

    [Fact]
    public void FC_10_ArchivoVacio_LanzaExcepcion()
    {
        Assert.Throws<ArgumentException>(() =>
            InstitutionalFaviconConverter.Convert(Array.Empty<byte>(), ".png"));
    }

    [Fact]
    public void FC_11_ArchivoDemasiadoGrande_LanzaExcepcion()
    {
        var oversized = new byte[InstitutionalFaviconValidator.MaxFaviconSizeBytes + 1];
        Assert.Throws<ArgumentException>(() =>
            InstitutionalFaviconConverter.Convert(oversized, ".png"));
    }

    [Fact]
    public void FC_12_ExtensionFalsificada_LanzaExcepcion()
    {
        var textBytes = "Este es un archivo de texto plano disfrazado de imagen"u8.ToArray();
        Assert.Throws<InvalidOperationException>(() =>
            InstitutionalFaviconConverter.Convert(textBytes, ".png"));
    }

    [Fact]
    public void FC_13_DimensionesPeligrosas_LanzaExcepcion()
    {
        // 8x8 es menor al mínimo institucional de 16x16
        var smallPng = FaviconEndpointTests.CrearPngValido(8, 8);
        Assert.Throws<InvalidOperationException>(() =>
            InstitutionalFaviconConverter.Convert(smallPng, ".png"));
    }

    [Fact]
    public void FC_14_HtmlRenombradoAPng_LanzaExcepcion()
    {
        var htmlBytes = "<!DOCTYPE html><html><head><title>Bad</title></head></html>"u8.ToArray();
        Assert.Throws<InvalidOperationException>(() =>
            InstitutionalFaviconConverter.Convert(htmlBytes, ".png"));
    }
    [Fact]
    public void FC_15_PngConTransparencia_GeneraBytesDistintosDePngOpaco()
    {
        var opaco = InstitutionalFaviconConverter.Convert(CrearPngRgba(48, 48), ".png");
        var transparente = InstitutionalFaviconConverter.Convert(CrearPngRgba(48, 48, conTransparencia: true), ".png");

        // Ambas entradas tienen la misma forma y color; difieren solamente en alfa.
        // El convertidor debe respetar esos píxeles, no reemplazarlos por colores fijos.
        Assert.NotEqual(Convert.ToHexString(opaco.IcoBytes), Convert.ToHexString(transparente.IcoBytes));
        Assert.True(InstitutionalFaviconValidator.Validate(transparente.IcoBytes, ".ico").IsValid);
    }

}
