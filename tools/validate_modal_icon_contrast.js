#!/usr/bin/env node
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const appRoot = path.join(root, 'frontend', 'rl-app', 'src', 'app');
const styles = fs.readFileSync(path.join(root, 'frontend', 'rl-app', 'src', 'styles.css'), 'utf8');
const formBuilderToolbar = fs.readFileSync(path.join(appRoot, 'features', 'admin', 'matrices-riesgos', 'components', 'form-builder', 'toolbar', 'form-builder-toolbar.component.html'), 'utf8');
const formBuilderStatusbar = fs.readFileSync(path.join(appRoot, 'features', 'admin', 'matrices-riesgos', 'components', 'form-builder', 'statusbar', 'form-builder-statusbar.component.html'), 'utf8');
const failures = [];

function walk(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const absolute = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(absolute) : [absolute];
  });
}

function attribute(attributes, names) {
  for (const name of names) {
    const escaped = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const value = attributes.match(new RegExp(`(?:^|\\s)${escaped}\\s*=\\s*["']([^"']*)["']`, 'i'))?.[1];
    if (value !== undefined) return value;
  }
  return '';
}

function isFooterRegion(attributes) {
  return /\bclass\s*=\s*["'][^"']*\bmodal-footer-institutional\b/i.test(attributes)
    || /\bdata-modal-shell-region\s*=\s*["']footer["']/i.test(attributes);
}

function auditModalFooterActions(templates) {
  const audit = { actions: 0, visibleText: 0, missingAria: 0, missingTooltip: 0, failures: [] };
  for (const [file, source] of templates) {
    const tags = [...source.matchAll(/<\/?([a-z][\w:-]*)\b([^>]*)>/gi)];
    const stack = [];
    let button = null;
    for (const tag of tags) {
      const closing = tag[0].startsWith('</');
      const name = tag[1].toLowerCase();
      if (!closing) {
        const attributes = tag[2];
        const inFooter = stack.some(frame => frame.footer) || isFooterRegion(attributes);
        if (name === 'button' && inFooter) button = { start: tag.index + tag[0].length, attributes, location: `${file}: ${attribute(attributes, ['aria-label', '[attr.aria-label]']) || '(sin aria-label)'}` };
        if (!/\/\s*>$/.test(tag[0]) && !['input', 'img', 'br', 'hr', 'meta', 'link'].includes(name)) {
          stack.push({ name, footer: inFooter });
        }
        continue;
      }

      if (name === 'button' && button) {
        const content = source.slice(button.start, tag.index);
        audit.actions += 1;
        const hasAria = attribute(button.attributes, ['aria-label', '[attr.aria-label]']).trim().length > 0;
        const hasTooltip = attribute(button.attributes, ['title', '[title]', 'data-tooltip', '[attr.title]']).trim().length > 0;
        const classes = attribute(button.attributes, ['class']);
        const usesPrimitive = /(?:^|\s)modal-action-icon(?:\s|$)/.test(classes);
        const hasVariant = /(?:^|\s)modal-action-icon--(?:primary|secondary|neutral|danger|success)(?:\s|$)/.test(classes);
        const hasActionIcon = /<app-action-icon\b/i.test(content);
        const withoutScreenReaderText = content.replace(/<[^>]*class\s*=\s*["'][^"']*\bsr-only\b[^"']*["'][^>]*>[\s\S]*?<\/[^>]+>/gi, ' ');
        const visibleText = withoutScreenReaderText.replace(/<[^>]+>/g, ' ').replace(/\{\{[\s\S]*?\}\}/g, ' ').trim();
        if (!hasAria) audit.missingAria += 1;
        if (!hasTooltip) audit.missingTooltip += 1;
        if (visibleText) audit.visibleText += 1;
        if (!hasAria) audit.failures.push(`${button.location} carece de aria-label`);
        if (!hasTooltip) audit.failures.push(`${button.location} carece de title/tooltip`);
        if (!usesPrimitive || !hasVariant) audit.failures.push(`${button.location} no usa una variante de modal-action-icon`);
        if (!hasActionIcon) audit.failures.push(`${button.location} carece de app-action-icon`);
        if (visibleText) audit.failures.push(`${button.location} conserva texto de acción visible`);
        if (/(?:^|\s)(?:bg|text|border)-(?:blue|indigo|violet|purple|cyan|sky|teal|emerald|red|rose|orange|amber|yellow|green|ihss|slate|gray)-\d{2,3}(?:\/\d+)?(?:\s|$)/i.test(classes)) {
          audit.failures.push(`${button.location} tiene colores locales en vez de la variante global`);
        }
        const inlineStyle = attribute(button.attributes, ['style']);
        if (/(?:background|color|border-color)\s*:/i.test(inlineStyle)) audit.failures.push(`${button.location} tiene branding inline`);
        button = null;
      }

      const frameIndex = stack.map(frame => frame.name).lastIndexOf(name);
      if (frameIndex >= 0) stack.splice(frameIndex);
    }
  }
  return audit;
}

const templates = walk(appRoot).filter(file => file.endsWith('.html')).map(file => [path.relative(root, file).replaceAll(path.sep, '/'), fs.readFileSync(file, 'utf8')]);
const audit = auditModalFooterActions(templates);
failures.push(...audit.failures);
if (!styles.includes('--modal-close-color') || !styles.includes('--modal-close-color-hover') || !styles.includes('--modal-close-border')) failures.push('Modal close icon lacks themed foreground/border tokens');
if (!/button\[aria-label\*="cerrar" i\][\s\S]*?focus-visible/.test(styles)) failures.push('Modal close controls lack a visible keyboard focus treatment');
if (!styles.includes('.modal-container-card [data-modal-shell-region="header"]')) failures.push('Brand close tokens are not scoped to a certified institutional header region');
if (styles.includes('.form-builder-toolbar__top-actions')) failures.push('Brand close tokens still target the Form Builder neutral toolbar actions');
if (!/class="[^"]*modal-header-institutional[^"]*"[^>]*data-modal-shell-region="header"/.test(formBuilderToolbar)
  || !/<button[^>]*aria-label="Cerrar modal de constructor"/.test(formBuilderToolbar)) {
  failures.push('Form Builder close control does not live inside its certified brand header region');
}
if (!/class="[^"]*modal-footer-institutional[^"]*"[^>]*data-modal-shell-region="footer"/.test(formBuilderStatusbar)
  || !/aria-label="Cerrar constructor"[\s\S]*?<app-action-icon action="close"/.test(formBuilderStatusbar)
  || !/aria-label="Cancelar cambios"[\s\S]*?<app-action-icon action="cancel"/.test(formBuilderStatusbar)
  || !/aria-label="Guardar cambios"[\s\S]*?<app-action-icon action="save"/.test(formBuilderStatusbar)) {
  failures.push('Form Builder footer must expose accessible close/cancel/save ActionIcon controls');
}
if (!styles.includes('currentColor') && !fs.readFileSync(path.join(appRoot, 'shared', 'components', 'action-icon', 'action-icon.component.ts'), 'utf8').includes('currentColor')) {
  failures.push('Action icons must inherit the foreground color');
}

if (failures.length) {
  console.error('ICON_CONTRAST_VALIDATOR=FAIL');
  console.error('MODAL_FOOTER_ICON_ONLY_STANDARD=FAIL');
  console.error(`MODAL_FOOTER_ACTIONS_AUDITED=${audit.actions}`);
  console.error(`MODAL_FOOTER_ICON_ACTIONS=${audit.actions - audit.visibleText}`);
  console.error(`MODAL_FOOTER_TEXT_ACTIONS=${audit.visibleText}`);
  console.error(`MODAL_FOOTER_MISSING_ARIA=${audit.missingAria}`);
  console.error(`MODAL_FOOTER_MISSING_TOOLTIP=${audit.missingTooltip}`);
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}
console.log('ICON_CONTRAST_VALIDATOR=PASS');
console.log('MODAL_FOOTER_ICON_ONLY_STANDARD=PASS');
console.log(`MODAL_FOOTER_ACTIONS_AUDITED=${audit.actions}`);
console.log(`MODAL_FOOTER_ICON_ACTIONS=${audit.actions - audit.visibleText}`);
console.log(`MODAL_FOOTER_TEXT_ACTIONS=${audit.visibleText}`);
console.log(`MODAL_FOOTER_MISSING_ARIA=${audit.missingAria}`);
console.log(`MODAL_FOOTER_MISSING_TOOLTIP=${audit.missingTooltip}`);

module.exports = { auditModalFooterActions };
