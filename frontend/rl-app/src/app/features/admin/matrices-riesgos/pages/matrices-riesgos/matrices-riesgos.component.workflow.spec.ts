import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { EvaluacionRiesgoDto, EvaluacionRiesgoResumenDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn().mockResolvedValue({ isConfirmed: true })
  }
}));

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

  const evaluacionResumen: EvaluacionRiesgoResumenDto = {
    evaId: 15,
    evaRiesgoId: 5,
    riesgoCodigo: 'RIE-005',
    riesgoNombre: 'Riesgo 5',
    evaVersionId: 10,
    versionCodigo: 'FORM_A',
    versionNumero: 2,
    estado: 'BORRADOR',
    vri: 7,
    vrr: 4,
    nivelResidual: 'MEDIO',
    fechaEval: '2026-08-05T10:00:00Z'
  };

  beforeEach(async () => {
    service = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([
        { famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: '', famActivo: true }
      ])),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(version)),
      metodologiaVigente: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'FORM_A',
        version: 2,
        secciones: [],
        catalogos: [],
        reglas: []
      })),
      obtenerEvaluacion: vi.fn().mockReturnValue(of(evaluacion)),
      metodologiaPorVersion: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'FORM_A',
        version: 2,
        secciones: [],
        catalogos: [],
        reglas: []
      })),
      listarRiesgos: vi.fn().mockReturnValue(of([{ rieId: 5, rieCodigo: 'R-005', rieNombre: 'Riesgo', rieActivo: true }])),
      listarEvaluaciones: vi.fn().mockReturnValue(of({
        items: [evaluacionResumen],
        pagina: 1,
        registrosPorPagina: 10,
        totalRegistros: 1,
        totalPaginas: 1
      })),
      obtenerConsolidado: vi.fn().mockReturnValue(of([])),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([version])),
      crearEvaluacion: vi.fn().mockReturnValue(of({ success: true, datos: 20 })),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      transicionarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      obtenerFlujos: vi.fn().mockReturnValue(of([{ fluId: 1, fluEstado: 'BORRADOR', fluFecha: '2026-08-05T10:00:00Z', fluUsrId: 1, fluMotivo: 'Registro' }])),
      cargarEvidencia: vi.fn().mockReturnValue(of({ eviId: 88 })),
      vincularEvidencia: vi.fn().mockReturnValue(of({ success: true })),
      eliminarEvidenciaHuerfana: vi.fn().mockReturnValue(of({ success: true })),
      crearBorradorFormulario: vi.fn().mockReturnValue(of(10)),
      actualizarBorradorFormulario: vi.fn().mockReturnValue(of({ success: true })),
      clonarVersionFormulario: vi.fn().mockReturnValue(of(11)),
      publicarVersionFormulario: vi.fn().mockReturnValue(of({ success: true })),
      cambiarVigenciaFormulario: vi.fn().mockReturnValue(of({ success: true })),
      eliminarVersionFormulario: vi.fn().mockReturnValue(of({ success: true }))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [{ provide: MatricesRiesgosService, useValue: service }]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga el historial de flujos al abrir seguimiento de una evaluación', () => {
    component.abrirModalSeguimiento(evaluacionResumen);
    expect(service['obtenerFlujos']).toHaveBeenCalledWith(15);
    expect(component.flujos()).toHaveLength(1);
  });

  it('descarta respuestas JSON que no sean un objeto durante la edición', () => {
    service['obtenerEvaluacion'].mockReturnValue(of({ ...evaluacion, evaDataJson: '"invalido"' }));
    component.editarEvaluacion(evaluacionResumen);
    expect(component.respuestas()).toEqual({});
  });

  it('transiciona, limpia el motivo y recarga evaluación e historial', () => {
    component.abrirModalSeguimiento(evaluacionResumen);
    component.nuevoEstado = 'EN_REVISION';
    component.motivoTransicion = 'Revisión técnica';
    component.transicionarEvaluacionModal();

    expect(service['transicionarEvaluacion']).toHaveBeenCalledWith(15, 'EN_REVISION', 'Revisión técnica');
    expect(component.motivoTransicion).toBe('');
    expect(component.mensaje()).toBe('Estado de la evaluación #15 actualizado a EN_REVISION correctamente.');
  });

  it('muestra el mensaje funcional cuando la transición falla', () => {
    service['transicionarEvaluacion'].mockReturnValue(throwError(() => ({
      error: { detail: 'Transición inválida desde el estado actual.' }
    })));

    component.abrirModalSeguimiento(evaluacionResumen);
    component.nuevoEstado = 'APROBADA';
    component.transicionarEvaluacionModal();

    expect(component.error()).toBe('Transición inválida desde el estado actual.');
  });

  it('carga y vincula una evidencia con el contrato único', () => {
    const archivo = new File(['contenido'], 'evidencia.pdf', { type: 'application/pdf' });
    component.archivoEvidencia = archivo;
    component.cargarYVincularEvidencia(evaluacion);

    expect(service['cargarEvidencia']).toHaveBeenCalledWith(archivo);
    expect(service['vincularEvidencia']).toHaveBeenCalledWith({
      entidadId: 15,
      evidenciaId: 88,
      tipoEntidad: 'evaluacion'
    });
    expect(component.archivoEvidencia).toBeNull();
  });

  it('elimina la evidencia huérfana cuando falla el vínculo', () => {
    service['vincularEvidencia'].mockReturnValue(throwError(() => new Error('Error vínculo')));
    component.archivoEvidencia = new File(['data'], 'doc.pdf');
    component.cargarYVincularEvidencia(evaluacion);

    expect(service['eliminarEvidenciaHuerfana']).toHaveBeenCalledWith(88);
    expect(component.error()).toBe('No se pudo vincular la evidencia al expediente.');
  });

  it('vacía el historial cuando no puede consultar los flujos', () => {
    service['obtenerFlujos'].mockReturnValue(throwError(() => new Error('Error flujos')));
    component.abrirModalSeguimiento(evaluacionResumen);

    expect(component.flujos()).toEqual([]);
  });

  it('rechaza una definición técnica con JSON inválido sin llamar al backend', () => {
    component.abrirDefinicion(version);
    component.definicionTecnica = '{ invalid json }';
    component.guardarDefinicion();

    expect(service['actualizarBorradorFormulario']).not.toHaveBeenCalled();
    expect(component.error()).toContain('La definición JSON no es válida');
  });

  it('publica una versión no vigente mediante SweetAlert2 de confirmación', async () => {
    const draftVersion = { ...version, verVigente: false, verEstado: 'DRAFT' as const };
    component.publicarVersion(draftVersion);

    // Esperar promesa dinámica de SweetAlert2
    await new Promise(resolve => setTimeout(resolve, 50));
    expect(service['publicarVersionFormulario']).toHaveBeenCalledWith(10);
    expect(component.mensaje()).toBe('Versión publicada correctamente.');
  });

  it('cambia la vigencia de una versión (activar / desactivar) mediante SweetAlert2', async () => {
    const draftVersion = { ...version, verVigente: false };
    component.cambiarVigenciaVersion(draftVersion, true);

    await new Promise(resolve => setTimeout(resolve, 50));
    expect(service['cambiarVigenciaFormulario']).toHaveBeenCalledWith(10, true);
    expect(component.mensaje()).toBe('Versión establecida como activa exitosamente.');

    component.cambiarVigenciaVersion(draftVersion, false);
    await new Promise(resolve => setTimeout(resolve, 50));
    expect(service['cambiarVigenciaFormulario']).toHaveBeenCalledWith(10, false);
    expect(component.mensaje()).toBe('Versión desactivada.');
  });

  it('elimina permanentemente una versión inactiva mediante SweetAlert2', async () => {
    const inactivaVersion = { ...version, verVigente: false };
    component.eliminarVersionFormulario(inactivaVersion);

    await new Promise(resolve => setTimeout(resolve, 50));
    expect(service['eliminarVersionFormulario']).toHaveBeenCalledWith(10);
    expect(component.mensaje()).toContain('eliminado correctamente');
  });

  it('bloquea eliminar versión cuando es vigente mostrando error', () => {
    const vigenteVersion = { ...version, verVigente: true };
    component.eliminarVersionFormulario(vigenteVersion);

    expect(component.error()).toBe('No se puede eliminar el formulario activo de la familia.');
    expect(service['eliminarVersionFormulario']).not.toHaveBeenCalled();
  });
});
