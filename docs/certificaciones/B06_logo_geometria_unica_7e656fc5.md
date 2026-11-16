# Evidencia sintética B06 — geometría única del logo IHSS en PDF

Esta evidencia es técnica, sintética y sanitizada. No contiene secretos, credenciales, dumps, datos de clientes ni trazas operativas.

```ini
PHASE=B06_UNIFIED_PDF_LOGO_GEOMETRY
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
SOURCE_CODE_SHA=7e656fc57548e1d1a2ee5c4ead81455765690f38
EVIDENCE_COMMIT_SHA=767cece877cf17d1c2821739019f658d01e5aa14
LOGO_PATH=frontend/rl-app/public/assets/institutional/ihss-logo.png
LOGO_SHA256=D57799214B441EA834320A1069D46A6930590ED4325F5A5EFAFCC65D115B7543
LOGO_DIMENSIONS=2034x773
LOGO_BASE_WIDTH_MM=55.88
LOGO_UNIFORM_INCREMENT=30_PERCENT
LOGO_PHYSICAL_WIDTH_MM=72.644
LOGO_PHYSICAL_WIDTH_INCHES=2.86
BACKEND_PDF_LOGO_WIDTH_POINTS=205.92
ANGULAR_PDF_LOGO_WIDTH_MM=72.644
LOGO_VERTICAL_ALIGNMENT=HEADER_CENTER
REPORT_METADATA_PLACEMENT=UNDER_TITLE_LEFT
B06_SHARED_LOGO_GEOMETRY=PASS
B06_ALL_SHARED_PDF_CONSUMERS=PASS
B06_CONTINUATION_HEADERS=PASS
B06_EXCEL_HORIZONTAL_SCROLLBAR=PRESERVED
B06_REPORT_DATA_PRESERVED=YES
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
QUALITY_GATE_RUN=43900362542
QUALITY_GATE_STATUS=SUCCESS
B06_LOGO_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE
GITHUB_REMOTE_VERIFIED=YES_AFTER_PUSH
SECURITY_SCAN=PASS_LOCAL_SCOPE
REVIEW_READY=YES_FOR_OWNER_VISUAL_REVIEW
```

## Procedencia y alcance

- Se compararon las dos rutas reales: `institutional-report.util.ts` para Monitoreo de Listas y `InstitutionalPdfDocument.cs` para Matrices de Riesgos.
- La medida común se declaró en los estándares compartidos: `INSTITUTIONAL_PDF_LOGO_WIDTH_MM=72.644` y `InstitutionalReportStandard.PdfLogoWidthPoints=205.92`.
- Se mantuvo el PNG canónico, su SHA-256, el texto de reporte, los identificadores, las fórmulas, los valores persistidos y los contratos REST.
- No se modificaron Oracle institucional, producción, `main`, B07, Bloque 10 ni migración 54.

## Verificaciones reproducidas

| Verificación | Resultado |
|---|---|
| Angular reportería focalizada | PASS — 10/10 |
| Backend logo focalizado | PASS — 4/4 |
| ESLint | PASS |
| Build Angular | PASS; warnings preexistentes SCSS/CommonJS |
| Quality Gate exact-SHA | SUCCESS — [run 43900362542](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43900362542), validado por API |

La captura visual aportada por el propietario es evidencia de la divergencia previa; no se declara aceptación visual automática. Tras reiniciar los servicios locales, cerrar las vistas antiguas y regenerar ambos PDFs, el propietario debe confirmar que el tamaño visible es equivalente.
