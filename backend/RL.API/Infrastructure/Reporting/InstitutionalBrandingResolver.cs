using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using RL.API.Features.Configuracion.Application;
using RL.API.Features.Configuracion.Contracts;

namespace RL.API.Infrastructure.Reporting;

public sealed class InstitutionalBrandingResolver : IInstitutionalBrandingResolver
{
    private static readonly ConcurrentDictionary<string, InstitutionalBrandingSnapshot> Cache = new(StringComparer.OrdinalIgnoreCase);

    private readonly IConfiguracionService _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IInstitutionalLogoDownloader _logoDownloader;
    private readonly ILogger<InstitutionalBrandingResolver>? _logger;

    public InstitutionalBrandingResolver(
        IConfiguracionService configuration,
        IWebHostEnvironment environment,
        IInstitutionalLogoDownloader? logoDownloader = null,
        ILogger<InstitutionalBrandingResolver>? logger = null)
    {
        _configuration = configuration;
        _environment = environment;
        _logoDownloader = logoDownloader ?? new InstitutionalLogoDownloader(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        _logger = logger;
    }

    public static void ClearCache() => Cache.Clear();

    public async Task<InstitutionalBrandingSnapshot> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var configuration = await _configuration.ObtenerConfigSistemaAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (configuration is null) return InstitutionalBrandingSnapshot.Canonical;

        var header = string.IsNullOrWhiteSpace(configuration.ColorEncabezadoReportes)
            ? InstitutionalReportStandard.Palette.Navy
            : configuration.ColorEncabezadoReportes;

        if (string.IsNullOrWhiteSpace(configuration.LogoUrl))
        {
            return InstitutionalBrandingSnapshot.Canonical with
            {
                InstitutionName = configuration.NombreInstitucion,
                SystemName = configuration.NombreSistema,
                Palette = InstitutionalReportStandard.CreatePalette(header),
                Source = "not-configured"
            };
        }

        var logoUrl = configuration.LogoUrl.Trim();
        var cacheKey = $"{logoUrl}|{header}|{configuration.NombreInstitucion?.Trim()}|{configuration.NombreSistema?.Trim()}";

        if (Cache.TryGetValue(cacheKey, out var cachedSnapshot))
            return cachedSnapshot;

        // Caso 1: URL HTTPS remota
        if (logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            var downloadResult = await _logoDownloader.DownloadLogoAsync(logoUrl, cancellationToken).ConfigureAwait(false);
            if (!downloadResult.Success || downloadResult.Bytes is null || downloadResult.Bytes.Length == 0)
            {
                return WithoutLogo(configuration, header, $"configured-logo-remote-failed: {downloadResult.FailureReason}");
            }

            try
            {
                var snapshot = InstitutionalBrandingSnapshot.FromImage(
                    downloadResult.Bytes,
                    configuration.NombreInstitucion,
                    configuration.NombreSistema,
                    logoUrl,
                    header,
                    "configured-https-url");

                Cache[cacheKey] = snapshot;
                return snapshot;
            }
            catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException)
            {
                return WithoutLogo(configuration, header, "configured-logo-invalid", ex);
            }
        }

        // Caso 2: Archivo local publicado en wwwroot/uploads/
        var path = InstitutionalBrandingStorage.ResolveLocalAsset(_environment, logoUrl);
        if (path is null)
            return WithoutLogo(configuration, header, "configured-logo-not-local");

        try
        {
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            var snapshot = InstitutionalBrandingSnapshot.FromImage(
                bytes,
                configuration.NombreInstitucion,
                configuration.NombreSistema,
                logoUrl,
                header,
                "configured-local-file");

            Cache[cacheKey] = snapshot;
            return snapshot;
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
