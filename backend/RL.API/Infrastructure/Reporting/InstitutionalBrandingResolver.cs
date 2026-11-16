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
        var path = ResolveLocalLogo(configuration.LogoUrl);
        if (path is null) return CreateMetadataOnly(configuration, header, "canonical-fallback");

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
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return CreateMetadataOnly(configuration, header, "canonical-fallback-invalid-logo");
        }
    }

    private InstitutionalBrandingSnapshot CreateMetadataOnly(
        RL.API.Features.Configuracion.Contracts.ConfigSistema configuration,
        string header,
        string source) => InstitutionalBrandingSnapshot.FromImage(
            InstitutionalLogoAsset.PngBytes,
            configuration.NombreInstitucion,
            configuration.NombreSistema,
            configuration.LogoUrl ?? InstitutionalBrandingSnapshot.Canonical.LogoUrl,
            header,
            source);

    private string? ResolveLocalLogo(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl)
            || logoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || logoUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return null;

        var relative = logoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "wwwroot"));
        var candidate = Path.GetFullPath(Path.Combine(root, relative));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate)
            ? candidate
            : null;
    }
}
