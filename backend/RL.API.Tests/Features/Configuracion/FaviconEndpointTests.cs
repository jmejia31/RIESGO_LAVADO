using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using RL.API.Features.Configuracion;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Infrastructure.Reporting;
using Xunit;

namespace RL.API.Tests.Features.Configuracion;

public sealed class FaviconEndpointTests : IDisposable
{
    private readonly string _tempWebRoot;
    private readonly FakeWebHostEnvironment _fakeEnv;

    public FaviconEndpointTests()
    {
        _tempWebRoot = Path.Combine(Path.GetTempPath(), "rl_favicon_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempWebRoot, "uploads"));
        _fakeEnv = new FakeWebHostEnvironment
        {
            WebRootPath = _tempWebRoot,
            ContentRootPath = _tempWebRoot
        };
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempWebRoot))
                Directory.Delete(_tempWebRoot, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors in temp folder
        }
    }

    public static byte[] CrearPngValido(int width = 32, int height = 32)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR
        var ihdr = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.AsSpan(4, 4), height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 2;  // Truecolor (RGB)
        ihdr[10] = 0; // deflate
        ihdr[11] = 0; // filter
        ihdr[12] = 0; // no interlace
        EscribirChunk(ms, "IHDR"u8, ihdr);

        // IDAT
        var rawRowLength = 1 + width * 3;
        var rawData = new byte[rawRowLength * height];
        for (int y = 0; y < height; y++)
        {
            var rowStart = y * rawRowLength;
            rawData[rowStart] = 0; // Filter none
            for (int x = 0; x < width; x++)
            {
                var p = rowStart + 1 + x * 3;
                rawData[p] = 0x28;
                rawData[p + 1] = 0x34;
                rawData[p + 2] = 0xE2;
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

    public static byte[] CrearJpegValido(int width = 32, int height = 32)
    {
        using var stream = new MemoryStream();
        // SOI
        stream.Write([0xFF, 0xD8]);
        // APP0 (JFIF)
        stream.Write([0xFF, 0xE0, 0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00]);
        // DQT
        stream.Write([0xFF, 0xDB, 0x00, 0x43, 0x00]);
        for (int i = 0; i < 64; i++) stream.WriteByte(16);
        // SOF0
        stream.Write([0xFF, 0xC0, 0x00, 0x11, 0x08]);
        stream.WriteByte((byte)(height >> 8));
        stream.WriteByte((byte)(height & 0xFF));
        stream.WriteByte((byte)(width >> 8));
        stream.WriteByte((byte)(width & 0xFF));
        stream.Write([0x03, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01]);
        // SOS
        stream.Write([0xFF, 0xDA, 0x00, 0x0C, 0x03, 0x01, 0x00, 0x02, 0x11, 0x03, 0x11, 0x00, 0x3F, 0x00]);
        stream.Write([0x00, 0x00]); // scan data
        // EOI
        stream.Write([0xFF, 0xD9]);
        return stream.ToArray();
    }

    public static byte[] CrearIcoValido(int width = 32, int height = 32)
    {
        var embeddedPng = CrearPngValido(width, height);
        var bytes = new byte[6 + 16 + embeddedPng.Length];

        // ICONDIR
        bytes[0] = 0; bytes[1] = 0;
        bytes[2] = 1; bytes[3] = 0; // ICO type
        bytes[4] = 1; bytes[5] = 0; // 1 image

        // ICONDIRENTRY (16 bytes)
        bytes[6] = (byte)(width >= 256 ? 0 : width);
        bytes[7] = (byte)(height >= 256 ? 0 : height);
        bytes[8] = 0; // colorCount
        bytes[9] = 0; // reserved
        bytes[10] = 1; bytes[11] = 0; // planes = 1
        bytes[12] = 32; bytes[13] = 0; // 32 bpp
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(14, 4), (uint)embeddedPng.Length); // bytesInRes
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(18, 4), 22); // imageOffset = 22

        embeddedPng.CopyTo(bytes.AsSpan(22));
        return bytes;
    }

    public static byte[] CrearGifValido(int width = 32, int height = 32)
    {
        using var ms = new MemoryStream();
        // Header: GIF89a
        ms.Write([0x47, 0x49, 0x46, 0x38, 0x39, 0x61]);
        // Logical Screen Descriptor (7 bytes)
        Span<byte> dim = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(dim, (ushort)width);
        ms.Write(dim);
        BinaryPrimitives.WriteUInt16LittleEndian(dim, (ushort)height);
        ms.Write(dim);
        ms.Write([0x80, 0x00, 0x00]); // Global Color Table Flag (2 colors)
        // GCT (6 bytes)
        ms.Write([0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF]);
        // Image Descriptor (0x2C)
        ms.Write([0x2C, 0x00, 0x00, 0x00, 0x00]);
        ms.Write(dim); // width
        ms.Write(dim); // height
        ms.Write([0x00]); // packed
        // LZW data
        ms.Write([0x02, 0x02, 0x44, 0x01, 0x00]);
        // Trailer
        ms.WriteByte(0x3B);
        return ms.ToArray();
    }

    public static byte[] CrearWebpValido(int width = 32, int height = 32)
    {
        using var ms = new MemoryStream();
        ms.Write("RIFF"u8);
        Span<byte> sizePlaceholder = stackalloc byte[4];
        ms.Write(sizePlaceholder); // to be filled
        ms.Write("WEBP"u8);
        ms.Write("VP8L"u8);

        // VP8L chunk payload
        var payload = new byte[9];
        payload[0] = 0x2F; // signature
        // 14 bits width-1, 14 bits height-1
        var wMinus1 = (width - 1) & 0x3FFF;
        var hMinus1 = (height - 1) & 0x3FFF;
        var packed = (uint)(wMinus1 | (hMinus1 << 14));
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(1, 4), packed);
        payload[5] = 0x00; payload[6] = 0x00; payload[7] = 0x00; payload[8] = 0x00;

        Span<byte> vp8lLen = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(vp8lLen, (uint)payload.Length);
        ms.Write(vp8lLen);
        ms.Write(payload);

        var totalLen = (uint)(ms.Length - 8);
        var bytes = ms.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), totalLen);
        return bytes;
    }

    [Fact]
    public async Task Favicon_SinIconoConfigurado_Retorna204NoContentConCabeceraNotConfigured()
    {
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = null });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("not-configured", controller.Response.Headers["X-Favicon-Status"].ToString());
        Assert.Contains("no-cache", controller.Response.Headers["Cache-Control"].ToString());
    }

    [Fact]
    public async Task Favicon_IconoLocalValido_Retorna200OkConBytesCompletosYHeadersDeStreaming()
    {
        var iconoPath = Path.Combine(_tempWebRoot, "uploads", "favicon.png");
        var pngBytes = CrearPngValido(32, 32);
        await File.WriteAllBytesAsync(iconoPath, pngBytes);

        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/favicon.png" });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(pngBytes.Length, fileResult.FileContents.Length);
        Assert.Equal("available", controller.Response.Headers["X-Favicon-Status"].ToString());
        Assert.Equal("configured-local-file", controller.Response.Headers["X-Favicon-Source"].ToString());
        Assert.Contains("no-store", controller.Response.Headers["Cache-Control"].ToString());
    }

    [Fact]
    public void Validator_FV01_PngValidoCompleto_PasaConDimensionesReales()
    {
        var bytes = CrearPngValido(64, 48);
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.True(result.IsValid);
        Assert.Equal("image/png", result.MimeType);
        Assert.Equal(64, result.Width);
        Assert.Equal(48, result.Height);
    }

    [Fact]
    public void Validator_FV02_PngTruncado_Rechaza()
    {
        var bytes = CrearPngValido(32, 32);
        // Truncate before IEND
        var truncated = bytes.AsSpan(0, bytes.Length - 10).ToArray();
        var result = InstitutionalFaviconValidator.Validate(truncated);
        Assert.False(result.IsValid);
        Assert.Contains("truncado", result.FailureReason?.ToLowerInvariant());
    }

    [Fact]
    public void Validator_FV03_PngConCrcInvalido_Rechaza()
    {
        var bytes = CrearPngValido(32, 32);
        // Corrupt CRC of IHDR chunk (last 4 bytes of IHDR chunk at offset 29..32)
        bytes[29] ^= 0xFF;
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.False(result.IsValid);
        Assert.Contains("crc", result.FailureReason?.ToLowerInvariant());
    }

    [Fact]
    public void Validator_FV04_JpegValidoCompleto_Pasa()
    {
        var bytes = CrearJpegValido(64, 32);
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.True(result.IsValid);
        Assert.Equal("image/jpeg", result.MimeType);
        Assert.Equal(64, result.Width);
        Assert.Equal(32, result.Height);
    }

    [Fact]
    public void Validator_FV05_JpegIncompleto_Rechaza()
    {
        var bytes = CrearJpegValido(32, 32);
        // Remove EOI (last 2 bytes: FF D9)
        var truncated = bytes.AsSpan(0, bytes.Length - 2).ToArray();
        var result = InstitutionalFaviconValidator.Validate(truncated);
        Assert.False(result.IsValid);
        Assert.Contains("incompleto", result.FailureReason?.ToLowerInvariant());
    }

    [Fact]
    public void Validator_FV06_IcoValidoCompleto_Pasa()
    {
        var bytes = CrearIcoValido(32, 32);
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.True(result.IsValid);
        Assert.Equal("image/x-icon", result.MimeType);
        Assert.Equal(32, result.Width);
        Assert.Equal(32, result.Height);
    }

    [Fact]
    public void Validator_FV07_IcoConOffsetsInvalidos_Rechaza()
    {
        var bytes = CrearIcoValido(32, 32);
        // Alter offset to point beyond file length
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(18, 4), 99999);
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.False(result.IsValid);
        Assert.Contains("límites", result.FailureReason?.ToLowerInvariant());
    }

    [Fact]
    public void Validator_FV08_GifValidoCompleto_Pasa()
    {
        var bytes = CrearGifValido(48, 48);
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.True(result.IsValid);
        Assert.Equal("image/gif", result.MimeType);
        Assert.Equal(48, result.Width);
        Assert.Equal(48, result.Height);
    }

    [Fact]
    public void Validator_FV09_WebpValidoCompleto_PasaConDimensionesReales()
    {
        var bytes = CrearWebpValido(64, 48);
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.True(result.IsValid);
        Assert.Equal("image/webp", result.MimeType);
        Assert.Equal(64, result.Width);
        Assert.Equal(48, result.Height);
    }

    [Fact]
    public void Validator_FV10_WebpConDimensionesInvalidas_Rechaza()
    {
        var bytes = CrearWebpValido(8, 8); // < 16 px
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.False(result.IsValid);
        Assert.Contains("rango permitido", result.FailureReason?.ToLowerInvariant());
    }

    [Fact]
    public void Validator_FV11_ImagenSuperiorAlLimite2Mb_Rechaza()
    {
        var bytes = new byte[2 * 1024 * 1024 + 1];
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.False(result.IsValid);
        Assert.Contains("supera el tamaño máximo", result.FailureReason?.ToLowerInvariant());
    }

    [Fact]
    public void Validator_FV12_ArchivoNoDecodificableOBasura_Rechaza()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };
        var result = InstitutionalFaviconValidator.Validate(bytes);
        Assert.False(result.IsValid);
        Assert.Contains("no contiene un formato", result.FailureReason?.ToLowerInvariant());
    }

    [Theory]
    [InlineData("http://ihss.hn/favicon.ico")]
    [InlineData("ftp://ihss.hn/favicon.ico")]
    [InlineData("file:///C:/favicon.ico")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("javascript:alert(1)")]
    public async Task Favicon_EsquemasInseguros_RechazaConInvalidScheme(string esquemaInseguro)
    {
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = esquemaInseguro });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("invalid-scheme", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Theory]
    [InlineData("/uploads/../../etc/passwd")]
    [InlineData("/uploads/..\\..\\windows\\win.ini")]
    [InlineData("C:\\inetpub\\wwwroot\\favicon.ico")]
    [InlineData("/uploads/archivo:invalido.png")]
    public async Task Favicon_PathTraversalYCaracteresProhibidos_RechazaConInvalidPath(string pathInseguro)
    {
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = pathInseguro });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("invalid-path", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Fact]
    public async Task Favicon_HttpsDescargaExitosa_RetornaArchivoRemotoValidado()
    {
        var pngBytes = CrearPngValido(32, 32);
        var fakeDownloader = new FakeLogoDownloader(new InstitutionalDownloadedLogoResult(true, pngBytes, "image/png", null, 32, 32));
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "https://cdn.ihss.hn/favicon.png" });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon(fakeDownloader);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(pngBytes.Length, fileResult.FileContents.Length);
        Assert.Equal("available", controller.Response.Headers["X-Favicon-Status"].ToString());
        Assert.Equal("configured-https-url", controller.Response.Headers["X-Favicon-Source"].ToString());
    }

    [Fact]
    public async Task Favicon_HttpsDescargaFallidaOSsrf_RetornaNoContentConDetalle()
    {
        var fakeDownloader = new FakeLogoDownloader(new InstitutionalDownloadedLogoResult(false, null, null, "Destino SSRF bloqueado"));
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "https://192.168.1.1/favicon.ico" });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon(fakeDownloader);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Destino SSRF bloqueado", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Fact]
    public async Task EstadoFavicon_LocalValido_RetornaJsonConTelemetriaInstitucional()
    {
        var iconoPath = Path.Combine(_tempWebRoot, "uploads", "favicon.png");
        await File.WriteAllBytesAsync(iconoPath, CrearPngValido(48, 48));

        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/favicon.png" });
        var controller = new ConfiguracionController(service, _fakeEnv);

        var result = await controller.EstadoFavicon();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("configurado").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("disponible").GetBoolean());
        Assert.Equal("FAVICON_LOCAL", doc.RootElement.GetProperty("tipo").GetString());
        Assert.Equal("configured-local-file", doc.RootElement.GetProperty("estado").GetString());
        Assert.Equal(48, doc.RootElement.GetProperty("ancho").GetInt32());
        Assert.Equal(48, doc.RootElement.GetProperty("alto").GetInt32());
    }

    [Fact]
    public async Task EstadoFavicon_ArchivoTruncado_RetornaDisponibleFalseConRazon()
    {
        var iconoPath = Path.Combine(_tempWebRoot, "uploads", "favicon_trunc.png");
        var png = CrearPngValido(32, 32);
        await File.WriteAllBytesAsync(iconoPath, png.AsSpan(0, png.Length - 10).ToArray());

        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/favicon_trunc.png" });
        var controller = new ConfiguracionController(service, _fakeEnv);

        var result = await controller.EstadoFavicon();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("configurado").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("disponible").GetBoolean());
        Assert.Contains("truncado", doc.RootElement.GetProperty("estado").GetString()?.ToLowerInvariant());
    }
}

internal sealed class FakeConfiguracionService : IConfiguracionService
{
    private ConfigSistema _config;

    public FakeConfiguracionService(ConfigSistema config) => _config = config;

    public Task<ConfigSistema?> ObtenerConfigSistemaAsync() => Task.FromResult<ConfigSistema?>(_config);
    public Task<List<LoginSlide>> ObtenerSlidesAsync() => Task.FromResult(new List<LoginSlide>());
    public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() => Task.FromResult(new List<LoginSlide>());
    public Task<bool> GuardarConfigSistemaAsync(ConfigSistema config, long usuarioId, string? ip)
    {
        _config = config;
        return Task.FromResult(true);
    }
    public Task<bool> CrearSlideAsync(LoginSlide slide, long usuarioId, string? ip) => Task.FromResult(true);
    public Task<bool> ActualizarSlideAsync(int id, LoginSlide slide, long usuarioId, string? ip) => Task.FromResult(true);
    public Task<bool> EliminarSlideAsync(int id, long usuarioId, string? ip) => Task.FromResult(true);
    public Task RegistrarCargaImagenAsync(string nombreOriginal, string nombreGuardado, string url, string tipoMime, long tamanioBytes, long usuarioId, string? ip) => Task.CompletedTask;
}

internal sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ApplicationName { get; set; } = "RL.API";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = "Development";
}

internal sealed class FakeLogoDownloader : IInstitutionalLogoDownloader
{
    private readonly InstitutionalDownloadedLogoResult _result;

    public FakeLogoDownloader(InstitutionalDownloadedLogoResult result) => _result = result;

    public Task<InstitutionalDownloadedLogoResult> DownloadLogoAsync(string url, CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);

    public Task<InstitutionalDownloadedLogoResult> DownloadFaviconAsync(string url, CancellationToken cancellationToken = default) =>
        Task.FromResult(_result);
}
