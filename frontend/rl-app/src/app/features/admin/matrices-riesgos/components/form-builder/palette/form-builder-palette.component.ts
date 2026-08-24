import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CampoBuilderModel, SeccionBuilderModel, TipoControlDefinicion } from '../../../models/form-builder.models';

@Component({
  selector: 'app-form-builder-palette',
  standalone: true,
  templateUrl: './form-builder-palette.component.html',
  styleUrls: ['./form-builder-palette.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderPaletteComponent {
  @Input() tiposControles: TipoControlDefinicion[] = [];
  @Input() secciones: SeccionBuilderModel[] = [];
  @Input() soloLectura: boolean = false;
  @Input() seccionActivaId: string | null = null;
  @Input() campoActivoId: string | null = null;

  @Output() agregarCampo = new EventEmitter<TipoControlDefinicion>();
  @Output() seleccionarSeccion = new EventEmitter<string>();
  @Output() seleccionarCampo = new EventEmitter<CampoBuilderModel>();
}
