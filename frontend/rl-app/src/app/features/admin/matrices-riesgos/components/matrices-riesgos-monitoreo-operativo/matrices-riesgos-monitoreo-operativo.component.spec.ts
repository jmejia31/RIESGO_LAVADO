import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosMonitoreoOperativoComponent } from './matrices-riesgos-monitoreo-operativo.component';

describe('MatricesRiesgosMonitoreoOperativoComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosMonitoreoOperativoComponent>;
  let component: MatricesRiesgosMonitoreoOperativoComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  const resumen = {
    fechaGeneracion: '2026-08-07T12:00:00Z',
    riesgosActivos: 5,
    evaluacionesActivas: 4,
    evaluacionesAprobadas: 2,
    riesgosAltoCritico: 1,
    alertasActivas: 1,
    planesAbiertos: 2,
    actividadesVencidas: 0,
    automonitoreosUltimos30Dias: 3
  };

  const alertaActiva = {
    aleId: 9,
    aleEvaluacionId: 20,
    aleCodigo: 'ALE-01',
    aleIndicador: 'Indicador UAT',
    aleEstado: 'ACTIVO' as const,
    aleFechaDisparo: '2026-08-07T12:00:00Z'
  };

  const alertaInactiva = {
    aleId: 10,
    aleEvaluacionId: 20,
    aleCodigo: 'ALE-02',
    aleIndicador: 'Indicador Inactivo',
    aleEstado: 'INACTIVO' as const,
    aleFechaDisparo: null
  };

  const mon = {
    monId: 11,
    monEvaluacionId: 20,
    monEstadoRiesgo: 'CONTROLADO',
    monEstadoContr: 'EFECTIVO',
    monResultado: 'Sin novedad',
    monUsrId: 1,
    monFecha: '2026-08-07T12:00:00Z'
  };

  beforeEach(async () => {
    service = {
      obtenerResumenOperativo: vi.fn().mockReturnValue(of(resumen)),
      listarAlertas: vi.fn().mockReturnValue(of([alertaActiva, alertaInactiva])),
      listarAutomonitoreo: vi.fn().mockReturnValue(of([mon])),
      crearAlerta: vi.fn().mockReturnValue(of(12)),
      cambiarEstadoAlerta: vi.fn().mockReturnValue(of({ success: true })),
      registrarAutomonitoreo: vi.fn().mockReturnValue(of(13))
    };
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosMonitoreoOperativoComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).compileComponents();
    fixture = TestBed.createComponent(MatricesRiesgosMonitoreoOperativoComponent);
    component = fixture.componentInstance;
    component.evaluaciones = [{
      evaId: 20,
      evaRiesgoId: 7,
      evaVersionId: 10,
      evaEstado: 'APROBADA',
      evaDataJson: '{}',
      evaDataCalcJson: '{}',
      evaVri: 8,
      evaVrr: 4,
      evaFechaEval: '2026-08-07',
      evaUsrEval: 1,
      evaVersionRow: 1,
      evaActivo: true
    }];
    fixture.detectChanges();
  });

  it('carga el resumen operativo al iniciar', () => {
    expect(service['obtenerResumenOperativo']).toHaveBeenCalled();
    expect(component.resumen()?.riesgosActivos).toBe(5);
    expect(component.resumen()?.evaluacionesAprobadas).toBe(2);
  });

  it('maneja error al cargar el resumen operativo', () => {
    service['obtenerResumenOperativo'].mockReturnValue(throwError(() => ({ message: 'Fallo resumen' })));
    component.cargarResumen();
    expect(component.error()).toBe('Fallo resumen');
  });

  it('carga alertas y automonitoreo por evaluación seleccionada', () => {
    component.seleccionarEvaluacion(20);
    expect(service['listarAlertas']).toHaveBeenCalledWith(20);
    expect(service['listarAutomonitoreo']).toHaveBeenCalledWith(20);
    expect(component.alertas()).toHaveLength(2);
    expect(component.automonitoreos()).toHaveLength(1);
    expect(component.cargando()).toBe(false);
  });

  it('reinicia listas de seguimiento al deseleccionar o pasar id 0', () => {
    component.seleccionarEvaluacion(20);
    expect(component.evaluacionId).toBe(20);

    component.seleccionarEvaluacion(0);
    expect(component.evaluacionId).toBe(0);
    expect(component.alertas()).toEqual([]);
    expect(component.automonitoreos()).toEqual([]);
    expect(component.error()).toBeNull();
    expect(component.mensaje()).toBeNull();
  });

  it('maneja error al listar señales de alerta', () => {
    service['listarAlertas'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Error al listar alertas' } })));
    component.seleccionarEvaluacion(20);
    expect(component.error()).toBe('Error al listar alertas');
    expect(component.cargando()).toBe(false);
  });

  it('maneja error al listar automonitoreo dentro de seguimiento', () => {
    service['listarAutomonitoreo'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Error al listar automonitoreo' } })));
    component.seleccionarEvaluacion(20);
    expect(component.error()).toBe('Error al listar automonitoreo');
    expect(component.cargando()).toBe(false);
  });

  it('registra una alerta válida y limpia campos', () => {
    component.evaluacionId = 20;
    component.alertaCodigo = 'ALE-03';
    component.alertaIndicador = 'Umbral crítico alcanzado';
    component.alertaEstado = 'ACTIVO';
    component.crearAlerta();

    expect(service['crearAlerta']).toHaveBeenCalledWith({
      aleEvaluacionId: 20,
      aleCodigo: 'ALE-03',
      aleIndicador: 'Umbral crítico alcanzado',
      aleEstado: 'ACTIVO'
    });
    expect(component.mensaje()).toBe('Señal de alerta registrada correctamente.');
    expect(component.alertaCodigo).toBe('');
    expect(component.alertaIndicador).toBe('');
  });

  it('valida campos obligatorios y longitudes máximas de alerta', () => {
    component.evaluacionId = 0;
    component.alertaCodigo = '';
    component.alertaIndicador = '';
    component.crearAlerta();
    expect(service['crearAlerta']).not.toHaveBeenCalled();
    expect(component.error()).toBe('Seleccione una evaluación y complete código e indicador de la alerta.');

    component.evaluacionId = 20;
    component.alertaCodigo = 'A'.repeat(51);
    component.alertaIndicador = 'Indicador';
    component.crearAlerta();
    expect(component.error()).toBe('Revise las longitudes máximas permitidas de la alerta.');

    component.alertaCodigo = 'ALE-01';
    component.alertaIndicador = 'I'.repeat(151);
    component.crearAlerta();
    expect(component.error()).toBe('Revise las longitudes máximas permitidas de la alerta.');
  });

  it('alterna el estado de una alerta activa e inactiva', () => {
    component.evaluacionId = 20;

    component.cambiarEstado(alertaActiva);
    expect(service['cambiarEstadoAlerta']).toHaveBeenCalledWith(9, 'INACTIVO');
    expect(component.mensaje()).toBe('Alerta inactivada correctamente.');

    component.cambiarEstado(alertaInactiva);
    expect(service['cambiarEstadoAlerta']).toHaveBeenCalledWith(10, 'ACTIVO');
    expect(component.mensaje()).toBe('Alerta activada correctamente.');
  });

  it('maneja error al cambiar estado de una alerta', () => {
    service['cambiarEstadoAlerta'].mockReturnValue(throwError(() => ({ message: 'Fallo al cambiar estado' })));
    component.cambiarEstado(alertaActiva);
    expect(component.error()).toBe('Fallo al cambiar estado');
    expect(component.guardando()).toBe(false);
  });

  it('registra automonitoreo completo y valida campos requeridos', () => {
    component.evaluacionId = 0;
    component.monEstadoRiesgo = '';
    component.monEstadoControles = '';
    component.monResultado = '';
    component.registrarAutomonitoreo();
    expect(service['registrarAutomonitoreo']).not.toHaveBeenCalled();
    expect(component.error()).toBe('Seleccione una evaluación y complete todos los campos del automonitoreo.');

    component.evaluacionId = 20;
    component.monEstadoRiesgo = 'CONTROLADO';
    component.monEstadoControles = 'EFECTIVO';
    component.monResultado = 'Seguimiento mensual completado sin incidencias';
    component.registrarAutomonitoreo();

    expect(service['registrarAutomonitoreo']).toHaveBeenCalledWith({
      monEvaluacionId: 20,
      monEstadoRiesgo: 'CONTROLADO',
      monEstadoContr: 'EFECTIVO',
      monResultado: 'Seguimiento mensual completado sin incidencias'
    });
    expect(component.mensaje()).toBe('Automonitoreo registrado correctamente.');
    expect(component.monEstadoRiesgo).toBe('');
    expect(component.monEstadoControles).toBe('');
    expect(component.monResultado).toBe('');
  });

  it('maneja error al registrar automonitoreo', () => {
    service['registrarAutomonitoreo'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Error base de datos' } })));
    component.evaluacionId = 20;
    component.monEstadoRiesgo = 'RIESGO';
    component.monEstadoControles = 'CONTROLES';
    component.monResultado = 'RESULTADO';
    component.registrarAutomonitoreo();
    expect(component.error()).toBe('Error base de datos');
    expect(component.guardando()).toBe(false);
  });

  it('renderiza tarjetas de resumen KPI y secciones de alerta/automonitoreo en el DOM', () => {
    component.seleccionarEvaluacion(20);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Riesgos activos');
    expect(element.textContent).toContain('ALE-01');
    expect(element.textContent).toContain('Indicador UAT');
    expect(element.textContent).toContain('Sin novedad');

    const botonesAccionAlerta = element.querySelectorAll('article button');
    expect(botonesAccionAlerta.length).toBeGreaterThanOrEqual(1);
    (botonesAccionAlerta[0] as HTMLButtonElement).click();
    expect(service['cambiarEstadoAlerta']).toHaveBeenCalled();
  });

  it('renderiza aviso cuando no hay evaluación seleccionada', () => {
    component.seleccionarEvaluacion(0);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Seleccione una evaluación para consultar y registrar seguimiento operativo.');
  });
});
