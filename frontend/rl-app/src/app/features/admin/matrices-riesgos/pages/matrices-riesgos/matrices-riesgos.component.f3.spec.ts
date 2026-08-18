import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import {
  EvaluacionRiesgoResumenDto,
  EvaluacionesPaginadasDto,
  VersionFormularioDto
} from '../../models/matrices-riesgos.models';
import { MatricesRiesgosComponent } from './matrices-riesgos.component';

describe('MatricesRiesgosComponent F3 Tabla Evaluaciones', () => {
  let fixture: ComponentFixture<MatricesRiesgosComponent>;
  let component: MatricesRiesgosComponent;
  let serviceMock: any;

  const version: VersionFormularioDto = {
    verId: 10,
    verFamiliaId: 1,
    verCodigo: 'V1.0',
    verVersion: 1,
    verJson: '{}',
    verHash: 'hash-f3',
    verEstado: 'PUBLISHED',
    verVigente: true,
    verFechaCreacion: '2026-08-17',
    verUsrCreacion: 1
  };

  const crearEvaluacion = (
    evaId: number,
    estado: string,
    nivelResidual: string | null,
    vri: number | null = 12,
    vrr: number | null = 4
  ): EvaluacionRiesgoResumenDto => ({
    evaId,
    evaRiesgoId: evaId + 100,
    riesgoCodigo: `RIE-${String(evaId).padStart(3, '0')}`,
    riesgoNombre: `Riesgo institucional ${evaId}`,
    evaVersionId: 10,
    versionCodigo: 'V1.0',
    versionNumero: 1,
    estado,
    vri,
    vrr,
    nivelResidual,
    fechaEval: '2026-08-17T10:30:00Z'
  });

  const paginado: EvaluacionesPaginadasDto = {
    items: [
      {
        evaId: 101,
        evaRiesgoId: 5,
        riesgoCodigo: 'RIE-005',
        riesgoNombre: 'Riesgo institucional cinco',
        evaVersionId: 10,
        versionCodigo: 'V1.0',
        versionNumero: 1,
        estado: 'BORRADOR',
        vri: 12,
        vrr: 4,
        nivelResidual: 'BAJO',
        fechaEval: '2026-08-17T10:30:00Z'
      },
      {
        evaId: 102,
        evaRiesgoId: 6,
        riesgoCodigo: 'RIE-006',
        riesgoNombre: 'Riesgo institucional seis',
        evaVersionId: 10,
        versionCodigo: 'V1.0',
        versionNumero: 1,
        estado: 'EN_REVISION',
        vri: null,
        vrr: null,
        nivelResidual: null,
        fechaEval: '2026-08-17T11:00:00Z'
      }
    ],
    pagina: 1,
    registrosPorPagina: 10,
    totalRegistros: 37,
    totalPaginas: 4
  };

  beforeEach(async () => {
    serviceMock = {
      listarFamiliasFormulario: vi.fn().mockReturnValue(of([])),
      listarRiesgos: vi.fn().mockReturnValue(of([])),
      listarEvaluaciones: vi.fn().mockReturnValue(of(paginado)),
      obtenerVersionVigenteFormulario: vi.fn().mockReturnValue(of(version)),
      metodologiaVigente: vi.fn().mockReturnValue(of({
        versionFormularioId: 10,
        codigo: 'MATRIZ_RIESGOS_LAFT',
        version: 1,
        secciones: [],
        catalogos: [],
        reglas: []
      })),
      obtenerConsolidado: vi.fn().mockReturnValue(of([])),
      listarHistorialVersionesFormulario: vi.fn().mockReturnValue(of([]))
    };

    await TestBed.configureTestingModule({
      imports: [MatricesRiesgosComponent],
      providers: [
        { provide: MatricesRiesgosService, useValue: serviceMock },
        { provide: AuthService, useValue: { tieneRol: vi.fn().mockReturnValue(true) } },
        { provide: GlobalHttpStateService, useValue: { limpiarError: vi.fn() } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MatricesRiesgosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('F3 consulta solo la pagina visible y no dispara la carga operativa de 200 registros', () => {
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledTimes(1);
    expect(serviceMock.listarEvaluaciones).toHaveBeenCalledWith({
      buscar: undefined,
      estado: undefined,
      pagina: 1,
      registrosPorPagina: 10
    });
  });

  it('F3 renderiza las nueve columnas institucionales de Evaluaciones', () => {
    const panel = fixture.nativeElement.querySelector('#panel-evaluaciones') as HTMLElement;
    const encabezados = Array.from(panel.querySelectorAll('thead th'))
      .map(th => (th.textContent || '').trim().replace(/\s+/g, ' '));

    expect(encabezados).toEqual([
      'EVALUACIÓN',
      'RIESGO',
      'VERSIÓN',
      'ESTADO',
      'VRI',
      'VRR',
      'NIVEL RESIDUAL',
      'FECHA',
      'ACCIONES'
    ]);
  });

  it('F3 conserva codigo, nombre, version, fecha y acciones de la fila real', () => {
    const panel = fixture.nativeElement.querySelector('#panel-evaluaciones') as HTMLElement;
    const filas = Array.from(panel.querySelectorAll('tbody tr')) as HTMLTableRowElement[];

    expect(filas).toHaveLength(2);
    expect(filas[0].textContent).toContain('#101');
    expect(filas[0].textContent).toContain('RIE-005');
    expect(filas[0].textContent).toContain('Riesgo institucional cinco');
    expect(filas[0].textContent).toContain('V1.0 · v1');
    expect(filas[0].textContent).toContain('17/08/2026');
    expect(filas[0].querySelector('td:nth-child(2) div[title]')?.getAttribute('title'))
      .toBe('Riesgo institucional cinco');
    expect(filas[0].querySelectorAll('td:nth-child(9) button')).toHaveLength(3);

    expect(filas[1].textContent).toContain('#102');
    expect(filas[1].textContent).toContain('RIE-006');
    expect(filas[1].querySelectorAll('td:nth-child(9) button')).toHaveLength(2);
  });

  it('F3 cubre los seis estados institucionales y restringe Editar a BORRADOR', () => {
    const estados = ['BORRADOR', 'EN_REVISION', 'OBSERVADA', 'APROBADA', 'RECHAZADA', 'CERRADA'];
    component.evaluaciones.set(estados.map((estado, indice) =>
      crearEvaluacion(201 + indice, estado, indice === 5 ? null : 'BAJO')
    ));
    fixture.detectChanges();

    const filas = Array.from(
      fixture.nativeElement.querySelectorAll('#panel-evaluaciones tbody tr')
    ) as HTMLTableRowElement[];

    expect(filas).toHaveLength(6);
    estados.forEach((estado, indice) => {
      const fila = filas[indice];
      const acciones = Array.from(fila.querySelectorAll('td:nth-child(9) button')) as HTMLButtonElement[];
      const etiquetas = acciones.map(boton => boton.getAttribute('aria-label'));

      expect(fila.querySelector('td:nth-child(4)')?.textContent).toContain(estado);
      expect(etiquetas).toContain('Ver evaluación');
      expect(etiquetas).toContain('Seguimiento de evaluación');
      if (estado === 'BORRADOR') {
        expect(etiquetas).toContain('Editar evaluación');
        expect(acciones).toHaveLength(3);
      } else {
        expect(etiquetas).not.toContain('Editar evaluación');
        expect(acciones).toHaveLength(2);
      }
    });
  });

  it('F3 preserva cero como valor valido de VRI/VRR y usa guion solo para ausencia', () => {
    component.evaluaciones.set([
      crearEvaluacion(301, 'BORRADOR', 'BAJO', 0, 0),
      crearEvaluacion(302, 'EN_REVISION', null, null, null)
    ]);
    fixture.detectChanges();

    const filas = Array.from(
      fixture.nativeElement.querySelectorAll('#panel-evaluaciones tbody tr')
    ) as HTMLTableRowElement[];

    expect(filas[0].querySelector('td:nth-child(5)')?.textContent?.trim()).toBe('0');
    expect(filas[0].querySelector('td:nth-child(6)')?.textContent?.trim()).toBe('0');
    expect(filas[1].querySelector('td:nth-child(5)')?.textContent?.trim()).toBe('-');
    expect(filas[1].querySelector('td:nth-child(6)')?.textContent?.trim()).toBe('-');
    expect(filas[1].querySelector('td:nth-child(7)')?.textContent?.trim()).toBe('-');
  });

  it('F3 renderiza BAJO, MEDIO, ALTO, CRITICO y ausencia con semantica visual estable', () => {
    const niveles: Array<string | null> = ['BAJO', 'MEDIO', 'ALTO', 'CRITICO', null];
    component.evaluaciones.set(niveles.map((nivel, indice) =>
      crearEvaluacion(401 + indice, 'APROBADA', nivel)
    ));
    fixture.detectChanges();

    const celdas = Array.from(
      fixture.nativeElement.querySelectorAll('#panel-evaluaciones tbody tr td:nth-child(7)')
    ) as HTMLTableCellElement[];

    expect(celdas.map(celda => celda.textContent?.trim())).toEqual(['BAJO', 'MEDIO', 'ALTO', 'CRITICO', '-']);
    expect(celdas[0].classList.contains('text-emerald-600')).toBe(true);
    expect(celdas[1].classList.contains('text-amber-600')).toBe(true);
    expect(celdas[2].classList.contains('text-red-600')).toBe(true);
    expect(celdas[3].classList.contains('text-red-600')).toBe(true);
  });

  it('F3 usa metadatos server-side independientes del numero de filas visibles', () => {
    expect(component.evaluaciones()).toHaveLength(2);
    expect(component.totalRegistros()).toBe(37);
    expect(component.totalPaginas()).toBe(4);

    const panel = fixture.nativeElement.querySelector('#panel-evaluaciones') as HTMLElement;
    const textoSinEspacios = (panel.textContent || '').replace(/\s+/g, '');
    expect(textoSinEspacios).toContain('de37registros');
    expect(textoSinEspacios).toContain('Pág.1de4');
  });

  it('F3 presenta estado vacio institucional sin romper las nueve columnas', () => {
    component.evaluaciones.set([]);
    component.totalRegistros.set(0);
    component.totalPaginas.set(0);
    fixture.detectChanges();

    const filaVacia = fixture.nativeElement.querySelector('#panel-evaluaciones tbody tr') as HTMLTableRowElement;
    const celda = filaVacia.querySelector('td') as HTMLTableCellElement;

    expect(celda.getAttribute('colspan')).toBe('9');
    expect(celda.textContent).toContain('Sin evaluaciones registradas con los filtros seleccionados.');
  });

  it('F3 mantiene loading independiente mientras la consulta de Evaluaciones esta pendiente', () => {
    const pendiente = new Subject<EvaluacionesPaginadasDto>();
    serviceMock.listarEvaluaciones.mockReturnValue(pendiente.asObservable());

    component.cargarEvaluaciones();
    fixture.detectChanges();

    expect(component.cargandoEvaluaciones()).toBe(true);
    expect(component.errorEvaluaciones()).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Cargando evaluaciones...');

    pendiente.next(paginado);
    pendiente.complete();
    fixture.detectChanges();
    expect(component.cargandoEvaluaciones()).toBe(false);
  });

  it('F3 ante error limpia filas, conserva el mensaje real y ofrece reintento', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(throwError(() => ({
      error: { detail: 'Fallo controlado F3.2' }
    })));

    component.cargarEvaluaciones();
    fixture.detectChanges();

    expect(component.evaluaciones()).toEqual([]);
    expect(component.errorEvaluaciones()).toBe('Fallo controlado F3.2');
    expect(component.cargandoEvaluaciones()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Fallo controlado F3.2');
    expect(fixture.nativeElement.textContent).toContain('Reintentar');
  });

  it('F3 normaliza items nulos sin romper el contrato Array de la tabla', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of({ ...paginado, items: null }));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    expect(Array.isArray(component.evaluaciones())).toBe(true);
    expect(component.evaluaciones()).toEqual([]);
    expect(component.errorEvaluaciones()).toBeNull();
  });

  it('F3.2 debe rechazar cualquier items truthy que no sea Array y resetear metadatos paginados ante error', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of({ ...paginado, items: { invalid: true } }));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    expect(Array.isArray(component.evaluaciones())).toBe(true);
    expect(component.evaluaciones()).toEqual([]);

    serviceMock.listarEvaluaciones.mockReturnValue(throwError(() => new Error('Error backend')));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    expect(component.evaluaciones()).toEqual([]);
    expect(component.totalRegistros()).toBe(0);
    expect(component.totalPaginas()).toBe(0);
  });

  it('F3.2 debe mostrar totalRegistros como Total Evaluaciones y declarar los conteos por estado como pagina visible', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of(paginado));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const textContent = el.textContent || '';

    expect(component.totalRegistros()).toBe(37);
    expect(textContent).toContain('37');
    expect(textContent).toContain('Total según la consulta actual');
    expect(textContent).toContain('Pendientes en la página actual');
    expect(textContent).toContain('En análisis en la página actual');
    expect(textContent).toContain('Oficiales en la página actual');
  });
});
