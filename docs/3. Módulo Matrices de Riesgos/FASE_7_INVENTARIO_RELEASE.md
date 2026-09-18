# Fase 7 — Inventario de release de Matrices de Riesgos

## Identidad

- Rama: `desarrollo`
- Baseline: `23637818731046d53c526dfc14cc9193d2b121de`
- Modelo Oracle: `RL_MR_17_TABLES_PLUS_8_CALCULATION_TABLES`
- V1: ID 61, `PUBLISHED`, vigente 1, hash `f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90`.
- V2: ID 63, `DRAFT`, vigente 0, hash `769b5b25cd7cbb03b69782b5864828fb53155c070483b6d22ef5adf36d295651`.

## Inventario operativo

| COMPONENT | PATH | VERSION_SOURCE | DEPLOYMENT_METHOD | DEPENDENCIES | ENVIRONMENT_VARIABLES | SECRETS_REQUIRED | INSTALL_ORDER | ROLLBACK_METHOD | VALIDATION_METHOD |
|---|---|---|---|---|---|---|---|---|---|
| Backend API | `backend/RL.API` | `RL.API.csproj`, `net10.0` | Dockerfile o `dotnet publish` | Oracle, ASP.NET runtime | `ConnectionStrings__OracleDB`, `Jwt__*`, `Cors__AllowedOrigins__0` | Oracle connection, JWT secret | 4 | Redeploy imagen anterior | `/healthz`, `/readyz`, smoke |
| Frontend | `frontend/rl-app` | `package-lock.json`, Angular CLI | Dockerfile/nginx | Node 24.18.0 durante build | API base URL por environment | Ninguno en cliente | 5 | Redeploy imagen anterior | `/healthz`, route fallback |
| Oracle modelo | `database/19_matrices_riesgos` | SQL versionado | SQL*Plus/cliente DBA | `RL_USUARIOS`, `RL_AUDITORIA` | Alias institucional, schema | Credencial DBA fuera del repositorio | 1 | Rollback lógico; nunca borrar V1 | pre/postflight |
| Semillas | `fase11/01_semillas_datos_iniciales_modelo_17_tablas.sql` | Script idempotente autorizado | SQL*Plus con autorización | Modelo Oracle instalado | Schema | Credencial DBA fuera del repositorio | 2 | Transacción/backup institucional | validadores de catálogos |
| Migrador | `tools/MatricesRiesgosMigrator` | Proyecto fuente y XLSX SHA | Ejecución administrativa controlada | Oracle, fuente aprobada | conexión fuera del repo | Credencial DBA fuera del repositorio | 3, solo si se autoriza | rollback/backup y conciliación | dry-run + reconciliación |
| Evidencias | `/app/App_Data/Evidencias` | Configuración `EvidenceStorage` | Volumen persistente del hosting | Permisos de usuario de proceso | path, límite de archivo | Ninguno | 6 | Restaurar volumen/backup | validación de ruta/tamaño |
| Reporting/export | Backend `Features/MatricesRiesgos` | Código publicado | Incluido en API | Oracle, ReportPreview | límites de upload/report | Ninguno | 4 | Redeploy API | preview y descarga |
| Logging | Serilog/appsettings | `appsettings*.json` | stdout + `/app/logs` | permisos filesystem | niveles de log | Ninguno | 4 | restaurar configuración | revisión de logs |
| Health/readiness | `Infrastructure/Health` | código backend + Docker HEALTHCHECK | endpoint HTTP | Oracle para readiness | `Health:OracleTimeoutSeconds` | Connection string | 4 | imagen anterior | HTTP 200/503 esperado |
| CI/CD | `.github/workflows` | workflow versionado | GitHub Actions | .NET, Node, Sonar según workflow | secretos del repositorio | GitHub secrets | 7 | revert/redeploy SHA anterior | Quality Gate SHA exacto |
| Documentación | `docs/3. Módulo Matrices de Riesgos` | Markdown versionado | release package | repositorio | ninguna | ninguna | 7 | revert commit | enlaces/documentation validator |

## Orden y restricciones

1. Respaldar y ejecutar preflight read-only.
2. Validar dependencias institucionales y el estado actual.
3. Instalar/actualizar únicamente el delta aprobado; los maestros Oracle no invocan la transición destructiva del modelo.
4. Publicar backend/frontend con secretos inyectados por el entorno.
5. Ejecutar postflight y smoke tests.
6. V2 permanece `DRAFT`, vigente 0; nunca es publicada por Fase 7.

Los placeholders `GTIC` y `MITIGAR` siguen siendo `TEST_DATA_ONLY` y bloquean la aceptación productiva si no existe decisión institucional.
