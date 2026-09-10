import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { ActionIconComponent } from '../action-icon/action-icon.component';

export type PaginationItem =
  | { type: 'page'; page: number }
  | { type: 'ellipsis'; key: string };

@Component({
  selector: 'app-data-pagination',
  standalone: true,
  imports: [ActionIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="flex min-w-0 flex-col gap-3 border-t border-gray-100 bg-white px-4 py-3 text-xs text-gray-500 sm:flex-row sm:items-center sm:justify-between" [attr.aria-label]="'Paginación de ' + itemLabel">
      <span class="shrink-0">Mostrando {{ startRecord }} a {{ endRecord }} de {{ totalRecords }} {{ itemLabel }}</span>
      <div class="flex min-w-0 items-center justify-center gap-1" [class.opacity-60]="disabled" data-pagination-controls>
        <button type="button" class="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border border-gray-200 bg-white text-gray-700 transition-colors hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ihss-500 focus:ring-offset-1 disabled:cursor-not-allowed disabled:opacity-40" [disabled]="disabled || effectivePage <= 1 || totalPages === 0" (click)="emitPage(effectivePage - 1)" [attr.title]="'Página anterior de ' + itemLabel" [attr.aria-label]="'Página anterior de ' + itemLabel">
          <app-action-icon action="previous" />
        </button>
        @for (item of visibleItems; track item.type === 'page' ? item.page : item.key) {
          @if (item.type === 'page') {
            <button type="button" class="inline-flex h-8 min-w-8 shrink-0 items-center justify-center rounded-lg border px-2 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ihss-500 focus:ring-offset-1" [class.border-ihss-900]="item.page === effectivePage" [class.bg-ihss-900]="item.page === effectivePage" [class.text-white]="item.page === effectivePage" [class.border-gray-200]="item.page !== effectivePage" [class.bg-white]="item.page !== effectivePage" [class.text-gray-600]="item.page !== effectivePage" [class.hover:bg-gray-50]="item.page !== effectivePage" [attr.aria-current]="item.page === effectivePage ? 'page' : null" [attr.aria-label]="'Ir a página ' + item.page" [disabled]="disabled || item.page === effectivePage" (click)="emitPage(item.page)">{{ item.page }}</button>
          } @else {
            <span class="inline-flex h-8 min-w-6 items-center justify-center px-1 text-gray-400" aria-hidden="true">…</span>
          }
        }
        <button type="button" class="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border border-gray-200 bg-white text-gray-700 transition-colors hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ihss-500 focus:ring-offset-1 disabled:cursor-not-allowed disabled:opacity-40" [disabled]="disabled || effectivePage >= totalPages || totalPages === 0" (click)="emitPage(effectivePage + 1)" [attr.title]="'Página siguiente de ' + itemLabel" [attr.aria-label]="'Página siguiente de ' + itemLabel">
          <app-action-icon action="next" />
        </button>
      </div>
    </nav>
  `
})
export class DataPaginationComponent {
  @Input() page = 1;
  @Input() pageSize = 10;
  @Input() totalRecords = 0;
  @Input() itemLabel = 'registros';
  @Input() disabled = false;
  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return this.totalRecords > 0 && this.pageSize > 0 ? Math.ceil(this.totalRecords / this.pageSize) : 0;
  }

  get effectivePage(): number {
    return this.totalPages === 0 ? 1 : Math.min(Math.max(this.page, 1), this.totalPages);
  }

  get startRecord(): number {
    return this.totalRecords === 0 ? 0 : (this.effectivePage - 1) * this.pageSize + 1;
  }

  get endRecord(): number {
    return this.totalRecords === 0 ? 0 : Math.min(this.effectivePage * this.pageSize, this.totalRecords);
  }

  get visibleItems(): PaginationItem[] {
    const total = this.totalPages;
    const current = this.effectivePage;
    if (total === 0) return [];
    if (total <= 7) return Array.from({ length: total }, (_, index) => ({ type: 'page', page: index + 1 }));

    const pages = new Set<number>([1, total]);
    const start = Math.max(2, current - 2);
    const end = Math.min(total - 1, current + 2);
    for (let page = start; page <= end; page++) pages.add(page);

    const ordered = [...pages].sort((a, b) => a - b);
    const items: PaginationItem[] = [];
    let previous: number | null = null;
    for (const page of ordered) {
      if (previous !== null && page - previous > 1) items.push({ type: 'ellipsis', key: `${previous}-${page}` });
      items.push({ type: 'page', page });
      previous = page;
    }
    return items;
  }

  emitPage(page: number): void {
    if (page >= 1 && page <= this.totalPages && page !== this.effectivePage) this.pageChange.emit(page);
  }
}
