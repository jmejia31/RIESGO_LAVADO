# P06 - paquete de certificacion QA confiable

Estado: `PREPARATION_READY_EXTERNAL_EXECUTION`.

Este paquete prepara la ejecucion, pero no acredita aislamiento Windows, identidad Oracle ni autenticacion real. El modo `CERTIFICATION` se bloquea si falta cualquier attestacion externa.

## Archivos y controles

- `frontend/rl-app/playwright.qa.config.ts`: `workers=1`, `retries=0`, screenshots/traces/videos/HAR/HTML/storage state desactivados.
- `frontend/rl-app/e2e/p06-qa-real-auth.spec.ts`: seis escenarios reales AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05 y AUTH-06A.
- `tools/p06-qa-runner.ps1`: valida hashes, descubrimiento, watcher, attestaciones y diferencia `PREFLIGHT`/`CERTIFICATION`.
- `tools/qa/P06_OracleIdentity.sql`: consulta Oracle de solo lectura; no contiene credenciales ni ejecuta DDL/DML.
- `tools/qa/P06_QA_PACKAGE_MANIFEST.json`: hashes individuales y digest del paquete.

Digest vigente del paquete: `9349b87c805d7f0837d2ab2fb25a7f72a7b5a137ab3f9a316b739b11ed6a24ea`.

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
- `P06_QA_ISOLATION_ATTESTATION_PATH`, `P06_QA_DB_ATTESTATION_PATH` y `P06_QA_WEB_ATTESTATION_PATH` deben apuntar a attestaciones sanitizadas fuera del checkout. El runner rechaza archivos dentro del repositorio, marcadores ausentes o texto que contenga secretos.

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
