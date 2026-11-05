import { expect, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

type PagedResponse = {
  success: boolean;
  datos?: { items?: unknown[]; totalRegistros?: number; totalPaginas?: number };
};

// This test is called only by the existing disposable Oracle XE runner.
// A successful family-only empty state is not full Matrices-module certification.
test.describe.configure({ retries: 0 });
test.skip(
  process.env['RL_MATRICES_SYNTHETIC_PURGE_REQUIRED'] !== 'true',
  'GATE02 requires the disposable Oracle XE synthetic purge runner.'
);

function unsignedTestToken(): string {
  const encode = (value: object): string => Buffer.from(JSON.stringify(value)).toString('base64url');
  return encode({ alg: 'none', typ: 'JWT' }) + '.' + encode({
    nameid: '987654',
    uid: 'g02-disposable-purge',
    role: 'ADMINISTRADOR',
    modulos: '10',
    exp: Math.floor(Date.now() / 1000) + 3600
  }) + '.';
}

test('GATE02 verifica el estado vacio de una familia eliminada desde la API y Angular reales', async ({ page, request }) => {
  const code = process.env['RL_G02_PURGED_FAMILY_CODE'] ?? '';
  const idText = process.env['RL_G02_PURGED_FAMILY_ID'] ?? '';
  expect(code).toMatch(/^SYNTH_PURGE_[A-F0-9]{12}$/);
  expect(idText).toMatch(/^[0-9]+$/);
  const id = Number(idText);
  expect(Number.isSafeInteger(id) && id > 0).toBe(true);

  // Reuse the exact identity emitted by the database test AFTER its cleanup.
  const apiBase = 'http://127.0.0.1:5043/api/matrices-riesgos/';
  const listUrl = apiBase + 'familias/paginado?buscar=' + encodeURIComponent(code) + '&archivado=TODAS';
  const list = await request.get(listUrl);
  expect(list.status()).toBe(200);
  const response = await list.json() as PagedResponse;
  expect(response.success).toBe(true);
  expect(response.datos?.items).toEqual([]);
  expect(response.datos?.totalRegistros).toBe(0);

  const detail = await request.get(apiBase + 'familias/' + id);
  expect(detail.status()).toBe(404);
  console.log('GATE02_POST_PURGE_FAMILY_API=PASS');

  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'g02-disposable-only');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, unsignedTestToken());
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();

  const search = page.locator('#gestor-buscar-familia');
  await expect(search).toBeVisible();
  await search.fill(code);

  // The current institutional UI applies the search when the tab reloads.
  const filteredResponse = page.waitForResponse(httpResponse =>
    httpResponse.request().method() === 'GET'
    && new URL(httpResponse.url()).pathname === '/api/matrices-riesgos/familias/paginado'
    && new URL(httpResponse.url()).searchParams.get('buscar') === code);
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  const filtered = await filteredResponse;
  expect(filtered.status()).toBe(200);
  const filteredBody = await filtered.json() as PagedResponse;
  expect(filteredBody.datos?.items).toEqual([]);
  expect(filteredBody.datos?.totalRegistros).toBe(0);

  const table = page.locator('table[data-ui-fam-table="principal"]');
  await expect(table).toBeVisible();
  await expect(table.locator('tbody')).toContainText('No hay familias que coincidan con los filtros.');
  await expect(table.locator('tbody tr').filter({ hasText: code })).toHaveCount(0);
  console.log('GATE02_POST_PURGE_FAMILY_ANGULAR=PASS');
});
