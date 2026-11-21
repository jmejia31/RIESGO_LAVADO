import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { InstitutionalNotificationService } from '../../../../../shared/notification/institutional-notification.service';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';
import { TipoListasComponent } from './tipo-listas.component';

describe('TipoListasComponent presentación institucional', () => {
  let http: HttpTestingController;
  const notification = {
    notify: vi.fn(() => Promise.resolve()),
    closeLoading: vi.fn(),
    showLoading: vi.fn(() => Promise.resolve())
  };
  const confirmation = { confirm: vi.fn(() => Promise.resolve(true)) };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TipoListasComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: InstitutionalNotificationService, useValue: notification },
        { provide: InstitutionalConfirmationService, useValue: confirmation }
      ]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('renderiza filtros institucionales y una superficie de tabla con scroll propio', () => {
    const fixture = TestBed.createComponent(TipoListasComponent);
    fixture.detectChanges();

    const request = http.expectOne('http://localhost:5043/api/listas/tipos-listas-cautela');
    request.flush({ success: true, datos: [] });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const tableSurface = root.querySelector('[aria-label="Tabla de tipos de listas de cautela"]') as HTMLElement | null;

    expect(root.querySelector('[data-ui-filter-block="tipo-listas"]')).not.toBeNull();
    expect(root.querySelector('#tipo-listas-buscar')).not.toBeNull();
    expect(root.querySelector('[aria-label="Limpiar filtros de tipos de listas"]')).not.toBeNull();
    expect(tableSurface?.className).toContain('h-[55vh]');
    expect(tableSurface?.className).toContain('overflow-y-scroll');
    expect(tableSurface?.className).toContain('overflow-x-hidden');
  });

  it('filtra por los campos visibles y permite limpiar la búsqueda', () => {
    const fixture = TestBed.createComponent(TipoListasComponent);
    fixture.detectChanges();

    const request = http.expectOne('http://localhost:5043/api/listas/tipos-listas-cautela');
    request.flush({
      success: true,
      datos: [
        { tipoListaCautelaId: 1, descripcion: 'OFAC', tipoArchivo: 'csv', cantidadColumnas: 12 },
        { tipoListaCautelaId: 2, descripcion: 'ONU', tipoArchivo: 'xml', cantidadColumnas: 4 }
      ]
    });

    const component = fixture.componentInstance;
    component.cambiarBusqueda('OFAC');
    expect(component.listasOrdenadas()).toHaveLength(1);
    expect(component.listasOrdenadas()[0].descripcion).toBe('OFAC');

    component.limpiarFiltros();
    expect(component.listasOrdenadas()).toHaveLength(2);
  });
});
