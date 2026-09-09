import { expect, Page, test } from '@playwright/test';
import { Buffer } from 'node:buffer';

const familia = {
  famId: 101,
  famCodigo: 'FAMILIA_UAT',
  famNombre: 'Familia UAT elegible',
  famDescripcion: 'Fixture no persistente para FP.2.',
  famActivo: true,
  famPredeterminada: false,
  famFechaCreacion: '2026-08-01T00:00:00Z',
  totalVersiones: 1,
  tieneVersionVigente: true
};

const version = {
  verId: 501,
  verFamiliaId: familia.famId,
  verCodigo: 'FAMILIA_UAT_V1',
  verVersion: 1,
  verJson: JSON.stringify({ codigoFormulario: 'FAMILIA_UAT_V1', nombreFormulario: 'Familia UAT', secciones: [] }),
  verHash: 'fp2-uat-hash',
  verEstado: 'PUBLISHED',
  verVigente: true,
  verFechaCreacion: '2026-08-01T00:00:00Z',
  verUsrCreacion: 1
};

function tokenAdministrador(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '1', uid: 'fp2.uat', email: 'fp2.uat@ihss.hn', role: 'ADMINISTRADOR', rol_id: '1', modulos: '10',
    debe_cambiar_pass: '0', exp: Math.floor(Date.now() / 1000) + 3600
  })}.`;
}

async function preparar(page: Page): Promise<void> {
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'fp2-refresh');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, tokenAdministrador());

  await page.route('**/api/configuracion/sistema', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: { nombreSistema: 'SGRLA-IHSS', nombreInstitucion: 'IHSS', colorPrimario: '#1e3a8a', colorSecundario: '#1d4ed8', timeoutSesion: 30 } })
  }));
  await page.route('**/api/configuracion/login', route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos: [] }) }));
  await page.route('**/api/matrices-riesgos/**', route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    let datos: unknown = [];
    if (path.endsWith('/familias/predeterminada')) datos = { configurada: false, tieneVersionVigente: false };
    else if (path.endsWith('/familias')) datos = [familia];
    else if (path.endsWith('/formulario/version-vigente')) datos = version;
    else if (path.endsWith('/metodologia/version/501')) datos = { versionFormularioId: 501, codigo: version.verCodigo, version: 1, secciones: [], catalogos: [], reglas: [] };
    else if (path.endsWith('/evaluaciones')) datos = { items: [], pagina: 1, registrosPorPagina: 10, totalRegistros: 0, totalPaginas: 0 };
    else if (path.endsWith('/riesgos')) datos = [];
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });
}

test.beforeEach(async ({ page }) => preparar(page));

test('FP.2 no default muestra estado de configuración y permite llegar al gestor', async ({ page }) => {
  await page.goto('/matrices-riesgos');
  const estado = page.locator('[data-ui-default-family-state]');
  await expect(estado).toBeVisible();
  await expect(estado).toContainText('Familia predeterminada pendiente');
  await expect(page.locator('[data-ui-default-family-state][role="alert"]')).toHaveCount(0);

  await page.getByRole('button', { name: 'Configurar familia predeterminada' }).click();
  await expect(page.getByRole('heading', { name: 'Familias de Formularios' })).toBeVisible();
  await expect(page.getByText('Familia UAT elegible')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Establecer como familia predeterminada' })).toBeEnabled();
});

test('FP.2 sin default mantiene selector vacío y permite selección manual sin persistir', async ({ page }) => {
  let putCount = 0;
  await page.route('**/api/matrices-riesgos/familias/101/predeterminada', route => {
    putCount++;
    return route.fallback();
  });

  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Nueva evaluación' }).click();
  const selector = page.locator('#modal-selector-familia');
  await expect(selector).toHaveValue('');
  await selector.selectOption('FAMILIA_UAT');
  await expect(selector).toHaveValue('FAMILIA_UAT');
  await expect(selector.locator('option:checked')).toHaveText('Familia UAT elegible');
  await expect(page.locator('[data-modal="nueva-evaluacion"] .modal-header-institutional')).toContainText('Versión activa:');
  await page.getByRole('button', { name: 'Cancelar' }).last().click();
  expect(putCount).toBe(0);
});
