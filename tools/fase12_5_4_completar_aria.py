from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HTML = ROOT / "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html"

text = HTML.read_text(encoding="utf-8")
replacements = {
    '<select [ngModel]="reporteFiltro().estado || \'\'" (ngModelChange)="actualizarFiltroReporte(\'estado\', $event)"':
        '<select aria-label="Filtrar reporte por estado" [ngModel]="reporteFiltro().estado || \'\'" (ngModelChange)="actualizarFiltroReporte(\'estado\', $event)"',
    '<select [ngModel]="reporteFiltro().sujetoTipo || \'\'" (ngModelChange)="actualizarFiltroReporte(\'sujetoTipo\', $event)"':
        '<select aria-label="Filtrar reporte por tipo de sujeto" [ngModel]="reporteFiltro().sujetoTipo || \'\'" (ngModelChange)="actualizarFiltroReporte(\'sujetoTipo\', $event)"',
    '<select [ngModel]="reporteFiltro().nivelResidual || \'\'" (ngModelChange)="actualizarFiltroReporte(\'nivelResidual\', $event)"':
        '<select aria-label="Filtrar reporte por nivel residual" [ngModel]="reporteFiltro().nivelResidual || \'\'" (ngModelChange)="actualizarFiltroReporte(\'nivelResidual\', $event)"',
    '<input type="date" [ngModel]="reporteFiltro().fechaInicio || \'\'"':
        '<input type="date" aria-label="Fecha inicial del reporte" [ngModel]="reporteFiltro().fechaInicio || \'\'"'
}

for source, target in replacements.items():
    count = text.count(source)
    if count != 1:
        raise RuntimeError(f"Se esperaba una coincidencia para {source!r}, encontradas: {count}")
    text = text.replace(source, target, 1)

HTML.write_text(text, encoding="utf-8")

for expected in replacements.values():
    if expected not in text:
        raise RuntimeError(f"No se aplicó la etiqueta accesible: {expected}")

print("Etiquetas ARIA de reportería completadas: 4")
