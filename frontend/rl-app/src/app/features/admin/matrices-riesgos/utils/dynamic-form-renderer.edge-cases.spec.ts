import { describe, expect, it } from 'vitest';

import {
  formatearValorRespuesta,
  normalizarDefinicionFormulario,
  normalizarMetodologiaFormulario,
  normalizarRespuestasFormulario,
  normalizarTipoCampoRenderer,
  tieneValorRespuesta
} from './dynamic-form-renderer.util';

describe('renderer dinámico — casos límite del contrato', () => {
  it('normaliza aliases adicionales y conserva el tipo original informado', () => {
    const casos: Array<[unknown, string]> = [
      [' TEXT ', 'texto'],
      ['string', 'texto'],
      ['cadena', 'texto'],
      ['numerico', 'numero'],
      ['numérico', 'numero'],
      ['integer', 'numero'],
      ['int', 'numero'],
      ['fecha', 'fecha'],
      ['texto-largo', 'texto-largo'],
      ['long-text', 'texto-largo'],
      ['selector-catalogo', 'selector-catalogo'],
      ['catalogo', 'selector-catalogo'],
      ['catálogo', 'selector-catalogo'],
      ['dropdown', 'selector-catalogo'],
      ['radio', 'radio'],
      ['catalogo-multiple', 'catalogo-multiple'],
      ['catálogo-multiple', 'catalogo-multiple'],
      ['multi-select', 'catalogo-multiple'],
      ['checkbox', 'checkbox'],
      ['boolean', 'checkbox'],
      ['bool', 'checkbox'],
      ['sino', 'checkbox'],
      ['formula', 'formula'],
      ['fórmula', 'formula'],
      ['calculada', 'formula']
    ];

    for (const [entrada, esperado] of casos) {
      const normalizado = normalizarTipoCampoRenderer(entrada);
      expect(normalizado.tipo).toBe(esperado);
      expect(normalizado.tipoOriginal).toBe(String(entrada).trim());
    }

    expect(normalizarTipoCampoRenderer('   ')).toEqual({ tipo: 'desconocido', tipoOriginal: null });
  });

  it('normaliza ids, booleanos, opciones numéricas y metadata opcional sin perder semántica', () => {
    const definicion = normalizarDefinicionFormulario({
      codigoFormulario: 'DINAMICO',
      nombreFormulario: 'Dinámico',
      catalogos: [{ codigo: 'CAT', nombre: 'Catálogo', items: [] }],
      reglas: [{ codigo: 'R1', expresion: 'a+b' }],
      secciones: [{
        clave: 'general',
        titulo: 'General',
        orden: '4',
        columnas: '3',
        campos: [
          {
            id: 101,
            label: 'Confirmado',
            type: 'bool',
            campoCanonicoId: '8',
            catalogCode: ' CAT ',
            options: [1, ' Dos ', 1, null, ''],
            required: 'sí',
            readOnly: 0,
            width: '2'
          },
          {
            key: 'solo-lectura',
            type: 'text',
            required: 1,
            readOnly: 'yes'
          },
          {
            key: 'id-invalido',
            type: 'text',
            campoCanonicoId: -3
          }
        ]
      }]
    });

    expect(definicion.catalogos).toHaveLength(1);
    expect(definicion.reglas).toHaveLength(1);
    expect(definicion.secciones[0]).toEqual(expect.objectContaining({ orden: 4, columnasPorFila: 3 }));

    const [confirmado, soloLectura, idInvalido] = definicion.secciones[0].campos;
    expect(confirmado).toEqual(expect.objectContaining({
      clave: '101',
      etiqueta: 'Confirmado',
      campoCanonicoId: 8,
      codigoCatalogo: 'CAT',
      opciones: ['1', 'Dos'],
      obligatorio: true,
      soloLectura: false,
      anchoColumnas: 2
    }));
    expect(soloLectura.obligatorio).toBe(true);
    expect(soloLectura.soloLectura).toBe(true);
    expect(idInvalido.campoCanonicoId).toBeNull();
  });

  it('usa defaults seguros para secciones incompletas e ignora entradas que no son objetos', () => {
    const definicion = normalizarDefinicionFormulario({
      sections: [
        null,
        'seccion-invalida',
        {
          fields: [
            null,
            'campo-invalido',
            { key: 'valido', type: 'text', width: Number.POSITIVE_INFINITY }
          ],
          order: Number.NaN,
          columns: 0
        }
      ]
    });

    expect(definicion.secciones).toHaveLength(1);
    expect(definicion.secciones[0]).toEqual(expect.objectContaining({
      clave: 'seccion_3',
      titulo: 'Sección 3',
      orden: 3,
      columnasPorFila: 2
    }));
    expect(definicion.secciones[0].campos).toHaveLength(1);
    expect(definicion.secciones[0].campos[0]).toEqual(expect.objectContaining({
      clave: 'valido',
      etiqueta: 'valido',
      anchoColumnas: 1
    }));
  });

  it('acepta definición JSON válida y aplica valores por defecto cuando faltan código o nombre', () => {
    const desdeJson = normalizarDefinicionFormulario(
      '{"sections":[{"key":"s1","fields":[{"key":"fecha","type":"date"}]}]}',
      'DEFECTO',
      'Formulario defecto'
    );

    expect(desdeJson.codigoFormulario).toBe('DEFECTO');
    expect(desdeJson.nombreFormulario).toBe('Formulario defecto');
    expect(desdeJson.secciones[0].campos[0].tipo).toBe('fecha');

    expect(normalizarDefinicionFormulario([], 'BASE')).toEqual({
      codigoFormulario: 'BASE',
      nombreFormulario: 'BASE',
      secciones: []
    });
  });

  it('normaliza metodología ausente sin excepciones', () => {
    expect(normalizarMetodologiaFormulario(null)).toEqual({
      codigoFormulario: '',
      nombreFormulario: '',
      secciones: []
    });
    expect(normalizarMetodologiaFormulario(undefined)).toEqual({
      codigoFormulario: '',
      nombreFormulario: '',
      secciones: []
    });
  });

  it('normaliza respuestas serializadas y arrays con entradas heterogéneas', () => {
    expect(normalizarRespuestasFormulario('{"numero":5,"check":false,"texto":"ok"}')).toEqual({
      numero: 5,
      check: false,
      texto: 'ok'
    });

    expect(normalizarRespuestasFormulario({
      multi: [' A ', 2, false, null, 'A', ''],
      nan: Number.NaN
    })).toEqual({
      multi: ['A', '2'],
      nan: null
    });

    expect(normalizarRespuestasFormulario(undefined)).toEqual({});
    expect(normalizarRespuestasFormulario(['A'])).toEqual({});
  });

  it('distingue NaN, objetos y falsy válidos al evaluar presencia', () => {
    expect(tieneValorRespuesta(Number.NaN)).toBe(false);
    expect(tieneValorRespuesta({})).toBe(false);
    expect(tieneValorRespuesta(undefined)).toBe(false);
    expect(tieneValorRespuesta(' dato ')).toBe(true);
    expect(tieneValorRespuesta(0)).toBe(true);
    expect(tieneValorRespuesta(false)).toBe(true);
  });

  it('formatea números no finitos, texto con espacios y ausencia de dato de forma estable', () => {
    expect(formatearValorRespuesta(undefined)).toBe('-');
    expect(formatearValorRespuesta(null)).toBe('-');
    expect(formatearValorRespuesta(Number.NaN)).toBe('-');
    expect(formatearValorRespuesta(Number.POSITIVE_INFINITY)).toBe('-');
    expect(formatearValorRespuesta('   ')).toBe('-');
    expect(formatearValorRespuesta(' valor ')).toBe('valor');
  });
});
