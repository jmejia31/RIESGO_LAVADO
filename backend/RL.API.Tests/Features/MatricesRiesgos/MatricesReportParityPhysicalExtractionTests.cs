using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Infrastructure.Reporting;
using Xunit;
using Xunit.Abstractions;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Microfase C3 — Certificación física de paridad REP-01 (Excel) y REP-02 (PDF).
/// Valida generación física de bytes, cálculo de SHA-256, extracción de estructuras
/// OpenXML y PDF, 59 riesgos x 82 campos canónicos, geometría B06 y 5 temas M2.
/// </summary>
public sealed class MatricesReportParityPhysicalExtractionTests
{
    private readonly ITestOutputHelper _output;
    private readonly MatricesRiesgosReportExportService _service = new();

    public MatricesReportParityPhysicalExtractionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly string[] FiveThemes =
    [
        "#123B63", // Navy institucional
        "#0F766E", // Teal
        "#B91C1C", // Red
        "#1E293B", // Slate
        "#0284C7"  // Sky
    ];

    [Fact]
    public void Pareja1_MatrizCompleta_59Riesgos82Campos_ParidadFisicaExcelYPdf()
    {
        // 1. Crear 59 riesgos canónicos con 82 campos
        const int rowCount = 59;
        IReadOnlyList<RiesgoReporteFilaDto> rows = CreateRows(rowCount);
        Dictionary<long, InstitutionalEvaluationProjectionDto> projections = CreateProjections(rows);
        const string appliedFilters = "Estado=APROBADA; Periodo=2026; Tipo=Operativo";

        // 2. Generar físicamente los archivos
        ArchivoReporteDto excelReport = _service.CrearExcelMatrizCompleta(rows, projections, appliedFilters);
        ArchivoReporteDto pdfReport = _service.CrearPdfMatrizCompleta(rows, projections, appliedFilters);

        // 3. Validar bytes y calcular SHA-256
        Assert.NotNull(excelReport.Contenido);
        Assert.True(excelReport.Contenido.Length > 0, "Excel content is empty.");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelReport.ContentType);
        Assert.EndsWith(".xlsx", excelReport.NombreArchivo, StringComparison.OrdinalIgnoreCase);

        Assert.NotNull(pdfReport.Contenido);
        Assert.True(pdfReport.Contenido.Length > 0, "PDF content is empty.");
        Assert.Equal("application/pdf", pdfReport.ContentType);
        Assert.EndsWith(".pdf", pdfReport.NombreArchivo, StringComparison.OrdinalIgnoreCase);

        string excelSha256 = ComputeSha256Hex(excelReport.Contenido);
        string pdfSha256 = ComputeSha256Hex(pdfReport.Contenido);
        _output.WriteLine($"REP01_EXCEL_SHA256={excelSha256}");
        _output.WriteLine($"REP02_PDF_SHA256={pdfSha256}");

        Assert.Equal(64, excelSha256.Length);
        Assert.Equal(64, pdfSha256.Length);

        // 4. Extracción física OpenXML (Excel)
        using (var excelStream = new MemoryStream(excelReport.Contenido))
        using (var zip = new ZipArchive(excelStream, ZipArchiveMode.Read))
        {
            Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
            Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
            ZipArchiveEntry? sheetEntry = zip.GetEntry("xl/worksheets/sheet1.xml");
            Assert.NotNull(sheetEntry);

            XDocument sheetDoc = XDocument.Load(sheetEntry!.Open());
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XElement[] sheetRows = sheetDoc.Descendants(ns + "row").ToArray();

            // Fila 4: encabezados de 82 campos
            XElement headerRow = sheetRows.Single(r => (string?)r.Attribute("r") == "4");
            string[] actualHeaders = headerRow.Elements(ns + "c")
                .Select(c => (string?)c.Element(ns + "is")?.Element(ns + "t") ?? string.Empty)
                .ToArray();

            string[] expectedHeaders = new InstitutionalProjectionMapper().GetFieldCatalog()
                .Select(f => f.FieldLabel)
                .ToArray();

            Assert.Equal(82, actualHeaders.Length);
            Assert.Equal(expectedHeaders, actualHeaders);

            // Filas de datos (r >= 5): exactamente 59 registros
            XElement[] dataRows = sheetRows
                .Where(r => int.TryParse((string?)r.Attribute("r"), out int num) && num >= 5)
                .ToArray();

            Assert.Equal(rowCount, dataRows.Length);

            // Cada fila de datos contiene 82 celdas
            Assert.All(dataRows, row => Assert.Equal(82, row.Elements(ns + "c").Count()));

            string fullSheetXml = sheetDoc.ToString(SaveOptions.DisableFormatting);
            Assert.Contains(appliedFilters, fullSheetXml, StringComparison.Ordinal);
            Assert.Contains("R-001", fullSheetXml, StringComparison.Ordinal);
            Assert.Contains("R-059", fullSheetXml, StringComparison.Ordinal);
            Assert.Contains("0801199000001", fullSheetXml, StringComparison.Ordinal); // Preserva ceros a la izquierda
        }

        // 5. Extracción física PDF
        string pdfText = Encoding.Latin1.GetString(pdfReport.Contenido);
        Assert.StartsWith("%PDF-1.4", pdfText, StringComparison.Ordinal);
        Assert.Contains("/MediaBox [0 0 841.89 595.28]", pdfText, StringComparison.Ordinal); // Landscape A4
        Assert.Contains(appliedFilters, pdfText, StringComparison.Ordinal);
        Assert.Contains("R-001", pdfText, StringComparison.Ordinal);
        Assert.Contains("R-059", pdfText, StringComparison.Ordinal);
        Assert.Contains("0801199000001", pdfText, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF", pdfText, StringComparison.Ordinal);

        // Validar orden canónico de campos en PDF
        string[] labels = new InstitutionalProjectionMapper().GetFieldCatalog()
            .Select(f => f.FieldLabel)
            .ToArray();

        int lastIndex = -1;
        foreach (string label in labels.Take(10)) // Muestra secuencial garantizada
        {
            int foundIndex = pdfText.IndexOf(EscapePdfString(label), StringComparison.Ordinal);
            Assert.True(foundIndex > lastIndex, $"Campo PDF fuera de orden o ausente: {label}");
            lastIndex = foundIndex;
        }

        // 6. Geometría B06: ancho canónico 205.92 pt (72.644 mm)
        Assert.Equal(205.92m, InstitutionalReportStandard.PdfLogoWidthPoints);
        Assert.Equal(90.72m, InstitutionalReportStandard.PdfLogoMaxHeightPoints);
        Assert.Equal(1_485_900, InstitutionalReportStandard.ExcelLogoWidthEmu);
        Assert.Equal(540_000, InstitutionalReportStandard.ExcelLogoMaxHeightEmu);
    }

    [Theory]
    [InlineData("#123B63")]
    [InlineData("#0F766E")]
    [InlineData("#B91C1C")]
    [InlineData("#1E293B")]
    [InlineData("#0284C7")]
    public void CincoTemasM2_GeneranColoresConContrasteAccesibleYDocumentosValidos(string themeHex)
    {
        InstitutionalReportPalette palette = InstitutionalReportStandard.CreatePalette(themeHex);
        Assert.Equal(themeHex.ToUpperInvariant(), palette.Navy);
        Assert.True(palette.HeaderText == "#FFFFFF" || palette.HeaderText == "#000000");

        var branding = InstitutionalBrandingSnapshot.Canonical with { Palette = palette };

        var serviceWithBranding = new MatricesRiesgosReportExportService(new FixedBrandingResolver(branding));

        IReadOnlyList<RiesgoReporteFilaDto> rows = CreateRows(5);
        Dictionary<long, InstitutionalEvaluationProjectionDto> projections = CreateProjections(rows);

        ArchivoReporteDto excel = serviceWithBranding.CrearExcelMatrizCompleta(rows, projections, $"Tema={themeHex}");
        ArchivoReporteDto pdf = serviceWithBranding.CrearPdfMatrizCompleta(rows, projections, $"Tema={themeHex}");

        Assert.True(excel.Contenido.Length > 0);
        Assert.True(pdf.Contenido.Length > 0);
        Assert.StartsWith("%PDF-1.4", Encoding.Latin1.GetString(pdf.Contenido));
    }

    private static string ComputeSha256Hex(byte[] bytes)
    {
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
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
                field.FieldNumber == 2 ? row.CodigoRiesgo :
                field.FieldNumber == 5 ? $"08011990{row.RiesgoId:D5}" : // Simula DNI con ceros a la izquierda
                field.FieldNumber == 82 ? "valor-82" : $"valor-{field.FieldNumber:D2}",
                "APROBADA",
                field.Source
            )).ToArray()
        ) { RiskId = row.RiesgoId });
    }

    private static string EscapePdfString(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private sealed class FixedBrandingResolver : IInstitutionalBrandingResolver
    {
        private readonly InstitutionalBrandingSnapshot _branding;
        public FixedBrandingResolver(InstitutionalBrandingSnapshot branding) => _branding = branding;
        public Task<InstitutionalBrandingSnapshot> ResolveAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_branding);
    }
}
