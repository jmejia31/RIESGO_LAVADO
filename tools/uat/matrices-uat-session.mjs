import { chromium } from '../../frontend/rl-app/node_modules/playwright/index.mjs';
import { mkdir } from 'node:fs/promises';

const FRONTEND_URL = process.env.UAT_FRONTEND_URL ?? 'http://localhost:4200';
const MATRICES_URL = `${FRONTEND_URL}/matrices-riesgos`;
const CONFIGURED_PROFILE_DIR = process.env.UAT_PROFILE_DIR;
const PREFERRED_PROFILE_DIR = `${process.env.LOCALAPPDATA ?? process.env.TEMP}/RIESGO_LAVADO_UAT/playwright-profile`;
const FALLBACK_PROFILE_DIR = `${process.env.TEMP ?? process.env.LOCALAPPDATA}/RIESGO_LAVADO_UAT/playwright-profile`;
const LOGIN_PATH = '/login';

function isAuthenticatedUrl(url) {
  return new URL(url).pathname !== LOGIN_PATH;
}

async function checkReachability(url, label) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`${label} no disponible: HTTP ${response.status}`);
  }
  console.log(`${label}: PASS`);
}

async function resolveProfileDir() {
  if (CONFIGURED_PROFILE_DIR) {
    await mkdir(CONFIGURED_PROFILE_DIR, { recursive: true });
    return CONFIGURED_PROFILE_DIR;
  }

  try {
    await mkdir(PREFERRED_PROFILE_DIR, { recursive: true });
    return PREFERRED_PROFILE_DIR;
  } catch {
    await mkdir(FALLBACK_PROFILE_DIR, { recursive: true });
    return FALLBACK_PROFILE_DIR;
  }
}

await checkReachability(`${FRONTEND_URL}/`, 'Frontend');
await checkReachability(process.env.UAT_BACKEND_URL ?? 'http://localhost:5043/swagger/index.html', 'Backend/Swagger');

const profileDir = await resolveProfileDir();
console.log(`Perfil UAT externo: ${profileDir}`);
const context = await chromium.launchPersistentContext(profileDir, {
  headless: false,
  viewport: { width: 1536, height: 1024 },
});

const page = context.pages()[0] ?? await context.newPage();
const httpFailures = [];
const consoleErrors = [];
const pageErrors = [];
page.on('response', response => {
  const status = response.status();
  if (status >= 400) httpFailures.push({ status, path: new URL(response.url()).pathname });
});
page.on('requestfailed', request => {
  httpFailures.push({ status: 'FAILED', path: new URL(request.url()).pathname });
});
page.on('console', message => {
  if (message.type() === 'error') consoleErrors.push(message.text());
});
page.on('pageerror', error => pageErrors.push(error.message));
await page.goto(MATRICES_URL, { waitUntil: 'domcontentloaded' });

if (!isAuthenticatedUrl(page.url())) {
  console.log('LOGIN MANUAL REQUERIDO');
  console.log('Use la cuenta UAT autorizada y complete el login en Chromium.');
  await page.waitForURL(url => isAuthenticatedUrl(url.toString()), { timeout: 0 });
}

await page.goto(MATRICES_URL, { waitUntil: 'domcontentloaded' });
if (!isAuthenticatedUrl(page.url())) {
  throw new Error('La sesión no quedó autenticada.');
}

console.log('Sesión UAT disponible: PASS');
console.log('Perfil persistente activo; los valores de sesión no se imprimen.');
console.log('Navegador UAT mantenido abierto para continuar las pruebas.');

await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => undefined);
const view = await page.evaluate(() => ({
  path: location.pathname,
  title: document.title,
  bodyTextLength: document.body?.innerText?.trim().length ?? 0,
  visibleHeadings: Array.from(document.querySelectorAll('h1,h2,h3'))
    .filter(element => element.getBoundingClientRect().width > 0)
    .map(element => element.textContent?.trim())
    .filter(Boolean)
    .slice(0, 12),
}));
console.log(`Ruta autenticada: ${view.path}`);
console.log(`Contenido visible: ${view.bodyTextLength > 0 ? 'PASS' : 'FAIL'}`);
console.log(`HTTP errores observados: ${httpFailures.length}`);
for (const failure of httpFailures) console.log(`HTTP ${failure.status} ${failure.path}`);
console.log(`Console errors: ${consoleErrors.length}`);
console.log(`Page errors: ${pageErrors.length}`);
await new Promise(() => {});
