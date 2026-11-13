using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using RL.API.Infrastructure.Reporting;
using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class InstitutionalReportLogoTests
{
    [Fact]
    public void CanonicalLogoSource_IsEmbeddedAndIntegrityChecked()
    {
        var bytes = InstitutionalLogoAsset.PngBytes;

        Assert.Equal(InstitutionalLogoAsset.Sha256, Convert.ToHexString(SHA256.HashData(bytes)));
        Assert.Equal(2034, InstitutionalLogoAsset.Width);
        Assert.Equal(773, InstitutionalLogoAsset.Height);
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);
    }

    [Fact]
    public void Pdf_EmbedsTheCanonicalLogoAsAValidImageXObjectOnEveryPage()
    {
        var document = new InstitutionalPdfDocument(
            "Reporte institucional de prueba",
            "Pruebas",
            InstitutionalReportOrientation.Portrait,
            new DateTime(2026, 11, 13, 12, 0, 0));
        for (var index = 0; index < 100; index++)
            document.AddParagraph($"Contenido sintético {index} para verificar el encabezado y el recurso institucional.");

        var pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.StartsWith("%PDF-1.4", pdf, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Image", pdf, StringComparison.Ordinal);
        Assert.Contains($"/Width {InstitutionalLogoAsset.Width}", pdf, StringComparison.Ordinal);
        Assert.Contains($"/Height {InstitutionalLogoAsset.Height}", pdf, StringComparison.Ordinal);
        Assert.Contains("/ImIHSS Do", pdf, StringComparison.Ordinal);
        Assert.Contains("/XObject << /ImIHSS", pdf, StringComparison.Ordinal);
        Assert.True(pdf.Split("/ImIHSS Do", StringSplitOptions.None).Length - 1 > 1, "El logo debe repetirse en un PDF multipágina.");
        Assert.EndsWith("%%EOF", pdf, StringComparison.Ordinal);
    }

    [Fact]
    public void Xlsx_EmbedsValidDrawingRelationshipsAndPreservesData()
    {
        var workbook = new InstitutionalXlsxWorkbook();
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

        Assert.Equal(InstitutionalLogoAsset.PngBytes, media);
        Assert.Contains("<drawing r:id=\"rId1\"/>", sheet, StringComparison.Ordinal);
        Assert.Contains("r:embed=\"rId1\"", drawing, StringComparison.Ordinal);
        Assert.Contains("name=\"IHSS logo\"", drawing, StringComparison.Ordinal);
        Assert.Contains("Target=\"../drawings/drawing1.xml\"", sheetRelations, StringComparison.Ordinal);
        Assert.Contains("Target=\"../media/image1.png\"", drawingRelations, StringComparison.Ordinal);
        Assert.Contains("Extension=\"png\" ContentType=\"image/png\"", contentTypes, StringComparison.Ordinal);
        Assert.Contains("0703197300189", sheet, StringComparison.Ordinal);
    }

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
