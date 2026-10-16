import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HTTP_ERROR_SCOPE } from '../../../../../core/http/http-error-scope';
import { TestBed } from '@angular/core/testing';
import { UsuariosComponent } from './usuarios.component';

describe('UsuariosComponent HTTP error scope', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [UsuariosComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('keeps list, save, domains and modules in their contextual scope', () => {
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
    component.guardar();

    const save = http.expectOne('http://localhost:5043/api/auth/usuarios');
    expect(save.request.method).toBe('POST');
    expect(save.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    expect(save.request.body).toEqual(component.form.value);
    save.flush({ success: false, mensaje: 'Validación local' }, { status: 400, statusText: 'Bad Request' });
    expect(component.errorGuardar()).toBe('Validación local');
  });
});
