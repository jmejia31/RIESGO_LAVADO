import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderCanvasComponent } from './form-builder-canvas.component';
import { SeccionBuilderModel } from '../../../models/form-builder.models';

describe('FormBuilderCanvasComponent — UI-FORM.2 Drag & Drop en Lienzo', () => {
  let component: FormBuilderCanvasComponent;
  let fixture: ComponentFixture<FormBuilderCanvasComponent>;

  const mockSecciones: SeccionBuilderModel[] = [
    {
      id: 'sec_1',
      clave: 'datos_principales',
      titulo: 'Datos Principales',
      orden: 1,
      columnasPorFila: 2,
      campos: [
        { id: 'cmp_1', clave: 'nombre', etiqueta: 'Nombre', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 }
      ]
    },
    {
      id: 'sec_2',
      clave: 'evaluacion_riesgo',
      titulo: 'Evaluación de Riesgo',
      orden: 2,
      columnasPorFila: 1,
      campos: []
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderCanvasComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderCanvasComponent);
    component = fixture.componentInstance;
    component.secciones = [...mockSecciones];
    component.soloLectura = false;
    component.seccionActivaId = 'sec_1';
    fixture.detectChanges();
  });

  it('1. dragover sobre una sección activa el estado visual de recepción (drop-zone)', () => {
    const preventDefaultSpy = vi.fn();
    const dragEvent = {
      type: 'dragover',
      preventDefault: preventDefaultSpy,
      dataTransfer: { dropEffect: '' }
    } as unknown as DragEvent;

    component.onDragOver(dragEvent, 'sec_1');
    fixture.detectChanges();

    expect(preventDefaultSpy).toHaveBeenCalled();
    expect(component.seccionArrastreSobreId()).toBe('sec_1');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.drop-zone')).toBeTruthy();
    expect(el.textContent).toContain('Suelta el campo en «Datos Principales»');
  });

  it('2. dragleave desactiva el estado visual de recepción', () => {
    component.seccionArrastreSobreId.set('sec_1');
    fixture.detectChanges();

    const dragEvent = {
      type: 'dragleave',
      currentTarget: null,
      relatedTarget: null
    } as unknown as DragEvent;

    component.onDragLeave(dragEvent, 'sec_1');
    fixture.detectChanges();

    expect(component.seccionArrastreSobreId()).toBeNull();
  });

  it('3. drop emite soltarControl con seccionId y tipo seguro extraído del dataTransfer', () => {
    let eventoSoltado: { seccionId: string; tipo: string } | null = null;
    component.soltarControl.subscribe(e => eventoSoltado = e);

    const mockDataTransfer = {
      getData: (format: string) => format === 'application/x-form-builder-control' ? 'numero' : ''
    } as unknown as DataTransfer;

    const dropEvent = {
      type: 'drop',
      preventDefault: vi.fn(),
      dataTransfer: mockDataTransfer
    } as unknown as DragEvent;

    component.onDrop(dropEvent, 'sec_1');

    expect(eventoSoltado).toEqual({ seccionId: 'sec_1', tipo: 'numero' });
    expect(component.seccionArrastreSobreId()).toBeNull();
  });

  it('4. drop sobre sección B emite correctamente con seccionId de la sección B', () => {
    let eventoSoltado: { seccionId: string; tipo: string } | null = null;
    component.soltarControl.subscribe(e => eventoSoltado = e);

    const mockDataTransfer = {
      getData: (format: string) => format === 'text/plain' ? 'fecha' : ''
    } as unknown as DataTransfer;

    const dropEvent = {
      type: 'drop',
      preventDefault: vi.fn(),
      dataTransfer: mockDataTransfer
    } as unknown as DragEvent;

    component.onDrop(dropEvent, 'sec_2');

    expect(eventoSoltado).toEqual({ seccionId: 'sec_2', tipo: 'fecha' });
  });

  it('5. drop en modo soloLectura es ignorado y no emite soltarControl', () => {
    component.soloLectura = true;
    let emitido = false;
    component.soltarControl.subscribe(() => emitido = true);

    const mockDataTransfer = {
      getData: () => 'texto'
    } as unknown as DataTransfer;

    const dropEvent = {
      type: 'drop',
      preventDefault: vi.fn(),
      dataTransfer: mockDataTransfer
    } as unknown as DragEvent;

    component.onDrop(dropEvent, 'sec_1');

    expect(emitido).toBe(false);
  });
});
