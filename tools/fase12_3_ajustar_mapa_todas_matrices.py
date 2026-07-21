from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding='utf-8-sig')
    if new in text:
        return
    if old not in text:
        raise RuntimeError(f'No se encontro el bloque esperado en {path}')
    path.write_text(text.replace(old, new, 1), encoding='utf-8')


# Backend DTOs: hacer explicito que el dashboard incluye matrices sin calculo
# y todos los tipos de sujeto, manteniendo un conteo unico por matriz.
dto = ROOT / 'backend/RL.API/Features/MatricesRiesgos/Contracts/Reporteria/ReporteriaDtos.cs'
replace_once(
    dto,
    """    public int TotalCalculadas { get; set; }\n    public int TotalCerradas { get; set; }\n    public int TotalConPlanAccion { get; set; }\n    public int TotalAltoCritico { get; set; }\n    public int TotalPlanesVencidos { get; set; }\n    public List<MatrizRiesgoConteoDto> PorEstado { get; set; } = new();\n    public List<MatrizRiesgoConteoDto> PorNivelInherente { get; set; } = new();\n""",
    """    public int TotalCalculadas { get; set; }\n    public int TotalSinCalculo { get; set; }\n    public int TotalCerradas { get; set; }\n    public int TotalConPlanAccion { get; set; }\n    public int TotalAltoCritico { get; set; }\n    public int TotalPlanesVencidos { get; set; }\n    public List<MatrizRiesgoConteoDto> PorEstado { get; set; } = new();\n    public List<MatrizRiesgoConteoDto> PorSujetoTipo { get; set; } = new();\n    public List<MatrizRiesgoConteoDto> PorNivelInherente { get; set; } = new();\n"""
)

repo = ROOT / 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
replace_once(
    repo,
    """            TotalMatrices = totales.TotalMatrices,\n            TotalCalculadas = totales.TotalCalculadas,\n            TotalCerradas = totales.TotalCerradas,\n""",
    """            TotalMatrices = totales.TotalMatrices,\n            TotalCalculadas = totales.TotalCalculadas,\n            TotalSinCalculo = Math.Max(0, totales.TotalMatrices - totales.TotalCalculadas),\n            TotalCerradas = totales.TotalCerradas,\n"""
)
replace_once(
    repo,
    """            PorEstado = await ObtenerConteosReporteAsync(conn, filtro, \"CASE WHEN m.MRMAT_ESTADO = 'CALCULADA' THEN 'EN_REVISION' ELSE m.MRMAT_ESTADO END\"),\n            PorNivelInherente = await ObtenerConteosReporteAsync(conn, filtro, \"NVL(ri.MRR_NIVEL_INHERENTE, 'SIN_CALCULO')\"),\n""",
    """            PorEstado = await ObtenerConteosReporteAsync(conn, filtro, \"CASE WHEN m.MRMAT_ESTADO = 'CALCULADA' THEN 'EN_REVISION' ELSE m.MRMAT_ESTADO END\"),\n            PorSujetoTipo = await ObtenerConteosReporteAsync(conn, filtro, \"m.MRMAT_SUJETO_TIPO\"),\n            PorNivelInherente = await ObtenerConteosReporteAsync(conn, filtro, \"NVL(ri.MRR_NIVEL_INHERENTE, 'SIN_CALCULO')\"),\n"""
)

# Frontend models.
models = ROOT / 'frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos.models.ts'
replace_once(
    models,
    """  totalMatrices: number;\n  totalCalculadas: number;\n  totalCerradas: number;\n""",
    """  totalMatrices: number;\n  totalCalculadas: number;\n  totalSinCalculo: number;\n  totalCerradas: number;\n"""
)
replace_once(
    models,
    """  porEstado: { nombre: string; total: number }[];\n  porNivelInherente: { nombre: string; total: number }[];\n""",
    """  porEstado: { nombre: string; total: number }[];\n  porSujetoTipo: { nombre: string; total: number }[];\n  porNivelInherente: { nombre: string; total: number }[];\n"""
)

# Frontend component: la paleta del mapa es visual; no calcula niveles de riesgo.
ts = ROOT / 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts'
replace_once(
    ts,
    """          promedioInherente: dato?.promedioInherente ?? 0,\n          promedioResidual: dato?.promedioResidual ?? 0,\n          color: residual.color || this.colorNivel(residual.nivel)\n""",
    """          promedioInherente: dato?.promedioInherente ?? 0,\n          promedioResidual: dato?.promedioResidual ?? 0,\n          color: this.colorMapaTransicion(inherente.nivel, residual.nivel),\n          colorBorde: this.colorNivel(inherente.nivel),\n          colorTexto: this.colorTextoMapa(inherente.nivel, residual.nivel)\n"""
)
replace_once(
    ts,
    """  colorNivel(nivel?: string | null): string {\n    const escala = this.escalasRiesgoOrdenadas().find(e => e.nivel.toUpperCase() === (nivel ?? '').toUpperCase());\n    if (escala?.color) return escala.color;\n    const normalizado = (nivel ?? '').toUpperCase();\n    if (normalizado.includes('CRIT')) return '#dc2626';\n    if (normalizado.includes('ALTO')) return '#f97316';\n    if (normalizado.includes('MEDIO')) return '#facc15';\n    if (normalizado.includes('BAJO')) return '#22c55e';\n    return '#94a3b8';\n  }\n""",
    """  colorNivel(nivel?: string | null): string {\n    const escala = this.escalasRiesgoOrdenadas().find(e => e.nivel.toUpperCase() === (nivel ?? '').toUpperCase());\n    if (escala?.color) return escala.color;\n    const normalizado = (nivel ?? '').toUpperCase();\n    if (normalizado.includes('CRIT')) return '#dc2626';\n    if (normalizado.includes('ALTO')) return '#f97316';\n    if (normalizado.includes('MEDIO')) return '#facc15';\n    if (normalizado.includes('BAJO')) return '#22c55e';\n    return '#94a3b8';\n  }\n\n  colorMapaTransicion(nivelInherente?: string | null, nivelResidual?: string | null): string {\n    // Paleta visual diagonal inspirada en el mapa institucional de referencia.\n    // Los niveles ya vienen calculados desde backend; aqui solo se representa su intensidad.\n    const paleta = ['#4ade80', '#86efac', '#bef264', '#fde047', '#facc15', '#fb923c', '#f97316', '#ef4444', '#dc2626'];\n    const niveles = this.escalasRiesgoOrdenadas();\n    const maximo = Math.max(1, niveles.length - 1);\n    const indiceInherente = Math.max(0, niveles.findIndex(n => this.normalizarNivelMapa(n.nivel) === this.normalizarNivelMapa(nivelInherente)));\n    const indiceResidual = Math.max(0, niveles.findIndex(n => this.normalizarNivelMapa(n.nivel) === this.normalizarNivelMapa(nivelResidual)));\n    const posicion = Math.round(((indiceInherente + indiceResidual) / (maximo * 2)) * (paleta.length - 1));\n    return paleta[Math.max(0, Math.min(paleta.length - 1, posicion))];\n  }\n\n  colorTextoMapa(nivelInherente?: string | null, nivelResidual?: string | null): string {\n    const color = this.colorMapaTransicion(nivelInherente, nivelResidual);\n    return ['#f97316', '#ef4444', '#dc2626'].includes(color) ? '#ffffff' : '#0f172a';\n  }\n\n  tipoSujetoEtiqueta(tipo?: string | null): string {\n    const normalizado = `${tipo ?? ''}`.trim().toUpperCase();\n    return this.tiposSujeto.find(item => item.valor === normalizado)?.texto ?? normalizado.replaceAll('_', ' ');\n  }\n"""
)

# Vista ejecutiva: todos los tipos, sin evaluar y paleta completa del mapa.
html = ROOT / 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html'
replace_once(
    html,
    '<p class="text-xs text-gray-500">Datos vigentes obtenidos desde Oracle y consolidados por el backend.</p>',
    '<p class="text-xs text-gray-500">Incluye todas las matrices activas y todos los tipos de sujeto. El backend consolida un único resultado vigente por matriz para evitar duplicaciones por factor.</p>'
)
replace_once(
    html,
    '<section class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-4">',
    '<section class="grid grid-cols-2 md:grid-cols-4 xl:grid-cols-7 gap-4">'
)
replace_once(
    html,
    '<div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Evaluadas</p><p class="mt-2 text-3xl font-bold text-gray-900">{{ dashboard()?.totalCalculadas ?? 0 }}</p></div>',
    '<div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Evaluadas</p><p class="mt-2 text-3xl font-bold text-gray-900">{{ dashboard()?.totalCalculadas ?? 0 }}</p></div>\n      <div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Sin evaluar</p><p class="mt-2 text-3xl font-bold text-slate-600">{{ dashboard()?.totalSinCalculo ?? 0 }}</p></div>'
)
replace_once(
    html,
    '<div><h3 class="text-lg font-bold text-gray-900">Mapa de transición de riesgo</h3><p class="text-xs text-gray-500">Cruza el nivel inherente con el nivel residual vigente. Cada número representa matrices reales.</p></div>',
    '<div><h3 class="text-lg font-bold text-gray-900">Mapa de transición de riesgo</h3><p class="text-xs text-gray-500">Cruza riesgo inherente y residual de todas las matrices evaluadas. Cada matriz se cuenta una sola vez; las pendientes permanecen visibles en KPI y listado.</p></div>'
)
replace_once(
    html,
    """                class=\"min-h-20 rounded-xl border p-2 text-left transition hover:-translate-y-0.5 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-ihss-600\"\n                [ngStyle]=\"celda.total > 0 ? {'background-color': celda.color + '24', 'border-color': celda.color} : {'background-color': '#f8fafc', 'border-color': '#e2e8f0'}\"\n                [title]=\"'Inherente ' + celda.nivelInherente + ' / Residual ' + celda.nivelResidual + ': ' + celda.total + ' matrices'\">\n                <span class=\"block text-2xl font-bold text-gray-900\">{{ celda.total }}</span>\n                <span class=\"block mt-1 text-[10px] font-semibold text-gray-500\">{{ celda.promedioInherente | number:'1.2-2' }} → {{ celda.promedioResidual | number:'1.2-2' }}</span>\n""",
    """                class=\"min-h-20 rounded-xl border-2 p-2 text-left transition hover:-translate-y-0.5 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-ihss-600 disabled:cursor-default disabled:hover:translate-y-0\"\n                [disabled]=\"celda.total === 0\"\n                [ngStyle]=\"{'background-color': celda.color + (celda.total > 0 ? 'E6' : '66'), 'border-color': celda.colorBorde, 'color': celda.colorTexto}\"\n                [title]=\"'Inherente ' + celda.nivelInherente + ' / Residual ' + celda.nivelResidual + ': ' + celda.total + ' matrices'\">\n                <span class=\"block text-2xl font-bold text-inherit\">{{ celda.total }}</span>\n                <span class=\"block mt-1 text-[10px] font-semibold text-inherit opacity-80\">{{ celda.promedioInherente | number:'1.2-2' }} → {{ celda.promedioResidual | number:'1.2-2' }}</span>\n"""
)
replace_once(
    html,
    '<div class="mt-3 flex justify-between text-xs font-semibold text-gray-500 min-w-[620px]"><span>Filas: riesgo inherente</span><span>Columnas: riesgo residual</span></div>',
    '<div class="mt-3 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 text-xs font-semibold text-gray-500 min-w-[620px]"><span>Filas: riesgo inherente · Columnas: riesgo residual</span><div class="flex items-center gap-2"><span class="w-5 h-3 rounded-sm bg-green-400"></span><span>Muy bajo</span><span class="w-5 h-3 rounded-sm bg-yellow-300"></span><span>Medio</span><span class="w-5 h-3 rounded-sm bg-orange-500"></span><span>Alto</span><span class="w-5 h-3 rounded-sm bg-red-600"></span><span>Crítico</span></div></div>'
)
replace_once(
    html,
    '<div><h3 class="text-base font-bold text-gray-900 mb-3">Distribución residual</h3><div class="space-y-2"><div *ngFor="let nivel of resumenNivelesResidual()"><div class="flex items-center justify-between text-xs font-semibold text-gray-600"><span>{{ nivel.nivel }}</span><span>{{ nivel.total }}</span></div><div class="h-2 rounded-full bg-gray-100 overflow-hidden mt-1"><div class="h-full rounded-full" [style.background]="nivel.color || colorNivel(nivel.nivel)" [style.width.%]="nivel.porcentaje"></div></div></div></div></div>',
    '<div><h3 class="text-base font-bold text-gray-900 mb-3">Distribución residual</h3><div class="space-y-2"><div *ngFor="let nivel of resumenNivelesResidual()"><div class="flex items-center justify-between text-xs font-semibold text-gray-600"><span>{{ nivel.nivel }}</span><span>{{ nivel.total }}</span></div><div class="h-2 rounded-full bg-gray-100 overflow-hidden mt-1"><div class="h-full rounded-full" [style.background]="nivel.color || colorNivel(nivel.nivel)" [style.width.%]="nivel.porcentaje"></div></div></div></div></div>\n        <div><h3 class="text-base font-bold text-gray-900 mb-3">Matrices por tipo de sujeto</h3><div class="grid grid-cols-1 sm:grid-cols-2 2xl:grid-cols-1 gap-2"><div *ngFor="let tipo of dashboard()?.porSujetoTipo" class="flex items-center justify-between rounded-xl border border-gray-100 bg-gray-50 px-3 py-2 text-xs font-semibold text-gray-600"><span>{{ tipoSujetoEtiqueta(tipo.nombre) }}</span><span class="rounded-full bg-white border border-gray-200 px-2 py-0.5 text-gray-900">{{ tipo.total }}</span></div></div></div>'
)

# Pruebas unitarias de la representacion visual y el conteo de pendientes.
spec = ROOT / 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.spec.ts'
replace_once(
    spec,
    """  it('aplica los niveles de una celda como filtros del dashboard', () => {\n""",
    """  it('aplica una paleta diagonal verde a rojo sin recalcular niveles de riesgo', () => {\n    component.metodologia.set({\n      escalasRiesgo: [\n        { nivel: 'Muy bajo', valorMinimo: 1 },\n        { nivel: 'Bajo', valorMinimo: 2 },\n        { nivel: 'Medio', valorMinimo: 3 },\n        { nivel: 'Alto', valorMinimo: 4 },\n        { nivel: 'Crítico', valorMinimo: 5 }\n      ]\n    } as never);\n\n    expect(component.colorMapaTransicion('Muy bajo', 'Muy bajo')).toBe('#4ade80');\n    expect(component.colorMapaTransicion('Medio', 'Medio')).toBe('#facc15');\n    expect(component.colorMapaTransicion('Crítico', 'Crítico')).toBe('#dc2626');\n    expect(component.colorTextoMapa('Crítico', 'Crítico')).toBe('#ffffff');\n  });\n\n  it('aplica los niveles de una celda como filtros del dashboard', () => {\n"""
)

print('Ajuste Fase 12.3 aplicado correctamente.')
