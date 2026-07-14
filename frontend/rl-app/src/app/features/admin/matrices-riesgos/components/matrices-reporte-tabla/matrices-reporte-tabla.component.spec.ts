import { MatricesReporteTablaComponent } from './matrices-reporte-tabla.component';

describe('MatricesReporteTablaComponent', () => {
  it('uses the configured risk-scale color', () => {
    const component = new MatricesReporteTablaComponent();
    component.escalas = [{
      escalaId: 1,
      tipo: 'RESIDUAL',
      nivel: 'ALTO',
      color: '#123456',
      valorMinimo: 3,
      valorMaximo: 4,
      requierePlanAccion: true
    }];

    expect(component.colorNivel('alto')).toBe('#123456');
  });

  it('keeps the fallback color mapping', () => {
    const component = new MatricesReporteTablaComponent();

    expect(component.colorNivel('CRITICO')).toBe('#dc2626');
    expect(component.colorNivel('BAJO')).toBe('#22c55e');
    expect(component.colorNivel()).toBe('#94a3b8');
  });
});
