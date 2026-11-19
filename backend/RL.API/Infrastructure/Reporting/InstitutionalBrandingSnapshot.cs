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
    string Source,
    string LogoMimeType = "image/png",
    byte[]? LogoRawBytes = null)
{
    public bool HasLogo => LogoWidth > 0 && LogoHeight > 0 && (LogoRgb.Length > 0 || IsJpeg);
    public decimal LogoAspectRatio => HasLogo ? LogoWidth / (decimal)LogoHeight : 1m;
    public bool IsJpeg => string.Equals(LogoMimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase);
    public bool IsPng => !IsJpeg && HasLogo;
    public byte[] LogoEffectiveBytes => (LogoRawBytes != null && LogoRawBytes.Length > 0) ? LogoRawBytes : LogoPngBytes;

    public static InstitutionalBrandingSnapshot Canonical { get; } = CreateCanonical();

    public static InstitutionalBrandingSnapshot FromImage(
        byte[] bytes,
        string? institutionName,
        string? systemName,
        string logoUrl,
        string headerColor,
        string source)
    {
        if (bytes == null || bytes.Length == 0) throw new InvalidDataException("El logo institucional está vacío.");

        var isPng = bytes.Length >= 8 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71;
        var isJpeg = bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

        if (!isPng && !isJpeg)
            throw new InvalidOperationException("La imagen configurada no es un PNG ni un JPEG válido.");

        int width, height;
        byte[] rgb;
        string mimeType;

        if (isPng)
        {
            var decoded = InstitutionalPngDecoder.DecodePngForBranding(bytes);
            width = decoded.Width;
            height = decoded.Height;
            rgb = decoded.Rgb;
            mimeType = "image/png";
        }
        else
        {
            var dims = InstitutionalJpegInspector.InspectJpeg(bytes);
            width = dims.Width;
            height = dims.Height;
            rgb = Array.Empty<byte>();
            mimeType = "image/jpeg";
        }

        if (width is < 32 or > 4096 || height is < 16 or > 4096)
            throw new InvalidDataException("Las dimensiones del logo institucional no están permitidas.");
        if ((long)width * height > 16_777_216)
            throw new InvalidDataException("El logo institucional supera el límite de píxeles permitido.");

        var palette = InstitutionalReportStandard.CreatePalette(headerColor);
        return new InstitutionalBrandingSnapshot(
            string.IsNullOrWhiteSpace(institutionName) ? InstitutionalReportStandard.InstitutionName : institutionName.Trim(),
            string.IsNullOrWhiteSpace(systemName) ? InstitutionalReportStandard.SystemName : systemName.Trim(),
            logoUrl,
            Convert.ToHexString(SHA256.HashData(bytes)),
            bytes.ToArray(),
            rgb,
            width,
            height,
            palette,
            source,
            mimeType,
            bytes.ToArray());
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
        "not-configured",
        "image/png",
        Array.Empty<byte>());
}

public interface IInstitutionalBrandingResolver
{
    Task<InstitutionalBrandingSnapshot> ResolveAsync(CancellationToken cancellationToken = default);
}
