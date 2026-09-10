import { expect, Page, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

const eventos = [
  {
    audId: 42,
    tabla: 'RL_USUARIOS',
    registroId: '123',
    accion: 'UPDATE',
    datosAnt: '{"activo":false}',
    datosNvo: '{"activo":true}',
    usrId: 102,
    usrEmail: 'javier.mejia@ihss.hn',
    ip: '10.0.0.8',
    fecha: '2026-08-26T11:48:36',
    modulo: 'AdminUsuarios'
  },
  {
    audId: 43,
    tabla: 'RL_LISTAS_CAUTELA',
    registroId: '987',
    accion: 'LOGIN',
    datosAnt: null,
    datosNvo: null,
    usrId: 102,
    usrEmail: 'javier.mejia@ihss.hn',
    ip: '10.0.0.8',
    fecha: '2026-08-26T10:30:00',
    modulo: 'Auth'
  }
];

function token(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '102',
    uid: 'javier.mejia',
    email: 'javier.mejia@ihss.hn',
    role: 'ADMINISTRADOR',
    rol_id: '1',
    modulos: '5',
    debe_cambiar_pass: '0',
    exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

async function prepare(page: Page): Promise<void> {
  await page.addInitScript(jwt => {
    localStorage.setItem('access_token', jwt);
    localStorage.setItem('refresh_token', 'bitacora-e2e-refresh');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, token());

  await page.route('**/api/configuracion/sistema', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: { nombreSistema: 'SGRLA-IHSS', nombreInstitucion: 'IHSS', timeoutSesion: 30 } })
  }));
  await page.route('**/api/configuracion/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] })
  }));
  await page.route('**/api/catalogos/modulos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [{ modId: 5, modRuta: 'bitacora', modSeccion: 'Administración', modNombre: 'Bitácora' }] })
  }));
  await page.route('**/api/auditoria**', route => {
    const url = new URL(route.request().url());
    const pagina = Number(url.searchParams.get('pagina') || 1);
    const limite = Number(url.searchParams.get('limite') || 10);
    const datos = Array.from({ length: limite }, (_, index) => ({
      ...eventos[index % eventos.length],
      audId: (pagina - 1) * limite + index + 1
    }));
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ datos, totalRegistros: 1310 })
    });
  });
}

test('muestra la consola de Bitácora y abre el detalle del evento', async ({ page }) => {
  await prepare(page);
  await page.setViewportSize({ width: 1366, height: 768 });
  await page.goto('/bitacora');

  await expect(page.getByRole('heading', { name: 'Bitácora General del Sistema' })).toBeVisible();
  await expect(page.getByText('Auditoría y registro de acciones, eventos y cambios de datos del SGRLA-IHSS.')).toBeVisible();
  await expect(page.getByLabel('Búsqueda general')).toHaveAttribute('placeholder', 'Usuario, tabla, IP o ID de registro');
  await expect(page.getByRole('columnheader', { name: 'Descripción del evento' })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Gestión de Usuarios' }).first()).toBeVisible();
  await expect(page.getByText('Modificación del registro #123 en RL_USUARIOS.', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Mostrando 1 a 10 de 1310 eventos', { exact: true })).toBeVisible();
  await expect(page.locator('#bitacora-page-size')).toBeVisible();
  await page.screenshot({ path: 'test-results/bitacora-desktop-1366x768.png', fullPage: true });

  const request = page.waitForRequest(request => request.method() === 'GET' && request.url().includes('/api/auditoria') && request.url().includes('buscar=javier'));
  await page.getByLabel('Búsqueda general').fill('javier');
  await request;

  await page.getByRole('button', { name: 'Ver detalle de auditoría' }).first().click();
  await expect(page.getByRole('heading', { name: 'Detalle de Auditoría' })).toBeVisible();
  await expect(page.getByText('Valores anteriores', { exact: true })).toBeVisible();
  await expect(page.getByText('Valores nuevos', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Cerrar detalle de auditoría' }).first().click();
  await expect(page.getByRole('heading', { name: 'Detalle de Auditoría' })).toBeHidden();
});

test('mantiene paginación compacta y mueve el tamaño a la barra de filtros', async ({ page }) => {
  await prepare(page);
  await page.setViewportSize({ width: 1366, height: 768 });
  await page.goto('/bitacora');

  await expect(page.getByRole('button', { name: 'Ir a página 131' })).toBeVisible();
  expect(await page.getByRole('button', { name: /^Ir a página \d+$/ }).count()).toBeLessThanOrEqual(9);

  const pageTwoRequest = page.waitForRequest(request => request.method() === 'GET'
    && request.url().includes('/api/auditoria')
    && new URL(request.url()).searchParams.get('pagina') === '2');
  await page.getByRole('button', { name: 'Ir a página 2' }).click();
  await pageTwoRequest;
  await expect(page.getByText('Mostrando 11 a 20 de 1310 eventos', { exact: true })).toBeVisible();

  const pageSizeRequest = page.waitForRequest(request => request.method() === 'GET'
    && new URL(request.url()).searchParams.get('limite') === '25');
  await page.locator('#bitacora-page-size').selectOption('25');
  await pageSizeRequest;
  await expect(page.getByText('Mostrando 1 a 25 de 1310 eventos', { exact: true })).toBeVisible();
  await expect(page.locator('[aria-label="Paginación de eventos de auditoría"] #bitacora-page-size')).toHaveCount(0);
});

test('mantiene filtros accesibles y evita overflow global en móvil', async ({ page }) => {
  await prepare(page);
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto('/bitacora');

  await expect(page.getByRole('heading', { name: 'Bitácora General del Sistema' })).toBeVisible();
  await expect(page.getByLabel('Búsqueda general')).toBeVisible();
  await expect(page.getByLabel('Entidad / tabla')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Limpiar filtros de bitácora' })).toBeVisible();

  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  expect(overflow).toBe(false);
  await expect(page.locator('.overflow-x-auto').first()).toBeVisible();
  await page.screenshot({ path: 'test-results/bitacora-mobile-375x812.png', fullPage: true });
});

test('genera evidencia responsive de la consola en las resoluciones institucionales', async ({ page }) => {
  await prepare(page);
  const viewports = [
    { width: 1440, height: 900, name: 'desktop-1440x900' },
    { width: 1920, height: 1080, name: 'desktop-1920x1080' },
    { width: 768, height: 1024, name: 'tablet-768x1024' },
    { width: 375, height: 812, name: 'mobile-375x812-repeat' }
  ];

  for (const viewport of viewports) {
    await page.setViewportSize(viewport);
    await page.goto('/bitacora');
    await expect(page.getByRole('heading', { name: 'Bitácora General del Sistema' })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
    await page.screenshot({ path: `test-results/bitacora-${viewport.name}.png`, fullPage: true });
  }
});
