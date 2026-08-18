import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionesPaginadasDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent — evaluaciones: búsqueda y filtros', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let serviceMock: any;

  const version: VersionFormularioDto = {
    verId: 10,
    verFamiliaId: 1,
    verCodigo: 'V1.0',
    verVersion: 1,
    verJson: '{}',
    verHash: 'hash-f41',
    verEstado: 'PUBLISHED',
    verVigente: true,
    verFechaCreacion: '2026-08-18',
    verUsrCreacion: 1
  };

  const paginadoMock: EvaluacionesPaginadasDto = {
    items: [],
    pagina: 1,
    registrosPorPagina: 10,
    totalRegistros: 0,
    totalPaginas: 0
  };

  beforeEach(async () => {
    vi.useFakeTimers();

    serviceMock = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([])),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarEvaluaciones: vi.fn().mockReturnValue(of(paginadoMock)),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(version)),
      metodologiaVigente: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'MATRIZ_RIESGOS_LAFT',
        version: 1,
        secciones: [],
        catalogos: [],
        reglasEvaluacion: []
      }))
    };

    const authServiceMock = {
      tieneRol: vi.fn().mockReturnValue(true)
    };

    const globalStateMock = {
      limpiarError: vi.fn(),
      mostrarError: vi.fn()
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
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('CASO 1 — DEBOUNCE EXACTO: no realiza petición antes de 300 ms y ejecuta 1 sola a los 300 ms', () => {
    serviceMock.listarEvaluaciones.mockClear();
    component.pagina.set(3);

    component.alCambiarFiltroBuscar('RIE-005');

    expect(component.pagina()).toBe(1);
    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();

    vi.advanceTimersByTime(299);
    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'RIE-005',
      estado: undefined,
      pagina: 1,
      registrosPorPagina: 10
    });
  });

  it('CASO 2 — ESCRITURA RÁPIDA: cancela timers intermedios y ejecuta 1 sola consulta final', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroBuscar('R');
    vi.advanceTimersByTime(100);

    component.alCambiarFiltroBuscar('RI');
    vi.advanceTimersByTime(100);

    component.alCambiarFiltroBuscar('RIE');
    vi.advanceTimersByTime(100);

    component.alCambiarFiltroBuscar('RIE-005');

    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();

    vi.advanceTimersByTime(300);

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'RIE-005',
      estado: undefined,
      pagina: 1,
      registrosPorPagina: 10
    });
  });

  it('CASO 3 — BÚSQUEDA PENDIENTE + ESTADO: cancela debounce previo, ejecuta inmediata y no duplica al vencer timer', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroBuscar('fraude');
    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();

    vi.advanceTimersByTime(100);

    component.alCambiarFiltroEstado('APROBADA');

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'fraude',
      estado: 'APROBADA',
      pagina: 1,
      registrosPorPagina: 10
    });

    vi.advanceTimersByTime(500);

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
  });

  it('CASO 4 — ESTADO + BÚSQUEDA: preserva filtro de estado previo al buscar', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroEstado('CERRADA');
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenLastCalledWith({
      buscar: undefined,
      estado: 'CERRADA',
      pagina: 1,
      registrosPorPagina: 10
    });

    component.alCambiarFiltroBuscar('fraude');
    vi.advanceTimersByTime(300);

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(2);
    expect(serviceMock.listarEvaluaciones).toHaveBeenLastCalledWith({
      buscar: 'fraude',
      estado: 'CERRADA',
      pagina: 1,
      registrosPorPagina: 10
    });
  });

  it('CASO 5 — BORRAR BÚSQUEDA CONSERVANDO ESTADO: vaciar texto mantiene el estado activo', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroEstado('OBSERVADA');
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);

    component.alCambiarFiltroBuscar('fraude');
    vi.advanceTimersByTime(300);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(2);

    component.alCambiarFiltroBuscar('');
    vi.advanceTimersByTime(300);

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(3);
    expect(serviceMock.listarEvaluaciones).toHaveBeenLastCalledWith({
      buscar: undefined,
      estado: 'OBSERVADA',
      pagina: 1,
      registrosPorPagina: 10
    });
  });

  it('CASO 6 — LIMPIAR FILTROS CON TIMER PENDIENTE: cancela timer, ejecuta 1 sola consulta limpia y no duplica', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroBuscar('fraude');
    vi.advanceTimersByTime(150);

    component.limpiarFiltrosEvaluaciones();

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: undefined,
      estado: undefined,
      pagina: 1,
      registrosPorPagina: 10
    });

    vi.advanceTimersByTime(500);

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
  });

  it('CASO 7 — RESET DE PÁGINA: restablece pagina=1 ante cambios de búsqueda o estado', () => {
    component.pagina.set(4);
    component.alCambiarFiltroBuscar('test');
    expect(component.pagina()).toBe(1);

    component.pagina.set(5);
    component.alCambiarFiltroEstado('RECHAZADA');
    expect(component.pagina()).toBe(1);
  });

  it('CASO 8 — ESTADOS EXACTOS UI: soporta los 6 estados institucionales más la opción de Todos', () => {
    const estadosValidos = ['', 'BORRADOR', 'EN_REVISION', 'OBSERVADA', 'APROBADA', 'RECHAZADA', 'CERRADA'];

    estadosValidos.forEach(est => {
      serviceMock.listarEvaluaciones.mockClear();
      component.alCambiarFiltroEstado(est);

      expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
        buscar: undefined,
        estado: est || undefined,
        pagina: 1,
        registrosPorPagina: 10
      });
    });
  });

  it('CASO 9 — TRIM: remueve espacios antes y después del texto buscado', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroBuscar('   fraude   ');
    vi.advanceTimersByTime(300);

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'fraude',
      estado: undefined,
      pagina: 1,
      registrosPorPagina: 10
    });
  });

  it('CASO 10 — WHITESPACE: texto compuesto únicamente de espacios se envía como undefined preservando estado', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroEstado('OBSERVADA');
    component.alCambiarFiltroBuscar('     ');
    vi.advanceTimersByTime(300);

    expect(serviceMock.listarEvaluaciones).toHaveBeenLastCalledWith({
      buscar: undefined,
      estado: 'OBSERVADA',
      pagina: 1,
      registrosPorPagina: 10
    });
  });

  it('CASO 11 — DESTROY: ngOnDestroy cancela timer pendiente y no genera petición tardía', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroBuscar('fraude');
    component.ngOnDestroy();

    vi.advanceTimersByTime(500);

    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();
  });
});
