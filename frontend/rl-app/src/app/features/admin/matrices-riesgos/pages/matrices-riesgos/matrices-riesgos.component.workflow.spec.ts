import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { EvaluacionRiesgoDto, EvaluacionRiesgoResumenDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { AuthService } from '../../../../../core/auth/auth.service';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';

describe('MatricesRiesgosComponent flujos y evidencias', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;
  let confirmationMock: { confirm: ReturnType<typeof vi.fn> };

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
    confirmationMock = { confirm: vi.fn().mockResolvedValue(true) };
    service = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([
        { famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: '', famActivo: true }
      ])),
      listarFamiliasFormularioPaginadas: vi.fn().mockReturnValue(of({ items: [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: '', famActivo: true }], pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1, totales: { totalFamilias: 1, activas: 1, inactivas: 0, totalVersiones: 0 } })),
      obtenerCatalogoEstadosEvaluacion: vi.fn().mockReturnValue(of([
        { codigo: 'APROBADA', etiqueta: 'Aprobada' },
        { codigo: 'BORRADOR', etiqueta: 'Borrador' },
        { codigo: 'CERRADA', etiqueta: 'Cerrada' },
        { codigo: 'EN_REVISION', etiqueta: 'En revisión' },
        { codigo: 'OBSERVADA', etiqueta: 'Observada' },
        { codigo: 'RECHAZADA', etiqueta: 'Rechazada' }
      ])),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(version)),
      obtenerVersionFormulario: vi.fn().mockImplementation((id: number) => of({ ...version, verId: id })),
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
      listarRiesgosPaginados: vi.fn().mockReturnValue(of({ items: [{ rieId: 5, rieCodigo: 'R-005', rieNombre: 'Riesgo', rieActivo: true }], pagina: 1, tamanoPagina: 200, totalRegistros: 1, totalPaginas: 1 })),
      listarEvaluaciones: vi.fn().mockReturnValue(of({
        items: [evaluacionResumen],
        pagina: 1,
        registrosPorPagina: 10,
        totalRegistros: 1,
        totalPaginas: 1
      })),
obtenerConsolidado: vi.fn().mockReturnValue(of([])),
      obtenerConsolidadoPaginado: vi.fn().mockReturnValue(of({ items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRiesgos: 0, totalConEvaluacionOficial: 0, totalSinEvaluacionOficial: 0, totalAltoCritico: 0 } })),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([version])),
      crearEvaluacion: vi.fn().mockReturnValue(of({ success: true, datos: 20 })),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      transicionarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      obtenerTransicionesEvaluacion: vi.fn().mockReturnValue(of({ estadoActual: 'BORRADOR', destinosPermitidos: ['APROBADA', 'CERRADA', 'EN_REVISION', 'OBSERVADA', 'RECHAZADA'] })),
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
      providers: [
        { provide: AuthService, useValue: { tieneRol: vi.fn().mockReturnValue(true) } },
        { provide: MatricesRiesgosService, useValue: service },
        { provide: InstitutionalConfirmationService, useValue: confirmationMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga historial y destinos válidos del backend al abrir seguimiento', () => {
    component.abrirModalSeguimiento(evaluacionResumen);
    expect(service['obtenerFlujos']).toHaveBeenCalledWith(15);
    expect(component.flujos()).toHaveLength(1);
    expect(component.estadoFlujos()).toBe('SUCCESS_WITH_DATA');
    expect(service['obtenerTransicionesEvaluacion']).toHaveBeenCalledWith(15);
    expect(component.transicionesPermitidas()).toEqual(['APROBADA', 'CERRADA', 'EN_REVISION', 'OBSERVADA', 'RECHAZADA']);
    expect(component.estadoTransiciones()).toBe('SUCCESS_WITH_DATA');
  });

  it('ofrece todos los otros cinco estados para cada uno de los seis estados actuales', () => {
    const estados = ['APROBADA', 'BORRADOR', 'CERRADA', 'EN_REVISION', 'OBSERVADA', 'RECHAZADA'];
    for (const actual of estados) {
      const destinos = estados.filter(estado => estado !== actual);
      service['obtenerTransicionesEvaluacion'].mockReturnValue(of({ estadoActual: actual, destinosPermitidos: destinos }));
      component.abrirModalSeguimiento({ ...evaluacionResumen, estado: actual });
      fixture.detectChanges();

      expect(component.transicionesPermitidas()).toEqual(destinos);
      expect(component.transicionesPermitidas()).toHaveLength(5);
      expect(component.transicionesPermitidas()).not.toContain(actual);
      expect(component.nuevoEstado).toBe('');
      const opciones = Array.from(fixture.nativeElement.querySelectorAll('#seg-nuevo-estado option')) as HTMLOptionElement[];
      expect(opciones.map(opcion => opcion.value)).toEqual(['', ...destinos]);
    }
  });

  it('mantiene ambas consultas en LOADING hasta que responden', () => {
    const transitions = new Subject<{ estadoActual: string; destinosPermitidos: string[] }>();
    const history = new Subject<Array<{ fluId: number; fluEstado: string; fluFecha: string; fluUsrId: number; fluMotivo: string }>>();
    service['obtenerTransicionesEvaluacion'].mockReturnValue(transitions);
    service['obtenerFlujos'].mockReturnValue(history);

    component.abrirModalSeguimiento(evaluacionResumen);
    fixture.detectChanges();

    expect(component.estadoTransiciones()).toBe('LOADING');
    expect(component.estadoFlujos()).toBe('LOADING');
    expect(fixture.nativeElement.textContent).toContain('Consultando transiciones permitidas');
    expect(fixture.nativeElement.textContent).toContain('Cargando historial de estados');
    expect(fixture.nativeElement.textContent).not.toContain('No hay transiciones pendientes');
    expect(fixture.nativeElement.textContent).not.toContain('Sin historial de flujo registrado');

    transitions.next({ estadoActual: 'BORRADOR', destinosPermitidos: ['EN_REVISION'] });
    history.next([]);
    fixture.detectChanges();
    expect(component.estadoTransiciones()).toBe('SUCCESS_WITH_DATA');
    expect(component.estadoFlujos()).toBe('SUCCESS_EMPTY');
  });

  it('distingue un estado no canónico sin transiciones de una carga fallida', () => {
    service['obtenerTransicionesEvaluacion'].mockReturnValue(of({ estadoActual: 'NO_CANONICO', destinosPermitidos: [] }));
    service['obtenerFlujos'].mockReturnValue(of([]));
    component.abrirModalSeguimiento({ ...evaluacionResumen, estado: 'NO_CANONICO' });
    fixture.detectChanges();

    expect(component.estadoTransiciones()).toBe('SUCCESS_EMPTY');
    expect(component.estadoFlujos()).toBe('SUCCESS_EMPTY');
    expect(fixture.nativeElement.textContent).toContain('No hay transiciones pendientes para el estado actual.');
    expect(fixture.nativeElement.textContent).toContain('Sin historial de flujo registrado.');
    expect(fixture.nativeElement.textContent).not.toContain('No se pudieron consultar las transiciones permitidas.');
    expect(fixture.nativeElement.textContent).not.toContain('No se pudo cargar el historial de estados.');
  });

  it('no convierte el error de transiciones en éxito vacío', () => {
    service['obtenerTransicionesEvaluacion'].mockReturnValue(throwError(() => ({ status: 404 })));
    service['obtenerFlujos'].mockReturnValue(of([]));
    component.abrirModalSeguimiento(evaluacionResumen);
    fixture.detectChanges();

    expect(component.estadoTransiciones()).toBe('ERROR');
    expect(component.errorTransiciones()).toBe('No se pudieron consultar las transiciones permitidas.');
    expect(component.estadoFlujos()).toBe('SUCCESS_EMPTY');
    expect(fixture.nativeElement.textContent).toContain('No se pudieron consultar las transiciones permitidas.');
    expect(fixture.nativeElement.textContent).not.toContain('No hay transiciones pendientes para el estado actual.');
  });

  it('presenta etiquetas institucionales en lugar de códigos internos', () => {
    expect(component.etiquetaEstadoEvaluacion('EN_REVISION')).toBe('En revisión');
    expect(component.etiquetaEstadoEvaluacion('RECHAZADA')).toBe('Rechazada');
    expect(component.etiquetaEstadoEvaluacion('DESCONOCIDO')).toBe('Estado no reconocido');
  });

  it('descarta respuestas JSON que no sean un objeto durante la edición', () => {
    service['obtenerEvaluacion'].mockReturnValue(of({ ...evaluacion, evaDataJson: '"invalido"' }));
    component.editarEvaluacion(evaluacionResumen);
    expect(component.respuestas()).toEqual({});
  });

  it('transiciona, limpia el motivo y recarga evaluación e historial', () => {
    service['obtenerTransicionesEvaluacion']
      .mockReturnValueOnce(of({ estadoActual: 'BORRADOR', destinosPermitidos: ['APROBADA', 'CERRADA', 'EN_REVISION', 'OBSERVADA', 'RECHAZADA'] }))
      .mockReturnValueOnce(of({ estadoActual: 'EN_REVISION', destinosPermitidos: ['APROBADA', 'BORRADOR', 'CERRADA', 'OBSERVADA', 'RECHAZADA'] }));
    component.abrirModalSeguimiento(evaluacionResumen);
    component.nuevoEstado = 'EN_REVISION';
    component.motivoTransicion = 'Revisión técnica';
    component.transicionarEvaluacionModal();

    expect(service['transicionarEvaluacion']).toHaveBeenCalledWith(15, 'EN_REVISION', 'Revisión técnica');
    expect(component.motivoTransicion).toBe('');
    expect(component.mensaje()).toBe('Estado de la evaluación #15 actualizado a En revisión correctamente.');
    expect(component.evaluacionResumenSeleccionada()?.estado).toBe('EN_REVISION');
    expect(component.transicionesPermitidas()).toEqual(['APROBADA', 'BORRADOR', 'CERRADA', 'OBSERVADA', 'RECHAZADA']);
    expect(component.transicionesPermitidas()).not.toContain('EN_REVISION');
  });

  it('muestra el mensaje funcional cuando la transición falla', () => {
    service['transicionarEvaluacion'].mockReturnValue(throwError(() => ({
      error: { detail: 'Transición inválida desde el estado actual.' }
    })));

    component.abrirModalSeguimiento(evaluacionResumen);
    component.nuevoEstado = 'APROBADA';
    component.transicionarEvaluacionModal();

    expect(component.errorSeguimiento()).toBe('Transición inválida desde el estado actual.');
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

  it('no convierte el error de historial en un historial vacío', () => {
    service['obtenerFlujos'].mockReturnValue(throwError(() => new Error('Error flujos')));
    component.abrirModalSeguimiento(evaluacionResumen);
    fixture.detectChanges();

    expect(component.flujos()).toEqual([]);
    expect(component.estadoFlujos()).toBe('ERROR');
    expect(component.errorFlujos()).toBe('No se pudo cargar el historial de estados.');
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar el historial de estados.');
    expect(fixture.nativeElement.textContent).not.toContain('Sin historial de flujo registrado.');
  });

  it('rechaza una definición técnica con JSON inválido sin llamar al backend', () => {
    const draftVersion = { ...version, verVigente: false, verEstado: 'DRAFT' as const };
    service['obtenerVersionFormulario'].mockReturnValue(of(draftVersion));
    component.abrirDefinicion(draftVersion);
    component.definicionTecnica = '{ invalid json }';
    component.guardarDefinicion();

    expect(service['actualizarBorradorFormulario']).not.toHaveBeenCalled();
    expect(component.error()).toContain('La definición JSON no es válida');
  });

  it('publica una versión no vigente tras la confirmación institucional', async () => {
    const draftVersion = { ...version, verVigente: false, verEstado: 'DRAFT' as const };
    component.publicarVersion(draftVersion);

    // Esperar promesa dinámica de SweetAlert2
    await new Promise(resolve => setTimeout(resolve, 50));
    expect(service['publicarVersionFormulario']).toHaveBeenCalledWith(10);
    expect(component.mensaje()).toBe('Versión publicada y establecida como vigente correctamente.');
  });

  it('cambia la vigencia de una versión (activar / desactivar) tras la confirmación institucional', async () => {
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

  it('elimina permanentemente una versión inactiva tras la confirmación institucional', async () => {
    const inactivaVersion = { ...version, verVigente: false, verEstado: 'DRAFT' as const };
    component.eliminarVersionFormulario(inactivaVersion);

    await new Promise(resolve => setTimeout(resolve, 50));
    expect(service['eliminarVersionFormulario']).toHaveBeenCalledWith(10);
    expect(component.mensaje()).toContain('eliminado correctamente');
  });

  it('bloquea eliminar versión cuando es vigente mostrando error', () => {
    const vigenteVersion = { ...version, verVigente: true };
    component.eliminarVersionFormulario(vigenteVersion);

    expect(component.error()).toContain('historial');
    expect(service['eliminarVersionFormulario']).not.toHaveBeenCalled();
  });
});
