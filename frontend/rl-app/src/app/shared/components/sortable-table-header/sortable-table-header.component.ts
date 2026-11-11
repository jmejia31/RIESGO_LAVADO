import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { ActionIconComponent } from '../action-icon/action-icon.component';

export type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-sortable-table-header',
  standalone: true,
  imports: [ActionIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      class="group inline-flex min-h-8 items-center gap-1.5 text-left focus:outline-none focus-visible:ring-2 focus-visible:ring-ihss-500 focus-visible:ring-offset-1"
      [disabled]="disabled"
      [attr.aria-label]="ariaLabel"
      [attr.title]="ariaLabel"
      [attr.aria-pressed]="activeColumn === column"
      [attr.aria-sort]="activeColumn === column ? (direction === 'asc' ? 'ascending' : 'descending') : 'none'"
      (click)="sortChange.emit(column)">
      <span>{{ label }}</span>
      <app-action-icon action="sort" [sortDirection]="direction" aria-hidden="true" />
    </button>
  `
})
export class SortableTableHeaderComponent {
  @Input({ required: true }) column!: string;
  @Input({ required: true }) label!: string;
  @Input() activeColumn = '';
  @Input() direction: SortDirection = 'asc';
  @Input() disabled = false;
  @Input() ariaLabel = '';
  @Output() readonly sortChange = new EventEmitter<string>();
}
