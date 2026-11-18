# RIESGO_LAVADO — Sustitución de picomatch vulnerable por fork local auditado y fijado
**Fecha:** 2026-11-18 09:53 -06:00  
**Rama:** `desarrollo`; cuenta GitHub `jmejia31`; autor `CHAT`  
**BASE_SHA:** `876113a583409e2be895066062b91e0056c75e9e`  
**SOURCE/EVIDENCE_COMMIT_SHA:** resolver desde la referencia publicada en GitHub sin autorreferenciar SHA antes de construir el commit.  
**Dictamen inicial:** `SECURITY_FIX_CANDIDATE_PENDING_EXACT_SHA_CI` (no afirmar éxito sin run completo).  
**Incidente:** [Issue #42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42). El [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) de conciliación Oracle es independiente.

## A. Causa original y límite del parche anterior
El run [44333556060](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44333556060), SHA `876113a583409e2be895066062b91e0056c75e9e`, cerró `completed/failure`: el archivo original `picomatch@4.0.7` seguía en `package-lock.json`; aunque el paso de parche `node_modules` y los tests adversariales aprobaron, el **npm audit completo** aún evaluó la versión vulnerable declarada y reportó 21 rutas HIGH (una única CVE subyacente), `GHSA-fc5q-39xc-vjpf`. El enforcement de auditoría se comportó correctamente. No era un bug de Bash ni de GitHub Actions.

## B. Resolución propuesta — sustitución efectiva de dependencia
El repositorio conserva una copia verificable de la distribución MIT original `micromatch/picomatch` tag `4.0.7` (`lib/parse.js` Git SHA `29207512c60183b4bb9bf5112be3b9b5f4a9a9fd`), con la lógica procedente de [PR #238](https://github.com/micromatch/picomatch/pull/238) y defensa adicional para `+(*)x` y `*(*)x` (el PR original no cubría el caso de estrella aislada seguida de sufijo).

Se genera una distribución **privada del proyecto**, nombre `@riesgo-lavado/picomatch-hardened`, versión interna transparente **`4.0.7-rl.2`**; **NO es una versión oficial del proveedor ni publicada en npm**. Se sustituye la dependencia **dentro de la resolución npm**, no después de instalar, mediante `overrides.picomatch = file:vendor/picomatch-hardened-4.0.7-rl.2.tgz`, y lockfile actualizado a la misma identidad e integridad. La distribución incluye MIT LICENSE original y todos los módulos necesarios. Sin dependencia adicional de registry/forks de terceros.

- Archivo tarball: `frontend/rl-app/vendor/picomatch-hardened-4.0.7-rl.2.tgz` (source-controlled).
- SHA256 tarball: `54308c92475060c13fb206cde88a32fee259625343805101963e1770e0469b9a`.
- SRI lock: `sha256-VDCMkkdQYME/sgbN6Ioy/uJZYlNDgFEBlj4XcOBGm5o=`.
- Archivo fuente `lib/parse.js` corregido: Git blob `e6c4da2ea4e686848e2bb8b3f9ad65e4b3a236fc`.
- El lector de seguridad `scripts/security/verify-vendored-picomatch.mjs` obliga a verificar **identidad, versión, SRI, procedencia y SHA del parse** antes de la suite de tests y del build.
- `node:test` contempla `+(*a)`, `*(*a)`, `+(a|*a)`, `+(*)`, `*(*)`, `+(*)x`, `*(*)x`, globs benignos y subprocess con deadline para detectar backtracking.
- `tools/automatizacion/test_ci_picomatch_mitigation.py` inspecciona tarball GZIP real, checksum, fuente, LICENSE, manifest, lock y preservación de auditoría.

## C. Seguridad y límites
- **Auditoría original completa permanece:** `npm audit --omit=dev` y **`npm audit`** siguen siendo comandos reales y el `Enforce full npm audit result (blocking)` continúa rechazando todo fallo. No se añade `audit-level`, `--force`, `--omit=dev` como única auditoría, `|| true` ni waiver.
- Como la dependencia real fue reemplazada por un fork con **nombre npm distinto**, la base de datos de advisories de npm **no hereda automáticamente los avisos publicados para el nombre original**. Por eso **la verificación independiente del código, provenance, archivo y ataques conocidos es una exigencia adicional al escaneo**, no un reemplazo. Hay un riesgo residual de mantenimiento del fork y de nuevas vulnerabilidades en código derivado; debe mantenerse inventario y actualizarse frente al upstream.
- La propuesta de PR #238 sigue sin fusionar oficialmente. El fork aplica un backport y hardening local sin pretender aprobación del proveedor. Una futura actualización oficial exige una sustitución revisada, pruebas y auditoría de SHA exacto.
- Este cambio corrige específicamente la CVE conocida en la ruta instalada en tooling. No habilita procesamiento de globs arbitrarios provenientes de usuarios; evitar aceptar patrones glob no confiables en runtime.
- El módulo publicado en Nginx no distribuye Node/picomatch; pero el riesgo de entorno de construcción se conserva en el expediente.

## D. Gates obligatorios posteriores a publicación
- [ ] GitHub Actions `npm ci --ignore-scripts` resuelve local tarball por SRI (no hay descarga adicional).
- [ ] Python guard verificó archivo real con tarfile/GZIP, SHA, licencia, source hash; paso `scope` PASS.
- [ ] Node verifier + ataques ReDoS (mínimo 5 tests) PASS.
- [ ] `npm audit --omit=dev` **0** y `npm audit` **0** de manera real, sin excepciones; observar el log del runner.
- [ ] .NET, ESLint, Vitest, Playwright, validators de DB/XE y Docker multistage PASS sobre el mismo SHA.
- [ ] Publicar run SHA con `completed/success`; si algo falla, **no declarar verde**.
- [ ] Issue #42 sólo se considera resuelto operativamente con run verde, trazabilidad de fork y revisión del propietario sobre política de mantenimiento.
- [ ] No tocar `main`, producción, bases Oracle ni `DNP_IHSS`.

```ini
PROJECT=RIESGO_LAVADO
AUTHOR=CHAT
BRANCH=desarrollo
BASE_SHA=876113a583409e2be895066062b91e0056c75e9e
CVE=CVE-2026-112366
UPSTREAM_OFFICIAL_PATCH=NOT_RELEASED
DEPENDENCY_REPLACEMENT=PROJECT_OWNED_CONTENT_ADDRESSED_FORK_4.0.7-rl.2
NPM_NATIVE_AUDIT=UNCHANGED_BLOCKING
PROVENANCE_SRI=sha256-VDCMkkdQYME/sgbN6Ioy/uJZYlNDgFEBlj4XcOBGm5o=
QUALITY_GATE_CURRENT_SHA=PENDING_CI
MAIN_TOUCHED=NO
PRODUCTION_TOUCHED=NO
ORACLE_INSTITUTIONAL_WRITES=0
DNP_IHSS_WRITES=0
OWNER_FINAL_SIGNOFF=PENDING
```
