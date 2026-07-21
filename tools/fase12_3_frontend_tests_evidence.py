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

p=F/'pages/matrices-riesgos/matrices-riesgos.component.spec.ts'; s=read(p)
newtests='''
  it('consulta el dashboard con los filtros ejecutivos activos', () => {
    component.reporteFiltro.set({ sujetoTipo: 'PROVEEDOR', nivelInherente: 'ALTO', nivelResidual: 'MEDIO' });
    service['dashboard'].mockReturnValue(of({ totalMatrices: 2 }));

    component.cargarDashboard();

    expect(service['dashboard']).toHaveBeenCalledWith({
      sujetoTipo: 'PROVEEDOR', nivelInherente: 'ALTO', nivelResidual: 'MEDIO'
    });
    expect(component.dashboard()).toEqual({ totalMatrices: 2 });
  });

  it('construye el mapa de transición con conteos reales del backend', () => {
    component.metodologia.set({
      escalasRiesgo: [
        { nivel: 'Bajo', valorMinimo: 1, color: '#22c55e' },
        { nivel: 'Medio', valorMinimo: 2, color: '#facc15' },
        { nivel: 'Alto', valorMinimo: 3, color: '#f97316' }
      ]
    } as never);
    component.dashboard.set({
      totalMatrices: 4,
      mapaTransicion: [
        { nivelInherente: 'Alto', nivelResidual: 'Medio', total: 3, promedioInherente: 4.5, promedioResidual: 2.5 }
      ],
      porNivelInherente: [],
      porNivelResidual: []
    } as never);

    const celda = component.heatmapFilas()
      .find(fila => fila.nivelInherente === 'Alto')?.celdas
      .find(item => item.nivelResidual === 'Medio');

    expect(celda).toEqual(expect.objectContaining({ total: 3, promedioInherente: 4.5, promedioResidual: 2.5 }));
  });

  it('aplica los niveles de una celda como filtros del dashboard', () => {
    component.seleccionarCeldaMapa({ nivelInherente: 'Alto', nivelResidual: 'Medio' });

    expect(component.reporteFiltro()).toEqual(expect.objectContaining({ nivelInherente: 'Alto', nivelResidual: 'Medio' }));
    expect(service['dashboard']).toHaveBeenCalled();
    expect(service['reporte']).toHaveBeenCalled();
  });

'''
marker="  it('lista matrices con los filtros activos y finaliza la carga', () => {"
s=s.replace(marker,newtests+marker,1)
write(p,s)

evidence = root/'docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor/Evidencia_Fase_12_3'
evidence.mkdir(parents=True, exist_ok=True)
(evidence/'fase12_3_alcance_implementado.json').write_text('''{
  "fase": "12.3 - Dashboard ejecutivo y mapa de calor",
  "rama": "fase-12-mejora-ejecutiva-matrices",
  "estado": "Implementación preparada para validación automatizada",
  "fuente_datos": "Oracle mediante backend .NET",
  "mapa_calor": {
    "tipo": "Transición de riesgo inherente a residual",
    "datos_reales": true,
    "datos_quemados": false,
    "tabla_origen": "RL_MR_RESULTADOS",
    "resultado_vigente": true,
    "filtros_backend": true,
    "interaccion_celdas": true
  },
  "componentes": [
    "KPIs ejecutivos filtrables",
    "Distribución inherente y residual",
    "Matrices críticas",
    "Planes de acción y vencimientos",
    "Listado ejecutivo filtrado",
    "Detalle navegable desde dashboard"
  ],
  "cambios_bd": false,
  "restricciones": [
    "Sin DNP.",
    "Sin CONTROL_ALMACEN.PROVEEDOR.",
    "Sin integración con Monitoreo de Listas.",
    "Sin cálculo de riesgo en frontend."
  ]
}
''', encoding='utf-8')

print('Integración Fase 12.3 aplicada correctamente.')
