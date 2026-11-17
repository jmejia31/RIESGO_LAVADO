# Fase 2A — Monitoreo de Listas: cierre técnico de brechas residuales

## Identificación

```ini
PHASE=MONITOREO_LISTAS_FASE_2A
REPOSITORY=jmejia31/RIESGO_LAVADO
BRANCH=desarrollo
BASE_SHA=624bdeab8ffe02efa46ff4e2993df44a028d2ff8
SOURCE_CODE_SHA=c49ced1c8be524a91ee5311004f19de214721385
AUTHOR=COD/CODEX
CLIENT=CLI
SCOPE=Brechas residuales de identidad, asociación y seguimiento
```

Esta intervención no reconstruye Fase 1, no modifica B01–B06, B07 ni Bloque 10, no escribe en `DNP_IHSS`, no ejecuta Oracle institucional y no modifica la migración 54. Los archivos ajenos preexistentes del checkout no fueron stageados.

## Diagnóstico reproducido

Se confirmó que quedaban cuatro defectos de cierre:

1. La política de presentación marcaba como `VERIFIED` cualquier identificador no vacío aunque no existiera cardinalidad comprobada.
2. La resolución natural contaba el mismo ID numérico de `SOCIOS` y `REPRESENTANTES` como un solo sujeto, ocultando una colisión entre fuentes.
3. Los DTO de natural/empleado tenían valores predeterminados `VERIFIED`, y los positivos/seguimientos de origen `DNP_LISTAS` no exigían verificar nuevamente el sujeto antes de mutar.
4. Un seguimiento podía confirmarse antes de completar el almacenamiento de evidencias; ahora el fallo devuelve error y ejecuta compensación lógica auditada.

## Corrección implementada

- `IdentidadMonitoreoPolicy` ahora requiere `REFERENCIAS > 0` y exactamente un sujeto distinguido para `VERIFIED`; los identificadores sin asociación quedan `UNVERIFIED_ASSOCIATION`.
- Las consultas natural y de resolución de identidad conservan `TIPO_FUENTE` (`SOCIO`/`REPRESENTANTE`) en la cardinalidad distinta.
- El hot path natural agrega la cardinalidad de fuentes sin reintroducir `V_SOCIOS_REPRESENTANTES`; las filas manuales no se presentan como identidad verificada.
- Empleados obtienen cardinalidad explícita del período vigente.
- Los positivos `DNP_LISTAS` y los seguimientos/evidencias DNP validan el tipo de sujeto y bloquean asociaciones ambiguas o no verificadas. Los orígenes manuales, noticia y otro permanecen operables como registros explícitos no atribuidos a DNP.
- El registro de positivos serializa operaciones concurrentes por documento dentro del proceso y usa transacción `Serializable`. La garantía distribuida sigue pendiente de una prueba Oracle autorizada.
- El seguimiento usa el positivo único completo (`PositivoId` y `TipoPositivoId`) y, si falla la evidencia, inactiva el detalle mediante la operación auditada de compensación y responde HTTP 500 controlado; no afirma éxito parcial.
- El contrato Angular incorpora `UNVERIFIED_ASSOCIATION` y estados no operables para la asociación.

## Archivos modificados

- `backend/RL.API/Features/Listas/Contracts/IdentidadMonitoreo.cs`
- `backend/RL.API/Features/Listas/Contracts/ListasModels.cs`
- `backend/RL.API/Features/Listas/Contracts/CatalogoPositivosDto.cs`
- `backend/RL.API/Features/Listas/Persistence/ListasRepository.cs`
- `backend/RL.API/Features/Listas/Application/ListasService.cs`
- `backend/RL.API/Features/Listas/Application/EvidenciasService.cs`
- `backend/RL.API/Shared/Results/ServiceResult.cs`
- `frontend/rl-app/src/app/features/admin/listas/models/listas.models.ts`
- regresiones en `backend/RL.API.Tests/Features/Listas/` y `backend/RL.API.Tests/Infrastructure/`

## Controles

```ini
REPEATED_VALID_IDENTIFIER=PASS_CODE_AND_POLICY_TEST
CROSS_SOURCE_SOCIO_REPRESENTANTE_COLLISION=PASS_CODE_AND_SQL_GUARD
FALSE_VERIFIED_IDENTITY=PASS_CORRECTED
UNVERIFIED_ASSOCIATION_NOT_ATTRIBUTED=PASS_TARGETED_TESTS
DNP_POSITIVE_ASSOCIATION=PASS_TARGETED_TESTS
FOLLOWUP_EVIDENCE_ASSOCIATION=PASS_CODE_REVIEW
EVIDENCE_FAILURE_COMPENSATION=PASS_CODE_PATH
POSITIVE_SAME_PROCESS_CONCURRENCY=PASS_SERIALIZED_GATE_CODE
POSITIVE_DISTRIBUTED_CONCURRENCY=NOT_VERIFIED_ORACLE_REQUIRED
DNP_IHSS_WRITES=0
ORACLE_INSTITUTIONAL_WRITES=0
B01_PRESERVED=YES
B02_PRESERVED=YES
B03_PRESERVED=YES
B04_PRESERVED=YES
B05_PRESERVED=YES
B06_PRESERVED=YES
B07_AUTHORIZED=NO
BLOCK10_AUTHORIZED=NO
MIGRATION_54_TOUCHED=NO
```

## Pruebas ejecutadas

```text
dotnet build backend/RL.API.Tests/RL.API.Tests.csproj --no-restore --configuration Release
Resultado: PASS; 0 errores.

dotnet test backend/RL.API.Tests/RL.API.Tests.csproj --no-build --no-restore --configuration Release --filter "FullyQualifiedName~Features.Listas|FullyQualifiedName~DbPaginationRegressionGuardTests"
Resultado: PASS; 58 passed, 0 failed, 0 skipped.

npm test -- --watch=false
Resultado: PASS; 96 archivos, 1009 tests.

npm run lint
Resultado: PASS.

npm run build
Resultado: PASS; warnings preexistentes de presupuesto SCSS y dependencia CommonJS exceljs.

validate_repository_structure.ps1
validate_database_scripts.ps1
validate_documentation_links.ps1
python tools/validate_agent_skills.py
Resultado: PASS en las cuatro validaciones.

npm run e2e
Resultado: 77 passed, 15 skipped, 2 failed en escenarios de Matrices ajenos a Fase 2A.
El escenario Monitoreo de Listas ejecutado (`monitoreo-listas-motivo-layout.spec.ts`) pasó.
```

La suite completa .NET terminó con `1138 passed`, `2 skipped` y `9 failed` por pruebas Oracle que exigen `Database:Profile=LOCAL_XE` y no tienen ese perfil configurado en este checkout. No se cambió el perfil ni se sustituyó por una base institucional.

## Estado de certificación

```ini
PHASE2A_TARGETED_REGRESSION=PASS
PHASE2A_BUILD_AND_FRONTEND=PASS
PHASE2A_E2E_FOCAL=PASS
PHASE2A_E2E_FULL=PARTIAL_UNRELATED_FAILURES
ORACLE_XE_INTEGRATION=BLOCKED_PROFILE_NOT_CONFIGURED
ORACLE_INSTITUTIONAL_RUNTIME=NOT_EXECUTED
AUDIT_FAILURE_ROLLBACK=NOT_VERIFIED_ORACLE_REQUIRED
QUALITY_GATE=PENDING_NEW_SOURCE_SHA
EVIDENCE_COMMIT_SHA=DOCUMENT_BUNDLE_COMMIT_REPORTED_IN_INDEX
GITHUB_REMOTE_VERIFIED=PENDING_DOCUMENT_BUNDLE_PUSH
READY_FOR_INDEPENDENT_REVIEW=YES_WITH_DOCUMENTED_LIMITATIONS
PHASE2A_TECHNICAL_STATUS=PARTIAL_PENDING_ORACLE_AND_UNRELATED_E2E_REPAIR
PHASE2A_FINAL_STATUS=OPEN
```

No se declara cierre técnico total ni `QUALITY_GATE=SUCCESS` antes de que GitHub ejecute el gate del SHA `c49ced1c` y antes de que se atiendan las pruebas ambientales pendientes. La aceptación del propietario permanece pendiente.
