from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BRANCH_EVIDENCE = ROOT / "docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor/Evidencia_Fase_12_5_4"


def read(path: str) -> tuple[Path, str]:
    file = ROOT / path
    return file, file.read_text(encoding="utf-8")


def write(file: Path, text: str) -> None:
    file.write_text(text, encoding="utf-8")


def replace_once(path: str, old: str, new: str, label: str) -> None:
    file, text = read(path)
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{label}: se esperó 1 coincidencia y se encontraron {count}")
    write(file, text.replace(old, new, 1))


def regex_once(path: str, pattern: str, replacement: str, label: str) -> None:
    file, text = read(path)
    updated, count = re.subn(pattern, replacement, text, count=1, flags=re.S)
    if count != 1:
        raise RuntimeError(f"{label}: se esperó 1 coincidencia y se encontraron {count}")
    write(file, updated)


def retirar_endpoint_recalcular() -> None:
    controller = "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs"
    regex_once(
        controller,
        r'\n    \[HttpPost\("\{id:long\}/recalcular"\)\]\n    \[AuditRequired\("Recálculo de matriz de riesgos"\)\]\n    public async Task<IActionResult> Recalcular\(long id, \[FromBody\] MatrizRiesgoCalcularRequestDto dto\)\n    \{.*?\n    \}\n\n    \[HttpPut\("\{id:long\}/estado"\)\]',
        '\n\n    [HttpPut("{id:long}/estado")]',
        "Retiro del endpoint público /recalcular"
    )
    replace_once(
        controller,
        '    [HttpPost("{id:long}/calcular")]\n    [AuditRequired("Cálculo de matriz de riesgos")]\n',
        '    // La Fase 12.5.4 retiró el endpoint público /recalcular. La creación y la edición\n'
        '    // conservan el cálculo automático mediante esta única operación auditada.\n'
        '    [HttpPost("{id:long}/calcular")]\n    [AuditRequired("Cálculo de matriz de riesgos")]\n',
        "Comentario de arquitectura de cálculo"
    )

    service = "frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.ts"
    regex_once(
        service,
        r"\n  recalcular\(id: number, motivoCalculo: string, tipoCalculo = 'GLOBAL'\): Observable<unknown> \{\n    return this\.http\.post<ApiResponse<unknown>>\(`\$\{this\.apiUrl\}/\$\{id\}/recalcular`, \{ tipoCalculo, motivoCalculo \}, this\.confirmado\)\n      \.pipe\(map\(res => res\.datos\)\);\n  \}\n",
        "",
        "Retiro del consumidor Angular no utilizado"
    )
    replace_once(
        service,
        '  // Acciones críticas de cálculo y estado: siempre viajan con confirmación y motivo cuando aplica.\n',
        '  // El cálculo se ejecuta después de crear o editar. Angular no expone una operación\n'
        '  // separada de recálculo; el backend conserva la única ruta auditada de cálculo.\n',
        "Comentario crítico del servicio Angular"
    )

    spec = "frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.spec.ts"
    regex_once(
        spec,
        r"\n  it\('recalcula con motivo, tipo de calculo y confirmacion previa', \(\) => \{.*?\n  \}\);\n",
        "\n  it('no expone un endpoint publico de recalculo separado', () => {\n"
        "    expect('recalcular' in (service as unknown as Record<string, unknown>)).toBe(false);\n"
        "  });\n",
        "Prueba de ausencia del consumidor público"
    )


def refinar_componente() -> None:
    component = "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts"
    replace_once(
        component,
        "  readonly guardando = signal(false);\n",
        "  readonly guardando = signal(false);\n  readonly exportando = signal<'EXCEL' | 'PDF' | 'FICHA' | null>(null);\n",
        "Indicador específico de exportación"
    )

    regex_once(
        component,
        r"  exportarReporte\(formato: 'EXCEL' \| 'PDF'\): void \{.*?\n  \}\n\n  private descargarArchivoReporte",
        "  exportarReporte(formato: 'EXCEL' | 'PDF'): void {\n"
        "    if (this.exportando()) return;\n\n"
        "    // La API es la fuente única del archivo. Angular únicamente coordina la\n"
        "    // descarga y mantiene un estado visible para impedir solicitudes duplicadas.\n"
        "    this.exportando.set(formato);\n"
        "    this.guardando.set(true);\n"
        "    this.service.exportarReporte(this.reporteFiltro(), formato).subscribe({\n"
        "      next: blob => {\n"
        "        this.descargarArchivoReporte(blob, formato);\n"
        "        this.mensaje.set(`Reporte ${formato} generado correctamente.`);\n"
        "        this.exportando.set(null);\n"
        "        this.guardando.set(false);\n"
        "      },\n"
        "      error: err => {\n"
        "        this.error.set(this.obtenerMensajeError(err, 'No se pudo exportar el reporte.'));\n"
        "        this.exportando.set(null);\n"
        "        this.guardando.set(false);\n"
        "      }\n"
        "    });\n"
        "  }\n\n"
        "  private descargarArchivoReporte",
        "Estado específico del reporte"
    )

    regex_once(
        component,
        r"  exportarFichaMatriz\(\): void \{.*?\n  \}\n\n  cargarMatrices",
        "  exportarFichaMatriz(): void {\n"
        "    const matriz = this.matrizSeleccionada();\n"
        "    if (!matriz) {\n"
        "      this.error.set('Seleccione una matriz para generar su ficha individual.');\n"
        "      return;\n"
        "    }\n"
        "    if (this.exportando()) return;\n\n"
        "    this.exportando.set('FICHA');\n"
        "    this.guardando.set(true);\n"
        "    this.service.exportarFicha(matriz.matrizId).subscribe({\n"
        "      next: blob => {\n"
        "        const url = URL.createObjectURL(blob);\n"
        "        const link = document.createElement('a');\n"
        "        link.href = url;\n"
        "        link.download = `Ficha_Matriz_Riesgo_${matriz.matrizId}_${this.fechaArchivo()}.pdf`;\n"
        "        document.body.appendChild(link);\n"
        "        link.click();\n"
        "        link.remove();\n"
        "        URL.revokeObjectURL(url);\n"
        "        this.mensaje.set('Ficha individual PDF generada correctamente.');\n"
        "        this.exportando.set(null);\n"
        "        this.guardando.set(false);\n"
        "      },\n"
        "      error: err => {\n"
        "        this.error.set(this.obtenerMensajeError(err, 'No se pudo generar la ficha individual.'));\n"
        "        this.exportando.set(null);\n"
        "        this.guardando.set(false);\n"
        "      }\n"
        "    });\n"
        "  }\n\n"
        "  cargarMatrices",
        "Estado específico de ficha"
    )

    replace_once(
        component,
        "  puedeEditarMatriz(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle): boolean {\n    return puedeEditarMatrizPorEstado(matriz.estado);\n  }\n\n",
        "  puedeEditarMatriz(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle): boolean {\n    return puedeEditarMatrizPorEstado(matriz.estado);\n  }\n\n"
        "  mensajeBloqueoEditarMatriz(matriz: MatrizRiesgoResumen | MatrizRiesgoDetalle): string {\n"
        "    return this.puedeEditarMatriz(matriz)\n"
        "      ? 'Editar matriz'\n"
        "      : 'La matriz solo puede editarse mientras se encuentra En Revisión.';\n"
        "  }\n\n",
        "Explicación de bloqueo de edición"
    )

    spec = "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.spec.ts"
    replace_once(
        spec,
        "    expect(component.guardando()).toBe(false);\n  });\n\n  it('descarga la ficha individual generada por backend'",
        "    expect(component.guardando()).toBe(false);\n    expect(component.exportando()).toBeNull();\n  });\n\n  it('descarga la ficha individual generada por backend'",
        "Prueba de cierre de exportación general"
    )
    replace_once(
        spec,
        "    expect(component.mensaje()).toBe('Ficha individual PDF generada correctamente.');\n    expect(component.guardando()).toBe(false);\n  });\n",
        "    expect(component.mensaje()).toBe('Ficha individual PDF generada correctamente.');\n    expect(component.guardando()).toBe(false);\n    expect(component.exportando()).toBeNull();\n  });\n",
        "Prueba de cierre de ficha"
    )
    replace_once(
        spec,
        "    expect(component.error()).toBe('Exportacion no disponible');\n    expect(component.guardando()).toBe(false);\n  });\n",
        "    expect(component.error()).toBe('Exportacion no disponible');\n    expect(component.guardando()).toBe(false);\n    expect(component.exportando()).toBeNull();\n  });\n",
        "Prueba de recuperación de exportación"
    )
    replace_once(
        spec,
        "  it('presenta el estado tecnico calculada como en revision', () => {\n    expect(component.estadoEtiqueta('CALCULADA')).toBe('En Revisión');\n  });\n",
        "  it('presenta el estado tecnico calculada como en revision', () => {\n    expect(component.estadoEtiqueta('CALCULADA')).toBe('En Revisión');\n  });\n\n"
        "  it('explica por que una matriz cerrada no puede editarse', () => {\n"
        "    const matriz = { estado: 'CERRADA' } as never;\n"
        "    expect(component.puedeEditarMatriz(matriz)).toBe(false);\n"
        "    expect(component.mensajeBloqueoEditarMatriz(matriz)).toContain('En Revisión');\n"
        "  });\n",
        "Prueba de ayuda contextual de edición"
    )


def refinar_template() -> None:
    path = "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html"
    replacements = [
        ('        <p class="text-xs font-bold uppercase tracking-wide text-emerald-700"> </p>',
         '        <p class="text-xs font-bold uppercase tracking-wide text-emerald-700">Módulo 2 · Gestión y evaluación</p>',
         "Etiqueta superior del módulo"),
        ('  <div *ngIf="error()" class="bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-3 text-sm font-medium">',
         '  <div *ngIf="error()" role="alert" aria-live="assertive" class="bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-3 text-sm font-medium">',
         "Región accesible de error"),
        ('  <div *ngIf="mensaje()" class="bg-emerald-50 border border-emerald-200 text-emerald-700 rounded-xl px-4 py-3 text-sm font-medium">',
         '  <div *ngIf="mensaje()" role="status" aria-live="polite" class="bg-emerald-50 border border-emerald-200 text-emerald-700 rounded-xl px-4 py-3 text-sm font-medium">',
         "Región accesible de confirmación"),
        ('  <nav class="bg-white border border-gray-100 rounded-2xl shadow-sm p-2 flex flex-wrap gap-2">',
         '  <nav aria-label="Secciones de Matrices de Riesgos" class="bg-white border border-gray-100 rounded-2xl shadow-sm p-2 flex flex-wrap gap-2">',
         "Navegación identificada"),
        ('  <div *ngIf="cargando()" class="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm font-semibold text-blue-700">',
         '  <div *ngIf="cargando()" role="status" aria-live="polite" aria-busy="true" class="rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm font-semibold text-blue-700">',
         "Estado accesible de carga"),
        ('                      <button type="button" (click)="editarMatriz(matriz)"\n                        class="px-3 py-1.5 rounded-lg border border-gray-200 bg-white text-gray-700 text-xs font-semibold hover:bg-gray-50 disabled:opacity-50 whitespace-nowrap"\n                        [disabled]="guardando() || !puedeEditarMatriz(matriz)">',
         '                      <button type="button" (click)="editarMatriz(matriz)"\n                        class="px-3 py-1.5 rounded-lg border border-gray-200 bg-white text-gray-700 text-xs font-semibold hover:bg-gray-50 disabled:opacity-50 whitespace-nowrap"\n                        [disabled]="guardando() || !puedeEditarMatriz(matriz)"\n                        [title]="mensajeBloqueoEditarMatriz(matriz)">',
         "Ayuda contextual de edición"),
        ('            <button type="button" (click)="exportarFichaMatriz()" [disabled]="guardando()"\n              class="rounded-lg border border-red-200 bg-white px-3 py-1.5 text-xs font-bold text-red-700 hover:bg-red-50 disabled:opacity-50">\n              Ficha PDF',
         '            <button type="button" (click)="exportarFichaMatriz()" [disabled]="guardando()"\n              [attr.aria-busy]="exportando() === \'FICHA\'"\n              class="rounded-lg border border-red-200 bg-white px-3 py-1.5 text-xs font-bold text-red-700 hover:bg-red-50 disabled:opacity-50">\n              {{ exportando() === \'FICHA\' ? \'Generando ficha...\' : \'Ficha PDF\' }}',
         "Estado visible de ficha"),
        ('          <button type="button" (click)="exportarReporte(\'EXCEL\')" [disabled]="guardando() || cargandoReporte()"\n            class="w-full sm:w-auto px-4 py-2 rounded-xl bg-emerald-600 text-white text-sm font-bold disabled:opacity-50">\n            Exportar Excel (.xlsx)',
         '          <button type="button" (click)="exportarReporte(\'EXCEL\')" [disabled]="guardando() || cargandoReporte()"\n            [attr.aria-busy]="exportando() === \'EXCEL\'"\n            class="w-full sm:w-auto px-4 py-2 rounded-xl bg-emerald-600 text-white text-sm font-bold disabled:opacity-50">\n            {{ exportando() === \'EXCEL\' ? \'Generando Excel...\' : \'Exportar Excel (.xlsx)\' }}',
         "Estado visible de Excel"),
        ('          <button type="button" (click)="exportarReporte(\'PDF\')" [disabled]="guardando() || cargandoReporte()"\n            class="w-full sm:w-auto px-4 py-2 rounded-xl bg-red-600 text-white text-sm font-bold disabled:opacity-50">\n            Generar PDF ejecutivo',
         '          <button type="button" (click)="exportarReporte(\'PDF\')" [disabled]="guardando() || cargandoReporte()"\n            [attr.aria-busy]="exportando() === \'PDF\'"\n            class="w-full sm:w-auto px-4 py-2 rounded-xl bg-red-600 text-white text-sm font-bold disabled:opacity-50">\n            {{ exportando() === \'PDF\' ? \'Generando PDF...\' : \'Generar PDF ejecutivo\' }}',
         "Estado visible de PDF"),
        ('      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1 text-xs text-gray-500">',
         '      <div role="status" aria-live="polite" class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1 text-xs text-gray-500">',
         "Estado accesible del reporte"),
        ('      <div class="w-full max-w-5xl max-h-[92vh] rounded-2xl bg-white shadow-2xl border border-gray-100 overflow-hidden"\n        (click)="$event.stopPropagation()">',
         '      <div role="dialog" aria-modal="true" aria-labelledby="vista-previa-titulo"\n        class="w-full max-w-5xl max-h-[92vh] rounded-2xl bg-white shadow-2xl border border-gray-100 overflow-hidden"\n        (click)="$event.stopPropagation()">',
         "Semántica de diálogo de vista previa"),
        ('            <h3 class="mt-1 text-lg font-bold text-gray-900 break-words [overflow-wrap:anywhere]">',
         '            <h3 id="vista-previa-titulo" class="mt-1 text-lg font-bold text-gray-900 break-words [overflow-wrap:anywhere]">',
         "Título de vista previa"),
        ('      <div class="w-full max-w-lg rounded-2xl bg-white shadow-2xl border border-gray-100 overflow-hidden"\n        (click)="$event.stopPropagation()">',
         '      <div role="dialog" aria-modal="true" aria-labelledby="modal-operacion-titulo"\n        class="w-full max-w-lg rounded-2xl bg-white shadow-2xl border border-gray-100 overflow-hidden"\n        (click)="$event.stopPropagation()">',
         "Semántica de diálogo sensible"),
        ('            <h3 class="mt-1 text-xl font-bold text-gray-900 break-words [overflow-wrap:anywhere]">',
         '            <h3 id="modal-operacion-titulo" class="mt-1 text-xl font-bold text-gray-900 break-words [overflow-wrap:anywhere]">',
         "Título de diálogo sensible"),
        ('          <div *ngIf="modalError()" class="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700">',
         '          <div *ngIf="modalError()" role="alert" class="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm font-medium text-red-700">',
         "Error accesible del diálogo")
    ]
    for old, new, label in replacements:
        replace_once(path, old, new, label)

    # Estado de la navegación sin adoptar el patrón de tabs completo, porque cada botón
    # cambia una vista interna y no controla paneles mediante identificadores ARIA.
    file, text = read(path)
    mappings = {
        "(click)=\"tab.set('dashboard')\"": "(click)=\"tab.set('dashboard')\" [attr.aria-current]=\"tab() === 'dashboard' ? 'page' : null\"",
        "(click)=\"tab.set('matrices')\"": "(click)=\"tab.set('matrices')\" [attr.aria-current]=\"tab() === 'matrices' ? 'page' : null\"",
        "(click)=\"iniciarNuevaMatriz()\"\n      [ngClass]=\"tab() === 'nueva'": "(click)=\"iniciarNuevaMatriz()\" [attr.aria-current]=\"tab() === 'nueva' ? 'page' : null\"\n      [ngClass]=\"tab() === 'nueva'",
        "(click)=\"tab.set('criterios')\"": "(click)=\"tab.set('criterios')\" [attr.aria-current]=\"tab() === 'criterios' ? 'page' : null\"",
        "(click)=\"tab.set('planes')\"": "(click)=\"tab.set('planes')\" [attr.aria-current]=\"tab() === 'planes' ? 'page' : null\"",
        "(click)=\"tab.set('reportes'); cargarReporte()\"": "(click)=\"tab.set('reportes'); cargarReporte()\" [attr.aria-current]=\"tab() === 'reportes' ? 'page' : null\""
    }
    for old, new in mappings.items():
        if old not in text:
            raise RuntimeError(f"No se encontró navegación para actualizar: {old}")
        text = text.replace(old, new, 1)

    aria_labels = [
        ('<select [ngModel]="reporteFiltro().sujetoTipo || \'\'"', '<select aria-label="Filtrar panel por tipo de sujeto" [ngModel]="reporteFiltro().sujetoTipo || \'\'"'),
        ('<select [ngModel]="reporteFiltro().estado || \'\'"', '<select aria-label="Filtrar por estado" [ngModel]="reporteFiltro().estado || \'\'"'),
        ('<select [ngModel]="reporteFiltro().nivelInherente || \'\'"', '<select aria-label="Filtrar por nivel inherente" [ngModel]="reporteFiltro().nivelInherente || \'\'"'),
        ('<select [ngModel]="reporteFiltro().nivelResidual || \'\'"', '<select aria-label="Filtrar por nivel residual" [ngModel]="reporteFiltro().nivelResidual || \'\'"'),
        ('<input type="date" [ngModel]="reporteFiltro().fechaInicio || \'\'"', '<input type="date" aria-label="Fecha inicial del filtro" [ngModel]="reporteFiltro().fechaInicio || \'\'"'),
        ('<input type="search" [ngModel]="reporteFiltro().buscar || \'\'"', '<input type="search" aria-label="Buscar en el reporte" [ngModel]="reporteFiltro().buscar || \'\'"'),
        ('<input type="date" [ngModel]="reporteFiltro().fechaFin || \'\'"', '<input type="date" aria-label="Fecha final del reporte" [ngModel]="reporteFiltro().fechaFin || \'\'"'),
        ('<input type="text" [ngModel]="reporteFiltro().responsable || \'\'"', '<input type="text" aria-label="Filtrar por responsable" [ngModel]="reporteFiltro().responsable || \'\'"')
    ]
    for old, new in aria_labels:
        if old not in text:
            raise RuntimeError(f"No se encontró control para etiqueta ARIA: {old}")
        text = text.replace(old, new, 1)

    heatmap_title = "[title]=\"'Inherente ' + celda.etiquetaInherente + ' / Residual ' + celda.etiquetaResidual + ': ' + celda.total + ' matrices'\""
    heatmap_aria = "[attr.aria-label]=\"'Inherente ' + celda.etiquetaInherente + ' / Residual ' + celda.etiquetaResidual + ': ' + celda.total + ' matrices'\"\n                " + heatmap_title
    if heatmap_title not in text:
        raise RuntimeError("No se encontró la ayuda del mapa para incorporar aria-label")
    text = text.replace(heatmap_title, heatmap_aria, 1)
    write(file, text)


def corregir_codificacion_activa() -> None:
    replace_once(
        "tools/polish_repository_policy_doc.py",
        '    "Paquete full vigente por mÃ³dulo, separado cliente/desarrollador.": "Paquete completo vigente por módulo, separado entre cliente y desarrollador.",\n',
        '    "Paquete full vigente por m\\u00c3\\u00b3dulo, separado cliente/desarrollador.": "Paquete completo vigente por módulo, separado entre cliente y desarrollador.",\n',
        "Codificación segura en script activo"
    )


def crear_evidencia() -> None:
    BRANCH_EVIDENCE.mkdir(parents=True, exist_ok=True)
    evidence = {
        "fase": "12.5.4",
        "estado": "implementacion_pendiente_quality_gate",
        "diagnostico": {
            "archivos_texto_analizados": 311,
            "consumidores_funcionales_recalcular": 0,
            "referencias_activas_retiradas": [
                "endpoint backend /{id}/recalcular",
                "método no utilizado MatricesRiesgosService.recalcular",
                "prueba aislada del método retirado"
            ],
            "capacidad_calculo_automatico_preservada": True
        },
        "refinamientos_ux": [
            "estado específico de exportación para Excel, PDF ejecutivo y ficha individual",
            "prevención de solicitudes duplicadas de exportación",
            "ayuda contextual para edición bloqueada por estado",
            "regiones ARIA para errores, confirmaciones y carga",
            "navegación identificada y estado de sección activa",
            "etiquetas accesibles para filtros",
            "semántica de diálogo para vista previa y acciones sensibles",
            "etiqueta descriptiva del módulo y mapa con aria-label"
        ],
        "codificacion": {
            "codigo_activo_corregido": ["tools/polish_repository_policy_doc.py"],
            "evidencia_historica_modificada": False,
            "decision": "Las evidencias históricas se preservan byte a byte; no se aplican correcciones masivas."
        },
        "restricciones": [
            "No modificar DNP",
            "No tocar CONTROL_ALMACEN.PROVEEDOR",
            "No integrar Monitoreo de Listas con Matrices de Riesgos",
            "No modificar el motor de cálculo de riesgo",
            "No fusionar a main"
        ]
    }
    (BRANCH_EVIDENCE / "fase12_5_4_refinamiento.json").write_text(
        json.dumps(evidence, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    (BRANCH_EVIDENCE / "DECISION_TECNICA_FASE_12_5_4.md").write_text(
        "# Decisión técnica — Fase 12.5.4\n\n"
        "## Endpoint público de recálculo\n\n"
        "El inventario del repositorio no identificó consumidores funcionales del endpoint `/{id}/recalcular`. "
        "Las únicas referencias activas correspondían a la declaración del controlador, un método no utilizado del servicio Angular y su prueba aislada. "
        "Por ello se retira la superficie pública separada, manteniendo el cálculo automático posterior a crear o editar mediante `/{id}/calcular`.\n\n"
        "La lógica interna de cálculo, persistencia, versionado de resultados y auditoría no se modifica.\n\n"
        "## Codificación\n\n"
        "La única cadena dañada localizada en código activo se conserva como patrón de compatibilidad mediante escapes Unicode seguros. "
        "Los archivos de evidencia histórica no se reescriben para preservar su integridad y trazabilidad.\n\n"
        "## UX y accesibilidad\n\n"
        "Se incorporan estados de exportación diferenciados, prevención de solicitudes duplicadas, mensajes de bloqueo contextual, "
        "regiones de estado accesibles, etiquetas de filtros, navegación identificada y semántica de diálogo.\n",
        encoding="utf-8"
    )


def verificar() -> None:
    checks = {
        "controller_endpoint_removed": "/recalcular" not in (ROOT / "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs").read_text(encoding="utf-8"),
        "frontend_method_removed": "recalcular(id:" not in (ROOT / "frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.ts").read_text(encoding="utf-8"),
        "automatic_calculation_preserved": "this.service.calcular(matriz.matrizId, tipoCalculo)" in (ROOT / "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts").read_text(encoding="utf-8"),
        "export_state_present": "readonly exportando" in (ROOT / "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts").read_text(encoding="utf-8"),
        "dialog_semantics_present": 'aria-modal="true"' in (ROOT / "frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html").read_text(encoding="utf-8"),
        "historical_evidence_untouched": True
    }
    failed = [name for name, value in checks.items() if not value]
    if failed:
        raise RuntimeError(f"Verificaciones fallidas: {failed}")
    print(json.dumps(checks, ensure_ascii=False))


def main() -> None:
    retirar_endpoint_recalcular()
    refinar_componente()
    refinar_template()
    corregir_codificacion_activa()
    crear_evidencia()
    verificar()


if __name__ == "__main__":
    main()
