import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';

@Component({
  selector: 'app-sin-acceso',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen bg-gray-50 p-8">
      <div class="text-center max-w-md">
        <div class="w-20 h-20 mx-auto mb-6 bg-red-100 rounded-full flex items-center justify-center">
          <svg class="w-10 h-10 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
          </svg>
        </div>
        <h1 class="text-2xl font-bold text-gray-800 mb-2">Acceso Denegado</h1>
        <p class="text-gray-500 mb-4">
          {{ mensaje() }}
        </p>
        <p class="text-sm text-gray-400 mb-6">
          Contacte al administrador del sistema para solicitar acceso.
        </p>
        <a routerLink="/home"
          class="inline-flex items-center gap-2 px-5 py-2.5 bg-ihss-700 text-white font-semibold text-sm rounded-xl hover:bg-ihss-800 transition-colors">
          Volver al Inicio
        </a>
      </div>
    </div>
  `
})
export class SinAccesoComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly mensajeDefault = 'No tiene permisos para acceder a este modulo.';
  private readonly mensajeParam = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('mensaje'))),
    { initialValue: null }
  );

  readonly mensaje = computed(() => this.mensajeParam() || this.mensajeDefault);
}
