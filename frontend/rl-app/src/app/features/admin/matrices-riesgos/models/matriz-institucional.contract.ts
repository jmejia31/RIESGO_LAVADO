export type MatrixFieldMode = 'INPUT' | 'COMPUTED' | 'MASTER' | 'REPEATER' | 'MONITORING' | 'EVIDENCE';

export interface MatrixFieldContract {
  ordinal: number;
  label: string;
  block: 1 | 2 | 3 | 4 | 5 | 6;
  excelColumn: string;
  mode: MatrixFieldMode;
  editable: boolean;
  key: string | null;
  source: string;
  implemented: boolean;
}

type MatrixFieldSeed = Pick<MatrixFieldContract, 'label' | 'mode' | 'editable' | 'key' | 'source'>;

const BLOCK_RANGES = [19, 33, 39, 49, 69, 82] as const;

const SEEDS: readonly MatrixFieldSeed[] = [
  { label: 'No.', mode: 'MASTER', editable: false, key: null, source: 'PRESENTATION_ONLY' },
  { label: 'Código de Riesgo', mode: 'MASTER', editable: false, key: null, source: 'RL_MR_RIESGOS.RIE_CODIGO' },
  { label: 'Área', mode: 'INPUT', editable: true, key: 'area_principal', source: 'EVA_DATOS_JSON.area_principal / PROY_AREA_PRINCIPAL' },
  { label: 'Área Consolidada', mode: 'INPUT', editable: true, key: 'area_consolidada', source: 'EVA_DATOS_JSON.area_consolidada; solo versiones que lo declaren' },
  { label: 'Tipo de Riesgo', mode: 'INPUT', editable: true, key: 'tipo_riesgo', source: 'EVA_DATOS_JSON.tipo_riesgo; solo versiones que lo declaren' },
  { label: 'Procedimiento', mode: 'INPUT', editable: true, key: 'procedimiento', source: 'EVA_DATOS_JSON.procedimiento; solo versiones que lo declaren' },
  { label: 'Objetivo(s) Estratégico(s)', mode: 'INPUT', editable: true, key: 'objetivos_estrategicos', source: 'EVA_DATOS_JSON.objetivos_estrategicos; solo versiones que lo declaren' },
  { label: 'Riesgo Inherente', mode: 'MASTER', editable: false, key: null, source: 'RL_MR_RIESGOS.RIE_NOMBRE' },
  { label: 'Evaluación', mode: 'MASTER', editable: false, key: null, source: 'RL_MR_RIESGOS.RIE_DESCRIPCION' },
  { label: 'Frecuencia', mode: 'INPUT', editable: true, key: 'frecuencia_inherente', source: 'EVA_DATOS_JSON.frecuencia_inherente / MR_FRECUENCIA_1_5' },
  { label: 'Impacto', mode: 'INPUT', editable: true, key: 'impacto_inherente', source: 'EVA_DATOS_JSON.impacto_inherente / MR_IMPACTO_1_5' },
  { label: 'Valor del Riesgo Inherente', mode: 'COMPUTED', editable: false, key: 'valor_riesgo_inherente', source: 'Backend F01 / EVA_CALCULOS_JSON / PROY_VRI' },
  { label: 'Nivel de Riesgo Inherente', mode: 'COMPUTED', editable: false, key: 'nivel_riesgo_inherente', source: 'Backend F02 / EVA_CALCULOS_JSON / catálogo institucional' },
  { label: 'Responsable o dueño del riesgo', mode: 'INPUT', editable: true, key: 'dueno_riesgo', source: 'EVA_DATOS_JSON.dueno_riesgo / PROY_DUENO_RIESGO' },
  { label: 'Régimen afectado', mode: 'INPUT', editable: true, key: 'regimen_afectado', source: 'EVA_DATOS_JSON.regimen_afectado; solo versiones que lo declaren' },
  { label: 'Transversalidad o Interrelación con otros Riesgos', mode: 'INPUT', editable: true, key: 'transversalidad', source: 'EVA_DATOS_JSON.transversalidad; solo versiones que lo declaren' },
  { label: 'Amenazas (Solo para riesgos de GTIC)', mode: 'INPUT', editable: true, key: 'amenazas_gtic', source: 'EVA_DATOS_JSON.amenazas_gtic; criterio GTIC controlado pendiente' },
  { label: 'Vulnerabilidades (Solo para riesgos de GTIC)', mode: 'INPUT', editable: true, key: 'vulnerabilidades_gtic', source: 'EVA_DATOS_JSON.vulnerabilidades_gtic; criterio GTIC controlado pendiente' },
  { label: 'Activos de Información (Solo para riesgos de GTIC)', mode: 'INPUT', editable: true, key: 'activos_informacion_gtic', source: 'EVA_DATOS_JSON.activos_informacion_gtic; criterio GTIC controlado pendiente' },
  { label: 'Descripción de Control(es) Preventivo(s)', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (PREVENTIVO)' },
  { label: 'Escala de efectividad preventivo', mode: 'INPUT', editable: true, key: null, source: 'RL_MR_EVALUACIONES_CONTROL.ECO_EFECTIVIDAD' },
  { label: 'Nivel de efectividad preventivo', mode: 'COMPUTED', editable: false, key: null, source: 'Derivado por catálogo institucional' },
  { label: '% efectividad preventivo', mode: 'INPUT', editable: true, key: 'controles_preventivo', source: 'EVA_DATOS_JSON.controles_preventivo' },
  { label: 'Descripción de Control(es) Detectivo(s)', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (DETECTIVO)' },
  { label: 'Escala de efectividad detectivo', mode: 'INPUT', editable: true, key: null, source: 'RL_MR_EVALUACIONES_CONTROL.ECO_EFECTIVIDAD' },
  { label: 'Nivel de efectividad detectivo', mode: 'COMPUTED', editable: false, key: null, source: 'Derivado por catálogo institucional' },
  { label: '% efectividad detectivo', mode: 'INPUT', editable: true, key: 'controles_detectivo', source: 'EVA_DATOS_JSON.controles_detectivo' },
  { label: 'Descripción de Control(es) Correctivo(s)', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (CORRECTIVO)' },
  { label: 'Escala de efectividad correctivo', mode: 'INPUT', editable: true, key: null, source: 'RL_MR_EVALUACIONES_CONTROL.ECO_EFECTIVIDAD' },
  { label: 'Nivel de efectividad correctivo', mode: 'COMPUTED', editable: false, key: null, source: 'Derivado por catálogo institucional' },
  { label: '% efectividad correctivo', mode: 'INPUT', editable: true, key: 'controles_correctivo', source: 'EVA_DATOS_JSON.controles_correctivo' },
  { label: 'Nivel de Automatización de los Controles', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_AUTOMATIZACION' },
  { label: 'Efectividad Total Ponderada de los Controles', mode: 'COMPUTED', editable: false, key: 'efectividad_total_ponderada', source: 'Runtime institucional' },
  { label: 'Riesgo Residual', mode: 'MASTER', editable: false, key: null, source: 'RL_MR_RIESGOS.RIE_NOMBRE' },
  { label: 'Frecuencia Residual', mode: 'INPUT', editable: true, key: 'frecuencia_residual', source: 'Contrato de evaluación / MR_FRECUENCIA_1_5' },
  { label: 'Impacto Residual', mode: 'INPUT', editable: true, key: 'impacto_residual', source: 'Contrato de evaluación / MR_IMPACTO_1_5' },
  { label: 'Valor del Riesgo Residual', mode: 'COMPUTED', editable: false, key: 'valor_riesgo_residual', source: 'Runtime institucional / PROY_VRR' },
  { label: 'Nivel del Riesgo Residual', mode: 'COMPUTED', editable: false, key: 'nivel_riesgo_residual', source: 'Runtime institucional / catálogo institucional' },
  { label: 'Respuesta al riesgo', mode: 'INPUT', editable: true, key: 'respuesta_riesgo', source: 'Contrato de evaluación / MR_RESPUESTA_RIESGO' },
  { label: 'Plan de Mitigación/Acciones Correctivas', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_PLANES.PLA_DESCRIPCION' },
  { label: 'No. Acciones de Mitigación', mode: 'COMPUTED', editable: false, key: null, source: 'Conteo de acciones persistidas' },
  { label: 'Actividades', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_ACTIVIDADES.ACT_DESCRIPCION' },
  { label: 'Cantidad de Actividades', mode: 'COMPUTED', editable: false, key: null, source: 'Conteo de actividades persistidas' },
  { label: 'Monitoreo/Seguimiento', mode: 'MONITORING', editable: true, key: null, source: 'RL_MR_AUTOMONITOREO; bloque pendiente' },
  { label: 'Responsables', mode: 'REPEATER', editable: true, key: null, source: 'Responsables de actividades' },
  { label: 'Fecha inicio', mode: 'INPUT', editable: true, key: null, source: 'Fecha de plan/actividad; bloque pendiente' },
  { label: 'Fecha final', mode: 'INPUT', editable: true, key: null, source: 'Fecha de plan/actividad; bloque pendiente' },
  { label: 'Recursos', mode: 'INPUT', editable: true, key: null, source: 'Plan de mitigación; bloque pendiente' },
  { label: 'Presupuesto', mode: 'INPUT', editable: true, key: null, source: 'RL_MR_PLANES.PLA_PRESUPUESTO; bloque pendiente' },
  { label: 'Frecuencia Residual (AUX)', mode: 'COMPUTED', editable: false, key: 'frecuencia_residual_aux', source: 'Runtime institucional' },
  { label: 'Impacto Residual (AUX)', mode: 'COMPUTED', editable: false, key: 'impacto_residual_aux', source: 'Runtime institucional' },
  { label: 'Suma Residual redondeada (AUX)', mode: 'COMPUTED', editable: false, key: 'suma_residual_redondeada_aux', source: 'Runtime institucional' },
  { label: 'F_base (AUX)', mode: 'COMPUTED', editable: false, key: 'f_base', source: 'Runtime institucional' },
  { label: 'I_base (AUX)', mode: 'COMPUTED', editable: false, key: 'i_base', source: 'Runtime institucional' },
  { label: 'Tope F (AUX)', mode: 'COMPUTED', editable: false, key: 'tope_f', source: 'Runtime institucional' },
  { label: 'Tope I (AUX)', mode: 'COMPUTED', editable: false, key: 'tope_i', source: 'Runtime institucional' },
  { label: 'Capacidad F (AUX)', mode: 'COMPUTED', editable: false, key: 'capacidad_f_aux', source: 'Runtime institucional' },
  { label: 'Capacidad I (AUX)', mode: 'COMPUTED', editable: false, key: 'capacidad_i_aux', source: 'Runtime institucional' },
  { label: 'Resto (AUX)', mode: 'COMPUTED', editable: false, key: 'resto_aux', source: 'Runtime institucional' },
  { label: 'Prefiere I (AUX)', mode: 'COMPUTED', editable: false, key: 'prefiere_i_aux', source: 'Runtime institucional' },
  { label: 'Inc_I (AUX)', mode: 'COMPUTED', editable: false, key: 'incremento_i_aux', source: 'Runtime institucional' },
  { label: 'Inc_F (AUX)', mode: 'COMPUTED', editable: false, key: 'incremento_f_aux', source: 'Runtime institucional' },
  { label: 'Valor del Riesgo Residual (AUX)', mode: 'COMPUTED', editable: false, key: 'valor_riesgo_residual_aux', source: 'Runtime institucional' },
  { label: 'Verificación', mode: 'COMPUTED', editable: false, key: 'verificacion', source: 'Runtime institucional' },
  { label: 'VRR 2', mode: 'COMPUTED', editable: false, key: 'vrr_2', source: 'Runtime institucional' },
  { label: 'Verificar VRR 2', mode: 'COMPUTED', editable: false, key: 'verificar_vrr_2', source: 'Runtime institucional' },
  { label: 'Verificar Frec', mode: 'COMPUTED', editable: false, key: 'verificar_frecuencia', source: 'Runtime institucional' },
  { label: 'Verificar Impact', mode: 'COMPUTED', editable: false, key: 'verificar_impacto', source: 'Runtime institucional' },
  { label: 'VRI-VRR', mode: 'COMPUTED', editable: false, key: 'diferencia_vri_vrr', source: 'Runtime institucional / F34_DIFERENCIA_VRI_VRR' },
  { label: 'Señales de Alerta', mode: 'MONITORING', editable: true, key: null, source: 'RL_MR_SENALES_ALERTA' },
  { label: 'Estado del Riesgo', mode: 'MONITORING', editable: true, key: null, source: 'Workflow institucional' },
  { label: 'Estado del Control Preventivo', mode: 'MONITORING', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO (PREVENTIVO)' },
  { label: 'Evaluación de la Efectividad del Control Preventivo', mode: 'MONITORING', editable: true, key: null, source: 'RL_MR_EVALUACIONES_CONTROL (PREVENTIVO)' },
  { label: 'Evidencia(s) del Control Preventivo', mode: 'EVIDENCE', editable: true, key: null, source: 'RL_MR_EVIDENCIAS_VINCULOS (PREVENTIVO)' },
  { label: 'Estado del Control Detectivo', mode: 'MONITORING', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO (DETECTIVO)' },
  { label: 'Evaluación de la Efectividad del Control Detectivo', mode: 'MONITORING', editable: true, key: null, source: 'RL_MR_EVALUACIONES_CONTROL (DETECTIVO)' },
  { label: 'Evidencia(s) del Control Detectivo', mode: 'EVIDENCE', editable: true, key: null, source: 'RL_MR_EVIDENCIAS_VINCULOS (DETECTIVO)' },
  { label: 'Estado del Control Correctivo', mode: 'MONITORING', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO (CORRECTIVO)' },
  { label: 'Evaluación de la Efectividad del Control Correctivo', mode: 'MONITORING', editable: true, key: null, source: 'RL_MR_EVALUACIONES_CONTROL (CORRECTIVO)' },
  { label: 'Evidencia(s) del Control Correctivo', mode: 'EVIDENCE', editable: true, key: null, source: 'RL_MR_EVIDENCIAS_VINCULOS (CORRECTIVO)' },
  { label: 'Observaciones del Área', mode: 'INPUT', editable: true, key: null, source: 'Contrato de observaciones pendiente' },
  { label: 'Observaciones UGR', mode: 'INPUT', editable: true, key: null, source: 'Contrato de observaciones pendiente' }
];

function blockForOrdinal(ordinal: number): MatrixFieldContract['block'] {
  const index = BLOCK_RANGES.findIndex(lastOrdinal => ordinal <= lastOrdinal);
  return (index + 1) as MatrixFieldContract['block'];
}

function excelColumnForOrdinal(ordinal: number): string {
  let remainder = ordinal;
  let column = '';
  while (remainder > 0) {
    remainder--;
    column = String.fromCharCode(65 + remainder % 26) + column;
    remainder = Math.floor(remainder / 26);
  }
  return column;
}

export const MATRIX_FIELDS: readonly MatrixFieldContract[] = Object.freeze(
  SEEDS.map((seed, index) => {
    const ordinal = index + 1;
    return Object.freeze({
      ...seed,
      ordinal,
      block: blockForOrdinal(ordinal),
      excelColumn: excelColumnForOrdinal(ordinal),
      implemented: ordinal <= 19
    });
  })
);

export const MATRIX_BLOCK_TITLES: Readonly<Record<MatrixFieldContract['block'], string>> = Object.freeze({
  1: '1. Identificación y Riesgo Inherente',
  2: '2. Controles',
  3: '3. Riesgo Residual y Respuesta',
  4: '4. Plan de Mitigación / Acciones Correctivas',
  5: '5. Cálculos Auxiliares y Verificaciones',
  6: '6. Monitoreo, Efectividad y Observaciones'
});

export const MATRIX_BLOCK_1_FIELDS = MATRIX_FIELDS.filter(field => field.block === 1);
