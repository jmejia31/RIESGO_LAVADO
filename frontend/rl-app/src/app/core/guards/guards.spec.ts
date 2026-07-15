import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { authGuard } from './auth.guard';
import { roleGuard } from './role.guard';
import { moduloGuard } from './modulo.guard';

describe('security guards', () => {
  const route = {} as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;
  let auth: {
    estaLogueado: ReturnType<typeof vi.fn>;
    requiereCambioPassword: ReturnType<typeof vi.fn>;
    cerrarSesionLocal: ReturnType<typeof vi.fn>;
    tieneRol: ReturnType<typeof vi.fn>;
    usuario: ReturnType<typeof vi.fn>;
  };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    auth = {
      estaLogueado: vi.fn(() => true),
      requiereCambioPassword: vi.fn(() => false),
      cerrarSesionLocal: vi.fn(),
      tieneRol: vi.fn(() => true),
      usuario: vi.fn(() => ({ modulosIds: [2, 5, 9] }))
    };
    router = { navigate: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router }
      ]
    });
  });

  afterEach(() => TestBed.resetTestingModule());

  it('authGuard permite una sesión válida', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(result).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('authGuard envía al login cuando no hay sesión', () => {
    auth.estaLogueado.mockReturnValue(false);
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('authGuard cierra una sesión que exige cambio de contraseña', () => {
    auth.requiereCambioPassword.mockReturnValue(true);
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));
    expect(result).toBe(false);
    expect(auth.cerrarSesionLocal).toHaveBeenCalledOnce();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], { queryParams: { razon: 'cambio-password' } });
  });

  it('roleGuard exige un usuario local', () => {
    auth.usuario.mockReturnValue(null);
    const result = TestBed.runInInjectionContext(() => roleGuard(['ADMINISTRADOR'])(route, state));
    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('roleGuard permite un rol autorizado', () => {
    const result = TestBed.runInInjectionContext(() => roleGuard(['ADMINISTRADOR'])(route, state));
    expect(result).toBe(true);
    expect(auth.tieneRol).toHaveBeenCalledWith(['ADMINISTRADOR']);
  });

  it('roleGuard dirige a sin acceso cuando el rol no coincide', () => {
    auth.tieneRol.mockReturnValue(false);
    const result = TestBed.runInInjectionContext(() => roleGuard(['ADMINISTRADOR'])(route, state));
    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/sin-acceso']);
  });

  it('moduloGuard permite un módulo asignado', () => {
    const result = TestBed.runInInjectionContext(() => moduloGuard(5)(route, state));
    expect(result).toBe(true);
  });

  it('moduloGuard rechaza un módulo no asignado', () => {
    const result = TestBed.runInInjectionContext(() => moduloGuard(7)(route, state));
    expect(result).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/sin-acceso']);
  });
});
