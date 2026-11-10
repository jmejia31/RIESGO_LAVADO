'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const repoRoot = path.resolve(__dirname, '..');

function read(relativePath) {
  return fs.readFileSync(path.join(repoRoot, relativePath), 'utf8');
}

const globalStyles = read('frontend/rl-app/src/styles.css');
const cycleTemplate = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos-ciclo-integral/matrices-riesgos-ciclo-integral.component.html');
const secondaryTemplate = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html');
const calculationTemplate = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/configuracion-calculo/configuracion-calculo.component.html');
const systemConfigurationTemplate = read('frontend/rl-app/src/app/features/admin/configuracion/pages/configuracion/configuracion.component.html');
const monitoringTemplate = read('frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.html');
const sidebarTemplate = read('frontend/rl-app/src/app/shared/layout/main-layout/main-layout.component.html');
const reportPreviewTemplate = read('frontend/rl-app/src/app/shared/report-preview/report-preview.component.html');
const formBuilderToolbarTemplate = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/components/form-builder/toolbar/form-builder-toolbar.component.html');

test('B01 defines one dynamic institutional selection contract with reusable variants', () => {
  assert.match(globalStyles, /\.institutional-selection\s*\{/);
  assert.match(globalStyles, /\.institutional-selection--compact\s*\{/);
  assert.match(globalStyles, /\.institutional-selection--regular\s*\{/);
  assert.match(globalStyles, /\.institutional-selection--card\s*\{/);
  assert.match(globalStyles, /\.institutional-selection--sidebar\s*\{/);
  assert.match(globalStyles, /\.institutional-selection__item\s*\{/);
  assert.match(globalStyles, /--institutional-selection-active-rgb:\s*var\(--ihss-700-rgb/);
  assert.match(globalStyles, /--institutional-selection-hover-rgb:\s*var\(--ihss-100-rgb/);
  assert.match(globalStyles, /institutional-selection__item:is\(\[aria-selected='true'/);
});

test('B01 migrates the three Matrices navigation groups without changing handlers', () => {
  assert.equal((cycleTemplate.match(/institutional-selection__item/g) ?? []).length, 5);
  assert.equal((cycleTemplate.match(/\[attr\.aria-pressed\]/g) ?? []).length, 5);
  assert.match(cycleTemplate, /seleccionarVista\('matriz'\)/);
  assert.match(cycleTemplate, /seleccionarVista\('configuracion'\)/);

  assert.match(secondaryTemplate, /institutional-selection institutional-selection--compact/);
  const secondaryNavigation = secondaryTemplate.slice(0, secondaryTemplate.indexOf('aria-label="Bloques funcionales'));
  assert.equal((secondaryNavigation.match(/institutional-selection__item/g) ?? []).length, 1);
  assert.match(secondaryNavigation, /role="tablist"/);
  assert.match(secondaryNavigation, /seleccionarTab\(/);

  const evaluationBlocks = secondaryTemplate.match(/institutional-selection institutional-selection--card/g) ?? [];
  assert.equal(evaluationBlocks.length, 2);
  assert.equal((secondaryTemplate.match(/\[attr\.aria-pressed\]="bloqueEvaluacionActual\(\) === bloque"/g) ?? []).length, 2);

  assert.match(calculationTemplate, /institutional-selection institutional-selection--regular/);
  assert.equal((calculationTemplate.match(/institutional-selection__item/g) ?? []).length, 1);
  assert.match(calculationTemplate, /role="tablist"/);
  assert.match(calculationTemplate, /seleccionarTab\(/);
});

test('B01 migrates Monitoring Lists cards and preserves the three functional types', () => {
  assert.match(monitoringTemplate, /institutional-selection institutional-selection--card/);
  assert.equal((monitoringTemplate.match(/institutional-selection__item/g) ?? []).length, 3);
  assert.equal((monitoringTemplate.match(/\[attr\.aria-pressed\]/g) ?? []).length, 3);
  for (const type of ['juridica', 'natural', 'empleado']) {
    assert.match(monitoringTemplate, new RegExp(`cambiarTipo\\('${type}'\\)`));
    assert.match(monitoringTemplate, new RegExp(`tipoActivo\\(\\) === '${type}'`));
  }
  assert.equal((monitoringTemplate.match(/institutional-selection__label/g) ?? []).length, 3);
  assert.equal((monitoringTemplate.match(/institutional-selection__description/g) ?? []).length, 3);
  assert.equal((monitoringTemplate.match(/institutional-selection__counter/g) ?? []).length, 3);
});

test('B01 migrates the system configuration tabs to the same contract', () => {
  assert.match(systemConfigurationTemplate, /institutional-selection institutional-selection--regular/);
  assert.equal((systemConfigurationTemplate.match(/institutional-selection__item/g) ?? []).length, 2);
  assert.equal((systemConfigurationTemplate.match(/\[attr\.aria-selected\]/g) ?? []).length, 2);
  assert.match(systemConfigurationTemplate, /role="tablist"/);
});

test('B01 negative regression rejects local active color decisions in migrated consumers', () => {
  const monitoringSelection = monitoringTemplate.slice(0, monitoringTemplate.indexOf('data-ui-monitoring-kpis'));
  const systemTabs = systemConfigurationTemplate.slice(0, systemConfigurationTemplate.indexOf('<!-- Alertas Globales -->'));
  assert.doesNotMatch(cycleTemplate, /\[class\.(?:bg-ihss-700|text-white)\]/);
  assert.doesNotMatch(secondaryTemplate, /\[class\.(?:bg-ihss-900|text-white|hover:text-gray-900|hover:bg-gray-100\/70)\]/);
  assert.doesNotMatch(calculationTemplate, /\[class\.(?:bg-ihss-700|text-white|bg-white|hover:bg-slate-50)\]/);
  assert.doesNotMatch(systemTabs, /\[class\]/);
  assert.doesNotMatch(monitoringSelection, /\[class\]=/);
});

test('B01 migrates the global sidebar and report/form-builder selectors', () => {
  assert.match(sidebarTemplate, /institutional-selection institutional-selection--sidebar/);
  assert.match(sidebarTemplate, /routerLinkActive="is-selected"/);
  assert.match(sidebarTemplate, /institutional-selection__item/);

  assert.match(reportPreviewTemplate, /institutional-selection institutional-selection--compact/);
  assert.match(reportPreviewTemplate, /role="tablist"/);
  assert.match(reportPreviewTemplate, /role="tab"/);
  assert.match(reportPreviewTemplate, /\[attr\.aria-selected\]/);

  assert.equal((formBuilderToolbarTemplate.match(/institutional-selection__item/g) ?? []).length, 4);
  assert.equal((formBuilderToolbarTemplate.match(/\[attr\.aria-selected\]/g) ?? []).length, 4);
  assert.equal((formBuilderToolbarTemplate.match(/\[class\.(?:bg-white|text-ihss-700|border-ihss-600)\]/g) ?? []).length, 0);
});
