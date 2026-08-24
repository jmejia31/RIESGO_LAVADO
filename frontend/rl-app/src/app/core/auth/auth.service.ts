import { Injectable, signal, computed, effect } from '@angular/core';
import { HttpClient }  from '@angular/common/http';
import { Router }      from '@angular/router';
import { Observable, tap, catchError, EMPTY, throwError, finalize, shareReplay } from 'rxjs';
import { LoginRequest, LoginResponse, UsuarioInfo } from './auth.models';
import { environment } from '../../../environments/environment';
import { jwtDecode }   from 'jwt-decode';
import { ConfiguracionService } from '../configuration/configuracion.service';

type RefreshResponse = { success: boolean; datos: LoginResponse };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = `${environment.apiUrl}/auth`;
  private readonly DEFAULT_INACTIVITY_MINUTES = 30;
  private readonly REFRESH_MARGIN_MS = 5 * 60 * 1000;
  private readonly MAX_REFRESH_TIMER_MS = 60 * 60 * 1000;

  // Inactividad y renovación son relojes independientes: renovar un JWT nunca cuenta como actividad humana.
  private inactivityTimer: ReturnType<typeof setTimeout> | null = null;
  private tokenRefreshTimer: ReturnType<typeof setTimeout> | null = null;
  private eventListeners: (() => void)[] = [];
  private monitoringActive = false;
  private lastActivityAt = 0;
  private refreshRequest$: Observable<RefreshResponse> | null = null;

  // Proceso de sesión local: conserva token y usuario decodificado para guards, menú y pantallas.
  private _usuario   = signal<UsuarioInfo | null>(this.obtenerUsuarioDeToken(localStorage.getItem('access_token')));
  private _token     = signal<string | null>(localStorage.getItem('access_token'));

  readonly usuario      = this._usuario.asReadonly();
  readonly estaLogueado = computed(() => !!this._token());
  readonly rol           = computed(() => this._usuario()?.rol ?? '');

  constructor(
    private http: HttpClient,
    private router: Router,
    private configService: ConfiguracionService
  ) {
    // El cambio del JWT por refresh no reinicia el reloj de inactividad.
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

  refreshToken(): Observable<RefreshResponse> {
    // Single-flight: varios 401 o verificaciones simultáneas comparten una sola rotación de refresh token.
    if (this.refreshRequest$) {
      return this.refreshRequest$;
    }

    const refreshToken = localStorage.getItem('refresh_token');
    if (!refreshToken) {
      this.cerrarSesionLocal();
      this.router.navigate(['/login'], { queryParams: { razon: 'expirada' } });
      return throwError(() => new Error('Refresh token no disponible.'));
    }

    const request$ = this.http.post<RefreshResponse>(
      `${this.API}/refresh`, { refreshToken }
    ).pipe(
      tap(res => {
        // Evita que una respuesta tardía restaure una sesión que el usuario ya cerró o reemplazó.
        if (res.success && localStorage.getItem('refresh_token') === refreshToken) {
          this.guardarSesion(res.datos);
        }
      }),
      catchError(() => {
        if (localStorage.getItem('refresh_token') === refreshToken) {
          this.limpiarSesion();
          this.router.navigate(['/login'], { queryParams: { razon: 'expirada' } });
        }
        return throwError(() => new Error('Refresh token inválido o expirado.'));
      }),
      finalize(() => {
        this.refreshRequest$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    this.refreshRequest$ = request$;
    return request$;
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

    // El login inicia el control inmediatamente; un refresh solo recalcula su próximo vencimiento.
    if (this.monitoringActive) {
      this.programarRenovacionProactiva();
    } else {
      this.iniciarMonitoreoInactividad();
    }
  }

  private limpiarSesion() {
    this.detenerMonitoreoInactividad();
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
    // Un refresh cambia _token y vuelve a ejecutar el effect; no debe reiniciar la inactividad.
    if (this.monitoringActive) {
      this.programarRenovacionProactiva();
      return;
    }

    this.monitoringActive = true;
    this.lastActivityAt = Date.now();

    const eventos = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'];
    eventos.forEach(evt => {
      const handler = () => this.registrarActividadUsuario();
      window.addEventListener(evt, handler, { passive: true });
      this.eventListeners.push(() => window.removeEventListener(evt, handler));
    });

    this.programarCierrePorInactividad();
    this.programarRenovacionProactiva();
  }

  private registrarActividadUsuario() {
    if (!this.monitoringActive || !this.estaLogueado()) return;

    this.lastActivityAt = Date.now();
    this.programarCierrePorInactividad();

    // Si el navegador estuvo suspendido y el JWT quedó próximo a vencer, la primera actividad lo renueva.
    this.verificarRenovacionProactiva();
  }

  private programarCierrePorInactividad() {
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
    }

    const timeoutMs = this.obtenerTimeoutInactividadMs();
    const transcurrido = Math.max(0, Date.now() - this.lastActivityAt);
    const restante = Math.max(0, timeoutMs - transcurrido);

    this.inactivityTimer = setTimeout(() => {
      const inactividadActual = Date.now() - this.lastActivityAt;
      if (inactividadActual >= this.obtenerTimeoutInactividadMs()) {
        this.logoutPorInactividad();
        return;
      }
      this.programarCierrePorInactividad();
    }, restante);
  }

  private obtenerTimeoutInactividadMs(): number {
    const configurado = Number(this.configService.configSistema()?.timeoutSesion);
    const minutos = Number.isFinite(configurado) && configurado > 0
      ? configurado
      : this.DEFAULT_INACTIVITY_MINUTES;
    return minutos * 60 * 1000;
  }

  private programarRenovacionProactiva() {
    if (this.tokenRefreshTimer) {
      clearTimeout(this.tokenRefreshTimer);
      this.tokenRefreshTimer = null;
    }

    if (!this.monitoringActive || !this.estaLogueado()) return;

    const expiraAt = this.obtenerExpiracionAccessTokenMs();
    if (!expiraAt) return;

    const hastaVentanaRenovacion = expiraAt - Date.now() - this.REFRESH_MARGIN_MS;
    const delay = Math.max(0, Math.min(hastaVentanaRenovacion, this.MAX_REFRESH_TIMER_MS));

    this.tokenRefreshTimer = setTimeout(() => {
      this.verificarRenovacionProactiva();
    }, delay);
  }

  private verificarRenovacionProactiva() {
    if (!this.monitoringActive || !this.estaLogueado() || this.refreshRequest$) return;

    const timeoutMs = this.obtenerTimeoutInactividadMs();
    if (Date.now() - this.lastActivityAt >= timeoutMs) {
      this.logoutPorInactividad();
      return;
    }

    const expiraAt = this.obtenerExpiracionAccessTokenMs();
    if (!expiraAt) return;

    if (expiraAt - Date.now() > this.REFRESH_MARGIN_MS) {
      this.programarRenovacionProactiva();
      return;
    }

    this.refreshToken().subscribe({
      error: () => {
        // refreshToken ya limpia la sesión y redirige cuando la rotación deja de ser válida.
      }
    });
  }

  private obtenerExpiracionAccessTokenMs(): number | null {
    const almacenada = localStorage.getItem('token_expira');
    if (almacenada) {
      const parsed = Date.parse(almacenada);
      if (Number.isFinite(parsed)) return parsed;
    }

    const token = this._token();
    if (!token) return null;

    try {
      const payload = jwtDecode<{ exp?: number }>(token);
      return payload.exp ? payload.exp * 1000 : null;
    } catch {
      return null;
    }
  }

  private detenerMonitoreoInactividad() {
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
      this.inactivityTimer = null;
    }
    if (this.tokenRefreshTimer) {
      clearTimeout(this.tokenRefreshTimer);
      this.tokenRefreshTimer = null;
    }

    this.eventListeners.forEach(removeFn => removeFn());
    this.eventListeners = [];
    this.monitoringActive = false;
    this.lastActivityAt = 0;
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
