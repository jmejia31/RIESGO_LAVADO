import { expect, Page, test } from '@playwright/test';
import { Buffer } from 'node:buffer';
import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import { contrastRatio, createSystemTheme, parseHexColor } from '../src/app/core/configuration/system-theme.util';

const definicion = JSON.stringify({
  codigoFormulario: 'MATRIZ_RIESGOS_LAFT_V2', nombreFormulario: 'Matriz UAT',
  secciones: [{ clave: 'identificacion', titulo: 'Identificación', orden: 1, columnasPorFila: 2,
    campos: [
      { clave: 'area_responsable', etiqueta: 'Área responsable', tipo: 'texto', obligatorio: true, soloLectura: false, anchoColumnas: 1 },
      { clave: 'dueno_riesgo', etiqueta: 'Dueño del riesgo', tipo: 'selector-catalogo', codigoCatalogo: 'CAT_DUENO', obligatorio: true, soloLectura: false, anchoColumnas: 1 },
      { clave: 'resultado_formula', etiqueta: 'Resultado calculado', tipo: 'formula', formula: '1 + 2', obligatorio: false, soloLectura: true, anchoColumnas: 2 }
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
  await page.route('**/api/catalogos/modulos', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) }));
  await page.route('**/api/auditoria**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) }));
  await page.route('**/api/matrices-riesgos/**', route => {
    const ruta = new URL(route.request().url()).pathname;
    let datos: unknown = [];
    if (ruta.endsWith('/familias/paginado')) datos = { items: [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz UAT', famDescripcion: 'Familia E2E del shell modal.', famActivo: true, famFechaCreacion: '2026-08-13T12:00:00Z', totalVersiones: 2, tieneVersionVigente: true }], pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1, totales: { totalFamilias: 1, activas: 1, inactivas: 0, totalVersiones: 2 } };
    else if (ruta.endsWith('/familias')) datos = [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz UAT', famDescripcion: 'Familia E2E del shell modal.', famActivo: true, famFechaCreacion: '2026-08-13T12:00:00Z', totalVersiones: 2, tieneVersionVigente: true }];
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

test('el Form Builder separa su header premium de la navegación neutra del workspace', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  await detalleFamilia.getByRole('button', { name: /Editar definici.n de la versi.n/i }).first().click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const header = dialogo.locator('[data-modal-shell-region="header"]');
  const workspace = dialogo.locator('.form-builder-toolbar__workspace');
  await expect(header).toBeVisible();
  await expect(header).toHaveClass(/modal-header-institutional/);
  await expect(header.getByRole('heading', { name: /Constructor de Formularios Din.micos/i })).toBeVisible();
  await expect(workspace).toBeVisible();

  const styles = await page.evaluate(() => {
    const headerElement = document.querySelector('[data-modal-shell-region="header"]');
    const workspaceElement = document.querySelector('.form-builder-toolbar__workspace');
    if (!(headerElement instanceof HTMLElement) || !(workspaceElement instanceof HTMLElement)) return null;
    const headerCss = getComputedStyle(headerElement);
    const workspaceCss = getComputedStyle(workspaceElement);
    return {
      headerBackgroundImage: headerCss.backgroundImage,
      headerBackgroundColor: headerCss.backgroundColor,
      workspaceBackgroundImage: workspaceCss.backgroundImage,
      workspaceBackgroundColor: workspaceCss.backgroundColor
    };
  });
  expect(styles).not.toBeNull();
  if (!styles) throw new Error('No se pudieron leer los estilos del shell compuesto.');
  expect(styles.headerBackgroundImage).toContain('gradient');
  expect(styles.headerBackgroundColor).not.toBe('rgb(255, 255, 255)');
  expect(styles.workspaceBackgroundColor).toBe('rgba(248, 250, 252, 0.8)');
  expect(styles.workspaceBackgroundImage).toBe('none');
  await writeFile('test-results/form-builder-header-after.json', JSON.stringify(styles, null, 2), 'utf8');
  await header.screenshot({ path: 'test-results/form-builder-premium-header-stage.png' });
});

test('la X del Form Builder conserva contraste, foco y cierre por ratón y teclado', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  await detalleFamilia.getByRole('button', { name: /Editar definici.n de la versi.n/i }).first().click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const header = dialogo.locator('[data-modal-shell-region="header"]');
  const cerrar = header.getByRole('button', { name: 'Cerrar modal de constructor' });
  await expect(cerrar).toBeVisible();
  const box = await cerrar.boundingBox();
  expect(box).not.toBeNull();
  if (!box) throw new Error('El control de cierre del Form Builder no tiene geometría visible.');
  expect(box.width).toBe(44);
  expect(box.height).toBe(44);

  const styles = await cerrar.evaluate(element => {
    const css = getComputedStyle(element);
    const icon = element.querySelector('app-action-icon[action="close"]') as HTMLElement | null;
    const iconCss = icon ? getComputedStyle(icon) : null;
    const root = getComputedStyle(document.documentElement);
    return {
      visibility: css.visibility,
      opacity: css.opacity,
      foreground: css.color,
      background: css.backgroundColor,
      border: css.borderColor,
      iconVisibility: iconCss?.visibility ?? 'missing',
      iconOpacity: iconCss?.opacity ?? '0',
      stops: ['--ihss-modal-primary-start-rgb', '--ihss-modal-primary-mid-rgb', '--ihss-modal-primary-end-rgb'].map(name => root.getPropertyValue(name).trim())
    };
  });
  expect(styles.visibility).toBe('visible');
  expect(styles.opacity).toBe('1');
  expect(styles.foreground).toBe('rgb(255, 255, 255)');
  expect(styles.background).toBe('rgba(255, 255, 255, 0.16)');
  expect(styles.border).toBe('rgba(255, 255, 255, 0.46)');
  expect(styles.iconVisibility).toBe('visible');
  expect(styles.iconOpacity).toBe('1');

  const white = { r: 255, g: 255, b: 255 };
  const buttonAlpha = 0.16;
  const contrastByStop = styles.stops.map(stop => {
    const [r, g, b] = stop.split(/\s+/).map(Number);
    const composited = {
      r: Math.round(white.r * buttonAlpha + r * (1 - buttonAlpha)),
      g: Math.round(white.g * buttonAlpha + g * (1 - buttonAlpha)),
      b: Math.round(white.b * buttonAlpha + b * (1 - buttonAlpha))
    };
    return contrastRatio(white, composited);
  });
  expect(Math.min(...contrastByStop)).toBeGreaterThanOrEqual(3);
  await writeFile('test-results/form-builder-close-after.json', JSON.stringify({ ...styles, contrastByStop, minimumContrast: Math.min(...contrastByStop) }, null, 2), 'utf8');

  await cerrar.focus();
  await page.keyboard.press('Tab');
  await page.keyboard.press('Shift+Tab');
  await expect(cerrar).toBeFocused();
  await expect.poll(() => cerrar.evaluate(element => element.matches(':focus-visible'))).toBe(true);
  await expect.poll(() => cerrar.evaluate(element => getComputedStyle(element).outlineStyle)).not.toBe('none');
  await header.screenshot({ path: 'test-results/form-builder-header-close-stage.png' });
  await cerrar.click();
  await expect(dialogo).toBeHidden();

  await detalleFamilia.getByRole('button', { name: /Editar definici.n de la versi.n/i }).first().click();
  const dialogoReabierto = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const cerrarReabierto = dialogoReabierto.locator('[data-modal-shell-region="header"] button[aria-label="Cerrar modal de constructor"]');
  await cerrarReabierto.focus();
  await page.keyboard.press('Enter');
  await expect(dialogoReabierto).toBeHidden();

  await detalleFamilia.getByRole('button', { name: /Editar definici.n de la versi.n/i }).first().click();
  const dialogoConFoco = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const cerrarConFoco = dialogoConFoco.locator('[data-modal-shell-region="header"] button[aria-label="Cerrar modal de constructor"]');
  await cerrarConFoco.focus();
  await page.keyboard.press('Space');
  await expect(dialogoConFoco).toBeHidden();
});

test('el footer del Form Builder hereda el shell y etiqueta sus acciones según el modo', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  await detalleFamilia.getByRole('button', { name: /Editar definici.n de la versi.n/i }).first().click();

  const editable = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const footer = editable.locator('[data-modal-shell-region="footer"]');
  await expect(footer).toHaveClass(/modal-footer-institutional/);
  await expect(footer.getByRole('button', { name: 'Cancelar cambios' })).toBeVisible();
  await expect(footer.getByRole('button', { name: 'Guardar cambios' })).toBeVisible();
  const editableFooterStyles = await footer.evaluate(element => {
    const css = getComputedStyle(element);
    return { backgroundImage: css.backgroundImage, backgroundColor: css.backgroundColor };
  });
  expect(editableFooterStyles.backgroundImage).toContain('gradient');
  expect(editableFooterStyles.backgroundColor).not.toBe('rgb(255, 255, 255)');
  await footer.screenshot({ path: 'test-results/form-builder-editable-footer-stage.png' });
  await footer.getByRole('button', { name: 'Cancelar cambios' }).click();
  await expect(editable).toBeHidden();

  await detalleFamilia.getByRole('button', { name: /Ver detalle de la versi.n/i }).last().click();
  const readonly = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const readonlyFooter = readonly.locator('[data-modal-shell-region="footer"]');
  await expect(readonlyFooter.getByRole('button', { name: 'Cerrar constructor' })).toBeVisible();
  await expect(readonlyFooter.locator('.form-builder-statusbar__save')).toHaveCount(0);
  const readonlyGradient = await readonlyFooter.evaluate(element => getComputedStyle(element).backgroundImage);
  expect(readonlyGradient).toContain('gradient');
  await writeFile('test-results/form-builder-footer-after.json', JSON.stringify({ editable: editableFooterStyles, readonlyGradient }, null, 2), 'utf8');
  await readonlyFooter.getByRole('button', { name: 'Cerrar constructor' }).click();
  await expect(readonly).toBeHidden();
});

test('el Form Builder conserva su diseño responsive y actualiza el shell durante el cambio de tema', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  await detalleFamilia.getByRole('button', { name: /Editar definici.n de la versi.n/i }).first().click();
  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const header = dialogo.locator('[data-modal-shell-region="header"]');
  const footer = dialogo.locator('[data-modal-shell-region="footer"]');
  const cerrar = header.getByRole('button', { name: 'Cerrar modal de constructor' });
  await expect(header).toBeVisible();
  await expect(cerrar).toBeVisible();
  await expect(footer.getByRole('button', { name: 'Guardar cambios' })).toBeVisible();

  const baseline = await page.evaluate(() => ({
    width: document.documentElement.scrollWidth,
    viewport: window.innerWidth,
    headerBackground: getComputedStyle(document.querySelector('[data-modal-shell-region="header"]')!).backgroundImage,
    footerBackground: getComputedStyle(document.querySelector('[data-modal-shell-region="footer"]')!).backgroundImage
  }));
  expect(baseline.width).toBeLessThanOrEqual(baseline.viewport);
  expect(baseline.headerBackground).toContain('gradient');
  expect(baseline.footerBackground).toContain('gradient');

  const redTheme = createSystemTheme('#991b1b', '#dc2626');
  const red = await page.evaluate(properties => {
    for (const [name, value] of Object.entries(properties)) document.documentElement.style.setProperty(name, value);
    return {
      headerBackground: getComputedStyle(document.querySelector('[data-modal-shell-region="header"]')!).backgroundImage,
      footerBackground: getComputedStyle(document.querySelector('[data-modal-shell-region="footer"]')!).backgroundImage,
      closeColor: getComputedStyle(document.querySelector('[data-modal-shell-region="header"] button[aria-label="Cerrar modal de constructor"]')!).color
    };
  }, redTheme.properties);
  expect(red.headerBackground).not.toBe(baseline.headerBackground);
  expect(red.footerBackground).not.toBe(baseline.footerBackground);
  expect(red.closeColor).toBe('rgb(255, 255, 255)');

  const lightTheme = createSystemTheme('#fca5a5', '#fecaca');
  const light = await page.evaluate(properties => {
    for (const [name, value] of Object.entries(properties)) document.documentElement.style.setProperty(name, value);
    const root = getComputedStyle(document.documentElement);
    const close = getComputedStyle(document.querySelector('[data-modal-shell-region="header"] button[aria-label="Cerrar modal de constructor"]')!);
    return {
      headerBackground: getComputedStyle(document.querySelector('[data-modal-shell-region="header"]')!).backgroundImage,
      closeColor: close.color,
      start: root.getPropertyValue('--ihss-modal-primary-start-rgb').trim(),
      mid: root.getPropertyValue('--ihss-modal-primary-mid-rgb').trim(),
      end: root.getPropertyValue('--ihss-modal-primary-end-rgb').trim()
    };
  }, lightTheme.properties);
  expect(light.headerBackground).not.toBe(red.headerBackground);
  expect(light.closeColor).toBe('rgb(255, 255, 255)');
  for (const stop of [light.start, light.mid, light.end]) {
    const [r, g, b] = stop.split(/\s+/).map(Number);
    expect(contrastRatio({ r: 255, g: 255, b: 255 }, { r, g, b })).toBeGreaterThanOrEqual(4.5);
  }
  await dialogo.screenshot({ path: 'test-results/form-builder-premium-mobile-live-theme.png' });
});

test('el maestro visual de modales muestra una X de cierre legible y consistente', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();

  const modal = page.locator('[data-ui-fam-detail="modal"]');
  await expect(modal).toBeVisible();
  const cerrar = modal.getByRole('button', { name: 'Cerrar detalle de familia' });
  await expect(cerrar).toBeVisible();

  const caja = await cerrar.boundingBox();
  expect(caja).not.toBeNull();
  if (!caja) throw new Error('No se pudo medir el control maestro de cierre.');
  expect(caja.width).toBeGreaterThanOrEqual(43);
  expect(caja.height).toBeGreaterThanOrEqual(43);

  const estilos = await cerrar.evaluate(element => {
    const css = getComputedStyle(element);
    const icono = element.querySelector('app-action-icon[action="close"]') as HTMLElement | null;
    const iconCss = icono ? getComputedStyle(icono) : null;
    return {
      backgroundColor: css.backgroundColor,
      borderColor: css.borderColor,
      color: css.color,
      iconWidth: iconCss?.width ?? '0px',
      iconHeight: iconCss?.height ?? '0px'
    };
  });

  expect(estilos.backgroundColor).toBe('rgba(255, 255, 255, 0.16)');
  expect(estilos.borderColor).toBe('rgba(255, 255, 255, 0.46)');
  expect(estilos.color).toBe('rgb(255, 255, 255)');
  expect(Number.parseFloat(estilos.iconWidth)).toBeGreaterThanOrEqual(20);
  expect(Number.parseFloat(estilos.iconHeight)).toBeGreaterThanOrEqual(20);

  await cerrar.hover();
  await expect.poll(async () => cerrar.evaluate(element => getComputedStyle(element).backgroundColor))
    .toBe('rgba(255, 255, 255, 0.28)');
});

test('el modal abierto hereda cambios azul, rojo y verde del tema sin alterar geometría', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const modal = page.locator('[data-ui-fam-detail="modal"]');
  const header = modal.locator('.modal-container-card > header');
  const title = header.getByRole('heading', { name: 'Detalle de Familia' });
  const close = header.getByRole('button', { name: 'Cerrar detalle de familia' });
  const card = modal.locator('.modal-container-card');
  await expect(modal).toBeVisible();

  const desktopBoxBefore = await card.boundingBox();
  const initialTheme = createSystemTheme('#1e3a8a', '#1d4ed8');
  const applyTheme = async (primary: string, accent: string) => {
    const theme = createSystemTheme(primary, accent);
    await page.evaluate(properties => {
      for (const [name, value] of Object.entries(properties)) document.documentElement.style.setProperty(name, value);
    }, theme.properties);
    return theme;
  };

  await applyTheme('#1e3a8a', '#1d4ed8');
  const blueHeader = await header.evaluate(element => getComputedStyle(element).backgroundImage);
  expect(blueHeader).toContain('rgb(30, 58, 138)');
  expect(await title.evaluate(element => getComputedStyle(element).color)).toBe('rgb(255, 255, 255)');
  const artifactsDir = process.env.RL_BLOCK9_ARTIFACT_DIR;
  if (artifactsDir) {
    await mkdir(artifactsDir, { recursive: true });
    await page.screenshot({ path: join(artifactsDir, 'modal-theme-blue-desktop.png'), fullPage: true });
  }

  const redTheme = await applyTheme('#991b1b', '#dc2626');
  const redHeader = await header.evaluate(element => getComputedStyle(element).backgroundImage);
  expect(redHeader).not.toBe(blueHeader);
  expect(redHeader).toContain('rgb(153, 27, 27)');
  const redFamilyStops = ['start', 'mid', 'end'].map(stop => `rgb(${redTheme.properties[`--ihss-modal-primary-${stop}-rgb`].split(' ').join(', ')})`);
  for (const stop of redFamilyStops) expect(redHeader).toContain(stop);
  const redForeground = parseHexColor(`#${redTheme.properties['--ihss-on-modal-rgb'].split(' ').map(value => Number(value).toString(16).padStart(2, '0')).join('')}`)!;
  const redSurface = parseHexColor(`#${redTheme.properties['--ihss-modal-primary-rgb'].split(' ').map(value => Number(value).toString(16).padStart(2, '0')).join('')}`)!;
  expect(contrastRatio(redSurface, redForeground)).toBeGreaterThanOrEqual(4.5);
  const subtitle = header.locator('p, small').first();
  if (await subtitle.count()) {
    const subtitleColor = await subtitle.evaluate(element => getComputedStyle(element).color);
    expect(subtitleColor).not.toBe('rgba(255, 255, 255, 0.4)');
  }
  const closeStyles = await close.evaluate(element => {
    const css = getComputedStyle(element);
    return { visibility: css.visibility, opacity: css.opacity, color: css.color };
  });
  expect(closeStyles.visibility).toBe('visible');
  expect(Number(closeStyles.opacity)).toBeGreaterThanOrEqual(0.9);
  if (artifactsDir) await page.screenshot({ path: join(artifactsDir, 'modal-theme-red-desktop.png'), fullPage: true });

  await applyTheme('#fff8b5', '#fef08a');
  const lightHeaderText = await title.evaluate(element => getComputedStyle(element).color);
  expect(lightHeaderText).toBe('rgb(255, 255, 255)');
  const lightSurface = await page.evaluate(() => getComputedStyle(document.documentElement).getPropertyValue('--ihss-modal-primary-rgb').trim());
  const lightSurfaceRgb = lightSurface.split(' ').map(Number);
  const lightModalSurface = { r: lightSurfaceRgb[0], g: lightSurfaceRgb[1], b: lightSurfaceRgb[2] };
  expect(contrastRatio(lightModalSurface, parseHexColor('#ffffff')!)).toBeGreaterThanOrEqual(4.5);
  if (artifactsDir) await page.screenshot({ path: join(artifactsDir, 'modal-theme-light-desktop.png'), fullPage: true });

  const greenTheme = await applyTheme('#166534', '#22c55e');
  const greenHeader = await header.evaluate(element => getComputedStyle(element).backgroundImage);
  expect(greenHeader).toContain('rgb(22, 101, 52)');
  expect(greenHeader).not.toBe(redHeader);
  await expect(close).toBeVisible();
  expect(await close.evaluate(element => getComputedStyle(element).color)).toBe('rgb(255, 255, 255)');
  expect(contrastRatio(
    parseHexColor(`#${greenTheme.properties['--ihss-modal-primary-rgb'].split(' ').map(value => Number(value).toString(16).padStart(2, '0')).join('')}`)!,
    parseHexColor(`#${greenTheme.properties['--ihss-on-modal-rgb'].split(' ').map(value => Number(value).toString(16).padStart(2, '0')).join('')}`)!
  )).toBeGreaterThanOrEqual(4.5);

  const desktopBoxAfter = await card.boundingBox();
  expect(desktopBoxBefore?.width).toBe(desktopBoxAfter?.width);
  expect(desktopBoxBefore?.height).toBe(desktopBoxAfter?.height);
  await page.setViewportSize({ width: 390, height: 844 });
  await applyTheme('#991b1b', '#dc2626');
  const mobileState = await page.evaluate(() => ({
    documentWidth: document.documentElement.scrollWidth,
    viewportWidth: document.documentElement.clientWidth,
    headerWidth: document.querySelector('[data-ui-fam-detail="modal"] .modal-container-card > header')?.getBoundingClientRect().width ?? 0
  }));
  expect(mobileState.documentWidth).toBeLessThanOrEqual(mobileState.viewportWidth);
  expect(mobileState.headerWidth).toBeLessThanOrEqual(390);
  if (artifactsDir) {
    await page.screenshot({ path: join(artifactsDir, 'modal-theme-red-mobile.png'), fullPage: true });
    await applyTheme('#1e3a8a', '#1d4ed8');
    await page.screenshot({ path: join(artifactsDir, 'modal-theme-blue-mobile.png'), fullPage: true });
  }
  expect(initialTheme.properties['--ihss-primary-rgb']).toBe('30 58 138');
});

test('Crear Usuario hereda theme institucional y mantiene acciones de footer icon-only visibles', async ({ page }) => {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  const usuariosToken = `${encode({ alg: 'none' })}.${encode({ nameid: '1', uid: 'modal-user-audit', role: 'ADMINISTRADOR', rol_id: '1', modulos: '2,10', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
  await page.addInitScript(jwt => localStorage.setItem('access_token', jwt), usuariosToken);
  await page.route('**/api/catalogos/modulos', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [{ moduloId: 2, nombre: 'Usuarios', codigo: 'USUARIOS' }] }) }));
  await page.route('**/api/catalogos/roles', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [{ rolId: 1, rolNombre: 'Administrador', rolActivo: true }] }) }));
  await page.route('**/api/catalogos/dominios', route => route.fulfill({ status: 200, contentType: 'application/json', body: '{"success":true,"datos":[]}' }));
  await page.route('**/api/auth/usuarios/paginado**', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: { items: [], pagina: 1, tamanoPagina: 25, totalRegistros: 0, totalPaginas: 0 } }) }));

  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/usuarios');
  await page.getByRole('button', { name: 'Crear nuevo usuario' }).click();
  const modal = page.locator('dialog[open] .modal-container-card');
  const header = modal.locator('.modal-header-institutional');
  const body = modal.locator('.modal-body-scrollable');
  const footer = modal.locator('.modal-footer-institutional');
  await expect(header).toBeVisible();
  await expect(footer).toBeVisible();
  const saveButton = modal.getByRole('button', { name: 'Guardar usuario' });
  const cancelButton = modal.getByRole('button', { name: 'Cancelar edición de usuario' });
  await expect(saveButton).toBeVisible();
  await expect(saveButton).toHaveAttribute('title', 'Guardar usuario');
  await expect(cancelButton).toHaveAttribute('title', 'Cancelar edición de usuario');
  await expect(saveButton).toHaveClass(/modal-action-icon/);
  await expect(cancelButton).toHaveClass(/modal-action-icon/);
  await expect(saveButton).toHaveText('');
  await expect(cancelButton).toHaveText('');
  const saveButtonBox = await saveButton.boundingBox();
  const cancelButtonBox = await cancelButton.boundingBox();
  expect(saveButtonBox?.width).toBeGreaterThanOrEqual(44);
  expect(saveButtonBox?.height).toBeGreaterThanOrEqual(44);
  expect(cancelButtonBox?.width).toBeGreaterThanOrEqual(44);
  expect(cancelButtonBox?.height).toBeGreaterThanOrEqual(44);
  const saveIconMetrics = await saveButton.locator('app-action-icon svg').evaluate(icon => {
    const bounds = icon.getBoundingClientRect();
    const css = getComputedStyle(icon);
    return { width: bounds.width, height: bounds.height, visibility: css.visibility, opacity: css.opacity, stroke: css.stroke };
  });
  expect(saveIconMetrics.width).toBeGreaterThanOrEqual(14);
  expect(saveIconMetrics.height).toBeGreaterThanOrEqual(14);
  expect(saveIconMetrics.visibility).toBe('visible');
  expect(saveIconMetrics.opacity).toBe('1');
  expect(saveIconMetrics.stroke).not.toBe('none');
  expect(await saveButton.evaluate(element => Number(getComputedStyle(element).opacity))).toBeGreaterThan(0.7);
  const artifactsDir = process.env.RL_BLOCK9_ARTIFACT_DIR;
  if (artifactsDir) {
    await mkdir(artifactsDir, { recursive: true });
    await modal.screenshot({ path: join(artifactsDir, 'modal-create-user-premium-blue.png') });
  }

  const redTheme = createSystemTheme('#991b1b', '#dc2626');
  await page.evaluate(properties => {
    for (const [name, value] of Object.entries(properties)) document.documentElement.style.setProperty(name, value);
  }, redTheme.properties);
  const styles = await page.evaluate(() => {
    const headerElement = document.querySelector('dialog[open] .modal-header-institutional')!;
    const footerElement = document.querySelector('dialog[open] .modal-footer-institutional')!;
    const bodyElement = document.querySelector('dialog[open] .modal-body-scrollable')!;
    const cardElement = document.querySelector('dialog[open] .modal-container-card')!;
    return { header: getComputedStyle(headerElement).backgroundImage, footer: getComputedStyle(footerElement).backgroundImage, body: getComputedStyle(bodyElement).backgroundColor, card: getComputedStyle(cardElement).backgroundColor };
  });
  const stops = ['start', 'mid', 'end'].map(stop => `rgb(${redTheme.properties[`--ihss-modal-primary-${stop}-rgb`].split(' ').join(', ')})`);
  for (const stop of stops) expect(styles.header).toContain(stop);
  expect(styles.footer).toContain(stops[0]);
  expect(styles.footer).toContain(stops[1]);
  expect(styles.footer).not.toContain('rgb(30, 58, 138)');
  expect(styles.card).toBe('rgb(255, 255, 255)');
  expect(['rgb(255, 255, 255)', 'rgba(0, 0, 0, 0)']).toContain(styles.body);

  if (artifactsDir) {
    await modal.screenshot({ path: join(artifactsDir, 'modal-create-user-premium-red.png') });
  }
});

test('captura el estado editable del constructor a 1536x1024', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  await detalleFamilia.getByRole('button', { name: 'Editar definición de la versión' }).first().click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  await expect(dialogo).toBeVisible();
  await expect(dialogo).toContainText('BORRADOR');
  await expect(dialogo.locator('#btn-guardar-builder')).toBeVisible();
  await expect(dialogo.locator('#btn-publicar-builder')).toBeVisible();
  await expect(dialogo.locator('#tab-editor-visual')).toBeVisible();
  await expect(dialogo.locator('#tab-vista-preview')).toBeVisible();
  await expect(dialogo.locator('#tab-configuracion-general')).toHaveCount(0);
  const editorTab = dialogo.locator('#tab-editor-visual');
  const previewTab = dialogo.locator('#tab-vista-preview');
  await expect(editorTab).toHaveAttribute('aria-current', 'page');
  await expect(editorTab).toHaveClass(/border-ihss-600/);
  await expect(previewTab).not.toHaveAttribute('aria-current', 'page');
  const canvasTab = dialogo.locator('#tab-vista-secciones');
  const catalogsTab = dialogo.locator('#tab-vista-catalogos');
  await expect(canvasTab).toHaveClass(/bg-white/);
  await expect(catalogsTab).not.toHaveClass(/bg-white/);
  await catalogsTab.click();
  await expect(catalogsTab).toHaveClass(/bg-white/);
  await expect(canvasTab).not.toHaveClass(/bg-white/);
  await canvasTab.click();
  await expect(canvasTab).toHaveClass(/bg-white/);
  await expect(catalogsTab).not.toHaveClass(/bg-white/);
  await previewTab.click();
  await expect(previewTab).toHaveAttribute('aria-current', 'page');
  await expect(previewTab).toHaveClass(/border-ihss-600/);
  await expect(editorTab).not.toHaveAttribute('aria-current', 'page');
  await editorTab.click();
  await expect(editorTab).toHaveAttribute('aria-current', 'page');
  await expect(previewTab).not.toHaveAttribute('aria-current', 'page');
  await expect(dialogo.getByText('Acciones', { exact: false })).toBeVisible();
  await expect(dialogo.locator('[data-modal-shell-region="footer"] button[aria-label="Cancelar cambios"]')).toBeVisible();
  await expect(dialogo.locator('[data-modal-shell-region="footer"] button[aria-label="Guardar cambios"]')).toBeEnabled();
  await expect(dialogo.getByText('Modo de visualización y consulta técnica', { exact: false })).toHaveCount(0);
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
  await expect(dialogo.locator('[data-modal-shell-region="footer"] button[aria-label="Cerrar constructor"]')).toBeVisible();
  await expect(dialogo.locator('[data-modal-shell-region="footer"] .form-builder-statusbar__save')).toHaveCount(0);
  await page.screenshot({ path: 'test-results/ui-form5-readonly-1536x1024.png', fullPage: true });
});

test('duplica secciones sin colisiones, conserva cards y expone menu contextual', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalleFamilia).toBeVisible();
  await detalleFamilia.getByRole('button', { name: 'Editar definición de la versión' }).first().click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const lienzo = dialogo.getByRole('main', { name: 'Lienzo del formulario' });
  const secciones = lienzo.locator('section.form-builder-section');
  await expect(secciones).toHaveCount(1);
  await expect(secciones.first()).toContainText('Área responsable');
  await secciones.first().getByRole('button', { name: 'Duplicar sección' }).click();
  await expect(secciones).toHaveCount(2);
  await expect(secciones.nth(0)).toContainText('Área responsable');
  await expect(secciones.nth(1)).toContainText('Área responsable');
  await expect(secciones.nth(1).locator('input[placeholder^="Nombre de la sección"]')).toHaveValue(/\(copia\)/);
  await expect(secciones.nth(1).locator('article')).toHaveCount(3);

  const claves = await lienzo.locator('.form-builder-field-card span.font-mono').filter({ hasText: ' · ' }).allTextContents();
  expect(claves).toHaveLength(6);
  expect(new Set(claves.map(texto => texto.split(' · ')[0].trim())).size).toBe(6);
  await secciones.nth(1).getByRole('button', { name: 'Acciones de sección' }).click();
  await expect(dialogo.getByRole('button', { name: 'Mover sección arriba' })).toBeVisible();
  await page.screenshot({ path: 'test-results/ui-form-final-b-menu-open-1536x1024.png', fullPage: true });
  await lienzo.locator('.form-builder-canvas__scroll').click({ position: { x: 8, y: 8 } });
  await expect(dialogo.getByRole('button', { name: 'Mover sección arriba' })).toBeHidden();
  await page.screenshot({ path: 'test-results/ui-form-final-b-menu-1536x1024.png', fullPage: true });
});

test('captura Vista Previa integrada sin herramientas de edición', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1024 });
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalleFamilia = page.locator('[data-ui-fam-detail="modal"]');
  await detalleFamilia.getByRole('button', { name: 'Editar definición de la versión' }).first().click();

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
  await expect(preview).toContainText('1 + 2');
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

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  await expect(dialogo).toBeVisible();
  await dialogo.getByRole('button', { name: 'Ver JSON Técnico' }).click();
  const json = dialogo.locator('#json-avanzado');
  await expect(json).toBeVisible();
  await expect(dialogo.getByRole('button', { name: 'Copiar JSON' })).toBeVisible();
  await expect(dialogo.getByRole('searchbox', { name: 'Buscar en JSON técnico' })).toBeVisible();
  await expect(dialogo.getByRole('button', { name: 'Validar' })).toBeVisible();
  await expect(dialogo.getByRole('button', { name: 'Sincronizar JSON hacia el lienzo visual' })).toBeVisible();
  await dialogo.getByRole('searchbox', { name: 'Buscar en JSON técnico' }).fill('area_responsable');
  await expect(dialogo).toContainText('1 coincidencia(s)');
  await dialogo.getByRole('button', { name: 'Validar' }).click();
  await expect(dialogo).toContainText('JSON válido y estructura compatible.');
  await page.screenshot({ path: 'test-results/ui-form6-json-1536x1024.png', fullPage: true });
});
