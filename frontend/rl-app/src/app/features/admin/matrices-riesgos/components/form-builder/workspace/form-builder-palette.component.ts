import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-form-builder-palette-v2',
  standalone: true,
  templateUrl: './form-builder-palette.component.html',
  styleUrls: ['./form-builder-palette.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderPaletteV2Component {}
