import { expect, Page, test } from '@playwright/test';

const systemConfiguration = {
  success: true,
  datos: {
    nombreSistema: 'SGRLA-IHSS',
    nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
    colorPrimario: '#1e3a8a',
    colorSecundario: '#1d4ed8',
    timeoutSesion: 30,
  },
};

async function stubPublicConfiguration(page: Page) {
  await page.route('**/api/configuracion/sistema', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(systemConfiguration),
  }));

  await page.route('**/api/configuracion/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));
}

function createUnsignedAccessToken() {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '27',
    uid: 'e2e.matrices',
    email: 'e2e.matrices@ihss.hn',
    given_name: 'Prueba',
    family_name: 'E2E',
    role: 'ADMINISTRADOR',
    rol_id: '1',
    modulos: '10',
    debe_cambiar_pass: '0',
    exp: Math.floor(Date.now() / 1000) + 3600,
  })}.`;
}

async function stubAuthenticatedMatrices(page: Page) {
  const accessToken = createUnsignedAccessToken();
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'refresh-e2e-local');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, accessToken);

  await page.route('**/api/matrices-riesgos/**', route => {
    const path = new URL(route.request().url()).pathname;
    let datos: unknown = [];
    if (path.endsWith('/metodologia/vigente')) {
      datos = { factores: [], variables: [], escalasCatalogo: [], escalasRiesgo: [], controlesMitigacion: [] };
    } else if (path.endsWith('/dashboard')) {
      datos = { totalMatrices: 0, porNivelResidual: [] };
    } else if (path.endsWith('/reportes')) {
      datos = { totales: { totalMatrices: 0 }, porEstado: [], porNivelInherente: [], porNivelResidual: [], porFactor: [], matricesCriticas: [] };
    }
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });

  await page.route('**/api/matrices-riesgos*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));
}

test.beforeEach(async ({ page }) => {
  await stubPublicConfiguration(page);
});

test('muestra el formulario de acceso institucional', async ({ page }) => {
  await page.goto('/login');

  await expect(page.getByRole('heading', { name: 'Prevención de Lavado' })).toBeVisible();
  await expect(page.getByLabel('Correo o Usuario')).toBeVisible();
  await expect(page.getByLabel('Contraseña')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Iniciar Sesión' })).toBeVisible();
});

test('valida campos obligatorios sin enviar credenciales', async ({ page }) => {
  let loginRequests = 0;
  page.on('request', request => {
    if (/\/api\/auth\/login$/i.test(request.url())) {
      loginRequests++;
    }
  });

  await page.goto('/login');
  await page.getByRole('button', { name: 'Iniciar Sesión' }).click();

  await expect(page.getByText('El correo o usuario es obligatorio')).toBeVisible();
  await expect(page.getByText('La contraseña es obligatoria')).toBeVisible();
  expect(loginRequests).toBe(0);
});

test('permite alternar la visibilidad de la contraseña', async ({ page }) => {
  await page.goto('/login');
  const password = page.getByLabel('Contraseña');

  await password.fill('clave-segura');
  await expect(password).toHaveAttribute('type', 'password');
  await page.locator('#password + button').click();
  await expect(password).toHaveAttribute('type', 'text');
});

test('redirige rutas protegidas al login cuando no existe sesión', async ({ page }) => {
  await page.goto('/matrices-riesgos');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('button', { name: 'Iniciar Sesión' })).toBeVisible();
});

test('redirige rutas desconocidas al login', async ({ page }) => {
  await page.goto('/ruta-inexistente');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Prevención de Lavado' })).toBeVisible();
});

test('abre Matrices de Riesgos con una sesion autenticada y autorizada', async ({ page }) => {
  await stubAuthenticatedMatrices(page);

  await page.goto('/matrices-riesgos');

  await expect(page).toHaveURL(/\/matrices-riesgos$/);
  await expect(page.getByRole('heading', { name: 'Matrices de Riesgos' })).toBeVisible();
  await page.getByRole('button', { name: 'Matrices', exact: true }).click();
  await expect(page.getByText('No hay matrices registradas.')).toBeVisible();
});
