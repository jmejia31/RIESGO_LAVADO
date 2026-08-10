import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize, retry, tap, throwError, timer } from 'rxjs';
import { GlobalHttpStateService } from '../services/global-http-state.service';

const MAX_GET_RETRIES = 2;
const BASE_RETRY_DELAY_MS = 300;

/**
 * Interceptor de Resiliencia HTTP (FE-02):
 * - Gestiona el estado global de carga (`cargando`) y la notificación unificada de errores HTTP (`notificarError`).
 * - Aplica reintentos automáticos con backoff exponencial EXCLUSIVAMENTE a solicitudes GET
 *   ante fallos de conexión de red (status 0) o indisponibilidad temporal del servidor (503, 504).
 * - PROHIBIDO estrictamente reintentar peticiones mutantes (POST, PUT, DELETE, PATCH).
 */
export const httpResilienceInterceptor: HttpInterceptorFn = (req, next) => {
  const globalState = inject(GlobalHttpStateService);
  const esGet = req.method.toUpperCase() === 'GET';

  globalState.iniciarPeticion();

  return next(req).pipe(
    retry({
      count: esGet ? MAX_GET_RETRIES : 0,
      delay: (error: HttpErrorResponse, retryCount: number) => {
        const esErrorRedOTemporal = error.status === 0 || error.status === 503 || error.status === 504;

        if (!esGet || !esErrorRedOTemporal) {
          return throwError(() => error);
        }

        const delayMs = BASE_RETRY_DELAY_MS * Math.pow(2, retryCount - 1);
        return timer(delayMs);
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
