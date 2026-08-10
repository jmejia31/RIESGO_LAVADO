import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { retry, timer, throwError, finalize, tap } from 'rxjs';
import { GlobalHttpStateService } from '../services/global-http-state.service';

/**
 * Interceptor de Resiliencia HTTP (FE-02):
 * - Gestiona el estado global de carga (`cargando`) y la notificación unificada de errores HTTP (`notificarError`).
 * - Aplica reintentos automáticos (Exponential Backoff) EXCLUSIVAMENTE a solicitudes de lectura (GET)
 *   ante fallos de conexión de red (status 0) o indisponibilidad temporal del servidor (503, 504).
 * - PROHIBIDO estrictamente reintentar peticiones mutantes (POST, PUT, DELETE, PATCH).
 */
export const httpResilienceInterceptor: HttpInterceptorFn = (req, next) => {
  const globalState = inject(GlobalHttpStateService);
  const esGet = req.method.toUpperCase() === 'GET';

  globalState.iniciarPeticion();

  return next(req).pipe(
    retry({
      count: esGet ? 2 : 0,
      delay: (error: HttpErrorResponse, retryCount: number) => {
        if (!esGet) {
          return throwError(() => error);
        }

        const esErrorRedOTemporal = error.status === 0 || error.status === 503 || error.status === 504;
        if (!esErrorRedOTemporal) {
          return throwError(() => error);
        }

        return timer(retryCount * 300);
      }
    }),
    tap({
      error: (error: HttpErrorResponse) => {
        globalState.notificarError(error);
      }
    }),
    finalize(() => {
      globalState.finalizarPeticion();
    })
  );
};
