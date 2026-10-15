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
  const audit = { actions: 0, visibleText: 0, missingAria: 0, missingTooltip: 0, items: [], failures: [] };
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
        const icon = content.match(/<app-action-icon\b[^>]*\baction\s*=\s*["']([^"']+)["']/i)?.[1] ?? '';
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
        audit.items.push({
          file,
          action: attribute(button.attributes, ['aria-label', '[attr.aria-label]']),
          icon,
          visibleText,
          ariaLabel: attribute(button.attributes, ['aria-label', '[attr.aria-label]']),
          title: attribute(button.attributes, ['title', '[title]', 'data-tooltip', '[attr.title]']),
          disabledRule: /\bdisabled\b|\[disabled\]/.test(button.attributes) ? 'YES' : 'NO',
          variant: (classes.match(/\bmodal-action-icon--(?:primary|secondary|neutral|danger|success)\b/) ?? [''])[0],
          usesGlobalPrimitive: usesPrimitive
        });
        button = null;
      }

      const frameIndex = stack.map(frame => frame.name).lastIndexOf(name);
      if (frameIndex >= 0) stack.splice(frameIndex);
    }
  }
  return audit;
}

function auditActionIconSemantics(templates) {
  const result = { headerClose: 0, footerExit: 0, footerCancel: 0, footerExitX: 0, footerCancelX: 0, footerCloseActions: 0, footerCloseHeaderX: 0, collisions: 0, failures: [] };
  for (const [file, source] of templates) {
    const tags = [...source.matchAll(/<\/?([a-z][\w:-]*)\b([^>]*)>/gi)];
    const stack = [];
    let button = null;
    for (const tag of tags) {
      const closing = tag[0].startsWith('</');
      const name = tag[1].toLowerCase();
      if (!closing) {
        const attrs = tag[2];
        const inheritedHeader = stack.some(frame => frame.header);
        const inheritedFooter = stack.some(frame => frame.footer);
        const header = inheritedHeader || /\bclass\s*=\s*["'][^"']*\bmodal-header-institutional\b/i.test(attrs)
          || /\bdata-modal-shell-region\s*=\s*["']header["']/i.test(attrs);
        const footer = inheritedFooter || isFooterRegion(attrs);
        if (name === 'button' && (header || footer)) button = { start: tag.index + tag[0].length, attrs, header, footer };
        if (!/\/\s*>$/.test(tag[0]) && !['input', 'img', 'br', 'hr', 'meta', 'link'].includes(name)) stack.push({ name, header, footer });
        continue;
      }

      if (name === 'button' && button) {
        const content = source.slice(button.start, tag.index);
        const label = attribute(button.attrs, ['aria-label', '[attr.aria-label]']).trim().toLowerCase();
        const icon = content.match(/<app-action-icon\b[^>]*\baction\s*=\s*["']([^"']+)["']/i)?.[1]?.toLowerCase() ?? '';
        const location = `${file}: ${label || '(sin aria-label)'}`;
        if (button.header && /cerrar|close/.test(label)) {
          result.headerClose += 1;
          if (icon !== 'close') result.failures.push(`${location} debe usar close en el header (usa ${icon || 'sin icono'})`);
          if (['exit', 'logout', 'cancel'].includes(icon)) result.collisions += 1;
        }
        if (button.footer && /\b(salir|salida)\b/.test(label)) {
          result.footerExit += 1;
          if (!['exit', 'logout'].includes(icon)) result.failures.push(`${location} debe usar una semántica exit`);
          if (icon === 'close') { result.footerExitX += 1; result.collisions += 1; }
        }
        if (button.footer && /cancelar|cancelaci[oó]n/.test(label)) {
          result.footerCancel += 1;
          if (icon !== 'cancel') result.failures.push(`${location} debe usar cancel y no la X del header`);
          if (icon === 'close') { result.footerCancelX += 1; result.collisions += 1; }
        }
        if (button.footer && /\b(cerrar|salir|vista de evaluaci[oó]n)\b/.test(label)) {
          result.footerCloseActions += 1;
          result.footerCloseHeaderX += Number(icon === 'close');
          if (!['exit', 'logout'].includes(icon)) result.failures.push(`${location} debe usar exit/logout para cerrar desde el footer`);
          if (icon === 'close' && !/cancelar|cancelaci[oó]n/.test(label) && !/\b(salir|salida)\b/.test(label)) result.collisions += 1;
        }
        button = null;
      }

      const frameIndex = stack.map(frame => frame.name).lastIndexOf(name);
      if (frameIndex >= 0) stack.splice(frameIndex);
    }
  }
  return result;
}

const templates = walk(appRoot).filter(file => file.endsWith('.html')).map(file => [path.relative(root, file).replaceAll(path.sep, '/'), fs.readFileSync(file, 'utf8')]);
const audit = auditModalFooterActions(templates);
const semantics = auditActionIconSemantics(templates);
failures.push(...audit.failures);
failures.push(...semantics.failures);
if (!styles.includes('--modal-close-color') || !styles.includes('--modal-close-color-hover') || !styles.includes('--modal-close-border')) failures.push('Modal close icon lacks themed foreground/border tokens');
if (!/button\[aria-label\*="cerrar" i\][\s\S]*?focus-visible/.test(styles)) failures.push('Modal close controls lack a visible keyboard focus treatment');
if (!styles.includes('.modal-container-card [data-modal-shell-region="header"]')) failures.push('Brand close tokens are not scoped to a certified institutional header region');
if (styles.includes('.form-builder-toolbar__top-actions')) failures.push('Brand close tokens still target the Form Builder neutral toolbar actions');
if (!/class="[^"]*modal-header-institutional[^"]*"[^>]*data-modal-shell-region="header"/.test(formBuilderToolbar)
  || !/<button[^>]*aria-label="Cerrar modal de constructor"/.test(formBuilderToolbar)) {
  failures.push('Form Builder close control does not live inside its certified brand header region');
}
if (!/class="[^"]*modal-footer-institutional[^"]*"[^>]*data-modal-shell-region="footer"/.test(formBuilderStatusbar)
  || !/aria-label="Cerrar constructor"[\s\S]*?<app-action-icon action="exit"/.test(formBuilderStatusbar)
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
  console.error(`HEADER_CLOSE_X_COUNT=${semantics.headerClose}`);
  console.error(`FOOTER_EXIT_ACTIONS_AUDITED=${semantics.footerExit}`);
  console.error(`FOOTER_CANCEL_ACTIONS_AUDITED=${semantics.footerCancel}`);
  console.error(`FOOTER_EXIT_X_COUNT=${semantics.footerExitX}`);
  console.error(`FOOTER_CANCEL_X_COUNT=${semantics.footerCancelX}`);
  console.error(`FOOTER_CLOSE_ACTIONS_AUDITED=${semantics.footerCloseActions}`);
  console.error(`FOOTER_CLOSE_HEADER_X_COUNT=${semantics.footerCloseHeaderX}`);
  console.error(`HEADER_CLOSE_EXIT_CANCEL_COLLISION=${semantics.collisions}`);
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
console.log(`HEADER_CLOSE_X_COUNT=${semantics.headerClose}`);
console.log(`FOOTER_EXIT_ACTIONS_AUDITED=${semantics.footerExit}`);
console.log(`FOOTER_CANCEL_ACTIONS_AUDITED=${semantics.footerCancel}`);
console.log(`FOOTER_EXIT_X_COUNT=${semantics.footerExitX}`);
console.log(`FOOTER_CANCEL_X_COUNT=${semantics.footerCancelX}`);
console.log(`FOOTER_CLOSE_ACTIONS_AUDITED=${semantics.footerCloseActions}`);
console.log(`FOOTER_CLOSE_HEADER_X_COUNT=${semantics.footerCloseHeaderX}`);
console.log(`HEADER_CLOSE_EXIT_CANCEL_COLLISION=${semantics.collisions}`);

module.exports = { auditActionIconSemantics, auditModalFooterActions };
