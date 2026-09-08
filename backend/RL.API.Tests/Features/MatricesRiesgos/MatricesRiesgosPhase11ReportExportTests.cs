using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using RL.API.Features.MatricesRiesgos.Application;
using RL.API.Features.MatricesRiesgos.Contracts;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosPhase11ReportExportTests
{
    private readonly MatricesRiesgosReportExportService _service = new();

    [Fact]
    public void ExcelConsolidado_GeneraXlsxOpenXmlValidoConTodosLosRegistros()
    {
        ArchivoReporteDto archivo = _service.CrearExcelConsolidado(CrearFilas(35));

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", archivo.ContentType);
        Assert.EndsWith(".xlsx", archivo.NombreArchivo, StringComparison.OrdinalIgnoreCase);
        Assert.True(archivo.Contenido.Length > 1000);
        Assert.Equal((byte)'P', archivo.Contenido[0]);
        Assert.Equal((byte)'K', archivo.Contenido[1]);

        using var stream = new MemoryStream(archivo.Contenido);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
        ZipArchiveEntry? hoja = zip.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(hoja);
        using var reader = new StreamReader(hoja!.Open(), Encoding.UTF8);
        string xml = reader.ReadToEnd();
        Assert.Contains("R-035", xml, StringComparison.Ordinal);
        string workbook = LeerEntrada(zip, "xl/workbook.xml");
        string styles = LeerEntrada(zip, "xl/styles.xml");
        string sheet = LeerEntrada(zip, "xl/worksheets/sheet1.xml");
        Assert.Contains("Matriz Consolidada", workbook, StringComparison.Ordinal);
        Assert.Contains("INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL", sheet, StringComparison.Ordinal);
        Assert.Contains("orientation=\"landscape\"", sheet, StringComparison.Ordinal);
        Assert.Contains("fitToWidth=\"1\"", sheet, StringComparison.Ordinal);
        Assert.Contains("&amp;LSGRLA-IHSS", sheet, StringComparison.Ordinal);
        Assert.Contains("numFmtId=\"165\"", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfConsolidado_GeneraPdfPaginadoYCompleto()
    {
        ArchivoReporteDto archivo = _service.CrearPdfConsolidado(CrearFilas(60));

        Assert.Equal("application/pdf", archivo.ContentType);
        Assert.EndsWith(".pdf", archivo.NombreArchivo, StringComparison.OrdinalIgnoreCase);
        string texto = Encoding.Latin1.GetString(archivo.Contenido);
        Assert.StartsWith("%PDF-1.4", texto, StringComparison.Ordinal);
        Assert.Contains("/Count ", texto, StringComparison.Ordinal);
        Assert.Contains("/MediaBox [0 0 841.89 595.28]", texto, StringComparison.Ordinal);
        Assert.Contains("INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL", texto, StringComparison.Ordinal);
        Assert.Contains("SGRLA-IHSS", texto, StringComparison.Ordinal);
        Assert.Contains("Página 1 de", texto, StringComparison.Ordinal);
        Assert.Contains("R-060", texto, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportesVacios_SiguenGenerandoArchivosValidos()
    {
        ArchivoReporteDto excel = _service.CrearExcelConsolidado(Array.Empty<RiesgoReporteFilaDto>());
        ArchivoReporteDto pdf = _service.CrearPdfConsolidado(Array.Empty<RiesgoReporteFilaDto>());
        File.WriteAllBytes("C:\\Temp\\fase52-empty.pdf", pdf.Contenido);
        Assert.True(excel.Contenido.Length > 0);
        string pdfTexto = Encoding.Latin1.GetString(pdf.Contenido);
        Assert.Contains("(Sin)", pdfTexto, StringComparison.Ordinal);
        Assert.Contains("(informaci)", pdfTexto, StringComparison.Ordinal);
        Assert.Contains("(ón)", pdfTexto, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportacionesConFiltro_ConservanVersionFechaNumericaYMetadata()
    {
        var filas = CrearFilas(1);
        ArchivoReporteDto excel = _service.CrearExcelConsolidado(filas, "Estado=APROBADA");
        ArchivoReporteDto pdf = _service.CrearPdfConsolidado(filas, "Estado=APROBADA");

        using var stream = new MemoryStream(excel.Contenido);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        string hoja = LeerEntrada(zip, "xl/worksheets/sheet1.xml");
        string estilos = LeerEntrada(zip, "xl/styles.xml");
        string pdfTexto = Encoding.Latin1.GetString(pdf.Contenido);
        Assert.Contains("r=\"M5\" s=\"13\"", hoja, StringComparison.Ordinal);
        Assert.Contains("<v>1</v>", hoja, StringComparison.Ordinal);
        Assert.Contains("Filtros aplicados: Estado=APROBADA", hoja, StringComparison.Ordinal);
        Assert.Contains("dd/mm/yyyy hh:mm", estilos, StringComparison.Ordinal);
        Assert.Contains("FILTROS APLICADOS", pdfTexto, StringComparison.Ordinal);
        Assert.Contains("Estado=APROBADA", pdfTexto, StringComparison.Ordinal);
        Assert.Contains("Versión", pdfTexto, StringComparison.Ordinal);
    }

    [Fact]
    public void ArtefactosReales_FueraDelRepositorio_SeAbrenYConservanContratoInstitucional()
    {
        string directory = Path.Combine(Path.GetTempPath(), "rl-fase52-reportes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var filas = CrearFilas(2);
            ArchivoReporteDto pdf = _service.CrearPdfConsolidado(filas, "Estado=APROBADA");
            ArchivoReporteDto excel = _service.CrearExcelConsolidado(filas, "Estado=APROBADA");
            string pdfPath = Path.Combine(directory, "matriz-consolidada.pdf");
            string excelPath = Path.Combine(directory, "matriz-consolidada.xlsx");
            File.WriteAllBytes(pdfPath, pdf.Contenido);
            File.WriteAllBytes(excelPath, excel.Contenido);

            string pdfTexto = Encoding.Latin1.GetString(File.ReadAllBytes(pdfPath));
            Assert.StartsWith("%PDF-1.4", pdfTexto, StringComparison.Ordinal);
            Assert.Contains("/MediaBox [0 0 841.89 595.28]", pdfTexto, StringComparison.Ordinal);
            Assert.Contains("FILTROS APLICADOS", pdfTexto, StringComparison.Ordinal);
            Assert.Contains("R-001", pdfTexto, StringComparison.Ordinal);
            Assert.Contains("R-002", pdfTexto, StringComparison.Ordinal);
            Assert.EndsWith("%%EOF", pdfTexto, StringComparison.Ordinal);

            using var zip = ZipFile.OpenRead(excelPath);
            string sheet = LeerEntrada(zip, "xl/worksheets/sheet1.xml");
            _ = XDocument.Parse(sheet);
            Assert.Contains("Matriz Consolidada de Riesgos", sheet, StringComparison.Ordinal);
            Assert.Contains("Filtros aplicados: Estado=APROBADA", sheet, StringComparison.Ordinal);
            Assert.Contains("<autoFilter ref=\"A4:M6\"/>", sheet, StringComparison.Ordinal);
            Assert.Contains("state=\"frozen\"", sheet, StringComparison.Ordinal);
            Assert.Contains("orientation=\"landscape\"", sheet, StringComparison.Ordinal);
            Assert.Contains("fitToWidth=\"1\"", sheet, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static IReadOnlyList<RiesgoReporteFilaDto> CrearFilas(int cantidad) =>
        Enumerable.Range(1, cantidad).Select(i => new RiesgoReporteFilaDto
        {
            RiesgoId = i,
            EvaluacionId = i + 100,
            VersionFormularioId = 1,
            CodigoRiesgo = $"R-{i:D3}",
            AreaPrincipal = "Cumplimiento",
            DuenoRiesgo = "Responsable institucional",
            Vri = 7,
            NivelInherente = "ALTO",
            Vrr = 4,
            NivelResidual = "MODERADO",
            RespuestaRiesgo = "MITIGAR",
            EstadoEvaluacion = "APROBADA",
            FechaEvaluacion = new DateTime(2026, 8, 7, 8, 0, 0, DateTimeKind.Utc)
        }).ToArray();

    private static string LeerEntrada(ZipArchive zip, string ruta)
    {
        using Stream stream = zip.GetEntry(ruta)!.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
