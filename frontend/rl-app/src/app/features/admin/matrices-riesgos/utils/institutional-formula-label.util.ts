import { FormulaDto, FormulaVersionSelectorOption } from '../models/calculo-configuracion.models';

const INSTITUTIONAL_FORMULA_NAMES: Readonly<Record<string, string>> = Object.freeze({
  F01_VALOR_RIESGO_INHERENTE: 'Valor del Riesgo Inherente',
  F02_NIVEL_RIESGO_INHERENTE: 'Nivel de Riesgo Inherente',
  F03_NIVEL_CONTROL_PREVENTIVO: 'Nivel de efectividad del control preventivo',
  F04_PORCENTAJE_CONTROL_PREVENTIVO: 'Porcentaje de efectividad del control preventivo',
  F05_NIVEL_CONTROL_DETECTIVO: 'Nivel de efectividad del control detectivo',
  F06_PORCENTAJE_CONTROL_DETECTIVO: 'Porcentaje de efectividad del control detectivo',
  F07_NIVEL_CONTROL_CORRECTIVO: 'Nivel de efectividad del control correctivo',
  F08_PORCENTAJE_CONTROL_CORRECTIVO: 'Porcentaje de efectividad del control correctivo',
  F09_EFECTIVIDAD_TOTAL_PONDERADA: 'Efectividad total ponderada de los controles',
  F10_RIESGO_RESIDUAL_DESCRIPCION: 'Riesgo residual',
  F11_FRECUENCIA_RESIDUAL: 'Frecuencia residual',
  F12_IMPACTO_RESIDUAL: 'Impacto residual',
  F13_VALOR_RIESGO_RESIDUAL: 'Valor del Riesgo Residual',
  F14_NIVEL_RIESGO_RESIDUAL: 'Nivel del Riesgo Residual',
  F15_FRECUENCIA_RESIDUAL_AUX: 'Frecuencia residual auxiliar',
  F16_IMPACTO_RESIDUAL_AUX: 'Impacto residual auxiliar',
  F17_SUMA_RESIDUAL_REDONDEADA_AUX: 'Suma residual redondeada auxiliar',
  F18_F_BASE_AUX: 'F_base auxiliar',
  F19_I_BASE_AUX: 'I_base auxiliar',
  F20_TOPE_F_AUX: 'Tope de frecuencia auxiliar',
  F21_TOPE_I_AUX: 'Tope de impacto auxiliar',
  F22_CAPACIDAD_F_AUX: 'Capacidad de frecuencia auxiliar',
  F23_CAPACIDAD_I_AUX: 'Capacidad de impacto auxiliar',
  F24_RESTO_AUX: 'Resto auxiliar',
  F25_PREFIERE_I_AUX: 'Preferencia de impacto auxiliar',
  F26_INCREMENTO_I_AUX: 'Incremento de impacto auxiliar',
  F27_INCREMENTO_F_AUX: 'Incremento de frecuencia auxiliar',
  F28_VALOR_RIESGO_RESIDUAL_AUX: 'Valor del Riesgo Residual auxiliar',
  F29_VERIFICACION_RIESGO_RESIDUAL: 'Verificación del Riesgo Residual',
  F30_VRR_2: 'VRR 2',
  F31_VERIFICAR_VRR_2: 'Verificar VRR 2',
  F32_VERIFICAR_FRECUENCIA: 'Verificar frecuencia',
  F33_VERIFICAR_IMPACTO: 'Verificar impacto',
  F34_DIFERENCIA_VRI_VRR: 'Diferencia VRI-VRR'
});

const TECHNICAL_FORMULA_CODES = new Set(Object.keys(INSTITUTIONAL_FORMULA_NAMES));

export function nombreFormulaLegible(formula: Pick<FormulaDto, 'codigo' | 'nombre' | 'referenciaInstitucional'> | FormulaVersionSelectorOption): string {
  const code = formula.codigo?.trim() ?? '';
  const name = formula.nombre?.trim() ?? '';
  if (name && !esNombreTecnico(name, code)) return name;
  return INSTITUTIONAL_FORMULA_NAMES[code.toUpperCase()] ?? (name || code);
}

export function etiquetaEstadoFormula(estado: string): string {
  return ({
    DRAFT: 'Borrador',
    IN_REVIEW: 'En revisión',
    APPROVED: 'Aprobada',
    PUBLISHED: 'Publicada',
    RETIRED: 'Retirada',
    ARCHIVED: 'Archivada',
    ACTIVE: 'Activa',
    INACTIVE: 'Inactiva'
  } as Record<string, string>)[estado] ?? estado;
}

export function normalizarBusquedaFormula(value: string): string {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
}

function esNombreTecnico(nombre: string, codigo: string): boolean {
  const normalized = nombre.toUpperCase();
  return normalized === codigo.toUpperCase()
    || (TECHNICAL_FORMULA_CODES.has(normalized) && nombre === normalized)
    || (nombre.includes('_') && nombre === normalized && !nombre.includes(' '));
}
