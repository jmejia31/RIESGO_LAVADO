import { contieneMojibakeVisible, normalizarMojibakeVisibleUtf8, normalizarTextoVisibleUtf8 } from './text-encoding.util';

describe('Integridad de texto visible UTF-8', () => {
  it('repara corrupción contextual y secuencias UTF-8 mal interpretadas', () => {
    expect(normalizarTextoVisibleUtf8('informaci\u00BFn')).toBe('información');
    expect(normalizarTextoVisibleUtf8('informaci\uFFFDn')).toBe('informaci\uFFFDn');
    expect(normalizarTextoVisibleUtf8('Informaci\u00C3\u00B3n t\u00C3\u00A9cnica')).toBe('Información técnica');
    expect(normalizarTextoVisibleUtf8('Due\u00C3\u00B1o')).toBe('Dueño');
    expect(normalizarTextoVisibleUtf8('Due\u00EF\u00BFo')).toBe('Dueño');
    expect(normalizarTextoVisibleUtf8('Descripci\u00BFn: vinculaci\u00BFn, P\u00BFrdidas econ\u00BFmicas, verificaci\u00BFn, validaci\u00BFn, instituci\u00BFn, autom\u00BFticos e il\u00BFcitas'))
      .toBe('Descripción: vinculación, Pérdidas económicas, verificación, validación, institución, automáticos e ilícitas');
  });

  it('preserva caracteres españoles y signos de pregunta legítimos', () => {
    expect(normalizarTextoVisibleUtf8('¿Qué información necesita?')).toBe('¿Qué información necesita?');
    expect(normalizarTextoVisibleUtf8('áéíóú ÁÉÍÓÚ ñÑ üÜ ¡! ¿?')).toBe('áéíóú ÁÉÍÓÚ ñÑ üÜ ¡! ¿?');
    expect(normalizarTextoVisibleUtf8('Secci\u00BFn de Cumplimiento')).toBe('Sección de Cumplimiento');
    expect(normalizarTextoVisibleUtf8('Afiliaci\u00BFn/Control Patronal')).toBe('Afiliación/Control Patronal');
  });

  it('detecta corrupción sin marcar texto sano', () => {
    expect(contieneMojibakeVisible('informaci\u00BFn')).toBe(true);
    expect(contieneMojibakeVisible('Due\u00C3\u00B1o')).toBe(true);
    expect(contieneMojibakeVisible('Due\u00EF\u00BFo')).toBe(true);
    expect(contieneMojibakeVisible('\uFFFD')).toBe(true);
    expect(contieneMojibakeVisible('¿Qué información necesita?')).toBe(false);
    expect(contieneMojibakeVisible('Dueño')).toBe(false);
  });
});
