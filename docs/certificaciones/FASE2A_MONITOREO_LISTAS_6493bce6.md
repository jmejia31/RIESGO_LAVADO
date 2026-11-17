# Fase 2A — Monitoreo de Listas: identidad segura y regresión defensiva

## Identificación

```ini
PHASE=MONITOREO_LISTAS_FASE_2A
REPOSITORY=jmejia31/RIESGO_LAVADO
BRANCH=desarrollo
BASE_SHA=97d17366f1566c8c26ddf7c303a79b82b4e708a9
SOURCE_CODE_SHA=6493bce63ae485336c6a232800865a92ebb107a8
AUTHOR=COD/CODEX
CLIENT=CLI
SCOPE=Fase 2A exclusivamente
```

Esta evidencia documenta la corrección compartida del flujo de Monitoreo de Listas. No certifica la ejecución contra Oracle institucional, no ejecuta DDL/DML y no interviene Fase 2B, B07 ni Bloque 10.

## Diagnóstico y corrección

Se corrigieron cuatro riesgos funcionales principales:

1. La asociación de una persona natural podía depender de una vista pesada y de una selección arbitraria del primer candidato. Ahora la consulta caliente usa la agregación `REPORTE_AGG`, conserva los candidatos y expone `ASOCIACIÓN NO VERIFICADA` cuando la relación no es única o el identificador es marcador.
2. Las rutas que recibían el documento directamente podían romperse con identificadores como `N/A(3)`, espacios, `?`, `#` o `/`. Los detalles, positivos y seguimientos usan query parameters y conservan las rutas de mutación existentes.
3. Las operaciones de positivo, motivo, seguimiento y evidencia podían continuar con una asociación ambigua. Ahora la política compartida bloquea identificadores vacíos/marcadores, asociaciones no verificadas y múltiples positivos activos.
4. El frontend podía convertir un error de API en una tabla vacía y permitir exportaciones con cero registros falsos. Ahora conserva la información anterior, presenta el error separado del estado vacío legítimo y bloquea la exportación durante error o carga.

## Archivos funcionales principales

- `backend/RL.API/Features/Listas/Contracts/IdentidadMonitoreo.cs`
- `backend/RL.API/Features/Listas/Contracts/ListasModels.cs`
- `backend/RL.API/Features/Listas/Persistence/IListasRepository.cs`
- `backend/RL.API/Features/Listas/Persistence/ListasRepository.cs`
- `backend/RL.API/Features/Listas/Application/ListasService.cs`
- `backend/RL.API/Features/Listas/Application/EvidenciasService.cs`
- `backend/RL.API/Features/Listas/ListasController.cs`
- `frontend/rl-app/src/app/features/admin/listas/data-access/listas.service.ts`
- `frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.impl.ts`
- `frontend/rl-app/src/app/features/admin/listas/pages/monitoreo-listas/monitoreo-listas.component.html`
- pruebas backend bajo `backend/RL.API.Tests/Features/Listas/` y `backend/RL.API.Tests/Infrastructure/`
- pruebas frontend de Monitoreo de Listas y servicio de datos.

## Controles y estado

```ini
DNP_IHSS_READ_ONLY=PASS_CODE_REVIEW_NO_WRITES
DNP_IHSS_WRITES=0
ORACLE_INSTITUTIONAL_WRITES=0
VALID_IDENTITY_FLOW=PASS_TARGETED_TESTS
AMBIGUOUS_IDENTITY_DETECTION=PASS
AMBIGUOUS_IDENTITY_BLOCKING=PASS
UNVERIFIED_ASSOCIATION_NOT_ATTRIBUTED=PASS
HTTP_SPECIAL_IDENTIFIERS=PASS
POSITIVE_ARBITRARY_SELECTION_REMOVED=PASS
MULTIPLE_POSITIVES_HANDLED_SAFELY=PASS
MOTIVE_ASSOCIATION_SAFETY=PASS
FOLLOWUP_ASSOCIATION_SAFETY=PASS
EVIDENCE_ASSOCIATION_SAFETY=PASS
API_ERROR_NOT_EMPTY=PASS
FALSE_ZERO_REPORTS_PREVENTED=PASS
LEGITIMATE_EMPTY_REPORTS_PRESERVED=PASS
ANGULAR_ROW_IDENTITY=PASS
STALE_REQUEST_PROTECTION=PASS
B06_REPORT_STANDARD=PRESERVED
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

La transaccionalidad Oracle de las mutaciones quedó implementada con la conexión, transacción, auditoría, commit/rollback e invalidación de caché posterior al commit. La evidencia de integración contra Oracle y de fallo inducido de auditoría no fue ejecutada en esta intervención.

```ini
AUDIT_TRANSACTIONALITY=PASS_CODE_REVIEW
AUDIT_FAILURE_ROLLBACK=NOT_VERIFIED_ORACLE_REQUIRED
```

## Pruebas ejecutadas

```text
dotnet build backend/RL.API.Tests/RL.API.Tests.csproj --no-restore --configuration Release --verbosity quiet
Resultado: PASS; 0 errores.

dotnet test backend/RL.API.Tests/RL.API.Tests.csproj --no-build --no-restore --configuration Release --filter "FullyQualifiedName~Features.Listas|FullyQualifiedName~DbPaginationRegressionGuardTests" --verbosity quiet
Resultado: 49 passed, 0 failed, 0 skipped.

npm test -- --watch=false
Resultado: 96 archivos de prueba passed; 1009 tests passed.
```

La suite Angular y el build frontend ya habían sido ejecutados en el mismo estado de código antes del commit; el build frontend terminó correctamente con warnings preexistentes de SCSS/CommonJS. No se reejecutó la suite .NET completa contra una base porque no se dispuso de un Oracle XE descartable autorizado para esta fase y la orden exige mantener DNP_IHSS en lectura.

```ini
BACKEND_TARGETED_TESTS=49_PASS
FRONTEND_TESTS=96_FILES_1009_PASS
PLAYWRIGHT_REAL_AUTHENTICATED=NOT_VERIFIED
ORACLE_XE_INTEGRATION=NOT_RUN_BLOCKED_ENVIRONMENT
OWNER_VISUAL_ACCEPTANCE=PENDING
```

## Dictamen

El código Fase 2A quedó publicado en `desarrollo` en `SOURCE_CODE_SHA`. Los controles estáticos, unitarios y de regresión focalizada están verdes. La certificación técnica completa permanece abierta por las verificaciones que requieren Oracle autorizado, flujo autenticado y navegador real.

```ini
PHASE2A_TECHNICAL_STATUS=PARTIAL_PENDING_ORACLE_AND_E2E
QUALITY_GATE= PENDING_AT_EVIDENCE_CREATION
MONITOREO_LISTAS_FINAL_CERTIFICATION=PENDING
READY_FOR_INDEPENDENT_REVIEW=YES_WITH_DOCUMENTED_LIMITATIONS
```

No se debe interpretar este expediente como aceptación visual del propietario ni como certificación de ejecución en la base institucional.
