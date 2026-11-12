const fs = require('node:fs');
const path = require('node:path');

const DEFAULT_ROOT = path.resolve(__dirname, '..');
const SOURCE_ROOT = path.join(DEFAULT_ROOT, 'frontend', 'rl-app', 'src', 'app');

function walkFiles(root) {
  const files = [];
  if (!fs.existsSync(root)) return files;
  for (const entry of fs.readdirSync(root, { withFileTypes: true })) {
    const fullPath = path.join(root, entry.name);
    if (entry.isDirectory()) files.push(...walkFiles(fullPath));
    else if (/\.(html|ts)$/.test(entry.name) && !/\.spec\.ts$/.test(entry.name)) files.push(fullPath);
  }
  return files;
}

function analyzeSource(files, root = SOURCE_ROOT) {
  const inventory = {
    totalSelectors: 0,
    totalActionMenus: 0,
    totalContextMenus: 0,
    totalBoundedSelects: 0,
    totalPageSizeSelectors: 0,
    totalNativeSelects: 0,
    totalReviewed: 0,
    unreviewedComponents: 0,
    nativeSelectFiles: [],
    detailFiles: [],
    actionMenuFiles: []
  };

  for (const file of files) {
    const source = fs.readFileSync(file, 'utf8');
    const relative = path.relative(root, file).replaceAll(path.sep, '/');
    const nativeSelects = (source.match(/<select\b/g) ?? []).length;
    const boundedSelects = (source.match(/<app-bounded-select\b/g) ?? []).length;
    const pageSizeSelectors = (source.match(/<app-page-size-selector\b/g) ?? []).length;
    const actionMenus = (source.match(/<app-institutional-action-menu\b/g) ?? []).length;
    const details = (source.match(/<(?:details|summary)\b/g) ?? []).length;
    const contextMenus = (source.match(/context-menu|menuSeccionAbiertaId/g) ?? []).length;

    inventory.totalNativeSelects += nativeSelects;
    inventory.totalBoundedSelects += boundedSelects;
    inventory.totalPageSizeSelectors += pageSizeSelectors;
    inventory.totalActionMenus += actionMenus;
    inventory.totalContextMenus += contextMenus;
    inventory.totalSelectors += nativeSelects + boundedSelects + pageSizeSelectors;
    inventory.totalReviewed += nativeSelects + boundedSelects + pageSizeSelectors + actionMenus;
    if (nativeSelects > 0) inventory.nativeSelectFiles.push({ file: relative, count: nativeSelects });
    if (details > 0) inventory.detailFiles.push({ file: relative, count: details });
    if (actionMenus > 0) inventory.actionMenuFiles.push({ file: relative, count: actionMenus });
  }

  inventory.unreviewedComponents = inventory.detailFiles.length;
  return inventory;
}

function validateSharedContracts(root = DEFAULT_ROOT) {
  const bounded = fs.readFileSync(path.join(root, 'frontend', 'rl-app', 'src', 'app', 'shared', 'components', 'bounded-select', 'bounded-select.component.ts'), 'utf8');
  const action = fs.readFileSync(path.join(root, 'frontend', 'rl-app', 'src', 'app', 'shared', 'components', 'institutional-action-menu', 'institutional-action-menu.component.ts'), 'utf8');
  const positioning = fs.readFileSync(path.join(root, 'frontend', 'rl-app', 'src', 'app', 'shared', 'components', 'institutional-overlay', 'institutional-overlay-position.util.ts'), 'utf8');
  const errors = [];
  const requiredBounded = ['role="combobox"', 'aria-haspopup="listbox"', 'document:click', 'document:keydown.escape', 'data-ui-bounded-select-search', 'overflow-y: auto'];
  const requiredAction = ['role="menu"', 'document:click', 'document:keydown.escape', 'InstitutionalOverlayRegistry', 'data-ui-action-menu-panel'];
  for (const token of requiredBounded) if (!bounded.includes(token)) errors.push(`BOUNDED_SELECT_CONTRACT_MISSING:${token}`);
  for (const token of requiredAction) if (!action.includes(token)) errors.push(`ACTION_MENU_CONTRACT_MISSING:${token}`);
  for (const token of ['calculateInstitutionalOverlayPlacement', 'maxHeight', 'direction']) if (!positioning.includes(token)) errors.push(`POSITIONING_CONTRACT_MISSING:${token}`);
  return errors;
}

function validate(root = DEFAULT_ROOT) {
  const inventory = analyzeSource(walkFiles(SOURCE_ROOT));
  const errors = validateSharedContracts(root);
  if (inventory.detailFiles.length > 0) errors.push(`UNSTANDARDIZED_DETAILS_OR_SUMMARY:${inventory.detailFiles.map(item => item.file).join(',')}`);
  return { pass: errors.length === 0, errors, inventory };
}

function printResult(result) {
  const { inventory } = result;
  console.log(`AUDIT_1_STATIC_INVENTORY=${result.pass ? 'PASS' : 'FAIL'}`);
  console.log(`TOTAL_SELECTORS=${inventory.totalSelectors}`);
  console.log(`TOTAL_ACTION_MENUS=${inventory.totalActionMenus}`);
  console.log(`TOTAL_CONTEXT_MENUS=${inventory.totalContextMenus}`);
  console.log(`TOTAL_BOUNDED_SELECTS=${inventory.totalBoundedSelects}`);
  console.log(`TOTAL_PAGE_SIZE_SELECTORS=${inventory.totalPageSizeSelectors}`);
  console.log(`TOTAL_NATIVE_SELECTS=${inventory.totalNativeSelects}`);
  console.log(`TOTAL_REVIEWED=${inventory.totalReviewed}`);
  console.log(`UNREVIEWED_COMPONENTS=${inventory.unreviewedComponents}`);
  for (const error of result.errors) console.error(error);
}

function selfTest() {
  const temporaryRoot = path.join(__dirname, '.b04-validator-self-test');
  fs.mkdirSync(path.join(temporaryRoot, 'frontend', 'rl-app', 'src', 'app'), { recursive: true });
  try {
    const file = path.join(temporaryRoot, 'frontend', 'rl-app', 'src', 'app', 'fixture.html');
    fs.writeFileSync(file, '<select aria-label="Estado"></select><app-bounded-select></app-bounded-select><app-page-size-selector></app-page-size-selector><app-institutional-action-menu></app-institutional-action-menu>');
    const result = analyzeSource([file], path.join(temporaryRoot, 'frontend', 'rl-app', 'src', 'app'));
    if (result.totalSelectors !== 3 || result.totalActionMenus !== 1) throw new Error('B04_VALIDATOR_SELF_TEST_FAILED');
    console.log('B04_VALIDATOR_SELF_TEST=PASS');
  } finally {
    fs.rmSync(temporaryRoot, { recursive: true, force: true });
  }
}

if (require.main === module) {
  if (process.argv.includes('--self-test')) selfTest();
  const result = validate();
  if (process.argv.includes('--json')) console.log(JSON.stringify(result, null, 2));
  else printResult(result);
  process.exitCode = result.pass ? 0 : 1;
}

module.exports = { analyzeSource, validateSharedContracts, validate, selfTest };
