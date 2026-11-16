# Evidencia sintética B06 — escala y posición del logo IHSS

```ini
EVIDENCE_TYPE=SANITIZED_TECHNICAL_EVIDENCE
EVIDENCE_ROLE=PRIMARY_LAYOUT_REGRESSION
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=06cdda89832d3cd4e3194cd28756884e9b42b981
SOURCE_CODE_SHA=bd923c7c77dce7404cd52a639d18d1b02354d4ea
EVIDENCE_COMMIT_SHA=PENDING_DOCUMENTAL_COMMIT
LOGO_SCALE=1.40
BACKEND_PDF_LOGO_WIDTH=100.8_POINTS
BACKEND_PDF_LOGO_HEIGHT=ASPECT_RATIO_PRESERVED
ANGULAR_PDF_LOGO_SCALE=1.40
LOGO_VERTICAL_ALIGNMENT=HEADER_CENTER
REPORT_METADATA_PLACEMENT=UNDER_TITLE_LEFT
B06_LOGO_TEXT_OVERLAP=0
B06_ALL_SHARED_PDF_CONSUMERS=PASS
BACKEND_FOCUSED_TESTS=4/4_PASS
ANGULAR_FOCUSED_TESTS=9/9_PASS
ANGULAR_FULL_SUITE=95_FILES_999_TESTS_PASS
ESLINT=PASS
ANGULAR_BUILD=PASS_WITH_PREEXISTING_WARNINGS
SOLUTION_RELEASE_BUILD=PASS_WITH_PREEXISTING_WARNINGS
QUALITY_GATE_RUN=PENDING_REMOTE
QUALITY_GATE_STATUS=PENDING
SECURITY_SCAN=PENDING_GITHUB_ACTIONS
GITHUB_REMOTE_VERIFIED=YES_FOR_FUNCTIONAL_COMMIT
REVIEW_READY=PENDING_QUALITY_GATE
B06_LOGO_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE
```

## Procedencia y alcance

El activo canónico continúa siendo `frontend/rl-app/public/assets/institutional/ihss-logo.png`, procedente del adjunto autorizado por el propietario, con SHA-256 `D57799214B441EA834320A1069D46A6930590ED4325F5A5EFAFCC65D115B7543` y dimensiones `2034x773`.

La corrección se realizó una sola vez en los generadores compartidos de PDF. El backend mueve el metadato debajo del título y calcula el centro vertical del logo dentro del encabezado; Angular aplica la misma escala y evita que el texto entre en la caja blanca. No se alteran datos, fórmulas, identificadores, contratos REST, XLSX, Oracle, auditoría ni autorizaciones.

Esta evidencia es sintética y sanitizada: no contiene secretos, dumps, PII, datos de clientes ni logs operativos. La aprobación estética final del propietario no se infiere de las pruebas automatizadas ni del Quality Gate.
