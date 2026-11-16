using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;
using RL.API.Infrastructure.Reporting;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class InstitutionalBrandingResolverTests
{
    [Fact]
    public void Resolver_UsaElArchivoLocalPublicadoEnElMismoStorageDeUploads()
    {
        var root = Path.Combine(Path.GetTempPath(), $"b06-branding-{Guid.NewGuid():N}");
        var uploads = Path.Combine(root, "uploads");
        Directory.CreateDirectory(uploads);
        var fileName = "synthetic-logo.png";
        var filePath = Path.Combine(uploads, fileName);
        File.WriteAllBytes(filePath, InstitutionalLogoAsset.PngBytes);

        try
        {
            var configuration = InterfaceStub.Create<IConfiguracionService>(out var configurationStub);
            configurationStub.On(nameof(IConfiguracionService.ObtenerConfigSistemaAsync), _ => Task.FromResult<ConfigSistema?>(new ConfigSistema
            {
                NombreInstitucion = "Institución sintética",
                NombreSistema = "Sistema sintético",
                LogoUrl = InstitutionalBrandingStorage.BuildPublicUploadUrl(fileName),
                ColorEncabezadoReportes = "#123B63"
            }));

            var environment = InterfaceStub.Create<IWebHostEnvironment>(out var environmentStub);
            environmentStub.On("get_WebRootPath", _ => root);
            environmentStub.On("get_ContentRootPath", _ => root);

            var snapshot = new InstitutionalBrandingResolver(configuration, environment).Resolve();

            Assert.Equal("configured-local-file", snapshot.Source);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(InstitutionalLogoAsset.PngBytes)), snapshot.LogoSha256);
            Assert.Equal(InstitutionalBrandingStorage.BuildPublicUploadUrl(fileName), snapshot.LogoUrl);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Resolver_BloqueaUrlRemotaEnLugarDeOcultarlaConElLogoCanonico()
    {
        var configuration = InterfaceStub.Create<IConfiguracionService>(out var configurationStub);
        configurationStub.On(nameof(IConfiguracionService.ObtenerConfigSistemaAsync), _ => Task.FromResult<ConfigSistema?>(new ConfigSistema
        {
            NombreInstitucion = "Institución sintética",
            NombreSistema = "Sistema sintético",
            LogoUrl = "https://example.invalid/logo.png",
            ColorEncabezadoReportes = "#123B63"
        }));

        var environment = InterfaceStub.Create<IWebHostEnvironment>(out _);

        var exception = Assert.Throws<InstitutionalBrandingUnavailableException>(
            () => new InstitutionalBrandingResolver(configuration, environment).Resolve());

        Assert.Equal("B06_BRANDING_LOGO_NOT_READY", exception.Code);
    }
}
