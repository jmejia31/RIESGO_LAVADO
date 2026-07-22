using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Generador OpenXML real para libros .xlsx institucionales.
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
            orientation ?? InstitutionalReportStandard.ResolveOrientation(headers.Count)));
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
        "<fonts count=\"3\">" +
        "<font><sz val=\"10\"/><name val=\"Arial\"/><color rgb=\"FF1F2937\"/></font>" +
        "<font><b/><sz val=\"14\"/><name val=\"Arial\"/><color rgb=\"FFFFFFFF\"/></font>" +
        "<font><b/><sz val=\"10\"/><name val=\"Arial\"/><color rgb=\"FFFFFFFF\"/></font>" +
        "</fonts>" +
        "<fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF123B63\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF3F6F9\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
        "<borders count=\"2\"><border/><border><left style=\"thin\"><color rgb=\"FFD8E0E8\"/></left>" +
        "<right style=\"thin\"><color rgb=\"FFD8E0E8\"/></right><top style=\"thin\"><color rgb=\"FFD8E0E8\"/></top>" +
        "<bottom style=\"thin\"><color rgb=\"FFD8E0E8\"/></bottom></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"5\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"1\" xfId=\"0\" applyAlignment=\"1\"><alignment vertical=\"top\" wrapText=\"1\"/></xf>" +
        "</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";

    private string WorksheetXml(InstitutionalXlsxSheet sheet)
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
               $"<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"4\" topLeftCell=\"A5\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>" +
               $"<cols>{columns}</cols><sheetData>{rows}</sheetData>" +
               $"<mergeCells count=\"2\"><mergeCell ref=\"A1:{lastColumn}1\"/><mergeCell ref=\"A2:{lastColumn}2\"/></mergeCells>" +
               $"<autoFilter ref=\"A4:{lastColumn}{lastRow}\"/>" +
               "<printOptions horizontalCentered=\"1\"/><pageMargins left=\"0.35\" right=\"0.35\" top=\"0.65\" bottom=\"0.55\" header=\"0.2\" footer=\"0.2\"/>" +
               $"<pageSetup paperSize=\"9\" orientation=\"{orientation}\" fitToWidth=\"1\" fitToHeight=\"0\"/>" +
               "<headerFooter><oddHeader>&amp;C&amp;&quot;Arial,Bold&quot;&amp;10 INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL</oddHeader>" +
               "<oddFooter>&amp;LSGRLA-IHSS&amp;CGenerado: &amp;D &amp;T&amp;RPágina &amp;P de &amp;N</oddFooter></headerFooter>" +
               "</worksheet>";
    }

    private static string RowXml(int rowNumber, IReadOnlyList<object?> values, int style, int maxColumns)
    {
        var cells = new StringBuilder();
        for (var column = 0; column < maxColumns; column++)
        {
            var value = column < values.Count ? values[column] : null;
            var reference = $"{ColumnName(column + 1)}{rowNumber}";
            if (value is null)
            {
                cells.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t></t></is></c>");
                continue;
            }

            if (value is byte or short or int or long or float or double or decimal)
            {
                var number = Convert.ToString(value, CultureInfo.InvariantCulture);
                cells.Append($"<c r=\"{reference}\" s=\"{style}\"><v>{number}</v></c>");
            }
            else if (value is bool boolean)
            {
                cells.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"b\"><v>{(boolean ? 1 : 0)}</v></c>");
            }
            else
            {
                var text = Xml(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                cells.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{text}</t></is></c>");
            }
        }
        return $"<row r=\"{rowNumber}\">{cells}</row>";
    }

    private static decimal[] CalculateWidths(InstitutionalXlsxSheet sheet)
    {
        // Anchos institucionales para columnas recurrentes. El objetivo es evitar
        // cortes de palabras en tipos, estados y niveles cuando la hoja se ajusta
        // a una página de ancho, sin sobredimensionar las columnas descriptivas.
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

        // Las hojas pequeñas deben conservar una anchura visual suficiente para
        // que títulos y valores no se compriman en una columna excesivamente estrecha.
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
        InstitutionalReportOrientation Orientation);
}
