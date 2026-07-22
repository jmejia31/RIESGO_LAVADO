using System.Collections;
using System.Globalization;
using System.Reflection;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Infrastructure.Reporting;

namespace RL.API.Features.MatricesRiesgos.Application;

internal static class MatricesRiesgosReportRenderer
{
    public static MatrizRiesgoExportacionDto ConstruirPdfEjecutivo(MatricesRiesgoReporteDto reporte)
    {
        var pdf = new InstitutionalPdfDocument(
            "REPORTE EJECUTIVO DE MATRICES DE RIESGOS",
            "Matrices de Riesgos",
            InstitutionalReportOrientation.Landscape,
            reporte.FechaGeneracion);

        pdf.AddSection("1. Filtros aplicados");
        pdf.AddKeyValueGrid(ConstruirFiltros(reporte.Filtro), 3);

        pdf.AddSection("2. Resumen ejecutivo");
        pdf.AddKpis(new[]
        {
            ("Total matrices", reporte.Totales.TotalMatrices.ToString()),
            ("Calculadas", reporte.Totales.TotalCalculadas.ToString()),
            ("Sin evaluar", reporte.Totales.TotalSinCalculo.ToString()),
            ("Cerradas", reporte.Totales.TotalCerradas.ToString()),
            ("Alto / Crítico", reporte.Totales.TotalAltoCritico.ToString()),
            ("Plan requerido", reporte.Totales.TotalPlanAccionRequerido.ToString()),
            ("Planes vencidos", reporte.Totales.TotalPlanesVencidos.ToString())
        });

        pdf.AddSection("3. Matrices filtradas");
        pdf.AddTable(
            new[] { "ID", "Sujeto", "Documento", "Tipo", "Estado", "Inherente", "Residual", "Plan", "Fecha" },
            reporte.MatricesFiltradas.Select(x => (IReadOnlyList<string>)new[]
            {
                x.MatrizId.ToString(), Texto(x.NombreSujeto), Texto(x.Documento), Texto(x.SujetoTipo),
                Texto(x.Estado), Resultado(x.PuntajeInherente, x.NivelInherente),
                Resultado(x.PuntajeResidual, x.NivelResidual), x.RequierePlanAccion ? "Sí" : "No",
                x.FechaEvaluacion.ToString(InstitutionalReportStandard.DateFormat)
            }),
            new decimal[] { 0.6m, 2.2m, 1.4m, 1.2m, 1m, 1.2m, 1.2m, 0.7m, 0.9m },
            7.4m);

        pdf.AddSection("4. Resultados por factor");
        pdf.AddTable(
            new[] { "Factor", "Matrices", "Promedio inherente", "Promedio residual", "Alto / Crítico", "Plan requerido" },
            reporte.PorFactor.Select(x => (IReadOnlyList<string>)new[]
            {
                $"{Texto(x.FactorCodigo)} - {Texto(x.FactorNombre)}", x.TotalMatrices.ToString(),
                x.PromedioInherente.ToString("0.0000"), x.PromedioResidual.ToString("0.0000"),
                x.TotalAltoCritico.ToString(), x.TotalPlanAccionRequerido.ToString()
            }),
            new decimal[] { 3m, 1m, 1.4m, 1.4m, 1.2m, 1.2m });

        pdf.AddSection("5. Mapa de transición inherente a residual");
        pdf.AddTable(
            new[] { "Nivel inherente", "Nivel residual", "Total", "Promedio inherente", "Promedio residual" },
            reporte.MapaTransicion.Select(x => (IReadOnlyList<string>)new[]
            {
                Texto(x.NivelInherente), Texto(x.NivelResidual), x.Total.ToString(),
                x.PromedioInherente.ToString("0.0000"), x.PromedioResidual.ToString("0.0000")
            }),
            new decimal[] { 1.5m, 1.5m, 0.8m, 1.4m, 1.4m });

        pdf.AddSection("6. Matrices Alto / Crítico");
        pdf.AddTable(
            new[] { "ID", "Sujeto", "Documento", "Estado", "Residual", "Plan", "Fecha" },
            reporte.MatricesCriticas.Select(x => (IReadOnlyList<string>)new[]
            {
                x.MatrizId.ToString(), Texto(x.NombreSujeto), Texto(x.Documento), Texto(x.Estado),
                Resultado(x.PuntajeResidual, x.NivelResidual), x.RequierePlanAccion ? "Sí" : "No",
                x.FechaEvaluacion.ToString(InstitutionalReportStandard.DateFormat)
            }),
            new decimal[] { 0.7m, 2.5m, 1.5m, 1.2m, 1.4m, 0.8m, 1m });

        pdf.AddSection("7. Planes de acción");
        pdf.AddTable(
            new[] { "Estado", "Total", "Vencidos" },
            reporte.PlanesAccion.Select(x => (IReadOnlyList<string>)new[]
            {
                Texto(x.Estado), x.Total.ToString(), x.Vencidos.ToString()
            }),
            new decimal[] { 2m, 1m, 1m });

        return new MatrizRiesgoExportacionDto
        {
            NombreArchivo = $"Reporte_Ejecutivo_Matrices_Riesgos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            ContentType = "application/pdf",
            Contenido = pdf.ToBytes()
        };
    }

    public static MatrizRiesgoExportacionDto ConstruirExcel(MatricesRiesgoReporteDto reporte)
    {
        var workbook = new InstitutionalXlsxWorkbook();
        var resumen = new List<IReadOnlyList<object?>>();
        resumen.AddRange(ConstruirFiltros(reporte.Filtro)
            .Select(item => (IReadOnlyList<object?>)new object?[] { "Filtro", item.Label, item.Value }));
        resumen.AddRange(new[]
        {
            (IReadOnlyList<object?>)new object?[] { "Indicador", "Total matrices", reporte.Totales.TotalMatrices },
            new object?[] { "Indicador", "Calculadas", reporte.Totales.TotalCalculadas },
            new object?[] { "Indicador", "Sin evaluar", reporte.Totales.TotalSinCalculo },
            new object?[] { "Indicador", "Cerradas", reporte.Totales.TotalCerradas },
            new object?[] { "Indicador", "Alto / Crítico", reporte.Totales.TotalAltoCritico },
            new object?[] { "Indicador", "Plan requerido", reporte.Totales.TotalPlanAccionRequerido },
            new object?[] { "Indicador", "Planes vencidos", reporte.Totales.TotalPlanesVencidos }
        });
        workbook.AddSheet("Resumen", "Resumen ejecutivo de Matrices de Riesgos",
            new[] { "Sección", "Indicador", "Valor" }, resumen, InstitutionalReportOrientation.Portrait);

        workbook.AddSheet("Matrices", "Matrices filtradas",
            new[] { "ID", "Sujeto", "Documento", "Tipo", "Estado", "Puntaje inherente", "Nivel inherente", "Puntaje residual", "Nivel residual", "Plan requerido", "Fecha" },
            reporte.MatricesFiltradas.Select(x => (IReadOnlyList<object?>)new object?[]
            {
                x.MatrizId, x.NombreSujeto, x.Documento, x.SujetoTipo, x.Estado, x.PuntajeInherente,
                x.NivelInherente, x.PuntajeResidual, x.NivelResidual, x.RequierePlanAccion ? "Sí" : "No",
                x.FechaEvaluacion.ToString(InstitutionalReportStandard.DateFormat)
            }), InstitutionalReportOrientation.Landscape);

        workbook.AddSheet("Factores", "Resultados por factor",
            new[] { "Código", "Factor", "Matrices", "Promedio inherente", "Promedio residual", "Alto / Crítico", "Plan requerido" },
            reporte.PorFactor.Select(x => (IReadOnlyList<object?>)new object?[]
            {
                x.FactorCodigo, x.FactorNombre, x.TotalMatrices, x.PromedioInherente, x.PromedioResidual,
                x.TotalAltoCritico, x.TotalPlanAccionRequerido
            }));

        workbook.AddSheet("Mapa transición", "Mapa de transición inherente a residual",
            new[] { "Nivel inherente", "Nivel residual", "Total", "Promedio inherente", "Promedio residual" },
            reporte.MapaTransicion.Select(x => (IReadOnlyList<object?>)new object?[]
            {
                x.NivelInherente, x.NivelResidual, x.Total, x.PromedioInherente, x.PromedioResidual
            }));

        workbook.AddSheet("Matrices críticas", "Matrices Alto / Crítico",
            new[] { "ID", "Sujeto", "Documento", "Tipo", "Estado", "Puntaje inherente", "Nivel inherente", "Puntaje residual", "Nivel residual", "Plan requerido", "Fecha" },
            reporte.MatricesCriticas.Select(x => (IReadOnlyList<object?>)new object?[]
            {
                x.MatrizId, x.NombreSujeto, x.Documento, x.SujetoTipo, x.Estado, x.PuntajeInherente,
                x.NivelInherente, x.PuntajeResidual, x.NivelResidual, x.RequierePlanAccion ? "Sí" : "No",
                x.FechaEvaluacion.ToString(InstitutionalReportStandard.DateFormat)
            }), InstitutionalReportOrientation.Landscape);

        workbook.AddSheet("Planes", "Resumen de planes de acción",
            new[] { "Estado", "Total", "Vencidos" },
            reporte.PlanesAccion.Select(x => (IReadOnlyList<object?>)new object?[] { x.Estado, x.Total, x.Vencidos }));

        return new MatrizRiesgoExportacionDto
        {
            NombreArchivo = $"Reporte_Matrices_Riesgos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Contenido = workbook.ToBytes()
        };
    }

    public static MatrizRiesgoExportacionDto ConstruirFicha(MatrizRiesgoDetalleDto matriz)
    {
        var pdf = new InstitutionalPdfDocument(
            "FICHA INDIVIDUAL DE MATRIZ DE RIESGO",
            "Matrices de Riesgos",
            InstitutionalReportOrientation.Portrait);

        pdf.AddSection("1. Identificación");
        pdf.AddKeyValueGrid(new[]
        {
            ("Matriz ID", Valor(matriz, "MatrizId")),
            ("Sujeto evaluado", Valor(matriz, "NombreSujeto")),
            ("Tipo de sujeto", Valor(matriz, "SujetoTipo")),
            ("Documento", PrimerValor(matriz, "Documento", "SujetoIdExt")),
            ("Estado", Valor(matriz, "Estado")),
            ("Fecha de evaluación", Fecha(Propiedad(matriz, "FechaEvaluacion"))),
            ("Modelo / versión", Valor(matriz, "ModeloVersion")),
            ("Origen de datos", Valor(matriz, "OrigenDatos"))
        }, 2);

        pdf.AddSection("2. Resultado consolidado");
        pdf.AddKpis(new[]
        {
            ("Riesgo inherente", $"{Valor(matriz, "PuntajeInherente")} {Valor(matriz, "NivelInherente")}".Trim()),
            ("Riesgo residual", $"{Valor(matriz, "PuntajeResidual")} {Valor(matriz, "NivelResidual")}".Trim()),
            ("Plan requerido", Booleano(Propiedad(matriz, "RequierePlanAccion")) ? "Sí" : "No")
        });

        pdf.AddSection("3. Variables evaluadas");
        pdf.AddTable(
            new[] { "Factor", "Variable", "Valor", "Puntaje", "Ponderado", "Justificación", "Fuente" },
            Coleccion(matriz, "Detalles").Select(item => (IReadOnlyList<string>)new[]
            {
                $"{Valor(item, "FactorCodigo")} - {Valor(item, "FactorNombre")}".Trim(' ', '-'),
                $"{Valor(item, "VariableCodigo")} - {Valor(item, "VariableNombre")}".Trim(' ', '-'),
                Valor(item, "ValorCapturado"), Valor(item, "Puntaje"), Valor(item, "PuntajePonderado"),
                Valor(item, "Justificacion"), Valor(item, "FuenteDato")
            }),
            new decimal[] { 1.5m, 2m, 1m, 0.7m, 0.8m, 2m, 1m },
            7.3m);

        pdf.AddSection("4. Controles mitigantes");
        pdf.AddTable(
            new[] { "Control", "Factor", "Efectividad", "Responsable", "Estado", "Evidencia" },
            Coleccion(matriz, "Controles").Select(item => (IReadOnlyList<string>)new[]
            {
                Valor(item, "Nombre"), PrimerValor(item, "FactorCodigo", "FactorNombre", "General"),
                $"{Valor(item, "EfectividadPct")}%".Replace("-%", "-"),
                Valor(item, "Responsable"), Valor(item, "Estado"),
                Booleano(Propiedad(item, "TieneEvidencia")) ? "Registrada" : "Pendiente"
            }),
            new decimal[] { 2.2m, 1m, 0.9m, 1.6m, 1m, 1m },
            7.5m);

        pdf.AddSection("5. Resultados vigentes");
        pdf.AddTable(
            new[] { "Tipo", "Versión", "Inherente", "Mitigación", "Residual", "Plan", "Fecha" },
            Coleccion(matriz, "Resultados").Where(item => Booleano(Propiedad(item, "EsVigente")))
                .Select(item => (IReadOnlyList<string>)new[]
                {
                    Valor(item, "TipoResultado"), Valor(item, "VersionCalculo"),
                    $"{Valor(item, "PuntajeInherente")} {Valor(item, "NivelInherente")}".Trim(),
                    $"{Valor(item, "MitigacionPct")}%".Replace("-%", "-"),
                    $"{Valor(item, "PuntajeResidual")} {Valor(item, "NivelResidual")}".Trim(),
                    Booleano(Propiedad(item, "RequierePlanAccion")) ? "Sí" : "No",
                    Fecha(Propiedad(item, "FechaCalculo"))
                }),
            new decimal[] { 1.1m, 1m, 1.4m, 0.9m, 1.4m, 0.7m, 1m },
            7.3m);

        pdf.AddSection("6. Planes de acción");
        pdf.AddTable(
            new[] { "Actividad", "Responsable", "Periodicidad", "Inicio", "Fin", "Estado", "Vencido" },
            Coleccion(matriz, "PlanesAccion").Select(item => (IReadOnlyList<string>)new[]
            {
                Valor(item, "Actividad"), Valor(item, "Responsable"), Valor(item, "Periodicidad"),
                Fecha(Propiedad(item, "FechaInicio")), Fecha(Propiedad(item, "FechaFin")),
                Valor(item, "Estado"), Booleano(Propiedad(item, "Vencido")) ? "Sí" : "No"
            }),
            new decimal[] { 2.4m, 1.4m, 1m, 0.9m, 0.9m, 1m, 0.7m },
            7.2m);

        pdf.AddSection("7. Evidencias");
        pdf.AddTable(
            new[] { "Archivo", "Tipo", "Tamaño", "Vínculo", "Estado", "Fecha" },
            Coleccion(matriz, "Evidencias").Select(item => (IReadOnlyList<string>)new[]
            {
                Valor(item, "NombreOriginal"), PrimerValor(item, "TipoMime", "Extension"),
                FormatearTamano(Propiedad(item, "TamanoBytes")),
                $"Plan {Valor(item, "PlanId")} · Control {Valor(item, "ControlId")}",
                Booleano(Propiedad(item, "Activa")) ? "Activa" : "Inactiva",
                Fecha(Propiedad(item, "FechaCreacion"))
            }),
            new decimal[] { 2m, 1.4m, 0.8m, 1.2m, 0.8m, 1m },
            7.2m);

        return new MatrizRiesgoExportacionDto
        {
            NombreArchivo = $"Ficha_Matriz_Riesgo_{Valor(matriz, "MatrizId")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            ContentType = "application/pdf",
            Contenido = pdf.ToBytes()
        };
    }

    private static IEnumerable<(string Label, string Value)> ConstruirFiltros(MatrizRiesgoReporteFiltroDto filtro)
    {
        yield return ("Búsqueda", Texto(filtro.Buscar, "Todos"));
        yield return ("Estado", Texto(filtro.Estado, "Todos"));
        yield return ("Tipo de sujeto", Texto(filtro.SujetoTipo, "Todos"));
        yield return ("Nivel inherente", Texto(filtro.NivelInherente, "Todos"));
        yield return ("Nivel residual", Texto(filtro.NivelResidual, "Todos"));
        yield return ("Modelo / versión", Texto(filtro.ModeloVersion, "Todos"));
        yield return ("Responsable", Texto(filtro.Responsable, "Todos"));
        yield return ("Fecha inicial", filtro.FechaInicio?.ToString(InstitutionalReportStandard.DateFormat) ?? "Todas");
        yield return ("Fecha final", filtro.FechaFin?.ToString(InstitutionalReportStandard.DateFormat) ?? "Todas");
    }

    private static string Resultado(decimal? puntaje, string? nivel) =>
        puntaje.HasValue ? $"{puntaje.Value:0.00} {Texto(nivel)}".Trim() : "Sin evaluar";

    private static object? Propiedad(object? instance, string name)
    {
        if (instance == null) return null;
        return instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
            ?.GetValue(instance);
    }

    private static IEnumerable<object> Coleccion(object instance, string name)
    {
        var value = Propiedad(instance, name);
        return value is IEnumerable enumerable
            ? enumerable.Cast<object>()
            : Enumerable.Empty<object>();
    }

    private static string Valor(object? instance, string name)
    {
        var value = Propiedad(instance, name);
        if (value == null) return "-";
        if (value is decimal decimalValue) return decimalValue.ToString("0.00", CultureInfo.InvariantCulture);
        if (value is double doubleValue) return doubleValue.ToString("0.00", CultureInfo.InvariantCulture);
        if (value is float floatValue) return floatValue.ToString("0.00", CultureInfo.InvariantCulture);
        return Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() is { Length: > 0 } text ? text : "-";
    }

    private static string PrimerValor(object instance, params string[] names)
    {
        foreach (var name in names)
        {
            if (name == "General") return "General";
            var value = Valor(instance, name);
            if (value != "-") return value;
        }
        return "-";
    }

    private static string Fecha(object? value)
    {
        if (value == null) return "-";
        if (value is DateTime date) return date.ToString(InstitutionalReportStandard.DateFormat);
        if (DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed))
            return parsed.ToString(InstitutionalReportStandard.DateFormat);
        return Texto(Convert.ToString(value, CultureInfo.InvariantCulture), "-");
    }

    private static bool Booleano(object? value) =>
        value is bool boolean ? boolean :
        value is int integer ? integer != 0 :
        bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed) && parsed;

    private static string FormatearTamano(object? value)
    {
        if (!long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var bytes) || bytes < 0)
            return "-";
        return bytes >= 1024 * 1024
            ? $"{bytes / 1024d / 1024d:0.00} MB"
            : $"{bytes / 1024d:0.0} KB";
    }

    private static string Texto(string? value, string fallback = "-") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Replace("\r", " ").Replace("\n", " ").Trim();
}
