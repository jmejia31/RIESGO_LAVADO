import { expect, Page, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

const definicion = JSON.stringify({
  codigoFormulario: 'MATRIZ_RIESGOS_LAFT_V2', nombreFormulario: 'Matriz UAT',
  secciones: [{ clave: 'identificacion', titulo: 'Identificación', orden: 1, columnasPorFila: 2,
    campos: [
      { clave: 'area_responsable', etiqueta: 'Área responsable', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 },
      { clave: 'dueno_riesgo', etiqueta: 'Dueño del riesgo', tipo: 'selector-catalogo', codigoCatalogo: 'CAT_DUENO', obligatorio: true, soloLectura: false, anchoColumnas: 1 },
      { clave: 'resultado_formula', etiqueta: 'Resultado calculado', tipo: 'formula', formula: 'NO_EJECUTAR(a + b)', obligatorio: false, soloLectura: true, anchoColumnas: 2 }
    ] }],
  catalogos: [{ codigo: 'CAT_DUENO', nombre: 'Dueños de riesgo', elementos: [{ codigo: '01', valor: 'Dirección General', orden: 1 }, { codigo: '02', valor: 'Cumplimiento', orden: 2 }] }]
});
const publicada = { verId: 10, verFamiliaId: 1, verCodigo: 'MATRIZ_RIESGOS_LAFT_V1', verVersion: 1,
  verJson: '{"secciones":[]}', verHash: 'vigente', verEstado: 'PUBLISHED', verVigente: true,
  verFechaCreacion: '2026-08-13T12:00:00Z', verUsrCreacion: 1 };
const borrador = { ...publicada, verId: 11, verCodigo: 'MATRIZ_RIESGOS_LAFT_V2', verVersion: 2,
  verJson: definicion, verHash: 'borrador', verEstado: 'DRAFT', verVigente: false };

function token(): string {
  const codificar = (valor: object) => Buffer.from(JSON.stringify(valor)).toString('base64url');
  return `${codificar({ alg: 'none' })}.${codificar({ nameid: '1', uid: 'admin.modal', role: 'ADMINISTRADOR', rol_id: '1', modulos: '10', debe_cambiar_pass: '0', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
}

async function preparar(page: Page): Promise<void> {
  await page.addInitScript(jwt => {
    localStorage.setItem('access_token', jwt);
    localStorage.setItem('refresh_token', 'modal-refresh');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, token());
  await page.route('**/api/configuracion/sistema', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: { nombreSistema: 'SGRLA-IHSS', nombreInstitucion: 'IHSS', timeoutSesion: 30 } }) }));
  await page.route('**/api/configuracion/login', route => route.fulfill({ status: 200, contentType: 'application/json', body: '{"success":true,"datos":[]}' }));
  await page.route('**/api/matrices-riesgos/**', route => {
    const ruta = new URL(route.request().url()).pathname;
    let datos: unknown = [];
    if (ruta.endsWith('/familias')) datos = [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz UAT', famDescripcion: 'Familia E2E del shell modal.', famActivo: true, famFechaCreacion: '2026-08-13T12:00:00Z', totalVersiones: 2, tieneVersionVigente: true }];
    else if (ruta.endsWith('/familias/1')) datos = { famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz UAT', famDescripcion: 'Familia E2E del shell modal.', famActivo: true, famFechaCreacion: '2026-08-13T12:00:00Z', totalVersiones: 2, tieneVersionVigente: true };
    else if (ruta.endsWith('/formulario/version-vigente')) datos = publicada;
    else if (ruta.endsWith('/metodologia/vigente')) datos = { versionFormularioId: 10, codigo: publicada.verCodigo, version: 1, secciones: [], catalogos: [], reglas: [] };
    else if (ruta.endsWith('/formularios/historial')) datos = [publicada, borrador];
    else if (/\/formularios\/\d+$/.test(ruta)) datos = borrador;
    else if (ruta.endsWith('/evaluaciones')) datos = { items: [], pagina: 1, registrosPorPagina: 10, totalRegistros: 0, totalPaginas: 0 };
    else if (ruta.endsWith('/riesgos')) datos = [];
    else if (ruta.endsWith('/consolidado')) datos = [];
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });
  await page.route('**/api/matrices-riesgos*', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) }));
}

test.beforeEach(async ({ page }) => preparar(page));

test('bloquea el shell y conserva el foco dentro del Form Builder modal', async ({ page }) => {
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  const editar = detalleFamilia.getByRole('button', { name: 'Editar definición de la versión' }).first();
  await editar.focus();
  await editar.click();
  await page.getByRole('button', { name: 'Editar definición', exact: true }).click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const tarjetaBuilder = dialogo.locator('.modal-container-card.modal-size-workspace');
  const header = page.locator('app-main-layout > div > div > header');
  const aside = page.locator('app-main-layout > div > aside');
  const salir = header.locator('button[aria-label="Cerrar sesión"]');
  await expect(dialogo).toBeVisible();
  await expect(tarjetaBuilder).toBeVisible();

  const viewport = page.viewportSize();
  const cajaBuilder = await tarjetaBuilder.boundingBox();
  expect(viewport).not.toBeNull();
  expect(cajaBuilder).not.toBeNull();
  if (!viewport || !cajaBuilder) throw new Error('No se pudo medir el contrato visual del Form Builder.');
  expect(cajaBuilder.width / viewport.width).toBeGreaterThanOrEqual(0.95);
  expect(cajaBuilder.width / viewport.width).toBeLessThanOrEqual(0.97);
  expect(cajaBuilder.height / viewport.height).toBeGreaterThanOrEqual(0.93);
  expect(cajaBuilder.height / viewport.height).toBeLessThanOrEqual(0.95);
  const estilosBuilder = await tarjetaBuilder.evaluate(el => {
    const css = getComputedStyle(el);
    return { backgroundColor: css.backgroundColor, borderRadius: css.borderRadius, boxShadow: css.boxShadow };
  });
  expect(estilosBuilder.backgroundColor).toBe('rgb(255, 255, 255)');
  expect(Number.parseFloat(estilosBuilder.borderRadius)).toBeGreaterThanOrEqual(16);
  expect(estilosBuilder.boxShadow).not.toBe('none');

  await expect.poll(() => header.evaluate(el => (el as HTMLElement).inert)).toBe(true);
  await expect.poll(() => aside.evaluate(el => (el as HTMLElement).inert)).toBe(true);

  const caja = await salir.boundingBox();
  expect(caja).not.toBeNull();
  if (!caja) throw new Error('No se pudo localizar físicamente el botón Salir.');
  await page.mouse.move(caja.x + caja.width / 2, caja.y + caja.height / 2);
  await expect.poll(() => salir.evaluate(el => el.matches(':hover'))).toBe(false);
  await page.mouse.click(caja.x + caja.width / 2, caja.y + caja.height / 2);
  await expect(page).toHaveURL(/\/matrices-riesgos$/);
  expect(await page.evaluate(() => localStorage.getItem('access_token'))).toBeTruthy();

  for (let i = 0; i < 5; i++) {
    await page.keyboard.press('Tab');
    expect(await page.evaluate(() => document.querySelector('dialog[open][aria-modal="true"]')?.contains(document.activeElement))).toBe(true);
  }

  for (let i = 0; i < 5; i++) {
    await page.keyboard.press('Shift+Tab');
    expect(await page.evaluate(() => document.querySelector('dialog[open][aria-modal="true"]')?.contains(document.activeElement))).toBe(true);
  }

  const primerCampoCard = dialogo.getByRole('main', { name: 'Lienzo del formulario' }).locator('article').first();
  await primerCampoCard.click();
  const inspector = dialogo.getByRole('complementary', { name: 'Inspector de propiedades' });
  const clave = inspector.getByRole('textbox', { name: 'Clave Técnica' });
  await expect(clave).toBeEnabled();
  await clave.fill('area_responsable_actualizada');
  await expect(clave).toHaveValue('area_responsable_actualizada');

  await dialogo.getByRole('button', { name: 'Cerrar modal de constructor' }).click();
  await expect(dialogo).toBeHidden();
  await expect.poll(() => header.evaluate(el => (el as HTMLElement).inert)).toBe(false);
  await expect.poll(() => aside.evaluate(el => (el as HTMLElement).inert)).toBe(false);
});

test('captura el estado editable del constructor a 1536x1024', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  await detalleFamilia.getByRole('button', { name: 'Editar definición de la versión' }).first().click();
  await page.getByRole('button', { name: 'Editar definición', exact: true }).click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  await expect(dialogo).toBeVisible();
  await expect(dialogo).toContainText('BORRADOR');
  await expect(dialogo.locator('#btn-guardar-builder')).toBeVisible();
  await expect(dialogo.locator('#btn-publicar-builder')).toBeVisible();
  await expect(dialogo.locator('#tab-editor-visual')).toBeVisible();
  await expect(dialogo.locator('#tab-vista-preview')).toBeVisible();
  await expect(dialogo.locator('#tab-configuracion-general')).toHaveCount(0);
  await expect(dialogo.locator('#tab-editor-visual')).toHaveAttribute('aria-current', 'page');
  await expect(dialogo.locator('#tab-editor-visual')).toHaveAttribute('aria-current', 'page');
  await expect(dialogo.locator('#tab-vista-preview')).not.toHaveAttribute('aria-current', 'page');
  await expect(dialogo.getByText('Acciones', { exact: false })).toBeVisible();
  await expect(dialogo.locator('[data-form-builder-region="statusbar"] .form-builder-statusbar__cancel')).toBeVisible();
  await expect(dialogo.locator('[data-form-builder-region="statusbar"] .form-builder-statusbar__save')).toBeEnabled();
  await expect(dialogo.getByText('Modo de visualizaciÃ³n y consulta tÃ©cnica', { exact: false })).toHaveCount(0);
  await dialogo.locator('#summary-acciones-builder').click();
  await expect(dialogo.locator('#btn-agregar-seccion')).toBeVisible();
  await expect(dialogo.locator('#btn-nuevo-catalogo-header')).toHaveCount(0);
  await page.screenshot({ path: 'test-results/ui-form-final-a-actions-1536x1024.png', fullPage: true });
  await page.screenshot({ path: 'test-results/ui-form5-editable-1536x1024.png', fullPage: true });
});

test('renderiza la misma identidad visual en solo lectura y oculta acciones mutantes', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();

  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  await detalleFamilia.getByRole('button', { name: 'Ver detalle de la versión' }).last().click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  await expect(dialogo).toBeVisible();
  await expect(dialogo).toContainText('BORRADOR');
  await expect(dialogo).toContainText('SOLO LECTURA');
  await expect(dialogo.locator('#btn-guardar-builder')).toHaveCount(0);
  await expect(dialogo.locator('#btn-publicar-builder')).toHaveCount(0);
  await expect(dialogo.locator('#tab-vista-preview')).toBeVisible();
  await expect(dialogo.locator('#tab-configuracion-general')).toHaveCount(0);
  await expect(dialogo.locator('[data-form-builder-region="statusbar"] .form-builder-statusbar__cancel')).toBeVisible();
  await expect(dialogo.locator('[data-form-builder-region="statusbar"] .form-builder-statusbar__save')).toBeDisabled();
  await page.screenshot({ path: 'test-results/ui-form5-readonly-1536x1024.png', fullPage: true });
});

test('captura Vista Previa integrada sin herramientas de edición', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await detalleFamilia.getByRole('button', { name: 'Editar definición de la versión' }).first().click();
  await page.getByRole('button', { name: 'Editar definición', exact: true }).click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  await expect(dialogo).toBeVisible();
  await dialogo.getByRole('button', { name: 'Vista Previa' }).click();
  await expect(dialogo.locator('#tab-vista-preview')).toHaveAttribute('aria-current', 'page');
  await expect(dialogo.locator('#tab-editor-visual')).not.toHaveAttribute('aria-current', 'page');
  const preview = dialogo.locator('[aria-label="Vista previa del formulario"]');
  await expect(preview).toBeVisible();
  await expect(preview.locator('app-dynamic-field-renderer')).toHaveCount(3);
  await expect(preview.locator('app-form-builder-palette')).toHaveCount(0);
  await expect(preview.locator('[data-preview-field="area_responsable"] input')).toBeVisible();
  await expect(preview.locator('[data-preview-field="dueno_riesgo"] select')).toBeVisible();
  await expect(preview.locator('[data-preview-field="resultado_formula"] [aria-readonly="true"]')).toBeVisible();
  await expect(preview).toContainText('Dirección General');
  await expect(preview).toContainText('NO_EJECUTAR(a + b)');
  await expect(dialogo.getByRole('main', { name: 'Lienzo del formulario' })).toHaveCount(0);
  await page.screenshot({ path: 'test-results/ui-form6-preview-1536x1024.png', fullPage: true });
});

test('captura JSON Técnico con búsqueda, validación y sincronización separadas', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await detalleFamilia.getByRole('button', { name: 'Editar definición de la versión' }).first().click();
  await page.getByRole('button', { name: 'Editar definición', exact: true }).click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  await expect(dialogo).toBeVisible();
  await dialogo.getByRole('button', { name: 'Ver JSON Técnico' }).click();
  const json = dialogo.locator('#json-avanzado');
  await expect(json).toBeVisible();
  await expect(dialogo.getByRole('button', { name: 'Copiar JSON' })).toBeVisible();
  await expect(dialogo.getByRole('searchbox', { name: 'Buscar en JSON técnico' })).toBeVisible();
  await expect(dialogo.getByRole('button', { name: 'Validar' })).toBeVisible();
  await expect(dialogo.getByRole('button', { name: 'Sincronizar hacia el Lienzo Visual' })).toBeVisible();
  await dialogo.getByRole('searchbox', { name: 'Buscar en JSON técnico' }).fill('area_responsable');
  await expect(dialogo).toContainText('1 coincidencia(s)');
  await dialogo.getByRole('button', { name: 'Validar' }).click();
  await expect(dialogo).toContainText('JSON válido y estructura compatible.');
  await page.screenshot({ path: 'test-results/ui-form6-json-1536x1024.png', fullPage: true });
});
