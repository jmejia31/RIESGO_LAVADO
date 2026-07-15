import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ConfiguracionService } from '../configuration/configuracion.service';
import { AuthService } from './auth.service';
import { LoginResponse } from './auth.models';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    vi.useFakeTimers();
    localStorage.clear();
    router = { navigate: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
        { provide: ConfiguracionService, useValue: { configSistema: vi.fn(() => ({ timeoutSesion: 30 })) } }
      ]
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    service.cerrarSesionLocal();
    http.verify();
    localStorage.clear();
    vi.clearAllTimers();
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  it('inicia sesión, almacena tokens y decodifica claims funcionales', () => {
    const response = crearLoginResponse('access-token-1', 'refresh-token-1');

    service.login({ email: 'ana@ihss.hn', password: 'ClaveSegura123!' }).subscribe();
    const request = http.expectOne('http://localhost:5043/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'ana@ihss.hn', password: 'ClaveSegura123!' });
    request.flush({ success: true, datos: response });

    expect(localStorage.getItem('access_token')).toBe(response.accessToken);
    expect(localStorage.getItem('refresh_token')).toBe('refresh-token-1');
    expect(service.usuario()?.id).toBe(27);
    expect(service.usuario()?.modulosIds).toEqual([2, 5, 9]);
    expect(service.requiereCambioPassword()).toBe(true);
    expect(service.tieneRol(['administrador'])).toBe(true);
  });

  it('rechaza la renovación cuando no existe refresh token', () => {
    const error = vi.fn();
    service.refreshToken().subscribe({ error });

    expect(error).toHaveBeenCalledOnce();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { razon: 'expirada' } });
    http.expectNone('http://localhost:5043/api/auth/refresh');
  });

  it('renueva y reemplaza la sesión local', () => {
    localStorage.setItem('refresh_token', 'refresh-anterior');
    const response = crearLoginResponse('access-token-2', 'refresh-token-2');

    service.refreshToken().subscribe();
    const request = http.expectOne('http://localhost:5043/api/auth/refresh');
    expect(request.request.body).toEqual({ refreshToken: 'refresh-anterior' });
    request.flush({ success: true, datos: response });

    expect(service.getAccessToken()).toBe(response.accessToken);
    expect(localStorage.getItem('refresh_token')).toBe('refresh-token-2');
  });

  it('envía los contratos de recuperación y cambio de contraseña', () => {
    service.recuperarPassword('ana@ihss.hn').subscribe();
    const recuperar = http.expectOne('http://localhost:5043/api/auth/recuperar-password');
    expect(recuperar.request.method).toBe('POST');
    expect(recuperar.request.body).toEqual({ email: 'ana@ihss.hn' });
    recuperar.flush({ success: true, mensaje: 'Enviado' });

    service.cambiarPassword('Actual123!', 'Nueva123!').subscribe();
    const cambiar = http.expectOne('http://localhost:5043/api/auth/password');
    expect(cambiar.request.method).toBe('PUT');
    expect(cambiar.request.body).toEqual({ passwordActual: 'Actual123!', nuevoPassword: 'Nueva123!' });
    cambiar.flush({ success: true, mensaje: 'Actualizada' });
  });

  function crearLoginResponse(accessLabel: string, refreshToken: string): LoginResponse {
    const payload = {
      nameid: '27',
      uid: 'uid-seguro',
      email: 'ana@ihss.hn',
      given_name: 'Ana',
      family_name: 'Pérez',
      role: 'ADMINISTRADOR',
      rol_id: '1',
      es_dom: '0',
      modulos: '2,5,invalido,9',
      debe_cambiar_pass: '1',
      exp: 4_102_444_800,
      etiqueta: accessLabel
    };
    const encode = (value: object) => btoa(unescape(encodeURIComponent(JSON.stringify(value))))
      .replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
    const accessToken = `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.firma`;
    return {
      accessToken,
      refreshToken,
      expiresAt: '2099-12-31T23:59:59Z',
      usuario: {} as LoginResponse['usuario']
    };
  }
});
