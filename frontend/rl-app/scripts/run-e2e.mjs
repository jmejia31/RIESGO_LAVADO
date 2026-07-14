import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(scriptDirectory, '..');
const angularCli = resolve(projectRoot, 'node_modules/@angular/cli/bin/ng.js');
const playwrightCli = resolve(projectRoot, 'node_modules/@playwright/test/cli.js');
const serverUrl = 'http://127.0.0.1:4200/login';
const testArguments = process.argv.slice(2);

let stopping = false;
let serverOutput = '';

const server = spawn(
  process.execPath,
  [angularCli, 'serve', '--host', '127.0.0.1', '--port', '4200'],
  {
    cwd: projectRoot,
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  },
);

for (const stream of [server.stdout, server.stderr]) {
  stream.on('data', chunk => {
    serverOutput = `${serverOutput}${chunk.toString()}`.slice(-8_000);
  });
}

async function waitForServer(timeoutMs = 120_000) {
  const startedAt = Date.now();
  while (Date.now() - startedAt < timeoutMs) {
    if (server.exitCode !== null) {
      throw new Error(`Angular finalizo antes de iniciar (codigo ${server.exitCode}).\n${serverOutput}`);
    }

    try {
      const response = await fetch(serverUrl);
      if (response.status < 500) {
        return;
      }
    }
    catch {
      // El servidor aun se esta compilando.
    }

    await new Promise(resolveWait => setTimeout(resolveWait, 500));
  }

  throw new Error(`Angular no respondio en ${serverUrl} dentro del tiempo esperado.\n${serverOutput}`);
}

async function stopServer() {
  if (stopping || server.exitCode !== null) {
    return;
  }

  stopping = true;
  server.kill();
  await Promise.race([
    new Promise(resolveExit => server.once('exit', resolveExit)),
    new Promise(resolveWait => setTimeout(resolveWait, 3_000)),
  ]);

  if (server.exitCode === null) {
    server.kill('SIGKILL');
  }
}

try {
  await waitForServer();

  const tests = spawn(process.execPath, [playwrightCli, 'test', ...testArguments], {
    cwd: projectRoot,
    windowsHide: true,
    stdio: 'inherit',
    env: process.env,
  });

  const testExitCode = await new Promise((resolveExit, reject) => {
    tests.once('error', reject);
    tests.once('exit', code => resolveExit(code ?? 1));
  });

  process.exitCode = testExitCode;
}
catch (error) {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
}
finally {
  await stopServer();
}
