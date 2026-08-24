import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-form-builder-statusbar-v2',
  standalone: true,
  templateUrl: './form-builder-statusbar.component.html',
  styleUrls: ['./form-builder-statusbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderStatusbarV2Component {}
