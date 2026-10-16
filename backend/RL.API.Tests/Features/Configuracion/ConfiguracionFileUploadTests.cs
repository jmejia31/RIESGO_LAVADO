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
}
