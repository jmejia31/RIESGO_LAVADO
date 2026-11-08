import { expect, test, type BrowserContext, type Page } from '@playwright/test';

const apiBaseUrl = process.env['P06_QA_API_ORIGIN'] ?? 'http://localhost:5043';
const mode = process.env['P06_QA_MODE'] ?? 'PREFLIGHT';
const isRealAuthMode = mode === 'CERTIFICATION' || mode === 'LOCAL_OWNER_MANUAL';
const username = process.env['RL_QA_WEB_USERNAME'] ?? '';
const password = process.env['RL_QA_WEB_PASSWORD'] ?? '';
const expectedRole = process.env['P06_QA_EXPECTED_ROLE'] ?? '';
const expectedModules = parseExpectedModules();

function parseExpectedModules(): number[] {
  const raw = process.env['P06_QA_EXPECTED_MODULE_IDS_JSON'] ?? '';
  if (!raw) return [];
  try {
    const parsed = JSON.parse(raw) as unknown;
    return Array.isArray(parsed) && parsed.every(value => Number.isInteger(value))
      ? parsed as number[]
      : [];
  } catch {
    return [];
  }
}

function requireCertification(): void {
  if (!isRealAuthMode) {
    test.skip(true, 'PREFLIGHT_ONLY: real certification is disabled.');
    return;
  }
  const missing = [
    username && password ? '' : 'credentials',
    expectedRole && expectedModules.length > 0 ? '' : 'permission-contract',
    mode === 'LOCAL_OWNER_MANUAL'
      ? process.env['P06_QA_MANUAL_OWNER_APPROVAL'] === 'YES' ? '' : 'manual-owner-approval'
      : process.env['P06_QA_TRUSTED_CONTEXT'] === 'YES' ? '' : 'trusted-context',
    mode === 'LOCAL_OWNER_MANUAL' || process.env['P06_QA_DB_TARGET_VERIFIED'] === 'YES' ? '' : 'oracle-attestation',
    process.env['P06_QA_WEB_ORIGIN_VERIFIED'] === 'YES' ? '' : 'web-origin-attestation',
  ].filter(Boolean);
  if (missing.length > 0) throw new Error(`BLOCKED_PRECONDITION:${missing.join(',')}`);
}

async function login(page: Page): Promise<void> {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.locator('#email').fill(username);
  await page.locator('#password').fill(password);
  const responsePromise = page.waitForResponse(response => response.url().endsWith('/api/auth/login'));
  await page.getByRole('button', { name: 'Iniciar sesión' }).click();
  expect((await responsePromise).status()).toBe(200);
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
      method: 'POST', credentials: 'include',
      headers: token === undefined ? {} : { 'X-XSRF-TOKEN': token }, body: null,
    });
    return response.status;
  }, { apiBaseUrl, path, token });
}

function cookieHeader(cookies: Awaited<ReturnType<BrowserContext['cookies']>>): string {
  return cookies.map(cookie => `${cookie.name}=${cookie.value}`).join('; ');
}

async function profile(page: Page): Promise<{ status: number; success: boolean; role: string; modules: number[] }> {
  return page.evaluate(async apiBaseUrl => {
    const token = localStorage.getItem('access_token');
    const response = await fetch(`${apiBaseUrl}/api/auth/perfil`, {
      credentials: 'include', headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
    const body = await response.json() as { success?: boolean; datos?: { rol?: string; modulosIds?: number[] } };
    return { status: response.status, success: body.success === true, role: body.datos?.rol ?? '', modules: body.datos?.modulosIds ?? [] };
  }, apiBaseUrl);
}

async function logoutSilently(page: Page): Promise<void> {
  if (mode !== 'CERTIFICATION') return;
  const token = await csrfToken(page);
  if (token) await postFromBrowser(page, '/api/auth/logout', token).catch(() => undefined);
}

test.describe('P06 autenticación real QA', () => {
  test.afterEach(async ({ page }) => logoutSilently(page));

  test('AUTH-01 login, perfil y permisos efectivos', async ({ page }) => {
    requireCertification(); await login(page);
    const result = await profile(page);
    expect(result.status).toBe(200); expect(result.success).toBe(true);
    expect(result.role).toBe(expectedRole);
    expect([...new Set(result.modules)].sort()).toEqual([...new Set(expectedModules)].sort());
  });

  test('AUTH-02 cookies y almacenamiento de sesión', async ({ page, context }) => {
    requireCertification(); await login(page);
    const cookies = await context.cookies();
    const refresh = cookies.find(cookie => cookie.name === 'RL-RefreshToken');
    const internalAntiforgery = cookies.find(cookie => cookie.name === 'RL-Antiforgery');
    const requestToken = cookies.find(cookie => cookie.name === 'XSRF-TOKEN');
    expect(refresh?.httpOnly).toBe(true); expect(refresh?.path).toBe('/api/auth'); expect(refresh?.sameSite).toBe('Strict');
    expect(internalAntiforgery?.httpOnly).toBe(true); expect(internalAntiforgery?.path).toBe('/api/auth');
    expect(requestToken?.httpOnly).toBe(false); expect(requestToken?.path).toBe('/');
    const storage = await page.evaluate(() => ({
      refreshTokenAbsent: localStorage.getItem('refresh_token') === null && sessionStorage.getItem('refresh_token') === null,
      accessTokenPresent: localStorage.getItem('access_token') !== null,
    }));
    expect(storage.refreshTokenAbsent).toBe(true); expect(storage.accessTokenPresent).toBe(true);
  });

  test('AUTH-03 CSRF válido, ausente e inválido', async ({ page }) => {
    requireCertification(); await login(page);
    expect(await postFromBrowser(page, '/api/auth/refresh', 'invalid-p06-csrf')).toBe(400);
    expect(await postFromBrowser(page, '/api/auth/refresh')).toBe(400);
    expect(await postFromBrowser(page, '/api/auth/refresh', await csrfToken(page))).toBe(200);
  });

  test('AUTH-04 refresh, rotación, persistencia y rechazo del anterior', async ({ page, context, request }) => {
    requireCertification(); await login(page);
    const before = await context.cookies();
    const oldRefresh = before.find(cookie => cookie.name === 'RL-RefreshToken');
    const oldXsrf = before.find(cookie => cookie.name === 'XSRF-TOKEN');
    expect(await postFromBrowser(page, '/api/auth/refresh', await csrfToken(page))).toBe(200);
    const after = await context.cookies();
    const newRefresh = after.find(cookie => cookie.name === 'RL-RefreshToken');
    const newXsrf = after.find(cookie => cookie.name === 'XSRF-TOKEN');
    expect(oldRefresh?.value === newRefresh?.value).toBe(false);
    expect(oldXsrf?.value === newXsrf?.value).toBe(false);
    expect((await profile(page)).status).toBe(200);
    const stale = await request.post(`${apiBaseUrl}/api/auth/refresh`, {
      headers: { Cookie: cookieHeader(before), 'X-XSRF-TOKEN': oldXsrf?.value ?? '' },
    });
    expect(stale.status()).toBe(401);
  });

  test('AUTH-05 logout, CSRF y revocación efectiva', async ({ page, context, request }) => {
    requireCertification(); await login(page);
    const before = await context.cookies();
    expect(await postFromBrowser(page, '/api/auth/logout', 'invalid-p06-csrf')).toBe(400);
    expect((await context.cookies()).some(cookie => cookie.name === 'RL-RefreshToken')).toBe(true);
    expect(await postFromBrowser(page, '/api/auth/logout', await csrfToken(page))).toBe(200);
    expect((await context.cookies()).some(cookie => cookie.name === 'RL-RefreshToken')).toBe(false);
    const revoked = await request.post(`${apiBaseUrl}/api/auth/refresh`, {
      headers: { Cookie: cookieHeader(before), 'X-XSRF-TOKEN': before.find(cookie => cookie.name === 'XSRF-TOKEN')?.value ?? '' },
    });
    expect(revoked.status()).toBe(401);
  });

  test('AUTH-06A concurrencia backend sin errores internos', async ({ page }) => {
    requireCertification(); await login(page);
    const token = await csrfToken(page);
    const statuses = await page.evaluate(async ({ apiBaseUrl, token }) => {
      const responses = await Promise.all([
        fetch(`${apiBaseUrl}/api/auth/refresh`, { method: 'POST', credentials: 'include', headers: { 'X-XSRF-TOKEN': token } }),
        fetch(`${apiBaseUrl}/api/auth/refresh`, { method: 'POST', credentials: 'include', headers: { 'X-XSRF-TOKEN': token } }),
      ]);
      return responses.map(response => response.status);
    }, { apiBaseUrl, token });
    expect(statuses.some(status => status === 200)).toBe(true);
    expect(statuses.every(status => status === 200 || status === 401)).toBe(true);
  });
});
