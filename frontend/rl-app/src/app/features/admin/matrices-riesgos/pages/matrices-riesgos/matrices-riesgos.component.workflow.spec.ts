import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { EvaluacionRiesgoDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent flujos y evidencias', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  const version: VersionFormularioDto = {
    verId: 10,
    verFamiliaId: 1,
    verCodigo: 'FORM_A',
    verVersion: 2,
    verJson: JSON.stringify({ codigoFormulario: 'FORM_A', nombreFormulario: 'Formulario A', secciones: [] }),
    verHash: 'hash',
    verEstado: 'PUBLISHED',
    verVigente: true,
    verFechaCreacion: '2026-08-05T10:00:00Z',
    verUsrCreacion: 1
  };

  const evaluacion: EvaluacionRiesgoDto = {
    evaId: 15,
    evaRiesgoId: 5,
    evaVersionId: 10,
    evaEstado: 'BORRADOR',
    evaDataJson: '{}',
    evaDataCalcJson: '{}',
    evaVri: 7,
    evaVrr: 4,
    evaFechaEval: '2026-08-05T10:00:00Z',
    evaUsrEval: 1,
    evaVersionRow: 1,
    evaActivo: true
  };

  beforeEach(async () => {
    service = {
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(version)),
      metodologiaVigente: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'FORM_A',
        version: 2,
        secciones: [],
        catalogos: [],
        reglas: []
      })),
      listarEvaluaciones: vi.fn().mockReturnValue(of([evaluacion])),
      obtenerConsolidado: vi.fn().mockReturnValue(of([])),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([version])),
      crearEvaluacion: vi.fn().mockReturnValue(of({ success: true, datos: 20 })),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      obtenerFlujos: vi.fn().mockReturnValue(of([
        {
          fluId: 1,
          fluEvaluacionId: 15,
          fluEstado: 'BORRADOR',
          fluMotivo: 'Captura inicial',
          fluUsrId: 1,
          fluFecha: '2026-08-05T10:00:00Z'
        }
      ])),
      transicionarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      cargarEvidencia: vi.fn().mockReturnValue(of({ eviId: 88 })),
      vincularEvidencia: vi.fn().mockReturnValue(of({ success: true })),
      eliminarEvidenciaHuerfana: vi.fn().mockReturnValue(of({ success: true })),
      actualizarBorradorFormulario: vi.fn().mockReturnValue(of({ success: true })),
      clonarVersionFormulario: vi.fn().mockReturnValue(of(11)),
      publicarVersionFormulario: vi.fn().mockReturnValue(of({ success: true }))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga el historial de flujos al editar una evaluación', () => {
    component.editarEvaluacion(evaluacion);

    expect(service['obtenerFlujos']).toHaveBeenCalledWith(15);
    expect(component.evaluacionSeleccionada()).toEqual(evaluacion);
    expect(component.flujos()).toEqual(expect.arrayContaining([
      expect.objectContaining({ fluEstado: 'BORRADOR', fluMotivo: 'Captura inicial' })
    ]));
  });

  it('descarta respuestas JSON que no sean un objeto durante la edición', () => {
    component.editarEvaluacion({ ...evaluacion, evaDataJson: '[1,2,3]' });

    expect(component.respuestas()).toEqual({});
  });

  it('transiciona, limpia el motivo y recarga evaluación e historial', () => {
    component.nuevoEstado = 'EN_REVISION';
    component.motivoTransicion = 'Captura completada';

    component.transicionarEvaluacion(evaluacion);

    expect(service['transicionarEvaluacion']).toHaveBeenCalledWith(15, 'EN_REVISION', 'Captura completada');
    expect(service['listarEvaluaciones']).toHaveBeenCalledTimes(2);
    expect(service['obtenerFlujos']).toHaveBeenCalledWith(15);
    expect(component.motivoTransicion).toBe('');
    expect(component.mensaje()).toBe('Estado actualizado correctamente.');
    expect(component.guardando()).toBe(false);
  });

  it('muestra el mensaje funcional cuando la transición falla', () => {
    service['transicionarEvaluacion'].mockReturnValueOnce(
      throwError(() => ({ error: { mensaje: 'La transición solicitada no está permitida.' } }))
    );

    component.transicionarEvaluacion(evaluacion);

    expect(component.error()).toBe('La transición solicitada no está permitida.');
    expect(component.guardando()).toBe(false);
  });

  it('carga y vincula una evidencia con el contrato único', () => {
    component.archivoEvidencia = new File(['evidencia'], 'evidencia.pdf', { type: 'application/pdf' });

    component.cargarYVincularEvidencia(evaluacion);

    expect(service['cargarEvidencia']).toHaveBeenCalled();
    expect(service['vincularEvidencia']).toHaveBeenCalledWith({
      evidenciaId: 88,
      entidadId: 15,
      tipoEntidad: 'evaluacion'
    });
    expect(service['eliminarEvidenciaHuerfana']).not.toHaveBeenCalled();
    expect(component.archivoEvidencia).toBeNull();
    expect(component.mensaje()).toBe('Evidencia cargada y vinculada correctamente.');
  });

  it('elimina la evidencia huérfana cuando falla el vínculo', () => {
    service['vincularEvidencia'].mockReturnValueOnce(
      throwError(() => ({ error: { mensaje: 'La evaluación destino no existe.' } }))
    );
    component.archivoEvidencia = new File(['evidencia'], 'evidencia.pdf', { type: 'application/pdf' });

    component.cargarYVincularEvidencia(evaluacion);

    expect(service['eliminarEvidenciaHuerfana']).toHaveBeenCalledWith(88);
    expect(component.error()).toBe('La evaluación destino no existe.');
    expect(component.guardando()).toBe(false);
  });

  it('vacía el historial cuando no puede consultar los flujos', () => {
    service['obtenerFlujos'].mockReturnValueOnce(throwError(() => new Error('Fallo de red')));
    component.flujos.set([
      {
        fluId: 9,
        fluEvaluacionId: 15,
        fluEstado: 'BORRADOR',
        fluUsrId: 1,
        fluFecha: '2026-08-05T10:00:00Z'
      }
    ]);

    component.cargarFlujos(15);

    expect(component.flujos()).toEqual([]);
  });

  it('rechaza una definición técnica con JSON inválido sin llamar al backend', () => {
    component.versionEditando.set(version);
    component.definicionTecnica = '{invalido';

    component.guardarDefinicion();

    expect(service['actualizarBorradorFormulario']).not.toHaveBeenCalled();
    expect(component.error()).toBe('La definición técnica no tiene una estructura válida.');
  });
});
