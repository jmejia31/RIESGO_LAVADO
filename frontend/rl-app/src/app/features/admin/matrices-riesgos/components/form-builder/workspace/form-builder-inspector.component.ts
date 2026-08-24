import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-form-builder-inspector-v2',
  standalone: true,
  templateUrl: './form-builder-inspector.component.html',
  styleUrls: ['./form-builder-inspector.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderInspectorV2Component {}
