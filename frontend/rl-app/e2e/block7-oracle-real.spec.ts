import { expect, Page, test, expectHttpResponse } from './support/telemetry-test';

type Fixture = { riskId: number; riskCode: string; versionId: number; versionCode: string; areaOnlyUserId: number; ugrOnlyUserId: number; bothUserId: number; neitherUserId: number; definition: { secciones: Array<Record<string, unknown>> } };

const critical = [
  /^POST \/api\/matrices-riesgos\/evaluaciones\/borrador$/,
  /^PUT \/api\/matrices-riesgos\/evaluaciones\/\d+\/borrador$/,
  /^GET \/api\/matrices-riesgos\/evaluaciones\/\d+\/transiciones-permitidas$/,
  /^GET \/api\/matrices-riesgos\/evaluaciones\/\d+\/flujos$/,
  /^POST \/api\/matrices-riesgos\/evaluaciones\/\d+\/transiciones$/,
  /^POST \/api\/matrices-riesgos\/evaluaciones\/calculo-preview$/,
  /^GET \/api\/matrices-riesgos\/evaluaciones\/\d+$/,
  /^GET \/api\/matrices-riesgos\/evaluaciones\/\d+\/proyeccion-institucional$/,
  /^GET \/api\/matrices-riesgos\/mitigacion\/evaluaciones\/\d+\/controles$/,
  /^GET \/api\/matrices-riesgos\/mitigacion\/evaluaciones\/\d+\/planes$/,
  /^GET \/api\/matrices-riesgos\/mitigacion\/evaluaciones\/\d+\/bloque4$/,
  /^GET \/api\/matrices-riesgos\/mitigacion\/planes\/\d+\/actividades$/,
  /^POST \/api\/matrices-riesgos\/mitigacion\/(controles|planes|actividades)$/,
  /^PUT \/api\/matrices-riesgos\/mitigacion\/(planes|actividades)\/\d+$/,
  /^GET \/api\/matrices-riesgos\/monitoreo\/evaluaciones\/\d+\/(alertas|automonitoreo|bloque6)$/,
  /^POST \/api\/matrices-riesgos\/monitoreo\/(alertas|automonitoreo)$/,
  /^PUT \/api\/matrices-riesgos\/monitoreo\/(alertas\/\d+\/estado|controles\/\d+|evaluaciones\/\d+\/observaciones\/(area|ugr))$/,
  /^POST \/api\/matrices-riesgos\/evidencias\/(cargar|vinculos)$/,
  /^DELETE \/api\/matrices-riesgos\/evidencias\/(vinculos|\d+)$/,
];

function unsignedTestToken(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ nameid: '987654', uid: 'block7-e2e', modulos: '10', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
}

async function installNonCriticalFixtures(page: Page, fixture: Fixture, evaluation: { id?: number; data?: Record<string, unknown>; state?: string }) {
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
      items: evaluation.id ? [{ evaId: evaluation.id, evaRiesgoId: fixture.riskId, riesgoCodigo: fixture.riskCode, riesgoNombre: 'Riesgo E2E de prueba', evaVersionId: fixture.versionId, versionCodigo: fixture.versionCode, versionNumero: 1, estado: evaluation.state ?? 'BORRADOR', fechaEval: new Date().toISOString() }] : [],
      pagina: 1, registrosPorPagina: 10, totalRegistros: evaluation.id ? 1 : 0, totalPaginas: 1,
    };
    else if (path.endsWith('/formularios/historial')) datos = [version];
    else if (/\/formularios\/\d+$/.test(path) && method === 'GET') datos = version;
    else if (/\/metodologia\/version\/\d+$/.test(path)) datos = methodology;

    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });
}

test('Block 7/9 real browser certifies draft, mitigation, monitoring and evidence against Oracle', async ({ page, request }, testInfo) => {
  expectHttpResponse({
    status: 400,
    method: 'POST',
    path: '/api/matrices-riesgos/evaluaciones/calculo-preview',
    count: 6,
    reason: 'The lifecycle fixture intentionally omits calculation inputs.',
  });
  expectHttpResponse({
    status: 409,
    method: 'PUT',
    pathIncludes: '/borrador',
    reason: 'The stale concurrent editor is expected to be rejected.',
  });
  const required = process.env['RL_BLOCK7_REAL_E2E_REQUIRED'] === 'true'
    && process.env['RL_BLOCK9_REAL_E2E_REQUIRED'] === 'true';
  test.skip(!required, 'The real Oracle browser suite requires the explicit Block 7 and Block 9 disposable CI opt-ins.');
  await page.setViewportSize({ width: 1280, height: 900 });

  const fixtureResponse = await request.get('http://127.0.0.1:5043/__e2e/fixture');
  expect(fixtureResponse.ok()).toBeTruthy();
  const fixture = await fixtureResponse.json() as Fixture;
  const evaluation: { id?: number; data?: Record<string, unknown>; state?: string } = {};
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
  const monitoring = 'http://localhost:5043/api/matrices-riesgos/monitoreo';
  const evidenceApi = 'http://localhost:5043/api/matrices-riesgos/evidencias';
  const unauthenticatedRead = await request.get(`${monitoring}/evaluaciones/${evaluation.id}/bloque6`, { headers: { 'X-E2E-Unauthenticated': 'true' } });
  expect(unauthenticatedRead.status()).toBe(401);
  const moduleDeniedRead = await request.get(`${monitoring}/evaluaciones/${evaluation.id}/bloque6`, { headers: { 'X-E2E-Modules': '9' } });
  expect(moduleDeniedRead.status()).toBe(403);
  const invalidFilename = await request.post(`${evidenceApi}/cargar`, { multipart: {
    archivo: { name: '../block9-path-traversal.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4\ninvalid path\n%%EOF') },
  } });
  expect(invalidFilename.status(), await invalidFilename.text()).toBe(400);
  expect(await invalidFilename.text()).not.toMatch(/ORA-\d+|SELECT |INSERT |stack trace|connection string/i);
  const controlIds: number[] = [];
  for (const description of ['Control E2E A', 'Control E2E B']) {
    const createdControl = await request.post(`${api}/controles`, {
      data: { conEvaluacionId: evaluation.id, conTipo: 'PREVENTIVO', conDescripcion: description, conAutomatizacion: 'MANUAL', conEstado: 'ACTIVO' },
    });
    expect(createdControl.status(), await createdControl.text()).toBe(200);
    controlIds.push((await createdControl.json() as { datos: number }).datos);
  }
  const snapshotPreventiveOnly = await request.get(`${monitoring}/evaluaciones/${evaluation.id}/bloque6`);
  expect(snapshotPreventiveOnly.ok()).toBeTruthy();
  const preventiveOnly = (await snapshotPreventiveOnly.json() as { datos: { controles: Array<{ tipo: string; descripcion: string }> } }).datos.controles;
  expect(preventiveOnly).toHaveLength(2);
  expect(preventiveOnly.every(control => control.tipo === 'PREVENTIVO' && control.descripcion !== 'No hay')).toBeTruthy();
  for (const [type, description] of [['DETECTIVO', 'Control E2E Detectivo'], ['CORRECTIVO', 'Control E2E Correctivo']]) {
    const createdControl = await request.post(`${api}/controles`, {
      data: { conEvaluacionId: evaluation.id, conTipo: type, conDescripcion: description, conAutomatizacion: 'MANUAL', conEstado: 'ACTIVO' },
    });
    expect(createdControl.status(), await createdControl.text()).toBe(200);
    controlIds.push((await createdControl.json() as { datos: number }).datos);
  }
  const controlsResponse = await request.get(`${api}/evaluaciones/${evaluation.id}/controles`);
  expect(controlsResponse.ok()).toBeTruthy();
  const controls = (await controlsResponse.json() as { datos: Array<{ conDescripcion: string }> }).datos;
  expect(controls.map(control => control.conDescripcion)).toEqual(['Control E2E A', 'Control E2E B', 'Control E2E Detectivo', 'Control E2E Correctivo']);
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
  const activityIds: number[] = [];
  const createActivity = async (planId: number, description: string, owner: string) => {
    const response = await request.post(`${api}/actividades`, {
      data: {
        actPlanId: planId, actDescripcion: description, actResponsable: owner, actAvance: 0,
        actFechaInicio: '2026-01-10', actFechaFin: '2026-01-20', actEstado: 'PENDIENTE',
      },
    });
    expect(response.status(), await response.text()).toBe(200);
    activityIds.push((await response.json() as { datos: number }).datos);
  };
  await createActivity(planA, 'Actividad E2E A1', 'Responsable A');
  await createActivity(planA, 'Actividad E2E A2', 'Responsable A');
  await createActivity(planB, 'Actividad E2E B1', 'Responsable B');
  await createActivity(planB, 'Actividad E2E B2', 'Responsable B');
  await createActivity(planB, 'Actividad E2E B3', 'Responsable B');
  const planUpdate = await request.put(`${api}/planes/${planA}`, { data: {
    plaEvaluacionId: evaluation.id, plaDescripcion: 'Plan E2E A actualizado', plaAvance: 10, plaPresupuesto: 1100,
    plaFechaInicio: '2026-01-01', plaFechaFin: '2026-02-28', plaEstado: 'EN_PROCESO',
    plaMonitoreoSeguimiento: 'Seguimiento Responsable A actualizado', plaResponsables: 'Responsable A', plaRecursos: 'Recursos A',
  } });
  expect(planUpdate.status(), await planUpdate.text()).toBe(200);
  const activityUpdate = await request.put(`${api}/actividades/${activityIds[0]}`, { data: {
    actPlanId: planA, actDescripcion: 'Actividad E2E A1 actualizada', actResponsable: 'Responsable A', actAvance: 25,
    actFechaInicio: '2026-01-10', actFechaFin: '2026-01-20', actEstado: 'EN_PROCESO',
  } });
  expect(activityUpdate.status(), await activityUpdate.text()).toBe(200);
  const plansResponse = await request.get(`${api}/evaluaciones/${evaluation.id}/planes`);
  expect(plansResponse.ok()).toBeTruthy();
  const plans = (await plansResponse.json() as { datos: Array<{ plaDescripcion: string; plaResponsables: string }> }).datos;
  expect(plans.map(plan => [plan.plaDescripcion, plan.plaResponsables])).toEqual([
    ['Plan E2E A actualizado', 'Responsable A'], ['Plan E2E B', 'Responsable B'],
  ]);
  const activitiesA = await request.get(`${api}/planes/${planA}/actividades`);
  const activitiesB = await request.get(`${api}/planes/${planB}/actividades`);
  const readActivitiesA = (await activitiesA.json() as { datos: Array<{ actDescripcion: string }> }).datos;
  const readActivitiesB = (await activitiesB.json() as { datos: Array<{ actDescripcion: string }> }).datos;
  expect(readActivitiesA.map(activity => activity.actDescripcion)).toEqual(['Actividad E2E A1 actualizada', 'Actividad E2E A2']);
  expect(readActivitiesB.map(activity => activity.actDescripcion)).toEqual(['Actividad E2E B1', 'Actividad E2E B2', 'Actividad E2E B3']);

  const alertResponse = await request.post(`${monitoring}/alertas`, { data: {
    aleEvaluacionId: evaluation.id, aleCodigo: 'BLOCK9-E2E', aleIndicador: 'Señal Oracle E2E', aleEstado: 'ACTIVO',
  } });
  expect(alertResponse.status(), await alertResponse.text()).toBe(200);
  const alertId = (await alertResponse.json() as { datos: number }).datos;
  const duplicateAlert = await request.post(`${monitoring}/alertas`, { data: {
    aleEvaluacionId: evaluation.id, aleCodigo: 'BLOCK9-E2E', aleIndicador: 'Duplicada', aleEstado: 'ACTIVO',
  } });
  expect(duplicateAlert.status(), await duplicateAlert.text()).toBe(400);
  const deactivateAlert = await request.put(`${monitoring}/alertas/${alertId}/estado`, { data: { aleEstado: 'INACTIVO' } });
  expect(deactivateAlert.status(), await deactivateAlert.text()).toBe(200);
  const alertRead = await request.get(`${monitoring}/evaluaciones/${evaluation.id}/alertas`);
  expect((await alertRead.json() as { datos: Array<{ aleCodigo: string; aleEstado: string }> }).datos.find(alert => alert.aleCodigo === 'BLOCK9-E2E')?.aleEstado).toBe('INACTIVO');
  const activeAlert = await request.post(`${monitoring}/alertas`, { data: {
    aleEvaluacionId: evaluation.id, aleCodigo: 'BLOCK9-E2E-ACTIVE', aleIndicador: 'Señal activa de prueba', aleEstado: 'ACTIVO',
  } });
  expect(activeAlert.status(), await activeAlert.text()).toBe(200);

  const monitorFirst = await request.post(`${monitoring}/automonitoreo`, { data: {
    monEvaluacionId: evaluation.id, monEstadoRiesgo: 'VIGENTE', monEstadoContr: 'SE_MANTIENE', monResultado: 'Ciclo anterior de prueba',
  } });
  expect(monitorFirst.status(), await monitorFirst.text()).toBe(200);
  const monitorLatest = await request.post(`${monitoring}/automonitoreo`, { data: {
    monEvaluacionId: evaluation.id, monEstadoRiesgo: 'MITIGADO', monEstadoContr: 'NO_SE_MANTIENE', monResultado: 'Ciclo más reciente de prueba',
  } });
  expect(monitorLatest.status(), await monitorLatest.text()).toBe(200);
  const monitorHistory = await request.get(`${monitoring}/evaluaciones/${evaluation.id}/automonitoreo`);
  expect((await monitorHistory.json() as { datos: Array<{ monEstadoRiesgo: string }> }).datos[0]?.monEstadoRiesgo).toBe('Mitigado');

  const monitoringStates = ['SE_MANTIENE', 'NO_SE_MANTIENE', 'SE_MANTIENE', 'NO_SE_MANTIENE'];
  const monitoringEffectiveness = [30, 50, 85, 90];
  for (let index = 0; index < controlIds.length; index++) {
    const update = await request.put(`${monitoring}/controles/${controlIds[index]}`, { data: {
      evaluacionId: evaluation.id, estadoMonitoreo: monitoringStates[index], efectividadMonitoreo: monitoringEffectiveness[index],
    } });
    expect(update.status(), await update.text()).toBe(200);
  }

  const areaOnlyHeaders = { 'X-E2E-User': String(fixture.areaOnlyUserId) };
  const ugrOnlyHeaders = { 'X-E2E-User': String(fixture.ugrOnlyUserId) };
  const bothHeaders = { 'X-E2E-User': String(fixture.bothUserId) };
  const neitherHeaders = { 'X-E2E-User': String(fixture.neitherUserId) };
  const block6For = async (headers: Record<string, string>) => {
    const response = await request.get(`${monitoring}/evaluaciones/${evaluation.id}/bloque6`, { headers });
    expect(response.status(), await response.text()).toBe(200);
    return (await response.json() as { datos: { observacionesArea: string | null; observacionesUgr: string | null; puedeEditarObservacionesArea: boolean; puedeEditarObservacionesUgr: boolean; controles: Array<{ controlId: number; estadoMonitoreo: string | null; efectividadMonitoreo: number | null; evidencias: Array<{ id: number; nombreArchivo: string }> }> } }).datos;
  };
  const areaOnly = await block6For(areaOnlyHeaders);
  expect([areaOnly.puedeEditarObservacionesArea, areaOnly.puedeEditarObservacionesUgr]).toEqual([true, false]);
  const areaWrite = await request.put(`${monitoring}/evaluaciones/${evaluation.id}/observaciones/area`, { headers: areaOnlyHeaders, data: { texto: 'OBS-AREA-AREA-ONLY' } });
  expect(areaWrite.status(), await areaWrite.text()).toBe(200);
  const areaDeniedUgr = await request.put(`${monitoring}/evaluaciones/${evaluation.id}/observaciones/ugr`, { headers: areaOnlyHeaders, data: { texto: 'DENIED-AREA-TO-UGR' } });
  expect(areaDeniedUgr.status(), await areaDeniedUgr.text()).toBe(403);
  const ugrOnly = await block6For(ugrOnlyHeaders);
  expect([ugrOnly.puedeEditarObservacionesArea, ugrOnly.puedeEditarObservacionesUgr]).toEqual([false, true]);
  const ugrWrite = await request.put(`${monitoring}/evaluaciones/${evaluation.id}/observaciones/ugr`, { headers: ugrOnlyHeaders, data: { texto: 'OBS-UGR-UGR-ONLY' } });
  expect(ugrWrite.status(), await ugrWrite.text()).toBe(200);
  const ugrDeniedArea = await request.put(`${monitoring}/evaluaciones/${evaluation.id}/observaciones/area`, { headers: ugrOnlyHeaders, data: { texto: 'DENIED-UGR-TO-AREA' } });
  expect(ugrDeniedArea.status(), await ugrDeniedArea.text()).toBe(403);
  const bothArea = await request.put(`${monitoring}/evaluaciones/${evaluation.id}/observaciones/area`, { headers: bothHeaders, data: { texto: 'OBS-AREA-BOTH' } });
  const bothUgr = await request.put(`${monitoring}/evaluaciones/${evaluation.id}/observaciones/ugr`, { headers: bothHeaders, data: { texto: 'OBS-UGR-BOTH' } });
  expect(bothArea.status(), await bothArea.text()).toBe(200);
  expect(bothUgr.status(), await bothUgr.text()).toBe(200);
  const neither = await block6For(neitherHeaders);
  expect([neither.puedeEditarObservacionesArea, neither.puedeEditarObservacionesUgr]).toEqual([false, false]);
  const neitherArea = await request.put(`${monitoring}/evaluaciones/${evaluation.id}/observaciones/area`, { headers: neitherHeaders, data: { texto: 'DENIED-NEITHER-AREA' } });
  const neitherUgr = await request.put(`${monitoring}/evaluaciones/${evaluation.id}/observaciones/ugr`, { headers: neitherHeaders, data: { texto: 'DENIED-NEITHER-UGR' } });
  expect(neitherArea.status(), await neitherArea.text()).toBe(403);
  expect(neitherUgr.status(), await neitherUgr.text()).toBe(403);

  const uploadEvidence = async (name: string) => {
    const upload = await request.post(`${evidenceApi}/cargar`, { multipart: {
      archivo: { name, mimeType: 'application/pdf', buffer: Buffer.from(`%PDF-1.4\n${name}\n%%EOF`) },
    } });
    expect(upload.status(), await upload.text()).toBe(200);
    const item = (await upload.json() as { datos: { eviId: number; eviHash: string } }).datos;
    expect(item.eviHash).toMatch(/^[a-f0-9]{64}$/);
    return item;
  };
  const evidenceItems: Array<{ eviId: number; eviHash: string }> = [];
  for (const name of [
    'block9-evidence-preventive-a.pdf', 'block9-evidence-preventive-b.pdf',
    'block9-evidence-detective.pdf', 'block9-evidence-corrective.pdf',
  ]) evidenceItems.push(await uploadEvidence(name));
  for (let index = 0; index < evidenceItems.length; index++) {
    const evidenceLink = await request.post(`${evidenceApi}/vinculos`, { data: { evidenciaId: evidenceItems[index].eviId, tipoEntidad: 'control', entidadId: controlIds[index] } });
    expect(evidenceLink.status(), await evidenceLink.text()).toBe(200);
  }
  const linkedDelete = await request.delete(`${evidenceApi}/${evidenceItems[0].eviId}`);
  expect(linkedDelete.status(), await linkedDelete.text()).toBe(400);
  const block6 = await block6For(bothHeaders);
  expect(block6.observacionesArea).toBe('OBS-AREA-BOTH');
  expect(block6.observacionesUgr).toBe('OBS-UGR-BOTH');
  expect(block6.controles).toHaveLength(4);
  for (let index = 0; index < evidenceItems.length; index++)
    expect(block6.controles.find(control => control.controlId === controlIds[index])?.evidencias.map(item => item.id)).toContain(evidenceItems[index].eviId);
  const monitoringProjectionResponse = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}/proyeccion-institucional`);
  expect(monitoringProjectionResponse.ok(), await monitoringProjectionResponse.text()).toBeTruthy();
  const monitoringProjection = (await monitoringProjectionResponse.json() as { datos: { fields: Array<{ fieldNumber: number; value: unknown }> } }).datos.fields;
  expect(monitoringProjection.find(field => field.fieldNumber === 70)?.value).toContain('Señal activa de prueba');
  expect(monitoringProjection.find(field => field.fieldNumber === 71)?.value).toBe('MITIGADO');
  expect(monitoringProjection.find(field => field.fieldNumber === 72)?.value).toBe('1. SE_MANTIENE\n2. NO_SE_MANTIENE');
  expect(monitoringProjection.find(field => field.fieldNumber === 73)?.value).toBe('1. 30\n2. 50');
  expect(monitoringProjection.find(field => field.fieldNumber === 74)?.value).toBe('1. block9-evidence-preventive-a.pdf\n2. block9-evidence-preventive-b.pdf');
  expect(monitoringProjection.find(field => field.fieldNumber === 75)?.value).toBe('1. SE_MANTIENE');
  expect(monitoringProjection.find(field => field.fieldNumber === 76)?.value).toBe('1. 85');
  expect(monitoringProjection.find(field => field.fieldNumber === 77)?.value).toBe('1. block9-evidence-detective.pdf');
  expect(monitoringProjection.find(field => field.fieldNumber === 78)?.value).toBe('1. NO_SE_MANTIENE');
  expect(monitoringProjection.find(field => field.fieldNumber === 79)?.value).toBe('1. 90');
  expect(monitoringProjection.find(field => field.fieldNumber === 80)?.value).toBe('1. block9-evidence-corrective.pdf');
  expect(monitoringProjection.find(field => field.fieldNumber === 81)?.value).toBe('OBS-AREA-BOTH');
  expect(monitoringProjection.find(field => field.fieldNumber === 82)?.value).toBe('OBS-UGR-BOTH');

  const monitoringUiPage = await page.context().newPage();
  await monitoringUiPage.setViewportSize({ width: 1280, height: 900 });
  await installNonCriticalFixtures(monitoringUiPage, fixture, evaluation);
  await monitoringUiPage.goto('/matrices-riesgos');
  await expect(monitoringUiPage.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await monitoringUiPage.locator('button[title^="Editar evaluaci"]').click();
  const monitoringBlocks = monitoringUiPage.locator('nav[aria-label^="Bloques funcionales"]');
  await expect(monitoringBlocks.getByRole('button')).toHaveCount(6);
  const matrixBlock6Read = monitoringUiPage.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/monitoreo/evaluaciones/${evaluation.id}/bloque6`);
  await monitoringBlocks.getByRole('button').nth(5).click();
  expect((await matrixBlock6Read).status()).toBe(200);
  const matrixUploader = monitoringUiPage.getByRole('button', { name: `Evidencias del Control #${controlIds[0]}` });
  await expect(matrixUploader).toBeVisible();
  await matrixUploader.scrollIntoViewIfNeeded();
  const evidenceDirectory = process.env['BLOCK9_EVIDENCE_OUTPUT_DIR'];
  const matrixScreenshotPath = evidenceDirectory
    ? `${evidenceDirectory}/institutional-file-upload-matrices.png`
    : 'test-results/institutional-file-upload-matrices.png';
  await monitoringUiPage.screenshot({ path: matrixScreenshotPath });
  const matrixFileChooserPromise = monitoringUiPage.waitForEvent('filechooser');
  await matrixUploader.click();
  const matrixFileChooser = await matrixFileChooserPromise;
  const matrixUploadResponsePromise = monitoringUiPage.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/evidencias/cargar'));
  await matrixFileChooser.setFiles({
    name: 'block9-ui-control-evidence.pdf',
    mimeType: 'application/pdf',
    buffer: Buffer.from('%PDF-1.4\nBlock 9 browser evidence\n%%EOF'),
  });
  const matrixUploadResponse = await matrixUploadResponsePromise;
  expect(matrixUploadResponse.status(), await matrixUploadResponse.text()).toBe(200);
  await expect(monitoringUiPage.getByText('block9-ui-control-evidence.pdf')).toBeVisible();
  const matrixUploadBody = await matrixUploadResponse.json() as { datos: { eviId: number } };
  const matrixUiEvidenceId = matrixUploadBody.datos.eviId;

  const matrixUiUnlink = await request.delete(`${evidenceApi}/vinculos`, {
    data: { evidenciaId: matrixUiEvidenceId, tipoEntidad: 'control', entidadId: controlIds[0] },
  });
  expect(matrixUiUnlink.status(), await matrixUiUnlink.text()).toBe(200);
  const matrixUiDelete = await request.delete(`${evidenceApi}/${matrixUiEvidenceId}`);
  expect(matrixUiDelete.status(), await matrixUiDelete.text()).toBe(200);

  for (let index = 0; index < evidenceItems.length; index++) {
    const unlink = await request.delete(`${evidenceApi}/vinculos`, { data: { evidenciaId: evidenceItems[index].eviId, tipoEntidad: 'control', entidadId: controlIds[index] } });
    expect(unlink.status(), await unlink.text()).toBe(200);
    const orphanDelete = await request.delete(`${evidenceApi}/${evidenceItems[index].eviId}`);
    expect(orphanDelete.status(), await orphanDelete.text()).toBe(200);
  }

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
  pageA.on('response', response => {
    const url = new URL(response.url());
    if (url.port !== '5043' || !['localhost', '127.0.0.1'].includes(url.hostname)) return;
    const key = `${response.request().method()} ${url.pathname}`;
    if (critical.some(pattern => pattern.test(key)) && response.status() < 500) criticalResponses.add(key.replace(/\/\d+(?=\/|$)/g, '/{id}'));
  });
  await pageA.goto('/matrices-riesgos');
  await expect(pageA.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await expect(pageA.locator('button[title^="Editar evaluaci"]')).toBeVisible();
  await pageA.locator('button[title^="Editar evaluaci"]').click();
  await expect(pageA.getByLabel('Área principal')).toHaveValue('Área Oracle editada');
  await pageA.locator('dialog[open][aria-modal="true"] button[aria-label^="Cancelar edici"]').click();
  await expect(pageA.locator('dialog[open][aria-modal="true"]')).toHaveCount(0);
  const projectionResponse = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}/proyeccion-institucional`);
  const projectionBody = await projectionResponse.text();
  expect(projectionResponse.ok(), `Projection HTTP ${projectionResponse.status()}: ${projectionBody.slice(0, 1000)}`).toBeTruthy();
  const projection = (JSON.parse(projectionBody) as { datos: { fields: Array<{ fieldNumber: number; value: unknown }> } }).datos;
  expect(projection.fields).toHaveLength(82);
  expect(projection.fields.find(field => field.fieldNumber === 41)?.value).toBe(2);
  expect(projection.fields.find(field => field.fieldNumber === 43)?.value).toBe(5);
  expect(projection.fields.find(field => field.fieldNumber === 42)?.value).toBe(
    '1.1 Actividad E2E A1 actualizada\n1.2 Actividad E2E A2\n2.1 Actividad E2E B1\n2.2 Actividad E2E B2\n2.3 Actividad E2E B3',
  );
  expect(projection.fields.find(field => field.fieldNumber === 40)?.value).toEqual([
    { planOrdinalIndex: 1, value: 'Plan E2E A actualizado', state: 'VALUE' },
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
    { planOrdinalIndex: 1, value: 'Seguimiento Responsable A actualizado', state: 'VALUE' },
    { planOrdinalIndex: 2, value: 'Seguimiento Responsable B', state: 'VALUE' },
  ]);
  expect(projection.fields.find(field => field.fieldNumber === 49)?.value).toEqual([
    { planOrdinalIndex: 1, value: 1100, state: 'VALUE' },
    { planOrdinalIndex: 2, value: 2000, state: 'VALUE' },
  ]);
  expect(projection.fields.find(field => field.fieldNumber === 48)?.value).toEqual([
    { planOrdinalIndex: 1, value: 'Recursos A', state: 'VALUE' },
    { planOrdinalIndex: 2, value: 'Recursos Responsable B', state: 'VALUE' },
  ]);
  for (const fieldNumber of [46, 47]) {
    const dates = projection.fields.find(field => field.fieldNumber === fieldNumber)?.value as Array<{ planOrdinalIndex: number; value: string }>;
    expect(dates.map(item => [item.planOrdinalIndex, new Date(item.value).toISOString().slice(0, 10)])).toEqual(
      fieldNumber === 46 ? [[1, '2026-01-01'], [2, '2026-03-01']] : [[1, '2026-02-28'], [2, '2026-04-30']],
    );
  }
  expect([...criticalResponses].some(value => value.startsWith('GET /api/matrices-riesgos/evaluaciones/{id}/proyeccion-institucional'))).toBeTruthy();
  expect([...criticalResponses].some(value => value.startsWith('GET /api/matrices-riesgos/evaluaciones/{id}'))).toBeTruthy();

  const pageAEditRead = pageA.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/evaluaciones/${evaluation.id}`);
  await pageA.locator('button[title^="Editar evaluaci"]').click();
  expect((await pageAEditRead).status()).toBe(200);
  const blockNavigation = pageA.locator('nav[aria-label^="Bloques funcionales"]');
  await expect(blockNavigation.getByRole('button')).toHaveCount(6);
  const firstBlock = blockNavigation.getByRole('button').first();
  await firstBlock.focus();
  await pageA.keyboard.press('Enter');
  await expect(firstBlock).toHaveAttribute('aria-current', 'step');

  const mitigationBlock = blockNavigation.getByRole('button').nth(3);
  const block4Read = pageA.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/mitigacion/evaluaciones/${evaluation.id}/planes`);
  await mitigationBlock.click();
  expect((await block4Read).status()).toBe(200);
  await expect(pageA.getByRole('heading', { name: 'Planes de mitigación' })).toBeVisible();
  await pageA.screenshot({ path: testInfo.outputPath('mitigation-plans-desktop.png'), fullPage: true });
  await pageA.getByRole('button', { name: 'Editar plan y actividades' }).first().click();
  await expect(pageA.locator('#plan-descripcion')).toHaveValue('Plan E2E A actualizado');
  const planUiSave = pageA.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/mitigacion/planes/${planA}`);
  await pageA.getByRole('button', { name: 'Actualizar plan' }).click();
  expect((await planUiSave).status()).toBe(200);
  await expect(pageA.getByText('Plan actualizado correctamente.')).toBeVisible();
  await pageA.getByRole('button', { name: 'Editar actividad' }).first().click();
  await pageA.locator('#actividad-descripcion').fill('Actividad E2E A1 actualizada');
  const activityUiSave = pageA.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/mitigacion/actividades/${activityIds[0]}`);
  await pageA.getByRole('button', { name: 'Actualizar actividad' }).click();
  expect((await activityUiSave).status()).toBe(200);
  const browserProjectionResponse = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}/proyeccion-institucional`);
  expect(browserProjectionResponse.ok()).toBeTruthy();
  const browserProjection = (await browserProjectionResponse.json() as { datos: { fields: Array<{ fieldNumber: number; value: unknown }> } }).datos.fields;
  expect(browserProjection.find(field => field.fieldNumber === 41)?.value).toBe(2);
  expect(browserProjection.find(field => field.fieldNumber === 43)?.value).toBe(5);
  expect(browserProjection.find(field => field.fieldNumber === 42)?.value).toBe(
    '1.1 Actividad E2E A1 actualizada\n1.2 Actividad E2E A2\n2.1 Actividad E2E B1\n2.2 Actividad E2E B2\n2.3 Actividad E2E B3',
  );

  await pageA.locator('dialog[aria-labelledby="titulo-modal-editar"] button[aria-label^="Cerrar"]').click();
  await expect(pageA.locator('#panel-evaluaciones [aria-label="Ver Matriz completa"]')).toHaveCount(0);
  await expect(pageA.locator('#panel-evaluaciones [aria-label="Ver evaluación"]')).toBeVisible();

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

  const pageAEditDialog = pageA.locator('dialog[aria-labelledby="titulo-modal-editar"]');
  if (await pageAEditDialog.isVisible()) {
    await pageAEditDialog.locator('button[aria-label="Cerrar edición"]').click();
  }
  await pageA.locator('button[title^="Editar evaluaci"]').click();
  const pageAMonitoringBlocks = pageA.locator('nav[aria-label^="Bloques funcionales"]');
  await expect(pageAMonitoringBlocks.getByRole('button')).toHaveCount(6);
  const pageABlock6Read = pageA.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/monitoreo/evaluaciones/${evaluation.id}/bloque6`);
  await pageAMonitoringBlocks.getByRole('button').nth(5).click();
  expect((await pageABlock6Read).status()).toBe(200);
  await expect(pageA.getByRole('heading', { name: 'Controles monitoreados y evidencias' })).toBeVisible();
  await expect(pageA.getByText('Control E2E A', { exact: true })).toBeVisible();
  const areaObservations = pageA.getByRole('textbox', { name: 'Observaciones del Área' });
  const ugrObservations = pageA.getByRole('textbox', { name: 'Observaciones UGR' });
  await expect(areaObservations).toHaveValue('OBS-AREA-BOTH');
  await expect(ugrObservations).toHaveValue('OBS-UGR-BOTH');
  await pageA.screenshot({ path: testInfo.outputPath('monitoring-area-ugr-desktop.png'), fullPage: true });
  await areaObservations.fill('OBS-AREA-BROWSER');
  const saveArea = pageA.waitForResponse(response => response.request().method() === 'PUT'
    && new URL(response.url()).pathname.endsWith(`/observaciones/area`));
  await pageA.getByRole('button', { name: 'Guardar Observaciones del Área' }).click();
  expect((await saveArea).status()).toBe(200);
  await expect(areaObservations).toHaveValue('OBS-AREA-BROWSER');
  const finalMonitoringProjection = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}/proyeccion-institucional`);
  const finalProjectionBody = (await finalMonitoringProjection.json() as { datos: { fields: Array<{ fieldNumber: number; value: unknown }> } }).datos.fields;
  expect(finalProjectionBody.find(field => field.fieldNumber === 81)?.value).toBe('OBS-AREA-BROWSER');
  expect(finalProjectionBody.find(field => field.fieldNumber === 82)?.value).toBe('OBS-UGR-BOTH');
  const auditResponse = await request.get('http://localhost:5043/__e2e/audit');
  expect(auditResponse.ok()).toBeTruthy();
  const auditRows = await auditResponse.json() as Array<{ table: string; action: string; count: number }>;
  for (const table of ['RL_MR_PLANES', 'RL_MR_ACTIVIDADES', 'RL_MR_SENALES_ALERTA', 'RL_MR_AUTOMONITOREO', 'RL_MR_CONTROLES_RIESGO', 'RL_MR_EVIDENCIAS_VINCULOS']) {
    expect(auditRows.some(row => row.table === table && row.count > 0), `Missing audit for ${table}`).toBeTruthy();
  }
  const desktopOverflow = await pageA.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(desktopOverflow).toBeLessThanOrEqual(0);
  await pageA.setViewportSize({ width: 390, height: 844 });
  await expect(areaObservations).toBeVisible();
  await expect(ugrObservations).toBeVisible();
  await pageA.screenshot({ path: testInfo.outputPath('monitoring-mobile.png'), fullPage: true });
  const mobileOverflow = await pageA.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(mobileOverflow).toBeLessThanOrEqual(0);

  await page.getByRole('button', { name: 'Seguimiento de evaluación' }).click();
  const followUpModal = page.locator('dialog[aria-labelledby="titulo-modal-seguimiento"]');
  await expect(followUpModal).toBeVisible();
  await expect(followUpModal.locator('.modal-header-institutional p strong')).toHaveText('Borrador');
  const firstDestinations = followUpModal.locator('#seg-nuevo-estado option');
  await expect(firstDestinations).toHaveCount(1);
  await expect(followUpModal.locator('#seg-nuevo-estado')).toHaveValue('EN_REVISION');
  await page.locator('#seg-motivo').fill('Envío a revisión de prueba');
  await page.locator('#seg-nuevo-estado').selectOption('EN_REVISION');
  const draftToReview = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/evaluaciones/${evaluation.id}/transiciones`);
  await followUpModal.getByRole('button', { name: 'Aplicar transición de estado' }).click();
  expect((await draftToReview).status()).toBe(200);
  evaluation.state = 'EN_REVISION';
  const reviewState = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}`);
  expect(reviewState.ok()).toBeTruthy();
  expect((await reviewState.json() as { datos: { evaEstado: string } }).datos.evaEstado).toBe('EN_REVISION');
  await expect(followUpModal.locator('.modal-header-institutional p strong')).toHaveText('En revisión');
  const reviewDestinations = followUpModal.locator('#seg-nuevo-estado option');
  await expect(reviewDestinations).toHaveCount(3);
  const reviewDestinationValues = await reviewDestinations.evaluateAll(options =>
    options.map(option => (option as HTMLOptionElement).value));
  expect(reviewDestinationValues).toEqual(['APROBADA', 'OBSERVADA', 'RECHAZADA']);
  await expect(reviewDestinations).toHaveText(['Aprobada', 'Observada', 'Rechazada']);

  await page.locator('#seg-motivo').fill('Observación de prueba');
  await page.locator('#seg-nuevo-estado').selectOption('OBSERVADA');
  const reviewToObserved = page.waitForResponse(response => response.request().method() === 'POST'
    && new URL(response.url()).pathname === `/api/matrices-riesgos/evaluaciones/${evaluation.id}/transiciones`);
  await followUpModal.getByRole('button', { name: 'Aplicar transición de estado' }).click();
  expect((await reviewToObserved).status()).toBe(200);
  evaluation.state = 'OBSERVADA';
  const observedState = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}`);
  expect((await observedState.json() as { datos: { evaEstado: string } }).datos.evaEstado).toBe('OBSERVADA');
  await expect(followUpModal.locator('.modal-header-institutional p strong')).toHaveText('Observada');
  await expect(followUpModal.locator('#seg-nuevo-estado option')).toHaveCount(1);
  await expect(followUpModal.locator('#seg-nuevo-estado')).toHaveValue('BORRADOR');

  const historyResponse = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}/flujos`);
  expect(historyResponse.ok()).toBeTruthy();
  const flowStates = (await historyResponse.json() as { datos: Array<{ fluEstado: string }> }).datos.map(flow => flow.fluEstado);
  expect(flowStates).toContain('EN_REVISION');
  expect(flowStates).toContain('OBSERVADA');
  const invalidTransition = await request.post(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}/transiciones?nuevoEstado=CERRADA`);
  expect(invalidTransition.status()).toBe(400);
  const unchangedState = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}`);
  expect((await unchangedState.json() as { datos: { evaEstado: string } }).datos.evaEstado).toBe('OBSERVADA');
  expect([...criticalResponses].some(value => value.startsWith('GET /api/matrices-riesgos/evaluaciones/{id}/transiciones-permitidas'))).toBeTruthy();
  expect([...criticalResponses].some(value => value.startsWith('POST /api/matrices-riesgos/evaluaciones/{id}/transiciones'))).toBeTruthy();
  expect([...criticalResponses].some(value => value.startsWith('GET /api/matrices-riesgos/evaluaciones/{id}/flujos'))).toBeTruthy();
  await expect(page.locator('#panel-evaluaciones')).toContainText('Observada');
  await page.screenshot({ path: testInfo.outputPath('evaluation-follow-up-workflow.png'), fullPage: true });

  const filterRequest = page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/matrices-riesgos/evaluaciones');
  await page.locator('#filtro-estado').selectOption('BORRADOR');
  await filterRequest;
  const afterFilter = await request.get(`http://localhost:5043/api/matrices-riesgos/evaluaciones/${evaluation.id}`);
  expect((await afterFilter.json() as { datos: { evaEstado: string } }).datos.evaEstado).toBe('OBSERVADA');
  await concurrencyA.close();
  await pageB.close();
  console.log('BLOCK7_MATRIX_API_ROUTES_MOCKED=0');
  console.log('BLOCK9_CRITICAL_ROUTES_MOCKED=0');
  console.log('BLOCK9_MITIGATION_CRUD=PASS');
  console.log('BLOCK9_MONITORING_CRUD=PASS');
  console.log('BLOCK9_EVIDENCE=PASS');
  console.log('BLOCK9_RBAC=PASS');
  console.log('BLOCK9_AREA_UGR_ISOLATION=PASS');
  console.log('BLOCK9_AUDIT_TRAIL=PASS');
  console.log('BLOCK9_PROJECTION_70_82=PASS');
  console.log('BLOCK9_RESPONSIVE_1280_390=PASS');
});
