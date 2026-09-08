using System.Globalization;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Reporting;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosReportExportService
{
    ArchivoReporteDto CrearExcelConsolidado(IReadOnlyList<RiesgoReporteFilaDto> filas, string? filtros = null);
    ArchivoReporteDto CrearPdfConsolidado(IReadOnlyList<RiesgoReporteFilaDto> filas, string? filtros = null);
}

/// <summary>
/// Adaptador de Matrices al estándar institucional compartido de reportería.
/// El backend genera los bytes; la UI solo descarga el archivo resultante.
/// </summary>
public sealed class MatricesRiesgosReportExportService : IMatricesRiesgosReportExportService
{
    private static readonly string[] Encabezados =
    {
        "Riesgo ID", "Evaluación ID", "Versión", "Código", "Área principal", "Dueño del riesgo",
        "VRI", "Nivel inherente", "VRR", "Nivel residual", "Respuesta", "Estado", "Fecha evaluación"
    };

    private static readonly decimal[] PesosColumnas =
    {
        0.8m, 0.9m, 0.8m, 1.0m, 1.5m, 1.7m, 0.6m, 1.1m, 0.6m, 1.1m, 1.0m, 1.0m, 1.2m
    };

    public ArchivoReporteDto CrearExcelConsolidado(IReadOnlyList<RiesgoReporteFilaDto> filas, string? filtros = null)
    {
        var workbook = new InstitutionalXlsxWorkbook();
        workbook.AddSheet(
            "Matriz Consolidada",
            "Matriz Consolidada de Riesgos",
            Encabezados,
            filas.Select(MapearExcel),
            InstitutionalReportOrientation.Landscape,
            $"Filtros aplicados: {DescribirFiltros(filtros)}");

        return new ArchivoReporteDto(
            workbook.ToBytes(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Matriz_Riesgos_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    public ArchivoReporteDto CrearPdfConsolidado(IReadOnlyList<RiesgoReporteFilaDto> filas, string? filtros = null)
    {
        InstitutionalReportStandard.ValidateMetadata(new InstitutionalReportMetadata(
            "Matriz Consolidada de Riesgos",
            "Matrices de Riesgos",
            DateTime.Now));

        var document = new InstitutionalPdfDocument(
            "Matriz Consolidada de Riesgos",
            "Matrices de Riesgos",
            InstitutionalReportStandard.ResolveOrientation(Encabezados.Length),
            DateTime.Now);
        document.AddSection("Filtros aplicados");
        document.AddParagraph(DescribirFiltros(filtros));
        document.AddSection("Matriz consolidada");
        document.AddTable(Encabezados, filas.Select(MapearPdf), PesosColumnas, 7m);

        return new ArchivoReporteDto(
            document.ToBytes(),
            "application/pdf",
            $"Matriz_Riesgos_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    private static string DescribirFiltros(string? filtros) =>
        string.IsNullOrWhiteSpace(filtros) ? "Sin filtros" : filtros.Trim();

    private static IReadOnlyList<object?> MapearExcel(RiesgoReporteFilaDto fila) => new object?[]
    {
        fila.RiesgoId,
        fila.EvaluacionId,
        fila.VersionFormularioId,
        fila.CodigoRiesgo,
        fila.AreaPrincipal,
        fila.DuenoRiesgo,
        fila.Vri,
        fila.NivelInherente,
        fila.Vrr,
        fila.NivelResidual,
        fila.RespuestaRiesgo,
        fila.EstadoEvaluacion,
        fila.FechaEvaluacion
    };

    private static IReadOnlyList<string> MapearPdf(RiesgoReporteFilaDto fila) => new[]
    {
        fila.RiesgoId.ToString(CultureInfo.InvariantCulture),
        fila.EvaluacionId.ToString(CultureInfo.InvariantCulture),
        fila.VersionFormularioId.ToString(CultureInfo.InvariantCulture),
        fila.CodigoRiesgo,
        fila.AreaPrincipal,
        fila.DuenoRiesgo,
        fila.Vri.ToString(CultureInfo.InvariantCulture),
        fila.NivelInherente,
        fila.Vrr.ToString(CultureInfo.InvariantCulture),
        fila.NivelResidual,
        fila.RespuestaRiesgo,
        fila.EstadoEvaluacion,
        fila.FechaEvaluacion.ToString(InstitutionalReportStandard.DateTimeFormat, CultureInfo.InvariantCulture)
    };
}
