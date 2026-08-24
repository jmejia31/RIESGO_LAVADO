import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderComponent } from './form-builder.component';

describe('FormBuilderComponent - shell permanente UI-FORM.1', () => {
  let component: FormBuilderComponent;
  let fixture: ComponentFixture<FormBuilderComponent>;

  const jsonPrueba = JSON.stringify({
    codigoFormulario: 'MATRIZ_LAFT_SHELL',
    nombreFormulario: 'Matriz Shell',
    secciones: [
      {
        clave: 'general',
        titulo: 'General',
        orden: 1,
        campos: []
      }
    ],
    catalogos: []
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderComponent);
    component = fixture.componentInstance;
    component.jsonDefinicion = jsonPrueba;
    component.esAdministrador = true;
    fixture.detectChanges();
  });

  function expectShellPermanente(): void {
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelectorAll('[data-form-builder-shell="true"]').length).toBe(1);
    expect(host.querySelectorAll('app-form-builder-toolbar').length).toBe(1);
    expect(host.querySelectorAll('app-form-builder-statusbar').length).toBe(1);
  }

  it('mantiene un único shell con toolbar y statusbar en Editor', () => {
    expectShellPermanente();
    expect((fixture.nativeElement as HTMLElement).querySelector('app-form-builder-canvas')).not.toBeNull();
  });

  it('mantiene el mismo shell con toolbar y statusbar en Catálogos', () => {
    component.cambiarVista('catalogos');
    fixture.detectChanges();

    expectShellPermanente();
    expect((fixture.nativeElement as HTMLElement).querySelector('aside[aria-label="Lista de catálogos"]')).not.toBeNull();
  });

  it('mantiene el mismo shell con toolbar y statusbar en JSON técnico', () => {
    component.toggleModoJson();
    fixture.detectChanges();

    expectShellPermanente();
    expect((fixture.nativeElement as HTMLElement).querySelector('#json-avanzado')).not.toBeNull();
  });
});
