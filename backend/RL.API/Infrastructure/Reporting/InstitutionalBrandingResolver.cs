using Microsoft.AspNetCore.Hosting;
using RL.API.Features.Configuracion.Application;

namespace RL.API.Infrastructure.Reporting;

public sealed class InstitutionalBrandingResolver : IInstitutionalBrandingResolver
{
    private readonly IConfiguracionService _configuration;
    private readonly IWebHostEnvironment _environment;

    public InstitutionalBrandingResolver(IConfiguracionService configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public InstitutionalBrandingSnapshot Resolve()
    {
        var configuration = _configuration.ObtenerConfigSistemaAsync().GetAwaiter().GetResult();
        if (configuration is null) return InstitutionalBrandingSnapshot.Canonical;

        var header = string.IsNullOrWhiteSpace(configuration.ColorEncabezadoReportes)
            ? InstitutionalReportStandard.Palette.Navy
            : configuration.ColorEncabezadoReportes;
        if (string.IsNullOrWhiteSpace(configuration.LogoUrl))
            return InstitutionalBrandingSnapshot.Canonical with
            {
                InstitutionName = configuration.NombreInstitucion,
                SystemName = configuration.NombreSistema,
                Palette = InstitutionalReportStandard.CreatePalette(header),
                Source = "canonical-default"
            };

        var path = InstitutionalBrandingStorage.ResolveLocalAsset(_environment, configuration.LogoUrl);
        if (path is null)
            throw new InstitutionalBrandingUnavailableException(
                "B06_BRANDING_LOGO_NOT_READY",
                "El logotipo configurado no es un archivo local accesible. Use la carga institucional y guarde la configuración antes de generar reportes.");

        try
        {
            return InstitutionalBrandingSnapshot.FromImage(
                File.ReadAllBytes(path),
                configuration.NombreInstitucion,
                configuration.NombreSistema,
                configuration.LogoUrl!,
                header,
                "configured-local-file");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            throw new InstitutionalBrandingUnavailableException(
                "B06_BRANDING_LOGO_INVALID",
                "El archivo de logotipo configurado no puede utilizarse en reportería institucional.",
                exception);
        }
    }
}
