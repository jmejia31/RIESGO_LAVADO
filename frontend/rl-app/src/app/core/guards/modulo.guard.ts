import { inject }        from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService }   from '../services/auth.service';

export const moduloGuard = (moduloId: number): CanActivateFn => () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  const usr    = auth.usuario();

  if (!usr) {
    // No está logueado: ir al login
    router.navigate(['/login']);
    return false;
  }

  if (auth.requiereCambioPassword()) {
    auth.cerrarSesionLocal();
    router.navigate(['/login'], { queryParams: { razon: 'cambio-password' } });
    return false;
  }

  if (usr.modulosIds?.includes(moduloId)) {
    return true;
  }

  // Tiene sesión pero no tiene acceso a este módulo → mostrar página de sin acceso
  // Por ahora redirigir a /home para evitar loops
  router.navigate(['/sin-acceso']);
  return false;
};
