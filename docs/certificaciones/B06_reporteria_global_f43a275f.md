# Evidencia sintética B06 — reportería global

```ini
EVIDENCE_TYPE=SANITIZED_TECHNICAL_EVIDENCE
EVIDENCE_ROLE=PRIMARY_TEST_AND_SOURCE_AUDIT_SUMMARY
AUTHOR=COD/CODEX
BRANCH=desarrollo
SOURCE_CODE_SHA=f43a275fa04d7b878908b0ade92e0ecff0cb19b4
EVIDENCE_COMMIT_SHA=PENDING_DOCUMENTAL_COMMIT
PROVENANCE=local checkout plus GitHub Actions exact-SHA run
QUALITY_GATE_SHA=d2d31a7bb2f6667d0c5da30bb95c7959c701fc3a
QUALITY_GATE_RUN=43498122123
QUALITY_GATE_STATUS=completed
QUALITY_GATE_CONCLUSION=success
SECURITY_SCAN=PASS_GITHUB_ACTIONS
GITHUB_REMOTE_VERIFIED=YES_FOR_FUNCTIONAL_COMMIT_AND_GREEN_GATE
REVIEW_READY=YES_FOR_TECHNICAL_REVIEW
```

## Objetivo

Verificar que PDF, XLSX y preview comparten reglas institucionales de encabezado, densidad, envoltura, roles y descarga sin cambiar datos ni contratos.

## Cambios verificados

- `InstitutionalReportStandard` define orientación y densidad de grillas clave/valor; Matrices de 82 campos usa paisaje y tres columnas.
- `InstitutionalPdfDocument` calcula altura de encabezado y envuelve institución, título, módulo, fecha, etiquetas y valores.
- `InstitutionalXlsxWorkbook` usa la paleta compartida y alturas explícitas para título, metadatos y cabeceras.
- `institutional-report.util.ts` conserva filtros completos y calcula el inicio real del contenido.
- `excel-export.util.ts` propaga `contextRowRoles`; el preview no concatena filas con `|`.
- `report-preview.component.html` renderiza roles y envuelve celdas sin eliminar columnas.
- Los consumidores de fichas de monitoreo usan el inicio retornado por el encabezado y no coordenadas fijas.

## Resultados reproducidos

```ini
BACKEND_FOCUSED_TESTS=15/15 PASS
ANGULAR_TESTS=997/997 PASS
ESLINT=PASS
ANGULAR_BUILD=PASS_WITH_PREEXISTING_WARNINGS
PLAYWRIGHT_CHROMIUM=1/1 PASS
PLAYWRIGHT_VIEWPORTS=320x568,390x844,375x812,768x1024,1024x768,1280x720,1366x768,1536x1024,1920x1080
B06_REQUIRED_VIEWPORTS=390x844,768x1024,1366x768,1536x1024
PDF_FIELD_STACKING=0
EXCEL_CONTEXT_PIPE_FLATTENING=0
GLOBAL_HORIZONTAL_OVERFLOW=0
ORACLE_INSTITUTIONAL=NOT_EXECUTED
LOGO_AUTHORIZED_ASSET=NOT_VERIFIED
```

## Límites y custodia

Esta evidencia es sintética y sanitizada. No contiene dumps, secretos, datos de clientes, credenciales, trazas operativas ni archivos institucionales restringidos. La prueba visual valida el preview real y sus límites de viewport; la aprobación estética final queda `CHANGES_REQUESTED` hasta revisión del propietario. El logo IHSS continúa `ACCESS_RESTRICTED/NOT_VERIFIED` y no fue fabricado, descargado ni extraído de capturas.

El gate previo `43497280782` quedó fallido por una expectativa heredada de orientación vertical en una prueba de contrato; no por una falla de la corrección global. La expectativa fue alineada en `d2d31a7bb2f6667d0c5da30bb95c7959c701fc3a` y el gate exacto `43498122123` validó el SHA correctivo en verde.
