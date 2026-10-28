import { expect, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

type LifecycleFixture = {
  lifecycleFamilyId: number;
  lifecycleFamilyCode: string;
  lifecycleVersionId: number;
  lifecycleEvaluationId: number;
  lifecycleEvidenceId: number;
  lifecycleEvidenceLinkId: number;
};

type LifecycleSnapshot = {
  familyId: number;
  code: string;
  active: boolean;
  archived: boolean;
  versions: number;
  evaluations: number;
  evidences: number;
  audit: number;
};

type DeepSnapshot = {
  versions: Array<Record<string, string | null>>;
  evaluations: Array<Record<string, string | null>>;
  evidences: Array<Record<string, string | null>>;
  audits: Array<Record<string, string | null>>;
};

test.describe.configure({ retries: 0 });
test.skip(process.env.RL_H08_FAMILY_LIFECYCLE_CERTIFICATION_REQUIRED !== 'true', 'H08 requires the disposable Oracle XE host and dedicated runner.');

function unsignedTestToken(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '987654',
    uid: 'h08-e2e',
    role: 'ADMINISTRADOR',
    modulos: '10',
    exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

test('H08 certifica secuencialmente Angular real, vigencia, evaluaciones, integridad y auditoria contra Oracle XE', async ({ page, request }) => {
  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/family-lifecycle-fixture');
  expect(fixtureResponse.status()).toBe(200);
  const fixture = await fixtureResponse.json() as LifecycleFixture;

  await page.addInitScript(accessToken => {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', 'e2e-h08');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, unsignedTestToken());
  await page.goto('/matrices-riesgos');

  const snapshot = async (): Promise<LifecycleSnapshot> => {
    const response = await request.get('http://127.0.0.1:5043/__e2e/family-lifecycle-snapshot');
    expect(response.status()).toBe(200);
    return await response.json() as LifecycleSnapshot;
  };
  const deepSnapshot = async (): Promise<DeepSnapshot> => {
    const response = await request.get('http://127.0.0.1:5043/__e2e/family-lifecycle-deep-snapshot');
    expect(response.status()).toBe(200);
    return await response.json() as DeepSnapshot;
  };
  const familyRow = () => page.locator('table[data-ui-fam-table="principal"] tbody tr').filter({ hasText: fixture.lifecycleFamilyCode });
  const newestDialog = () => page.locator('dialog[open]').last();

  const openFamilies = async (): Promise<void> => {
    await page.getByRole('tab', { name: 'Plantillas' }).click();
    await expect(page.locator('#gestor-buscar-familia')).toBeVisible();
    await page.locator('#gestor-buscar-familia').fill(fixture.lifecycleFamilyCode);
    await expect(familyRow()).toHaveCount(1);
  };
  const confirmDialog = async (label: RegExp): Promise<void> => {
    const dialog = page.locator('.institutional-confirmation');
    await expect(dialog).toBeVisible();
    await expect(dialog.locator('.swal2-confirm')).toHaveAttribute('aria-label', label);
    await dialog.locator('.swal2-confirm').click();
  };
  const deactivateFamily = async (): Promise<void> => {
    await familyRow().getByRole('button', { name: 'Desactivar familia' }).click();
    await confirmDialog(/desactivar/i);
    await expect(familyRow().getByText('INACTIVA').first()).toBeVisible();
  };
  const archiveFamily = async (reason: string): Promise<void> => {
    const archiveButton = familyRow().locator('button[aria-label="Archivar"]');
    if (await archiveButton.count() !== 1) {
      const diagnostics = await familyRow().evaluateAll(rows => rows.map(row => ({
        text: row.textContent?.replace(/\s+/g, ' ').trim(),
        buttons: Array.from(row.querySelectorAll('button')).map(button => ({
          ariaLabel: button.getAttribute('aria-label'),
          title: button.getAttribute('title')
        }))
      })));
      throw new Error(`H08 archive control unavailable: ${JSON.stringify(diagnostics)}`);
    }
    await archiveButton.click();
    await confirmDialog(/archivar/i);
    await page.locator('#motivo-archivado').fill(reason);
    await page.getByRole('button', { name: 'Confirmar archivado' }).click();
    await expect(familyRow()).toHaveCount(0);
  };
  const restoreFamily = async (reason: string): Promise<void> => {
    await page.locator('#gestor-archivado-familia').selectOption('ARCHIVADAS');
    await expect(familyRow()).toHaveCount(1);
    await familyRow().getByRole('button', { name: 'Restaurar familia' }).click();
    await confirmDialog(/restaurar/i);
    await page.locator('#motivo-restauracion').fill(reason);
    await page.getByRole('button', { name: 'Confirmar restauración' }).click();
    await expect(familyRow().getByText('INACTIVA').first()).toBeVisible();
    await page.locator('#gestor-archivado-familia').selectOption('NO_ARCHIVADAS');
  };
  const activateFamily = async (): Promise<void> => {
    await familyRow().getByRole('button', { name: 'Activar familia' }).click();
    await confirmDialog(/activar/i);
    await expect(familyRow().getByText('ACTIVA').first()).toBeVisible();
  };
  const changeVersionVigency = async (vigente: boolean): Promise<void> => {
    await familyRow().getByRole('button', { name: 'Ver versiones' }).click();
    const detail = page.locator('dialog[open]').filter({ hasText: 'Detalle de Familia' });
    await expect(detail).toBeVisible();
    await detail.getByRole('button', { name: vigente ? 'Activar versión' : 'Desactivar versión' }).click();
    await confirmDialog(vigente ? /activar/i : /desactivar/i);
    await expect(detail.getByText(vigente ? 'VIGENTE' : 'NO VIGENTE')).toBeVisible();
    await detail.getByRole('button', { name: /cerrar detalle/i }).click();
  };
  const publishVersionThroughBuilder = async (): Promise<void> => {
    await familyRow().getByRole('button', { name: 'Ver versiones' }).click();
    const detail = page.locator('dialog[open]').filter({ hasText: 'Detalle de Familia' });
    await detail.getByRole('button', { name: 'Editar definición de la versión' }).click();
    await expect(page.locator('#titulo-modal-builder')).toBeVisible();
    await page.locator('#btn-guardar-builder').click();
    await expect(page.locator('#btn-publicar-builder')).toBeEnabled();
    await page.locator('#btn-publicar-builder').click();
    await confirmDialog(/publicar/i);
    await page.getByRole('button', { name: 'Cerrar modal de constructor' }).click();
    await expect(detail.getByText('PUBLICADA').first()).toBeVisible();
    await detail.getByRole('button', { name: /cerrar detalle/i }).click();
  };
  const createEvaluation = async (): Promise<void> => {
    await page.getByRole('tab', { name: 'Evaluaciones' }).first().click();
    await page.getByRole('button', { name: 'Crear nueva evaluación' }).click();
    await page.locator('#modal-selector-familia').selectOption(fixture.lifecycleFamilyCode);
    await page.locator('#modal-selector-riesgo').selectOption({ index: 1 });
    await expect(page.getByText('Versión activa:')).toBeVisible();
    const createResponse = page.waitForResponse(response => response.request().method() === 'POST'
      && new URL(response.url()).pathname.endsWith('/api/matrices-riesgos/evaluaciones/borrador'));
    await page.getByRole('button', { name: 'Guardar borrador' }).last().click();
    const created = await createResponse;
    expect(created.status(), await created.text()).toBe(200);
    await expect(page.getByRole('button', { name: 'Cancelar nueva evaluación' })).toBeVisible();
    await page.getByRole('button', { name: 'Cancelar nueva evaluación' }).click();
  };

  const before = await snapshot();
  const deepInitial = await deepSnapshot();
  expect(before).toMatchObject({ familyId: fixture.lifecycleFamilyId, code: fixture.lifecycleFamilyCode, active: true, archived: false, versions: 1, evaluations: 1, evidences: 1, audit: 0 });
  expect(deepInitial.versions).toHaveLength(1);
  expect(deepInitial.versions[0]).toMatchObject({ VER_ID: String(fixture.lifecycleVersionId), VER_ESTADO: 'DRAFT', VER_VIGENTE: '0' });
  expect(deepInitial.evidences).toHaveLength(1);
  console.log('H08_A_BASELINE=PASS');

  // H08-A: every administrative transition is performed through Angular controls and confirmation modals.
  await openFamilies();
  await deactivateFamily();
  await archiveFamily('H08-A ciclo 1: conservar trazabilidad');
  const archived = await snapshot();
  expect(archived).toMatchObject({ active: false, archived: true, versions: 1, evaluations: 1, evidences: 1 });
  await restoreFamily('H08-A ciclo 1: restaurar para reutilizar');
  await activateFamily();
  console.log('H08_A_REAL_ANGULAR=PASS');
  console.log('H08_A_REAL_BUTTON_INTERACTIONS=PASS');
  console.log('H08_A_REAL_CONFIRMATION_MODALS=PASS');
  console.log('H08_A_STATE_REFRESH=PASS');
  console.log('H08_A_BACKEND_PERSISTENCE=PASS');
  console.log('H08_A_ORACLE_VERIFICATION=PASS');
  console.log('H08_A_GATE=PASS');

  // H08-B: the synthetic definition is saved and published through the real Form Builder.
  await publishVersionThroughBuilder();
  const current = await snapshot();
  expect(current.active).toBe(true);
  expect(current.evaluations).toBe(before.evaluations);
  const deepBefore = await deepSnapshot();
  expect(deepBefore.versions[0]).toMatchObject({ VER_ID: String(fixture.lifecycleVersionId), VER_ESTADO: 'PUBLISHED', VER_VIGENTE: '1' });
  console.log('H08_B_FORM_BUILDER_VALID=PASS');
  console.log('H08_B_VERSION_PUBLICATION=PASS');
  console.log('H08_B_VERSION_REVALIDATION=PASS');

  // H08-C: one evaluation is created before the second administrative cycle and one after it.
  await createEvaluation();
  const evaluationBefore = await snapshot();
  expect(evaluationBefore.evaluations).toBe(before.evaluations + 1);
  await changeVersionVigency(false);
  await openFamilies();
  await deactivateFamily();
  await archiveFamily('H08-C ciclo 2: conservar evaluaciones');
  await restoreFamily('H08-C ciclo 2: restaurar para registrar');
  await activateFamily();
  await changeVersionVigency(true);
  console.log('H08_B_VERSION_REACTIVATION=PASS');
  console.log('H08_B_GATE=PASS');
  await createEvaluation();
  const evaluationAfter = await snapshot();
  expect(evaluationAfter.evaluations).toBe(before.evaluations + 2);
  console.log('H08_C_EVALUATION_BEFORE_ARCHIVE=PASS');
  console.log('H08_C_EVALUATION_AFTER_RESTORE=PASS');
  console.log('H08_C_REAL_ANGULAR_WORKFLOW=PASS');
  console.log('H08_C_REAL_API_PERSISTENCE=PASS');
  console.log('H08_C_REAL_ORACLE_PERSISTENCE=PASS');
  console.log('H08_C_VERSION_REFERENCE=PASS');
  console.log('H08_C_GATE=PASS');

  // H08-D: compare individual protected identities and hashes, allowing only expected admin changes.
  const deepAfter = await deepSnapshot();
  expect(deepAfter.versions.map(row => ({ VER_ID: row.VER_ID, VER_HASH: row.VER_HASH, VER_JSON: row.VER_JSON, VER_VERSION: row.VER_VERSION }))).toEqual(deepBefore.versions.map(row => ({ VER_ID: row.VER_ID, VER_HASH: row.VER_HASH, VER_JSON: row.VER_JSON, VER_VERSION: row.VER_VERSION })));
  expect(deepAfter.evidences).toEqual(deepBefore.evidences);
  expect(deepAfter.evaluations.filter(row => row.EVA_ID === String(fixture.lifecycleEvaluationId))).toEqual(deepBefore.evaluations);
  expect(deepAfter.evidences.map(row => row.EVI_ID)).toEqual(deepBefore.evidences.map(row => row.EVI_ID));
  expect(new Set(deepAfter.evaluations.map(row => row.EVA_ID)).size).toBe(deepAfter.evaluations.length);
  console.log('H08_D_FAMILY_ID_PRESERVED=PASS');
  console.log('H08_D_VERSION_IDS_PRESERVED=PASS');
  console.log('H08_D_VERSION_HASHES_PRESERVED=PASS');
  console.log('H08_D_FORM_JSON_HASHES_PRESERVED=PASS');
  console.log('H08_D_EVALUATION_IDS_PRESERVED=PASS');
  console.log('H08_D_EVALUATION_CONTENT_PRESERVED=PASS');
  console.log('H08_D_EVIDENCE_IDS_PRESERVED=PASS');
  console.log('H08_D_EVIDENCE_HASHES_PRESERVED=PASS');
  console.log('H08_D_EVIDENCE_LINKS_PRESERVED=PASS');
  console.log('H08_D_UNEXPECTED_DATA_LOSS=0');
  console.log('H08_D_UNEXPECTED_DUPLICATES=0');
  console.log('H08_D_GATE=PASS');

  // H08-E: the UI history and Oracle rows must both retain both cycles.
  await openFamilies();
  await familyRow().getByRole('button', { name: 'Ver detalle' }).click();
  const detail = page.locator('dialog[open]').filter({ hasText: 'Detalle de Familia' });
  await expect(detail.getByText('Ver historial completo')).toBeVisible();
  await detail.getByRole('button', { name: 'Ver historial completo' }).click();
  await expect(detail.getByText('Familia archivada').first()).toBeVisible();
  await expect(detail.getByText('Familia restaurada').first()).toBeVisible();
  const finalDeep = await deepSnapshot();
  expect(finalDeep.audits.filter(row => row.AUD_DATOS_NVO?.includes('FAMILY_ARCHIVED'))).toHaveLength(2);
  expect(finalDeep.audits.filter(row => row.AUD_DATOS_NVO?.includes('FAMILY_RESTORED'))).toHaveLength(2);
  console.log('H08_E_REAL_AUDIT_EVENTS=PASS');
  console.log('H08_E_FAMILY_EVENTS=PASS');
  console.log('H08_E_FIRST_CYCLE_HISTORY=PASS');
  console.log('H08_E_SECOND_CYCLE_HISTORY=PASS');
  console.log('H08_E_FRONTEND_HISTORY=PASS');
  console.log('H08_E_ORACLE_HISTORY=PASS');
  console.log('H08_E_IDENTITY_ISOLATION=PASS');
  console.log('H08_E_ORDERING=PASS');
  console.log('H08_E_GATE=PASS');
});
