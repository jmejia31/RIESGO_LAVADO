import { DOCUMENT } from '@angular/common';
import { Directive, ElementRef, HostListener, Inject, OnDestroy, Renderer2 } from '@angular/core';

let tooltipSequence = 0;

type InputModality = 'keyboard' | 'pointer' | 'programmatic';

interface InputModalityState {
  count: number;
  modality: InputModality;
  onPointerDown: () => void;
  onKeyDown: (event: KeyboardEvent) => void;
}

const inputModalityStates = new WeakMap<Document, InputModalityState>();
const keyboardNavigationKeys = new Set(['Tab', 'ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End']);

function registerInputModality(document: Document): void {
  const current = inputModalityStates.get(document);
  if (current) {
    current.count += 1;
    return;
  }

  const state: InputModalityState = {
    count: 1,
    modality: 'programmatic',
    onPointerDown: () => {
      state.modality = 'pointer';
    },
    onKeyDown: (event: KeyboardEvent) => {
      state.modality = keyboardNavigationKeys.has(event.key) ? 'keyboard' : 'programmatic';
    }
  };

  document.addEventListener('pointerdown', state.onPointerDown, true);
  document.addEventListener('keydown', state.onKeyDown, true);
  inputModalityStates.set(document, state);
}

function unregisterInputModality(document: Document): void {
  const state = inputModalityStates.get(document);
  if (!state) return;

  state.count -= 1;
  if (state.count > 0) return;

  document.removeEventListener('pointerdown', state.onPointerDown, true);
  document.removeEventListener('keydown', state.onKeyDown, true);
  inputModalityStates.delete(document);
}

function consumeKeyboardFocus(document: Document): boolean {
  const state = inputModalityStates.get(document);
  if (!state || state.modality !== 'keyboard') return false;
  state.modality = 'programmatic';
  return true;
}

@Directive({
  selector: 'button[title]',
  standalone: true
})
export class InstitutionalTooltipDirective implements OnDestroy {
  private readonly id = `institutional-tooltip-${++tooltipSequence}`;
  private tooltip: HTMLDivElement | null = null;
  private legacyTooltip: HTMLElement | null = null;
  private previousLegacyHidden = false;
  private hideTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly windowKeydown = (event: KeyboardEvent): void => {
    if (event.key === 'Escape') this.hide();
  };

  constructor(
    private readonly elementRef: ElementRef<HTMLButtonElement>,
    private readonly renderer: Renderer2,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
    registerInputModality(this.document);
    this.document.defaultView?.addEventListener('keydown', this.windowKeydown, true);
    this.prepareLegacyTooltip();
  }

  ngOnDestroy(): void {
    this.clearHideTimer();
    this.hide();
    if (this.legacyTooltip?.isConnected) {
      this.renderer.setProperty(this.legacyTooltip, 'hidden', this.previousLegacyHidden);
      this.renderer.removeAttribute(this.legacyTooltip, 'data-ui-tooltip-legacy');
    }
    this.document.defaultView?.removeEventListener('keydown', this.windowKeydown, true);
    unregisterInputModality(this.document);
  }

  @HostListener('mouseenter')
  onPointerEnter(): void {
    this.show();
  }

  @HostListener('mouseleave')
  onPointerLeave(): void {
    this.scheduleHide();
  }

  @HostListener('focusin')
  onFocusIn(): void {
    if (consumeKeyboardFocus(this.document)) this.show();
  }

  @HostListener('focusout', ['$event'])
  onFocusOut(event: FocusEvent): void {
    if (event.relatedTarget instanceof Node && this.elementRef.nativeElement.contains(event.relatedTarget)) return;
    this.scheduleHide();
  }

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') this.hide();
  }

  @HostListener('window:resize')
  onWindowResize(): void {
    if (this.tooltip) this.position();
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    if (this.tooltip) this.position();
  }

  private show(): void {
    this.clearHideTimer();
    this.prepareLegacyTooltip();
    const label = this.elementRef.nativeElement.getAttribute('title')?.trim();
    if (!label || this.elementRef.nativeElement.disabled) return;

    if (!this.tooltip) {
      this.tooltip = this.renderer.createElement('div') as HTMLDivElement;
      this.renderer.setAttribute(this.tooltip, 'id', this.id);
      this.renderer.setAttribute(this.tooltip, 'role', 'tooltip');
      this.renderer.setAttribute(this.tooltip, 'data-ui-tooltip', 'institutional');
      this.renderer.addClass(this.tooltip, 'institutional-tooltip-layer');
      this.renderer.appendChild(this.tooltip, this.document.createTextNode(label));
      this.renderer.appendChild(this.document.body, this.tooltip);
      this.renderer.setAttribute(this.elementRef.nativeElement, 'aria-describedby', this.id);
    }

    this.renderer.removeStyle(this.tooltip, 'visibility');
    this.position();
  }

  private hide(): void {
    this.clearHideTimer();
    if (!this.tooltip) return;
    this.renderer.removeChild(this.document.body, this.tooltip);
    this.renderer.removeAttribute(this.elementRef.nativeElement, 'aria-describedby');
    this.tooltip = null;
  }

  private scheduleHide(): void {
    this.clearHideTimer();
    this.hideTimer = setTimeout(() => this.hide(), 80);
  }

  private clearHideTimer(): void {
    if (this.hideTimer !== null) {
      clearTimeout(this.hideTimer);
      this.hideTimer = null;
    }
  }

  private position(): void {
    if (!this.tooltip) return;
    const trigger = this.elementRef.nativeElement.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const gap = 8;
    const margin = 8;
    const tooltipRect = this.tooltip.getBoundingClientRect();
    const placeBelow = trigger.bottom + gap + tooltipRect.height <= viewportHeight - margin || trigger.top < tooltipRect.height + gap + margin;
    const top = placeBelow
      ? Math.min(trigger.bottom + gap, viewportHeight - tooltipRect.height - margin)
      : Math.max(margin, trigger.top - tooltipRect.height - gap);
    const left = Math.min(
      Math.max(margin, trigger.left + (trigger.width - tooltipRect.width) / 2),
      Math.max(margin, viewportWidth - tooltipRect.width - margin)
    );

    this.renderer.setStyle(this.tooltip, 'top', `${Math.round(top)}px`);
    this.renderer.setStyle(this.tooltip, 'left', `${Math.round(left)}px`);
  }

  private findLegacyTooltip(): HTMLElement | null {
    const parent = this.elementRef.nativeElement.parentElement;
    if (!parent) return null;
    return Array.from(parent.children).find(
      child => child !== this.elementRef.nativeElement && child instanceof HTMLElement && child.getAttribute('role') === 'tooltip'
    ) as HTMLElement | null;
  }

  private prepareLegacyTooltip(): void {
    if (this.legacyTooltip) return;
    this.legacyTooltip = this.findLegacyTooltip();
    if (!this.legacyTooltip) return;
    this.previousLegacyHidden = this.legacyTooltip.hasAttribute('hidden');
    this.renderer.setProperty(this.legacyTooltip, 'hidden', true);
    this.renderer.setAttribute(this.legacyTooltip, 'data-ui-tooltip-legacy', 'hidden');
  }
}
