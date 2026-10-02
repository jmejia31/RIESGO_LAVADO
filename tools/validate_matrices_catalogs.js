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

console.log(`CATALOG_COUNT=${catalogs.length}`);
console.log(`CATALOG_ITEMS_TOTAL=${catalogs.reduce((n, c) => n + c.items.length, 0)}`);
console.log(`RISK_LEVEL_1_9=${process.exitCode ? 'FAIL' : 'PASS'}`);
console.log(`CONTROL_WEIGHT_SUM=${Number.isFinite(weightSum) ? weightSum.toFixed(2) : 'INVALID'}`);
console.log(`UNRESOLVED_SOURCES=${manifest.unresolvedSources?.length ?? 0}`);
console.log(`CATALOG_COMPLETENESS=${manifest.unresolvedSources?.length ? 'PENDING' : 'PASS'}`);
if (!process.exitCode) console.log('CATALOG_MANIFEST_STRUCTURE=PASS');
