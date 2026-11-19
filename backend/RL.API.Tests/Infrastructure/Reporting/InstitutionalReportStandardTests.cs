using Xunit;
using RL.API.Infrastructure.Reporting;

namespace RL.API.Tests.Infrastructure.Reporting;

public sealed class InstitutionalReportStandardTests
{
    [Fact]
    public void Palette_UsesApprovedInstitutionalColors()
    {
        Assert.Equal("#123B63", InstitutionalReportStandard.Palette.Navy);
        Assert.Equal("#FFFFFF", InstitutionalReportStandard.Palette.HeaderText);
        Assert.Equal("#F3F6F9", InstitutionalReportStandard.Palette.AlternateRow);
    }

    [Theory]
    [InlineData("#123B63", "#FFFFFF")]
    [InlineData("#FFFFFF", "#000000")]
    [InlineData("#000000", "#FFFFFF")]
    [InlineData("#777777", "#000000")]
    [InlineData("#767676", "#000000")]
    [InlineData("#F3F6F9", "#000000")]
    public void CreatePalette_ResolvesReadableHeaderForeground(string background, string expectedForeground)
    {
        var palette = InstitutionalReportStandard.CreatePalette(background);

        Assert.Equal(background, palette.Navy);
        Assert.Equal(expectedForeground, palette.HeaderText);
    }

    [Theory]
    [InlineData(6, InstitutionalReportOrientation.Portrait)]
    [InlineData(8, InstitutionalReportOrientation.Portrait)]
    [InlineData(9, InstitutionalReportOrientation.Landscape)]
    [InlineData(14, InstitutionalReportOrientation.Landscape)]
    public void ResolveOrientation_DependsOnVisibleColumns(int columns, InstitutionalReportOrientation expected)
    {
        Assert.Equal(expected, InstitutionalReportStandard.ResolveOrientation(columns));
    }

    [Theory]
    [InlineData(82, InstitutionalReportOrientation.Landscape, 3)]
    [InlineData(4, InstitutionalReportOrientation.Portrait, 2)]
    [InlineData(2, InstitutionalReportOrientation.Portrait, 1)]
    public void ResolveKeyValueLayout_UsesSharedDensityRule(int fields, InstitutionalReportOrientation orientation, int expectedColumns)
    {
        var layout = InstitutionalReportStandard.ResolveKeyValueLayout(fields, orientation);

        Assert.Equal(expectedColumns, layout.Columns);
        Assert.Equal(orientation, layout.Orientation);
    }

    [Theory]
    [InlineData(100, 20, 140, false)]
    [InlineData(125, 20, 140, true)]
    public void MustMoveRowToNextPage_PreventsSplitRows(decimal currentY, decimal rowHeight, decimal footerStartY, bool expected)
    {
        Assert.Equal(expected, InstitutionalReportStandard.MustMoveRowToNextPage(currentY, rowHeight, footerStartY));
    }

    [Fact]
    public void ValidateMetadata_RejectsMissingTitle()
    {
        var metadata = new InstitutionalReportMetadata("", "Matrices de Riesgos", DateTime.UtcNow);
        Assert.Throws<ArgumentException>(() => InstitutionalReportStandard.ValidateMetadata(metadata));
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    public void InstitutionalPdfDocument_EmbedsLogo_WhenBrandingHasLogo(string mimeType)
    {
        var sampleBytes = mimeType == "image/png"
            ? new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            : new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        var branding = InstitutionalBrandingSnapshot.Canonical with
        {
            LogoMimeType = mimeType,
            LogoRawBytes = sampleBytes,
            LogoWidth = 100,
            LogoHeight = 50,
            LogoRgb = sampleBytes
        };

        var doc = new InstitutionalPdfDocument(
            "Reporte Prueba",
            "Módulo Prueba",
            InstitutionalReportOrientation.Portrait,
            DateTime.UtcNow,
            branding);

        var bytes = doc.ToBytes();
        var pdfText = System.Text.Encoding.ASCII.GetString(bytes);

        Assert.Contains("/ImIHSS", pdfText);
        Assert.Contains("/XObject << /ImIHSS", pdfText);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    public void InstitutionalXlsxWorkbook_EmbedsLogoAndDrawings_WhenBrandingHasLogo(string mimeType)
    {
        var sampleBytes = mimeType == "image/png"
            ? new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            : new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        var branding = InstitutionalBrandingSnapshot.Canonical with
        {
            LogoMimeType = mimeType,
            LogoRawBytes = sampleBytes,
            LogoWidth = 100,
            LogoHeight = 50,
            LogoRgb = sampleBytes
        };

        var workbook = new InstitutionalXlsxWorkbook(branding);
        var bytes = workbook.ToBytes();

        using var zip = new System.IO.Compression.ZipArchive(new System.IO.MemoryStream(bytes));
        Assert.NotNull(zip.GetEntry("xl/drawings/drawing1.xml"));
        Assert.NotNull(zip.GetEntry("xl/drawings/_rels/drawing1.xml.rels"));
        var imageExt = mimeType == "image/png" ? "png" : "jpeg";
        Assert.NotNull(zip.GetEntry($"xl/media/image1.{imageExt}"));
    }
}
