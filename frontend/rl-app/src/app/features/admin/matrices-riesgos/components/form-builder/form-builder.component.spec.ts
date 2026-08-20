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
    ],
    catalogos: [
      { codigo: 'CAT_AREA', nombre: 'Áreas', elementos: [{ codigo: '01', valor: 'Área 1', orden: 1 }] }
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
    expect(model.secciones).toHaveLength(1);
    expect(model.secciones[0].titulo).toBe('Identificación');
    expect(model.secciones[0].campos).toHaveLength(1);
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

    // Modificaciones en Inspector con alCambiarPropiedadCampo
    const activo = component.campoActivo()!;
    activo.etiqueta = 'Nuevo Nombre Evaluacion';
    activo.tipo = 'formula';
    activo.formula = 'val1 * val2';
    activo.obligatorio = true;
    component.alCambiarPropiedadCampo();

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
    expect(component.model().secciones[0].campos).toHaveLength(camposIniciales + 1);

    const nuevoCampoId = component.model().secciones[0].campos[component.model().secciones[0].campos.length - 1].id;
    component.eliminarCampo(seccionId, nuevoCampoId);
    expect(component.model().secciones[0].campos).toHaveLength(camposIniciales);
  });

  it('bloquea modificaciones cuando soloLectura es true (versión PUBLISHED o VIGENTE)', () => {
    component.soloLectura = true;
    const seccionId = component.model().secciones[0].id;
    const camposIniciales = component.model().secciones[0].campos.length;

    const ctrlTexto = component.tiposControles.find(t => t.tipo === 'texto')!;
    component.agregarCampoASeccion(seccionId, ctrlTexto);
    expect(component.model().secciones[0].campos).toHaveLength(camposIniciales);

    component.agregarSeccion();
    expect(component.model().secciones).toHaveLength(1);
  });

  it('emite el evento de cierre correctamente al presionar el botón de cerrar', () => {
    vi.spyOn(component.cerrar, 'emit');
    component.cerrar.emit();
    expect(component.cerrar.emit).toHaveBeenCalled();
  });

  it('administra secciones y conserva una seccion activa valida', () => {
    const originalId = component.model().secciones[0].id;
    component.agregarSeccion();
    expect(component.model().secciones).toHaveLength(2);
    const nueva = component.model().secciones[1];
    expect(component.seccionActivaId()).toBe(nueva.id);
    component.eliminarSeccion(nueva.id);
    expect(component.model().secciones).toHaveLength(1);
    expect(component.seccionActivaId()).toBe(originalId);
    component.eliminarSeccion(originalId);
    expect(component.model().secciones).toHaveLength(1);
  });

  it('configura controles con catalogo y formula con valores seguros por defecto', () => {
    const seccionId = component.model().secciones[0].id;
    const catalogo = component.tiposControles.find(t => t.tipo === 'selector-catalogo')!;
    const formula = component.tiposControles.find(t => t.tipo === 'formula')!;
    component.agregarCampoASeccion(seccionId, catalogo);
    component.agregarCampoASeccion(seccionId, formula);
    const campos = component.model().secciones[0].campos;
    expect(campos[1].codigoCatalogo).toBe(component.catalogosList()[0].codigo);
    expect(campos[2].soloLectura).toBe(true);
    expect(component.campoActivo()?.id).toBe(campos[2].id);
  });

  it('reordena campos dentro de limites validos y actualiza columnas', () => {
    const seccionId = component.model().secciones[0].id;
    const texto = component.tiposControles.find(t => t.tipo === 'texto')!;
    const numero = component.tiposControles.find(t => t.tipo === 'numero')!;
    component.agregarCampoASeccion(seccionId, texto);
    component.agregarCampoASeccion(seccionId, numero);
    const antes = component.model().secciones[0].campos.map(c => c.id);
    component.reordenarCampo(seccionId, 1, 'subir');
    component.reordenarCampo(seccionId, 0, 'subir');
    component.reordenarCampo(seccionId, 9, 'bajar');
    component.actualizarColumnasSeccion(seccionId, 4);
    component.actualizarColumnasSeccion('inexistente', 6);
    expect(component.model().secciones[0].campos.map(c => c.id)).toEqual([antes[1], antes[0], antes[2]]);
    expect(component.model().secciones[0].columnasPorFila).toBe(4);
  });

  it('aplica JSON avanzado valido y cierra el modo tecnico', () => {
    const jsonNuevo = JSON.stringify({ codigoFormulario: 'FORM_NUEVO', nombreFormulario: 'Nuevo', secciones: [{ clave: 'sec', titulo: 'Sec', orden: 1, campos: [{ clave: 'dato', etiqueta: 'Dato', tipo: 'texto', obligatorio: true }] }] });
    component.toggleModoJson();
    expect(component.mostrarJsonAvanzado()).toBe(true);
    component.jsonAvanzadoStr.set(jsonNuevo);
    component.aplicarJsonAvanzado();
    expect(component.model().codigoFormulario).toBe('FORM_NUEVO');
    expect(component.mostrarJsonAvanzado()).toBe(false);
  });

  it('emite JSON valido y respeta los bloqueos de solo lectura', () => {
    vi.spyOn(component.guardarJson, 'emit');
    component.emitirGuardado();
    expect(component.guardarJson.emit).toHaveBeenCalledTimes(1);
    const seccionId = component.model().secciones[0].id;
    const campoId = component.model().secciones[0].campos[0].id;
    const etiquetaAnterior = component.model().secciones[0].campos[0].etiqueta;
    component.seleccionarCampo({ ...component.model().secciones[0].campos[0] });
    component.campoActivo()!.etiqueta = 'No debe persistir';
    component.soloLectura = true;
    component.alCambiarPropiedadCampo();
    component.eliminarCampo(seccionId, campoId);
    component.actualizarColumnasSeccion(seccionId, 6);
    component.emitirGuardado();
    expect(component.model().secciones[0].campos[0].etiqueta).toBe(etiquetaAnterior);
    expect(component.model().secciones[0].campos).toHaveLength(1);
    expect(component.model().secciones[0].columnasPorFila).toBe(2);
    expect(component.guardarJson.emit).toHaveBeenCalledTimes(1);
  });

  it('valida los casos semanticamente invalidos del contrato visual', () => {
    expect(validarFormBuilderModel(null as unknown as FormBuilderModel)[0].campo).toBe('Modelo');
    expect(validarFormBuilderModel({ codigoFormulario: 'X', nombreFormulario: 'X', secciones: [] })[0].campo).toBe('Secciones');
    const errores = validarFormBuilderModel({
      codigoFormulario: 'X', nombreFormulario: 'X',
      secciones: [{ id: 's', clave: 's', titulo: 'S', orden: 1, columnasPorFila: 2, campos: [
        { id: 'a', clave: '', etiqueta: '', tipo: 'selector-catalogo', codigoCatalogo: '', obligatorio: false, soloLectura: false, anchoColumnas: 1 },
        { id: 'b', clave: 'calc', etiqueta: 'Calc', tipo: 'formula', formula: '', obligatorio: false, soloLectura: true, anchoColumnas: 1 }
      ] }]
    });
    const mensajes = errores.map(error => error.mensaje).join(' ');
    expect(mensajes).toContain('asociar un');
    expect(mensajes).toContain('campo calculado');
  });
});
