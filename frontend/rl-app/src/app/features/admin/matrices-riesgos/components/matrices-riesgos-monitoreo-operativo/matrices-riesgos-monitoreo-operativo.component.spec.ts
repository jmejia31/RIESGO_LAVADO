import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosMonitoreoOperativoComponent } from './matrices-riesgos-monitoreo-operativo.component';

describe('MatricesRiesgosMonitoreoOperativoComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosMonitoreoOperativoComponent>;
  let component: MatricesRiesgosMonitoreoOperativoComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  const resumen = { fechaGeneracion: '2026-08-07T12:00:00Z', riesgosActivos: 5, evaluacionesActivas: 4, evaluacionesAprobadas: 2, riesgosAltoCritico: 1, alertasActivas: 1, planesAbiertos: 2, actividadesVencidas: 0, automonitoreosUltimos30Dias: 3 };
  const alerta = { aleId: 9, aleEvaluacionId: 20, aleCodigo: 'ALE-01', aleIndicador: 'Indicador UAT', aleEstado: 'ACTIVO' as const, aleFechaDisparo: '2026-08-07T12:00:00Z' };
  const mon = { monId: 11, monEvaluacionId: 20, monEstadoRiesgo: 'CONTROLADO', monEstadoContr: 'EFECTIVO', monResultado: 'Sin novedad', monUsrId: 1, monFecha: '2026-08-07T12:00:00Z' };

  beforeEach(async () => {
    service = {
      obtenerResumenOperativo: vi.fn().mockReturnValue(of(resumen)),
      listarAlertas: vi.fn().mockReturnValue(of([alerta])),
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
    component.evaluaciones = [{ evaId: 20, evaRiesgoId: 7, evaVersionId: 10, evaEstado: 'APROBADA', evaDataJson: '{}', evaDataCalcJson: '{}', evaVri: 8, evaVrr: 4, evaFechaEval: '2026-08-07', evaUsrEval: 1, evaVersionRow: 1, evaActivo: true }];
    fixture.detectChanges();
  });

  it('carga el resumen operativo al iniciar', () => {
    expect(service['obtenerResumenOperativo']).toHaveBeenCalled();
    expect(component.resumen()?.riesgosActivos).toBe(5);
  });

  it('carga alertas y automonitoreo por evaluación', () => {
    component.seleccionarEvaluacion(20);
    expect(service['listarAlertas']).toHaveBeenCalledWith(20);
    expect(service['listarAutomonitoreo']).toHaveBeenCalledWith(20);
    expect(component.alertas()).toHaveLength(1);
    expect(component.automonitoreos()).toHaveLength(1);
  });

  it('registra una alerta y permite inactivarla', () => {
    component.evaluacionId = 20;
    component.alertaCodigo = 'ALE-02';
    component.alertaIndicador = 'Umbral superado';
    component.crearAlerta();
    expect(service['crearAlerta']).toHaveBeenCalledWith({ aleEvaluacionId: 20, aleCodigo: 'ALE-02', aleIndicador: 'Umbral superado', aleEstado: 'ACTIVO' });

    component.cambiarEstado(alerta);
    expect(service['cambiarEstadoAlerta']).toHaveBeenCalledWith(9, 'INACTIVO');
  });

  it('registra automonitoreo completo', () => {
    component.evaluacionId = 20;
    component.monEstadoRiesgo = 'CONTROLADO';
    component.monEstadoControles = 'EFECTIVO';
    component.monResultado = 'Seguimiento correcto';
    component.registrarAutomonitoreo();
    expect(service['registrarAutomonitoreo']).toHaveBeenCalledWith({
      monEvaluacionId: 20,
      monEstadoRiesgo: 'CONTROLADO',
      monEstadoContr: 'EFECTIVO',
      monResultado: 'Seguimiento correcto'
    });
  });

  it('rechaza entradas incompletas y conserva el error de backend', () => {
    component.evaluacionId = 20;
    component.crearAlerta();
    expect(service['crearAlerta']).not.toHaveBeenCalled();
    expect(component.error()).toContain('complete');

    service['registrarAutomonitoreo'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Estado inválido' } })));
    component.monEstadoRiesgo = 'RIESGO';
    component.monEstadoControles = 'CONTROLES';
    component.monResultado = 'RESULTADO';
    component.registrarAutomonitoreo();
    expect(component.error()).toBe('Estado inválido');
    expect(component.guardando()).toBe(false);
  });
});
