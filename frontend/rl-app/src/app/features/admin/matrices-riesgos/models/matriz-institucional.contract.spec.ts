import { MATRIX_BLOCK_1_FIELDS, MATRIX_BLOCK_2_FIELDS, MATRIX_FIELDS } from './matriz-institucional.contract';

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

  it('define el contrato literal 20–33 sin filtrar evaluación de monitoreo a la valoración inicial', () => {
    expect(MATRIX_BLOCK_2_FIELDS).toHaveLength(14);
    expect(MATRIX_BLOCK_2_FIELDS.map(field => field.ordinal)).toEqual(Array.from({ length: 14 }, (_, index) => index + 20));
    expect(MATRIX_BLOCK_2_FIELDS.map(field => field.excelColumn)).toEqual(['T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'AA', 'AB', 'AC', 'AD', 'AE', 'AF', 'AG']);
    expect(MATRIX_BLOCK_2_FIELDS.map(field => field.label)).toEqual([
      'Descripción de Control(es) Preventivo(s)',
      'Escala de efectividad de control(es) preventivo(s)',
      'Nivel de efectividad de control(es) preventivo(s)',
      '% efectividad de control(es) preventivo(s)',
      'Descripción de Control(es) Detectivo(s)',
      'Escala de efectividad de control(es) detectivo(s)',
      'Nivel de efectividad de control(es) detectivo(s)',
      '% efectividad de control detectivo',
      'Descripción de Control(es) Correctivo(s)',
      'Escala de efectividad de control(es) correctivo(s)',
      'Nivel de efectividad de control(es) correctivo(s)',
      '% efectividad de control correctivo',
      'Nivel de Automatización de los Controles',
      'Efectividad Total Ponderada de los Controles'
    ]);
    expect(MATRIX_BLOCK_2_FIELDS.map(field => [field.mode, field.editable])).toEqual([
      ['REPEATER', true], ['INPUT', true], ['COMPUTED', false], ['COMPUTED', false],
      ['REPEATER', true], ['INPUT', true], ['COMPUTED', false], ['COMPUTED', false],
      ['REPEATER', true], ['INPUT', true], ['COMPUTED', false], ['COMPUTED', false],
      ['REPEATER', true], ['COMPUTED', false]
    ]);
    for (const field of [MATRIX_BLOCK_2_FIELDS[1], MATRIX_BLOCK_2_FIELDS[2], MATRIX_BLOCK_2_FIELDS[3], MATRIX_BLOCK_2_FIELDS[5], MATRIX_BLOCK_2_FIELDS[6], MATRIX_BLOCK_2_FIELDS[7], MATRIX_BLOCK_2_FIELDS[9], MATRIX_BLOCK_2_FIELDS[10], MATRIX_BLOCK_2_FIELDS[11]]) {
      expect(field.source).not.toContain('RL_MR_EVALUACIONES_CONTROL');
    }
  });
});
