'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { validateModalTemplates } = require('./validate_institutional_modal_standard');

const directModal = '<dialog open aria-modal="true" class="modal-backdrop-overlay"><div class="modal-container-card"><header class="modal-header-institutional"></header><main class="modal-body-scrollable"></main></div></dialog>';

function compositeContract() {
  return {
    composites: [{
      selector: 'app-form-builder',
      hostAttribute: 'data-modal-composite-shell',
      hostTemplate: 'parent.html',
      componentTemplate: 'builder.html',
      header: { componentSelector: 'app-toolbar', template: 'toolbar.html', marker: 'header', className: 'modal-header-institutional' },
      body: { marker: 'body', minimumInstances: 2 },
      footer: { componentSelector: 'app-statusbar', template: 'statusbar.html', marker: 'footer', className: 'modal-footer-institutional' }
    }]
  };
}

function compositeTemplates(host = '<dialog open aria-modal="true" class="modal-backdrop-overlay"><div class="modal-container-card"><app-form-builder data-modal-composite-shell="true"></app-form-builder></div></dialog>') {
  return new Map([
    ['parent.html', host],
    ['builder.html', '<app-toolbar></app-toolbar><div data-modal-shell-region="body"></div><div data-modal-shell-region="body"></div><app-statusbar></app-statusbar>'],
    ['toolbar.html', '<div class="modal-header-institutional" data-modal-shell-region="header"></div>'],
    ['statusbar.html', '<footer class="modal-footer-institutional" data-modal-shell-region="footer"></footer>']
  ]);
}

test('validates each modal instance: a good first modal cannot certify a bad second modal', () => {
  const templates = new Map([['two-modals.html', `${directModal}<dialog open aria-modal="true" class="modal-backdrop-overlay"><div class="modal-container-card"><section><header class="bg-white"></header></section></div></dialog>`]]);
  const { inventory, failures } = validateModalTemplates(templates, { composites: [] });

  assert.equal(inventory.length, 2);
  assert.equal(inventory[0].standardHeader, true);
  assert.equal(inventory[1].standardHeader, false);
  assert.ok(failures.some(message => message.includes('modal sin header institucional propio')));
});

test('an arbitrary nested header is not an institutional modal header', () => {
  const templates = new Map([['arbitrary-header.html', '<dialog open aria-modal="true" class="modal-backdrop-overlay"><div class="modal-container-card"><app-preview><header class="bg-white"></header></app-preview></div></dialog>']]);
  const { inventory, failures } = validateModalTemplates(templates, { composites: [] });

  assert.equal(inventory[0].standardHeader, false);
  assert.ok(failures.some(message => message.includes('sin header institucional propio')));
  assert.ok(failures.some(message => message.includes('componente(s) sin contrato')));
});

test('a declared composite passes only when header, body and footer regions match its contract', () => {
  const { inventory, failures } = validateModalTemplates(compositeTemplates(), compositeContract());

  assert.deepEqual(failures, []);
  assert.equal(inventory.length, 1);
  assert.equal(inventory[0].standardHeader, true);
  assert.equal(inventory[0].standardBody, true);
  assert.equal(inventory[0].standardFooter, true);
  assert.equal(inventory[0].composite, 'app-form-builder');
});

test('a composite marker without a registry entry fails closed', () => {
  const templates = new Map([['unknown.html', '<dialog open aria-modal="true" class="modal-backdrop-overlay"><div class="modal-container-card"><app-unknown data-modal-composite-shell="true"></app-unknown></div></dialog>']]);
  const { inventory, failures } = validateModalTemplates(templates, { composites: [] });

  assert.equal(inventory[0].standardHeader, false);
  assert.ok(failures.some(message => message.includes('composite sin contrato registrado')));
});

test('a normal inline institutional header passes without a composite registry', () => {
  const { inventory, failures } = validateModalTemplates(new Map([['direct.html', directModal]]), { composites: [] });
  assert.deepEqual(failures, []);
  assert.equal(inventory[0].standardHeader, true);
});

test('a composite fails if either certified region or required body markers are missing', () => {
  const templates = compositeTemplates();
  templates.set('toolbar.html', '<header data-modal-shell-region="header"></header>');
  templates.set('statusbar.html', '<footer class="bg-white" data-modal-shell-region="footer"></footer>');
  templates.set('builder.html', '<app-toolbar></app-toolbar><div data-modal-shell-region="body"></div><app-statusbar></app-statusbar>');
  const { failures } = validateModalTemplates(templates, compositeContract());

  assert.ok(failures.some(message => message.includes('regiones body')));
  assert.ok(failures.some(message => message.includes('header certificado')));
  assert.ok(failures.some(message => message.includes('footer certificado')));
});
