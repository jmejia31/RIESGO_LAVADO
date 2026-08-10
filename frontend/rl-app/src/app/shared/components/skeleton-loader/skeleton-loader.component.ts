import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type SkeletonVariant = 'content' | 'table' | 'cards' | 'form';

@Component({
  selector: 'app-skeleton-loader',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="w-full"
      aria-live="polite"
      aria-atomic="true"
      aria-busy="true"
      [attr.aria-label]="label"
      data-testid="skeleton-loader">
      <span class="sr-only">{{ label }}</span>

      <div aria-hidden="true" class="pointer-events-none select-none">
        @switch (variant) {
          @case ('table') {
            <div class="overflow-hidden rounded-xl border border-slate-200 bg-white">
              <div class="grid grid-cols-4 gap-4 border-b border-slate-200 bg-slate-50 px-4 py-3">
                @for (item of columnas; track item) {
                  <span class="skeleton-block h-3 rounded-md"></span>
                }
              </div>
              <div class="divide-y divide-slate-100">
                @for (row of filasArray; track row) {
                  <div class="grid grid-cols-4 gap-4 px-4 py-4" data-skeleton-row>
                    <span class="skeleton-block h-4 rounded-md"></span>
                    <span class="skeleton-block h-4 rounded-md"></span>
                    <span class="skeleton-block h-4 rounded-md"></span>
                    <span class="skeleton-block h-4 rounded-md"></span>
                  </div>
                }
              </div>
            </div>
          }
          @case ('cards') {
            <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              @for (row of filasArray; track row) {
                <div class="rounded-xl border border-slate-200 bg-white p-4" data-skeleton-row>
                  <span class="skeleton-block block h-4 w-2/3 rounded-md"></span>
                  <span class="skeleton-block mt-4 block h-3 w-full rounded-md"></span>
                  <span class="skeleton-block mt-2 block h-3 w-4/5 rounded-md"></span>
                </div>
              }
            </div>
          }
          @case ('form') {
            <div class="grid gap-4 rounded-xl border border-slate-200 bg-white p-5 md:grid-cols-2">
              @for (row of filasArray; track row) {
                <div data-skeleton-row>
                  <span class="skeleton-block block h-3 w-1/3 rounded-md"></span>
                  <span class="skeleton-block mt-2 block h-10 w-full rounded-lg"></span>
                </div>
              }
            </div>
          }
          @default {
            <div class="space-y-3 rounded-xl border border-slate-200 bg-white p-5">
              <span class="skeleton-block block h-5 w-1/3 rounded-md"></span>
              @for (row of filasArray; track row) {
                <span
                  class="skeleton-block block h-4 rounded-md"
                  [ngClass]="row === filasArray.length - 1 ? 'w-4/5' : 'w-full'"
                  data-skeleton-row></span>
              }
            </div>
          }
        }
      </div>
    </div>
  `
})
export class SkeletonLoaderComponent {
  @Input() variant: SkeletonVariant = 'content';
  @Input() label = 'Cargando información';
  @Input() rows = 4;

  readonly columnas = [1, 2, 3, 4];

  get filasArray(): number[] {
    const cantidad = Math.min(12, Math.max(1, Number.isFinite(this.rows) ? Math.trunc(this.rows) : 4));
    return Array.from({ length: cantidad }, (_, index) => index);
  }
}
