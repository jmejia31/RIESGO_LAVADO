import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CampoBuilderModel, SeccionBuilderModel } from '../../../models/form-builder.models';

@Component({
  selector: 'app-form-builder-canvas',
  standalone: true,
  imports: [FormsModule],
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
}
