import { describe, expect, it } from 'vitest';
import { canonicalCatalogSelectOptions, canonicalMonitoringEffectivenessOptions, canonicalRiskLevelLabel, canonicalRiskResponseKey, canonicalRiskResponseLabel, normalizeCatalogKey } from './matrices-riesgos-catalogos-canonicos';

describe('matrices-riesgos-catalogos-canonicos', () => {
  it.each([
    [1, 'Riesgo no significativo'], [2, 'Riesgo no significativo'],
    [3, 'Riesgo bajo'], [4, 'Riesgo bajo'], [5, 'Riesgo Medio'],
    [6, 'Riesgo Alto'], [7, 'Riesgo Alto'], [8, 'Riesgo Intolerable'], [9, 'Riesgo Intolerable']
  ])('mapea nivel %s al label institucional', (value, label) => {
    expect(canonicalRiskLevelLabel(value)).toBe(label);
  });

  it('presenta ROP-CUMP-59 con el mapeo VRI=3 y VRR=1', () => {
    expect(canonicalRiskLevelLabel(3)).toBe('Riesgo bajo');
    expect(canonicalRiskLevelLabel(1)).toBe('Riesgo no significativo');
  });

  it.each([
    ['Transferir', 'TRANSFERIR', 'Transferir/Compartir'],
    ['Reducir', 'MITIGAR', 'Mitigar']
  ])('normaliza el alias de respuesta %s', (input, key, label) => {
    expect(canonicalRiskResponseKey(input)).toBe(key);
    expect(canonicalRiskResponseLabel(input)).toBe(label);
  });

  it('UnknownCatalogValue_FailsClosed sin fuzzy matching', () => {
    expect(canonicalRiskLevelLabel(10)).toBeNull();
    expect(canonicalRiskResponseKey('Transferir ya')).toBeNull();
  });

  it('normaliza únicamente los aliases aprobados de efectividad y automatización', () => {
    expect(normalizeCatalogKey('CONTROL_EFFECTIVENESS', 'Es inefectivo')).toBe('INEFECTIVO');
    expect(normalizeCatalogKey('CONTROL_AUTOMATION', 'Semi-Automatizado')).toBe('SEMIAUTOMATIZADO');
    expect(normalizeCatalogKey('CONTROL_AUTOMATION', 'Semi automatizado')).toBeNull();
  });

  it('provee listas operativas de automatización y monitoreo desde el contrato', () => {
    expect(canonicalCatalogSelectOptions('CONTROL_AUTOMATION')).toEqual([
      { value: 'AUTOMATICO', label: 'Automatizado' },
      { value: 'SEMIAUTOMATICO', label: 'Semiautomatizado' },
      { value: 'MANUAL', label: 'Manual' }
    ]);
    expect(canonicalCatalogSelectOptions('MONITORING_RISK_STATUS').map(item => item.label)).toEqual(['Vigente', 'Mitigado', 'Nuevo']);
    expect(canonicalCatalogSelectOptions('MONITORING_CONTROL_STATUS').map(item => item.label)).toEqual(['Se mantiene', 'No se mantiene', 'Requiere actualización']);
    expect(canonicalMonitoringEffectivenessOptions()).toEqual([
      { value: 0, label: 'Inefectivo' }, { value: 30, label: 'Razonable' },
      { value: 50, label: 'Parcialmente Efectivo' }, { value: 85, label: 'Moderado' }, { value: 90, label: 'Alta Efectividad' }
    ]);
  });
});
