import { expect, Page, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

const familia = {
  famId: 1,
  famCodigo: 'MATRIZ_RIESGOS_LAFT',
  famNombre: 'Matriz de Riesgos LA/FT',
  famDescripcion: 'Familia E2E para validar bloqueo modal',
  famActivo: true
};

const definicion = JSON.stringify({
  codigoFormulario: 'MATRIZ_RIESGOS_LAFT_V2',
  nombreFormulario: 'Matriz de Riesgos LA/FT',
  secciones: [
    {
      clave: 'identificacion',
      titulo: 'Identificación',
      orden: 1,
      columnasPorFila: 1,
      campos: [
        {
          clave: 'area_responsable',
          etiqueta: 'Área responsable',
          tipo: 'texto',
          obligatorio: true,
          soloLectura: false
        }
      ]
    }
  ]
});

const versionVigente = {
  verId: 10,
  verFamiliaId: 1,
  verCodigo: 'MATRIZ_RIESGOS_LAFT_V1',
  verVersion: 1,
  verJson: definicion,
  verHash: 'modal-shell-vigente',
  verEstado: 'PUBLISHED',
  verVigente: true,
  verFechaCreacion: '2026-08-13T12:00:00Z',
  verUsrCreacion: 1
};

const versionBorrador = {
  ...versionVigente,
  verId: 11,
  verCodigo: 'MATRIZ_RIESGOS_LAFT_V2',
  verVersion: 2,
  verHash: 'modal-shell-draft',
  verEstado: 'DRAFT',
  verVigente: false
};

function tokenAdministrador(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '1',
    uid: 'admin.modal.e2e',
    email: 'admin.modal.e2e@ihss.hn',
    given_name: 'Admin',
    family_name: 'Modal',
    role: 'ADMINISTRADOR',
    rol_id: '1',
    modulos: '10',
    debe_cambiar_pass: '0',
    exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

async function preparar(page: Page): Promise<void> {
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'modal-refresh');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, tokenAdministrador());

  await page.route('**/api/configuracion/sistema', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      success: true,
      datos: {
        nombreSistema: 'SGRLA-IHSS',
        nombreInstitucion: 'IHSS',
        colorPrimario: '#1e3a8a',
        colorSecundario: '#1d4ed8',
        timeoutSesion: 30
      }
    })
  }));

  await page.route('**/api/configuracion/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] })
  }));

  await page.route('**/api/matrices-riesgos/**', route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    let datos: unknown = [];

    if (path.endsWith('/familias')) datos = [familia];
    else if (path.endsWith('/formulario/version-vigente')) datos = versionVigente;
    else if (path.endsWith('/metodologia/vigente')) datos = {
      versionFormularioId: versionVigente.verId,
      codigo: versionVigente.verCodigo,
      version: versionVigente.verVersion,
      secciones: [],
      catalogos: [],
      reglas: []
    };
    else if (path.endsWith('/formularios/historial')) datos = [versionVigente, versionBorrador];
    else if (path.endsWith('/riesgos')) datos = [];
    else if (path.endsWith('/evaluaciones')) datos = [];
    else if (path.endsWith('/consolidado')) datos = [];

    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, datos })
    });
  });

  await page.route('**/api/matrices-riesgos*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] })
  }));
}

test.beforeEach(async ({ page }) => preparar(page));

test('el Form Builder bloquea Salir, navegación y foco del shell mientras el modal está abierto', async ({ page }) => {
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();

  const editarDefinicion = page.getByRole('button', { name: 'Editar definición' }).first();
  await expect(editarDefinicion).toBeVisible();
  await editarDefinicion.focus();
  await editarDefinicion.click();

  const dialogo = page.locator('[role="dialog"][aria-modal="true"]:has(app-form-builder)');
  await expect(dialogo).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Constructor de Formularios Dinámicos' })).toBeVisible();

  const headerPrincipal = page.locator('app-main-layout > div > div > header');
  const menuPrincipal = page.locator('app-main-layout > div > aside');
  const salir = page.locator('app-main-layout > div > div > header button[aria-label="Cerrar sesión"]');

  await expect.poll(() => headerPrincipal.evaluate(elemento => (elemento as HTMLElement).inert)).toBe(true);
  await expect.poll(() => menuPrincipal.evaluate(elemento => (elemento as HTMLElement).inert)).toBe(true);

  const cajaSalir = await salir.boundingBox();
  expect(cajaSalir).not.toBeNull();
  if (!cajaSalir) throw new Error('No se pudo obtener la posición del botón Salir.');

  await page.mouse.move(cajaSalir.x + cajaSalir.width / 2, cajaSalir.y + cajaSalir.height / 2);
  await expect.poll(() => salir.evaluate(elemento => elemento.matches(':hover'))).toBe(false);

  await page.mouse.click(cajaSalir.x + cajaSalir.width / 2, cajaSalir.y + cajaSalir.height / 2);
  await expect(page).toHaveURL(/\/matrices-riesgos$/);
  expect(await page.evaluate(() => localStorage.getItem('access_token'))).toBeTruthy();

  for (let intento = 0; intento < 20; intento++) {
    await page.keyboard.press('Tab');
    expect(await page.evaluate(() => {
      const modal = document.querySelector('[role="dialog"][aria-modal="true"]');
      return Boolean(modal?.contains(document.activeElement));
    })).toBe(true);
  }
  await expect(salir).not.toBeFocused();

  await page.getByText('Área responsable', { exact: true }).click();
  const inspector = page.getByText('Propiedades del Campo', { exact: true }).locator('..');
  const claveTecnica = inspector.locator('input[type="text"]').first();
  await expect(claveTecnica).toBeEnabled();
  await claveTecnica.fill('area_responsable_actualizada');
  await expect(claveTecnica).toHaveValue('area_responsable_actualizada');

  await page.getByRole('button', { name: 'Cerrar modal de constructor' }).click();
  await expect(dialogo).toBeHidden();
  await expect.poll(() => headerPrincipal.evaluate(elemento => (elemento as HTMLElement).inert)).toBe(false);
  await expect.poll(() => menuPrincipal.evaluate(elemento => (elemento as HTMLElement).inert)).toBe(false);
  await expect(editarDefinicion).toBeFocused();

  await salir.hover();
  await expect(salir).toHaveCSS('pointer-events', 'auto');
});
