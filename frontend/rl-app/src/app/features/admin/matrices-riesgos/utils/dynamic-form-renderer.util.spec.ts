import { MetodologiaFormulario } from '../models/matrices-riesgos.models';
import {
  formatearValorRespuesta,
  normalizarDefinicionFormulario,
  normalizarMetodologiaFormulario,
  normalizarRespuestasFormulario,
  normalizarTipoCampoRenderer,
  tieneValorRespuesta
} from './dynamic-form-renderer.util';

describe('Renderer dinámico — normalización defensiva', () => {
  it('normaliza los nueve tipos canónicos y sus aliases operativos', () => {
    const casos: Array<[string, string]> = [
      ['texto', 'texto'],
      ['number', 'numero'],
      ['date', 'fecha'],
      ['textarea', 'texto-largo'],
      ['select', 'selector-catalogo'],
      ['opciones', 'radio'],
      ['multiselect', 'catalogo-multiple'],
      ['booleano', 'checkbox'],
      ['calculado', 'formula']
    ];

    for (const [entrada, esperado] of casos) {
      expect(normalizarTipoCampoRenderer(entrada).tipo).toBe(esperado);
    }
  });

  it('degrada tipos desconocidos sin convertirlos silenciosamente en texto editable', () => {
    expect(normalizarTipoCampoRenderer('control-futuro')).toEqual({
      tipo: 'desconocido',
      tipoOriginal: 'control-futuro'
    });
    expect(normalizarTipoCampoRenderer(null)).toEqual({ tipo: 'desconocido', tipoOriginal: null });
  });

  it('tolera JSON vacío o malformado sin lanzar excepciones', () => {
    expect(normalizarDefinicionFormulario('', 'FORM', 'Formulario')).toEqual({
      codigoFormulario: 'FORM',
      nombreFormulario: 'Formulario',
      secciones: []
    });
    expect(normalizarDefinicionFormulario('{mal-json', 'FORM', 'Formulario').secciones).toEqual([]);
  });

  it('acepta aliases de contrato para secciones y campos', () => {
    const definicion = normalizarDefinicionFormulario({
      formCode: 'RIESGOS',
      formName: 'Riesgos dinámicos',
      sections: [{
        key: 'general',
        title: 'General',
        order: 2,
        columns: 3,
        fields: [{ key: 'monto', label: 'Monto', type: 'decimal', required: true, width: 2 }]
      }]
    });

    expect(definicion.codigoFormulario).toBe('RIESGOS');
    expect(definicion.nombreFormulario).toBe('Riesgos dinámicos');
    expect(definicion.secciones[0].columnasPorFila).toBe(3);
    expect(definicion.secciones[0].campos[0]).toEqual(expect.objectContaining({
      clave: 'monto',
      etiqueta: 'Monto',
      tipo: 'numero',
      obligatorio: true,
      anchoColumnas: 2
    }));
  });

  it('ordena secciones y limita columnas/anchos inválidos a defaults seguros', () => {
    const definicion = normalizarDefinicionFormulario({
      secciones: [
        { clave: 'b', titulo: 'B', orden: 20, columnasPorFila: 99, campos: [{ clave: 'b1', etiqueta: 'B1', tipo: 'texto', anchoColumnas: 99 }] },
        { clave: 'a', titulo: 'A', orden: 1, columnasPorFila: 1, campos: [{ clave: 'a1', etiqueta: 'A1', tipo: 'texto', anchoColumnas: 3 }] }
      ]
    });

    expect(definicion.secciones.map(s => s.clave)).toEqual(['a', 'b']);
    expect(definicion.secciones[0].columnasPorFila).toBe(1);
    expect(definicion.secciones[0].campos[0].anchoColumnas).toBe(3);
    expect(definicion.secciones[1].columnasPorFila).toBe(2);
    expect(definicion.secciones[1].campos[0].anchoColumnas).toBe(1);
  });

  it('descarta campos sin clave y duplicados globales conservando la primera definición', () => {
    const definicion = normalizarDefinicionFormulario({
      secciones: [
        {
          clave: 'uno',
          campos: [
            { clave: 'codigo', etiqueta: 'Primero', tipo: 'texto' },
            { etiqueta: 'Sin clave', tipo: 'texto' }
          ]
        },
        {
          clave: 'dos',
          campos: [{ clave: 'CODIGO', etiqueta: 'Duplicado', tipo: 'numero' }]
        }
      ]
    });

    expect(definicion.secciones[0].campos).toHaveLength(1);
    expect(definicion.secciones[0].campos[0].etiqueta).toBe('Primero');
    expect(definicion.secciones[1].campos).toHaveLength(0);
  });

  it('fuerza fórmula y tipo desconocido a solo lectura', () => {
    const definicion = normalizarDefinicionFormulario({
      secciones: [{
        clave: 'general',
        campos: [
          { clave: 'calc', etiqueta: 'Cálculo', tipo: 'formula', soloLectura: false, formula: 'a + b' },
          { clave: 'futuro', etiqueta: 'Futuro', tipo: 'widget-v2', soloLectura: false }
        ]
      }]
    });

    const [formula, desconocido] = definicion.secciones[0].campos;
    expect(formula).toEqual(expect.objectContaining({ tipo: 'formula', soloLectura: true }));
    expect(desconocido).toEqual(expect.objectContaining({
      tipo: 'desconocido',
      tipoOriginal: 'widget-v2',
      soloLectura: true
    }));
  });

  it('preserva opciones inline sin blancos ni duplicados', () => {
    const definicion = normalizarDefinicionFormulario({
      secciones: [{
        clave: 'general',
        campos: [{ clave: 'decision', etiqueta: 'Decisión', tipo: 'radio', opciones: ['Sí', ' ', 'No', 'Sí'] }]
      }]
    });

    expect(definicion.secciones[0].campos[0].opciones).toEqual(['Sí', 'No']);
  });

  it('normaliza una metodología tipada usando el mismo contrato del renderer', () => {
    const metodologia: MetodologiaFormulario = {
      versionFormularioId: 7,
      codigo: 'MATRIZ',
      version: 2,
      secciones: [{
        clave: 's1',
        titulo: 'Sección',
        orden: 1,
        campos: [{ clave: 'acepta', etiqueta: 'Acepta', tipo: 'bool', obligatorio: false, soloLectura: false }]
      }],
      catalogos: [],
      reglas: []
    };

    const definicion = normalizarMetodologiaFormulario(metodologia);
    expect(definicion.codigoFormulario).toBe('MATRIZ');
    expect(definicion.secciones[0].campos[0].tipo).toBe('checkbox');
  });

  it('normaliza respuestas escalares y selección múltiple sin perder 0 ni false', () => {
    expect(normalizarRespuestasFormulario({
      cero: 0,
      falso: false,
      texto: 'dato',
      multi: ['A', 'A', 'B'],
      nulo: null
    })).toEqual({
      cero: 0,
      falso: false,
      texto: 'dato',
      multi: ['A', 'B'],
      nulo: null
    });
  });

  it('rechaza objetos arbitrarios y números no finitos en respuestas', () => {
    const normalizadas = normalizarRespuestasFormulario({
      objeto: { peligro: true },
      infinito: Number.POSITIVE_INFINITY
    });
    expect(normalizadas).toEqual({ objeto: null, infinito: null });
  });

  it('tolera respuestas JSON malformadas', () => {
    expect(normalizarRespuestasFormulario('{mal')).toEqual({});
    expect(normalizarRespuestasFormulario('[]')).toEqual({});
  });

  it('determina presencia de valor sin tratar 0 o false como vacíos', () => {
    expect(tieneValorRespuesta(0)).toBe(true);
    expect(tieneValorRespuesta(false)).toBe(true);
    expect(tieneValorRespuesta(['A'])).toBe(true);
    expect(tieneValorRespuesta([])).toBe(false);
    expect(tieneValorRespuesta('   ')).toBe(false);
    expect(tieneValorRespuesta(null)).toBe(false);
  });

  it('formatea lectura segura para escalares y selección múltiple', () => {
    expect(formatearValorRespuesta(0)).toBe('0');
    expect(formatearValorRespuesta(false)).toBe('No');
    expect(formatearValorRespuesta(true)).toBe('Sí');
    expect(formatearValorRespuesta(['A', 'B'])).toBe('A, B');
    expect(formatearValorRespuesta([])).toBe('-');
    expect(formatearValorRespuesta('')).toBe('-');
  });
});
