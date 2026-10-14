import { expect, test } from '@playwright/test';

function token(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ nameid: '987654', uid: 'file-upload-e2e', modulos: '4', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
}

test('Monitoreo de Listas reutiliza la carga institucional con selector, retiro y drag/drop', async ({ page }, testInfo) => {
  test.setTimeout(60_000);
  await page.setViewportSize({ width: 1280, height: 900 });
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
  await page.route('**/api/catalogos/modulos', route => route.fulfill({ status: 200, contentType: 'application/json', body: '{"success":true,"datos":[]}' }));

  await page.route('**/api/listas/**', async route => {
    const pathname = new URL(route.request().url()).pathname;
    let datos: unknown = [];
    if (pathname.endsWith('/juridicas/paginado')) {
      datos = {
        items: [{ rtn: '08011999123456', nombre: 'Empresa E2E', numeroPatrono: '08011999123456', listaCoincidencia: 'Lista de prueba', fechaEncontro: '2026-10-01T12:00:00Z', fechaCalifico: '2026-10-01T12:00:00Z', fechaRegistroInterno: null, esProveedorIhss: 'NO', tieneMotivo: true, esManual: false }],
        pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1,
        totales: { totalRegistros: 1, pendientes: 0, conMotivo: 1, manuales: 0, cerradosPasivos: 0 },
      };
    } else if (pathname.endsWith('/evidencias/politica')) {
      datos = { maximoMb: 10, maximoBytes: 10 * 1024 * 1024, extensionesPermitidas: ['.pdf', '.png', '.jpg', '.jpeg', '.doc', '.docx', '.xls', '.xlsx'], tiposPermitidosTexto: 'PDF, imágenes, Word, Excel' };
    } else if (pathname.endsWith('/positivos/08011999123456/seguimientos')) {
      datos = [];
    }
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });

  await page.goto('/monitoreo-listas');
  await expect(page.getByText('Empresa E2E')).toBeVisible();
  await page.getByRole('button', { name: 'Dar seguimiento' }).click();
  const uploadButton = page.getByRole('button', { name: 'Archivos de Evidencia' });
  await expect(uploadButton).toBeVisible();
  const descriptionId = await uploadButton.getAttribute('aria-describedby');
  expect(descriptionId).toBeTruthy();
  await expect(page.locator(`[id="${descriptionId}"]`)).toContainText('PDF, imágenes, Word, Excel');

  const chooserPromise = page.waitForEvent('filechooser');
  await uploadButton.click();
  const chooser = await chooserPromise;
  await chooser.setFiles({ name: 'evidencia-pendiente.pdf', mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4 test') });
  await expect(page.getByText('evidencia-pendiente.pdf')).toBeVisible();
  await page.getByRole('button', { name: 'Quitar archivo evidencia-pendiente.pdf' }).click();
  await expect(page.getByText('evidencia-pendiente.pdf')).toHaveCount(0);

  const dropzone = page.getByRole('button', { name: 'Archivos de Evidencia' });
  const transfer = await page.evaluateHandle(() => {
    const data = new DataTransfer();
    data.items.add(new File(['%PDF-1.4 test'], 'evidencia-arrastrada.pdf', { type: 'application/pdf' }));
    return data;
  });
  await dropzone.dispatchEvent('dragenter', { dataTransfer: transfer });
  await expect(dropzone).toHaveClass(/border-ihss-500/);
  await dropzone.dispatchEvent('dragover', { dataTransfer: transfer });
  await dropzone.dispatchEvent('drop', { dataTransfer: transfer });
  await expect(page.getByText('evidencia-arrastrada.pdf')).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('institutional-file-upload-monitoring-listas.png'), fullPage: true });

  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.locator('html')).toHaveJSProperty('scrollWidth', await page.locator('html').evaluate(node => node.clientWidth));
  await page.screenshot({ path: testInfo.outputPath('institutional-file-upload-mobile.png'), fullPage: true });
});
