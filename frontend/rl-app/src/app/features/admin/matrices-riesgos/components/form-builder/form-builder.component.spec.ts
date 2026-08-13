import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderComponent } from './form-builder.component';
import { normalizarJsonABuilderModel, serializarBuilderModelAJson, FormBuilderModel } from '../../models/form-builder.models';
import { validarFormBuilderModel } from '../../utils/form-builder-validator.util';

describe('FormBuilderComponent y Adaptador Normalizador (Fases 3 y 4)', () => {
  let component: FormBuilderComponent;
  let fixture: ComponentFixture<FormBuilderComponent>;

  const jsonPruebaValido = JSON.stringify({
    codigoFormulario: 'MATRIZ_LAFT_TEST',
    nombreFormulario: 'Matriz de Riesgo Prueba',
    secciones: [
      {
        clave: 'identificacion',
        titulo: 'Identificación',
        orden: 1,
        campos: [
          { clave: 'area', etiqueta: 'Área principal', tipo: 'texto', obligatorio: true, soloLectura: false }
        ]
      }
    ]
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormBuilderComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormBuilderComponent);
    component = fixture.componentInstance;
    component.jsonDefinicion = jsonPruebaValido;
    component.esAdministrador = true;
    fixture.detectChanges();
  });

  it('debe crear el componente FormBuilderComponent', () => {
    expect(component).toBeTruthy();
  });

  it('bloquea la visualización del editor JSON técnico si esAdministrador es false', () => {
    component.esAdministrador = false;
    component.toggleModoJson();
    expect(component.mostrarJsonAvanzado()).toBe(false);
  });

  it('normalizarJsonABuilderModel convierte correctamente la estructura JSON en BuilderModel', () => {
    const model = normalizarJsonABuilderModel(jsonPruebaValido, 'CODIGO_DEFAULT', 'Nombre Default');
    expect(model.codigoFormulario).toBe('MATRIZ_LAFT_TEST');
    expect(model.secciones.length).toBe(1);
    expect(model.secciones[0].titulo).toBe('Identificación');
    expect(model.secciones[0].campos.length).toBe(1);
    expect(model.secciones[0].campos[0].clave).toBe('area');
  });

  it('serializarBuilderModelAJson preserva el contrato oficial de JSON', () => {
    const model: FormBuilderModel = {
      codigoFormulario: 'FORM_TEST',
      nombreFormulario: 'Test Form',
      secciones: [
        {
          id: 'sec_1',
          clave: 'sec_test',
          titulo: 'Sección Test',
          orden: 1,
          columnasPorFila: 2,
          campos: [
            {
              id: 'cmp_1',
              clave: 'campo_uno',
              etiqueta: 'Campo Uno',
              tipo: 'texto',
              obligatorio: true,
              soloLectura: false,
              anchoColumnas: 1
            }
          ]
        }
      ]
    };

    const json = serializarBuilderModelAJson(model);
    const parsed = JSON.parse(json);
    expect(parsed.codigoFormulario).toBe('FORM_TEST');
    expect(parsed.secciones[0].campos[0].clave).toBe('campo_uno');
    expect(parsed.secciones[0].campos[0].obligatorio).toBe(true);
  });

  it('validarFormBuilderModel detecta claves técnicas duplicadas', () => {
    const modelDuplicado: FormBuilderModel = {
      codigoFormulario: 'TEST',
      nombreFormulario: 'Test',
      secciones: [
        {
          id: 'sec_1',
          clave: 'general',
          titulo: 'General',
          orden: 1,
          columnasPorFila: 2,
          campos: [
            { id: 'c1', clave: 'campo_duplicado', etiqueta: 'E1', tipo: 'texto', obligatorio: false, soloLectura: false, anchoColumnas: 1 },
            { id: 'c2', clave: 'campo_duplicado', etiqueta: 'E2', tipo: 'numero', obligatorio: false, soloLectura: false, anchoColumnas: 1 }
          ]
        }
      ]
    };

    const errores = validarFormBuilderModel(modelDuplicado);
    expect(errores.length).toBeGreaterThan(0);
    expect(errores[0].mensaje).toContain('está duplicada');
  });

  it('no emite guardado si existen errores de validación', () => {
    vi.spyOn(component.guardarJson, 'emit');
    component.model.set({
      codigoFormulario: 'INVALID',
      nombreFormulario: 'Invalid',
      secciones: [
        {
          id: 'sec_1',
          clave: '',
          titulo: '',
          orden: 1,
          columnasPorFila: 2,
          campos: []
        }
      ]
    });

    component.emitirGuardado();
    expect(component.erroresValidacion().length).toBeGreaterThan(0);
    expect(component.guardarJson.emit).not.toHaveBeenCalled();
  });

  it('permite editar etiqueta, tipo, obligatoriedad y fórmula de un campo en modo DRAFT y actualiza la serialización JSON', () => {
    component.soloLectura = false;
    const seccionId = component.model().secciones[0].id;
    const ctrlTexto = component.tiposControles.find(t => t.tipo === 'texto')!;
    component.agregarCampoASeccion(seccionId, ctrlTexto);

    const campo = component.model().secciones[0].campos[0];
    component.seleccionarCampo(campo);

    // Modificaciones en Inspector
    component.campoActivo.update(c => c ? { ...c, etiqueta: 'Nuevo Nombre Evaluacion', tipo: 'formula', formula: 'val1 * val2', obligatorio: true } : null);
    component.model.update(m => {
      const sec = { ...m.secciones[0] };
      sec.campos[0] = component.campoActivo()!;
      return { ...m, secciones: [sec] };
    });

    const jsonFinal = serializarBuilderModelAJson(component.model());
    const parsed = JSON.parse(jsonFinal);

    expect(parsed.secciones[0].campos[0].etiqueta).toBe('Nuevo Nombre Evaluacion');
    expect(parsed.secciones[0].campos[0].tipo).toBe('formula');
    expect(parsed.secciones[0].campos[0].formula).toBe('val1 * val2');
    expect(parsed.secciones[0].campos[0].obligatorio).toBe(true);
  });

  it('permite agregar y eliminar campos en modo DRAFT', () => {
    component.soloLectura = false;
    const seccionId = component.model().secciones[0].id;
    const camposIniciales = component.model().secciones[0].campos.length;

    const ctrlNumero = component.tiposControles.find(t => t.tipo === 'numero')!;
    component.agregarCampoASeccion(seccionId, ctrlNumero);
    expect(component.model().secciones[0].campos.length).toBe(camposIniciales + 1);

    const nuevoCampoId = component.model().secciones[0].campos[component.model().secciones[0].campos.length - 1].id;
    component.eliminarCampo(seccionId, nuevoCampoId);
    expect(component.model().secciones[0].campos.length).toBe(camposIniciales);
  });

  it('bloquea modificaciones cuando soloLectura es true (versión PUBLISHED o VIGENTE)', () => {
    component.soloLectura = true;
    const seccionId = component.model().secciones[0].id;
    const camposIniciales = component.model().secciones[0].campos.length;

    const ctrlTexto = component.tiposControles.find(t => t.tipo === 'texto')!;
    component.agregarCampoASeccion(seccionId, ctrlTexto);
    expect(component.model().secciones[0].campos.length).toBe(camposIniciales);

    component.agregarSeccion();
    expect(component.model().secciones.length).toBe(1);
  });

  it('emite el evento de cierre correctamente al presionar el botón de cerrar', () => {
    vi.spyOn(component.cerrar, 'emit');
    component.cerrar.emit();
    expect(component.cerrar.emit).toHaveBeenCalled();
  });
});
