import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderComponent } from './form-builder.component';

describe('FormBuilderComponent — fuente de verdad de catálogos', () => {
  let fixture: ComponentFixture<FormBuilderComponent>;
  let component: FormBuilderComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderComponent]
    }).compileComponents();
  });

  function crearComponente(jsonDefinicion: string): void {
    fixture = TestBed.createComponent(FormBuilderComponent);
    component = fixture.componentInstance;
    component.jsonDefinicion = jsonDefinicion;
    component.esAdministrador = true;
    fixture.detectChanges();
  }

  it('usa únicamente los catálogos declarados por la plantilla', () => {
    crearComponente(JSON.stringify({
      codigoFormulario: 'FORM_FUENTE_CATALOGOS',
      nombreFormulario: 'Fuente de catálogos',
      secciones: [],
      catalogos: [
        {
          codigo: 'CAT_REAL',
          nombre: 'Catálogo real',
          elementos: [{ codigo: '001', valor: 'Opción real', orden: 1 }]
        }
      ]
    }));

    expect(component.catalogosDisponiblesParaCampos()).toEqual([
      { codigo: 'CAT_REAL', nombre: 'Catálogo real', cantidadElementos: 1 }
    ]);
    expect(component.catalogosDisponiblesParaCampos().some(c => c.codigo === 'MR_IMPACTO_1_5')).toBe(false);
    expect(component.catalogosDisponiblesParaCampos().some(c => c.codigo === 'CAT_TIPO_LISTA')).toBe(false);
  });

  it('no inventa una referencia de catálogo cuando la plantilla no tiene catálogos', () => {
    crearComponente(JSON.stringify({
      codigoFormulario: 'FORM_SIN_CATALOGOS',
      nombreFormulario: 'Sin catálogos',
      secciones: [
        {
          id: 'sec_1',
          clave: 'sec_1',
          titulo: 'Sección',
          orden: 1,
          columnasPorFila: 1,
          campos: []
        }
      ],
      catalogos: []
    }));

    expect(component.catalogosDisponiblesParaCampos()).toEqual([]);

    const selector = component.tiposControles.find(tipo => tipo.tipo === 'selector-catalogo');
    expect(selector).toBeTruthy();

    component.agregarCampoASeccion('sec_1', selector!);

    const campoCreado = component.model().secciones[0].campos[0];
    expect(campoCreado.codigoCatalogo).toBeUndefined();
    expect(component.validarYObtenerErrores()).toBe(false);
    expect(component.erroresValidacion().some(error => error.mensaje.toLowerCase().includes('catálogo'))).toBe(true);
  });
});
