using System.Text;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Reporting;
using Xunit;

namespace RL.API.Tests.Infrastructure.Reporting;

public sealed class InstitutionalColorIndependenceTests
{
    [Theory]
    [InlineData("#FFFFFF")] // Encabezado blanco
    [InlineData("#123B63")] // Azul institucional IHSS
    [InlineData("#A7F3D0")] // Menta / Turquesa (caso reportado en captura)
    [InlineData("#F9A8D4")] // Rosa pastel
    [InlineData("#000000")] // Negro total
    [InlineData("#E0E7FF")] // Índigo claro
    public void Palette_EnsuresContentColorsAreIndependentOfHeaderColor(string headerColor)
    {
        var palette = InstitutionalReportStandard.CreatePalette(headerColor);

        // Los colores de contenido sobre fondo blanco DEBEN ser siempre negros independientes del encabezado
        Assert.Equal("#000000", palette.SectionTitle);
        Assert.Equal("#000000", palette.FilterTitle);
        Assert.Equal("#000000", palette.BodyText);
        Assert.Equal("#000000", palette.KpiValue);

        // El color del encabezado sí debe reflejar la configuración
        Assert.Equal(headerColor.ToUpperInvariant(), palette.Navy);
    }

    [Fact]
    public void InstitutionalPdfDocument_AddSection_EmitsBlackBoldTextRegardlessOfBrandingHeader()
    {
        // Branding con encabezado turquesa claro (caso de la captura)
        var branding = InstitutionalBrandingSnapshot.Canonical with
        {
            Palette = InstitutionalReportStandard.CreatePalette("#A7F3D0")
        };

        var doc = new InstitutionalPdfDocument(
            "Matriz Consolidada de Riesgos — Matriz completa 82 campos",
            "Matrices de Riesgos",
            InstitutionalReportOrientation.Landscape,
            new DateTime(2026, 11, 18, 19, 6, 0),
            branding);

        doc.AddSection("FILTROS APLICADOS");
        doc.AddParagraph("Sin filtros");
        doc.AddSection("ROTR-AFIL-1 — EVALUACIÓN 29");

        var bytes = doc.ToBytes();
        var pdfText = Encoding.ASCII.GetString(bytes);

        // 1. Debe contener los títulos de sección
        Assert.Contains("FILTROS APLICADOS", pdfText);
        Assert.Contains("ROTR-AFIL-1", pdfText);

        // 2. Debe usar "0 0 0 rg" (color negro) para el texto de las secciones
        Assert.Contains("0 0 0 rg BT /F2 11 Tf", pdfText);

        // 3. NO debe pintar los títulos de sección con el color del encabezado turquesa (#A7F3D0 -> r=0.655, g=0.953, b=0.816)
        // El comando "rg" para texto de sección nunca debe ser el del encabezado
        Assert.DoesNotContain("0.655 0.953 0.816 rg BT /F2 11 Tf", pdfText);
    }

    [Fact]
    public void InstitutionalPdfDocument_AddKeyValueGrid_EmitsBlackTextForLabelsAndValues()
    {
        var branding = InstitutionalBrandingSnapshot.Canonical with
        {
            Palette = InstitutionalReportStandard.CreatePalette("#FFFFFF")
        };

        var doc = new InstitutionalPdfDocument(
            "Reporte Prueba",
            "Módulo Prueba",
            InstitutionalReportOrientation.Portrait,
            DateTime.UtcNow,
            branding);

        doc.AddKeyValueGrid(new[]
        {
            ("Código de Riesgo", "ROTR-AFIL-1"),
            ("Área", "Afiliación")
        });

        var pdfText = Encoding.ASCII.GetString(doc.ToBytes());

        Assert.Contains("ROTR-AFIL-1", pdfText);
        Assert.Contains("0 0 0 rg BT /F2 7 Tf", pdfText); // Etiqueta negrita negra
        Assert.Contains("0 0 0 rg BT /F1 8.5 Tf", pdfText); // Valor negro
    }

    [Fact]
    public void InstitutionalXlsxWorkbook_EmitsBlackFontForSectionsAndKpis()
    {
        var branding = InstitutionalBrandingSnapshot.Canonical with
        {
            Palette = InstitutionalReportStandard.CreatePalette("#F9A8D4")
        };

        var workbook = new InstitutionalXlsxWorkbook(branding);
        var bytes = workbook.ToBytes();

        using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(bytes));
        var stylesEntry = zip.GetEntry("xl/styles.xml");
        Assert.NotNull(stylesEntry);

        using var reader = new StreamReader(stylesEntry.Open(), Encoding.UTF8);
        var stylesXml = reader.ReadToEnd();

        // Font 0 (Body): FF000000
        Assert.Contains("<font><sz val=\"10\"/><name val=\"Arial\"/><color rgb=\"FF000000\"/></font>", stylesXml);
        // Font 3 (Section): FF000000
        Assert.Contains("<font><b/><sz val=\"11\"/><name val=\"Arial\"/><color rgb=\"FF000000\"/></font>", stylesXml);
        // Font 4 (Card Label): FF000000
        Assert.Contains("<font><b/><sz val=\"8\"/><name val=\"Arial\"/><color rgb=\"FF000000\"/></font>", stylesXml);
        // Font 5 (KPI Value): FF000000
        Assert.Contains("<font><b/><sz val=\"16\"/><name val=\"Arial\"/><color rgb=\"FF000000\"/></font>", stylesXml);
    }

    [Fact]
    public void MatricesConsolidadasReport_GeneratesPdfWithBlackSectionHeaders()
    {
        var mapper = new InstitutionalProjectionMapper();
        var catalog = mapper.GetFieldCatalog().ToArray();
        var fields = catalog.Select(def => new InstitutionalProjectionFieldDto(
            def.FieldNumber,
            def.FieldKey,
            def.FieldLabel,
            def.ValueType,
            "VALOR_PRUEBA",
            "VALUE",
            def.Source)).ToArray();

        var proyecciones = new Dictionary<long, InstitutionalEvaluationProjectionDto>
        {
            [29] = new(29, "ROTR-AFIL-1", fields) { RiskId = 1 }
        };

        var filas = new[]
        {
            new RiesgoReporteFilaDto
            {
                RiesgoId = 1,
                EvaluacionId = 29,
                VersionFormularioId = 1,
                CodigoRiesgo = "ROTR-AFIL-1",
                EstadoEvaluacion = "APROBADA"
            }
        };

        var branding = InstitutionalBrandingSnapshot.Canonical with
        {
            Palette = InstitutionalReportStandard.CreatePalette("#A7F3D0")
        };

        // Instanciar documento directo con branding turquesa
        var doc = new InstitutionalPdfDocument(
            "Matriz Consolidada de Riesgos — Matriz completa 82 campos",
            "Matrices de Riesgos",
            InstitutionalReportStandard.ResolveOrientation(catalog.Length),
            DateTime.Now,
            branding);

        doc.AddSection("Filtros aplicados");
        doc.AddParagraph("Sin filtros");
        doc.AddSection("ROTR-AFIL-1 — Evaluación 29");
        doc.AddKeyValueGrid(proyecciones[29].Fields.Select(f => (f.FieldLabel, f.Value?.ToString() ?? "Sin dato")), preserveLabelCase: true);

        var pdfBytes = doc.ToBytes();
        var pdfText = Encoding.ASCII.GetString(pdfBytes);

        // Validar títulos exactos
        Assert.Contains("FILTROS APLICADOS", pdfText);
        Assert.Contains("ROTR-AFIL-1", pdfText);
        Assert.Contains("EVALUACI", pdfText); // EVALUACIÓN con escape

        // Validar texto negro y negrita
        Assert.Contains("0 0 0 rg BT /F2 11 Tf", pdfText);
    }
}
