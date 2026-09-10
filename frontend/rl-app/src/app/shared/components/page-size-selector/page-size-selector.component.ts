import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-page-size-selector',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label class="inline-flex items-center gap-2 text-xs font-semibold text-gray-500" [attr.for]="selectId">
      <span>{{ label }}</span>
      <select [id]="selectId" class="h-8 w-auto min-w-14 rounded-lg border border-gray-200 bg-white px-2 text-xs font-semibold text-gray-700 focus:border-ihss-500 focus:outline-none focus:ring-2 focus:ring-ihss-500/20" [value]="value" [attr.aria-label]="ariaLabel" (change)="emitValue($event)">
        @for (option of options; track option) { <option [value]="option">{{ option }}</option> }
      </select>
    </label>
  `
})
export class PageSizeSelectorComponent {
  private static nextId = 0;
  @Input() value = 10;
  @Input() options: readonly number[] = [10, 25, 50];
  @Input() label = 'Mostrar';
  @Input() ariaLabel = 'Registros por página';
  @Input() selectId = `page-size-${PageSizeSelectorComponent.nextId++}`;
  @Output() valueChange = new EventEmitter<number>();

  emitValue(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    if (Number.isFinite(value) && value > 0) this.valueChange.emit(value);
  }
}
