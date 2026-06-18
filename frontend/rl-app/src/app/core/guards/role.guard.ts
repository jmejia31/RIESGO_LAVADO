import { inject }        from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService }   from '../services/auth.service';

export const roleGuard = (roles: string[]): CanActivateFn => () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  if (auth.tieneRol(roles)) return true;

  router.navigate(['/login']); // Redirigir a login en caso de no poseer el rol
  return false;
};
