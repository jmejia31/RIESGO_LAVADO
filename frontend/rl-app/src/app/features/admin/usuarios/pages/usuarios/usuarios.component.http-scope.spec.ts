import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HTTP_ERROR_SCOPE } from '../../../../../core/http/http-error-scope';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { UsuariosComponent } from './usuarios.component';
import { InstitutionalConfirmationService } from '../../../../../shared/confirmation/institutional-confirmation.service';

describe('UsuariosComponent HTTP error scope', () => {
  let http: HttpTestingController;
  const confirmation = { confirm: vi.fn(() => Promise.resolve(true)) };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [UsuariosComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: InstitutionalConfirmationService, useValue: confirmation }
      ]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('keeps list, save, domains and modules in their contextual scope', async () => {
    const fixture = TestBed.createComponent(UsuariosComponent);
    fixture.detectChanges();

    const list = http.expectOne(request => request.url === 'http://localhost:5043/api/auth/usuarios/paginado');
    expect(list.request.params.get('pagina')).toBe('1');
    expect(list.request.params.get('tamanoPagina')).toBe('25');
    expect(list.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    list.flush({ success: true, datos: { items: [], pagina: 1, tamanoPagina: 25, totalRegistros: 0, totalPaginas: 0 } });

    const roles = http.expectOne('http://localhost:5043/api/catalogos/roles');
    expect(roles.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    roles.flush({ success: true, datos: [] });

    const domains = http.expectOne('http://localhost:5043/api/catalogos/dominios');
    expect(domains.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    domains.flush({ success: true, datos: [] });

    const modules = http.expectOne('http://localhost:5043/api/catalogos/modulos');
    expect(modules.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    modules.flush({ success: true, datos: [] });

    const component = fixture.componentInstance;
    component.prepararNuevo();
    component.form.patchValue({ nombre: 'Ana', apellido: 'Pérez', email: 'ana@example.test', rolId: 1, modulosIds: [10] });
    await component.guardar();

    const save = http.expectOne('http://localhost:5043/api/auth/usuarios');
    expect(save.request.method).toBe('POST');
    expect(save.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    expect(save.request.body).toEqual(component.form.value);
    save.flush({ success: false, mensaje: 'Validación local' }, { status: 400, statusText: 'Bad Request' });
    expect(component.errorGuardar()).toBe('Validación local');
  });

  it('renders the institutional user filter and bounded table scroll surface', () => {
    const fixture = TestBed.createComponent(UsuariosComponent);
    fixture.detectChanges();

    http.expectOne(request => request.url === 'http://localhost:5043/api/auth/usuarios/paginado').flush({ success: true, datos: { items: [], pagina: 1, tamanoPagina: 25, totalRegistros: 0, totalPaginas: 0 } });
    http.expectOne('http://localhost:5043/api/catalogos/roles').flush({ success: true, datos: [] });
    http.expectOne('http://localhost:5043/api/catalogos/dominios').flush({ success: true, datos: [] });
    http.expectOne('http://localhost:5043/api/catalogos/modulos').flush({ success: true, datos: [] });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const filter = root.querySelector('[data-ui-filter-block="usuarios"]') as HTMLElement | null;
    const tableSurface = root.querySelector('[aria-label="Tabla de usuarios"]') as HTMLElement | null;

    expect(filter).not.toBeNull();
    expect(root.querySelector('#usuarios-buscar')).not.toBeNull();
    expect(root.querySelector('[aria-label="Limpiar filtros de usuarios"]')).not.toBeNull();
    expect(tableSurface?.className).toContain('h-[55vh]');
    expect(tableSurface?.className).toContain('overflow-y-scroll');
    expect(tableSurface?.className).toContain('overflow-x-hidden');
  });

  it('sends the institutional search filter and clears it from the first page', () => {
    const fixture = TestBed.createComponent(UsuariosComponent);
    fixture.detectChanges();

    http.expectOne(request => request.url === 'http://localhost:5043/api/auth/usuarios/paginado').flush({ success: true, datos: { items: [], pagina: 1, tamanoPagina: 25, totalRegistros: 0, totalPaginas: 0 } });
    http.expectOne('http://localhost:5043/api/catalogos/roles').flush({ success: true, datos: [] });
    http.expectOne('http://localhost:5043/api/catalogos/dominios').flush({ success: true, datos: [] });
    http.expectOne('http://localhost:5043/api/catalogos/modulos').flush({ success: true, datos: [] });

    const component = fixture.componentInstance;
    component.cambiarBusqueda('Ana');
    const filtered = http.expectOne(request => request.url === 'http://localhost:5043/api/auth/usuarios/paginado');
    expect(filtered.request.params.get('buscar')).toBe('Ana');
    expect(filtered.request.params.get('pagina')).toBe('1');
    filtered.flush({ success: true, datos: { items: [], pagina: 1, tamanoPagina: 25, totalRegistros: 0, totalPaginas: 0 } });

    component.limpiarFiltros();
    const cleared = http.expectOne(request => request.url === 'http://localhost:5043/api/auth/usuarios/paginado');
    expect(cleared.request.params.get('buscar')).toBe('');
    expect(cleared.request.params.get('pagina')).toBe('1');
    cleared.flush({ success: true, datos: { items: [], pagina: 1, tamanoPagina: 25, totalRegistros: 0, totalPaginas: 0 } });
  });
});
