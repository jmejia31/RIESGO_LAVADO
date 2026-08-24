import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class App {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
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
