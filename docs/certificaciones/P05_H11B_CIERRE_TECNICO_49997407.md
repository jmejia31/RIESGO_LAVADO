# Dictamen independiente — P05 / H11-B, cierre técnico de CI

- **Fecha:** 2026-11-06 (-06:00).
- **Revisor:** CHAT (ChatGPT), mediante GitHub remoto. Aprobación funcional/institucional final reservada al propietario.
- **Repositorio y rama de código:** `jmejia31/RIESGO_LAVADO`, `desarrollo`.
- **SOURCE_CODE_SHA:** `499974072170f5790635c878db60e1f2964cc866`.
- **EVIDENCE_COMMIT_SHA:** `499974072170f5790635c878db60e1f2964cc866` (commit que contiene código, pruebas y el Quality Gate; el SHA del commit de este registro documental es posterior y NO cambia el SHA del código certificado).
- **Quality Gate:** [run 42261424014](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/42261424014), `completed/success`, intento `1`, `head_sha=499974072170f5790635c878db60e1f2964cc866`.
- **Job de controles:** `128042048471`, `success`.
- **Job integral:** [job 128042121794](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/42261424014/job/128042121794), `success`.
- **Índice de evidencias:** [INDEX.json](INDEX.json).
- **Expediente histórico P05:** [P05_SEGURIDAD_USUARIOS.md](P05_SEGURIDAD_USUARIOS.md).
- **Artefacto CI:** `block7-browser-evidence-42261424014` (evidencia del bloque 7, **no** prueba específica de autenticación).

## Alcance y evidencia comprobada

| Control | Resultado y fuente |
|---|---|
| SHA exacto de `origin/desarrollo` al certificar código | `499974072170f5790635c878db60e1f2964cc866`; comprobado mediante GitHub REST. |
| Workflow Quality Gates | `SUCCESS`, intento 1, mismo SHA. Ambos jobs finalizaron `SUCCESS`. |
| Backend Identidad / atomicidad | Test `UsuarioRepositoryAtomicityOracleIntegrationTests` ejecutado en Oracle XE descartable y aprobado en job integral. Evidencia focalizada local informada por COD: `14/14 PASS` (no repetida localmente por CHAT). |
| Angular | Log CI: `91` archivos de tests y `957/957 PASS`, incluidos `auth.service.spec.ts` (12 tests). |
| Integración Matrices / Oracle XE / E2E | Pasos aplicables del job integral `SUCCESS`, no equivalen a aceptación institucional de autenticación. |
| Refresco de CSRF | Código de `AuthController.Refresh()` reemite request token al rotar refresh; `AntiforgeryRequestCookieOptions` alinea vencimiento con `RefreshTokenExpiresAt`. Regresión de controlador publicada. |
| Cookies | `RL-RefreshToken` permanece `HttpOnly`, `SameSite=Strict` y restringida a `/api/auth`; `RL-Antiforgery` es cookie interna `HttpOnly`; `XSRF-TOKEN` es request token legible por Angular con `Path=/`. |
| JWT y seguridad funcional | Access token Bearer se conserva. Refresh token no se entrega en JSON ni se escribe en `localStorage` por la implementación revisada. |

## Limitaciones y reservas expresas

1. **AUTH_REAL_E2E=PENDING:** no se ejecutó en este cierre un Playwright/TestServer autenticado que cubra Angular -> cookies -> middleware antiforgery -> API con usuario sintético. Falta verificar la vinculación identidad/JWT y el ciclo completo login-refresh-logout en entorno permitido.
2. **QA/PRODUCTION=NOT_EXECUTED:** no se autentica cuenta institucional ni se afirma que QA o producción estén protegidos por código no desplegado.
3. **ACCESS_TOKEN_LOCALSTORAGE=RESIDUAL_RISK_DOCUMENTED:** el access token permanece accesible a JavaScript; cookie HttpOnly del refresh no elimina todo riesgo XSS.
4. **H11-A=ARCHITECTURAL_DEBT:** consulta hash directa/indexación del refresh backend pendiente de decisión independiente; no se ejecuta DDL.
5. **BACKEND_LOCAL_FULL_SUITE_LIMITATION:** COD informó `1091 PASS, 2 SKIP, 10 FAIL` en un entorno local previo: 9 dependencias `LOCAL_XE` y 1 contrato hash Fase6 preexistente; esta certificación no los presenta como corregidos ni refuta la clasificación sin una reproducción independiente.
6. P04 continúa `PASS_WITH_DOCUMENTED_LIMITATIONS`. Este dictamen no certifica el sistema completo, una liberación institucional, el cierre H08/TX12 ni la autorización de Bloque 10.

## Dictamen de CHAT

```ini
SOURCE_CODE_SHA=499974072170f5790635c878db60e1f2964cc866
EVIDENCE_COMMIT_SHA=499974072170f5790635c878db60e1f2964cc866
QUALITY_GATE_RUN=42261424014
QUALITY_GATE_STATUS=SUCCESS
QUALITY_GATE_ATTEMPT=1
QUALITY_GATE_SHA_MATCH=YES
CONTROL_JOB=SUCCESS
INTEGRAL_JOB=SUCCESS
ANGULAR_UNIT_TESTS=957/957_PASS
ORACLE_XE_ATOMICITY=PASS_IN_CI
CSRF_REFRESH_RENEWAL=IMPLEMENTED_AND_CI_TESTED
P05_H11B_TECHNICAL_CI=CERTIFIED_WITH_DOCUMENTED_LIMITATIONS
P05_REVIEW=PASS_WITH_DOCUMENTED_LIMITATIONS
AUTH_REAL_E2E=PENDING_ALLOWED_ENVIRONMENT
PRODUCTION_VALIDATION=NOT_EXECUTED
QA_SECRET_ACCESS_GATE=BLOCKED
QA_DATA_MUTATIONS=DISABLED
P06_STATUS=READY_FOR_DOCUMENTARY_PREPARATION
BLOCK10_AUTHORIZED=NO
GITHUB_REMOTE_VERIFIED=YES
```

**Siguiente etapa: P06 — preparación QA, únicamente documental en su inicio.** La [Issue #25](https://github.com/jmejia31/RIESGO_LAVADO/issues/25) está abierta; la [Issue #23](https://github.com/jmejia31/RIESGO_LAVADO/issues/23) está cerrada. La cola contiene `ISSUE25-QA-PLAN-001` y `ISSUE25-MATRICES-PLAN-002` en estado `READY`, con `allowed_paths` estrictos a `docs/qa/`. Ninguna tarea READY autoriza contactos institucionales, secretos, escritura Oracle ni pruebas autenticadas. El workflow `qa-config-preflight.yml` ya existe y es manual; no se duplica ni ejecuta durante este cierre.
