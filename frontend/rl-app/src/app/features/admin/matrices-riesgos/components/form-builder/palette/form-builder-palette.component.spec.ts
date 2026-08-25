import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderPaletteComponent } from './form-builder-palette.component';
import { TIPOS_CONTROLES_DISPONIBLES, SeccionBuilderModel } from '../../../models/form-builder.models';

describe('FormBuilderPaletteComponent — UI-FORM.2 Biblioteca de Campos', () => {
  let component: FormBuilderPaletteComponent;
  let fixture: ComponentFixture<FormBuilderPaletteComponent>;

  const mockSecciones: SeccionBuilderModel[] = [
    {
      id: 'sec_1',
      clave: 'datos_generales',
      titulo: 'Datos Generales',
      orden: 1,
      columnasPorFila: 2,
      campos: [
        { id: 'cmp_1', clave: 'nombre', etiqueta: 'Nombre Completo', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 }
      ]
    },
    {
      id: 'sec_2',
      clave: 'evaluacion',
      titulo: 'Evaluación Técnica',
      orden: 2,
      columnasPorFila: 1,
      campos: []
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderPaletteComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderPaletteComponent);
    component = fixture.componentInstance;
    component.tiposControles = [...TIPOS_CONTROLES_DISPONIBLES];
    component.secciones = [...mockSecciones];
    component.soloLectura = false;
    component.seccionActivaId = 'sec_1';
    fixture.detectChanges();
  });

  it('1. renderiza las tres categorías oficiales: Básicos, Selección y Avanzados', () => {
    const el = fixture.nativeElement as HTMLElement;
    const titulos = Array.from(el.querySelectorAll('h4')).map(h => h.textContent?.trim());
    expect(titulos).toContain('Básicos');
    expect(titulos).toContain('Selección');
    expect(titulos).toContain('Avanzados');
  });

  it('2. agrupa la cantidad exacta de controles por categoría (4 Básicos, 4 Selección, 1 Avanzado = 9 total)', () => {
    expect(component.controlesBasicos().length).toBe(4);
    expect(component.controlesSeleccion().length).toBe(4);
    expect(component.controlesAvanzados().length).toBe(1);
    expect(component.totalFiltrados()).toBe(9);
  });

  it('3. busca en tiempo real por etiqueta (nombre)', () => {
    component.terminoBusqueda.set('Texto largo');
    fixture.detectChanges();

    expect(component.totalFiltrados()).toBe(1);
    expect(component.controlesFiltrados()[0].tipo).toBe('texto-largo');
  });

  it('4. busca en tiempo real por descripción', () => {
    component.terminoBusqueda.set('múltiples líneas');
    fixture.detectChanges();

    expect(component.totalFiltrados()).toBe(1);
    expect(component.controlesFiltrados()[0].tipo).toBe('texto-largo');
  });

  it('5. busca en tiempo real por tipo técnico', () => {
    component.terminoBusqueda.set('selector-catalogo');
    fixture.detectChanges();

    expect(component.totalFiltrados()).toBe(1);
    expect(component.controlesFiltrados()[0].etiqueta).toBe('Lista desplegable');
  });

  it('6. busca por categoría normalizada para devolver el grupo completo', () => {
    component.terminoBusqueda.set('seleccion');
    fixture.detectChanges();

    expect(component.controlesSeleccion().length).toBe(4);
    expect(component.controlesBasicos().length).toBe(0);
    expect(component.controlesAvanzados().length).toBe(0);
  });

  it('7. búsqueda es case-insensitive', () => {
    component.terminoBusqueda.set('NUMERO');
    fixture.detectChanges();

    expect(component.totalFiltrados()).toBe(1);
    expect(component.controlesFiltrados()[0].tipo).toBe('numero');
  });

  it('8. búsqueda es tolerante a acentos ortográficos (NFD normalization)', () => {
    component.terminoBusqueda.set('numero');
    fixture.detectChanges();
    expect(component.totalFiltrados()).toBe(1);
    expect(component.controlesFiltrados()[0].tipo).toBe('numero');

    component.terminoBusqueda.set('formula');
    fixture.detectChanges();
    expect(component.totalFiltrados()).toBe(1);
    expect(component.controlesFiltrados()[0].tipo).toBe('formula');
  });

  it('9. muestra empty state profesional si no hay coincidencias', () => {
    component.terminoBusqueda.set('control-inexistente-xyz');
    fixture.detectChanges();

    expect(component.totalFiltrados()).toBe(0);
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('No se encontraron campos compatibles');
  });

  it('10. limpiar búsqueda restaura todos los controles', () => {
    component.terminoBusqueda.set('fecha');
    fixture.detectChanges();
    expect(component.totalFiltrados()).toBe(1);

    component.limpiarBusqueda();
    fixture.detectChanges();
    expect(component.totalFiltrados()).toBe(9);
    expect(component.terminoBusqueda()).toBe('');
  });

  it('11. click en tarjeta emite agregarCampo cuando hay seccionActivaId', () => {
    let emitido: unknown = null;
    component.agregarCampo.subscribe(ctrl => emitido = ctrl);

    const el = fixture.nativeElement as HTMLElement;
    const card = el.querySelector('.palette-card') as HTMLElement;
    card.click();

    expect(emitido).toBeTruthy();
  });

  it('12. click en tarjeta no emite agregarCampo si seccionActivaId es null', () => {
    component.seccionActivaId = null;
    fixture.detectChanges();

    let emitido = false;
    component.agregarCampo.subscribe(() => emitido = true);

    const el = fixture.nativeElement as HTMLElement;
    const card = el.querySelector('.palette-card') as HTMLElement;
    card.click();

    expect(emitido).toBe(false);
  });

  it('13. teclado Enter y Space emiten agregarCampo de forma accesible', () => {
    let emitidos = 0;
    component.agregarCampo.subscribe(() => emitidos++);

    const el = fixture.nativeElement as HTMLElement;
    const card = el.querySelector('.palette-card') as HTMLElement;

    card.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    expect(emitidos).toBe(1);

    card.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
    expect(emitidos).toBe(2);
  });

  it('14. dragstart transporta exclusivamente el identificador de tipo seguro', () => {
    const dataTransferMap = new Map<string, string>();
    const mockDataTransfer = {
      setData: (format: string, data: string) => dataTransferMap.set(format, data),
      getData: (format: string) => dataTransferMap.get(format) || '',
      effectAllowed: ''
    } as unknown as DataTransfer;

    const dragEvent = {
      type: 'dragstart',
      dataTransfer: mockDataTransfer,
      preventDefault: vi.fn()
    } as unknown as DragEvent;

    const ctrl = TIPOS_CONTROLES_DISPONIBLES[0]; // texto
    component.onDragStart(dragEvent, ctrl);

    expect(dataTransferMap.get('text/plain')).toBe('texto');
    expect(dataTransferMap.get('application/x-form-builder-control')).toBe('texto');
    expect(mockDataTransfer.effectAllowed).toBe('copy');
  });

  it('15. dragstart es cancelado si soloLectura es true o seccionActivaId es null', () => {
    component.soloLectura = true;
    const preventDefaultSpy = vi.fn();
    const dragEvent = {
      type: 'dragstart',
      preventDefault: preventDefaultSpy
    } as unknown as DragEvent;

    component.onDragStart(dragEvent, TIPOS_CONTROLES_DISPONIBLES[0]);
    expect(preventDefaultSpy).toHaveBeenCalled();
  });

  it('16. modo soloLectura conserva la biblioteca visual y bloquea la interacción', () => {
    component.soloLectura = true;
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Agregar campos');
    expect(el.querySelector('#buscador-palette')).not.toBeNull();
    expect(el.querySelectorAll('.palette-card').length).toBe(9);
    expect(el.querySelectorAll('.palette-card[draggable="true"]').length).toBe(0);
    expect(el.querySelectorAll('.palette-card[aria-disabled="true"]').length).toBe(9);
  });

  it('17. modo soloLectura no emite agregarCampo por click ni teclado', () => {
    component.soloLectura = true;
    fixture.detectChanges();

    let emitidos = 0;
    component.agregarCampo.subscribe(() => emitidos++);

    const el = fixture.nativeElement as HTMLElement;
    const card = el.querySelector('.palette-card') as HTMLElement;
    card.click();
    card.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

    expect(emitidos).toBe(0);
  });
});
