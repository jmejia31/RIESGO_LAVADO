'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { auditActionIconSemantics, auditModalFooterActions } = require('./validate_modal_icon_contrast');

function audit(button, footer = '<div class="modal-footer-institutional">') {
  const template = `${footer}${button}</div>`;
  return auditModalFooterActions([['fixture.html', template]]);
}

const iconOnly = '<button type="button" class="modal-action-icon modal-action-icon--primary" aria-label="Guardar" title="Guardar"><app-action-icon action="save"></app-action-icon></button>';

test('accepts an accessible icon-only action in a standard footer', () => {
  const result = audit(iconOnly);
  assert.deepEqual(result.failures, []);
  assert.equal(result.actions, 1);
  assert.equal(result.visibleText, 0);
  assert.equal(result.missingAria, 0);
  assert.equal(result.missingTooltip, 0);
});

test('accepts screen-reader-only text but rejects visible text next to the icon', () => {
  const hidden = iconOnly.replace('</app-action-icon>', '</app-action-icon><span class="sr-only">Guardar</span>');
  assert.deepEqual(audit(hidden).failures, []);
  const visible = iconOnly.replace('</app-action-icon>', '</app-action-icon><span>Guardar</span>');
  assert.ok(audit(visible).failures.some(message => message.includes('texto de acción visible')));
});

test('rejects missing accessible name or tooltip', () => {
  const noAria = iconOnly.replace(' aria-label="Guardar"', '');
  const noTitle = iconOnly.replace(' title="Guardar"', '');
  assert.ok(audit(noAria).failures.some(message => message.includes('aria-label')));
  assert.ok(audit(noTitle).failures.some(message => message.includes('title/tooltip')));
});

test('rejects text-only controls and actions without the shared primitive', () => {
  const textOnly = '<button type="button" aria-label="Guardar" title="Guardar">Guardar</button>';
  assert.ok(audit(textOnly).failures.some(message => message.includes('modal-action-icon')));
  assert.ok(audit(textOnly).failures.some(message => message.includes('app-action-icon')));
});

test('rejects hardcoded brand utility colors on a footer action', () => {
  const localBrand = iconOnly.replace('modal-action-icon--primary', 'modal-action-icon--primary bg-blue-900 text-white');
  assert.ok(audit(localBrand).failures.some(message => message.includes('colores locales')));
});

test('recognizes a composite footer region and ignores controls outside it', () => {
  const composite = '<section data-modal-shell-region="footer">' + iconOnly + '</section>';
  const outside = '<button type="button">Paginación</button>' + composite;
  const result = audit(outside, '<div class="composite-shell">');
  assert.deepEqual(result.failures, []);
  assert.equal(result.actions, 1);
});

test('requires X for header close and distinct exit/cancel footer semantics', () => {
  const template = `
    <dialog><header class="modal-header-institutional"><button aria-label="Cerrar modal" title="Cerrar modal"><app-action-icon action="close"></app-action-icon></button></header>
    <footer class="modal-footer-institutional">
      <button aria-label="Salir de la vista" title="Salir de la vista"><app-action-icon action="exit"></app-action-icon></button>
      <button aria-label="Cancelar cambios" title="Cancelar cambios"><app-action-icon action="cancel"></app-action-icon></button>
    </footer></dialog>`;
  const result = auditActionIconSemantics([['positive.html', template]]);
  assert.deepEqual(result.failures, []);
  assert.equal(result.headerClose, 1);
  assert.equal(result.footerExit, 1);
  assert.equal(result.footerCancel, 1);
  assert.equal(result.collisions, 0);
});

test('rejects X reused for footer exit or cancel', () => {
  const template = `<footer class="modal-footer-institutional">
    <button aria-label="Cerrar vista" title="Cerrar vista"><app-action-icon action="close"></app-action-icon></button>
    <button aria-label="Cancelar cambios" title="Cancelar cambios"><app-action-icon action="close"></app-action-icon></button>
  </footer>`;
  const result = auditActionIconSemantics([['negative.html', template]]);
  assert.equal(result.footerCloseHeaderX, 1);
  assert.equal(result.collisions, 2);
  assert.equal(result.failures.length, 2);
});
