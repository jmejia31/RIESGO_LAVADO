import { CATALOGO_RESPUESTA_RIESGO, MATRIX_BLOCK_1_FIELDS, MATRIX_BLOCK_2_FIELDS, MATRIX_BLOCK_3_FIELDS, MATRIX_BLOCK_4_FIELDS, MATRIX_BLOCK_5_FIELDS, MATRIX_BLOCK_6_FIELDS, MATRIX_FIELDS } from './matriz-institucional.contract';

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

  it('define el contrato literal 34–39 de Riesgo Residual y Respuesta', () => {
    expect(MATRIX_BLOCK_3_FIELDS).toHaveLength(6);
    expect(MATRIX_BLOCK_3_FIELDS.map(field => field.ordinal)).toEqual([34, 35, 36, 37, 38, 39]);
    expect(MATRIX_BLOCK_3_FIELDS.map(field => field.excelColumn)).toEqual(['AH', 'AI', 'AJ', 'AK', 'AL', 'AM']);
    expect(MATRIX_BLOCK_3_FIELDS.map(field => field.label)).toEqual([
      'Riesgo Residual',
      'Frecuencia Residual',
      'Impacto Residual',
      'Valor del Riesgo Residual',
      'Nivel del Riesgo Residual',
      'Respuesta al riesgo'
    ]);
    expect(MATRIX_BLOCK_3_FIELDS.map(field => [field.mode, field.editable])).toEqual([
      ['COMPUTED', false],
      ['COMPUTED', false],
      ['COMPUTED', false],
      ['COMPUTED', false],
      ['COMPUTED', false],
      ['INPUT', true]
    ]);
    expect(MATRIX_BLOCK_3_FIELDS.every(field => field.implemented)).toBe(true);
    expect(MATRIX_FIELDS.filter(field => field.implemented)).toHaveLength(82);
    expect(CATALOGO_RESPUESTA_RIESGO).toEqual(['EVITAR', 'MITIGAR', 'TRANSFERIR', 'ACEPTAR']);
  });

  it('define exactamente los campos 70–82 con etiquetas institucionales y fuentes no workflow', () => {
    expect(MATRIX_BLOCK_6_FIELDS.map(field => field.ordinal)).toEqual(Array.from({ length: 13 }, (_, index) => index + 70));
    expect(MATRIX_BLOCK_6_FIELDS.map(field => field.label)).toEqual([
      'Señales de Alerta', 'Estado del Riesgo', 'Estado del Control Preventivo',
      'Evaluación de la Efectividad del Control Preventivo', 'Evidencia(s) del Control Preventivo',
      'Estado del Control Detectivo', 'Evaluación de la Efectividad del Control Detectivo',
      'Evidencia(s) del Control Detectivo', 'Estado del Control Correctivo',
      'Evaluación de la Efectividad del Control Correctivo', 'Evidencia(s) del Control Correctivo',
      'Observaciones del Área', 'Observaciones UGR'
    ]);
    expect(MATRIX_BLOCK_6_FIELDS.every(field => field.implemented && !field.editable)).toBe(true);
    expect(MATRIX_BLOCK_6_FIELDS[1].source).toContain('MON_ESTADO_RIESGO');
    expect(MATRIX_BLOCK_6_FIELDS[1].source).not.toContain('WORKFLOW');
    expect(MATRIX_FIELDS.filter(field => field.implemented)).toHaveLength(82);
  });

  it('define exactamente los diez campos 40–49 con labels, orden y mapeos institucionales', () => {
    expect(MATRIX_BLOCK_4_FIELDS).toHaveLength(10);
    expect(MATRIX_BLOCK_4_FIELDS.map(field => field.ordinal)).toEqual([40, 41, 42, 43, 44, 45, 46, 47, 48, 49]);
    expect(MATRIX_BLOCK_4_FIELDS.map(field => field.label)).toEqual([
      'Plan de Mitigación/Acciones Correctivas',
      'No. Acciones de Mitigación',
      'Actividades',
      'Cantidad de Actividades',
      'Monitoreo/ Seguimiento',
      'Responsables',
      'Fecha inicio',
      'Fecha final',
      'Recursos',
      'Presupuesto'
    ]);
    expect(MATRIX_BLOCK_4_FIELDS.map(field => field.excelColumn)).toEqual(['AN', 'AO', 'AP', 'AQ', 'AR', 'AS', 'AT', 'AU', 'AV', 'AW']);
    expect(MATRIX_BLOCK_4_FIELDS.every(field => field.implemented && !field.editable)).toBe(true);
    expect(MATRIX_FIELDS.filter(field => field.implemented)).toHaveLength(82);
    expect(MATRIX_BLOCK_4_FIELDS[0].source).toBe('RL_MR_PLANES.PLA_DESCRIPCION');
    expect(MATRIX_BLOCK_4_FIELDS[2].source).toContain('ACT_PLAN_ID → PLA_ID');
    expect(MATRIX_BLOCK_4_FIELDS[4].source).toBe('RL_MR_PLANES.PLA_MONITOREO_SEGUIMIENTO');
    expect(MATRIX_BLOCK_4_FIELDS[5].source).toBe('RL_MR_PLANES.PLA_RESPONSABLES');
    expect(MATRIX_BLOCK_4_FIELDS[8].source).toBe('RL_MR_PLANES.PLA_RECURSOS');
  });

  it('define exactamente los veinte campos computados y de solo lectura 50–69', () => {
    expect(MATRIX_BLOCK_5_FIELDS).toHaveLength(20);
    expect(MATRIX_BLOCK_5_FIELDS.map(field => field.ordinal)).toEqual(Array.from({ length: 20 }, (_, index) => index + 50));
    expect(MATRIX_BLOCK_5_FIELDS.map(field => field.label)).toEqual([
      'Frecuencia Residual (AUX)', 'Impacto Residual (AUX)', 'Suma Residual redondeada (AUX)',
      'F_base (AUX)', 'I_base (AUX)', 'Tope F (AUX)', 'Tope I (AUX)', 'Capacidad F (AUX)',
      'Capacidad I (AUX)', 'Resto (AUX)', 'Prefiere I (AUX)', 'Inc_I (AUX)', 'Inc_F (AUX)',
      'Valor del Riesgo Residual (AUX)', 'Verificación', 'VRR 2', 'Verificar VRR 2',
      'Verificar Frec', 'Verificar Impact', 'VRI-VRR'
    ]);
    expect(MATRIX_BLOCK_5_FIELDS.map(field => field.excelColumn)).toEqual([
      'AX', 'AY', 'AZ', 'BA', 'BB', 'BC', 'BD', 'BE', 'BF', 'BG', 'BH', 'BI', 'BJ', 'BK', 'BL', 'BM', 'BN', 'BO', 'BP', 'BQ'
    ]);
    expect(MATRIX_BLOCK_5_FIELDS.every(field => field.mode === 'COMPUTED' && !field.editable && field.implemented)).toBe(true);
    expect(MATRIX_FIELDS.filter(field => field.implemented)).toHaveLength(82);
    expect(MATRIX_FIELDS.slice(69).every(field => field.implemented)).toBe(true);
  });
});
