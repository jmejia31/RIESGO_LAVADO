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
const modalActionContractFailures = [];
let modalActionButtonCount = 0;
let iconActionCount = 0;

function walk(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const absolute = path.join(directory, entry.name);
    return entry.isDirectory() ? walk(absolute) : [absolute];
  });
}

for (const file of walk(appRoot).filter(file => file.endsWith('.html'))) {
  const source = fs.readFileSync(file, 'utf8');
  iconActionCount += (source.match(/<app-action-icon\b/gi) || []).length;
  const tags = [...source.matchAll(/<\/?([a-z][\w:-]*)\b([^>]*)>/gi)];
  const stack = [];
  let buttonStart = -1;
  let buttonAttrs = '';
  let insideFooterButton = false;
  for (const tag of tags) {
    const closing = tag[0].startsWith('</');
    const name = tag[1].toLowerCase();
    if (!closing) {
      const attrs = tag[2];
      const isFooter = /\bclass\s*=\s*["'][^"']*modal-footer-institutional/i.test(attrs);
      if (name === 'button' && stack.some(frame => frame.footer) || name === 'button' && isFooter) {
        buttonStart = tag.index;
        buttonAttrs = attrs;
        insideFooterButton = true;
      }
      if (!/\/\s*>$/.test(tag[0]) && !['input', 'img', 'br', 'hr', 'meta', 'link'].includes(name)) stack.push({ name, footer: isFooter || stack.some(frame => frame.footer) });
      continue;
    }
      if (name === 'button' && insideFooterButton && buttonStart >= 0) {
        const buttonHtml = source.slice(buttonStart, tag.index + tag[0].length);
        const label = buttonAttrs.match(/aria-label=["']([^"']+)["']/i)?.[1] ?? '';
        const content = buttonHtml.slice(buttonHtml.indexOf('>') + 1, buttonHtml.lastIndexOf('</'));
        const isModalAction = /\bclass\s*=\s*["'][^"']*\bmodal-action-icon\b/i.test(buttonAttrs);
        if (isModalAction) {
          modalActionButtonCount += 1;
          const tooltip = buttonAttrs.match(/(?:title|data-tooltip)=["']([^"']+)["']/i)?.[1] ?? '';
          const visibleContent = content.replace(/<[^>]*class=["'][^"']*\bsr-only\b[^"']*["'][^>]*>[\s\S]*?<\/[^>]+>/gi, ' ');
          const visibleText = visibleContent.replace(/<[^>]+>/g, ' ').replace(/\{\{[\s\S]*?\}\}/g, ' ').trim();
          const location = `${path.relative(root, file).replaceAll(path.sep, '/')}: ${label || '(missing aria-label)'}`;
          if (!label.trim()) modalActionContractFailures.push(`${location} has no accessible name`);
          if (!tooltip.trim()) modalActionContractFailures.push(`${location} has no title or tooltip`);
          if (!/<app-action-icon\b/i.test(content)) modalActionContractFailures.push(`${location} has no shared ActionIcon`);
          if (visibleText) modalActionContractFailures.push(`${location} contains visible action text`);
        }
      buttonStart = -1;
      buttonAttrs = '';
      insideFooterButton = false;
    }
    const frameIndex = stack.map(frame => frame.name).lastIndexOf(name);
    if (frameIndex >= 0) stack.splice(frameIndex);
  }
}

failures.push(...modalActionContractFailures);
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
  for (const failure of failures) console.error(`- ${failure}`);
  process.exit(1);
}
console.log(`ICON_CONTRAST_VALIDATOR=PASS MODAL_ACTION_ICON_BUTTONS=${modalActionButtonCount} ICON_ACTIONS_AUDITED=${iconActionCount}`);
