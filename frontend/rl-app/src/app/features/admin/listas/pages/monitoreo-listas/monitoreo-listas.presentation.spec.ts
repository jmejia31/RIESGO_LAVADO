import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DomSanitizer } from '@angular/platform-browser';
import { of } from 'rxjs';
import { ConfiguracionService } from '../../../../../core/configuration/configuracion.service';
import { ListasService } from '../../data-access/listas.service';
import { MonitoreoListasComponent } from './monitoreo-listas.component';

describe('MonitoreoListasComponent — deduplicación de detalle', () => {
  let fixture: ComponentFixture<MonitoreoListasComponent>;
  let component: MonitoreoListasComponent;

  beforeEach(async () => {
    const service = {
      getJuridicas: vi.fn(() => of([])),
      getNaturales: vi.fn(() => of([])),
      getEmpleados: vi.fn(() => of([])),
      getJuridicasPaginadas: vi.fn(() => of({ items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRegistros: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } })),
      getNaturalesPaginadas: vi.fn(() => of({ items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRegistros: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } })),
      getEmpleadosPaginadas: vi.fn(() => of({ items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRegistros: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 } })),
      getTiposDocumento: vi.fn(() => of([])),
      getTiposListasCautela: vi.fn(() => of([])),
      getPoliticaEvidencias: vi.fn(() => of({
        maximoMb: 10,
        maximoBytes: 10 * 1024 * 1024,
        extensionesPermitidas: [],
        tiposPermitidosTexto: 'Archivos permitidos'
      })),
      getSeguimientos: vi.fn(() => of([])),
      getDetalleNatural: vi.fn(() => of([]))
    };

    await TestBed.configureTestingModule({
      imports: [MonitoreoListasComponent],
      providers: [
        { provide: ListasService, useValue: service },
        { provide: ConfiguracionService, useValue: { configSistema: vi.fn(() => null) } },
        { provide: DomSanitizer, useValue: { bypassSecurityTrustResourceUrl: vi.fn(value => value) } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MonitoreoListasComponent);
    component = fixture.componentInstance;
  });

  it('mantiene nombre y documento con un único owner visual en Sujeto identificado', () => {
    const nombre = 'JUAN PRUEBA';
    const documento = '0801-1990-00001';

    component.tipoActivo.set('natural');
    component.personaSeleccionada.set({
      numeroIdentificacion: documento,
      nombre,
      listaCoincidencia: 'LISTA DEMO',
      totalRepetidos: 1
    });
    component.detallesNatural.set([{
      numeroIdentificacion: documento,
      nombresPersona: nombre,
      tipoCondicionActuaDesc: 'Persona natural',
      numeroPatronal: 'N/A',
      nombreEmpresa: 'Empresa demo',
      esPep: 'NO',
      listaCoincidencia: 'LISTA DEMO',
      fechaCalifico: '2026-09-08',
      fechaCoincidencia: '2026-09-07'
    }]);
    component.detalleCargando.set(false);
    component.modalDetalleAbierto.set(true);
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector('[role="dialog"]') as HTMLElement;
    const header = dialog.querySelector('.modal-header-institutional');
    const visibleText = dialog.textContent ?? '';
    const subjectCard = Array.from(dialog.querySelectorAll('article'))
      .find(article => article.textContent?.includes('Sujeto identificado'));

    expect(header?.textContent).not.toContain(nombre);
    expect(header?.textContent).not.toContain(documento);
    expect(visibleText.match(new RegExp(nombre, 'g')) ?? []).toHaveLength(1);
    expect(visibleText.match(new RegExp(documento, 'g')) ?? []).toHaveLength(1);
    expect(subjectCard?.textContent).toContain(nombre);
    expect(subjectCard?.textContent).toContain(documento);
  });

  it('renderiza cinco KPIs superiores sin bloque duplicado y cambia con el tipo activo', () => {
    fixture.detectChanges();
    component.juridicasTotales.set({
      totalRegistros: 10, pendientes: 4, conMotivo: 3, manuales: 2, cerradosPasivos: 1
    });
    component.naturalesTotales.set({
      totalRegistros: 20, pendientes: 8, conMotivo: 6, manuales: 4, cerradosPasivos: 2
    });
    component.empleadosTotales.set({
      totalRegistros: 30, pendientes: 12, conMotivo: 9, manuales: 6, cerradosPasivos: 3
    });
    component.cargando.set(false);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const cards = () => Array.from(root.querySelectorAll('[data-ui-monitoring-kpi]')) as HTMLElement[];

    expect(root.querySelector('[data-ui-monitoring-kpis]')).not.toBeNull();
    expect(cards()).toHaveLength(5);
    expect(cards().map(card => card.dataset['uiMonitoringKpi'])).toEqual([
      'active', 'pending', 'with-reason', 'manual', 'closed-passive'
    ]);
    expect(root.querySelector('[data-ui-monitoring-selected-view]')).toBeNull();
    expect(root.textContent).not.toContain('Vista seleccionada');
    expect(root.textContent).not.toContain('Total consolidado del módulo');
    expect(root.querySelector('[data-ui-monitoring-kpi="closed-passive"]')?.textContent).toContain('1');

    component.tipoActivo.set('natural');
    fixture.detectChanges();
    expect(root.querySelector('[data-ui-monitoring-kpi="active"]')?.textContent).toContain('20');
    expect(root.querySelector('[data-ui-monitoring-kpi="closed-passive"]')?.textContent).toContain('2');

    component.tipoActivo.set('empleado');
    fixture.detectChanges();
    expect(root.querySelector('[data-ui-monitoring-kpi="active"]')?.textContent).toContain('30');
    expect(root.querySelector('[data-ui-monitoring-kpi="closed-passive"]')?.textContent).toContain('3');
  });
});
