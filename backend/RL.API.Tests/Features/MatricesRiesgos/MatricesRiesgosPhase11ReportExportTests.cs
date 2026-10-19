using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosPhase11ReportExportTests
{
    private readonly MatricesRiesgosReportExportService _service = new();

    [Fact]
    public void ExcelConsolidado_GeneraXlsxValidoCon82ColumnasPorCadaRiesgo()
    {
        IReadOnlyList<RiesgoReporteFilaDto> rows = CreateRows(35);
        Dictionary<long, InstitutionalEvaluationProjectionDto> projections = CreateProjections(rows);
        ArchivoReporteDto file = _service.CrearExcelMatrizCompleta(rows, projections, "Estado=APROBADA");

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.EndsWith(".xlsx", file.NombreArchivo, StringComparison.OrdinalIgnoreCase);
        using var stream = new MemoryStream(file.Contenido);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        XDocument sheet = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XElement[] rowsInSheet = sheet.Descendants(ns + "row").ToArray();
        string[] expectedHeaders = new InstitutionalProjectionMapper().GetFieldCatalog().Select(field => field.FieldLabel).ToArray();
        string[] headers = rowsInSheet.Single(row => (string?)row.Attribute("r") == "4")
            .Elements(ns + "c").Select(cell => (string?)cell.Element(ns + "is")?.Element(ns + "t") ?? string.Empty).ToArray();

        Assert.Equal(82, headers.Length);
        Assert.Equal(expectedHeaders, headers);
        Assert.Equal(35, rowsInSheet.Count(row => int.TryParse((string?)row.Attribute("r"), out int number) && number >= 5));
        Assert.All(rowsInSheet.Where(row => int.TryParse((string?)row.Attribute("r"), out int number) && number >= 5),
            row => Assert.Equal(82, row.Elements(ns + "c").Count()));
        Assert.Contains("Estado=APROBADA", sheet.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal);
        Assert.Contains("R-035", sheet.ToString(SaveOptions.DisableFormatting), StringComparison.Ordinal);
    }

    [Fact]
    public void PdfConsolidado_GeneraPdfCompletoConCamposCanonicosEnOrden()
    {
        IReadOnlyList<RiesgoReporteFilaDto> rows = CreateRows(3);
        Dictionary<long, InstitutionalEvaluationProjectionDto> projections = CreateProjections(rows);
        ArchivoReporteDto file = _service.CrearPdfMatrizCompleta(rows, projections, "Estado=APROBADA");

        Assert.Equal("application/pdf", file.ContentType);
        string content = Encoding.Latin1.GetString(file.Contenido);
        Assert.StartsWith("%PDF-1.4", content, StringComparison.Ordinal);
        Assert.Contains("/Count ", content, StringComparison.Ordinal);
        Assert.Contains("R-003", content, StringComparison.Ordinal);
        Assert.Contains("Estado=APROBADA", content, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF", content, StringComparison.Ordinal);

        string[] labels = new InstitutionalProjectionMapper().GetFieldCatalog().Select(field => field.FieldLabel).ToArray();
        int previous = -1;
        foreach (string label in labels)
        {
            int index = content.IndexOf(EscapePdfString(label), StringComparison.Ordinal);
            Assert.True(index > previous, $"PDF canonical field missing or out of order: {label}");
            previous = index;
        }
        Assert.Contains("valor-82", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportesVacios_ConservanWorkbookDe82ColumnasYDocumentoPdfValido()
    {
        IReadOnlyList<RiesgoReporteFilaDto> rows = Array.Empty<RiesgoReporteFilaDto>();
        var projections = new Dictionary<long, InstitutionalEvaluationProjectionDto>();
        ArchivoReporteDto excel = _service.CrearExcelMatrizCompleta(rows, projections);
        ArchivoReporteDto pdf = _service.CrearPdfMatrizCompleta(rows, projections);

        using var stream = new MemoryStream(excel.Contenido);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        XDocument sheet = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XElement header = sheet.Descendants(ns + "row").Single(row => (string?)row.Attribute("r") == "4");
        Assert.Equal(82, header.Elements(ns + "c").Count());
        string pdfText = Encoding.Latin1.GetString(pdf.Contenido);
        Assert.StartsWith("%PDF-1.4", pdfText, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF", pdfText, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportacionesConsolidado_FallanCerradoSiFaltaProyeccionSeleccionada()
    {
        RiesgoReporteFilaDto row = CreateRows(1).Single();
        Assert.Throws<InvalidOperationException>(() => _service.CrearExcelMatrizCompleta([row], new Dictionary<long, InstitutionalEvaluationProjectionDto>()));
        Assert.Throws<InvalidOperationException>(() => _service.CrearPdfMatrizCompleta([row], new Dictionary<long, InstitutionalEvaluationProjectionDto>()));
    }

    private static IReadOnlyList<RiesgoReporteFilaDto> CreateRows(int count) => Enumerable.Range(1, count)
        .Select(index => new RiesgoReporteFilaDto
        {
            RiesgoId = index,
            EvaluacionId = index + 100,
            VersionFormularioId = 1,
            CodigoRiesgo = $"R-{index:D3}",
            EstadoEvaluacion = "APROBADA"
        }).ToArray();

    private static Dictionary<long, InstitutionalEvaluationProjectionDto> CreateProjections(IEnumerable<RiesgoReporteFilaDto> rows)
    {
        InstitutionalProjectionFieldDefinitionDto[] catalog = new InstitutionalProjectionMapper().GetFieldCatalog().ToArray();
        return rows.ToDictionary(row => row.EvaluacionId, row => new InstitutionalEvaluationProjectionDto(
            row.EvaluacionId,
            row.CodigoRiesgo,
            catalog.Select(field => new InstitutionalProjectionFieldDto(
                field.FieldNumber,
                field.FieldKey,
                field.FieldLabel,
                field.ValueType,
                field.FieldNumber == 2 ? row.CodigoRiesgo : field.FieldNumber == 82 ? "valor-82" : $"valor-{field.FieldNumber:D2}",
                "VALUE",
                field.Source)).ToArray()) { RiskId = row.RiesgoId });
    }

    private static string EscapePdfString(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
}
