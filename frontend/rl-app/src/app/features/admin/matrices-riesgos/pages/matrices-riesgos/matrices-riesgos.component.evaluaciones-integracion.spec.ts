import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { vi, describe, beforeEach, afterEach, it, expect } from 'vitest';

import { MatricesRiesgosComponent } from './matrices-riesgos.component';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionRiesgoResumenDto, EvaluacionesPaginadasDto } from '../../models/matrices-riesgos.models';

describe('MatricesRiesgosComponent — evaluaciones: integración funcional', () => {
  let component: MatricesRiesgosComponent;
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let serviceMock: any;
  let routerMock: { navigate: ReturnType<typeof vi.fn> };

  function crearEvaluacion(id: number, codigo: string, estado: string = 'BORRADOR'): EvaluacionRiesgoResumenDto {
    return {
      evaId: id,
      evaRiesgoId: 100 + id,
      riesgoCodigo: codigo,
      riesgoNombre: `Riesgo ${codigo}`,
      evaVersionId: 1,
      versionCodigo: 'V1',
      versionNumero: 1,
      estado,
      vri: 6,
      vrr: 4,
      nivelResidual: 'MEDIO',
      fechaEval: '2026-08-18T10:00:00Z'
    };
  }

  function crearPaginado(
    pagina: number = 1,
    registrosPorPagina: number = 10,
    totalRegistros: number = 50,
    totalPaginas: number = 5,
    cantItems: number = 10
  ): EvaluacionesPaginadasDto {
    const items: EvaluacionRiesgoResumenDto[] = [];
    for (let i = 1; i <= cantItems; i++) {
      const id = ((pagina - 1) * registrosPorPagina) + i;
      items.push(crearEvaluacion(id, `RIESGO-${id}`));
    }
    return {
      items,
      pagina,
      registrosPorPagina,
      totalRegistros,
      totalPaginas
    };
  }

  beforeEach(async () => {
    vi.useFakeTimers();

    serviceMock = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([])),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarEvaluaciones: vi.fn().mockReturnValue(of(crearPaginado(1, 10, 50, 5, 10))),
      obtenerEvaluacion: vi.fn().mockReturnValue(of({ evaId: 12, evaRiesgoId: 112, evaVersionId: 1, evaDataJson: '{}' })),
      metodologiaPorVersion: vi.fn().mockReturnValue(of({ versionFormularioId: 1, codigo: 'MATRIZ_RIESGOS_LAFT', version: 1, secciones: [] })),
      obtenerFamiliaFormularioPorId: vi.fn().mockReturnValue(of({ famId: 1 })),
      obtenerFormularioVigente: vi.fn().mockReturnValue(of(null)),
      obtenerResumenEvaluacion: vi.fn().mockReturnValue(of(null)),
      obtenerFlujoEvaluacion: vi.fn().mockReturnValue(of([])),
      obtenerFlujos: vi.fn().mockReturnValue(of([])),
      obtenerMetodologiaVigente: vi.fn().mockReturnValue(of(null)),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(null)),
      metodologiaVigente: vi.fn().mockReturnValue(of({
        versionFormularioId: 1,
        codigo: 'MATRIZ_RIESGOS_LAFT',
        version: 1,
        secciones: [],
        catalogos: [],
        reglasEvaluacion: []
      }))
    };

    routerMock = {
      navigate: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent, HttpClientTestingModule, FormsModule],
      providers: [
        { provide: MatricesRiesgosService, useValue: serviceMock },
        { provide: Router, useValue: routerMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  // 1. CARGA INICIAL
  it('1. Carga inicial: exactamente 1 consulta con filtros vacíos y size 10 (sin prefetch 200)', () => {
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: undefined,
      estado: undefined,
      pagina: 1,
      registrosPorPagina: 10
    });
    expect(component.pagina()).toBe(1);
    expect(component.registrosPorPagina()).toBe(10);
    expect(component.filtroBuscar()).toBe('');
    expect(component.filtroEstado()).toBe('');
  });

  // 2. DOM SUPERIOR COMPLETO
  it('2. DOM Superior: existe #filtro-buscar, #filtro-estado y #filtro-registros-por-pagina con [10, 20, 50]', () => {
    const buscarEl = fixture.nativeElement.querySelector('#filtro-buscar');
    const estadoEl = fixture.nativeElement.querySelector('#filtro-estado');
    const porPaginaEl = fixture.nativeElement.querySelector('#filtro-registros-por-pagina') as HTMLSelectElement;

    expect(buscarEl).toBeTruthy();
    expect(estadoEl).toBeTruthy();
    expect(porPaginaEl).toBeTruthy();

    const opciones = Array.from(porPaginaEl.options).map(o => Number(o.value));
    expect(opciones).toEqual([10, 20, 50]);
  });

  it('2b. DOM Superior: porPaginaEl se deshabilita durante carga y se habilita al finalizar', () => {
    const subjectA = new Subject<EvaluacionesPaginadasDto>();
    serviceMock.listarEvaluaciones.mockReturnValue(subjectA);

    component.cargarEvaluaciones();
    expect(component.cargandoEvaluaciones()).toBe(true);

    subjectA.next(crearPaginado(1, 10, 10, 1, 10));
    subjectA.complete();
    expect(component.cargandoEvaluaciones()).toBe(false);
    fixture.detectChanges();

    const porPaginaEl = fixture.nativeElement.querySelector('#filtro-registros-por-pagina') as HTMLSelectElement;
    expect(porPaginaEl.disabled).toBe(false);
  });

  // 3. FLUJO INTEGRAL
  it('3. Flujo integral: buscar -> estado -> size -> pagina preserva contexto completo', () => {
    serviceMock.listarEvaluaciones.mockClear();

    // Buscar
    component.alCambiarFiltroBuscar('fraude');
    vi.advanceTimersByTime(300);

    // Estado
    component.alCambiarFiltroEstado('APROBADA');
    vi.advanceTimersByTime(100);

    // Size
    component.cambiarRegistrosPorPagina(20);
    vi.advanceTimersByTime(100);

    // Página (totalPaginas debe ser >= 2 para permitir cambiar a página 2)
    component.totalPaginas.set(5);
    component.cambiarPagina(2);
    vi.advanceTimersByTime(100);

    expect(serviceMock.listarEvaluaciones).toHaveBeenLastCalledWith({
      buscar: 'fraude',
      estado: 'APROBADA',
      pagina: 2,
      registrosPorPagina: 20
    });
  });

  // 4. RESETS
  it('4. Resets: nueva búsqueda, cambio de estado o cambio de page-size desde pág >1 reinician pagina a 1', () => {
    component.pagina.set(3);

    component.alCambiarFiltroBuscar('riesgo');
    expect(component.pagina()).toBe(1);

    component.pagina.set(4);
    component.alCambiarFiltroEstado('EN_REVISION');
    expect(component.pagina()).toBe(1);

    component.pagina.set(5);
    component.cambiarRegistrosPorPagina(50);
    expect(component.pagina()).toBe(1);
  });

  // 5. DEBOUNCE + ESTADO (F4.1 REGRESIÓN)
  it('5. Debounce + Estado: escribir y cambiar Estado antes de 300 ms produce 1 sola consulta combinada', () => {
    serviceMock.listarEvaluaciones.mockClear();

    component.alCambiarFiltroBuscar('operativo');
    vi.advanceTimersByTime(100);

    component.alCambiarFiltroEstado('OBSERVADA');

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'operativo',
      estado: 'OBSERVADA',
      pagina: 1,
      registrosPorPagina: 10
    });

    vi.advanceTimersByTime(500);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
  });

  // 6. PAGINADOR DOM Y ACCESIBILIDAD
  it('6. Paginador DOM: renderiza Anterior, Siguiente, paginasVisibles, aria-current y aria-label', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(6, 10, 250, 25, 10)));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const btnAnterior = buttons.find(b => b.textContent?.includes('Anterior'));
    const btnSiguiente = buttons.find(b => b.textContent?.includes('Siguiente'));
    const btnActivo = buttons.find(b => b.getAttribute('aria-current') === 'page');

    expect(btnAnterior).toBeTruthy();
    expect(btnSiguiente).toBeTruthy();
    expect(btnActivo?.textContent?.trim()).toBe('6');
    expect(btnActivo?.getAttribute('aria-label')).toBe('Ir a la página 6');

    // 1 ... 4 5 6 7 8 ... 25
    expect(component.paginasVisibles()).toEqual([1, 4, 5, 6, 7, 8, 25]);
  });

  // 7. 0 REGISTROS
  it('7. 0 Registros: muestra "Mostrando 0 – 0 de 0 registros" y deshabilita Anterior y Siguiente', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of({ items: [], pagina: 1, registrosPorPagina: 10, totalRegistros: 0, totalPaginas: 0 }));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Mostrando 0 – 0');

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const btnAnterior = buttons.find(b => b.textContent?.includes('Anterior'));
    const btnSiguiente = buttons.find(b => b.textContent?.includes('Siguiente'));

    expect(btnAnterior?.disabled).toBe(true);
    expect(btnSiguiente?.disabled).toBe(true);
  });

  // 8. UNA PÁGINA
  it('8. Una página: totalRegistros <= size -> Anterior y Siguiente deshabilitados', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(1, 10, 5, 1, 5)));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const btnAnterior = buttons.find(b => b.textContent?.includes('Anterior'));
    const btnSiguiente = buttons.find(b => b.textContent?.includes('Siguiente'));

    expect(btnAnterior?.disabled).toBe(true);
    expect(btnSiguiente?.disabled).toBe(true);
  });

  // 9. ÚLTIMA PÁGINA PARCIAL
  it('9. Última página parcial: 23 registros, size 10, pág 3 -> "Mostrando 21 – 23 de 23 registros"', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(3, 10, 23, 3, 3)));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Mostrando 21 – 23');
    expect(component.totalRegistros()).toBe(23);
  });

  // 10. PÁGINA FUERA DE RANGO (BACKEND PAGINA EFECTIVA)
  it('10. Página fuera de rango: solicitar pág 8 en total 2 -> adopta pagina() === 2', () => {
    component.pagina.set(8);
    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(2, 10, 15, 2, 5)));

    component.cargarEvaluaciones();
    expect(component.pagina()).toBe(2);
    expect(component.totalPaginas()).toBe(2);
  });

  // 11. CONCURRENCIA (STALE SUCCESS & STALE ERROR)
  it('11. Concurrencia: respuesta A tardía (SUCCESS o ERROR) es ignorada tras respuesta B nueva', () => {
    const subjectA = new Subject<EvaluacionesPaginadasDto>();
    const subjectB = new Subject<EvaluacionesPaginadasDto>();

    serviceMock.listarEvaluaciones
      .mockReturnValueOnce(subjectA)
      .mockReturnValueOnce(subjectB);

    component.cargarEvaluaciones(); // Solicitud A
    component.cargarEvaluaciones(); // Solicitud B

    // B responde primero
    const paginadoB = crearPaginado(3, 10, 30, 3, 10);
    subjectB.next(paginadoB);
    subjectB.complete();

    expect(component.pagina()).toBe(3);
    expect(component.evaluaciones()).toEqual(paginadoB.items);

    // A responde tardíamente con SUCCESS
    subjectA.next(crearPaginado(2, 10, 30, 3, 10));
    subjectA.complete();

    expect(component.pagina()).toBe(3);
    expect(component.evaluaciones()).toEqual(paginadoB.items);

    // A responde tardíamente con ERROR
    subjectA.error(new Error('Network error tardío'));
    expect(component.errorEvaluaciones()).toBeNull();
    expect(component.pagina()).toBe(3);
  });

  // 12. RETRY PRESERVA CONTEXTO
  it('12. Retry preserva contexto completo de filtros, página y page-size', () => {
    component.filtroBuscar.set('fraude');
    component.filtroEstado.set('CERRADA');
    component.pagina.set(2);
    component.registrosPorPagina.set(20);

    serviceMock.listarEvaluaciones.mockReturnValue(throwError(() => new Error('Server error')));
    component.cargarEvaluaciones();

    expect(component.errorEvaluaciones()).toBeTruthy();

    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(2, 20, 30, 2, 10)));
    component.cargarEvaluaciones();

    expect(serviceMock.listarEvaluaciones).toHaveBeenLastCalledWith({
      buscar: 'fraude',
      estado: 'CERRADA',
      pagina: 2,
      registrosPorPagina: 20
    });
    expect(component.errorEvaluaciones()).toBeNull();
  });

  // 13. KPIs
  it('13. KPIs: TOTAL EVALUACIONES coincide con totalRegistros del servidor', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of(crearPaginado(1, 10, 42, 5, 10)));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    expect(component.totalRegistros()).toBe(42);
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('42');
  });

  // 14. ACCIONES (VER, EDITAR BORRADOR, SEGUIMIENTO)
  it('14. Acciones: Ver, Editar (solo BORRADOR) y Seguimiento abren modales con evaId correcto', () => {
    const evalBorrador = crearEvaluacion(12, 'RIE-12', 'BORRADOR');
    const evalAprobada = crearEvaluacion(15, 'RIE-15', 'APROBADA');

    // Ver
    component.abrirModalVer(evalBorrador);
    expect(component.modalVerAbierto()).toBe(true);
    expect(component.evaluacionSeleccionada()?.evaId).toBe(12);
    component.cerrarModalVer();
    expect(component.modalVerAbierto()).toBe(false);

    // Editar BORRADOR
    component.editarEvaluacion(evalBorrador);
    expect(component.modalEditarAbierto()).toBe(true);
    expect(component.evaluacionSeleccionada()?.evaId).toBe(12);
    component.cerrarModalEditar();
    expect(component.modalEditarAbierto()).toBe(false);

    // Seguimiento
    component.abrirModalSeguimiento(evalAprobada);
    expect(component.modalSeguimientoAbierto()).toBe(true);
    expect(component.evaluacionResumenSeleccionada()?.evaId).toBe(15);
    component.cerrarModalSeguimiento();
    expect(component.modalSeguimientoAbierto()).toBe(false);
  });

  // 15. RECARGA POST EDICIÓN / TRANSICIÓN
  it('15. Recarga post edición o transición: reutiliza buscar, estado, página y page-size actuales', () => {
    component.filtroBuscar.set('matriz');
    component.filtroEstado.set('EN_REVISION');
    component.pagina.set(2);
    component.registrosPorPagina.set(20);

    serviceMock.listarEvaluaciones.mockClear();
    component.cargarEvaluaciones();

    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: 'matriz',
      estado: 'EN_REVISION',
      pagina: 2,
      registrosPorPagina: 20
    });
  });
});
