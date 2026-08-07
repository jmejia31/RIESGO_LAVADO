import { expect, Page, test } from '@playwright/test';

function tokenAdministrador(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '1', uid: 'admin.uat', email: 'admin.uat@ihss.hn', given_name: 'Admin', family_name: 'UAT',
    role: 'ADMINISTRADOR', rol_id: '1', modulos: '10', debe_cambiar_pass: '0', exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

const version = {
  verId: 10, verFamiliaId: 1, verCodigo: 'MATRIZ_RIESGOS_LAFT_V1', verVersion: 1,
  verJson: JSON.stringify({ codigoFormulario: 'MATRIZ_RIESGOS_LAFT_V1', nombreFormulario: 'Matriz', secciones: [] }),
  verHash: 'uat-hash', verEstado: 'PUBLISHED', verVigente: true, verFechaCreacion: '2026-08-07T12:00:00Z', verUsrCreacion: 1
};
const evaluacion = {
  evaId: 20, evaRiesgoId: 7, evaVersionId: 10, evaEstado: 'BORRADOR', evaDataJson: '{}', evaDataCalcJson: '{}',
  evaVri: 7, evaVrr: 4, evaFechaEval: '2026-08-07T12:00:00Z', evaUsrEval: 1, evaVersionRow: 1, evaActivo: true
};
const riesgo = { rieId: 7, rieCodigo: 'R-007', rieNombre: 'Riesgo UAT', rieDescripcion: 'Base UAT', rieActivo: true, rieUsrCreacion: 1, rieFechaCreacion: '2026-08-07T12:00:00Z' };

async function preparar(page: Page): Promise<void> {
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'uat-refresh');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, tokenAdministrador());

  await page.route('**/api/configuracion/sistema', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: { nombreSistema: 'SGRLA-IHSS', nombreInstitucion: 'IHSS', colorPrimario: '#1e3a8a', colorSecundario: '#1d4ed8', timeoutSesion: 30 } }) }));
  await page.route('**/api/configuracion/login', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) }));

  await page.route('**/api/matrices-riesgos/**', route => {
    const req = route.request();
    const path = new URL(req.url()).pathname;
    const method = req.method();
    let datos: unknown = [];

    if (path.endsWith('/formulario/version-vigente')) datos = version;
    else if (path.endsWith('/metodologia/vigente')) datos = { versionFormularioId: 10, codigo: version.verCodigo, version: 1, secciones: [], catalogos: [], reglas: [] };
    else if (path.endsWith('/formularios/historial')) datos = [version];
    else if (path.endsWith('/evaluaciones') && method === 'GET') datos = [evaluacion];
    else if (path.endsWith('/riesgos') && method === 'GET') datos = [riesgo];
    else if (path.endsWith('/consolidado')) datos = [];
    else if (path.endsWith('/mitigacion/evaluaciones/20/controles')) datos = [];
    else if (path.endsWith('/mitigacion/evaluaciones/20/planes')) datos = [];
    else if (path.endsWith('/mitigacion/controles/31/evaluaciones')) datos = [];
    else if (path.endsWith('/mitigacion/planes/41/actividades')) datos = [];
    else if (path.endsWith('/monitoreo/evaluaciones/20/alertas')) datos = [];
    else if (path.endsWith('/monitoreo/evaluaciones/20/automonitoreo')) datos = [];
    else if (path.endsWith('/monitoreo/resumen')) datos = { fechaGeneracion: '2026-08-07T12:00:00Z', riesgosActivos: 1, evaluacionesActivas: 1, evaluacionesAprobadas: 0, riesgosAltoCritico: 0, alertasActivas: 0, planesAbiertos: 0, actividadesVencidas: 0, automonitoreosUltimos30Dias: 0 };

    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });
  await page.route('**/api/matrices-riesgos*', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [evaluacion] }) }));
}

test.beforeEach(async ({ page }) => preparar(page));

test('UAT administra un riesgo desde la interfaz integral', async ({ page }) => {
  let payload: any;
  await page.route('**/api/matrices-riesgos/riesgos', async route => {
    if (route.request().method() === 'POST') {
      payload = route.request().postDataJSON();
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 8 }) });
    }
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [riesgo] }) });
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Riesgos', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Gestión de riesgos' })).toBeVisible();
  await page.getByLabel('Código', { exact: true }).fill('R-008');
  await page.getByLabel('Nombre', { exact: true }).fill('Riesgo integral UAT');
  await page.getByLabel('Descripción', { exact: true }).fill('Creado por prueba UAT');
  await page.getByRole('button', { name: 'Crear riesgo' }).click();
  await expect.poll(() => payload?.rieCodigo).toBe('R-008');
  await expect(page.getByText('Riesgo creado correctamente.')).toBeVisible();
});

test('UAT registra control, efectividad, plan y actividad', async ({ page }) => {
  const recibidos: Record<string, any> = {};
  await page.route('**/api/matrices-riesgos/mitigacion/**', route => {
    const req = route.request();
    const path = new URL(req.url()).pathname;
    if (req.method() === 'POST' && path.endsWith('/mitigacion/controles')) { recibidos.control = req.postDataJSON(); return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 31 }) }); }
    if (req.method() === 'POST' && path.endsWith('/mitigacion/controles/31/evaluaciones')) { recibidos.efectividad = req.postDataJSON(); return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 32 }) }); }
    if (req.method() === 'POST' && path.endsWith('/mitigacion/planes')) { recibidos.plan = req.postDataJSON(); return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 41 }) }); }
    if (req.method() === 'POST' && path.endsWith('/mitigacion/actividades')) { recibidos.actividad = req.postDataJSON(); return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 51 }) }); }
    return route.fallback();
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Mitigación', exact: true }).click();
  await page.getByLabel('Evaluación', { exact: true }).selectOption('20');
  await page.getByLabel('Descripción', { exact: true }).first().fill('Control preventivo UAT');
  await page.getByRole('button', { name: 'Crear control' }).click();
  await expect.poll(() => recibidos.control?.conEvaluacionId).toBe(20);

  await page.route('**/api/matrices-riesgos/mitigacion/evaluaciones/20/controles', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [{ conId: 31, conEvaluacionId: 20, conTipo: 'PREVENTIVO', conDescripcion: 'Control preventivo UAT', conAutomatizacion: 'MANUAL', conEstado: 'ACTIVO' }] }) }));
  await page.getByLabel('Evaluación', { exact: true }).selectOption('20');
  await page.getByRole('button', { name: 'Editar / evaluar' }).click();
  await page.getByLabel('Efectividad %').fill('85');
  await page.getByRole('button', { name: 'Registrar efectividad' }).click();
  await expect.poll(() => recibidos.efectividad?.ecoEfectividad).toBe(85);

  await page.getByLabel('Descripción', { exact: true }).nth(1).fill('Plan UAT');
  await page.getByRole('button', { name: 'Crear plan' }).click();
  await expect.poll(() => recibidos.plan?.plaEvaluacionId).toBe(20);

  await page.route('**/api/matrices-riesgos/mitigacion/evaluaciones/20/planes', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [{ plaId: 41, plaEvaluacionId: 20, plaDescripcion: 'Plan UAT', plaAvance: 0, plaPresupuesto: 0, plaFechaInicio: '2026-08-07T00:00:00Z', plaFechaFin: '2026-08-08T00:00:00Z', plaEstado: 'ABIERTO' }] }) }));
  await page.getByLabel('Evaluación', { exact: true }).selectOption('20');
  await page.getByRole('button', { name: 'Editar / actividades' }).click();
  await page.getByLabel('Descripción', { exact: true }).last().fill('Actividad UAT');
  await page.getByLabel('Responsable', { exact: true }).fill('Responsable UAT');
  await page.getByRole('button', { name: 'Crear actividad' }).click();
  await expect.poll(() => recibidos.actividad?.actPlanId).toBe(41);
});

test('UAT registra alerta y automonitoreo operativo', async ({ page }) => {
  const recibidos: Record<string, any> = {};
  await page.route('**/api/matrices-riesgos/monitoreo/**', route => {
    const req = route.request();
    const path = new URL(req.url()).pathname;
    if (req.method() === 'POST' && path.endsWith('/monitoreo/alertas')) { recibidos.alerta = req.postDataJSON(); return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 61 }) }); }
    if (req.method() === 'POST' && path.endsWith('/monitoreo/automonitoreo')) { recibidos.monitoreo = req.postDataJSON(); return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 71 }) }); }
    return route.fallback();
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Monitoreo', exact: true }).click();
  await page.getByLabel('Evaluación', { exact: true }).selectOption('20');
  await page.getByLabel('Código', { exact: true }).fill('ALE-UAT');
  await page.getByLabel('Indicador', { exact: true }).fill('Umbral operativo UAT');
  await page.getByRole('button', { name: 'Registrar alerta' }).click();
  await expect.poll(() => recibidos.alerta?.aleCodigo).toBe('ALE-UAT');

  await page.getByLabel('Estado del riesgo').fill('CONTROLADO');
  await page.getByLabel('Estado de controles').fill('EFECTIVO');
  await page.getByLabel('Resultado').fill('Seguimiento conforme');
  await page.getByRole('button', { name: 'Registrar automonitoreo' }).click();
  await expect.poll(() => recibidos.monitoreo?.monResultado).toBe('Seguimiento conforme');
});
