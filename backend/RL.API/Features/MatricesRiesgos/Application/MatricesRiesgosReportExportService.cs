using System.Globalization;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Infrastructure.Reporting;

namespace RL.API.Features.MatricesRiesgos.Application;

public interface IMatricesRiesgosReportExportService
{
    ArchivoReporteDto CrearExcelMatrizCompleta(
        IReadOnlyList<RiesgoReporteFilaDto> filas,
        IReadOnlyDictionary<long, InstitutionalEvaluationProjectionDto> proyecciones,
        string? filtros = null);
    ArchivoReporteDto CrearPdfMatrizCompleta(
        IReadOnlyList<RiesgoReporteFilaDto> filas,
        IReadOnlyDictionary<long, InstitutionalEvaluationProjectionDto> proyecciones,
        string? filtros = null);
}

/// <summary>
/// Adaptador de Matrices al estándar institucional compartido de reportería.
/// El backend genera los bytes; la UI solo descarga el archivo resultante.
/// </summary>
public sealed class MatricesRiesgosReportExportService : IMatricesRiesgosReportExportService
{
    public ArchivoReporteDto CrearExcelMatrizCompleta(
        IReadOnlyList<RiesgoReporteFilaDto> filas,
        IReadOnlyDictionary<long, InstitutionalEvaluationProjectionDto> proyecciones,
        string? filtros = null)
    {
        IReadOnlyList<InstitutionalProjectionFieldDefinitionDto> catalog = new InstitutionalProjectionMapper().GetFieldCatalog();
        ValidarProyecciones(filas, proyecciones, catalog);
        var workbook = new InstitutionalXlsxWorkbook();
        workbook.AddSheet(
            "Matriz Consolidada",
            "Matriz Consolidada de Riesgos — Matriz completa 82 campos",
            catalog.Select(field => field.FieldLabel).ToArray(),
            filas.Select(row => MapearProyeccionExcel(proyecciones[row.EvaluacionId], catalog)),
            InstitutionalReportOrientation.Landscape,
            $"Filtros aplicados: {DescribirFiltros(filtros)}");
        return new ArchivoReporteDto(
            workbook.ToBytes(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Matriz_Riesgos_Completa_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
    }

    public ArchivoReporteDto CrearPdfMatrizCompleta(
        IReadOnlyList<RiesgoReporteFilaDto> filas,
        IReadOnlyDictionary<long, InstitutionalEvaluationProjectionDto> proyecciones,
        string? filtros = null)
    {
        IReadOnlyList<InstitutionalProjectionFieldDefinitionDto> catalog = new InstitutionalProjectionMapper().GetFieldCatalog();
        ValidarProyecciones(filas, proyecciones, catalog);
        var document = new InstitutionalPdfDocument(
            "Matriz Consolidada de Riesgos — Matriz completa 82 campos",
            "Matrices de Riesgos",
            InstitutionalReportOrientation.Portrait,
            DateTime.Now);
        document.AddSection("Filtros aplicados");
        document.AddParagraph(DescribirFiltros(filtros));
        foreach (RiesgoReporteFilaDto fila in filas)
        {
            InstitutionalEvaluationProjectionDto projection = proyecciones[fila.EvaluacionId];
            document.AddSection($"{projection.RiskCode} — Evaluación {projection.EvaluationId}");
            document.AddKeyValueGrid(projection.Fields.Select(field =>
                (field.FieldLabel, FormatearValorVisible(field.Value, field.State))), columns: 1, preserveLabelCase: true);
        }
        return new ArchivoReporteDto(document.ToBytes(), "application/pdf",
            $"Matriz_Riesgos_Completa_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
    }

    private static void ValidarProyecciones(
        IReadOnlyList<RiesgoReporteFilaDto> filas,
        IReadOnlyDictionary<long, InstitutionalEvaluationProjectionDto> proyecciones,
        IReadOnlyList<InstitutionalProjectionFieldDefinitionDto> catalog)
    {
        if (catalog.Count != 82 || catalog.Where((field, index) => field.FieldNumber != index + 1).Any())
            throw new InvalidOperationException("El catálogo institucional no contiene exactamente 82 campos ordenados.");
        if (filas.Select(row => row.EvaluacionId).Distinct().Count() != filas.Count)
            throw new InvalidOperationException("La exportación contiene evaluaciones duplicadas.");
        if (filas.Count != proyecciones.Count || filas.Any(row => !proyecciones.ContainsKey(row.EvaluacionId)))
            throw new InvalidOperationException("El conjunto de proyecciones no coincide exactamente con las evaluaciones ya seleccionadas por el consolidado.");

        foreach (RiesgoReporteFilaDto row in filas)
        {
            InstitutionalEvaluationProjectionDto projection = proyecciones[row.EvaluacionId];
            // RiskCode in the consolidated row is a historical projection snapshot, while the
            // canonical 82-field model uses the current risk master code. Risk/evaluation IDs
            // are the stable identity and must match; requiring the display codes to match
            // would reject valid exports after a risk code change.
            if (projection.EvaluationId != row.EvaluacionId || projection.RiskId != row.RiesgoId
                || projection.Fields.Count != catalog.Count)
                throw new InvalidOperationException("La proyección 82 no coincide con el riesgo/evaluación o el conteo canónico.");
            for (int index = 0; index < catalog.Count; index++)
                if (projection.Fields[index].FieldNumber != catalog[index].FieldNumber
                    || !string.Equals(projection.Fields[index].FieldLabel, catalog[index].FieldLabel, StringComparison.Ordinal))
                    throw new InvalidOperationException($"La proyección del riesgo {row.RiesgoId} no respeta el catálogo 82 en la posición {index + 1}.");
        }
    }

    private static IReadOnlyList<object?> MapearProyeccionExcel(
        InstitutionalEvaluationProjectionDto projection,
        IReadOnlyList<InstitutionalProjectionFieldDefinitionDto> catalog)
    {
        if (projection.Fields.Count != catalog.Count) throw new InvalidOperationException("La fila Excel no contiene 82 valores.");
        return projection.Fields.Select(field => ValorExcel(field.Value, field.State)).ToArray();
    }

    private static object ValorExcel(object? value, string state)
    {
        if (value is null) return EstadoSinValor(state);
        if (value is InstitutionalPlanProjectionValueDto[] plans)
            return string.Join(Environment.NewLine, plans.Select(plan => $"{plan.PlanOrdinalIndex}. {FormatearValorVisible(plan.Value, plan.State)}"));
        if (value is string[] strings) return string.Join(Environment.NewLine, strings);
        if (value is System.Text.Json.JsonElement element) return element.ToString();
        return value is string text ? TextoVisibleUtf8Normalizer.Normalizar(text) : value;
    }

    private static string FormatearValorVisible(object? value, string state)
    {
        if (value is null) return EstadoSinValor(state);
        if (value is InstitutionalPlanProjectionValueDto[] plans)
            return string.Join(Environment.NewLine, plans.Select(plan => $"{plan.PlanOrdinalIndex}. {FormatearValorVisible(plan.Value, plan.State)}"));
        if (value is string[] strings) return string.Join(Environment.NewLine, strings);
        if (value is System.Text.Json.JsonElement element) return element.ToString();
        return TextoVisibleUtf8Normalizer.Normalizar(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static string EstadoSinValor(string state) => state switch
    {
        "NOT_APPLICABLE" or "ABSENT_CONTROL_PROJECTED" => "No aplica",
        "LEGIT_BLANK" or "LEGIT_FORMULA_BLANK" or "OPERATIONAL_PENDING" or "CALCULATION_PENDING" => "Sin dato",
        _ => throw new InvalidOperationException($"Estado semántico no reconocido para exportación: {state}.")
    };

    private static string DescribirFiltros(string? filtros) =>
        string.IsNullOrWhiteSpace(filtros) ? "Sin filtros" : filtros.Trim();

}
