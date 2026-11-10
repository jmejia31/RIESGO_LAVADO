'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const repoRoot = path.resolve(__dirname, '..');

function read(relativePath) {
  return fs.readFileSync(path.join(repoRoot, relativePath), 'utf8');
}

function rulesFor(css, selectorPart) {
  return [...css.matchAll(/([^{}]+)\{([^{}]*)\}/g)]
    .filter(match => match[1].includes(selectorPart))
    .map(match => ({ selector: match[1].trim(), declarations: match[2] }));
}

function hasActiveBackgroundConflict(css) {
  return rulesFor(css, '.calculation-workspace__header nav button')
    .some(rule => /background\s*:\s*transparent\s*!important/i.test(rule.declarations));
}

test('B01 calculation tabs preserve the dynamic active surface at desktop breakpoints', () => {
  const template = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/configuracion-calculo/configuracion-calculo.component.html');
  const styles = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/configuracion-calculo/configuracion-calculo.component.scss');

  const secondaryNavigation = template.slice(0, template.indexOf('aria-label="Bloques funcionales'));
  assert.equal((secondaryNavigation.match(/institutional-selection__item/g) ?? []).length, 1);
  assert.equal((secondaryNavigation.match(/\[attr\.aria-selected\]/g) ?? []).length, 1);
  assert.equal(hasActiveBackgroundConflict(styles), false);
  assert.match(styles, /\.calculation-workspace__header nav button\{[^}]*border-bottom:2px solid transparent!important/);
});

test('B01 negative regression detects the real transparent-background cascade conflict', () => {
  const conflictingCss = '@media(min-width:1280px){.calculation-workspace__header nav button{background:transparent!important;color:white!important}}';
  assert.equal(hasActiveBackgroundConflict(conflictingCss), true);
  assert.equal(hasActiveBackgroundConflict(read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/configuracion-calculo/configuracion-calculo.component.scss')), false);
});

test('B01 cycle navigation uses dynamic institutional tokens for all five options', () => {
  const template = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos-ciclo-integral/matrices-riesgos-ciclo-integral.component.html');
  const styles = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos-ciclo-integral/matrices-riesgos-ciclo-integral.component.scss');

  assert.equal((template.match(/institutional-selection__item/g) ?? []).length, 5);
  assert.equal((template.match(/\[attr\.aria-pressed\]/g) ?? []).length, 5);
  assert.equal(template.includes('[class.bg-slate-900]'), false);
  assert.match(styles, /button\[aria-pressed='true'\]/);
  assert.doesNotMatch(styles, /button\[aria-pressed='true'\]\{[^}]*background\s*:/);
  assert.match(styles, /--cycle-ink:\s*rgb\(var\(--ihss-950-rgb/);
});

test('B01 secondary navigation retains the dynamic active surface and theme-derived shadows', () => {
  const template = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html');
  const styles = read('frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.scss');
  const activeRule = rulesFor(styles, ".matrices-tabs-shell [role='tab'][aria-selected='true']")[0];

  assert.ok(activeRule);
  const secondaryNavigation = template.slice(0, template.indexOf('aria-label="Bloques funcionales'));
  assert.equal((secondaryNavigation.match(/institutional-selection__item/g) ?? []).length, 1);
  assert.equal((secondaryNavigation.match(/\[attr\.aria-selected\]/g) ?? []).length, 1);
  assert.match(activeRule.declarations, /background-color:\s*rgb\(var\(--institutional-selection-active-rgb/);
  assert.match(activeRule.declarations, /color:\s*rgb\(var\(--institutional-selection-active-fg-rgb/);
  assert.match(activeRule.declarations, /var\(--ihss-primary-rgb/);
  assert.doesNotMatch(styles, /--matrices-ink:\s*#0f1f3d/i);
});

test('B01 dynamic foreground contract remains available for token surfaces', () => {
  const globalStyles = read('frontend/rl-app/src/styles.css');
  assert.match(globalStyles, /bg-ihss-700"\]\[class~="text-white"\]\)\s*\{\s*color:\s*rgb\(var\(--ihss-on-700-rgb/);
  assert.match(globalStyles, /bg-ihss-900"\]\[class~="text-white"\]\)\s*\{\s*color:\s*rgb\(var\(--ihss-on-900-rgb/);
});
