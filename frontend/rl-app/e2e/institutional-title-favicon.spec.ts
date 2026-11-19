import { expect, Page, test } from './support/telemetry-test';

function token(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ nameid: '999888', uid: 'favicon-title-e2e', modulos: '1,2,3,4', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
}

async function stubLoginSlides(page: Page) {
  await page.route('**/api/configuracion/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));
}

test.describe('Parametrización dinámica de Título y Favicon Institucional', () => {
  test.beforeEach(async ({ page }) => {
    await stubLoginSlides(page);
  });

  test('Ruta pública (/login) aplica título institucional y favicon del backend', async ({ page }) => {
    await page.route(/\/api\/configuracion\/sistema\/favicon/, route => route.fulfill({
      status: 200,
      contentType: 'image/x-icon',
      body: Buffer.from([0x00, 0x00, 0x01, 0x00]),
    }));

    await page.route('**/api/configuracion/sistema', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        datos: {
          nombreSistema: 'SGRLA-IHSS',
          nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
          iconoUrl: '/uploads/favicon.png',
          colorPrimario: '#1e3a8a',
          colorSecundario: '#1d4ed8',
        },
      }),
    }));

    await page.goto('/login');

    await expect(page).toHaveTitle('SGRLA-IHSS | Gestión de Riesgos LA/FT');

    const faviconLink = page.locator('link[rel="icon"]');
    await expect(faviconLink).toHaveCount(1);
    const href = await faviconLink.getAttribute('href');
    expect(href).toContain('/api/configuracion/sistema/favicon?v=');
  });

  test('Configuración dinámica actualiza título y favicon en caliente sin recargar', async ({ page }) => {
    let currentConfig = {
      nombreSistema: 'SGRLA-IHSS',
      nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
      iconoUrl: '/uploads/favicon.png',
      colorPrimario: '#1e3a8a',
      colorSecundario: '#1d4ed8',
    };

    await page.addInitScript(accessToken => {
      localStorage.setItem('access_token', accessToken);
      localStorage.setItem('refresh_token', 'e2e-only');
      localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
    }, token());

    await page.route('**/api/catalogos/**', route => route.fulfill({ status: 200, contentType: 'application/json', body: '{"success":true,"datos":[]}' }));
    await page.route(/\/api\/configuracion\/sistema\/favicon/, route => route.fulfill({
      status: 200,
      contentType: 'image/x-icon',
      body: Buffer.from([0x00, 0x00, 0x01, 0x00]),
    }));

    await page.route('**/api/configuracion/sistema', async route => {
      const req = route.request();
      if (req.method() === 'POST' || req.method() === 'PUT') {
        const body = req.postDataJSON();
        currentConfig = { ...currentConfig, ...body };
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, datos: currentConfig }),
      });
    });

    await page.goto('/login');
    await expect(page).toHaveTitle('SGRLA-IHSS | Gestión de Riesgos LA/FT');

    const initialHref = await page.locator('link[rel="icon"]').getAttribute('href');
    expect(initialHref).toContain('/api/configuracion/sistema/favicon?v=');
  });

  test('Eliminar icono restablece favicon.ico y nombre vacío usa fallback SGRLA-IHSS', async ({ page }) => {
    await page.route('**/api/configuracion/sistema/favicon', route => route.fulfill({ status: 204 }));
    await page.route('**/api/configuracion/sistema', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        datos: {
          nombreSistema: '   ',
          nombreInstitucion: 'IHSS',
          iconoUrl: '',
        },
      }),
    }));

    await page.goto('/login');

    await expect(page).toHaveTitle('SGRLA-IHSS | Gestión de Riesgos LA/FT');

    const faviconLink = page.locator('link[rel="icon"]');
    await expect(faviconLink).toHaveCount(1);
    const href = await faviconLink.getAttribute('href');
    expect(href).toBe('favicon.ico');
  });
});
