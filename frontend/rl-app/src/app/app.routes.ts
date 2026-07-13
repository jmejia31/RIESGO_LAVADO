import { inject } from '@angular/core';
import { Routes, Router } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { UsuariosComponent } from './features/admin/usuarios/usuarios.component';
import { ConfiguracionComponent } from './features/admin/configuracion/configuracion.component';
import { MonitoreoListasComponent } from './features/admin/monitoreo-listas/monitoreo-listas.component';
import { BitacoraComponent } from './features/admin/bitacora/bitacora.component';
import { TipoListasComponent } from './features/admin/tipo-listas/tipo-listas.component';
import { CargarListasComponent } from './features/admin/cargar-listas/cargar-listas.component';
import { CoincidenciasPatronoComponent } from './features/admin/coincidencias-patrono/coincidencias-patrono.component';
import { CoincidenciasEmpleadoComponent } from './features/admin/coincidencias-empleado/coincidencias-empleado.component';
import { MatricesRiesgosComponent } from './features/admin/matrices-riesgos/matrices-riesgos.component';
import { MainLayoutComponent } from './shared/layout/main-layout/main-layout.component';
import { SinAccesoComponent } from './shared/pages/sin-acceso/sin-acceso.component';
import { authGuard } from './core/guards/auth.guard';
import { moduloGuard } from './core/guards/modulo.guard';
import { AuthService } from './core/services/auth.service';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  // Proceso de acceso denegado: requiere sesión activa, pero no permiso de módulo específico.
  { path: 'sin-acceso', component: SinAccesoComponent, canActivate: [authGuard] },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'home',
        component: SinAccesoComponent, // Ruta técnica: redirige al primer módulo autorizado del usuario.
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
        component: UsuariosComponent,
        canActivate: [moduloGuard(2)]
      },
      {
        path: 'configuracion',
        component: ConfiguracionComponent,
        canActivate: [moduloGuard(3)]
      },
      {
        path: 'monitoreo-listas',
        component: MonitoreoListasComponent,
        canActivate: [moduloGuard(4)]
      },
      {
        path: 'bitacora',
        component: BitacoraComponent,
        canActivate: [moduloGuard(5)]
      },
      {
        path: 'tipo-listas',
        component: TipoListasComponent,
        canActivate: [moduloGuard(6)]
      },
      {
        path: 'cargar-listas',
        component: CargarListasComponent,
        canActivate: [moduloGuard(7)]
      },
      {
        path: 'coincidencias-patrono',
        component: CoincidenciasPatronoComponent,
        canActivate: [moduloGuard(8)]
      },
      {
        path: 'coincidencias-empleado',
        component: CoincidenciasEmpleadoComponent,
        canActivate: [moduloGuard(9)]
      },
      {
        path: 'matrices-riesgos',
        component: MatricesRiesgosComponent,
        canActivate: [moduloGuard(10)]
      },
      { path: '', redirectTo: 'home', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
