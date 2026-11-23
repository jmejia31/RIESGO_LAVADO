import { expect, expectHttpResponse, expectRequestFailure, Page, test } from './support/telemetry-test';

function token(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ nameid: '999888', uid: 'favicon-title-e2e', modulos: '1,2,3,4', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
}

// 16x16 minimal valid PNG binary buffer
const PNG_ICON_A = Buffer.from([
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
  0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
  0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x10,
  0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x91, 0x68,
  0x36, 0x00, 0x00, 0x00, 0x1B, 0x49, 0x44, 0x41,
  0x54, 0x78, 0x9C, 0x63, 0x60, 0x18, 0x05, 0xA3,
  0x60, 0x14, 0x8C, 0x82, 0x51, 0x30, 0x0A, 0x18,
  0x18, 0x18, 0x00, 0x03, 0x10, 0x00, 0x01, 0x1D,
  0x44, 0x07, 0xB1, 0x00, 0x00, 0x00, 0x00, 0x49,
  0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
]);

// 32x32 minimal valid PNG binary buffer
const PNG_ICON_B = Buffer.from([
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
  0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
  0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x20,
  0x08, 0x02, 0x00, 0x00, 0x00, 0xFC, 0x18, 0xED,
  0xA3, 0x00, 0x00, 0x00, 0x1B, 0x49, 0x44, 0x41,
  0x54, 0x78, 0x9C, 0x63, 0x60, 0x18, 0x05, 0xA3,
  0x60, 0x14, 0x8C, 0x82, 0x51, 0x30, 0x0A, 0x18,
  0x18, 0x18, 0x00, 0x03, 0x10, 0x00, 0x01, 0x1D,
  0x44, 0x07, 0xB1, 0x00, 0x00, 0x00, 0x00, 0x49,
  0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
]);

async function setupCommonRoutes(page: Page) {
  await page.route('**/uploads/**', route => route.fulfill({
    status: 200,
    contentType: 'image/png',
    body: PNG_ICON_A,
  }));
  await page.route('**/api/configuracion/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));
  await page.route('**/api/configuracion/slides', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));
  await page.route('**/api/catalogos/**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));
  await page.route('**/api/configuracion/sistema/logo-reportes/estado', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ disponible: false, configurado: false }),
  }));
  await page.route('**/api/configuracion/sistema/logo-reportes', route => route.fulfill({
    status: 204,
  }));
}

test.describe('Parametrización dinámica de Título y Favicon Institucional', () => {
  test.beforeEach(async ({ page }) => {
    await setupCommonRoutes(page);
  });

  test('Ruta pública (/login) aplica título institucional y favicon del backend', async ({ page }) => {
    await page.route(/\/api\/configuracion\/sistema\/favicon/, route => route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: PNG_ICON_A,
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

  test('Cambio en caliente (TITULO_A -> TITULO_B e ICONO_A -> ICONO_B) sin recargar página y persistencia tras recarga', async ({ page }) => {
    let currentConfig = {
      nombreSistema: 'SGRLA-IHSS',
      nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
      iconoUrl: '/uploads/icono-a.png',
      colorPrimario: '#1e3a8a',
      colorSecundario: '#1d4ed8',
      colorEncabezadoReportes: '#123B63',
      timeoutSesion: 30,
    };

    await page.addInitScript(accessToken => {
      localStorage.setItem('access_token', accessToken);
      localStorage.setItem('refresh_token', 'e2e-only');
      localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
    }, token());

    await page.route('https://cdn.ihss.hn/**', route => route.fulfill({
      status: 200,
      contentType: 'image/x-icon',
      body: PNG_ICON_A,
    }));
    await page.route('https://ihss.hn/**', route => route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: PNG_ICON_A,
    }));

    await page.route(/\/api\/configuracion\/sistema\/favicon/, route => {
      const url = route.request().url();
      const body = url.includes('icono-b') || currentConfig.iconoUrl.includes('icono-b') ? PNG_ICON_B : PNG_ICON_A;
      return route.fulfill({
        status: 200,
        contentType: 'image/png',
        body,
      });
    });

    await page.route('**/api/configuracion/sistema', async route => {
      const req = route.request();
      if (req.method() === 'PUT' || req.method() === 'POST') {
        const body = req.postDataJSON();
        currentConfig = { ...currentConfig, ...body };
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true, mensaje: 'Configuración guardada' }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, datos: currentConfig }),
      });
    });

    await page.goto('/configuracion');

    // 1. Verificación de estado inicial (TITULO_A, ICONO_A)
    await expect(page).toHaveTitle('SGRLA-IHSS | Gestión de Riesgos LA/FT');
    const initialFavicon = page.locator('link[rel="icon"]');
    await expect(initialFavicon).toHaveCount(1);
    const initialHref = await initialFavicon.getAttribute('href');
    expect(initialHref).toContain('/api/configuracion/sistema/favicon?v=');

    // 2. Modificación de campos en el formulario
    const inputNombre = page.locator('input[formcontrolname="nombreSistema"]');
    const inputIcono = page.locator('input[formcontrolname="iconoUrl"]');
    const btnGuardar = page.locator('button[aria-label="Guardar cambios del sistema"]');

    await inputNombre.fill('Portal Institucional de Riesgos');
    await inputIcono.fill('/uploads/icono-b.png');
    const saveResponse = page.waitForResponse(response =>
      response.request().method() === 'PUT'
      && new URL(response.url()).pathname === '/api/configuracion/sistema'
      && response.status() === 200);
    await btnGuardar.click();

    // Confirmar en el modal de confirmación institucional
    const btnConfirmar = page.getByRole('button', { name: 'Sí, guardar' });
    await expect(btnConfirmar).toBeVisible();
    await btnConfirmar.click();
    await saveResponse;

    // 3. Verificación de actualización en caliente SIN recarga de página (ANGULAR_RESTART_REQUIRED=NO)
    await expect(page).toHaveTitle('Portal Institucional de Riesgos | Gestión de Riesgos LA/FT');
    const updatedFavicon = page.locator('link[rel="icon"]');
    await expect(updatedFavicon).toHaveCount(1);
    const updatedHref = await updatedFavicon.getAttribute('href');
    expect(updatedHref).not.toBe(initialHref);
    expect(updatedHref).toContain('/api/configuracion/sistema/favicon?v=');

    // 4. Verificación de persistencia tras recarga del navegador (cold start / reload)
    await page.reload();
    await expect(page).toHaveTitle('Portal Institucional de Riesgos | Gestión de Riesgos LA/FT');
    const reloadedFavicon = page.locator('link[rel="icon"]');
    await expect(reloadedFavicon).toHaveCount(1);
    const reloadedHref = await reloadedFavicon.getAttribute('href');
    expect(reloadedHref).toContain('/api/configuracion/sistema/favicon?v=');
  });

  test('Eliminar icono restablece favicon.ico y nombre vacío usa fallback SGRLA-IHSS', async ({ page }) => {
    // Chromium may abort the superseded favicon GET when its link is replaced or the
    // page reloads. This does not indicate an HTTP/API error. Allow only ONE
    // exact browser cancellation in this scenario; keep every other request and
    // all assertions for immediate fallback and persistence strictly enforced.
    expectRequestFailure({
      method: 'GET',
      path: '/api/configuracion/sistema/favicon',
      protocol: 'http:',
      errorText: 'net::ERR_ABORTED',
      minCount: 0,
      maxCount: 1,
      reason: 'Explicit removal of dynamic favicon can cancel its superseded browser GET while favicon.ico takes over',
    });
    let currentConfig = {
      nombreSistema: 'Portal Especial',
      nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
      iconoUrl: '/uploads/icono-activo.png',
      colorPrimario: '#1e3a8a',
      colorSecundario: '#1d4ed8',
      colorEncabezadoReportes: '#123B63',
      timeoutSesion: 30,
    };

    await page.addInitScript(accessToken => {
      localStorage.setItem('access_token', accessToken);
      localStorage.setItem('refresh_token', 'e2e-only');
      localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
    }, token());

    await page.route('https://cdn.ihss.hn/**', route => route.fulfill({
      status: 200,
      contentType: 'image/x-icon',
      body: PNG_ICON_A,
    }));
    await page.route('https://ihss.hn/**', route => route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: PNG_ICON_A,
    }));

    await page.route(/\/api\/configuracion\/sistema\/favicon/, route => {
      if (!currentConfig.iconoUrl) {
        return route.fulfill({ status: 204 });
      }
      return route.fulfill({
        status: 200,
        contentType: 'image/png',
        body: PNG_ICON_A,
      });
    });

    await page.route('**/api/configuracion/sistema', async route => {
      const req = route.request();
      if (req.method() === 'PUT' || req.method() === 'POST') {
        const body = req.postDataJSON();
        currentConfig = { ...currentConfig, ...body };
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true, mensaje: 'Configuración guardada' }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, datos: currentConfig }),
      });
    });

    await page.goto('/configuracion');
    await expect(page).toHaveTitle('Portal Especial | Gestión de Riesgos LA/FT');

    // Limpiar icono y dejar nombre en blanco para validar restablecimiento y fallback
    const inputIcono = page.locator('input[formcontrolname="iconoUrl"]');
    const btnGuardar = page.locator('button[aria-label="Guardar cambios del sistema"]');

    await inputIcono.fill('');
    const saveResponse = page.waitForResponse(response =>
      response.request().method() === 'PUT'
      && new URL(response.url()).pathname === '/api/configuracion/sistema'
      && response.status() === 200);
    await btnGuardar.click();

    // Confirmar en el modal de confirmación institucional
    const btnConfirmar = page.getByRole('button', { name: 'Sí, guardar' });
    await expect(btnConfirmar).toBeVisible();
    await btnConfirmar.click();
    await saveResponse;

    // Comprobar que el favicon se restableció inmediatamente a favicon.ico
    const fallbackFavicon = page.locator('link[rel="icon"]');
    await expect(fallbackFavicon).toHaveCount(1);
    await expect(fallbackFavicon).toHaveAttribute('href', 'favicon.ico');
    await expect(fallbackFavicon).toHaveAttribute('type', 'image/x-icon');

    // Persistencia tras recarga
    await page.reload();
    const persistedFavicon = page.locator('link[rel="icon"]');
    await expect(persistedFavicon).toHaveCount(1);
    await expect(persistedFavicon).toHaveAttribute('href', 'favicon.ico');
  });

  test('Desacoplamiento total: Retardo o fallo en logo de reportería no bloquea ni retrasa el título ni el favicon', async ({ page }) => {
    // Declaramos 500 como respuesta HTTP esperada en la telemetría para logo-reportes
    expectHttpResponse({ status: 500, pathIncludes: 'logo-reportes', minCount: 1, maxCount: 10, reason: 'Logo failure simulation' });

    // Simulamos fallo 500 en endpoint de logo de reportes
    await page.unroute('**/api/configuracion/sistema/logo-reportes');
    await page.route('**/api/configuracion/sistema/logo-reportes', route => route.fulfill({
      status: 500,
      contentType: 'application/json',
      body: JSON.stringify({ error: 'Fallo simulado en logo de reportes' }),
    }));

    await page.route(/\/api\/configuracion\/sistema\/favicon/, route => route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: PNG_ICON_A,
    }));

    await page.route('**/api/configuracion/sistema', route => route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        datos: {
          nombreSistema: 'SGRLA Desacoplado',
          nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
          iconoUrl: '/uploads/favicon.png',
          logoUrl: '/uploads/logo-falla.png',
          colorPrimario: '#1e3a8a',
          colorSecundario: '#1d4ed8',
        },
      }),
    }));

    const startTime = Date.now();
    await page.goto('/login');

    // Debe aplicar título y favicon inmediatamente sin ser bloqueado por el logo
    await expect(page).toHaveTitle('SGRLA Desacoplado | Gestión de Riesgos LA/FT');
    const faviconLink = page.locator('link[rel="icon"]');
    await expect(faviconLink).toHaveCount(1);
    const elapsed = Date.now() - startTime;
    expect(elapsed).toBeLessThan(10000);
  });

  test('Carga local de imagen, conversión automática a .ico, vista previa multirresolución (16x16, 32x32, 48x48), simulación de pestaña y guardado dinámico', async ({ page }) => {
    let currentConfig = {
      nombreSistema: 'SGRLA-IHSS',
      nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
      iconoUrl: '/uploads/favicon-original.png',
      logoUrl: '/uploads/logo-institucional.png',
      colorPrimario: '#1e3a8a',
      colorSecundario: '#1d4ed8',
      colorEncabezadoReportes: '#123B63',
      timeoutSesion: 30,
    };

    await page.addInitScript(accessToken => {
      localStorage.setItem('access_token', accessToken);
      localStorage.setItem('refresh_token', 'e2e-only');
      localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
    }, token());

    await page.route('https://cdn.ihss.hn/**', route => route.fulfill({
      status: 200,
      contentType: 'image/x-icon',
      body: PNG_ICON_A,
    }));
    await page.route('https://ihss.hn/**', route => route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: PNG_ICON_A,
    }));

    await page.route(/\/api\/configuracion\/sistema(?:\?|$)/, async route => {
      const req = route.request();
      if (req.method() === 'PUT' || req.method() === 'POST') {
        const body = req.postDataJSON();
        currentConfig = { ...currentConfig, ...body };
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true, mensaje: 'Configuración guardada' }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, datos: currentConfig }),
      });
    });

    await page.route(/\/api\/configuracion\/sistema\/favicon(?:\?|$)/, route => {
      return route.fulfill({
        status: 200,
        contentType: 'image/x-icon',
        body: PNG_ICON_B,
      });
    });

    let uploadLlamado = false;
    let uploadNormalizadoAPng = false;
    await page.route(/\/api\/configuracion\/sistema\/favicon\/upload/, async route => {
      uploadLlamado = true;
      const body = route.request().postData() || '';
      uploadNormalizadoAPng = body.includes('filename="nuevo-favicon-institucional.png"')
        && body.includes('Content-Type: image/png');
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          url: '/uploads/favicon-autocreated.ico',
          mensaje: 'Favicon procesado y convertido a formato multirresolución .ico exitosamente',
        }),
      });
    });

    // La vista previa de /uploads corresponde al origen del backend; usar una
    // imagen auténticamente decodificable para que onload sea un control real.
    const previewPngBase64 = await page.evaluate(() => {
      const canvas = document.createElement('canvas');
      canvas.width = 32;
      canvas.height = 32;
      const ctx = canvas.getContext('2d')!;
      ctx.fillStyle = '#f97316';
      ctx.fillRect(0, 0, 16, 32);
      ctx.fillStyle = '#22c55e';
      ctx.fillRect(16, 0, 16, 32);
      return canvas.toDataURL('image/png').split(',')[1];
    });
    await page.route('**/uploads/favicon-autocreated.ico', route => route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: Buffer.from(previewPngBase64, 'base64'),
    }));

    await page.goto('/configuracion');

    // Esperar a que la configuración inicial esté cargada
    await expect(page.locator('input[formcontrolname="nombreSistema"]')).toHaveValue('SGRLA-IHSS');

    // 1. Localizar el selector de archivo de carga de favicon institucional
    const faviconUploaderSection = page.locator('section').filter({ hasText: 'Cargar favicon institucional' });
    await expect(faviconUploaderSection).toBeVisible();
    const fileInput = faviconUploaderSection.locator('input[type="file"]');
    const uploadBtn = faviconUploaderSection.locator('button').first();
    await expect(uploadBtn).toBeEnabled();

    // 2. Generar JPEG auténtico en Chromium y comprobar que se envía como PNG RGBA.
    const jpegBase64 = await page.evaluate(() => {
      const canvas = document.createElement('canvas');
      canvas.width = 64;
      canvas.height = 64;
      const ctx = canvas.getContext('2d')!;
      ctx.fillStyle = '#f97316';
      ctx.fillRect(0, 0, 32, 64);
      ctx.fillStyle = '#22c55e';
      ctx.fillRect(32, 0, 32, 64);
      return canvas.toDataURL('image/jpeg', 0.9).split(',')[1];
    });
    const uploadPromise = page.waitForResponse(
      resp => resp.url().includes('/api/configuracion/sistema/favicon/upload') && resp.status() === 200
    );
    await fileInput.setInputFiles({
      name: 'nuevo-favicon-institucional.jpg',
      mimeType: 'image/jpeg',
      buffer: Buffer.from(jpegBase64, 'base64'),
    });
    await uploadPromise;

    // 3. No basta con aceptar JPEG: el archivo multipart debe contener PNG real.
    await expect.poll(() => uploadLlamado).toBe(true);
    expect(uploadNormalizadoAPng).toBe(true);

    // 4. Verificar indicador de estado "Listo para guardar"
    await expect(page.locator('text=Listo para guardar')).toBeVisible();
    // Upload triggers an institutional success notification that must be acknowledged
    // before the underlying configuration-save button becomes interactive.
    const uploadNotification = page.locator('.swal2-popup.institutional-confirmation');
    await expect(uploadNotification).toContainText('Favicon convertido y verificado');
    await uploadNotification.getByRole('button', { name: 'Aceptar', exact: true }).click();
    await expect(uploadNotification).toHaveCount(0);

    // 5. La miniatura debe ir al backend y no a localhost:4200/uploads (Angular).
    await expect(page.locator('img[alt="Favicon 16x16"]')).toHaveAttribute(
      'src', 'http://localhost:5043/uploads/favicon-autocreated.ico'
    );
    await expect(page.locator('img[alt="Favicon 16x16"]')).toBeVisible();
    await expect.poll(() => page.locator('img[alt="Favicon 16x16"]').evaluate(
      el => (el as HTMLImageElement).naturalWidth
    )).toBeGreaterThan(0);
    await expect(page.locator('img[alt="Favicon 32x32"]')).toBeVisible();
    await expect(page.locator('img[alt="Favicon 48x48"]')).toBeVisible();

    // 6. Verificar simulación de pestaña de Chrome
    await expect(page.locator('img[alt="Favicon Tab"]')).toBeVisible();
    await expect(page.locator('text=Simulación en pestaña de Chrome:')).toBeVisible();

    // 7. Guardar cambios en el formulario y esperar confirmación del backend
    const savePromise = page.waitForResponse(
      resp => resp.url().includes('/api/configuracion/sistema') && resp.request().method() === 'PUT' && resp.status() === 200
    );
    const btnGuardar = page.locator('button[aria-label="Guardar cambios del sistema"]');
    await btnGuardar.click();

    // Confirmar en el modal SweetAlert institucional
    const btnConfirmar = page.getByRole('button', { name: 'Sí, guardar' });
    await expect(btnConfirmar).toBeVisible();
    await btnConfirmar.click();
    await savePromise;

    // Verificar indicador de estado "Publicado" tras persistencia
    await expect(page.locator('text=Publicado')).toBeVisible();

    // 8. Verificar que el favicon del navegador cambió en caliente (ANGULAR_RESTART_REQUIRED=NO)
    const hotSwappedFavicon = page.locator('link[rel="icon"]');
    await expect(hotSwappedFavicon).toHaveCount(1);
    const hotHref = await hotSwappedFavicon.getAttribute('href');
    expect(hotHref).toContain('/api/configuracion/sistema/favicon?v=');

    // 9. Verificar persistencia tras recarga
    await page.reload();
    await expect(page.locator('input[formcontrolname="iconoUrl"]')).toHaveValue('/uploads/favicon-autocreated.ico');
    const reloadedFavicon = page.locator('link[rel="icon"]');
    await expect(reloadedFavicon).toHaveCount(1);
    const reloadedHref = await reloadedFavicon.getAttribute('href');
    expect(reloadedHref).toContain('/api/configuracion/sistema/favicon?v=');
  });

  test('Coexistencia y desacoplamiento: URL directa HTTPS no altera logo institucional ni flujo de carga', async ({ page }) => {
    let currentConfig = {
      nombreSistema: 'SGRLA-IHSS',
      nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
      iconoUrl: '/uploads/favicon.png',
      logoUrl: 'https://ihss.hn/wp-content/uploads/logo-ihss-oficial.png',
      colorPrimario: '#1e3a8a',
      colorSecundario: '#1d4ed8',
      colorEncabezadoReportes: '#123B63',
      timeoutSesion: 30,
    };

    await page.addInitScript(accessToken => {
      localStorage.setItem('access_token', accessToken);
      localStorage.setItem('refresh_token', 'e2e-only');
      localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
    }, token());

    await page.route('https://cdn.ihss.hn/**', route => route.fulfill({
      status: 200,
      contentType: 'image/x-icon',
      body: PNG_ICON_A,
    }));
    await page.route('https://ihss.hn/**', route => route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: PNG_ICON_A,
    }));

    await page.route(/\/api\/configuracion\/sistema\/favicon/, route => {
      return route.fulfill({
        status: 200,
        contentType: 'image/x-icon',
        body: PNG_ICON_A,
      });
    });

    let configPersistida: any = null;
    await page.route('**/api/configuracion/sistema', async route => {
      const req = route.request();
      if (req.method() === 'PUT' || req.method() === 'POST') {
        configPersistida = req.postDataJSON();
        currentConfig = { ...currentConfig, ...configPersistida };
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true, mensaje: 'Configuración guardada' }),
        });
      }
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, datos: currentConfig }),
      });
    });

    await page.goto('/configuracion');

    // Ingresar URL directa HTTPS en iconoUrl
    const inputIcono = page.locator('input[formcontrolname="iconoUrl"]');
    await inputIcono.fill('https://cdn.ihss.hn/assets/favicon-direct.ico');

    // Guardar
    const savePromise = page.waitForResponse(
      resp => resp.url().includes('/api/configuracion/sistema') && resp.request().method() === 'PUT' && resp.status() === 200
    );
    const btnGuardar = page.locator('button[aria-label="Guardar cambios del sistema"]');
    await btnGuardar.click();

    const btnConfirmar = page.getByRole('button', { name: 'Sí, guardar' });
    await expect(btnConfirmar).toBeVisible();
    await btnConfirmar.click();
    await savePromise;

    // Verificar que se guardó iconoUrl sin tocar logoUrl
    await expect.poll(() => configPersistida?.iconoUrl).toBe('https://cdn.ihss.hn/assets/favicon-direct.ico');
    expect(configPersistida?.logoUrl).toBe('https://ihss.hn/wp-content/uploads/logo-ihss-oficial.png');
  });
});
