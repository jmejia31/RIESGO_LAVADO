import { expect, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

type Fixture = { familyId: number; familyCode: string; versionId: number };
type Snapshot = { state: string; vigency: number; hash: string; json: string; updateAuditCount: number; publishAuditCount: number };

test.describe.configure({ retries: 0 });
test.skip(process.env.RL_P0_DELETE_REAL_E2E_REQUIRED !== 'true', 'P0 dirty publication requires the disposable Oracle XE host and dedicated runner.');

function unsignedTestToken(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '987654', uid: 'p0-dirty-publish-e2e', role: 'ADMINISTRADOR', modulos: '10', exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

test('P0 certifica guardado real y publicacion de una definicion modificada', async ({ page, request }) => {
  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/version-publish-dirty-fixture');
  expect(fixtureResponse.status()).toBe(200);
  const fixture = await fixtureResponse.json() as Fixture;
  const snapshot = async (): Promise<Snapshot> => {
    const response = await request.get('http://127.0.0.1:5043/__e2e/version-publish-dirty-snapshot');
    expect(response.status()).toBe(200);
    return await response.json() as Snapshot;
  };

  await page.addInitScript(accessToken => {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', 'p0-dirty-publish-e2e');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, unsignedTestToken());
  await page.goto('/matrices-riesgos');
  const initial = await snapshot();
  expect(initial).toMatchObject({ state: 'DRAFT', vigency: 0, updateAuditCount: 0, publishAuditCount: 0 });
  const originalHash = initial.hash;

  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.locator('#gestor-buscar-familia').fill(fixture.familyCode);
  const familyRow = page.locator('table[data-ui-fam-table="principal"] tbody tr').filter({ hasText: fixture.familyCode });
  await expect(familyRow).toHaveCount(1);
  await familyRow.getByRole('button', { name: 'Ver versiones' }).click();
  const detail = page.locator('dialog[open]').filter({ hasText: 'Detalle de Familia' });
  await detail.locator('table tbody tr').filter({ hasText: 'P0_DIRTY_PUBLISH_V1' }).getByRole('button', { name: 'Editar definiciÃ³n de la versiÃ³n' }).click();
  const builder = page.locator('dialog[open]').filter({ hasText: 'Constructor de Formularios DinÃ¡micos' });
  await expect(builder).toBeVisible();
  const editedTitle = 'Identificación del riesgo GTIC modificado';
  const titleInput = builder.locator('input[id^="seccion-titulo-"]').first();
  await expect(titleInput).toBeVisible();
  await titleInput.fill(editedTitle);

  const putResponse = page.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/formularios/${fixture.versionId}`);
  const publishPost = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/formularios/${fixture.versionId}/publicar`);
  const publishButton = builder.locator('#btn-publicar-builder');
  await expect(publishButton).toBeEnabled();
  await publishButton.click();
  const confirmation = page.locator('.institutional-confirmation');
  await expect(confirmation).toBeVisible();
  const readback = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/formularios/${fixture.versionId}`
    && response.status() === 200);
  await confirmation.getByRole('button', { name: /publicar/i }).click();
  const put = await putResponse;
  expect(put.status()).toBeGreaterThanOrEqual(200);
  expect(put.status()).toBeLessThan(300);
  const post = await publishPost;
  expect(post.status()).toBeGreaterThanOrEqual(200);
  expect(post.status()).toBeLessThan(300);
  await readback;

  await expect(builder.locator('[data-uat="feedback-form-builder-success"]')).toContainText('publicada correctamente');
  const final = await snapshot();
  expect(final.state).toBe('PUBLISHED');
  expect(final.vigency).toBe(1);
  expect(final.hash).not.toBe(originalHash);
  expect(final.json).toContain(editedTitle);
  expect(final.updateAuditCount).toBe(1);
  expect(final.publishAuditCount).toBe(1);

  console.log('P0_PUBLISH_DIRTY_BROWSER=PASS');
  console.log('P0_PUBLISH_DIRTY_PUT_STATUS=2XX');
  console.log('P0_PUBLISH_DIRTY_PUT_READBACK=PASS');
  console.log('P0_PUBLISH_DIRTY_JSON_HASH=PASS');
  console.log('P0_PUBLISH_DIRTY_POST_STATUS=2XX');
  console.log('P0_PUBLISH_DIRTY_STATE=PUBLISHED');
  console.log('P0_PUBLISH_DIRTY_VIGENCY=1');
  console.log('P0_PUBLISH_DIRTY_AUDIT=PASS');
  console.log('P0_PUBLISH_DIRTY_FEEDBACK=PASS');
  console.log('P0_PUBLICATION_DIRTY_ORACLE_E2E=PASS');
});
