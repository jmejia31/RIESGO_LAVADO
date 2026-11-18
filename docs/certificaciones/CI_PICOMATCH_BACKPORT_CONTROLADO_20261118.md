# RIESGO_LAVADO — Contención reproducible CVE-2026-112366 (picomatch)
**Fecha:** 2026-11-18 09 -06:00  
**Autor:** CHAT / ChatGPT, cuenta GitHub `jmejia31`  
**Rama:** `desarrollo`  
**BASE_SHA:** `170578f0f40c9cc67ea0bfb85a938bb793bdf923`  
**ESTADO PREPUBLICACION:** `BACKPORT_CANDIDATE_PREPARED_PENDING_EXACT_SHA_CI`; la publicación y certificación se comprobarán exclusivamente en GitHub Actions.  
**EVIDENCE_COMMIT_SHA:** resolver por la referencia inmutable después de publicar (no puede incrustarse el SHA de un commit dentro de su propio contenido).

## 1. Causa raíz documentada
El run previo [44324753203](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44324753203), SHA `170578f0f40c9cc67ea0bfb85a938bb793bdf923`, completó `failure`: 44 pasos exitosos, 2 omitidos y un fallo en el enforcement final. `npm ci --ignore-scripts` y `npm audit --omit=dev` pasaron (**0 hallazgos de producción**); `npm audit` completo reportó **21 rutas HIGH** derivadas de `picomatch@4.0.7` (`GHSA-fc5q-39xc-vjpf`, `CVE-2026-112366`) y respondió `No fix available`. No eran 21 CVE independientes.

La versión oficial más reciente del proveedor, comprobada el 18/11/2026, sigue siendo **4.0.7**, publicada el 24/08/2026. La propuesta [micromatch/picomatch PR #238](https://github.com/micromatch/picomatch/pull/238), revisión head `44f192261f312ddf236cb7bdc05d8f22663f98f9`, continúa **sin fusionar**. Por tanto, no existe remediación oficial que permita certificar `npm audit` como verde.

## 2. Mitigación provisional aplicada al código (candidato revisado, NO parche oficial)
- **Procedencia:** archivo `lib/parse.js` de `picomatch@4.0.7`, MIT, desde el tag oficial, Git blob `29207512c60183b4bb9bf5112be3b9b5f4a9a9fd`. La lógica añadida reproduce el detector limitado `hasUnsafeLeadingQuantifier` de PR #238; blob resultante íntegro fijado `9f968c0681bc08d7383c1be29f376a2caf6c60e4`.
- **Ubicación:** `frontend/rl-app/scripts/security/picomatch-mitigation.mjs`. Se ejecuta **después** de `npm ci --ignore-scripts` y antes de compilar/validar. Solo acepta la versión 4.0.7 y el blob exacto de origen; si la fuente cambia, el proceso falla cerrado. Tras insertar el cambio comprueba la huella exacta del archivo remediado. Es idempotente.
- **Pruebas adversariales:** `frontend/rl-app/scripts/security/picomatch-mitigation.test.mjs` con `node:test` verifica fuente fijada, formas `+(*a)`, `*(*a)`, `+(a|*a)`, casos benignos y ataque en proceso hijo con timeout para evitar bloqueo prolongado.
- **Activación:** nuevo paso obligatorio y temprano de `.github/workflows/quality-gates.yml`; el workflow M1 manual y el build Docker también aplican/verifican la mitigación. Hooks `prebuild/pretest/prelint/prestart/prewatch/pree2e/preng` y `pretest:coverage` cubren comandos npm habituales. La suite de controles `tools/automatizacion/test_ci_picomatch_mitigation.py` comprueba que no se supriman auditorías ni pines.
- **Cadena de suministro:** no se instala un fork de terceros, ni se modifica la versión o nombre registrado del paquete, ni `package-lock.json`. `npm ci` sigue resolviendo los mismos paquetes y el parche efímero se aplica solo a `node_modules` con verificación criptográfica de contenido.

## 3. Límites de seguridad no negociables
1. **Esto es defensa temporal, no declaración de CVE completamente reparada.** La propia PR #238 reconoce formas emparentadas no cubiertas, por ejemplo `+(*(ab)c)`; no se debe permitir el procesamiento de patrones glob no confiables. El código de picomatch proviene de una propuesta sin aprobación upstream.
2. El bloqueo `npm audit` completo **no se modifica ni se perdona**: el escáner continuará reportando `picomatch@4.0.7` porque el lockfile permanece igual y no hay release oficial corregido. El Quality Gate global debe permanecer **rojo** hasta que el análisis completo pase realmente, o exista un proceso de aceptación de riesgos autorizado por el propietario con política formal separada. En esta intervención NO se creó excepción ni waiver.
3. Nunca interpretar una compilación verde o el paso de la prueba anti-ReDoS como `SECURITY_AUDIT=PASS_FULL_TREE`. La fuente de verdad será el run **del SHA exacto**, observado después de publicar.
4. Cualquier nueva versión oficial de picomatch hará que el patcher falle cerrado, para obligar a revisar y **eliminar** el backport en el mismo cambio que actualice la dependencia/lockfile, y ejecutar nuevamente `npm ci`, `npm audit`, test, lint, Angular, E2E y contenedores.

## 4. Checklist de aceptación posterior a publicación
- [ ] `node scripts/security/picomatch-mitigation.mjs --apply` => `PASS_PINNED_BACKPORT_APPLIED`.
- [ ] Repetir `--apply` y `--verify` => idéntico hash, sin mutaciones.
- [ ] `node --test scripts/security/picomatch-mitigation.test.mjs` => pruebas adversariales PASS.
- [ ] `npm run lint`, `npm test`, `npm run build`, Playwright y Docker => PASS con parche aplicado.
- [ ] `npm audit --omit=dev` => 0 vulnerabilidades; `npm audit` completo se mantiene obligatorio; su FAIL implica `Quality Gate=FAIL`.
- [ ] Certificar run final con SHA propio y completar [issue #42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42) **solo** tras release oficial corregido y auditoría completa realmente PASS.

**Prohibiciones:** no se ejecutó Oracle institucional; `main`, producción, `DNP_IHSS` y datos personales siguen intactos. La conciliación real de Microfase 1 continúa separada en [issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41).

```ini
PROJECT=RIESGO_LAVADO
AUTHOR=CHAT
BRANCH=desarrollo
BASE_SHA=170578f0f40c9cc67ea0bfb85a938bb793bdf923
CVE=CVE-2026-112366
UPSTREAM_RELEASE_LATEST=4.0.7
UPSTREAM_PR_238=OPEN_UNMERGED
ORIGINAL_GIT_BLOB=29207512c60183b4bb9bf5112be3b9b5f4a9a9fd
BACKPORT_GIT_BLOB=9f968c0681bc08d7383c1be29f376a2caf6c60e4
SECURITY_BACKPORT=REPRODUCIBLE_CANDIDATE_PENDING_CI
FULL_NPM_AUDIT=BLOCKING_NOT_WAIVED
QUALITY_GATE_GLOBAL=NOT_CERTIFIED
MAIN_TOUCHED=NO
PRODUCTION_TOUCHED=NO
ORACLE_INSTITUTIONAL_WRITES=0
DNP_IHSS_WRITES=0
OWNER_SECURITY_ACCEPTANCE=NOT_GRANTED
```


## 7. Sustitución operativa de dependencia — 2026-11-18 09:53 -06:00

El parche temporal aplicado sobre `node_modules` no podía resolver `npm audit` porque el lock seguía declarando `picomatch@4.0.7`. Se preparó una **distribución privada rastreable** `@riesgo-lavado/picomatch-hardened@4.0.7-rl.2` que sí se resuelve mediante `npm overrides` con tarball local, SRI SHA256, licencia MIT, hash Git del código corregido y pruebas adversariales. El backport contiene defensa adicional para `+(*)x`/ `*(*)x`. El escáner `npm audit` completo sigue bloqueante sin excepción; como el fork tiene otro nombre, existe limitación conocida de herencia de advisories y se exige control adicional de procedencia. **No se declara PASS antes de correr GitHub Actions**. [Expediente técnico](CI_PICOMATCH_FORK_VENDORIZADO_20261118.md); [Issue #42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42).
