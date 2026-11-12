const test = require('node:test');
const assert = require('node:assert/strict');
const { analyzeSource, validateFormulaInspectorContract } = require('./validate_b04_menus_selectors');

test('B04 inventory counts canonical selector and action menu contracts', () => {
  const source = '<select aria-label="Estado"></select><app-bounded-select></app-bounded-select><app-page-size-selector></app-page-size-selector><app-institutional-action-menu></app-institutional-action-menu>';
  const inventory = analyzeSourceFromSource(source);
  assert.equal(inventory.totalSelectors, 3);
  assert.equal(inventory.totalActionMenus, 1);
  assert.equal(inventory.unreviewedComponents, 0);
});

test('B04 formula inspector uses the bounded selector and contains long expressions', () => {
  assert.deepEqual(validateFormulaInspectorContract(process.cwd()), []);
});

function analyzeSourceFromSource(source) {
  const fs = require('node:fs');
  const os = require('node:os');
  const path = require('node:path');
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'b04-'));
  const file = path.join(root, 'fixture.html');
  try {
    fs.writeFileSync(file, source);
    return analyzeSource([file], root);
  } finally {
    fs.rmSync(root, { recursive: true, force: true });
  }
}
