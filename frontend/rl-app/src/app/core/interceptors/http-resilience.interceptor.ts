import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { retry, timer, throwError } from 'rxjs';

/**
 * Interceptor de Resiliencia HTTP (FE-02):
 * - Aplica reintentos automáticos (Exponential Backoff) EXCLUSIVAMENTE a solicitudes de lectura (GET)
 *   ante fallos de conexión de red (status 0) o indisponibilidad temporal del servidor (503, 504).
 * - PROHIBIDO estrictamente reintentar peticiones mutantes (POST, PUT, DELETE, PATCH).
 */
export const httpResilienceInterceptor: HttpInterceptorFn = (req, next) => {
  const esGet = req.method.toUpperCase() === 'GET';

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
    })
  );
};
