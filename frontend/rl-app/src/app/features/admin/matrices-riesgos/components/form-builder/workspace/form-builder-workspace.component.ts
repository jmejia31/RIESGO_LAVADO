import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-form-builder-workspace-v2',
  standalone: true,
  templateUrl: './form-builder-workspace.component.html',
  styleUrls: ['./form-builder-workspace.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FormBuilderWorkspaceV2Component {}
