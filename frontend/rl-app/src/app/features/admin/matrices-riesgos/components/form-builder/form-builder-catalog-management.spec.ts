import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilderComponent } from './form-builder.component';
import { serializarBuilderModelAJson } from '../../models/form-builder.models';
import { validarFormBuilderModel } from '../../utils/form-builder-validator.util';

describe('FormBuilderCatalogManagementSpec — Administración Visual de Catálogos (F6.2)', () => {
  let component: FormBuilderComponent;
  let fixture: ComponentFixture<FormBuilderComponent>;

  const jsonPlantillaConCatalogos = JSON.stringify({
    codigoFormulario: 'MATRIZ_LAFT_CATALOGOS',
    nombreFormulario: 'Matriz con Catálogos Dinámicos',
    propiedadFuturaRaiz: { retener: true, idExterno: 'EXT-999' },
    secciones: [
      {
        id: 'sec_1',
        clave: 'identificacion',
        titulo: 'Identificación y Clasificación',
        orden: 1,
        columnasPorFila: 2,
        campos: [
          {
            id: 'cmp_1',
            clave: 'tipo_riesgo',
            etiqueta: 'Tipo de Riesgo',
            tipo: 'selector-catalogo',
            codigoCatalogo: 'CAT_TIPO_RIESGO',
            obligatorio: true,
            soloLectura: false,
            anchoColumnas: 1,
            propiedadFuturaCampo: 'conservar_este_valor'
          },
          {
            id: 'cmp_2',
            clave: 'controles_aplicables',
            etiqueta: 'Controles Aplicables',
            tipo: 'catalogo-multiple',
            codigoCatalogo: 'CAT_CONTROLES',
            obligatorio: false,
            soloLectura: false,
            anchoColumnas: 2
          }
        ]
      }
    ],
    catalogos: [
      {
        codigo: 'CAT_TIPO_RIESGO',
        nombre: 'Tipos de Riesgo LA/FT',
        origenMetadato: 'SISTEMA_ORIGEN',
        elementos: [
          { codigo: '001', valor: 'Lavado de Activos', orden: 1, colorHex: '#FF0000' },
          { codigo: '002', valor: 'Financiamiento del Terrorismo', orden: 2, colorHex: '#00FF00' },
          { codigo: 'G-IVM', valor: 'Riesgo Integral IVM', orden: 3, flagEspecial: true }
        ]
      },
      {
        codigo: 'CAT_CONTROLES',
        nombre: 'Controles Mitigantes',
        elementos: [
          { codigo: 'CTRL_PREV', valor: 'Control Preventivo', orden: 1 },
          { codigo: 'CTRL_DET', valor: 'Control Detectivo', orden: 2 }
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
    component.jsonDefinicion = jsonPlantillaConCatalogos;
    component.esAdministrador = true;
    fixture.detectChanges();
  });

  it('1. Renderiza correctamente el listado inicial de catálogos', () => {
    expect(component.catalogosList()).toHaveLength(2);
    expect(component.catalogosList()[0].codigo).toBe('CAT_TIPO_RIESGO');
    expect(component.catalogosList()[0].elementos).toHaveLength(3);
    expect(component.catalogosList()[1].codigo).toBe('CAT_CONTROLES');
  });

  it('2. Permite crear un catálogo nuevo con código y nombre', () => {
    component.cambiarVista('catalogos');
    component.iniciarNuevoCatalogo();
    expect(component.catalogoEnEdicion()).toBeTruthy();
    expect(component.catalogoEnEdicion()?.esNuevo).toBe(true);

    component.catalogoEnEdicion.set({
      codigoOriginal: null,
      codigo: 'CAT_FRECUENCIA',
      nombre: 'Frecuencia de Ocurrencia',
      esNuevo: true
    });

    component.guardarEdicionCatalogo();

    expect(component.catalogosList()).toHaveLength(3);
    const nuevo = component.catalogosList().find(c => c.codigo === 'CAT_FRECUENCIA');
    expect(nuevo).toBeTruthy();
    expect(nuevo?.nombre).toBe('Frecuencia de Ocurrencia');
    expect(nuevo?.elementos).toEqual([]);
    expect(component.catalogoActivoCodigo()).toBe('CAT_FRECUENCIA');
  });

  it('3. Impide la creación de catálogo con código duplicado (case-insensitive)', () => {
    component.iniciarNuevoCatalogo();
    component.catalogoEnEdicion.set({
      codigoOriginal: null,
      codigo: 'cat_tipo_riesgo', // Mismo código en minúsculas
      nombre: 'Otro Nombre',
      esNuevo: true
    });

    component.guardarEdicionCatalogo();

    expect(component.feedbackCatalogo()?.tipo).toBe('error');
    expect(component.feedbackCatalogo()?.mensaje).toContain('Ya existe un catálogo con el código');
    expect(component.catalogosList()).toHaveLength(2); // No se agregó
  });

  it('4. Permite editar el código y nombre de un catálogo existente y preserva integridad referencial', () => {
    const catOriginal = component.catalogosList()[0];
    component.iniciarEdicionCatalogo(catOriginal);

    component.catalogoEnEdicion.set({
      codigoOriginal: 'CAT_TIPO_RIESGO',
      codigo: 'CAT_TIPO_RIESGO_MOD',
      nombre: 'Tipos de Riesgo Modificados',
      esNuevo: false
    });

    component.guardarEdicionCatalogo();

    expect(component.catalogosList()[0].codigo).toBe('CAT_TIPO_RIESGO_MOD');
    expect(component.catalogosList()[0].nombre).toBe('Tipos de Riesgo Modificados');

    // Integridad referencial: el campo 'tipo_riesgo' que usaba CAT_TIPO_RIESGO debe actualizar su referencia
    const campo = component.model().secciones[0].campos[0];
    expect(campo.codigoCatalogo).toBe('CAT_TIPO_RIESGO_MOD');
  });

  it('5. Permite agregar elementos a un catálogo y preserva códigos alfanuméricos', () => {
    component.seleccionarCatalogo('CAT_TIPO_RIESGO');
    component.iniciarNuevoElemento();
    expect(component.elementoEnEdicion()).toBeTruthy();

    component.elementoEnEdicion.set({
      codigoOriginal: null,
      codigo: '004',
      valor: 'Riesgo Operacional Crítico',
      orden: 4,
      indice: null
    });

    component.guardarElementoCatalogo();

    const cat = component.catalogoActivo();
    expect(cat?.elementos).toHaveLength(4);
    const elem004 = cat?.elementos.find(e => e.codigo === '004');
    expect(elem004).toBeTruthy();
    expect(elem004?.codigo).toBe('004'); // String exacto sin coerción numérica
    expect(elem004?.valor).toBe('Riesgo Operacional Crítico');
  });

  it('6. Permite editar un elemento existente del catálogo', () => {
    component.seleccionarCatalogo('CAT_TIPO_RIESGO');
    const elementoGIVM = component.catalogoActivo()!.elementos[2];
    expect(elementoGIVM.codigo).toBe('G-IVM');

    component.iniciarEdicionElemento(elementoGIVM, 2);
    component.elementoEnEdicion.set({
      codigoOriginal: 'G-IVM',
      codigo: 'G-IVM',
      valor: 'Riesgo Integral IVM Actualizado',
      orden: 3,
      indice: 2
    });

    component.guardarElementoCatalogo();

    const elemModificado = component.catalogoActivo()!.elementos[2];
    expect(elemModificado.valor).toBe('Riesgo Integral IVM Actualizado');
    expect(elemModificado.codigo).toBe('G-IVM');
  });

  it('7. Permite eliminar un elemento del catálogo y reindexa el orden secuencial', () => {
    component.seleccionarCatalogo('CAT_TIPO_RIESGO');
    expect(component.catalogoActivo()!.elementos).toHaveLength(3);

    component.eliminarElementoCatalogo(1); // Elimina el elemento 2 (índice 1)

    const elementosRestantes = component.catalogoActivo()!.elementos;
    expect(elementosRestantes).toHaveLength(2);
    expect(elementosRestantes[0].codigo).toBe('001');
    expect(elementosRestantes[0].orden).toBe(1);
    expect(elementosRestantes[1].codigo).toBe('G-IVM');
    expect(elementosRestantes[1].orden).toBe(2); // Reindexado a 2
  });

  it('8. Impide elementos con código duplicado dentro del mismo catálogo', () => {
    component.seleccionarCatalogo('CAT_TIPO_RIESGO');
    component.iniciarNuevoElemento();

    component.elementoEnEdicion.set({
      codigoOriginal: null,
      codigo: '001', // Ya existe 001
      valor: 'Duplicado',
      orden: 4,
      indice: null
    });

    component.guardarElementoCatalogo();

    expect(component.feedbackCatalogo()?.tipo).toBe('error');
    expect(component.feedbackCatalogo()?.mensaje).toContain('ya existe');
    expect(component.catalogoActivo()!.elementos).toHaveLength(3);
  });

  it('9. Bloquea la eliminación de un catálogo que está siendo utilizado por campos', () => {
    expect(component.catalogoEstaEnUso('CAT_TIPO_RIESGO')).toBe(true);

    component.eliminarCatalogo('CAT_TIPO_RIESGO');

    expect(component.feedbackCatalogo()?.tipo).toBe('error');
    expect(component.feedbackCatalogo()?.mensaje).toContain('está en uso por');
    expect(component.catalogosList()).toHaveLength(2); // No fue eliminado
  });

  it('10. Permite eliminar un catálogo que NO está en uso por ningún campo', () => {
    // Primero creamos un catálogo huérfano sin campos asociados
    component.iniciarNuevoCatalogo();
    component.catalogoEnEdicion.set({
      codigoOriginal: null,
      codigo: 'CAT_TEMPORAL',
      nombre: 'Catálogo Temporal',
      esNuevo: true
    });
    component.guardarEdicionCatalogo();
    expect(component.catalogosList()).toHaveLength(3);

    // Procedemos a eliminarlo
    component.eliminarCatalogo('CAT_TEMPORAL');
    expect(component.feedbackCatalogo()?.tipo).toBe('exito');
    expect(component.catalogosList()).toHaveLength(2);
    expect(component.catalogosList().some(c => c.codigo === 'CAT_TEMPORAL')).toBe(false);
  });

  it('11. Permite reordenar elementos con subir y bajar', () => {
    component.seleccionarCatalogo('CAT_TIPO_RIESGO');
    const elementosIniciales = component.catalogoActivo()!.elementos;
    expect(elementosIniciales[0].codigo).toBe('001');
    expect(elementosIniciales[1].codigo).toBe('002');

    // Bajar elemento 0 (001)
    component.reordenarElementoCatalogo(0, 'bajar');

    const elementosDespues = component.catalogoActivo()!.elementos;
    expect(elementosDespues[0].codigo).toBe('002');
    expect(elementosDespues[0].orden).toBe(1);
    expect(elementosDespues[1].codigo).toBe('001');
    expect(elementosDespues[1].orden).toBe(2);
  });

  it('12. Preserva propiedades desconocidas y metadatos opacos en round-trip tras edición visual', () => {
    // Editamos visualmente un elemento
    component.seleccionarCatalogo('CAT_TIPO_RIESGO');
    component.iniciarEdicionElemento(component.catalogoActivo()!.elementos[0], 0);
    component.elementoEnEdicion.set({
      codigoOriginal: '001',
      codigo: '001',
      valor: 'Lavado de Activos Editado',
      orden: 1,
      indice: 0
    });
    component.guardarElementoCatalogo();

    // Serializamos a JSON
    const jsonOutput = serializarBuilderModelAJson(component.model());
    const parsed = JSON.parse(jsonOutput);

    // Verificamos que los cambios visuales se aplicaron
    expect(parsed.catalogos[0].elementos[0].valor).toBe('Lavado de Activos Editado');
    expect(parsed.catalogos[0].elementos[0].codigo).toBe('001');

    // Verificamos que las propiedades desconocidas sobrevivieron intactas (Lossless)
    expect(parsed.propiedadFuturaRaiz.retener).toBe(true);
    expect(parsed.propiedadFuturaRaiz.idExterno).toBe('EXT-999');
    expect(parsed.secciones[0].campos[0].propiedadFuturaCampo).toBe('conservar_este_valor');
    expect(parsed.catalogos[0].origenMetadato).toBe('SISTEMA_ORIGEN');
    expect(parsed.catalogos[0].elementos[0].colorHex).toBe('#FF0000');
    expect(parsed.catalogos[0].elementos[2].flagEspecial).toBe(true);
  });

  it('13. La validación del modelo valida y aprueba la estructura con catálogos creados', () => {
    const errores = validarFormBuilderModel(component.model());
    expect(errores).toHaveLength(0);
    expect(component.validarYObtenerErrores()).toBe(true);
  });
});
