import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { of } from 'rxjs';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import { ListasService } from '../../data-access/listas.service';
import { MonitoreoListasComponent } from './monitoreo-listas.component';

describe('MonitoreoListasComponent - paridad Excel/PDF', () => {
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
      getSeguimientos: vi.fn(() => of([])),
      registrarPositivo: vi.fn(() => of({ mensaje: 'Registrado' })),
      registrarSeguimiento: vi.fn(() => of({ mensaje: 'Seguimiento registrado' })),
      actualizarSeguimiento: vi.fn(() => of({ mensaje: 'Seguimiento actualizado' })),
      eliminarEvidencia: vi.fn(() => of({ mensaje: 'Evidencia eliminada' })),
      eliminarSeguimiento: vi.fn(() => of({ mensaje: 'Seguimiento eliminado' })),
      descargarEvidenciaBlob: vi.fn(() => of(new Blob())),
      getPositivoPorDocumento: vi.fn(() => of(null)),
      getDetalleNatural: vi.fn(() => of([])),
      getDetalleEmpleado: vi.fn(() => of([])),
      registrarAuditoriaExportacion: vi.fn(() => of({ success: true }))
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
    vi.restoreAllMocks();
    TestBed.resetTestingModule();
  });

  it('exporta filtros, resumen y las nueve columnas juridicas del mismo modelo utilizado por el PDF', async () => {
    const click = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    component.juridicasRaw.set([{
      numeroPatrono: 'P-150',
      rtn: '08019000123460',
      nombre: 'Empresa con Estado',
      listaCoincidencia: 'OFAC',
      esProveedorIhss: 'No',
      tieneMotivo: true,
      esManual: false,
      fechaEncontro: '2026-07-10T12:00:00',
      fechaCalifico: '2026-07-11T12:00:00',
      fechaRegistroInterno: '2026-07-12T12:00:00'
    }] as never);

    const instancia = component as unknown as {
      construirReporteListaPrincipalPdf(): { title: string; headers: string[]; rows: string[][] };
      construirDatosExcelListaPrincipal(reporte: {
        title: string;
        headers: string[];
        rows: string[][];
      }): string[][];
    };
    const reportePdf = instancia.construirReporteListaPrincipalPdf();
    const dataExcel = instancia.construirDatosExcelListaPrincipal(reportePdf);
    const constructorCompartido = vi.spyOn(instancia, 'construirReporteListaPrincipalPdf');

    component.exportarListaPrincipal();

    expect(constructorCompartido).toHaveBeenCalledOnce();
    expect(dataExcel[4][0]).toContain('Filtros aplicados: Tipo: Personas jurídicas');
    expect(dataExcel[6]).toEqual([
      'Registros filtrados', '1', 'Coincidencias visibles en la vista actual',
      'Pendientes', '0', 'Requieren motivo o revisión'
    ]);
    expect(dataExcel[7]).toEqual([
      'Con motivo', '1', 'Con sustento registrado',
      'Cerrados / pasivos', '0', 'Registros no activos'
    ]);
    expect(dataExcel[10]).toEqual([
      'Número Patronal',
      'RTN',
      'Nombre Empresa',
      'Lista Coincidencia',
      'Proveedor IHSS',
      'Estado',
      'Fecha Coincidencia',
      'Fecha Calificación',
      'Registro Interno'
    ]);
    expect(dataExcel[11]).toEqual([
      'P-150',
      '08019000123460',
      'Empresa con Estado',
      'OFAC',
      'No',
      'Con motivo',
      '10/07/2026',
      '11/07/2026',
      '12/07/2026'
    ]);
    expect(service['registrarAuditoriaExportacion']).toHaveBeenCalledWith(
      'RL_LISTA_POSITIVOS',
      'juridica',
      'ExportacionMonitoreoListas',
      expect.objectContaining({
        cantidadRegistros: 1,
        filtros: expect.stringContaining('Tipo: Personas jurídicas')
      })
    );
    await vi.waitFor(() => expect(click).toHaveBeenCalledOnce());
  });
});
