import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HTTP_ERROR_SCOPE } from '../http/http-error-scope';
import { CatalogoService } from './catalogo.service';

describe('CatalogoService matrices', () => {
  let service: CatalogoService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(CatalogoService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('loads versioned matrix catalogs with inactive option', () => {
    let result: unknown;
    service.matrices(true).subscribe(value => result = value);
    const request = http.expectOne(request => request.url.endsWith('/catalogos/matrices'));
    expect(request.request.params.get('incluirInactivos')).toBe('true');
    request.flush([{ id: 1, codigo: 'CAT_TEST', nombre: 'Test', activo: true, elementos: [] }]);
    expect(result).toEqual([{ id: 1, codigo: 'CAT_TEST', nombre: 'Test', activo: true, elementos: [] }]);
  });

  it('creates a catalog element through the backend contract', () => {
    service.crearElemento(7, 'A', 'Activo', 1).subscribe();
    const request = http.expectOne('http://localhost:5043/api/catalogos/matrices/7/elementos');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ codigo: 'A', valor: 'Activo', orden: 1 });
    request.flush(12);
  });

  it('preserves global module lookup by default and accepts a local consumer scope', () => {
    service.modulos().subscribe();
    const globalRequest = http.expectOne('http://localhost:5043/api/catalogos/modulos');
    expect(globalRequest.request.context.get(HTTP_ERROR_SCOPE)).toBe('global');
    globalRequest.flush({ success: true, datos: [] });

    service.modulos('local').subscribe();
    const localRequest = http.expectOne('http://localhost:5043/api/catalogos/modulos');
    expect(localRequest.request.context.get(HTTP_ERROR_SCOPE)).toBe('local');
    localRequest.flush({ success: true, datos: [] });
  });
});
