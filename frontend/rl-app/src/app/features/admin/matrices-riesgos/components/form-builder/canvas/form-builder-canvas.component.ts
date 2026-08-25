import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CampoBuilderModel, SeccionBuilderModel } from '../../../models/form-builder.models';

@Component({
  selector: 'app-form-builder-canvas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './form-builder-canvas.component.html',
  styleUrls: ['./form-builder-canvas.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderCanvasComponent {
  @Input() secciones: SeccionBuilderModel[] = [];
  @Input() soloLectura: boolean = false;
  @Input() seccionActivaId: string | null = null;
  @Input() campoActivoId: string | null = null;

  @Output() seleccionarSeccion = new EventEmitter<string>();
  @Output() seleccionarCampo = new EventEmitter<CampoBuilderModel>();
  @Output() agregarSeccion = new EventEmitter<void>();
  @Output() eliminarSeccion = new EventEmitter<string>();
  @Output() eliminarCampo = new EventEmitter<{ seccionId: string; campoId: string }>();
  @Output() reordenarCampo = new EventEmitter<{ seccionId: string; index: number; direccion: 'subir' | 'bajar' }>();
  @Output() tituloSeccionChange = new EventEmitter<{ seccionId: string; titulo: string }>();
  @Output() columnasSeccionChange = new EventEmitter<{ seccionId: string; columnas: number }>();
  @Output() soltarControl = new EventEmitter<{ seccionId: string; tipo: string }>();

  readonly seccionArrastreSobreId = signal<string | null>(null);

  onDragOver(event: DragEvent, seccionId: string): void {
    if (this.soloLectura) return;
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'copy';
    }
    if (this.seccionArrastreSobreId() !== seccionId) {
      this.seccionArrastreSobreId.set(seccionId);
    }
  }

  onDragLeave(event: DragEvent, seccionId: string): void {
    const relatedTarget = event.relatedTarget as HTMLElement | null;
    const currentTarget = event.currentTarget as HTMLElement | null;
    if (currentTarget && relatedTarget && currentTarget.contains(relatedTarget)) {
      return;
    }
    if (this.seccionArrastreSobreId() === seccionId) {
      this.seccionArrastreSobreId.set(null);
    }
  }

  onDrop(event: DragEvent, seccionId: string): void {
    event.preventDefault();
    this.seccionArrastreSobreId.set(null);
    if (this.soloLectura) return;

    const tipo = event.dataTransfer?.getData('application/x-form-builder-control')
      || event.dataTransfer?.getData('text/plain');

    if (tipo && tipo.trim() !== '') {
      this.soltarControl.emit({ seccionId, tipo: tipo.trim() });
    }
  }
}
