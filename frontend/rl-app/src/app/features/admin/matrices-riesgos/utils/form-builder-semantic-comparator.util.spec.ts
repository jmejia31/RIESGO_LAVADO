import {
  canonicalizarValor,
  sonJsonSemanticamenteEquivalentes
} from './form-builder-semantic-comparator.util';

describe('form-builder-semantic-comparator.util', () => {
  it('identifica objetos con distinto orden de propiedades como semánticamente equivalentes', () => {
    const json1 = JSON.stringify({ a: 1, b: 2, c: { d: 3, e: 4 } });
    const json2 = JSON.stringify({ b: 2, c: { e: 4, d: 3 }, a: 1 });

    expect(sonJsonSemanticamenteEquivalentes(json1, json2)).toBe(true);
  });

  it('preserva estrictamente el orden de los arrays (orden no conmutativo)', () => {
    const json1 = JSON.stringify({ catalogo: ['001', 'G-IVM'] });
    const json2 = JSON.stringify({ catalogo: ['G-IVM', '001'] });

    expect(sonJsonSemanticamenteEquivalentes(json1, json2)).toBe(false);
  });

  it('distingue tipos primitivos estrictamente: 0 vs null vs false vs "0"', () => {
    expect(sonJsonSemanticamenteEquivalentes({ val: 0 }, { val: '0' })).toBe(false);
    expect(sonJsonSemanticamenteEquivalentes({ val: 0 }, { val: null })).toBe(false);
    expect(sonJsonSemanticamenteEquivalentes({ val: 0 }, { val: false })).toBe(false);
    expect(sonJsonSemanticamenteEquivalentes({ val: null }, { val: false })).toBe(false);
    expect(sonJsonSemanticamenteEquivalentes({ val: false }, { val: 'false' })).toBe(false);
  });

  it('preserva cadenas de texto exactas como "001" y "G-IVM"', () => {
    const json1 = JSON.stringify({
      catalogos: [
        {
          codigo: 'CAT_TEST',
          elementos: [
            { codigo: '001', valor: 'Primero' },
            { codigo: 'G-IVM', valor: 'Grupo IVM' }
          ]
        }
      ]
    });

    const json2 = JSON.stringify({
      catalogos: [
        {
          elementos: [
            { valor: 'Primero', codigo: '001' },
            { codigo: 'G-IVM', valor: 'Grupo IVM' }
          ],
          codigo: 'CAT_TEST'
        }
      ]
    });

    expect(sonJsonSemanticamenteEquivalentes(json1, json2)).toBe(true);
  });

  it('retorna false ante cadenas JSON inválidas', () => {
    expect(sonJsonSemanticamenteEquivalentes('{ clave: invalida }', '{"clave": "valida"}')).toBe(false);
    expect(sonJsonSemanticamenteEquivalentes('{"clave": "valida"}', '{ clave: invalida }')).toBe(false);
  });

  it('canonicaliza correctamente estructuras anidadas', () => {
    const entrada = {
      z: [3, 2, 1],
      b: { y: 2, x: 1 },
      a: 'test'
    };

    const canon = canonicalizarValor(entrada) as Record<string, unknown>;
    expect(Object.keys(canon)).toEqual(['a', 'b', 'z']);
    expect(Object.keys(canon['b'] as Record<string, unknown>)).toEqual(['x', 'y']);
    expect(canon['z']).toEqual([3, 2, 1]); // Array preserves order
  });
});
