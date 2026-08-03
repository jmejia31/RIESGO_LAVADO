import { MatricesReporteTablaComponent } from './matrices-reporte-tabla.component';

describe('MatricesReporteTablaComponent', () => {
  it('recibe filas tipadas del consolidado', () => {
    const component = new MatricesReporteTablaComponent();
    component.filas = [{
      riesgoId: 1,
      evaluacionId: 2,
      versionFormularioId: 10,
      codigoRiesgo: 'R-001',
      areaPrincipal: 'Cumplimiento',
      duenoRiesgo: 'Responsable',
      vri: 7,
      nivelInherente: 'ALTO',
      vrr: 4,
      nivelResidual: 'MODERADO',
      respuestaRiesgo: 'MITIGAR',
      estadoEvaluacion: 'APROBADA',
      fechaEvaluacion: '2026-08-03T10:00:00'
    }];

    expect(component.filas).toHaveLength(1);
    expect(component.filas[0].codigoRiesgo).toBe('R-001');
    expect(component.filas[0].nivelResidual).toBe('MODERADO');
  });

  it('no calcula colores ni clasificaciones en el cliente', () => {
    const component = new MatricesReporteTablaComponent() as unknown as Record<string, unknown>;

    expect('escalas' in component).toBe(false);
    expect('colorNivel' in component).toBe(false);
  });
});
