# Evidencia sintética B06 — logo IHSS global en reportería

```ini
EVIDENCE_TYPE=SANITIZED_TECHNICAL_EVIDENCE
EVIDENCE_ROLE=PRIMARY_ASSET_AND_TEST_SUMMARY
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
SOURCE_CODE_SHA=15ae2ab7ce220bcdc1b0f3d5343cf2f168c802f5
REGRESSION_FIX_SHA=40199d45718b24279701d006c1bb6bd8451a1ad3
CONTAINER_CONTEXT_FIX_SHA=e9f9e95759c3f717c9950ae4d6a8a28d5e690a58
CERTIFIED_HEAD_SHA=e9f9e95759c3f717c9950ae4d6a8a28d5e690a58
EVIDENCE_COMMIT_SHA=39a31d36629217c711939f0d459a83f375bae177
LOGO_REPOSITORY_PATH=frontend/rl-app/public/assets/institutional/ihss-logo.png
LOGO_SHA256=D57799214B441EA834320A1069D46A6930590ED4325F5A5EFAFCC65D115B7543
LOGO_DIMENSIONS=2034x773
LOGO_PROVENANCE=OWNER_ATTACHED_AUTHORIZED
LOGO_FORMAT=PNG_32BPP_ARGB
BACKEND_FOCUSED_TESTS=3/3 PASS
ANGULAR_FOCUSED_TESTS=23/23 PASS
ANGULAR_FULL_SUITE=95_FILES_999_TESTS_PASS
ESLINT=PASS
ANGULAR_BUILD=PASS_WITH_PREEXISTING_WARNINGS
SOLUTION_RELEASE_BUILD=PASS
QUALITY_GATE_RUN=43519183344
QUALITY_GATE_STATUS=completed
QUALITY_GATE_CONCLUSION=success
DOCUMENTATION_QUALITY_GATE_RUN=43521788411
DOCUMENTATION_QUALITY_GATE_SHA=93fe1ed918137062bccd13a61e74a59334600eaf
DOCUMENTATION_QUALITY_GATE_STATUS=completed
DOCUMENTATION_QUALITY_GATE_CONCLUSION=success
SECURITY_SCAN=PASS_GITHUB_ACTIONS
GITHUB_REMOTE_VERIFIED=YES
REVIEW_READY=YES_FOR_OWNER_VISUAL_REVIEW
B06_LOGO_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE
```

## Objetivo y alcance

Verificar que el logo oficial adjunto por el propietario se publique una sola vez como activo canónico y sea reutilizado por los generadores compartidos de PDF, XLSX y preview, preservando los datos y contratos existentes.

## Evidencia primaria sintética

- `InstitutionalLogoAsset` valida firma PNG, dimensiones y SHA-256 exacto antes de servir el recurso al backend.
- `InstitutionalPdfDocument` emite el logo en el encabezado institucional de cada página.
- `InstitutionalXlsxWorkbook` agrega el PNG y sus relaciones OpenXML a cada hoja sin modificar valores, fórmulas o identificadores.
- Angular usa el mismo path canónico en el estándar PDF, el exportador Excel y el preview. Los consumidores activos reutilizan esas utilidades.
- El Dockerfile incluye únicamente el PNG requerido para que el recurso embebido exista durante `dotnet publish`; el build multistage remoto fue verificado en verde.

## Seguridad, límites y procedencia

Esta evidencia es sintética y sanitizada. No contiene secretos, dumps, datos de clientes, credenciales, logs operativos ni información institucional restringida. El PNG procede exclusivamente del adjunto del propietario y está versionado en el path canónico indicado. La verificación Docker local quedó pendiente por indisponibilidad del daemon; el mismo build fue ejecutado y aprobado por GitHub Actions.

La aprobación estética final no se infiere del gate técnico: `B06_LOGO_OWNER_VISUAL_ACCEPTANCE=PENDING`. B07, Bloque 10, `main`, producción, Oracle institucional y migración 54 permanecen fuera de alcance.
