import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
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

export interface BoundedSelectPlacement {
  direction: 'up' | 'down';
  left: number;
  top: number;
  width: number;
  maxHeight: number;
}

export function calculateBoundedSelectPlacement(
  anchor: Pick<DOMRect, 'left' | 'right' | 'top' | 'bottom' | 'width'>,
  viewport: { width: number; height: number },
  constrainTo?: Pick<DOMRect, 'left' | 'right' | 'top' | 'bottom'>,
  preferredHeight = 320,
  gap = 4,
  margin = 12
): BoundedSelectPlacement {
  const leftBound = Math.max(margin, constrainTo?.left ?? margin);
  const rightBound = Math.min(viewport.width - margin, constrainTo?.right ?? viewport.width - margin);
  const topBound = Math.max(margin, constrainTo?.top ?? margin);
  const bottomBound = Math.min(viewport.height - margin, constrainTo?.bottom ?? viewport.height - margin);
  const below = Math.max(0, bottomBound - anchor.bottom - gap);
  const above = Math.max(0, anchor.top - topBound - gap);
  const direction: BoundedSelectPlacement['direction'] = below >= Math.min(preferredHeight, 160) || below >= above ? 'down' : 'up';
  const availableHeight = direction === 'down' ? below : above;
  const maxHeight = Math.max(0, Math.min(preferredHeight, availableHeight));
  const width = Math.min(Math.max(0, anchor.width), Math.max(0, rightBound - leftBound));
  const left = Math.min(Math.max(anchor.left, leftBound), Math.max(leftBound, rightBound - width));
  const top = direction === 'down' ? anchor.bottom + gap : anchor.top - gap - maxHeight;

  return { direction, left, top, width, maxHeight };
}

@Component({
  selector: 'app-bounded-select',
  standalone: true,
  imports: [ActionIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="w-full min-w-0" data-ui-bounded-select [attr.data-ui-dropdown-direction]="placement()?.direction ?? 'closed'">
      <button
        type="button"
        [id]="inputId"
        class="flex w-full items-center justify-between gap-3 rounded-lg border border-slate-300 bg-white px-3 py-2 text-left text-xs font-medium text-slate-800 shadow-sm transition-colors hover:border-ihss-500 focus:border-ihss-600 focus:outline-none focus:ring-2 focus:ring-ihss-600/20 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-400"
        role="combobox"
        aria-haspopup="listbox"
        [attr.aria-label]="ariaLabel"
        [attr.aria-expanded]="abierto()"
        [attr.aria-controls]="panelId"
        [attr.aria-activedescendant]="activeOptionId()"
        [disabled]="disabled"
        data-ui-bounded-select-trigger
        (click)="alternar()"
        (keydown)="manejarTeclasCombobox($event)">
        <span class="min-w-0 flex-1 truncate" [class.text-slate-500]="esValorVacio()">{{ etiquetaSeleccionada }}</span>
        <span class="inline-flex h-4 w-4 shrink-0 items-center justify-center text-slate-500" aria-hidden="true">
          <app-action-icon [action]="abierto() ? 'move-up' : 'move-down'" />
        </span>
      </button>

      @if (abierto()) {
        <div
          [id]="panelId"
          class="bounded-select__panel overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xl"
          data-ui-bounded-select-panel
          [style.left.px]="placement()?.left"
          [style.top.px]="placement()?.top"
          [style.width.px]="placement()?.width"
          [style.max-height.px]="placement()?.maxHeight"
          (keydown)="manejarTeclasPanel($event)">
          @if (showSearch && options.length >= searchThreshold) {
            <div class="border-b border-slate-100 p-2">
              <div class="relative">
                <span class="pointer-events-none absolute inset-y-0 left-0 flex w-9 items-center justify-center text-slate-400" aria-hidden="true">
                  <app-action-icon action="search" />
                </span>
                <input
                  type="search"
                  class="w-full rounded-lg border border-slate-200 bg-slate-50 py-2 pl-9 pr-3 text-xs text-slate-800 outline-none transition-colors focus:border-ihss-600 focus:bg-white focus:ring-2 focus:ring-ihss-600/20"
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
              [id]="optionId(null)"
              tabindex="-1"
              class="flex w-full items-center rounded-lg px-3 py-2 text-left text-xs transition-colors hover:bg-slate-100 focus:bg-slate-100 focus:outline-none"
                [class.bg-ihss-50]="esValorVacio()"
              [class.font-semibold]="esValorVacio()"
                [class.text-ihss-800]="esValorVacio()"
              [class.text-slate-700]="!esValorVacio()"
              [attr.aria-selected]="esValorVacio()"
              (click)="seleccionarVacio()">
              {{ placeholder }}
            </button>

            @for (option of opcionesFiltradas; track trackOption(option)) {
              <button
                type="button"
                role="option"
                [id]="optionId(option)"
                tabindex="-1"
                class="flex w-full items-center rounded-lg px-3 py-2 text-left text-xs transition-colors hover:bg-slate-100 focus:bg-slate-100 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
                [class.bg-ihss-50]="esSeleccionada(option)"
                [class.font-semibold]="esSeleccionada(option)"
                [class.text-ihss-800]="esSeleccionada(option)"
                [class.text-slate-700]="!esSeleccionada(option)"
                [disabled]="option.disabled"
                [attr.aria-selected]="esSeleccionada(option)"
                [title]="option.label"
                (click)="seleccionar(option)">
                <span class="whitespace-normal break-words">{{ option.label }}</span>
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
      position: fixed;
      z-index: 1100;
      display: flex;
      min-width: 0;
      flex-direction: column;
      max-width: calc(100vw - 24px);
      overscroll-behavior: contain;
    }

    .bounded-select__options {
      flex: 1 1 auto;
      min-height: 0;
      overflow-y: auto;
      overscroll-behavior: contain;
      scrollbar-gutter: stable;
    }
  `]
})
export class BoundedSelectComponent implements OnDestroy {
  private static nextId = 0;
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

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
  readonly placement = signal<BoundedSelectPlacement | null>(null);
  readonly activeOptionIndex = signal(-1);
  readonly panelId = `${this.inputId}-panel-${BoundedSelectComponent.nextId++}`;
  private positionFrame: number | null = null;
  private listenersAttached = false;
  private readonly viewportListener = () => this.programarPosicionamiento();

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
    this.activeOptionIndex.set(-1);
    this.abierto.set(true);
    this.instalarListenersViewport();
    this.programarPosicionamiento();
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
    this.placement.set(null);
    this.activeOptionIndex.set(-1);
    this.removerListenersViewport();
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
    this.activeOptionIndex.set(-1);
    this.programarPosicionamiento();
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

  optionId(option: BoundedSelectOption | null): string {
    if (!option) return `${this.panelId}-option-placeholder`;
    return `${this.panelId}-option-${encodeURIComponent(String(option.value)).replaceAll('%', '-')}`;
  }

  activeOptionId(): string | null {
    const option = this.opcionesFiltradas[this.activeOptionIndex()];
    return option ? this.optionId(option) : null;
  }

  manejarTeclasCombobox(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      if (!this.abierto()) this.abrir();
      this.moverOpcion(event.key === 'ArrowDown' ? 1 : -1);
    }
    else if (event.key === 'Enter' && this.abierto()) {
      const option = this.opcionesFiltradas[this.activeOptionIndex()];
      if (option) {
        event.preventDefault();
        this.seleccionar(option);
      }
    }
    else if (event.key === 'Escape' && this.abierto()) {
      event.preventDefault();
      this.cerrar(true);
    }
  }

  manejarTeclasPanel(event: KeyboardEvent): void {
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      this.moverOpcion(event.key === 'ArrowDown' ? 1 : -1);
    }
    else if (event.key === 'Enter' && event.target instanceof HTMLInputElement) {
      const option = this.opcionesFiltradas[this.activeOptionIndex()];
      if (option) {
        event.preventDefault();
        this.seleccionar(option);
      }
    }
  }

  ngOnDestroy(): void {
    this.removerListenersViewport();
    if (this.positionFrame !== null) cancelAnimationFrame(this.positionFrame);
  }

  @HostListener('document:click', ['$event'])
  cerrarAlHacerClickFuera(event: MouseEvent): void {
    if (this.abierto() && !this.host.nativeElement.contains(event.target as Node)) this.cerrar();
  }

  @HostListener('document:keydown.escape')
  cerrarConEscape(): void {
    if (this.abierto()) this.cerrar(true);
  }

  private moverOpcion(delta: number): void {
    const opciones = this.opcionesFiltradas;
    if (!opciones.some(option => !option.disabled)) return;
    const current = this.activeOptionIndex();
    let next = current < 0 ? (delta > 0 ? 0 : opciones.length - 1) : Math.min(opciones.length - 1, Math.max(0, current + delta));
    while (opciones[next]?.disabled) {
      const candidate = next + delta;
      if (candidate < 0 || candidate >= opciones.length) return;
      next = candidate;
    }
    this.activeOptionIndex.set(next);
    requestAnimationFrame(() => {
      const id = this.optionId(opciones[next]);
      const option = document.getElementById(id);
      if (option && this.host.nativeElement.contains(option)) option.focus();
    });
  }

  private instalarListenersViewport(): void {
    if (this.listenersAttached || typeof document === 'undefined' || typeof window === 'undefined') return;
    document.addEventListener('scroll', this.viewportListener, true);
    window.addEventListener('resize', this.viewportListener, { passive: true });
    window.visualViewport?.addEventListener('resize', this.viewportListener, { passive: true });
    window.visualViewport?.addEventListener('scroll', this.viewportListener, { passive: true });
    this.listenersAttached = true;
  }

  private removerListenersViewport(): void {
    if (!this.listenersAttached || typeof document === 'undefined' || typeof window === 'undefined') return;
    document.removeEventListener('scroll', this.viewportListener, true);
    window.removeEventListener('resize', this.viewportListener);
    window.visualViewport?.removeEventListener('resize', this.viewportListener);
    window.visualViewport?.removeEventListener('scroll', this.viewportListener);
    this.listenersAttached = false;
  }

  private programarPosicionamiento(): void {
    if (!this.abierto() || typeof window === 'undefined') return;
    if (this.positionFrame !== null) cancelAnimationFrame(this.positionFrame);
    this.positionFrame = requestAnimationFrame(() => {
      this.positionFrame = null;
      const trigger = this.host.nativeElement.querySelector<HTMLElement>('[data-ui-bounded-select-trigger]');
      const panel = this.host.nativeElement.querySelector<HTMLElement>('[data-ui-bounded-select-panel]');
      if (!trigger || !panel) return;
      const anchor = trigger.getBoundingClientRect();
      const body = this.host.nativeElement.closest('.modal-body-scrollable')?.getBoundingClientRect();
      const placement = calculateBoundedSelectPlacement(
        anchor,
        { width: window.innerWidth, height: window.innerHeight },
        body,
        320
      );
      const lowerBound = Math.max(12, body?.top ?? 12);
      const upperBound = Math.min(window.innerHeight - 12, body?.bottom ?? window.innerHeight - 12);
      if (anchor.bottom <= lowerBound || anchor.top >= upperBound || placement.maxHeight < 80) {
        this.cerrar();
        return;
      }
      this.placement.set(placement);
      requestAnimationFrame(() => {
        const currentPanel = this.host.nativeElement.querySelector<HTMLElement>('[data-ui-bounded-select-panel]');
        if (!currentPanel || placement.direction !== 'up') return;
        const measuredHeight = currentPanel.getBoundingClientRect().height;
        this.placement.set({ ...placement, top: Math.max(lowerBound, anchor.top - 4 - measuredHeight) });
      });
    });
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
