import { expect, Page, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

const familiaListado = {
  famId: 7,
  famCodigo: 'MATRIZ_RIESGOS_LAFT',
  famNombre: 'Matriz de Riesgos LA/FT',
  famDescripcion: 'Descripción obtenida desde el listado.',
  famActivo: true,
  famFechaCreacion: '2026-08-07T00:00:00Z',
  totalVersiones: 2,
  tieneVersionVigente: true
};

const familiaDetalle = {
  ...familiaListado,
  famDescripcion: 'Detalle autoritativo UI-FAM.2 recuperado por ID.'
};

const versionVigente = {
  verId: 71,
  verFamiliaId: 7,
  verCodigo: 'MATRIZ_RIESGOS_LAFT',
  verVersion: 2,
  verJson: '{"secciones":[]}',
  verHash: 'hash-ui-fam-2',
  verEstado: 'PUBLISHED',
  verVigente: true,
  verFechaInicio: '2026-08-20T00:00:00Z',
  verFechaFin: null,
  verFechaCreacion: '2026-08-20T00:00:00Z',
  verUsrCreacion: 1
};

const versionBorrador = {
  ...versionVigente,
  verId: 72,
  verVersion: 3,
  verEstado: 'DRAFT',
  verVigente: false,
  verJson: '{"secciones":[]}'
};

function tokenAdministradorModulo10(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '1',
    uid: 'admin.ui.fam2',
    email: 'admin.ui.fam2@ihss.hn',
    given_name: 'Administrador',
    family_name: 'UI-FAM.2',
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
    localStorage.setItem('refresh_token', 'refresh-ui-fam2');
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

async function stubLecturasBase(page: Page): Promise<void> {
  await page.route('**/api/matrices-riesgos/**', route => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;

    let datos: unknown = [];
    if (path.endsWith('/familias')) datos = [familiaListado];
    else if (path.endsWith('/formulario/version-vigente')) datos = versionVigente;
    else if (path.endsWith('/metodologia/vigente')) datos = {
      versionFormularioId: 71,
      codigo: 'MATRIZ_RIESGOS_LAFT',
      version: 2,
      secciones: [],
      catalogos: [],
      reglas: []
    };
    else if (path.endsWith('/formularios/historial')) datos = [versionVigente];
    else if (/\/formularios\/\d+$/.test(path) && request.method() === 'GET') datos = versionVigente;
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

async function abrirGestor(page: Page): Promise<void> {
  await page.goto('/matrices-riesgos');
  await page.getByRole('tab', { name: 'Plantillas' }).click();
  await expect(page.getByRole('heading', { name: 'Familias de Formularios' })).toBeVisible();
}

async function abrirDetalle(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Ver detalle' }).first().click();
}

test.beforeEach(async ({ page }) => {
  await prepararSesion(page);
  await stubLecturasBase(page);
});

test('UI-FAM.2 carga el detalle por ID dentro de un único modal XL con sus versiones integradas', async ({ page }) => {
  let lecturasPorId = 0;
  await page.route('**/api/matrices-riesgos/familias/7', route => {
    lecturasPorId++;
    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, datos: familiaDetalle })
    });
  });

  await abrirGestor(page);
  const disparador = page.getByRole('button', { name: 'Ver detalle' }).first();
  await disparador.focus();
  await abrirDetalle(page);

  const modal = page.locator('[data-ui-fam-detail="modal"]');
  await expect(modal).toBeVisible();
  await expect.poll(() => lecturasPorId).toBe(1);
  await expect(modal.getByText('Detalle autoritativo UI-FAM.2 recuperado por ID.')).toBeVisible();
  await expect(modal.getByRole('heading', { name: 'Versiones del formulario' })).toBeVisible();
  await expect(modal.getByText('Actividad reciente')).toBeVisible();
  await expect(modal.getByText('MATRIZ_RIESGOS_LAFT').last()).toBeVisible();
  await expect(modal).not.toContainText('Historial de versiones');
  await expect(page.locator('dialog[open]')).toHaveCount(1);
  await expect(modal).not.toContainText('Última actividad');

  await page.getByRole('button', { name: 'Cerrar detalle de familia' }).click();
  await expect(modal).toHaveCount(0);
  await expect(disparador).toBeFocused();
});

test('MCV.1 Escape conserva gestor y detalle abiertos hasta el cierre explicito', async ({ page }) => {
  await abrirGestor(page);
  await page.keyboard.press('Escape');
  await expect(page.getByRole('heading', { name: 'Familias de Formularios' })).toBeVisible();

  await abrirDetalle(page);
  const detalle = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalle).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(detalle).toBeVisible();
  await page.getByRole('button', { name: 'Cerrar detalle de familia' }).click();
  await expect(detalle).toHaveCount(0);
});

test('MCV.2 Editar Familia regresa al mismo Detalle sin volver al listado', async ({ page }) => {
  await abrirGestor(page);
  await abrirDetalle(page);

  const detalle = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalle).toBeVisible();
  await expect(detalle).toContainText(familiaDetalle.famCodigo);

  const accionEditar = detalle.locator('[data-ui-fam-detail-action="editar"]');
  await accionEditar.click();

  const editor = page.locator('[data-ui-fam-edit="modal"]');
  await expect(editor).toBeVisible();
  await editor.locator('[data-ui-fam-edit-action="regresar"]').click();

  await expect(editor).toHaveCount(0);
  await expect(detalle).toBeVisible();
  await expect(detalle).toContainText(familiaDetalle.famCodigo);
  await expect(accionEditar).toBeFocused();
});

test('MCV.2 Constructor regresa al mismo Detalle y conserva Versiones', async ({ page }) => {
  await abrirGestor(page);
  await abrirDetalle(page);

  const detalle = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalle).toBeVisible();
  const version = detalle.getByRole('button', { name: 'Ver detalle de la versión' }).first();
  await version.click();

  const builder = page.locator('[data-form-builder-shell="true"]');
  await expect(builder).toBeVisible();
  await expect(builder.getByText(/Versión MATRIZ_RIESGOS_LAFT \(v2\)/)).toBeVisible();
  await builder.getByRole('button', { name: 'Cerrar modal de constructor' }).click();

  await expect(builder).toHaveCount(0);
  await expect(detalle).toBeVisible();
  await expect(detalle.getByRole('heading', { name: 'Versiones del formulario' })).toBeVisible();
  await expect(detalle).toContainText('v2');
  await expect(detalle.locator(':focus')).toHaveCount(1);
});

test('MCV.3 Editar definición de borrador abre el Constructor desde el Detalle', async ({ page }) => {
  await page.route('**/api/matrices-riesgos/formularios/historial**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [versionBorrador, versionVigente] })
  }));
  await page.route('**/api/matrices-riesgos/formularios/72', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: versionBorrador })
  }));

  await abrirGestor(page);
  await abrirDetalle(page);

  const detalle = page.locator('[data-ui-fam-detail="modal"]');
  await expect(detalle.getByText('v3')).toBeVisible();
  await detalle.getByRole('button', { name: 'Editar definición de la versión' }).first().click();

  const builder = page.locator('[data-form-builder-shell="true"]');
  await expect(builder).toBeVisible();
  await expect(builder.locator('#btn-guardar-builder')).toBeVisible();
  await expect(builder.locator('#btn-publicar-builder')).toBeVisible();
  await page.getByRole('button', { name: 'Cerrar modal de constructor' }).click();
  await expect(builder).toHaveCount(0);
  await expect(detalle).toBeVisible();
  await expect(detalle).toContainText('v3');
});

test('UI-FAM.2 presenta un 404 como Familia no encontrada sin inventar contenido', async ({ page }) => {
  await page.route('**/api/matrices-riesgos/familias/7', route => route.fulfill({
    status: 404,
    contentType: 'application/problem+json',
    body: JSON.stringify({ title: 'No encontrado', status: 404, detail: 'La familia no existe.' })
  }));

  await abrirGestor(page);
  await abrirDetalle(page);

  const modal = page.locator('[data-ui-fam-detail="modal"]');
  await expect(modal.getByText('Familia no encontrada')).toBeVisible();
  await expect(modal).not.toContainText('Detalle autoritativo UI-FAM.2');
});

test('UI-FAM.2 permite reintentar un fallo temporal sin cerrar el modal', async ({ page }) => {
  let intentos = 0;
  await page.route('**/api/matrices-riesgos/familias/7', route => {
    intentos++;
    if (intentos === 1) {
      return route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Error', status: 500, detail: 'Falla temporal UI-FAM.2.' })
      });
    }

    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, datos: familiaDetalle })
    });
  });

  await abrirGestor(page);
  await abrirDetalle(page);

  const modal = page.locator('[data-ui-fam-detail="modal"]');
  await expect(modal.getByText('Falla temporal UI-FAM.2.')).toBeVisible();
  await modal.getByRole('button', { name: 'Reintentar' }).click();

  await expect.poll(() => intentos).toBe(2);
  await expect(modal.getByText('Detalle autoritativo UI-FAM.2 recuperado por ID.')).toBeVisible();
});
