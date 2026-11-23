
/**
 * Reproduces the *historical* Block 12 post-workflow Full Matrix defect using
 * the original pre-fix UI, with real API + disposable Oracle XE.
 *
 * This case is deliberately appended to the detached historical copy by
 * run_block12_prefx_reproduction.sh. Do not import modern modal selectors or
 * change the repository's current positive E2E test.
 */
test('Block 12 historical pre-fix Full Matrix projection regression after workflow', async ({ page, request }, testInfo) => {
  test.setTimeout(120_000);
  test.skip(process.env['BLOCK12_EXPECT_PREFX_REPRODUCTION'] !== 'true',
    'Only valid against the known pre-fix SHA on disposable Oracle XE.');
  expectHttpResponse({
    status: 409,
    method: 'GET',
    pathIncludes: '/proyeccion-institucional',
    minCount: 1,
    maxCount: 1,
    reason: 'The historical Full Matrix erroneously rejects a read after workflow transitions.',
  });

  const seed = await request.post('http://127.0.0.1:5043/__e2e/seed-full-chain-calculations');
  expect(seed.ok(), await seed.text()).toBe(true);
  const candidates = await seed.json() as Array<{ ordinal: number; riskCode: string; evaluationId: number }>;
  expect(candidates).toHaveLength(59);
  const subject = candidates[0];
  expect(subject.evaluationId).toBeGreaterThan(0);

  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/fixture');
  expect(fixtureResponse.status()).toBe(200);
  const fixture = await fixtureResponse.json() as Fixture;

  // Keep the original historical support fixtures: critical evaluation,
  // projection, monitoring and workflow requests must reach the real API.
  await installNonCriticalFixtures(page, fixture, {}, { useRealConsolidated: true });
  await page.goto('/matrices-riesgos');
  await expect(page.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  const listLoaded = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname.endsWith('/consolidado/paginado'));
  await page.getByRole('tab', { name: /Consolidado|Evaluaciones consolidadas/i }).click();
  expect((await listLoaded).status()).toBe(200);
  const search = page.locator('#consolidado-buscar');
  const filtered = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname.endsWith('/consolidado/paginado'));
  await search.fill(subject.riskCode);
  expect((await filtered).status()).toBe(200);
  const row = page.locator(`#panel-consolidado tbody tr[data-evaluacion-id="${subject.evaluationId}"]`);
  await expect(row).toBeVisible();

  const apiRoot = `http://127.0.0.1:5043/api/matrices-riesgos/evaluaciones/${subject.evaluationId}`;
  const originalDetailResponse = await request.get(apiRoot);
  expect(originalDetailResponse.status()).toBe(200);
  const originalDetail = (await originalDetailResponse.json() as { datos: {
    evaId: number; evaEstado: string; evaVersionId: number; evaDataJson: string;
    evaDataCalcJson: string; evaVri: number | null; evaVrr: number | null;
  } }).datos;
  const businessFingerprint = (detail: typeof originalDetail) => createHash('sha256')
    .update(JSON.stringify([detail.evaId, detail.evaVersionId, detail.evaDataJson,
      detail.evaDataCalcJson, detail.evaVri, detail.evaVrr])).digest('hex');
  const originalFingerprint = businessFingerprint(originalDetail);
  const baselineProjection = await request.get(`${apiRoot}/proyeccion-institucional`);
  expect(baselineProjection.status(), await baselineProjection.text()).toBe(200);
  const baseline = (await baselineProjection.json() as { datos: {
    evaluationId: number; fields: Array<{ fieldNumber: number; value: unknown; state: string }>
  } }).datos;
  expect(baseline.evaluationId).toBe(subject.evaluationId);
  expect(baseline.fields).toHaveLength(82);

  const beforeUiProjection = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/evaluaciones/${subject.evaluationId}/proyeccion-institucional`);
  await row.locator('[data-matrix-open="complete"]').click();
  expect((await beforeUiProjection).status()).toBe(200);
  const matrix = page.locator('[data-matrix-modal="complete"]');
  await expect(matrix).toBeVisible();
  await expect(matrix.getByText('Cargando la evaluación y su riesgo maestro…')).toHaveCount(0);
  const beforeFields = await matrix.locator('[data-matrix-field]').evaluateAll(elements =>
    [...new Set(elements.map(el => Number(el.getAttribute('data-matrix-field'))).filter(Number.isFinite))]);
  expect(beforeFields).toHaveLength(82);
  await matrix.getByRole('button', { name: 'Cerrar Matriz completa' }).first().click();
  await expect(matrix).toBeHidden();

  const transitionUrl = `${apiRoot}/transiciones`;
  if (originalDetail.evaEstado !== 'BORRADOR') {
    const reset = await request.post(`${transitionUrl}?nuevoEstado=BORRADOR&motivo=${encodeURIComponent('Preparacion historica aislada')}`);
    expect(reset.status(), await reset.text()).toBe(200);
  }
  for (const [index, destination] of ['EN_REVISION', 'OBSERVADA', 'APROBADA'].entries()) {
    const transition = await request.post(`${transitionUrl}?nuevoEstado=${destination}&motivo=${encodeURIComponent(`E2E pre-fix step ${index + 1}`)}`);
    expect(transition.status(), await transition.text()).toBe(200);
    const state = await request.get(apiRoot);
    expect(state.status()).toBe(200);
    expect((await state.json() as { datos: { evaEstado: string } }).datos.evaEstado).toBe(destination);
  }
  const workflowHistory = await request.get(`${apiRoot}/flujos`);
  expect(workflowHistory.status()).toBe(200);
  const states = (await workflowHistory.json() as { datos: Array<{ fluEstado: string }> }).datos.map(flow => flow.fluEstado);
  for (const state of ['EN_REVISION', 'OBSERVADA', 'APROBADA']) expect(states).toContain(state);

  const finalDetailResponse = await request.get(apiRoot);
  expect(finalDetailResponse.status()).toBe(200);
  const finalDetail = (await finalDetailResponse.json() as { datos: typeof originalDetail }).datos;
  expect(finalDetail.evaId).toBe(originalDetail.evaId);
  expect(finalDetail.evaVersionId).toBe(originalDetail.evaVersionId);
  expect(finalDetail.evaEstado).toBe('APROBADA');
  const finalFingerprint = businessFingerprint(finalDetail);
  expect(finalFingerprint, 'Workflow must not silently rewrite evaluation business data')
    .toBe(originalFingerprint);

  // A direct API read proves the old endpoint is defective, independent of UI.
  const serverProjection = await request.get(`${apiRoot}/proyeccion-institucional`);
  expect(serverProjection.status(), await serverProjection.text()).toBe(409);
  const projectionError = await serverProjection.json() as { mensaje?: string; detail?: string };
  const errorMessage = projectionError.mensaje ?? projectionError.detail ?? '';
  expect(errorMessage.trim().length).toBeGreaterThan(0);

  // Re-open Full Matrix through the *historical* Angular button. No route mocks
  // are allowed for this projection or the companion monitoring endpoint.
  const afterUiProjection = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/evaluaciones/${subject.evaluationId}/proyeccion-institucional`);
  const afterUiMonitoring = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/monitoreo/evaluaciones/${subject.evaluationId}/bloque6`);
  await row.locator('[data-matrix-open="complete"]').click();
  const browserProjection = await afterUiProjection;
  const browserMonitoring = await afterUiMonitoring;
  expect(browserProjection.status()).toBe(409);
  expect(browserMonitoring.status()).toBe(200);
  await expect(matrix).toBeVisible();
  await expect(page.getByText(/La evaluaci.n fue modificada por otra sesi.n o usuario/).first()).toBeVisible();
  const renderedFields = await matrix.locator('[data-matrix-field]').count();
  expect(renderedFields).toBe(0);
  await expect(matrix.locator('.modal-metadata-card dd').nth(1)).toHaveText('Aprobada');

  const evidence = {
    historicalSha: '3325778c81dee92853044036945ae16ba0942311',
    evaluationId: subject.evaluationId,
    riskCode: subject.riskCode,
    baselineProjectionStatus: baselineProjection.status(),
    baselineFieldCount: baseline.fields.length,
    baselineRenderedFields: beforeFields.length,
    workflowSteps: states.filter(state => ['EN_REVISION', 'OBSERVADA', 'APROBADA'].includes(state)),
    finalState: finalDetail.evaEstado,
    businessDataPreserved: finalFingerprint === originalFingerprint,
    directProjectionStatus: serverProjection.status(),
    browserProjectionStatus: browserProjection.status(),
    monitoringStatus: browserMonitoring.status(),
    postWorkflowRenderedFields: renderedFields,
    falseConcurrencyWarningVisible: true,
    baselineBusinessHash: originalFingerprint,
    afterBusinessHash: finalFingerprint,
  };
  await testInfo.attach('block12-historical-prefx-proof', {
    body: Buffer.from(JSON.stringify(evidence, null, 2)),
    contentType: 'application/json',
  });
  console.log('BLOCK12_PRE_FIX_FULL_MATRIX_DEFECT_REPRODUCED=YES');
  console.log(`BLOCK12_PREFX_DIAGNOSTIC=${JSON.stringify(evidence)}`);
});
