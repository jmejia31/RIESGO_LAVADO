import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { of } from 'rxjs';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import {
  construirExcelInstitucionalDesdeReporte,
  InstitutionalReportDefinition
} from '../../../../../core/reporting/institutional-report-parity.util';
import { ListasService } from '../../data-access/listas.service';
import { MonitoreoListasComponent } from './monitoreo-listas.component';

describe('MonitoreoListasComponent - estándar integral PDF/Excel', () => {
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
      registrarAuditoriaExportacion: vi.fn(() => of({ success: true })),
      registrarAuditoriaImpresion: vi.fn(() => of({ success: true }))
    };

    await TestBed.configureTestingModule({
      imports: [MonitoreoListasComponent],
      providers: [
        { provide: ListasService, useValue: service },
        {
          provide: ConfiguracionService,
          useValue: {
            configSistema: vi.fn(() => ({
              nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
              nombreSistema: 'SGRLA-IHSS'
            }))
          }
        },
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

  it('replica en Excel todas las secciones del PDF integral de patrono', () => {
    const interna = component as unknown as {
      construirReporteIntegralPatrono(
        row: unknown,
        positivo: unknown,
        seguimientos: unknown[],
        rango: { texto: string }
      ): InstitutionalReportDefinition;
    };

    const report = interna.construirReporteIntegralPatrono({
      numeroPatrono: '201200601751',
      rtn: '05019006500703',
      nombre: 'TECPROFIRE S DE R.L. DE C.V.',
      esProveedorIhss: 'No',
      listaCoincidencia: 'OFAC',
      fechaEncontro: '2026-05-21T12:00:00',
      fechaCalifico: '2026-06-14T12:00:00',
      tieneMotivo: false
    }, null, [], { texto: 'Todos los seguimientos registrados' });

    const excel = construirExcelInstitucionalDesdeReporte(report);
    const text = excel.data.flat().map(value => String(value ?? ''));

    expect(report.title).toBe('REPORTE INTEGRAL DE PATRONO');
    expect(text).toContain('1. INFORMACIÓN GENERAL DEL PATRONO');
    expect(text).toContain('201200601751');
    expect(text).toContain('05019006500703');
    expect(text).toContain('PENDIENTE DE REGISTRO');
    expect(text).toContain('2. MOTIVO DE INGRESO A LISTA DE MONITOREO');
    expect(text).toContain('3. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS');
    expect(text).toContain('Todos los seguimientos registrados');
    expect(text).toContain('No se registran acciones de seguimiento ni evidencias adicionales para este patrono.');
    expect(excel.worksheet['!keyValueRows']).toHaveLength(6);
    expect(excel.worksheet['!autoFilterRow']).toBe(0);
  });

  it('replica detalle e historial del PDF integral de persona natural', () => {
    const interna = component as unknown as {
      construirReporteIntegralNatural(
        row: unknown,
        detalles: unknown[],
        positivo: unknown,
        seguimientos: unknown[],
        rango: { texto: string }
      ): InstitutionalReportDefinition;
    };

    const report = interna.construirReporteIntegralNatural({
      numeroIdentificacion: '0801199900010',
      nombre: 'Persona Natural',
      listaCoincidencia: 'OFAC',
      totalRepetidos: 1,
      tieneMotivo: true
    }, [{
      tipoCondicionActuaDesc: 'Afiliado',
      numeroPatronal: 'P-001',
      nombreEmpresa: 'Empresa Uno',
      esPep: 'SI',
      listaCoincidencia: 'OFAC',
      fechaCoincidencia: '2026-07-01T12:00:00',
      fechaCalifico: '2026-07-02T12:00:00'
    }], {
      motivoIngreso: 'Coincidencia confirmada',
      origenRegistro: 'DNP_LISTAS',
      fechaRegistroInterno: '2026-07-03T12:00:00'
    }, [{
      fechaCreacion: '2026-07-04T12:00:00',
      usrEmail: 'cumplimiento@ihss.hn',
      motivoIngreso: 'Revisión mensual',
      evidencias: [{ nombreArchivo: 'soporte.pdf' }]
    }], { texto: 'Desde 01/07/2026' });

    const excel = construirExcelInstitucionalDesdeReporte(report);
    const text = excel.data.flat().map(value => String(value ?? ''));

    expect(text).toContain('3. DETALLE DE COINCIDENCIAS ENCONTRADAS');
    expect(text).toContain('Es PEP');
    expect(text).toContain('SÍ');
    expect(text).toContain('4. HISTORIAL DE SEGUIMIENTOS Y EVIDENCIAS');
    expect(text).toContain('Revisión mensual');
    expect(text).toContain('soporte.pdf');
    expect(excel.worksheet['!headerRows']).toHaveLength(2);
    expect(excel.worksheet['!autoFilterRow']).toBe(excel.worksheet['!headerRows']?.[0]);
  });

  it('replica detalle e historial del PDF integral de empleado', () => {
    const interna = component as unknown as {
      construirReporteIntegralEmpleado(
        row: unknown,
        detalles: unknown[],
        positivo: unknown,
        seguimientos: unknown[],
        rango: { texto: string }
      ): InstitutionalReportDefinition;
    };

    const report = interna.construirReporteIntegralEmpleado({
      identidad: '0801198800011',
      nombre: 'Empleado IHSS',
      listaCoincidencia: 'OFAC',
      totalRepetidos: 1,
      tieneMotivo: true
    }, [{
      tipoCondicionActuaDesc: 'Empleado',
      numeroPatrono: 'IHSS',
      nombreEmpresa: 'IHSS',
      razoSoci: 'Instituto Hondureño de Seguridad Social',
      listaCoincidencia: 'OFAC',
      fechaCoincidencia: '2026-07-01T12:00:00',
      fechaCalifico: '2026-07-02T12:00:00'
    }], {
      motivoIngreso: 'Coincidencia en revisión',
      origenRegistro: 'MANUAL_CUMPLIMIENTO'
    }, [], { texto: 'Todos los seguimientos registrados' });

    const excel = construirExcelInstitucionalDesdeReporte(report);
    const text = excel.data.flat().map(value => String(value ?? ''));

    expect(report.title).toBe('REPORTE INTEGRAL DE EMPLEADO IHSS');
    expect(text).toContain('Razón Social');
    expect(text).toContain('Instituto Hondureño de Seguridad Social');
    expect(text).toContain('No se registran acciones de seguimiento ni evidencias adicionales para este empleado.');
  });

  it('estandariza también la exportación detallada de coincidencias naturales y empleados', () => {
    const interna = component as unknown as {
      construirReporteDetallado(): InstitutionalReportDefinition | null;
    };

    component.tipoActivo.set('natural');
    component.personaSeleccionada.set({
      numeroIdentificacion: '0801',
      nombre: 'Persona Natural',
      listaCoincidencia: 'OFAC',
      totalRepetidos: 1
    });
    component.detallesNatural.set([{
      numeroIdentificacion: '0801',
      nombresPersona: 'Persona Natural',
      tipoCondicionActuaDesc: 'Afiliado',
      numeroPatronal: 'P-10',
      nombreEmpresa: 'Empresa Diez',
      esPep: 'NO',
      listaCoincidencia: 'OFAC'
    }]);

    const natural = interna.construirReporteDetallado();
    expect(natural?.title).toContain('PERSONA NATURAL');
    expect(natural?.sections.some(section => section.kind === 'table')).toBe(true);
    expect(natural?.sections.some(section => section.title === 'RESUMEN DEL REPORTE')).toBe(true);

    component.tipoActivo.set('empleado');
    component.personaSeleccionadaEmpleado.set({
      identidad: '0901',
      nombre: 'Empleado',
      listaCoincidencia: 'OFAC',
      totalRepetidos: 1
    });
    component.detallesEmpleado.set([{
      identidad: '0901',
      nombre: 'Empleado',
      tipoCondicionActuaDesc: 'Empleado',
      numeroPatrono: 'IHSS',
      nombreEmpresa: 'IHSS',
      razoSoci: 'IHSS',
      listaCoincidencia: 'OFAC'
    }]);

    const empleado = interna.construirReporteDetallado();
    expect(empleado?.title).toContain('EMPLEADO IHSS');
    const excel = construirExcelInstitucionalDesdeReporte(empleado!);
    expect(excel.data.flat()).toContain('Empresas Relacionadas:');
  });
});
