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

const metodologiaMatrices = {
  version: '2026.1',
  factores: [],
  variables: [],
  escalasCatalogo: [],
  escalasRiesgo: [
    { escalaId: 1, tipo: 'RESIDUAL', nivel: 'Muy Bajo', color: '#16a34a', valorMinimo: 1, valorMaximo: 1.99, requierePlanAccion: false },
    { escalaId: 2, tipo: 'RESIDUAL', nivel: 'Bajo', color: '#65a30d', valorMinimo: 2, valorMaximo: 2.99, requierePlanAccion: false },
    { escalaId: 3, tipo: 'RESIDUAL', nivel: 'Medio', color: '#eab308', valorMinimo: 3, valorMaximo: 3.99, requierePlanAccion: false },
    { escalaId: 4, tipo: 'RESIDUAL', nivel: 'Alto', color: '#f97316', valorMinimo: 4, valorMaximo: 4.99, requierePlanAccion: true },
    { escalaId: 5, tipo: 'RESIDUAL', nivel: 'Crítico', color: '#dc2626', valorMinimo: 5, valorMaximo: 5, requierePlanAccion: true },
  ],
  controlesMitigacion: [],
};

const dashboardEjecutivo = {
  fechaGeneracion: '2026-07-21T16:30:00Z',
  filtro: {},
  totalMatrices: 12,
  totalCalculadas: 11,
  totalCerradas: 4,
  totalConPlanAccion: 5,
  totalAltoCritico: 4,
  totalPlanesVencidos: 2,
  porEstado: [{ nombre: 'EN_REVISION', total: 5 }, { nombre: 'CERRADA', total: 4 }],
  porNivelInherente: [{ nombre: 'Alto', total: 5 }, { nombre: 'Medio', total: 7 }],
  porNivelResidual: [{ nombre: 'Medio', total: 8 }, { nombre: 'Alto', total: 4 }],
  mapaTransicion: [
    { nivelInherente: 'Alto', nivelResidual: 'Medio', total: 3, promedioInherente: 4.5, promedioResidual: 3.2 },
    { nivelInherente: 'Alto', nivelResidual: 'Alto', total: 2, promedioInherente: 4.7, promedioResidual: 4.2 },
    { nivelInherente: 'Medio', nivelResidual: 'Medio', total: 7, promedioInherente: 3.4, promedioResidual: 3.1 },
  ],
  matricesCriticas: [
    {
      matrizId: 88,
      modeloId: 1,
      modeloVersion: '2026.1',
      sujetoTipo: 'PROVEEDOR',
      nombreSujeto: 'Proveedor crítico E2E',
      estado: 'APROBADA',
      fechaEvaluacion: '2026-07-20T10:00:00Z',
      puntajeInherente: 4.8,
      nivelInherente: 'Alto',
      puntajeResidual: 4.2,
      nivelResidual: 'Alto',
      requierePlanAccion: true,
    },
  ],
  matricesFiltradas: [],
  planesAccion: [{ estado: 'PENDIENTE', total: 4, vencidos: 2 }],
};

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
    const path = new URL(route.request().url()).pathname;
    let datos: unknown = [];
    if (path.endsWith('/metodologia/vigente')) {
      datos = metodologiaMatrices;
    } else if (path.endsWith('/dashboard')) {
      const url = new URL(route.request().url());
      const nivelInherente = url.searchParams.get('nivelInherente');
      const nivelResidual = url.searchParams.get('nivelResidual');
      const coincideCuadrante = nivelInherente === 'Alto' && nivelResidual === 'Medio';
      datos = nivelInherente || nivelResidual
        ? {
            ...dashboardEjecutivo,
            filtro: { nivelInherente, nivelResidual },
            matricesFiltradas: coincideCuadrante ? [{
              matrizId: 91,
              modeloId: 1,
              modeloVersion: '2026.1',
              sujetoTipo: 'PROVEEDOR',
              documento: '0801-E2E',
              nombreSujeto: 'Proveedor cuadrante E2E',
              estado: 'APROBADA',
              fechaEvaluacion: '2026-07-21T10:00:00Z',
              puntajeInherente: 4.5,
              nivelInherente: 'Alto',
              puntajeResidual: 3.2,
              nivelResidual: 'Medio',
              requierePlanAccion: false,
            }] : [],
          }
        : dashboardEjecutivo;
    } else if (path.endsWith('/reportes')) {
      datos = {
        totales: { totalMatrices: 12 },
        porEstado: dashboardEjecutivo.porEstado,
        porNivelInherente: dashboardEjecutivo.porNivelInherente,
        porNivelResidual: dashboardEjecutivo.porNivelResidual,
        porFactor: [],
        matricesCriticas: dashboardEjecutivo.matricesCriticas,
      };
    }
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ success: true, datos }) });
  });

  await page.route('**/api/matrices-riesgos*', route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({ success: true, datos: [] }),
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

test('abre Matrices de Riesgos con una sesion autenticada y autorizada', async ({ page }) => {
  await stubAuthenticatedMatrices(page);

  await page.goto('/matrices-riesgos');

  await expect(page).toHaveURL(/\/matrices-riesgos$/);
  await expect(page.getByRole('heading', { name: 'Matrices de Riesgos' })).toBeVisible();
  await page.getByRole('button', { name: 'Matrices', exact: true }).click();
  await expect(page.getByText('No hay matrices registradas.')).toBeVisible();
});

test('muestra el dashboard ejecutivo y filtra al seleccionar una celda real del mapa', async ({ page }) => {
  await stubAuthenticatedMatrices(page);

  await page.goto('/matrices-riesgos');

  await expect(page.getByRole('heading', { name: 'Panel ejecutivo de Matrices' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Mapa de transición de riesgo' })).toBeVisible();
  await expect(page.getByText('12', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Proveedor crítico E2E')).toBeVisible();
  await expect(page.getByTitle('Inherente Alto / Residual Medio: 3 matrices')).toBeVisible();

  const filteredRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname.endsWith('/api/matrices-riesgos/dashboard')
      && url.searchParams.get('nivelInherente') === 'Alto'
      && url.searchParams.get('nivelResidual') === 'Medio';
  });

  await page.getByTitle('Inherente Alto / Residual Medio: 3 matrices').click();
  await filteredRequest;
  await expect(page.getByRole('heading', { name: 'Matrices del cuadrante seleccionado' })).toBeVisible();
  await expect(page.getByText('Proveedor cuadrante E2E')).toBeVisible();
  await expect(page.getByText('Inherente Alto / Residual Medio')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Limpiar selección del mapa' }).first()).toBeVisible();

  const emptyRequest = page.waitForRequest(request => {
    const url = new URL(request.url());
    return url.pathname.endsWith('/api/matrices-riesgos/dashboard')
      && url.searchParams.get('nivelInherente') === 'Bajo'
      && url.searchParams.get('nivelResidual') === 'Crítico';
  });
  await page.getByTitle('Inherente Bajo / Residual Crítico: 0 matrices').click();
  await emptyRequest;
  await expect(page.getByText('No existen matrices en esta combinación de riesgo inherente y residual.')).toBeVisible();

  await page.screenshot({ path: 'test-results/fase12-mapa-cuadrante-detalle.png', fullPage: true });
});
