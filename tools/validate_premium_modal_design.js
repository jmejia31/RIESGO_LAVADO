#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');
const { validateCompositeContract } = require('./validate_institutional_modal_standard');

const root = path.resolve(__dirname, '..');
const stylesPath = path.join(root, 'frontend', 'rl-app', 'src', 'styles.css');
const styles = fs.readFileSync(stylesPath, 'utf8');
const failures = [];
const compositeContract = JSON.parse(fs.readFileSync(path.join(__dirname, 'modal_composite_shell_contract.json'), 'utf8'));

function rule(selectorPattern) {
  const match = styles.match(new RegExp(`${selectorPattern}\\s*\\{([\\s\\S]*?)\\n  }`));
  return match?.[1] ?? '';
}

const header = rule('\\.modal-header-institutional,\\s*\\.modal-container-card\\s*>\\s*header');
const footer = rule('\\.modal-footer-institutional');
const body = rule('\\.modal-body-scrollable');
const card = rule('\\.modal-container-card');
const close = styles.match(/:where\([\s\S]*?button\[aria-label\*="cerrar" i\][\s\S]*?\n  }/)?.[0] ?? '';

for (const [name, block] of [['header', header], ['footer', footer]]) {
  if (!block.includes('linear-gradient') || !block.includes('radial-gradient')) failures.push(`Modal ${name} must use a layered gradient surface`);
  for (const token of ['--ihss-modal-primary-start-rgb', '--ihss-modal-primary-mid-rgb']) {
    if (!block.includes(token)) failures.push(`Modal ${name} does not use dynamic token ${token}`);
  }
  if (!block.includes('--ihss-on-modal-rgb')) failures.push(`Modal ${name} lacks dynamic foreground contrast token`);
  if (/#[\da-f]{3,8}\b|\b(?:blue|indigo|violet)-(?:[3-9]00|950)\b/i.test(block)) failures.push(`Modal ${name} contains a fixed brand color`);
  if (/var\(--ihss-modal-[\w-]+-rgb\s*,/i.test(block)) failures.push(`Modal ${name} duplicates theme fallback values instead of consuming central tokens`);
}

if (!header.includes('--ihss-modal-primary-end-rgb') || !header.includes('--ihss-modal-highlight-rgb')) failures.push('Modal header lacks same-family end tone/highlight tokens');
if (!/\.modal-header-institutional::before[\s\S]*?pointer-events:\s*none/.test(styles)) failures.push('Modal header geometry must not capture pointer events');
if (!/\.modal-header-institutional::after[\s\S]*?pointer-events:\s*none/.test(styles)) failures.push('Modal header highlight must not capture pointer events');
if (!card.includes('background-color: #ffffff') || !body) failures.push('Modal card/body must retain a neutral content surface');
if (!close.includes('--modal-close-bg') || !close.includes('--modal-close-color')) failures.push('Modal close control does not consume global close tokens');
if (!styles.includes('--modal-close-focus-rgb: var(--ihss-accent-rgb)')) failures.push('Modal close focus ring does not inherit configured accent');

const appRoot = path.join(root, 'frontend', 'rl-app', 'src', 'app');
function walk(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const absolute = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(absolute) : [absolute];
  });
}
const localOverrides = [];
for (const file of walk(appRoot).filter(file => /\.(?:scss|css)$/.test(file))) {
  const source = fs.readFileSync(file, 'utf8');
  if (/\.modal-(?:header|footer)-institutional[^{}]*\{[^}]*background(?:-image|-color)?\s*:/is.test(source)) {
    localOverrides.push(path.relative(root, file).replaceAll(path.sep, '/'));
  }
}
if (localOverrides.length) failures.push(`Local modal shell brand overrides found: ${localOverrides.join(', ')}`);

const compositeTemplates = new Map();
for (const composite of compositeContract.composites ?? []) {
  for (const file of [composite.hostTemplate, composite.componentTemplate, composite.header.template, composite.footer.template]) {
    const absolute = path.join(root, file);
    if (fs.existsSync(absolute)) compositeTemplates.set(file, fs.readFileSync(absolute, 'utf8'));
  }
}
const compositeValidation = validateCompositeContract(compositeTemplates, compositeContract);
if (compositeValidation.failures.length) failures.push(...compositeValidation.failures);

if (failures.length) {
  console.error('PREMIUM_MODAL_VALIDATOR=FAIL');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}
console.log('PREMIUM_MODAL_VALIDATOR=PASS MODAL_HEADER_MULTILAYER=PASS MODAL_FOOTER_GRADIENT=PASS COMPOSITE_MODAL_REGIONS=PASS LOCAL_MODAL_BRAND_OVERRIDES=0');
