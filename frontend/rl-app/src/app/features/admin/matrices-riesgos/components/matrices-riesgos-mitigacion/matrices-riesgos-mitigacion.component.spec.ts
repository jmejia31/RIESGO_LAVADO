import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosMitigacionComponent } from './matrices-riesgos-mitigacion.component';

describe('MatricesRiesgosMitigacionComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosMitigacionComponent>;
  let component: MatricesRiesgosMitigacionComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  const control = { conId: 3, conEvaluacionId: 20, conTipo: 'PREVENTIVO', conDescripcion: 'Control UAT', conAutomatizacion: 'MANUAL', conEstado: 'ACTIVO' } as const;
  const plan = { plaId: 4, plaEvaluacionId: 20, plaDescripcion: 'Plan UAT', plaAvance: 10, plaPresupuesto: 100, plaFechaInicio: '2026-08-07T00:00:00Z', plaFechaFin: '2026-08-08T00:00:00Z', plaEstado: 'ABIERTO' };
  const actividad = { actId: 5, actPlanId: 4, actDescripcion: 'Actividad UAT', actResponsable: 'Responsable', actAvance: 0, actFechaInicio: '2026-08-07T00:00:00Z', actFechaFin: '2026-08-08T00:00:00Z', actEstado: 'PENDIENTE' };

  beforeEach(async () => {
    service = {
      listarControles: vi.fn().mockReturnValue(of([control])),
      listarPlanes: vi.fn().mockReturnValue(of([plan])),
      crearControl: vi.fn().mockReturnValue(of(3)),
      actualizarControl: vi.fn().mockReturnValue(of({ success: true })),
      listarEvaluacionesControl: vi.fn().mockReturnValue(of([{ ecoId: 1, ecoControlId: 3, ecoEfectividad: 80, ecoComentario: 'OK' }])),
      evaluarControl: vi.fn().mockReturnValue(of(1)),
      crearPlan: vi.fn().mockReturnValue(of(4)),
      actualizarPlan: vi.fn().mockReturnValue(of({ success: true })),
      listarActividades: vi.fn().mockReturnValue(of([actividad])),
      crearActividad: vi.fn().mockReturnValue(of(5)),
      actualizarActividad: vi.fn().mockReturnValue(of({ success: true }))
    };
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosMitigacionComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).compileComponents();
    fixture = TestBed.createComponent(MatricesRiesgosMitigacionComponent);
    component = fixture.componentInstance;
    component.evaluaciones = [{ evaId: 20, evaRiesgoId: 7, evaVersionId: 10, evaEstado: 'BORRADOR', evaDataJson: '{}', evaDataCalcJson: '{}', evaVri: 7, evaVrr: 4, evaFechaEval: '2026-08-07', evaUsrEval: 1, evaVersionRow: 1, evaActivo: true }];
    fixture.detectChanges();
  });

  it('carga controles y planes al seleccionar una evaluación', () => {
    component.seleccionarEvaluacion(20);
    expect(service['listarControles']).toHaveBeenCalledWith(20);
    expect(service['listarPlanes']).toHaveBeenCalledWith(20);
    expect(component.controles()).toHaveLength(1);
    expect(component.planes()).toHaveLength(1);
  });

  it('crea un control y registra su efectividad', () => {
    component.seleccionarEvaluacion(20);
    component.controlDescripcion = 'Control nuevo';
    component.guardarControl();
    expect(service['crearControl']).toHaveBeenCalledWith(expect.objectContaining({ conEvaluacionId: 20, conDescripcion: 'Control nuevo' }));

    component.editarControl(control);
    component.efectividad = 85;
    component.comentarioEfectividad = 'Efectivo';
    component.evaluarControl();
    expect(service['evaluarControl']).toHaveBeenCalledWith(3, { ecoEfectividad: 85, ecoComentario: 'Efectivo' });
  });

  it('crea plan y actividad respetando relaciones y fechas', () => {
    component.seleccionarEvaluacion(20);
    component.planDescripcion = 'Plan nuevo';
    component.planFechaInicio = '2026-08-07';
    component.planFechaFin = '2026-08-08';
    component.guardarPlan();
    expect(service['crearPlan']).toHaveBeenCalledWith(expect.objectContaining({ plaEvaluacionId: 20, plaDescripcion: 'Plan nuevo' }));

    component.editarPlan(plan);
    component.actividadDescripcion = 'Actividad nueva';
    component.actividadResponsable = 'Responsable UAT';
    component.actividadFechaInicio = '2026-08-07';
    component.actividadFechaFin = '2026-08-08';
    component.guardarActividad();
    expect(service['crearActividad']).toHaveBeenCalledWith(expect.objectContaining({ actPlanId: 4, actDescripcion: 'Actividad nueva' }));
  });

  it('bloquea porcentajes y fechas inválidas antes del backend', () => {
    component.seleccionarEvaluacion(20);
    component.controlSeleccionadoId = 3;
    component.efectividad = 101;
    component.evaluarControl();
    expect(service['evaluarControl']).not.toHaveBeenCalled();

    component.planDescripcion = 'Plan inválido';
    component.planFechaInicio = '2026-08-10';
    component.planFechaFin = '2026-08-09';
    component.guardarPlan();
    expect(service['crearPlan']).not.toHaveBeenCalled();
  });

  it('propaga errores funcionales sin dejar guardado bloqueado', () => {
    service['crearControl'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Evaluación inexistente' } })));
    component.evaluacionId = 20;
    component.controlDescripcion = 'Control';
    component.guardarControl();
    expect(component.error()).toBe('Evaluación inexistente');
    expect(component.guardando()).toBe(false);
  });
});
