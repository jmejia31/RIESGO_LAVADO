import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ConfiguracionService } from '../../../core/configuration/configuracion.service';
import { MatricesRiesgosService } from './data-access/matrices-riesgos.service';
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
      historial: vi.fn(() => of([]))
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
    TestBed.resetTestingModule();
  });

  it('lista matrices con los filtros activos y finaliza la carga', () => {
    const matrices = [{ matrizId: 7, nombreSujeto: 'Proveedor Uno' }];
    component.filtroBuscar.set('Proveedor');
    component.filtroEstado.set('CALCULADA');
    component.filtroSujetoTipo.set('PROVEEDOR');
    service['listar'].mockReturnValue(of(matrices));

    component.cargarMatrices();

    expect(service['listar']).toHaveBeenCalledWith({
      buscar: 'Proveedor', estado: 'CALCULADA', sujetoTipo: 'PROVEEDOR'
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

  it('carga el reporte usando el filtro vigente', () => {
    const reporte = { totales: { totalMatrices: 3 } };
    component.reporteFiltro.set({ estado: 'ACTIVA' });
    service['reporte'].mockReturnValue(of(reporte));

    component.cargarReporte();

    expect(service['reporte']).toHaveBeenCalledWith({ estado: 'ACTIVA' });
    expect(component.reporte()).toEqual(reporte);
    expect(component.cargandoReporte()).toBe(false);
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

  it('detiene la carga inicial y no consulta dependencias si falla la metodologia', () => {
    service['metodologiaVigente'].mockReturnValue(throwError(() => ({ error: { detalle: 'Metodologia no configurada' } })));

    component.cargarTodo();

    expect(component.error()).toBe('Metodologia no configurada');
    expect(component.cargando()).toBe(false);
    expect(service['dashboard']).not.toHaveBeenCalled();
    expect(service['reporte']).not.toHaveBeenCalled();
    expect(service['listar']).not.toHaveBeenCalled();
  });
});
