import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionRiesgoResumenDto, EvaluacionesPaginadasDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent — evaluaciones: paginación y concurrencia', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let serviceMock: any;

  const version: VersionFormularioDto = {
    verId: 10,
    verFamiliaId: 1,
    verCodigo: 'V1.0',
    verVersion: 1,
    verJson: '{}',
    verHash: 'hash-f42',
    verEstado: 'PUBLISHED',
    verVigente: true,
    verFechaCreacion: '2026-08-18',
    verUsrCreacion: 1
  };

  const crearPaginado = (
    pagina: number,
    registrosPorPagina: number,
    totalRegistros: number,
    totalPaginas: number,
    numItems = 2
  ): EvaluacionesPaginadasDto => {
    const items: EvaluacionRiesgoResumenDto[] = Array.from({ length: numItems }, (_, i) => ({
      evaId: 100 + i,
      evaRiesgoId: 10 + i,
      riesgoCodigo: `RIE-${100 + i}`,
      riesgoNombre: `Riesgo ${100 + i}`,
      evaVersionId: 10,
      versionCodigo: 'V1.0',
      versionNumero: 1,
      estado: 'BORRADOR',
      vri: 10,
      vrr: 5,
      nivelResidual: 'MEDIO',
      fechaEval: '2026-08-18T10:00:00Z'
    }));

    return {
      items,
      pagina,
      registrosPorPagina,
      totalRegistros,
      totalPaginas
    };
  };

  beforeEach(async () => {
    vi.useFakeTimers();

    serviceMock = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([])),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarEvaluaciones: vi.fn().mockImplementation((dto: any) =>
        of(crearPaginado(dto?.pagina || 1, dto?.registrosPorPagina || 10, 35, 4))
      ),
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

  it('1. Anterior válido conserva filtros', () => {
    component.filtroBuscar.set('fraude');
    component.filtroEstado.set('BORRADOR');
    component.pagina.set(3);
    serviceMock.listarEvaluaciones.mockClear();

    component.cambiarPagina(2);

    expect(component.pagina()).toBe(2);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'fraude',
      estado: 'BORRADOR',
      pagina: 2,
      registrosPorPagina: 10
    });
  });

  it('2. Siguiente válido conserva filtros', () => {
    component.filtroBuscar.set('laft');
    component.filtroEstado.set('APROBADA');
    component.pagina.set(1);
    serviceMock.listarEvaluaciones.mockClear();

    component.cambiarPagina(2);

    expect(component.pagina()).toBe(2);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'laft',
      estado: 'APROBADA',
      pagina: 2,
      registrosPorPagina: 10
    });
  });

  it('3. Misma página: 0 nuevas consultas', () => {
    component.pagina.set(2);
    serviceMock.listarEvaluaciones.mockClear();

    component.cambiarPagina(2);

    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();
  });

  it('4. Página < 1: 0 consultas', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.cambiarPagina(0);
    component.cambiarPagina(-1);

    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();
  });

  it('5. Página > TotalPaginas: 0 consultas', () => {
    component.totalPaginas.set(4);
    serviceMock.listarEvaluaciones.mockClear();

    component.cambiarPagina(5);

    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();
  });

  it('6. 0 registros: pagina coherente y 0 paginasVisibles', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(1, 10, 0, 0, 0)));

    component.cargarEvaluaciones();

    expect(component.totalRegistros()).toBe(0);
    expect(component.totalPaginas()).toBe(0);
    expect(component.pagina()).toBe(1);
    expect(component.paginasVisibles()).toEqual([]);
  });

  it('7. 1 página: Anterior/Siguiente no navegables', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(1, 10, 5, 1, 5)));

    component.cargarEvaluaciones();

    expect(component.pagina()).toBe(1);
    expect(component.totalPaginas()).toBe(1);
    expect(component.paginasVisibles()).toEqual([1]);
  });

  it('8. Page-size: 10 -> 20, pagina=1, filtros preservados, 1 consulta', () => {
    component.filtroBuscar.set('riesgo');
    component.filtroEstado.set('OBSERVADA');
    component.pagina.set(3);
    serviceMock.listarEvaluaciones.mockClear();

    component.cambiarRegistrosPorPagina(20);

    expect(component.registrosPorPagina()).toBe(20);
    expect(component.pagina()).toBe(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'riesgo',
      estado: 'OBSERVADA',
      pagina: 1,
      registrosPorPagina: 20
    });
  });

  it('9. Page-size: 20 -> 50, mismo criterio', () => {
    component.registrosPorPagina.set(20);
    serviceMock.listarEvaluaciones.mockClear();

    component.cambiarRegistrosPorPagina(50);

    expect(component.registrosPorPagina()).toBe(50);
    expect(component.pagina()).toBe(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
  });

  it('10. Page-size inválido (25, 0, -1, NaN, texto): 0 consultas', () => {
    serviceMock.listarEvaluaciones.mockClear();

    [25, 0, -1, NaN, 'invalido' as any].forEach(val => {
      component.cambiarRegistrosPorPagina(val);
    });

    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();
    expect(component.registrosPorPagina()).toBe(10);
  });

  it('11. Mismo page-size: 0 consultas', () => {
    component.registrosPorPagina.set(10);
    serviceMock.listarEvaluaciones.mockClear();

    component.cambiarRegistrosPorPagina(10);

    expect(serviceMock.listarEvaluaciones).not.toHaveBeenCalled();
  });

  it('12. paginasVisibles en inicio', () => {
    component.totalPaginas.set(10);
    component.pagina.set(1);

    expect(component.paginasVisibles()).toEqual([1, 2, 3, 10]);
  });

  it('13. paginasVisibles intermedia', () => {
    component.totalPaginas.set(25);
    component.pagina.set(6);

    expect(component.paginasVisibles()).toEqual([1, 4, 5, 6, 7, 8, 25]);
  });

  it('14. paginasVisibles final', () => {
    component.totalPaginas.set(10);
    component.pagina.set(10);

    expect(component.paginasVisibles()).toEqual([1, 8, 9, 10]);
  });

  it('15. gaps correctos', () => {
    component.totalPaginas.set(20);
    component.pagina.set(10);

    const vis = component.paginasVisibles();
    expect(vis).toEqual([1, 8, 9, 10, 11, 12, 20]);
    // Gap 1 -> 8 es > 1 (requiere ellipsis)
    expect(vis[1] - vis[0]).toBeGreaterThan(1);
    // Gap 12 -> 20 es > 1 (requiere ellipsis)
    expect(vis[vis.length - 1] - vis[vis.length - 2]).toBeGreaterThan(1);
  });

  it('16. Concurrencia: Respuesta A pendiente, B nueva, B SUCCESS, A SUCCESS -> Prevalece B', () => {
    const subjectA = new Subject<EvaluacionesPaginadasDto>();
    const subjectB = new Subject<EvaluacionesPaginadasDto>();

    serviceMock.listarEvaluaciones
      .mockReturnValueOnce(subjectA)
      .mockReturnValueOnce(subjectB);

    // Consulta A (página 2)
    component.cambiarPagina(2);
    expect(component.cargandoEvaluaciones()).toBe(true);

    // Consulta B (página 3)
    component.cambiarPagina(3);

    // B responde primero con éxito
    const resB = crearPaginado(3, 10, 35, 4, 2);
    resB.items[0].riesgoCodigo = 'RIE-B-PAGE3';
    subjectB.next(resB);
    subjectB.complete();

    expect(component.pagina()).toBe(3);
    expect(component.evaluaciones()[0].riesgoCodigo).toBe('RIE-B-PAGE3');
    expect(component.cargandoEvaluaciones()).toBe(false);

    // A responde después con éxito (stale)
    const resA = crearPaginado(2, 10, 35, 4, 2);
    resA.items[0].riesgoCodigo = 'RIE-A-PAGE2';
    subjectA.next(resA);
    subjectA.complete();

    // Deberá IGNORAR A y mantener B (página 3)
    expect(component.pagina()).toBe(3);
    expect(component.evaluaciones()[0].riesgoCodigo).toBe('RIE-B-PAGE3');
  });

  it('17. Concurrencia: A pendiente, B nueva, B SUCCESS, A ERROR -> Error de A es ignorado', () => {
    const subjectA = new Subject<EvaluacionesPaginadasDto>();
    const subjectB = new Subject<EvaluacionesPaginadasDto>();

    serviceMock.listarEvaluaciones
      .mockReturnValueOnce(subjectA)
      .mockReturnValueOnce(subjectB);

    // Consulta A (búsqueda vieja)
    component.alCambiarFiltroBuscar('viejo');
    vi.advanceTimersByTime(300);

    // Consulta B (búsqueda nueva)
    component.alCambiarFiltroBuscar('nuevo');
    vi.advanceTimersByTime(300);

    // B responde con éxito
    const resB = crearPaginado(1, 10, 10, 1, 1);
    resB.items[0].riesgoCodigo = 'RIE-NUEVO';
    subjectB.next(resB);
    subjectB.complete();

    expect(component.evaluaciones()[0].riesgoCodigo).toBe('RIE-NUEVO');
    expect(component.errorEvaluaciones()).toBeNull();

    // A responde con error de red (stale)
    subjectA.error(new Error('Network error on old query'));

    // Deberá IGNORAR el error de A
    expect(component.errorEvaluaciones()).toBeNull();
    expect(component.evaluaciones()[0].riesgoCodigo).toBe('RIE-NUEVO');
    expect(component.cargandoEvaluaciones()).toBe(false);
  });

  it('18. Concurrencia: A ERROR antes, B SUCCESS vigente -> UI termina con B', () => {
    serviceMock.listarEvaluaciones.mockReturnValueOnce(throwError(() => ({ error: { detail: 'Error previo' } })));

    component.cargarEvaluaciones();
    expect(component.errorEvaluaciones()).toBe('Error previo');

    serviceMock.listarEvaluaciones.mockReturnValueOnce(of(crearPaginado(1, 10, 10, 1)));

    component.cargarEvaluaciones();
    expect(component.errorEvaluaciones()).toBeNull();
    expect(component.evaluaciones().length).toBe(2);
  });

  it('19. items=null -> []', () => {
    const res: any = { items: null, pagina: 1, registrosPorPagina: 10, totalRegistros: 0, totalPaginas: 0 };
    serviceMock.listarEvaluaciones.mockReturnValue(of(res));

    component.cargarEvaluaciones();

    expect(component.evaluaciones()).toEqual([]);
  });

  it('20. items={} truthy no-array -> []', () => {
    const res: any = { items: { not: 'an array' }, pagina: 1, registrosPorPagina: 10, totalRegistros: 0, totalPaginas: 0 };
    serviceMock.listarEvaluaciones.mockReturnValue(of(res));

    component.cargarEvaluaciones();

    expect(component.evaluaciones()).toEqual([]);
  });

  it('21. totalRegistros null/NaN/negativo -> normalización segura a 0', () => {

    [null, NaN, -5, 'invalido'].forEach(badVal => {
      const res: any = { items: [], pagina: 1, registrosPorPagina: 10, totalRegistros: badVal, totalPaginas: 0 };
      serviceMock.listarEvaluaciones.mockReturnValue(of(res));

      component.cargarEvaluaciones();

      expect(component.totalRegistros()).toBe(0);
    });
  });

  it('22. totalPaginas null/NaN/negativo -> derivación segura', () => {
    [null, NaN, -5].forEach(badVal => {
      const res: any = { items: [], pagina: 1, registrosPorPagina: 10, totalRegistros: 25, totalPaginas: badVal };
      serviceMock.listarEvaluaciones.mockReturnValue(of(res));

      component.cargarEvaluaciones();

      expect(component.totalPaginas()).toBe(3); // Math.ceil(25 / 10)
    });
  });

  it('23. Backend devuelve pagina=2 cuando frontend solicitó 8 -> Frontend adopta pagina=2', () => {
    component.pagina.set(8);
    // Servidor ajustó a la página 2 efectiva
    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(2, 10, 15, 2)));

    component.cargarEvaluaciones();

    expect(component.pagina()).toBe(2);
    expect(component.totalPaginas()).toBe(2);
  });

  it('24. Retry preserva filtros', () => {
    component.filtroBuscar.set('fraude');
    component.filtroEstado.set('CERRADA');
    component.pagina.set(2);
    component.registrosPorPagina.set(20);
    serviceMock.listarEvaluaciones.mockClear();

    // Reintentar invoca cargarEvaluaciones()
    component.cargarEvaluaciones();

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'fraude',
      estado: 'CERRADA',
      pagina: 2,
      registrosPorPagina: 20
    });
  });

  it('25. loading solo termina por consulta vigente', () => {
    const subjectA = new Subject<EvaluacionesPaginadasDto>();
    const subjectB = new Subject<EvaluacionesPaginadasDto>();

    serviceMock.listarEvaluaciones
      .mockReturnValueOnce(subjectA)
      .mockReturnValueOnce(subjectB);

    component.cambiarPagina(2); // A
    component.cambiarPagina(3); // B

    expect(component.cargandoEvaluaciones()).toBe(true);

    // A responde (stale)
    subjectA.next(crearPaginado(2, 10, 30, 3));
    subjectA.complete();

    // Loading DEBE SEGUIR TRUE porque B está en curso
    expect(component.cargandoEvaluaciones()).toBe(true);

    // B responde
    subjectB.next(crearPaginado(3, 10, 30, 3));
    subjectB.complete();

    expect(component.cargandoEvaluaciones()).toBe(false);
  });

  it('26. ngOnDestroy: cancelación e invalida request pendiente', () => {
    const itemsIniciales = crearPaginado(1, 10, 10, 1).items;
    component.evaluaciones.set(itemsIniciales);
    component.pagina.set(1);
    component.totalRegistros.set(10);
    component.totalPaginas.set(1);
    component.errorEvaluaciones.set(null);

    const subjectA = new Subject<EvaluacionesPaginadasDto>();
    serviceMock.listarEvaluaciones.mockReturnValue(subjectA);

    component.cargarEvaluaciones();
    expect(component.cargandoEvaluaciones()).toBe(true);

    component.ngOnDestroy();

    const respuestaTardia = crearPaginado(2, 10, 50, 5, 5);
    respuestaTardia.items[0].riesgoCodigo = 'TARDIO';
    subjectA.next(respuestaTardia);
    subjectA.complete();

    expect(component.evaluaciones()).toBe(itemsIniciales);
    expect(component.pagina()).toBe(1);
    expect(component.totalRegistros()).toBe(10);
    expect(component.totalPaginas()).toBe(1);
    expect(component.errorEvaluaciones()).toBeNull();
    expect(subjectA.observed).toBe(false);
  });

  it('27. Búsqueda debounce + cambio Estado continúa generando UNA sola consulta', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroBuscar('prueba');
    vi.advanceTimersByTime(100);

    component.alCambiarFiltroEstado('APROBADA');

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'prueba',
      estado: 'APROBADA',
      pagina: 1,
      registrosPorPagina: 10
    });

    vi.advanceTimersByTime(500);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
  });

  it('28. Limpieza trim/whitespace en búsqueda sigue funcionando', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroBuscar('   búsqueda con espacios   ');
    vi.advanceTimersByTime(300);

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'búsqueda con espacios',
      estado: undefined,
      pagina: 1,
      registrosPorPagina: 10
    });
  });

  it('29. Botón Anterior: deshabilitado cuando totalPaginas === 0', () => {
    component.evaluaciones.set([]);
    component.totalRegistros.set(0);
    component.totalPaginas.set(0);
    component.pagina.set(1);
    fixture.detectChanges();

    const btnAnterior = Array.from(fixture.nativeElement.querySelectorAll('button'))
      .find((b: any) => b.textContent.includes('Anterior'));

    expect((btnAnterior as HTMLButtonElement).disabled).toBe(true);
  });
});
