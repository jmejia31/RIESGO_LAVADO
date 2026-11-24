# P06 - paquete de certificacion QA confiable

Estado: `PREPARATION_READY_EXTERNAL_EXECUTION`.

Este paquete prepara la ejecucion, pero no acredita aislamiento Windows, identidad Oracle ni autenticacion real. El modo `CERTIFICATION` se bloquea si falta cualquier attestacion externa.

## Archivos y controles

- `frontend/rl-app/playwright.qa.config.ts`: `workers=1`, `retries=0`, screenshots/traces/videos/HAR/HTML/storage state desactivados.
- `frontend/rl-app/e2e/p06-qa-real-auth.spec.ts`: seis escenarios reales AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05 y AUTH-06A.
- `tools/p06-qa-runner.ps1`: valida hashes, descubrimiento, watcher, attestaciones y diferencia `PREFLIGHT`/`CERTIFICATION`.
- `tools/qa/P06_OracleIdentity.sql`: consulta Oracle de solo lectura; no contiene credenciales ni ejecuta DDL/DML.
- `tools/qa/P06_QA_PACKAGE_MANIFEST.json`: hashes individuales y digest del paquete.

Digest vigente del paquete: `d0b237403ab14a1acb95dc93e08cac67f15506a47112033989fab0d4df213f18`. Versión fijada: `P06-QA-REAL-2026-11-23-01`. El Quality Gate de `desarrollo` valida esta huella y descubre exactamente seis escenarios, sin secretos ni conexión a Oracle.

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
- Cada atestacion debe incluir `ISSUER`, `APPROVER`, `APPROVAL_ID`, `APPROVED_AT`, `INDEPENDENT_REVIEW=YES` y `SIGNATURE_ALGORITHM=CMS-SHA256`. El runner rechaza fechas vencidas o futuras, aprobacion de Codex/agente y firmas ausentes, invalidas o emitidas por un certificado distinto al thumbprint confiable externo.
- La firma se entrega como CMS detached en `P06_QA_*_ATTESTATION_SIGNATURE_PATH`, fuera del checkout; el ejecutor confiable debe tener disponible `System.Security.Cryptography.Pkcs` y validar la cadena del certificado. Un hash editable junto con su variable no sustituye la firma.

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

`p06-qa-contract-tests.ps1` verifica firma ausente/invalida, aprobador Codex, expiracion, `CODEX_ACCESS=YES`, `WATCHER_ENABLED=YES`, ausencia, duplicados, campos desconocidos, archivos inexistentes, ruta dentro del checkout, hash incorrecto, contenido sensible y formato invalido. Tambien prueba preflight sin credenciales y el bloqueo de certificacion sin contexto externo. Las pruebas usan solo sentinelas sinteticos y archivos temporales fuera del repositorio.

## Ejecucion manual del propietario en Windows

La ruta alternativa no sustituye `CERTIFICATION` ni declara aislamiento confiable. Desde una consola iniciada por el propietario, despues de cerrar Codex/Antigravity y con el watcher deshabilitado, ejecutar:

```powershell
powershell -NoProfile -File tools/p06-owner-manual.ps1
```

El comando comprueba que el proceso que escucha el puerto API autorizado pertenece al checkout esperado, verifica el endpoint de configuración de solo lectura y solo entonces consulta Oracle mediante `sqlplus` sin mostrar la credencial. La consulta distingue `QUERY_OPERATION=SELECT_ONLY` de la comprobación conservadora de privilegios efectivos (`session_roles`, privilegios de sistema y privilegios recibidos); cualquier ambigüedad bloquea. Genera evidencia sanitizada externa y exige `SCHEMA=RIESGO_LAVADO`, `SERVICE=hpprod1`, `DB_UNIQUE_NAME=hpprod1`, `READ_ONLY=YES`, correspondencia del runtime y `PACKAGE_SHA256`. Luego solicita aprobación manual, rol, módulos y credencial mediante `Get-Credential`; no usa argumentos, archivos persistentes ni logs para la contraseña.

## Addendum operativo vigente — decisión del propietario 2026-11-24

La cuenta Oracle requerida para la comprobación local es la cuenta administrativa QA existente en `RL_QA_DB_TEST_USERNAME` / `RL_QA_DB_TEST_PASSWORD`, con `RL_QA_DB_HOST` como destino suministrado en la sesión protegida. No se solicitan ni se crean `RL_QA_DB_READONLY_USERNAME` o `RL_QA_DB_READONLY_PASSWORD`. Una cuenta administrativa nunca se declara como cuenta de solo lectura.

El modo `LOCAL_OWNER_MANUAL` solo puede ejecutarse por el operador autorizado, fuera de Codex/Antigravity/watcher, y consume esos secretos únicamente desde el proceso protegido. La consulta de identidad y persistencia del paquete es `SELECT_ONLY`, sin DDL, DML, cambios de grants ni pruebas de escritura. La evidencia debe declarar explícitamente:

```ini
ORACLE_ACCOUNT_TYPE=ADMIN_OWNER_APPROVED
ORACLE_READONLY_SECRETS_REQUIRED=NO
ORACLE_INDEPENDENT_RO=NOT_REQUIRED_BY_OWNER_DECISION
QA_ORACLE_QUERY_MODE=SELECT_ONLY
```

El texto histórico que exigía `READ_ONLY=YES` ya no es un criterio vigente; no debe convertirse una consulta SELECT-only en una certificación de privilegios RO. El digest vigente del paquete tras la corrección de compatibilidad Windows PowerShell/PowerShell 7 y la ampliación diagnóstica de AUTH-06A es `d0b237403ab14a1acb95dc93e08cac67f15506a47112033989fab0d4df213f18`.
