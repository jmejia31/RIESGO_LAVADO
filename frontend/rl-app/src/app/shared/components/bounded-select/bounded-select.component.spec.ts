import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BoundedSelectComponent, calculateBoundedSelectPlacement } from './bounded-select.component';

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

  it('abre con un panel controlado y scroll interno', () => {
    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('[data-ui-bounded-select-trigger]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(root.querySelector('[data-ui-bounded-select-panel]')).not.toBeNull();
    expect(root.querySelector('[data-ui-bounded-select-scroll]')).not.toBeNull();
    expect(root.querySelector('[data-ui-bounded-select-count]')?.textContent).toContain('30 de 30 opciones');
  });

  it('elige arriba cuando el cuerpo del modal no tiene espacio debajo y mantiene el panel dentro de sus límites', () => {
    const placement = calculateBoundedSelectPlacement(
      { left: 220, right: 660, top: 600, bottom: 640, width: 440 },
      { width: 1280, height: 720 },
      { left: 24, right: 1256, top: 120, bottom: 680 }
    );

    expect(placement.direction).toBe('up');
    expect(placement.top).toBeGreaterThanOrEqual(120);
    expect(placement.top + placement.maxHeight).toBeLessThanOrEqual(596);
    expect(placement.left).toBeGreaterThanOrEqual(12);
    expect(placement.left + placement.width).toBeLessThanOrEqual(1268);
  });

  it('limita ancho y alto a viewport aunque el ancla esté junto al borde', () => {
    const placement = calculateBoundedSelectPlacement(
      { left: 350, right: 590, top: 150, bottom: 190, width: 240 },
      { width: 390, height: 844 },
      { left: 0, right: 390, top: 80, bottom: 760 }
    );

    expect(placement.direction).toBe('down');
    expect(placement.left).toBeGreaterThanOrEqual(12);
    expect(placement.left + placement.width).toBeLessThanOrEqual(378);
    expect(placement.top + placement.maxHeight).toBeLessThanOrEqual(748);
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

  it('navega opciones con flechas y confirma el valor numérico con Enter', () => {
    const valores: Array<string | number | null> = [];
    component.valueChange.subscribe(value => valores.push(value));
    const event = new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true });
    component.manejarTeclasCombobox(event);
    expect(event.defaultPrevented).toBe(true);
    expect(component.abierto()).toBe(true);
    expect(component.activeOptionIndex()).toBe(0);

    const enter = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
    component.manejarTeclasCombobox(enter);
    expect(valores).toEqual([1]);
    expect(component.abierto()).toBe(false);
  });

  it('omite opciones deshabilitadas durante la navegación por teclado', () => {
    component.options = [
      { value: 1, label: 'Disponible' },
      { value: 2, label: 'No disponible', disabled: true },
      { value: 3, label: 'Siguiente disponible' }
    ];
    component.abrir();
    component.manejarTeclasCombobox(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
    component.manejarTeclasCombobox(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true, cancelable: true }));
    expect(component.activeOptionIndex()).toBe(2);
  });

  it('abre desde el teclado con ArrowUp y activa la última opción disponible', () => {
    component.manejarTeclasCombobox(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true, cancelable: true }));
    expect(component.abierto()).toBe(true);
    expect(component.activeOptionIndex()).toBe(component.options.length - 1);
  });
});
