import { normalizarJsonABuilderModel, serializarBuilderModelAJson } from './form-builder.models';

describe('Form builder — fórmula manual', () => {
  it('conserva la intención manual y no crea una vinculación central', () => {
    const model = normalizarJsonABuilderModel(JSON.stringify({
      secciones: [{ clave: 'riesgo', campos: [{
        clave: 'vri', etiqueta: 'VRI', tipo: 'formula', formula: 'frecuencia + impacto - 1',
        formulaModo: 'manual', formulaNombre: 'VRI manual', formulaTipoResultado: 'DECIMAL'
      }] }]
    }));

    const field = model.secciones[0].campos[0];
    expect(field.formulaModo).toBe('manual');
    const serialized = JSON.parse(serializarBuilderModelAJson(model)) as { secciones: Array<{ campos: Array<Record<string, unknown>> }> };
    const raw = serialized.secciones[0].campos[0];
    expect(raw['formula']).toBe('frecuencia + impacto - 1');
    expect(raw['formulaModo']).toBe('manual');
    expect(raw['formulaNombre']).toBe('VRI manual');
    expect(raw['formulaTipoResultado']).toBe('DECIMAL');
    expect(raw['formulaVersionId']).toBeUndefined();
  });

  it('elimina metadatos centrales heredados al serializar modo manual', () => {
    const model = normalizarJsonABuilderModel(JSON.stringify({
      secciones: [{ clave: 'riesgo', campos: [{
        clave: 'vri', etiqueta: 'VRI', tipo: 'formula', formula: 'frecuencia + impacto - 1',
        formulaModo: 'manual', formulaId: 10, formulaVersionId: 0,
        formulaCodigo: 'F01', formulaVersion: 1
      }] }]
    }));

    const raw = JSON.parse(serializarBuilderModelAJson(model)) as { secciones: Array<{ campos: Array<Record<string, unknown>> }> };
    const field = raw.secciones[0].campos[0];
    expect(field['formulaId']).toBeUndefined();
    expect(field['formulaVersionId']).toBeUndefined();
    expect(field['formulaCodigo']).toBeUndefined();
    expect(field['formulaVersion']).toBeUndefined();
  });

  it('mantiene la vinculación central exacta y elimina la fórmula embebida', () => {
    const model = normalizarJsonABuilderModel(JSON.stringify({
      secciones: [{ clave: 'riesgo', campos: [{
        clave: 'vri', etiqueta: 'VRI', tipo: 'formula', formula: 'frecuencia + impacto - 1',
        formulaId: 10, formulaVersionId: 11, formulaCodigo: 'F01', formulaVersion: 1
      }] }]
    }));

    const raw = JSON.parse(serializarBuilderModelAJson(model)) as { secciones: Array<{ campos: Array<Record<string, unknown>> }> };
    const field = raw.secciones[0].campos[0];
    expect(field['formulaVersionId']).toBe(11);
    expect(field['formulaCodigo']).toBe('F01');
    expect(field['formula']).toBeUndefined();
  });
});
