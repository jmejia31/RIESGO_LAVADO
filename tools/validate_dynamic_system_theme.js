#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const frontend = path.join(root, 'frontend', 'rl-app');
const tailwindPath = path.join(frontend, 'tailwind.config.js');
const servicePath = path.join(frontend, 'src', 'app', 'core', 'configuration', 'configuracion.service.ts');
const themePath = path.join(frontend, 'src', 'app', 'core', 'configuration', 'system-theme.util.ts');
const stylesPath = path.join(frontend, 'src', 'styles.css');
const tailwind = fs.readFileSync(tailwindPath, 'utf8');
const service = fs.readFileSync(servicePath, 'utf8');
const theme = fs.readFileSync(themePath, 'utf8');
const styles = fs.readFileSync(stylesPath, 'utf8');
const levels = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950];
const failures = [];

function walk(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const absolute = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(absolute) : [absolute];
  });
}

const auditRows = [];
let semanticBlueCount = 0;
let dynamicConfirmationButtonCount = 0;
const forbiddenBrandHex = /#(?:1e3a8a|1d4ed8|2563eb|3b82f6|2868dc|4d83e6|366fe0|2465d2|93c5fd|eff6ff|9bbcf7|f4f8ff)\b/ig;
for (const file of walk(path.join(frontend, 'src', 'app')).filter(file => /\.(?:html|scss|css|ts)$/.test(file) && !/\.spec\.ts$/.test(file))) {
  const source = fs.readFileSync(file, 'utf8');
  const relative = path.relative(root, file).replaceAll(path.sep, '/');
  const lines = source.split(/\r?\n/);
  lines.forEach((line, index) => {
    const semanticInfo = relative.endsWith('form-builder.component.html') && line.includes("fb.tipo === 'info'");
    for (const match of line.matchAll(/\b(?:text|bg|border|ring|outline|accent|fill|stroke)-blue-\d{2,3}(?:\/[\d.]+)?/g)) {
      if (!semanticInfo) failures.push(`Color azul fijo de UI en ${relative}:${index + 1} (${match[0]})`);
      else {
        semanticBlueCount += 1;
        auditRows.push([relative, index + 1, match[0], 'Mensaje de información', 'SEMANTIC', 'NO', 'Conservar color semántico informativo', 'PASS']);
      }
    }
    const centralDefault = relative.endsWith('core/configuration/system-theme.util.ts') && /DEFAULT_SYSTEM_(?:PRIMARY|ACCENT)/.test(line);
    if (forbiddenBrandHex.test(line) && !centralDefault) failures.push(`HEX de marca azul fijo en ${relative}:${index + 1}`);
    forbiddenBrandHex.lastIndex = 0;
    if (/confirmButtonColor\s*:\s*['"]#(?:1e3a8a|1d4ed8|2563eb|3b82f6)['"]/i.test(line)) {
      failures.push(`Color fijo de marca en confirmButtonColor en ${relative}:${index + 1}`);
    }
    if (/confirmButtonColor\s*:\s*['"]var\(--ihss-primary-color\)['"]/i.test(line)) dynamicConfirmationButtonCount += 1;
    for (const match of line.matchAll(/(?:text|bg|border|ring)-ihss-\d{2,3}(?:\/[\d.]+)?|--ihss-(?:primary|accent|modal|(?:on-)?\d{2,3})-[a-z-]+/g)) {
      auditRows.push([relative, index + 1, match[0], 'Identidad institucional', 'BRAND', 'YES', 'Consume tema global de Configuración', 'PASS']);
    }
  });
}

for (const level of levels) {
  const line = tailwind.match(new RegExp(`\\b${level}:\\s*([^,\\n]+)`));
  if (!line || !line[1].includes(`--ihss-${level}-rgb`)) failures.push(`Tailwind ihss-${level} no usa su token dinámico`);
  if (line && /#[\da-f]{3,8}/i.test(line[1])) failures.push(`Tailwind ihss-${level} contiene HEX fijo`);
}
if (!service.includes('config.colorPrimario') || !service.includes('config.colorSecundario')) failures.push('Configuración no aplica ambos colores fuente');
if ((service.match(/this\.aplicarTemaSistema\(/g) || []).length !== 2) failures.push('Carga y guardado no comparten el único método de aplicación del tema');
if (!theme.includes('relativeLuminance') || !theme.includes('contrastRatio') || !theme.includes('darkSurfaceWithWhiteContrast')) failures.push('Falta generación central y accesible de superficies/contraste');
if (!styles.includes('rgb(var(--ihss-modal-primary-rgb') || !styles.includes('rgb(var(--ihss-modal-accent-rgb')) failures.push('El shell modal no consume los tokens primario y secundario');
if (!styles.includes('rgb(var(--ihss-on-modal-rgb')) failures.push('El shell modal no usa foreground dinámico');
if (!styles.includes('--modal-close-focus-rgb: var(--ihss-accent-rgb)')) failures.push('El foco del control de cierre no hereda el accent configurado');
if (!/\.btn-primary[\s\S]*?color:\s*rgb\(var\(--ihss-on-700-rgb/.test(styles)) failures.push('El botón primario no consume foreground de contraste de su superficie dinámica');
if (!dynamicConfirmationButtonCount) failures.push('No se detectaron confirmaciones SweetAlert con color primario dinámico');

if (failures.length) {
  console.error('DYNAMIC_THEME_VALIDATOR=FAIL');
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}
const auditPath = process.env.RL_THEME_COLOR_AUDIT_OUTPUT;
if (auditPath) {
  fs.mkdirSync(path.dirname(auditPath), { recursive: true });
  const header = ['FILE', 'LINE', 'COLOR_OR_CLASS', 'CONTEXT', 'CLASSIFICATION', 'BRAND_DYNAMIC', 'ACTION', 'PASS'];
  fs.writeFileSync(auditPath, `${header.join(',')}\n${auditRows.map(row => row.map(value => `"${String(value).replaceAll('"', '""')}"`).join(',')).join('\n')}\n`, 'utf8');
}
console.log(`DYNAMIC_THEME_VALIDATOR=PASS IHSS_DYNAMIC_PALETTE_LEVELS=${levels.length}/${levels.length} SYSTEM_THEME_APPLICATIONS=1 DYNAMIC_CONFIRMATION_BUTTONS=${dynamicConfirmationButtonCount} HARDCODED_BRAND_BLUE=0 SEMANTIC_BLUE=${semanticBlueCount}`);
