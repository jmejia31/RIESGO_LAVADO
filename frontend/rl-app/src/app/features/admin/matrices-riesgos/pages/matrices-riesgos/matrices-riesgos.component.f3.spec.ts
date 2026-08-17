import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../../../core/auth/auth.service';
import { GlobalHttpStateService } from '../../../../../core/services/global-http-state.service';
import { MatricesRiesgosService } from '../../data-access/matrices-riesgos.service';
import { EvaluacionesPaginadasDto, VersionFormularioDto } from '../../models/matrices-riesgos.models';
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

  it('F3 conserva la semantica de fila y acciones segun estado', () => {
    const panel = fixture.nativeElement.querySelector('#panel-evaluaciones') as HTMLElement;
    const filas = Array.from(panel.querySelectorAll('tbody tr')) as HTMLTableRowElement[];

    expect(filas).toHaveLength(2);
    expect(filas[0].textContent).toContain('#101');
    expect(filas[0].textContent).toContain('RIE-005');
    expect(filas[0].textContent).toContain('Riesgo institucional cinco');
    expect(filas[0].textContent).toContain('V1.0 · v1');
    expect(filas[0].textContent).toContain('BORRADOR');
    expect(filas[0].textContent).toContain('BAJO');
    expect(filas[0].querySelectorAll('button')).toHaveLength(3);

    expect(filas[1].textContent).toContain('#102');
    expect(filas[1].textContent).toContain('EN_REVISION');
    expect(filas[1].querySelectorAll('button')).toHaveLength(2);
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

  it('F3 normaliza items nulos sin romper el contrato Array de la tabla', () => {
    serviceMock.listarEvaluaciones.mockReturnValue(of({ ...paginado, items: null }));
    component.cargarEvaluaciones();
    fixture.detectChanges();

    expect(Array.isArray(component.evaluaciones())).toBe(true);
    expect(component.evaluaciones()).toEqual([]);
    expect(component.errorEvaluaciones()).toBeNull();
  });
});