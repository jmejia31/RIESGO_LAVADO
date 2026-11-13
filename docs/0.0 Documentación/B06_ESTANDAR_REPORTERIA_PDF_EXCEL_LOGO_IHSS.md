# B06 — Estándar de reportería institucional PDF/Excel y logo IHSS

## Actualización global B06 — 2026-11-13

La intervención funcional `f43a275fa04d7b878908b0ade92e0ecff0cb19b4` corrige el estándar compartido de reportería, no un reporte aislado. El PDF backend dejó de forzar la matriz de 82 campos a una sola columna: la orientación y la densidad de la grilla etiqueta/valor se resuelven desde `InstitutionalReportStandard`, y el encabezado calcula alto y saltos de línea. El workbook OpenXML reutiliza la paleta institucional y alturas de fila con envoltura para títulos, metadatos y cabeceras.

En Angular, `institutional-report.util.ts` centraliza el encabezado PDF adaptable y conserva todos los filtros; `excel-export.util.ts` conserva roles de contexto y paleta; y `report-preview.component.*` presenta `TITLE`, `SECTION`, `KEY_VALUE`, `PARAGRAPH`, `DATA` y `SPACER` sin concatenar filas con `|`. Los productores de fichas de monitoreo dejaron de depender de coordenadas fijas después del encabezado. No se modificaron valores, identificadores, fórmulas, contratos REST, Oracle, auditoría, autorizaciones, producción, `main`, B07, Bloque 10 ni migración 54.

```ini
PHASE=B06_GLOBAL_REPORTING_STANDARD
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=3adbf209be5f0ac294b3804af9f25e1515157e77
SOURCE_CODE_SHA=f43a275fa04d7b878908b0ade92e0ecff0cb19b4
FUNCTIONAL_COMMIT=f43a275fa04d7b878908b0ade92e0ecff0cb19b4
DOCUMENTAL_COMMIT=PENDING_DOCUMENTAL_COMMIT
ACTIVE_BACKEND_EXPORTER_FAMILIES=1
ACTIVE_ANGULAR_REPORT_GENERATOR_FAMILIES=2
ACTIVE_SHARED_PREVIEW_CONSUMERS=1
B06_PDF_LAYOUT_SHARED=PASS
B06_XLSX_LAYOUT_SHARED=PASS
B06_PREVIEW_ROLE_RENDERING=PASS
B06_PDF_82_FIELDS_VERTICAL_STACK=0
B06_EXCEL_CONTEXT_PIPE_FLATTENING=0
B06_FORMULA_VALUES_PRESERVED=YES
B06_IDENTIFIERS_PRESERVED=YES
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

### Verificación técnica y visual reproducida

| Verificación | Resultado | Alcance/evidencia |
|---|---|---|
| Backend reportería focalizada | PASS | 15/15: estándar, orientación, exportación Matrices y contrato de 82 campos |
| Suite Angular | PASS | 997/997 pruebas |
| ESLint | PASS | `npm run lint` |
| Build Angular | PASS | `npm run build`; solo warnings preexistentes de presupuesto SCSS/CommonJS |
| Playwright Chromium | PASS | 1/1; preview PDF/XLSX, geometría y overflow global en 320, 390, 375, 768, 1024, 1280, 1366, 1536 y 1920 px |
| Diff selectivo | PASS | `git diff --check` en el alcance B06 |
| Oracle institucional | NOT_EXECUTED | No requerido para esta corrección visual; no se sustituyó por una afirmación documental |
| Logo IHSS autorizado | BLOCKED | No existe activo verificable en el checkout; se conserva el bloqueo seguro histórico |

La prueba Chromium verifica el comportamiento real del preview y su geometría; no se presenta como aprobación visual del propietario ni como certificación tipográfica de un PDF generado por Oracle. Las capturas aportadas por el propietario permanecen como evidencia de antes; la evidencia posterior reproducible está sintetizada en [`docs/certificaciones/B06_reporteria_global_f43a275f.md`](../certificaciones/B06_reporteria_global_f43a275f.md).

### Estado de revisión

```ini
B06_OWNER_VISUAL_ACCEPTANCE=CHANGES_REQUESTED
B06_PLAYWRIGHT_VISUAL=PASS
B06_TECHNICAL_STATUS=PARTIAL_BLOCKED_LOGO_AUTHORIZATION
B06_FINAL_STATUS=OPEN_PENDING_OWNER_REVIEW
EVIDENCE_COMMIT_SHA=PENDING_DOCUMENTAL_COMMIT
QUALITY_GATE_RUN=43498122123
QUALITY_GATE_SHA=d2d31a7bb2f6667d0c5da30bb95c7959c701fc3a
QUALITY_GATE_STATUS=completed
QUALITY_GATE_CONCLUSION=success
GITHUB_REMOTE_VERIFIED=YES_FOR_FUNCTIONAL_COMMIT_AND_GREEN_GATE
SECURITY_SCAN=PASS_GITHUB_ACTIONS
REVIEW_READY=YES_FOR_TECHNICAL_REVIEW
GITHUB_EVIDENCE_INDEX_URL=PENDING_DOCUMENTAL_COMMIT
GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/40
```

El primer gate del cambio funcional, `43497280782`, falló únicamente porque una regresión heredada todavía esperaba la orientación vertical anterior para el PDF de Matrices. El contrato fue actualizado en `d2d31a7bb2f6667d0c5da30bb95c7959c701fc3a`; el gate exacto del SHA correctivo, `43498122123`, terminó `completed/success` con la matriz integral verde.

El gate técnico y el expediente no cierran B06: falta la aceptación visual del propietario y permanece bloqueada la incorporación del logo hasta recibir un activo autorizado con procedencia verificable.

## Expediente y alcance

Este documento registra la auditoría e implementación autorizada de B06 en `desarrollo`. El objetivo es consolidar el inventario real de generadores activos, preservar la paridad PDF/Excel, hacer explícitos los reportes sin información y verificar la procedencia del logo institucional sin fabricar ni incorporar activos no autorizados.

```ini
PHASE=B06_REPORTERIA_PDF_EXCEL_LOGO_IHSS
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=2d6c07b0c9bfc1b9666489d8ecbf74b8cd1e92c4
SOURCE_CODE_SHA=3400fadb1e942cd4c8e948a5c91bb92ebf2cb83f
REGRESSION_FIX_SHA=3d8cc1aeaf27cda48d74c6cf90061bac795499a5
B05_PRESERVED=YES
B07_AUTHORIZED=NO
BLOCK10_AUTHORIZED=NO
MAIN_TOUCHED=NO
PRODUCTION_TOUCHED=NO
ORACLE_INSTITUTIONAL_TOUCHED=NO
MIGRATION_54_TOUCHED=NO
```

El alcance no modifica fórmulas, valores persistidos, contratos REST, autorizaciones, auditoría ni Oracle institucional. La corrección funcional de esta intervención solo mejora la representación de un resultado vacío en los exportadores activos de Matrices de Riesgos.

## Inventario activo verificado

| Superficie | Generador activo | Formato | Resultado de auditoría |
|---|---|---|---|
| Matrices de Riesgos | `MatricesRiesgosReportesController` + `MatricesRiesgosReportExportService` | PDF/XLSX | Activo; fuente backend única y proyección canónica de 82 campos |
| PDF institucional | `InstitutionalPdfDocument` | PDF | Activo; encabezado, secciones, tablas, paginación y `application/pdf` |
| Excel institucional | `InstitutionalXlsxWorkbook` | XLSX/OpenXML | Activo; hoja `Matriz Consolidada`, encabezados canónicos y MIME OpenXML |
| Monitoreo y listas | `institutional-report-parity.util.ts` + `excel-export.util.ts` | XLSX/PDF/preview | Activo en Angular; reutiliza las utilidades compartidas y el preview institucional |
| Vista previa compartida | `report-preview.service.ts` + `report-preview.component.*` | Blob PDF/XLSX | Activo; la descarga usa el mismo blob que se abrió en preview |

Los inventarios históricos de las fases anteriores se trataron como documentación de referencia y no como generadores activos. No se creó un renderer paralelo ni se reconstruyeron datos en el cliente.

## Implementación B06 publicada

En `MatricesRiesgosReportExportService`:

- El XLSX vacío conserva la hoja institucional, los 82 encabezados y una fila explícita con `Sin información` en la primera columna.
- El PDF vacío conserva el documento válido y agrega la sección institucional `SIN INFORMACIÓN`, respetando el comportamiento de títulos del generador PDF.
- Los reportes con datos mantienen el mapeo canónico existente, el orden de campos, las validaciones fail-closed y los identificadores de las proyecciones.

La regresión `MatricesRiesgosPhase11ReportExportTests` cubre el workbook OpenXML de 82 columnas, el PDF válido, el mensaje vacío y el rechazo de proyecciones faltantes. El cambio funcional está publicado en `3400fadb1e942cd4c8e948a5c91bb92ebf2cb83f`.

## Verificaciones ejecutadas en esta intervención

| Verificación | Resultado | Evidencia |
|---|---|---|
| Regresión backend B06 focalizada | PASS | 6/6 pruebas: Phase11 + BatchExport |
| Regresión frontend de reportería | PASS | 62/62 pruebas en 6 archivos focalizados |
| ESLint Angular | PASS | `npm run lint` |
| Build Angular | PASS | `npm run build`; warnings de presupuesto SCSS/CommonJS preexistentes, sin error |
| Diff de archivos B06 | PASS | `git diff --check` sobre los dos archivos funcionales |
| Validación de skills en commit | PASS | Hook institucional: 14 skills |
| Oracle institucional | NOT_EXECUTED | Fuera de alcance; no se alteró ni se sustituyó por una afirmación documental |
| Pruebas de rendimiento | NOT_EXECUTED | Requieren entorno y datos operativos aprobados |
| Inspección visual del logo en PDF/XLSX | BLOCKED | No existe activo oficial autorizado verificable en el checkout |

El `git diff --check` global no se presenta como PASS porque el checkout ya contenía archivos ajenos modificados con CRLF/trailing whitespace (`Program.cs`, scripts P06 y otros). Esos archivos no fueron stageados ni corregidos.

## Contratos y estados B06

```ini
B06_ACTIVE_REPORT_GENERATORS=BACKEND_MATRICES,ANGULAR_LISTAS,SHARED_PREVIEW
B06_82_FIELD_CONTRACT=PASS
B06_XLSX_OPENXML=PASS
B06_PDF_VALID=PASS
B06_EMPTY_REPORTS=PASS
B06_PREVIEW_DOWNLOAD_SAME_ARTIFACT=PASS
B06_SECURITY_AUDIT_CONTENT_TYPES=PASS_BY_CONTROLLER_AND_REGRESSION_REVIEW
B06_PERFORMANCE=NOT_EXECUTED
B06_EXECUTIVE_7_SECTIONS=NOT_VERIFIED_ACTIVE_GENERATOR
B06_FORMULA_VALUES_PRESERVED=YES
B06_IDENTIFIERS_PRESERVED=YES
B06_BACKEND_CONTRACTS_PRESERVED=YES
B06_ORACLE_PRESERVED=YES
```

La paridad de datos de los reportes con información se conserva mediante la misma proyección canónica de 82 campos y las pruebas existentes; la verificación Oracle de integración queda fuera de esta ejecución local y no se declara como reproducida.

## Logo IHSS: bloqueo seguro y trazable

No se encontró en el checkout un archivo oficial IHSS con procedencia y autorización verificables. Los recursos existentes bajo `frontend/rl-app/public/assets/login/` son imágenes de la pantalla de acceso y no fueron reutilizados como logo institucional. Tampoco se descargó un logo de internet, se usó una captura ni se generó un sustituto.

```ini
IHSS_LOGO_SOURCE=NOT_VERIFIED
IHSS_LOGO_AUTHORIZATION=NOT_VERIFIED
IHSS_LOGO_ASSET_STATUS=BLOCKED_AUTHORIZED_ASSET
IHSS_LOGO_PDF=BLOCKED
IHSS_LOGO_XLSX=BLOCKED
IHSS_LOGO_PROPORTIONS=BLOCKED
IHSS_LOGO_PRINT_QUALITY=BLOCKED
```

Para convertir este punto en `PASS` se requiere que el propietario o custodio autorizado entregue el activo institucional y su procedencia. Hasta entonces el estado máximo es técnico parcial; no se oculta el bloqueo ni se publica un activo dudoso.

## Estado de cierre

```ini
B06_TECHNICAL_STATUS=PARTIAL_BLOCKED_LOGO_AUTHORIZATION
B06_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=OPEN_PENDING_OWNER_ACCEPTANCE
EVIDENCE_COMMIT_SHA=30b92f83e4354caa660b020b2389975cadec0edb
QUALITY_GATE_RUN=43481762215
QUALITY_GATE_SHA=3d8cc1aeaf27cda48d74c6cf90061bac795499a5
QUALITY_GATE_STATUS=completed
QUALITY_GATE_CONCLUSION=success
DOCUMENTATION_QUALITY_GATE_RUN=43484290449
DOCUMENTATION_QUALITY_GATE_SHA=2eea92640d3c19fea9a919e60fb35dab341fd875
DOCUMENTATION_QUALITY_GATE_STATUS=completed
DOCUMENTATION_QUALITY_GATE_CONCLUSION=success
GITHUB_REMOTE_VERIFIED=YES
SECURITY_SCAN=PASS_GITHUB_ACTIONS
REVIEW_READY=YES_FOR_TECHNICAL_REVIEW
```

B06 no se cierra con este expediente: queda abierto para la entrega/autorización del logo y la aceptación visual del propietario. B07 y Bloque 10 permanecen fuera de alcance.

## Verificación remota del código y regresión

- El primer gate documental sobre `30b92f83e4354caa660b020b2389975cadec0edb` quedó `failure` exclusivamente porque la regresión E2E de B05 leía la grilla antes del reflow Angular. No se usó como certificación.
- La espera de reflow se corrigió de forma aislada en `3d8cc1aeaf27cda48d74c6cf90061bac795499a5` y el caso Chromium pasó `1/1` localmente.
- Quality Gate exacto de código, pruebas y expediente previo: [run 43481762215](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43481762215), `STATUS=completed`, `CONCLUSION=success`; ambos jobs y todos los pasos integrales terminaron verdes.
- El commit documental final `2eea92640d3c19fea9a919e60fb35dab341fd875` quedó publicado en `origin/desarrollo` y su Quality Gate [43484290449](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43484290449) terminó `STATUS=completed`, `CONCLUSION=success`.
- La referencia remota `desarrollo` y el checkout local tienen el mismo SHA (`2eea92640d3c19fea9a919e60fb35dab341fd875`); GitHub API devolvió el expediente B06 y su URL verificable.
