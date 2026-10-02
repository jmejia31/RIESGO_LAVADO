#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const manifestPath = path.join(__dirname, '..', 'backend', 'RL.API', 'Features', 'MatricesRiesgos', 'Contracts', 'matriz_riesgos_catalogos_manifest.json');
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
const fail = (message) => { console.error(`FAIL: ${message}`); process.exitCode = 1; };
const catalogs = manifest.catalogs;

if (!Array.isArray(catalogs) || catalogs.length < 1) fail('catalogs must be a non-empty array');
const catalogIds = catalogs.map(c => c.catalogId);
if (new Set(catalogIds).size !== catalogIds.length) fail('catalogId values must be unique');
for (const catalog of catalogs) {
  if (!catalog.label || !catalog.source?.workbookSheet || !catalog.source?.workbookRange) fail(`${catalog.catalogId}: missing label/source`);
  for (const property of ['institutionalName', 'sourceWorkbookSheet', 'sourceWorkbookRange', 'namedRangeOrTable', 'dataValidationReference', 'normalizationRule', 'dbCatalogCode', 'backendDefinition', 'frontendDefinition', 'importBehavior', 'exportBehavior', 'usedByFields', 'calculationDependencies', 'driftStatus', 'driftEvidence', 'requiredRemediation']) {
    if (catalog[property] === undefined || catalog[property] === '') fail(`${catalog.catalogId}: missing contract property ${property}`);
  }
  if (!Array.isArray(catalog.items) || catalog.items.length === 0) fail(`${catalog.catalogId}: items must be non-empty`);
  const keys = catalog.items.map(i => i.key);
  if (new Set(keys).size !== keys.length) fail(`${catalog.catalogId}: duplicate canonical keys`);
  for (const item of catalog.items) {
    if (!item.key || !item.label || !Number.isInteger(item.sortOrder) || item.sortOrder < 1) fail(`${catalog.catalogId}: invalid item ${JSON.stringify(item)}`);
  }
  const aliases = catalog.items.flatMap(i => i.aliases ?? []);
  if (new Set(aliases).size !== aliases.length) fail(`${catalog.catalogId}: duplicate aliases`);
  if (catalog.unknownValueBehavior && catalog.unknownValueBehavior !== 'FAIL_CLOSED') fail(`${catalog.catalogId}: unknown values must fail closed`);
}

const get = id => catalogs.find(c => c.catalogId === id);
const risk = get('RISK_LEVEL');
const expectedRisk = [
  'Riesgo no significativo', 'Riesgo no significativo', 'Riesgo bajo', 'Riesgo bajo',
  'Riesgo Medio', 'Riesgo Alto', 'Riesgo Alto', 'Riesgo Intolerable', 'Riesgo Intolerable'
];
if (!risk || risk.items.length !== 9 || risk.items.some((i, n) => i.numericValue !== n + 1 || i.label !== expectedRisk[n])) fail('RISK_LEVEL must exactly match workbook values 1..9');

const weights = get('CONTROL_WEIGHT');
const weightSum = weights?.items.reduce((sum, item) => sum + item.weight, 0);
if (!weights || Math.abs(weightSum - 1) > 1e-10) fail(`control weights must sum to 1.00 (actual ${weightSum})`);

const effectiveness = get('CONTROL_EFFECTIVENESS');
const expectedEffectiveness = [[0, 0], [1, 0], [2, .3], [3, .5], [4, .85], [5, .9]];
if (!effectiveness || effectiveness.items.length !== 6 || effectiveness.items.some((i, n) => i.level !== expectedEffectiveness[n][0] || i.percentage !== expectedEffectiveness[n][1])) fail('control effectiveness levels/percentages mismatch');

const response = get('RISK_RESPONSE');
if (!response || response.items.map(i => i.key).join(',') !== 'EVITAR,TRANSFERIR,ACEPTAR,MITIGAR') fail('risk response key contract mismatch');
if (!response.items.find(i => i.key === 'MITIGAR')?.aliases.includes('Reducir')) fail('response alias Reducir is not registered');
if (!response.items.find(i => i.key === 'TRANSFERIR')?.aliases.includes('Transferir')) fail('response alias Transferir is not registered');

const automation = get('CONTROL_AUTOMATION');
if (!automation?.items.find(i => i.label === 'Semiautomatizado')?.aliases.includes('Semi-Automatizado')) fail('automation alias is not registered');
for (const id of ['MONITORING_RISK_STATUS', 'MONITORING_CONTROL_STATUS', 'MONITORING_EFFECTIVENESS']) if (!get(id)) fail(`missing ${id}`);

const backendService = fs.readFileSync(path.join(__dirname, '..', 'backend', 'RL.API', 'Features', 'MatricesRiesgos', 'Application', 'MatricesRiesgosAppService.cs'), 'utf8');
const frontendContract = fs.readFileSync(path.join(__dirname, '..', 'frontend', 'rl-app', 'src', 'app', 'features', 'admin', 'matrices-riesgos', 'models', 'matriz-institucional.contract.ts'), 'utf8');
for (const [label, source] of [['backend', backendService], ['frontend', frontendContract]]) {
  for (const key of ['EVITAR', 'MITIGAR', 'TRANSFERIR', 'ACEPTAR']) if (!source.includes(key)) fail(`${label} risk response key ${key} is missing`);
}
const residualTest = fs.readFileSync(path.join(__dirname, '..', 'backend', 'RL.API.Tests', 'Features', 'MatricesRiesgos', 'MatricesRiesgosBlock3ResidualCertificacionTests.cs'), 'utf8');
for (const label of expectedRisk) if (!residualTest.includes(label)) fail(`backend regression test is missing risk label ${label}`);

const apiRoot = path.join(__dirname, '..', 'backend', 'RL.API', 'Features', 'MatricesRiesgos');
const frontendRoot = path.join(__dirname, '..', 'frontend', 'rl-app', 'src', 'app', 'features', 'admin', 'matrices-riesgos');
const resolver = fs.readFileSync(path.join(apiRoot, 'Domain', 'MatrizRiesgosCatalogoCanonico.cs'), 'utf8');
const runtimeFactory = fs.readFileSync(path.join(apiRoot, 'Application', 'DbDrivenCalculationRuntimeFactory.cs'), 'utf8');
const reportExport = fs.readFileSync(path.join(apiRoot, 'Application', 'MatricesRiesgosReportExportService.cs'), 'utf8');
const appService = fs.readFileSync(path.join(apiRoot, 'Application', 'MatricesRiesgosAppService.cs'), 'utf8');
const frontendResolver = fs.readFileSync(path.join(frontendRoot, 'utils', 'matrices-riesgos-catalogos-canonicos.ts'), 'utf8');
const generatedPath = path.join(frontendRoot, 'utils', 'matrices-riesgos-catalogos-data.generated.ts');
const generatedSource = fs.readFileSync(generatedPath, 'utf8');
const frontendTests = fs.readFileSync(path.join(frontendRoot, 'utils', 'matrices-riesgos-catalogos-canonicos.spec.ts'), 'utf8');
const dynamicOptionsTests = fs.readFileSync(path.join(frontendRoot, 'pages', 'matrices-riesgos', 'matrices-riesgos.component.renderer-dinamico.spec.ts'), 'utf8');
const frontendComponent = fs.readFileSync(path.join(frontendRoot, 'pages', 'matrices-riesgos', 'matrices-riesgos.component.ts'), 'utf8');
const frontendTemplate = fs.readFileSync(path.join(frontendRoot, 'pages', 'matrices-riesgos', 'matrices-riesgos.component.html'), 'utf8');

if (manifest.unresolvedSources?.length) fail('unresolvedSources must be empty after observed Area mapping reconciliation');
for (const catalog of catalogs) {
  if (/PENDING|UNKNOWN|UNRESOLVED|TODO|TBD/i.test(`${catalog.driftStatus} ${catalog.driftEvidence} ${catalog.requiredRemediation}`)) fail(`${catalog.catalogId}: unresolved catalog state is forbidden`);
  if (catalog.driftStatus !== 'PASS') fail(`${catalog.catalogId}: driftStatus must be PASS`);
}

const generatedJson = generatedSource.match(/export const MATRICES_RIESGOS_CATALOGS = ([\s\S]*?) as const;\s*$/)?.[1];
let generated;
try { generated = JSON.parse(generatedJson); } catch { fail('generated frontend catalog data is not valid JSON'); }
const manifestData = Object.fromEntries(catalogs.map(c => [c.catalogId, c.items.map(i => {
  const item = { key: i.key, label: i.label };
  if (i.dbKey !== undefined) item.dbKey = i.dbKey;
  item.sortOrder = i.sortOrder;
  item.aliases = i.aliases ?? [];
  for (const field of ['numericValue', 'level', 'percentage', 'weight', 'parentKey', 'metadata']) if (i[field] !== undefined) item[field] = i[field];
  return item;
})]));
if (JSON.stringify(generated) !== JSON.stringify(manifestData)) fail('generated frontend catalog data must exactly match the canonical manifest');
if (!resolver.includes('matriz_riesgos_catalogos_manifest.json') || !reportExport.includes('ObtenerEtiquetaNivelRiesgo(fila.Vri)')
  || !reportExport.includes('ObtenerEtiquetaNivelRiesgo(fila.Vrr)') || !reportExport.includes('ObtenerEtiqueta("RISK_RESPONSE", fila.RespuestaRiesgo)'))
  fail('backend Excel/PDF exports must resolve canonical risk/response labels through the manifest helper');
if (!runtimeFactory.includes('CrearSnapshotNivelRiesgo') || !appService.includes('NormalizarRespuestaRiesgo(dto)')) fail('backend calculation/import alias consumers do not use canonical catalog rules');
if (!frontendComponent.includes('canonicalRiskLevelLabel') || !frontendComponent.includes('canonicalRiskResponseLabel')
  || !frontendTemplate.includes('etiquetaNivelRiesgo(fila.vri)') || !frontendTemplate.includes('etiquetaRespuestaRiesgo(fila.respuestaRiesgo)'))
  fail('frontend report and calculation display consumers do not use canonical mappings');
if (!frontendComponent.includes("canonicalCatalogOptions('RISK_RESPONSE')") || !dynamicOptionsTests.includes('presenta las etiquetas canónicas para la lista de respuesta')) fail('frontend response selector options must be projected from the canonical manifest');
if (!frontendTests.includes('UnknownCatalogValue') || !residualTest.includes('UnknownCatalogValue_FailsClosed')) fail('unknown values must fail closed in frontend/backend tests');
const monitoringService = fs.readFileSync(path.join(apiRoot, 'Application', 'MatricesRiesgosMonitoreoService.cs'), 'utf8');
const mitigationService = fs.readFileSync(path.join(apiRoot, 'Application', 'MatricesRiesgosMitigacionService.cs'), 'utf8');
const monitoringUi = fs.readFileSync(path.join(frontendRoot, 'components', 'matrices-riesgos-monitoreo-operativo', 'matrices-riesgos-monitoreo-operativo.component.html'), 'utf8');
const mitigationUi = fs.readFileSync(path.join(frontendRoot, 'components', 'matrices-riesgos-mitigacion', 'matrices-riesgos-mitigacion.component.html'), 'utf8');
for (const marker of ['ObtenerClavePersistente("MONITORING_RISK_STATUS"', 'EsValorValido("MONITORING_CONTROL_STATUS"']) if (!monitoringService.includes(marker)) fail(`backend monitoring catalog validation missing ${marker}`);
for (const marker of ['CONTROL_AUTOMATION', 'MONITORING_CONTROL_STATUS', 'EsPorcentajeEfectividadValido']) if (!mitigationService.includes(marker)) fail(`backend control catalog validation missing ${marker}`);
if (!monitoringUi.includes('estadosRiesgo') || !monitoringUi.includes('estadosControl') || monitoringUi.includes('id="mon-estado-riesgo" class="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2" [(ngModel)]="monEstadoRiesgo"><input')) fail('monitoring status form must consume canonical select options');
if (!mitigationUi.includes('automatizacionesCatalogo') || !mitigationUi.includes('estadosMonitoreoCatalogo') || !mitigationUi.includes('efectividadesMonitoreoCatalogo')) fail('control form must consume canonical control and monitoring catalogs');

const frequency = get('FREQUENCY');
const impact = get('IMPACT');
if (JSON.stringify(frequency?.items.map(item => item.numericValue)) !== JSON.stringify([1, 2, 3, 4, 5])) fail('frequency numeric domain must be exactly 1..5');
if (JSON.stringify(impact?.items.map(item => item.numericValue)) !== JSON.stringify([1, 2, 3, 4, 5])) fail('impact numeric domain must be exactly 1..5');
const monitoringExpectations = {
  MONITORING_RISK_STATUS: ['Vigente', 'Mitigado', 'Nuevo'],
  MONITORING_CONTROL_STATUS: ['Se mantiene', 'No se mantiene', 'Requiere actualización'],
  MONITORING_EFFECTIVENESS: ['Inexistente', 'Inefectivo', 'Razonable', 'Parcialmente Efectivo', 'Moderado', 'Alta Efectividad']
};
for (const [id, expected] of Object.entries(monitoringExpectations)) if (JSON.stringify(get(id)?.items.map(item => item.label)) !== JSON.stringify(expected)) fail(`${id}: workbook labels/order differ from the canonical contract`);

console.log(`CATALOG_COUNT=${catalogs.length}`);
console.log(`CATALOG_ITEMS_TOTAL=${catalogs.reduce((n, c) => n + c.items.length, 0)}`);
console.log(`RISK_LEVEL_1_9=${process.exitCode ? 'FAIL' : 'PASS'}`);
console.log(`CONTROL_WEIGHT_SUM=${Number.isFinite(weightSum) ? weightSum.toFixed(2) : 'INVALID'}`);
console.log(`UNRESOLVED_SOURCES=${manifest.unresolvedSources?.length ?? 0}`);
console.log(`CATALOG_COMPLETENESS=${manifest.unresolvedSources?.length ? 'PENDING' : 'PASS'}`);
console.log(`RISK_LEVEL_CONSUMER_PARITY=${process.exitCode ? 'FAIL' : 'PASS'}`);
console.log(`RESPONSE_CONSUMER_PARITY=${process.exitCode ? 'FAIL' : 'PASS'}`);
console.log(`CONTROL_CONSUMER_PARITY=${process.exitCode ? 'FAIL' : 'PASS'}`);
console.log(`AUTOMATION_CONSUMER_PARITY=${process.exitCode ? 'FAIL' : 'PASS'}`);
console.log(`MONITORING_CONSUMER_PARITY=${process.exitCode ? 'FAIL' : 'PASS'}`);
console.log(`EXPORT_CONSUMER_PARITY=${process.exitCode ? 'FAIL' : 'PASS'}`);
if (!process.exitCode) console.log('CATALOG_MANIFEST_STRUCTURE=PASS');
