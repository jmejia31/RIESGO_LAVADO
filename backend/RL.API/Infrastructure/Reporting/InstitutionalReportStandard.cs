namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Fuente única de reglas visuales y de paginación para reportes PDF y Excel del SGRLA-IHSS.
/// Los módulos aportan sus datos; esta clase define identidad, formatos y restricciones comunes.
/// </summary>
public static class InstitutionalReportStandard
{
    public const string InstitutionName = "INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL";
    public const string SystemName = "SGRLA-IHSS";
    public const string DateFormat = "dd/MM/yyyy";
    public const string DateTimeFormat = "dd/MM/yyyy HH:mm";

    public static InstitutionalReportPalette Palette { get; } = new(
        Navy: "#123B63",
        NavyDark: "#0B2E4F",
        HeaderText: "#FFFFFF",
        BodyText: "#1F2937",
        AlternateRow: "#F3F6F9",
        Border: "#D8E0E8",
        Success: "#0F766E",
        Warning: "#D97706",
        Danger: "#B91C1C",
        Muted: "#64748B");

    public static InstitutionalReportOrientation ResolveOrientation(int visibleColumns) =>
        visibleColumns > 8 ? InstitutionalReportOrientation.Landscape : InstitutionalReportOrientation.Portrait;

    public static bool MustMoveRowToNextPage(decimal currentY, decimal rowHeight, decimal footerStartY) =>
        rowHeight > 0 && currentY + rowHeight > footerStartY;

    public static string PageLabel(int page, int totalPages) =>
        totalPages > 0 ? $"Página {page} de {totalPages}" : $"Página {page}";

    public static void ValidateMetadata(InstitutionalReportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (string.IsNullOrWhiteSpace(metadata.Title))
            throw new ArgumentException("El título del reporte es obligatorio.", nameof(metadata));
        if (string.IsNullOrWhiteSpace(metadata.ModuleName))
            throw new ArgumentException("El módulo del reporte es obligatorio.", nameof(metadata));
    }
}

public sealed record InstitutionalReportPalette(
    string Navy,
    string NavyDark,
    string HeaderText,
    string BodyText,
    string AlternateRow,
    string Border,
    string Success,
    string Warning,
    string Danger,
    string Muted);

public sealed record InstitutionalReportMetadata(
    string Title,
    string ModuleName,
    DateTime GeneratedAt,
    string? Subtitle = null,
    string? GeneratedBy = null);

public sealed record InstitutionalTablePolicy(
    bool KeepRowsTogether = true,
    bool RepeatHeaderOnEveryPage = true,
    bool KeepSectionTitleWithFirstRow = true,
    bool PreventDateWrapping = true,
    bool AlternateRows = true);

public enum InstitutionalReportOrientation
{
    Portrait,
    Landscape
}
