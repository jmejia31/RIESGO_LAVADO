# P05 — Seguridad del módulo Usuarios

> **Estado vigente certificado por revisión remota CHAT (2026-11-06):** P05 `PASS_WITH_DOCUMENTED_LIMITATIONS`; H11-B `CERTIFIED_TECHNICALLY_WITH_LIMITATIONS` sobre código `499974072170f5790635c878db60e1f2964cc866`, Quality Gate `42261424014` `SUCCESS`, intento 1. La narrativa histórica `BLOCKED` más abajo se conserva como registro de la búsqueda previa. [Dictamen final H11-B](P05_H11B_CIERRE_TECNICO_49997407.md).

Estado de esta intervención: `BLOCKED` por falta de la fuente original de los tres hallazgos.

## Alcance y procedencia

- Rama: `desarrollo`
- `SOURCE_SHA=594204723c294ecac50c7380480d3d6b4bcdeec1`
- Quality Gate consultado: [42235275235](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/42235275235), `SUCCESS`, `run_attempt=1`, SHA exacto `594204723c294ecac50c7380480d3d6b4bcdeec1`.
- El expediente P05 exige identificar tres hallazgos originales antes de modificar código. La orden recibida no contiene sus descripciones.
- La búsqueda en `AGENTS.md`, bitácora, estado colaborativo, matriz P04, `SECURITY.md`, historial Git, issues y comentarios remotos no localizó una definición individual verificable de esos tres hallazgos.

Las observaciones históricas genéricas del documento eliminado `docs/modulos/01_SEGURIDAD_USUARIOS.md` no se tratan como hallazgos P05: no tienen identificador P05, criterio de reproducción ni autorización para convertirlas en vulnerabilidades.

## Matriz individual

| Hallazgo | Descripción original | Fuente verificable | Superficie revisada | Resultado | Estado |
|---|---|---|---|---|---|
| P05-01 | No localizada en las fuentes autorizadas | Ausente | Identidad, autorización administrativa y rutas de Usuarios inspeccionadas; no se asigna un defecto sustituto | No puede reproducirse un comportamiento sin contrato original | `BLOCKED` |
| P05-02 | No localizada en las fuentes autorizadas | Ausente | Credenciales, mensajes de error y contratos de autenticación inspeccionados; no se asigna un defecto sustituto | No puede reproducirse un comportamiento sin contrato original | `BLOCKED` |
| P05-03 | No localizada en las fuentes autorizadas | Ausente | Refresh tokens, revocación, claims y persistencia de asignaciones inspeccionados; no se asigna un defecto sustituto | No puede reproducirse un comportamiento sin contrato original | `BLOCKED` |

## Evidencia técnica ejecutada

- Backend focalizado `Features.Identidad`: `15 PASS`, `1 SKIP`; el skip es `UsuarioRepositoryAtomicityOracleIntegrationTests.UserAndModuleWritesRollbackWhenNthAssignmentFails` y requiere Oracle XE. No se conectó Oracle institucional.
- Angular focalizado de autenticación, guards y Usuarios: `13/13 PASS` mediante `ng test`.
- `validate_documentation_links.ps1`: `PASS` (`206` documentos Markdown, `218` enlaces locales).
- `git diff --check`: `PASS`.
- Inspección estática: los endpoints administrativos conservan `[Authorize(Roles = "ADMINISTRADOR")]` y `[ModuloAuthorize(2)]`; no se modificó código.
- El gate heredado `42154789302` y la ejecución documental `42235275235` no se cuentan como una certificación P05 de seguridad; se citan solo como evidencia de integridad del SHA.

## Dictamen

`FINDINGS_IDENTIFIED=0/3` y `FINDINGS_REVIEWED=0/3` en sentido de trazabilidad del universo original. Se revisaron las superficies técnicas disponibles, pero eso no permite clasificar honestamente los tres hallazgos como `ALREADY_FIXED`, `NOT_REPRODUCIBLE` o `FIXED`.

No se confirmaron vulnerabilidades dentro de un alcance identificable, no se modificó código funcional y no se requiere un Quality Gate nuevo para este cambio documental. P05 no queda certificado. Para continuar se necesita la fuente original que vincule cada hallazgo con su descripción, componente y criterio de reproducción.

```ini
P05_FINDING_01=BLOCKED
P05_FINDING_02=BLOCKED
P05_FINDING_03=BLOCKED
CONFIRMED_VULNERABILITIES=0
VULNERABILITIES_FIXED=0
ALREADY_FIXED=0
NOT_REPRODUCIBLE=0
UNRESOLVED_SECURITY_DEFECTS=NOT_DETERMINABLE
BACKEND_TESTS=15_PASS_1_SKIP_ORACLE_XE
ANGULAR_TESTS=13/13_PASS
SECURITY_REGRESSION=NOT_APPLICABLE_WITHOUT_ORIGINAL_FINDINGS
ORACLE_XE_TESTS=NOT_EXECUTED
P05_STATUS=BLOCKED_MISSING_ORIGINAL_FINDINGS
P06_STATUS=NOT_STARTED
PRODUCTION_MODIFIED=NO
INSTITUTIONAL_ORACLE_MODIFIED=NO
MAIN_MODIFIED=NO
```
## Continuación: fuente histórica recuperada

Fuente histórica recibida en esta continuación:

```ini
P05_HISTORICAL_SOURCE=RECOVERED
P05_HISTORICAL_BASE_SHA=76e057dd6f040778fb3f0e1043aca019d270fbb6
AUDIT_QUALITY_GATE=38759493745
AUDIT_MODE=READ_ONLY
```

La correspondencia técnica de los tres candidatos queda suficientemente demostrada, aunque la auditoría histórica no usaba identificadores P05: H7 corresponde a atomicidad usuario/módulos; H11-A a refresh tokens backend; H11-B a almacenamiento frontend. La limitación de numeración original queda conservada.

| Candidato histórico | Evidencia vigente | Clasificación | Decisión pendiente |
|---|---|---|---|
| H7 — atomicidad usuario/módulos | `UsuarioRepository.CrearAsync` y `ActualizarAsync` abren transacción, asignan `cmd.Transaction`, hacen `CommitAsync` y revierten en `catch`. `UsuarioRepositoryAtomicityOracleIntegrationTests` verifica rollback de alta/actualización; la prueba local se omitió por no disponer de Oracle XE. | `ALREADY_FIXED` | Ninguna dentro de P05; Oracle XE actual queda pendiente de infraestructura disposable.
| H11-A — refresh tokens backend | Nuevos valores pasan por `RefreshTokenSecurity.Hash`; rotación revoca e inserta dentro de transacción; tests cubren hash, legacy, rotación, replay y tokens inactivos. La búsqueda actual recorre filas activas y compara en memoria; esto es deuda de rendimiento/índice, no una explotación de seguridad reproducida. | `NOT_REPRODUCIBLE` | Decidir en una fase autorizada si se incorpora lookup directo/indexado y cómo completar la migración legacy; no hacer DDL aquí.
| H11-B — refresh token en `localStorage` | `auth.service.ts` conserva `access_token`, `refresh_token` y `token_expira` en `localStorage`; las pruebas verifican persistencia, rotación y limpieza. No se reprodujo XSS ni acceso no autorizado en un navegador real. | `NOT_REPRODUCIBLE` | Decisión arquitectónica institucional: cookies HttpOnly/Secure/SameSite, CSRF, CORS, HTTPS y compatibilidad. No migrar unilateralmente.

```ini
P05_ORIGINAL_THREE_MAPPING=H7_ATOMICITY;H11-A_REFRESH_BACKEND;H11-B_REFRESH_FRONTEND
P05_ATOMICITY_STATUS=ALREADY_FIXED
P05_REFRESH_BACKEND_STATUS=NOT_REPRODUCIBLE_SECURITY_DEBT_DOCUMENTED
P05_REFRESH_FRONTEND_STATUS=NOT_REPRODUCIBLE_ARCHITECTURAL_DECISION_PENDING
P05_CONFIRMED_VULNERABILITIES=0
P05_VULNERABILITIES_FIXED=0
P05_ARCHITECTURAL_DECISIONS_PENDING=REFRESH_COOKIE_MIGRATION;DIRECT_HASH_LOOKUP_INDEX
P05_UNRESOLVED_SECURITY_DEFECTS=0_CONFIRMED;2_ARCHITECTURAL_DECISIONS_PENDING
P05_BACKEND_TESTS=15_PASS_1_SKIP_ORACLE_XE
P05_ANGULAR_TESTS=13/13_PASS
P05_ORACLE_XE_TESTS=NOT_EXECUTED_LOCAL;HISTORICAL_GATE_38759493745_SUCCESS
P05_STATUS=PASS_WITH_DOCUMENTED_LIMITATIONS
```

## Dictamen independiente final H11-B — 2026-11-06 (CHAT)

- `SOURCE_CODE_SHA=499974072170f5790635c878db60e1f2964cc866`, `QUALITY_GATE_RUN=42261424014`, `SUCCESS`, `attempt=1`, ambos jobs `SUCCESS`.
- H7 `ALREADY_FIXED`; H11-A conserva deuda técnica de búsqueda/indexación; H11-B `CERTIFIED_TECHNICALLY_WITH_LIMITATIONS` por migración HttpOnly, separación CSRF y renovación del request token con vigencia correcta.
- `AUTH_REAL_E2E=PENDING_ALLOWED_ENVIRONMENT`; `ACCESS_TOKEN_LOCALSTORAGE=RESIDUAL_RISK`; QA, Oracle institucional y producción `NOT_EXECUTED`. No se afirma certificación integral ni aceptación institucional.
- Dictamen remoto: [P05_H11B_CIERRE_TECNICO_49997407.md](P05_H11B_CIERRE_TECNICO_49997407.md). Siguiente: P06 documental Issue #25; no iniciar Bloque 10.
