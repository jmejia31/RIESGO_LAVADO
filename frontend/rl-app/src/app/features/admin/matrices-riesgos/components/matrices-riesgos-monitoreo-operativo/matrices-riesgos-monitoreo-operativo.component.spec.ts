import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosMonitoreoOperativoComponent } from './matrices-riesgos-monitoreo-operativo.component';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';

describe('MatricesRiesgosMonitoreoOperativoComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosMonitoreoOperativoComponent>;
  let component: MatricesRiesgosMonitoreoOperativoComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;
  let confirmation: { confirm: ReturnType<typeof vi.fn> };

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
    confirmation = { confirm: vi.fn().mockResolvedValue(true) };
    service = {
      obtenerCatalogoEstadosEvaluacion: vi.fn().mockReturnValue(of([
        { codigo: 'APROBADA', etiqueta: 'Aprobada' }, { codigo: 'BORRADOR', etiqueta: 'Borrador' },
        { codigo: 'CERRADA', etiqueta: 'Cerrada' }, { codigo: 'EN_REVISION', etiqueta: 'En revisión' },
        { codigo: 'OBSERVADA', etiqueta: 'Observada' }, { codigo: 'RECHAZADA', etiqueta: 'Rechazada' }
      ])),
      obtenerResumenOperativo: vi.fn().mockReturnValue(of(resumen)),
      listarAlertas: vi.fn().mockReturnValue(of([alertaActiva, alertaInactiva])),
      listarAutomonitoreo: vi.fn().mockReturnValue(of([mon])),
      obtenerBloque6: vi.fn().mockReturnValue(of({ senalesAlerta: [alertaActiva, alertaInactiva], estadoRiesgo: 'CONTROLADO', controles: [], observacionesArea: 'Área', observacionesUgr: 'UGR', puedeEditarObservacionesArea: true, puedeEditarObservacionesUgr: false })),
      obtenerProyeccionInstitucional: vi.fn().mockReturnValue(of({})),
      actualizarMonitoreoControl: vi.fn().mockReturnValue(of({ success: true })),
      actualizarObservacionArea: vi.fn().mockReturnValue(of({ success: true })),
      actualizarObservacionUgr: vi.fn().mockReturnValue(of({ success: true })),
      cargarEvidencia: vi.fn().mockReturnValue(of({ eviId: 44 })),
      vincularEvidencia: vi.fn().mockReturnValue(of({ success: true })),
      desvincularEvidencia: vi.fn().mockReturnValue(of({ success: true })),
      crearAlerta: vi.fn().mockReturnValue(of(12)),
      cambiarEstadoAlerta: vi.fn().mockReturnValue(of({ success: true })),
      registrarAutomonitoreo: vi.fn().mockReturnValue(of(13))
    };
    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosMonitoreoOperativoComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: service },
        { provide: InstitutionalConfirmationService, useValue: confirmation }
      ]
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

  it('muestra etiquetas amigables del estado de evaluación en el selector', () => {
    expect(component.opcionesEvaluacion[0].label).toBe('#20 · Riesgo 7 · Aprobada');
    expect(component.etiquetaEstadoEvaluacion('EN_REVISION')).toBe('En revisión');
  });

  it('lee el snapshot F70-F82 y persiste el seguimiento posterior por control', async () => {
    const control = { controlId: 31, tipo: 'PREVENTIVO' as const, descripcion: 'Control test', estadoMonitoreo: null, efectividadMonitoreo: null, evidencias: [] };
    service['obtenerBloque6'].mockReturnValue(of({
      senalesAlerta: [alertaActiva], estadoRiesgo: 'MITIGADO', controles: [control],
      observacionesArea: 'Area A', observacionesUgr: 'UGR A',
      puedeEditarObservacionesArea: true, puedeEditarObservacionesUgr: false
    }));
    component.seleccionarEvaluacion(20);
    expect(service['obtenerBloque6']).toHaveBeenCalledWith(20);
    expect(component.bloque6()?.controles).toHaveLength(1);
    expect(component.bloque6()?.puedeEditarObservacionesArea).toBe(true);
    expect(component.bloque6()?.puedeEditarObservacionesUgr).toBe(false);

    component.estadoMonitoreoDrafts[31] = 'SE_MANTIENE';
    component.efectividadMonitoreoDrafts[31] = 85;
    await component.guardarMonitoreoControl(control);
    expect(service['actualizarMonitoreoControl']).toHaveBeenCalledWith(31, {
      evaluacionId: 20, estadoMonitoreo: 'SE_MANTIENE', efectividadMonitoreo: 85
    });
    expect(service['obtenerProyeccionInstitucional']).toHaveBeenCalledWith(20);
  });

  it('muestra la observación sin permiso como solo lectura y comunica 403 por separado', async () => {
    component.seleccionarEvaluacion(20);
    component.observacionesUgr = 'intentado';
    await component.guardarObservacion('ugr');
    expect(service['actualizarObservacionUgr']).not.toHaveBeenCalled();
    expect(component.error()).toContain('No tiene permiso para editar Observaciones UGR');

    service['actualizarObservacionArea'].mockReturnValue(throwError(() => ({ status: 403 })));
    await component.guardarObservacion('area');
    expect(component.error()).toContain('No tiene permiso para editar Observaciones del Área');
  });

  it('sube y vincula la evidencia al control seleccionado antes de refrescar la proyección', () => {
    component.seleccionarEvaluacion(20);
    const control = { controlId: 31, tipo: 'PREVENTIVO' as const, descripcion: 'Control test', estadoMonitoreo: null, efectividadMonitoreo: null, evidencias: [] };
    const file = new File(['pdf'], 'control.pdf', { type: 'application/pdf' });
    component.cargarEvidencia(control, [file]);

    expect(service['cargarEvidencia']).toHaveBeenCalledWith(file);
    expect(service['vincularEvidencia']).toHaveBeenCalledWith({ evidenciaId: 44, tipoEntidad: 'control', entidadId: 31 });
    expect(service['obtenerProyeccionInstitucional']).toHaveBeenCalledWith(20);
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

  it('registra una alerta válida y limpia campos', async () => {
    component.evaluacionId = 20;
    component.alertaCodigo = 'ALE-03';
    component.alertaIndicador = 'Umbral crítico alcanzado';
    component.alertaEstado = 'ACTIVO';
    await component.crearAlerta();

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

  it('alterna el estado de una alerta activa e inactiva', async () => {
    component.evaluacionId = 20;

    await component.cambiarEstado(alertaActiva);
    expect(service['cambiarEstadoAlerta']).toHaveBeenCalledWith(9, 'INACTIVO');
    expect(component.mensaje()).toBe('Alerta inactivada correctamente.');

    await component.cambiarEstado(alertaInactiva);
    expect(service['cambiarEstadoAlerta']).toHaveBeenCalledWith(10, 'ACTIVO');
    expect(component.mensaje()).toBe('Alerta activada correctamente.');
  });

  it('maneja error al cambiar estado de una alerta', async () => {
    service['cambiarEstadoAlerta'].mockReturnValue(throwError(() => ({ message: 'Fallo al cambiar estado' })));
    await component.cambiarEstado(alertaActiva);
    expect(component.error()).toBe('Fallo al cambiar estado');
    expect(component.guardando()).toBe(false);
  });

  it('registra automonitoreo completo y valida campos requeridos', async () => {
    component.evaluacionId = 0;
    component.monEstadoRiesgo = '';
    component.monEstadoControles = '';
    component.monResultado = '';
    await component.registrarAutomonitoreo();
    expect(service['registrarAutomonitoreo']).not.toHaveBeenCalled();
    expect(component.error()).toBe('Seleccione una evaluación y complete todos los campos del automonitoreo.');

    component.evaluacionId = 20;
    component.monEstadoRiesgo = 'CONTROLADO';
    component.monEstadoControles = 'EFECTIVO';
    component.monResultado = 'Seguimiento mensual completado sin incidencias';
    await component.registrarAutomonitoreo();

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

  it('maneja error al registrar automonitoreo', async () => {
    service['registrarAutomonitoreo'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Error base de datos' } })));
    component.evaluacionId = 20;
    component.monEstadoRiesgo = 'RIESGO';
    component.monEstadoControles = 'CONTROLES';
    component.monResultado = 'RESULTADO';
    await component.registrarAutomonitoreo();
    expect(component.error()).toBe('Error base de datos');
    expect(component.guardando()).toBe(false);
  });

  it('no registra una alerta cuando el usuario rechaza la confirmación', async () => {
    confirmation.confirm.mockResolvedValue(false);
    component.evaluacionId = 20;
    component.alertaCodigo = 'ALE-NO-GUARDAR';
    component.alertaIndicador = 'Indicador no persistente';

    await component.crearAlerta();

    expect(confirmation.confirm).toHaveBeenCalled();
    expect(service['crearAlerta']).not.toHaveBeenCalled();
  });

  it('mantiene las altas de alerta y automonitoreo fuera del flujo inline', () => {
    component.seleccionarEvaluacion(20);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('#alerta-codigo')).toBeNull();
    expect(root.querySelector('#mon-resultado')).toBeNull();

    component.nuevoAlerta();
    fixture.detectChanges();
    expect(root.querySelector('dialog[aria-labelledby="titulo-modal-alerta"] .modal-size-lg')).not.toBeNull();
    expect(root.querySelector('#alerta-codigo')).not.toBeNull();
    expect(root.querySelector('#alerta-estado')).not.toBeNull();
    expect(root.querySelector('#alerta-indicador')).not.toBeNull();
    component.cerrarModalAlertaForzado();
    component.nuevoAutomonitoreo();
    fixture.detectChanges();
    expect(root.querySelector('dialog[aria-labelledby="titulo-modal-automonitoreo"] .modal-size-lg')).not.toBeNull();
    expect(root.querySelector('#mon-estado-riesgo')).not.toBeNull();
    expect(root.querySelector('#mon-estado-controles')).not.toBeNull();
    expect(root.querySelector('#mon-resultado')).not.toBeNull();
  });

  it('renderiza tarjetas de resumen KPI y secciones de alerta/automonitoreo en el DOM', async () => {
    component.seleccionarEvaluacion(20);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Riesgos activos');
    expect(element.textContent).toContain('ALE-01');
    expect(element.textContent).toContain('Indicador UAT');
    expect(element.textContent).toContain('Sin novedad');

    const botonesAccionAlerta = element.querySelectorAll('section[aria-labelledby="titulo-alertas"] article button');
    expect(botonesAccionAlerta.length).toBeGreaterThanOrEqual(1);
    await component.cambiarEstado(alertaActiva);
    expect(service['cambiarEstadoAlerta']).toHaveBeenCalled();
  });

  it('renderiza aviso cuando no hay evaluación seleccionada', () => {
    component.seleccionarEvaluacion(0);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Seleccione una evaluación para consultar y registrar seguimiento operativo.');
  });
});
