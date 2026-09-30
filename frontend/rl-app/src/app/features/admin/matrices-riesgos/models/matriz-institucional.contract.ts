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
  { label: 'Descripción de Control(es) Preventivo(s)', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (CON_TIPO=PREVENTIVO)' },
  { label: 'Escala de efectividad de control(es) preventivo(s)', mode: 'INPUT', editable: true, key: 'escala_preventivo', source: 'Snapshot institucional CAT_EFECTIVIDAD_NIVEL / CAT_EFECTIVIDAD_PORCENTAJE; selector pendiente de contrato versionado' },
  { label: 'Nivel de efectividad de control(es) preventivo(s)', mode: 'COMPUTED', editable: false, key: 'nivel_control_preventivo', source: 'Backend F03 / CAT_EFECTIVIDAD_NIVEL' },
  { label: '% efectividad de control(es) preventivo(s)', mode: 'COMPUTED', editable: false, key: 'porcentaje_control_preventivo', source: 'Backend F04 / CAT_EFECTIVIDAD_PORCENTAJE; proporción 0..1' },
  { label: 'Descripción de Control(es) Detectivo(s)', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (CON_TIPO=DETECTIVO)' },
  { label: 'Escala de efectividad de control(es) detectivo(s)', mode: 'INPUT', editable: true, key: 'escala_detectivo', source: 'Snapshot institucional CAT_EFECTIVIDAD_NIVEL / CAT_EFECTIVIDAD_PORCENTAJE; selector pendiente de contrato versionado' },
  { label: 'Nivel de efectividad de control(es) detectivo(s)', mode: 'COMPUTED', editable: false, key: 'nivel_control_detectivo', source: 'Backend F05 / CAT_EFECTIVIDAD_NIVEL' },
  { label: '% efectividad de control detectivo', mode: 'COMPUTED', editable: false, key: 'porcentaje_control_detectivo', source: 'Backend F06 / CAT_EFECTIVIDAD_PORCENTAJE; proporción 0..1' },
  { label: 'Descripción de Control(es) Correctivo(s)', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION (CON_TIPO=CORRECTIVO)' },
  { label: 'Escala de efectividad de control(es) correctivo(s)', mode: 'INPUT', editable: true, key: 'escala_correctivo', source: 'Snapshot institucional CAT_EFECTIVIDAD_NIVEL / CAT_EFECTIVIDAD_PORCENTAJE; selector pendiente de contrato versionado' },
  { label: 'Nivel de efectividad de control(es) correctivo(s)', mode: 'COMPUTED', editable: false, key: 'nivel_control_correctivo', source: 'Backend F07 / CAT_EFECTIVIDAD_NIVEL' },
  { label: '% efectividad de control correctivo', mode: 'COMPUTED', editable: false, key: 'porcentaje_control_correctivo', source: 'Backend F08 / CAT_EFECTIVIDAD_PORCENTAJE; proporción 0..1' },
  { label: 'Nivel de Automatización de los Controles', mode: 'REPEATER', editable: true, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_AUTOMATIZACION por control' },
  { label: 'Efectividad Total Ponderada de los Controles', mode: 'COMPUTED', editable: false, key: 'efectividad_total_ponderada', source: 'Runtime institucional' },
  { label: 'Riesgo Residual', mode: 'COMPUTED', editable: false, key: 'riesgo_residual_descripcion', source: 'Backend F10 / Runtime institucional' },
  { label: 'Frecuencia Residual', mode: 'COMPUTED', editable: false, key: 'frecuencia_residual', source: 'Backend F11 / Runtime institucional' },
  { label: 'Impacto Residual', mode: 'COMPUTED', editable: false, key: 'impacto_residual', source: 'Backend F12 / Runtime institucional' },
  { label: 'Valor del Riesgo Residual', mode: 'COMPUTED', editable: false, key: 'valor_riesgo_residual', source: 'Backend F13 / PROY_VRR' },
  { label: 'Nivel del Riesgo Residual', mode: 'COMPUTED', editable: false, key: 'nivel_riesgo_residual', source: 'Backend F14 / catálogo institucional' },
  { label: 'Respuesta al riesgo', mode: 'INPUT', editable: true, key: 'respuesta_riesgo', source: 'Contrato de evaluación / MR_RESPUESTA_RIESGO' },
  { label: 'Plan de Mitigación/Acciones Correctivas', mode: 'REPEATER', editable: false, key: null, source: 'RL_MR_PLANES.PLA_DESCRIPCION' },
  { label: 'No. Acciones de Mitigación', mode: 'COMPUTED', editable: false, key: null, source: 'COUNT RL_MR_PLANES por evaluación' },
  { label: 'Actividades', mode: 'REPEATER', editable: false, key: null, source: 'RL_MR_ACTIVIDADES.ACT_DESCRIPCION; ACT_PLAN_ID → PLA_ID' },
  { label: 'Cantidad de Actividades', mode: 'COMPUTED', editable: false, key: null, source: 'COUNT RL_MR_ACTIVIDADES por plan' },
  { label: 'Monitoreo/ Seguimiento', mode: 'INPUT', editable: false, key: null, source: 'RL_MR_PLANES.PLA_MONITOREO_SEGUIMIENTO' },
  { label: 'Responsables', mode: 'INPUT', editable: false, key: null, source: 'RL_MR_PLANES.PLA_RESPONSABLES' },
  { label: 'Fecha inicio', mode: 'INPUT', editable: false, key: null, source: 'RL_MR_PLANES.PLA_FECHA_INICIO' },
  { label: 'Fecha final', mode: 'INPUT', editable: false, key: null, source: 'RL_MR_PLANES.PLA_FECHA_FIN' },
  { label: 'Recursos', mode: 'INPUT', editable: false, key: null, source: 'RL_MR_PLANES.PLA_RECURSOS' },
  { label: 'Presupuesto', mode: 'INPUT', editable: false, key: null, source: 'RL_MR_PLANES.PLA_PRESUPUESTO' },
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
  { label: 'Señales de Alerta', mode: 'MONITORING', editable: false, key: null, source: 'RL_MR_SENALES_ALERTA.ALE_EVALUACION_ID' },
  { label: 'Estado del Riesgo', mode: 'MONITORING', editable: false, key: null, source: 'RL_MR_AUTOMONITOREO.MON_ESTADO_RIESGO' },
  { label: 'Estado del Control Preventivo', mode: 'MONITORING', editable: false, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_ESTADO_MONITOREO (CON_TIPO=PREVENTIVO)' },
  { label: 'Evaluación de la Efectividad del Control Preventivo', mode: 'MONITORING', editable: false, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_EFECTIVIDAD_MONITOREO (CON_TIPO=PREVENTIVO)' },
  { label: 'Evidencia(s) del Control Preventivo', mode: 'EVIDENCE', editable: false, key: null, source: 'RL_MR_EVIDENCIAS_VINCULOS (CONTROL PREVENTIVO)' },
  { label: 'Estado del Control Detectivo', mode: 'MONITORING', editable: false, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_ESTADO_MONITOREO (CON_TIPO=DETECTIVO)' },
  { label: 'Evaluación de la Efectividad del Control Detectivo', mode: 'MONITORING', editable: false, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_EFECTIVIDAD_MONITOREO (CON_TIPO=DETECTIVO)' },
  { label: 'Evidencia(s) del Control Detectivo', mode: 'EVIDENCE', editable: false, key: null, source: 'RL_MR_EVIDENCIAS_VINCULOS (CONTROL DETECTIVO)' },
  { label: 'Estado del Control Correctivo', mode: 'MONITORING', editable: false, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_ESTADO_MONITOREO (CON_TIPO=CORRECTIVO)' },
  { label: 'Evaluación de la Efectividad del Control Correctivo', mode: 'MONITORING', editable: false, key: null, source: 'RL_MR_CONTROLES_RIESGO.CON_EFECTIVIDAD_MONITOREO (CON_TIPO=CORRECTIVO)' },
  { label: 'Evidencia(s) del Control Correctivo', mode: 'EVIDENCE', editable: false, key: null, source: 'RL_MR_EVIDENCIAS_VINCULOS (CONTROL CORRECTIVO)' },
  { label: 'Observaciones del Área', mode: 'INPUT', editable: false, key: null, source: 'RL_MR_AUTOMONITOREO.MON_OBSERVACIONES_AREA' },
  { label: 'Observaciones UGR', mode: 'INPUT', editable: false, key: null, source: 'RL_MR_AUTOMONITOREO.MON_OBSERVACIONES_UGR' }
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
      implemented: ordinal <= 82
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
export const MATRIX_BLOCK_2_FIELDS = MATRIX_FIELDS.filter(field => field.block === 2);
export const MATRIX_BLOCK_3_FIELDS = MATRIX_FIELDS.filter(field => field.block === 3);
export const MATRIX_BLOCK_4_FIELDS = MATRIX_FIELDS.filter(field => field.block === 4);
export const MATRIX_BLOCK_5_FIELDS = MATRIX_FIELDS.filter(field => field.block === 5);
export const MATRIX_BLOCK_6_FIELDS = MATRIX_FIELDS.filter(field => field.block === 6);

export const CATALOGO_RESPUESTA_RIESGO = ['EVITAR', 'MITIGAR', 'TRANSFERIR', 'ACEPTAR'] as const;
export type RespuestaRiesgoCanonica = typeof CATALOGO_RESPUESTA_RIESGO[number];
