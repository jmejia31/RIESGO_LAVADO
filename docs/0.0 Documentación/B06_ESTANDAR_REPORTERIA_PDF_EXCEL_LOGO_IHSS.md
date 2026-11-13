# B06 — Estándar de reportería institucional PDF/Excel y logo IHSS

## Expediente y alcance

Este documento registra la auditoría e implementación autorizada de B06 en `desarrollo`. El objetivo es consolidar el inventario real de generadores activos, preservar la paridad PDF/Excel, hacer explícitos los reportes sin información y verificar la procedencia del logo institucional sin fabricar ni incorporar activos no autorizados.

```ini
PHASE=B06_REPORTERIA_PDF_EXCEL_LOGO_IHSS
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=2d6c07b0c9bfc1b9666489d8ecbf74b8cd1e92c4
SOURCE_CODE_SHA=3400fadb1e942cd4c8e948a5c91bb92ebf2cb83f
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
EVIDENCE_COMMIT_SHA=PENDING_DOCUMENTAL_COMMIT
QUALITY_GATE_SHA=PENDING_DOCUMENTAL_COMMIT
GITHUB_REMOTE_VERIFIED=PENDING_DOCUMENTAL_COMMIT
SECURITY_SCAN=PENDING_GITHUB_ACTIONS
REVIEW_READY=PENDING_GITHUB_ACTIONS
```

B06 no se cierra con este expediente: queda abierto para la entrega/autorización del logo y la aceptación visual del propietario. B07 y Bloque 10 permanecen fuera de alcance.
