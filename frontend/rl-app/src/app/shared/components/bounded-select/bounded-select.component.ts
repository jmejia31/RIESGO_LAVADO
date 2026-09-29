import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Output,
  inject,
  signal
} from '@angular/core';
import { ActionIconComponent } from '../action-icon/action-icon.component';

export interface BoundedSelectOption {
  value: string | number;
  label: string;
  searchText?: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-bounded-select',
  standalone: true,
  imports: [ActionIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="w-full min-w-0" data-ui-bounded-select data-ui-dropdown-direction="down">
      <button
        type="button"
        [id]="inputId"
        class="flex w-full items-center justify-between gap-3 rounded-lg border border-slate-300 bg-white px-3 py-2 text-left text-xs font-medium text-slate-800 shadow-sm transition-colors hover:border-slate-400 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-400"
        role="combobox"
        aria-haspopup="listbox"
        [attr.aria-label]="ariaLabel"
        [attr.aria-expanded]="abierto()"
        [attr.aria-controls]="panelId"
        [disabled]="disabled"
        data-ui-bounded-select-trigger
        (click)="alternar()"
        (keydown.arrowdown)="abrirDesdeTeclado($event)">
        <span class="min-w-0 flex-1 truncate" [class.text-slate-500]="esValorVacio()">{{ etiquetaSeleccionada }}</span>
        <span class="inline-flex h-4 w-4 shrink-0 items-center justify-center text-slate-500" aria-hidden="true">
          <app-action-icon [action]="abierto() ? 'move-up' : 'move-down'" />
        </span>
      </button>

      @if (abierto()) {
        <div
          [id]="panelId"
          class="bounded-select__panel mt-1 overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xl"
          data-ui-bounded-select-panel>
          @if (showSearch && options.length >= searchThreshold) {
            <div class="border-b border-slate-100 p-2">
              <div class="relative">
                <span class="pointer-events-none absolute inset-y-0 left-0 flex w-9 items-center justify-center text-slate-400" aria-hidden="true">
                  <app-action-icon action="search" />
                </span>
                <input
                  type="search"
                  class="w-full rounded-lg border border-slate-200 bg-slate-50 py-2 pl-9 pr-3 text-xs text-slate-800 outline-none transition-colors focus:border-blue-500 focus:bg-white focus:ring-2 focus:ring-blue-500/20"
                  [value]="consulta()"
                  [placeholder]="searchPlaceholder"
                  [attr.aria-label]="'Buscar en ' + ariaLabel"
                  data-ui-bounded-select-search
                  (input)="actualizarConsulta($event)">
              </div>
            </div>
          }

          <div
            class="bounded-select__options p-1"
            role="listbox"
            [attr.aria-label]="ariaLabel"
            data-ui-bounded-select-scroll>
            <button
              type="button"
              role="option"
              class="flex w-full items-center rounded-lg px-3 py-2 text-left text-xs transition-colors hover:bg-slate-100 focus:bg-slate-100 focus:outline-none"
              [class.bg-blue-50]="esValorVacio()"
              [class.font-semibold]="esValorVacio()"
              [class.text-blue-800]="esValorVacio()"
              [class.text-slate-700]="!esValorVacio()"
              [attr.aria-selected]="esValorVacio()"
              (click)="seleccionarVacio()">
              {{ placeholder }}
            </button>

            @for (option of opcionesFiltradas; track trackOption(option)) {
              <button
                type="button"
                role="option"
                class="flex w-full items-center rounded-lg px-3 py-2 text-left text-xs transition-colors hover:bg-slate-100 focus:bg-slate-100 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
                [class.bg-blue-50]="esSeleccionada(option)"
                [class.font-semibold]="esSeleccionada(option)"
                [class.text-blue-800]="esSeleccionada(option)"
                [class.text-slate-700]="!esSeleccionada(option)"
                [disabled]="option.disabled"
                [attr.aria-selected]="esSeleccionada(option)"
                [title]="option.label"
                (click)="seleccionar(option)">
                <span class="truncate">{{ option.label }}</span>
              </button>
            } @empty {
              <p class="px-3 py-5 text-center text-xs font-medium text-slate-500">No hay coincidencias.</p>
            }
          </div>

          <div class="border-t border-slate-100 bg-slate-50 px-3 py-2 text-[11px] font-medium text-slate-500" data-ui-bounded-select-count>
            {{ opcionesFiltradas.length }} de {{ options.length }} opciones
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-width: 0;
    }

    .bounded-select__panel {
      position: relative;
      z-index: 20;
    }

    .bounded-select__options {
      max-height: min(14rem, 28dvh);
      overflow-y: auto;
      overscroll-behavior: contain;
      scrollbar-gutter: stable;
    }
  `]
})
export class BoundedSelectComponent {
  private static nextId = 0;
  private readonly host = inject(ElementRef<HTMLElement>);

  @Input() value: string | number | null = null;
  @Input() options: readonly BoundedSelectOption[] = [];
  @Input() placeholder = 'Seleccione una opción';
  @Input() emptyValue: string | number | null = null;
  @Input() ariaLabel = 'Selector';
  @Input() searchPlaceholder = 'Buscar...';
  @Input() searchThreshold = 8;
  @Input() showSearch = true;
  @Input() disabled = false;
  @Input() inputId = `bounded-select-${BoundedSelectComponent.nextId++}`;
  @Output() valueChange = new EventEmitter<string | number | null>();

  readonly abierto = signal(false);
  readonly consulta = signal('');
  readonly panelId = `${this.inputId}-panel-${BoundedSelectComponent.nextId++}`;

  get etiquetaSeleccionada(): string {
    const seleccionada = this.options.find(option => this.sonIguales(option.value, this.value));
    return seleccionada?.label ?? this.placeholder;
  }

  get opcionesFiltradas(): readonly BoundedSelectOption[] {
    const consulta = this.normalizar(this.consulta());
    if (!consulta) return this.options;
    return this.options.filter(option => {
      const texto = this.normalizar(`${option.label} ${option.searchText ?? ''}`);
      return texto.includes(consulta);
    });
  }

  alternar(): void {
    if (this.disabled) return;
    this.abierto() ? this.cerrar() : this.abrir();
  }

  abrir(): void {
    if (this.disabled || this.abierto()) return;
    this.consulta.set('');
    this.abierto.set(true);
    if (this.showSearch && this.options.length >= this.searchThreshold) {
      setTimeout(() => {
        (this.host.nativeElement.querySelector('[data-ui-bounded-select-search]') as HTMLInputElement | null)?.focus();
      }, 0);
    }
  }

  abrirDesdeTeclado(event: Event): void {
    event.preventDefault();
    this.abrir();
  }

  cerrar(devolverFoco = false): void {
    if (!this.abierto()) return;
    this.abierto.set(false);
    this.consulta.set('');
    if (devolverFoco) {
      setTimeout(() => {
        (this.host.nativeElement.querySelector('[data-ui-bounded-select-trigger]') as HTMLButtonElement | null)?.focus();
      }, 0);
    }
  }

  seleccionar(option: BoundedSelectOption): void {
    if (option.disabled) return;
    this.valueChange.emit(option.value);
    this.cerrar(true);
  }

  seleccionarVacio(): void {
    this.valueChange.emit(this.emptyValue);
    this.cerrar(true);
  }

  actualizarConsulta(event: Event): void {
    this.consulta.set((event.target as HTMLInputElement).value);
  }

  esSeleccionada(option: BoundedSelectOption): boolean {
    return this.sonIguales(option.value, this.value);
  }

  esValorVacio(): boolean {
    return this.sonIguales(this.value, this.emptyValue);
  }

  trackOption(option: BoundedSelectOption): string {
    return `${String(option.value)}::${option.label}`;
  }

  @HostListener('document:click', ['$event'])
  cerrarAlHacerClickFuera(event: MouseEvent): void {
    if (this.abierto() && !this.host.nativeElement.contains(event.target as Node)) this.cerrar();
  }

  @HostListener('document:keydown.escape')
  cerrarConEscape(): void {
    if (this.abierto()) this.cerrar(true);
  }

  private sonIguales(a: string | number | null, b: string | number | null): boolean {
    if (a === null || b === null) return a === b;
    return String(a) === String(b);
  }

  private normalizar(value: string): string {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .trim()
      .toLowerCase();
  }
}
