import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { TipoControlDefinicion } from '../../../models/form-builder.models';

@Component({
  selector: 'app-form-builder-palette-v2',
  standalone: true,
  templateUrl: './form-builder-palette.component.html',
  styleUrls: ['./form-builder-palette.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderPaletteV2Component {
  @Input() tiposControles: TipoControlDefinicion[] = [];
  @Input() soloLectura: boolean = false;
  @Input() seccionActivaId: string | null = null;

  @Output() agregarCampo = new EventEmitter<TipoControlDefinicion>();
}
