import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CampoFormulario } from '../../models/matrices-riesgos.models';
import { DynamicFieldRendererComponent } from './dynamic-field-renderer.component';

describe('DynamicFieldRendererComponent — controles dinámicos', () => {
  let fixture: ComponentFixture<DynamicFieldRendererComponent>;
  let component: DynamicFieldRendererComponent;

  const campo = (tipo: string, extra: Partial<CampoFormulario> = {}): CampoFormulario => ({
    clave: `campo_${tipo.replace(/[^a-z0-9]+/gi, '_')}`,
    etiqueta: `Campo ${tipo}`,
    tipo,
    obligatorio: false,
    soloLectura: false,
    ...extra
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DynamicFieldRendererComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(DynamicFieldRendererComponent);
    component = fixture.componentInstance;
  });

  function render(campoActual: CampoFormulario, valor: any = null): HTMLElement {
    component.campo = campoActual;
    component.valor = valor;
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('renderiza texto, número, fecha y texto largo con controles nativos correctos', () => {
    let host = render(campo('texto'));
    expect(host.querySelector('input[type="text"]')).not.toBeNull();

    host = render(campo('numero'));
    expect(host.querySelector('input[type="number"]')).not.toBeNull();

    host = render(campo('fecha'));
    expect(host.querySelector('input[type="date"]')).not.toBeNull();

    host = render(campo('texto-largo'));
    expect(host.querySelector('textarea')).not.toBeNull();
  });

  it('renderiza selector de catálogo y mantiene opción nula controlada', () => {
    component.opcionesCatalogo = [
      { codigo: 'A', valor: 'Alto' },
      { codigo: 'B', valor: 'Bajo' }
    ];
    const host = render(campo('selector-catalogo', { codigoCatalogo: 'NIVEL' }), 'B');
    const select = host.querySelector('select') as HTMLSelectElement;

    expect(select).not.toBeNull();
    expect(Array.from(select.options).map(o => o.textContent?.trim())).toEqual([
      'Seleccione una opción',
      'Alto',
      'Bajo'
    ]);
    expect(select.value).toBe('B');
  });

  it('renderiza radio con opciones inline y emite una selección exclusiva', () => {
    const emitSpy = spyOn(component.valorChange, 'emit');
    const host = render(campo('radio', { opciones: ['Sí', 'No'] }));
    const radios = host.querySelectorAll('input[type="radio"]');

    expect(radios.length).toBe(2);
    (radios[1] as HTMLInputElement).dispatchEvent(new Event('change', { bubbles: true }));
    fixture.detectChanges();

    expect(emitSpy).toHaveBeenCalledWith('No');
  });

  it('renderiza catálogo múltiple y emite string[] al marcar y desmarcar', () => {
    const emitSpy = spyOn(component.valorChange, 'emit');
    component.opcionesCatalogo = [
      { codigo: 'A', valor: 'A' },
      { codigo: 'B', valor: 'B' }
    ];
    const host = render(campo('catalogo-multiple', { codigoCatalogo: 'MULTI' }), ['A']);
    const checks = host.querySelectorAll('input[type="checkbox"]');

    expect(checks.length).toBe(2);
    expect((checks[0] as HTMLInputElement).checked).toBe(true);
    const segundo = checks[1] as HTMLInputElement;
    segundo.checked = true;
    segundo.dispatchEvent(new Event('change', { bubbles: true }));

    expect(emitSpy).toHaveBeenCalledWith(['A', 'B']);
  });

  it('renderiza checkbox simple preservando false como valor válido', () => {
    const host = render(campo('checkbox'), false);
    const checkbox = host.querySelector('input[type="checkbox"]') as HTMLInputElement;
    expect(checkbox).not.toBeNull();
    expect(checkbox.checked).toBe(false);
    expect(component.tieneValorActual).toBe(true);
  });

  it('renderiza fórmula exclusivamente en modo no editable', () => {
    const host = render(campo('formula', { formula: 'probabilidad * impacto', soloLectura: false }), 12);
    expect(host.querySelector('input, textarea, select')).toBeNull();
    expect(host.textContent).toContain('12');
    expect(host.textContent).toContain('probabilidad * impacto');
  });

  it('bloquea tipos desconocidos y expone el tipo original sin crear input editable', () => {
    const host = render(campo('desconocido', { tipoOriginal: 'widget-v2', soloLectura: true }), 'histórico');
    expect(host.querySelector('input, textarea, select')).toBeNull();
    expect(host.textContent).toContain('widget-v2');
    expect(host.textContent).toContain('histórico');
  });

  it('muestra un estado controlado cuando selector, radio o catálogo múltiple no tienen opciones', () => {
    let host = render(campo('selector-catalogo'));
    expect(host.textContent).toContain('No hay opciones disponibles');

    host = render(campo('radio'));
    expect(host.textContent).toContain('No hay opciones configuradas');

    host = render(campo('catalogo-multiple'));
    expect(host.textContent).toContain('No hay opciones configuradas');
  });

  it('respeta required y aria-required en controles editables', () => {
    const host = render(campo('texto', { obligatorio: true }));
    const input = host.querySelector('input') as HTMLInputElement;
    expect(input.required).toBe(true);
    expect(input.getAttribute('aria-required')).toBe('true');
    expect(host.textContent).toContain('*');
  });

  it('respeta soloLectura sin convertir el control en otro tipo', () => {
    const host = render(campo('texto', { soloLectura: true }), 'dato');
    const input = host.querySelector('input[type="text"]') as HTMLInputElement;
    expect(input.readOnly).toBe(true);
    expect(input.value).toBe('dato');
  });

  it('muestra 0 y false correctamente en modo lectura', () => {
    component.modoLectura = true;
    let host = render(campo('numero'), 0);
    expect(host.textContent).toContain('0');
    expect(host.textContent).not.toContain('> - <');

    host = render(campo('checkbox'), false);
    expect(host.textContent).toContain('No');
  });

  it('muestra etiquetas de catálogo en modo lectura sin perder el código desconocido', () => {
    component.modoLectura = true;
    component.opcionesCatalogo = [{ codigo: 'A', valor: 'Alto' }];
    let host = render(campo('selector-catalogo'), 'A');
    expect(host.textContent).toContain('Alto');

    host = render(campo('selector-catalogo'), 'Z');
    expect(host.textContent).toContain('Z');
  });

  it('muestra selección múltiple como etiquetas de catálogo en modo lectura', () => {
    component.modoLectura = true;
    component.opcionesCatalogo = [
      { codigo: 'A', valor: 'Alta' },
      { codigo: 'B', valor: 'Baja' }
    ];
    const host = render(campo('catalogo-multiple'), ['A', 'B']);
    expect(host.textContent).toContain('Alta, Baja');
  });

  it('genera ids estables y saneados para label/control', () => {
    const host = render(campo('texto', { clave: 'campo con / caracteres' }));
    const input = host.querySelector('input') as HTMLInputElement;
    const label = host.querySelector('label') as HTMLLabelElement;

    expect(input.id).toBe('campo-campo-con-caracteres');
    expect(label.htmlFor).toBe(input.id);
  });
});
