import { FormulaDto } from '../models/calculo-configuracion.models';
import { etiquetaEstadoFormula, nombreFormulaLegible, normalizarBusquedaFormula } from './institutional-formula-label.util';

describe('etiquetas institucionales de fórmulas', () => {
  it('resuelve los 34 códigos institucionales sin exponer el identificador técnico', () => {
    const codes = [
      'F01_VALOR_RIESGO_INHERENTE', 'F02_NIVEL_RIESGO_INHERENTE', 'F03_NIVEL_CONTROL_PREVENTIVO', 'F04_PORCENTAJE_CONTROL_PREVENTIVO',
      'F05_NIVEL_CONTROL_DETECTIVO', 'F06_PORCENTAJE_CONTROL_DETECTIVO', 'F07_NIVEL_CONTROL_CORRECTIVO', 'F08_PORCENTAJE_CONTROL_CORRECTIVO',
      'F09_EFECTIVIDAD_TOTAL_PONDERADA', 'F10_RIESGO_RESIDUAL_DESCRIPCION', 'F11_FRECUENCIA_RESIDUAL', 'F12_IMPACTO_RESIDUAL',
      'F13_VALOR_RIESGO_RESIDUAL', 'F14_NIVEL_RIESGO_RESIDUAL', 'F15_FRECUENCIA_RESIDUAL_AUX', 'F16_IMPACTO_RESIDUAL_AUX',
      'F17_SUMA_RESIDUAL_REDONDEADA_AUX', 'F18_F_BASE_AUX', 'F19_I_BASE_AUX', 'F20_TOPE_F_AUX', 'F21_TOPE_I_AUX',
      'F22_CAPACIDAD_F_AUX', 'F23_CAPACIDAD_I_AUX', 'F24_RESTO_AUX', 'F25_PREFIERE_I_AUX', 'F26_INCREMENTO_I_AUX',
      'F27_INCREMENTO_F_AUX', 'F28_VALOR_RIESGO_RESIDUAL_AUX', 'F29_VERIFICACION_RIESGO_RESIDUAL', 'F30_VRR_2',
      'F31_VERIFICAR_VRR_2', 'F32_VERIFICAR_FRECUENCIA', 'F33_VERIFICAR_IMPACTO', 'F34_DIFERENCIA_VRI_VRR'
    ];
    for (const [index, code] of codes.entries()) {
      const formula: FormulaDto = {
        id: index + 1,
        codigo: code,
        nombre: code,
        estado: 'ACTIVE',
        versionRow: 1
      };
      expect(nombreFormulaLegible(formula)).not.toBe(formula.codigo);
    }
  });

  it('prioriza el nombre descriptivo administrado cuando no es técnico', () => {
    const formula: FormulaDto = { id: 1, codigo: 'F01_VALOR_RIESGO_INHERENTE', nombre: 'VRI institucional', estado: 'ACTIVE', versionRow: 1 };
    expect(nombreFormulaLegible(formula)).toBe('VRI institucional');
  });

  it('normaliza búsqueda con acentos y estado institucional', () => {
    expect(normalizarBusquedaFormula('  Porcentaje de EFECTIVIDAD  ')).toBe('porcentaje de efectividad');
    expect(etiquetaEstadoFormula('DRAFT')).toBe('Borrador');
  });
});
