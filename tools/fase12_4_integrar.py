from pathlib import Path


def replace_once(path: str, old: str, new: str, marker: str | None = None) -> None:
    file_path = Path(path)
    text = file_path.read_text(encoding="utf-8")
    if marker and marker in text:
        return
    if old not in text:
        raise RuntimeError(f"No se encontró el bloque esperado en {path}: {old[:140]!r}")
    file_path.write_text(text.replace(old, new, 1), encoding="utf-8")


# Contratos de reportería: mismos indicadores y mapa que el dashboard.
dto_path = "backend/RL.API/Features/MatricesRiesgos/Contracts/Reporteria/ReporteriaDtos.cs"
replace_once(
    dto_path,
    "    public List<MatrizRiesgoMapaNivelDto> MapaResidual { get; set; } = new();\n    public List<MatrizRiesgoResumenDto> MatricesFiltradas { get; set; } = new();",
    "    public List<MatrizRiesgoMapaNivelDto> MapaResidual { get; set; } = new();\n    public List<MatrizRiesgoMapaTransicionDto> MapaTransicion { get; set; } = new();\n    public List<MatrizRiesgoResumenDto> MatricesFiltradas { get; set; } = new();",
    "public List<MatrizRiesgoMapaTransicionDto> MapaTransicion"
)
replace_once(
    dto_path,
    "    public int TotalCalculadas { get; set; }\n    public int TotalCerradas { get; set; }",
    "    public int TotalCalculadas { get; set; }\n    public int TotalSinCalculo { get; set; }\n    public int TotalCerradas { get; set; }",
    "public sealed class MatricesRiesgoReporteTotalesDto\n{\n    public int TotalMatrices { get; set; }\n    public int TotalCalculadas { get; set; }\n    public int TotalSinCalculo"
)

# El reporte reutiliza exactamente los totales y la transición del dashboard.
repository_path = "backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs"
replace_once(
    repository_path,
    "        reporte.Totales = await ObtenerTotalesReporteAsync(conn, filtro);\n        reporte.PorEstado =",
    "        reporte.Totales = await ObtenerTotalesReporteAsync(conn, filtro);\n        reporte.Totales.TotalSinCalculo = Math.Max(0, reporte.Totales.TotalMatrices - reporte.Totales.TotalCalculadas);\n        reporte.PorEstado =",
    "reporte.Totales.TotalSinCalculo = Math.Max"
)
replace_once(
    repository_path,
    "        reporte.MapaResidual = await ObtenerMapaNivelReporteAsync(conn, filtro, \"RESIDUAL\");\n        reporte.MatricesFiltradas =",
    "        reporte.MapaResidual = await ObtenerMapaNivelReporteAsync(conn, filtro, \"RESIDUAL\");\n        reporte.MapaTransicion = await ObtenerMapaTransicionDashboardAsync(conn, filtro);\n        reporte.MatricesFiltradas =",
    "reporte.MapaTransicion = await ObtenerMapaTransicionDashboardAsync"
)

# Excel y PDF incorporan matrices sin evaluación y el mapa inherente -> residual.
app_path = "backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosAppService.cs"
replace_once(
    app_path,
    "            new[] { \"Calculadas\", reporte.Totales.TotalCalculadas.ToString() },\n            new[] { \"Cerradas\", reporte.Totales.TotalCerradas.ToString() },",
    "            new[] { \"Calculadas\", reporte.Totales.TotalCalculadas.ToString() },\n            new[] { \"Sin evaluar\", reporte.Totales.TotalSinCalculo.ToString() },\n            new[] { \"Cerradas\", reporte.Totales.TotalCerradas.ToString() },",
    "new[] { \"Sin evaluar\", reporte.Totales.TotalSinCalculo.ToString() }"
)
replace_once(
    app_path,
    "        AgregarTablaHtml(sb, \"Mapa residual persistido\", new[] { \"Nivel\", \"Total\", \"Promedio\" },\n            reporte.MapaResidual.Select(x => new[] { x.Nivel, x.Total.ToString(), x.Promedio.ToString(\"0.0000\") }));\n        AgregarTablaHtml(sb, \"Matrices filtradas\",",
    "        AgregarTablaHtml(sb, \"Mapa residual persistido\", new[] { \"Nivel\", \"Total\", \"Promedio\" },\n            reporte.MapaResidual.Select(x => new[] { x.Nivel, x.Total.ToString(), x.Promedio.ToString(\"0.0000\") }));\n        AgregarTablaHtml(sb, \"Mapa de transición inherente a residual\", new[] { \"Nivel inherente\", \"Nivel residual\", \"Total\", \"Promedio inherente\", \"Promedio residual\" },\n            reporte.MapaTransicion.Select(x => new[] { x.NivelInherente, x.NivelResidual, x.Total.ToString(), x.PromedioInherente.ToString(\"0.0000\"), x.PromedioResidual.ToString(\"0.0000\") }));\n        AgregarTablaHtml(sb, \"Matrices filtradas\",",
    "AgregarTablaHtml(sb, \"Mapa de transición inherente a residual\""
)
replace_once(
    app_path,
    "            $\" - Calculadas: {reporte.Totales.TotalCalculadas}\",\n            $\" - Cerradas: {reporte.Totales.TotalCerradas}\",",
    "            $\" - Calculadas: {reporte.Totales.TotalCalculadas}\",\n            $\" - Sin evaluar: {reporte.Totales.TotalSinCalculo}\",\n            $\" - Cerradas: {reporte.Totales.TotalCerradas}\",",
    "$\" - Sin evaluar: {reporte.Totales.TotalSinCalculo}\""
)
replace_once(
    app_path,
    "        lineas.Add(\"\");\n        lineas.Add(\"7. MATRICES FILTRADAS\");",
    "        lineas.Add(\"\");\n        lineas.Add(\"7. MAPA DE TRANSICIÓN INHERENTE A RESIDUAL\");\n        lineas.AddRange(reporte.MapaTransicion.Select(x => $\" - {QuitarSaltos(x.NivelInherente)} -> {QuitarSaltos(x.NivelResidual)} | Total {x.Total} | Promedio inherente {x.PromedioInherente:0.0000} | Promedio residual {x.PromedioResidual:0.0000}\"));\n        lineas.Add(\"\");\n        lineas.Add(\"8. MATRICES FILTRADAS\");",
    "7. MAPA DE TRANSICIÓN INHERENTE A RESIDUAL"
)
replace_once(app_path, "        lineas.Add(\"8. RESULTADOS POR FACTOR\");", "        lineas.Add(\"9. RESULTADOS POR FACTOR\");", "9. RESULTADOS POR FACTOR")
replace_once(app_path, "        lineas.Add(\"9. MATRICES ALTO / CRÍTICO\");", "        lineas.Add(\"10. MATRICES ALTO / CRÍTICO\");", "10. MATRICES ALTO / CRÍTICO")
replace_once(app_path, "        lineas.Add(\"10. PLANES DE ACCIÓN\");", "        lineas.Add(\"11. PLANES DE ACCIÓN\");", "11. PLANES DE ACCIÓN")

# Contrato Angular alineado con la respuesta final del backend.
models_path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos.models.ts"
replace_once(
    models_path,
    "    totalCalculadas: number;\n    totalCerradas: number;",
    "    totalCalculadas: number;\n    totalSinCalculo: number;\n    totalCerradas: number;",
    "  totales: {\n    totalMatrices: number;\n    totalCalculadas: number;\n    totalSinCalculo"
)
replace_once(
    models_path,
    "  mapaResidual: { nivel: string; total: number; promedio: number }[];\n  matricesFiltradas:",
    "  mapaResidual: { nivel: string; total: number; promedio: number }[];\n  mapaTransicion: MatrizRiesgoMapaTransicion[];\n  matricesFiltradas:",
    "  mapaTransicion: MatrizRiesgoMapaTransicion[];"
)

# Todos los tipos de sujeto administrados por backend deben aparecer en filtros/captura.
component_path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts"
replace_once(
    component_path,
    "    { valor: 'EMPLEADO', texto: 'Empleado' },\n    { valor: 'INSTITUCIONAL', texto: 'Institucional' }",
    "    { valor: 'EMPLEADO', texto: 'Empleado' },\n    { valor: 'AREA', texto: 'Área' },\n    { valor: 'PROCESO', texto: 'Proceso' },\n    { valor: 'CASO_POSITIVO', texto: 'Caso positivo' },\n    { valor: 'INSTITUCIONAL', texto: 'Institucional' }",
    "{ valor: 'CASO_POSITIVO', texto: 'Caso positivo' }"
)
replace_once(
    component_path,
    "      next: blob => {\n        if (formato === 'EXCEL') {\n          this.generarExcelReporte();\n        } else {\n          this.generarPdfReporte();\n        }\n        this.mensaje.set(`Reporte ${formato} generado correctamente.`);",
    "      next: blob => {\n        this.descargarArchivoReporte(blob, formato);\n        this.mensaje.set(`Reporte ${formato} generado correctamente.`);",
    "this.descargarArchivoReporte(blob, formato);"
)
replace_once(
    component_path,
    "  cargarMatrices(): void {",
    "  private descargarArchivoReporte(blob: Blob, formato: 'EXCEL' | 'PDF'): void {\n    const extension = formato === 'PDF' ? 'pdf' : 'xls';\n    const url = URL.createObjectURL(blob);\n    const link = document.createElement('a');\n    link.href = url;\n    link.download = `Reporte_Matrices_Riesgos_${this.fechaArchivo()}.${extension}`;\n    document.body.appendChild(link);\n    link.click();\n    link.remove();\n    URL.revokeObjectURL(url);\n  }\n\n  cargarMatrices(): void {",
    "private descargarArchivoReporte(blob: Blob"
)

# Pruebas frontend: la salida descargada debe ser exactamente el blob del backend.
frontend_spec_path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.spec.ts"
old_frontend_test = """  it.each([\n    ['EXCEL', 'generarExcelReporte'],\n    ['PDF', 'generarPdfReporte']\n  ] as const)('exporta un reporte %s usando el generador correspondiente', (formato, metodo) => {\n    const generar = vi.fn();\n    (component as any)[metodo] = generar;\n    component.reporteFiltro.set({ estado: 'APROBADA' });\n    const archivo = new Blob(['reporte'], { type: 'application/octet-stream' });\n    service['exportarReporte'].mockReturnValue(of(archivo));\n\n    component.exportarReporte(formato);\n\n    expect(service['exportarReporte']).toHaveBeenCalledWith({ estado: 'APROBADA' }, formato);\n    expect(generar).toHaveBeenCalledOnce();\n    expect(component.mensaje()).toBe(`Reporte ${formato} generado correctamente.`);\n    expect(component.guardando()).toBe(false);\n  });"""
new_frontend_test = """  it.each(['EXCEL', 'PDF'] as const)('exporta un reporte %s descargando exactamente el archivo del backend', formato => {\n    const descargar = vi.spyOn(component as any, 'descargarArchivoReporte').mockImplementation(() => undefined);\n    component.reporteFiltro.set({ estado: 'APROBADA' });\n    const archivo = new Blob(['reporte'], { type: 'application/octet-stream' });\n    service['exportarReporte'].mockReturnValue(of(archivo));\n\n    component.exportarReporte(formato);\n\n    expect(service['exportarReporte']).toHaveBeenCalledWith({ estado: 'APROBADA' }, formato);\n    expect(descargar).toHaveBeenCalledWith(archivo, formato);\n    expect(component.mensaje()).toBe(`Reporte ${formato} generado correctamente.`);\n    expect(component.guardando()).toBe(false);\n  });\n\n  it('expone todos los tipos de sujeto permitidos por backend', () => {\n    expect(component.tiposSujeto.map(item => item.valor)).toEqual([\n      'PROVEEDOR', 'CLIENTE_PATRONO', 'EMPLEADO', 'AREA', 'PROCESO', 'CASO_POSITIVO', 'INSTITUCIONAL'\n    ]);\n  });"""
replace_once(frontend_spec_path, old_frontend_test, new_frontend_test, "descargando exactamente el archivo del backend")

# Pruebas backend: firma, extensión y secciones nuevas de la exportación.
backend_spec_path = "backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosApplicationTests.cs"
replace_once(backend_spec_path, "using Newtonsoft.Json;\n", "using Newtonsoft.Json;\nusing System.Text;\n", "using System.Text;")
replace_once(
    backend_spec_path,
    "        var reporte = new MatricesRiesgoReporteDto();",
    "        var reporte = new MatricesRiesgoReporteDto\n        {\n            Totales = new MatricesRiesgoReporteTotalesDto { TotalMatrices = 3, TotalCalculadas = 2, TotalSinCalculo = 1 },\n            MapaTransicion = new List<MatrizRiesgoMapaTransicionDto>\n            {\n                new() { NivelInherente = \"ALTO\", NivelResidual = \"MEDIO\", Total = 2, PromedioInherente = 4.5m, PromedioResidual = 2.5m }\n            }\n        };",
    "TotalSinCalculo = 1"
)
replace_once(
    backend_spec_path,
    "        Assert.NotEmpty(result.Data.Contenido);\n        Assert.Equal(\"APROBADA\", filtro.Estado);",
    "        Assert.NotEmpty(result.Data.Contenido);\n        Assert.EndsWith(formato == \"PDF\" ? \".pdf\" : \".xls\", result.Data.NombreArchivo, StringComparison.OrdinalIgnoreCase);\n        if (formato == \"PDF\")\n        {\n            Assert.StartsWith(\"%PDF-1.4\", Encoding.Latin1.GetString(result.Data.Contenido));\n        }\n        else\n        {\n            var contenido = Encoding.UTF8.GetString(result.Data.Contenido);\n            Assert.Contains(\"Sin evaluar\", contenido, StringComparison.OrdinalIgnoreCase);\n            Assert.Contains(\"Mapa de transición inherente a residual\", contenido, StringComparison.OrdinalIgnoreCase);\n            Assert.Contains(\"ALTO\", contenido, StringComparison.OrdinalIgnoreCase);\n            Assert.Contains(\"MEDIO\", contenido, StringComparison.OrdinalIgnoreCase);\n        }\n        Assert.Equal(\"APROBADA\", filtro.Estado);",
    "Assert.EndsWith(formato == \"PDF\""
)

print("Integración de Fase 12.4 aplicada o ya presente.")
