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
    expect(button.className).toContain('border-blue-600');
  });

  it('renders the authoritative published state in the statusbar', () => {
    const fixture = TestBed.createComponent(FormBuilderStatusbarComponent);
    fixture.componentInstance.versionCodigo = 'V3.0';
    fixture.componentInstance.estadoVersion = 'PUBLISHED';
    fixture.componentInstance.soloLectura = true;
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Publicada · Solo lectura');
  });
});
