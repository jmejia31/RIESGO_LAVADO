import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, beforeEach } from 'vitest';
import { FormBuilderToolbarComponent } from './toolbar/form-builder-toolbar.component';
import { FormBuilderStatusbarComponent } from './statusbar/form-builder-statusbar.component';

describe('Form Builder UI-FORM.5 visual state reconciliation', () => {
  let toolbar: ComponentFixture<FormBuilderToolbarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderToolbarComponent, FormBuilderStatusbarComponent]
    }).compileComponents();
    toolbar = TestBed.createComponent(FormBuilderToolbarComponent);
  });

  it('keeps the Editor Visual affordance in the secondary toolbar level', () => {
    toolbar.componentInstance.vistaActiva = 'secciones';
    toolbar.detectChanges();

    const button = (toolbar.nativeElement as HTMLElement).querySelector('#tab-editor-visual') as HTMLButtonElement;
    expect(button).toBeTruthy();
    expect(button.textContent).toContain('Editor Visual');
    expect(button.className).toContain('border-ihss-600');
    expect(button.getAttribute('aria-current')).toBe('page');
    expect((toolbar.nativeElement as HTMLElement).querySelector('#tab-configuracion-general')).toBeNull();
  });

  it('marks Preview as the only active navigation tab', () => {
    toolbar.componentInstance.vistaActiva = 'preview';
    toolbar.detectChanges();

    const element = toolbar.nativeElement as HTMLElement;
    expect(element.querySelector('#tab-vista-preview')?.getAttribute('aria-current')).toBe('page');
    expect(element.querySelector('#tab-editor-visual')?.getAttribute('aria-current')).toBeNull();
    expect(element.querySelector('#tab-configuracion-general')).toBeNull();
  });

  it('exposes only the real contextual action for the active editor view', () => {
    toolbar.componentInstance.soloLectura = false;
    toolbar.componentInstance.vistaActiva = 'secciones';
    toolbar.detectChanges();

    const element = toolbar.nativeElement as HTMLElement;
    expect(element.querySelector('#btn-agregar-seccion')).toBeTruthy();
    expect(element.querySelector('#btn-nuevo-catalogo-header')).toBeNull();
    expect(element.querySelector('#btn-publicar-builder')).toBeNull();
  });

  it('renders the authoritative published state in the statusbar', () => {
    const fixture = TestBed.createComponent(FormBuilderStatusbarComponent);
    fixture.componentInstance.versionCodigo = 'V3.0';
    fixture.componentInstance.estadoVersion = 'PUBLISHED';
    fixture.componentInstance.soloLectura = true;
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Publicada · Solo lectura');
  });

  it('declares the statusbar as the institutional footer region and exposes textual actions', () => {
    const fixture = TestBed.createComponent(FormBuilderStatusbarComponent);
    fixture.componentInstance.soloLectura = false;
    fixture.componentInstance.puedeGuardar = true;
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const footer = root.querySelector('footer');
    expect(footer?.classList.contains('modal-footer-institutional')).toBe(true);
    expect(footer?.getAttribute('data-modal-shell-region')).toBe('footer');
    expect(root.querySelector('.form-builder-statusbar__cancel')?.textContent).toContain('Cancelar');
    expect(root.querySelector('.form-builder-statusbar__save')?.textContent).toContain('Guardar');
  });

  it('provides a textual close action and no ineffective save action in read-only mode', () => {
    const fixture = TestBed.createComponent(FormBuilderStatusbarComponent);
    fixture.componentInstance.soloLectura = true;
    fixture.componentInstance.puedeGuardar = false;
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const close = root.querySelector('.form-builder-statusbar__cancel') as HTMLButtonElement;
    expect(close.getAttribute('aria-label')).toBe('Cerrar constructor');
    expect(close.textContent).toContain('Cerrar');
    expect(root.querySelector('.form-builder-statusbar__save')).toBeNull();
  });

  it('preserves Cancelar and Guardar output events in editable mode', () => {
    const fixture = TestBed.createComponent(FormBuilderStatusbarComponent);
    fixture.componentInstance.soloLectura = false;
    fixture.componentInstance.puedeGuardar = true;
    fixture.detectChanges();
    let cancelCount = 0;
    let saveCount = 0;
    fixture.componentInstance.cancelar.subscribe(() => cancelCount++);
    fixture.componentInstance.guardar.subscribe(() => saveCount++);

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.form-builder-statusbar__cancel')?.click();
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.form-builder-statusbar__save')?.click();

    expect(cancelCount).toBe(1);
    expect(saveCount).toBe(1);
  });
});
