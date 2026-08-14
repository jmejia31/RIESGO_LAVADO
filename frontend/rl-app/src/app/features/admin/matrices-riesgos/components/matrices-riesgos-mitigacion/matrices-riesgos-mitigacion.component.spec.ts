import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosMitigacionComponent } from './matrices-riesgos-mitigacion.component';

describe('MatricesRiesgosMitigacionComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosMitigacionComponent>;
  let component: MatricesRiesgosMitigacionComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  const control = {
    conId: 3,
    conEvaluacionId: 20,
    conTipo: 'PREVENTIVO',
    conDescripcion: 'Control UAT',
    conAutomatizacion: 'MANUAL',
    conEstado: 'ACTIVO'
  } as const;

  const plan = {
    plaId: 4,
    plaEvaluacionId: 20,
    plaDescripcion: 'Plan UAT',
    plaAvance: 10,
    plaPresupuesto: 100,
    plaFechaInicio: '2026-08-07T00:00:00Z',
    plaFechaFin: '2026-08-08T00:00:00Z',
    plaEstado: 'ABIERTO'
  };

  const actividad = {
    actId: 5,
    actPlanId: 4,
    actDescripcion: 'Actividad UAT',
    actResponsable: 'Responsable',
    actAvance: 0,
    actFechaInicio: '2026-08-07T00:00:00Z',
    actFechaFin: '2026-08-08T00:00:00Z',
    actEstado: 'PENDIENTE'
  };

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
    component.evaluaciones = [{
      evaId: 20,
      evaRiesgoId: 7,
      evaVersionId: 10,
      evaEstado: 'BORRADOR',
      evaDataJson: '{}',
      evaDataCalcJson: '{}',
      evaVri: 7,
      evaVrr: 4,
      evaFechaEval: '2026-08-07',
      evaUsrEval: 1,
      evaVersionRow: 1,
      evaActivo: true
    }];
    fixture.detectChanges();
  });

  it('carga controles y planes al seleccionar una evaluación', () => {
    component.seleccionarEvaluacion(20);
    expect(service['listarControles']).toHaveBeenCalledWith(20);
    expect(service['listarPlanes']).toHaveBeenCalledWith(20);
    expect(component.controles()).toHaveLength(1);
    expect(component.planes()).toHaveLength(1);
    expect(component.cargando()).toBe(false);
  });

  it('reinicia listas y selección cuando se deselecciona o pasa id 0', () => {
    component.seleccionarEvaluacion(20);
    expect(component.evaluacionId).toBe(20);

    component.seleccionarEvaluacion(0);
    expect(component.evaluacionId).toBe(0);
    expect(component.controles()).toEqual([]);
    expect(component.planes()).toEqual([]);
    expect(component.evaluacionesControl()).toEqual([]);
    expect(component.actividades()).toEqual([]);
    expect(component.controlSeleccionadoId).toBe(0);
    expect(component.planSeleccionadoId).toBe(0);
  });

  it('maneja error al listar controles y actualiza estado', () => {
    service['listarControles'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Error controles' } })));
    component.seleccionarEvaluacion(20);
    expect(component.error()).toBe('Error controles');
    expect(component.cargando()).toBe(false);
  });

  it('maneja error al listar planes dentro del flujo de mitigación', () => {
    service['listarPlanes'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Error planes' } })));
    component.seleccionarEvaluacion(20);
    expect(component.error()).toBe('Error planes');
    expect(component.cargando()).toBe(false);
  });

  it('crea un control válido y resetea el formulario', () => {
    component.seleccionarEvaluacion(20);
    component.controlTipo = 'DETECTIVO';
    component.controlAutomatizacion = 'AUTOMATICO';
    component.controlEstado = 'ACTIVO';
    component.controlDescripcion = 'Control detectivo automático';
    component.guardarControl();

    expect(service['crearControl']).toHaveBeenCalledWith({
      conEvaluacionId: 20,
      conTipo: 'DETECTIVO',
      conDescripcion: 'Control detectivo automático',
      conAutomatizacion: 'AUTOMATICO',
      conEstado: 'ACTIVO'
    });
    expect(component.mensaje()).toBe('Control creado correctamente.');
    expect(component.controlEditandoId).toBe(0);
  });

  it('actualiza un control existente', () => {
    component.seleccionarEvaluacion(20);
    component.editarControl(control);
    expect(component.controlEditandoId).toBe(3);
    expect(component.controlDescripcion).toBe('Control UAT');

    component.controlDescripcion = 'Control modificado';
    component.controlEstado = '';
    component.guardarControl();

    expect(service['actualizarControl']).toHaveBeenCalledWith(3, expect.objectContaining({
      conDescripcion: 'Control modificado',
      conEstado: 'ACTIVO'
    }));
    expect(component.mensaje()).toBe('Control actualizado correctamente.');
  });

  it('valida datos obligatorios antes de guardar control', () => {
    component.evaluacionId = 0;
    component.controlDescripcion = '   ';
    component.guardarControl();
    expect(service['crearControl']).not.toHaveBeenCalled();
    expect(component.error()).toBe('Seleccione una evaluación y describa el control.');
  });

  it('registra efectividad de control con validación de rango (0 a 100)', () => {
    component.seleccionarEvaluacion(20);
    component.controlSeleccionadoId = 3;
    component.efectividad = -1;
    component.evaluarControl();
    expect(service['evaluarControl']).not.toHaveBeenCalled();
    expect(component.error()).toContain('efectividad entre 0 y 100');

    component.efectividad = 90;
    component.comentarioEfectividad = 'Prueba satisfactoria';
    component.evaluarControl();
    expect(service['evaluarControl']).toHaveBeenCalledWith(3, {
      ecoEfectividad: 90,
      ecoComentario: 'Prueba satisfactoria'
    });
    expect(component.mensaje()).toBe('Efectividad del control registrada correctamente.');
  });

  it('maneja error al listar evaluaciones de efectividad de un control', () => {
    service['listarEvaluacionesControl'].mockReturnValue(throwError(() => ({ message: 'Fallo red efectividad' })));
    component.cargarEvaluacionesControl(3);
    expect(component.error()).toBe('Fallo red efectividad');
  });

  it('crea un plan de mitigación validando avance, presupuesto y fechas', () => {
    component.seleccionarEvaluacion(20);
    component.planDescripcion = 'Plan de acción 2026';
    component.planAvance = 25;
    component.planPresupuesto = 5000;
    component.planFechaInicio = '2026-08-01';
    component.planFechaFin = '2026-08-15';
    component.planEstado = 'EN_PROCESO';
    component.guardarPlan();

    expect(service['crearPlan']).toHaveBeenCalledWith({
      plaEvaluacionId: 20,
      plaDescripcion: 'Plan de acción 2026',
      plaAvance: 25,
      plaPresupuesto: 5000,
      plaFechaInicio: '2026-08-01T00:00:00.000Z',
      plaFechaFin: '2026-08-15T00:00:00.000Z',
      plaEstado: 'EN_PROCESO'
    });
    expect(component.mensaje()).toBe('Plan creado correctamente.');
  });

  it('actualiza un plan de mitigación existente', () => {
    component.seleccionarEvaluacion(20);
    component.editarPlan(plan);
    expect(component.planEditandoId).toBe(4);
    expect(component.planDescripcion).toBe('Plan UAT');

    component.planDescripcion = 'Plan corregido';
    component.guardarPlan();

    expect(service['actualizarPlan']).toHaveBeenCalledWith(4, expect.objectContaining({
      plaDescripcion: 'Plan corregido'
    }));
    expect(component.mensaje()).toBe('Plan actualizado correctamente.');
  });

  it('valida campos obligatorios y rangos de plan antes de invocar backend', () => {
    component.evaluacionId = 0;
    component.planDescripcion = '';
    component.guardarPlan();
    expect(service['crearPlan']).not.toHaveBeenCalled();
    expect(component.error()).toBe('Seleccione una evaluación y describa el plan.');

    component.evaluacionId = 20;
    component.planDescripcion = 'Plan X';
    component.planAvance = 101;
    component.guardarPlan();
    expect(component.error()).toBe('Revise avance, presupuesto y rango de fechas del plan.');

    component.planAvance = 50;
    component.planPresupuesto = -10;
    component.guardarPlan();
    expect(component.error()).toBe('Revise avance, presupuesto y rango de fechas del plan.');

    component.planPresupuesto = 100;
    component.planFechaInicio = '2026-08-10';
    component.planFechaFin = '2026-08-05';
    component.guardarPlan();
    expect(component.error()).toBe('Revise avance, presupuesto y rango de fechas del plan.');
  });

  it('crea y actualiza una actividad de plan validando fechas y campos obligatorios', () => {
    component.planSeleccionadoId = 4;
    component.actividadDescripcion = '';
    component.actividadResponsable = '';
    component.guardarActividad();
    expect(service['crearActividad']).not.toHaveBeenCalled();
    expect(component.error()).toBe('Seleccione un plan y complete descripción y responsable de la actividad.');

    component.actividadDescripcion = 'Actividad A';
    component.actividadResponsable = 'Responsable A';
    component.actividadAvance = -5;
    component.guardarActividad();
    expect(component.error()).toBe('Revise avance y rango de fechas de la actividad.');

    component.actividadAvance = 10;
    component.actividadFechaInicio = '2026-08-01';
    component.actividadFechaFin = '2026-08-10';
    component.actividadEstado = '';
    component.guardarActividad();
    expect(service['crearActividad']).toHaveBeenCalledWith(expect.objectContaining({
      actPlanId: 4,
      actDescripcion: 'Actividad A',
      actResponsable: 'Responsable A',
      actAvance: 10,
      actEstado: 'PENDIENTE'
    }));
    expect(component.mensaje()).toBe('Actividad creada correctamente.');

    component.editarActividad(actividad);
    expect(component.actividadEditandoId).toBe(5);
    component.actividadDescripcion = 'Actividad editada';
    component.guardarActividad();
    expect(service['actualizarActividad']).toHaveBeenCalledWith(5, expect.objectContaining({
      actDescripcion: 'Actividad editada'
    }));
    expect(component.mensaje()).toBe('Actividad actualizada correctamente.');
  });

  it('maneja error al listar actividades del plan', () => {
    service['listarActividades'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Error al listar actividades' } })));
    component.cargarActividades(4);
    expect(component.error()).toBe('Error al listar actividades');
  });

  it('maneja error al guardar actividad', () => {
    service['crearActividad'].mockReturnValue(throwError(() => ({})));
    component.planSeleccionadoId = 4;
    component.actividadDescripcion = 'Actividad';
    component.actividadResponsable = 'Responsable';
    component.guardarActividad();
    expect(component.error()).toBe('No se pudo guardar la actividad.');
    expect(component.guardando()).toBe(false);
  });
});
