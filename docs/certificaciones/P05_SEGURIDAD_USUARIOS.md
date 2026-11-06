# P05 — Seguridad del módulo Usuarios

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
