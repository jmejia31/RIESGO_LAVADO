import { expect, Page, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

const definicion = JSON.stringify({
  codigoFormulario: 'MATRIZ_RIESGOS_LAFT_V2', nombreFormulario: 'Matriz UAT',
  secciones: [{ clave: 'identificacion', titulo: 'Identificación', orden: 1, columnasPorFila: 1,
    campos: [{ clave: 'area_responsable', etiqueta: 'Área responsable', tipo: 'texto', obligatorio: true, soloLectura: false }] }]
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
    if (ruta.endsWith('/familias')) datos = [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz UAT', famActivo: true }];
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
  await page.getByLabel('Más acciones para Matriz UAT').click();
  await page.getByRole('button', { name: 'Ver versiones' }).click();
  const editar = page.getByRole('button', { name: 'Editar definición' }).first();
  await editar.focus();
  await editar.click();

  const dialogo = page.locator('dialog[open][aria-modal="true"]:has(app-form-builder)');
  const header = page.locator('app-main-layout > div > div > header');
  const aside = page.locator('app-main-layout > div > aside');
  const salir = header.locator('button[aria-label="Cerrar sesión"]');
  await expect(dialogo).toBeVisible();
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

  const primerCampoCard = dialogo.locator('.grid > div').first();
  await primerCampoCard.click();
  const inspector = page.getByText('Propiedades del Campo', { exact: true }).locator('..');
  const clave = inspector.locator('input[type="text"]').first();
  await expect(clave).toBeEnabled();
  await clave.fill('area_responsable_actualizada');
  await expect(clave).toHaveValue('area_responsable_actualizada');

  await page.keyboard.press('Escape');
  await expect(dialogo).toBeHidden();
  await expect.poll(() => header.evaluate(el => (el as HTMLElement).inert)).toBe(false);
  await expect.poll(() => aside.evaluate(el => (el as HTMLElement).inert)).toBe(false);
});
