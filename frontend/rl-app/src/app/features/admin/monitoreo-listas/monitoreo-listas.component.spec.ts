import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { ConfiguracionService } from '../../../core/configuration/configuracion.service';
import { ListasService } from '../listas/data-access/listas.service';
import { MonitoreoListasComponent } from './monitoreo-listas.component';

describe('MonitoreoListasComponent', () => {
  let fixture: ComponentFixture<MonitoreoListasComponent>;
  let component: MonitoreoListasComponent;
  let service: Record<string, ReturnType<typeof vi.fn>>;

  beforeEach(async () => {
    service = {
      getJuridicas: vi.fn(() => of([])),
      getNaturales: vi.fn(() => of([])),
      getEmpleados: vi.fn(() => of([])),
      getTiposDocumento: vi.fn(() => of([])),
      getTiposListasCautela: vi.fn(() => of([])),
      getPoliticaEvidencias: vi.fn(() => of({
        maximoMb: 10,
        maximoBytes: 10 * 1024 * 1024,
        extensionesPermitidas: [],
        tiposPermitidosTexto: 'Archivos permitidos'
      })),
      getSeguimientos: vi.fn(() => of([]))
    };

    await TestBed.configureTestingModule({
      imports: [MonitoreoListasComponent],
      providers: [
        { provide: ListasService, useValue: service },
        { provide: ConfiguracionService, useValue: { configSistema: vi.fn(() => null) } },
        { provide: DomSanitizer, useValue: { bypassSecurityTrustResourceUrl: vi.fn(value => value) } }
      ]
    })
      .overrideComponent(MonitoreoListasComponent, { set: { template: '' } })
      .compileComponents();

    fixture = TestBed.createComponent(MonitoreoListasComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    fixture.destroy();
    TestBed.resetTestingModule();
  });

  it('carga las coincidencias juridicas y finaliza el indicador', () => {
    const juridicas = [{ numeroPatrono: 'P-01', nombre: 'Empresa Uno' }];
    service['getJuridicas'].mockReturnValue(of(juridicas));

    component.cargarDatos();

    expect(service['getJuridicas']).toHaveBeenCalledOnce();
    expect(component.juridicasRaw()).toEqual(juridicas);
    expect(component.cargando()).toBe(false);
  });

  it('cambia a personas naturales, limpia la busqueda y recarga el servicio correcto', () => {
    const naturales = [{ numeroIdentificacion: '0801', nombre: 'Ana' }];
    service['getNaturales'].mockReturnValue(of(naturales));
    component.busqueda.set('anterior');
    component.paginaActual.set(4);

    component.cambiarTipo('natural');

    expect(component.tipoActivo()).toBe('natural');
    expect(component.busqueda()).toBe('');
    expect(component.paginaActual()).toBe(1);
    expect(service['getNaturales']).toHaveBeenCalledOnce();
    expect(component.naturalesRaw()).toEqual(naturales);
  });

  it('limpia resultados de empleados y detiene la carga cuando el servicio falla', () => {
    component.tipoActivo.set('empleado');
    component.empleadosRaw.set([{ identidad: '01' }] as never);
    service['getEmpleados'].mockReturnValue(throwError(() => new Error('API no disponible')));

    component.cargarDatos();

    expect(component.empleadosRaw()).toEqual([]);
    expect(component.cargando()).toBe(false);
  });

  it('consulta seguimientos con el rango seleccionado y actualiza el historial', () => {
    const seguimientos = [{ detalleListaId: 12, motivoIngreso: 'Revision' }];
    component.filtroSeguimientoDesde.set('2026-07-01');
    component.filtroSeguimientoHasta.set('2026-07-15');
    service['getSeguimientos'].mockReturnValue(of(seguimientos));

    component.cargarSeguimientos('0801');

    expect(service['getSeguimientos']).toHaveBeenCalledWith('0801', '2026-07-01', '2026-07-15');
    expect(component.listaSeguimientos()).toEqual(seguimientos);
    expect(component.cargandoSeguimiento()).toBe(false);
  });

  it('deja el historial vacio y recupera el estado si falla la consulta de seguimientos', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    component.listaSeguimientos.set([{ detalleListaId: 12 }] as never);
    service['getSeguimientos'].mockReturnValue(throwError(() => new Error('Fallo controlado')));

    component.cargarSeguimientos('0801');

    expect(component.listaSeguimientos()).toEqual([]);
    expect(component.cargandoSeguimiento()).toBe(false);
    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it('inicializa datos, catalogos y conserva extensiones seguras por defecto', () => {
    const tiposDocumento = [{ tipoDocumentoId: 1, descripcion: 'Identidad' }];
    const tiposLista = [{ tipoListaCautelaId: 2, descripcion: 'PEP' }];
    service['getTiposDocumento'].mockReturnValue(of(tiposDocumento));
    service['getTiposListasCautela'].mockReturnValue(of(tiposLista));

    component.ngOnInit();

    expect(service['getJuridicas']).toHaveBeenCalledOnce();
    expect(component.listaTiposDocumento()).toEqual(tiposDocumento);
    expect(component.listaTiposListasCautela()).toEqual(tiposLista);
    expect(component.politicaEvidencias().extensionesPermitidas).toContain('.pdf');
  });
});
