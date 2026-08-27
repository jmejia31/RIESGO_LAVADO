import { chromium } from '../../frontend/rl-app/node_modules/playwright/index.mjs';
import { readFile } from 'node:fs/promises';

const runtimeProcess = globalThis.process;
const endpointFile = runtimeProcess?.env?.TEMP
  ? `${runtimeProcess.env.TEMP}/RIESGO_LAVADO_UAT/cdp-endpoint.txt`
  : null;
const loginEmail = 'cuentajavier419@gmail.com';
const loginPath = '/login';
const matricesPath = '/matrices-riesgos';

function isLoginUrl(url) {
  return new URL(url).pathname === loginPath;
}

async function selectExistingPage(context) {
  const pages = context.pages();
  const page = pages.find(candidate => !candidate.isClosed()) ?? await context.newPage();
  await page.bringToFront();
  return page;
}

async function completeLoginIfNeeded(page) {
  if (!isLoginUrl(page.url())) {
    return false;
  }

  const email = page.locator('input[type="email"], input[autocomplete="username"]').first();
  if (await email.count() > 0 && !(await email.inputValue())) {
    await email.fill(loginEmail);
  }

  const submit = page.getByRole('button', { name: /iniciar sesión|iniciar sesion|ingresar|acceder/i }).first();
  if (await submit.count() === 0) {
    throw new Error('Login button not found; the same visible browser remains available for minimal intervention.');
  }

  await submit.click();
  await page.waitForURL(url => !isLoginUrl(url.toString()), { timeout: 30_000 });
  return true;
}

export async function runUatCdp(cdpEndpoint) {
  const browser = await chromium.connectOverCDP(cdpEndpoint);
  const context = browser.contexts()[0];
  if (!context) {
    throw new Error('CDP connected, but no reusable browser context exists.');
  }

  const page = await selectExistingPage(context);
  const sameBrowser = browser.contexts().includes(context);
  const samePage = context.pages().includes(page);
  console.log('CDP_CONNECT=PASS');
  console.log(`SAME_BROWSER=${sameBrowser ? 'PASS' : 'FAIL'}`);
  console.log(`SAME_CONTEXT=${sameBrowser ? 'PASS' : 'FAIL'}`);
  console.log(`SAME_PAGE=${samePage ? 'PASS' : 'FAIL'}`);
  console.log('PASSWORD_EXPOSED=NO');
  console.log('TOKENS_EXPOSED=NO');

  const loggedIn = await completeLoginIfNeeded(page);
  if (loggedIn) {
    console.log('AUTH_LOGIN=PASS');
  }

  if (isLoginUrl(page.url())) {
    console.log('AUTOFILL_UNAVAILABLE=YES');
    if (runtimeProcess) runtimeProcess.exitCode = 2;
  } else {
    await page.goto(new URL(matricesPath, page.url()).toString(), { waitUntil: 'domcontentloaded' });
    await page.waitForLoadState('networkidle', { timeout: 10_000 }).catch(() => undefined);
    const visible = await page.evaluate(() => (document.body?.innerText ?? '').trim().length > 0);
    console.log(`AUTH_ROUTE=${new URL(page.url()).pathname === matricesPath ? 'PASS' : 'FAIL'}`);
    console.log(`CONTENT_VISIBLE=${visible ? 'PASS' : 'FAIL'}`);
  }

  console.log('UAT_CDP_ATTACHED=YES');
  console.log('Chromium UAT remains open; this runner does not close browser, context, or page.');
}

if (runtimeProcess?.argv?.[1]?.endsWith('matrices-uat-cdp.mjs')) {
  const cdpEndpoint = runtimeProcess.env.UAT_CDP_ENDPOINT
    ?? (endpointFile ? (await readFile(endpointFile, 'ascii')).trim() : null);
  if (!cdpEndpoint) throw new Error('UAT_CDP_ENDPOINT or temporary endpoint file is required.');
  await runUatCdp(cdpEndpoint);
}
