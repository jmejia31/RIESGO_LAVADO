import { expect, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

type VersionDeleteFixture = {
  familyId: number;
  familyCode: string;
  versionAId: number;
  versionBId: number;
  formulaVersionId: number;
  formulaUsageId: number;
  evaluationVersionId: number;
  evaluationId: number;
};

type VersionDeleteSnapshot = {
  familyId: number;
  familyCode: string;
  targetCount: number;
  otherVersionCount: number;
  familyVersionCount: number;
  formulaVersionCount: number;
  evaluationCount: number;
  otherVersion: Record<string, unknown>;
  deleteAuditCount: number;
};

test.describe.configure({ retries: 0 });
test.skip(process.env.RL_P0_DELETE_REAL_E2E_REQUIRED !== 'true', 'P0 version deletion requires the disposable Oracle XE host and dedicated runner.');

function unsignedTestToken(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '987654', uid: 'p0-delete-e2e', role: 'ADMINISTRADOR', modulos: '10', exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

test('P0 certifica eliminación permanente de DRAFT mediante Angular, API y Oracle XE', async ({ page, request }) => {
  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/version-delete-fixture');
  expect(fixtureResponse.status()).toBe(200);
  const fixture = await fixtureResponse.json() as VersionDeleteFixture;

  const snapshot = async (): Promise<VersionDeleteSnapshot> => {
    const response = await request.get('http://127.0.0.1:5043/__e2e/version-delete-snapshot');
    expect(response.status()).toBe(200);
    return await response.json() as VersionDeleteSnapshot;
  };

  await page.addInitScript(accessToken => {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', 'p0-delete-e2e');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, unsignedTestToken());
  await page.goto('/matrices-riesgos');

  const initial = await snapshot();
  expect(initial).toMatchObject({
    familyId: fixture.familyId,
    familyCode: fixture.familyCode,
    targetCount: 1,
    otherVersionCount: 1,
    familyVersionCount: 2,
    formulaVersionCount: 1,
    evaluationCount: 1,
    deleteAuditCount: 0
  });
  const otherBefore = initial.otherVersion;

  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await expect(page.locator('#gestor-buscar-familia')).toBeVisible();
  await page.locator('#gestor-buscar-familia').fill(fixture.familyCode);
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  const familyRow = page.locator('table[data-ui-fam-table="principal"] tbody tr').filter({ hasText: fixture.familyCode });
  await expect(familyRow).toHaveCount(1);
  await familyRow.getByRole('button', { name: 'Ver versiones' }).click();

  const detail = page.locator('dialog[open]').filter({ hasText: 'Detalle de Familia' });
  await expect(detail).toBeVisible();
  await expect(detail).toContainText(fixture.familyCode);
  const versionRows = detail.locator('table tbody tr');
  await expect(versionRows).toHaveCount(2);
  const targetRow = versionRows.filter({ hasText: 'P0_DELETE_A' });
  const otherRow = versionRows.filter({ hasText: 'P0_DELETE_B' });
  await expect(targetRow).toHaveCount(1);
  await expect(otherRow).toHaveCount(1);
  await expect(targetRow).toContainText('DRAFT');
  await expect(otherRow).toContainText('DRAFT');

  await targetRow.getByRole('button', { name: 'Eliminar versión' }).click();
  const confirmation = page.locator('.institutional-confirmation');
  await expect(confirmation).toBeVisible();
  await expect(confirmation).toContainText(String(fixture.versionAId));

  const deleteResponse = page.waitForResponse(response => response.request().method() === 'DELETE'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/formularios/${fixture.versionAId}`);
  const readbackResponse = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/formularios/${fixture.versionAId}`
    && response.status() === 404);
  await confirmation.getByRole('button', { name: /eliminar/i }).click();
  const deleted = await deleteResponse;
  const readback = await readbackResponse;
  expect(deleted.status()).toBeGreaterThanOrEqual(200);
  expect(deleted.status()).toBeLessThan(300);
  expect(readback.status()).toBe(404);

  await expect(detail).toBeVisible();
  await expect(detail.locator('table tbody tr').filter({ hasText: 'P0_DELETE_A' })).toHaveCount(0);
  await expect(detail.locator('table tbody tr').filter({ hasText: 'P0_DELETE_B' })).toHaveCount(1);
  const feedback = detail.locator('[role="status"]').filter({ hasText: String(fixture.versionAId) });
  await expect(feedback).toBeVisible();
  await expect(feedback).toContainText('eliminado correctamente');
  await expect(feedback.locator('button[aria-label="Cerrar mensaje"] app-action-icon[action="close"]')).toHaveCount(1);
  await feedback.getByRole('button', { name: 'Cerrar mensaje' }).click();
  await expect(feedback).toBeHidden();

  const final = await snapshot();
  expect(final).toMatchObject({
    familyId: fixture.familyId,
    familyCode: fixture.familyCode,
    targetCount: 0,
    otherVersionCount: 1,
    familyVersionCount: 1,
    formulaVersionCount: 1,
    evaluationCount: 1,
    deleteAuditCount: 1
  });
  expect(final.otherVersion).toEqual(otherBefore);

  const formulaDelete = await request.delete(`http://127.0.0.1:5043/api/matrices-riesgos/formularios/${fixture.formulaVersionId}`);
  expect(formulaDelete.status()).toBe(409);
  const afterFormulaConflict = await snapshot();
  expect(afterFormulaConflict.formulaVersionCount).toBe(1);
  expect(afterFormulaConflict.deleteAuditCount).toBe(1);

  const evaluationDelete = await request.delete(`http://127.0.0.1:5043/api/matrices-riesgos/formularios/${fixture.evaluationVersionId}`);
  expect(evaluationDelete.status()).toBe(409);
  const afterEvaluationConflict = await snapshot();
  expect(afterEvaluationConflict.evaluationCount).toBe(1);
  expect(afterEvaluationConflict.deleteAuditCount).toBe(1);

  console.log('P0_DELETE_REAL_BROWSER=PASS');
  console.log('P0_DELETE_REAL_ANGULAR=PASS');
  console.log('P0_DELETE_REAL_API=PASS');
  console.log('P0_DELETE_REAL_ORACLE_XE=PASS');
  console.log('P0_DELETE_FIXTURE=PASS');
  console.log('P0_DELETE_INITIAL_VERSIONS=2');
  console.log('P0_DELETE_FINAL_VERSIONS=1');
  console.log('P0_DELETE_HTTP_STATUS=2XX');
  console.log('P0_DELETE_READBACK_STATUS=404');
  console.log('P0_DELETE_TARGET_REMOVED=YES');
  console.log('P0_DELETE_OTHER_VERSION_PRESERVED=YES');
  console.log('P0_DELETE_FAMILY_PRESERVED=YES');
  console.log('P0_DELETE_COUNTER_UPDATED=YES');
  console.log('P0_DELETE_MODAL_REMAINS_OPEN=YES');
  console.log('P0_DELETE_FEEDBACK=PASS');
  console.log('P0_DELETE_ACCESSIBILITY=PASS');
  console.log('P0_DELETE_AUDIT=PASS');
  console.log('P0_DELETE_AUDIT_DUPLICATES=0');
  console.log('P0_DELETE_FORMULA_DEPENDENCY_CONFLICT=PASS');
  console.log('P0_DELETE_FORMULA_DEPENDENCY_PRESERVED=YES');
  console.log('P0_DELETE_EVALUATION_DEPENDENCY_CONFLICT=PASS');
  console.log('P0_DELETE_EVALUATION_PRESERVED=YES');
  console.log('P0_DELETE_ORACLE_INTEGRITY=PASS');
  console.log('P0_VERSION_DELETE_E2E=PASS');
});
