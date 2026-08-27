import { normalizarJsonABuilderModel, serializarBuilderModelAJson, FormBuilderModel, TIPOS_CONTROLES_DISPONIBLES } from '../../models/form-builder.models';

describe('Form Builder — HARD GATE de Integridad JSON (UI-FORM.3)', () => {
  const jsonOriginalValido = JSON.stringify({
    codigoFormulario: 'MATRIZ_LAFT_GATE',
    nombreFormulario: 'Matriz de Riesgo Integridad',
    descripcion: 'Descripción del formulario institucional',
    secciones: [
      {
        clave: 'identificacion',
        titulo: 'Identificación del Riesgo',
        orden: 1,
        columnasPorFila: 2,
        campos: [
          { clave: 'area', etiqueta: 'Área Principal', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 },
          { clave: 'dueno', etiqueta: 'Dueño del Riesgo', tipo: 'selector-catalogo', codigoCatalogo: 'CAT_DUENO', obligatorio: true, soloLectura: false, anchoColumnas: 1 }
        ]
      }
    ],
    catalogos: [
      { codigo: 'CAT_DUENO', nombre: 'Dueños de Riesgo', elementos: [{ codigo: '01', valor: 'Dirección General', orden: 1 }] }
    ]
  });

  const PROPIEDADES_PROHIBIDAS_UI = [
    'selected',
    'isSelected',
    'active',
    'isActive',
    'expanded',
    'dragging',
    'dropZone',
    'isDragging',
    'uiState',
    'visualState',
    'columnCss',
    'fieldWidth',
    'sectionColor',
    'highlight',
    'isHovered'
  ];

  it('1. el catálogo de tipos contiene exactamente los 9 tipos oficiales (0 tipos inventados)', () => {
    const tipos = TIPOS_CONTROLES_DISPONIBLES.map(t => t.tipo).sort();
    const esperados = [
      'catalogo-multiple',
      'checkbox',
      'fecha',
      'formula',
      'numero',
      'radio',
      'selector-catalogo',
      'texto',
      'texto-largo'
    ].sort();

    expect(tipos).toEqual(esperados);
    expect(tipos.length).toBe(9);
  });

  it('2. la serialización JSON nunca incluye propiedades UI temporales o visuales', () => {
    const model: FormBuilderModel = normalizarJsonABuilderModel(jsonOriginalValido);
    const jsonResultante = serializarBuilderModelAJson(model);
    const parsed = JSON.parse(jsonResultante);

    // Verificar en nivel raíz
    for (const prop of PROPIEDADES_PROHIBIDAS_UI) {
      expect(parsed[prop]).toBeUndefined();
    }

    // Verificar en cada sección
    for (const sec of parsed.secciones) {
      for (const prop of PROPIEDADES_PROHIBIDAS_UI) {
        expect(sec[prop]).toBeUndefined();
      }

      // Verificar en cada campo
      for (const cmp of sec.campos) {
        for (const prop of PROPIEDADES_PROHIBIDAS_UI) {
          expect(cmp[prop]).toBeUndefined();
        }
      }
    }
  });

  it('3. cambiar columnasPorFila modifica únicamente la propiedad contractual columnasPorFila', () => {
    const model = normalizarJsonABuilderModel(jsonOriginalValido);
    model.secciones[0].columnasPorFila = 4;

    const serializado = JSON.parse(serializarBuilderModelAJson(model));
    expect(serializado.secciones[0].columnasPorFila).toBe(4);
    expect(serializado.secciones[0].titulo).toBe('Identificación del Riesgo');
    expect(serializado.secciones[0].campos.length).toBe(2);
  });

  it('4. cambiar titulo modifica únicamente la propiedad contractual titulo', () => {
    const model = normalizarJsonABuilderModel(jsonOriginalValido);
    model.secciones[0].titulo = 'Nuevo Título de Sección';

    const serializado = JSON.parse(serializarBuilderModelAJson(model));
    expect(serializado.secciones[0].titulo).toBe('Nuevo Título de Sección');
    expect(serializado.secciones[0].columnasPorFila).toBe(2);
  });

  it('5. el round-trip de normalización y serialización es 100% lossless', () => {
    const model1 = normalizarJsonABuilderModel(jsonOriginalValido);
    const json1 = serializarBuilderModelAJson(model1);
    const model2 = normalizarJsonABuilderModel(json1);
    const json2 = serializarBuilderModelAJson(model2);

    expect(JSON.parse(json1)).toEqual(JSON.parse(json2));
  });

  it('6. la serialización nunca incluye propiedades de workflow o ciclo de vida (UI-FORM.5)', () => {
    const PROPIEDADES_WORKFLOW_PROHIBIDAS = [
      'estadoVersion',
      'estado',
      'workflow',
      'puedePublicar',
      'soloLecturaUI',
      'isDraft',
      'isPublished',
      'procesando',
      'guardando',
      'publicando',
      'permission',
      'permissions'
    ];

    const model = normalizarJsonABuilderModel(jsonOriginalValido);
    const jsonResultante = serializarBuilderModelAJson(model);
    const parsed = JSON.parse(jsonResultante);

    for (const prop of PROPIEDADES_WORKFLOW_PROHIBIDAS) {
      expect(parsed[prop]).toBeUndefined();
    }
  });
});
