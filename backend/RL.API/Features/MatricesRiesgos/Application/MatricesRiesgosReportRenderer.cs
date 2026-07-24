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
        workbook.AddDocumentSheet(
            "Reporte Ejecutivo",
            ConstruirDocumentoExcel(reporte),
            11,
            new decimal[] { 7m, 18m, 18m, 13m, 13m, 14m, 14m, 15m, 15m, 10m, 12m },
            InstitutionalReportOrientation.Landscape,
            2);

        return new MatrizRiesgoExportacionDto
        {
            NombreArchivo = $"Reporte_Matrices_Riesgos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Contenido = workbook.ToBytes()
        };
    }

    private static IReadOnlyList<InstitutionalXlsxDocumentRow> ConstruirDocumentoExcel(MatricesRiesgoReporteDto reporte)
    {
        var rows = new List<InstitutionalXlsxDocumentRow>
        {
            Fila(24m,
                Celda(InstitutionalReportStandard.InstitutionName, 7, InstitutionalXlsxCellStyle.Institution),
                Celda($"{InstitutionalReportStandard.SystemName} · Matrices de Riesgos", 4, InstitutionalXlsxCellStyle.HeaderRight)),
            Fila(32m,
                Celda("REPORTE EJECUTIVO DE MATRICES DE RIESGOS", 7, InstitutionalXlsxCellStyle.Title),
                Celda($"Generado: {reporte.FechaGeneracion.ToString(InstitutionalReportStandard.DateTimeFormat, CultureInfo.InvariantCulture)}", 4, InstitutionalXlsxCellStyle.HeaderRight)),
            Espacio(),
            Seccion("1. FILTROS APLICADOS")
        };

        AgregarTarjetas(rows, ConstruirFiltros(reporte.Filtro)
            .Select(item => (item.Label.ToUpperInvariant(), item.Value))
            .ToArray(), new[] { 4, 4, 3 });

        rows.Add(Espacio());
        rows.Add(Seccion("2. RESUMEN EJECUTIVO"));
        AgregarKpis(rows, new[]
        {
            ("TOTAL MATRICES", reporte.Totales.TotalMatrices.ToString()),
            ("CALCULADAS", reporte.Totales.TotalCalculadas.ToString()),
            ("SIN EVALUAR", reporte.Totales.TotalSinCalculo.ToString()),
            ("CERRADAS", reporte.Totales.TotalCerradas.ToString()),
            ("ALTO / CRÍTICO", reporte.Totales.TotalAltoCritico.ToString()),
            ("PLAN REQUERIDO", reporte.Totales.TotalPlanAccionRequerido.ToString()),
            ("PLANES VENCIDOS", reporte.Totales.TotalPlanesVencidos.ToString())
        });

        rows.Add(Espacio());
        rows.Add(Seccion("3. MATRICES FILTRADAS"));
        rows.Add(Fila(25m,
            Celda("ID", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Sujeto", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Documento", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Tipo", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Estado", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Inherente", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Residual", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Plan", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Fecha", 1, InstitutionalXlsxCellStyle.TableHeader)));
        AgregarFilasMatrices(rows, reporte.MatricesFiltradas.Select(x => new object?[]
        {
            x.MatrizId, Texto(x.NombreSujeto), Texto(x.Documento), Texto(x.SujetoTipo), Texto(x.Estado),
            Resultado(x.PuntajeInherente, x.NivelInherente), Resultado(x.PuntajeResidual, x.NivelResidual),
            x.RequierePlanAccion ? "Sí" : "No", x.FechaEvaluacion.ToString(InstitutionalReportStandard.DateFormat)
        }));

        rows.Add(Espacio());
        rows.Add(Seccion("4. RESULTADOS POR FACTOR"));
        rows.Add(Fila(25m,
            Celda("Factor", 3, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Matrices", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Promedio inherente", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Promedio residual", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Alto / Crítico", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Plan requerido", 1, InstitutionalXlsxCellStyle.TableHeader)));
        AgregarFilas(rows, reporte.PorFactor.Select(x => new (object? Value, int Span, bool Centered)[]
        {
            ($"{Texto(x.FactorCodigo)} - {Texto(x.FactorNombre)}", 3, false),
            (x.TotalMatrices, 1, true),
            (x.PromedioInherente.ToString("0.0000", CultureInfo.InvariantCulture), 2, true),
            (x.PromedioResidual.ToString("0.0000", CultureInfo.InvariantCulture), 2, true),
            (x.TotalAltoCritico, 2, true),
            (x.TotalPlanAccionRequerido, 1, true)
        }));

        rows.Add(Espacio());
        rows.Add(Seccion("5. MAPA DE TRANSICIÓN INHERENTE A RESIDUAL"));
        rows.Add(Fila(25m,
            Celda("Nivel inherente", 3, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Nivel residual", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Total", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Promedio inherente", 3, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Promedio residual", 2, InstitutionalXlsxCellStyle.TableHeader)));
        AgregarFilas(rows, reporte.MapaTransicion.Select(x => new (object? Value, int Span, bool Centered)[]
        {
            (Texto(x.NivelInherente), 3, false),
            (Texto(x.NivelResidual), 2, false),
            (x.Total, 1, true),
            (x.PromedioInherente.ToString("0.0000", CultureInfo.InvariantCulture), 3, true),
            (x.PromedioResidual.ToString("0.0000", CultureInfo.InvariantCulture), 2, true)
        }));

        rows.Add(Espacio());
        rows.Add(Seccion("6. MATRICES ALTO / CRÍTICO"));
        rows.Add(Fila(25m,
            Celda("ID", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Sujeto", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Documento", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Estado", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Residual", 2, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Plan", 1, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Fecha", 1, InstitutionalXlsxCellStyle.TableHeader)));
        AgregarFilas(rows, reporte.MatricesCriticas.Select(x => new (object? Value, int Span, bool Centered)[]
        {
            (x.MatrizId, 1, true),
            (Texto(x.NombreSujeto), 2, false),
            (Texto(x.Documento), 2, true),
            (Texto(x.Estado), 2, true),
            (Resultado(x.PuntajeResidual, x.NivelResidual), 2, true),
            (x.RequierePlanAccion ? "Sí" : "No", 1, true),
            (x.FechaEvaluacion.ToString(InstitutionalReportStandard.DateFormat), 1, true)
        }));

        rows.Add(Espacio());
        rows.Add(Seccion("7. PLANES DE ACCIÓN"));
        rows.Add(Fila(25m,
            Celda("Estado", 5, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Total", 3, InstitutionalXlsxCellStyle.TableHeader),
            Celda("Vencidos", 3, InstitutionalXlsxCellStyle.TableHeader)));
        AgregarFilas(rows, reporte.PlanesAccion.Select(x => new (object? Value, int Span, bool Centered)[]
        {
            (Texto(x.Estado), 5, false),
            (x.Total, 3, true),
            (x.Vencidos, 3, true)
        }));

        rows.Add(Espacio());
        rows.Add(Fila(20m,
            Celda(InstitutionalReportStandard.SystemName, 3),
            Celda(reporte.FechaGeneracion.ToString(InstitutionalReportStandard.DateTimeFormat, CultureInfo.InvariantCulture), 5),
            Celda("Reporte ejecutivo · Hoja única", 3)));
        return rows;
    }

    private static void AgregarTarjetas(
        ICollection<InstitutionalXlsxDocumentRow> rows,
        IReadOnlyList<(string Label, string Value)> items,
        IReadOnlyList<int> spans)
    {
        for (var index = 0; index < items.Count; index += spans.Count)
        {
            var labels = new List<InstitutionalXlsxDocumentCell>();
            var values = new List<InstitutionalXlsxDocumentCell>();
            for (var column = 0; column < spans.Count; column++)
            {
                var itemIndex = index + column;
                var span = spans[column];
                if (itemIndex < items.Count)
                {
                    labels.Add(Celda(items[itemIndex].Label, span, InstitutionalXlsxCellStyle.CardLabel));
                    values.Add(Celda(items[itemIndex].Value, span, InstitutionalXlsxCellStyle.CardValue));
                }
                else
                {
                    labels.Add(Celda(null, span));
                    values.Add(Celda(null, span));
                }
            }
            rows.Add(new InstitutionalXlsxDocumentRow(labels, 18m));
            rows.Add(new InstitutionalXlsxDocumentRow(values, 22m));
        }
    }

    private static void AgregarKpis(
        ICollection<InstitutionalXlsxDocumentRow> rows,
        IReadOnlyList<(string Label, string Value)> items)
    {
        AgregarKpiFila(rows, items.Take(4).ToArray(), new[] { 3, 3, 3, 2 });
        AgregarKpiFila(rows, items.Skip(4).Take(3).ToArray(), new[] { 3, 3, 3, 2 });
    }

    private static void AgregarKpiFila(
        ICollection<InstitutionalXlsxDocumentRow> rows,
        IReadOnlyList<(string Label, string Value)> items,
        IReadOnlyList<int> spans)
    {
        var labels = new List<InstitutionalXlsxDocumentCell>();
        var values = new List<InstitutionalXlsxDocumentCell>();
        for (var index = 0; index < spans.Count; index++)
        {
            if (index < items.Count)
            {
                labels.Add(Celda(items[index].Label, spans[index], InstitutionalXlsxCellStyle.CardLabel));
                values.Add(Celda(items[index].Value, spans[index], InstitutionalXlsxCellStyle.KpiValue));
            }
            else
            {
                labels.Add(Celda(null, spans[index]));
                values.Add(Celda(null, spans[index]));
            }
        }
        rows.Add(new InstitutionalXlsxDocumentRow(labels, 18m));
        rows.Add(new InstitutionalXlsxDocumentRow(values, 30m));
    }

    private static void AgregarFilasMatrices(
        ICollection<InstitutionalXlsxDocumentRow> rows,
        IEnumerable<object?[]> values)
    {
        var materialized = values.ToArray();
        if (materialized.Length == 0)
        {
            rows.Add(Fila(23m, Celda("Sin matrices para los filtros aplicados.", 11, InstitutionalXlsxCellStyle.BorderedBody)));
            return;
        }

        for (var index = 0; index < materialized.Length; index++)
        {
            var row = materialized[index];
            var alternate = index % 2 == 1;
            rows.Add(Fila(23m,
                Celda(row[0], 1, EstiloCuerpo(alternate, true)),
                Celda(row[1], 2, EstiloCuerpo(alternate, false)),
                Celda(row[2], 2, EstiloCuerpo(alternate, true)),
                Celda(row[3], 1, EstiloCuerpo(alternate, true)),
                Celda(row[4], 1, EstiloCuerpo(alternate, true)),
                Celda(row[5], 1, EstiloCuerpo(alternate, true)),
                Celda(row[6], 1, EstiloCuerpo(alternate, true)),
                Celda(row[7], 1, EstiloCuerpo(alternate, true)),
                Celda(row[8], 1, EstiloCuerpo(alternate, true))));
        }
    }

    private static void AgregarFilas(
        ICollection<InstitutionalXlsxDocumentRow> rows,
        IEnumerable<(object? Value, int Span, bool Centered)[]> values)
    {
        var materialized = values.ToArray();
        if (materialized.Length == 0)
        {
            rows.Add(Fila(23m, Celda("Sin registros para mostrar.", 11, InstitutionalXlsxCellStyle.BorderedBody)));
            return;
        }

        for (var index = 0; index < materialized.Length; index++)
        {
            var alternate = index % 2 == 1;
            rows.Add(new InstitutionalXlsxDocumentRow(
                materialized[index]
                    .Select(cell => Celda(cell.Value, cell.Span, EstiloCuerpo(alternate, cell.Centered)))
                    .ToArray(),
                23m));
        }
    }

    private static InstitutionalXlsxCellStyle EstiloCuerpo(bool alternate, bool centered) =>
        alternate
            ? centered ? InstitutionalXlsxCellStyle.AlternateCenteredBody : InstitutionalXlsxCellStyle.AlternateBody
            : centered ? InstitutionalXlsxCellStyle.CenteredBody : InstitutionalXlsxCellStyle.BorderedBody;

    private static InstitutionalXlsxDocumentRow Seccion(string title) =>
        Fila(25m, Celda(title, 11, InstitutionalXlsxCellStyle.Section));

    private static InstitutionalXlsxDocumentRow Espacio() =>
        Fila(8m, Celda(null, 11));

    private static InstitutionalXlsxDocumentRow Fila(
        decimal height,
        params InstitutionalXlsxDocumentCell[] cells) =>
        new(cells, height);

    private static InstitutionalXlsxDocumentCell Celda(
        object? value,
        int span,
        InstitutionalXlsxCellStyle style = InstitutionalXlsxCellStyle.Body) =>
        new(value, span, style);

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
