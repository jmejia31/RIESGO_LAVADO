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

function countVisibleNativeSelects(source) {
  const tags = source.match(/<select\b[^>]*>/g) ?? [];
  const hidden = tags.filter(tag => /\bsr-only\b/.test(tag) || /aria-hidden\s*=\s*["']true["']/.test(tag)).length;
  return { total: tags.length, visible: tags.length - hidden, hidden };
}

function analyzeSource(files, root = SOURCE_ROOT) {
  const inventory = {
    totalSelectors: 0,
    totalActionMenus: 0,
    totalContextMenus: 0,
    totalBoundedSelects: 0,
    totalPageSizeSelectors: 0,
    totalNativeSelects: 0,
    totalVisibleNativeSelects: 0,
    totalHiddenSyncSelects: 0,
    totalReviewed: 0,
    unreviewedComponents: 0,
    nativeSelectFiles: [],
    detailFiles: [],
    actionMenuFiles: [],
    familySurfaceFiles: [],
    tooltipStandardFiles: [],
    tooltipButtonFiles: [],
    totalTooltipButtons: 0
  };

  for (const file of files) {
    const source = fs.readFileSync(file, 'utf8');
    const relative = path.relative(root, file).replaceAll(path.sep, '/');
    const nativeSelectCounts = countVisibleNativeSelects(source);
    const nativeSelects = nativeSelectCounts.total;
    const boundedSelects = (source.match(/<app-bounded-select\b/g) ?? []).length;
    const pageSizeSelectors = (source.match(/<app-page-size-selector\b/g) ?? []).length;
    const actionMenus = (source.match(/<app-institutional-action-menu\b/g) ?? []).length;
    const titledButtons = (source.match(/<button\b[^>]*\btitle\s*=/g) ?? []).length;
    const details = (source.match(/<(?:details|summary)\b/g) ?? []).length;
    const contextMenus = (source.match(/context-menu|menuSeccionAbiertaId/g) ?? []).length;

    inventory.totalNativeSelects += nativeSelects;
    inventory.totalVisibleNativeSelects += nativeSelectCounts.visible;
    inventory.totalHiddenSyncSelects += nativeSelectCounts.hidden;
    inventory.totalBoundedSelects += boundedSelects;
    inventory.totalPageSizeSelectors += pageSizeSelectors;
    inventory.totalActionMenus += actionMenus;
    inventory.totalContextMenus += contextMenus;
    inventory.totalSelectors += nativeSelectCounts.visible + boundedSelects + pageSizeSelectors;
    inventory.totalReviewed += nativeSelectCounts.visible + boundedSelects + pageSizeSelectors + actionMenus;
    inventory.totalTooltipButtons += titledButtons;
    if (nativeSelects > 0) inventory.nativeSelectFiles.push({ file: relative, count: nativeSelects });
    if (details > 0) inventory.detailFiles.push({ file: relative, count: details });
    if (actionMenus > 0) inventory.actionMenuFiles.push({ file: relative, count: actionMenus });
    if (titledButtons > 0) inventory.tooltipButtonFiles.push({ file: relative, count: titledButtons });
    if (/matrices-riesgos\/(?:components\/(?:familia-(?:crear|detalle|editar)-modal|form-builder)|pages\/matrices-riesgos)/.test(relative)) {
      inventory.familySurfaceFiles.push(relative);
      if (source.includes('InstitutionalTooltipDirective')) inventory.tooltipStandardFiles.push(relative);
    }
  }

  inventory.unreviewedComponents = inventory.detailFiles.length;
  return inventory;
}

function validateSharedContracts(root = DEFAULT_ROOT) {
  const bounded = fs.readFileSync(path.join(root, 'frontend', 'rl-app', 'src', 'app', 'shared', 'components', 'bounded-select', 'bounded-select.component.ts'), 'utf8');
  const action = fs.readFileSync(path.join(root, 'frontend', 'rl-app', 'src', 'app', 'shared', 'components', 'institutional-action-menu', 'institutional-action-menu.component.ts'), 'utf8');
  const positioning = fs.readFileSync(path.join(root, 'frontend', 'rl-app', 'src', 'app', 'shared', 'components', 'institutional-overlay', 'institutional-overlay-position.util.ts'), 'utf8');
  const tooltip = fs.readFileSync(path.join(root, 'frontend', 'rl-app', 'src', 'app', 'shared', 'components', 'institutional-tooltip', 'institutional-tooltip.directive.ts'), 'utf8');
  const errors = [];
  const requiredBounded = ['role="combobox"', 'aria-haspopup="listbox"', 'document:click', 'document:keydown.escape', 'data-ui-bounded-select-search', 'overflow-y: auto'];
  const requiredAction = ['role="menu"', 'document:click', 'document:keydown.escape', 'InstitutionalOverlayRegistry', 'data-ui-action-menu-panel'];
  for (const token of requiredBounded) if (!bounded.includes(token)) errors.push(`BOUNDED_SELECT_CONTRACT_MISSING:${token}`);
  for (const token of requiredAction) if (!action.includes(token)) errors.push(`ACTION_MENU_CONTRACT_MISSING:${token}`);
  for (const token of ['calculateInstitutionalOverlayPlacement', 'maxHeight', 'direction']) if (!positioning.includes(token)) errors.push(`POSITIONING_CONTRACT_MISSING:${token}`);
  for (const token of ['selector: \'button[title]\'', 'role', 'aria-describedby', 'getBoundingClientRect', 'window:resize', 'window:scroll']) if (!tooltip.includes(token)) errors.push(`TOOLTIP_CONTRACT_MISSING:${token}`);
  return errors;
}

function validateFormulaInspectorContract(root = DEFAULT_ROOT) {
  const inspectorRoot = path.join(root, 'frontend', 'rl-app', 'src', 'app', 'features', 'admin', 'matrices-riesgos', 'components', 'form-builder', 'inspector');
  const htmlPath = path.join(inspectorRoot, 'form-builder-inspector.component.html');
  const tsPath = path.join(inspectorRoot, 'form-builder-inspector.component.ts');
  const scssPath = path.join(inspectorRoot, 'form-builder-inspector.component.scss');
  const errors = [];
  if (![htmlPath, tsPath, scssPath].every(fs.existsSync)) {
    return ['FORMULA_INSPECTOR_FILES_MISSING'];
  }
  const html = fs.readFileSync(htmlPath, 'utf8');
  const ts = fs.readFileSync(tsPath, 'utf8');
  const scss = fs.readFileSync(scssPath, 'utf8');
  if (!html.includes('<app-bounded-select')) errors.push('FORMULA_SELECTOR_NOT_BOUNDED');
  if (html.includes('<select [id]="\'prop-formula-\'')) errors.push('FORMULA_SELECTOR_NATIVE_LONG_LIST');
  for (const token of ['formulaVersionSelectOptions', 'valueChange', 'seleccionarFormulaCentral($event)']) {
    if (!html.includes(token)) errors.push(`FORMULA_SELECTOR_BINDING_MISSING:${token}`);
  }
  for (const token of ['formulaVersionSelectOptions', 'Number(versionId)', 'formulaVersionId']) {
    if (!ts.includes(token)) errors.push(`FORMULA_SELECTOR_ADAPTER_MISSING:${token}`);
  }
  for (const token of ['inspector-central-preview', 'overflow-wrap:anywhere', 'word-break:break-word', 'overflow-x:hidden']) {
    if (!scss.includes(token)) errors.push(`FORMULA_TEXT_CONTAINMENT_MISSING:${token}`);
  }
  return errors;
}

function validate(root = DEFAULT_ROOT) {
  const inventory = analyzeSource(walkFiles(SOURCE_ROOT));
  const errors = [...validateSharedContracts(root), ...validateFormulaInspectorContract(root)];
  const reviewManifestPath = path.join(root, 'tools', 'b04-control-review.json');
  if (!fs.existsSync(reviewManifestPath)) {
    errors.push('CONTROL_REVIEW_MANIFEST_MISSING:tools/b04-control-review.json');
  }
  const reviewManifest = fs.existsSync(reviewManifestPath)
    ? JSON.parse(fs.readFileSync(reviewManifestPath, 'utf8'))
    : { nativeSelectReviews: [] };
  const reviewedNativeFiles = new Set((reviewManifest.nativeSelectReviews ?? []).map(item => item.file));
  const nativeInventoryFiles = new Set(inventory.nativeSelectFiles.map(item => item.file));
  for (const file of nativeInventoryFiles) {
    if (!reviewedNativeFiles.has(file)) errors.push(`NATIVE_SELECT_UNREVIEWED:${file}`);
  }
  for (const file of reviewedNativeFiles) {
    if (!nativeInventoryFiles.has(file)) errors.push(`NATIVE_SELECT_REVIEW_STALE:${file}`);
  }
  for (const item of inventory.tooltipButtonFiles) {
    const componentTs = path.join(SOURCE_ROOT, item.file.replace(/\.html$/, '.ts'));
    if (!fs.existsSync(componentTs) || !fs.readFileSync(componentTs, 'utf8').includes('InstitutionalTooltipDirective')) {
      errors.push(`TOOLTIP_STANDARD_MISSING_GLOBAL:${item.file}`);
    }
  }
  const requiredFamilyComponents = [
    'features/admin/matrices-riesgos/components/familia-crear-modal/familia-crear-modal.component.ts',
    'features/admin/matrices-riesgos/components/familia-detalle-modal/familia-detalle-modal.component.ts',
    'features/admin/matrices-riesgos/components/familia-editar-modal/familia-editar-modal.component.ts',
    'features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts',
    'features/admin/matrices-riesgos/components/form-builder/form-builder.component.ts'
  ];
  for (const relative of requiredFamilyComponents) {
    const file = path.join(SOURCE_ROOT, relative);
    if (!fs.existsSync(file)) errors.push(`FAMILY_SURFACE_MISSING:${relative}`);
    else if (!fs.readFileSync(file, 'utf8').includes('InstitutionalTooltipDirective')) errors.push(`TOOLTIP_STANDARD_MISSING:${relative}`);
  }
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
  console.log(`VISIBLE_NATIVE_SELECTS=${inventory.totalVisibleNativeSelects}`);
  console.log(`HIDDEN_SYNC_SELECTS=${inventory.totalHiddenSyncSelects}`);
  console.log(`TOTAL_REVIEWED=${inventory.totalReviewed}`);
  console.log(`UNREVIEWED_COMPONENTS=${inventory.unreviewedComponents}`);
  console.log(`TOTAL_TOOLTIP_BUTTONS=${inventory.totalTooltipButtons}`);
  console.log(`TOOLTIP_BUTTON_FILES=${inventory.tooltipButtonFiles.length}`);
  console.log(`NATIVE_SELECT_REVIEWS=${inventory.nativeSelectFiles.length}`);
  console.log(`FAMILY_SURFACE_FILES=${inventory.familySurfaceFiles.length}`);
  console.log(`TOOLTIP_STANDARD_FILES=${inventory.tooltipStandardFiles.length}`);
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

module.exports = { analyzeSource, validateSharedContracts, validateFormulaInspectorContract, validate, selfTest };
