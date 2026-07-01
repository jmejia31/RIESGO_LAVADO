const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const baseUrl = process.env.RL_API_URL || 'http://localhost:5043/api';
const outDir = path.resolve('docs/8. Pruebas Mínimas');
const outFile = path.join(outDir, 'resultado_pruebas_minimas_readonly.json');

function base64url(input) {
  return Buffer.from(input)
    .toString('base64')
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
}

function createJwt() {
  const configPath = path.resolve('backend/RL.API/appsettings.json');
  const config = JSON.parse(fs.readFileSync(configPath, 'utf8'));
  const secret = config.Jwt.SecretKey;
  const now = Math.floor(Date.now() / 1000);
  const header = { alg: 'HS256', typ: 'JWT' };
  const payload = {
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '1',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Prueba Controlada',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'prueba.codex@ihss.hn',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'ADMINISTRADOR',
    rol_id: '1',
    uid: 'test-admin',
    modulos: '2,3,4,5,6,7,8,9',
    debe_cambiar_pass: '0',
    iss: config.Jwt.Issuer,
    aud: config.Jwt.Audience,
    iat: now,
    nbf: now,
    exp: now + 3600,
  };
  const unsigned = `${base64url(JSON.stringify(header))}.${base64url(JSON.stringify(payload))}`;
  const signature = crypto.createHmac('sha256', secret).update(unsigned).digest('base64')
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
  return `${unsigned}.${signature}`;
}

async function request(name, method, url, token = null) {
  const started = Date.now();
  const headers = { Accept: 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  try {
    const response = await fetch(url, { method, headers });
    const text = await response.text();
    let parsed = null;
    try {
      parsed = text ? JSON.parse(text) : null;
    } catch {
      parsed = text.slice(0, 300);
    }
    return {
      name,
      method,
      url: url.replace(baseUrl, '/api'),
      status: response.status,
      ok: response.ok,
      elapsedMs: Date.now() - started,
      summary: summarize(parsed),
    };
  } catch (error) {
    return {
      name,
      method,
      url: url.replace(baseUrl, '/api'),
      status: null,
      ok: false,
      elapsedMs: Date.now() - started,
      error: error.message,
    };
  }
}

function summarize(data) {
  if (data == null) return null;
  if (typeof data === 'string') return data.slice(0, 240);
  if (Array.isArray(data)) return { type: 'array', count: data.length };
  const summary = {};
  for (const [key, value] of Object.entries(data)) {
    if (Array.isArray(value)) summary[key] = { type: 'array', count: value.length };
    else if (value && typeof value === 'object') summary[key] = Object.keys(value).slice(0, 10);
    else summary[key] = value;
  }
  return summary;
}

async function main() {
  const token = createJwt();
  const checks = [
    ['Swagger disponible', 'GET', 'http://localhost:5043/swagger/v1/swagger.json', null],
    ['Configuración del sistema', 'GET', `${baseUrl}/configuracion/sistema`, null],
    ['Slides de login', 'GET', `${baseUrl}/configuracion/login`, null],
    ['Perfil con token de prueba', 'GET', `${baseUrl}/auth/perfil`, token],
    ['Acceso por módulo - usuarios', 'GET', `${baseUrl}/auth/usuarios`, token],
    ['Consultar monitoreo - jurídicas', 'GET', `${baseUrl}/listas/juridicas`, token],
    ['Consultar monitoreo - naturales', 'GET', `${baseUrl}/listas/naturales`, token],
    ['Consultar monitoreo - empleados', 'GET', `${baseUrl}/listas/empleados`, token],
    ['Tipos de documento', 'GET', `${baseUrl}/listas/tipos-documento`, token],
    ['Política de evidencias', 'GET', `${baseUrl}/listas/evidencias/politica`, token],
    ['Tipos de listas', 'GET', `${baseUrl}/listas/tipos-listas-cautela`, token],
    ['Resumen de listas', 'GET', `${baseUrl}/listas/resumen`, token],
    ['Bitácora', 'GET', `${baseUrl}/auditoria?pagina=1&limite=5`, token],
    ['Coincidencias patrono resumen', 'GET', `${baseUrl}/listas/coincidencias-patrono/resumen`, token],
    ['Coincidencias empleado resumen', 'GET', `${baseUrl}/listas/coincidencias-empleado/resumen`, token],
  ];

  const results = [];
  for (const [name, method, url, auth] of checks) {
    results.push(await request(name, method, url, auth));
  }

  fs.mkdirSync(outDir, { recursive: true });
  const output = {
    generatedAt: new Date().toISOString(),
    baseUrl,
    mode: 'readonly',
    note: 'No se ejecutaron operaciones POST/PUT/DELETE para evitar alterar datos existentes.',
    results,
  };
  fs.writeFileSync(outFile, JSON.stringify(output, null, 2), 'utf8');
  console.log(outFile);
  for (const result of results) {
    console.log(`${result.ok ? 'OK' : 'FAIL'} | ${result.status ?? 'ERR'} | ${result.name} | ${result.url}`);
  }
  if (results.some(r => !r.ok)) process.exitCode = 1;
}

main();
