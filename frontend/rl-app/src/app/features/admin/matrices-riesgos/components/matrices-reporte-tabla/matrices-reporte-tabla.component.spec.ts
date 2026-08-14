import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatricesReporteTablaComponent } from './matrices-reporte-tabla.component';
import { RiesgoReporteFila } from '../../models/matrices-riesgos.models';

describe('MatricesReporteTablaComponent', () => {
  let fixture: ComponentFixture<MatricesReporteTablaComponent>;
  let component: MatricesReporteTablaComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MatricesReporteTablaComponent]
    }).compileComponents();
    fixture = TestBed.createComponent(MatricesReporteTablaComponent);
    component = fixture.componentInstance;
  });

  it('recibe filas tipadas del consolidado y las asigna', () => {
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

  it('no calcula colores ni clasificaciones en el cliente (principio institucional backend)', () => {
    const raw = component as unknown as Record<string, unknown>;
    expect('escalas' in raw).toBe(false);
    expect('colorNivel' in raw).toBe(false);
  });

  it('renderiza mensaje cuando la lista de filas está vacía', () => {
    component.filas = [];
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('No hay registros disponibles.');
    expect(element.textContent).toContain('0 registro(s)');
  });

  it('renderiza correctamente las columnas proyectadas con datos', () => {
    const fila: RiesgoReporteFila = {
      riesgoId: 10,
      evaluacionId: 25,
      versionFormularioId: 3,
      codigoRiesgo: 'RIE-PLD-001',
      areaPrincipal: 'Operaciones',
      duenoRiesgo: 'Oficial de Cumplimiento',
      vri: 9,
      nivelInherente: 'CRITICO',
      vrr: 2,
      nivelResidual: 'BAJO',
      respuestaRiesgo: 'ACEPTAR',
      estadoEvaluacion: 'VIGENTE',
      fechaEvaluacion: '2026-08-14T09:00:00'
    };
    component.filas = [fila];
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('RIE-PLD-001');
    expect(element.textContent).toContain('Operaciones');
    expect(element.textContent).toContain('Oficial de Cumplimiento');
    expect(element.textContent).toContain('CRITICO');
    expect(element.textContent).toContain('BAJO');
    expect(element.textContent).toContain('1 registro(s)');
  });
});
