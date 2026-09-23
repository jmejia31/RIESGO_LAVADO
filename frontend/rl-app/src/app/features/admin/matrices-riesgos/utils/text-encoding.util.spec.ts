import { contieneMojibakeVisible, normalizarTextoVisibleUtf8 } from './text-encoding.util';

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
