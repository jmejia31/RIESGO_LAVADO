using Microsoft.AspNetCore.Hosting;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;

namespace RL.API.Infrastructure.Reporting;

public sealed class InstitutionalBrandingResolver : IInstitutionalBrandingResolver
{
    private readonly IConfiguracionService _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<InstitutionalBrandingResolver>? _logger;

    public InstitutionalBrandingResolver(
        IConfiguracionService configuration,
        IWebHostEnvironment environment,
        ILogger<InstitutionalBrandingResolver>? logger = null)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<InstitutionalBrandingSnapshot> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await _configuration.ObtenerConfigSistemaAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
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
                Source = "not-configured"
            };

        var path = InstitutionalBrandingStorage.ResolveLocalAsset(_environment, configuration.LogoUrl);
        if (path is null)
            return WithoutLogo(configuration, header, "configured-logo-not-local");

        try
        {
            return InstitutionalBrandingSnapshot.FromImage(
                await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false),
                configuration.NombreInstitucion,
                configuration.NombreSistema,
                configuration.LogoUrl!,
                header,
                "configured-local-file");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            return WithoutLogo(configuration, header, "configured-logo-invalid", exception);
        }
    }

    private InstitutionalBrandingSnapshot WithoutLogo(
        ConfigSistema configuration,
        string header,
        string reason,
        Exception? exception = null)
    {
        _logger?.LogWarning(
            exception,
            "B06: se omitirá el logo del reporte; el logo configurado no está disponible. Motivo: {Reason}",
            reason);

        return InstitutionalBrandingSnapshot.Canonical with
        {
            InstitutionName = string.IsNullOrWhiteSpace(configuration.NombreInstitucion)
                ? InstitutionalReportStandard.InstitutionName
                : configuration.NombreInstitucion.Trim(),
            SystemName = string.IsNullOrWhiteSpace(configuration.NombreSistema)
                ? InstitutionalReportStandard.SystemName
                : configuration.NombreSistema.Trim(),
            Palette = InstitutionalReportStandard.CreatePalette(header),
            Source = $"configured-no-logo-{reason}"
        };
    }
}
