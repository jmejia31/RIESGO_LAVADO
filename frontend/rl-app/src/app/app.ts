import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ReportPreviewComponent } from './shared/report-preview/report-preview.component';
import { ConfiguracionService } from './core/configuration/configuracion.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ReportPreviewComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly configService = inject(ConfiguracionService, { optional: true });

  constructor() {
    this.configService?.CargarConfiguracion().subscribe();
    // Política global de modales institucionales: Escape nunca cierra un diálogo.
    // El cierre queda exclusivamente en manos de los botones/acciones explícitas del modal.
    const bloquearEscape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape' || !this.document.querySelector('dialog[open]')) return;
      event.preventDefault();
      event.stopImmediatePropagation();
    };

    // Los <dialog> nativos también disparan "cancel" ante Escape. Se bloquea en captura
    // para cubrir tanto modales nuevos como cualquier diálogo legacy aún presente durante la migración.
    const bloquearCancel = (event: Event) => {
      if (!(event.target instanceof HTMLDialogElement)) return;
      event.preventDefault();
      event.stopImmediatePropagation();
    };

    this.document.addEventListener('keydown', bloquearEscape, true);
    this.document.addEventListener('cancel', bloquearCancel, true);

    this.destroyRef.onDestroy(() => {
      this.document.removeEventListener('keydown', bloquearEscape, true);
      this.document.removeEventListener('cancel', bloquearCancel, true);
    });
  }
}
