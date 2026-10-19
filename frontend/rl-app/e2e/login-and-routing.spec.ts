import { expect, Page, test } from './support/telemetry-test';

const systemConfiguration = {
  success: true,
  datos: {
    nombreSistema: 'SGRLA-IHSS',
    nombreInstitucion: 'Instituto Hondureño de Seguridad Social',
    colorPrimario: '#1e3a8a',
    colorSecundario: '#1d4ed8',
    timeoutSesion: 30,
  },
};

const versionFormulario = {
  verId: 10,
  verFamiliaId: 1,
  verCodigo: 'FORM_MATRIZ_LAFT',
  verVersion: 3,
  verJson: JSON.stringify({
    codigoFormulario: 'FORM_MATRIZ_LAFT',
    nombreFormulario: 'Matriz de Riesgos LA/FT',
    secciones: [
      {
        clave: 'identificacion',
        titulo: 'Identificación del riesgo',
        orden: 1,
        campos: [
          {
            clave: 'area_principal',
            etiqueta: 'Área principal',
            tipo: 'texto',
            obligatorio: true,
            soloLectura: false,
          },
          {
            clave: 'dueno_riesgo',
            etiqueta: 'Dueño del riesgo',
            tipo: 'texto',
            obligatorio: true,
            soloLectura: false,
          },
        ],
      },
    ],
    reglas: [{ codigo: 'CALCULO_VRI_VRR', version: '1.0' }],
  }),
  verHash: 'hash-e2e',
  verEstado: 'PUBLISHED',
  verVigente: true,
  verFechaInicio: '2026-08-03T10:00:00Z',
  verFechaFin: null,
  verFechaCreacion: '2026-08-03T09:00:00Z',
  verUsrCreacion: 27,
};

const metodologiaFormulario = {
  versionFormularioId: 10,
  codigo: 'FORM_MATRIZ_LAFT',
  version: 3,
  secciones: [
    {
      clave: 'identificacion',
      titulo: 'Identificación del riesgo',
      orden: 1,
      campos: [
        {
          campoCanonicoId: 101,
          clave: 'area_principal',
          etiqueta: 'Área principal',
          tipo: 'texto',
          codigoCatalogo: null,
          obligatorio: true,
          soloLectura: false,
        },
        {
          campoCanonicoId: 102,
          clave: 'dueno_riesgo',
          etiqueta: 'Dueño del riesgo',
          tipo: 'texto',
          codigoCatalogo: null,
          obligatorio: true,
          soloLectura: false,
        },
      ],
    },
  ],
  catalogos: [],
  reglas: [
    {
      codigo: 'CALCULO_VRI_VRR',
      version: '1.0',
      algoritmoId: 'MATRICES_VRI_ADITIVO_1_9',
      parametros: null,
    },
  ],
};

const riesgos = [
  {
    rieId: 501,
    rieCodigo: 'R-501',
    rieNombre: 'Riesgo de cumplimiento',
    rieDescripcion: 'Riesgo institucional de prueba E2E',
    rieActivo: true,
    rieUsrCreacion: 27,
    rieFechaCreacion: '2026-08-03T09:00:00Z',
  },
  {
    rieId: 502,
    rieCodigo: 'R-502',
    rieNombre: 'Riesgo tecnológico',
    rieDescripcion: 'Riesgo tecnológico de prueba E2E',
    rieActivo: true,
    rieUsrCreacion: 27,
    rieFechaCreacion: '2026-08-03T09:00:00Z',
  },
];

const evaluaciones = [
  {
    evaId: 200,
    evaRiesgoId: 501,
    evaVersionId: 10,
    evaEstado: 'BORRADOR',
    evaDataJson: JSON.stringify({
      area_principal: 'Cumplimiento',
      dueno_riesgo: 'Jefatura de Cumplimiento',
    }),
    evaDataCalcJson: '{}',
    evaVri: 7,
    evaVrr: 4,
    evaFechaEval: '2026-08-03T10:00:00Z',
    evaUsrEval: 27,
    evaVersionRow: 1,
    evaActivo: true,
  },
];

const consolidadoTipado = [
  {
    riesgoId: 501,
    evaluacionId: 200,
    versionFormularioId: 10,
    codigoRiesgo: 'R-501',
    areaPrincipal: 'Cumplimiento',
    duenoRiesgo: 'Jefatura de Cumplimiento',
    vri: 7,
    nivelInherente: 'ALTO',
    vrr: 4,
    nivelResidual: 'MODERADO',
    respuestaRiesgo: 'MITIGAR',
    estadoEvaluacion: 'BORRADOR',
    fechaEvaluacion: '2026-08-03T10:00:00Z',
  },
];

const flujosEvaluacion = [
  {
    fluId: 1,
    fluEvaluacionId: 200,
    fluEstado: 'BORRADOR',
    fluMotivo: 'Captura inicial',
    fluUsrId: 27,
    fluFecha: '2026-08-03T10:00:00Z',
  },
];

async function stubPublicConfiguration(page: Page) {
  await page.route('**/api/configuracion/sistema', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(systemConfiguration),
  }));

  await page.route('**/api/configuracion/login', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));
}

function createUnsignedAccessToken() {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({
    nameid: '27',
    uid: 'e2e.matrices',
    email: 'e2e.matrices@ihss.hn',
    given_name: 'Prueba',
    family_name: 'E2E',
    role: 'ADMINISTRADOR',
    rol_id: '1',
    modulos: '10',
    debe_cambiar_pass: '0',
    exp: Math.floor(Date.now() / 1000) + 3600,
  })}.`;
}

async function stubAuthenticatedMatrices(page: Page, fixture: { version?: unknown; metodologia?: unknown } = {}) {
  await page.route('**/api/catalogos/modulos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));

  const accessToken = createUnsignedAccessToken();
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'refresh-e2e-local');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, accessToken);

  let persistedDraft: Record<string, unknown> | null = null;
  const draftProjection = {
    evaluationId: 301,
    riskCode: 'R-502',
    fields: Array.from({ length: 82 }, (_, index) => ({
      fieldNumber: index + 1,
      fieldKey: `field_${index + 1}`,
      fieldLabel: `Field ${index + 1}`,
      valueType: 'text',
      value: null,
      state: 'LEGIT_BLANK',
      source: 'E2E_FIXTURE',
    })),
  };

  await page.route('**/api/matrices-riesgos/**', route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();
    let datos: unknown = [];

    if (path.endsWith('/formulario/version-vigente')) {
      datos = fixture.version ?? versionFormulario;
    } else if (path.endsWith('/familias/paginado')) {
      datos = {
        items: [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: 'Familia E2E', famActivo: true, famFechaCreacion: '2026-08-01T00:00:00Z', totalVersiones: 1, tieneVersionVigente: true }],
        pagina: 1,
        tamanoPagina: 10,
        totalRegistros: 1,
        totalPaginas: 1,
        totales: { totalFamilias: 1, activas: 1, inactivas: 0, totalVersiones: 1 },
      };
    } else if (path.endsWith('/familias')) {
      datos = [{ famId: 1, famCodigo: 'MATRIZ_RIESGOS_LAFT', famNombre: 'Matriz de Riesgos LAFT', famDescripcion: 'Familia E2E', famActivo: true, famFechaCreacion: '2026-08-01T00:00:00Z', totalVersiones: 1, tieneVersionVigente: true }];
    } else if (path.endsWith('/metodologia/vigente')) {
      datos = fixture.metodologia ?? metodologiaFormulario;
    } else if (path.endsWith('/formularios/historial')) {
      datos = [versionFormulario];
    } else if (/\/formularios\/\d+$/.test(path) && method === 'GET') {
      datos = versionFormulario;
    } else if (path.endsWith('/consolidado/paginado')) {
      datos = {
        items: consolidadoTipado,
        pagina: 1,
        tamanoPagina: 10,
        totalRegistros: consolidadoTipado.length,
        totalPaginas: 1,
        totales: {
          totalRiesgos: consolidadoTipado.length,
          totalConEvaluacionOficial: consolidadoTipado.length,
          totalSinEvaluacionOficial: 0,
          totalAltoCritico: consolidadoTipado.filter(fila => fila.nivelResidual === 'ALTO' || fila.nivelResidual === 'CRITICO').length,
        },
      };
    } else if (path.endsWith('/consolidado')) {
      datos = consolidadoTipado;
    } else if (path.endsWith('/riesgos/paginado')) {
      datos = {
        items: riesgos,
        pagina: 1,
        tamanoPagina: 200,
        totalRegistros: riesgos.length,
        totalPaginas: 1,
      };
    } else if (path.endsWith('/riesgos') && method === 'GET') {
      datos = riesgos;
    } else if (path.endsWith('/evaluaciones') && method === 'GET') {
      datos = {
        items: [{
          evaId: 200,
          evaRiesgoId: 501,
          riesgoCodigo: 'R-501',
          riesgoNombre: 'Riesgo de cumplimiento',
          evaVersionId: 10,
          versionCodigo: 'FORM_MATRIZ_LAFT',
          versionNumero: 1,
          estado: 'BORRADOR',
          vri: 7,
          vrr: 4,
          nivelResidual: 'MODERADO',
          fechaEval: '2026-08-03T10:00:00Z'
        }, ...(persistedDraft ? [{
          evaId: 301,
          evaRiesgoId: 502,
          riesgoCodigo: 'R-502',
          riesgoNombre: 'Riesgo tecnológico',
          evaVersionId: 10,
          versionCodigo: 'FORM_MATRIZ_LAFT',
          versionNumero: 3,
          estado: 'BORRADOR',
          fechaEval: '2026-08-03T10:00:00Z'
        }] : [])],
        pagina: 1,
        registrosPorPagina: 10,
        totalRegistros: persistedDraft ? 2 : 1,
        totalPaginas: 1
      };
    } else if (/\/evaluaciones\/\d+$/.test(path) && method === 'GET') {
      datos = path.endsWith('/301')
        ? persistedDraft ?? { ...evaluaciones[0], evaId: 301, evaRiesgoId: 502 }
        : evaluaciones[0];
    } else if (/\/metodologia\/version\/\d+$/.test(path) && method === 'GET') {
      datos = metodologiaFormulario;
    } else if (path.endsWith('/evaluaciones/borrador') && method === 'POST') {
      const body = request.postDataJSON() as Record<string, unknown>;
      persistedDraft = { ...body, evaId: 301 };
      datos = 301;
    } else if (/\/evaluaciones\/301\/borrador$/.test(path) && method === 'PUT') {
      persistedDraft = request.postDataJSON() as Record<string, unknown>;
      datos = null;
    } else if (path.endsWith('/evaluaciones/calculo-preview') && method === 'POST') {
      datos = { listo: true, evaDataCalcJson: '{}', evaVri: null, evaEtp: null, evaVrr: null, obligatoriosPendientes: [] };
    } else if (path.endsWith('/evaluaciones/301/proyeccion-institucional') && method === 'GET') {
      datos = draftProjection;
    } else if (path.endsWith('/evaluaciones') && method === 'POST') {
      datos = 201;
    } else if (/\/evaluaciones\/\d+$/.test(path) && method === 'PUT') {
      datos = null;
    } else if (/\/evaluaciones\/\d+\/flujos$/.test(path) && method === 'GET') {
      datos = flujosEvaluacion;
    } else if (/\/evaluaciones\/\d+\/transiciones$/.test(path) && method === 'POST') {
      datos = null;
    }

    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ success: true, datos }),
    });
  });

  await page.route('**/api/matrices-riesgos*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      success: true,
      datos: {
        items: [{
          evaId: 200,
          evaRiesgoId: 501,
          riesgoCodigo: 'R-501',
          riesgoNombre: 'Riesgo de cumplimiento',
          evaVersionId: 10,
          versionCodigo: 'FORM_MATRIZ_LAFT',
          versionNumero: 1,
          estado: 'BORRADOR',
          vri: 7,
          vrr: 4,
          nivelResidual: 'MODERADO',
          fechaEval: '2026-08-03T10:00:00Z'
        }],
        pagina: 1,
        registrosPorPagina: 10,
        totalRegistros: 1,
        totalPaginas: 1
      }
    }),
  }));
}

test.beforeEach(async ({ page }) => {
  await stubPublicConfiguration(page);
});

test('muestra el formulario de acceso institucional', async ({ page }) => {
  await page.goto('/login');

  await expect(page.getByRole('heading', { name: 'Prevención de Lavado' })).toBeVisible();
  await expect(page.getByLabel('Correo o Usuario')).toBeVisible();
  await expect(page.getByRole('textbox', { name: 'Contraseña' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Iniciar Sesión' })).toBeVisible();
});

test('valida campos obligatorios sin enviar credenciales', async ({ page }) => {
  let loginRequests = 0;
  page.on('request', request => {
    if (/\/api\/auth\/login$/i.test(request.url())) {
      loginRequests++;
    }
  });

  await page.goto('/login');
  await page.getByRole('button', { name: 'Iniciar Sesión' }).click();

  await expect(page.getByText('El correo o usuario es obligatorio')).toBeVisible();
  await expect(page.getByText('La contraseña es obligatoria')).toBeVisible();
  expect(loginRequests).toBe(0);
});

test('permite alternar la visibilidad de la contraseña', async ({ page }) => {
  await page.goto('/login');
  const password = page.getByRole('textbox', { name: 'Contraseña' });

  await password.fill('clave-segura');
  await expect(password).toHaveAttribute('type', 'password');
  await page.locator('#password + button').click();
  await expect(password).toHaveAttribute('type', 'text');
});

test('redirige rutas protegidas al login cuando no existe sesión', async ({ page }) => {
  await page.goto('/matrices-riesgos');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('button', { name: 'Iniciar Sesión' })).toBeVisible();
});

test('redirige rutas desconocidas al login', async ({ page }) => {
  await page.goto('/ruta-inexistente');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Prevención de Lavado' })).toBeVisible();
});

test('abre Matrices de Riesgos con una sesión autenticada y contratos dinámicos', async ({ page }) => {
  await stubAuthenticatedMatrices(page);

  await page.goto('/matrices-riesgos');

  await expect(page).toHaveURL(/\/matrices-riesgos$/);
  await expect(page.locator('#titulo-matrices')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Borrador', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Nueva evaluación' })).toBeVisible();
});

test('crea y recarga un borrador parcial antes de avanzar por bloques', async ({ page }) => {
  await stubAuthenticatedMatrices(page);
  await page.goto('/matrices-riesgos');

  await expect(page).toHaveURL(/\/matrices-riesgos$/);
  await expect(page.locator('#titulo-matrices')).toBeVisible();
  await page.getByRole('button', { name: 'Nueva evaluación' }).click();
  await expect(page.getByRole('heading', { name: 'Nueva Evaluación de Riesgo' })).toBeVisible();
  await page.locator('#modal-selector-familia').selectOption('MATRIZ_RIESGOS_LAFT');
  await expect(page.getByText('Identificación del riesgo', { exact: true })).toBeVisible();

  const modalNuevaEvaluacion = page.locator('[data-modal="nueva-evaluacion"]');
  await expect(modalNuevaEvaluacion).toHaveClass(/modal-size-workspace/);
  await expect(modalNuevaEvaluacion.locator('.modal-body-scrollable')).toHaveCSS('overflow-y', 'auto');

  await page.locator('#modal-selector-riesgo').selectOption({ label: 'R-502 — Riesgo tecnológico' });
  await page.getByLabel('Área principal').fill('Tecnología');
  await page.getByLabel('Dueño del riesgo').fill('Gerencia de Tecnología');
  const saveRequest = page.waitForRequest(request =>
    request.method() === 'POST'
      && new URL(request.url()).pathname.endsWith('/api/matrices-riesgos/evaluaciones/borrador')
  );
  await page.getByRole('button', { name: 'Continuar' }).click();
  const request = await saveRequest;
  const payload = request.postDataJSON();
  expect(payload.evaRiesgoId).toBe(502);
  expect(payload.evaVersionId).toBe(10);
  expect(JSON.parse(payload.evaDataJson)).toEqual({
    area_principal: 'Tecnología',
    dueno_riesgo: 'Gerencia de Tecnología',
  });
  expect(payload.evaDataCalcJson).toBe('{}');

  await expect(page.getByRole('heading', { name: 'Editar Evaluación #301' })).toBeVisible();
  await expect(page.getByRole('button', { name: '2. Controles' })).toBeEnabled();
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Matrices de Riesgos' })).toBeVisible();
  await page.getByTitle('Editar evaluación').last().click();
  await expect(page.getByRole('heading', { name: 'Editar Evaluación #301' })).toBeVisible();
  await expect(page.getByLabel('Área principal')).toHaveValue('Tecnología');
});

test('asocia el error requerido al control y enfoca el primer campo al continuar', async ({ page }) => {
  await stubAuthenticatedMatrices(page);
  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: 'Nueva evaluación' }).click();
  await page.locator('#modal-selector-familia').selectOption('MATRIZ_RIESGOS_LAFT');
  await page.locator('#modal-selector-riesgo').selectOption({ label: 'R-502 — Riesgo tecnológico' });

  const continueButton = page.getByRole('button', { name: /Continuar/ }).last();
  await continueButton.click();

  const area = page.getByLabel('Área principal');
  await expect(area).toHaveAttribute('aria-invalid', 'true');
  const describedBy = await area.getAttribute('aria-describedby');
  expect(describedBy).toBeTruthy();
  await expect(page.locator(`#${describedBy}`)).toContainText('Área principal es obligatorio.');
  await expect(area).toBeFocused();
  await expect(page.getByRole('heading', { name: 'Nueva Evaluación de Riesgo' })).toBeVisible();
  const modalError = page.locator('[data-uat="feedback-nueva-error"]');
  await expect(modalError).toContainText('campo(s) requerido(s)');
  await expect(page.locator('[data-uat="feedback-page-error"]')).toHaveCount(0);

  page.on('dialog', dialog => dialog.accept());
  await page.getByRole('button', { name: 'Cerrar creación' }).click();
  await expect(page.locator('[data-modal="nueva-evaluacion"]')).toHaveCount(0);
  await expect(page.locator('[data-uat="feedback-page-error"]')).toHaveCount(0);
  await page.getByRole('button', { name: 'Nueva evaluación' }).click();
  await expect(page.locator('[data-modal="nueva-evaluacion"]')).toBeVisible();
  await expect(page.locator('[data-uat="feedback-nueva-error"]')).toHaveCount(0);
});

test('el detalle de cálculo expone su estado expandido en el editor', async ({ page }) => {
  await stubAuthenticatedMatrices(page);
  await page.goto('/matrices-riesgos');
  await page.getByTitle('Editar evaluación').first().click();
  await page.getByRole('button', { name: '5. Cálculos Auxiliares y Verificaciones' }).click();

  const disclosure = page.getByRole('button', { name: 'Mostrar detalle de cálculos' });
  await expect(disclosure).toHaveAttribute('aria-expanded', 'false');
  await disclosure.click();
  await expect(page.getByRole('button', { name: 'Ocultar detalle de cálculos' })).toHaveAttribute('aria-expanded', 'true');
  await expect(page.locator('[data-matrix-field]').filter({ has: page.getByText(/Frecuencia Residual \(AUX\)/) })).toBeVisible();
});

test('el editor de evaluación no causa overflow horizontal en escritorio ni móvil', async ({ page }, testInfo) => {
  await stubAuthenticatedMatrices(page);
  await page.goto('/matrices-riesgos');
  await page.getByTitle('Editar evaluación').first().click();

  for (const viewport of [{ width: 1280, height: 900 }, { width: 390, height: 844 }]) {
    await page.setViewportSize(viewport);
    for (const block of [1, 2, 4, 5]) {
      await page.getByRole('button', { name: new RegExp(`^${block}\\.`) }).click();
      if (block === 5) {
        const detail = page.getByRole('button', { name: 'Mostrar detalle de cálculos' });
        if (await detail.isVisible()) await detail.click();
      }
      const widths = await page.evaluate(() => ({
        documentWidth: document.documentElement.scrollWidth,
        viewportWidth: document.documentElement.clientWidth
      }));
      expect(widths.documentWidth, `horizontal overflow at ${viewport.width}px in block ${block}`).toBeLessThanOrEqual(widths.viewportWidth);
      const device = viewport.width === 1280 ? 'desktop' : 'mobile';
      const layout = block === 5 ? 'block5-expanded' : `block${block}`;
      await page.screenshot({ path: testInfo.outputPath(`${device}-${layout}.png`), fullPage: true });
    }
  }
});

test('consulta una evaluación existente y permite abrir su edición', async ({ page }) => {
  await stubAuthenticatedMatrices(page);
  await page.goto('/matrices-riesgos');

  await page.getByTitle('Editar evaluación').first().click();
  await expect(page.getByRole('heading', { name: 'Editar Evaluación' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Guardar borrador' })).toBeVisible();
});
test('smoke anti-regresion: Matrices no queda en blanco ni registra errores de runtime', async ({ page }) => {
  const pageErrors: string[] = [];
  const consoleErrors: string[] = [];
  const failedRequests: string[] = [];
  page.on('pageerror', error => pageErrors.push(error.stack || error.message));
  page.on('console', message => {
    if (message.type() === 'error') consoleErrors.push(message.text());
  });
  page.on('requestfailed', request => failedRequests.push(`${request.url()} :: ${request.failure()?.errorText || 'unknown'}`));

  await page.route('**/api/catalogos/modulos', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
  }));
  await page.route('https://fonts.googleapis.com/**', route => route.fulfill({ status: 200, contentType: 'text/css', body: '' }));
  await page.route('https://fonts.gstatic.com/**', route => route.fulfill({ status: 200, body: '' }));
  await stubAuthenticatedMatrices(page);
  await page.goto('/matrices-riesgos');

  await expect(page.locator('app-matrices-riesgos')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Matrices de Riesgos' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await expect(page.locator('app-matrices-riesgos')).not.toBeEmpty();
  await page.screenshot({ path: 'test-results/p0-matrices-smoke-1536x1024.png', fullPage: true });
  expect(pageErrors).toEqual([]);
  expect(consoleErrors, failedRequests.join('\n')).toEqual([]);
});

test('long form test-only valida robustez con un escenario representativo', async ({ page }) => {
  const secciones = Array.from({ length: 9 }, (_, s) => ({ clave: `long_${s}`, titulo: `Long ${s}`, orden: s + 1, columnasPorFila: 3, campos: Array.from({ length: 10 }, (_, f) => ({ clave: `long_field_${s * 10 + f + 1}`, etiqueta: `Long field ${s * 10 + f + 1}`, tipo: 'texto', obligatorio: false, soloLectura: false })) }));
  const definicion = { codigoFormulario: 'FORM_LONG_UAT', nombreFormulario: 'Long UAT', secciones };
  const version = { ...versionFormulario, verId: 910, verCodigo: 'FORM_LONG_UAT', verVersion: 90, verJson: JSON.stringify(definicion) };
  const metodologia = { ...metodologiaFormulario, versionFormularioId: 910, codigo: 'FORM_LONG_UAT', version: 90, secciones, catalogos: [] };
  await stubAuthenticatedMatrices(page, { version, metodologia });
  await page.goto('/matrices-riesgos');
  await page.getByRole('button', { name: /crear nueva evaluación/i }).click();
  const modal = page.locator('[data-modal="nueva-evaluacion"]');
  const scroll = modal.locator('.modal-body-scrollable');
  await modal.locator('#modal-selector-familia').selectOption('MATRIZ_RIESGOS_LAFT');
  await expect(modal.locator('[data-evaluation-field="long_field_1"]')).toBeVisible();
  const middleField = modal.locator('[data-evaluation-field="long_field_45"]');
  await middleField.scrollIntoViewIfNeeded();
  await expect(middleField).toBeVisible();
  await scroll.evaluate(element => { element.scrollTop = element.scrollHeight; });
  await expect(modal.locator('[data-evaluation-field="long_field_90"]')).toBeVisible();
  const metrics = await scroll.evaluate(element => ({ vertical: element.scrollHeight > element.clientHeight, horizontal: element.scrollWidth > element.clientWidth, top: element.scrollTop }));
  expect(metrics.vertical).toBe(true); expect(metrics.horizontal).toBe(false); expect(metrics.top).toBeGreaterThan(0);
  await expect(modal.getByRole('button', { name: 'Guardar borrador' })).toBeVisible();
});
