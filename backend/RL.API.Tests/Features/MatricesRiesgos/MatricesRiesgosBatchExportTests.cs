using System.IO.Compression;
using System.Xml.Linq;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosBatchExportTests
{
    [Fact]
    public void Exports_UseCanonical82CatalogAndPreserveTypedAndSemanticValues()
    {
        InstitutionalProjectionFieldDefinitionDto[] catalog = new InstitutionalProjectionMapper().GetFieldCatalog().ToArray();
        InstitutionalProjectionFieldDto[] fields = catalog.Select((field, index) => new InstitutionalProjectionFieldDto(
            field.FieldNumber,
            field.FieldKey,
            field.FieldLabel,
            field.ValueType,
            index switch
            {
                0 => 1L,
                1 => "ROP-Á-Ñ-001",
                2 => "Valor con á, é, ñ y Ñ",
                3 => 0L,
                4 => false,
                5 => new DateTime(2026, 10, 18, 12, 30, 0, DateTimeKind.Utc),
                6 => null,
                _ => $"valor-{field.FieldNumber:D2}"
            },
            index == 6 ? "NOT_APPLICABLE" : "VALUE",
            field.Source)).ToArray();
        var projection = new InstitutionalEvaluationProjectionDto(7101, "ROP-Á-Ñ-001", fields) { RiskId = 41 };
        var row = new RiesgoReporteFilaDto { RiesgoId = 41, EvaluacionId = 7101, CodigoRiesgo = "ROP-Á-Ñ-001" };
        var exports = new MatricesRiesgosReportExportService();
        string directory = Path.Combine(Path.GetTempPath(), "rl-block10-exports-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            ArchivoReporteDto excel = exports.CrearExcelMatrizCompleta([row], new Dictionary<long, InstitutionalEvaluationProjectionDto> { [7101] = projection });
            string xlsxPath = Path.Combine(directory, "matriz.xlsx");
            File.WriteAllBytes(xlsxPath, excel.Contenido);
            using (ZipArchive zip = ZipFile.OpenRead(xlsxPath))
            {
                XDocument sheet = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                XElement header = sheet.Descendants(ns + "row").Single(item => (string?)item.Attribute("r") == "4");
                XElement data = sheet.Descendants(ns + "row").Single(item => (string?)item.Attribute("r") == "5");
                string[] headers = header.Elements(ns + "c").Select(cell => (string?)cell.Element(ns + "is")?.Element(ns + "t") ?? string.Empty).ToArray();
                XElement[] cells = data.Elements(ns + "c").ToArray();
                Assert.Equal(82, headers.Length);
                Assert.Equal(catalog.Select(item => item.FieldLabel), headers);
                Assert.Equal(82, cells.Length);
                Assert.Contains("<v>0</v>", cells[3].ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal);
                Assert.Contains("t=\"b\"", cells[4].ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal);
                Assert.Contains("No aplica", cells[6].ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal);
                Assert.Contains("valor-82", cells[81].ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal);
                for (int index = 0; index < fields.Length; index++)
                    Assert.Equal(ExpectedExcelValue(fields[index]), ReadExcelCell(cells[index], ns));
            }

            ArchivoReporteDto pdf = exports.CrearPdfMatrizCompleta([row], new Dictionary<long, InstitutionalEvaluationProjectionDto> { [7101] = projection });
            string pdfPath = Path.Combine(directory, "matriz.pdf");
            File.WriteAllBytes(pdfPath, pdf.Contenido);
            string content = System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(pdfPath));
            Assert.StartsWith("%PDF-1.4", content, StringComparison.Ordinal);
            Assert.Contains(catalog[0].FieldLabel, content, StringComparison.Ordinal);
            Assert.Contains(catalog[40].FieldLabel, content, StringComparison.Ordinal);
            Assert.Contains(catalog[81].FieldLabel, content, StringComparison.Ordinal);
            Assert.Contains("valor-82", content, StringComparison.Ordinal);
            Assert.Contains("ROP-Á-Ñ-001", content, StringComparison.Ordinal);
            Assert.Contains("No aplica", content, StringComparison.Ordinal);
            Assert.EndsWith("%%EOF", content, StringComparison.Ordinal);
            int previousLabelOffset = -1;
            foreach (InstitutionalProjectionFieldDto field in fields)
            {
                int labelOffset = content.IndexOf(EscapePdfString(field.FieldLabel), StringComparison.Ordinal);
                Assert.True(labelOffset > previousLabelOffset, $"PDF field {field.FieldNumber} label was missing or out of order.");
                previousLabelOffset = labelOffset;
                Assert.Contains(ExpectedPdfValue(field), content, StringComparison.Ordinal);
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static object ReadExcelCell(XElement cell, XNamespace ns)
    {
        string? type = (string?)cell.Attribute("t");
        if (type == "inlineStr") return (string?)cell.Element(ns + "is")?.Element(ns + "t") ?? string.Empty;
        string value = (string?)cell.Element(ns + "v") ?? string.Empty;
        if (type == "b") return value == "1";
        return double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double number)
            ? number : string.Empty;
    }

    private static object ExpectedExcelValue(InstitutionalProjectionFieldDto field)
    {
        if (field.Value is null) return field.State is "NOT_APPLICABLE" or "ABSENT_CONTROL_PROJECTED" ? "No aplica" : "Sin dato";
        if (field.Value is DateTime date) return date.ToOADate();
        if (field.Value is bool boolean) return boolean;
        if (field.Value is IConvertible convertible && field.Value is not string)
            return Convert.ToDouble(convertible, System.Globalization.CultureInfo.InvariantCulture);
        return Convert.ToString(field.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string ExpectedPdfValue(InstitutionalProjectionFieldDto field)
    {
        if (field.Value is null) return field.State is "NOT_APPLICABLE" or "ABSENT_CONTROL_PROJECTED" ? "No aplica" : "Sin dato";
        return Convert.ToString(field.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string EscapePdfString(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);

    [Fact]
    public void Exports_FailClosedWhenASelectedEvaluationIsMissingOrFieldOrderDiffers()
    {
        InstitutionalProjectionFieldDefinitionDto[] catalog = new InstitutionalProjectionMapper().GetFieldCatalog().ToArray();
        InstitutionalProjectionFieldDto[] fields = catalog.Select(field => new InstitutionalProjectionFieldDto(
            field.FieldNumber, field.FieldKey, field.FieldLabel, field.ValueType, "valor", "VALUE", field.Source)).ToArray();
        var projection = new InstitutionalEvaluationProjectionDto(7101, "R-001", fields) { RiskId = 1 };
        var row = new RiesgoReporteFilaDto { RiesgoId = 1, EvaluacionId = 7101, CodigoRiesgo = "R-001" };
        var exporter = new MatricesRiesgosReportExportService();

        Assert.Throws<InvalidOperationException>(() => exporter.CrearExcelMatrizCompleta([row], new Dictionary<long, InstitutionalEvaluationProjectionDto>()));
        InstitutionalProjectionFieldDto[] reversed = fields.Reverse().ToArray();
        Assert.Throws<InvalidOperationException>(() => exporter.CrearPdfMatrizCompleta([row], new Dictionary<long, InstitutionalEvaluationProjectionDto>
        {
            [7101] = projection with { Fields = reversed }
        }));
        Assert.Throws<InvalidOperationException>(() => exporter.CrearExcelMatrizCompleta([row], new Dictionary<long, InstitutionalEvaluationProjectionDto>
        {
            [7101] = projection with { RiskId = 2 }
        }));
    }
}
