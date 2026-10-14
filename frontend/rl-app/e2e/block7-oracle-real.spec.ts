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
  /^GET \/api\/matrices-riesgos\/mitigacion\/evaluaciones\/\d+\/bloque4$/,
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

test('Block 7 real browser persists draft through API and Oracle across reload/edit', async ({ page, request }, testInfo) => {
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
  const riskOption = page.locator('#modal-selector-riesgo option').filter({ hasText: fixture.riskCode }).last();
  await expect(riskOption).toHaveCount(1);
  await page.locator('#modal-selector-riesgo').selectOption({ label: (await riskOption.textContent())!.trim() });
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

  const api = 'http://localhost:5043/api/matrices-riesgos/mitigacion';
  for (const description of ['Control E2E A', 'Control E2E B']) {
    const createdControl = await request.post(`${api}/controles`, {
      data: { conEvaluacionId: evaluation.id, conTipo: 'PREVENTIVO', conDescripcion: description, conAutomatizacion: 'MANUAL', conEstado: 'ACTIVO' },
    });
    expect(createdControl.status(), await createdControl.text()).toBe(200);
  }
  const controlsResponse = await request.get(`${api}/evaluaciones/${evaluation.id}/controles`);
  expect(controlsResponse.ok()).toBeTruthy();
  const controls = (await controlsResponse.json() as { datos: Array<{ conDescripcion: string }> }).datos;
  expect(controls.map(control => control.conDescripcion)).toEqual(['Control E2E A', 'Control E2E B']);
  expect(controls.some(control => control.conDescripcion === 'No hay')).toBeFalsy();

  const createPlan = async (description: string, owner: string, startDate: string, endDate: string, budget: number) => {
    const response = await request.post(`${api}/planes`, {
      data: {
        plaEvaluacionId: evaluation.id, plaDescripcion: description, plaAvance: 0, plaPresupuesto: budget,
        plaFechaInicio: startDate, plaFechaFin: endDate, plaEstado: 'PENDIENTE',
        plaMonitoreoSeguimiento: `Seguimiento ${owner}`, plaResponsables: owner, plaRecursos: `Recursos ${owner}`,
      },
    });
    expect(response.status(), await response.text()).toBe(200);
    return (await response.json() as { datos: number }).datos;
  };
  const planA = await createPlan('Plan E2E A', 'Responsable A', '2026-01-01', '2026-02-28', 1000);
  const planB = await createPlan('Plan E2E B', 'Responsable B', '2026-03-01', '2026-04-30', 2000);
  const createActivity = async (planId: number, description: string, owner: string) => {
    const response = await request.post(`${api}/actividades`, {
      data: {
        actPlanId: planId, actDescripcion: description, actResponsable: owner, actAvance: 0,
        actFechaInicio: '2026-01-10', actFechaFin: '2026-01-20', actEstado: 'PENDIENTE',
      },
    });
    expect(response.status(), await response.text()).toBe(200);
  };
  await createActivity(planA, 'Actividad E2E A1', 'Responsable A');
  await createActivity(planA, 'Actividad E2E A2', 'Responsable A');
  await createActivity(planB, 'Actividad E2E B1', 'Responsable B');
  const plansResponse = await request.get(`${api}/evaluaciones/${evaluation.id}/planes`);
  expect(plansResponse.ok()).toBeTruthy();
  const plans = (await plansResponse.json() as { datos: Array<{ plaDescripcion: string; plaResponsables: string }> }).datos;
  expect(plans.map(plan => [plan.plaDescripcion, plan.plaResponsables])).toEqual([
    ['Plan E2E A', 'Responsable A'], ['Plan E2E B', 'Responsable B'],
  ]);
  const activitiesA = await request.get(`${api}/planes/${planA}/actividades`);
  const activitiesB = await request.get(`${api}/planes/${planB}/actividades`);
  expect((await activitiesA.json() as { datos: unknown[] }).datos).toHaveLength(2);
  expect((await activitiesB.json() as { datos: unknown[] }).datos).toHaveLength(1);

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
  const pageA = await page.context().newPage();
  await installNonCriticalFixtures(pageA, fixture, evaluation);
  await pageA.goto('/matrices-riesgos');
  await expect(pageA.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await expect(pageA.locator('button[title^="Editar evaluaci"]')).toBeVisible();
  await pageA.locator('button[title^="Editar evaluaci"]').click();
  await expect(pageA.getByLabel('Área principal')).toHaveValue('Área Oracle editada');
  const projectionResponse = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}/proyeccion-institucional`);
  const projectionBody = await projectionResponse.text();
  expect(projectionResponse.ok(), `Projection HTTP ${projectionResponse.status()}: ${projectionBody.slice(0, 1000)}`).toBeTruthy();
  const projection = (JSON.parse(projectionBody) as { datos: { fields: Array<{ fieldNumber: number; value: unknown }> } }).datos;
  expect(projection.fields).toHaveLength(82);
  expect(projection.fields.find(field => field.fieldNumber === 41)?.value).toBe(2);
  expect(projection.fields.find(field => field.fieldNumber === 43)?.value).toBe(3);
  expect(projection.fields.find(field => field.fieldNumber === 40)?.value).toEqual([
    { planOrdinalIndex: 1, value: 'Plan E2E A', state: 'VALUE' },
    { planOrdinalIndex: 2, value: 'Plan E2E B', state: 'VALUE' },
  ]);
  expect(projection.fields.find(field => field.fieldNumber === 45)?.value).toEqual([
    { planOrdinalIndex: 1, value: 'Responsable A', state: 'VALUE' },
    { planOrdinalIndex: 2, value: 'Responsable B', state: 'VALUE' },
  ]);
  for (const fieldNumber of [44, 46, 47, 48, 49]) {
    expect(projection.fields.find(field => field.fieldNumber === fieldNumber)?.value).toMatchObject([
      { planOrdinalIndex: 1 },
      { planOrdinalIndex: 2 },
    ]);
  }
  expect(projection.fields.find(field => field.fieldNumber === 44)?.value).toEqual([
    { planOrdinalIndex: 1, value: 'Seguimiento Responsable A', state: 'VALUE' },
    { planOrdinalIndex: 2, value: 'Seguimiento Responsable B', state: 'VALUE' },
  ]);
  expect(projection.fields.find(field => field.fieldNumber === 49)?.value).toEqual([
    { planOrdinalIndex: 1, value: 1000, state: 'VALUE' },
    { planOrdinalIndex: 2, value: 2000, state: 'VALUE' },
  ]);
  expect([...criticalResponses].some(value => value.startsWith('GET /api/matrices-riesgos/evaluaciones/{id}/proyeccion-institucional'))).toBeTruthy();
  expect([...criticalResponses].some(value => value.startsWith('GET /api/matrices-riesgos/evaluaciones/{id}'))).toBeTruthy();

  const blockNavigation = pageA.locator('nav[aria-label^="Bloques funcionales"]');
  await expect(blockNavigation.getByRole('button')).toHaveCount(6);
  const firstBlock = blockNavigation.getByRole('button').first();
  await firstBlock.focus();
  await pageA.keyboard.press('Enter');
  await expect(firstBlock).toHaveAttribute('aria-current', 'step');

  await pageA.locator('dialog[aria-labelledby="titulo-modal-editar"] button[aria-label^="Cerrar"]').click();
  const block4Read = pageA.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/mitigacion/evaluaciones/${evaluation.id}/bloque4`);
  await pageA.getByRole('button', { name: 'Ver Matriz completa' }).click();
  const matrixDialog = pageA.locator('dialog[data-matrix-modal="complete"]');
  await expect(matrixDialog).toBeVisible();
  await expect(matrixDialog).toHaveAttribute('aria-modal', 'true');
  await expect(matrixDialog).toHaveAttribute('aria-labelledby', 'titulo-modal-matriz-completa');
  await expect(matrixDialog.getByRole('heading', { name: 'Matriz completa' })).toBeVisible();
  await expect(matrixDialog.getByRole('button', { name: 'Cerrar Matriz completa' }).first()).toBeVisible();
  await expect(matrixDialog.locator('[data-matrix-plan]')).toHaveCount(2);
  await expect(matrixDialog.locator('[data-control-type="PREVENTIVO"]')).toHaveCount(4);
  expect((await block4Read).status()).toBe(200);

  const block5 = matrixDialog.locator('[data-matrix-block="5"]');
  const details = block5.getByRole('button', { name: 'Mostrar detalle' });
  await expect(details).toHaveAttribute('aria-expanded', 'false');
  await details.click();
  await expect(block5.getByRole('button', { name: 'Ocultar detalle' })).toHaveAttribute('aria-expanded', 'true');
  await expect(block5.locator('[data-matrix-field]')).toHaveCount(20);
  await expect(block5.locator('input, select, textarea')).toHaveCount(0);

  const responsiveSections = [
    { block: '1', name: 'block1' },
    { block: '2', name: 'block2-controls' },
    { block: '4', name: 'block4-plans' },
    { block: '5', name: 'block5-expanded' },
  ];
  for (const viewport of [{ width: 1280, height: 900, name: 'desktop' }, { width: 390, height: 844, name: 'mobile' }]) {
    await pageA.setViewportSize({ width: viewport.width, height: viewport.height });
    await expect.poll(() => pageA.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
    for (const section of responsiveSections) {
      const target = matrixDialog.locator(`[data-matrix-block="${section.block}"]`);
      await expect(target).toBeVisible();
      await target.screenshot({ path: testInfo.outputPath(`block7-${viewport.name}-${section.name}.png`) });
    }
  }

  await matrixDialog.getByRole('button', { name: 'Cerrar Matriz completa' }).first().click();

  const concurrencyA = await page.context().newPage();
  await installNonCriticalFixtures(concurrencyA, fixture, evaluation);
  await concurrencyA.goto('/matrices-riesgos');
  await expect(concurrencyA.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await expect(concurrencyA.locator('button[title^="Editar evaluaci"]')).toBeVisible();
  const concurrencyAEvaluationRead = concurrencyA.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/evaluaciones/${evaluation.id}`);
  await concurrencyA.locator('button[title^="Editar evaluaci"]').click();
  expect((await concurrencyAEvaluationRead).status()).toBe(200);
  await expect(concurrencyA.getByLabel('Área principal')).toHaveValue('Área Oracle editada');

  const pageB = await page.context().newPage();
  await installNonCriticalFixtures(pageB, fixture, evaluation);
  await pageB.goto('/matrices-riesgos');
  await expect(pageB.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await expect(pageB.locator('button[title^="Editar evaluaci"]')).toBeVisible();
  const pageBEvaluationRead = pageB.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/evaluaciones/${evaluation.id}`);
  await pageB.locator('button[title^="Editar evaluaci"]').click();
  expect((await pageBEvaluationRead).status()).toBe(200);
  await expect(pageB.locator('dialog[aria-labelledby="titulo-modal-editar"]')).toBeVisible();
  await expect(pageB.getByLabel('Área principal')).toHaveValue('Área Oracle editada');

  await concurrencyA.getByLabel('Área principal').fill('Editor A');
  const saveA = concurrencyA.waitForResponse(response => response.request().method() === 'PUT'
    && /\/evaluaciones\/\d+\/borrador$/.test(new URL(response.url()).pathname));
  await concurrencyA.getByRole('button', { name: 'Guardar borrador' }).click();
  expect((await saveA).status()).toBe(200);

  await pageB.getByLabel('Área principal').fill('Editor B stale');
  const saveB = pageB.waitForResponse(response => response.request().method() === 'PUT'
    && /\/evaluaciones\/\d+\/borrador$/.test(new URL(response.url()).pathname));
  await pageB.getByRole('button', { name: 'Guardar borrador' }).click();
  expect((await saveB).status()).toBe(409);
  await expect(pageB.getByText(/modificada por otra sesión o usuario/i).first()).toBeVisible();
  await expect(pageB.getByLabel('Área principal')).toHaveValue('Editor B stale');

  const persisted = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}`);
  expect(persisted.ok()).toBeTruthy();
  const persistedBody = (await persisted.json() as { datos: { evaVersionRow: number; evaDataJson: string } }).datos;
  expect(persistedBody.evaVersionRow).toBe(3);
  expect(persistedBody.evaDataJson).toContain('Editor A');
  expect(persistedBody.evaDataJson).not.toContain('Editor B stale');
  await concurrencyA.close();
  await pageB.close();
  console.log('BLOCK7_MATRIX_API_ROUTES_MOCKED=0');
});
