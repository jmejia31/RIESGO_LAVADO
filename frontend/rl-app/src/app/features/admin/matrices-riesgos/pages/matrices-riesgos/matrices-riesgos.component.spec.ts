import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  beforeEach(async () => {
    service = {
      metodologiaVigente: vi.fn(() => of({ variables: [], escalasCatalogo: [], escalasRiesgo: [] })),
      dashboard: vi.fn(() => of({})),
      reporte: vi.fn(() => of({})),
      listar: vi.fn(() => of([])),
      listarCriterios: vi.fn(() => of([])),
      obtener: vi.fn(() => of({})),
      historial: vi.fn(() => of([])),
      listarPlanes: vi.fn(() => of([])),
      crearPlan: vi.fn(() => of({ planId: 1 })),
      actualizarPlan: vi.fn(() => of({ planId: 1 })),
      cambiarEstadoPlan: vi.fn(() => of({ success: true })),
      inactivarPlan: vi.fn(() => of({ success: true })),
      reactivarPlan: vi.fn(() => of({ success: true })),
      listarEvidencias: vi.fn(() => of([])),
      cargarEvidencia: vi.fn(() => of({ evidenciaId: 1 })),
      descargarEvidencia: vi.fn(() => of(new Blob())),
      inactivarEvidencia: vi.fn(() => of({ success: true })),
      crear: vi.fn(() => of({ matrizId: 1, sujetoTipo: 'PROVEEDOR' })),
      actualizar: vi.fn(() => of({ matrizId: 1, sujetoTipo: 'PROVEEDOR' })),
      calcular: vi.fn(() => of({})),
      recalcular: vi.fn(() => of({})),
      crearCriterio: vi.fn(() => of({ criterioId: 1 })),
      actualizarCriterio: vi.fn(() => of({ criterioId: 1 })),
      cambiarEstado: vi.fn(() => of({ success: true })),
      eliminarMatriz: vi.fn(() => of({ success: true })),
      inactivarCriterio: vi.fn(() => of({ success: true })),
      reactivarCriterio: vi.fn(() => of({ success: true })),
      eliminarCriterio: vi.fn(() => of({ success: true })),
      exportarReporte: vi.fn(() => of(new Blob()))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: service },
        { provide: ConfiguracionService, useValue: { configSistema: vi.fn(() => null) } }
      ]
    })
      .overrideComponent(MatricesRiesgosComponent, { set: { template: '' } })
      .compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    TestBed.resetTestingModule();
  });


  it('consulta el dashboard con los filtros ejecutivos activos', () => {
    component.reporteFiltro.set({ sujetoTipo: 'PROVEEDOR', nivelInherente: 'ALTO', nivelResidual: 'MEDIO' });
    service['dashboard'].mockReturnValue(of({ totalMatrices: 2 }));

    component.cargarDashboard();

    expect(service['dashboard']).toHaveBeenCalledWith({
      sujetoTipo: 'PROVEEDOR', nivelInherente: 'ALTO', nivelResidual: 'MEDIO'
    });
    expect(component.dashboard()).toEqual({ totalMatrices: 2 });
  });

  it('construye el mapa de transición con conteos reales del backend', () => {
    component.metodologia.set({
      escalasRiesgo: [
        { nivel: 'Bajo', valorMinimo: 1, color: '#22c55e' },
        { nivel: 'Medio', valorMinimo: 2, color: '#facc15' },
        { nivel: 'Alto', valorMinimo: 3, color: '#f97316' }
      ]
    } as never);
    component.dashboard.set({
      totalMatrices: 4,
      mapaTransicion: [
        { nivelInherente: 'Alto', nivelResidual: 'Medio', total: 3, promedioInherente: 4.5, promedioResidual: 2.5 }
      ],
      porNivelInherente: [],
      porNivelResidual: []
    } as never);

    const celda = component.heatmapFilas()
      .find(fila => fila.nivelInherente === 'Alto')?.celdas
      .find(item => item.nivelResidual === 'Medio');

    expect(celda).toEqual(expect.objectContaining({ total: 3, promedioInherente: 4.5, promedioResidual: 2.5 }));
  });

  it('aplica los niveles de una celda como filtros del dashboard', () => {
    component.seleccionarCeldaMapa({ nivelInherente: 'Alto', nivelResidual: 'Medio' });

    expect(component.reporteFiltro()).toEqual(expect.objectContaining({ nivelInherente: 'Alto', nivelResidual: 'Medio' }));
    expect(service['dashboard']).toHaveBeenCalled();
    expect(service['reporte']).toHaveBeenCalled();
  });

  it('lista matrices con los filtros activos y finaliza la carga', () => {
    const matrices = [{ matrizId: 7, nombreSujeto: 'Proveedor Uno' }];
    component.filtroBuscar.set('Proveedor');
    component.filtroEstado.set('EN_REVISION');
    component.filtroSujetoTipo.set('PROVEEDOR');
    service['listar'].mockReturnValue(of(matrices));

    component.cargarMatrices();

    expect(service['listar']).toHaveBeenCalledWith({
      buscar: 'Proveedor', estado: 'EN_REVISION', sujetoTipo: 'PROVEEDOR'
    });
    expect(component.matrices()).toEqual(matrices);
    expect(component.cargando()).toBe(false);
  });

  it('muestra el mensaje devuelto por la API y recupera el indicador al fallar el listado', () => {
    service['listar'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Consulta rechazada' } })));

    component.cargarMatrices();

    expect(component.error()).toBe('Consulta rechazada');
    expect(component.cargando()).toBe(false);
  });

  it('actualiza el modulo limpiando seleccion, planes y evidencias visibles', () => {
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    component.planesAccion.set([{ planId: 1 }] as never);
    component.evidencias.set([{ evidenciaId: 2 }] as never);
    component.historial.set([{ historialId: 3 }] as never);
    component.evidenciaPlanId = 1;
    component.evidenciaControlId = 2;

    component.actualizarModulo();

    expect(component.matrizSeleccionada()).toBeNull();
    expect(component.planesAccion()).toEqual([]);
    expect(component.evidencias()).toEqual([]);
    expect(component.historial()).toEqual([]);
    expect(component.evidenciaPlanId).toBeNull();
    expect(component.evidenciaControlId).toBeNull();
    expect(service['metodologiaVigente']).toHaveBeenCalled();
  });

  it('carga el reporte usando el filtro vigente', () => {
    const reporte = { totales: { totalMatrices: 3 } };
    component.reporteFiltro.set({ estado: 'ACTIVA' });
    service['reporte'].mockReturnValue(of(reporte));

    component.cargarReporte();

    expect(service['reporte']).toHaveBeenCalledWith({ estado: 'ACTIVA' });
    expect(component.reporte()).toEqual(reporte);
    expect(component.cargandoReporte()).toBe(false);
  });

  it('incluir inactivos conserva criterios activos e inactivos', () => {
    const criterios = [
      { criterioId: 1, activo: true },
      { criterioId: 2, activo: false }
    ];
    service['listarCriterios'].mockReturnValue(of(criterios));
    component.incluirCriteriosInactivos.set(true);

    component.cargarCriterios();

    expect(service['listarCriterios']).toHaveBeenCalledWith(true);
    expect(component.criterios()).toEqual(criterios);
  });


  it('bloquea un criterio cuando el rango se superpone con otro activo', () => {
    component.criterios.set([{ criterioId: 4, variableId: 2, activo: true, valorDesde: 10, valorHasta: 20 }] as never);
    component.criteriosForm = {
      variableId: 2,
      escalaId: null,
      valorDesde: 15,
      valorHasta: 25,
      puntaje: 4,
      descripcion: 'Rango solapado'
    };

    component.guardarCriterio();

    expect(component.error()).toContain('se superpone');
    expect(service['crearCriterio']).not.toHaveBeenCalled();
  });

  it('reactiva un criterio inactivo con motivo', () => {
    const criterio = { criterioId: 9, activo: false } as never;
    component.reactivarCriterio(criterio);
    component.actualizarModalMotivo('Rango nuevamente vigente');

    component.confirmarModal();

    expect(service['reactivarCriterio']).toHaveBeenCalledWith(9, 'Rango nuevamente vigente');
    expect(component.mensaje()).toBe('Criterio activado correctamente.');
    expect(component.modalOperacion()).toBeNull();
  });

  it('conserva un error controlado y detiene la carga si falla el reporte', () => {
    service['reporte'].mockReturnValue(throwError(() => new Error('Servicio temporalmente no disponible')));

    component.cargarReporte();

    expect(component.error()).toBe('Servicio temporalmente no disponible');
    expect(component.cargandoReporte()).toBe(false);
  });

  it('selecciona una matriz y solicita su historial', () => {
    const matriz = { matrizId: 21, nombreSujeto: 'Institucional' };
    const historial = [{ historialId: 5, accion: 'CREAR' }];
    service['obtener'].mockReturnValue(of(matriz));
    service['historial'].mockReturnValue(of(historial));

    component.seleccionarMatriz(21);

    expect(service['obtener']).toHaveBeenCalledWith(21);
    expect(service['historial']).toHaveBeenCalledWith(21);
    expect(component.matrizSeleccionada()).toEqual(matriz);
    expect(component.historial()).toEqual(historial);
    expect(component.cargando()).toBe(false);
  });

  it('mantiene vacio el responsable al iniciar y limpiar formularios', () => {
    expect(component.planForm.responsable).toBe('');
    expect(component.nuevoControl.responsable).toBe('');

    component.planForm.responsable = 'Responsable temporal';
    component.limpiarFormularioPlan();

    expect(component.planForm.responsable).toBe('');
  });

  it('carga planes y evidencias de la matriz seleccionada', () => {
    const planes = [{ planId: 3, actividad: 'Seguimiento' }];
    const evidencias = [{ evidenciaId: 8, nombreOriginal: 'reporte.pdf' }];
    service['listarPlanes'].mockReturnValue(of(planes));
    service['listarEvidencias'].mockReturnValue(of(evidencias));
    component.matrizSeleccionada.set({ matrizId: 12 } as never);

    component.cargarPlanesYEvidencias();

    expect(service['listarPlanes']).toHaveBeenCalledWith(12);
    expect(service['listarEvidencias']).toHaveBeenCalledWith(12);
    expect(component.planesAccion()).toEqual(planes);
    expect(component.evidencias()).toEqual(evidencias);
  });

  it('exige actividad y responsable antes de crear un plan', () => {
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    component.planForm.actividad = '';
    component.planForm.responsable = '';

    component.guardarPlanAccion();

    expect(component.error()).toContain('actividad y el responsable');
    expect(service['crearPlan']).not.toHaveBeenCalled();
  });

  it('crea un plan normalizado y refresca planes y reporte', () => {
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    component.planForm = {
      resultadoId: null,
      actividad: '  Revisar expediente  ',
      responsable: '  Cumplimiento  ',
      periodicidad: 'Mensual',
      fechaInicio: component.fechaActualIso,
      fechaFin: component.fechaActualIso,
      medioPrueba: 'Informe',
      observaciones: ''
    };

    component.guardarPlanAccion();

    expect(service['crearPlan']).toHaveBeenCalledWith(12, expect.objectContaining({
      actividad: 'Revisar expediente', responsable: 'Cumplimiento'
    }));
    expect(service['listarPlanes']).toHaveBeenCalledWith(12);
    expect(service['reporte']).toHaveBeenCalled();
    expect(component.planForm.responsable).toBe('');
    expect(component.guardando()).toBe(false);
  });

  it('carga un plan en edicion y actualiza por su identificador', () => {
    const plan = {
      planId: 7, matrizId: 12, actividad: 'Seguimiento', responsable: 'Cumplimiento',
      periodicidad: 'Mensual', fechaInicio: `${component.fechaActualIso}T00:00:00`, fechaFin: null,
      medioPrueba: 'Informe', observaciones: null
    } as never;
    component.matrizSeleccionada.set({ matrizId: 12 } as never);

    component.editarPlan(plan);
    component.planForm.actividad = 'Seguimiento actualizado';
    component.guardarPlanAccion();

    expect(service['actualizarPlan']).toHaveBeenCalledWith(12, 7, expect.objectContaining({
      actividad: 'Seguimiento actualizado', responsable: 'Cumplimiento'
    }));
    expect(component.planEditandoId()).toBeNull();
    expect(component.mensaje()).toBe('Plan de acción actualizado correctamente.');
  });

  it('carga una evidencia y limpia sus vinculos temporales', () => {
    const archivo = new File(['%PDF-1.7'], 'reporte.pdf', { type: 'application/pdf' });
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    component.evidenciaArchivo = archivo;
    component.evidenciaControlId = 3;
    component.evidenciaPlanId = 7;
    service['obtener'].mockReturnValue(of({ matrizId: 12, planesAccion: [], evidencias: [] }));

    component.cargarEvidencia();

    expect(service['cargarEvidencia']).toHaveBeenCalledWith(12, archivo, 3, 7);
    expect(component.evidenciaArchivo).toBeNull();
    expect(component.evidenciaControlId).toBeNull();
    expect(component.evidenciaPlanId).toBeNull();
    expect(component.mensaje()).toBe('Evidencia registrada correctamente.');
    expect(component.guardando()).toBe(false);
  });

  it('cambia el estado de un plan con motivo y cierra el modal', () => {
    const plan = { planId: 7, matrizId: 12, estado: 'PENDIENTE' } as never;
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    component.cambiarEstadoPlan(plan, 'CERRADO');
    component.actualizarModalMotivo('Evidencia aprobada');

    component.confirmarModal();

    expect(service['cambiarEstadoPlan']).toHaveBeenCalledWith(12, 7, 'CERRADO', 'Evidencia aprobada');
    expect(component.modalOperacion()).toBeNull();
    expect(component.mensaje()).toBe('Estado del plan actualizado correctamente.');
  });

  it('reactiva un plan inactivo con motivo y refresca los indicadores', () => {
    const plan = { planId: 7, matrizId: 12, estado: 'PENDIENTE', activo: false } as never;
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    component.reactivarPlan(plan);
    component.actualizarModalMotivo('Reapertura autorizada');

    component.confirmarModal();

    expect(service['reactivarPlan']).toHaveBeenCalledWith(12, 7, 'Reapertura autorizada');
    expect(service['listarPlanes']).toHaveBeenCalledWith(12);
    expect(service['reporte']).toHaveBeenCalled();
    expect(component.modalOperacion()).toBeNull();
    expect(component.mensaje()).toBe('Plan de acción reactivado correctamente.');
    expect(component.guardando()).toBe(false);
  });

  it('conserva el modal y muestra el error cuando falla la reactivacion del plan', () => {
    service['reactivarPlan'].mockReturnValue(throwError(() => ({ error: { mensaje: 'El plan ya se encuentra activo.' } })));
    const plan = { planId: 7, matrizId: 12, activo: false } as never;
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    component.reactivarPlan(plan);
    component.actualizarModalMotivo('Reapertura autorizada');

    component.confirmarModal();

    expect(component.modalError()).toBe('El plan ya se encuentra activo.');
    expect(component.modalOperacion()?.tipo).toBe('reactivarPlan');
    expect(component.guardando()).toBe(false);
  });

  it('inactiva una evidencia con motivo y refresca el detalle', () => {
    const evidencia = { evidenciaId: 8, matrizId: 12, activa: true } as never;
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    service['obtener'].mockReturnValue(of({ matrizId: 12, planesAccion: [], evidencias: [] }));
    component.inactivarEvidencia(evidencia);
    component.actualizarModalMotivo('Documento sustituido');

    component.confirmarModal();

    expect(service['inactivarEvidencia']).toHaveBeenCalledWith(12, 8, 'Documento sustituido');
    expect(service['obtener']).toHaveBeenCalledWith(12);
    expect(component.modalOperacion()).toBeNull();
    expect(component.mensaje()).toBe('Evidencia eliminada correctamente.');
  });

  it('rechaza la vista previa cuando no hay archivo o supera 10 MB', () => {
    component.vistaPreviaArchivoSeleccionado();
    expect(component.error()).toBe('Seleccione un archivo para visualizar.');

    component.error.set(null);
    component.evidenciaArchivo = new File([new Uint8Array(10 * 1024 * 1024 + 1)], 'grande.pdf', { type: 'application/pdf' });
    component.vistaPreviaArchivoSeleccionado();

    expect(component.error()).toContain('hasta 10 MB');
    expect(component.evidenciaPreview()).toBeNull();
  });

  it('genera y cierra la vista previa de un archivo de texto seleccionado', async () => {
    const createObjectURL = vi.fn(() => 'blob:preview-texto');
    const revokeObjectURL = vi.fn();
    vi.stubGlobal('URL', { createObjectURL, revokeObjectURL });
    component.evidenciaArchivo = new File(['contenido de auditoria'], 'auditoria.log', { type: 'text/plain' });

    component.vistaPreviaArchivoSeleccionado();
    await fixture.whenStable();

    expect(component.evidenciaPreview()).toEqual(expect.objectContaining({
      nombre: 'auditoria.log', tipoVista: 'texto', texto: 'contenido de auditoria',
      url: 'blob:preview-texto', cargando: false
    }));
    component.cerrarVistaPreviaEvidencia();
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:preview-texto');
    expect(component.evidenciaPreview()).toBeNull();
  });

  it('obtiene una evidencia activa y prepara su vista previa PDF', async () => {
    const createObjectURL = vi.fn(() => 'blob:preview-pdf');
    vi.stubGlobal('URL', { createObjectURL, revokeObjectURL: vi.fn() });
    const evidencia = {
      evidenciaId: 8, matrizId: 12, activa: true, nombreOriginal: 'informe.pdf',
      tipoMime: 'application/pdf', tamanoBytes: 8
    } as never;
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    service['descargarEvidencia'].mockReturnValue(of(new Blob(['%PDF-1.7'], { type: 'application/pdf' })));

    component.vistaPreviaEvidencia(evidencia);
    await fixture.whenStable();

    expect(service['descargarEvidencia']).toHaveBeenCalledWith(12, 8);
    expect(component.evidenciaPreview()).toEqual(expect.objectContaining({
      nombre: 'informe.pdf', tipoVista: 'pdf', url: 'blob:preview-pdf', cargando: false
    }));
  });

  it('expone un error controlado si no puede generar la vista previa almacenada', () => {
    component.matrizSeleccionada.set({ matrizId: 12 } as never);
    service['descargarEvidencia'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Archivo no disponible' } })));

    component.vistaPreviaEvidencia({
      evidenciaId: 8, activa: true, nombreOriginal: 'informe.pdf',
      tipoMime: 'application/pdf', tamanoBytes: 8
    } as never);

    expect(component.evidenciaPreview()).toEqual(expect.objectContaining({
      nombre: 'informe.pdf', cargando: false, error: 'Archivo no disponible'
    }));
  });

  it('descarga desde la vista previa usando el nombre original', () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    component.evidenciaPreview.set({
      nombre: 'informe.pdf', tipoMime: 'application/pdf', tamanoBytes: 8,
      url: 'blob:preview-pdf', urlSegura: null, tipoVista: 'pdf', cargando: false
    });

    component.descargarVistaPreviaActual();

    expect(click).toHaveBeenCalledOnce();
  });

  it('detiene la carga inicial y no consulta dependencias si falla la metodologia', () => {
    service['metodologiaVigente'].mockReturnValue(throwError(() => ({ error: { detalle: 'Metodologia no configurada' } })));

    component.cargarTodo();

    expect(component.error()).toBe('Metodologia no configurada');
    expect(component.cargando()).toBe(false);
    expect(service['dashboard']).not.toHaveBeenCalled();
    expect(service['reporte']).not.toHaveBeenCalled();
    expect(service['listar']).not.toHaveBeenCalled();
  });

  it('bloquea la creacion cuando no existen variables configuradas', () => {
    component.nuevaMatriz.nombreSujeto = 'Proveedor Uno';

    component.crearMatriz();

    expect(component.error()).toBe('No existen variables configuradas para el tipo de sujeto seleccionado.');
    expect(service['crear']).not.toHaveBeenCalled();
    expect(component.guardando()).toBe(false);
  });

  it('bloquea una matriz duplicada antes de escribir', () => {
    component.nuevaMatriz.nombreSujeto = 'Proveedor Uno';
    component.capturasVariables.set([{
      variableId: 1, criterioId: null, puntaje: 4, valorCapturado: '',
      justificacion: 'Evaluacion', fuenteDato: 'CAPTURA'
    }] as never);
    component.matricesDuplicadas.set([{ matrizId: 9, nombreSujeto: 'Proveedor Uno' }] as never);

    component.crearMatriz();

    expect(component.error()).toContain('Ya existe una matriz activa');
    expect(service['crear']).not.toHaveBeenCalled();
  });

  it('crea una matriz valida y solicita su calculo automatico', () => {
    const matrizCreada = { matrizId: 31, sujetoTipo: 'PROVEEDOR', nombreSujeto: 'Proveedor Uno' };
    service['crear'].mockReturnValue(of(matrizCreada));
    component.nuevaMatriz.nombreSujeto = 'Proveedor Uno';
    component.nuevaMatriz.documento = 'RTN-01';
    component.capturasVariables.set([{
      variableId: 1, criterioId: null, puntaje: 3, valorCapturado: '3',
      justificacion: 'Evaluacion inicial', fuenteDato: 'CAPTURA'
    }] as never);

    component.crearMatriz();

    expect(service['crear']).toHaveBeenCalledWith(expect.objectContaining({
      nombreSujeto: 'Proveedor Uno', documento: 'RTN-01'
    }));
    expect(service['calcular']).toHaveBeenCalledWith(31, 'FACTOR');
    expect(component.mensaje()).toBe('Matriz creada y calculada automáticamente.');
    expect(component.guardando()).toBe(false);
  });

  it('recupera el formulario cuando la creacion de la matriz falla', () => {
    service['crear'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Documento duplicado' } })));
    component.nuevaMatriz.nombreSujeto = 'Proveedor Uno';
    component.capturasVariables.set([{
      variableId: 1, criterioId: null, puntaje: 3, valorCapturado: '',
      justificacion: '', fuenteDato: 'CAPTURA'
    }] as never);

    component.crearMatriz();

    expect(component.error()).toBe('Documento duplicado');
    expect(component.guardando()).toBe(false);
  });

  it('valida los campos obligatorios de un criterio antes de escribir', () => {
    component.criteriosForm = {
      variableId: 0, escalaId: null, valorDesde: null, valorHasta: null,
      puntaje: 2, descripcion: ''
    };

    component.guardarCriterio();

    expect(component.error()).toBe('La variable y la descripción del criterio son obligatorias.');
    expect(service['crearCriterio']).not.toHaveBeenCalled();
  });

  it('limita una matriz inactiva a la accion visible de activar', () => {
    const matriz = { matrizId: 18, estado: 'INACTIVA', sujetoTipo: 'PROVEEDOR' } as never;

    expect(component.estadosGestionablesParaMatriz('INACTIVA')).toEqual(['EN_REVISION']);
    expect(component.textoBotonEstado(matriz, 'EN_REVISION')).toBe('Activar');

    component.cambiarEstado(matriz, 'EN_REVISION');
    component.actualizarModalMotivo('Reactivar para revision');
    component.confirmarModal();

    expect(service['cambiarEstado']).toHaveBeenCalledWith(18, 'EN_REVISION', 'Reactivar para revision');
    expect(component.mensaje()).toBe('Estado actualizado correctamente.');
    expect(component.guardando()).toBe(false);
  });

  it('bloquea eliminacion de matrices aprobadas, cerradas o inactivas', () => {
    expect(component.puedeEliminarMatriz({ estado: 'EN_REVISION' } as never)).toBe(true);
    expect(component.puedeEliminarMatriz({ estado: 'CALCULADA' } as never)).toBe(true);
    expect(component.puedeEliminarMatriz({ estado: 'APROBADA' } as never)).toBe(false);
    expect(component.puedeEliminarMatriz({ estado: 'CERRADA' } as never)).toBe(false);
    expect(component.puedeEliminarMatriz({ estado: 'INACTIVA' } as never)).toBe(false);
  });

  it('presenta el estado tecnico calculada como en revision', () => {
    expect(component.estadoEtiqueta('CALCULADA')).toBe('En Revisión');
  });

  it('reposiciona el historial debajo del listado cuando hay pocas matrices', () => {
    component.matrizSeleccionada.set({ matrizId: 1 } as never);
    component.matrices.set([
      { matrizId: 1, nombreSujeto: 'Proveedor Uno' },
      { matrizId: 2, nombreSujeto: 'Proveedor Dos' }
    ] as never);

    expect(component.mostrarHistorialDebajoListado()).toBe(true);

    component.matrices.set(Array.from({ length: 5 }, (_, index) => ({
      matrizId: index + 1,
      nombreSujeto: `Matriz ${index + 1}`
    })) as never);

    expect(component.mostrarHistorialDebajoListado()).toBe(false);
  });

  it('rechaza un rango de criterio cuyo valor inicial supera al final', () => {
    component.criteriosForm = {
      variableId: 3, escalaId: null, valorDesde: 10, valorHasta: 5,
      puntaje: 4, descripcion: 'Rango invalido'
    };

    component.guardarCriterio();

    expect(component.error()).toBe('El valor desde no puede ser mayor que el valor hasta.');
    expect(service['crearCriterio']).not.toHaveBeenCalled();
  });

  it('impide reutilizar el mismo motivo en un cambio de estado', () => {
    const matriz = { matrizId: 18, sujetoTipo: 'PROVEEDOR' } as never;
    component.historial.set([{
      historialId: 1, accion: 'CAMBIO_ESTADO', motivo: 'Revision anual'
    }] as never);
    component.cambiarEstado(matriz, 'APROBADA');
    component.actualizarModalMotivo(' revision anual ');

    component.confirmarModal();

    expect(component.modalError()).toContain('Este motivo ya fue utilizado');
    expect(service['cambiarEstado']).not.toHaveBeenCalled();
  });

  it('cambia el estado con un motivo nuevo y refresca la matriz', () => {
    const matriz = { matrizId: 18, sujetoTipo: 'PROVEEDOR' } as never;
    component.cambiarEstado(matriz, 'APROBADA');
    component.actualizarModalMotivo('Aprobacion del comite');

    component.confirmarModal();

    expect(service['cambiarEstado']).toHaveBeenCalledWith(18, 'APROBADA', 'Aprobacion del comite');
    expect(component.mensaje()).toBe('Estado actualizado correctamente.');
    expect(component.modalOperacion()).toBeNull();
    expect(component.guardando()).toBe(false);
  });

  it('elimina logicamente una matriz y limpia su seleccion', () => {
    const matriz = { matrizId: 25, sujetoTipo: 'INSTITUCIONAL' } as never;
    component.matrizSeleccionada.set({ matrizId: 25 } as never);
    component.historial.set([{ historialId: 3 }] as never);
    component.eliminarMatriz(matriz);
    component.actualizarModalMotivo('Registro creado por error');

    component.confirmarModal();

    expect(service['eliminarMatriz']).toHaveBeenCalledWith(25, 'Registro creado por error');
    expect(component.matrizSeleccionada()).toBeNull();
    expect(component.historial()).toEqual([]);
    expect(component.mensaje()).toBe('Matriz eliminada correctamente.');
    expect(component.modalOperacion()).toBeNull();
  });

  it('inactiva un criterio con motivo y recarga el catalogo', () => {
    const criterio = { criterioId: 7, variableId: 3, activo: true } as never;
    component.inactivarCriterio(criterio);
    component.actualizarModalMotivo('Criterio fuera de vigencia');

    component.confirmarModal();

    expect(service['inactivarCriterio']).toHaveBeenCalledWith(7, 'Criterio fuera de vigencia');
    expect(service['listarCriterios']).toHaveBeenCalled();
    expect(component.mensaje()).toBe('Criterio desactivado correctamente.');
    expect(component.modalOperacion()).toBeNull();
  });

  it('mantiene abierto el modal si falla la eliminacion de un criterio', () => {
    service['eliminarCriterio'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Criterio en uso' } })));
    const criterio = { criterioId: 9, variableId: 3, activo: false } as never;
    component.eliminarCriterio(criterio);
    component.actualizarModalMotivo('Depuracion del catalogo');

    component.confirmarModal();

    expect(service['eliminarCriterio']).toHaveBeenCalledWith(9, 'Depuracion del catalogo');
    expect(component.modalError()).toBe('Criterio en uso');
    expect(component.modalOperacion()?.tipo).toBe('eliminarCriterio');
    expect(component.guardando()).toBe(false);
  });

  it.each([
    ['EXCEL', 'generarExcelReporte'],
    ['PDF', 'generarPdfReporte']
  ] as const)('exporta un reporte %s usando el generador correspondiente', (formato, metodo) => {
    const generar = vi.fn();
    (component as any)[metodo] = generar;
    component.reporteFiltro.set({ estado: 'APROBADA' });
    const archivo = new Blob(['reporte'], { type: 'application/octet-stream' });
    service['exportarReporte'].mockReturnValue(of(archivo));

    component.exportarReporte(formato);

    expect(service['exportarReporte']).toHaveBeenCalledWith({ estado: 'APROBADA' }, formato);
    expect(generar).toHaveBeenCalledOnce();
    expect(component.mensaje()).toBe(`Reporte ${formato} generado correctamente.`);
    expect(component.guardando()).toBe(false);
  });

  it('recupera el indicador si falla la exportacion del reporte', () => {
    service['exportarReporte'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Exportacion no disponible' } })));

    component.exportarReporte('PDF');

    expect(component.error()).toBe('Exportacion no disponible');
    expect(component.guardando()).toBe(false);
  });

  it('carga el detalle y reconstruye las variables para editar una matriz', () => {
    component.metodologia.set({
      variables: [{
        variableId: 4, factorId: 2, factorCodigo: 'PROVEEDORES',
        factorNombre: 'Proveedores', nombre: 'Pais de origen'
      }],
      escalasCatalogo: [], escalasRiesgo: []
    } as never);
    const detalle = {
      matrizId: 40,
      sujetoTipo: 'PROVEEDOR',
      sujetoIdExt: 'PR-40',
      documento: 'RTN-40',
      nombreSujeto: 'Proveedor Cuarenta',
      origenDatos: 'CAPTURA',
      detalles: [{
        variableId: 4, puntaje: 5, valorCapturado: 'HN',
        justificacion: 'Pais evaluado', fuenteDato: 'CAPTURA'
      }]
    };
    service['obtener'].mockReturnValue(of(detalle));

    component.editarMatriz({ matrizId: 40 } as never);

    expect(service['obtener']).toHaveBeenCalledWith(40);
    expect(component.matrizEditandoId()).toBe(40);
    expect(component.nuevaMatriz).toEqual({
      sujetoTipo: 'PROVEEDOR', sujetoIdExt: 'PR-40', documento: 'RTN-40',
      nombreSujeto: 'Proveedor Cuarenta', origenDatos: 'CAPTURA'
    });
    expect(component.capturasVariables()).toEqual([expect.objectContaining({
      variableId: 4, puntaje: 5, valorCapturado: 'HN'
    })]);
    expect(component.tab()).toBe('nueva');
    expect(component.cargando()).toBe(false);
  });

  it('actualiza y recalcula una matriz cargada en modo edicion', () => {
    component.metodologia.set({
      variables: [{
        variableId: 4, factorId: 2, factorCodigo: 'PROVEEDORES',
        factorNombre: 'Proveedores', nombre: 'Pais de origen'
      }],
      escalasCatalogo: [], escalasRiesgo: []
    } as never);
    const detalle = {
      matrizId: 41, sujetoTipo: 'PROVEEDOR', sujetoIdExt: 'PR-41', documento: 'RTN-41',
      nombreSujeto: 'Proveedor Editado', origenDatos: 'CAPTURA',
      detalles: [{ variableId: 4, puntaje: 4, valorCapturado: 'HN', justificacion: '', fuenteDato: 'CAPTURA' }]
    };
    service['obtener'].mockReturnValue(of(detalle));
    service['actualizar'].mockReturnValue(of(detalle));

    component.editarMatriz({ matrizId: 41 } as never);
    component.nuevaMatriz.nombreSujeto = 'Proveedor Actualizado';
    component.crearMatriz();

    expect(service['actualizar']).toHaveBeenCalledWith(41, expect.objectContaining({
      nombreSujeto: 'Proveedor Actualizado'
    }));
    expect(service['calcular']).toHaveBeenCalledWith(41, 'FACTOR');
    expect(component.mensaje()).toBe('Matriz actualizada y recalculada automáticamente.');
    expect(component.matrizEditandoId()).toBeNull();
  });

  it('conserva el formulario y muestra el error si no puede cargar la edicion', () => {
    service['obtener'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Matriz bloqueada' } })));

    component.editarMatriz({ matrizId: 42 } as never);

    expect(component.error()).toBe('Matriz bloqueada');
    expect(component.matrizEditandoId()).toBeNull();
    expect(component.cargando()).toBe(false);
  });

  it('crea un criterio valido normalizando los valores numericos', () => {
    component.criteriosForm = {
      variableId: 6, escalaId: 2, valorDesde: 1, valorHasta: 10,
      puntaje: 4, descripcion: '  Riesgo elevado  '
    };

    component.guardarCriterio();

    expect(service['crearCriterio']).toHaveBeenCalledWith({
      variableId: 6, escalaId: 2, valorDesde: 1, valorHasta: 10,
      puntaje: 4, descripcion: 'Riesgo elevado'
    });
    expect(component.mensaje()).toBe('Criterio registrado correctamente.');
    expect(component.criterioEditandoId()).toBeNull();
    expect(component.criteriosForm.variableId).toBe(0);
    expect(component.guardando()).toBe(false);
  });

  it('carga un criterio en el formulario y lo actualiza por su identificador', () => {
    const criterio = {
      criterioId: 13, variableId: 6, escalaId: 2, valorDesde: 11,
      valorHasta: 20, puntaje: 5, descripcion: 'Riesgo critico', activo: true
    } as never;

    component.editarCriterio(criterio);
    component.criteriosForm.descripcion = 'Riesgo crítico actualizado';
    component.guardarCriterio();

    expect(service['actualizarCriterio']).toHaveBeenCalledWith(13, expect.objectContaining({
      variableId: 6, puntaje: 5, descripcion: 'Riesgo crítico actualizado'
    }));
    expect(component.mensaje()).toBe('Criterio actualizado correctamente.');
    expect(component.criterioEditandoId()).toBeNull();
    expect(component.guardando()).toBe(false);
  });

  it('conserva el criterio en edicion cuando el servicio rechaza la actualizacion', () => {
    service['actualizarCriterio'].mockReturnValue(throwError(() => ({ error: { mensaje: 'Rango superpuesto' } })));
    const criterio = {
      criterioId: 14, variableId: 6, escalaId: null, valorDesde: 1,
      valorHasta: 5, puntaje: 2, descripcion: 'Rango existente', activo: true
    } as never;
    component.editarCriterio(criterio);

    component.guardarCriterio();

    expect(component.error()).toBe('Rango superpuesto');
    expect(component.criterioEditandoId()).toBe(14);
    expect(component.criteriosForm.descripcion).toBe('Rango existente');
    expect(component.guardando()).toBe(false);
  });
});
