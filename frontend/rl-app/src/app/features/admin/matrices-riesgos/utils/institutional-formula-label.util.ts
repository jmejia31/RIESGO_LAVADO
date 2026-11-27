import { FormulaDto, FormulaVersionSelectorOption } from '../models/calculo-configuracion.models';
import { MATRIX_FIELDS, MatrixFieldContract } from '../models/matriz-institucional.contract';

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

const INSTITUTIONAL_FORMULA_TARGET_KEYS: Readonly<Record<string, string>> = Object.freeze({
  F01_VALOR_RIESGO_INHERENTE: 'valor_riesgo_inherente',
  F02_NIVEL_RIESGO_INHERENTE: 'nivel_riesgo_inherente',
  F03_NIVEL_CONTROL_PREVENTIVO: 'nivel_control_preventivo',
  F04_PORCENTAJE_CONTROL_PREVENTIVO: 'porcentaje_control_preventivo',
  F05_NIVEL_CONTROL_DETECTIVO: 'nivel_control_detectivo',
  F06_PORCENTAJE_CONTROL_DETECTIVO: 'porcentaje_control_detectivo',
  F07_NIVEL_CONTROL_CORRECTIVO: 'nivel_control_correctivo',
  F08_PORCENTAJE_CONTROL_CORRECTIVO: 'porcentaje_control_correctivo',
  F09_EFECTIVIDAD_TOTAL_PONDERADA: 'efectividad_total_ponderada',
  F10_RIESGO_RESIDUAL_DESCRIPCION: 'riesgo_residual_descripcion',
  F11_FRECUENCIA_RESIDUAL: 'frecuencia_residual',
  F12_IMPACTO_RESIDUAL: 'impacto_residual',
  F13_VALOR_RIESGO_RESIDUAL: 'valor_riesgo_residual',
  F14_NIVEL_RIESGO_RESIDUAL: 'nivel_riesgo_residual',
  F15_FRECUENCIA_RESIDUAL_AUX: 'frecuencia_residual_aux',
  F16_IMPACTO_RESIDUAL_AUX: 'impacto_residual_aux',
  F17_SUMA_RESIDUAL_REDONDEADA_AUX: 'suma_residual_redondeada_aux',
  F18_F_BASE_AUX: 'f_base',
  F19_I_BASE_AUX: 'i_base',
  F20_TOPE_F_AUX: 'tope_f',
  F21_TOPE_I_AUX: 'tope_i',
  F22_CAPACIDAD_F_AUX: 'capacidad_f_aux',
  F23_CAPACIDAD_I_AUX: 'capacidad_i_aux',
  F24_RESTO_AUX: 'resto_aux',
  F25_PREFIERE_I_AUX: 'prefiere_i_aux',
  F26_INCREMENTO_I_AUX: 'incremento_i_aux',
  F27_INCREMENTO_F_AUX: 'incremento_f_aux',
  F28_VALOR_RIESGO_RESIDUAL_AUX: 'valor_riesgo_residual_aux',
  F29_VERIFICACION_RIESGO_RESIDUAL: 'verificacion',
  F30_VRR_2: 'vrr_2',
  F31_VERIFICAR_VRR_2: 'verificar_vrr_2',
  F32_VERIFICAR_FRECUENCIA: 'verificar_frecuencia',
  F33_VERIFICAR_IMPACTO: 'verificar_impacto',
  F34_DIFERENCIA_VRI_VRR: 'diferencia_vri_vrr'
});

type FormulaLabelInput = Pick<FormulaDto, 'codigo' | 'nombre' | 'referenciaInstitucional'> | FormulaVersionSelectorOption;

export function esFormulaInstitucional(formula: Pick<FormulaLabelInput, 'codigo'>): boolean {
  return TECHNICAL_FORMULA_CODES.has(formula.codigo?.trim().toUpperCase() ?? '');
}

export function campoMatrizFormula(formula: FormulaLabelInput): MatrixFieldContract | null {
  const code = formula.codigo?.trim() ?? '';
  const normalizedCode = code.toUpperCase();
  const targetKey = INSTITUTIONAL_FORMULA_TARGET_KEYS[normalizedCode];
  if (!targetKey) return null;

  const field = MATRIX_FIELDS.find(item => item.key === targetKey) ?? null;
  if (!field) return null;

  const reference = formula.referenciaInstitucional;
  if (!reference) return field;

  const number = Number(normalizedCode.match(/^F(\d{2})_/)?.[1]);
  return reference.numero === number
    && reference.targetField === field.key
    && reference.excelColumn === field.excelColumn
    ? field
    : null;
}

export function nombreFormulaLegible(formula: FormulaLabelInput): string {
  const code = formula.codigo?.trim() ?? '';
  const name = formula.nombre?.trim() ?? '';
  if (name && !esNombreTecnico(name, code)) return name;
  return campoMatrizFormula(formula)?.label ?? INSTITUTIONAL_FORMULA_NAMES[code.toUpperCase()] ?? (name || code);
}

export function etiquetaFormulaVersion(formula: FormulaVersionSelectorOption): string {
  const indicadorInstitucional = esFormulaInstitucional(formula) ? ' · FI' : '';
  return `${nombreFormulaLegible(formula)}${indicadorInstitucional} · v${formula.version} · ${etiquetaEstadoFormula(formula.estado)}`;
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
  const normalized = nombreFormulaParaComparar(nombre);
  const normalizedCode = codigo.trim().toUpperCase();
  if (normalized === nombreFormulaParaComparar(normalizedCode)) return true;
  if (esNombreInstitucionalGenerico(normalized, normalizedCode)) return true;
  return nombre.includes('_') && nombre === nombre.toUpperCase() && !nombre.includes(' ');
}

function esNombreInstitucionalGenerico(nombreNormalizado: string, codigo: string): boolean {
  if (!esFormulaInstitucional({ codigo })) return false;
  const numeroCodigo = Number(codigo.match(/^F(\d{2})_/)?.[1]);
  const match = nombreNormalizado.match(/^(?:FORMULA INSTITUCIONAL|FI)\s*0?(\d{1,2})$/);
  return match !== null && Number(match[1]) === numeroCodigo;
}

function nombreFormulaParaComparar(value: string): string {
  return normalizarBusquedaFormula(value).replace(/\s+/g, ' ').trim().toUpperCase();
}
