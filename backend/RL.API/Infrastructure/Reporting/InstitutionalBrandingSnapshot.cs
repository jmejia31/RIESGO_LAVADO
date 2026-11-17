using System.Security.Cryptography;

namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Identidad visual inmutable resuelta una sola vez por documento.
/// El snapshot evita consultas por página y garantiza que PDF, Excel y preview
/// trabajen con el mismo logo y los mismos colores.
/// </summary>
public sealed record InstitutionalBrandingSnapshot(
    string InstitutionName,
    string SystemName,
    string LogoUrl,
    string LogoSha256,
    byte[] LogoPngBytes,
    byte[] LogoRgb,
    int LogoWidth,
    int LogoHeight,
    InstitutionalReportPalette Palette,
    string Source)
{
    public bool HasLogo => LogoPngBytes.Length > 0 && LogoRgb.Length > 0 && LogoWidth > 0 && LogoHeight > 0;
    public decimal LogoAspectRatio => HasLogo ? LogoWidth / (decimal)LogoHeight : 1m;

    public static InstitutionalBrandingSnapshot Canonical { get; } = CreateCanonical();

    public static InstitutionalBrandingSnapshot FromImage(
        byte[] bytes,
        string institutionName,
        string systemName,
        string logoUrl,
        string headerColor,
        string source)
    {
        if (bytes.Length == 0) throw new InvalidDataException("El logo institucional está vacío.");
        var decoded = InstitutionalPngDecoder.DecodePngForBranding(bytes);
        if (decoded.Width is < 32 or > 4096 || decoded.Height is < 16 or > 4096)
            throw new InvalidDataException("Las dimensiones del logo institucional no están permitidas.");
        if ((long)decoded.Width * decoded.Height > 16_777_216)
            throw new InvalidDataException("El logo institucional supera el límite de píxeles permitido.");
        var palette = InstitutionalReportStandard.CreatePalette(headerColor);
        return new InstitutionalBrandingSnapshot(
            string.IsNullOrWhiteSpace(institutionName) ? InstitutionalReportStandard.InstitutionName : institutionName.Trim(),
            string.IsNullOrWhiteSpace(systemName) ? InstitutionalReportStandard.SystemName : systemName.Trim(),
            logoUrl,
            Convert.ToHexString(SHA256.HashData(bytes)),
            bytes.ToArray(),
            decoded.Rgb,
            decoded.Width,
            decoded.Height,
            palette,
            source);
    }

    private static InstitutionalBrandingSnapshot CreateCanonical() => new(
        InstitutionalReportStandard.InstitutionName,
        InstitutionalReportStandard.SystemName,
        string.Empty,
        string.Empty,
        Array.Empty<byte>(),
        Array.Empty<byte>(),
        0,
        0,
        InstitutionalReportStandard.Palette,
        "not-configured");

}

public interface IInstitutionalBrandingResolver
{
    InstitutionalBrandingSnapshot Resolve();
}
