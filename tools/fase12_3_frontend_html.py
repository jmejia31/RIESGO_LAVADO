from pathlib import Path
import re

root=Path(__file__).resolve().parents[1]
B=root/'backend/RL.API/Features/MatricesRiesgos'
F=root/'frontend/rl-app/src/app/features/admin/matrices-riesgos'

def read(p): return p.read_text(encoding='utf-8-sig')
def write(p,s): p.write_text(s,encoding='utf-8')
def rep(s,old,new,label):
    if old not in s:
        if new in s: return s
        raise RuntimeError(label)
    return s.replace(old,new,1)
def reg(s,pat,new,label):
    out,n=re.subn(pat,new,s,count=1,flags=re.S)
    if n!=1: raise RuntimeError(label)
    return out

p=F/'pages/matrices-riesgos/matrices-riesgos.component.html'; s=read(p)
start='  <ng-container *ngIf="tab() === \'dashboard\'">'
end='  <ng-container *ngIf="tab() === \'matrices\'">'
a=s.index(start); b=s.index(end)
block='''  <ng-container *ngIf="tab() === 'dashboard'">
    <section class="bg-white border border-gray-100 rounded-2xl shadow-sm p-4 space-y-4">
      <div class="flex flex-col xl:flex-row xl:items-end xl:justify-between gap-4">
        <div>
          <h3 class="text-lg font-bold text-gray-900">Panel ejecutivo de Matrices</h3>
          <p class="text-xs text-gray-500">Datos vigentes obtenidos desde Oracle y consolidados por el backend.</p>
        </div>
        <button *ngIf="reporteFiltrosActivos()" type="button" (click)="limpiarFiltrosReporte()"
          class="px-3 py-2 rounded-xl border border-gray-200 text-xs font-semibold text-gray-700 hover:bg-gray-50">
          Limpiar filtros
        </button>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-3">
        <select [ngModel]="reporteFiltro().sujetoTipo || ''" (ngModelChange)="actualizarFiltroReporte('sujetoTipo', $event)"
          class="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm bg-white">
          <option value="">Todos los sujetos</option>
          <option *ngFor="let tipo of tiposSujeto" [value]="tipo.valor">{{ tipo.texto }}</option>
        </select>
        <select [ngModel]="reporteFiltro().estado || ''" (ngModelChange)="actualizarFiltroReporte('estado', $event)"
          class="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm bg-white">
          <option value="">Todos los estados</option>
          <option *ngFor="let estado of estadosDisponibles" [value]="estado">{{ estadoEtiqueta(estado) }}</option>
        </select>
        <select [ngModel]="reporteFiltro().nivelInherente || ''" (ngModelChange)="actualizarFiltroReporte('nivelInherente', $event)"
          class="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm bg-white">
          <option value="">Todo riesgo inherente</option>
          <option *ngFor="let nivel of escalasRiesgoOrdenadas()" [value]="nivel.nivel">{{ nivel.nivel }}</option>
        </select>
        <select [ngModel]="reporteFiltro().nivelResidual || ''" (ngModelChange)="actualizarFiltroReporte('nivelResidual', $event)"
          class="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm bg-white">
          <option value="">Todo riesgo residual</option>
          <option *ngFor="let nivel of escalasRiesgoOrdenadas()" [value]="nivel.nivel">{{ nivel.nivel }}</option>
        </select>
        <input type="date" [ngModel]="reporteFiltro().fechaInicio || ''" (ngModelChange)="actualizarFiltroReporte('fechaInicio', $event)"
          [max]="fechaActualIso" class="w-full rounded-xl border border-gray-200 px-3 py-2 text-sm" />
      </div>
    </section>

    <section class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-4">
      <div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Matrices</p><p class="mt-2 text-3xl font-bold text-gray-900">{{ dashboard()?.totalMatrices ?? 0 }}</p></div>
      <div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Evaluadas</p><p class="mt-2 text-3xl font-bold text-gray-900">{{ dashboard()?.totalCalculadas ?? 0 }}</p></div>
      <div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Alto / Crítico</p><p class="mt-2 text-3xl font-bold text-red-700">{{ dashboard()?.totalAltoCritico ?? 0 }}</p></div>
      <div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Plan requerido</p><p class="mt-2 text-3xl font-bold text-amber-700">{{ dashboard()?.totalConPlanAccion ?? 0 }}</p></div>
      <div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Planes vencidos</p><p class="mt-2 text-3xl font-bold text-orange-700">{{ dashboard()?.totalPlanesVencidos ?? 0 }}</p></div>
      <div class="bg-white border border-gray-100 rounded-2xl p-4 shadow-sm"><p class="text-[11px] font-bold uppercase text-gray-400">Cerradas</p><p class="mt-2 text-3xl font-bold text-emerald-700">{{ dashboard()?.totalCerradas ?? 0 }}</p></div>
    </section>

    <section class="grid grid-cols-1 2xl:grid-cols-[minmax(0,1.45fr)_minmax(360px,0.75fr)] gap-5">
      <div class="bg-white border border-gray-100 rounded-2xl shadow-sm p-5 min-w-0">
        <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3 mb-5">
          <div><h3 class="text-lg font-bold text-gray-900">Mapa de transición de riesgo</h3><p class="text-xs text-gray-500">Cruza el nivel inherente con el nivel residual vigente. Cada número representa matrices reales.</p></div>
          <div class="text-right"><span class="block text-xs font-semibold text-gray-500">Metodología {{ metodologia()?.version || 'No disponible' }}</span><button *ngIf="reporteFiltro().nivelInherente || reporteFiltro().nivelResidual" type="button" (click)="limpiarSeleccionMapa()" class="mt-1 text-xs font-semibold text-ihss-900 hover:underline">Quitar selección del mapa</button></div>
        </div>

        <div class="overflow-x-auto pb-2">
          <div class="min-w-[620px] grid grid-cols-[120px_repeat(5,minmax(86px,1fr))] gap-2 items-stretch">
            <div></div>
            <div *ngFor="let nivel of nivelesMapaColumnas()" class="text-center text-[11px] font-bold text-gray-500 px-1">{{ nivel }}</div>
            <ng-container *ngFor="let fila of heatmapFilas()">
              <div class="flex items-center justify-end pr-2 text-xs font-bold text-gray-600">{{ fila.nivelInherente }}</div>
              <button *ngFor="let celda of fila.celdas" type="button" (click)="seleccionarCeldaMapa(celda)"
                class="min-h-20 rounded-xl border p-2 text-left transition hover:-translate-y-0.5 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-ihss-600"
                [ngStyle]="celda.total > 0 ? {'background-color': celda.color + '24', 'border-color': celda.color} : {'background-color': '#f8fafc', 'border-color': '#e2e8f0'}"
                [title]="'Inherente ' + celda.nivelInherente + ' / Residual ' + celda.nivelResidual + ': ' + celda.total + ' matrices'">
                <span class="block text-2xl font-bold text-gray-900">{{ celda.total }}</span>
                <span class="block mt-1 text-[10px] font-semibold text-gray-500">{{ celda.promedioInherente | number:'1.2-2' }} → {{ celda.promedioResidual | number:'1.2-2' }}</span>
              </button>
            </ng-container>
          </div>
          <div class="mt-3 flex justify-between text-xs font-semibold text-gray-500 min-w-[620px]"><span>Filas: riesgo inherente</span><span>Columnas: riesgo residual</span></div>
        </div>
      </div>

      <div class="bg-white border border-gray-100 rounded-2xl shadow-sm p-5 space-y-6">
        <div><h3 class="text-base font-bold text-gray-900 mb-3">Distribución inherente</h3><div class="space-y-2"><div *ngFor="let nivel of resumenNivelesInherente()"><div class="flex items-center justify-between text-xs font-semibold text-gray-600"><span>{{ nivel.nivel }}</span><span>{{ nivel.total }}</span></div><div class="h-2 rounded-full bg-gray-100 overflow-hidden mt-1"><div class="h-full rounded-full" [style.background]="nivel.color || colorNivel(nivel.nivel)" [style.width.%]="nivel.porcentaje"></div></div></div></div></div>
        <div><h3 class="text-base font-bold text-gray-900 mb-3">Distribución residual</h3><div class="space-y-2"><div *ngFor="let nivel of resumenNivelesResidual()"><div class="flex items-center justify-between text-xs font-semibold text-gray-600"><span>{{ nivel.nivel }}</span><span>{{ nivel.total }}</span></div><div class="h-2 rounded-full bg-gray-100 overflow-hidden mt-1"><div class="h-full rounded-full" [style.background]="nivel.color || colorNivel(nivel.nivel)" [style.width.%]="nivel.porcentaje"></div></div></div></div></div>
      </div>
    </section>

    <section class="grid grid-cols-1 xl:grid-cols-2 gap-5">
      <div class="bg-white border border-gray-100 rounded-2xl shadow-sm p-5 min-w-0">
        <div class="flex items-center justify-between gap-3 mb-4"><h3 class="text-lg font-bold text-gray-900">Matrices críticas</h3><span class="text-xs font-semibold text-gray-500">Top {{ dashboard()?.matricesCriticas?.length ?? 0 }}</span></div>
        <div class="space-y-2 max-h-80 overflow-auto pr-1">
          <button *ngFor="let matriz of dashboard()?.matricesCriticas" type="button" (click)="abrirMatrizDesdeDashboard(matriz.matrizId)" class="w-full rounded-xl border border-gray-100 bg-gray-50 p-3 text-left hover:bg-white hover:border-red-200">
            <div class="flex items-start justify-between gap-3"><div class="min-w-0"><p class="font-semibold text-gray-900 truncate">{{ matriz.nombreSujeto }}</p><p class="text-xs text-gray-500">{{ matriz.sujetoTipo }} · {{ matriz.fechaEvaluacion | date:'dd/MM/yyyy' }}</p></div><span class="shrink-0 rounded-full px-2 py-1 text-[11px] font-bold" [ngStyle]="{'background-color': colorNivel(matriz.nivelResidual) + '22', 'color': colorNivel(matriz.nivelResidual)}">{{ matriz.nivelResidual }} {{ matriz.puntajeResidual | number:'1.2-2' }}</span></div>
          </button>
          <p *ngIf="(dashboard()?.matricesCriticas?.length ?? 0) === 0" class="py-6 text-center text-sm text-gray-500">No hay matrices Alto o Crítico para los filtros seleccionados.</p>
        </div>
      </div>

      <div class="bg-white border border-gray-100 rounded-2xl shadow-sm p-5 min-w-0">
        <h3 class="text-lg font-bold text-gray-900 mb-4">Planes de acción</h3>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div *ngFor="let plan of dashboard()?.planesAccion" class="rounded-xl border border-gray-100 bg-gray-50 p-4"><p class="text-xs font-bold uppercase text-gray-500">{{ plan.estado }}</p><p class="mt-1 text-2xl font-bold text-gray-900">{{ plan.total }}</p><p class="mt-1 text-xs font-semibold" [ngClass]="plan.vencidos > 0 ? 'text-red-700' : 'text-emerald-700'">{{ plan.vencidos }} vencido(s)</p></div>
          <p *ngIf="(dashboard()?.planesAccion?.length ?? 0) === 0" class="sm:col-span-2 py-6 text-center text-sm text-gray-500">No existen planes para los filtros seleccionados.</p>
        </div>
      </div>
    </section>

    <section *ngIf="reporteFiltrosActivos()" class="bg-white border border-gray-100 rounded-2xl shadow-sm p-5">
      <div class="flex items-center justify-between gap-3 mb-4"><div><h3 class="text-lg font-bold text-gray-900">Matrices del filtro ejecutivo</h3><p class="text-xs text-gray-500">Máximo 25 registros ordenados por riesgo residual y fecha.</p></div><span class="text-xs font-semibold text-gray-500">{{ dashboard()?.matricesFiltradas?.length ?? 0 }} resultado(s)</span></div>
      <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
        <button *ngFor="let matriz of dashboard()?.matricesFiltradas" type="button" (click)="abrirMatrizDesdeDashboard(matriz.matrizId)" class="rounded-xl border border-gray-100 p-3 text-left hover:border-ihss-300 hover:bg-gray-50"><p class="font-semibold text-gray-900 truncate">{{ matriz.nombreSujeto }}</p><div class="mt-2 flex items-center justify-between text-xs text-gray-500"><span>{{ estadoEtiqueta(matriz.estado) }}</span><span>{{ matriz.nivelInherente || '-' }} → {{ matriz.nivelResidual || '-' }}</span></div></button>
      </div>
    </section>
  </ng-container>

'''
s=s[:a]+block+s[b:]
write(p,s)
