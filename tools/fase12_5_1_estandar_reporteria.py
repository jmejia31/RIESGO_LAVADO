from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOC_ROOT = ROOT / "docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor"
EVIDENCE = DOC_ROOT / "Evidencia_Fase_12_5_1"

KEYWORDS = (
    "pdf", "excel", "xlsx", "xls", "jspdf", "autotable", "questpdf",
    "itext", "closedxml", "epplus", "workbook", "exportar", "reporte"
)
TEXT_EXTENSIONS = {".cs", ".ts", ".html", ".scss", ".css", ".json", ".md", ".ps1", ".csproj"}
MOJIBAKE = re.compile(r"Ã.|Â.|â.|�")


def classify(path: Path) -> str:
    text = path.as_posix().lower()
    if "matricesriesgos" in text or "matrices-riesgos" in text or "matrices de riesgos" in text:
        return "Matrices de Riesgos"
    if "monitoreo" in text or "coincidencia" in text or "listas" in text:
        return "Monitoreo de Listas"
    return "Compartido/Otros"


def inventory() -> dict:
    matches: list[dict] = []
    mojibake: list[dict] = []
    for base in (ROOT / "backend", ROOT / "frontend", ROOT / "docs"):
        if not base.exists():
            continue
        for path in base.rglob("*"):
            if not path.is_file() or path.suffix.lower() not in TEXT_EXTENSIONS:
                continue
            try:
                text = path.read_text(encoding="utf-8")
            except UnicodeDecodeError:
                continue
            lower = text.lower()
            found = sorted({keyword for keyword in KEYWORDS if keyword in lower})
            if found:
                matches.append({
                    "archivo": path.relative_to(ROOT).as_posix(),
                    "modulo": classify(path),
                    "coincidencias": found,
                    "lineas": len(text.splitlines()),
                })
            for line_no, line in enumerate(text.splitlines(), start=1):
                if MOJIBAKE.search(line):
                    mojibake.append({
                        "archivo": path.relative_to(ROOT).as_posix(),
                        "linea": line_no,
                        "texto": line.strip()[:240],
                    })
    return {
        "fase": "12.5.1",
        "objetivo": "Inventario y estándar institucional compartido de reportería",
        "archivos_detectados": len(matches),
        "por_modulo": {
            module: sum(1 for item in matches if item["modulo"] == module)
            for module in ("Monitoreo de Listas", "Matrices de Riesgos", "Compartido/Otros")
        },
        "archivos": sorted(matches, key=lambda item: (item["modulo"], item["archivo"])),
        "caracteres_danados": mojibake,
        "reglas_aprobadas": {
            "fuente_unica_backend": True,
            "pdf_horizontal_y_vertical_mismo_encabezado": True,
            "excel_xlsx_real": True,
            "filas_no_partidas": True,
            "encabezados_tabla_repetidos": True,
            "fechas_no_partidas": True,
            "frontend_no_regenera_documentos": True,
        },
    }


def write_standard_code() -> None:
    path = ROOT / "backend/RL.API/Infrastructure/Reporting/InstitutionalReportStandard.cs"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text('''namespace RL.API.Infrastructure.Reporting;

/// <summary>
/// Fuente única de reglas visuales y de paginación para reportes PDF y Excel del SGRLA-IHSS.
/// Los módulos aportan sus datos; esta clase define identidad, formatos y restricciones comunes.
/// </summary>
public static class InstitutionalReportStandard
{
    public const string InstitutionName = "INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL";
    public const string SystemName = "SGRLA-IHSS";
    public const string DateFormat = "dd/MM/yyyy";
    public const string DateTimeFormat = "dd/MM/yyyy HH:mm";

    public static InstitutionalReportPalette Palette { get; } = new(
        Navy: "#123B63",
        NavyDark: "#0B2E4F",
        HeaderText: "#FFFFFF",
        BodyText: "#1F2937",
        AlternateRow: "#F3F6F9",
        Border: "#D8E0E8",
        Success: "#0F766E",
        Warning: "#D97706",
        Danger: "#B91C1C",
        Muted: "#64748B");

    public static InstitutionalReportOrientation ResolveOrientation(int visibleColumns) =>
        visibleColumns > 8 ? InstitutionalReportOrientation.Landscape : InstitutionalReportOrientation.Portrait;

    public static bool MustMoveRowToNextPage(decimal currentY, decimal rowHeight, decimal footerStartY) =>
        rowHeight > 0 && currentY + rowHeight > footerStartY;

    public static string PageLabel(int page, int totalPages) =>
        totalPages > 0 ? $"Página {page} de {totalPages}" : $"Página {page}";

    public static void ValidateMetadata(InstitutionalReportMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (string.IsNullOrWhiteSpace(metadata.Title))
            throw new ArgumentException("El título del reporte es obligatorio.", nameof(metadata));
        if (string.IsNullOrWhiteSpace(metadata.ModuleName))
            throw new ArgumentException("El módulo del reporte es obligatorio.", nameof(metadata));
    }
}

public sealed record InstitutionalReportPalette(
    string Navy,
    string NavyDark,
    string HeaderText,
    string BodyText,
    string AlternateRow,
    string Border,
    string Success,
    string Warning,
    string Danger,
    string Muted);

public sealed record InstitutionalReportMetadata(
    string Title,
    string ModuleName,
    DateTime GeneratedAt,
    string? Subtitle = null,
    string? GeneratedBy = null);

public sealed record InstitutionalTablePolicy(
    bool KeepRowsTogether = true,
    bool RepeatHeaderOnEveryPage = true,
    bool KeepSectionTitleWithFirstRow = true,
    bool PreventDateWrapping = true,
    bool AlternateRows = true);

public enum InstitutionalReportOrientation
{
    Portrait,
    Landscape
}
''', encoding="utf-8")


def write_tests() -> None:
    path = ROOT / "backend/RL.API.Tests/Infrastructure/Reporting/InstitutionalReportStandardTests.cs"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text('''using RL.API.Infrastructure.Reporting;

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
    [InlineData(6, InstitutionalReportOrientation.Portrait)]
    [InlineData(8, InstitutionalReportOrientation.Portrait)]
    [InlineData(9, InstitutionalReportOrientation.Landscape)]
    [InlineData(14, InstitutionalReportOrientation.Landscape)]
    public void ResolveOrientation_DependsOnVisibleColumns(int columns, InstitutionalReportOrientation expected)
    {
        Assert.Equal(expected, InstitutionalReportStandard.ResolveOrientation(columns));
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
}
''', encoding="utf-8")


def write_documentation(data: dict) -> None:
    DOC_ROOT.mkdir(parents=True, exist_ok=True)
    path = DOC_ROOT / "Fase_12_5_1_Estandar_Institucional_Reporteria_SGRLA_IHSS.md"
    modules = data["por_modulo"]
    damaged = len(data["caracteres_danados"])
    path.write_text(f'''# Fase 12.5.1 — Estándar Institucional Compartido de Reportería

## Estado

**En implementación y validación técnica.** Esta subfase define el patrón común; no sustituye todavía los generadores de Monitoreo de Listas ni Matrices de Riesgos.

## Inventario automatizado

- Archivos relacionados con reportería detectados: **{data["archivos_detectados"]}**.
- Monitoreo de Listas: **{modules["Monitoreo de Listas"]}**.
- Matrices de Riesgos: **{modules["Matrices de Riesgos"]}**.
- Compartidos u otros módulos: **{modules["Compartido/Otros"]}**.
- Posibles textos con caracteres dañados: **{damaged}**.

El detalle se conserva en `Evidencia_Fase_12_5_1/inventario_reporteria.json`.

## Referencias visuales aprobadas

1. El reporte horizontal de Monitoreo de Listas define la identidad base: encabezado azul marino, institución, título, fecha, filtros, resumen, tabla con encabezado azul, filas alternadas y numeración.
2. El reporte vertical de detalle conserva la misma identidad, con secciones numeradas y tablas de continuación.
3. El formato actual de Matrices de Riesgos se reemplazará en 12.5.3; no se reutilizará el diseño de texto plano.

## Reglas institucionales obligatorias

### Encabezado
- Franja azul marino de ancho completo.
- `INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL`.
- Título propio del reporte.
- `SGRLA-IHSS` y fecha/hora de generación.
- Usuario generador cuando aplique.

### Tablas
- Encabezado azul con texto blanco.
- Filas alternadas blanco/gris claro.
- Bordes discretos y texto legible.
- Encabezado repetido en cada página.
- Ninguna fila se divide entre páginas.
- Fechas e identificadores no se parten.
- El título de sección permanece con la primera fila.

### Orientación
- Vertical hasta 8 columnas visibles.
- Horizontal desde 9 columnas visibles o cuando el contenido no sea legible en vertical.
- Ambas orientaciones usan el mismo encabezado, paleta, pie y numeración.

### Pie
- Nombre del sistema.
- Fecha de generación cuando corresponda.
- `Página X de Y` cuando el motor soporte total de páginas.

### Excel
- Salida `.xlsx` real.
- Encabezado institucional, título, fecha y filtros.
- Autofiltro, panel congelado, ajuste de texto y anchos controlados.
- Configuración de impresión, orientación y filas repetidas.
- Sin HTML disfrazado de `.xls`.

## Arquitectura aprobada

La fuente compartida comienza en:

`backend/RL.API/Infrastructure/Reporting/InstitutionalReportStandard.cs`

En 12.5.2 y 12.5.3 se agregarán adaptadores concretos para el motor PDF y el motor Excel realmente utilizados por el repositorio. Monitoreo y Matrices seguirán separados funcionalmente; únicamente compartirán identidad y reglas documentales.

## Política de migración

1. Inventariar generadores actuales.
2. Normalizar Monitoreo como patrón en 12.5.2.
3. Reemplazar Matrices usando el mismo patrón en 12.5.3.
4. Prohibir nuevas implementaciones locales de encabezados, paletas o paginación.
5. Validar visualmente cada PDF renderizado y cada archivo Excel.

## Criterios de salida de 12.5.1

- Inventario generado.
- Estándar documentado.
- Paleta y reglas centralizadas en backend.
- Política de orientación y no partición probada.
- Sin cambios funcionales en Monitoreo o Matrices.
- Quality Gates aprobados.
''', encoding="utf-8")


def main() -> None:
    data = inventory()
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    (EVIDENCE / "inventario_reporteria.json").write_text(
        json.dumps(data, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    write_standard_code()
    write_tests()
    write_documentation(data)
    print(json.dumps({
        "archivos_detectados": data["archivos_detectados"],
        "caracteres_danados": len(data["caracteres_danados"]),
        "estado": "generado"
    }, ensure_ascii=False))


if __name__ == "__main__":
    main()
