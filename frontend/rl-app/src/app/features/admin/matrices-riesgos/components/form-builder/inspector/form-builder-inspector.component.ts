import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CampoBuilderModel, TipoControlDefinicion } from '../../../models/form-builder.models';

export interface InspectorCatalogoOption {
  codigo: string;
  nombre: string;
  cantidadElementos: number;
}

@Component({
  selector: 'app-form-builder-inspector',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './form-builder-inspector.component.html',
  styleUrls: ['./form-builder-inspector.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderInspectorComponent {
  @Input() campoActivo: CampoBuilderModel | null = null;
  @Input() soloLectura: boolean = false;
  @Input() tiposControles: TipoControlDefinicion[] = [];
  @Input() catalogosDisponibles: InspectorCatalogoOption[] = [];

  @Output() propiedadCambiada = new EventEmitter<void>();
  @Output() navegarCatalogos = new EventEmitter<void>();
}
