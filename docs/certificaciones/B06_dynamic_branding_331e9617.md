# Evidencia sintetica B06 — parametrizacion global de identidad institucional

Evidencia tecnica sintetica y sanitizada. No contiene secretos, credenciales, dumps, datos de clientes ni logs operativos.

```ini
PHASE=B06_DYNAMIC_INSTITUTIONAL_BRANDING
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=167d5f72ea20e72000553ad190c7089eaea41a85
SOURCE_CODE_SHA=331e9617c9ee2dba9e008ae3f3b7071b9b8551b5
EVIDENCE_COMMIT_SHA=PENDING_DOCUMENTAL_COMMIT
EVIDENCE_TYPE=PRIMARY_SYNTHETIC_SANITIZED
CANONICAL_LOGO_SHA256=D57799214B441EA834320A1069D46A6930590ED4325F5A5EFAFCC65D115B7543
CANONICAL_LOGO_DIMENSIONS=2034x773
PDF_LOGO_WIDTH_MM=72.644
PDF_LOGO_WIDTH_POINTS=205.92
HEADER_COLOR_VALIDATION=HEX_6_DIGITS
HEADER_FOREGROUND=WCAG_CONTRAST_DERIVED
LOCAL_LOGO_SCOPE=WWWROOT_ONLY
REMOTE_LOGO_FETCH=DISABLED_TO_PREVENT_SSRF
JPG_JPEG_WEBP_UPLOAD=NORMALIZED_TO_PNG_IN_BROWSER
UPLOAD_MAX_BYTES=5242880
PIXEL_LIMIT=16777216
REPORT_DATA_MUTATION=NO
FORMULA_MUTATION=NO
IDENTIFIER_MUTATION=NO
REST_CONTRACT_MUTATION=NO
AUDIT_POLICY=REUSED_CONFIGURATION_IMAGE_OPERATION
ORACLE_SCHEMA_CHANGE=CONTROLLED_SCRIPT_PREPARED_NOT_EXECUTED
B06_BACKEND_REPORTING=PASS
B06_FRONTEND_REPORTING=PASS
B06_PDF_EXCEL_PARITY=PASS
B06_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=OPEN_PENDING_OWNER_ACCEPTANCE
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
QUALITY_GATE_STATUS=PENDING_DOCUMENTAL_COMMIT
GITHUB_REMOTE_VERIFIED=YES_FOR_SOURCE_COMMIT
SECURITY_SCAN=PASS_LOCAL_SCOPE_PENDING_GITHUB_ACTIONS
REVIEW_READY=PENDING_DOCUMENTAL_GATE
```

## Procedencia e implementacion

- Backend: `InstitutionalBrandingSnapshot`, `InstitutionalBrandingResolver`, `InstitutionalPdfDocument`, `InstitutionalXlsxWorkbook` y la configuracion administrativa.
- Frontend: `institutional-report-standard.ts`, utilidades PDF/XLSX, configuracion de identidad y politica de carga institucional.
- Persistencia controlada: `20_b06_dynamic_branding_precheck.sql`, `20_b06_dynamic_branding.sql`, `20_b06_dynamic_branding_postcheck.sql` y `20_b06_dynamic_branding_rollback.sql`.
- El stage funcional fue selectivo. No se incluyeron cambios ajenos del checkout, `main`, produccion, Oracle institucional, migracion 54, B07 ni Bloque 10.

## Limites

La columna Oracle no se ejecuta desde Codex. El backend rechaza URL remota para reportes de servidor por seguridad; una URL externa requiere una integracion aprobada con allowlist y descarga controlada. La aceptacion visual del propietario permanece pendiente y no se sustituye con pruebas unitarias.
