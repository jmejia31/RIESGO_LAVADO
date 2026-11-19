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

    private static byte[] CrearIcoValido()
    {
        var bytes = new byte[22 + 16];
        bytes[0] = 0; bytes[1] = 0; bytes[2] = 1; bytes[3] = 0;
        bytes[4] = 1; bytes[5] = 0; // count = 1
        bytes[6] = 32; bytes[7] = 32; // 32x32
        return bytes;
    }

    private static byte[] CrearPngValido(int width = 32, int height = 32)
    {
        var bytes = new byte[33];
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
        bytes[4] = 0x0D; bytes[5] = 0x0A; bytes[6] = 0x1A; bytes[7] = 0x0A;
        bytes[12] = (byte)'I'; bytes[13] = (byte)'H'; bytes[14] = (byte)'D'; bytes[15] = (byte)'R';
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16, 4), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20, 4), height);
        return bytes;
    }

    private static byte[] CrearJpegValido(int width = 32, int height = 32)
    {
        var stream = new MemoryStream();
        stream.Write([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00]);
        stream.Write([0xFF, 0xC0, 0x00, 0x11, 0x08]);
        stream.WriteByte((byte)(height >> 8));
        stream.WriteByte((byte)(height & 0xFF));
        stream.WriteByte((byte)(width >> 8));
        stream.WriteByte((byte)(width & 0xFF));
        stream.Write([0x03, 0x01, 0x11, 0x00, 0x02, 0x11, 0x01, 0x03, 0x11, 0x01]);
        stream.Write([0xFF, 0xD9]);
        return stream.ToArray();
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
    [InlineData("/uploads/../../windows/system32/cmd.exe")]
    [InlineData("..\\favicon.ico")]
    [InlineData("c:/secret.ico")]
    public async Task Favicon_PathTraversal_RechazaConInvalidPath(string pathTraversal)
    {
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = pathTraversal });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("invalid-path", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Fact]
    public async Task Favicon_ArchivoLocalNoExiste_RetornaNotFound()
    {
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/inexistente.ico" });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("not-found", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Fact]
    public async Task Favicon_ArchivoLocalIcoValido_RetornaArchivoConMimeXIcon()
    {
        var icoPath = Path.Combine(_tempWebRoot, "uploads", "favicon.ico");
        await File.WriteAllBytesAsync(icoPath, CrearIcoValido());

        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/favicon.ico" });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/x-icon", fileResult.ContentType);
        Assert.Equal("available", controller.Response.Headers["X-Favicon-Status"].ToString());
        Assert.Equal("configured-local-file", controller.Response.Headers["X-Favicon-Source"].ToString());
    }

    [Fact]
    public async Task Favicon_ArchivoLocalPngValido_RetornaArchivoConMimePng()
    {
        var pngPath = Path.Combine(_tempWebRoot, "uploads", "favicon.png");
        await File.WriteAllBytesAsync(pngPath, CrearPngValido(32, 32));

        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/favicon.png" });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal("available", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Fact]
    public async Task Favicon_ArchivoLocalJpegValido_RetornaArchivoConMimeJpeg()
    {
        var jpegPath = Path.Combine(_tempWebRoot, "uploads", "favicon.jpg");
        await File.WriteAllBytesAsync(jpegPath, CrearJpegValido(64, 64));

        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/favicon.jpg" });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/jpeg", fileResult.ContentType);
        Assert.Equal("available", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Fact]
    public async Task Favicon_ArchivoLocalCorrupto_RetornaInvalidContent()
    {
        var badPath = Path.Combine(_tempWebRoot, "uploads", "corrupto.ico");
        await File.WriteAllBytesAsync(badPath, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/corrupto.ico" });
        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon();

        Assert.IsType<NoContentResult>(result);
        Assert.Contains("El archivo no contiene un formato de imagen de favicon admitido", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Fact]
    public async Task Favicon_HttpsDescargaValida_RetornaArchivoConMime()
    {
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "https://www.ihss.hn/favicon.png" });
        var downloader = new FakeLogoDownloader
        {
            OnDownloadFavicon = _ => Task.FromResult(new InstitutionalDownloadedLogoResult(true, CrearPngValido(32, 32), "image/png", null, 32, 32))
        };

        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon(downloader);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal("available", controller.Response.Headers["X-Favicon-Status"].ToString());
        Assert.Equal("configured-https-url", controller.Response.Headers["X-Favicon-Source"].ToString());
    }

    [Fact]
    public async Task Favicon_HttpsDescargaFalla_RetornaMotivoDeFalla()
    {
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "https://malicioso.com/favicon.ico" });
        var downloader = new FakeLogoDownloader
        {
            OnDownloadFavicon = _ => Task.FromResult(new InstitutionalDownloadedLogoResult(false, null, null, "unauthorized-domain"))
        };

        var controller = new ConfiguracionController(service, _fakeEnv)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Favicon(downloader);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("unauthorized-domain", controller.Response.Headers["X-Favicon-Status"].ToString());
    }

    [Fact]
    public async Task EstadoFavicon_SinIcono_RetornaSinFavicon()
    {
        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = null });
        var controller = new ConfiguracionController(service, _fakeEnv);

        var result = Assert.IsType<OkObjectResult>(await controller.EstadoFavicon());
        var json = JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"configurado\":false", json);
        Assert.Contains("\"tipo\":\"SIN_FAVICON\"", json);
        Assert.Contains("\"estado\":\"not-configured\"", json);
    }

    [Fact]
    public async Task EstadoFavicon_ConIconoLocalValido_RetornaDisponible()
    {
        var icoPath = Path.Combine(_tempWebRoot, "uploads", "favicon.ico");
        await File.WriteAllBytesAsync(icoPath, CrearIcoValido());

        var service = new FakeConfiguracionService(new ConfigSistema { IconoUrl = "/uploads/favicon.ico" });
        var controller = new ConfiguracionController(service, _fakeEnv);

        var result = Assert.IsType<OkObjectResult>(await controller.EstadoFavicon());
        var json = JsonSerializer.Serialize(result.Value);
        Assert.Contains("\"configurado\":true", json);
        Assert.Contains("\"disponible\":true", json);
        Assert.Contains("\"tipo\":\"FAVICON_LOCAL\"", json);
        Assert.Contains("\"mimeType\":\"image/x-icon\"", json);
    }

    [Fact]
    public void InstitutionalFaviconValidator_ValidacionesDeFormato()
    {
        var ico = InstitutionalFaviconValidator.Validate(CrearIcoValido());
        Assert.True(ico.IsValid);
        Assert.Equal("image/x-icon", ico.MimeType);

        var png = InstitutionalFaviconValidator.Validate(CrearPngValido(48, 48));
        Assert.True(png.IsValid);
        Assert.Equal("image/png", png.MimeType);

        var jpeg = InstitutionalFaviconValidator.Validate(CrearJpegValido(64, 64));
        Assert.True(jpeg.IsValid);
        Assert.Equal("image/jpeg", jpeg.MimeType);

        var invalid = InstitutionalFaviconValidator.Validate(new byte[] { 0x12, 0x34 });
        Assert.False(invalid.IsValid);
        Assert.Contains("El archivo no contiene un formato de imagen de favicon admitido", invalid.FailureReason);

        var empty = InstitutionalFaviconValidator.Validate(Array.Empty<byte>());
        Assert.False(empty.IsValid);
    }

    private sealed class FakeConfiguracionService : IConfiguracionService
    {
        private readonly ConfigSistema _config;

        public FakeConfiguracionService(ConfigSistema config) => _config = config;

        public Task<ConfigSistema?> ObtenerConfigSistemaAsync() => Task.FromResult<ConfigSistema?>(_config);
        public Task<List<LoginSlide>> ObtenerSlidesAsync() => Task.FromResult(new List<LoginSlide>());
        public Task<List<LoginSlide>> ObtenerTodosSlidesAsync() => Task.FromResult(new List<LoginSlide>());
        public Task<bool> GuardarConfigSistemaAsync(ConfigSistema config, long usuarioId, string? ip) => Task.FromResult(true);
        public Task<bool> CrearSlideAsync(LoginSlide slide, long usuarioId, string? ip) => Task.FromResult(true);
        public Task<bool> ActualizarSlideAsync(int id, LoginSlide slide, long usuarioId, string? ip) => Task.FromResult(true);
        public Task<bool> EliminarSlideAsync(int id, long usuarioId, string? ip) => Task.FromResult(true);
        public Task RegistrarCargaImagenAsync(string nombreOriginal, string nombreGuardado, string url, string tipoMime, long tamanioBytes, long usuarioId, string? ip) => Task.CompletedTask;
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "RL.API";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
    }

    private sealed class FakeLogoDownloader : IInstitutionalLogoDownloader
    {
        public Func<string, Task<InstitutionalDownloadedLogoResult>>? OnDownloadFavicon { get; init; }
        public Func<string, Task<InstitutionalDownloadedLogoResult>>? OnDownloadLogo { get; init; }

        public Task<InstitutionalDownloadedLogoResult> DownloadLogoAsync(string url, CancellationToken cancellationToken = default) =>
            OnDownloadLogo?.Invoke(url) ?? Task.FromResult(new InstitutionalDownloadedLogoResult(false, null, null, "not-implemented"));

        public Task<InstitutionalDownloadedLogoResult> DownloadFaviconAsync(string url, CancellationToken cancellationToken = default) =>
            OnDownloadFavicon?.Invoke(url) ?? Task.FromResult(new InstitutionalDownloadedLogoResult(false, null, null, "not-implemented"));
    }
}
