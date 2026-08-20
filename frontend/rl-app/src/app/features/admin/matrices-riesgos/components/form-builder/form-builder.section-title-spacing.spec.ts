import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderComponent } from './form-builder.component';

describe('FormBuilderComponent - edición del título de sección', () => {
  let fixture: ComponentFixture<FormBuilderComponent>;
  let component: FormBuilderComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderComponent);
    component = fixture.componentInstance;
    component.jsonDefinicion = JSON.stringify({
      codigoFormulario: 'FORM_ESPACIOS',
      nombreFormulario: 'Formulario de prueba',
      secciones: [
        {
          clave: 'prueba_funcional',
          titulo: 'PruebaFuncional',
          orden: 1,
          columnasPorFila: 2,
          campos: []
        }
      ],
      catalogos: []
    });
    component.esAdministrador = true;
    fixture.detectChanges();
  });

  it('permite escribir espacios dentro del nombre de una sección sin que el contenedor cancele la tecla', () => {
    const input = fixture.nativeElement.querySelector('input[id^="seccion-titulo-"]') as HTMLInputElement | null;
    expect(input).toBeTruthy();

    const spaceEvent = new KeyboardEvent('keydown', {
      key: ' ',
      code: 'Space',
      bubbles: true,
      cancelable: true
    });

    input!.dispatchEvent(spaceEvent);
    expect(spaceEvent.defaultPrevented).toBe(false);

    input!.value = 'Prueba Funcional';
    input!.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();

    expect(component.model().secciones[0].titulo).toBe('Prueba Funcional');
  });
});
