import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionRiesgoDto, EvaluacionRiesgoResumenDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent — operaciones del componente', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  const version: VersionFormularioDto = {
    verId: 10,
    verFamiliaId: 1,
    verCodigo: 'FORM_A',
    verVersion: 2,
    verJson: JSON.stringify({
      codigoFormulario: 'FORM_A',
      nombreFormulario: 'Formulario A',
      secciones: [{
        clave: 'identificacion',
        titulo: 'Identificacion',
        orden: 1,
        campos: [{ clave: 'area', etiqueta: 'Area', tipo: 'texto', obligatorio: true, soloLectura: false }]
      }]
    }),
    verHash: 'hash',
    verEstado: 'PUBLISHED',
    verVigente: true,
    verFechaCreacion: '2026-08-14T00:00:00Z',
    verUsrCreacion: 1
  };

  const evaluacion: EvaluacionRiesgoDto = {
    evaId: 31,
    evaRiesgoId: 5,
    evaVersionId: 10,
    evaEstado: 'BORRADOR',
    evaDataJson: '{"area":"Cumplimiento"}',
    evaDataCalcJson: '{}',
    evaVri: null,
    evaVrr: null,
    evaFechaEval: '2026-08-14T00:00:00Z',
    evaUsrEval: 1,
    evaVersionRow: 1,
    evaActivo: true
  };

  const evaluacionResumen: EvaluacionRiesgoResumenDto = {
    evaId: 31,
    evaRiesgoId: 5,
    riesgoCodigo: 'RIE-005',
    riesgoNombre: 'Riesgo 5',
    evaVersionId: 10,
    versionCodigo: 'FORM_A',
    versionNumero: 2,
    estado: 'BORRADOR',
    vri: null,
    vrr: null,
    nivelResidual: null,
    fechaEval: '2026-08-14T00:00:00Z'
  };

  beforeEach(async () => {
    service = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([
        { famId: 1, famCodigo: 'FORM_A', famNombre: 'Formulario A', famDescripcion: '', famActivo: true }
      ])),
      obtenerFamiliaFormularioPorId: vi.fn().mockReturnValue(of({ famId: 1 })),
      crearFamiliaFormulario: vi.fn().mockReturnValue(of(2)),
      actualizarFamiliaFormulario: vi.fn().mockReturnValue(of(true)),
      desactivarFamiliaFormulario: vi.fn().mockReturnValue(of(true)),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(version)),
      obtenerVersionFormulario: vi.fn().mockImplementation((id: number) => of({ ...version, verId: id })),
      obtenerEvaluacion: vi.fn().mockReturnValue(of(evaluacion)),
      metodologiaPorVersion: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'FORM_A',
        version: 2,
        secciones: [{
          clave: 'identificacion',
          titulo: 'Identificacion',
          orden: 1,
          campos: [{ clave: 'area', etiqueta: 'Area', tipo: 'texto', obligatorio: true, soloLectura: false }]
        }],
        catalogos: [],
        reglas: []
      })),
      metodologiaVigente: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'FORM_A',
        version: 2,
        secciones: [],
        catalogos: [{
          codigo: 'AREAS',
          elementos: [{ codigo: 'B', valor: 'Beta', orden: 2 }, { codigo: 'A', valor: 'Alfa', orden: 1 }]
        }],
        reglas: []
      })),
      listarRiesgos: vi.fn().mockReturnValue(of([{ rieId: 5, rieCodigo: 'R-5', rieNombre: 'Riesgo', rieActivo: true }])),
      listarEvaluaciones: vi.fn().mockReturnValue(of({
        items: [evaluacionResumen],
        pagina: 1,
        registrosPorPagina: 10,
        totalRegistros: 1,
        totalPaginas: 1
      })),
      obtenerConsolidado: vi.fn().mockReturnValue(of([])),
      descargarConsolidadoExcel: vi.fn().mockReturnValue(of(new Blob(['excel']))),
      descargarConsolidadoPdf: vi.fn().mockReturnValue(of(new Blob(['pdf']))),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([version])),
      crearEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      actualizarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      transicionarEvaluacion: vi.fn().mockReturnValue(of({ success: true })),
      obtenerFlujos: vi.fn().mockReturnValue(of([])),
      cargarEvidencia: vi.fn().mockReturnValue(of({ eviId: 51 })),
      vincularEvidencia: vi.fn().mockReturnValue(of({ success: true })),
      eliminarEvidenciaHuerfana: vi.fn().mockReturnValue(of({ success: true })),
      crearBorradorFormulario: vi.fn().mockReturnValue(of(11)),
      actualizarBorradorFormulario: vi.fn().mockReturnValue(of({ success: true })),
      clonarVersionFormulario: vi.fn().mockReturnValue(of(12)),
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

  afterEach(() => {
    component.cerrarModalCrearFamilia();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('selecciona una familia, restablece el editor y carga su version vigente', () => {
    component.versionEditando.set(version);
    component.soloLecturaDefinicion.set(true);
    component.definicionTecnica = '{"anterior":true}';

    component.seleccionarFamilia('FORM_B');

    expect(component.familiaSeleccionada()).toBe('FORM_B');
    expect(component.versionEditando()).toBeNull();
    expect(component.soloLecturaDefinicion()).toBe(false);
    expect(component.definicionTecnica).toBe('');
    expect(service['listarHistorialVersionesFormulario']).toHaveBeenLastCalledWith('FORM_B');
    expect(service['obtenerVersionVigenteFormulario']).toHaveBeenLastCalledWith('FORM_B');
  });

  it('responde a Escape solo para diálogos administrados por el componente padre', () => {
    const event = new Event('keydown', { cancelable: true });
    const preventDefault = vi.spyOn(event, 'preventDefault');

    component.abrirDefinicion(version);
    component.manejarTeclaEscape(event);
    expect(component.versionEditando()).toBeNull();

    component.abrirModalCrearFamilia();
    expect(document.body.querySelector('app-familia-crear-modal')).not.toBeNull();
    component.cerrarModalCrearFamilia();
    expect(document.body.querySelector('app-familia-crear-modal')).toBeNull();

    component.abrirModalCrearFormulario();
    component.manejarTeclaEscape(event);
    expect(component.modalFormularioAbierto()).toBe(false);
    expect(preventDefault).toHaveBeenCalledTimes(2);
  });

  it('aplica filtros con debounce, normaliza espacios y permite restablecerlos', () => {
    vi.useFakeTimers();
    service['listarEvaluaciones'].mockClear();

    component.alCambiarFiltroBuscar('  riesgo LAFT  ');
    vi.advanceTimersByTime(299);
    expect(service['listarEvaluaciones']).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1);
    expect(service['listarEvaluaciones']).toHaveBeenCalledWith(expect.objectContaining({ buscar: 'riesgo LAFT' }));

    component.alCambiarFiltroEstado(' EN_REVISION ');
    expect(service['listarEvaluaciones']).toHaveBeenLastCalledWith(expect.objectContaining({ estado: 'EN_REVISION' }));

    component.limpiarFiltrosEvaluaciones();
    expect(component.filtroBuscar()).toBe('');
    expect(component.filtroEstado()).toBe('');
    expect(service['listarEvaluaciones']).toHaveBeenLastCalledWith(expect.objectContaining({ buscar: undefined, estado: undefined }));
  });

  it('muestra los detalles HTTP al fallar consultas de evaluaciones y consolidado', () => {
    service['listarEvaluaciones'].mockReturnValue(throwError(() => ({ error: { detail: 'Evaluaciones no disponibles' } })));
    component.cargarEvaluaciones();
    expect(component.errorEvaluaciones()).toBe('Evaluaciones no disponibles');
    expect(component.cargandoEvaluaciones()).toBe(false);

    service['obtenerConsolidado'].mockReturnValue(throwError(() => ({ error: { errors: { filtro: ['Filtro invalido'] } } })));
    component.cargarConsolidado();
    expect(component.errorConsolidado()).toBe('Filtro invalido');
    expect(component.cargandoConsolidado()).toBe(false);
  });

  it('ordena catalogos, protege versiones vigentes de edicion y bloquea la eliminacion activa', () => {
    const opciones = component.opcionesCatalogo({ clave: 'area', etiqueta: 'Area', tipo: 'selector-catalogo', obligatorio: false, soloLectura: false, codigoCatalogo: 'AREAS' });
    expect(opciones.map(opcion => opcion.codigo)).toEqual(['A', 'B']);
    expect(component.opcionesCatalogo({ clave: 'libre', etiqueta: 'Libre', tipo: 'texto', obligatorio: false, soloLectura: false })).toEqual([]);

    component.abrirDefinicion(version);
    expect(component.soloLecturaDefinicion()).toBe(true);
    component.eliminarVersionFormulario(version);
    expect(component.error()).toContain('eliminarse');
    expect(service['eliminarVersionFormulario']).not.toHaveBeenCalled();
  });

  it('actualiza una evaluacion existente y expone el error de guardado sin ocultarlo', () => {
    component.editarEvaluacion(evaluacionResumen);
    component.actualizarRespuesta(component.seccionesModal()[0].campos[0], 'Cumplimiento actualizado');
    component.guardarEvaluacion();
    expect(service['actualizarEvaluacion']).toHaveBeenCalledWith(31, expect.objectContaining({ evaId: 31, evaRiesgoId: 5 }));

    service['actualizarEvaluacion'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Conflicto de version' } })));
    component.editarEvaluacion(evaluacionResumen);
    component.actualizarRespuesta(component.seccionesModal()[0].campos[0], 'Cumplimiento actualizado');
    component.guardarEvaluacion();
    expect(component.error()).toBe('Conflicto de version');
    expect(component.guardando()).toBe(false);
  });

  it('valida evidencia obligatoria y comunica fallos de carga', () => {
    component.cargarYVincularEvidencia(evaluacion);
    expect(component.error()).toContain('Seleccione un archivo');

    service['cargarEvidencia'].mockReturnValue(throwError(() => ({ error: { title: 'Archivo rechazado' } })));
    component.archivoEvidencia = new File(['contenido'], 'evidencia.pdf');
    component.cargarYVincularEvidencia(evaluacion);
    expect(component.error()).toBe('Archivo rechazado');
    expect(component.guardando()).toBe(false);
  });

  it('clona versiones y conserva el error visible cuando el backend rechaza la solicitud', () => {
    component.clonarVersion({ ...version, verVigente: false, verEstado: 'DRAFT' });
    expect(service['clonarVersionFormulario']).toHaveBeenCalledWith(10);
    expect(service['listarHistorialVersionesFormulario']).toHaveBeenCalled();

    service['clonarVersionFormulario'].mockReturnValue(throwError(() => ({ error: { detail: 'No es posible clonar' } })));
    component.clonarVersion({ ...version, verVigente: false, verEstado: 'DRAFT' });
    expect(component.error()).toBe('No es posible clonar');
    expect(component.guardando()).toBe(false);
  });

  it('descarga el consolidado y revoca la URL temporal; tambien informa errores del reporte', () => {
    const crearUrl = vi.fn().mockReturnValue('blob:matriz');
    const revocarUrl = vi.fn();
    Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: crearUrl });
    Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: revocarUrl });
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    component.descargarConsolidado('excel');
    expect(service['descargarConsolidadoExcel']).toHaveBeenCalled();
    expect(crearUrl).toHaveBeenCalled();
    expect(click).toHaveBeenCalled();
    expect(revocarUrl).toHaveBeenCalledWith('blob:matriz');

    service['descargarConsolidadoPdf'].mockReturnValue(throwError(() => ({ error: { detail: 'PDF no disponible' } })));
    component.descargarConsolidado('pdf');
    expect(component.error()).toBe('PDF no disponible');
  });

  it('mantiene el modal abierto si la familia no existe y limpia sus errores al cerrarlo', () => {
    component.familias.set([]);
    component.abrirModalCrearFormulario();
    component.guardarNuevoFormulario();
    expect(component.errorModal()).toContain('familia v');
    expect(service['crearBorradorFormulario']).not.toHaveBeenCalled();

    component.cerrarModalFormulario();
    expect(component.modalFormularioAbierto()).toBe(false);
    expect(component.errorModal()).toBeNull();
  });
});