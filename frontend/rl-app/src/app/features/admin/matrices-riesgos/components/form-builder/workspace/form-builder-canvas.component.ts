import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-form-builder-canvas-v2',
  standalone: true,
  templateUrl: './form-builder-canvas.component.html',
  styleUrls: ['./form-builder-canvas.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderCanvasV2Component {}
