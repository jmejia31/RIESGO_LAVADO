const path = require('path');
const fs = require('fs');
const http = require('http');
const { chromium } = require('playwright');

const outDir = path.resolve('docs/5. Documentacion Modular/capturas');
const staticDir = path.resolve('frontend/rl-app/dist/rl-app/browser');
const baseUrl = 'http://127.0.0.1:4300';

function b64url(obj) {
  return Buffer.from(JSON.stringify(obj)).toString('base64url');
}

const payload = {
  nameid: '1',
  uid: 'captura-local',
  email: 'capturas@ihss.hn',
  given_name: 'Usuario',
  family_name: 'Documentacion',
  role: 'ADMINISTRADOR',
  rol_id: '1',
  dominio: '',
  dom_id: '',
  usr_dom: '',
  es_dom: '0',
  modulos: '2,3,4,5,6,7,8,9',
  debe_cambiar_pass: '0',
  exp: Math.floor(Date.now() / 1000) + 3600,
};

const token = `${b64url({ alg: 'none', typ: 'JWT' })}.${b64url(payload)}.`;

const modules = [
  { route: '/login', file: '00_login.png' },
  { route: '/usuarios', file: '01_usuarios.png' },
  { route: '/configuracion', file: '02_configuracion.png' },
  { route: '/monitoreo-listas', file: '03_monitoreo_listas.png' },
  { route: '/bitacora', file: '04_bitacora.png' },
  { route: '/tipo-listas', file: '05_tipo_listas.png' },
  { route: '/cargar-listas', file: '06_cargar_listas.png' },
  { route: '/coincidencias-patrono', file: '07_coincidencias_patrono.png' },
  { route: '/coincidencias-empleado', file: '08_coincidencias_empleado.png' },
  { route: '/sin-acceso', file: '09_sin_acceso.png' },
];

const moduleCatalog = [
  { modId: 2, modNombre: 'Usuarios', modDescripcion: 'Gestion de usuarios', modRuta: '/usuarios', modIcono: 'users', modSeccion: 'Seguridad y Accesos' },
  { modId: 3, modNombre: 'Configuracion', modDescripcion: 'Configuracion del sistema', modRuta: '/configuracion', modIcono: 'settings', modSeccion: 'Configuracion del Sistema' },
  { modId: 4, modNombre: 'Monitoreo de Listas', modDescripcion: 'Monitoreo y seguimiento', modRuta: '/monitoreo-listas', modIcono: 'shield', modSeccion: 'Monitoreo y Operacion' },
  { modId: 5, modNombre: 'Bitacora', modDescripcion: 'Auditoria', modRuta: '/bitacora', modIcono: 'list', modSeccion: 'Seguridad y Accesos' },
  { modId: 6, modNombre: 'Tipo Listas', modDescripcion: 'Tipos de listas', modRuta: '/tipo-listas', modIcono: 'list', modSeccion: 'Listas de Cautela' },
  { modId: 7, modNombre: 'Cargar Listas', modDescripcion: 'Carga de listas', modRuta: '/cargar-listas', modIcono: 'upload', modSeccion: 'Listas de Cautela' },
  { modId: 8, modNombre: 'Coincidencias Patrono', modDescripcion: 'Coincidencias patrono', modRuta: '/coincidencias-patrono', modIcono: 'users', modSeccion: 'Listas de Cautela' },
  { modId: 9, modNombre: 'Coincidencias Empleado', modDescripcion: 'Coincidencias empleado', modRuta: '/coincidencias-empleado', modIcono: 'users', modSeccion: 'Listas de Cautela' },
];

function json(data) {
  return { status: 200, contentType: 'application/json', body: JSON.stringify(data) };
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });
  const server = await startStaticServer();
  const browser = await chromium.launch({
    headless: true,
    executablePath: 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1100 }, deviceScaleFactor: 1 });

  await page.route('**/api/configuracion/sistema', route => route.fulfill(json({
    success: true,
    datos: {
      nombreInstitucion: 'Instituto Hondureno de Seguridad Social',
      nombreSistema: 'SGRLA-IHSS',
      colorPrimario: '#1e3a8a',
      colorSecundario: '#1d4ed8',
      timeoutSesion: 30,
      maxIntentos: 5,
    }
  })));
  await page.route('**/api/configuracion/login', route => route.fulfill(json({ success: true, datos: {} })));
  await page.route('**/api/configuracion/slides', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/catalogos/modulos', route => route.fulfill(json({ success: true, datos: moduleCatalog })));
  await page.route('**/api/catalogos/roles', route => route.fulfill(json({ success: true, datos: [{ rolId: 1, rolNombre: 'ADMINISTRADOR', rolActivo: 1 }] })));
  await page.route('**/api/catalogos/dominios', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/auth/usuarios', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/listas/evidencias/politica', route => route.fulfill(json({ success: true, datos: { maximoMb: 10, extensionesPermitidas: ['pdf','docx','xlsx','jpg','png'], tiposPermitidosTexto: 'PDF, Word, Excel e imagenes' } })));
  await page.route('**/api/listas/juridicas', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/listas/naturales', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/listas/empleados', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/listas/tipos-documento', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/listas/tipos-listas-cautela', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/listas/resumen', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/listas/coincidencias-patrono/resumen', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/listas/coincidencias-empleado/resumen', route => route.fulfill(json({ success: true, datos: [] })));
  await page.route('**/api/auditoria**', route => route.fulfill(json({ datos: [], totalRegistros: 0 })));

  for (const item of modules) {
    await page.goto(baseUrl + item.route, { waitUntil: 'networkidle' });
    if (item.route !== '/login') {
      await page.evaluate(tokenValue => {
        localStorage.setItem('access_token', tokenValue);
        localStorage.setItem('refresh_token', 'captura-refresh');
        localStorage.setItem('token_expira', new Date(Date.now() + 3600000).toISOString());
      }, token);
      await page.goto(baseUrl + item.route, { waitUntil: 'networkidle' });
    }
    await page.evaluate(() => {
      document.querySelectorAll('link[rel="stylesheet"]').forEach(link => link.setAttribute('media', 'all'));
    });
    await page.waitForTimeout(1200);
    await page.screenshot({ path: path.join(outDir, item.file), fullPage: false });
    console.log(path.join(outDir, item.file));
  }

  await browser.close();
  await new Promise(resolve => server.close(resolve));
}

function contentType(file) {
  if (file.endsWith('.html')) return 'text/html; charset=utf-8';
  if (file.endsWith('.js')) return 'text/javascript; charset=utf-8';
  if (file.endsWith('.css')) return 'text/css; charset=utf-8';
  if (file.endsWith('.png')) return 'image/png';
  if (file.endsWith('.ico')) return 'image/x-icon';
  return 'application/octet-stream';
}

function startStaticServer() {
  const server = http.createServer((req, res) => {
    const urlPath = decodeURIComponent((req.url || '/').split('?')[0]);
    const relative = urlPath === '/' ? 'index.html' : urlPath.replace(/^\/+/, '');
    let target = path.join(staticDir, relative);
    if (!target.startsWith(staticDir) || !fs.existsSync(target) || fs.statSync(target).isDirectory()) {
      target = path.join(staticDir, 'index.html');
    }
    res.writeHead(200, { 'Content-Type': contentType(target) });
    fs.createReadStream(target).pipe(res);
  });
  return new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(4300, '127.0.0.1', () => resolve(server));
  });
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
