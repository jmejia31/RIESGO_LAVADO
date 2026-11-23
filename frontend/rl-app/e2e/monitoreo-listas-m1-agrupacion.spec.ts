import { expect, test } from './support/telemetry-test';

function token(): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ nameid: '987654', uid: 'm1-agrupacion-e2e', modulos: '4', exp: Math.floor(Date.now() / 1000) + 3600 })}.`;
}

test('M1-P0: Monitoreo de Listas muestra naturales agrupados por DNI, detalle completo y empleados historicos', async ({ page }) => {
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
    const url = new URL(route.request().url());
    const pathname = url.pathname;
    let datos: unknown = [];

    if (pathname.endsWith('/juridicas/paginado')) {
      datos = {
        items: [], pagina: 1, tamanoPagina: 10, totalRegistros: 0, totalPaginas: 0,
        totales: { totalRegistros: 0, totalCoincidenciasFuente: 0, pendientes: 0, conMotivo: 0, manuales: 0, cerradosPasivos: 0 },
      };
    } else if (pathname.endsWith('/naturales/paginado')) {
      datos = {
        items: [
          {
            numeroIdentificacion: '0801-1981-00002',
            nombre: 'MARIA MULTI',
            listaCoincidencia: 'OFAC (+1)',
            totalRepetidos: 3,
            idRegistroFuente: 91002,
            tipoFuente: 'DNP_COINCIDENCIA',
            categoriaIdentidad: 'IDENTIFICADA',
            fechaEncontro: '2026-07-15T00:00:00',
            tieneMotivo: false,
            esManual: false,
            estadoIdentidad: 'VERIFIED',
            estadoAsociacion: 'AMBIGUOUS',
          },
        ],
        pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1,
        totales: { totalRegistros: 1, totalCoincidenciasFuente: 3, pendientes: 1, conMotivo: 0, manuales: 0, cerradosPasivos: 0 },
      };
    } else if (pathname.includes('/naturales/detalle') || pathname.endsWith('/naturales/0801-1981-00002/detalle')) {
      datos = [
        {
          idRegistroFuente: 91002, tipoFuente: 'DNP_COINCIDENCIA', numeroIdentificacion: '0801-1981-00002',
          nombresPersona: 'MARIA MULTI', tipoCondicionActuaDesc: 'SOCIO', numeroPatronal: '2001001',
          nombreEmpresa: 'EMPRESA ALPHA SA', esPep: 'SI', listaCoincidencia: 'OFAC',
          fechaCoincidencia: '2026-05-01T00:00:00', fechaCalifico: '2026-05-02T00:00:00', estadoAsociacion: 'VERIFIED',
        },
        {
          idRegistroFuente: 91003, tipoFuente: 'DNP_COINCIDENCIA', numeroIdentificacion: '0801-1981-00002',
          nombresPersona: 'MARIA MULTI', tipoCondicionActuaDesc: 'REPRESENTANTE LEGAL', numeroPatronal: '2001002',
          nombreEmpresa: 'EMPRESA BETA SA', esPep: 'NO', listaCoincidencia: 'PEPS',
          fechaCoincidencia: '2026-06-10T00:00:00', fechaCalifico: '2026-06-11T00:00:00', estadoAsociacion: 'VERIFIED',
        },
        {
          idRegistroFuente: 91004, tipoFuente: 'DNP_COINCIDENCIA', numeroIdentificacion: '0801-1981-00002',
          nombresPersona: 'MARIA MULTI', tipoCondicionActuaDesc: 'SOCIO', numeroPatronal: '2001001',
          nombreEmpresa: 'EMPRESA ALPHA SA', esPep: 'SI', listaCoincidencia: 'OFAC',
          fechaCoincidencia: '2026-07-15T00:00:00', fechaCalifico: '2026-07-16T00:00:00', estadoAsociacion: 'VERIFIED',
        },
      ];
    } else if (pathname.endsWith('/empleados/paginado')) {
      datos = {
        items: [
          {
            identidad: '0801-1990-00010',
            nombre: 'PEDRO EMPLEADO HISTORICO',
            listaCoincidencia: 'OFAC (+1)',
            totalRepetidos: 2,
            idRegistroFuente: 91011,
            tipoFuente: 'DNP_COINCIDENCIA',
            numeroPatrono: '101196100011',
            categoriaIdentidad: 'IDENTIFICADA',
            fechaEncontro: '2026-01-20T00:00:00',
            tieneMotivo: false,
            esManual: false,
            estadoIdentidad: 'VERIFIED',
            estadoAsociacion: 'AMBIGUOUS',
          },
        ],
        pagina: 1, tamanoPagina: 10, totalRegistros: 1, totalPaginas: 1,
        totales: { totalRegistros: 1, totalCoincidenciasFuente: 2, pendientes: 1, conMotivo: 0, manuales: 0, cerradosPasivos: 0 },
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

  await page.getByRole('button', { name: /Personas naturales/i }).click();
  await expect(page.getByText('MARIA MULTI')).toBeVisible();
  await expect(page.getByText('3 coincidencias', { exact: true })).toBeVisible();
  await expect(page.getByText('Registros agrupados por identificación (3 coincidencias de origen)')).toBeVisible();

  await page.getByRole('button', { name: 'Consultar la coincidencia seleccionada de persona natural' }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await expect(dialog.getByText('#91002')).toBeVisible();
  await expect(dialog.getByText('#91003')).toBeVisible();
  await expect(dialog.getByText('#91004')).toBeVisible();
  await expect(dialog.getByText('EMPRESA ALPHA SA').first()).toBeVisible();
  await expect(dialog.getByText('EMPRESA BETA SA')).toBeVisible();
  await dialog.getByRole('button', { name: 'Cerrar detalle de coincidencias' }).first().click();

  await page.getByRole('button', { name: /Empleados/i }).click();
  await expect(page.getByText('PEDRO EMPLEADO HISTORICO')).toBeVisible();
  await expect(page.getByText('2 coincidencias', { exact: true })).toBeVisible();
  await expect(page.getByText('Registros agrupados por identificación (2 coincidencias de origen)')).toBeVisible();
});
