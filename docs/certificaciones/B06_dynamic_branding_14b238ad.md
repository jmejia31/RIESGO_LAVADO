# B06 — corrección integral de publicación institucional

Evidencia técnica sintética y sanitizada. No contiene secretos, credenciales, dumps, datos de clientes ni logs operativos.

```ini
PHASE=B06_DYNAMIC_INSTITUTIONAL_BRANDING_CORRECTION
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=bd1deb43e8996efa6fc6a5690dcd005e56c5a778
SOURCE_CODE_SHA=510d4bea75f8ca5bd3ac42d470cfe7e4fac97880
EVIDENCE_COMMIT_SHA=PENDING_DOCUMENTAL_COMMIT
EVIDENCE_TYPE=PRIMARY_SYNTHETIC_SANITIZED
ROOT_CAUSE_STORAGE_PATH=UPLOAD_USED_CURRENT_DIRECTORY_WHILE_RESOLVER_USED_CONTENT_ROOT
ROOT_CAUSE_FALSE_SUCCESS=SECOND_CONFIGURATION_UPDATE_ALLOWED_PARTIAL_PERSISTENCE
ROOT_CAUSE_SECURITY_OVERWRITE=OMITTED_SECURITY_FIELDS_COULD_BE_DESERIALIZED_WITH_DEFAULTS
ROOT_CAUSE_AUDIT_ATOMICITY=CONFIGURATION_AND_AUDIT_USED_SEPARATE_COMMITS
LOGO_UPLOAD_STATE_MACHINE=EXPLICIT
LOCAL_LOGO_ROOT=WEB_ROOT_PATH
REMOTE_LOGO_FETCH=DISABLED_TO_PREVENT_SSRF
SECURITY_FIELDS_PRESERVED=YES
AUDIT_AND_CONFIGURATION_TRANSACTION=IMPLEMENTED
HEADER_FOREGROUND=WCAG_CONTRAST_DERIVED
HEADER_COLOR_CASES=#FFFFFF,#000000,#123B63,#777777,#767676,#F3F6F9
PDF_LOGO_GEOMETRY=SHARED_POLICY
XLSX_LOGO_GEOMETRY=SHARED_POLICY
ALPHA_COMPOSITING=WHITE_BACKGROUND
FRONTEND_TESTS=PASS_96_FILES_1007_TESTS
FRONTEND_BUILD=PASS_WITH_PREEXISTING_WARNINGS
FRONTEND_LINT=PASS
BACKEND_BUILD=PASS_0_ERRORS_505_INHERITED_ANALYZER_WARNINGS
BACKEND_FULL_TEST_SUITE=9_ENVIRONMENTAL_FAILURES_1113_PASS_2_SKIPPED
ORACLE_XE_INTEGRATION=NOT_EXECUTED_THIS_INTERVENTION
REAL_ADMIN_LOGO_A_LOGO_B_FLOW=NOT_VERIFIED
REAL_PDF_EXCEL_PREVIEW_FLOW=NOT_VERIFIED
PLAYWRIGHT_REAL_CONFIG_FLOW=NOT_VERIFIED
QUALITY_GATE_STATUS=PENDING_NEW_SOURCE_SHA
SECURITY_SCAN=PENDING_GITHUB_ACTIONS
GITHUB_REMOTE_VERIFIED=YES_FOR_SOURCE_COMMIT
REVIEW_READY=BLOCKED_ENVIRONMENTAL_VERIFICATION
B06_TECHNICAL_STATUS=BLOCKED
B06_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=BLOCKED_PENDING_REAL_FLOW_AND_OWNER_ACCEPTANCE
B01_PRESERVED=YES
B02_PRESERVED=YES
B03_PRESERVED=YES
B04_PRESERVED=YES
B05_PRESERVED=YES
B07_AUTHORIZED=NO
BLOCK10_AUTHORIZED=NO
MAIN_TOUCHED=NO
PRODUCTION_TOUCHED=NO
ORACLE_INSTITUTIONAL_TOUCHED=NO
MIGRATION_54_TOUCHED=NO
```

## Alcance implementado

- El almacenamiento de imágenes usa el `WebRootPath` del host y el resolver de reportería usa el mismo raíz efectiva; Angular resuelve las URLs por el servicio institucional.
- La publicación de configuración y auditoría usa una transacción conjunta en la ruta productiva. Los campos de seguridad omitidos se preservan y el contrato GET expone `validezClaveTemp`.
- El estándar compartido calcula foreground accesible, usa la paleta en PDF/XLSX/Angular, conserva proporciones, aumenta la geometría Excel de forma uniforme y compone PNG con alfa sobre blanco.
- La interfaz distingue selección, validación, carga, publicación pendiente, guardado, publicación confirmada y error; el mensaje de éxito se emite solo después de releer la configuración persistida.
- Se agregó regresión de contraste institucional y se preservaron consumidores existentes mediante los helpers compartidos.

## Verificaciones ejecutadas en esta intervención

| Verificación | Resultado | Evidencia |
|---|---|---|
| `npm test -- --watch=false` | PASS | 96 archivos, 1007 pruebas |
| `npm run lint` | PASS | exit code 0 |
| `npm run build` | PASS | bundle generado; warnings de presupuesto SCSS/CommonJS preexistentes |
| `dotnet build backend/RL.API/RL.API.csproj --no-restore` aislado | PASS | 0 errores; 505 warnings heredados |
| `git diff --cached --check` | PASS | stage funcional selectivo |
| Suite .NET completa | BLOCKED_ENVIRONMENTAL | 1124 total; 1113 passed; 2 skipped; 9 fallas por falta de `Database:Profile=LOCAL_XE` |
| Oracle institucional / migración | NOT_EXECUTED | prohibido sin DBA; no se ejecutó DDL/DML institucional |
| Playwright real Configuración→reporte | NOT_VERIFIED | requiere entorno autenticado y base descartable |
| LOGO_A/LOGO_B end-to-end | NOT_VERIFIED | no se inventa evidencia sintética de integración |

## Límites y dictamen

El commit funcional vigente es [`510d4bea`](https://github.com/jmejia31/RIESGO_LAVADO/commit/510d4bea75f8ca5bd3ac42d470cfe7e4fac97880), precedido por [`14b238ad`](https://github.com/jmejia31/RIESGO_LAVADO/commit/14b238ad8ef8533564012113cec621423757e821). La suite local demuestra compilación y regresión Angular, pero no demuestra por sí sola la publicación real de dos logos, la persistencia Oracle ni la paridad de documentos generados. Por eso el estado máximo de esta intervención es `BLOCKED`, no `PASS`.

La aceptación visual del propietario permanece pendiente. No se modifica `main`, producción, Oracle institucional, migración 54, B07, Bloque 10 ni se incluyen cambios ajenos del checkout.
