import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, Output, inject } from '@angular/core';

@Component({
  selector: 'app-page-size-selector',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label class="inline-flex items-center gap-2 text-xs font-semibold text-gray-500" [attr.for]="selectId">
      <span>{{ label }}</span>
      <select [id]="selectId" class="h-8 w-auto min-w-14 rounded-lg border border-gray-200 bg-white px-2 text-xs font-semibold text-gray-700 focus:border-ihss-500 focus:outline-none focus:ring-2 focus:ring-ihss-500/20" [value]="value" [attr.aria-label]="ariaLabel" (change)="emitValue($any($event.target).value)">
        @for (option of options; track option) { <option [value]="option" [selected]="option === value">{{ option }}</option> }
      </select>
    </label>
  `
})
export class PageSizeSelectorComponent {
  private static nextId = 0;
  private readonly cdr = inject(ChangeDetectorRef);
  private _value = 10;

  @Input()
  get value(): number {
    return this._value;
  }
  set value(val: number) {
    this._value = val;
    this.cdr.markForCheck();
  }
  @Input() options: readonly number[] = [10, 25, 50];
  @Input() label = 'Mostrar';
  @Input() ariaLabel = 'Registros por página';
  @Input() selectId = `page-size-${PageSizeSelectorComponent.nextId++}`;
  @Output() valueChange = new EventEmitter<number>();

  emitValue(value: number | string): void {
    const normalized = Number(value);
    if (Number.isFinite(normalized) && normalized > 0) this.valueChange.emit(normalized);
  }
}
