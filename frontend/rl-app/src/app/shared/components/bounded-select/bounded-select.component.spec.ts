import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BoundedSelectComponent } from './bounded-select.component';

describe('BoundedSelectComponent', () => {
  let fixture: ComponentFixture<BoundedSelectComponent>;
  let component: BoundedSelectComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [BoundedSelectComponent] }).compileComponents();
    fixture = TestBed.createComponent(BoundedSelectComponent);
    component = fixture.componentInstance;
    component.ariaLabel = 'Evaluación';
    component.emptyValue = 0;
    component.value = 0;
    component.options = Array.from({ length: 30 }, (_, index) => ({
      value: index + 1,
      label: `#${index + 1} · Riesgo ${100 + index} · BORRADOR`,
      searchText: `RIE-${index + 1}`
    }));
    fixture.detectChanges();
  });

  it('abre exclusivamente hacia abajo con panel acotado y scroll interno estándar', () => {
    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-ui-bounded-select-trigger]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(root.querySelector('[data-ui-bounded-select]')?.getAttribute('data-ui-dropdown-direction')).toBe('down');
    expect(root.querySelector('[data-ui-bounded-select-panel]')).not.toBeNull();
    expect(root.querySelector('[data-ui-bounded-select-scroll]')).not.toBeNull();
    expect(root.querySelector('[data-ui-bounded-select-count]')?.textContent).toContain('30 de 30 opciones');
  });

  it('filtra una lista extensa sin perder el valor original', () => {
    component.abrir();
    fixture.detectChanges();
    const input = (fixture.nativeElement as HTMLElement).querySelector('[data-ui-bounded-select-search]') as HTMLInputElement;
    input.value = 'RIE-27';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.opcionesFiltradas).toHaveLength(1);
    expect(component.opcionesFiltradas[0].value).toBe(27);
    expect(component.value).toBe(0);
  });

  it('emite la selección y cierra el panel', () => {
    const valores: Array<string | number | null> = [];
    component.valueChange.subscribe(value => valores.push(value));
    component.abrir();
    component.seleccionar(component.options[19]);

    expect(valores).toEqual([20]);
    expect(component.abierto()).toBe(false);
  });

  it('emite el valor vacío al elegir el placeholder', () => {
    const valores: Array<string | number | null> = [];
    component.valueChange.subscribe(value => valores.push(value));
    component.abrir();
    component.seleccionarVacio();

    expect(valores).toEqual([0]);
    expect(component.abierto()).toBe(false);
  });
});
