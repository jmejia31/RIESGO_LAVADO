import { expect, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

type PublishFixture = { familyId: number; familyCode: string; versionId: number };
type PublishSnapshot = { familyId: number; familyCode: string; state: string; vigency: number; auditCount: number };

test.describe.configure({ retries: 0 });
test.skip(process.env.RL_P0_DELETE_REAL_E2E_REQUIRED !== 'true', 'P0 direct publication requires the disposable Oracle XE host and dedicated runner.');

function unsignedTestToken(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '987654', uid: 'p0-publish-e2e', role: 'ADMINISTRADOR', modulos: '10', exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

test('P0 certifica publicacion directa de DRAFT mediante Angular, API y Oracle XE', async ({ page, request }) => {
  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/version-publish-fixture');
  expect(fixtureResponse.status()).toBe(200);
  const fixture = await fixtureResponse.json() as PublishFixture;
  const snapshot = async (): Promise<PublishSnapshot> => {
    const response = await request.get('http://127.0.0.1:5043/__e2e/version-publish-snapshot');
    expect(response.status()).toBe(200);
    return await response.json() as PublishSnapshot;
  };

  await page.addInitScript(accessToken => {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', 'p0-publish-e2e');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, unsignedTestToken());
  await page.goto('/matrices-riesgos');
  const initial = await snapshot();
  expect(initial).toMatchObject({ familyId: fixture.familyId, familyCode: fixture.familyCode, state: 'DRAFT', vigency: 0, auditCount: 0 });

  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.locator('#gestor-buscar-familia').fill(fixture.familyCode);
  const familyRow = page.locator('table[data-ui-fam-table="principal"] tbody tr').filter({ hasText: fixture.familyCode });
  await expect(familyRow).toHaveCount(1);
  await familyRow.getByRole('button', { name: 'Ver versiones' }).click();
  const detail = page.locator('dialog[open]').filter({ hasText: 'Detalle de Familia' });
  const versionRow = detail.locator('table tbody tr').filter({ hasText: 'P0_PUBLISH_V1' });
  await expect(versionRow).toContainText('BORRADOR');
  await versionRow.getByRole('button', { name: 'Editar definición de la versión' }).click();

  const builder = page.locator('dialog[open]').filter({ hasText: 'Constructor de Formularios Dinámicos' });
  await expect(builder).toBeVisible();
  const confirmation = page.locator('.institutional-confirmation');
  const publishPost = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/formularios/${fixture.versionId}/publicar`);
  const publishButton = builder.locator('#btn-publicar-builder');
  await expect(publishButton).toBeVisible();
  await expect(publishButton).toBeEnabled();
  await publishButton.click();
  await expect(confirmation).toBeVisible();
  await expect(confirmation).toContainText(String(fixture.versionId));
  const readback = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/formularios/${fixture.versionId}`
    && response.status() === 200);
  await confirmation.getByRole('button', { name: /publicar/i }).click();
  const post = await publishPost;
  expect(post.status()).toBeGreaterThanOrEqual(200);
  expect(post.status()).toBeLessThan(300);

  await readback;
  await expect(builder.locator('[data-uat="feedback-form-builder-success"]')).toContainText('publicada correctamente');
  const final = await snapshot();
  expect(final).toMatchObject({ familyId: fixture.familyId, familyCode: fixture.familyCode, state: 'PUBLISHED', vigency: 1, auditCount: 1 });

  console.log('P0_PUBLISH_DIRECT_BROWSER=PASS');
  console.log('P0_PUBLISH_DIRECT_ANGULAR=PASS');
  console.log('P0_PUBLISH_DIRECT_API=PASS');
  console.log('P0_PUBLISH_DIRECT_ORACLE_XE=PASS');
  console.log('P0_PUBLISH_DIRECT_STATUS=2XX');
  console.log('P0_PUBLISH_DIRECT_READBACK=PASS');
  console.log('P0_PUBLISH_DIRECT_STATE=PUBLISHED');
  console.log('P0_PUBLISH_DIRECT_VIGENCY=1');
  console.log('P0_PUBLISH_DIRECT_AUDIT=PASS');
  console.log('P0_PUBLISH_DIRECT_FEEDBACK=PASS');
  console.log('P0_PUBLICATION_ORACLE_E2E=PASS');
});
