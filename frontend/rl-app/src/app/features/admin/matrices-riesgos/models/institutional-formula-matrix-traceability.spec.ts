import { FormulaDto } from './calculo-configuracion.models';
import { MATRIX_FIELDS } from './matriz-institucional.contract';

const references = [
  [1, 'F01_VALOR_RIESGO_INHERENTE', 'valor_riesgo_inherente', 12, 'Valor del Riesgo Inherente', 'L'],
  [2, 'F02_NIVEL_RIESGO_INHERENTE', 'nivel_riesgo_inherente', 13, 'Nivel de Riesgo Inherente', 'M'],
  [3, 'F03_NIVEL_CONTROL_PREVENTIVO', 'nivel_control_preventivo', 22, 'Nivel de efectividad de control(es) preventivo(s)', 'V'],
  [4, 'F04_PORCENTAJE_CONTROL_PREVENTIVO', 'porcentaje_control_preventivo', 23, '% efectividad de control(es) preventivo(s)', 'W'],
  [5, 'F05_NIVEL_CONTROL_DETECTIVO', 'nivel_control_detectivo', 26, 'Nivel de efectividad de control(es) detectivo(s)', 'Z'],
  [6, 'F06_PORCENTAJE_CONTROL_DETECTIVO', 'porcentaje_control_detectivo', 27, '% efectividad de control detectivo', 'AA'],
  [7, 'F07_NIVEL_CONTROL_CORRECTIVO', 'nivel_control_correctivo', 30, 'Nivel de efectividad de control(es) correctivo(s)', 'AD'],
  [8, 'F08_PORCENTAJE_CONTROL_CORRECTIVO', 'porcentaje_control_correctivo', 31, '% efectividad de control correctivo', 'AE'],
  [9, 'F09_EFECTIVIDAD_TOTAL_PONDERADA', 'efectividad_total_ponderada', 33, 'Efectividad Total Ponderada de los Controles', 'AG'],
  [10, 'F10_RIESGO_RESIDUAL_DESCRIPCION', 'riesgo_residual_descripcion', 34, 'Riesgo Residual', 'AH'],
  [11, 'F11_FRECUENCIA_RESIDUAL', 'frecuencia_residual', 35, 'Frecuencia Residual', 'AI'],
  [12, 'F12_IMPACTO_RESIDUAL', 'impacto_residual', 36, 'Impacto Residual', 'AJ'],
  [13, 'F13_VALOR_RIESGO_RESIDUAL', 'valor_riesgo_residual', 37, 'Valor del Riesgo Residual', 'AK'],
  [14, 'F14_NIVEL_RIESGO_RESIDUAL', 'nivel_riesgo_residual', 38, 'Nivel del Riesgo Residual', 'AL'],
  [15, 'F15_FRECUENCIA_RESIDUAL_AUX', 'frecuencia_residual_aux', 50, 'Frecuencia Residual (AUX)', 'AX'],
  [16, 'F16_IMPACTO_RESIDUAL_AUX', 'impacto_residual_aux', 51, 'Impacto Residual (AUX)', 'AY'],
  [17, 'F17_SUMA_RESIDUAL_REDONDEADA_AUX', 'suma_residual_redondeada_aux', 52, 'Suma Residual redondeada (AUX)', 'AZ'],
  [18, 'F18_F_BASE_AUX', 'f_base', 53, 'F_base (AUX)', 'BA'],
  [19, 'F19_I_BASE_AUX', 'i_base', 54, 'I_base (AUX)', 'BB'],
  [20, 'F20_TOPE_F_AUX', 'tope_f', 55, 'Tope F (AUX)', 'BC'],
  [21, 'F21_TOPE_I_AUX', 'tope_i', 56, 'Tope I (AUX)', 'BD'],
  [22, 'F22_CAPACIDAD_F_AUX', 'capacidad_f_aux', 57, 'Capacidad F (AUX)', 'BE'],
  [23, 'F23_CAPACIDAD_I_AUX', 'capacidad_i_aux', 58, 'Capacidad I (AUX)', 'BF'],
  [24, 'F24_RESTO_AUX', 'resto_aux', 59, 'Resto (AUX)', 'BG'],
  [25, 'F25_PREFIERE_I_AUX', 'prefiere_i_aux', 60, 'Prefiere I (AUX)', 'BH'],
  [26, 'F26_INCREMENTO_I_AUX', 'incremento_i_aux', 61, 'Inc_I (AUX)', 'BI'],
  [27, 'F27_INCREMENTO_F_AUX', 'incremento_f_aux', 62, 'Inc_F (AUX)', 'BJ'],
  [28, 'F28_VALOR_RIESGO_RESIDUAL_AUX', 'valor_riesgo_residual_aux', 63, 'Valor del Riesgo Residual (AUX)', 'BK'],
  [29, 'F29_VERIFICACION_RIESGO_RESIDUAL', 'verificacion', 64, 'Verificación', 'BL'],
  [30, 'F30_VRR_2', 'vrr_2', 65, 'VRR 2', 'BM'],
  [31, 'F31_VERIFICAR_VRR_2', 'verificar_vrr_2', 66, 'Verificar VRR 2', 'BN'],
  [32, 'F32_VERIFICAR_FRECUENCIA', 'verificar_frecuencia', 67, 'Verificar Frec', 'BO'],
  [33, 'F33_VERIFICAR_IMPACTO', 'verificar_impacto', 68, 'Verificar Impact', 'BP'],
  [34, 'F34_DIFERENCIA_VRI_VRR', 'diferencia_vri_vrr', 69, 'VRI-VRR', 'BQ']
] as const;

describe('trazabilidad institucional Fórmula → Matriz', () => {
  it('resuelve exactamente los 34 targets a una definición con label y columna Excel concordantes', () => {
    const formulas: FormulaDto[] = references.map(([numero, codigo, targetField, , , excelColumn]) => ({
      id: numero,
      codigo,
      nombre: codigo,
      estado: 'ACTIVE',
      versionRow: 1,
      referenciaInstitucional: {
        numero,
        targetField,
        sourceCell: `Matriz Consolidada!${excelColumn}2`,
        excelColumn
      }
    }));

    expect(formulas).toHaveLength(34);
    expect(new Set(formulas.map(formula => formula.codigo)).size).toBe(34);
    expect(new Set(formulas.map(formula => formula.referenciaInstitucional?.targetField)).size).toBe(34);

    for (const [numero, codigo, targetField, ordinal, label, excelColumn] of references) {
      const formula = formulas.find(item => item.codigo === codigo);
      expect(formula?.referenciaInstitucional?.numero).toBe(numero);
      expect(formula?.referenciaInstitucional?.targetField).toBe(targetField);
      expect(formula?.referenciaInstitucional?.sourceCell).toBe(`Matriz Consolidada!${excelColumn}2`);
      const matches = MATRIX_FIELDS.filter(field => field.key === formula?.referenciaInstitucional?.targetField);
      expect(matches).toHaveLength(1);
      expect(matches[0]).toMatchObject({ ordinal, label, excelColumn });
    }
  });
});
