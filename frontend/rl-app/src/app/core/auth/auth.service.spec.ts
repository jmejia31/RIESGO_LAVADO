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
  const now = new Date('2026-08-24T16:00:00.000Z');

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(now);
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

  it('mantiene la sesión cuando existe actividad antes de completar 30 minutos de inactividad', () => {
    iniciarSesionActiva();

    vi.advanceTimersByTime(29 * 60 * 1000);
    window.dispatchEvent(new Event('keydown'));
    vi.advanceTimersByTime(29 * 60 * 1000);

    expect(router.navigate).not.toHaveBeenCalledWith(['/login'], { queryParams: { razon: 'expirada' } });
    http.expectNone('http://localhost:5043/api/auth/logout');

    vi.advanceTimersByTime(60 * 1000);
    const logout = http.expectOne('http://localhost:5043/api/auth/logout');
    logout.flush({ success: true });
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { razon: 'expirada' } });
  });

  it('cierra la sesión exactamente por inactividad cuando transcurren 30 minutos sin acciones', () => {
    iniciarSesionActiva();

    vi.advanceTimersByTime(30 * 60 * 1000);

    const logout = http.expectOne('http://localhost:5043/api/auth/logout');
    expect(logout.request.body).toEqual({ refreshToken: 'refresh-token-activo' });
    logout.flush({ success: true });
    expect(service.getAccessToken()).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { razon: 'expirada' } });
  });

  it('renueva proactivamente el JWT mientras la sesión continúa activa', () => {
    const expiraEn10Min = new Date(now.getTime() + 10 * 60 * 1000).toISOString();
    iniciarSesionActiva(expiraEn10Min);

    vi.advanceTimersByTime(5 * 60 * 1000);

    const refresh = http.expectOne('http://localhost:5043/api/auth/refresh');
    expect(refresh.request.body).toEqual({ refreshToken: 'refresh-token-activo' });
    const renovada = crearLoginResponse('access-renovado', 'refresh-renovado');
    refresh.flush({ success: true, datos: renovada });

    expect(service.getAccessToken()).toBe(renovada.accessToken);
    expect(localStorage.getItem('refresh_token')).toBe('refresh-renovado');
  });

  it('una renovación automática no reinicia el reloj de inactividad del usuario', () => {
    const expiraEn10Min = new Date(now.getTime() + 10 * 60 * 1000).toISOString();
    iniciarSesionActiva(expiraEn10Min);

    vi.advanceTimersByTime(5 * 60 * 1000);
    const refresh = http.expectOne('http://localhost:5043/api/auth/refresh');
    refresh.flush({ success: true, datos: crearLoginResponse('access-renovado', 'refresh-renovado') });

    vi.advanceTimersByTime(25 * 60 * 1000);
    const logout = http.expectOne('http://localhost:5043/api/auth/logout');
    expect(logout.request.body).toEqual({ refreshToken: 'refresh-renovado' });
    logout.flush({ success: true });

    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { razon: 'expirada' } });
  });

  it('comparte una sola petición cuando varias operaciones solicitan refresh simultáneamente', () => {
    localStorage.setItem('refresh_token', 'refresh-anterior');
    const response = crearLoginResponse('access-token-2', 'refresh-token-2');
    const resultado1 = vi.fn();
    const resultado2 = vi.fn();

    service.refreshToken().subscribe(resultado1);
    service.refreshToken().subscribe(resultado2);

    const requests = http.match('http://localhost:5043/api/auth/refresh');
    expect(requests).toHaveLength(1);
    expect(requests[0].request.body).toEqual({ refreshToken: 'refresh-anterior' });
    requests[0].flush({ success: true, datos: response });

    expect(resultado1).toHaveBeenCalledOnce();
    expect(resultado2).toHaveBeenCalledOnce();
    expect(localStorage.getItem('refresh_token')).toBe('refresh-token-2');
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

  function iniciarSesionActiva(expiresAt = '2099-12-31T23:59:59Z') {
    const response = crearLoginResponse('access-activo', 'refresh-token-activo', expiresAt);
    service.login({ email: 'ana@ihss.hn', password: 'ClaveSegura123!' }).subscribe();
    const request = http.expectOne('http://localhost:5043/api/auth/login');
    request.flush({ success: true, datos: response });
  }

  function crearLoginResponse(
    accessLabel: string,
    refreshToken: string,
    expiresAt = '2099-12-31T23:59:59Z'
  ): LoginResponse {
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
      exp: Math.floor(Date.parse(expiresAt) / 1000),
      etiqueta: accessLabel
    };
    const encode = (value: object) => btoa(unescape(encodeURIComponent(JSON.stringify(value))))
      .replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
    const accessToken = `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.firma`;
    return {
      accessToken,
      refreshToken,
      expiresAt,
      usuario: {} as LoginResponse['usuario']
    };
  }
});
