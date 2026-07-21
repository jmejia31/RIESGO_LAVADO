from pathlib import Path
import re

ROOT = Path('.')


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8')


def write(path: str, content: str) -> None:
    (ROOT / path).write_text(content, encoding='utf-8')


def replace_once(text: str, old: str, new: str, label: str) -> str:
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f'{label}: se esperaba 1 coincidencia y se encontraron {count}.')
    return text.replace(old, new, 1)


def replace_regex(text: str, pattern: str, replacement: str, label: str) -> str:
    updated, count = re.subn(pattern, replacement, text, count=1, flags=re.S)
    if count != 1:
        raise RuntimeError(f'{label}: no se encontró el bloque esperado.')
    return updated


# ---------------------------------------------------------------------------
# Backend: todas las matrices en mapa, categoría SIN_CALCULO y conteos coherentes
# ---------------------------------------------------------------------------
repo_path = 'backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs'
repo = read(repo_path)

repo = replace_once(
    repo,
    """            SELECT COUNT(*) TOTAL,
                   COUNT(DISTINCT CASE WHEN ri.MRR_ID IS NOT NULL THEN m.MRMAT_ID END) CALCULADAS,
                   COUNT(DISTINCT CASE WHEN m.MRMAT_ESTADO = 'CERRADA' THEN m.MRMAT_ID END) CERRADAS,
                   SUM(CASE WHEN UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')) IN ('ALTO','CRITICO','CRÍTICO') THEN 1 ELSE 0 END) ALTO_CRITICO,
                   SUM(CASE WHEN ri.MRR_REQUIERE_PLAN = 1 THEN 1 ELSE 0 END) PLAN_REQUERIDO,
                   0 PLANES_VENCIDOS""",
    """            SELECT COUNT(DISTINCT m.MRMAT_ID) TOTAL,
                   COUNT(DISTINCT CASE
                       WHEN ri.MRR_ID IS NOT NULL
                        AND ri.MRR_NIVEL_INHERENTE IS NOT NULL
                        AND ri.MRR_NIVEL_RESIDUAL IS NOT NULL
                       THEN m.MRMAT_ID
                   END) CALCULADAS,
                   COUNT(DISTINCT CASE WHEN m.MRMAT_ESTADO = 'CERRADA' THEN m.MRMAT_ID END) CERRADAS,
                   COUNT(DISTINCT CASE WHEN UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')) IN ('ALTO','CRITICO','CRÍTICO') THEN m.MRMAT_ID END) ALTO_CRITICO,
                   COUNT(DISTINCT CASE WHEN ri.MRR_REQUIERE_PLAN = 1 THEN m.MRMAT_ID END) PLAN_REQUERIDO,
                   0 PLANES_VENCIDOS""",
    'conteos ejecutivos completos'
)

new_map_level = r'''    private async Task<List<MatrizRiesgoMapaNivelDto>> ObtenerMapaNivelReporteAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro, string tipo)
    {
        var nivelCol = tipo.Equals("INHERENTE", StringComparison.OrdinalIgnoreCase) ? "ri.MRR_NIVEL_INHERENTE" : "ri.MRR_NIVEL_RESIDUAL";
        var puntajeCol = tipo.Equals("INHERENTE", StringComparison.OrdinalIgnoreCase) ? "ri.MRR_PUNTAJE_INHERENTE" : "ri.MRR_PUNTAJE_RESIDUAL";
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1" };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT NVL({nivelCol}, 'SIN_CALCULO') NIVEL,
                   COUNT(DISTINCT m.MRMAT_ID) TOTAL,
                   ROUND(AVG({puntajeCol}), 4) PROMEDIO
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
               AND ri.MRR_ES_VIGENTE = 1
               AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
             WHERE {string.Join(" AND ", where)}
             GROUP BY NVL({nivelCol}, 'SIN_CALCULO')
             ORDER BY MIN(NVL({puntajeCol}, -1))";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoMapaNivelDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoMapaNivelDto
            {
                Nivel = reader["NIVEL"].ToString() ?? string.Empty,
                Total = ToInt(reader["TOTAL"]),
                Promedio = ToDecimal(reader["PROMEDIO"])
            });
        }

        return result;
    }

'''
repo = replace_regex(
    repo,
    r'    private async Task<List<MatrizRiesgoMapaNivelDto>> ObtenerMapaNivelReporteAsync\(.*?\n    private async Task<List<MatrizRiesgoMapaTransicionDto>> ObtenerMapaTransicionDashboardAsync',
    new_map_level + '    private async Task<List<MatrizRiesgoMapaTransicionDto>> ObtenerMapaTransicionDashboardAsync',
    'mapa de nivel con matrices sin cálculo'
)

new_transition = r'''    private async Task<List<MatrizRiesgoMapaTransicionDto>> ObtenerMapaTransicionDashboardAsync(OracleConnection conn, MatrizRiesgoReporteFiltroDto filtro)
    {
        // INSTITUCIONAL identifica el resultado consolidado de la matriz, no el tipo de sujeto.
        // El LEFT JOIN conserva absolutamente todas las matrices operativas; las que no tienen
        // ambos niveles completos se agrupan explícitamente como SIN_CALCULO.
        var where = new List<string> { "m.MRMAT_ESTADO_REGISTRO = 1" };
        var parameters = new List<OracleParameter>();
        AgregarFiltrosReporte(filtro, where, parameters);

        await using var cmd = conn.CreateCommand();
        cmd.BindByName = true;
        cmd.CommandText = $@"
            SELECT NVL(ri.MRR_NIVEL_INHERENTE, 'SIN_CALCULO') NIVEL_INHERENTE,
                   NVL(ri.MRR_NIVEL_RESIDUAL, 'SIN_CALCULO') NIVEL_RESIDUAL,
                   COUNT(DISTINCT m.MRMAT_ID) TOTAL,
                   ROUND(AVG(ri.MRR_PUNTAJE_INHERENTE), 4) PROMEDIO_INHERENTE,
                   ROUND(AVG(ri.MRR_PUNTAJE_RESIDUAL), 4) PROMEDIO_RESIDUAL
              FROM RL_MR_MATRICES m
              JOIN RL_MR_MODELOS mo ON mo.MRM_ID = m.MRMAT_MODELO_ID
              LEFT JOIN RL_MR_RESULTADOS ri
                ON ri.MRR_MATRIZ_ID = m.MRMAT_ID
               AND ri.MRR_ES_VIGENTE = 1
               AND ri.MRR_TIPO_RESULTADO = 'INSTITUCIONAL'
             WHERE {string.Join(" AND ", where)}
             GROUP BY NVL(ri.MRR_NIVEL_INHERENTE, 'SIN_CALCULO'),
                      NVL(ri.MRR_NIVEL_RESIDUAL, 'SIN_CALCULO')
             ORDER BY MIN(NVL(ri.MRR_PUNTAJE_INHERENTE, -1)) DESC,
                      MIN(NVL(ri.MRR_PUNTAJE_RESIDUAL, -1))";

        foreach (var parameter in parameters)
            cmd.Parameters.Add(parameter);

        var result = new List<MatrizRiesgoMapaTransicionDto>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new MatrizRiesgoMapaTransicionDto
            {
                NivelInherente = reader["NIVEL_INHERENTE"].ToString() ?? string.Empty,
                NivelResidual = reader["NIVEL_RESIDUAL"].ToString() ?? string.Empty,
                Total = ToInt(reader["TOTAL"]),
                PromedioInherente = ToDecimal(reader["PROMEDIO_INHERENTE"]),
                PromedioResidual = ToDecimal(reader["PROMEDIO_RESIDUAL"])
            });
        }

        return result;
    }

'''
repo = replace_regex(
    repo,
    r'    private async Task<List<MatrizRiesgoMapaTransicionDto>> ObtenerMapaTransicionDashboardAsync\(.*?\n    private async Task<List<MatrizRiesgoResumenDto>> ObtenerMatricesDashboardAsync',
    new_transition + '    private async Task<List<MatrizRiesgoResumenDto>> ObtenerMatricesDashboardAsync',
    'mapa de transición completo'
)

repo = replace_once(
    repo,
    """        if (!string.IsNullOrWhiteSpace(filtro.NivelInherente))
        {
            where.Add("TRANSLATE(UPPER(NVL(ri.MRR_NIVEL_INHERENTE, '')), 'ÁÉÍÓÚ', 'AEIOU') = :repNivelInherente");
            parameters.Add(new OracleParameter("repNivelInherente", NormalizarTexto(filtro.NivelInherente)));
        }

        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual))
        {
            where.Add("TRANSLATE(UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')), 'ÁÉÍÓÚ', 'AEIOU') = :repNivelResidual");
            parameters.Add(new OracleParameter("repNivelResidual", NormalizarTexto(filtro.NivelResidual)));
        }""",
    """        if (!string.IsNullOrWhiteSpace(filtro.NivelInherente))
        {
            var nivelInherente = NormalizarTexto(filtro.NivelInherente);
            if (nivelInherente == "SIN_CALCULO")
            {
                where.Add("ri.MRR_NIVEL_INHERENTE IS NULL");
            }
            else
            {
                where.Add("TRANSLATE(UPPER(NVL(ri.MRR_NIVEL_INHERENTE, '')), 'ÁÉÍÓÚ', 'AEIOU') = :repNivelInherente");
                parameters.Add(new OracleParameter("repNivelInherente", nivelInherente));
            }
        }

        if (!string.IsNullOrWhiteSpace(filtro.NivelResidual))
        {
            var nivelResidual = NormalizarTexto(filtro.NivelResidual);
            if (nivelResidual == "SIN_CALCULO")
            {
                where.Add("ri.MRR_NIVEL_RESIDUAL IS NULL");
            }
            else
            {
                where.Add("TRANSLATE(UPPER(NVL(ri.MRR_NIVEL_RESIDUAL, '')), 'ÁÉÍÓÚ', 'AEIOU') = :repNivelResidual");
                parameters.Add(new OracleParameter("repNivelResidual", nivelResidual));
            }
        }""",
    'filtros de nivel SIN_CALCULO'
)
write(repo_path, repo)


# ---------------------------------------------------------------------------
# Frontend: selección independiente, detalle inmediato y fila/columna Sin evaluar
# ---------------------------------------------------------------------------
ts_path = 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts'
ts = read(ts_path)

ts = replace_once(
    ts,
    """interface ModalOperacion {""",
    """interface CeldaMapaVista {
  nivelInherente: string;
  etiquetaInherente: string;
  nivelResidual: string;
  etiquetaResidual: string;
  total: number;
  promedioInherente: number;
  promedioResidual: number;
  color: string;
  colorBorde: string;
  colorTexto: string;
}

interface ModalOperacion {""",
    'tipo de celda del mapa'
)

ts = replace_once(
    ts,
    """  readonly dashboard = signal<MatrizRiesgoDashboard | null>(null);
  readonly reporte = signal<MatricesRiesgoReporte | null>(null);""",
    """  readonly dashboard = signal<MatrizRiesgoDashboard | null>(null);
  readonly seleccionMapa = signal<CeldaMapaVista | null>(null);
  readonly matricesCuadrante = signal<MatrizRiesgoResumen[]>([]);
  readonly cargandoCuadrante = signal(false);
  readonly reporte = signal<MatricesRiesgoReporte | null>(null);""",
    'estado navegable del mapa'
)

new_heatmap = r'''  readonly nivelesMapa = computed(() => [
    ...this.escalasRiesgoOrdenadas().map(escala => ({ valor: escala.nivel, etiqueta: escala.nivel })),
    { valor: 'SIN_CALCULO', etiqueta: 'Sin evaluar' }
  ]);

  readonly heatmapFilas = computed(() => {
    const niveles = this.nivelesMapa();
    const nivelesRiesgo = niveles.filter(nivel => nivel.valor !== 'SIN_CALCULO');
    const sinEvaluar = niveles.find(nivel => nivel.valor === 'SIN_CALCULO')!;
    const filas = [...nivelesRiesgo].reverse().concat(sinEvaluar);
    const datos = new Map((this.dashboard()?.mapaTransicion ?? []).map(celda => [
      `${this.normalizarNivelMapa(celda.nivelInherente)}|${this.normalizarNivelMapa(celda.nivelResidual)}`,
      celda
    ]));

    return filas.map(inherente => ({
      nivelInherente: inherente.valor,
      etiquetaInherente: inherente.etiqueta,
      celdas: niveles.map(residual => {
        const dato = datos.get(`${this.normalizarNivelMapa(inherente.valor)}|${this.normalizarNivelMapa(residual.valor)}`);
        return {
          nivelInherente: inherente.valor,
          etiquetaInherente: inherente.etiqueta,
          nivelResidual: residual.valor,
          etiquetaResidual: residual.etiqueta,
          total: dato?.total ?? 0,
          promedioInherente: dato?.promedioInherente ?? 0,
          promedioResidual: dato?.promedioResidual ?? 0,
          color: this.colorMapaTransicion(inherente.valor, residual.valor),
          colorBorde: this.colorNivel(inherente.valor),
          colorTexto: this.colorTextoMapa(inherente.valor, residual.valor)
        } satisfies CeldaMapaVista;
      })
    }));
  });

  readonly nivelesMapaColumnas = computed(() => this.nivelesMapa());

  seleccionarCeldaMapa(celda: CeldaMapaVista): void {
    this.seleccionMapa.set(celda);
    this.matricesCuadrante.set([]);
    this.cargandoCuadrante.set(true);

    const filtroCuadrante: MatrizRiesgoReporteFiltro = {
      ...this.reporteFiltro(),
      nivelInherente: celda.nivelInherente,
      nivelResidual: celda.nivelResidual
    };

    this.service.dashboard(filtroCuadrante).subscribe({
      next: datos => {
        this.matricesCuadrante.set(datos.matricesFiltradas ?? []);
        this.cargandoCuadrante.set(false);
      },
      error: err => {
        this.error.set(this.obtenerMensajeError(err, 'No se pudieron consultar las matrices del cuadrante.'));
        this.matricesCuadrante.set([]);
        this.cargandoCuadrante.set(false);
      }
    });
  }

  limpiarSeleccionMapa(): void {
    this.seleccionMapa.set(null);
    this.matricesCuadrante.set([]);
    this.cargandoCuadrante.set(false);
  }

  esCeldaMapaSeleccionada(celda: CeldaMapaVista): boolean {
    const seleccion = this.seleccionMapa();
    return !!seleccion
      && this.normalizarNivelMapa(seleccion.nivelInherente) === this.normalizarNivelMapa(celda.nivelInherente)
      && this.normalizarNivelMapa(seleccion.nivelResidual) === this.normalizarNivelMapa(celda.nivelResidual);
  }

  textoPromedioCelda(celda: CeldaMapaVista): string {
    if (celda.nivelInherente === 'SIN_CALCULO' || celda.nivelResidual === 'SIN_CALCULO') {
      return 'Nivel pendiente';
    }
    return `${celda.promedioInherente.toFixed(2)} → ${celda.promedioResidual.toFixed(2)}`;
  }

'''
ts = replace_regex(
    ts,
    r'  readonly heatmapFilas = computed\(\(\) => \{.*?\n  abrirMatrizDesdeDashboard',
    new_heatmap + '  abrirMatrizDesdeDashboard',
    'interacción navegable del mapa'
)

ts = replace_once(
    ts,
    """    this.matricesDuplicadas.set([]);
    this.matrizSeleccionada.set(null);""",
    """    this.matricesDuplicadas.set([]);
    this.limpiarSeleccionMapa();
    this.matrizSeleccionada.set(null);""",
    'limpieza de selección al actualizar módulo'
)

ts = replace_once(
    ts,
    """    this.reporteFiltro.set(filtroNuevo);
    this.programarCargaReporte();""",
    """    this.limpiarSeleccionMapa();
    this.reporteFiltro.set(filtroNuevo);
    this.programarCargaReporte();""",
    'limpieza de selección al cambiar filtros'
)

ts = replace_once(
    ts,
    """  limpiarFiltrosReporte(): void {
    this.reporteFiltro.set({});
    this.cargarDashboard();""",
    """  limpiarFiltrosReporte(): void {
    this.limpiarSeleccionMapa();
    this.reporteFiltro.set({});
    this.cargarDashboard();""",
    'limpieza total de filtros'
)

new_colors = r'''  colorMapaTransicion(nivelInherente?: string | null, nivelResidual?: string | null): string {
    if (this.normalizarNivelMapa(nivelInherente) === 'SIN_CALCULO'
      || this.normalizarNivelMapa(nivelResidual) === 'SIN_CALCULO') {
      return '#cbd5e1';
    }

    // Paleta visual diagonal inspirada en el mapa institucional de referencia.
    // Los niveles ya vienen calculados desde backend; aquí solo se representa su intensidad.
    const paleta = ['#4ade80', '#86efac', '#bef264', '#fde047', '#facc15', '#fb923c', '#f97316', '#ef4444', '#dc2626'];
    const niveles = this.escalasRiesgoOrdenadas();
    const maximo = Math.max(1, niveles.length - 1);
    const indiceInherente = Math.max(0, niveles.findIndex(n => this.normalizarNivelMapa(n.nivel) === this.normalizarNivelMapa(nivelInherente)));
    const indiceResidual = Math.max(0, niveles.findIndex(n => this.normalizarNivelMapa(n.nivel) === this.normalizarNivelMapa(nivelResidual)));
    const posicion = Math.round(((indiceInherente + indiceResidual) / (maximo * 2)) * (paleta.length - 1));
    return paleta[Math.max(0, Math.min(paleta.length - 1, posicion))];
  }

  colorTextoMapa(nivelInherente?: string | null, nivelResidual?: string | null): string {
    if (this.normalizarNivelMapa(nivelInherente) === 'SIN_CALCULO'
      || this.normalizarNivelMapa(nivelResidual) === 'SIN_CALCULO') {
      return '#334155';
    }
    const color = this.colorMapaTransicion(nivelInherente, nivelResidual);
    return ['#f97316', '#ef4444', '#dc2626'].includes(color) ? '#ffffff' : '#0f172a';
  }

'''
ts = replace_regex(
    ts,
    r'  colorMapaTransicion\(.*?\n  tipoSujetoEtiqueta',
    new_colors + '  tipoSujetoEtiqueta',
    'colores de sin evaluación'
)
write(ts_path, ts)


# ---------------------------------------------------------------------------
# Template: clic en todas las celdas, selección visual y detalle de registros
# ---------------------------------------------------------------------------
html_path = 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html'
html = read(html_path)

old_map_block = '''        <div class="overflow-x-auto pb-2">
          <div class="min-w-[620px] grid grid-cols-[120px_repeat(5,minmax(86px,1fr))] gap-2 items-stretch">
            <div></div>
            <div *ngFor="let nivel of nivelesMapaColumnas()" class="text-center text-[11px] font-bold text-gray-500 px-1">{{ nivel }}</div>
            <ng-container *ngFor="let fila of heatmapFilas()">
              <div class="flex items-center justify-end pr-2 text-xs font-bold text-gray-600">{{ fila.nivelInherente }}</div>
              <button *ngFor="let celda of fila.celdas" type="button" (click)="seleccionarCeldaMapa(celda)"
                class="min-h-20 rounded-xl border-2 p-2 text-left transition hover:-translate-y-0.5 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-ihss-600 disabled:cursor-default disabled:hover:translate-y-0"
                [disabled]="celda.total === 0"
                [ngStyle]="{'background-color': celda.color + (celda.total > 0 ? 'E6' : '66'), 'border-color': celda.colorBorde, 'color': celda.colorTexto}"
                [title]="'Inherente ' + celda.nivelInherente + ' / Residual ' + celda.nivelResidual + ': ' + celda.total + ' matrices'">
                <span class="block text-2xl font-bold text-inherit">{{ celda.total }}</span>
                <span class="block mt-1 text-[10px] font-semibold text-inherit opacity-80">{{ celda.promedioInherente | number:'1.2-2' }} → {{ celda.promedioResidual | number:'1.2-2' }}</span>
              </button>
            </ng-container>
          </div>
          <div class="mt-3 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 text-xs font-semibold text-gray-500 min-w-[620px]"><span>Filas: riesgo inherente · Columnas: riesgo residual</span><div class="flex items-center gap-2"><span class="w-5 h-3 rounded-sm bg-green-400"></span><span>Muy bajo</span><span class="w-5 h-3 rounded-sm bg-yellow-300"></span><span>Medio</span><span class="w-5 h-3 rounded-sm bg-orange-500"></span><span>Alto</span><span class="w-5 h-3 rounded-sm bg-red-600"></span><span>Crítico</span></div></div>
        </div>'''

new_map_block = '''        <div class="overflow-x-auto pb-2">
          <div class="min-w-[760px] grid gap-2 items-stretch"
            [style.grid-template-columns]="'120px repeat(' + nivelesMapaColumnas().length + ', minmax(86px, 1fr))'">
            <div></div>
            <div *ngFor="let nivel of nivelesMapaColumnas()" class="text-center text-[11px] font-bold text-gray-500 px-1">{{ nivel.etiqueta }}</div>
            <ng-container *ngFor="let fila of heatmapFilas()">
              <div class="flex items-center justify-end pr-2 text-xs font-bold text-gray-600">{{ fila.etiquetaInherente }}</div>
              <button *ngFor="let celda of fila.celdas" type="button" (click)="seleccionarCeldaMapa(celda)"
                class="min-h-20 rounded-xl border-2 p-2 text-left transition hover:-translate-y-0.5 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-ihss-600"
                [ngClass]="esCeldaMapaSeleccionada(celda) ? 'ring-4 ring-blue-300 shadow-lg -translate-y-0.5' : ''"
                [attr.aria-pressed]="esCeldaMapaSeleccionada(celda)"
                [ngStyle]="{'background-color': celda.color + (celda.total > 0 ? 'E6' : '66'), 'border-color': esCeldaMapaSeleccionada(celda) ? '#1d4ed8' : celda.colorBorde, 'color': celda.colorTexto}"
                [title]="'Inherente ' + celda.etiquetaInherente + ' / Residual ' + celda.etiquetaResidual + ': ' + celda.total + ' matrices'">
                <span class="block text-2xl font-bold text-inherit">{{ celda.total }}</span>
                <span class="block mt-1 text-[10px] font-semibold text-inherit opacity-80">{{ textoPromedioCelda(celda) }}</span>
              </button>
            </ng-container>
          </div>
          <div class="mt-3 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 text-xs font-semibold text-gray-500 min-w-[760px]"><span>Filas: riesgo inherente · Columnas: riesgo residual · “Sin evaluar” conserva matrices sin nivel completo</span><div class="flex items-center gap-2"><span class="w-5 h-3 rounded-sm bg-green-400"></span><span>Muy bajo</span><span class="w-5 h-3 rounded-sm bg-yellow-300"></span><span>Medio</span><span class="w-5 h-3 rounded-sm bg-orange-500"></span><span>Alto</span><span class="w-5 h-3 rounded-sm bg-red-600"></span><span>Crítico</span><span class="w-5 h-3 rounded-sm bg-slate-300"></span><span>Sin evaluar</span></div></div>
        </div>'''
html = replace_once(html, old_map_block, new_map_block, 'cuadrícula navegable')

html = replace_once(
    html,
    '''          <div class="text-right"><span class="block text-xs font-semibold text-gray-500">Metodología {{ metodologia()?.version || 'No disponible' }}</span><button *ngIf="reporteFiltro().nivelInherente || reporteFiltro().nivelResidual" type="button" (click)="limpiarSeleccionMapa()" class="mt-1 text-xs font-semibold text-ihss-900 hover:underline">Quitar selección del mapa</button></div>''',
    '''          <div class="text-right"><span class="block text-xs font-semibold text-gray-500">Metodología {{ metodologia()?.version || 'No disponible' }}</span><button *ngIf="seleccionMapa()" type="button" (click)="limpiarSeleccionMapa()" class="mt-1 text-xs font-semibold text-ihss-900 hover:underline">Limpiar selección del mapa</button></div>''',
    'acción de limpieza del mapa'
)

detail_section = '''
    <section *ngIf="seleccionMapa() as seleccion" class="bg-white border border-blue-200 rounded-2xl shadow-sm p-5" data-testid="detalle-cuadrante-mapa">
      <div class="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-4">
        <div>
          <p class="text-xs font-bold uppercase tracking-wide text-blue-700">Filtro activo del mapa</p>
          <h3 class="text-lg font-bold text-gray-900">Matrices del cuadrante seleccionado</h3>
          <p class="text-sm text-gray-500">Inherente <strong>{{ seleccion.etiquetaInherente }}</strong> / Residual <strong>{{ seleccion.etiquetaResidual }}</strong></p>
        </div>
        <div class="flex items-center gap-3">
          <span class="rounded-full border border-blue-200 bg-blue-50 px-3 py-1 text-xs font-bold text-blue-800">{{ matricesCuadrante().length }} registro(s)</span>
          <button type="button" (click)="limpiarSeleccionMapa()" class="px-3 py-2 rounded-xl border border-gray-200 text-xs font-semibold text-gray-700 hover:bg-gray-50">Limpiar selección del mapa</button>
        </div>
      </div>

      <div *ngIf="cargandoCuadrante()" class="rounded-xl border border-blue-100 bg-blue-50 px-4 py-4 text-sm font-semibold text-blue-700">Consultando matrices del cuadrante...</div>

      <div *ngIf="!cargandoCuadrante() && matricesCuadrante().length === 0" class="rounded-xl border border-gray-200 bg-gray-50 px-4 py-8 text-center text-sm text-gray-600">
        No existen matrices en esta combinación de riesgo inherente y residual.
      </div>

      <div *ngIf="!cargandoCuadrante() && matricesCuadrante().length > 0" class="overflow-x-auto rounded-xl border border-gray-200">
        <table class="w-full min-w-[1050px] text-sm">
          <thead class="bg-gray-50 text-xs uppercase text-gray-500">
            <tr><th class="px-3 py-3 text-left">ID</th><th class="px-3 py-3 text-left">Sujeto / documento</th><th class="px-3 py-3 text-left">Tipo</th><th class="px-3 py-3 text-left">Estado</th><th class="px-3 py-3 text-left">Fecha</th><th class="px-3 py-3 text-left">Inherente</th><th class="px-3 py-3 text-left">Residual</th><th class="px-3 py-3 text-left">Plan</th><th class="px-3 py-3 text-right">Acción</th></tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr *ngFor="let matriz of matricesCuadrante()" class="hover:bg-gray-50">
              <td class="px-3 py-3 font-bold text-gray-700">{{ matriz.matrizId }}</td>
              <td class="px-3 py-3"><p class="font-semibold text-gray-900">{{ matriz.nombreSujeto }}</p><p class="text-xs text-gray-500">{{ matriz.documento || matriz.sujetoIdExt || 'Sin documento' }}</p></td>
              <td class="px-3 py-3 text-gray-600">{{ tipoSujetoEtiqueta(matriz.sujetoTipo) }}</td>
              <td class="px-3 py-3"><span class="rounded-full bg-gray-100 px-2 py-1 text-xs font-bold text-gray-700">{{ estadoEtiqueta(matriz.estado) }}</span></td>
              <td class="px-3 py-3 text-gray-600 whitespace-nowrap">{{ matriz.fechaEvaluacion | date:'dd/MM/yyyy' }}</td>
              <td class="px-3 py-3"><span class="font-semibold">{{ matriz.nivelInherente || 'Sin evaluar' }}</span><span class="block text-xs text-gray-500">{{ matriz.puntajeInherente ?? '-' }}</span></td>
              <td class="px-3 py-3"><span class="font-semibold">{{ matriz.nivelResidual || 'Sin evaluar' }}</span><span class="block text-xs text-gray-500">{{ matriz.puntajeResidual ?? '-' }}</span></td>
              <td class="px-3 py-3"><span [ngClass]="matriz.requierePlanAccion ? 'text-amber-700' : 'text-emerald-700'" class="font-bold">{{ matriz.requierePlanAccion ? 'Requerido' : 'No requerido' }}</span></td>
              <td class="px-3 py-3 text-right"><button type="button" (click)="abrirMatrizDesdeDashboard(matriz.matrizId)" class="px-3 py-2 rounded-lg bg-ihss-900 text-white text-xs font-semibold hover:bg-ihss-800">Ver detalle</button></td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
'''
html = replace_once(
    html,
    '''    <section class="grid grid-cols-1 xl:grid-cols-2 gap-5">''',
    detail_section + '''
    <section class="grid grid-cols-1 xl:grid-cols-2 gap-5">''',
    'panel inmediato del cuadrante'
)
write(html_path, html)


# ---------------------------------------------------------------------------
# Unit tests frontend
# ---------------------------------------------------------------------------
spec_path = 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.spec.ts'
spec = read(spec_path)

spec = replace_once(
    spec,
    """  it('aplica los niveles de una celda como filtros del dashboard', () => {
    component.seleccionarCeldaMapa({ nivelInherente: 'Alto', nivelResidual: 'Medio' });

    expect(component.reporteFiltro()).toEqual(expect.objectContaining({ nivelInherente: 'Alto', nivelResidual: 'Medio' }));
    expect(service['dashboard']).toHaveBeenCalled();
    expect(service['reporte']).toHaveBeenCalled();
  });""",
    """  it('consulta y muestra las matrices del cuadrante sin colapsar el mapa principal', () => {
    const matriz = { matrizId: 31, nombreSujeto: 'Proveedor del cuadrante' };
    service['dashboard'].mockReturnValue(of({ matricesFiltradas: [matriz] }));
    const celda = {
      nivelInherente: 'Alto', etiquetaInherente: 'Alto',
      nivelResidual: 'Medio', etiquetaResidual: 'Medio',
      total: 1, promedioInherente: 4.2, promedioResidual: 3.1,
      color: '#f97316', colorBorde: '#f97316', colorTexto: '#ffffff'
    };

    component.seleccionarCeldaMapa(celda);

    expect(component.seleccionMapa()).toEqual(celda);
    expect(component.reporteFiltro()).toEqual({});
    expect(service['dashboard']).toHaveBeenCalledWith({ nivelInherente: 'Alto', nivelResidual: 'Medio' });
    expect(component.matricesCuadrante()).toEqual([matriz]);
    expect(component.cargandoCuadrante()).toBe(false);
  });

  it('incluye matrices sin nivel completo en la fila y columna Sin evaluar', () => {
    component.metodologia.set({
      escalasRiesgo: [{ nivel: 'Bajo', valorMinimo: 1, color: '#22c55e' }]
    } as never);
    component.dashboard.set({
      totalMatrices: 2,
      mapaTransicion: [
        { nivelInherente: 'SIN_CALCULO', nivelResidual: 'Bajo', total: 2, promedioInherente: 0, promedioResidual: 2 }
      ],
      porNivelInherente: [], porNivelResidual: []
    } as never);

    const fila = component.heatmapFilas().find(item => item.nivelInherente === 'SIN_CALCULO');
    const celda = fila?.celdas.find(item => item.nivelResidual === 'Bajo');

    expect(component.nivelesMapaColumnas().some(item => item.valor === 'SIN_CALCULO')).toBe(true);
    expect(celda).toEqual(expect.objectContaining({ total: 2, etiquetaInherente: 'Sin evaluar' }));
  });

  it('permite seleccionar un cuadrante vacío y muestra resultado vacío controlado', () => {
    service['dashboard'].mockReturnValue(of({ matricesFiltradas: [] }));
    component.seleccionarCeldaMapa({
      nivelInherente: 'Bajo', etiquetaInherente: 'Bajo',
      nivelResidual: 'Crítico', etiquetaResidual: 'Crítico',
      total: 0, promedioInherente: 0, promedioResidual: 0,
      color: '#facc15', colorBorde: '#22c55e', colorTexto: '#0f172a'
    });

    expect(service['dashboard']).toHaveBeenCalledWith({ nivelInherente: 'Bajo', nivelResidual: 'Crítico' });
    expect(component.seleccionMapa()?.total).toBe(0);
    expect(component.matricesCuadrante()).toEqual([]);
  });""",
    'pruebas de navegación por cuadrante'
)
write(spec_path, spec)


# ---------------------------------------------------------------------------
# Backend application test: sentinel SIN_CALCULO preserved
# ---------------------------------------------------------------------------
backend_spec_path = 'backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosApplicationTests.cs'
backend_spec = read(backend_spec_path)
backend_test = '''
    [Fact]
    public async Task Dashboard_SinCalculo_ConservaFiltroEspecialParaRepositorio()
    {
        var service = CrearServicio(out var repo, out _);
        MatrizRiesgoReporteFiltroDto? recibido = null;
        repo.On(nameof(IMatricesRiesgosRepository.ObtenerDashboardAsync), args =>
        {
            recibido = Assert.IsType<MatrizRiesgoReporteFiltroDto>(args[0]);
            return Task.FromResult(new MatricesRiesgoDashboardDto());
        });

        var result = await service.ObtenerDashboardAsync(new MatrizRiesgoReporteFiltroDto
        {
            NivelInherente = " SIN_CALCULO ",
            NivelResidual = " SIN_CALCULO "
        });

        Assert.True(result.Success);
        Assert.Equal("SIN_CALCULO", recibido?.NivelInherente);
        Assert.Equal("SIN_CALCULO", recibido?.NivelResidual);
    }

'''
backend_spec = replace_once(
    backend_spec,
    '''    private static MatricesRiesgosAppService CrearServicio''',
    backend_test + '''    private static MatricesRiesgosAppService CrearServicio''',
    'prueba backend SIN_CALCULO'
)
write(backend_spec_path, backend_spec)


# ---------------------------------------------------------------------------
# E2E: detalle visible y cuadrante vacío seleccionable
# ---------------------------------------------------------------------------
e2e_path = 'frontend/rl-app/e2e/login-and-routing.spec.ts'
e2e = read(e2e_path)

e2e = replace_once(
    e2e,
    """    } else if (path.endsWith('/dashboard')) {
      datos = dashboardEjecutivo;
    } else if (path.endsWith('/reportes')) {""",
    """    } else if (path.endsWith('/dashboard')) {
      const url = new URL(route.request().url());
      const nivelInherente = url.searchParams.get('nivelInherente');
      const nivelResidual = url.searchParams.get('nivelResidual');
      const coincideCuadrante = nivelInherente === 'Alto' && nivelResidual === 'Medio';
      datos = nivelInherente || nivelResidual
        ? {
            ...dashboardEjecutivo,
            filtro: { nivelInherente, nivelResidual },
            matricesFiltradas: coincideCuadrante ? [{
              matrizId: 91,
              modeloId: 1,
              modeloVersion: '2026.1',
              sujetoTipo: 'PROVEEDOR',
              documento: '0801-E2E',
              nombreSujeto: 'Proveedor cuadrante E2E',
              estado: 'APROBADA',
              fechaEvaluacion: '2026-07-21T10:00:00Z',
              puntajeInherente: 4.5,
              nivelInherente: 'Alto',
              puntajeResidual: 3.2,
              nivelResidual: 'Medio',
              requierePlanAccion: false,
            }] : [],
          }
        : dashboardEjecutivo;
    } else if (path.endsWith('/reportes')) {""",
    'respuesta E2E por cuadrante'
)

e2e = replace_once(
    e2e,
    """  await page.getByTitle('Inherente Alto / Residual Medio: 3 matrices').click();
  await filteredRequest;
  await expect(page.getByRole('button', { name: 'Quitar selección del mapa' })).toBeVisible();

  await page.screenshot({ path: 'test-results/fase12-3-dashboard-ejecutivo.png', fullPage: true });""",
    """  await page.getByTitle('Inherente Alto / Residual Medio: 3 matrices').click();
  await filteredRequest;
  await expect(page.getByRole('heading', { name: 'Matrices del cuadrante seleccionado' })).toBeVisible();
  await expect(page.getByText('Proveedor cuadrante E2E')).toBeVisible();
  await expect(page.getByText('Inherente Alto / Residual Medio')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Limpiar selección del mapa' }).first()).toBeVisible();

  const emptyRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname.endsWith('/api/matrices-riesgos/dashboard')
      && url.searchParams.get('nivelInherente') === 'Bajo'
      && url.searchParams.get('nivelResidual') === 'Crítico';
  });
  await page.getByTitle('Inherente Bajo / Residual Crítico: 0 matrices').click();
  await emptyRequest;
  await expect(page.getByText('No existen matrices en esta combinación de riesgo inherente y residual.')).toBeVisible();

  await page.screenshot({ path: 'test-results/fase12-mapa-cuadrante-detalle.png', fullPage: true });""",
    'E2E detalle y celda vacía'
)
write(e2e_path, e2e)


# Evidencia del ajuste
proof_path = Path('docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor/Evidencia_Fase_12_4/fase12_mapa_navegable_ajuste.json')
proof_path.write_text('''{
  "ajuste": "Mapa de calor navegable y cobertura total de matrices",
  "estado": "pendiente_validacion_ci",
  "reglas": [
    "Todas las matrices operativas se conservan mediante LEFT JOIN",
    "Las matrices sin ambos niveles se agrupan como SIN_CALCULO",
    "Cualquier cuadrante puede seleccionarse, incluso con total cero",
    "La selección muestra un panel inmediato con registros y acceso al detalle",
    "El resultado INSTITUCIONAL se mantiene como consolidado por matriz y no como tipo de sujeto"
  ]
}\n''', encoding='utf-8')

print('Integración de corrección del mapa aplicada correctamente.')
