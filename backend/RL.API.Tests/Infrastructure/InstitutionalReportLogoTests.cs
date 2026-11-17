using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using RL.API.Infrastructure.Reporting;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class InstitutionalReportLogoTests
{
    [Fact]
    public void ConfiguredLogoSource_UsesOnlySyntheticFixture()
    {
        var bytes = SyntheticReportLogo.PngBytes;
        var snapshot = CreateConfiguredLogo();
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), snapshot.LogoSha256);
        Assert.Equal(SyntheticReportLogo.Width, snapshot.LogoWidth);
        Assert.Equal(SyntheticReportLogo.Height, snapshot.LogoHeight);
        Assert.True(snapshot.HasLogo);
    }

    [Fact]
    public void Pdf_EmbedsConfiguredLogoAsAValidImageXObjectOnEveryPage()
    {
        var document = new InstitutionalPdfDocument(
            "Reporte institucional de prueba",
            "Pruebas",
            InstitutionalReportOrientation.Portrait,
            new DateTime(2026, 11, 13, 12, 0, 0), CreateConfiguredLogo());
        for (var index = 0; index < 100; index++)
            document.AddParagraph($"Contenido sintético {index} para verificar el encabezado y el recurso institucional.");

        var pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.StartsWith("%PDF-1.4", pdf, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Image", pdf, StringComparison.Ordinal);
        Assert.Contains($"/Width {SyntheticReportLogo.Width}", pdf, StringComparison.Ordinal);
        Assert.Contains($"/Height {SyntheticReportLogo.Height}", pdf, StringComparison.Ordinal);
        Assert.Contains("/ImIHSS Do", pdf, StringComparison.Ordinal);
        Assert.Contains("/XObject << /ImIHSS", pdf, StringComparison.Ordinal);
        Assert.True(pdf.Split("/ImIHSS Do", StringSplitOptions.None).Length - 1 > 1, "El logo debe repetirse en un PDF multipágina.");
        Assert.EndsWith("%%EOF", pdf, StringComparison.Ordinal);
    }

    [Fact]
    public void Pdf_UsesConfiguredLogoWidthAndCentersItVerticallyInTheHeader()
    {
        var document = new InstitutionalPdfDocument(
            "Reporte institucional de prueba",
            "Pruebas",
            InstitutionalReportOrientation.Portrait,
            new DateTime(2026, 11, 16, 12, 0, 0), CreateConfiguredLogo());

        var pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.Equal(205.92m, InstitutionalReportStandard.PdfLogoWidthPoints);
        Assert.Equal(90.72m, InstitutionalReportStandard.PdfLogoMaxHeightPoints);
        // El logo sintético es 2:1. Debe encajar dentro de la caja institucional
        // sin deformarse: al alcanzar la altura máxima, su ancho efectivo es 181.44.
        Assert.Matches(@"q 181\.44 0 0 90\.72 [0-9]+\.[0-9]+ [0-9]+\.[0-9]+ cm /ImIHSS Do Q", pdf);
        Assert.Contains("(SGRLA-IHSS", pdf, StringComparison.Ordinal);
    }

    [Fact]
    public void Xlsx_EmbedsValidDrawingRelationshipsAndPreservesData()
    {
        var workbook = new InstitutionalXlsxWorkbook(CreateConfiguredLogo());
        workbook.AddSheet(
            "Coincidencias",
            "Reporte de prueba",
            new[] { "Identificación", "Estado" },
            new[] { new object?[] { "0703197300189", "Pendiente" } });

        using var archive = new ZipArchive(new MemoryStream(workbook.ToBytes()), ZipArchiveMode.Read);
        var media = ReadEntry(archive, "xl/media/image1.png");
        var sheet = ReadEntryText(archive, "xl/worksheets/sheet1.xml");
        var sheetRelations = ReadEntryText(archive, "xl/worksheets/_rels/sheet1.xml.rels");
        var drawing = ReadEntryText(archive, "xl/drawings/drawing1.xml");
        var drawingRelations = ReadEntryText(archive, "xl/drawings/_rels/drawing1.xml.rels");
        var contentTypes = ReadEntryText(archive, "[Content_Types].xml");

        Assert.Equal(SyntheticReportLogo.PngBytes, media);
        Assert.Contains("<drawing r:id=\"rId1\"/>", sheet, StringComparison.Ordinal);
        Assert.Contains("r:embed=\"rId1\"", drawing, StringComparison.Ordinal);
        Assert.Contains("name=\"IHSS logo\"", drawing, StringComparison.Ordinal);
        Assert.Contains("Target=\"../drawings/drawing1.xml\"", sheetRelations, StringComparison.Ordinal);
        Assert.Contains("Target=\"../media/image1.png\"", drawingRelations, StringComparison.Ordinal);
        Assert.Contains("Extension=\"png\" ContentType=\"image/png\"", contentTypes, StringComparison.Ordinal);
        Assert.Contains("0703197300189", sheet, StringComparison.Ordinal);
    }

    [Fact]
    public void Xlsx_UsesTheResolvedBrandingForHeaderAndMetadata()
    {
        var branding = CreateConfiguredLogo() with
        {
            InstitutionName = "INSTITUCIÓN DE PRUEBA",
            SystemName = "SISTEMA DE PRUEBA",
            Palette = InstitutionalReportStandard.CreatePalette("#FFFFFF")
        };
        var workbook = new InstitutionalXlsxWorkbook(branding);
        workbook.AddSheet(
            "Prueba",
            "Reporte dinámico",
            new[] { "Código" },
            new[] { new object?[] { "R-001" } });

        using var archive = new ZipArchive(new MemoryStream(workbook.ToBytes()), ZipArchiveMode.Read);
        var sheet = ReadEntryText(archive, "xl/worksheets/sheet1.xml");
        var styles = ReadEntryText(archive, "xl/styles.xml");
        var core = ReadEntryText(archive, "docProps/core.xml");

        Assert.Contains("INSTITUCIÓN DE PRUEBA", sheet, StringComparison.Ordinal);
        Assert.Contains("rgb=\"FFFFFFFF\"", styles, StringComparison.Ordinal);
        Assert.Contains("SISTEMA DE PRUEBA", core, StringComparison.Ordinal);
    }

    [Fact]
    public void Pdf_WithoutConfiguredLogo_DoesNotEmbedFallbackImage()
    {
        var document = new InstitutionalPdfDocument(
            "Reporte sin logo", "Pruebas", InstitutionalReportOrientation.Portrait, new DateTime(2026, 11, 17));
        var pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.DoesNotContain("/Subtype /Image", pdf, StringComparison.Ordinal);
        Assert.DoesNotContain("/ImIHSS", pdf, StringComparison.Ordinal);
        Assert.Contains("Reporte sin logo", pdf, StringComparison.Ordinal);
    }

    [Fact]
    public void Xlsx_WithoutConfiguredLogo_DoesNotCreateMediaOrBrokenRelationships()
    {
        var workbook = new InstitutionalXlsxWorkbook();
        workbook.AddSheet("Reporte", "Sin logo", new[] { "Código" }, new[] { new object?[] { "A001" } });

        using var archive = new ZipArchive(new MemoryStream(workbook.ToBytes()), ZipArchiveMode.Read);
        Assert.Null(archive.GetEntry("xl/media/image1.png"));
        Assert.Null(archive.GetEntry("xl/drawings/drawing1.xml"));
        Assert.Null(archive.GetEntry("xl/worksheets/_rels/sheet1.xml.rels"));
        Assert.DoesNotContain("<drawing r:id=", ReadEntryText(archive, "xl/worksheets/sheet1.xml"), StringComparison.Ordinal);
        Assert.Contains("A001", ReadEntryText(archive, "xl/worksheets/sheet1.xml"), StringComparison.Ordinal);
    }

    private static InstitutionalBrandingSnapshot CreateConfiguredLogo() =>
        InstitutionalBrandingSnapshot.FromImage(
            SyntheticReportLogo.PngBytes,
            "INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL",
            "SGRLA-IHSS",
            "/uploads/logo-prueba.png",
            "#123B63",
            "configured-local-file");

    private static byte[] ReadEntry(ZipArchive archive, string name)
    {
        using var stream = archive.GetEntry(name)?.Open() ?? throw new InvalidOperationException($"Falta {name}.");
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static string ReadEntryText(ZipArchive archive, string name) =>
        Encoding.UTF8.GetString(ReadEntry(archive, name));
}
