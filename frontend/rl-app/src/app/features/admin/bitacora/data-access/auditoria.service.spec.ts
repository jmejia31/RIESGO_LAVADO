import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuditoriaService } from './auditoria.service';

describe('AuditoriaService', () => {
  let service: AuditoriaService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuditoriaService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('envía paginación y todos los filtros informados', () => {
    service.getBitacora({
      pagina: 3, limite: 25, buscar: 'ana', accion: 'VER', modulo: 'Bitacora',
      tabla: 'RL_AUDITORIA', fechaInicio: '2026-07-01', fechaFin: '2026-07-15'
    }).subscribe();

    const request = http.expectOne(req => req.url === 'http://localhost:5043/api/auditoria');
    expect(request.request.params.get('pagina')).toBe('3');
    expect(request.request.params.get('limite')).toBe('25');
    expect(request.request.params.get('buscar')).toBe('ana');
    expect(request.request.params.get('accion')).toBe('VER');
    expect(request.request.params.get('modulo')).toBe('Bitacora');
    expect(request.request.params.get('tabla')).toBe('RL_AUDITORIA');
    expect(request.request.params.get('fechaInicio')).toBe('2026-07-01');
    expect(request.request.params.get('fechaFin')).toBe('2026-07-15');
    request.flush({ datos: [], totalRegistros: 0 });
  });

  it('omite filtros opcionales vacíos', () => {
    service.getBitacora({ pagina: 1, limite: 10 }).subscribe();
    const request = http.expectOne(req => req.url === 'http://localhost:5043/api/auditoria');
    expect(request.request.params.keys()).toEqual(['pagina', 'limite']);
    request.flush({ datos: [], totalRegistros: 0 });
  });
});
