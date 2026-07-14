import { inject }        from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService }   from '../auth/auth.service';

export const moduloGuard = (moduloId: number): CanActivateFn => () => {
  // Proceso de autorización frontend: valida el módulo asignado en el JWT.
  // La autorización definitiva siempre se confirma nuevamente en backend con ModuloAuthorize.
  const auth   = inject(AuthService);
  const router = inject(Router);
  const usr    = auth.usuario();

  if (!usr) {
    // Usuario sin sesión local: regresa al login.
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

  // Usuario autenticado sin módulo autorizado: muestra página institucional de sin acceso.
  router.navigate(['/sin-acceso']);
  return false;
};
