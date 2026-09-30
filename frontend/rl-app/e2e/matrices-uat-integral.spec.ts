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
const evaluacion = {
  evaId: 20, evaRiesgoId: 7, evaVersionId: 10, evaEstado: 'BORRADOR',
  evaDataJson: JSON.stringify({ area_principal: 'Área de Cumplimiento', frecuencia_inherente: '3', impacto_inherente: '3', dueno_riesgo: 'Responsable UAT', controles_preventivo: 90, controles_detectivo: 50, controles_correctivo: 30 }),
  evaDataCalcJson: JSON.stringify({ nivel_riesgo_inherente: 'Riesgo Moderado' }),
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
    else if (path.endsWith('/mitigacion/controles/31/evaluaciones')) datos = [];
    else if (path.endsWith('/mitigacion/planes/41/actividades')) datos = [];
    else if (path.endsWith('/monitoreo/evaluaciones/20/alertas')) datos = [];
    else if (path.endsWith('/monitoreo/evaluaciones/20/automonitoreo')) datos = [];
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
  await expect(fields).toHaveCount(39);
  await expect(fields.evaluateAll(items => items.map(item => item.getAttribute('data-matrix-field')))).resolves.toEqual(
    Array.from({ length: 39 }, (_, index) => String(index + 1).padStart(2, '0'))
  );
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

  await expect(view.locator('[data-matrix-block="4"]')).toContainText('Bloque pendiente de implementación');
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
  await fields.nth(33).scrollIntoViewIfNeeded();
  await page.screenshot({ path: 'test-results/block-3-closure-desktop-1280x900.png' });
  await page.setViewportSize({ width: 390, height: 844 });
  const backdrop = page.locator('button[aria-label="Cerrar menú lateral"]');
  if (await backdrop.count() > 0) {
    await backdrop.dispatchEvent('click');
    await page.waitForTimeout(300);
  }
  await expect(fields).toHaveCount(39);
  expect(await matrixView.evaluate(element => element.scrollWidth <= element.clientWidth)).toBeTruthy();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBeTruthy();
  await fields.nth(33).scrollIntoViewIfNeeded();
  await page.screenshot({ path: 'test-results/block-3-closure-mobile-390x844.png' });
  await page.setViewportSize({ width: 1280, height: 900 });

  await modalMatriz.getByRole('button', { name: 'Cerrar Matriz completa' }).first().click();
  await expect(modalMatriz).toBeHidden();
  await expect(page.getByRole('tab', { name: 'Evaluaciones', exact: true })).toHaveAttribute('aria-selected', 'true');

  await page.getByRole('tab', { name: 'Evaluaciones', exact: true }).click();
  await expect(page.getByRole('columnheader', { name: 'EVALUACIÓN' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Ver Matriz completa' }).first()).toBeVisible();
  await page.getByRole('tab', { name: 'Consolidado', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Matriz Consolidada' })).toBeVisible();
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

  await page.getByLabel('Estado del riesgo').fill('CONTROLADO');
  await page.getByLabel('Estado de controles').fill('EFECTIVO');
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
          { codigo: 'MITIGAR', valor: 'Mitigar', orden: 2 },
          { codigo: 'TRANSFERIR', valor: 'Transferir', orden: 3 },
          { codigo: 'ACEPTAR', valor: 'Aceptar', orden: 4 }
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
  expect(optionValues).toEqual(['EVITAR', 'MITIGAR', 'TRANSFERIR', 'ACEPTAR']);

  const optionTexts = await options.evaluateAll(opts =>
    opts.map(o => o.textContent?.trim()).filter(t => t && !t.includes('Seleccione'))
  );
  expect(optionTexts).toEqual(['Evitar', 'Mitigar', 'Transferir', 'Aceptar']);

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
  await expect(field39).toContainText('MITIGAR');

  // P. Confirmar que campos 34–38 siguen read-only
  const fields = view.locator('[data-matrix-field]');
  for (const index of [33, 34, 35, 36, 37]) {
    await expect(fields.nth(index).locator('[aria-readonly="true"]')).toBeVisible();
  }

  // Q. Confirmar que Matriz completa continúa teniendo exactamente 39 campos implementados
  await expect(fields).toHaveCount(39);
  await expect(view.locator('[data-matrix-block="4"]')).toContainText('Bloque pendiente de implementación');

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
