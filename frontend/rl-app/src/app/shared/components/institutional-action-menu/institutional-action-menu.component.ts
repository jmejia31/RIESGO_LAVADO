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
import { InstitutionalOverlayRegistry } from '../institutional-overlay/institutional-overlay-registry.service';
import {
  InstitutionalOverlayPlacement,
  calculateInstitutionalOverlayPlacement
} from '../institutional-overlay/institutional-overlay-position.util';

@Component({
  selector: 'app-institutional-action-menu',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="institutional-action-menu" data-ui-action-menu [attr.data-ui-dropdown-direction]="placement()?.direction ?? 'closed'">
      <button
        type="button"
        [id]="triggerId"
        [class]="triggerClass"
        [disabled]="disabled"
        [attr.aria-label]="ariaLabel"
        [attr.aria-expanded]="abierto()"
        [attr.aria-controls]="panelId"
        aria-haspopup="menu"
        data-ui-action-menu-trigger
        (click)="alternar($event)"
        (keydown.escape)="cerrar(true)">
        <ng-content select="[action-menu-trigger]"></ng-content>
      </button>

      <div
        [id]="panelId"
        [class]="panelClass"
        role="menu"
        [hidden]="!abierto()"
        [style.left.px]="placement()?.left"
        [style.top.px]="placement()?.top"
        [style.width.px]="placement()?.width"
        [style.max-height.px]="placement()?.maxHeight"
        data-ui-action-menu-panel
        (click)="manejarClickMenu($event)"
        (keydown)="manejarTeclasMenu($event)">
        <ng-content select="[action-menu-content]"></ng-content>
      </div>
    </div>
  `,
  styles: [`
    :host { display: inline-block; min-width: 0; }
    .institutional-action-menu { position: relative; min-width: 0; }
    [data-ui-action-menu-panel] {
      position: fixed;
      z-index: 1100;
      display: grid;
      min-width: 12rem;
      max-width: calc(100vw - 24px);
      gap: 0.25rem;
      overflow-y: auto;
      overscroll-behavior: contain;
      border: 1px solid rgb(226 232 240);
      border-radius: 0.75rem;
      background: white;
      padding: 0.35rem;
      box-shadow: 0 12px 28px rgb(15 23 42 / 0.14);
    }
    [data-ui-action-menu-panel] ::ng-deep [data-ui-action-menu-item] {
      min-height: 2.5rem;
      border-radius: 0.5rem;
      padding: 0.55rem 0.7rem;
      text-align: left;
    }
    [data-ui-action-menu-panel] ::ng-deep [data-ui-action-menu-item]:focus-visible {
      outline: 3px solid rgb(var(--ihss-accent-rgb) / 0.35);
      outline-offset: 2px;
    }
  `]
})
export class InstitutionalActionMenuComponent implements OnDestroy {
  private static nextId = 0;
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly registry = inject(InstitutionalOverlayRegistry);
  private readonly handle = { close: () => this.cerrar() };
  private positionFrame: number | null = null;
  private listenersAttached = false;

  @Input() ariaLabel = 'Acciones';
  @Input() disabled = false;
  @Input() triggerClass = 'institutional-action-menu__trigger';
  @Input() panelClass = 'institutional-action-menu__panel';
  @Input() panelWidth = 192;
  @Input() preferredHeight = 280;
  @Input() triggerId = `institutional-action-menu-trigger-${InstitutionalActionMenuComponent.nextId++}`;
  @Output() openChange = new EventEmitter<boolean>();

  readonly abierto = signal(false);
  readonly placement = signal<InstitutionalOverlayPlacement | null>(null);
  readonly panelId = `${this.triggerId}-panel`;

  constructor() {
    this.registry.register(this.handle);
  }

  alternar(event?: Event): void {
    event?.stopPropagation();
    if (this.disabled) return;
    this.abierto() ? this.cerrar(true) : this.abrir();
  }

  abrir(): void {
    if (this.disabled || this.abierto()) return;
    this.registry.activate(this.handle);
    this.abierto.set(true);
    this.openChange.emit(true);
    this.instalarListenersViewport();
    this.programarPosicionamiento();
    requestAnimationFrame(() => this.enfocarPrimerElemento());
  }

  cerrar(devolverFoco = false): void {
    if (!this.abierto()) return;
    this.abierto.set(false);
    this.placement.set(null);
    this.removerListenersViewport();
    this.openChange.emit(false);
    if (devolverFoco) requestAnimationFrame(() => this.host.nativeElement.querySelector<HTMLButtonElement>('[data-ui-action-menu-trigger]')?.focus());
  }

  ngOnDestroy(): void {
    this.registry.unregister(this.handle);
    this.removerListenersViewport();
    if (this.positionFrame !== null) cancelAnimationFrame(this.positionFrame);
  }

  @HostListener('document:click', ['$event'])
  cerrarAlHacerClickFuera(event: MouseEvent): void {
    if (this.abierto() && !this.host.nativeElement.contains(event.target as Node)) this.cerrar(true);
  }

  @HostListener('document:keydown.escape')
  cerrarConEscape(): void {
    if (this.abierto()) this.cerrar(true);
  }

  private enfocarPrimerElemento(): void {
    this.host.nativeElement.querySelector<HTMLElement>('[data-ui-action-menu-panel] [data-ui-action-menu-item]:not(:disabled)')?.focus();
  }

  manejarClickMenu(event: MouseEvent): void {
    event.stopPropagation();
    if ((event.target as HTMLElement | null)?.closest('[data-ui-action-menu-item]')) this.cerrar(true);
  }

  manejarTeclasMenu(event: KeyboardEvent): void {
    const items = Array.from(this.host.nativeElement.querySelectorAll<HTMLElement>('[data-ui-action-menu-panel] [data-ui-action-menu-item]:not(:disabled)'));
    if (event.key === 'Escape') {
      event.preventDefault();
      this.cerrar(true);
      return;
    }
    if (!items.length || !['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;
    event.preventDefault();
    const current = items.indexOf(document.activeElement as HTMLElement);
    const delta = event.key === 'ArrowUp' ? -1 : 1;
    const next = event.key === 'Home' ? 0 : event.key === 'End' ? items.length - 1 : (current + delta + items.length) % items.length;
    items[next]?.focus();
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

  private readonly viewportListener = (): void => this.programarPosicionamiento();

  private programarPosicionamiento(): void {
    if (!this.abierto() || typeof window === 'undefined') return;
    if (this.positionFrame !== null) cancelAnimationFrame(this.positionFrame);
    this.positionFrame = requestAnimationFrame(() => {
      this.positionFrame = null;
      const trigger = this.host.nativeElement.querySelector<HTMLElement>('[data-ui-action-menu-trigger]');
      if (!trigger) return;
      const anchor = trigger.getBoundingClientRect();
      const placement = calculateInstitutionalOverlayPlacement(
        anchor,
        { width: window.innerWidth, height: window.innerHeight },
        undefined,
        this.preferredHeight,
        this.panelWidth
      );
      this.placement.set(placement);
    });
  }
}
