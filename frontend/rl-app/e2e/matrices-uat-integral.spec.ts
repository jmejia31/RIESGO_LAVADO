import { expect, Page, test } from '@playwright/test';
import { Buffer } from 'node:buffer';
import * as ExcelJS from 'exceljs';

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
const calculosAuxiliares = {
  riesgo_residual_descripcion: 'Riesgo UAT', frecuencia_residual: 1, impacto_residual: 1,
  valor_riesgo_residual: 1, nivel_riesgo_residual: 'Riesgo no significativo',
  frecuencia_residual_aux: 0.3, impacto_residual_aux: 0.5, suma_residual_redondeada_aux: 2,
  f_base: 1, i_base: 1, tope_f: 3, tope_i: 5, capacidad_f_aux: 2, capacidad_i_aux: 4,
  resto_aux: 0, prefiere_i_aux: 1, incremento_i_aux: 0, incremento_f_aux: 0,
  valor_riesgo_residual_aux: 1, verificacion: 0, vrr_2: 1, verificar_vrr_2: 0,
  verificar_frecuencia: 2, verificar_impacto: 4, diferencia_vri_vrr: 6
};
const evaluacion = {
  evaId: 20, evaRiesgoId: 7, evaVersionId: 10, evaEstado: 'BORRADOR',
  evaDataJson: JSON.stringify({ area_principal: 'Área de Cumplimiento', frecuencia_inherente: '5', impacto_inherente: '5', dueno_riesgo: 'Responsable UAT', controles_preventivo: 90, controles_detectivo: 50, controles_correctivo: 30, frecuencia_residual_aux: 999, verificacion: 999 }),
  evaDataCalcJson: JSON.stringify({ nivel_riesgo_inherente: 'Riesgo Moderado', ...calculosAuxiliares }),
  evaVri: 7, evaVrr: 4, evaFechaEval: '2026-08-07T12:00:00Z', evaUsrEval: 1, evaVersionRow: 1, evaActivo: true
};
const riesgo = { rieId: 7, rieCodigo: 'R-007', rieNombre: 'Riesgo UAT', rieDescripcion: 'Base UAT', rieActivo: true, rieUsrCreacion: 1, rieFechaCreacion: '2026-08-07T12:00:00Z' };
const controles = [
  { conId: 31, conEvaluacionId: 20, conTipo: 'PREVENTIVO', conDescripcion: 'Control preventivo UAT', conAutomatizacion: 'MANUAL', conEstado: 'ACTIVO' },
  { conId: 32, conEvaluacionId: 20, conTipo: 'PREVENTIVO', conDescripcion: 'Segundo control preventivo UAT', conAutomatizacion: 'AUTOMATICO', conEstado: 'ACTIVO' },
  { conId: 33, conEvaluacionId: 20, conTipo: 'DETECTIVO', conDescripcion: 'Control detectivo UAT', conAutomatizacion: 'SEMIAUTOMATICO', conEstado: 'ACTIVO' },
  { conId: 34, conEvaluacionId: 20, conTipo: 'CORRECTIVO', conDescripcion: 'Control correctivo UAT', conAutomatizacion: 'AUTOMATICO', conEstado: 'ACTIVO' }
];
const pdfFixture = '%PDF-1.4\n1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\ntrailer\n<< /Root 1 0 R >>\n%%EOF';

async function preparar(page: Page): Promise<void> {
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'uat-refresh');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, tokenAdministrador());

  await page.route('**/api/configuracion/sistema', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: { nombreSistema: 'SGRLA-IHSS', nombreInstitucion: 'IHSS', colorPrimario: '#1e3a8a', colorSecundario: '#1d4ed8', timeoutSesion: 30 } }) }));
  await page.route('**/api/configuracion/login', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) }));
  await page.route('**/api/catalogos/modulos', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) }));

  await page.route('**/api/matrices-riesgos/**', async route => {
    const req = route.request();
    const path = new URL(req.url()).pathname;
    const method = req.method();
    if (path.endsWith('/reportes/consolidado.pdf')) {
      return route.fulfill({ status: 200, contentType: 'application/pdf', body: Buffer.from(pdfFixture) });
    }
    if (path.endsWith('/reportes/consolidado.xlsx')) {
      const reportWorkbook = new ExcelJS.Workbook();
      const reportSheet = reportWorkbook.addWorksheet('Coincidencias');
      reportSheet.addRows([
        ['Código', 'Nombre', 'Estado'],
        ['R-001', 'Riesgo UAT 1', 'ACTIVO'],
        ['R-002', 'Riesgo UAT 2', 'ACTIVO']
      ]);
      const reportBuffer = await reportWorkbook.xlsx.writeBuffer();
      const reportBody = Buffer.isBuffer(reportBuffer) ? reportBuffer : Buffer.from(reportBuffer as ArrayBuffer);
      return route.fulfill({ status: 200, contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', body: reportBody });
    }
    let datos: unknown = [];

    if (path.endsWith('/formulario/version-vigente')) datos = version;
    else if (path.endsWith('/formularios/10')) datos = version;
    else if (path.endsWith('/metodologia/vigente') || path.includes('/metodologia/version/')) datos = { versionFormularioId: 10, codigo: version.verCodigo, version: 1, secciones: [], catalogos: [], reglas: [] };
    else if (path.endsWith('/formularios/historial')) datos = [version];
    else if (path.endsWith('/evaluaciones/20') && method === 'GET') datos = evaluacion;
    else if (path.endsWith('/evaluaciones') && method === 'GET') datos = {
      items: [{
        evaId: 20,
        evaRiesgoId: 7,
        riesgoCodigo: 'R-007',
        riesgoNombre: 'Riesgo UAT',
        evaVersionId: 10,
        versionCodigo: 'MATRIZ_RIESGOS_LAFT_V1',
        versionNumero: 1,
        estado: 'BORRADOR',
        vri: 7,
        vrr: 4,
        nivelResidual: 'MEDIO',
        fechaEval: '2026-08-07T12:00:00Z'
      }],
      pagina: 1,
      registrosPorPagina: 10,
      totalRegistros: 1,
      totalPaginas: 1
    };
    else if (path.endsWith('/familias/paginado')) datos = { items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalFamilias: 0, activas: 0, inactivas: 0, totalVersiones: 0 } };
    else if (path.endsWith('/riesgos/paginado')) datos = { items: [riesgo], pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1 };
    else if (path.endsWith('/riesgos/7') && method === 'GET') datos = riesgo;
    else if (path.endsWith('/riesgos/paginado') && method === 'GET') datos = { items: [riesgo], pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1 };
    else if (path.endsWith('/riesgos') && method === 'GET') datos = [riesgo];
    else if (path.endsWith('/consolidado/paginado')) datos = { items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0, totales: { totalRiesgos: 0, totalConEvaluacionOficial: 0, totalSinEvaluacionOficial: 0, totalAltoCritico: 0 } };
    else if (path.endsWith('/consolidado')) datos = [];
    else if (path.endsWith('/mitigacion/evaluaciones/20/controles')) datos = controles;
    else if (path.endsWith('/mitigacion/evaluaciones/20/planes')) datos = [];
    else if (path.endsWith('/mitigacion/evaluaciones/20/bloque4')) datos = { cantidadAcciones: 0, planes: [] };
    else if (path.endsWith('/mitigacion/controles/31/evaluaciones')) datos = [];
    else if (path.endsWith('/mitigacion/planes/41/actividades')) datos = [];
    else if (path.endsWith('/monitoreo/evaluaciones/20/alertas')) datos = [];
    else if (path.endsWith('/monitoreo/evaluaciones/20/automonitoreo')) datos = [];
    else if (path.endsWith('/monitoreo/evaluaciones/20/bloque6')) datos = { senalesAlerta: [{ aleId: 501, aleCodigo: 'AL-01', aleIndicador: 'Alerta UAT 1', aleEstado: 'ACTIVO' }, { aleId: 502, aleCodigo: 'AL-02', aleIndicador: 'Alerta UAT 2', aleEstado: 'ACTIVO' }], estadoRiesgo: 'ALTO', controles: [{ controlId: 31, tipo: 'PREVENTIVO', descripcion: 'Control preventivo UAT', estadoMonitoreo: 'En seguimiento', efectividadMonitoreo: 80, evidencias: [{ id: 601, nombreArchivo: 'P1.pdf' }, { id: 602, nombreArchivo: 'P2.pdf' }] }, { controlId: 33, tipo: 'DETECTIVO', descripcion: 'Control detectivo UAT', estadoMonitoreo: 'Revisado', efectividadMonitoreo: 70, evidencias: [{ id: 603, nombreArchivo: 'D1.pdf' }, { id: 604, nombreArchivo: 'D2.pdf' }] }, { controlId: 34, tipo: 'CORRECTIVO', descripcion: 'Control correctivo UAT', estadoMonitoreo: 'En seguimiento', efectividadMonitoreo: 60, evidencias: [{ id: 605, nombreArchivo: 'C1.pdf' }, { id: 606, nombreArchivo: 'C2.pdf' }] }], observacionesArea: 'Área fixture', observacionesUgr: 'UGR fixture', puedeEditarObservacionesArea: false, puedeEditarObservacionesUgr: false };
    else if (path.endsWith('/monitoreo/resumen')) datos = { fechaGeneracion: '2026-08-07T12:00:00Z', riesgosActivos: 1, evaluacionesActivas: 1, evaluacionesAprobadas: 0, riesgosAltoCritico: 0, alertasActivas: 0, planesAbiertos: 0, actividadesVencidas: 0, automonitoreosUltimos30Dias: 0 };

    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });
  await page.route('**/api/matrices-riesgos*', route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.endsWith('/evaluaciones')) return route.fallback();
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
      success: true,
      datos: {
        items: [{
          evaId: 20,
          evaRiesgoId: 7,
          riesgoCodigo: 'R-007',
          riesgoNombre: 'Riesgo UAT',
          evaVersionId: 10,
          versionCodigo: 'MATRIZ_RIESGOS_LAFT_V1',
          versionNumero: 1,
          estado: 'BORRADOR',
          vri: 7,
          vrr: 4,
          nivelResidual: 'MEDIO',
          fechaEval: '2026-08-07T12:00:00Z'
        }],
        pagina: 1,
        registrosPorPagina: 10,
        totalRegistros: 1,
        totalPaginas: 1
      }
    }) });
  });
}

test.beforeEach(async ({ page }) => preparar(page));

test('UAT abre Matriz completa desde una evaluación y conserva la navegación histórica', async ({ page }) => {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const requestFailures: string[] = [];
  const familiaUnoRequests: string[] = [];
  const unexpectedHttpErrors: string[] = [];
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('requestfailed', request => requestFailures.push(request.url()));
  page.on('request', request => { if (/\/familias\/1(?:\?|$)/.test(new URL(request.url()).pathname + new URL(request.url()).search)) familiaUnoRequests.push(request.url()); });
  page.on('response', response => { if (response.status() >= 400) unexpectedHttpErrors.push(`${response.status()} ${response.url()}`); });
  await page.goto('/matrices-riesgos');
  await expect(page.getByRole('tab', { name: 'Evaluaciones', exact: true })).toBeVisible({ timeout: 15000 });
  await expect(page.getByRole('tab', { name: 'Consolidado', exact: true })).toBeVisible();
  await expect(page.getByRole('tab', { name: 'Matriz completa', exact: true })).toHaveCount(0);
  await expect(page.getByRole('tab', { name: 'Plantillas', exact: true })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'EVALUACIÓN' })).toBeVisible();

  await page.getByRole('button', { name: 'Ver Matriz completa' }).first().click();
  const modalMatriz = page.locator('[data-matrix-modal="complete"]');
  await expect(modalMatriz).toBeVisible();
  await expect(modalMatriz.locator('.modal-size-workspace')).toBeVisible();
  const view = modalMatriz.locator('[data-matrix-view="complete"]');
  await expect(view.getByRole('heading', { name: '1. Identificación y Riesgo Inherente' })).toBeVisible();
  await expect(view.getByRole('heading', { name: '2. Controles' })).toBeVisible();
  await expect(view.getByRole('heading', { name: '3. Riesgo Residual y Respuesta' })).toBeVisible();
  const fields = view.locator('[data-matrix-field]');
  await expect(fields).toHaveCount(82);
  const block2Labels = [
    'Descripción de Control(es) Preventivo(s)', 'Escala de efectividad de control(es) preventivo(s)',
    'Nivel de efectividad de control(es) preventivo(s)', '% efectividad de control(es) preventivo(s)',
    'Descripción de Control(es) Detectivo(s)', 'Escala de efectividad de control(es) detectivo(s)',
    'Nivel de efectividad de control(es) detectivo(s)', '% efectividad de control detectivo',
    'Descripción de Control(es) Correctivo(s)', 'Escala de efectividad de control(es) correctivo(s)',
    'Nivel de efectividad de control(es) correctivo(s)', '% efectividad de control correctivo',
    'Nivel de Automatización de los Controles', 'Efectividad Total Ponderada de los Controles'
  ];
  for (let index = 0; index < block2Labels.length; index++) await expect(fields.nth(index + 19)).toContainText(block2Labels[index]);
  for (const index of [21, 22, 25, 26, 29, 30, 32]) await expect(fields.nth(index).locator('[aria-readonly="true"]')).toBeVisible();
  await expect(fields.nth(19)).toContainText('Control preventivo UAT');
  await expect(fields.nth(19)).toContainText('Segundo control preventivo UAT');
  await expect(fields.nth(23)).toContainText('Control detectivo UAT');
  await expect(fields.nth(27)).toContainText('Control correctivo UAT');
  await expect(fields.nth(31)).toContainText('Manual');
  await expect(fields.nth(31)).toContainText('Semiautomático');
  await expect(fields.nth(31)).toContainText('Automático');

  const block3Labels = [
    'Riesgo Residual',
    'Frecuencia Residual',
    'Impacto Residual',
    'Valor del Riesgo Residual',
    'Nivel del Riesgo Residual',
    'Respuesta al riesgo'
  ];
  for (let index = 0; index < block3Labels.length; index++) {
    await expect(fields.nth(index + 33)).toContainText(block3Labels[index]);
  }
  for (const index of [33, 34, 35, 36, 37]) {
    await expect(fields.nth(index).locator('[aria-readonly="true"]')).toBeVisible();
  }

  await expect(view.getByRole('heading', { name: '4. Plan de Mitigación / Acciones Correctivas' })).toBeVisible();
  await expect(fields.nth(39)).toContainText('Plan de Mitigación/Acciones Correctivas');
  await expect(fields.nth(40)).toContainText('No. Acciones de Mitigación');
  await expect(fields.nth(40)).toContainText('0');
  const block5 = view.locator('[data-matrix-block="5"]');
  await expect(block5.getByRole('heading', { name: '5. Cálculos Auxiliares y Verificaciones' })).toBeVisible();
  await expect(block5).toContainText('20 campos · Calculados automáticamente');
  const detailToggle = block5.getByRole('button', { name: 'Mostrar detalle' });
  await expect(detailToggle).toHaveAttribute('aria-expanded', 'false');
  const block5Fields = block5.locator('[data-matrix-field]');
  await expect(block5Fields).toHaveCount(20);
  await expect(block5Fields.first()).toBeHidden();
  await expect(block5Fields.last()).toBeHidden();
  await detailToggle.click();
  await expect(block5.getByRole('button', { name: 'Ocultar detalle' })).toHaveAttribute('aria-expanded', 'true');
  await expect(block5Fields).toHaveCount(20);
  await expect(block5Fields.first()).toBeVisible();
  await expect(block5Fields.last()).toBeVisible();
  await expect(block5Fields.evaluateAll(items => items.map(item => item.getAttribute('data-matrix-field')))).resolves.toEqual(
    Array.from({ length: 20 }, (_, index) => String(index + 50))
  );
  for (const item of await block5Fields.all()) await expect(item.locator('[aria-readonly="true"]')).toBeVisible();
  await expect(block5.locator('#matrix-block-5-details input, #matrix-block-5-details textarea, #matrix-block-5-details select, #matrix-block-5-details button')).toHaveCount(0);
  const expectedAuxiliaryValues = ['0.3', '0.5', '2', '1', '1', '3', '5', '2', '4', '0', '1', '0', '0', '1', '0', '1', '0', '2', '4', '6'];
  for (let index = 0; index < expectedAuxiliaryValues.length; index++) {
    await expect(block5Fields.nth(index).locator('[aria-readonly="true"]')).toHaveText(expectedAuxiliaryValues[index]);
  }
  await expect(block5Fields.nth(14)).toContainText('PASS');
  await expect(block5Fields.nth(16)).toContainText('PASS');
  await expect(block5Fields.nth(17)).not.toContainText('Revisar');
  await expect(block5Fields.nth(18)).not.toContainText('Revisar');
  await expect(block5Fields.nth(19)).not.toContainText('Revisar');
  await expect(view.locator('[data-matrix-block="6"]')).toContainText('6. Monitoreo, Efectividad y Observaciones');
  await expect(view.locator('[data-matrix-block="6"] [data-matrix-field-definition]')).toHaveCount(13);
  const block6Fields = view.locator('[data-matrix-block="6"] [data-matrix-field-definition]');
  await expect(block6Fields.evaluateAll(items => items.map(item => item.getAttribute('data-matrix-field-definition')))).resolves.toEqual(
    Array.from({ length: 13 }, (_, index) => String(index + 70))
  );
  await expect(block6Fields.nth(0)).toContainText('Señales de Alerta');
  await expect(block6Fields.nth(0).locator('[data-alert-id]')).toHaveCount(2);
  await expect(block6Fields.nth(1)).toContainText('ALTO');
  await expect(block6Fields.nth(1)).not.toContainText('BORRADOR');
  await expect(block6Fields.nth(2)).toContainText('En seguimiento');
  await expect(block6Fields.nth(3)).toContainText('80');
  await expect(block6Fields.nth(4)).toContainText('P1.pdf');
  await expect(block6Fields.nth(4)).toContainText('P2.pdf');
  await expect(block6Fields.nth(4)).not.toContainText('D1.pdf');
  await expect(block6Fields.nth(5)).toContainText('Revisado');
  await expect(block6Fields.nth(7)).toContainText('D1.pdf');
  await expect(block6Fields.nth(7)).not.toContainText('P1.pdf');
  await expect(block6Fields.nth(8)).toContainText('En seguimiento');
  await expect(block6Fields.nth(10)).toContainText('C1.pdf');
  await expect(block6Fields.nth(10)).not.toContainText('D1.pdf');
  await expect(block6Fields.nth(11)).toContainText('Área fixture');
  await expect(block6Fields.nth(12)).toContainText('UGR fixture');
  await expect(fields.evaluateAll(items => items.filter(item => item.getClientRects().length > 0).map(item => item.getAttribute('data-matrix-field')))).resolves.toEqual(
    Array.from({ length: 82 }, (_, index) => String(index + 1).padStart(2, '0'))
  );
  await expect(fields.nth(11).locator('[aria-readonly="true"]')).toBeVisible();
  await expect(fields.nth(12).locator('[aria-readonly="true"]')).toBeVisible();
  await expect(fields.nth(16)).toContainText('Amenazas (Solo para riesgos de GTIC)');
  await expect(fields.nth(17)).toContainText('Vulnerabilidades (Solo para riesgos de GTIC)');
  await expect(fields.nth(18)).toContainText('Activos de Información (Solo para riesgos de GTIC)');
  await expect(fields.nth(3)).toContainText('No disponible en esta versión');
  await expect(fields.nth(20)).toContainText('No disponible en esta versión');
  await expect(fields.nth(21)).toContainText('No disponible en esta versión');
  await expect(fields.nth(22)).toContainText('90%');
  await expect(fields.nth(24)).toContainText('No disponible en esta versión');
  await expect(fields.nth(25)).toContainText('No disponible en esta versión');
  await expect(fields.nth(26)).toContainText('50%');
  await expect(fields.nth(28)).toContainText('No disponible en esta versión');
  await expect(fields.nth(29)).toContainText('No disponible en esta versión');
  await expect(fields.nth(30)).toContainText('30%');

  const matrixView = page.locator('[data-matrix-view="complete"]');
  await page.setViewportSize({ width: 1280, height: 900 });
  await block6Fields.first().scrollIntoViewIfNeeded();
  await page.screenshot({ path: 'test-results/block-6-controls-desktop-1280x900.png' });
  await block6Fields.last().scrollIntoViewIfNeeded();
  await page.screenshot({ path: 'test-results/block-6-observations-desktop-1280x900.png' });
  await page.setViewportSize({ width: 390, height: 844 });
  const backdrop = page.locator('button[aria-label="Cerrar menú lateral"]');
  if (await backdrop.count() > 0) {
    await backdrop.dispatchEvent('click');
    await page.waitForTimeout(300);
  }
  await expect(fields).toHaveCount(82);
  expect(await matrixView.evaluate(element => element.scrollWidth <= element.clientWidth)).toBeTruthy();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBeTruthy();
  await block6Fields.first().scrollIntoViewIfNeeded();
  await page.screenshot({ path: 'test-results/block-6-controls-mobile-390x844.png' });
  await block6Fields.last().scrollIntoViewIfNeeded();
  await page.screenshot({ path: 'test-results/block-6-observations-mobile-390x844.png' });
  await page.setViewportSize({ width: 1280, height: 900 });

  await modalMatriz.getByRole('button', { name: 'Cerrar Matriz completa' }).first().click();
  await expect(modalMatriz).toBeHidden();
  await page.getByRole('button', { name: 'Ver Matriz completa' }).first().click();
  await expect(modalMatriz.locator('[data-matrix-block="5"] button[aria-controls="matrix-block-5-details"]')).toHaveAttribute('aria-expanded', 'false');
  await expect(modalMatriz.locator('#matrix-block-5-details')).toBeHidden();
  await modalMatriz.getByRole('button', { name: 'Cerrar Matriz completa' }).first().click();
  expect(consoleErrors).toHaveLength(0);
  expect(pageErrors).toHaveLength(0);
  expect(requestFailures).toHaveLength(0);
  expect(unexpectedHttpErrors).toHaveLength(0);
  expect(familiaUnoRequests).toHaveLength(0);
  await expect(page.getByRole('tab', { name: 'Evaluaciones', exact: true })).toHaveAttribute('aria-selected', 'true');

  await page.getByRole('tab', { name: 'Evaluaciones', exact: true }).click();
  await expect(page.getByRole('columnheader', { name: 'EVALUACIÓN' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Ver Matriz completa' }).first()).toBeVisible();
  await page.getByRole('tab', { name: 'Consolidado', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Matriz Consolidada' })).toBeVisible();
});

test('Configuración de cálculo muestra referencias institucionales F01, F15 y F34 desde el API en modo lectura', async ({ page }) => {
  const formulas = [
    { id: 1, codigo: 'F01_VALOR_RIESGO_INHERENTE', nombre: 'F01_VALOR_RIESGO_INHERENTE', estado: 'ACTIVE', versionRow: 1, referenciaInstitucional: { numero: 1, targetField: 'valor_riesgo_inherente', sourceCell: 'Matriz Consolidada!L2', excelColumn: 'L' } },
    { id: 15, codigo: 'F15_FRECUENCIA_RESIDUAL_AUX', nombre: 'F15_FRECUENCIA_RESIDUAL_AUX', estado: 'ACTIVE', versionRow: 1, referenciaInstitucional: { numero: 15, targetField: 'frecuencia_residual_aux', sourceCell: 'Matriz Consolidada!AX2', excelColumn: 'AX' } },
    { id: 34, codigo: 'F34_DIFERENCIA_VRI_VRR', nombre: 'F34_DIFERENCIA_VRI_VRR', estado: 'ACTIVE', versionRow: 1, referenciaInstitucional: { numero: 34, targetField: 'diferencia_vri_vrr', sourceCell: 'Matriz Consolidada!BQ2', excelColumn: 'BQ' } }
  ];
  const mutations: string[] = [];
  await page.route('**/api/matrices-riesgos/configuracion-calculo/**', async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (request.method() !== 'GET') mutations.push(`${request.method()} ${path}`);
    let datos: unknown = [];
    if (path.endsWith('/formulas')) datos = formulas;
    else if (path.endsWith('/funciones') || path.endsWith('/parametros')) datos = [];
    else if (/\/formulas\/\d+\/(versiones|usos)$/.test(path)) datos = [];
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => pageErrors.push(error.message));

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Configuración de cálculo' }).click();
  await expect(page.getByRole('heading', { name: 'Configuración de cálculo' })).toBeVisible();
  for (const expected of [
    { code: formulas[0].codigo, label: 'Valor del Riesgo Inherente', column: 'L', number: '01' },
    { code: formulas[1].codigo, label: 'Frecuencia Residual (AUX)', column: 'AX', number: '15' },
    { code: formulas[2].codigo, label: 'VRI-VRR', column: 'BQ', number: '34' }
  ]) {
    await page.getByRole('button', { name: new RegExp(expected.code) }).click();
    const reference = page.locator('[data-institutional-formula-reference]');
    await expect(reference).toBeVisible();
    await expect(reference).toContainText(expected.label);
    await expect(reference).toContainText(`Columna ${expected.column}`);
    await expect(reference).toContainText(`Matriz Consolidada!${expected.column}2`);
    await expect(reference).toContainText(expected.number);
    await expect(reference.locator('input, textarea, select')).toHaveCount(0);
  }
  expect(mutations).toHaveLength(0);
  expect(consoleErrors).toHaveLength(0);
  expect(pageErrors).toHaveLength(0);
});

test('Bloque 6 guarda observaciones por capability y las rehidrata desde el GET posterior', async ({ page }) => {
  const serverBlock = {
    senalesAlerta: [], estadoRiesgo: 'ALTO', controles: [],
    observacionesArea: 'Área inicial', observacionesUgr: 'UGR inicial',
    puedeEditarObservacionesArea: true, puedeEditarObservacionesUgr: true
  };
  const writes: { path: string; texto: string }[] = [];
  let reads = 0;
  await page.route('**/api/matrices-riesgos/monitoreo/evaluaciones/20/bloque6', async route => {
    if (route.request().method() === 'GET') {
      reads++;
      await route.fulfill({ json: { success: true, datos: { ...serverBlock } } });
      return;
    }
    await route.fallback();
  });
  await page.route('**/api/matrices-riesgos/monitoreo/evaluaciones/20/observaciones/**', async route => {
    const path = new URL(route.request().url()).pathname;
    const payload = route.request().postDataJSON() as { texto: string };
    writes.push({ path, texto: payload.texto });
    if (path.endsWith('/area')) serverBlock.observacionesArea = payload.texto;
    if (path.endsWith('/ugr')) serverBlock.observacionesUgr = payload.texto;
    await route.fulfill({ json: { success: true, mensaje: 'Observación actualizada.' } });
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Ver Matriz completa' }).first().click();
  const modal = page.locator('[data-matrix-modal="complete"]');
  const area = modal.getByRole('textbox', { name: 'Observaciones del Área' });
  await expect(area).toHaveValue('Área inicial');
  await area.fill('Área persistida por capability');
  const guardarArea = modal.getByRole('button', { name: 'Guardar Observaciones del Área' });
  const areaReload = page.waitForResponse(response => response.url().includes('/bloque6') && response.request().method() === 'GET');
  await guardarArea.click();
  await areaReload;
  await expect(guardarArea).toBeEnabled();
  await expect(area).toHaveValue('Área persistida por capability');
  await expect(modal.getByRole('textbox', { name: 'Observaciones UGR' })).toHaveValue('UGR inicial');

  const ugr = modal.getByRole('textbox', { name: 'Observaciones UGR' });
  await ugr.fill('UGR persistida por capability');
  const guardarUgr = modal.getByRole('button', { name: 'Guardar Observaciones UGR' });
  await expect(guardarUgr).toBeEnabled();
  const ugrReload = page.waitForResponse(response => response.url().includes('/bloque6') && response.request().method() === 'GET');
  await guardarUgr.click();
  await ugrReload;
  await expect(guardarUgr).toBeEnabled();
  await expect(ugr).toHaveValue('UGR persistida por capability');
  await expect(modal.getByRole('textbox', { name: 'Observaciones del Área' })).toHaveValue('Área persistida por capability');
  expect(writes.map(write => write.path.split('/').at(-1))).toEqual(['area', 'ugr']);
  expect(reads).toBeGreaterThanOrEqual(3);

  await modal.getByRole('button', { name: 'Cerrar Matriz completa' }).first().click();
  await page.reload();
  await page.getByRole('button', { name: 'Ver Matriz completa' }).first().click();
  await expect(page.getByRole('textbox', { name: 'Observaciones del Área' })).toHaveValue('Área persistida por capability');
  await expect(page.getByRole('textbox', { name: 'Observaciones UGR' })).toHaveValue('UGR persistida por capability');
});

test('selectores de evaluación operativa abren hacia abajo, quedan acotados y usan scroll interno', async ({ page }) => {
  const items = Array.from({ length: 80 }, (_, index) => ({
    evaId: index + 1,
    evaRiesgoId: 100 + index,
    riesgoCodigo: 'R-' + String(index + 1).padStart(3, '0'),
    riesgoNombre: 'Riesgo operativo ' + (index + 1),
    evaVersionId: 10,
    versionCodigo: 'MATRIZ_RIESGOS_LAFT_V1',
    versionNumero: 1,
    estado: 'BORRADOR',
    vri: 7,
    vrr: 4,
    nivelResidual: 'MEDIO',
    fechaEval: '2026-08-07T12:00:00Z'
  }));

  await page.route('**/api/matrices-riesgos/evaluaciones**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      success: true,
      datos: { items, pagina: 1, registrosPorPagina: 200, totalRegistros: items.length, totalPaginas: 1 }
    })
  }));

  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/matrices-riesgos');

  for (const vista of ['Mitigación', 'Monitoreo']) {
    await page.getByRole('button', { name: vista, exact: true }).click();
    const combo = page.getByRole('combobox', { name: 'Evaluación', exact: true });
    await combo.click();

    const panel = page.locator('[data-ui-bounded-select-panel]');
    const scroll = panel.locator('[data-ui-bounded-select-scroll]');
    await expect(panel).toBeVisible();

    const comboBox = await combo.boundingBox();
    const panelBox = await panel.boundingBox();
    expect(comboBox).not.toBeNull();
    expect(panelBox).not.toBeNull();
    if (!comboBox || !panelBox) throw new Error('No se pudo medir el selector operativo.');

    expect(panelBox.y).toBeGreaterThanOrEqual(comboBox.y + comboBox.height - 1);
    expect(panelBox.y + panelBox.height).toBeLessThanOrEqual(892);
    expect(await panel.evaluate(element => getComputedStyle(element).position)).toBe('relative');
    expect(await scroll.evaluate(element => element.scrollHeight > element.clientHeight)).toBe(true);

    await page.keyboard.press('Escape');
    await expect(panel).toBeHidden();
  }
});

test('Riesgos usa filtro estándar completo y pagina exactamente 10 registros al inicio', async ({ page }) => {
  const consultas: Array<{ tamano: string | null; buscar: string | null; activo: string | null }> = [];

  await page.route('**/api/matrices-riesgos/riesgos/paginado**', route => {
    const url = new URL(route.request().url());
    const tamano = Number(url.searchParams.get('tamanoPagina') || '10');
    const pagina = Number(url.searchParams.get('pagina') || '1');
    consultas.push({
      tamano: url.searchParams.get('tamanoPagina'),
      buscar: url.searchParams.get('buscar'),
      activo: url.searchParams.get('activo')
    });
    const items = Array.from({ length: Math.min(tamano, 37) }, (_, index) => ({
      rieId: index + 1,
      rieCodigo: 'RIESGO-' + String(index + 1).padStart(2, '0'),
      rieNombre: 'Riesgo ' + (index + 1),
      rieDescripcion: 'Registro paginado UAT',
      rieActivo: url.searchParams.get('activo') !== 'false',
      rieUsrCreacion: 1,
      rieFechaCreacion: '2026-08-07T12:00:00Z'
    }));
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        datos: { items, pagina, tamanoPagina: tamano, totalRegistros: 37, totalPaginas: Math.ceil(37 / tamano) }
      })
    });
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Riesgos', exact: true }).click();

  await expect.poll(() => consultas.filter(c => c.tamano === '10').length).toBeGreaterThan(0);
  const consultaGestion = consultas.find(c => c.tamano === '10')!;
  expect(consultaGestion.tamano).toBe('10');
  await expect(page.getByLabel('Riesgos por página')).toHaveValue('10');
  await expect(page.locator('app-matrices-riesgos-gestion table tbody tr')).toHaveCount(10);

  const filtros = page.locator('[data-ui-filter-block="riesgos"]');
  await expect(filtros).toHaveAttribute('data-ui-filter-standard', 'full');
  await filtros.getByLabel('Buscar', { exact: true }).fill('proveedor');
  await filtros.getByLabel('Estado', { exact: true }).selectOption('INACTIVOS');

  await expect.poll(() => consultas.at(-1)?.activo).toBe('false');
  expect(consultas.at(-1)?.buscar).toBe('proveedor');
});

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
  await page.getByRole('button', { name: 'Guardar nuevo riesgo', exact: true }).click();
  await expect.poll(() => payload?.rieCodigo).toBe('R-008');
  await expect(page.getByText('Riesgo creado correctamente.')).toBeVisible();
});

test('preview de exportaciones consolidado conserva el modal dentro del viewport', async ({ page }) => {
  test.setTimeout(120_000);
  for (const viewport of [
    { width: 320, height: 568 },
    { width: 375, height: 812 },
    { width: 768, height: 1024 },
    { width: 1024, height: 768 },
    { width: 1280, height: 720 },
    { width: 1366, height: 768 },
    { width: 1536, height: 1024 },
    { width: 1920, height: 1080 }
  ]) {
    await page.setViewportSize(viewport);
    await page.goto('/matrices-riesgos');
    await page.getByRole('tab', { name: 'Consolidado' }).click();
    await page.getByRole('button', { name: 'Generar reporte PDF de la matriz consolidada' }).click();
    const preview = page.locator('[data-report-preview]');
    await expect(preview).toBeVisible();
    await expect(preview.getByRole('button', { name: 'Descargar reporte revisado' })).toBeVisible();
    const box = await preview.locator('.modal-container-card').boundingBox();
    expect(box).not.toBeNull();
    if (!box) throw new Error('No se pudo medir el preview PDF.');
    expect(box.width).toBeLessThanOrEqual(viewport.width);
    expect(box.height).toBeLessThanOrEqual(viewport.height);
    if (viewport.width >= 1024) {
      expect(box.width).toBeGreaterThanOrEqual(Math.min(viewport.width * 0.9, 1510 * 0.9));
    }
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
    await preview.getByRole('button', { name: 'Cerrar vista previa' }).first().click();
    await expect(preview).toBeHidden();

    await page.getByRole('button', { name: 'Exportar matriz consolidada a Excel' }).click();
    await expect(preview).toBeVisible();
    await expect(preview.locator('[data-excel-preview-table]')).toBeVisible();
    await expect(preview.getByText('Coincidencias', { exact: true })).toBeVisible();
    await expect(preview.getByRole('columnheader', { name: 'Código' })).toBeVisible();
    await expect(preview.getByText('R-001', { exact: true })).toBeVisible();
    await expect(preview.getByText('Riesgo UAT 1', { exact: true })).toBeVisible();
    const downloadPromise = page.waitForEvent('download');
    await preview.getByRole('button', { name: 'Descargar reporte revisado' }).click();
    expect((await downloadPromise).suggestedFilename()).toBe('Matriz_Riesgos.xlsx');
    await preview.getByRole('button', { name: 'Cerrar vista previa' }).first().click();
  }
});

test('UAT registra control, efectividad, plan y actividad', async ({ page }) => {
  const recibidos: Record<string, any> = {};
  let controlCreado = false;
  let planCreado = false;

  await page.route('**/api/matrices-riesgos/mitigacion/**', route => {
    const req = route.request();
    const path = new URL(req.url()).pathname;

    if (req.method() === 'POST' && path.endsWith('/mitigacion/controles')) {
      recibidos['control'] = req.postDataJSON();
      controlCreado = true;
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 31 }) });
    }
    if (req.method() === 'GET' && path.endsWith('/mitigacion/evaluaciones/20/controles')) {
      const datos = controlCreado
        ? [{ conId: 31, conEvaluacionId: 20, conTipo: 'PREVENTIVO', conDescripcion: 'Control preventivo UAT', conAutomatizacion: 'MANUAL', conEstado: 'ACTIVO' }]
        : [];
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
    }
    if (req.method() === 'POST' && path.endsWith('/mitigacion/controles/31/evaluaciones')) {
      recibidos['efectividad'] = req.postDataJSON();
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 32 }) });
    }
    if (req.method() === 'GET' && path.endsWith('/mitigacion/controles/31/evaluaciones')) {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) });
    }
    if (req.method() === 'POST' && path.endsWith('/mitigacion/planes')) {
      recibidos['plan'] = req.postDataJSON();
      planCreado = true;
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 41 }) });
    }
    if (req.method() === 'GET' && path.endsWith('/mitigacion/evaluaciones/20/planes')) {
      const datos = planCreado
        ? [{ plaId: 41, plaEvaluacionId: 20, plaDescripcion: 'Plan UAT', plaAvance: 0, plaPresupuesto: 0, plaFechaInicio: '2026-08-07T00:00:00Z', plaFechaFin: '2026-08-08T00:00:00Z', plaEstado: 'PENDIENTE' }]
        : [];
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
    }
    if (req.method() === 'POST' && path.endsWith('/mitigacion/actividades')) {
      recibidos['actividad'] = req.postDataJSON();
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 51 }) });
    }
    if (req.method() === 'GET' && path.endsWith('/mitigacion/planes/41/actividades')) {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) });
    }
    return route.fallback();
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Mitigación', exact: true }).click();
  const selectorEvaluacionMitigacion = page.getByRole('combobox', { name: 'Evaluación', exact: true });
  await selectorEvaluacionMitigacion.click();
  await page.getByRole('option', { name: /#20 · Riesgo 7 · BORRADOR/ }).click();
  await expect(page.locator('#control-estado')).toHaveValue('ACTIVO');
  await expect(page.locator('#control-estado option')).toHaveCount(2);
  await page.getByLabel('Descripción', { exact: true }).first().fill('Control preventivo UAT');
  await page.getByRole('button', { name: 'Guardar nuevo control', exact: true }).click();
  await expect.poll(() => recibidos['control']?.conEvaluacionId).toBe(20);
  await expect(page.getByText('Control creado correctamente.')).toBeVisible();
  await page.getByRole('button', { name: 'Editar y evaluar control' }).click();
  await page.getByLabel('Efectividad %').fill('85');
  await page.getByRole('button', { name: 'Registrar efectividad' }).click();
  await expect.poll(() => recibidos['efectividad']?.ecoEfectividad).toBe(85);
  await expect(page.getByText('Efectividad del control registrada correctamente.')).toBeVisible();

  await expect(page.locator('#plan-estado')).toHaveValue('PENDIENTE');
  await expect(page.locator('#plan-estado option')).toHaveCount(5);
  await expect(page.locator('#plan-estado option')).toHaveText(['Pendiente', 'En proceso', 'Cerrado', 'Vencido', 'Inactivo']);
  await page.getByLabel('Descripción', { exact: true }).nth(1).fill('Plan UAT');
  await page.getByRole('button', { name: 'Guardar nuevo plan', exact: true }).click();
  await expect.poll(() => recibidos['plan']?.plaEvaluacionId).toBe(20);
  await expect.poll(() => recibidos['plan']?.plaEstado).toBe('PENDIENTE');
  await expect(page.getByText('Plan creado correctamente.')).toBeVisible();
  await page.getByRole('button', { name: 'Editar plan y actividades' }).click();
  await expect(page.getByRole('heading', { name: 'Actividades del plan' })).toBeVisible();
  const seccionActividades = page.locator('div.bg-slate-50', { has: page.getByRole('heading', { name: 'Actividades del plan' }) });
  await seccionActividades.getByLabel('Descripción', { exact: true }).fill('Actividad UAT');
  await expect(seccionActividades.getByLabel('Descripción', { exact: true })).toHaveValue('Actividad UAT');
  await seccionActividades.getByLabel('Responsable', { exact: true }).fill('Responsable UAT');
  await expect(seccionActividades.getByLabel('Responsable', { exact: true })).toHaveValue('Responsable UAT');
  await seccionActividades.getByRole('button', { name: 'Guardar nueva actividad', exact: true }).click();
  await expect.poll(() => recibidos['actividad']?.actPlanId).toBe(41);
  await expect(page.getByText('Actividad creada correctamente.')).toBeVisible();
});

test('UAT registra alerta y automonitoreo operativo', async ({ page }) => {
  const recibidos: Record<string, any> = {};
  await page.route('**/api/matrices-riesgos/monitoreo/**', route => {
    const req = route.request();
    const path = new URL(req.url()).pathname;
    if (req.method() === 'POST' && path.endsWith('/monitoreo/alertas')) { recibidos['alerta'] = req.postDataJSON(); return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 61 }) }); }
    if (req.method() === 'POST' && path.endsWith('/monitoreo/automonitoreo')) { recibidos['monitoreo'] = req.postDataJSON(); return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: 71 }) }); }
    return route.fallback();
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Monitoreo', exact: true }).click();
  const selectorEvaluacionMonitoreo = page.getByRole('combobox', { name: 'Evaluación', exact: true });
  await selectorEvaluacionMonitoreo.click();
  await page.getByRole('option', { name: /#20 · Riesgo 7 · BORRADOR/ }).click();
  await page.getByLabel('Código', { exact: true }).fill('ALE-UAT');
  await page.getByLabel('Indicador', { exact: true }).fill('Umbral operativo UAT');
  await page.getByRole('button', { name: 'Registrar alerta' }).click();
  await expect.poll(() => recibidos['alerta']?.aleCodigo).toBe('ALE-UAT');

  await page.getByLabel('Estado del riesgo').selectOption('VIGENTE');
  await page.getByLabel('Estado de controles').selectOption('SE_MANTIENE');
  await page.getByLabel('Resultado').fill('Seguimiento conforme');
  await page.getByRole('button', { name: 'Guardar monitoreo' }).click();
  await expect.poll(() => recibidos['monitoreo']?.monResultado).toBe('Seguimiento conforme');
});

test('H5-B: Ver evaluación abre sin consultar familias/1 hardcodeado, sin 404 y con consola limpia', async ({ page }) => {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const requestedUrls: string[] = [];
  const failedResponses: string[] = [];

  page.on('console', msg => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });
  page.on('pageerror', err => pageErrors.push(err.message));
  page.on('request', req => requestedUrls.push(req.url()));
  page.on('response', resp => {
    if (resp.status() >= 400) failedResponses.push(`${resp.status()} ${resp.url()}`);
  });

  await page.goto('/matrices-riesgos');
  await expect(page.getByRole('tab', { name: 'Evaluaciones', exact: true })).toBeVisible();

  // Abrir Ver evaluación para evaluación existente (evaId 20)
  const botonVer = page.getByTitle('Ver evaluación').first();
  await expect(botonVer).toBeVisible();
  await botonVer.click();

  // Modal Ver debe abrirse correctamente
  const modalVer = page.locator('dialog.modal-backdrop-overlay');
  await expect(modalVer).toBeVisible();
  await expect(page.locator('#titulo-modal-ver')).toContainText('Evaluación #20');

  // Cerrar modal
  await page.getByRole('button', { name: 'Cerrar vista', exact: true }).click();
  await expect(modalVer).not.toBeVisible();

  // Comprobar que NUNCA se solicitó /api/matrices-riesgos/familias/1 ni existieron 404
  const familias1Requests = requestedUrls.filter(u => u.includes('/familias/1'));
  expect(familias1Requests).toHaveLength(0);
  expect(failedResponses).toHaveLength(0);
  expect(consoleErrors).toHaveLength(0);
  expect(pageErrors).toHaveLength(0);
});

test('UAT Bloque 4 persiste dos planes y tres actividades y los proyecta agrupados en Matriz completa', async ({ page }) => {
  test.setTimeout(90_000);
  const serverPlans: Record<string, any>[] = [];
  const serverActivities: Record<string, any>[] = [];
  const requests: string[] = [];
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const failedRequests: string[] = [];
  const httpErrors: string[] = [];
  let nextPlanId = 201;
  let nextActivityId = 301;
  page.on('request', request => requests.push(request.url()));
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('requestfailed', request => failedRequests.push(request.url()));
  page.on('response', response => { if (response.status() >= 400) httpErrors.push(`${response.status()} ${response.url()}`); });

  await page.route('**/api/matrices-riesgos/mitigacion/**', async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();
    if (method === 'POST' && path.endsWith('/mitigacion/planes')) {
      const plan = { ...request.postDataJSON(), plaId: nextPlanId++ };
      serverPlans.push(plan);
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: plan.plaId }) });
    }
    if (method === 'PUT' && /\/mitigacion\/planes\/\d+$/.test(path)) {
      const planId = Number(path.split('/').at(-1));
      const index = serverPlans.findIndex(plan => plan['plaId'] === planId);
      if (index >= 0) serverPlans[index] = { ...request.postDataJSON(), plaId: planId };
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, mensaje: 'Plan actualizado' }) });
    }
    if (method === 'GET' && path.endsWith('/mitigacion/evaluaciones/20/planes')) {
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: serverPlans }) });
    }
    if (method === 'POST' && path.endsWith('/mitigacion/actividades')) {
      const activity = { ...request.postDataJSON(), actId: nextActivityId++ };
      serverActivities.push(activity);
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: activity.actId }) });
    }
    if (method === 'GET' && /\/mitigacion\/planes\/\d+\/actividades$/.test(path)) {
      const planId = Number(path.split('/').at(-2));
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: serverActivities.filter(activity => activity['actPlanId'] === planId) }) });
    }
    if (method === 'GET' && path.endsWith('/mitigacion/evaluaciones/20/bloque4')) {
      const planes = serverPlans.map(plan => {
        const actividades = serverActivities.filter(activity => activity['actPlanId'] === plan['plaId']);
        return { ...plan, cantidadActividades: actividades.length, actividades };
      });
      return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: { cantidadAcciones: planes.length, planes } }) });
    }
    return route.fallback();
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Mitigación', exact: true }).click();
  const evaluationPicker = page.getByRole('combobox', { name: 'Evaluación', exact: true });
  await evaluationPicker.click();
  await page.getByRole('option', { name: /#20 · Riesgo 7 · BORRADOR/ }).click();

  const savePlan = async (description: string, monitoring: string, managers: string, resources: string, budget: string, start: string, end: string): Promise<void> => {
    await page.locator('#plan-descripcion').fill(description);
    await page.locator('#plan-avance').fill('35');
    await page.locator('#plan-estado').selectOption('EN_PROCESO');
    await page.locator('#plan-fecha-inicio').fill(start);
    await page.locator('#plan-fecha-fin').fill(end);
    await page.locator('#plan-presupuesto').fill(budget);
    await page.locator('#plan-monitoreo-seguimiento').fill(monitoring);
    await page.locator('#plan-responsables').fill(managers);
    await page.locator('#plan-recursos').fill(resources);
    await page.getByRole('button', { name: 'Guardar nuevo plan', exact: true }).click();
  };
  await savePlan('Plan correctivo de revisión reforzada', 'Seguimiento mensual por la unidad responsable', 'Unidad de Cumplimiento y Jefatura Operativa', 'Personal especializado y herramienta de monitoreo', '12500.50', '2026-08-07', '2026-09-07');
  await expect.poll(() => serverPlans.length).toBe(1);
  await page.getByRole('button', { name: 'Editar plan y actividades' }).first().click();

  const addActivity = async (description: string, responsible: string): Promise<void> => {
    await page.locator('#actividad-descripcion').fill(description);
    await page.locator('#actividad-responsable').fill(responsible);
    await page.locator('#actividad-fecha-inicio').fill('2026-08-08');
    await page.locator('#actividad-fecha-fin').fill('2026-08-28');
    await page.locator('#actividad-estado').fill('EN_PROCESO');
    await page.getByRole('button', { name: 'Guardar nueva actividad', exact: true }).click();
  };
  await addActivity('Revisar controles documentales', 'Analista de Cumplimiento');
  await expect.poll(() => serverActivities.length).toBe(1);
  await page.getByRole('button', { name: 'Nueva actividad', exact: true }).click();
  await addActivity('Actualizar expediente de seguimiento', 'Jefatura Operativa');
  await expect.poll(() => serverActivities.length).toBe(2);

  await page.getByRole('button', { name: 'Nuevo plan de mitigación', exact: true }).click();
  await savePlan('Plan complementario de supervisión', 'Seguimiento quincenal', 'Gerencia Operativa', 'Equipo interno de supervisión', '5000.00', '2026-08-10', '2026-09-10');
  await expect.poll(() => serverPlans.length).toBe(2);
  await page.getByRole('button', { name: 'Editar plan y actividades' }).nth(1).click();
  await addActivity('Verificar cierre de hallazgos', 'Supervisor Operativo');
  await expect.poll(() => serverActivities.length).toBe(3);
  expect(serverActivities.map(activity => activity['actPlanId'])).toEqual([201, 201, 202]);

  await page.getByRole('button', { name: 'Matriz y evaluaciones', exact: true }).click();
  await page.getByRole('button', { name: 'Mitigación', exact: true }).click();
  await evaluationPicker.click();
  await page.getByRole('option', { name: /#20 · Riesgo 7 · BORRADOR/ }).click();
  await page.getByRole('button', { name: 'Editar plan y actividades' }).first().click();
  await expect(page.locator('#plan-recursos')).toHaveValue('Personal especializado y herramienta de monitoreo');
  await expect(page.locator('#plan-responsables')).toHaveValue('Unidad de Cumplimiento y Jefatura Operativa');
  await expect(page.locator('#plan-monitoreo-seguimiento')).toHaveValue('Seguimiento mensual por la unidad responsable');
  await page.locator('#plan-recursos').fill('Personal especializado, monitoreo y análisis');
  await page.getByRole('button', { name: 'Actualizar plan', exact: true }).click();
  await expect.poll(() => serverPlans[0]['plaRecursos']).toBe('Personal especializado, monitoreo y análisis');

  await page.getByRole('button', { name: 'Matriz y evaluaciones', exact: true }).click();
  await page.getByRole('button', { name: 'Ver Matriz completa' }).first().click();
  const matrix = page.locator('[data-matrix-view="complete"]');
  const fields = matrix.locator('[data-matrix-field]');
  await expect(fields).toHaveCount(82);
  await expect(fields.evaluateAll(items => items.map(item => item.getAttribute('data-matrix-field')))).resolves.toEqual(
    Array.from({ length: 82 }, (_, index) => String(index + 1).padStart(2, '0'))
  );
  const block4 = matrix.locator('[data-matrix-block="4"]');
  await expect(block4.getByRole('heading', { name: '4. Plan de Mitigación / Acciones Correctivas' })).toBeVisible();
  const definitions = block4.locator('[data-matrix-field-definition]');
  await expect(definitions).toHaveCount(10);
  await expect(definitions.evaluateAll(items => items.map(item => item.getAttribute('data-matrix-field-definition')))).resolves.toEqual(
    ['40', '41', '42', '43', '44', '45', '46', '47', '48', '49']
  );
  const block4Labels = [
    'Plan de Mitigación/Acciones Correctivas', 'No. Acciones de Mitigación', 'Actividades',
    'Cantidad de Actividades', 'Monitoreo/ Seguimiento', 'Responsables', 'Fecha inicio',
    'Fecha final', 'Recursos', 'Presupuesto'
  ];
  for (let index = 0; index < block4Labels.length; index++) await expect(definitions.nth(index)).toContainText(block4Labels[index]);
  await expect(block4.locator('[data-matrix-field="41"]')).toContainText('2');
  await expect(block4.locator('[data-matrix-plan]')).toHaveCount(2);
  await expect(block4.locator('[data-plan-index="1"] [data-matrix-plan-field-instance="43"]')).toContainText('2');
  await expect(block4.locator('[data-plan-index="2"] [data-matrix-plan-field-instance="43"]')).toContainText('1');
  await expect(block4).toContainText('Personal especializado, monitoreo y análisis');
  await expect(block4).toContainText('Unidad de Cumplimiento y Jefatura Operativa');
  await expect(block4).toContainText('Analista de Cumplimiento');
  await expect(block4).toContainText('Seguimiento mensual por la unidad responsable');
  await expect(matrix.locator('[data-ui-action]')).toHaveCount(0);
  const block5 = matrix.locator('[data-matrix-block="5"]');
  await expect(block5).toContainText('20 campos · Calculados automáticamente');
  await expect(block5.getByRole('button', { name: 'Mostrar detalle' })).toHaveAttribute('aria-expanded', 'false');
  const closeButtons = page.locator('[data-matrix-modal="complete"] button[aria-label="Cerrar Matriz completa"]');
  await expect(closeButtons).toHaveCount(2);
  await expect(closeButtons.first()).toBeVisible();
  await expect(closeButtons.nth(1)).toBeVisible();

  await page.setViewportSize({ width: 1280, height: 900 });
  await block4.scrollIntoViewIfNeeded();
  expect(await matrix.evaluate(element => element.scrollWidth <= element.clientWidth)).toBeTruthy();
  await page.screenshot({ path: 'test-results/block-4-multiple-plans-desktop-1280x900.png' });
  await page.setViewportSize({ width: 390, height: 844 });
  await block4.scrollIntoViewIfNeeded();
  expect(await matrix.evaluate(element => element.scrollWidth <= element.clientWidth)).toBeTruthy();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBeTruthy();
  await page.screenshot({ path: 'test-results/block-4-multiple-plans-mobile-390x844.png' });
  expect(requests.filter(url => url.includes('/familias/1'))).toHaveLength(0);
  expect(requests.filter(url => url.includes('/monitoreo/evaluaciones/20/automonitoreo'))).toHaveLength(0);
  expect(requests.filter(url => url.includes('/mitigacion/evaluaciones/20/bloque4'))).toHaveLength(1);
  expect(consoleErrors).toHaveLength(0);
  expect(pageErrors).toHaveLength(0);
  expect(failedRequests).toHaveLength(0);
  expect(httpErrors).toHaveLength(0);
});

test('BLOCK 3 campo 39 persiste Respuesta al riesgo después de guardar y reabrir', async ({ page }) => {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const requestedUrls: string[] = [];
  const unexpectedHttpErrors: string[] = [];

  page.on('console', msg => {
    if (msg.type() === 'error') consoleErrors.push(msg.text());
  });
  page.on('pageerror', err => pageErrors.push(err.message));
  page.on('request', req => requestedUrls.push(req.url()));
  page.on('response', resp => {
    if (resp.status() >= 400) unexpectedHttpErrors.push(`${resp.status()} ${resp.url()}`);
  });

  // Stateful server representation
  let updatePayloadReceived: any = null;
  let serverEvaVersionRow = 1;
  let serverEvaDataJson = JSON.stringify({
    area_principal: 'Área de Cumplimiento',
    frecuencia_inherente: '3',
    impacto_inherente: '3',
    dueno_riesgo: 'Responsable UAT',
    controles_preventivo: 90,
    controles_detectivo: 50,
    controles_correctivo: 30
  });

  const metodologiaConRespuesta = {
    versionFormularioId: 10,
    codigo: 'MATRIZ_RIESGOS_LAFT_V1',
    version: 1,
    secciones: [
      {
        clave: 'riesgo_residual_seccion',
        titulo: '3. Riesgo Residual y Respuesta',
        columnasPorFila: 2,
        orden: 1,
        campos: [
          {
            clave: 'respuesta_riesgo',
            etiqueta: 'Respuesta al riesgo',
            tipo: 'selector-catalogo',
            codigoCatalogo: 'MR_RESPUESTA_RIESGO',
            obligatorio: false,
            soloLectura: false,
            orden: 1
          }
        ]
      }
    ],
    catalogos: [
      {
        codigo: 'MR_RESPUESTA_RIESGO',
        nombre: 'Respuesta al riesgo',
        elementos: [
          { codigo: 'EVITAR', valor: 'Evitar', orden: 1 },
          { codigo: 'TRANSFERIR', valor: 'Transferir/Compartir', orden: 2 },
          { codigo: 'ACEPTAR', valor: 'Aceptar', orden: 3 },
          { codigo: 'MITIGAR', valor: 'Mitigar', orden: 4 }
        ]
      }
    ],
    reglas: []
  };

  await page.route('**/api/matrices-riesgos/**', async route => {
    const req = route.request();
    const path = new URL(req.url()).pathname;
    const method = req.method();

    if (path.endsWith('/metodologia/vigente') || path.includes('/metodologia/version/')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, datos: metodologiaConRespuesta })
      });
    }

    if (path.endsWith('/evaluaciones/20') && method === 'GET') {
      const data = {
        evaId: 20,
        evaRiesgoId: 7,
        evaVersionId: 10,
        evaEstado: 'BORRADOR',
        evaDataJson: serverEvaDataJson,
        evaDataCalcJson: JSON.stringify({
          nivel_riesgo_inherente: 'Riesgo Moderado',
          riesgo_residual_descripcion: 'Riesgo residual derivado de controles',
          frecuencia_residual: 2,
          impacto_residual: 1,
          valor_riesgo_residual: 2,
          nivel_riesgo_residual: 'BAJO'
        }),
        evaVri: 7,
        evaVrr: 2,
        evaFechaEval: '2026-08-07T12:00:00Z',
        evaUsrEval: 1,
        evaVersionRow: serverEvaVersionRow,
        evaActivo: true
      };
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, datos: data })
      });
    }

    if (path.endsWith('/evaluaciones/20') && method === 'PUT') {
      updatePayloadReceived = req.postDataJSON();
      serverEvaVersionRow++;
      serverEvaDataJson = updatePayloadReceived.evaDataJson;
      const data = {
        evaId: 20,
        evaRiesgoId: 7,
        evaVersionId: 10,
        evaEstado: 'BORRADOR',
        evaDataJson: serverEvaDataJson,
        evaDataCalcJson: updatePayloadReceived.evaDataCalcJson,
        evaVri: 7,
        evaVrr: 2,
        evaFechaEval: '2026-08-07T12:00:00Z',
        evaUsrEval: 1,
        evaVersionRow: serverEvaVersionRow,
        evaActivo: true
      };
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, datos: data })
      });
    }

    return route.fallback();
  });

  // A. Cargar /matrices-riesgos
  await page.goto('/matrices-riesgos');
  await expect(page.getByRole('tab', { name: 'Evaluaciones', exact: true })).toBeVisible({ timeout: 15000 });

  // B. Entrar al flujo real de edición de una evaluación BORRADOR
  const botonEditar = page.getByRole('button', { name: 'Editar evaluación' }).first();
  await expect(botonEditar).toBeVisible();
  await botonEditar.click();

  const modalEditar = page.locator('dialog.modal-backdrop-overlay');
  await expect(modalEditar).toBeVisible();
  await expect(page.locator('#titulo-modal-editar')).toContainText('Editar Evaluación #20');

  // C. Localizar el campo/selector “Respuesta al riesgo”
  const selectorRespuesta = page.locator('#campo-edit-respuesta_riesgo');
  await expect(selectorRespuesta).toBeVisible();

  // D. Verificar que el catálogo disponible corresponde a las cuatro respuestas canónicas
  const options = selectorRespuesta.locator('option');
  const optionValues = await options.evaluateAll(opts =>
    opts.map(o => (o as HTMLOptionElement).value).filter(v => v !== '')
  );
  expect(optionValues).toEqual(['EVITAR', 'TRANSFERIR', 'ACEPTAR', 'MITIGAR']);

  const optionTexts = await options.evaluateAll(opts =>
    opts.map(o => o.textContent?.trim()).filter(t => t && !t.includes('Seleccione'))
  );
  expect(optionTexts).toEqual(['Evitar', 'Transferir/Compartir', 'Aceptar', 'Mitigar']);

  // E. Seleccionar al menos una opción: MITIGAR
  await selectorRespuesta.selectOption('MITIGAR');
  await expect(selectorRespuesta).toHaveValue('MITIGAR');

  // F. Guardar la evaluación mediante la UI real
  const botonGuardar = page.getByRole('button', { name: 'Guardar cambios de evaluación' });
  await expect(botonGuardar).toBeEnabled();
  await botonGuardar.click();

  const feedbackExito = page.locator('[data-uat="feedback-edicion-exito"]');
  await expect(feedbackExito).toBeVisible();
  await expect(feedbackExito).toContainText('guardados y verificados correctamente');

  // G. Capturar/verificar la petición de actualización real
  expect(updatePayloadReceived).not.toBeNull();
  const parsedEvaData = JSON.parse(updatePayloadReceived.evaDataJson);
  expect(parsedEvaData.respuesta_riesgo).toBe('MITIGAR');

  // J. Después de guardar: cerrar el editor/modal según la UX real
  await page.getByLabel('Cerrar edición').click();
  await expect(page.locator('#titulo-modal-editar')).toHaveCount(0);

  // K. Volver a abrir LA MISMA evaluación desde la UI (rehidratación desde el API stateful)
  await botonEditar.click();
  await expect(page.locator('#titulo-modal-editar')).toContainText('Editar Evaluación #20');

  // L. Verificar que el selector muestra la respuesta persistida: MITIGAR
  const selectorReabierto = page.locator('#campo-edit-respuesta_riesgo');
  await expect(selectorReabierto).toBeVisible();
  await expect(selectorReabierto).toHaveValue('MITIGAR');

  // M. Cerrar el editor
  await page.getByLabel('Cerrar edición').click();
  await expect(page.locator('#titulo-modal-editar')).toHaveCount(0);

  // N. Abrir “Ver Matriz completa” de esa misma evaluación
  const botonMatriz = page.getByRole('button', { name: 'Ver Matriz completa' }).first();
  await expect(botonMatriz).toBeVisible();
  await botonMatriz.click();

  const modalMatriz = page.locator('[data-matrix-modal="complete"]');
  await expect(modalMatriz).toBeVisible();
  const view = modalMatriz.locator('[data-matrix-view="complete"]');

  // O. Localizar [data-matrix-field="39"] y verificar label y valor proyectado MITIGAR
  const field39 = view.locator('[data-matrix-field="39"]');
  await expect(field39).toBeVisible();
  await expect(field39).toContainText('Respuesta al riesgo');
  await expect(field39).toContainText('Mitigar');

  // P. Confirmar que campos 34–38 siguen read-only
  const fields = view.locator('[data-matrix-field]');
  for (const index of [33, 34, 35, 36, 37]) {
    await expect(fields.nth(index).locator('[aria-readonly="true"]')).toBeVisible();
  }

  // Q. Bloques 5 y 6 quedan disponibles y sus campos contractuales se proyectan.
  await expect(fields).toHaveCount(82);
  await expect(view.locator('[data-matrix-block="5"]')).toContainText('20 campos · Calculados automáticamente');
  await expect(view.locator('[data-matrix-block="6"]')).toContainText('6. Monitoreo, Efectividad y Observaciones');
  await expect(view.locator('[data-matrix-block="6"] [data-matrix-field-definition]')).toHaveCount(13);

  // Cerrar Matriz completa
  await modalMatriz.getByRole('button', { name: 'Cerrar Matriz completa' }).first().click();
  await expect(modalMatriz).toBeHidden();

  // Consola / Network limpias
  const familias1Requests = requestedUrls.filter(u => u.includes('/familias/1'));
  expect(familias1Requests).toHaveLength(0);
  expect(unexpectedHttpErrors).toHaveLength(0);
  expect(consoleErrors).toHaveLength(0);
  expect(pageErrors).toHaveLength(0);
});
