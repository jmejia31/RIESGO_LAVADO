import { expect, Locator, Page, test, expectHttpStatuses, expectHttpResponse } from './support/telemetry-test';

const versionPublicada = {
  verId: 10,
  verFamiliaId: 1,
  verCodigo: 'MATRIZ_RIESGOS_LAFT_V1',
  verVersion: 1,
  verJson: JSON.stringify({
    codigoFormulario: 'MATRIZ_RIESGOS_LAFT_V1',
    nombreFormulario: 'Matriz de Riesgos LA/FT',
    secciones: []
  }),
  verHash: 'hash-e2e-autorizacion',
  verEstado: 'PUBLISHED',
  verVigente: true,
  verFechaCreacion: '2026-08-07T08:00:00Z',
  verUsrCreacion: 1
};

function tokenAdministrador(modulos = '10', role = 'ADMINISTRADOR'): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '1',
    uid: 'admin.e2e',
    email: 'admin.e2e@ihss.hn',
    given_name: 'Administrador',
    family_name: 'E2E',
    role,
    rol_id: '1',
    modulos,
    debe_cambiar_pass: '0',
    exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

async function prepararSesion(page: Page, modulos = '10', role = 'ADMINISTRADOR'): Promise<void> {
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'refresh-local-e2e');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, tokenAdministrador(modulos, role));

  await page.route('**/api/configuracion/sistema', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      success: true,
      datos: {
        nombreSistema: 'SGRLA-IHSS',
        nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
        colorPrimario: '#1e3a8a',
        colorSecundario: '#1d4ed8',
        timeoutSesion: 30
      }
    })
  }));

  await page.route('**/api/configuracion/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] })
  }));

  await page.route('**/api/catalogos/modulos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] })
  }));

  await page.route('**/api/auditoria**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] })
  }));
}

async function stubLecturasMatrices(page: Page): Promise<void> {
  await page.route('**/api/matrices-riesgos/**', route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;

    if (request.method() === 'POST' && /\/formularios\/10\/clonar$/.test(path)) {
      return route.fallback();
    }

    let datos: unknown = [];
    if (path.endsWith('/familias/paginado')) datos = { items: [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: 'Familia E2E de autorización.', famActivo: true, famFechaCreacion: '2026-08-07T08:00:00Z', totalVersiones: 1, tieneVersionVigente: true }], pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1, totales: { totalFamilias: 1, activas: 1, inactivas: 0, totalVersiones: 1 } };
    else if (path.endsWith('/familias')) datos = [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: 'Familia E2E de autorización.', famActivo: true, famFechaCreacion: '2026-08-07T08:00:00Z', totalVersiones: 1, tieneVersionVigente: true }];
    else if (path.endsWith('/familias/1')) datos = { famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: 'Familia E2E de autorización.', famActivo: true, famFechaCreacion: '2026-08-07T08:00:00Z', totalVersiones: 1, tieneVersionVigente: true };
    else if (path.endsWith('/formulario/version-vigente')) datos = versionPublicada;
    else if (path.endsWith('/metodologia/vigente')) datos = {
      versionFormularioId: 10,
      codigo: 'MATRIZ_RIESGOS_LAFT_V1',
      version: 1,
      secciones: [],
      catalogos: [],
      reglas: []
    };
    else if (path.endsWith('/formularios/historial')) datos = [versionPublicada];
    else if (/\/formularios\/\d+$/.test(path) && request.method() === 'GET') datos = versionPublicada;
    else if (path.endsWith('/riesgos')) datos = [];
    else if (path.endsWith('/evaluaciones')) datos = { items: [], pagina: 1, registrosPorPagina: 10, totalRegistros: 0, totalPaginas: 0 };
    else if (path.endsWith('/consolidado')) datos = [];

    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, datos })
    });
  });

  await page.route('**/api/matrices-riesgos*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] })
  }));
}

async function abrirVersionesDeLaFamilia(page: Page): Promise<void> {
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
  const detalle = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalle).toBeVisible();
  await expect(detalle.getByRole('heading', { name: 'Versiones del formulario' })).toBeVisible();
  await expect(detalle.getByText('MATRIZ_RIESGOS_LAFT_V1')).toBeVisible();
}

test.beforeEach(async ({ page }, testInfo) => {
  const modulos = testInfo.title.includes('sin acceso al módulo') ? '2' : '10';
  const role = testInfo.title.includes('alias ADMIN') ? 'ADMIN' : 'ADMINISTRADOR';
  await prepararSesion(page, modulos, role);
  await stubLecturasMatrices(page);
});

test('ADMINISTRADOR con módulo 10 clona una plantilla sin ir a Acceso Denegado', async ({ page }) => {
  let clonaciones = 0;
  await page.route('**/api/matrices-riesgos/formularios/10/clonar', route => {
    clonaciones++;
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, datos: 11, mensaje: 'Versión clonada como borrador exitosamente.' })
    });
  });

  await page.goto('/matrices-riesgos');
  await abrirVersionesDeLaFamilia(page);

  await page.getByRole('button', { name: 'Clonar versión' }).click();

  await expect.poll(() => clonaciones).toBe(1);
  await expect(page).not.toHaveURL(/\/sin-acceso/);
});

test('alias ADMIN no habilita acciones reservadas a ADMINISTRADOR', async ({ page }) => {
  await page.goto('/matrices-riesgos');
  await abrirVersionesDeLaFamilia(page);

  await expect(page.getByRole('button', { name: /Clonar/ })).toHaveCount(0);
});

test('un 403 de operación se muestra dentro del modal sin abandonar el módulo', async ({ page }) => {
  expectHttpStatuses(403);
  await page.route('**/api/matrices-riesgos/formularios/10/clonar', route => route.fulfill({
    status: 403,
    contentType: 'application/json',
    body: JSON.stringify({ success: false, mensaje: 'No tiene permiso para clonar esta versión.' })
  }));

  await page.goto('/matrices-riesgos');
  await abrirVersionesDeLaFamilia(page);
  await page.getByRole('button', { name: 'Clonar versión' }).click();

  await expect(page).toHaveURL(/\/matrices-riesgos(?:\?.*)?$/);
  await expect(page.locator('[data-ui-fam-detail="modal"]')).toBeVisible();
  await expect(page.getByText('No tiene permiso para clonar esta versión.')).toBeVisible();
});

test('una ruta sin acceso al módulo redirige a Acceso Denegado', async ({ page }) => {
  await page.goto('/matrices-riesgos');

  await expect(page).toHaveURL(/\/sin-acceso(?:\?.*)?$/);
  await expect(page.getByRole('heading', { name: 'Acceso Denegado' })).toBeVisible();
});

type WorkflowResponse = { status: number; datos?: unknown; mensaje?: string };

async function prepararSeguimientoEvaluacion(
  page: Page,
  estado: string,
  transiciones: WorkflowResponse,
  flujos: WorkflowResponse
): Promise<void> {
  await page.route('**/api/matrices-riesgos/**', async route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;

    if (request.method() === 'GET' && path === '/api/matrices-riesgos/evaluaciones') {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          datos: {
            items: [{
              evaId: 87,
              evaRiesgoId: 5,
              riesgoCodigo: 'RIE-005',
              riesgoNombre: 'Riesgo de prueba E2E',
              evaVersionId: 10,
              versionCodigo: 'MATRIZ_RIESGOS_LAFT_V1',
              versionNumero: 1,
              estado,
              vri: 0,
              vrr: 0,
              nivelResidual: 'BAJO',
              fechaEval: '2026-10-19T10:00:00Z'
            }],
            pagina: 1,
            registrosPorPagina: 10,
            totalRegistros: 1,
            totalPaginas: 1
          }
        })
      });
    }

    const isTransitions = request.method() === 'GET' && path.endsWith('/evaluaciones/87/transiciones-permitidas');
    const isHistory = request.method() === 'GET' && path.endsWith('/evaluaciones/87/flujos');
    const response = isTransitions ? transiciones : isHistory ? flujos : null;
    if (!response) return route.fallback();

    return route.fulfill({
      status: response.status,
      contentType: 'application/json',
      body: JSON.stringify(response.status < 400
        ? { success: true, datos: response.datos }
        : { success: false, mensaje: response.mensaje ?? 'La consulta del flujo falló.' })
    });
  });
}

async function abrirSeguimiento(page: Page): Promise<Locator> {
  await page.goto('/matrices-riesgos');
  const button = page.getByRole('button', { name: 'Seguimiento de evaluación' });
  await expect(button).toBeVisible();
  await button.click();
  const dialog = page.locator('dialog[aria-labelledby="titulo-modal-seguimiento"]');
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText('Seguimiento de Evaluación #87');
  return dialog;
}

test('seguimiento de BORRADOR ofrece todos los destinos y trata historial 200 vacío como vacío legítimo', async ({ page }) => {
  await prepararSeguimientoEvaluacion(
    page,
    'BORRADOR',
    { status: 200, datos: { estadoActual: 'BORRADOR', destinosPermitidos: ['APROBADA', 'CERRADA', 'EN_REVISION', 'OBSERVADA', 'RECHAZADA'] } },
    { status: 200, datos: [] }
  );

  const dialog = await abrirSeguimiento(page);
  await expect(dialog.locator('#seg-nuevo-estado')).toHaveValue('');
  await expect(dialog.locator('#seg-nuevo-estado option')).toHaveCount(6);
  await expect(dialog.getByText('No hay transiciones pendientes para el estado actual.')).toHaveCount(0);
  await expect(dialog.getByText('Sin historial de flujo registrado.')).toBeVisible();
  await expect(dialog.getByRole('alert')).toHaveCount(0);
});

test('fallo HTTP al consultar transiciones no se presenta como éxito vacío', async ({ page }) => {
  expectHttpStatuses(404);
  await prepararSeguimientoEvaluacion(
    page,
    'BORRADOR',
    { status: 404, mensaje: 'Ruta de transiciones no disponible en el API activo.' },
    { status: 200, datos: [] }
  );

  const dialog = await abrirSeguimiento(page);
  await expect(dialog.getByText('Ruta de transiciones no disponible en el API activo.')).toBeVisible();
  await expect(dialog.getByText('No hay transiciones pendientes para el estado actual.')).toHaveCount(0);
  await expect(dialog.getByText('Sin historial de flujo registrado.')).toBeVisible();
});

test('fallo al consultar historial muestra error y no afirma que está vacío', async ({ page }) => {
  expectHttpResponse({
    status: 503,
    method: 'GET',
    path: '/api/matrices-riesgos/evaluaciones/87/flujos',
    count: 3,
    reason: 'The controlled history outage is returned for the initial GET and both configured retries.',
  });
  await prepararSeguimientoEvaluacion(
    page,
    'BORRADOR',
    { status: 200, datos: { estadoActual: 'BORRADOR', destinosPermitidos: ['APROBADA', 'CERRADA', 'EN_REVISION', 'OBSERVADA', 'RECHAZADA'] } },
    { status: 503, mensaje: 'Servicio de historial no disponible.' }
  );

  const dialog = await abrirSeguimiento(page);
  await expect(dialog.locator('#seg-nuevo-estado')).toHaveValue('');
  await expect(dialog.getByText('Servicio de historial no disponible.')).toBeVisible();
  await expect(dialog.getByText('Sin historial de flujo registrado.')).toHaveCount(0);
});

test('estado actual queda excluido de los destinos permitidos', async ({ page }) => {
  await prepararSeguimientoEvaluacion(
    page,
    'CERRADA',
    { status: 200, datos: { estadoActual: 'CERRADA', destinosPermitidos: ['APROBADA', 'BORRADOR', 'EN_REVISION', 'OBSERVADA', 'RECHAZADA'] } },
    { status: 200, datos: [] }
  );

  const dialog = await abrirSeguimiento(page);
  await expect(dialog.locator('#seg-nuevo-estado option')).toHaveCount(6);
  await expect(dialog.locator('#seg-nuevo-estado option[value="CERRADA"]')).toHaveCount(0);
  await expect(dialog.getByText('No hay transiciones pendientes para el estado actual.')).toHaveCount(0);
  await expect(dialog.getByText('Sin historial de flujo registrado.')).toBeVisible();
  await expect(dialog.getByRole('alert')).toHaveCount(0);
});
