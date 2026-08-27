import { chromium } from '../../frontend/rl-app/node_modules/playwright/index.mjs';

const endpoint = process.env.UAT_CDP_ENDPOINT ?? 'http://127.0.0.1:54257';
const browser = await chromium.connectOverCDP(endpoint);
const context = browser.contexts()[0];
const page = context.pages().find(candidate => !candidate.isClosed());

if (!context || !page) throw new Error('No reusable UAT context/page found.');
await page.bringToFront();
page.setDefaultTimeout(8_000);
console.log('UAT_SCRIPT=START');
await page.goto('http://localhost:4200/matrices-riesgos', { waitUntil: 'domcontentloaded' });
await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => undefined);

const wait = ms => new Promise(resolve => setTimeout(resolve, ms));

async function modalState() {
  return page.evaluate(() => {
    const isVisible = element => {
      const rect = element.getBoundingClientRect();
      const style = getComputedStyle(element);
      return rect.width > 0 && rect.height > 0 && style.display !== 'none' && style.visibility !== 'hidden';
    };
    const modalCandidates = [...document.querySelectorAll('dialog, [role="dialog"], [data-app-modal="true"]')].filter(isVisible);
    const backdrops = [...document.querySelectorAll('[class*="backdrop"], [class*="bg-black"]')].filter(isVisible);
    return {
      visibleModalCount: modalCandidates.length,
      backdropCount: backdrops.length,
      bodyOverflow: getComputedStyle(document.body).overflow,
      scrollY: Math.round(window.scrollY),
      activeElement: document.activeElement?.getAttribute('aria-label') ?? document.activeElement?.id ?? document.activeElement?.tagName ?? null,
    };
  });
}

async function tabState(tab) {
  const tabId = tab.toLowerCase();
  await page.locator(`#tab-${tabId}`).click();
  await wait(450);
  const state = await page.evaluate(tabName => {
    const panel = document.querySelector(`#panel-${tabName}`);
    const visibleElement = element => {
      const rect = element.getBoundingClientRect();
      const style = getComputedStyle(element);
      return rect.width > 0 && rect.height > 0 && style.display !== 'none' && style.visibility !== 'hidden';
    };
    const controls = panel ? [...panel.querySelectorAll('input, select, button')].filter(visibleElement) : [];
    const ids = controls.map(element => element.id).filter(Boolean);
    const filterIds = tabName === 'evaluaciones'
      ? ['filtro-buscar', 'filtro-estado', 'filtro-registros-por-pagina']
      : tabName === 'consolidado'
        ? ['consolidado-buscar', 'consolidado-estado', 'consolidado-por-pagina']
        : ['gestor-buscar-familia', 'gestor-estado-familia', 'gestor-vigencia-familia', 'familias-por-pagina-superior'];
    const filterControls = controls.filter(element => filterIds.includes(element.id));
    const filterTops = [...new Set(filterControls.map(element => Math.round(element.getBoundingClientRect().top)))];
    const panelWidth = panel?.clientWidth ?? 0;
    return {
      filterControlIds: filterControls.map(element => element.id),
      visibleFilterBlockCount: filterControls.length > 0 ? 1 : 0,
      filterTops,
      widths: filterControls.map(element => Math.round(element.getBoundingClientRect().width)),
      heights: filterControls.map(element => Math.round(element.getBoundingClientRect().height)),
      bottomControls: controls.filter(element => ['familias-por-pagina', 'filtro-registros-por-pagina'].includes(element.id) || /Anterior|Siguiente/.test(element.textContent ?? '')).map(element => element.id || element.textContent.trim()),
      horizontalOverflow: (panel?.scrollWidth ?? 0) > panelWidth + 1,
      kpiText: document.querySelector('[data-ui-kpis-context]')?.textContent?.replace(/\s+/g, ' ').trim().slice(0, 130) ?? '',
      crossControls: [...document.querySelectorAll('input, select, button')].filter(visibleElement).map(element => element.id).filter(id => id && !filterIds.includes(id) && /^filtro-|^consolidado-|^gestor-/.test(id)),
    };
  }, tabId);
  console.log(`TAB=${tab} ${JSON.stringify(state)}`);
  return state;
}

for (let cycle = 1; cycle <= 5; cycle += 1) {
  for (const tab of ['Evaluaciones', 'Consolidado', 'Plantillas']) {
    const state = await tabState(tab);
    console.log(`TAB_CYCLE=${cycle} TAB=${tab} FILTER_BLOCKS=${state.visibleFilterBlockCount} OVERFLOW=${state.horizontalOverflow ? 'YES' : 'NO'}`);
  }
}

await page.locator('#tab-evaluaciones').click();
await page.getByRole('button', { name: 'Nueva evaluación', exact: true }).click();
await wait(400);
const emptyNew = await page.evaluate(() => ({
  family: document.querySelector('#modal-selector-familia')?.value ?? null,
  form: document.querySelectorAll('[data-evaluation-section]').length,
  eyeDisabled: document.querySelector('button[aria-label="Ver detalle de familia"]')?.hasAttribute('disabled') ?? false,
  createDisabled: [...document.querySelectorAll('button')].find(button => button.textContent?.includes('Crear Evaluación'))?.hasAttribute('disabled') ?? true,
  metadata: document.querySelector('#titulo-modal-nueva-eval')?.parentElement?.textContent?.includes('Familia:') ?? false,
}));
console.log(`NEW_EVALUATION_EMPTY=${JSON.stringify(emptyNew)}`);
const familySelect = page.locator('#modal-selector-familia:visible');
const familyOptions = await familySelect.locator('option').evaluateAll(options => options.map(option => ({ value: option.value, text: option.textContent?.trim() ?? '' })).filter(option => option.value));
if (familyOptions.length === 0) throw new Error('No real family options available.');
await familySelect.selectOption(familyOptions[0].value);
await page.waitForFunction(() => Boolean(document.querySelector('#modal-selector-familia')?.value) && document.querySelectorAll('[data-evaluation-section]').length > 0, null, { timeout: 8_000 });
const selectedNew = await page.evaluate(() => ({
  family: document.querySelector('#modal-selector-familia')?.value ?? null,
  form: document.querySelectorAll('[data-evaluation-section]').length,
  eyeDisabled: document.querySelector('button[aria-label="Ver detalle de familia"]')?.hasAttribute('disabled') ?? true,
  metadata: document.querySelector('#titulo-modal-nueva-eval')?.parentElement?.textContent?.replace(/\s+/g, ' ').trim() ?? '',
}));
console.log(`NEW_EVALUATION_SELECTED=${JSON.stringify(selectedNew)}`);
if (familyOptions.length > 1) {
  let familiaBValida = null;
  for (const candidate of familyOptions.slice(1)) {
    await familySelect.selectOption(candidate.value);
    await page.waitForFunction(() => document.querySelectorAll('[data-evaluation-section]').length > 0 || Boolean([...document.querySelectorAll('[role="status"]')].find(element => /no dispone de una versión activa/i.test(element.textContent ?? ''))), null, { timeout: 8_000 });
    const estadoB = await page.evaluate(() => ({ family: document.querySelector('#modal-selector-familia')?.value ?? null, formCount: document.querySelectorAll('[data-evaluation-section]').length, noActive: [...document.querySelectorAll('[role="status"]')].some(element => /no dispone de una versión activa/i.test(element.textContent ?? '')) }));
    console.log(`NEW_EVALUATION_FAMILY_CANDIDATE=${JSON.stringify(estadoB)}`);
    if (estadoB.formCount > 0) { familiaBValida = candidate.value; break; }
  }
  if (familiaBValida) console.log(`NEW_EVALUATION_FAMILY_CHANGE=${JSON.stringify(await page.evaluate(() => ({ family: document.querySelector('#modal-selector-familia')?.value ?? null, formCount: document.querySelectorAll('[data-evaluation-section]').length })))}`);
  await familySelect.selectOption(familyOptions[0].value);
  await page.waitForFunction(() => document.querySelectorAll('[data-evaluation-section]').length > 0, null, { timeout: 8_000 });
}
for (let cycle = 1; cycle <= 5; cycle += 1) {
  await page.locator('button[aria-label="Ver detalle de familia"]:visible').click();
  await page.waitForSelector('[data-ui-fam-detail="modal"]:visible', { timeout: 8_000 });
  const familyDetailVisible = await page.locator('[data-ui-fam-detail="modal"]:visible').count();
  const familyDetailTitle = await page.locator('[data-ui-fam-detail="modal"] h3#titulo-detalle-familia:visible').count();
  await page.locator('[data-ui-fam-detail="modal"] [data-ui-fam-detail-action="regresar"]:visible').click();
  await page.waitForSelector('#modal-selector-familia:visible', { timeout: 8_000 });
  console.log(`NEW_EVALUATION_DETAIL_RETURN_CYCLE=${cycle} ${JSON.stringify({ visible: familyDetailVisible, correctTitle: familyDetailTitle === 1, returned: await page.locator('[data-modal="nueva-evaluacion"]:visible').count() === 1 })}`);
}
await familySelect.selectOption('');
await page.waitForFunction(() => !document.querySelector('#modal-selector-familia')?.value && document.querySelectorAll('[data-evaluation-section]').length === 0, null, { timeout: 2_000 });
console.log(`NEW_EVALUATION_CLEARED=${JSON.stringify(await page.evaluate(() => ({ form: document.querySelectorAll('[data-evaluation-section]').length, eyeDisabled: document.querySelector('button[aria-label="Ver detalle de familia"]')?.hasAttribute('disabled') ?? false })))} `);
await page.locator('button[aria-label="Cerrar creación"]:visible').click();

async function openDetail() {
  await page.locator('#tab-plantillas').click();
  await wait(400);
  await page.locator('button[aria-label="Ver detalle"]:visible').first().click();
  await wait(650);
  if (await page.locator('[data-ui-fam-detail="modal"]:visible').count() !== 1) throw new Error('Detail modal did not open exactly once.');
}

for (let cycle = 1; cycle <= 5; cycle += 1) {
  await openDetail();
  const before = await modalState();
  const editDefinition = page.locator('[data-ui-fam-detail="modal"] button[aria-label^="Editar definición"]:visible').first();
  await editDefinition.click();
  await wait(700);
  const toolbar = page.locator('.form-builder-toolbar:visible');
  const summary = page.locator('#summary-acciones-builder:visible');
  const builderMetrics = await page.evaluate(() => {
    const summaryElement = document.querySelector('#summary-acciones-builder');
    const columns = [...document.querySelectorAll('select[id^="seccion-columnas-"]')].filter(element => {
      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0;
    });
    return {
      summaryWidth: summaryElement ? Math.round(summaryElement.getBoundingClientRect().width) : 0,
      summaryHeight: summaryElement ? Math.round(summaryElement.getBoundingClientRect().height) : 0,
      columns: columns.map(element => ({ width: Math.round(element.getBoundingClientRect().width), height: Math.round(element.getBoundingClientRect().height), value: element.value })),
    };
  });
  await summary.click();
  await wait(150);
  const actionsOpen = await page.locator('.form-builder-toolbar__actions-popover:visible').count();
  const after = await modalState();
  console.log(`BUILDER_CYCLE=${cycle} TOOLBAR=${await toolbar.count()} ACTIONS_OPEN=${actionsOpen} METRICS=${JSON.stringify(builderMetrics)} MODAL=${JSON.stringify({ before, after })}`);
  await page.locator('button[aria-label="Cerrar modal de constructor"]:visible').click();
  await wait(500);
  console.log(`BUILDER_RETURN_DETAIL=${cycle} ${await page.locator('[data-ui-fam-detail="modal"]:visible').count()}`);
  await page.locator('[data-ui-fam-detail="modal"] button[aria-label="Cerrar detalle de familia"]:visible').click();
  await wait(400);
}

for (let cycle = 1; cycle <= 5; cycle += 1) {
  await openDetail();
  await page.locator('[data-ui-fam-detail="modal"] button[aria-label="Editar familia"]:visible').first().click();
  await wait(650);
  const before = await modalState();
  const editVisible = await page.locator('#titulo-modal-editar-familia:visible').count();
  await page.locator('[data-ui-fam-edit-action="regresar"]:visible').click();
  await wait(500);
  const returned = await page.locator('[data-ui-fam-detail="modal"]:visible').count();
  const after = await modalState();
  console.log(`EDIT_FAMILY_CYCLE=${cycle} EDIT_VISIBLE=${editVisible} RETURN_DETAIL=${returned} MODAL=${JSON.stringify({ before, after })}`);
  await page.locator('[data-ui-fam-detail="modal"] button[aria-label="Cerrar detalle de familia"]:visible').click();
  await wait(400);
}

await page.screenshot({ path: 'C:/Users/francisco.perez/AppData/Local/Temp/RIESGO_LAVADO_UAT/matrices-visual-cdp-final.png', fullPage: true });
console.log('UAT_VISUAL_CAMPAIGN=COMPLETE');
console.log('BROWSER_LEFT_OPEN=YES');
