import { expect, Page, test } from '@playwright/test';

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

async function stubAuthenticatedMatrices(page: Page) {
  const accessToken = createUnsignedAccessToken();
  await page.addInitScript(token => {
    localStorage.setItem('access_token', token);
    localStorage.setItem('refresh_token', 'refresh-e2e-local');
    localStorage.setItem('token_expira', new Date(Date.now() + 3_600_000).toISOString());
  }, accessToken);

  await page.route('**/api/matrices-riesgos/**', route => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    const method = request.method();
    let datos: unknown = [];

    if (path.endsWith('/formulario/version-vigente')) {
      datos = versionFormulario;
    } else if (path.endsWith('/metodologia/vigente')) {
      datos = metodologiaFormulario;
    } else if (path.endsWith('/formularios/historial')) {
      datos = [versionFormulario];
    } else if (/\/formularios\/\d+$/.test(path) && method === 'GET') {
      datos = versionFormulario;
    } else if (path.endsWith('/consolidado')) {
      datos = consolidadoTipado;
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
        }],
        pagina: 1,
        registrosPorPagina: 10,
        totalRegistros: 1,
        totalPaginas: 1
      };
    } else if (/\/evaluaciones\/\d+$/.test(path) && method === 'GET') {
      datos = evaluaciones[0];
    } else if (/\/metodologia\/version\/\d+$/.test(path) && method === 'GET') {
      datos = metodologiaFormulario;
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
  await expect(page.getByLabel('Contraseña')).toBeVisible();
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
  const password = page.getByLabel('Contraseña');

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
  await expect(page.getByRole('heading', { name: 'Matrices de Riesgos' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Evaluaciones de Riesgo' })).toBeVisible();
  await expect(page.getByText('BORRADOR', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Nueva evaluación' })).toBeVisible();
});

test('crea una evaluación desde el modal y muestra el consolidado tipado', async ({ page }) => {
  await stubAuthenticatedMatrices(page);
  await page.goto('/matrices-riesgos');

  await page.getByRole('button', { name: 'Nueva evaluación' }).click();
  await expect(page.getByRole('heading', { name: 'Nueva Evaluación de Riesgo' })).toBeVisible();
  await expect(page.getByText('Identificación del riesgo', { exact: true })).toBeVisible();

  const guardar = page.getByRole('button', { name: 'Crear Evaluación' });
  await expect(guardar).toBeDisabled();

  await page.locator('#modal-selector-riesgo').selectOption({ label: 'R-502 — Riesgo tecnológico' });
  await page.getByLabel('Área principal').fill('Tecnología');
  await page.getByLabel('Dueño del riesgo').fill('Gerencia de Tecnología');
  await expect(guardar).toBeEnabled();

  const solicitudCreacion = page.waitForRequest(request =>
    request.method() === 'POST'
      && new URL(request.url()).pathname.endsWith('/api/matrices-riesgos/evaluaciones')
  );
  await guardar.click();
  const request = await solicitudCreacion;
  const payload = request.postDataJSON();
  expect(payload.evaRiesgoId).toBe(502);
  expect(payload.evaVersionId).toBe(10);
  expect(JSON.parse(payload.evaDataJson)).toEqual({
    area_principal: 'Tecnología',
    dueno_riesgo: 'Gerencia de Tecnología',
  });

  await page.getByRole('tab', { name: 'Consolidado' }).click();
  await expect(page.getByRole('heading', { name: 'Matriz consolidada' })).toBeVisible();
  await expect(page.getByText('R-501', { exact: true })).toBeVisible();
  await expect(page.getByText('Cumplimiento', { exact: true })).toBeVisible();
  await expect(page.getByText('MODERADO', { exact: true })).toBeVisible();

  await page.screenshot({ path: 'test-results/evaluacion-modal-consolidado.png', fullPage: true });
});

test('consulta una evaluación existente y permite abrir su edición', async ({ page }) => {
  await stubAuthenticatedMatrices(page);
  await page.goto('/matrices-riesgos');

  await page.getByTitle('Editar evaluación').first().click();
  await expect(page.getByRole('heading', { name: 'Editar Evaluación' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Guardar cambios' })).toBeVisible();
});
