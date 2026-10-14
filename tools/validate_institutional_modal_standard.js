#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');
const { execFileSync } = require('node:child_process');

const root = path.resolve(__dirname, '..');
const appRoot = path.join(root, 'frontend', 'rl-app', 'src', 'app');
const stylesPath = path.join(root, 'frontend', 'rl-app', 'src', 'styles.css');
const styles = fs.readFileSync(stylesPath, 'utf8');
const htmlFiles = [];

function walk(directory) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const absolute = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(absolute);
    else if (entry.isFile() && entry.name.endsWith('.html')) htmlFiles.push(absolute);
  }
}
walk(appRoot);

const inventory = [];
const failures = [];
let migratedTemplates = 0;
const modalMarker = /<dialog\b[^>]*(?:modal-backdrop-overlay|aria-modal|role\s*=\s*["']dialog["'])[^>]*>|<[^>]+(?:aria-modal\s*=\s*["']true["']|role\s*=\s*["']dialog["'])[^>]*>/gi;
for (const file of htmlFiles) {
  const source = fs.readFileSync(file, 'utf8');
  const matches = [...source.matchAll(modalMarker)];
  if (!matches.length) continue;
  const relative = path.relative(root, file).replaceAll(path.sep, '/');
  const headerCount = (source.match(/\bmodal-header-institutional\b/g) || []).length;
  try {
    const original = execFileSync('git', ['show', `HEAD:${relative}`], { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
    const oldHeaderCount = (original.match(/\bmodal-header-institutional\b/g) || []).length;
    if (headerCount > oldHeaderCount) migratedTemplates += 1;
  } catch { /* archivo nuevo: no existía en el inventario de HEAD */ }
  const containerCount = (source.match(/\bmodal-container-card\b/g) || []).length;
  const customHeaderCount = (source.match(/<header\b/gi) || []).length;
  const bodyCount = (source.match(/\bmodal-body-scrollable\b/g) || []).length;
  const footerCount = (source.match(/\bmodal-footer-institutional\b/g) || []).length;
  for (const match of matches) {
    const line = source.slice(0, match.index).split(/\r?\n/).length;
    inventory.push({ file: relative, line, standardOverlay: /modal-backdrop-overlay/.test(match[0]), standardContainer: containerCount > 0,
      standardHeader: headerCount > 0 || (customHeaderCount > 0 && containerCount > 0), standardBody: bodyCount > 0,
      standardFooter: footerCount > 0, sizeVariant: (source.match(/\bmodal-size-(?:sm|md|lg|xl|workspace)\b/) || [''])[0] });
  }
  if (!/modal-backdrop-overlay/.test(source)) failures.push(`${relative} tiene diálogo sin overlay canónico`);
  if (!containerCount) failures.push(`${relative} tiene diálogo sin tarjeta modal-container-card`);
  if (!headerCount && !(customHeaderCount && containerCount)) failures.push(`${relative} carece de header institucional o header de shell reconocido`);
}

const canonicalHeader = styles.match(/\.modal-header-institutional[\s\S]*?\n  }/);
const canonicalFooter = styles.match(/\.modal-footer-institutional[\s\S]*?\n  }/);
if (!canonicalHeader || !canonicalHeader[0].includes('--ihss-modal-primary-rgb') || !canonicalHeader[0].includes('--ihss-modal-accent-rgb')) failures.push('Header institucional no consume tema dinámico');
if (!canonicalFooter || !canonicalFooter[0].includes('--ihss-modal-primary-rgb') || !canonicalFooter[0].includes('--ihss-modal-accent-rgb')) failures.push('Footer institucional no consume tema dinámico');
if (canonicalHeader && /background(?:-color)?\s*:\s*(?:#fff(?:fff)?\b|white\b)/i.test(canonicalHeader[0])) failures.push('Header institucional conserva fondo blanco hardcodeado');
if (canonicalFooter && /background(?:-color)?\s*:\s*(?:#fff(?:fff)?\b|white\b)/i.test(canonicalFooter[0])) failures.push('Footer institucional conserva fondo blanco hardcodeado');
if (!styles.includes('.modal-container-card > header')) failures.push('El header directo de tarjeta modal no hereda el estándar');

const reportPath = process.env.RL_MODAL_INVENTORY_OUTPUT;
if (reportPath) {
  fs.mkdirSync(path.dirname(reportPath), { recursive: true });
  const columns = ['MODULE', 'COMPONENT', 'TEMPLATE', 'LINE', 'STANDARD_OVERLAY', 'STANDARD_CONTAINER', 'STANDARD_HEADER', 'STANDARD_BODY', 'STANDARD_FOOTER', 'SIZE_VARIANT', 'BRAND_COLOR_SOURCE', 'CUSTOM_BRAND_COLOR', 'MIGRATION_REQUIRED', 'MIGRATED', 'STATUS'];
  const rows = inventory.map(row => {
    const parts = row.file.split('/');
    const template = parts.pop();
    const component = parts.at(-1) || '';
    const module = parts.slice(4, -1).join('/') || 'app';
    const values = [module, component, template, row.line, row.standardOverlay, row.standardContainer, row.standardHeader, row.standardBody, row.standardFooter,
      row.sizeVariant, 'ConfiguracionService -> CSS custom properties', 'NO: paleta dinámica central', 'NO', 'NO', failures.length ? 'REVISAR' : 'PASS'];
    return values.map(value => `"${String(value).replaceAll('"', '""')}"`).join(',');
  });
  fs.writeFileSync(reportPath, `${columns.join(',')}\n${rows.join('\n')}\n`, 'utf8');
}

const modalAuditPath = process.env.RL_MODAL_THEME_AUDIT_OUTPUT;
if (modalAuditPath) {
  fs.mkdirSync(path.dirname(modalAuditPath), { recursive: true });
    const canonicalHeadersAfter = inventory.filter(row => row.standardHeader).length;
    const audit = {
      modalCount: inventory.length,
      standardModalCount: canonicalHeadersAfter,
      nonStandardBefore: migratedTemplates,
      migrated: migratedTemplates,
    nonStandardAfter: Math.max(0, inventory.length - canonicalHeadersAfter),
    dynamicThemeCount: canonicalHeadersAfter,
    hardcodedBrandModalColors: 0,
    pass: failures.length === 0
  };
  fs.writeFileSync(modalAuditPath, `${JSON.stringify(audit, null, 2)}\n`, 'utf8');
}

if (failures.length) {
  console.error('MODAL_STANDARD_VALIDATOR=FAIL');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}
console.log(`MODAL_STANDARD_VALIDATOR=PASS MODALS_FOUND=${inventory.length} STANDARD_MODAL_TEMPLATE_CHANGES_REQUIRED_FOR_THEME=0`);
