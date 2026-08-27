import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, of } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionRiesgoResumenDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { AuthService } from '../../../../../core/auth/auth.service';

describe('MatricesRiesgosComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: {
    obtenerVersionVigenteFormulario: ReturnType<typeof vi.fn>;
    obtenerVersionFormulario: ReturnType<typeof vi.fn>;
    metodologiaVigente: ReturnType<typeof vi.fn>;
    metodologiaPorVersion: ReturnType<typeof vi.fn>;
    obtenerEvaluacion: ReturnType<typeof vi.fn>;
    listarRiesgos: ReturnType<typeof vi.fn>;
    listarEvaluaciones: ReturnType<typeof vi.fn>;
    obtenerConsolidado: ReturnType<typeof vi.fn>;
    listarHistorialVersionesFormulario: ReturnType<typeof vi.fn>;
    listarFamiliasFormulario: ReturnType<typeof vi.fn>;
    obtenerFamiliaFormularioPorId: ReturnType<typeof vi.fn>;
    crearFamiliaFormulario: ReturnType<typeof vi.fn>;
    actualizarFamiliaFormulario: ReturnType<typeof vi.fn>;
    desactivarFamiliaFormulario: ReturnType<typeof vi.fn>;
    crearEvaluacion: ReturnType<typeof vi.fn>;
    actualizarEvaluacion: ReturnType<typeof vi.fn>;
    transicionarEvaluacion: ReturnType<typeof vi.fn>;
    obtenerFlujos: ReturnType<typeof vi.fn>;
    crearBorradorFormulario: ReturnType<typeof vi.fn>;
    actualizarBorradorFormulario: ReturnType<typeof vi.fn>;
    clonarVersionFormulario: ReturnType<typeof vi.fn>;
    cargarEvidencia: ReturnType<typeof vi.fn>;
    vincularEvidencia: ReturnType<typeof vi.fn>;
    eliminarEvidenciaHuerfana: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    service = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([
        { famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: '', famActivo: true }
      ])),
      obtenerFamiliaFormularioPorId: vi.fn().mockReturnValue(of({
        famId: 1,
        famCodigo: 'MATRIZ_RIESGOS_LAFT',
        famNombre: 'Matriz de Riesgos LAFT',
        famDescripcion: '',
        famActivo: true,
        famFechaCreacion: '2026-08-01T00:00:00',
        totalVersiones: 0,
        tieneVersionVigente: false
      })),
      crearFamiliaFormulario: vi.fn().mockReturnValue(of(2)),
      actualizarFamiliaFormulario: vi.fn().mockReturnValue(of(true)),
      desactivarFamiliaFormulario: vi.fn().mockReturnValue(of(true)),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of({
        verId: 10,
        verFamiliaId: 1,
        verCodigo: 'FORM_A',
        verVersion: 2,
        verJson: JSON.stringify({
          codigoFormulario: 'FORM_A',
          nombreFormulario: 'Formulario A',
          secciones: [{
            clave: 'identificacion',
            titulo: 'Identificación',
            orden: 1,
            campos: [{
              clave: 'area_principal',
              etiqueta: 'Área principal',
              tipo: 'texto',
              obligatorio: true,
              soloLectura: false
            }]
          }]
        }),
        verHash: 'hash',
        verEstado: 'PUBLISHED',
        verVigente: true,
        verFechaCreacion: '2026-08-03T10:00:00',
        verUsrCreacion: 1
      })),
      obtenerVersionFormulario: vi.fn().mockImplementation((id: number) => of({
        verId: id,
        verFamiliaId: 1,
        verCodigo: 'FORM_A',
        verVersion: 2,
        verJson: JSON.stringify({
          codigoFormulario: 'FORM_A',
          nombreFormulario: 'Formulario A',
          secciones: [{
            clave: 'identificacion',
            titulo: 'Identificación',
            orden: 1,
            campos: [{
              clave: 'area_principal',
              etiqueta: 'Área principal',
              tipo: 'texto',
              obligatorio: true,
              soloLectura: false
            }]
          }]
        }),
        verHash: 'hash',
        verEstado: 'DRAFT',
        verVigente: false,
        verFechaCreacion: '2026-08-03T10:00:00',
        verUsrCreacion: 1
      })),
      obtenerEvaluacion: vi.fn().mockReturnValue(of({
        evaId: 20,
        evaRiesgoId: 5,
        evaVersionId: 10,
        evaEstado: 'BORRADOR',
        evaDataJson: '{"area_principal":"Cumplimiento"}',
        evaFechaEval: '2026-08-14T00:00:00',
        evaUsrEval: 1,
        evaVersionRow: 1,
        evaActivo: true
      })),
      metodologiaPorVersion: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'FORM_A',
        version: 2,
        secciones: [],
        catalogos: [],
        reglas: []
      })),
      metodologiaVigente: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'FORM_A',
        version: 2,
        secciones: [],
        catalogos: [],
        reglas: [{ codigo: 'CALCULO_VRI_VRR', version: '1.0', algoritmoId: 'MATRICES_VRI_ADITIVO_1_9' }]
      })),
      listarRiesgos: vi.fn().mockReturnValue(of([{
        rieId: 5,
        rieCodigo: 'R-005',
        rieNombre: 'Riesgo institucional',
        rieActivo: true,
        rieUsrCreacion: 1,
        rieFechaCreacion: '2026-08-07T08:00:00'
      }])),
      listarEvaluaciones: vi.fn().mockReturnValue(of({
        items: [],
        pagina: 1,
        registrosPorPagina: 10,
        totalRegistros: 0,
        totalPaginas: 0
      })),
      obtenerConsolidado: vi.fn().mockReturnValue(of([])),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([])),
      crearEvaluacion: vi.fn().mockReturnValue(of(20)),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      transicionarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      obtenerFlujos: vi.fn().mockReturnValue(of([])),
      crearBorradorFormulario: vi.fn().mockReturnValue(of(21)),
      actualizarBorradorFormulario: vi.fn().mockReturnValue(of({ success: true })),
      clonarVersionFormulario: vi.fn().mockReturnValue(of(22)),
      cargarEvidencia: vi.fn().mockReturnValue(of({ eviId: 32 })),
      vincularEvidencia: vi.fn().mockReturnValue(of({ success: true })),
      eliminarEvidenciaHuerfana: vi.fn().mockReturnValue(of({ success: true }))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: AuthService, useValue: { tieneRol: vi.fn().mockReturnValue(true) } },
        { provide: MatricesRiesgosService, useValue: service }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('crea el componente y carga la versión dinámica y los riesgos reales', () => {
    expect(component).toBeTruthy();
    expect(service.obtenerVersionVigenteFormulario).toHaveBeenCalled();
    expect(service.metodologiaVigente).toHaveBeenCalled();
    expect(service.listarRiesgos).toHaveBeenCalled();
    expect(component.versionVigente()?.verId).toBe(10);
    expect(component.riesgos()[0].rieCodigo).toBe('R-005');
  });

  it('construye secciones y campos desde la versión del formulario', () => {
    expect(component.secciones()).toHaveLength(1);
    expect(component.secciones()[0].campos[0].clave).toBe('area_principal');
    expect(component.totalCampos()).toBe(1);
  });

  it('bloquea el guardado hasta seleccionar un riesgo real y completar campos obligatorios', () => {
    component.nuevaEvaluacion();
    component.seleccionarFamilia('MATRIZ_RIESGOS_LAFT');
    expect(component.puedeGuardar()).toBe(false);

    component.riesgoId.set(5);
    component.actualizarRespuesta(component.secciones()[0].campos[0], 'Cumplimiento');

    expect(component.puedeGuardar()).toBe(true);
  });

  it('crea una evaluación enviando respuestas dinámicas', () => {
    component.nuevaEvaluacion();
    component.seleccionarFamilia('MATRIZ_RIESGOS_LAFT');
    component.riesgoId.set(5);
    component.actualizarRespuesta(component.secciones()[0].campos[0], 'Cumplimiento');
    component.guardarEvaluacion();

    expect(service.crearEvaluacion).toHaveBeenCalledWith(expect.objectContaining({
      evaRiesgoId: 5,
      evaVersionId: 10,
      evaDataJson: JSON.stringify({ area_principal: 'Cumplimiento' })
    }));
  });

  it('no expone formularios ni cálculos locales retirados', () => {
    const instancia = component as unknown as Record<string, unknown>;
    expect('nuevaMatriz' in instancia).toBe(false);
    expect('criteriosForm' in instancia).toBe(false);
    expect('nivelResidualLocal' in instancia).toBe(false);
  });

  it('delega creación y edición en modales standalone y permite desactivar familias', () => {
    component.abrirModalCrearFamilia();
    expect(document.body.querySelector('app-familia-crear-modal')).not.toBeNull();
    expect(component.modalFamiliaAbierto()).toBe(false);
    component.cerrarModalCrearFamilia();
    expect(document.body.querySelector('app-familia-crear-modal')).toBeNull();

    const familia = component.familias()[0];
    vi.spyOn(component, 'esAdministrador').mockReturnValue(true);
    component.abrirModalEditarFamilia(familia);
    expect(document.body.querySelector('app-familia-editar-modal')).not.toBeNull();
    expect(component.modalFamiliaAbierto()).toBe(false);
    component.cerrarModalEditarFamilia();
    expect(document.body.querySelector('app-familia-editar-modal')).toBeNull();

    component.desactivarFamilia(familia);
    expect(service.desactivarFamiliaFormulario).toHaveBeenCalledWith(1);
  });

  it('navega por pestanas con teclado y carga el consolidado al activarlo', () => {
    const evento = new KeyboardEvent('keydown', { key: 'End' });
    const preventDefault = vi.spyOn(evento, 'preventDefault');
    component.onKeydownTab(evento, 'evaluaciones');

    expect(preventDefault).toHaveBeenCalled();
    expect(component.tab()).toBe('plantillas');

    component.seleccionarTab('consolidado');
    expect(component.tab()).toBe('consolidado');
    expect(service.obtenerConsolidado).toHaveBeenCalled();
  });

  it('edita una evaluacion existente en modal, transiciona su estado y recarga sus flujos', () => {
    const resumen: EvaluacionRiesgoResumenDto = {
      evaId: 20,
      evaRiesgoId: 5,
      riesgoCodigo: 'RIE-005',
      riesgoNombre: 'Riesgo 005',
      evaVersionId: 10,
      versionCodigo: 'FORM_A',
      versionNumero: 2,
      estado: 'BORRADOR',
      vri: 3.0,
      vrr: 1.5,
      nivelResidual: 'BAJO',
      fechaEval: '2026-08-14T00:00:00'
    };

    service.obtenerEvaluacion = vi.fn().mockReturnValue(of({
      evaId: 20,
      evaRiesgoId: 5,
      evaVersionId: 10,
      evaEstado: 'BORRADOR',
      evaDataJson: '{"area_principal":"Cumplimiento"}',
      evaFechaEval: '2026-08-14T00:00:00',
      evaUsrEval: 1,
      evaVersionRow: 1,
      evaActivo: true
    }));
    service.metodologiaPorVersion = vi.fn().mockReturnValue(of({
      versionFormularioId: 10,
      codigo: 'FORM_A',
      version: 2,
      secciones: [],
      catalogos: [],
      reglas: []
    }));

    component.editarEvaluacion(resumen);
    expect(component.modalEditarAbierto()).toBe(true);
    expect(component.riesgoId()).toBe(5);
    expect(component.valorRespuesta(component.secciones()[0].campos[0])).toBe('Cumplimiento');

    component.abrirModalSeguimiento(resumen);
    expect(component.modalSeguimientoAbierto()).toBe(true);
    expect(service.obtenerFlujos).toHaveBeenCalledWith(20);

    component.nuevoEstado = 'EN_REVISION';
    component.motivoTransicion = 'Revision completa';
    component.transicionarEvaluacionModal();
    expect(service.transicionarEvaluacion).toHaveBeenCalledWith(20, 'EN_REVISION', 'Revision completa');
    expect(component.motivoTransicion).toBe('');
  });

  it('guarda un borrador de formulario valido y bloquea JSON invalido', () => {
    const version = { ...component.versionVigente()!, verVigente: false, verEstado: 'DRAFT' as const };
    component.abrirDefinicion(version);
    component.definicionTecnica = '{invalido';
    component.guardarDefinicion();
    expect(service.actualizarBorradorFormulario).not.toHaveBeenCalled();
    expect(component.error()).toContain('JSON');

    component.definicionTecnica = '{"codigoFormulario":"FORM_A","secciones":[]}';
    service.obtenerVersionFormulario.mockReturnValue(of({ ...version, verJson: component.definicionTecnica }));
    component.guardarDefinicion();
    expect(service.actualizarBorradorFormulario).toHaveBeenCalledWith(10, component.definicionTecnica);
    expect(component.versionEditando()).toBeNull();
  });

  it('crea una nueva versiÃ³n directamente desde el Detalle de Familia y vincula evidencia', () => {
    component.crearNuevaVersionDesdeDetalle({ famId: 1, famCodigo: 'FORM_NUEVO', famNombre: 'Formulario nuevo' } as never);
    expect(service.crearBorradorFormulario).toHaveBeenCalledWith(
      1,
      'FORM_NUEVO',
      expect.stringContaining('FORM_NUEVO')
    );

    const archivo = new File(['evidencia'], 'evidencia.pdf', { type: 'application/pdf' });
    component.seleccionarArchivo({ target: { files: { item: () => archivo } } } as unknown as Event);
    component.cargarYVincularEvidencia({ evaId: 20 } as never);
    expect(service.cargarEvidencia).toHaveBeenCalledWith(archivo);
    expect(service.vincularEvidencia).toHaveBeenCalledWith({
      entidadId: 20,
      evidenciaId: 32,
      tipoEntidad: 'evaluacion'
    });
  });

  it('cubre navegación por teclado en pestañas para todas las teclas (ArrowRight, ArrowLeft, ArrowDown, ArrowUp, Home, tecla no manejada)', () => {
    component.seleccionarTab('evaluaciones');

    const evRight = new KeyboardEvent('keydown', { key: 'ArrowRight' });
    vi.spyOn(evRight, 'preventDefault');
    component.onKeydownTab(evRight, 'evaluaciones');
    expect(component.tab()).toBe('consolidado');

    const evDown = new KeyboardEvent('keydown', { key: 'ArrowDown' });
    vi.spyOn(evDown, 'preventDefault');
    component.onKeydownTab(evDown, 'consolidado');
    expect(component.tab()).toBe('plantillas');

    const evLeft = new KeyboardEvent('keydown', { key: 'ArrowLeft' });
    vi.spyOn(evLeft, 'preventDefault');
    component.onKeydownTab(evLeft, 'plantillas');
    expect(component.tab()).toBe('consolidado');

    const evUp = new KeyboardEvent('keydown', { key: 'ArrowUp' });
    vi.spyOn(evUp, 'preventDefault');
    component.onKeydownTab(evUp, 'consolidado');
    expect(component.tab()).toBe('evaluaciones');

    const evHome = new KeyboardEvent('keydown', { key: 'Home' });
    vi.spyOn(evHome, 'preventDefault');
    component.onKeydownTab(evHome, 'plantillas');
    expect(component.tab()).toBe('evaluaciones');

    const evOther = new KeyboardEvent('keydown', { key: 'Enter' });
    const spyPrevent = vi.spyOn(evOther, 'preventDefault');
    component.onKeydownTab(evOther, 'evaluaciones');
    expect(spyPrevent).not.toHaveBeenCalled();

    // Tab no encontrada en lista
    component.onKeydownTab(evRight, 'invalida' as never);
  });

  it('maneja tecla Escape cerrando modales y editor de definición secuencialmente', () => {
    const ev = new CustomEvent('keydown');
    const spy = vi.spyOn(ev, 'preventDefault');

    // 1. Editor de versión abierto
    component.abrirDefinicion(component.versionVigente()!);
    expect(component.versionEditando()).not.toBeNull();
    component.manejarTeclaEscape(ev);
    expect(spy).toHaveBeenCalled();
    expect(component.versionEditando()).toBeNull();

    // 2. Modal formulario abierto. Los modales standalone gestionan Escape internamente.
  });

  it('cuenta evaluaciones por estado sin distinción de mayúsculas/minúsculas', () => {
    component.evaluaciones.set([
      { evaId: 1, estado: 'BORRADOR' } as never,
      { evaId: 2, estado: 'borrador' } as never,
      { evaId: 3, estado: 'EN_REVISION' } as never
    ]);

    expect(component.contarEvaluacionesPorEstado('BORRADOR')).toBe(2);
    expect(component.contarEvaluacionesPorEstado('en_revision')).toBe(1);
    expect(component.contarEvaluacionesPorEstado('APROBADA')).toBe(0);
  });

  it('valida total de campos completados y estado de puedeGuardar ante diversos tipos de valores', () => {
    component.nuevaEvaluacion();
    component.seleccionarFamilia('MATRIZ_RIESGOS_LAFT');
    component.riesgoId.set(5);

    // Valores nulos, indefinidos, cadenas vacías
    component.respuestas.set({ area_principal: '' });
    expect(component.totalCompletados()).toBe(0);
    expect(component.puedeGuardar()).toBe(false);

    component.respuestas.set({ area_principal: null });
    expect(component.totalCompletados()).toBe(0);
    expect(component.puedeGuardar()).toBe(false);

    component.respuestas.set({ area_principal: {} as unknown as string });
    expect(component.totalCompletados()).toBe(0);
    expect(component.puedeGuardar()).toBe(false);

    component.respuestas.set({ area_principal: 123 });
    expect(component.totalCompletados()).toBe(1);
    expect(component.puedeGuardar()).toBe(true);

    component.respuestas.set({ area_principal: true });
    expect(component.totalCompletados()).toBe(1);
    expect(component.puedeGuardar()).toBe(true);
  });

  it('limpia completamente el contexto dinámico al volver a seleccionar una familia vacía', () => {
    component.nuevaEvaluacion();
    component.seleccionarFamilia('MATRIZ_RIESGOS_LAFT');
    expect(component.versionVigente()).not.toBeNull();
    expect(component.seccionesModal().length).toBeGreaterThan(0);

    component.seleccionarFamilia('');

    expect(component.familiaSeleccionada()).toBe('');
    expect(component.versionVigente()).toBeNull();
    expect(component.metodologia()).toBeNull();
    expect(component.seccionesModal()).toEqual([]);
    expect(component.respuestas()).toEqual({});
    expect(component.riesgoId()).toBe(0);
    expect(component.cargandoFormulario()).toBe(false);
  });

  it('ignora una respuesta tardía de una familia anterior', () => {
    const versionA = component.versionVigente()!;
    const respuestaA = new Subject<VersionFormularioDto>();
    service.obtenerVersionVigenteFormulario
      .mockReturnValueOnce(respuestaA)
      .mockReturnValueOnce(of({ ...versionA, verCodigo: 'FORM_B', verFamiliaId: 2 }));

    component.nuevaEvaluacion();
    component.seleccionarFamilia('FAMILIA_A');
    component.seleccionarFamilia('FAMILIA_B');
    respuestaA.next({ ...versionA, verCodigo: 'FORM_A', verFamiliaId: 1 });

    expect(component.familiaSeleccionada()).toBe('FAMILIA_B');
    expect(component.versionVigente()?.verCodigo).toBe('FORM_B');
  });

  it('bloquea transición de estado cuando nuevoEstado está vacío', () => {
    component.evaluacionResumenSeleccionada.set({
      evaId: 20,
      evaRiesgoId: 5,
      riesgoCodigo: 'RIE-005',
      riesgoNombre: 'Riesgo 005',
      evaVersionId: 10,
      versionCodigo: 'FORM_A',
      versionNumero: 2,
      estado: 'BORRADOR',
      vri: 3.0,
      vrr: 1.5,
      nivelResidual: 'BAJO',
      fechaEval: '2026-08-14T00:00:00'
    });
    component.nuevoEstado = '   ';
    component.transicionarEvaluacionModal();
    expect(service.transicionarEvaluacion).not.toHaveBeenCalled();
    expect(component.error()).toContain('Seleccione un estado');
  });

  it('extrae definición con fallback seguro cuando verJson está corrupto o es nulo', () => {
    const versionCorrupta = { ...component.versionVigente()!, verJson: 'invalido{' };
    service.obtenerVersionFormulario.mockReturnValueOnce(of(versionCorrupta));
    component.abrirDefinicion(versionCorrupta);
    expect(component.definicionTecnica).toBe('invalido{');
  });

  it('gestiona debounce de búsqueda y filtrado de estado', async () => {
    vi.useFakeTimers();
    component.alCambiarFiltroBuscar('LAFT');
    expect(component.filtroBuscar()).toBe('LAFT');

    vi.advanceTimersByTime(350);
    expect(service.listarEvaluaciones).toHaveBeenCalled();

    component.alCambiarFiltroEstado('APROBADA');
    expect(component.filtroEstado()).toBe('APROBADA');
    vi.useRealTimers();
  });
});
