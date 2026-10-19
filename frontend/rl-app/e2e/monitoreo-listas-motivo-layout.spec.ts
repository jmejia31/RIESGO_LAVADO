import { expect, test } from './support/telemetry-test';

function token(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ nameid: '987654', uid: 'motivo-layout-e2e', modulos: '4', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
}

test('Monitoreo de Listas mantiene el layout del formulario de motivo en desktop, tablet y mobile', async ({ page }, testInfo) => {
  await page.addInitScript(accessToken => {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', 'e2e-only');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, token());

  await page.route('**/api/configuracion/**', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: { nombreSistema: 'SGRLA-IHSS', nombreInstitucion: 'IHSS', timeoutSesion: 30 } }),
  }));
  await page.route('**/api/catalogos/modulos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: '{"success":true,"datos":[]}',
  }));
  await page.route('**/api/listas/**', async route => {
    const pathname = new URL(route.request().url()).pathname;
    let datos: unknown = [];
    if (pathname.endsWith('/juridicas/paginado')) {
      datos = {
        items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0,
        totales: { totalRegistros: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 },
      };
    } else if (pathname.endsWith('/tipos-documento')) {
      datos = [{ tipoDocumentoId: 1, descripcion: 'Documento de identidad' }];
    } else if (pathname.endsWith('/tipos-listas-cautela')) {
      datos = [{ tipoListaCautelaId: 1, descripcion: 'Lista institucional' }];
    } else if (pathname.endsWith('/evidencias/politica')) {
      datos = { maximoMb: 10, maximoBytes: 10 * 1024 * 1024, extensionesPermitidas: ['.pdf'], tiposPermitidosTexto: 'PDF' };
    }
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });

  await page.goto('/monitoreo-listas');
  await expect(page.getByRole('heading', { name: 'Monitoreo de Listas' })).toBeVisible();
  await page.getByRole('button', { name: 'Crear positivo manual' }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByRole('heading', { name: 'Registrar Motivo en Lista de Positivos' })).toBeVisible();

  const identityRow = dialog.getByTestId('positive-reason-identity-row');
  const catalogRow = dialog.getByTestId('positive-reason-catalog-row');
  const catalogControls = catalogRow.locator('select');
  await expect(catalogControls).toHaveCount(3);
  await expect(catalogRow.getByText('Tipo de Documento', { exact: true })).toBeVisible();
  await expect(catalogRow.getByText('Tipo de Lista de Cautela', { exact: true })).toBeVisible();
  await expect(catalogRow.getByText('Origen del Registro', { exact: true })).toBeVisible();

  const layouts = [
    { name: 'desktop-1920x1080', width: 1920, height: 1080, catalogColumns: 3, identityColumns: 2 },
    { name: 'desktop-1536x864', width: 1536, height: 864, catalogColumns: 3, identityColumns: 2 },
    { name: 'desktop-1366x768', width: 1366, height: 768, catalogColumns: 3, identityColumns: 2 },
    { name: 'tablet-1024x768', width: 1024, height: 768, catalogColumns: 2, identityColumns: 2 },
    { name: 'mobile-390x844', width: 390, height: 844, catalogColumns: 1, identityColumns: 1 },
  ];

  for (const layout of layouts) {
    await page.setViewportSize({ width: layout.width, height: layout.height });
    await expect.poll(() => catalogRow.locator(':scope > div').evaluateAll(nodes => new Set(nodes.map(node => Math.round(node.getBoundingClientRect().top))).size)).toBe(layout.catalogColumns === 3 ? 1 : layout.catalogColumns === 2 ? 2 : 3);
    await expect.poll(() => identityRow.locator(':scope > div').evaluateAll(nodes => new Set(nodes.map(node => Math.round(node.getBoundingClientRect().top))).size)).toBe(layout.identityColumns === 2 ? 1 : 2);
    await expect(page.locator('html')).toHaveJSProperty('scrollWidth', await page.locator('html').evaluate(node => node.clientWidth));
    const boxes = await catalogControls.evaluateAll(nodes => nodes.map(node => {
      const rect = node.getBoundingClientRect();
      return { x: rect.x, y: rect.y, width: rect.width, right: rect.right };
    }));
    expect(boxes.every(box => box.width > 0 && box.right <= layout.width)).toBe(true);
    expect(new Set(boxes.map(box => Math.round(box.y))).size).toBe(layout.catalogColumns === 3 ? 1 : layout.catalogColumns === 2 ? 2 : 3);
    const labelWidths = await catalogRow.locator('label').evaluateAll(nodes => nodes.map(node => ({ client: node.clientWidth, scroll: node.scrollWidth })));
    expect(labelWidths.every(label => label.scroll <= label.client + 1)).toBe(true);
    const textareas = dialog.locator('.modal-body-scrollable > div textarea');
    await expect(textareas).toHaveCount(2);
    const fullWidth = await catalogRow.evaluate(node => node.getBoundingClientRect().width);
    const textareaWidths = await textareas.evaluateAll(nodes => nodes.map(node => node.getBoundingClientRect().width));
    expect(textareaWidths.every(width => width >= fullWidth * 0.98)).toBe(true);
    await page.screenshot({ path: testInfo.outputPath(`motivo-layout-${layout.name}.png`), fullPage: true });
  }
});
