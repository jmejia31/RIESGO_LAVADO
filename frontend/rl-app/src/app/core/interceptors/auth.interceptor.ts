import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject }   from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { Router }      from '@angular/router';

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
      return throwError(() => error);
    })
  );
};
