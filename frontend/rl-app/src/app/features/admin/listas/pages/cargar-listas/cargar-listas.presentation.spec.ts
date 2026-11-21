import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { AuthService } from '../../../../../core/auth/auth.service';
import { InstitutionalNotificationService } from '../../../../../shared/notification/institutional-notification.service';
import { ReportPreviewService } from '../../../../../shared/report-preview/report-preview.service';
import { CargarListasComponent } from './cargar-listas.component';

describe('CargarListasComponent presentación institucional', () => {
  let http: HttpTestingController;
  const notification = {
    notify: vi.fn(() => Promise.resolve()),
    closeLoading: vi.fn(),
    showLoading: vi.fn(() => Promise.resolve())
  };
  const reportPreview = {
    openExcel: vi.fn(() => Promise.resolve())
  };
  const auth = { usuario: vi.fn(() => null) };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CargarListasComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: InstitutionalNotificationService, useValue: notification },
        { provide: ReportPreviewService, useValue: reportPreview },
        { provide: AuthService, useValue: auth }
      ]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  function createComponent() {
    const fixture = TestBed.createComponent(CargarListasComponent);
    fixture.detectChanges();
    http.expectOne('http://localhost:5043/api/listas/tipos-listas-cautela').flush({ success: true, datos: [] });
    http.expectOne('http://localhost:5043/api/listas/resumen').flush({ success: true, datos: [] });
    fixture.detectChanges();
    return fixture;
  }

  it('renderiza el filtro institucional y una superficie de tabla con scroll propio', () => {
    const fixture = createComponent();
    const root = fixture.nativeElement as HTMLElement;
    const tableSurface = root.querySelector('[aria-label="Tabla de cargas de listas de cautela"]') as HTMLElement | null;

    expect(root.querySelector('[data-ui-filter-block="cargar-listas"]')).not.toBeNull();
    expect(root.querySelector('#cargar-listas-buscar')).not.toBeNull();
    expect(root.querySelector('[aria-label="Limpiar filtros de cargas de listas"]')).not.toBeNull();
    expect(tableSurface?.className).toContain('h-[55vh]');
    expect(tableSurface?.className).toContain('overflow-y-scroll');
    expect(tableSurface?.className).toContain('overflow-x-hidden');
  });

  it('filtra por los campos visibles y permite limpiar la búsqueda', () => {
    const fixture = TestBed.createComponent(CargarListasComponent);
    fixture.detectChanges();
    http.expectOne('http://localhost:5043/api/listas/tipos-listas-cautela').flush({ success: true, datos: [] });
    http.expectOne('http://localhost:5043/api/listas/resumen').flush({
      success: true,
      datos: [
        { tipoListaCautelaId: 1, lista: 'OFAC', usuario: 'Christian Barahona', fechaCreacion: '2026-06-16T00:00:00', cantidadRegistros: 18947 },
        { tipoListaCautelaId: 2, lista: 'ONU', usuario: 'Edgar Ernesto Barahona Flores', fechaCreacion: '2026-06-16T00:00:00', cantidadRegistros: 1009 }
      ]
    });

    const component = fixture.componentInstance;
    component.cambiarBusqueda('Christian');
    expect(component.resumenOrdenado()).toHaveLength(1);
    expect(component.resumenOrdenado()[0].lista).toBe('OFAC');

    component.limpiarFiltros();
    expect(component.resumenOrdenado()).toHaveLength(2);
  });
});
