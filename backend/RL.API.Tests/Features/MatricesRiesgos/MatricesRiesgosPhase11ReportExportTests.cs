using System.IO;
using System.IO.Compression;
using System.Text;
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
        Assert.Contains("Matriz Consolidada", LeerEntrada(zip, "xl/workbook.xml"), StringComparison.Ordinal);
    }

    [Fact]
    public void PdfConsolidado_GeneraPdfPaginadoYCompleto()
    {
        ArchivoReporteDto archivo = _service.CrearPdfConsolidado(CrearFilas(60));

        Assert.Equal("application/pdf", archivo.ContentType);
        Assert.EndsWith(".pdf", archivo.NombreArchivo, StringComparison.OrdinalIgnoreCase);
        string texto = Encoding.ASCII.GetString(archivo.Contenido);
        Assert.StartsWith("%PDF-1.4", texto, StringComparison.Ordinal);
        Assert.Contains("/Count 3", texto, StringComparison.Ordinal);
        Assert.Contains("R-060", texto, StringComparison.Ordinal);
        Assert.EndsWith("%%EOF\n", texto, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportesVacios_SiguenGenerandoArchivosValidos()
    {
        ArchivoReporteDto excel = _service.CrearExcelConsolidado(Array.Empty<RiesgoReporteFilaDto>());
        ArchivoReporteDto pdf = _service.CrearPdfConsolidado(Array.Empty<RiesgoReporteFilaDto>());
        Assert.True(excel.Contenido.Length > 0);
        Assert.Contains("Sin registros para mostrar", Encoding.ASCII.GetString(pdf.Contenido), StringComparison.Ordinal);
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
