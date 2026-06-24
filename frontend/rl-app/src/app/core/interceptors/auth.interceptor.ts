import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject }   from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router }      from '@angular/router';

const obtenerMensaje403 = (error: HttpErrorResponse): string => {
  const body = error.error;
  if (typeof body === 'string' && body.trim()) return body.trim();
  if (body?.mensaje) return body.mensaje;
  if (body?.message) return body.message;
  return 'No tiene permiso para realizar esta accion.';
};

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  const token = auth.getAccessToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/auth/refresh') && !req.url.includes('/auth/login')) {
        // Intentar renovar el token
        return auth.refreshToken().pipe(
          switchMap(() => {
            const nuevoToken = auth.getAccessToken();
            const retryReq   = req.clone({
              setHeaders: { Authorization: `Bearer ${nuevoToken}` }
            });
            return next(retryReq);
          }),
          catchError(refreshError => {
            router.navigate(['/login'], { queryParams: { razon: 'expirada' } });
            return throwError(() => refreshError);
          })
        );
      }
      if (error.status === 403) {
        const mensaje = obtenerMensaje403(error);
        const rutaActual = router.url.split('?')[0];

        // Mantiene la sesion activa y muestra una salida clara cuando el backend rechaza por permiso.
        if (rutaActual !== '/sin-acceso') {
          router.navigate(['/sin-acceso'], {
            queryParams: { mensaje },
            replaceUrl: true
          });
        }
      }

      return throwError(() => error);
    })
  );
};
