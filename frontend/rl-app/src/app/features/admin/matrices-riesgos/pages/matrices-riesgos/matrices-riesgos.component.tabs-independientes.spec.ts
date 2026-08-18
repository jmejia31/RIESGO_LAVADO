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
      listarEvaluacionesRiesgoPaginadas: vi.fn().mockReturnValue(of(mockPaginado)),
      listarEvaluaciones: vi.fn().mockReturnValue(of(mockPaginado)),
      obtenerConsolidadoReporte: vi.fn().mockReturnValue(of([])),
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
    serviceMock.obtenerConsolidado.mockReturnValue(throwError(() => new Error('Error consolidado')));
    component.seleccionarTab('consolidado');

    expect(component.errorConsolidado()).toBeTruthy();
    expect(component.tab()).toBe('consolidado');

    component.seleccionarTab('evaluaciones');
    expect(component.tab()).toBe('evaluaciones');
  });

  // 8. fallo Plantillas no bloquea otras tabs.
  it('8. fallo Plantillas no bloquea otras tabs', () => {
    serviceMock.listarFamiliasFormulario.mockReturnValue(throwError(() => new Error('Error familias')));
    component.seleccionarTab('plantillas');

    expect(component.familias().length).toBe(0);
    expect(component.tab()).toBe('plantillas');

    component.seleccionarTab('captura');
    expect(component.tab()).toBe('captura');
  });

  // 9. loading Evaluaciones no bloquea tablist.
  it('9. loading Evaluaciones no bloquea tablist', () => {
    component.cargandoEvaluaciones.set(true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const tabList = compiled.querySelector('[role="tablist"]');
    expect(tabList).not.toBeNull();
  });

  // 10. entrada a Plantillas provoca UNA SOLA carga del historial.
  it('10. entrada a Plantillas provoca UNA SOLA carga del historial', () => {
    serviceMock.listarHistorialVersionesFormulario.mockClear();
    serviceMock.listarFamiliasFormulario.mockClear();

    component.seleccionarTab('plantillas');

    expect(serviceMock.listarFamiliasFormulario).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarHistorialVersionesFormulario).toHaveBeenCalledTimes(1);
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
});
