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
