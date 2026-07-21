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

p=F/'pages/matrices-riesgos/matrices-riesgos.component.ts'; s=read(p)
old='''  readonly resumenNiveles = computed(() => {
    const totalMatrices = this.dashboard()?.totalMatrices ?? 0;
    const conteos = new Map((this.dashboard()?.porNivelResidual ?? []).map((x: { nombre: string; total: number }) => [x.nombre.toUpperCase(), x.total]));
    return this.escalasRiesgoOrdenadas().map(e => ({
      ...e,
      total: conteos.get(e.nivel.toUpperCase()) ?? 0,
      porcentaje: totalMatrices > 0 ? ((conteos.get(e.nivel.toUpperCase()) ?? 0) / totalMatrices) * 100 : 0
    }));
  });

  readonly heatmapFilas = computed(() => {
    const niveles = this.escalasRiesgoOrdenadas();
    const colores = niveles.length > 0 ? niveles.map(n => n.color || this.colorNivel(n.nivel)) : ['#4caf50', '#8bc34a', '#ffc107', '#ff9800', '#f44336'];
    const etiquetas = ['Frecuente', 'Probable', 'Ocasional', 'Posible', 'Improbable'];
    return etiquetas.map((frecuencia, fila) => ({
      frecuencia,
      celdas: [0, 1, 2, 3, 4].map(col => {
        const idx = Math.min(4, Math.max(0, fila + col - 2));
        return { color: colores[idx] ?? '#e5e7eb', nivel: niveles[idx]?.nivel ?? 'Sin escala' };
      })
    }));
  });
'''
new='''  readonly resumenNivelesInherente = computed(() =>
    this.construirResumenNiveles(this.dashboard()?.porNivelInherente ?? [])
  );

  readonly resumenNivelesResidual = computed(() =>
    this.construirResumenNiveles(this.dashboard()?.porNivelResidual ?? [])
  );

  readonly heatmapFilas = computed(() => {
    const nivelesAsc = this.escalasRiesgoOrdenadas();
    const filas = [...nivelesAsc].reverse();
    const datos = new Map((this.dashboard()?.mapaTransicion ?? []).map(celda => [
      `${this.normalizarNivelMapa(celda.nivelInherente)}|${this.normalizarNivelMapa(celda.nivelResidual)}`,
      celda
    ]));

    return filas.map(inherente => ({
      nivelInherente: inherente.nivel,
      celdas: nivelesAsc.map(residual => {
        const dato = datos.get(`${this.normalizarNivelMapa(inherente.nivel)}|${this.normalizarNivelMapa(residual.nivel)}`);
        return {
          nivelInherente: inherente.nivel,
          nivelResidual: residual.nivel,
          total: dato?.total ?? 0,
          promedioInherente: dato?.promedioInherente ?? 0,
          promedioResidual: dato?.promedioResidual ?? 0,
          color: residual.color || this.colorNivel(residual.nivel)
        };
      })
    }));
  });

  readonly nivelesMapaColumnas = computed(() => this.escalasRiesgoOrdenadas().map(nivel => nivel.nivel));
'''
s=rep(s,old,new,'component dashboard computed')
s=rep(s,'    this.service.dashboard().subscribe({','    this.service.dashboard(this.reporteFiltro()).subscribe({','component dashboard filters')
s=rep(s,'  limpiarFiltrosReporte(): void {\n    this.reporteFiltro.set({});\n    this.cargarReporte();\n  }','  limpiarFiltrosReporte(): void {\n    this.reporteFiltro.set({});\n    this.cargarDashboard();\n    this.cargarReporte();\n  }','clear filters')
s=rep(s,'    this.reporteFiltroTimer = setTimeout(() => this.cargarReporte(), 350);','    this.reporteFiltroTimer = setTimeout(() => {\n      this.cargarDashboard();\n      this.cargarReporte();\n    }, 350);','debounce dashboard')
marker='  readonly mostrarHistorialDebajoListado = computed(() => {'
extra='''  seleccionarCeldaMapa(celda: { nivelInherente: string; nivelResidual: string }): void {
    this.reporteFiltro.update(filtro => ({
      ...filtro,
      nivelInherente: celda.nivelInherente,
      nivelResidual: celda.nivelResidual
    }));
    this.cargarDashboard();
    this.cargarReporte();
  }

  limpiarSeleccionMapa(): void {
    this.reporteFiltro.update(filtro => ({
      ...filtro,
      nivelInherente: undefined,
      nivelResidual: undefined
    }));
    this.cargarDashboard();
    this.cargarReporte();
  }

  abrirMatrizDesdeDashboard(matrizId: number): void {
    this.tab.set('matrices');
    this.seleccionarMatriz(matrizId);
  }

  private construirResumenNiveles(conteosOrigen: { nombre: string; total: number }[]) {
    const totalMatrices = this.dashboard()?.totalMatrices ?? 0;
    const conteos = new Map(conteosOrigen.map(x => [this.normalizarNivelMapa(x.nombre), x.total]));
    return this.escalasRiesgoOrdenadas().map(escala => {
      const total = conteos.get(this.normalizarNivelMapa(escala.nivel)) ?? 0;
      return {
        ...escala,
        total,
        porcentaje: totalMatrices > 0 ? (total / totalMatrices) * 100 : 0
      };
    });
  }

  private normalizarNivelMapa(nivel?: string | null): string {
    return `${nivel ?? ''}`.trim().normalize('NFD').replace(/[\u0300-\u036f]/g, '').toUpperCase();
  }

'''
if marker not in s: raise RuntimeError('component insert marker')
s=s.replace(marker,extra+marker,1)
write(p,s)
