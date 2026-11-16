# B06 — publicación de logo local en reportería

Evidencia técnica sintética y sanitizada. No contiene secretos, credenciales, dumps, datos de clientes ni URLs operativas completas.

```ini
PHASE=B06_DYNAMIC_INSTITUTIONAL_BRANDING_CORRECTION
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=f90d0815dd2bfdc5a6c9d733e0255e05c04d75e6
SOURCE_CODE_SHA=f1d42079bc549ded50a9d245fc1132fc5da2e08d
EVIDENCE_COMMIT_SHA=286689c9328aad2b84594f00dd86333311fd33c2
EVIDENCE_TYPE=PRIMARY_SYNTHETIC_SANITIZED
ROOT_CAUSE_LOCAL_UPLOAD=FRONTEND_ACCEPTED_UNSUPPORTED_PNG_VARIANTS
ROOT_CAUSE_REPORTING_FALLBACK=CONFIGURED_ASSET_ERROR_WAS_HIDDEN_BY_CANONICAL_FALLBACK
LOCAL_STORAGE_ROOT=WEB_ROOT_PATH_UPLOADS
LOCAL_ASSET_STATIC_HTTP=PASS_200_IMAGE_PNG
LOCAL_LOGO_RESOLVER=PASS_CONFIGURED_LOCAL_FILE
REMOTE_SERVER_FETCH=BLOCKED_SSRF_POLICY
REMOTE_CONFIG_CURRENT_STATE=EXTERNAL_URL_PRESENT_AT_VERIFICATION_TIME
REMOTE_URL_REPORTING=BLOCKED_EXPLICIT_B06_BRANDING_LOGO_NOT_READY
FRONTEND_IMAGE_NORMALIZATION=PNG_CANONICAL_REENCODE
PDF_REPORTING=FAIL_CLOSED_WHEN_CONFIGURED_ASSET_UNAVAILABLE
XLSX_PREVIEW_LOGO_SOURCE=CONFIGURATION_SERVICE_NOT_CANONICAL_CONSTANT
BACKEND_REGRESSION=PASS_9_TESTS
FRONTEND_TESTS=PASS_96_FILES_1007_TESTS
FRONTEND_LINT=PASS
FRONTEND_BUILD=PASS_WITH_PREEXISTING_WARNINGS
BACKEND_BUILD=PASS_0_ERRORS
TEXT_ENCODING_VALIDATION=PASS
ORACLE_B06_PRECHECK=PASS
ORACLE_B06_POSTCHECK=PASS_COLUMN_PRESENT_DEFAULT_123B63
ORACLE_DATA_CHANGE_SCOPE=ADDITIVE_B06_COLUMN_ONLY
REAL_ADMIN_LOGO_A_LOGO_B_FLOW=NOT_VERIFIED_AUTHENTICATED
REAL_PDF_EXCEL_FLOW=NOT_VERIFIED_AUTHENTICATED
PLAYWRIGHT_REAL_CONFIG_FLOW=BLOCKED_BROWSER_AUTHENTICATION
QUALITY_GATE_STATUS=SUCCESS
QUALITY_GATE_RUN=43955398124
QUALITY_GATE_URL=https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43955398124
SECURITY_SCAN=PASS_GITHUB_ACTIONS
GITHUB_REMOTE_VERIFIED=YES_SOURCE_AND_EVIDENCE_SHA
REVIEW_READY=YES_FOR_OWNER_VISUAL_REVIEW
B06_TECHNICAL_STATUS=PARTIAL_PENDING_AUTHENTICATED_E2E
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
MIGRATION_54_TOUCHED=NO
```

## Defecto reproducido y corrección

El enlace remoto se mostraba en el navegador porque Angular podía cargarlo directamente, pero el backend de reportería lo rechazaba correctamente por política anti-SSRF y luego ocultaba el motivo usando el logo canónico. El flujo local además podía conservar PNGs que el decodificador documental no podía interpretar.

La corrección centraliza la raíz `wwwroot/uploads`, rechaza de forma explícita un logo configurado que no pueda resolverse, convierte las imágenes seleccionadas por el frontend a un PNG compatible antes de subirlas y elimina el logo canónico fijo de la vista previa Excel. PDF y reportería devuelven un 503 con código B06 cuando existe una configuración de logo no utilizable; ya no reportan éxito visual con un logo distinto.

El mensaje `LOGO_UPLOADED_PENDING_SAVE` continúa siendo correcto después de arrastrar un archivo: significa que la carga terminó, pero todavía falta pulsar Guardar para publicar la configuración. La publicación válida debe terminar con la lectura posterior de la configuración y una ruta local `/uploads/...`.

## Límites de evidencia

La configuración consultada durante esta intervención tenía una URL externa persistida. No se sustituyó directamente en Oracle porque eso saltaría el flujo administrativo auditado. La prueba autenticada completa `carga local → guardar → releer → PDF → XLSX → preview` y la aceptación visual del propietario siguen pendientes. El enlace externo no es el único flujo soportado: no es una fuente válida para la reportería de servidor; el flujo soportado es carga local, publicación y persistencia de una ruta bajo `wwwroot/uploads`.

El Quality Gate exacto del código terminó correctamente en el run [`43955398124`](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43955398124). El Quality Gate exacto del expediente documental terminó correctamente en [`43958425555`](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43958425555). No se declara PASS global mientras falte la integración autenticada.
