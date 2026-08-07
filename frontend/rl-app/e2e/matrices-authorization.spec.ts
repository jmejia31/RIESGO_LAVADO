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

function tokenAdministradorModulo10(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '1',
    uid: 'admin.e2e',
    email: 'admin.e2e@ihss.hn',
    given_name: 'Administrador',
    family_name: 'E2E',
    role: 'ADMINISTRADOR',
    rol_id: '1',
    modulos: '10',
    debe_cambiar_pass: '0',
    exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

async function prepararSesion(page: Page): Promise<void> {
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'refresh-local-e2e');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, tokenAdministradorModulo10());

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
    if (path.endsWith('/formulario/version-vigente')) datos = versionPublicada;
    else if (path.endsWith('/metodologia/vigente')) datos = {
      versionFormularioId: 10,
      codigo: 'MATRIZ_RIESGOS_LAFT_V1',
      version: 1,
      secciones: [],
      catalogos: [],
      reglas: []
    };
    else if (path.endsWith('/formularios/historial')) datos = [versionPublicada];
    else if (path.endsWith('/riesgos')) datos = [];
    else if (path.endsWith('/evaluaciones')) datos = [];
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

test.beforeEach(async ({ page }) => {
  await prepararSesion(page);
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
  await page.getByRole('button', { name: 'Plantillas' }).click();
  await expect(page.getByText('MATRIZ_RIESGOS_LAFT_V1 · v1')).toBeVisible();

  await page.getByRole('button', { name: 'Clonar' }).click();

  await expect.poll(() => clonaciones).toBe(1);
  await expect(page).not.toHaveURL(/\/acceso-denegado/);
  await expect(page.getByRole('status')).toContainText('Versión clonada como borrador');
});

test('un 403 real del Backend conserva la protección y redirige a Acceso Denegado', async ({ page }) => {
  await page.route('**/api/matrices-riesgos/formularios/10/clonar', route => route.fulfill({
    status: 403,
    contentType: 'application/json',
    body: JSON.stringify({ success: false, mensaje: 'No tiene permiso para realizar esta acción.' })
  }));

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Plantillas' }).click();
  await page.getByRole('button', { name: 'Clonar' }).click();

  await expect(page).toHaveURL(/\/acceso-denegado$/);
  await expect(page.getByRole('heading', { name: 'Acceso Denegado' })).toBeVisible();
});
