import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
  selector: 'app-form-builder-statusbar',
  standalone: true,
  templateUrl: './form-builder-statusbar.component.html',
  styleUrls: ['./form-builder-statusbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderStatusbarComponent {
  @Input() versionCodigo: string = '';
  @Input() soloLectura: boolean = false;
  @Input() seccionesCount: number = 0;
  @Input() catalogosCount: number = 0;
}
