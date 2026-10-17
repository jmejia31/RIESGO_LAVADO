'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { analyzeSource } = require('./validate_no_parallel_confirmations');

test('rejects direct SweetAlert confirmation calls outside institutional adapter', () => {
  const result = analyzeSource("import Swal from 'sweetalert2'; Swal.fire({ showCancelButton: true });");
  assert.equal(result[0]?.classification, 'LEGACY_DIRECT_CONFIRMATION');
});

test('rejects deny-button decisions and imported aliases', () => {
  const result = analyzeSource("import SweetAlert from 'sweetalert2'; SweetAlert.fire({ showDenyButton: true });");
  assert.equal(result[0]?.classification, 'LEGACY_DIRECT_CONFIRMATION');
});

test('allows the institutional confirmation adapter implementation', () => {
  const result = analyzeSource("import Swal from 'sweetalert2'; Swal.fire({ showCancelButton: true });", 'shared/confirmation/institutional-confirmation.service.ts');
  assert.deepEqual(result, []);
});

test('classifies input dialogs separately and allows informational notifications', () => {
  const input = analyzeSource("import Swal from 'sweetalert2'; Swal.fire({ input: 'email', showCancelButton: true });");
  const success = analyzeSource("import Swal from 'sweetalert2'; Swal.fire({ title: 'Saved', icon: 'success', timer: 800 });");
  const toast = analyzeSource("import Swal from 'sweetalert2'; Swal.fire({ toast: true, icon: 'success' });");
  assert.equal(input[0]?.classification, 'INPUT_DIALOG');
  assert.equal(success[0]?.classification, 'SUCCESS_NOTIFICATION');
  assert.equal(toast[0]?.classification, 'TOAST');
});

test('fails closed when fire options are indirect or decision properties are dynamic', () => {
  const indirect = analyzeSource("import Swal from 'sweetalert2'; Swal.fire(options);");
  const dynamic = analyzeSource("import Swal from 'sweetalert2'; Swal.fire({ showCancelButton: enabled });");
  assert.equal(indirect[0]?.classification, 'UNKNOWN_CONFIRMATION');
  assert.equal(dynamic[0]?.classification, 'UNKNOWN_CONFIRMATION');
});

test('recognizes dynamic-import SweetAlert aliases used in product files', () => {
  const result = analyzeSource("import('sweetalert2').then(Swal => Swal.default.fire({ showCancelButton: true }));");
  assert.equal(result[0]?.classification, 'LEGACY_DIRECT_CONFIRMATION');
});

test('recognizes namespace imports and confirmation mixins', () => {
  const namespace = analyzeSource("import * as AlertLibrary from 'sweetalert2'; AlertLibrary.default.fire({ showCancelButton: true });");
  const mixin = analyzeSource("import Swal from 'sweetalert2'; const featureDialog = Swal.mixin({ showCancelButton: true });");
  assert.equal(namespace[0]?.classification, 'LEGACY_DIRECT_CONFIRMATION');
  assert.equal(mixin[0]?.classification, 'LEGACY_DIRECT_CONFIRMATION');
});
