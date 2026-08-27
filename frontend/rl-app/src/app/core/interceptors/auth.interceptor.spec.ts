import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let testing: HttpTestingController;
  let auth: {
    getAccessToken: ReturnType<typeof vi.fn>;
    refreshToken: ReturnType<typeof vi.fn>;
  };
  let router: { navigate: ReturnType<typeof vi.fn>; url: string };

  beforeEach(() => {
    auth = {
      getAccessToken: vi.fn(() => 'token-vigente'),
      refreshToken: vi.fn(() => of({}))
    };
    router = { navigate: vi.fn(), url: '/usuarios?pagina=1' };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router }
      ]
    });
    http = TestBed.inject(HttpClient);
    testing = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    testing.verify();
    TestBed.resetTestingModule();
  });

  it('adjunta el bearer token cuando existe sesión', () => {
    http.get('/api/recurso').subscribe();
    const request = testing.expectOne('/api/recurso');
    expect(request.request.headers.get('Authorization')).toBe('Bearer token-vigente');
    request.flush({ success: true });
  });

  it('no agrega Authorization cuando no existe token', () => {
    auth.getAccessToken.mockReturnValue(null);
    http.get('/api/publico').subscribe();
    const request = testing.expectOne('/api/publico');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ success: true });
  });

  it('renueva el token y reintenta una solicitud que recibe 401', () => {
    auth.getAccessToken.mockReturnValueOnce('token-vencido').mockReturnValue('token-renovado');
    http.get('/api/protegido').subscribe();

    const original = testing.expectOne('/api/protegido');
    expect(original.request.headers.get('Authorization')).toBe('Bearer token-vencido');
    original.flush({ mensaje: 'Expirado' }, { status: 401, statusText: 'Unauthorized' });

    const retry = testing.expectOne('/api/protegido');
    expect(auth.refreshToken).toHaveBeenCalledOnce();
    expect(retry.request.headers.get('Authorization')).toBe('Bearer token-renovado');
    retry.flush({ success: true });
  });

  it('no fuerza logout ni redirección cuando el refresh falla transitoriamente', () => {
    auth.refreshToken.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503, statusText: 'Service Unavailable' }))
    );
    const error = vi.fn();

    http.get('/api/protegido').subscribe({ error });
    const original = testing.expectOne('/api/protegido');
    original.flush({ mensaje: 'Expirado' }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.refreshToken).toHaveBeenCalledOnce();
    expect(error).toHaveBeenCalledOnce();
    expect(router.navigate).not.toHaveBeenCalledWith(['/login'], { queryParams: { razon: 'expirada' } });
  });

  it('mantiene la sesión y redirige a sin acceso ante 403', () => {
    const error = vi.fn();
    http.get('/api/restringido').subscribe({ error });
    const request = testing.expectOne('/api/restringido');
    request.flush({ mensaje: 'Módulo no autorizado' }, { status: 403, statusText: 'Forbidden' });

    expect(router.navigate).toHaveBeenCalledWith(['/sin-acceso'], {
      queryParams: { mensaje: 'Módulo no autorizado' },
      replaceUrl: true
    });
    expect(error).toHaveBeenCalledOnce();
  });

  it('mantiene la pantalla cuando la auditoría opcional responde 403', () => {
    const error = vi.fn();
    http.get('/api/auditoria?tabla=RL_MR_FAMILIAS_FORMULARIO').subscribe({ error });
    const request = testing.expectOne('/api/auditoria?tabla=RL_MR_FAMILIAS_FORMULARIO');
    request.flush({ mensaje: 'Auditoría no autorizada' }, { status: 403, statusText: 'Forbidden' });

    expect(router.navigate).not.toHaveBeenCalled();
    expect(error).toHaveBeenCalledOnce();
  });
});
