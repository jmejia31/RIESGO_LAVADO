import { expect, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

type Fixture = { familyId: number; familyCode: string; versionId: number; triggerName: string };
type Snapshot = {
  versionId: number;
  familyId: number;
  code: string;
  state: string;
  vigency: number;
  json: string;
  hash: string;
  auditCount: number;
  triggerEnabled: boolean;
};

test.describe.configure({ retries: 0 });
test.skip(process.env.RL_P03_TX12_AUDIT_FAILURE_REQUIRED !== 'true', 'TX12 negative E2E requires the disposable Oracle XE host and dedicated runner.');

function unsignedTestToken(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '987654', uid: 'tx12-audit-rollback-e2e', role: 'ADMINISTRADOR', modulos: '10', exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

test('TX12 certifica fallo real de auditoría y rollback desde Angular', async ({ page, request }) => {
  const token = unsignedTestToken();
  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/tx12-audit-fixture');
  expect(fixtureResponse.status()).toBe(200);
  const fixture = await fixtureResponse.json() as Fixture;
  const snapshot = async (): Promise<Snapshot> => {
    const response = await request.get('http://127.0.0.1:5043/__e2e/tx12-audit-snapshot');
    expect(response.status()).toBe(200);
    return await response.json() as Snapshot;
  };

  await page.addInitScript(accessToken => {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', 'tx12-audit-rollback-e2e');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, token);
  await page.goto('/matrices-riesgos');

  const initial = await snapshot();
  expect(initial).toMatchObject({
    versionId: fixture.versionId,
    familyId: fixture.familyId,
    code: 'TX12_AUDIT_V1',
    state: 'DRAFT',
    vigency: 0,
    auditCount: 0,
    triggerEnabled: true
  });
  const original = { json: initial.json, hash: initial.hash, state: initial.state, vigency: initial.vigency, auditCount: initial.auditCount };

  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.locator('#gestor-buscar-familia').fill(fixture.familyCode);
  const familyRow = page.locator('table[data-ui-fam-table="principal"] tbody tr').filter({ hasText: fixture.familyCode });
  await expect(familyRow).toHaveCount(1);
  await familyRow.getByRole('button', { name: 'Ver versiones' }).click();
  const detail = page.locator('dialog[open]').filter({ hasText: 'Detalle de Familia' });
  await detail.locator('table tbody tr').filter({ hasText: 'TX12_AUDIT_V1' }).getByRole('button', { name: 'Editar definición de la versión' }).click();
  const builder = page.locator('dialog[open]').filter({ hasText: 'Constructor de Formularios Dinámicos' });
  await expect(builder).toBeVisible();

  const titleInput = builder.locator('input[id^="seccion-titulo-"]').first();
  await expect(titleInput).toBeVisible();
  await titleInput.fill('TX12 cambio que debe revertirse');
  const putResponse = page.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/formularios/${fixture.versionId}`);
  await builder.locator('#btn-guardar-builder').click();
  const put = await putResponse;
  expect(put.status()).toBe(500);
  const putBody = await put.text();
  expect(putBody).not.toContain('ORA-20991');
  await expect(builder.locator('[data-uat="feedback-form-builder-error"]')).toContainText('No se pudo actualizar la definición');
  await expect(builder.locator('[data-uat="feedback-form-builder-success"]')).toHaveCount(0);

  const readback = await request.get(`http://127.0.0.1:5043/api/matrices-riesgos/formularios/${fixture.versionId}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(readback.status()).toBe(200);
  const readbackEnvelope = await readback.json() as { datos?: { verJson?: string; verHash?: string; verEstado?: string; verVigente?: boolean } };
  const readbackBody = readbackEnvelope.datos;
  expect(readbackBody?.verJson).toBe(original.json);
  expect(readbackBody?.verHash).toBe(original.hash);
  expect(readbackBody?.verEstado).toBe(original.state);
  expect(readbackBody?.verVigente).toBe(false);

  const after = await snapshot();
  expect(after).toMatchObject({
    versionId: fixture.versionId,
    json: original.json,
    hash: original.hash,
    state: original.state,
    vigency: original.vigency,
    auditCount: original.auditCount,
    triggerEnabled: true
  });

  console.log('TX12_REAL_ANGULAR_INTERACTION=PASS');
  console.log('TX12_REAL_HTTP_ERROR=PASS');
  console.log('TX12_REAL_ORACLE_AUDIT_FAILURE=PASS');
  console.log('TX12_JSON_ROLLBACK=PASS');
  console.log('TX12_HASH_ROLLBACK=PASS');
  console.log('TX12_STATE_ROLLBACK=PASS');
  console.log('TX12_VIGENCY_ROLLBACK=PASS');
  console.log('TX12_AUDIT_DELTA=0');
  console.log('TX12_PARTIAL_COMMIT=0');
  console.log('TX12_ANGULAR_ERROR_VISIBLE=PASS');
  console.log('TX12_FALSE_SUCCESS_MESSAGE=NO');
  console.log('TX12_FRONTEND_NEGATIVE_E2E=PASS');
});
