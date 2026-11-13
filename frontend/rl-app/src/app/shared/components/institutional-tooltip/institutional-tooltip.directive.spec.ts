import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InstitutionalTooltipDirective } from './institutional-tooltip.directive';

@Component({
  standalone: true,
  imports: [InstitutionalTooltipDirective],
  template: `<div><button title="Acción de prueba">Acción</button><span role="tooltip">Tooltip legacy</span></div>`
})
class HostComponent {}

describe('InstitutionalTooltipDirective', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  it('oculta tooltips legacy y crea una capa accesible fuera del modal', () => {
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    const legacy = fixture.nativeElement.querySelector('[role="tooltip"]') as HTMLElement;
    button.dispatchEvent(new Event('mouseenter'));
    fixture.detectChanges();

    expect(legacy.hidden).toBe(true);
    expect(document.body.querySelector('[data-ui-tooltip="institutional"]')?.textContent).toBe('Acción de prueba');
    expect(button.getAttribute('aria-describedby')).toMatch(/^institutional-tooltip-/);
  });

  it('ignora el foco programático y muestra el tooltip con navegación de teclado', () => {
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.focus();
    fixture.detectChanges();
    expect(document.body.querySelector('[data-ui-tooltip="institutional"]')).toBeNull();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true }));
    button.dispatchEvent(new FocusEvent('focusin'));
    fixture.detectChanges();
    expect(document.body.querySelector('[data-ui-tooltip="institutional"]')).not.toBeNull();

    button.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();
    expect(document.body.querySelector('[data-ui-tooltip="institutional"]')).toBeNull();
  });
});
