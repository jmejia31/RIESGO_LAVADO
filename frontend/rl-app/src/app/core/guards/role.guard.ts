import { inject }        from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService }   from '../services/auth.service';

export const roleGuard = (roles: string[]): CanActivateFn => () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (!auth.usuario()) {
    router.navigate(['/login']);
    return false;
  }

  if (auth.requiereCambioPassword()) {
    auth.cerrarSesionLocal();
    router.navigate(['/login'], { queryParams: { razon: 'cambio-password' } });
    return false;
  }

  if (auth.tieneRol(roles)) return true;

  router.navigate(['/sin-acceso']);
  return false;
};
