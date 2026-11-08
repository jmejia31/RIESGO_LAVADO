# P06 - paquete de certificacion QA confiable

Estado: `PREPARATION_READY_EXTERNAL_EXECUTION`.

Este paquete prepara la ejecucion, pero no acredita aislamiento Windows, identidad Oracle ni autenticacion real. El modo `CERTIFICATION` se bloquea si falta cualquier attestacion externa.

## Archivos y controles

- `frontend/rl-app/playwright.qa.config.ts`: `workers=1`, `retries=0`, screenshots/traces/videos/HAR/HTML/storage state desactivados.
- `frontend/rl-app/e2e/p06-qa-real-auth.spec.ts`: seis escenarios reales AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05 y AUTH-06A.
- `tools/p06-qa-runner.ps1`: valida hashes, descubrimiento, watcher, attestaciones y diferencia `PREFLIGHT`/`CERTIFICATION`.
- `tools/qa/P06_OracleIdentity.sql`: consulta Oracle de solo lectura; no contiene credenciales ni ejecuta DDL/DML.
- `tools/qa/P06_QA_PACKAGE_MANIFEST.json`: hashes individuales y digest del paquete.

Digest vigente del paquete: `e69bacd72b8838f9bf87cd80f8802c895bcc5b35c9ec0f535856abd490c3908a`.

## Preflight sin credenciales

Desde una copia congelada del paquete, antes de introducir secretos:

```powershell
powershell -NoProfile -File tools/p06-qa-runner.ps1 -Mode PREFLIGHT -RepoRoot C:\Ruta\paquete
```

Resultado esperado: `P06_PACKAGE_INTEGRITY=PASS`, `P06_PLAYWRIGHT_DISCOVERY=6`, `P06_PREFLIGHT=PASS`, `P06_REAL_CERTIFICATION=NOT_EXECUTED`.

## Requisitos externos para CERTIFICATION

El propietario/infraestructura debe ejecutar el paquete desde una identidad Windows confiable, sin Codex, Antigravity, watcher, acceso de escritura al checkout de desarrollo ni acceso del agente a credenciales, memoria, temporales o resultados crudos.

El contexto confiable debe aportar, sin imprimir valores:

- `RL_QA_WEB_USERNAME` y `RL_QA_WEB_PASSWORD`.
- `P06_QA_TRUSTED_CONTEXT=YES`.
- `P06_QA_PACKAGE_SHA256` igual al digest del manifiesto.
- `P06_QA_EXPECTED_ROLE` y `P06_QA_EXPECTED_MODULE_IDS_JSON`, definidos por el responsable QA; no se asume ADMINISTRADOR.
- `P06_QA_DB_TARGET_VERIFIED=YES` solo después de comparar la salida sanitizada de `P06_OracleIdentity.sql` con `RIESGO_LAVADO`/`hpprod1`/`hpprod1`.
- `P06_QA_WEB_ORIGIN_VERIFIED=YES` solo después de comprobar origen, release y TLS aprobados.
- `P06_QA_ISOLATION_ATTESTATION_PATH`, `P06_QA_DB_ATTESTATION_PATH` y `P06_QA_WEB_ATTESTATION_PATH` deben apuntar a attestaciones sanitizadas fuera del checkout. Cada archivo debe estar ligado por su `*_SHA256` externo, usar un contrato de campos cerrado, incluir `PACKAGE_SHA256` y no contener secretos.
- La atestacion de aislamiento exige exactamente `TRUSTED_RUNNER_ISOLATION=YES`, `CODEX_ACCESS=NO` y `WATCHER_ENABLED=NO`; los valores `YES` para acceso de Codex o watcher son rechazo.
- La atestacion web debe declarar `QA_WEB_ORIGIN` y `QA_API_ORIGIN`, coincidir con `P06_QA_WEB_ORIGIN`/`P06_QA_API_ORIGIN`, declarar `RELEASE` igual a `P06_QA_RELEASE` y demostrar TLS. HTTP solo se admite para localhost con `P06_QA_LOCALHOST_EXCEPTION=YES` y `QA_LOCALHOST_EXCEPTION_APPROVED=YES` en la atestacion externa.

La contraseña nunca debe ir en argumentos, archivos del repositorio, logs, Issues, capturas, HAR, storage state o chat.

## Matriz de controles

| Control | Estado actual | Evidencia necesaria |
|---|---|---|
| Package hash | Preparado | Digest del manifiesto coincide |
| Suite real | Preparada | 6 escenarios descubiertos |
| Credenciales | Bloqueado | Custodia externa y ejecución confiable |
| Aislamiento Windows | No verificado | Acta de ACL, identidad y ausencia de acceso del agente |
| Origen web | No verificado | Release/origen/TLS aprobados |
| Perfil Oracle efectivo | No verificado | Salida sanitizada del SQL desde la conexión efectiva del backend |
| Login/CSRF/refresh/logout | No ejecutado | Resultado sanitizado del modo CERTIFICATION |
| Matrices | No ejecutado | Orden independiente, preflight de dependencias y autorización específica |

El modo `PREFLIGHT` nunca certifica P06. El modo `CERTIFICATION` no acepta pruebas omitidas, hashes divergentes, watcher habilitado ni attestaciones ausentes.

## Contrato de ejecucion

`P06_QA_MODE` es la unica fuente de modo. El runner lo propaga al proceso Playwright. `PREFLIGHT` valida dependencias, manifiesto y descubrimiento de seis escenarios sin autenticacion; `CERTIFICATION` valida todas las compuertas antes de ejecutar. El runner rechaza skips, cero pruebas, una salida distinta de `6 passed`, hashes divergentes y watcher habilitado.

Para probar solo las compuertas, sin autenticacion, se usa `-ValidateOnly`; nunca certifica P06 ni ejecuta Playwright autenticado:

```powershell
powershell -NoProfile -File tools/p06-qa-runner.ps1 -Mode CERTIFICATION -ValidateOnly
powershell -NoProfile -File tools/p06-qa-contract-tests.ps1
```

`p06-qa-contract-tests.ps1` verifica aceptacion correcta, `CODEX_ACCESS=YES`, `WATCHER_ENABLED=YES`, ausencia, duplicados, campos desconocidos, archivos inexistentes, ruta dentro del checkout, hash incorrecto, contenido sensible y formato invalido. Tambien prueba preflight sin credenciales y el bloqueo de certificacion sin contexto externo. Las pruebas usan solo sentinelas sinteticos y archivos temporales fuera del repositorio.
