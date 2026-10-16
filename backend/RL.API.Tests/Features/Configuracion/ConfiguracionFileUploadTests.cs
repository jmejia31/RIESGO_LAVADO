using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RL.API.Features.Configuracion;
using RL.API.Features.Configuracion.Application;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.Configuracion;

public sealed class ConfiguracionFileUploadTests
{
    [Fact]
    public async Task SubirImagenRechazaMimetypeSpoofedAntesDeGuardar()
    {
        var service = InterfaceStub.Create<IConfiguracionService>(out InterfaceStub serviceStub);
        var controller = CreateController(service);
        var content = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));
        var file = new FormFile(content, 0, content.Length, "archivo", "login.png")
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "image/png" }
        };

        IActionResult result = await controller.SubirImagen(file);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(serviceStub.CallsTo(nameof(IConfiguracionService.RegistrarCargaImagenAsync)));
    }

    [Theory]
    [InlineData("fake.png", "image/png", "not a PNG")]
    [InlineData("fake.jpg", "image/jpeg", "not a JPEG")]
    [InlineData("unsupported.bmp", "image/bmp", "BM-fake")]
    public async Task SubirImagenRechazaFormatoFalsoOExtensionNoPermitida(string name, string mime, string payload)
    {
        var service = InterfaceStub.Create<IConfiguracionService>(out InterfaceStub serviceStub);
        var controller = CreateController(service);
        var bytes = Encoding.UTF8.GetBytes(payload);
        var file = CreateFile(name, mime, bytes);

        IActionResult result = await controller.SubirImagen(file);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(serviceStub.CallsTo(nameof(IConfiguracionService.RegistrarCargaImagenAsync)));
    }

    [Fact]
    public async Task SubirImagenRechazaArchivoMayorAlLimiteAntesDeGuardar()
    {
        var service = InterfaceStub.Create<IConfiguracionService>(out InterfaceStub serviceStub);
        var controller = CreateController(service);
        var bytes = new byte[10 * 1024 * 1024 + 1];
        var file = CreateFile("oversized.png", "image/png", bytes);

        IActionResult result = await controller.SubirImagen(file);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(serviceStub.CallsTo(nameof(IConfiguracionService.RegistrarCargaImagenAsync)));
    }

    [Theory]
    [InlineData("../escape.png")]
    [InlineData("..\\escape.png")]
    public async Task SubirImagenRechazaNombresConTraversal(string name)
    {
        var service = InterfaceStub.Create<IConfiguracionService>(out InterfaceStub serviceStub);
        var controller = CreateController(service);
        var file = CreateFile(name, "image/png", [0x89, 0x50, 0x4E, 0x47]);

        IActionResult result = await controller.SubirImagen(file);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(serviceStub.CallsTo(nameof(IConfiguracionService.RegistrarCargaImagenAsync)));
    }

    [Fact]
    public async Task SubirImagenRechazaArchivoVacio()
    {
        var service = InterfaceStub.Create<IConfiguracionService>(out InterfaceStub serviceStub);
        var controller = CreateController(service);
        var file = CreateFile("empty.png", "image/png", []);

        IActionResult result = await controller.SubirImagen(file);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(serviceStub.CallsTo(nameof(IConfiguracionService.RegistrarCargaImagenAsync)));
    }

    private static ConfiguracionController CreateController(IConfiguracionService service)
    {
        var controller = new ConfiguracionController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "17")], "test"))
                }
            }
        };

        return controller;
    }

    private static FormFile CreateFile(string name, string mime, byte[] bytes) =>
        new(new MemoryStream(bytes), 0, bytes.Length, "archivo", name)
        {
            Headers = new HeaderDictionary { ["Content-Type"] = mime }
        };
}
