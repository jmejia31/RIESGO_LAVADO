import { expect, Page, test } from '@playwright/test';

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
