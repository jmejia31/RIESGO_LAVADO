import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import {
  EvaluacionesPaginadasDto,
  FamiliaFormularioDto,
  VersionFormularioDto
} from '../../models/matrices-riesgos.models';

describe('MatricesRiesgosComponent — pestañas y cargas independientes', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let serviceMock: any;
  let authServiceMock: any;
  let globalStateMock: any;

  const mockFamilia: FamiliaFormularioDto = {
    famId: 1,
    famCodigo: 'MATRIZ_RIESGOS_LAFT',
    famNombre: 'Matriz de Riesgos LAFT',
    famDescripcion: 'Descripción',
    famActivo: true,
    famFechaCreacion: '2026-08-01',
    totalVersiones: 1,
    tieneVersionVigente: true
  };

  const mockVersion: VersionFormularioDto = {
    verId: 10,
    verFamiliaId: 1,
    verCodigo: 'V1.0',
    verVersion: 1,
    verJson: '{}',
    verHash: 'hash123',
    verEstado: 'PUBLISHED',
    verVigente: true,
    verUsrCreacion: 1,
    verFechaCreacion: '2026-08-01'
  };

  const mockPaginado: EvaluacionesPaginadasDto = {
    items: [
      {
        evaId: 101,
        evaRiesgoId: 5,
        riesgoCodigo: 'RIE-005',
        riesgoNombre: 'Riesgo Test 5',
        evaVersionId: 10,
        versionCodigo: 'V1.0',
        versionNumero: 1,
        estado: 'REGISTRADA',
        fechaEval: '2026-08-17',
        vri: 12,
        vrr: 4,
        nivelResidual: 'BAJO'
      },
      {
        evaId: 102,
        evaRiesgoId: 6,
        riesgoCodigo: 'RIE-006',
        riesgoNombre: 'Riesgo Test 6',
        evaVersionId: 10,
        versionCodigo: 'V1.0',
        versionNumero: 1,
        estado: 'EN_REVISION',
        fechaEval: '2026-08-17',
        vri: 8,
        vrr: 2,
        nivelResidual: 'BAJO'
      }
    ],
    totalRegistros: 2,
    pagina: 1,
    registrosPorPagina: 10,
    totalPaginas: 1
  };

  beforeEach(async () => {
    serviceMock = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([mockFamilia])),
      listarFamiliasFormularioPaginadas: vi.fn().mockReturnValue(of({ items: [mockFamilia], pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1, totales: { totalFamilias: 1, activas: 1, inactivas: 0, totalVersiones: 1 } })),
      listarVersionesFormulario: vi.fn().mockReturnValue(of([mockVersion])),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([mockVersion])),
      obtenerFormularioVigente: vi.fn().mockReturnValue(of({
        secciones: [],
        versionVigente: mockVersion
      })),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(mockVersion)),
      obtenerFormularioPorDefinicion: vi.fn().mockReturnValue(of({
        secciones: [],
        versionVigente: mockVersion
      })),
      metodologiaVigente: vi.fn().mockReturnValue(of({ secciones: [] })),
      metodologiaPorVersion: vi.fn().mockReturnValue(of({ secciones: [] })),
      listarRiesgosCatalogos: vi.fn().mockReturnValue(of([])),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarRiesgosPaginados: vi.fn().mockReturnValue(of({ items: [], pagina: 1, tamanoPagina: 200, totalRegistros: 0, totalPaginas: 0 })),
      listarEvaluacionesRiesgoPaginadas: vi.fn().mockReturnValue(of(mockPaginado)),
      listarEvaluaciones: vi.fn().mockReturnValue(of(mockPaginado)),
      obtenerMitigacionBloque4: vi.fn().mockReturnValue(of({ cantidadAcciones: 0, planes: [] })),
      obtenerBloque6: vi.fn().mockReturnValue(of({ senalesAlerta: [], estadoRiesgo: null, controles: [], observacionesArea: null, observacionesUgr: null, puedeEditarObservacionesArea: false, puedeEditarObservacionesUgr: false })),
      obtenerConsolidadoReporte: vi.fn().mockReturnValue(of([])),
      obtenerConsolidadoPaginado: vi.fn().mockReturnValue(of({ items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRiesgos: 0, totalConEvaluacionOficial: 0, totalSinEvaluacionOficial: 0, totalAltoCritico: 0 } })),
      obtenerConsolidado: vi.fn().mockReturnValue(of([]))
    };

    authServiceMock = {
      tieneRol: vi.fn().mockReturnValue(true)
    };

    globalStateMock = {
      limpiarError: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: serviceMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: GlobalHttpStateService, useValue: globalStateMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
  });

  // 1. evaluaciones inicia como Array.
  it('1. evaluaciones inicia como Array vacio', () => {
    expect(Array.isArray(component.evaluaciones())).toBe(true);
    expect(component.evaluaciones().length).toBe(0);
  });

  // 2. respuesta paginada válida mantiene evaluaciones como Array.
  it('2. respuesta paginada valida mantiene evaluaciones como Array', () => {
    component.cargarEvaluaciones();
    expect(Array.isArray(component.evaluaciones())).toBe(true);
    expect(component.evaluaciones().length).toBe(2);
    expect(component.evaluaciones()[0].evaId).toBe(101);
  });

  // 3. contarEvaluacionesPorEstado funciona bajo contrato Array.
  it('3. contarEvaluacionesPorEstado funciona bajo contrato Array', () => {
    component.cargarEvaluaciones();
    expect(component.contarEvaluacionesPorEstado('REGISTRADA')).toBe(1);
    expect(component.contarEvaluacionesPorEstado('EN_REVISION')).toBe(1);
    expect(component.contarEvaluacionesPorEstado('APROBADA')).toBe(0);
  });

  // 4. fallo versión vigente no bloquea Evaluaciones.
  it('4. fallo version vigente no bloquea Evaluaciones', () => {
    serviceMock.obtenerVersionVigenteFormulario.mockReturnValue(throwError(() => new Error('Error backend')));
    component.cargarFormularioVigente();
    component.cargarEvaluaciones();

    expect(component.errorFormulario()).toBeTruthy();
    expect(Array.isArray(component.evaluaciones())).toBe(true);
    expect(component.evaluaciones().length).toBe(2);
  });

  // 5. fallo metodología no bloquea Evaluaciones.
  it('5. fallo metodologia no bloquea Evaluaciones', () => {
    serviceMock.obtenerVersionVigenteFormulario.mockReturnValue(of(mockVersion));
    serviceMock.metodologiaVigente.mockReturnValue(throwError(() => new Error('Error metodologia')));

    component.cargarFormularioVigente();
    component.cargarEvaluaciones();

    expect(component.errorFormulario()).toBeTruthy();
    expect(component.cargandoFormulario()).toBe(false);
    expect(Array.isArray(component.evaluaciones())).toBe(true);
    expect(component.evaluaciones().length).toBe(2);
    expect(component.errorEvaluaciones()).toBeNull();
  });

  // 6. fallo Evaluaciones no bloquea otras tabs.
  it('6. fallo Evaluaciones no bloquea otras tabs', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(throwError(() => new Error('Error al listar')));
    component.cargarEvaluaciones();

    expect(component.errorEvaluaciones()).toBeTruthy();
    expect(component.evaluaciones().length).toBe(0);
    expect(component.tab()).toBe('evaluaciones');

    component.seleccionarTab('consolidado');
    expect(component.tab()).toBe('consolidado');
  });

  // 7. fallo Consolidado no bloquea otras tabs.
  it('7. fallo Consolidado no bloquea otras tabs', () => {
    serviceMock.obtenerConsolidadoPaginado.mockReturnValue(throwError(() => new Error('Error consolidado')));
    component.seleccionarTab('consolidado');

    expect(component.errorConsolidado()).toBeTruthy();
    expect(component.tab()).toBe('consolidado');

    component.seleccionarTab('evaluaciones');
    expect(component.tab()).toBe('evaluaciones');
  });

  // 8. fallo Plantillas no bloquea otras tabs.
  it('8. fallo Plantillas no bloquea otras tabs', () => {
    serviceMock.listarFamiliasFormularioPaginadas.mockReturnValue(throwError(() => new Error('Error familias')));
    component.seleccionarTab('plantillas');

    expect(component.familias().length).toBe(0);
    expect(component.tab()).toBe('plantillas');

    component.seleccionarTab('evaluaciones');
    expect(component.tab()).toBe('evaluaciones');
  });

  // 9. loading Evaluaciones no bloquea tablist.
  it('9. loading Evaluaciones no bloquea tablist', () => {
    component.cargandoEvaluaciones.set(true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const tabList = compiled.querySelector('[role="tablist"]');
    expect(tabList).not.toBeNull();
  });

  // 10. entrada a Plantillas carga solamente el gestor de familias; el historial se solicita al abrir versiones.
  it('10. entrada a Plantillas carga el gestor sin solicitar historial anticipadamente', () => {
    serviceMock.listarHistorialVersionesFormulario.mockClear();
    serviceMock.listarFamiliasFormulario.mockClear();

    component.seleccionarTab('plantillas');

    expect(serviceMock.listarFamiliasFormularioPaginadas).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarHistorialVersionesFormulario).not.toHaveBeenCalled();
  });

  // 11. cambio rápido de pestañas no mezcla loading/error.
  it('11. cambio rapido de pestañas no mezcla loading/error', () => {
    component.cargandoEvaluaciones.set(true);
    component.errorEvaluaciones.set('Error eval');

    component.seleccionarTab('consolidado');

    expect(component.cargandoEvaluaciones()).toBe(true);
    expect(component.errorEvaluaciones()).toBe('Error eval');
    expect(component.cargandoConsolidado()).toBe(false);
    expect(component.errorConsolidado()).toBeNull();
  });

  it('12. cada pestana expone exactamente sus filtros y no cruza controles de otro dominio', () => {
    const assertOnly = (tab: 'evaluaciones' | 'consolidado' | 'plantillas', present: string[], absent: string[]) => {
      component.tab.set(tab);
      fixture.detectChanges();
      const root = fixture.nativeElement as HTMLElement;
      for (const selector of present) expect(root.querySelector(selector)).not.toBeNull();
      for (const selector of absent) expect(root.querySelector(selector)).toBeNull();
    };

    assertOnly('evaluaciones', ['#filtro-buscar', '#filtro-estado', '#filtro-registros-por-pagina'], ['#consolidado-buscar', '#gestor-buscar-familia']);
    assertOnly('consolidado', ['#consolidado-buscar', '#consolidado-estado'], ['#filtro-buscar', '#gestor-buscar-familia']);
    assertOnly('plantillas', ['#gestor-buscar-familia', '#gestor-estado-familia', '#gestor-vigencia-familia'], ['#filtro-buscar', '#consolidado-buscar']);
  });

  it('13. renderiza un solo bloque superior de KPIs contextual a la pestana activa', () => {
    const kpis = () => (fixture.nativeElement as HTMLElement).querySelectorAll('[data-ui-kpis-context]');

    component.tab.set('evaluaciones');
    fixture.detectChanges();
    expect(kpis()).toHaveLength(1);
    expect(kpis()[0].textContent).toContain('Total evaluaciones');

    component.tab.set('consolidado');
    fixture.detectChanges();
    expect(kpis()).toHaveLength(1);
    expect(kpis()[0].textContent).toContain('Total registros');
    expect(kpis()[0].textContent).not.toContain('Total evaluaciones');

    component.tab.set('plantillas');
    fixture.detectChanges();
    expect(kpis()).toHaveLength(1);
    expect(kpis()[0].textContent).toContain('Total familias');
    expect(kpis()[0].textContent).not.toContain('Total evaluaciones');
  });

  it('14. conserva tres pestañas y reserva Matriz completa exclusivamente como modal', () => {
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    const tabs = Array.from(root.querySelectorAll('[role="tab"]')).map(tab => tab.textContent?.trim());
    expect(tabs).toEqual(['Evaluaciones', 'Consolidado', 'Plantillas']);
    expect(root.querySelector('#tab-matriz-completa')).toBeNull();
    expect(root.querySelector('#panel-matriz-completa')).toBeNull();
    expect(root.querySelector('[data-matrix-modal="complete"]')).toBeNull();
  });

  it('15. renderiza campos 01–19 en orden, conserva los GTIC y no inventa valores históricos V1', () => {
    component.evaluacionResumenSeleccionada.set(mockPaginado.items[0]);
    component.evaluacionSeleccionada.set({
      evaId: 101,
      evaRiesgoId: 5,
      evaVersionId: 10,
      evaEstado: 'BORRADOR',
      evaDataJson: '{"area_principal":"Área de Cumplimiento","frecuencia_inherente":"3","impacto_inherente":"3","dueno_riesgo":"Responsable"}',
      evaDataCalcJson: '{"nivel_riesgo_inherente":"RIESGO_MEDIO"}',
      evaVri: 5,
      evaFechaEval: '2026-08-17T00:00:00',
      evaUsrEval: 1,
      evaVersionRow: 1,
      evaActivo: true
    });
    component.respuestas.set({ area_principal: 'Área de Cumplimiento', frecuencia_inherente: '3', impacto_inherente: '3', dueno_riesgo: 'Responsable' });
    component.riesgoMaestroMatriz.set({ rieId: 5, rieCodigo: 'RIE-005', rieNombre: 'Riesgo maestro', rieDescripcion: 'Descripción maestra', rieActivo: true, rieUsrCreacion: 1, rieFechaCreacion: '2026-08-01' });
    component.modalMatrizCompletaAbierto.set(true);
    component.bloque4Matriz.set({ cantidadAcciones: 0, planes: [] });
    component.bloque6Matriz.set({ senalesAlerta: [], estadoRiesgo: null, controles: [], observacionesArea: null, observacionesUgr: null, puedeEditarObservacionesArea: false, puedeEditarObservacionesUgr: false });
    expect(component.valorCampoMatriz(component.camposMatrizCompleta[2])).toBe('Área de Cumplimiento');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const fields = Array.from(root.querySelectorAll('[data-matrix-view="complete"] [data-matrix-field]'));
    expect(fields).toHaveLength(82);
    expect(fields.slice(0, 19).map(field => field.getAttribute('data-matrix-field'))).toEqual(Array.from({ length: 19 }, (_, index) => String(index + 1).padStart(2, '0')));
    expect(fields.slice(19, 33).map(field => field.getAttribute('data-matrix-field'))).toEqual(Array.from({ length: 14 }, (_, index) => String(index + 20).padStart(2, '0')));
    expect(fields.slice(33, 49).map(field => field.getAttribute('data-matrix-field'))).toEqual(Array.from({ length: 16 }, (_, index) => String(index + 34).padStart(2, '0')));
    expect(fields.slice(49, 69).map(field => field.getAttribute('data-matrix-field'))).toEqual(Array.from({ length: 20 }, (_, index) => String(index + 50)));
    expect(fields.slice(69).map(field => field.getAttribute('data-matrix-field'))).toEqual(Array.from({ length: 13 }, (_, index) => String(index + 70)));
    expect(fields.slice(16, 19).map(field => field.getAttribute('data-matrix-field'))).toEqual(['17', '18', '19']);
    const visibleLabels = fields.slice(0, 19).map(field => field.querySelector('dt span:nth-child(2)')?.textContent?.trim());
    expect(visibleLabels).toEqual([
      'No.', 'Código de Riesgo', 'Área', 'Área Consolidada', 'Tipo de Riesgo', 'Procedimiento',
      'Objetivo(s) Estratégico(s)', 'Riesgo Inherente', 'Evaluación', 'Frecuencia', 'Impacto',
      'Valor del Riesgo Inherente', 'Nivel de Riesgo Inherente', 'Responsable o dueño del riesgo',
      'Régimen afectado', 'Transversalidad o Interrelación con otros Riesgos',
      'Amenazas (Solo para riesgos de GTIC)', 'Vulnerabilidades (Solo para riesgos de GTIC)',
      'Activos de Información (Solo para riesgos de GTIC)'
    ]);
    expect(fields[0].getAttribute('data-matrix-field')).toBe('01');
    expect(fields[0].textContent).not.toContain('101');
    expect(fields[3].textContent).toContain('No disponible en esta versión');
    expect(fields[11].textContent).toContain('Calculado automáticamente');
    expect(fields[12].textContent).toContain('Calculado automáticamente');
    expect(fields[11].querySelector('[aria-readonly="true"]')).not.toBeNull();
    expect(fields[12].querySelector('[aria-readonly="true"]')).not.toBeNull();
    expect(root.querySelector('[data-matrix-block="3"] h3')?.textContent).toContain('3. Riesgo Residual y Respuesta');
    expect(root.querySelectorAll('[data-matrix-block="3"] [data-matrix-field]')).toHaveLength(6);
    expect(root.querySelectorAll('[data-matrix-block="4"] .text-amber-800')).toHaveLength(0);
    expect(root.querySelectorAll('[data-matrix-block="5"] .text-amber-800')).toHaveLength(0);
    expect(root.querySelectorAll('[data-matrix-block="6"] .text-amber-800')).toHaveLength(0);
    expect(root.querySelector('[data-matrix-modal="complete"] .modal-size-workspace')).not.toBeNull();
  });

  it('16. alinea las acciones del Consolidado como grupo horizontal y conserva su orden', () => {
    component.consolidado.set([{
      riesgoId: 5,
      evaluacionId: 101,
      codigoRiesgo: 'RIE-005',
      areaPrincipal: 'Cumplimiento',
      duenoRiesgo: 'Responsable',
      vri: 5,
      nivelInherente: 'MEDIO',
      vrr: 2,
      nivelResidual: 'BAJO',
      respuestaRiesgo: 'ACEPTAR',
      estadoEvaluacion: 'APROBADA',
      versionFormularioId: 10,
      fechaEvaluacion: '2026-08-17T00:00:00'
    } as never]);
    component.totalRegistrosConsolidado.set(1);
    component.tab.set('consolidado');
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const acciones = root.querySelector('[data-consolidado-actions]') as HTMLElement | null;
    expect(acciones).not.toBeNull();
    expect(acciones?.classList.contains('flex-nowrap')).toBe(true);
    const etiquetas = Array.from(acciones?.querySelectorAll('button') ?? []).map(button => button.getAttribute('aria-label'));
    expect(etiquetas).toEqual(['Ver detalle del consolidado', 'Ver Matriz completa']);
  });
});
