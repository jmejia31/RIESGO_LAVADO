import { expect, test } from '@playwright/test';

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

const apiHeaders = { 'X-E2E-User': '987654', 'X-E2E-Modules': '10', 'Content-Type': 'application/json' };

test('H08 certifica dos ciclos de archivado/restauracion y reutilizacion contra Angular, API y Oracle XE', async ({ page, request }) => {
  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/family-lifecycle-fixture');
  expect(fixtureResponse.status()).toBe(200);
  const fixture = await fixtureResponse.json() as LifecycleFixture;

  await page.addInitScript(() => {
    localStorage.setItem('access_token', 'e2e-h08');
    localStorage.setItem('refresh_token', 'e2e-h08');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  });
  await page.goto('/matrices-riesgos');

  const api = async (method: string, path: string, body?: object): Promise<{ status: number; json: unknown }> =>
    page.evaluate(async ({ method, path, body, headers }) => {
      const response = await fetch(`http://127.0.0.1:5043${path}`, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined
      });
      return { status: response.status, json: await response.json() };
    }, { method, path, body, headers: apiHeaders });

  const snapshot = async (): Promise<LifecycleSnapshot> => {
    const response = await request.get('http://127.0.0.1:5043/__e2e/family-lifecycle-snapshot');
    expect(response.status()).toBe(200);
    return await response.json() as LifecycleSnapshot;
  };

  const before = await snapshot();
  expect(before).toMatchObject({
    familyId: fixture.lifecycleFamilyId,
    code: fixture.lifecycleFamilyCode,
    active: true,
    archived: false,
    versions: 1,
    evaluations: 1,
    evidences: 1,
    audit: 0
  });

  const activeArchive = await api('PUT', `/api/matrices-riesgos/familias/${fixture.lifecycleFamilyId}/archivar`, { motivo: 'H08 debe rechazar archivado activo' });
  expect(activeArchive.status).toBe(409);

  for (let cycle = 1; cycle <= 2; cycle++) {
    expect((await api('PUT', `/api/matrices-riesgos/familias/${fixture.lifecycleFamilyId}/desactivar`)).status).toBe(200);
    expect((await api('PUT', `/api/matrices-riesgos/familias/${fixture.lifecycleFamilyId}/archivar`, { motivo: `H08 ciclo ${cycle}: conservar trazabilidad` })).status).toBe(200);
    const archived = await snapshot();
    expect(archived).toMatchObject({ active: false, archived: true, versions: 1, evaluations: 1, evidences: 1, audit: cycle * 2 });

    expect((await api('PUT', `/api/matrices-riesgos/familias/${fixture.lifecycleFamilyId}/activar`)).status).toBe(409);
    expect((await api('PUT', `/api/matrices-riesgos/familias/${fixture.lifecycleFamilyId}/restaurar`, { motivo: `H08 ciclo ${cycle}: restaurar para reutilizar` })).status).toBe(200);
    expect((await api('PUT', `/api/matrices-riesgos/familias/${fixture.lifecycleFamilyId}/activar`)).status).toBe(200);
    const reused = await snapshot();
    expect(reused).toMatchObject({ active: true, archived: false, versions: 1, evaluations: 1, evidences: 1, audit: cycle * 4 });
  }

  const detail = await api('GET', `/api/matrices-riesgos/familias/${fixture.lifecycleFamilyId}`);
  expect(detail.status).toBe(200);
  const precheck = await api('GET', `/api/matrices-riesgos/familias/${fixture.lifecycleFamilyId}/purga-precheck`);
  expect(precheck.status).toBe(200);
  const final = await snapshot();
  expect(final).toEqual({
    familyId: fixture.lifecycleFamilyId,
    code: fixture.lifecycleFamilyCode,
    active: true,
    archived: false,
    versions: before.versions,
    evaluations: before.evaluations,
    evidences: before.evidences,
    audit: 8
  });
  console.log('H08_FAMILY_LIFECYCLE_TWO_CYCLES=PASS');
  console.log('H08_FAMILY_REUSE_IDENTITIES_EVIDENCE=PASS');
});
