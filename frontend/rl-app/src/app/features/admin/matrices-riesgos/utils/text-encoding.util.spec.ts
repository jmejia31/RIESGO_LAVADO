import { contieneMojibakeVisible, normalizarMojibakeVisibleUtf8, normalizarTextoVisibleUtf8 } from './text-encoding.util';

describe('Integridad de texto visible UTF-8', () => {
  it('repara mojibake UTF-8/Windows-1252 común', () => {
    expect(normalizarTextoVisibleUtf8('Identificaci\u00C3\u00B3n')).toBe('Identificación');
    expect(normalizarTextoVisibleUtf8('\u00C3\u0081rea')).toBe('Área');
    expect(normalizarTextoVisibleUtf8('Due\u00C3\u00B1o')).toBe('Dueño');
    expect(normalizarTextoVisibleUtf8('Cr\u00C3\u00ADtico')).toBe('Crítico');
  });

  it('reconstruye términos institucionales cuando el carácter original fue reemplazado', () => {
    const bad = '\u00EF\u00BF\u00BD';
    expect(normalizarTextoVisibleUtf8(`Identificaci${bad}n del riesgo`)).toBe('Identificación del riesgo');
    expect(normalizarTextoVisibleUtf8(`${bad}rea principal`)).toBe('Área principal');
    expect(normalizarTextoVisibleUtf8(`Due${bad}o del riesgo`)).toBe('Dueño del riesgo');
    expect(normalizarTextoVisibleUtf8(`Objetivo(s) estrat${bad}gico(s)`)).toBe('Objetivo(s) estratégico(s)');
    expect(normalizarTextoVisibleUtf8(`R${bad}gimen afectado`)).toBe('Régimen afectado');
    expect(normalizarTextoVisibleUtf8(`interrelaci${bad}n`)).toBe('interrelación');
    expect(normalizarTextoVisibleUtf8(`Valoraci${bad}n`)).toBe('Valoración');
  });

  it('repara los patrones observados en nombres de riesgos institucionales', () => {
    const bad = '\u00EF\u00BF\u00BD';

    expect(normalizarTextoVisibleUtf8(`Registro de proveedores con informaci${bad}n inconsistente`))
      .toBe('Registro de proveedores con información inconsistente');
    expect(normalizarTextoVisibleUtf8(`Empresas que no est${bad}n inscritas`))
      .toBe('Empresas que no están inscritas');
    expect(normalizarTextoVisibleUtf8(`evaluaci${bad}n t${bad}cnica, econ${bad}mica`))
      .toBe('evaluación técnica, económica');
    expect(normalizarTextoVisibleUtf8(`Definici${bad}n sin garant${bad}as adecuadas de ejecuci${bad}n`))
      .toBe('Definición sin garantías adecuadas de ejecución');
    expect(normalizarTextoVisibleUtf8(`t${bad}rminos de referencia de una licitaci${bad}n p${bad}blica`))
      .toBe('términos de referencia de una licitación pública');
  });

  it('el normalizador de mojibake preserva texto limpio aunque tenga ortografía histórica', () => {
    expect(normalizarMojibakeVisibleUtf8('Version del Area responsable')).toBe('Version del Area responsable');
  });

  it('corrige términos visibles sin tilde sin alterar identificadores técnicos ajenos al helper', () => {
    expect(normalizarTextoVisibleUtf8('Identificacion del riesgo')).toBe('Identificación del riesgo');
    expect(normalizarTextoVisibleUtf8('Area responsable')).toBe('Área responsable');
    expect(normalizarTextoVisibleUtf8('Version')).toBe('Versión');
  });

  it('detecta marcadores de mojibake', () => {
    expect(contieneMojibakeVisible('texto correcto')).toBe(false);
    expect(contieneMojibakeVisible('Due\u00C3\u00B1o')).toBe(true);
    expect(contieneMojibakeVisible('Due\u00EF\u00BF\u00BDo')).toBe(true);
    expect(contieneMojibakeVisible('Due\uFFFDo')).toBe(true);
  });
});
