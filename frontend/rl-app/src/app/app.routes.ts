import { inject } from '@angular/core';
import { Routes, Router } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { moduloGuard } from './core/guards/modulo.guard';
import { AuthService } from './core/auth/auth.service';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/pages/login/login.component').then(m => m.LoginComponent)
  },
  // Proceso de acceso denegado: requiere sesión activa, pero no permiso de módulo específico.
  {
    path: 'sin-acceso',
    loadComponent: () => import('./shared/pages/sin-acceso/sin-acceso.component').then(m => m.SinAccesoComponent),
    canActivate: [authGuard]
  },
  {
    path: '',
    loadComponent: () => import('./shared/layout/main-layout/main-layout.component').then(m => m.MainLayoutComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'home',
        // Ruta técnica: redirige al primer módulo autorizado del usuario.
        loadComponent: () => import('./shared/pages/sin-acceso/sin-acceso.component').then(m => m.SinAccesoComponent),
        canActivate: [() => {
          const auth = inject(AuthService);
          const router = inject(Router);
          const usr = auth.usuario();
          if (!usr) {
            router.navigate(['/login']);
            return false;
          }
          if (auth.requiereCambioPassword()) {
            auth.cerrarSesionLocal();
            router.navigate(['/login'], { queryParams: { razon: 'cambio-password' } });
            return false;
          }
          const ids = usr.modulosIds ?? [];
          if (ids.includes(2)) {
            router.navigate(['/usuarios']);
            return false;
          }
          if (ids.includes(3)) {
            router.navigate(['/configuracion']);
            return false;
          }
          if (ids.includes(4)) {
            router.navigate(['/monitoreo-listas']);
            return false;
          }
          if (ids.includes(5)) {
            router.navigate(['/bitacora']);
            return false;
          }
          if (ids.includes(6)) {
            router.navigate(['/tipo-listas']);
            return false;
          }
          if (ids.includes(7)) {
            router.navigate(['/cargar-listas']);
            return false;
          }
          if (ids.includes(8)) {
            router.navigate(['/coincidencias-patrono']);
            return false;
          }
          if (ids.includes(9)) {
            router.navigate(['/coincidencias-empleado']);
            return false;
          }
          if (ids.includes(10)) {
            router.navigate(['/matrices-riesgos']);
            return false;
          }
          router.navigate(['/sin-acceso']);
          return false;
        }]
      },
      {
        path: 'usuarios',
        loadComponent: () => import('./features/admin/usuarios/pages/usuarios/usuarios.component').then(m => m.UsuariosComponent),
        canActivate: [moduloGuard(2)]
      },
      {
        path: 'configuracion',
        loadComponent: () => import('./features/admin/configuracion/pages/configuracion/configuracion.component').then(m => m.ConfiguracionComponent),
        canActivate: [moduloGuard(3)]
      },
      {
        path: 'monitoreo-listas',
        loadComponent: () => import('./features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component').then(m => m.MonitoreoListasComponent),
        canActivate: [moduloGuard(4)]
      },
      {
        path: 'bitacora',
        loadComponent: () => import('./features/admin/bitacora/pages/bitacora/bitacora.component').then(m => m.BitacoraComponent),
        canActivate: [moduloGuard(5)]
      },
      {
        path: 'tipo-listas',
        loadComponent: () => import('./features/admin/listas/pages/tipo-listas/tipo-listas.component').then(m => m.TipoListasComponent),
        canActivate: [moduloGuard(6)]
      },
      {
        path: 'cargar-listas',
        loadComponent: () => import('./features/admin/listas/pages/cargar-listas/cargar-listas.component').then(m => m.CargarListasComponent),
        canActivate: [moduloGuard(7)]
      },
      {
        path: 'coincidencias-patrono',
        loadComponent: () => import('./features/admin/listas/pages/coincidencias-patrono/coincidencias-patrono.component').then(m => m.CoincidenciasPatronoComponent),
        canActivate: [moduloGuard(8)]
      },
      {
        path: 'coincidencias-empleado',
        loadComponent: () => import('./features/admin/listas/pages/coincidencias-empleado/coincidencias-empleado.component').then(m => m.CoincidenciasEmpleadoComponent),
        canActivate: [moduloGuard(9)]
      },
      {
        path: 'matrices-riesgos',
        loadComponent: () => import('./features/admin/matrices-riesgos/pages/matrices-riesgos-ciclo-integral/matrices-riesgos-ciclo-integral.component').then(m => m.MatricesRiesgosCicloIntegralComponent),
        canActivate: [moduloGuard(10)]
      },
      { path: '', redirectTo: 'home', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
