import { Injectable, signal, computed, OnDestroy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class GlobalHttpStateService implements OnDestroy {
  private readonly peticionesActivas = signal(0);
  readonly ultimoError = signal<string | null>(null);
  private autoDismissTimer: ReturnType<typeof setTimeout> | null = null;

  readonly cargando = computed(() => this.peticionesActivas() > 0);

  ngOnDestroy(): void {
    this.cancelarAutoDismiss();
  }

  iniciarPeticion(): void {
    this.peticionesActivas.update(c => c + 1);
  }

  finalizarPeticion(): void {
    this.peticionesActivas.update(c => Math.max(0, c - 1));
  }

  notificarError(error: HttpErrorResponse): void {
    // Ignorar 401, 403 (manejados por authInterceptor) y 499 (cancelación por usuario)
    if (error.status === 401 || error.status === 403 || error.status === 499) {
      return;
    }

    const body = error.error;
    let mensaje = 'Ocurrió un error inesperado al procesar la solicitud.';

    if (typeof body === 'string' && body.trim()) {
      mensaje = body.trim();
    } else if (body) {
      mensaje = body.detail || body.mensaje || body.title || body.message || mensaje;
    } else if (error.message) {
      mensaje = error.message;
    }

    this.ultimoError.set(mensaje);
    this.programarAutoDismiss();
  }

  limpiarError(): void {
    this.cancelarAutoDismiss();
    this.ultimoError.set(null);
  }

  private programarAutoDismiss(): void {
    this.cancelarAutoDismiss();
    this.autoDismissTimer = setTimeout(() => {
      this.ultimoError.set(null);
    }, 8000);
  }

  private cancelarAutoDismiss(): void {
    if (this.autoDismissTimer) {
      clearTimeout(this.autoDismissTimer);
      this.autoDismissTimer = null;
    }
  }
}

