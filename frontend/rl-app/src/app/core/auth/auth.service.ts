import { Injectable, signal, computed, effect } from '@angular/core';
import { HttpClient }  from '@angular/common/http';
import { Router }      from '@angular/router';
import { tap, catchError, EMPTY, throwError } from 'rxjs';
import { LoginRequest, LoginResponse, UsuarioInfo } from './auth.models';
import { environment } from '../../../environments/environment';
import { jwtDecode }   from 'jwt-decode';
import { ConfiguracionService } from '../configuration/configuracion.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = `${environment.apiUrl}/auth`;

  // Proceso de inactividad: controla temporizador y oyentes para cerrar sesión por falta de uso.
  private inactivityTimer: any;
  private eventListeners: (() => void)[] = [];

  // Proceso de sesión local: conserva token y usuario decodificado para guards, menú y pantallas.
  private _usuario   = signal<UsuarioInfo | null>(this.obtenerUsuarioDeToken(localStorage.getItem('access_token')));
  private _token     = signal<string | null>(localStorage.getItem('access_token'));

  readonly usuario    = this._usuario.asReadonly();
  readonly estaLogueado = computed(() => !!this._token());
  readonly rol          = computed(() => this._usuario()?.rol ?? '');

  constructor(
    private http: HttpClient,
    private router: Router,
    private configService: ConfiguracionService
  ) {
    // Escuchar el estado de autenticación para iniciar o detener el monitoreo de inactividad
    effect(() => {
      if (this.estaLogueado()) {
        this.iniciarMonitoreoInactividad();
      } else {
        this.detenerMonitoreoInactividad();
      }
    });
  }

  login(dto: LoginRequest) {
    // Autenticación frontend: envía credenciales y guarda tokens solo cuando backend confirma éxito.
    return this.http.post<{ success: boolean; datos: LoginResponse }>(
      `${this.API}/login`, dto
    ).pipe(
      tap(res => {
        if (res.success) {
          this.guardarSesion(res.datos);
        }
      })
    );
  }

  recuperarPassword(email: string) {
    return this.http.post<{ success: boolean; mensaje: string }>(
      `${this.API}/recuperar-password`, { email }
    );
  }

  cambiarPassword(passwordActual: string, nuevoPassword: string) {
    return this.http.put<{ success: boolean; mensaje: string }>(
      `${this.API}/password`, { passwordActual, nuevoPassword }
    );
  }

  logout() {
    const refreshToken = localStorage.getItem('refresh_token') ?? '';
    this.http.post(`${this.API}/logout`, { refreshToken }).pipe(
      catchError(() => EMPTY)
    ).subscribe();
    this.limpiarSesion();
    this.router.navigate(['/login']);
  }

  refreshToken() {
    // Renovación de sesión: usa refresh token y actualiza el estado local; si falla, limpia la sesión.
    const refreshToken = localStorage.getItem('refresh_token');
    if (!refreshToken) {
      this.cerrarSesionLocal();
      this.router.navigate(['/login'], { queryParams: { razon: 'expirada' } });
      return throwError(() => new Error('Refresh token no disponible.'));
    }

    return this.http.post<{ success: boolean; datos: LoginResponse }>(
      `${this.API}/refresh`, { refreshToken }
    ).pipe(
      tap(res => {
        if (res.success) this.guardarSesion(res.datos);
      }),
      catchError(() => {
        this.limpiarSesion();
        this.router.navigate(['/login']);
        return throwError(() => new Error('Refresh token inválido o expirado.'));
      })
    );
  }

  getAccessToken(): string | null {
    return this._token();
  }

  tieneRol(roles: string[]): boolean {
    const rolActual = this.rol();
    return roles.some(r => r.toUpperCase() === rolActual.toUpperCase());
  }

  requiereCambioPassword(): boolean {
    return this._usuario()?.debeCambiarPassword === true;
  }

  cerrarSesionLocal() {
    this.limpiarSesion();
  }

  private guardarSesion(datos: LoginResponse) {
    localStorage.setItem('access_token',  datos.accessToken);
    localStorage.setItem('refresh_token', datos.refreshToken);
    localStorage.setItem('token_expira',  datos.expiresAt);
    this._token.set(datos.accessToken);
    this._usuario.set(this.obtenerUsuarioDeToken(datos.accessToken));
  }

  private limpiarSesion() {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('token_expira');
    this._token.set(null);
    this._usuario.set(null);
  }

  private obtenerUsuarioDeToken(token: string | null): UsuarioInfo | null {
    if (!token) return null;
    try {
      const p: any = jwtDecode(token);
      
      const get = (key: string, alias?: string) => p[key] || (alias ? p[alias] : undefined);

      const id = parseInt(get('nameid', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier') ?? '0');
      if (!id) return null;

      return {
        id:      id,
        uid:     get('uid') ?? '',
        email:   get('email', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress') ?? '',
        nombre:  get('given_name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname') ?? '',
        apellido:get('family_name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname') ?? '',
        rol:     get('role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role') ?? '',
        rolId:   parseInt(get('rol_id') ?? '0'),
        dominio: get('dominio') ?? '',
        dominioId: get('dom_id') ? parseInt(get('dom_id')) : undefined,
        usuarioDominio: get('usr_dom') ?? '',
        esUsuarioDominio: parseInt(get('es_dom') ?? '0'),
        dni:     get('dni') || get('DNI') || get('Dni') || '',
        modulosIds: get('modulos') 
          ? get('modulos').split(',').map((i: string) => parseInt(i)).filter((i: number) => !isNaN(i)) 
          : [],
        debeCambiarPassword: get('debe_cambiar_pass') === '1'
      };
    } catch (e) {
      console.error('Error decodificando token:', e);
      return null;
    }
  }

  private iniciarMonitoreoInactividad() {
    this.detenerMonitoreoInactividad();

    const reiniciarTimer = () => {
      if (this.inactivityTimer) {
        clearTimeout(this.inactivityTimer);
      }
      
      // Obtener el timeout configurado en la base de datos (por defecto 30 minutos).
      const timeoutMinutos = this.configService.configSistema()?.timeoutSesion || 30;
      const timeoutMs = timeoutMinutos * 60 * 1000;

      this.inactivityTimer = setTimeout(() => {
        this.logoutPorInactividad();
      }, timeoutMs);
    };

    // Eventos que indican que el usuario está interactuando con la interfaz.
    const eventos = ['mousemove', 'mousedown', 'keypress', 'scroll', 'touchstart', 'click'];
    
    // Registrar oyentes de eventos y guardar su función de limpieza.
    eventos.forEach(evt => {
      const handler = () => reiniciarTimer();
      window.addEventListener(evt, handler, { passive: true });
      this.eventListeners.push(() => window.removeEventListener(evt, handler));
    });

    // Iniciar primer temporizador.
    reiniciarTimer();
  }

  private detenerMonitoreoInactividad() {
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
      this.inactivityTimer = null;
    }
    // Desvincular todos los oyentes de eventos registrados.
    this.eventListeners.forEach(removeFn => removeFn());
    this.eventListeners = [];
  }

  private logoutPorInactividad() {
    this.detenerMonitoreoInactividad();
    
    const refreshToken = localStorage.getItem('refresh_token') ?? '';
    this.http.post(`${this.API}/logout`, { refreshToken }).pipe(
      catchError(() => EMPTY)
    ).subscribe();
    
    this.limpiarSesion();
    this.router.navigate(['/login'], { queryParams: { razon: 'expirada' } });
  }
}
