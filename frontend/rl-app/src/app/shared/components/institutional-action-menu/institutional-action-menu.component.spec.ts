import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { InstitutionalActionMenuComponent } from './institutional-action-menu.component';

@Component({
  standalone: true,
  imports: [InstitutionalActionMenuComponent],
  template: `<app-institutional-action-menu ariaLabel="Acciones de prueba"><span action-menu-trigger>Acciones</span><button action-menu-content data-ui-action-menu-item type="button">Primera acción</button></app-institutional-action-menu>`
})
class HostComponent {}

describe('InstitutionalActionMenuComponent', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('abre y cierra mediante el disparador y expone el contrato accesible', () => {
    const root = fixture.nativeElement as HTMLElement;
    const trigger = root.querySelector('[data-ui-action-menu-trigger]') as HTMLButtonElement;
    trigger.click();
    fixture.detectChanges();

    expect(trigger.getAttribute('aria-expanded')).toBe('true');
    expect(root.querySelector('[data-ui-action-menu-panel]')).not.toBeNull();
    expect(root.querySelector('[data-ui-action-menu-item]')).not.toBeNull();

    trigger.click();
    fixture.detectChanges();
    expect(trigger.getAttribute('aria-expanded')).toBe('false');
  });

  it('cierra con Escape y con clic externo', () => {
    const root = fixture.nativeElement as HTMLElement;
    const trigger = root.querySelector('[data-ui-action-menu-trigger]') as HTMLButtonElement;
    trigger.click();
    fixture.detectChanges();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();
    expect((root.querySelector('[data-ui-action-menu-panel]') as HTMLElement).hidden).toBe(true);

    trigger.click();
    fixture.detectChanges();
    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();
    expect((root.querySelector('[data-ui-action-menu-panel]') as HTMLElement).hidden).toBe(true);
  });

  it('cierra al seleccionar una acción y permite recorrerla con teclado', () => {
    const root = fixture.nativeElement as HTMLElement;
    const trigger = root.querySelector('[data-ui-action-menu-trigger]') as HTMLButtonElement;
    trigger.click();
    fixture.detectChanges();
    const item = root.querySelector('[data-ui-action-menu-item]') as HTMLButtonElement;
    item.dispatchEvent(new KeyboardEvent('keydown', { key: 'Home', bubbles: true, cancelable: true }));
    fixture.detectChanges();
    expect(document.activeElement).toBe(item);
    item.click();
    fixture.detectChanges();
    expect((root.querySelector('[data-ui-action-menu-panel]') as HTMLElement).hidden).toBe(true);
  });
});
