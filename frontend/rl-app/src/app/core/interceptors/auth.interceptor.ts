import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject }   from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // El interceptor adjunta JWT e intenta renovar sesión ante 401.
  // Las rutas se deniegan en sus guards; un 403 de operación API se propaga
  // al consumidor para conservar el contexto de la pantalla autorizada.
  const auth   = inject(AuthService);

  const token = auth.getAccessToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/auth/refresh') && !req.url.includes('/auth/login')) {
        // Intentar renovar el token antes de cerrar la sesión del usuario.
        return auth.refreshToken().pipe(
          switchMap(() => {
            const nuevoToken = auth.getAccessToken();
            const retryReq   = req.clone({
              setHeaders: { Authorization: `Bearer ${nuevoToken}` }
            });
            return next(retryReq);
          }),
          catchError(refreshError => {
            // AuthService decide si el refresh fue rechazado de forma definitiva. Un fallo
            // transitorio de red/servidor se propaga sin convertirlo artificialmente en logout.
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
