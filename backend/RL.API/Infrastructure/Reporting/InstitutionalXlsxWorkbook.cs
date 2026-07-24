using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Generador OpenXML real para libros .xlsx institucionales.
/// Permite hojas tabulares tradicionales y documentos ejecutivos de una sola hoja.
/// </summary>
public sealed class InstitutionalXlsxWorkbook
{
    private readonly List<InstitutionalXlsxSheet> _sheets = new();

    public void AddSheet(
        string name,
        string title,
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<object?>> rows,
        InstitutionalReportOrientation? orientation = null)
    {
        var materialized = rows.Select(row => (IReadOnlyList<object?>)row.ToArray()).ToList();
        _sheets.Add(new InstitutionalXlsxSheet(
            NormalizeSheetName(name),
            title,
            headers.ToArray(),
            materialized,
            orientation ?? InstitutionalReportStandard.ResolveOrientation(headers.Count),
            null));
    }

    /// <summary>
    /// Agrega un reporte completo como documento continuo dentro de una única hoja.
    /// Cada fila define celdas, estilos y combinaciones de columnas, lo que permite
    /// replicar en Excel la jerarquía visual e informativa del PDF institucional.
    /// </summary>
    public void AddDocumentSheet(
        string name,
        IReadOnlyList<InstitutionalXlsxDocumentRow> rows,
        int columnCount,
        IReadOnlyList<decimal> columnWidths,
        InstitutionalReportOrientation orientation = InstitutionalReportOrientation.Landscape,
        int freezeRows = 2)
    {
        if (columnCount <= 0) throw new ArgumentOutOfRangeException(nameof(columnCount));
        if (columnWidths.Count != columnCount)
            throw new ArgumentException("La cantidad de anchos debe coincidir con el número de columnas.", nameof(columnWidths));

        _sheets.Add(new InstitutionalXlsxSheet(
            NormalizeSheetName(name),
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<IReadOnlyList<object?>>(),
            orientation,
            new InstitutionalXlsxDocument(
                rows.ToArray(),
                columnCount,
                columnWidths.ToArray(),
                Math.Clamp(freezeRows, 0, rows.Count))));
    }

    public byte[] ToBytes()
    {
        if (_sheets.Count == 0)
            AddSheet("Reporte", "Reporte institucional", new[] { "Información" },
                new[] { new object?[] { "Sin información" } });

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypes());
            WriteEntry(archive, "_rels/.rels", RootRelationships());
            WriteEntry(archive, "docProps/core.xml", CoreProperties());
            WriteEntry(archive, "docProps/app.xml", AppProperties());
            WriteEntry(archive, "xl/workbook.xml", WorkbookXml());
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationships());
            WriteEntry(archive, "xl/styles.xml", StylesXml());

            for (var index = 0; index < _sheets.Count; index++)
                WriteEntry(archive, $"xl/worksheets/sheet{index + 1}.xml", WorksheetXml(_sheets[index]));
        }
        return stream.ToArray();
    }

    private string ContentTypes()
    {
        var overrides = string.Join(string.Empty, Enumerable.Range(1, _sheets.Count)
            .Select(index => $"<Override PartName=\"/xl/worksheets/sheet{index}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>"));
        return $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
               "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
               "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
               "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
               "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
               "<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>" +
               "<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>" +
               overrides + "</Types>";
    }

    private static string RootRelationships() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
        "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/>" +
        "</Relationships>";

    private static string CoreProperties()
    {
        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" " +
               "xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" " +
               "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
               $"<dc:creator>{Xml(InstitutionalReportStandard.SystemName)}</dc:creator>" +
               $"<dc:title>Reporte institucional</dc:title><dc:subject>{Xml(InstitutionalReportStandard.InstitutionName)}</dc:subject>" +
               $"<dcterms:created xsi:type=\"dcterms:W3CDTF\">{now}</dcterms:created>" +
               $"<dcterms:modified xsi:type=\"dcterms:W3CDTF\">{now}</dcterms:modified></cp:coreProperties>";
    }

    private static string AppProperties() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" " +
        "xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\">" +
        "<Application>SGRLA-IHSS</Application><Company>Instituto Hondureño de Seguridad Social</Company></Properties>";

    private string WorkbookXml()
    {
        var sheets = string.Join(string.Empty, _sheets.Select((sheet, index) =>
            $"<sheet name=\"{XmlAttribute(sheet.Name)}\" sheetId=\"{index + 1}\" r:id=\"rId{index + 1}\"/>"));
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
               "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
               $"<sheets>{sheets}</sheets></workbook>";
    }

    private string WorkbookRelationships()
    {
        var sheetRelations = string.Join(string.Empty, Enumerable.Range(1, _sheets.Count)
            .Select(index => $"<Relationship Id=\"rId{index}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{index}.xml\"/>"));
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
               sheetRelations +
               $"<Relationship Id=\"rId{_sheets.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
               "</Relationships>";
    }

    private static string StylesXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"8\">" +
        "<font><sz val=\"10\"/><name val=\"Arial\"/><color rgb=\"FF1F2937\"/></font>" +
        "<font><b/><sz val=\"14\"/><name val=\"Arial\"/><color rgb=\"FFFFFFFF\"/></font>" +
        "<font><b/><sz val=\"10\"/><name val=\"Arial\"/><color rgb=\"FFFFFFFF\"/></font>" +
        "<font><b/><sz val=\"11\"/><name val=\"Arial\"/><color rgb=\"FF17466F\"/></font>" +
        "<font><b/><sz val=\"8\"/><name val=\"Arial\"/><color rgb=\"FF64748B\"/></font>" +
        "<font><b/><sz val=\"16\"/><name val=\"Arial\"/><color rgb=\"FF17466F\"/></font>" +
        "<font><sz val=\"9\"/><name val=\"Arial\"/><color rgb=\"FFE2E8F0\"/></font>" +
        "<font><b/><sz val=\"11\"/><name val=\"Arial\"/><color rgb=\"FFFFFFFF\"/></font>" +
        "</fonts>" +
        "<fills count=\"5\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF17466F\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF3F6F9\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFEEF3F7\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
        "<borders count=\"3\"><border/><border><left style=\"thin\"><color rgb=\"FFD5DEE7\"/></left>" +
        "<right style=\"thin\"><color rgb=\"FFD5DEE7\"/></right><top style=\"thin\"><color rgb=\"FFD5DEE7\"/></top>" +
        "<bottom style=\"thin\"><color rgb=\"FFD5DEE7\"/></bottom></border>" +
        "<border><bottom style=\"thin\"><color rgb=\"FFD5DEE7\"/></bottom></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"13\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"0\" borderId=\"2\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"4\" fillId=\"4\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"4\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"5\" fillId=\"4\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"6\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"right\" vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"7\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";

    private string WorksheetXml(InstitutionalXlsxSheet sheet) =>
        sheet.Document is null ? TabularWorksheetXml(sheet) : DocumentWorksheetXml(sheet);

    private string TabularWorksheetXml(InstitutionalXlsxSheet sheet)
    {
        var maxColumns = Math.Max(1, sheet.Headers.Count);
        var columnWidths = CalculateWidths(sheet);
        var columns = string.Join(string.Empty, columnWidths.Select((width, index) =>
            $"<col min=\"{index + 1}\" max=\"{index + 1}\" width=\"{width:0.##}\" customWidth=\"1\"/>"));

        var rows = new StringBuilder();
        rows.Append(RowXml(1, new object?[] { sheet.Title }, 1, maxColumns));
        rows.Append(RowXml(2, new object?[] { $"{InstitutionalReportStandard.InstitutionName} · Generado {DateTime.Now.ToString(InstitutionalReportStandard.DateTimeFormat, CultureInfo.InvariantCulture)}" }, 0, maxColumns));
        rows.Append("<row r=\"3\"/>");
        rows.Append(RowXml(4, sheet.Headers.Cast<object?>().ToArray(), 2, maxColumns));
        for (var index = 0; index < sheet.Rows.Count; index++)
            rows.Append(RowXml(index + 5, sheet.Rows[index], index % 2 == 1 ? 3 : 4, maxColumns));

        var lastRow = Math.Max(4, sheet.Rows.Count + 4);
        var lastColumn = ColumnName(maxColumns);
        var orientation = sheet.Orientation == InstitutionalReportOrientation.Landscape ? "landscape" : "portrait";
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
               "<sheetPr><pageSetUpPr fitToPage=\"1\" autoPageBreaks=\"0\"/></sheetPr>" +
               $"<dimension ref=\"A1:{lastColumn}{lastRow}\"/>" +
               "<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"4\" topLeftCell=\"A5\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
               "<sheetFormatPr defaultRowHeight=\"18\"/>" +
               $"<cols>{columns}</cols><sheetData>{rows}</sheetData>" +
               $"<autoFilter ref=\"A4:{lastColumn}{lastRow}\"/>" +
               $"<mergeCells count=\"2\"><mergeCell ref=\"A1:{lastColumn}1\"/><mergeCell ref=\"A2:{lastColumn}2\"/></mergeCells>" +
               "<printOptions horizontalCentered=\"1\"/><pageMargins left=\"0.35\" right=\"0.35\" top=\"0.65\" bottom=\"0.55\" header=\"0.2\" footer=\"0.2\"/>" +
               $"<pageSetup paperSize=\"9\" orientation=\"{orientation}\" fitToWidth=\"1\" fitToHeight=\"0\"/>" +
               "<headerFooter><oddHeader>&amp;C&amp;&quot;Arial,Bold&quot;&amp;10 INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL</oddHeader>" +
               "<oddFooter>&amp;LSGRLA-IHSS&amp;CGenerado: &amp;D &amp;T&amp;RPágina &amp;P de &amp;N</oddFooter></headerFooter>" +
               "</worksheet>";
    }

    private string DocumentWorksheetXml(InstitutionalXlsxSheet sheet)
    {
        var document = sheet.Document!;
        var columns = string.Join(string.Empty, document.ColumnWidths.Select((width, index) =>
            $"<col min=\"{index + 1}\" max=\"{index + 1}\" width=\"{width:0.##}\" customWidth=\"1\"/>"));
        var rows = new StringBuilder();
        var merges = new List<string>();

        for (var index = 0; index < document.Rows.Count; index++)
            rows.Append(DocumentRowXml(index + 1, document.Rows[index], document.ColumnCount, merges));

        var lastRow = Math.Max(1, document.Rows.Count);
        var lastColumn = ColumnName(document.ColumnCount);
        var orientation = sheet.Orientation == InstitutionalReportOrientation.Landscape ? "landscape" : "portrait";
        var freezePane = document.FreezeRows > 0
            ? $"<pane ySplit=\"{document.FreezeRows}\" topLeftCell=\"A{document.FreezeRows + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/>"
            : string.Empty;
        var mergeXml = merges.Count > 0
            ? $"<mergeCells count=\"{merges.Count}\">{string.Join(string.Empty, merges.Select(reference => $"<mergeCell ref=\"{reference}\"/>"))}</mergeCells>"
            : string.Empty;

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
               "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
               "<sheetPr><pageSetUpPr fitToPage=\"1\" autoPageBreaks=\"0\"/></sheetPr>" +
               $"<dimension ref=\"A1:{lastColumn}{lastRow}\"/>" +
               $"<sheetViews><sheetView workbookViewId=\"0\">{freezePane}</sheetView></sheetViews>" +
               "<sheetFormatPr defaultRowHeight=\"18\"/>" +
               $"<cols>{columns}</cols><sheetData>{rows}</sheetData>{mergeXml}" +
               "<printOptions horizontalCentered=\"1\"/><pageMargins left=\"0.25\" right=\"0.25\" top=\"0.45\" bottom=\"0.45\" header=\"0.2\" footer=\"0.2\"/>" +
               $"<pageSetup paperSize=\"9\" orientation=\"{orientation}\" fitToWidth=\"1\" fitToHeight=\"0\"/>" +
               "<headerFooter><oddHeader>&amp;C&amp;&quot;Arial,Bold&quot;&amp;10 INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL</oddHeader>" +
               "<oddFooter>&amp;LSGRLA-IHSS&amp;CGenerado: &amp;D &amp;T&amp;RPágina &amp;P de &amp;N</oddFooter></headerFooter>" +
               "</worksheet>";
    }

    private static string DocumentRowXml(
        int rowNumber,
        InstitutionalXlsxDocumentRow row,
        int maxColumns,
        ICollection<string> merges)
    {
        var cells = new StringBuilder();
        var column = 1;
        foreach (var cell in row.Cells)
        {
            var span = Math.Max(1, cell.ColumnSpan);
            if (column + span - 1 > maxColumns)
                throw new InvalidOperationException($"La fila {rowNumber} supera las {maxColumns} columnas configuradas.");

            cells.Append(CellXml(rowNumber, column, cell.Value, (int)cell.Style));
            for (var offset = 1; offset < span; offset++)
                cells.Append(CellXml(rowNumber, column + offset, null, (int)cell.Style));

            if (span > 1)
                merges.Add($"{ColumnName(column)}{rowNumber}:{ColumnName(column + span - 1)}{rowNumber}");
            column += span;
        }

        while (column <= maxColumns)
        {
            cells.Append(CellXml(rowNumber, column, null, (int)InstitutionalXlsxCellStyle.Body));
            column++;
        }

        var height = row.Height > 0 ? row.Height : 18m;
        return $"<row r=\"{rowNumber}\" ht=\"{height:0.##}\" customHeight=\"1\">{cells}</row>";
    }

    private static string RowXml(int rowNumber, IReadOnlyList<object?> values, int style, int maxColumns)
    {
        var cells = new StringBuilder();
        for (var column = 0; column < maxColumns; column++)
        {
            var value = column < values.Count ? values[column] : null;
            cells.Append(CellXml(rowNumber, column + 1, value, style));
        }
        return $"<row r=\"{rowNumber}\">{cells}</row>";
    }

    private static string CellXml(int rowNumber, int columnNumber, object? value, int style)
    {
        var reference = $"{ColumnName(columnNumber)}{rowNumber}";
        if (value is null)
            return $"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t></t></is></c>";

        if (value is byte or short or int or long or float or double or decimal)
        {
            var number = Convert.ToString(value, CultureInfo.InvariantCulture);
            return $"<c r=\"{reference}\" s=\"{style}\"><v>{number}</v></c>";
        }

        if (value is bool boolean)
            return $"<c r=\"{reference}\" s=\"{style}\" t=\"b\"><v>{(boolean ? 1 : 0)}</v></c>";

        var text = Xml(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        return $"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{text}</t></is></c>";
    }

    private static decimal[] CalculateWidths(InstitutionalXlsxSheet sheet)
    {
        var preferredWidths = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["ID"] = 8m,
            ["Sujeto"] = 30m,
            ["Documento"] = 18m,
            ["Tipo"] = 15m,
            ["Estado"] = 15m,
            ["Puntaje inherente"] = 14m,
            ["Nivel inherente"] = 14m,
            ["Puntaje residual"] = 14m,
            ["Nivel residual"] = 14m,
            ["Plan requerido"] = 14m,
            ["Fecha"] = 12m,
            ["Código"] = 10m,
            ["Factor"] = 16m,
            ["Matrices"] = 10m,
            ["Promedio inherente"] = 17m,
            ["Promedio residual"] = 17m,
            ["Alto / Crítico"] = 14m,
            ["Total"] = 11m,
            ["Vencidos"] = 12m
        };

        var widths = new decimal[sheet.Headers.Count];
        for (var column = 0; column < widths.Length; column++)
        {
            var header = sheet.Headers[column];
            if (preferredWidths.TryGetValue(header, out var preferred))
            {
                widths[column] = preferred;
                continue;
            }

            var maximum = header.Length;
            foreach (var row in sheet.Rows)
            {
                if (column < row.Count)
                    maximum = Math.Max(maximum, Convert.ToString(row[column], CultureInfo.InvariantCulture)?.Length ?? 0);
            }
            widths[column] = Math.Clamp(maximum + 2, 10, 34);
        }

        if (widths.Length > 0 && widths.Length <= 4)
        {
            const decimal minimumTotalWidth = 60m;
            var currentTotal = widths.Sum();
            if (currentTotal < minimumTotalWidth)
            {
                var extraPerColumn = (minimumTotalWidth - currentTotal) / widths.Length;
                for (var column = 0; column < widths.Length; column++)
                    widths[column] += extraPerColumn;
            }
        }

        return widths;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string NormalizeSheetName(string name)
    {
        var invalid = new HashSet<char>(new[] { '\\', '/', '?', '*', '[', ']', ':' });
        var clean = new string((name ?? "Reporte").Where(c => !invalid.Contains(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(clean)) clean = "Reporte";
        return clean.Length > 31 ? clean[..31] : clean;
    }

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return name;
    }

    private static string Xml(string value) => SecurityElement.Escape(value) ?? string.Empty;
    private static string XmlAttribute(string value) => Xml(value).Replace("\"", "&quot;");

    private sealed record InstitutionalXlsxSheet(
        string Name,
        string Title,
        IReadOnlyList<string> Headers,
        IReadOnlyList<IReadOnlyList<object?>> Rows,
        InstitutionalReportOrientation Orientation,
        InstitutionalXlsxDocument? Document);

    private sealed record InstitutionalXlsxDocument(
        IReadOnlyList<InstitutionalXlsxDocumentRow> Rows,
        int ColumnCount,
        IReadOnlyList<decimal> ColumnWidths,
        int FreezeRows);
}

public enum InstitutionalXlsxCellStyle
{
    Body = 0,
    Title = 1,
    TableHeader = 2,
    AlternateBody = 3,
    BorderedBody = 4,
    Section = 5,
    CardLabel = 6,
    CardValue = 7,
    KpiValue = 8,
    HeaderRight = 9,
    CenteredBody = 10,
    AlternateCenteredBody = 11,
    Institution = 12
}

public sealed record InstitutionalXlsxDocumentCell(
    object? Value,
    int ColumnSpan = 1,
    InstitutionalXlsxCellStyle Style = InstitutionalXlsxCellStyle.Body);

public sealed record InstitutionalXlsxDocumentRow(
    IReadOnlyList<InstitutionalXlsxDocumentCell> Cells,
    decimal Height = 18m);
