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
    // Incremento uniforme del 30% sobre el estándar base de 2.2 pulgadas.
    // Equivalente a 72.644 mm, usado también por el generador Angular.
    public const decimal PdfLogoWidthPoints = 205.92m;
    // Caja máxima compartida: el logo conserva su proporción y nunca invade
    // el texto ni el primer bloque del reporte, aunque sea vertical o cuadrado.
    public const decimal PdfLogoMaxHeightPoints = 90.72m;
    public const int ExcelLogoWidthEmu = 1_485_900;
    public const int ExcelLogoMaxHeightEmu = 540_000;

    public const string SectionTitleColor = "#000000";
    public const string FilterTitleColor = "#000000";
    public const string EvaluationTitleColor = "#000000";
    public const string BodyTextColor = "#000000";
    public const string KpiValueColor = "#000000";

    public static InstitutionalReportPalette Palette { get; } = new(
        Navy: "#123B63",
        NavyDark: "#0B2E4F",
        HeaderText: "#FFFFFF",
        BodyText: "#000000",
        AlternateRow: "#F3F6F9",
        Border: "#D8E0E8",
        Success: "#0F766E",
        Warning: "#D97706",
        Danger: "#B91C1C",
        Muted: "#64748B");

    public static InstitutionalReportPalette CreatePalette(string? headerColor)
    {
        var navy = IsHexColor(headerColor) ? headerColor!.ToUpperInvariant() : Palette.Navy;
        var foreground = ResolveAccessibleForeground(navy);
        var metadata = ContrastRatio(navy, "#DCE6F0") >= 4.5m ? "#DCE6F0" : ResolveAccessibleForeground(navy);
        return Palette with { Navy = navy, HeaderText = foreground, HeaderMetadataText = metadata };
    }

    public static string ResolveAccessibleForeground(string background) =>
        ContrastRatio(background, "#FFFFFF") >= ContrastRatio(background, "#000000")
            ? "#FFFFFF"
            : "#000000";

    private static bool IsHexColor(string? value) => !string.IsNullOrWhiteSpace(value)
        && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static decimal ContrastRatio(string left, string right)
    {
        static decimal Channel(int value)
        {
            var normalized = value / 255m;
            return normalized <= 0.03928m ? normalized / 12.92m : (decimal)Math.Pow((double)((normalized + 0.055m) / 1.055m), 2.4);
        }
        static decimal Luminance(string color)
        {
            var rgb = Enumerable.Range(0, 3).Select(index => Convert.ToInt32(color.Substring(1 + index * 2, 2), 16)).ToArray();
            return Channel(rgb[0]) * 0.2126m + Channel(rgb[1]) * 0.7152m + Channel(rgb[2]) * 0.0722m;
        }
        var a = Luminance(left); var b = Luminance(right);
        return (Math.Max(a, b) + 0.05m) / (Math.Min(a, b) + 0.05m);
    }

    public static InstitutionalReportOrientation ResolveOrientation(int visibleColumns) =>
        visibleColumns > 8 ? InstitutionalReportOrientation.Landscape : InstitutionalReportOrientation.Portrait;

    public static InstitutionalKeyValueLayout ResolveKeyValueLayout(
        int fieldCount,
        InstitutionalReportOrientation orientation)
    {
        var columns = orientation == InstitutionalReportOrientation.Landscape ? 3 : 2;
        if (fieldCount <= 2) columns = 1;
        return new InstitutionalKeyValueLayout(orientation, columns);
    }

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
    string Muted)
{
    public string HeaderMetadataText { get; init; } = "#DCE6F0";
    public string SectionTitle { get; init; } = "#000000";
    public string FilterTitle { get; init; } = "#000000";
    public string KpiValue { get; init; } = "#000000";
}

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

public sealed record InstitutionalKeyValueLayout(
    InstitutionalReportOrientation Orientation,
    int Columns);

public enum InstitutionalReportOrientation
{
    Portrait,
    Landscape
}
