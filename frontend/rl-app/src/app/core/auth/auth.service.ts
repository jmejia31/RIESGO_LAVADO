import { Injectable, signal, computed, effect } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router }      from '@angular/router';
import { Observable, tap, catchError, EMPTY, throwError, finalize, shareReplay, switchMap, timer } from 'rxjs';
import { LoginRequest, LoginResponse, UsuarioInfo } from './auth.models';
import { environment } from '../../../environments/environment';
import { jwtDecode }   from 'jwt-decode';
import { ConfiguracionService } from '../configuration/configuracion.service';
import { createHttpErrorScopeContext } from '../http/http-error-scope';

type RefreshResponse = { success: boolean; datos: LoginResponse };

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly API = `${environment.apiUrl}/auth`;
  private readonly DEFAULT_INACTIVITY_MINUTES = 30;
  private readonly ACTIVIDAD_KEY = 'rl_session_last_activity_at';
  private readonly REFRESH_MARGIN_MS = 5 * 60 * 1000;
  private readonly MAX_REFRESH_TIMER_MS = 60 * 60 * 1000;
  private readonly REFRESH_RETRY_MS = 30 * 1000;
  private readonly REFRESH_RACE_RETRY_MS = 250;

  // Inactividad y renovación son relojes independientes: renovar un JWT nunca cuenta como actividad humana.
  private inactivityTimer: ReturnType<typeof setTimeout> | null = null;
  private tokenRefreshTimer: ReturnType<typeof setTimeout> | null = null;
  private eventListeners: (() => void)[] = [];
  private monitoringActive = false;
  private lastActivityAt = 0;
  private nextRefreshAttemptAt = 0;
  private refreshRequest$: Observable<RefreshResponse> | null = null;
  private sessionEpoch = 0;

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
    localStorage.removeItem('refresh_token');
    // Un JWT persistido sin registro de actividad válida no restaura automáticamente la sesión.
    if (this._token()) {
      const guardada = this.ultimaActividadPersistida();
      if (guardada === null || Date.now() - guardada >= this.obtenerTimeoutInactividadMs()) {
        this.limpiarSesion();
      } else {
        this.lastActivityAt = guardada;
      }
    }
    // El cambio del JWT por refresh no reinicia el reloj de inactividad.
    effect(() => {
      if (this.estaLogueado()) {
        this.iniciarMonitoreoInactividad();
      } else {
        this.detenerMonitoreoInactividad();
      }
    });

    // The system timeout can change while the session is active. Recalculate
    // the timer instead of silently retaining the previous value.
    effect(() => {
      this.configService.configSistema()?.timeoutSesion;
      if (this.monitoringActive && this.estaLogueado()) {
        this.programarCierrePorInactividad();
      }
    });
  }

  login(dto: LoginRequest) {
    // Autenticación frontend: envía credenciales y guarda tokens solo cuando backend confirma éxito.
    return this.http.post<{ success: boolean; datos: LoginResponse }>(
      `${this.API}/login`, dto,
      { withCredentials: true, context: createHttpErrorScopeContext('local') }).pipe(
      tap(res => {
        if (res.success) {
          this.guardarSesion(res.datos, true);
        }
      })
    );
  }

  recuperarPassword(email: string) {
    return this.http.post<{ success: boolean; mensaje: string }>(
      `${this.API}/recuperar-password`, { email }, { context: createHttpErrorScopeContext('local') }
    );
  }

  cambiarPassword(passwordActual: string, nuevoPassword: string) {
    return this.http.put<{ success: boolean; mensaje: string }>(
      `${this.API}/password`, { passwordActual, nuevoPassword }, { context: createHttpErrorScopeContext('local') }
    );
  }

  logout() {
    const headers = this.xsrfHeaders();
    this.limpiarSesion();
    this.http.post(`${this.API}/logout`, null, {
      withCredentials: true, headers, context: createHttpErrorScopeContext('local')
    }).pipe(catchError(() => EMPTY)).subscribe();
    this.router.navigate(['/login']);
  }

  refreshToken(): Observable<RefreshResponse> {
    if (this.monitoringActive && this.verificarVencimientoInactividad()) {
      return throwError(() => new Error('Sesión expirada por inactividad.'));
    }
    // Single-flight: varios 401 o verificaciones simultáneas comparten una sola rotación de refresh token.
    if (this.refreshRequest$) {
      return this.refreshRequest$;
    }

    const epoch = this.sessionEpoch;

    const request$ = this.ejecutarRefreshConRecuperacion().pipe(
      tap(res => {
        // Evita que una respuesta tardía restaure una sesión que el usuario ya cerró o reemplazó.
        if (res.success && epoch === this.sessionEpoch) {
          this.nextRefreshAttemptAt = 0;
          this.guardarSesion(res.datos);
        }
      }),
      catchError((error: unknown) => {
        // Solo un rechazo explícito del refresh invalida la sesión. Un corte de red o un 5xx
        // no debe expulsar a un usuario que continúa activo; se conserva la sesión y se reintenta.
        if (this.esFalloDefinitivoRefresh(error) && epoch === this.sessionEpoch) {
          this.limpiarSesion();
          this.router.navigate(['/login'], { queryParams: { razon: 'expirada' } });
        }
        return throwError(() => error);
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
    if (this.monitoringActive && this.verificarVencimientoInactividad()) return null;
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

  private guardarSesion(datos: LoginResponse, esLoginHumano = false) {
    this.sessionEpoch++;
    if (esLoginHumano || this.ultimaActividadPersistida() === null) {
      this.lastActivityAt = Date.now();
      localStorage.setItem(this.ACTIVIDAD_KEY, String(this.lastActivityAt));
    }
    localStorage.setItem('access_token',  datos.accessToken);
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
    this.sessionEpoch++;
    this.detenerMonitoreoInactividad();
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('token_expira');
    localStorage.removeItem(this.ACTIVIDAD_KEY);
    this._token.set(null);
    this._usuario.set(null);
  }

  private xsrfHeaders(): { [name: string]: string } {
    const token = document.cookie.split('; ')
      .find(item => item.startsWith('XSRF-TOKEN='))
      ?.split('=').slice(1).join('=');
    return token ? { 'X-XSRF-TOKEN': decodeURIComponent(token) } : {};
  }

  private ejecutarRefreshConRecuperacion(): Observable<RefreshResponse> {
    return this.http.post<RefreshResponse>(
      `${this.API}/refresh`, null, { withCredentials: true, headers: this.xsrfHeaders(), context: createHttpErrorScopeContext('local') }
    ).pipe(
      catchError((error: unknown) => {
        if (!(error instanceof HttpErrorResponse)) return throwError(() => error);

        // A refresh 400 can mean an expired or desynchronized antiforgery
        // request token. Obtain a new pair and retry once; backend antiforgery
        // validation remains enabled.
        if (error.status === 400) {
          return this.http.get(`${this.API}/antiforgery`, { withCredentials: true, context: createHttpErrorScopeContext('local') }).pipe(
            switchMap(() => this.http.post<RefreshResponse>(
              `${this.API}/refresh`, null, { withCredentials: true, headers: this.xsrfHeaders(), context: createHttpErrorScopeContext('local') }
            ))
          );
        }

        // Two tabs can rotate the same refresh token concurrently. The second
        // request retries with the shared rotated cookie; a second failure is
        // still treated as a definitive rejection.
        if (error.status === 401) {
          return timer(this.REFRESH_RACE_RETRY_MS).pipe(
            switchMap(() => this.http.post<RefreshResponse>(
              `${this.API}/refresh`, null, { withCredentials: true, headers: this.xsrfHeaders(), context: createHttpErrorScopeContext('local') }
            ))
          );
        }

        return throwError(() => error);
      })
    );
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
    const ultima = this.ultimaActividadPersistida();
    if (ultima === null) { this.logoutPorInactividad(); return; }
    this.lastActivityAt = ultima;
    if (this.verificarVencimientoInactividad()) return;

    const eventos = ['mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'];
    eventos.forEach(evt => {
      const handler = () => this.registrarActividadUsuario();
      window.addEventListener(evt, handler, { passive: true });
      this.eventListeners.push(() => window.removeEventListener(evt, handler));
    });

    // Los timers se pausan con el navegador suspendido: revalidar el reloj real al regresar.
    const revalidar = () => this.programarCierrePorInactividad();
    window.addEventListener('focus', revalidar);
    window.addEventListener('pageshow', revalidar);
    document.addEventListener('visibilitychange', revalidar);
    this.eventListeners.push(() => window.removeEventListener('focus', revalidar));
    this.eventListeners.push(() => window.removeEventListener('pageshow', revalidar));
    this.eventListeners.push(() => document.removeEventListener('visibilitychange', revalidar));
    const sincronizar = (event: StorageEvent) => {
      if (!this.monitoringActive) return;
      if (event.key === 'access_token' && event.newValue === null) {
        this.limpiarSesion();
        this.router.navigate(['/login'], { queryParams: { razon: 'expirada' } });
      } else if (event.key === this.ACTIVIDAD_KEY) {
        this.programarCierrePorInactividad();
      }
    };
    window.addEventListener('storage', sincronizar);
    this.eventListeners.push(() => window.removeEventListener('storage', sincronizar));
    this.programarCierrePorInactividad();
    this.programarRenovacionProactiva();
  }

  private registrarActividadUsuario() {
    if (!this.monitoringActive || !this.estaLogueado()) return;

    if (this.verificarVencimientoInactividad()) return;
    this.lastActivityAt = Date.now();
    localStorage.setItem(this.ACTIVIDAD_KEY, String(this.lastActivityAt));
    this.programarCierrePorInactividad();

    // Si el navegador estuvo suspendido y el JWT quedó próximo a vencer, la primera actividad lo renueva.
    this.verificarRenovacionProactiva();
  }

  private ultimaActividadPersistida(): number | null {
    const raw = localStorage.getItem(this.ACTIVIDAD_KEY);
    if (!raw) return null;
    const fecha = Number(raw);
    return Number.isFinite(fecha) && fecha > 0 && fecha <= Date.now() ? fecha : null;
  }

  private verificarVencimientoInactividad(): boolean {
    if (!this.monitoringActive || !this.estaLogueado()) return false;
    const ultima = this.ultimaActividadPersistida();
    if (ultima === null || Date.now() - ultima >= this.obtenerTimeoutInactividadMs()) {
      this.logoutPorInactividad();
      return true;
    }
    this.lastActivityAt = ultima;
    return false;
  }

  private programarCierrePorInactividad() {
    if (this.inactivityTimer) {
      clearTimeout(this.inactivityTimer);
    }

    if (this.verificarVencimientoInactividad()) return;
    const timeoutMs = this.obtenerTimeoutInactividadMs();
    const transcurrido = Math.max(0, Date.now() - this.lastActivityAt);
    const restante = Math.max(0, timeoutMs - transcurrido);

    this.inactivityTimer = setTimeout(() => {
      if (this.verificarVencimientoInactividad()) return;
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
      this.tokenRefreshTimer = null;
      this.verificarRenovacionProactiva();
    }, delay);
  }

  private verificarRenovacionProactiva() {
    if (!this.monitoringActive || !this.estaLogueado() || this.refreshRequest$) return;

    if (this.verificarVencimientoInactividad()) return;

    if (Date.now() < this.nextRefreshAttemptAt) {
      return;
    }

    const expiraAt = this.obtenerExpiracionAccessTokenMs();
    if (!expiraAt) return;

    if (expiraAt - Date.now() > this.REFRESH_MARGIN_MS) {
      this.programarRenovacionProactiva();
      return;
    }

    this.refreshToken().subscribe({
      error: (error: unknown) => {
        // Los rechazos definitivos ya cerraron sesión dentro de refreshToken.
        // Los fallos transitorios conservan la sesión y se reintentan sin convertirlos en inactividad.
        if (!this.esFalloDefinitivoRefresh(error) && this.monitoringActive && this.estaLogueado()) {
          this.programarReintentoRenovacion();
        }
      }
    });
  }

  private programarReintentoRenovacion() {
    this.nextRefreshAttemptAt = Date.now() + this.REFRESH_RETRY_MS;

    if (this.tokenRefreshTimer) {
      clearTimeout(this.tokenRefreshTimer);
    }

    this.tokenRefreshTimer = setTimeout(() => {
      this.tokenRefreshTimer = null;
      this.nextRefreshAttemptAt = 0;
      this.verificarRenovacionProactiva();
    }, this.REFRESH_RETRY_MS);
  }

  private esFalloDefinitivoRefresh(error: unknown): boolean {
    if (!(error instanceof HttpErrorResponse)) return false;
    return error.status === 400 || error.status === 401 || error.status === 403;
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
    this.nextRefreshAttemptAt = 0;
  }

  private logoutPorInactividad() {
    const headers = this.xsrfHeaders();
    this.limpiarSesion(); // Fallos HTTP/antiforgery no impiden el cierre local.
    this.http.post(`${this.API}/logout`, null, {
      withCredentials: true, headers, context: createHttpErrorScopeContext('local')
    }).pipe(catchError(() => EMPTY)).subscribe();
    this.router.navigate(['/login'], { queryParams: { razon: 'expirada' } });
  }
}
