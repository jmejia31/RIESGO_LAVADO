import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { createHmac } from 'node:crypto';
import { performance } from 'node:perf_hooks';

const apiBase = (process.env.RL_API_URL ?? 'http://localhost:5043/api').replace(/\/$/, '');
const runs = positiveInteger(process.env.RL_PERF_RUNS, 10);
const warmups = positiveInteger(process.env.RL_PERF_WARMUPS, 2);
const timeoutMs = positiveInteger(process.env.RL_PERF_TIMEOUT_MS, 10_000);
const oracleReal = process.env.RL_PERF_ORACLE_REAL === 'true';
const outputJson = resolve(process.env.RL_PERF_OUTPUT_JSON ?? defaultJsonPath());
const outputMarkdown = resolve(process.env.RL_PERF_OUTPUT_MD ?? outputJson.replace(/\.json$/i, '.md'));

const types = [
  { key: 'juridicas', path: 'listas/juridicas/paginado' },
  { key: 'naturales', path: 'listas/naturales/paginado' },
  { key: 'empleados', path: 'listas/empleados/paginado' },
];
const selectedTypes = (process.env.RL_PERF_TYPES ?? '')
  .split(',').map(value => value.trim().toLowerCase()).filter(Boolean);
const benchmarkTypes = selectedTypes.length
  ? types.filter(type => selectedTypes.includes(type.key))
  : types;
const includeConcurrency = process.env.RL_PERF_SKIP_CONCURRENCY !== 'true';

function positiveInteger(value, fallback) {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}

function defaultJsonPath() {
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  return `artifacts/performance/monitoreo-api-performance-${timestamp}.json`;
}

function base64url(input) {
  return Buffer.from(input).toString('base64')
    .replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
}

function createJwt() {
  const configPath = resolve('backend/RL.API/appsettings.json');
  const config = JSON.parse(readFileSync(configPath, 'utf8'));
  const secret = config?.Jwt?.SecretKey;
  if (!secret || /^CHANGE_ME|YOUR_/i.test(secret)) {
    throw new Error('No existe una clave JWT local válida para el benchmark.');
  }
  const now = Math.floor(Date.now() / 1000);
  const header = { alg: 'HS256', typ: 'JWT' };
  const payload = {
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': '1',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name': 'Prueba Controlada',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'prueba.codex@ihss.hn',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': 'ADMINISTRADOR',
    rol_id: '1', uid: 'test-admin', modulos: '2,3,4,5,6,7,8,9',
    debe_cambiar_pass: '0', iss: config.Jwt.Issuer, aud: config.Jwt.Audience,
    iat: now, nbf: now, exp: now + 3600,
  };
  const unsigned = `${base64url(JSON.stringify(header))}.${base64url(JSON.stringify(payload))}`;
  const signature = createHmac('sha256', secret).update(unsigned).digest('base64')
    .replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  return `${unsigned}.${signature}`;
}

async function requestJson(token, url) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  const started = performance.now();
  try {
    const response = await fetch(url, {
      method: 'GET',
      headers: { Accept: 'application/json', Authorization: `Bearer ${token}` },
      signal: controller.signal,
    });
    const body = await response.text();
    const elapsedMs = performance.now() - started;
    let parsed;
    try {
      parsed = JSON.parse(body);
    } catch {
      throw new Error(`HTTP ${response.status}: respuesta no JSON`);
    }
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    return { elapsedMs, response: validateResponse(parsed) };
  } catch (error) {
    const elapsedMs = performance.now() - started;
    const message = error?.name === 'AbortError' ? `timeout ${timeoutMs}ms` : error.message;
    throw new Error(`${message} (${elapsedMs.toFixed(2)}ms)`);
  } finally {
    clearTimeout(timeout);
  }
}

function validateResponse(payload) {
  const data = payload?.datos ?? payload?.data;
  const items = data?.items ?? data?.registros;
  if (!Array.isArray(items)) throw new Error('Contrato inválido: items/registros ausente');
  const page = Number(data?.pagina);
  const pageSize = Number(data?.tamanoPagina ?? data?.registrosPorPagina);
  const total = Number(data?.totalRegistros);
  if (!Number.isInteger(page) || !Number.isInteger(pageSize) || !Number.isInteger(total)) {
    throw new Error('Contrato inválido: metadata de paginación ausente');
  }
  return { itemsLength: items.length, page, pageSize, totalRecords: total };
}

function urlFor(type, page, pageSize, extra = {}) {
  const params = new URLSearchParams({ pagina: String(page), tamanoPagina: String(pageSize) });
  for (const [key, value] of Object.entries(extra)) {
    if (value !== undefined && value !== '') params.set(key, value);
  }
  return `${apiBase}/${type.path}?${params.toString()}`;
}

async function sample(token, type, page, pageSize, extra = {}) {
  const result = await requestJson(token, urlFor(type, page, pageSize, extra));
  if (result.response.page !== page || result.response.pageSize !== pageSize) {
    throw new Error(`Contrato inválido: página esperada ${page}/${pageSize}, recibida ${result.response.page}/${result.response.pageSize}`);
  }
  if (result.response.itemsLength > pageSize) throw new Error('La API materializó más filas que el tamaño solicitado');
  return result;
}

function stats(samples) {
  if (!samples.length) return { minMs: null, maxMs: null, meanMs: null, medianMs: null, p90Ms: null, p95Ms: null };
  const values = samples.map(sample => sample.elapsedMs).sort((a, b) => a - b);
  const percentile = rank => values[Math.min(values.length - 1, Math.ceil(rank * values.length) - 1)];
  return {
    minMs: round(values[0]), maxMs: round(values.at(-1)), meanMs: round(values.reduce((sum, value) => sum + value, 0) / values.length),
    medianMs: round(percentile(0.5)), p90Ms: round(percentile(0.9)), p95Ms: round(percentile(0.95)),
  };
}

function round(value) { return Math.round(value * 100) / 100; }

function volumeClass(total) {
  if (total === 0) return 'EMPTY';
  if (total <= 100) return 'SMALL';
  if (total <= 10_000) return 'MEDIUM';
  return 'LARGE';
}

async function measureType(token, type) {
  const failures = [];
  const cold = await capture(failures, 'cold', () => sample(token, type, 1, 10));
  const warmSamples = await collect(failures, 'warm', warmups, runs, () => sample(token, type, 1, 10));
  const totalRecords = cold?.response?.totalRecords ?? warmSamples[0]?.response?.totalRecords ?? 0;
  const page2Samples = totalRecords > 10
    ? await collect(failures, 'page2', warmups, runs, () => sample(token, type, 2, 10))
    : [];
  const page25Samples = totalRecords > 25
    ? await collect(failures, 'pageSize25', warmups, runs, () => sample(token, type, 1, 25))
    : [];
  const filteredSamples = await collect(failures, 'filtered', 0, runs, () => sample(token, type, 1, 10, { estado: 'pendiente' }));
  const concurrencySamples = includeConcurrency ? await concurrent(failures, token, type) : [];
  const warmStats = stats(warmSamples);
  const page2Stats = page2Samples.length ? stats(page2Samples) : null;
  const page25Stats = page25Samples.length ? stats(page25Samples) : null;
  const filteredStats = filteredSamples.length ? stats(filteredSamples) : null;
  const concurrencyStats = concurrencySamples.length ? stats(concurrencySamples) : null;
  const pass = Boolean(cold && warmSamples.length === runs && failures.length === 0 && oracleReal
    && cold.elapsedMs <= 5000 && warmStats.medianMs <= 3000 && warmStats.p95Ms <= 5000
    && (!page2Stats || page2Stats.medianMs <= 5000)
    && (!page25Stats || page25Stats.medianMs <= 5000));
  return {
    coldMs: cold ? round(cold.elapsedMs) : null,
    warmSamplesMs: warmSamples.map(sample => round(sample.elapsedMs)),
    ...warmStats,
    totalRecords,
    datasetVolumeClass: volumeClass(totalRecords),
    page2: page2Stats ? { ...page2Stats, samplesMs: page2Samples.map(sample => round(sample.elapsedMs)) } : { applicable: false },
    pageSize25: page25Stats ? { ...page25Stats, samplesMs: page25Samples.map(sample => round(sample.elapsedMs)) } : { applicable: false },
    filtered: filteredStats ? { ...filteredStats, samplesMs: filteredSamples.map(sample => round(sample.elapsedMs)) } : null,
    concurrency5: concurrencyStats ? { ...concurrencyStats, samplesMs: concurrencySamples.map(sample => round(sample.elapsedMs)) } : null,
    failures,
    result: pass ? 'PASS' : 'FAIL',
  };
}

async function capture(failures, label, operation) {
  try { return await operation(); }
  catch (error) { failures.push(`${label}: ${error.message}`); return null; }
}

async function collect(failures, label, warmupCount, sampleCount, operation) {
  for (let index = 0; index < warmupCount; index += 1) {
    await capture(failures, `${label}-warmup-${index + 1}`, operation);
  }
  const results = [];
  for (let index = 0; index < sampleCount; index += 1) {
    const result = await capture(failures, `${label}-run-${index + 1}`, operation);
    if (result) results.push(result);
  }
  return results;
}

async function concurrent(failures, token, type) {
  const started = performance.now();
  try {
    const results = await Promise.all(Array.from({ length: 5 }, () => sample(token, type, 1, 10)));
    const elapsedMs = performance.now() - started;
    return results.map(() => ({ elapsedMs }));
  } catch (error) {
    failures.push(`concurrency5: ${error.message}`);
    return [];
  }
}

function markdown(report) {
  const rows = Object.entries(report.tests).map(([key, value]) =>
    `| ${key} | ${value.coldMs ?? 'FAIL'} | ${value.medianMs ?? 'FAIL'} | ${value.p90Ms ?? 'FAIL'} | ${value.p95Ms ?? 'FAIL'} | ${value.maxMs ?? 'FAIL'} | ${value.totalRecords} | ${value.result} |`);
  return `# Certificación HTTP de Monitoreo de Listas\n\n` +
    `Modo: **${report.environment.apiMode}**; Oracle real: **${report.environment.oracleReal}**; SHA: \`${report.gitSha}\`.\n\n` +
    `Medición: cuerpo HTTP completo consumido con response.text(); ${report.environment.runs} ejecuciones warm y ${report.environment.warmups} warmups por escenario; timeout ${report.environment.timeoutMs} ms.\n\n` +
    `| Tipo | Cold ms | Median ms | P90 ms | P95 ms | Max ms | Total | Resultado |\n|---|---:|---:|---:|---:|---:|---:|---|\n${rows.join('\n')}\n\n` +
    `Resultado global: **${report.result}**. Failed requests: **${report.failedRequests}**.\n\n` +
    `La clasificación de volumen es descriptiva del dataset conectado y no implica certificación de 100k registros.\n`;
}

async function main() {
  if (!oracleReal) throw new Error('RL_PERF_ORACLE_REAL=true es obligatorio para certificar contra Oracle real.');
  const token = createJwt();
  const started = new Date().toISOString();
  const tests = {};
  for (const type of benchmarkTypes) tests[type.key] = await measureType(token, type);
  const failedRequests = Object.values(tests).reduce((count, test) => count + test.failures.length, 0);
  const result = Object.values(tests).every(test => test.result === 'PASS') ? 'PASS' : 'FAIL';
  const report = {
    generatedAt: started,
    gitSha: safeGitSha(),
    environment: {
      apiBase: apiBase.replace(/:\/\/[^/]+/, '://<host>'),
      apiMode: process.env.RL_PERF_API_MODE ?? 'FULL_STACK_PRODUCTION_MODE_REAL_ORACLE',
      oracleReal,
      runs, warmups, timeoutMs, measurement: 'TIME_TO_FULL_RESPONSE_BODY',
      coldMethod: 'first request per endpoint after API readiness; process startup excluded',
      types: benchmarkTypes.map(type => type.key),
      concurrency: includeConcurrency ? 5 : 0,
      userObservedBaselineMs: 60_000,
    },
    tests,
    failedRequests,
    result,
  };
  mkdirSync(dirname(outputJson), { recursive: true });
  mkdirSync(dirname(outputMarkdown), { recursive: true });
  writeFileSync(outputJson, JSON.stringify(report, null, 2), 'utf8');
  writeFileSync(outputMarkdown, markdown(report), 'utf8');
  console.log(JSON.stringify({ outputJson, outputMarkdown, result, failedRequests }, null, 2));
  if (result !== 'PASS') process.exitCode = 1;
}

function safeGitSha() {
  try { return execFileSync('git', ['rev-parse', 'HEAD'], { encoding: 'utf8' }).trim(); }
  catch { return 'unknown'; }
}

main().catch(error => {
  console.error(`BENCHMARK_FAIL: ${error.message}`);
  process.exitCode = 1;
});
