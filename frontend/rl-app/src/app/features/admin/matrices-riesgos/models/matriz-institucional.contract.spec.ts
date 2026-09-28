import { MATRIX_BLOCK_1_FIELDS, MATRIX_FIELDS } from './matriz-institucional.contract';

describe('contrato canónico de la Matriz institucional', () => {
  it('define las 82 posiciones una sola vez con ordinales y columnas continuas', () => {
    expect(MATRIX_FIELDS).toHaveLength(82);
    expect(MATRIX_FIELDS.map(field => field.ordinal)).toEqual(Array.from({ length: 82 }, (_, index) => index + 1));
    expect(new Set(MATRIX_FIELDS.map(field => field.ordinal)).size).toBe(82);
    expect(MATRIX_FIELDS[0].excelColumn).toBe('A');
    expect(MATRIX_FIELDS[81].excelColumn).toBe('CD');
  });

  it('define literalmente los 19 encabezados del bloque 1 en su orden institucional', () => {
    expect(MATRIX_BLOCK_1_FIELDS.map(field => field.label)).toEqual([
      'No.',
      'Código de Riesgo',
      'Área',
      'Área Consolidada',
      'Tipo de Riesgo',
      'Procedimiento',
      'Objetivo(s) Estratégico(s)',
      'Riesgo Inherente',
      'Evaluación',
      'Frecuencia',
      'Impacto',
      'Valor del Riesgo Inherente',
      'Nivel de Riesgo Inherente',
      'Responsable o dueño del riesgo',
      'Régimen afectado',
      'Transversalidad o Interrelación con otros Riesgos',
      'Amenazas (Solo para riesgos de GTIC)',
      'Vulnerabilidades (Solo para riesgos de GTIC)',
      'Activos de Información (Solo para riesgos de GTIC)'
    ]);
    expect(MATRIX_BLOCK_1_FIELDS.map(field => field.ordinal)).toEqual(Array.from({ length: 19 }, (_, index) => index + 1));
    expect(MATRIX_BLOCK_1_FIELDS.every(field => field.implemented)).toBe(true);
  });

  it('mantiene el ordinal 01 como presentación y los resultados 12/13 calculados no editables', () => {
    expect(MATRIX_FIELDS[0]).toMatchObject({ ordinal: 1, key: null, editable: false, source: 'PRESENTATION_ONLY' });
    expect(MATRIX_FIELDS[11]).toMatchObject({ ordinal: 12, mode: 'COMPUTED', editable: false });
    expect(MATRIX_FIELDS[12]).toMatchObject({ ordinal: 13, mode: 'COMPUTED', editable: false });
    expect(MATRIX_BLOCK_1_FIELDS.slice(16).map(field => field.ordinal)).toEqual([17, 18, 19]);
  });
});
