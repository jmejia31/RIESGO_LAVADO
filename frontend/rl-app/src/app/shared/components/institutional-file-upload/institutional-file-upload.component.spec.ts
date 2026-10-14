import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InstitutionalFileUploadComponent } from './institutional-file-upload.component';
import { INSTITUTIONAL_EVIDENCE_FILE_POLICY } from './institutional-file-upload-policy';

describe('InstitutionalFileUploadComponent', () => {
  let fixture: ComponentFixture<InstitutionalFileUploadComponent>;
  let component: InstitutionalFileUploadComponent;
  let root: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [InstitutionalFileUploadComponent] }).compileComponents();
    fixture = TestBed.createComponent(InstitutionalFileUploadComponent);
    component = fixture.componentInstance;
    root = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  it('renderiza etiqueta configurable y la política real de evidencia', () => {
    fixture.componentRef.setInput('label', 'Evidencias del Control');
    fixture.detectChanges();
    expect(root.textContent).toContain('Evidencias del Control');
    expect(root.textContent).toContain('Arrastra archivos aquí o haz clic');
    expect(root.textContent).toContain('PDF, imágenes, Word, Excel (Máx. 10MB c/u)');
    expect(root.querySelector('input[type="file"]')?.getAttribute('accept')).toContain('.pdf');
    expect(root.querySelector('button')?.getAttribute('aria-describedby')).toContain('description');
    expect(component.formatFileSize(512)).toBe('512 B');
  });

  it('abre el selector con click, Enter y Space', () => {
    const input = root.querySelector('input[type="file"]') as HTMLInputElement;
    const click = vi.spyOn(input, 'click');
    const zone = root.querySelector('button') as HTMLButtonElement;
    zone.click();
    zone.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    zone.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
    expect(click).toHaveBeenCalledTimes(3);
  });

  it('emite archivos seleccionados por click y permite quitar y volver a seleccionar el mismo archivo', () => {
    const file = new File(['evidence'], 'evidence.pdf', { lastModified: 1 });
    const changes: File[][] = [];
    component.filesChange.subscribe(value => changes.push(value));
    component.onSelection({ target: { files: [file], value: 'selected' } } as unknown as Event);
    fixture.componentRef.setInput('selectedFiles', [file]);
    fixture.detectChanges();
    expect(changes[0]).toEqual([file]);
    expect(root.textContent).toContain('evidence.pdf');
    root.querySelector<HTMLButtonElement>('[aria-label="Quitar archivo evidence.pdf"]')!.click();
    fixture.detectChanges();
    component.onSelection({ target: { files: [file], value: 'selected' } } as unknown as Event);
    fixture.componentRef.setInput('selectedFiles', [file]);
    fixture.detectChanges();
    expect(changes.map(value => value.length)).toEqual([1, 0, 1]);
  });

  it('acepta drag/drop real y rechaza extensión inválida, archivo vacío y tamaño excedido', () => {
    const file = new File(['pdf'], 'drop.pdf');
    const changes: File[][] = [];
    const rejected: string[] = [];
    component.filesChange.subscribe(value => changes.push(value));
    component.fileRejected.subscribe(value => rejected.push(value));
    const event = new Event('drop', { bubbles: true, cancelable: true }) as DragEvent;
    Object.defineProperty(event, 'dataTransfer', { value: { files: [file] } });
    component.onDrop(event);
    expect(event.defaultPrevented).toBe(true);
    expect(changes.at(-1)).toEqual([file]);

    component.onDrop(Object.assign(new Event('drop'), { dataTransfer: { files: [new File(['x'], 'blocked.exe')] } }) as unknown as DragEvent);
    component.onDrop(Object.assign(new Event('drop'), { dataTransfer: { files: [new File([], 'empty.pdf')] } }) as unknown as DragEvent);
    component.policy = { ...INSTITUTIONAL_EVIDENCE_FILE_POLICY, maxFileSizeBytes: 2 };
    component.onDrop(Object.assign(new Event('drop'), { dataTransfer: { files: [new File(['large'], 'large.pdf')] } }) as unknown as DragEvent);
    expect(changes.at(-1)).toEqual([file]);
    expect(rejected).toHaveLength(3);
    expect(rejected[0]).toContain('formato permitido');
    expect(rejected[2]).toContain('supera el límite');
    expect(component.rejectionMessage()).toContain('supera el límite');
  });

  it('mantiene los archivos válidos ante selección mixta y bloquea duplicados/cantidad máxima', () => {
    component.policy = { ...INSTITUTIONAL_EVIDENCE_FILE_POLICY, maxFiles: 2 };
    const first = new File(['a'], 'a.pdf', { lastModified: 1 });
    const second = new File(['b'], 'b.pdf', { lastModified: 2 });
    const rejected: string[] = [];
    component.fileRejected.subscribe(value => rejected.push(value));
    component.onSelection({ target: { files: [first, new File(['x'], 'bad.exe')] } } as unknown as Event);
    component.onSelection({ target: { files: [first, second, new File(['c'], 'c.pdf', { lastModified: 3 })] } } as unknown as Event);
    expect(component.selectedFiles).toEqual([first, second]);
    expect(rejected.some(value => value.includes('ya está seleccionado'))).toBe(true);
    expect(rejected.some(value => value.includes('hasta 2 archivos'))).toBe(true);
  });

  it('single-file policy replaces selection and disabled state blocks click, keyboard, drop and remove', () => {
    component.multiple = false;
    const first = new File(['a'], 'a.pdf');
    const second = new File(['b'], 'b.pdf');
    component.onSelection({ target: { files: [first] } } as unknown as Event);
    component.onSelection({ target: { files: [second] } } as unknown as Event);
    expect(component.selectedFiles).toEqual([second]);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const input = root.querySelector('input[type="file"]') as HTMLInputElement;
    const click = vi.spyOn(input, 'click');
    component.onKeydown(new KeyboardEvent('keydown', { key: 'Enter' }), input);
    component.onDrop(Object.assign(new Event('drop'), { dataTransfer: { files: [first] } }) as unknown as DragEvent);
    component.removeFile(0);
    expect(click).not.toHaveBeenCalled();
    expect(component.selectedFiles).toEqual([second]);
    expect(root.querySelector('button')?.getAttribute('aria-disabled')).toBe('true');
  });
});
