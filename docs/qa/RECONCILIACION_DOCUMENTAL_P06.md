# P06 — Reconciliación de documentos y estados de cola

**Revisión técnica independiente:** CHAT, 2026-11-06. **Fuentes:** documentos del commit `edd8328b389f63ba7c9cbac586590eb0c5cc792b`, Quality Gate [42266632011](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/42266632011) `SUCCESS` con job integral; observaciones del commit `501a56e2d1128b9e10f3a753f399c55a8d351a48`.

## Revisión del entregable

- Los cuatro documentos P06 existen en `docs/qa/`, dentro de las rutas autorizadas; son **preparación estática**, sin pruebas QA conectadas.
- `P06_DOCUMENTARY_REVIEW=PASS_WITH_LIMITATIONS` y `P06_REAL_QA_CERTIFICATION=NOT_EXECUTED`.
- El Quality Gate `42269995632` del commit de observaciones finalizó `SUCCESS`, con job integral `SKIPPED`; **no** se usa como sustituto del integral `42266632011`.

## No falsificar el historial del ejecutor

| Tarea | Instrucción en cola | Estado persistido | Resultado externo publicado |
|---|---|---|---|
| `ISSUE25-QA-PLAN-001` | `READY` | `BLOCKED`, intento 2, `CODEX_FAILED` | Documentos QA/catálogo en `edd8328b` |
| `ISSUE25-MATRICES-PLAN-002` | `READY` | `BLOCKED`, intento 2, `CODEX_FAILED` | Plan de escenarios/matriz en `edd8328b` |

**Regla:** estos documentos fueron entregados **fuera del ejecutor**, no mediante una nueva reclamación de Codex. No reejecutar `BLOCKED`, no reponer intentos, no cambiar `BLOCKED` a `APPROVED` sin transición formal aprobada por el propietario, ni activar watcher/cola automáticamente. Conservamos las observaciones añadidas en `TAREAS_CODEX.json` como referencia, no como aprobación.

```ini
P06_DOCUMENTS_PUBLISHED=YES
P06_DOC_QUALITY_GATE=SUCCESS
P06_DOCUMENTARY_REVIEW=PASS_WITH_LIMITATIONS
P06_QUEUE_RECONCILIATION=PENDING_OWNER_DECISION
P06_REAL_QA_CERTIFICATION=NOT_EXECUTED
QA_SECRET_ACCESS_GATE=BLOCKED
BLOCK10_AUTHORIZED=NO
```

## Reconciliación vigente — decisión Oracle del propietario 2026-11-24

La Issue #25 sustituyó el requisito previo de cuenta Oracle RO independiente. El alcance vigente usa la cuenta administrativa QA existente (`RL_QA_DB_TEST_*`) y `RL_QA_DB_HOST` en una sesión protegida del operador. La cuenta no se presenta como RO; el paquete solo ejecuta diagnósticos `SELECT_ONLY` y no realiza DML/DDL ni mutaciones de negocio en esta intervención.

La huella del paquete se actualizó tras corregir la ordenación cultural del digest entre Windows PowerShell 5 y PowerShell 7:

```ini
P06_PACKAGE_SHA256=d0b237403ab14a1acb95dc93e08cac67f15506a47112033989fab0d4df213f18
P06_WINDOWS_POWERSHELL_PREFLIGHT=PASS
P06_CONTRACT_TESTS=PASS
AUTH_REAL_6_OF_6=PENDING_OPERATOR_PRIMARY_EVIDENCE
ORACLE_EFFECTIVE_TARGET_VERIFIED=PENDING_OPERATOR_PRIMARY_EVIDENCE
ORACLE_ACCOUNT_TYPE=ADMIN_OWNER_APPROVED
ORACLE_READONLY_SECRETS_REQUIRED=NO
ORACLE_INDEPENDENT_RO=NOT_REQUIRED_BY_OWNER_DECISION
QA_ORACLE_QUERY_MODE=SELECT_ONLY
ISSUE25=OPEN_PENDING_REAL_TESTS_AND_ACCEPTANCE
```

Ver [decisión de seguridad](DECISION_SEGURIDAD_EJECUCION_QA.md) y [controles de aceptación](CONTROL_ACEPTACION_SEGURIDAD_QA.md).

## Decisión del propietario y reconciliación final — 2026-11-06

El propietario `jmejia31` **APROBÓ EXPLÍCITAMENTE la aceptación documental** de los entregables de ambas tareas (commit `edd8328b389f63ba7c9cbac586590eb0c5cc792b`, Quality Gate integral `42266632011=SUCCESS`) y autorizó registrar la reconciliación por revisión externa, conservando íntegro el historial de intentos fallidos.

En `TAREAS_CODEX.json` se cambia **solo** el estado declarativo de `ISSUE25-QA-PLAN-001` y `ISSUE25-MATRICES-PLAN-002` de `READY` a `APPROVED`, con observaciones inequívocas de **entrega fuera del ejecutor**. Este estado representa aprobación de **documentos**, no éxito de Codex.

En `ESTADO_TAREAS.json` se preserva **sin modificación** el registro histórico: `BLOCKED`, `attempt=2`, `reason=CODEX_FAILED` para ambas tareas. No se reejecutan, no se alteran los intentos y no se activa el watcher. La aprobación documental se acredita exclusivamente por la autorización expresa del propietario y el acta de revisión, no por un falso `RUNNING` o `IMPLEMENTED_PENDING_REVIEW`.

La restricción de acceso QA real permanece independiente; la nueva autorización sobre la **base QA/de desarrollo alojada en infraestructura productiva** se documenta en [ALCANCE_QA_DESARROLLO_AUTORIZADO.md](ALCANCE_QA_DESARROLLO_AUTORIZADO.md), con libertad de pruebas funcionales de Matrices y eventual borrado completo del **perímetro propio de Matrices** después de comprobar identidad de la base y dependencias ajenas.

```ini
P06_DOCUMENTARY_ACCEPTANCE=OWNER_APPROVED
ISSUE25_QA_PLAN_001=APPROVED_DOCUMENTS_OUT_OF_BAND
ISSUE25_MATRICES_PLAN_002=APPROVED_DOCUMENTS_OUT_OF_BAND
EXECUTOR_HISTORICAL_STATES=BLOCKED_ATTEMPT_2_PRESERVED
WATCHER_ACTIVATION=NOT_AUTHORIZED
P06_REAL_QA_CERTIFICATION=NOT_EXECUTED
QA_MUTATIONS_EXECUTED=NO
BLOCK10_AUTHORIZED=NO
```
