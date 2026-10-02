import { MATRICES_RIESGOS_CATALOGS } from './matrices-riesgos-catalogos-data.generated';

export type MatricesCatalogId = keyof typeof MATRICES_RIESGOS_CATALOGS;
export type CanonicalCatalogItem = { key: string; label: string; dbKey?: string; sortOrder: number; aliases: readonly string[]; percentage?: number; level?: number };

const CATALOG_CODE_MAP: Readonly<Record<string, MatricesCatalogId>> = {
  MR_RESPUESTA_RIESGO: 'RISK_RESPONSE',
  CAT_NIVEL_RIESGO: 'RISK_LEVEL',
  MR_FRECUENCIA_1_5: 'FREQUENCY',
  MR_IMPACTO_1_5: 'IMPACT',
  MR_TIPO_RIESGO: 'RISK_TYPE',
  MR_REGIMEN: 'REGIME',
  MR_REGIMEN_AFECTADO: 'REGIME',
  MR_TIPO_CONTROL: 'CONTROL_TYPE',
  MR_PESO_CONTROL: 'CONTROL_WEIGHT',
  CAT_EFECTIVIDAD_ESCALA: 'CONTROL_EFFECTIVENESS',
  CAT_EFECTIVIDAD_NIVEL: 'CONTROL_EFFECTIVENESS',
  CAT_EFECTIVIDAD_PORCENTAJE: 'CONTROL_EFFECTIVENESS',
  MR_EFECTIVIDAD_CONTROL: 'CONTROL_EFFECTIVENESS',
  MR_AUTOMATIZACION: 'CONTROL_AUTOMATION',
  MON_ESTADO_RIESGO: 'MONITORING_RISK_STATUS',
  MON_ESTADO_CONTROL: 'MONITORING_CONTROL_STATUS',
  MON_EFECTIVIDAD: 'MONITORING_EFFECTIVENESS'
};

function findItem(catalogId: MatricesCatalogId, value: unknown): CanonicalCatalogItem | null {
  if (typeof value !== 'string' && typeof value !== 'number') return null;
  const candidate = String(value).trim();
  const catalog = MATRICES_RIESGOS_CATALOGS[catalogId] as readonly CanonicalCatalogItem[];
  return catalog.find(item => item.key === candidate || item.label === candidate || item.dbKey === candidate || item.aliases.includes(candidate)) ?? null;
}

export function canonicalRiskLevelLabel(value: unknown): string | null {
  const number = typeof value === 'number' ? value : typeof value === 'string' && /^\d+$/.test(value.trim()) ? Number(value.trim()) : NaN;
  if (!Number.isInteger(number) || number < 1 || number > 9) return null;
  return findItem('RISK_LEVEL', String(number))?.label ?? null;
}

export function canonicalRiskResponseLabel(value: unknown): string | null {
  return canonicalCatalogLabel('RISK_RESPONSE', value);
}

export function canonicalRiskResponseKey(value: unknown): string | null {
  return normalizeCatalogKey('RISK_RESPONSE', value);
}

export function canonicalCatalogLabel(catalogIdOrCode: string, value: unknown): string | null {
  const catalogId = (CATALOG_CODE_MAP[catalogIdOrCode] ?? catalogIdOrCode) as MatricesCatalogId;
  if (!(catalogId in MATRICES_RIESGOS_CATALOGS)) return null;
  return findItem(catalogId, value)?.label ?? null;
}

export function normalizeCatalogKey(catalogIdOrCode: string, value: unknown): string | null {
  const catalogId = (CATALOG_CODE_MAP[catalogIdOrCode] ?? catalogIdOrCode) as MatricesCatalogId;
  if (!(catalogId in MATRICES_RIESGOS_CATALOGS)) return null;
  return findItem(catalogId, value)?.key ?? null;
}

export function canonicalCatalogOptions(catalogIdOrCode: string): Array<{ codigo: string; valor: string; orden: number }> {
  const catalogId = (CATALOG_CODE_MAP[catalogIdOrCode] ?? catalogIdOrCode) as MatricesCatalogId;
  if (!(catalogId in MATRICES_RIESGOS_CATALOGS)) return [];
  return [...MATRICES_RIESGOS_CATALOGS[catalogId] as readonly CanonicalCatalogItem[]]
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map(item => ({ codigo: item.key, valor: item.label, orden: item.sortOrder }));
}

export function canonicalCatalogSelectOptions(catalogId: MatricesCatalogId): Array<{ value: string; label: string }> {
  return [...MATRICES_RIESGOS_CATALOGS[catalogId] as readonly CanonicalCatalogItem[]]
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map(item => ({ value: item.dbKey ?? item.key, label: item.label }));
}

export function canonicalMonitoringEffectivenessOptions(): Array<{ value: number; label: string }> {
  const items = MATRICES_RIESGOS_CATALOGS.CONTROL_EFFECTIVENESS as readonly CanonicalCatalogItem[];
  return items.filter(item => (item.level ?? 0) > 0 && item.percentage !== undefined)
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map(item => ({ value: Math.round((item.percentage ?? 0) * 100), label: item.label }));
}
