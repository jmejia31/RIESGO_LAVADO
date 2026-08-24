import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-form-builder-toolbar-v2',
  standalone: true,
  templateUrl: './form-builder-toolbar.component.html',
  styleUrls: ['./form-builder-toolbar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderToolbarV2Component {}
