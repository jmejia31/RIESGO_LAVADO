import { expect, Page, test } from '@playwright/test';

type Fixture = { riskId: number; riskCode: string; versionId: number; versionCode: string; definition: { secciones: Array<Record<string, unknown>> } };

const critical = [
  /^POST \/api\/matrices-riesgos\/evaluaciones\/borrador$/,
  /^PUT \/api\/matrices-riesgos\/evaluaciones\/\d+\/borrador$/,
  /^POST \/api\/matrices-riesgos\/evaluaciones\/calculo-preview$/,
  /^GET \/api\/matrices-riesgos\/evaluaciones\/\d+$/,
  /^GET \/api\/matrices-riesgos\/evaluaciones\/\d+\/proyeccion-institucional$/,
  /^GET \/api\/matrices-riesgos\/mitigacion\/evaluaciones\/\d+\/controles$/,
  /^GET \/api\/matrices-riesgos\/mitigacion\/evaluaciones\/\d+\/planes$/,
  /^GET \/api\/matrices-riesgos\/mitigacion\/planes\/\d+\/actividades$/,
  /^POST \/api\/matrices-riesgos\/mitigacion\/(controles|planes|actividades)$/,
];

function unsignedTestToken(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ nameid: '987654', uid: 'block7-e2e', modulos: '10', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
}

async function installNonCriticalFixtures(page: Page, fixture: Fixture, evaluation: { id?: number; data?: Record<string, unknown> }) {
  const token = unsignedTestToken();
  await page.addInitScript(accessToken => {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', 'e2e-only');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, token);

  await page.route('**/api/catalogos/modulos', route => route.fulfill({ status: 200, contentType: 'application/json', body: '{"success":true,"datos":[]}' }));
  await page.route('**/api/configuracion/**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: { nombreSistema: 'SGRLA-IHSS', nombreInstitucion: 'IHSS', timeoutSesion: 30 } }),
  }));
  await page.route('**/api/matrices-riesgos/**', async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();
    const key = `${method} ${path}`;
    if (critical.some(pattern => pattern.test(key))) {
      await route.continue();
      return;
    }

    const family = { famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LA/FT', famDescripcion: 'Fixture de prueba', famActivo: true, tieneVersionVigente: true };
    const version = {
      verId: fixture.versionId,
      verFamiliaId: 1,
      verCodigo: fixture.versionCode,
      verVersion: 1,
      verJson: JSON.stringify(fixture.definition),
      verHash: 'B'.repeat(64),
      verEstado: 'PUBLISHED',
      verVigente: true,
    };
    const methodology = { versionFormularioId: fixture.versionId, codigo: fixture.versionCode, version: 1, secciones: fixture.definition.secciones, catalogos: [], reglas: [] };
    let datos: unknown = [];

    if (path.endsWith('/formulario/version-vigente')) datos = version;
    else if (path.endsWith('/metodologia/vigente')) datos = methodology;
    else if (path.endsWith('/familias/predeterminada')) datos = { configurada: true, familiaId: 1, familiaCodigo: family.famCodigo, familiaNombre: family.famNombre };
    else if (path.endsWith('/familias/paginado')) datos = { items: [family], pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1, totales: { totalFamilias: 1, activas: 1, inactivas: 0, totalVersiones: 1 } };
    else if (path.endsWith('/familias')) datos = [family];
    else if (path.endsWith('/riesgos/paginado')) datos = { items: [{ rieId: fixture.riskId, rieCodigo: fixture.riskCode, rieNombre: 'Riesgo E2E de prueba', rieDescripcion: 'Riesgo desechable Oracle', rieActivo: true }], pagina: 1, tamanoPagina: 200, totalRegistros: 1, totalPaginas: 1 };
    else if (path.endsWith('/riesgos') && method === 'GET') datos = [{ rieId: fixture.riskId, rieCodigo: fixture.riskCode, rieNombre: 'Riesgo E2E de prueba', rieDescripcion: 'Riesgo desechable Oracle', rieActivo: true }];
    else if (path.endsWith('/evaluaciones') && method === 'GET') datos = {
      items: evaluation.id ? [{ evaId: evaluation.id, evaRiesgoId: fixture.riskId, riesgoCodigo: fixture.riskCode, riesgoNombre: 'Riesgo E2E de prueba', evaVersionId: fixture.versionId, versionCodigo: fixture.versionCode, versionNumero: 1, estado: 'BORRADOR', fechaEval: new Date().toISOString() }] : [],
      pagina: 1, registrosPorPagina: 10, totalRegistros: evaluation.id ? 1 : 0, totalPaginas: 1,
    };
    else if (path.endsWith('/formularios/historial')) datos = [version];
    else if (/\/formularios\/\d+$/.test(path) && method === 'GET') datos = version;
    else if (/\/metodologia\/version\/\d+$/.test(path)) datos = methodology;

    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });
}

test('Block 7 real browser persists draft through API and Oracle across reload/edit', async ({ page, request }) => {
  const required = process.env['RL_BLOCK7_REAL_E2E_REQUIRED'] === 'true';
  test.skip(!required, 'The real Oracle browser suite is enabled only in its disposable CI environment.');

  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/fixture');
  expect(fixtureResponse.ok()).toBeTruthy();
  const fixture = await fixtureResponse.json() as Fixture;
  const evaluation: { id?: number; data?: Record<string, unknown> } = {};
  const criticalResponses = new Set<string>();
  page.on('response', response => {
    const url = new URL(response.url());
    if (url.port !== '5043' || !['localhost', '127.0.0.1'].includes(url.hostname)) return;
    const key = `${response.request().method()} ${url.pathname}`;
    if (critical.some(pattern => pattern.test(key)) && response.status() < 500) criticalResponses.add(key.replace(/\/\d+(?=\/|$)/g, '/{id}'));
  });

  await installNonCriticalFixtures(page, fixture, evaluation);
  await page.goto('/matrices-riesgos');
  await expect(page.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await page.getByRole('button', { name: 'Nueva evaluación' }).click();
  await page.locator('#modal-selector-familia').selectOption('MATRIZ_RIESGOS_LAFT');
  await page.locator('#modal-selector-riesgo').selectOption(String(fixture.riskId));
  await page.getByLabel('Área principal').fill('Área Oracle Browser');

  const createResponsePromise = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/evaluaciones/borrador'));
  await page.getByRole('button', { name: 'Guardar borrador' }).click();
  const createResponse = await createResponsePromise;
  expect(createResponse.status()).toBe(200);
  const created = await createResponse.json() as { datos: number };
  evaluation.id = created.datos;
  evaluation.data = { area_principal: 'Área Oracle Browser' };
  expect(evaluation.id).toBeGreaterThan(0);

  await page.reload();
  await expect(page.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await page.getByTitle('Editar evaluación').click();
  await expect(page.getByLabel('Área principal')).toHaveValue('Área Oracle Browser');

  await page.getByLabel('Área principal').fill('Área Oracle editada');
  const updateResponsePromise = page.waitForResponse(response =>
    response.request().method() === 'PUT'
    && /\/evaluaciones\/\d+\/borrador$/.test(new URL(response.url()).pathname));
  await page.getByRole('button', { name: 'Guardar borrador' }).click();
  expect((await updateResponsePromise).status()).toBe(200);

  await page.reload();
  await page.getByTitle('Editar evaluación').click();
  await expect(page.getByLabel('Área principal')).toHaveValue('Área Oracle editada');
  expect([...criticalResponses].some(value => value.startsWith('GET /api/matrices-riesgos/evaluaciones/{id}/proyeccion-institucional'))).toBeTruthy();
  expect([...criticalResponses].some(value => value.startsWith('GET /api/matrices-riesgos/evaluaciones/{id}'))).toBeTruthy();
});
