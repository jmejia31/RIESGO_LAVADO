using System.Globalization;
using System.Text;

namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Generador PDF institucional sin dependencias externas. Mantiene encabezados,
/// pies, títulos y filas completas en una sola página.
/// </summary>
public sealed class InstitutionalPdfDocument
{
    private const decimal MarginLeft = 36m;
    private const decimal MarginRight = 36m;
    private const decimal HeaderHeight = 64m;
    private const decimal FooterHeight = 30m;
    private const decimal BodyTop = 82m;
    private const decimal DefaultFontSize = 8.5m;
    private const decimal DefaultLineHeight = 11m;

    private readonly decimal _pageWidth;
    private readonly decimal _pageHeight;
    private readonly string _title;
    private readonly string _module;
    private readonly DateTime _generatedAt;
    private readonly List<PdfPage> _pages = new();
    private PdfPage _current = null!;

    public InstitutionalPdfDocument(
        string title,
        string module,
        InstitutionalReportOrientation orientation,
        DateTime? generatedAt = null)
    {
        _title = Sanitizar(title);
        _module = Sanitizar(module);
        _generatedAt = generatedAt ?? DateTime.Now;
        (_pageWidth, _pageHeight) = orientation == InstitutionalReportOrientation.Landscape
            ? (841.89m, 595.28m)
            : (595.28m, 841.89m);
        NuevaPagina();
    }

    public void AddSection(string title)
    {
        const decimal topSpacing = 12m;
        const decimal bottomSpacing = 10m;
        var requiereSeparacion = _current.CursorY > BodyTop + 0.5m;
        AsegurarEspacio((requiereSeparacion ? topSpacing : 0m) + 30m);

        // Si AsegurarEspacio abrió una página nueva, el título inicia en el margen
        // institucional. En caso contrario se conserva una sangría vertical clara
        // respecto de la tabla o bloque anterior.
        if (_current.CursorY > BodyTop + 0.5m)
            _current.CursorY += topSpacing;

        DrawText(Sanitizar(title).ToUpperInvariant(), MarginLeft, _current.CursorY, 11m, bold: true,
            InstitutionalReportStandard.Palette.Navy);
        _current.CursorY += 15m;
        DrawLine(MarginLeft, _current.CursorY, _pageWidth - MarginRight, _current.CursorY,
            InstitutionalReportStandard.Palette.Border, 0.7m);
        _current.CursorY += bottomSpacing;
    }

    public void AddParagraph(string text)
    {
        var width = _pageWidth - MarginLeft - MarginRight;
        var lines = Wrap(Sanitizar(text), width, 9m);
        var height = Math.Max(DefaultLineHeight, lines.Count * DefaultLineHeight) + 4m;
        AsegurarEspacio(height);
        DrawLines(lines, MarginLeft, _current.CursorY, 9m, DefaultLineHeight,
            InstitutionalReportStandard.Palette.BodyText);
        _current.CursorY += height;
    }

    public void AddKpis(IEnumerable<(string Label, string Value)> items)
    {
        var data = items.ToList();
        if (data.Count == 0) return;
        var gap = 8m;
        var columns = Math.Min(4, data.Count);
        var cardWidth = (_pageWidth - MarginLeft - MarginRight - gap * (columns - 1)) / columns;
        var rows = (int)Math.Ceiling(data.Count / (decimal)columns);
        var totalHeight = rows * 52m + Math.Max(0, rows - 1) * gap;
        AsegurarEspacio(totalHeight);

        for (var index = 0; index < data.Count; index++)
        {
            var row = index / columns;
            var col = index % columns;
            var x = MarginLeft + col * (cardWidth + gap);
            var y = _current.CursorY + row * (52m + gap);
            DrawRect(x, y, cardWidth, 52m, InstitutionalReportStandard.Palette.AlternateRow,
                InstitutionalReportStandard.Palette.Border);
            DrawText(Sanitizar(data[index].Label).ToUpperInvariant(), x + 8m, y + 16m, 7.5m, true,
                InstitutionalReportStandard.Palette.Muted);
            DrawText(Sanitizar(data[index].Value), x + 8m, y + 37m, 16m, true,
                InstitutionalReportStandard.Palette.Navy);
        }

        _current.CursorY += totalHeight + 8m;
    }

    public void AddKeyValueGrid(IEnumerable<(string Label, string Value)> items, int columns = 2)
    {
        var data = items.ToList();
        if (data.Count == 0) return;
        columns = Math.Clamp(columns, 1, 3);
        var available = _pageWidth - MarginLeft - MarginRight;
        var cellWidth = available / columns;
        var rows = (int)Math.Ceiling(data.Count / (decimal)columns);

        for (var row = 0; row < rows; row++)
        {
            var rowItems = data.Skip(row * columns).Take(columns).ToList();
            var wrapped = rowItems.Select(item => Wrap(Sanitizar(item.Value), cellWidth - 18m, 8.5m)).ToList();
            var maxLines = wrapped.Count == 0 ? 1 : wrapped.Max(lines => Math.Max(1, lines.Count));
            var rowHeight = 27m + maxLines * 10m;
            AsegurarEspacio(rowHeight);

            for (var col = 0; col < columns; col++)
            {
                var x = MarginLeft + col * cellWidth;
                DrawRect(x, _current.CursorY, cellWidth, rowHeight,
                    col % 2 == 0 ? "#FFFFFF" : InstitutionalReportStandard.Palette.AlternateRow,
                    InstitutionalReportStandard.Palette.Border);
                if (col >= rowItems.Count) continue;
                DrawText(Sanitizar(rowItems[col].Label).ToUpperInvariant(), x + 7m, _current.CursorY + 13m,
                    7m, true, InstitutionalReportStandard.Palette.Muted);
                DrawLines(wrapped[col], x + 7m, _current.CursorY + 28m, 8.5m, 10m,
                    InstitutionalReportStandard.Palette.BodyText);
            }

            _current.CursorY += rowHeight;
        }

        _current.CursorY += 8m;
    }

    public void AddTable(
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string>> rows,
        IReadOnlyList<decimal>? columnWeights = null,
        decimal fontSize = DefaultFontSize)
    {
        if (headers.Count == 0) return;
        var materialized = rows.ToList();
        var widths = ResolveWidths(headers.Count, columnWeights);
        const decimal padding = 5m;
        var headerHeight = CalculateRowHeight(headers, widths, fontSize, padding);
        var tableHeader = headers.Select(Sanitizar).ToArray();

        void DrawHeader()
        {
            var x = MarginLeft;
            for (var col = 0; col < tableHeader.Length; col++)
            {
                DrawRect(x, _current.CursorY, widths[col], headerHeight,
                    InstitutionalReportStandard.Palette.Navy, InstitutionalReportStandard.Palette.Navy);
                var lines = Wrap(tableHeader[col], widths[col] - padding * 2, fontSize);
                DrawLines(lines, x + padding, _current.CursorY + padding + fontSize, fontSize,
                    fontSize + 2m, InstitutionalReportStandard.Palette.HeaderText, bold: true);
                x += widths[col];
            }
            _current.CursorY += headerHeight;
        }

        AsegurarEspacio(headerHeight + 22m);
        DrawHeader();

        if (materialized.Count == 0)
        {
            materialized.Add(new[] { "Sin información" }
                .Concat(Enumerable.Repeat(string.Empty, headers.Count - 1)).ToArray());
        }

        for (var rowIndex = 0; rowIndex < materialized.Count; rowIndex++)
        {
            var normalized = Enumerable.Range(0, headers.Count)
                .Select(index => index < materialized[rowIndex].Count ? Sanitizar(materialized[rowIndex][index]) : string.Empty)
                .ToArray();
            var rowHeight = CalculateRowHeight(normalized, widths, fontSize, padding);

            if (!TieneEspacio(rowHeight))
            {
                NuevaPagina();
                DrawHeader();
            }

            var x = MarginLeft;
            var fill = rowIndex % 2 == 1 ? InstitutionalReportStandard.Palette.AlternateRow : "#FFFFFF";
            for (var col = 0; col < headers.Count; col++)
            {
                DrawRect(x, _current.CursorY, widths[col], rowHeight, fill,
                    InstitutionalReportStandard.Palette.Border);
                var lines = Wrap(normalized[col], widths[col] - padding * 2, fontSize);
                DrawLines(lines, x + padding, _current.CursorY + padding + fontSize, fontSize,
                    fontSize + 2m, InstitutionalReportStandard.Palette.BodyText);
                x += widths[col];
            }
            _current.CursorY += rowHeight;
        }

        _current.CursorY += 10m;
    }

    public byte[] ToBytes()
    {
        for (var index = 0; index < _pages.Count; index++)
            DibujarPie(_pages[index], index + 1, _pages.Count);

        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [] /Count 0 >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"
        };
        var pageIds = new List<int>();

        foreach (var page in _pages)
        {
            var pageId = objects.Count + 1;
            var contentId = objects.Count + 2;
            pageIds.Add(pageId);
            objects.Add(FormattableString.Invariant(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {_pageWidth:0.##} {_pageHeight:0.##}] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>"));
            var content = page.Content.ToString();
            objects.Add($"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream");
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pageIds.Count} >>";

        using var stream = new MemoryStream();
        var offsets = new List<long> { 0 };
        WriteLatin1(stream, "%PDF-1.4\n");
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(stream.Position);
            WriteLatin1(stream, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = stream.Position;
        WriteLatin1(stream, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            WriteLatin1(stream, $"{offset:0000000000} 00000 n \n");
        WriteLatin1(stream, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return stream.ToArray();
    }

    private void NuevaPagina()
    {
        _current = new PdfPage { CursorY = BodyTop };
        _pages.Add(_current);
        DibujarEncabezado(_current);
    }

    private void DibujarEncabezado(PdfPage page)
    {
        var previous = _current;
        _current = page;
        DrawRect(0m, 0m, _pageWidth, HeaderHeight, InstitutionalReportStandard.Palette.Navy,
            InstitutionalReportStandard.Palette.Navy);
        DrawText(InstitutionalReportStandard.InstitutionName, MarginLeft, 23m, 10m, true, "#FFFFFF");
        DrawText(_title, MarginLeft, 43m, 15m, true, "#FFFFFF");
        DrawText($"{InstitutionalReportStandard.SystemName} · {_module}", _pageWidth - MarginRight, 23m,
            8m, false, "#DCE6F0", alignRight: true);
        DrawText($"Generado: {_generatedAt.ToString(InstitutionalReportStandard.DateTimeFormat, CultureInfo.InvariantCulture)}",
            _pageWidth - MarginRight, 43m, 8m, false, "#DCE6F0", alignRight: true);
        _current = previous;
    }

    private void DibujarPie(PdfPage page, int pageNumber, int totalPages)
    {
        var previous = _current;
        _current = page;
        var y = _pageHeight - FooterHeight;
        DrawLine(MarginLeft, y, _pageWidth - MarginRight, y, InstitutionalReportStandard.Palette.Border, 0.6m);
        DrawText(InstitutionalReportStandard.SystemName, MarginLeft, y + 16m, 7m, false,
            InstitutionalReportStandard.Palette.Muted);
        DrawText(_generatedAt.ToString(InstitutionalReportStandard.DateTimeFormat, CultureInfo.InvariantCulture),
            _pageWidth / 2m, y + 16m, 7m, false, InstitutionalReportStandard.Palette.Muted, center: true);
        DrawText(InstitutionalReportStandard.PageLabel(pageNumber, totalPages), _pageWidth - MarginRight,
            y + 16m, 7m, false, InstitutionalReportStandard.Palette.Muted, alignRight: true);
        _current = previous;
    }

    private bool TieneEspacio(decimal height) =>
        _current.CursorY + height <= _pageHeight - FooterHeight - 10m;

    private void AsegurarEspacio(decimal height)
    {
        if (!TieneEspacio(height))
            NuevaPagina();
    }

    private decimal[] ResolveWidths(int count, IReadOnlyList<decimal>? weights)
    {
        var available = _pageWidth - MarginLeft - MarginRight;
        if (weights == null || weights.Count != count || weights.Sum() <= 0)
            return Enumerable.Repeat(available / count, count).ToArray();
        var total = weights.Sum();
        return weights.Select(weight => available * weight / total).ToArray();
    }

    private static decimal CalculateRowHeight(
        IReadOnlyList<string> values,
        IReadOnlyList<decimal> widths,
        decimal fontSize,
        decimal padding)
    {
        var maxLines = 1;
        for (var index = 0; index < values.Count; index++)
            maxLines = Math.Max(maxLines, Wrap(Sanitizar(values[index]), widths[index] - padding * 2, fontSize).Count);
        return Math.Max(22m, maxLines * (fontSize + 2m) + padding * 2);
    }

    private static List<string> Wrap(string text, decimal width, decimal fontSize)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string> { string.Empty };
        var maxChars = Math.Max(4, (int)Math.Floor(width / Math.Max(3.2m, fontSize * 0.52m)));
        var result = new List<string>();
        foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();
            foreach (var word in words)
            {
                if (word.Length > maxChars)
                {
                    if (line.Length > 0)
                    {
                        result.Add(line.ToString());
                        line.Clear();
                    }
                    for (var offset = 0; offset < word.Length; offset += maxChars)
                        result.Add(word.Substring(offset, Math.Min(maxChars, word.Length - offset)));
                    continue;
                }

                if (line.Length == 0)
                    line.Append(word);
                else if (line.Length + 1 + word.Length <= maxChars)
                    line.Append(' ').Append(word);
                else
                {
                    result.Add(line.ToString());
                    line.Clear();
                    line.Append(word);
                }
            }
            if (line.Length > 0) result.Add(line.ToString());
            if (words.Length == 0) result.Add(string.Empty);
        }
        return result.Count == 0 ? new List<string> { string.Empty } : result;
    }

    private void DrawLines(
        IReadOnlyList<string> lines,
        decimal x,
        decimal top,
        decimal fontSize,
        decimal lineHeight,
        string color,
        bool bold = false)
    {
        for (var index = 0; index < lines.Count; index++)
            DrawText(lines[index], x, top + index * lineHeight, fontSize, bold, color);
    }

    private void DrawText(
        string text,
        decimal x,
        decimal top,
        decimal fontSize,
        bool bold,
        string color,
        bool alignRight = false,
        bool center = false)
    {
        var safe = EscapePdf(Sanitizar(text));
        var estimatedWidth = safe.Length * fontSize * 0.48m;
        if (alignRight) x -= estimatedWidth;
        if (center) x -= estimatedWidth / 2m;
        var y = _pageHeight - top;
        var (r, g, b) = ParseColor(color);
        _current.Content.AppendLine(FormattableString.Invariant(
            $"{r:0.###} {g:0.###} {b:0.###} rg BT /{(bold ? "F2" : "F1")} {fontSize:0.##} Tf 1 0 0 1 {x:0.##} {y:0.##} Tm ({safe}) Tj ET"));
    }

    private void DrawRect(decimal x, decimal top, decimal width, decimal height, string fill, string border)
    {
        var y = _pageHeight - top - height;
        var (fr, fg, fb) = ParseColor(fill);
        var (br, bg, bb) = ParseColor(border);
        _current.Content.AppendLine(FormattableString.Invariant(
            $"{fr:0.###} {fg:0.###} {fb:0.###} rg {br:0.###} {bg:0.###} {bb:0.###} RG 0.5 w {x:0.##} {y:0.##} {width:0.##} {height:0.##} re B"));
    }

    private void DrawLine(decimal x1, decimal top1, decimal x2, decimal top2, string color, decimal width)
    {
        var y1 = _pageHeight - top1;
        var y2 = _pageHeight - top2;
        var (r, g, b) = ParseColor(color);
        _current.Content.AppendLine(FormattableString.Invariant(
            $"{r:0.###} {g:0.###} {b:0.###} RG {width:0.##} w {x1:0.##} {y1:0.##} m {x2:0.##} {y2:0.##} l S"));
    }

    private static (decimal R, decimal G, decimal B) ParseColor(string color)
    {
        var value = color.TrimStart('#');
        if (value.Length != 6) return (0m, 0m, 0m);
        return (
            int.Parse(value[..2], NumberStyles.HexNumber) / 255m,
            int.Parse(value.Substring(2, 2), NumberStyles.HexNumber) / 255m,
            int.Parse(value.Substring(4, 2), NumberStyles.HexNumber) / 255m);
    }

    private static string Sanitizar(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("\u0000", string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();

    private static string EscapePdf(string value) =>
        value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static void WriteLatin1(Stream stream, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private sealed class PdfPage
    {
        public decimal CursorY { get; set; }
        public StringBuilder Content { get; } = new();
    }
}
