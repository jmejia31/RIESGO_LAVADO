import { expect, test, type APIRequestContext, type BrowserContext, type Page } from '@playwright/test';

const apiBaseUrl = 'http://localhost:5043';
const username = process.env['RL_QA_WEB_USERNAME'] ?? '';
const password = process.env['RL_QA_WEB_PASSWORD'] ?? '';
const credentialsAvailable = username.length > 0 && password.length > 0;

function authBlocked(): void {
  test.skip(!credentialsAvailable, 'BLOCKED_CREDENTIAL_BOUNDARY: credentials must be supplied by the trusted QA owner context.');
}

async function login(page: Page): Promise<void> {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.locator('#email').fill(username);
  await page.locator('#password').fill(password);
  const responsePromise = page.waitForResponse(response => response.url().endsWith('/api/auth/login'));
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  const response = await responsePromise;
  expect(response.status()).toBe(200);
  await expect(page).toHaveURL(/\/home(?:\?.*)?$/);
}

async function csrfToken(page: Page): Promise<string> {
  return page.evaluate(() => {
    const item = document.cookie.split('; ').find(value => value.startsWith('XSRF-TOKEN='));
    return item ? decodeURIComponent(item.slice('XSRF-TOKEN='.length)) : '';
  });
}

async function postFromBrowser(page: Page, path: string, token?: string): Promise<number> {
  return page.evaluate(async ({ apiBaseUrl, path, token }) => {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      method: 'POST',
      credentials: 'include',
      headers: token === undefined ? {} : { 'X-XSRF-TOKEN': token },
      body: null,
    });
    return response.status;
  }, { apiBaseUrl, path, token });
}

function cookieHeader(cookies: Awaited<ReturnType<BrowserContext['cookies']>>): string {
  return cookies.map(cookie => `${cookie.name}=${cookie.value}`).join('; ');
}

async function logoutSilently(page: Page): Promise<void> {
  const token = await csrfToken(page);
  if (!token) return;
  await postFromBrowser(page, '/api/auth/logout', token).catch(() => undefined);
}

test.describe('P06 autenticación real QA', () => {
  test.afterEach(async ({ page }) => {
    if (credentialsAvailable) await logoutSilently(page);
  });

  test('AUTH-01 login real y perfil efectivo', async ({ page }) => {
    authBlocked();
    await login(page);
    const profile = await page.evaluate(async apiBaseUrl => {
      const token = localStorage.getItem('access_token');
      const response = await fetch(`${apiBaseUrl}/api/auth/perfil`, {
        credentials: 'include',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      const body = await response.json() as { success?: boolean };
      return { status: response.status, success: body.success === true };
    }, apiBaseUrl);
    expect(profile.status).toBe(200);
    expect(profile.success).toBe(true);
  });

  test('AUTH-02 cookies y almacenamiento de sesión', async ({ page, context }) => {
    authBlocked();
    await login(page);
    const cookies = await context.cookies();
    const refresh = cookies.find(cookie => cookie.name === 'RL-RefreshToken');
    const internalAntiforgery = cookies.find(cookie => cookie.name === 'RL-Antiforgery');
    const requestToken = cookies.find(cookie => cookie.name === 'XSRF-TOKEN');
    expect(refresh?.httpOnly).toBe(true);
    expect(refresh?.path).toBe('/api/auth');
    expect(refresh?.sameSite).toBe('Strict');
    expect(internalAntiforgery?.httpOnly).toBe(true);
    expect(internalAntiforgery?.path).toBe('/api/auth');
    expect(requestToken?.httpOnly).toBe(false);
    expect(requestToken?.path).toBe('/');
    const storage = await page.evaluate(() => ({
      refreshTokenAbsent: localStorage.getItem('refresh_token') === null && sessionStorage.getItem('refresh_token') === null,
      accessTokenPresent: localStorage.getItem('access_token') !== null,
    }));
    expect(storage.refreshTokenAbsent).toBe(true);
    expect(storage.accessTokenPresent).toBe(true);
  });

  test('AUTH-03 CSRF válido, ausente e inválido', async ({ page }) => {
    authBlocked();
    await login(page);
    expect(await postFromBrowser(page, '/api/auth/refresh', 'invalid-p06-csrf')).toBeGreaterThanOrEqual(400);
    expect(await postFromBrowser(page, '/api/auth/refresh')).toBeGreaterThanOrEqual(400);
    expect(await postFromBrowser(page, '/api/auth/refresh', await csrfToken(page))).toBe(200);
  });

  test('AUTH-04 refresh real y rotación de cookie', async ({ page, context }) => {
    authBlocked();
    await login(page);
    const before = await context.cookies();
    const status = await postFromBrowser(page, '/api/auth/refresh', await csrfToken(page));
    const after = await context.cookies();
    expect(status).toBe(200);
    const oldRefresh = before.find(cookie => cookie.name === 'RL-RefreshToken');
    const newRefresh = after.find(cookie => cookie.name === 'RL-RefreshToken');
    expect(oldRefresh).toBeDefined();
    expect(newRefresh).toBeDefined();
    expect(oldRefresh?.value === newRefresh?.value).toBe(false);
  });

  test('AUTH-05 logout real y revocación', async ({ page, context, request }) => {
    authBlocked();
    await login(page);
    const before = await context.cookies();
    expect(await postFromBrowser(page, '/api/auth/logout', await csrfToken(page))).toBe(200);
    expect((await context.cookies()).some(cookie => cookie.name === 'RL-RefreshToken')).toBe(false);
    const revoked = await request.post(`${apiBaseUrl}/api/auth/refresh`, {
      headers: {
        Cookie: cookieHeader(before),
        'X-XSRF-TOKEN': (before.find(cookie => cookie.name === 'XSRF-TOKEN')?.value ?? ''),
      },
    });
    expect(revoked.status()).toBe(401);
  });

  test('AUTH-06 errores limitados y concurrencia controlada', async ({ page, request }) => {
    authBlocked();
    const invalid = await request.post(`${apiBaseUrl}/api/auth/login`, {
      data: { email: '__p06_nonexistent_account__', password: '__invalid_p06_password__' },
    });
    expect([400, 401]).toContain(invalid.status());
    await login(page);
    const token = await csrfToken(page);
    const statuses = await page.evaluate(async ({ apiBaseUrl, token }) => {
      const responses = await Promise.all([
        fetch(`${apiBaseUrl}/api/auth/refresh`, { method: 'POST', credentials: 'include', headers: { 'X-XSRF-TOKEN': token } }),
        fetch(`${apiBaseUrl}/api/auth/refresh`, { method: 'POST', credentials: 'include', headers: { 'X-XSRF-TOKEN': token } }),
      ]);
      return responses.map(response => response.status);
    }, { apiBaseUrl, token });
    expect(statuses.some(status => status === 200)).toBe(true);
    expect(statuses.every(status => status < 500)).toBe(true);
  });
});
