using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RL.API.Features.Listas.Application;
using RL.API.Features.Listas.Persistence;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.Listas;

public sealed class EvidenciasServiceFilePolicyTests
{
    [Fact]
    public void ValidarArchivosRechazaMimetypeSpoofing()
    {
        var repository = InterfaceStub.Create<IListasRepository>(out _);
        var environment = InterfaceStub.Create<IWebHostEnvironment>(out _);
        var service = new EvidenciasService(repository, new ConfigurationBuilder().Build(), environment);
        var bytes = Encoding.UTF8.GetBytes("not a pdf");
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "archivo", "evidence.pdf")
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "application/pdf" }
        };

        Assert.NotNull(service.ValidarArchivos([file]));
    }

    [Fact]
    public void ValidarArchivosAceptaPdfConFirmaCorrecta()
    {
        var repository = InterfaceStub.Create<IListasRepository>(out _);
        var environment = InterfaceStub.Create<IWebHostEnvironment>(out _);
        var service = new EvidenciasService(repository, new ConfigurationBuilder().Build(), environment);
        var bytes = "%PDF-1.4\n%%EOF"u8.ToArray();
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "archivo", "evidence.pdf")
        {
            Headers = new HeaderDictionary { ["Content-Type"] = "application/pdf" }
        };

        Assert.Null(service.ValidarArchivos([file]));
    }
}
