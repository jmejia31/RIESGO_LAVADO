# B06 — Recuperación de generación de reportes

```ini
PHASE=B06_REPORT_AVAILABILITY_RECOVERY
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=13436555ea25980ac206fda15af1e915f0402f9c
SOURCE_CODE_SHA=0ea9df6b62a4aa1da85eb9e320f263518c397310
B06_REPORT_GENERATION_RECOVERY=IMPLEMENTED
B06_BACKEND_REMOTE_LOGO_SSRF=PROTECTED
B06_BACKEND_INVALID_LOGO_AVAILABILITY=SAFE_CANONICAL_FALLBACK
B06_FRONTEND_REMOTE_LOGO_REPORTING=SAFE_CANONICAL_FALLBACK
B06_FRONTEND_LOCAL_LOGO_REPORTING=CONFIGURED_LOCAL_URL
B06_MATRICES_PDF_EXPORT_REGRESSION=PASS
B06_MATRICES_XLSX_EXPORT_REGRESSION=PASS
B06_MONITOREO_SHARED_BRANDING_REGRESSION=PASS
B06_SECURITY_FIELDS_PRESERVED=YES
B06_ORACLE_TOUCHED=NO
B06_REAL_AUTHENTICATED_E2E=NOT_VERIFIED
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
MIGRATION_54_TOUCHED=NO
```

## Diagnóstico reproducido

La configuración consultada tenía una URL `https://...` en `SFS_LOGO_URL`. El resolver de reportería la rechazaba para evitar que el servidor descargara recursos externos, pero el controlador convertía esa situación en `503 B06_BRANDING_LOGO_NOT_READY`. Por eso Matrices de Riesgos mostraba simultáneamente error al generar PDF y Excel aunque los datos estuvieran disponibles.

El frontend también trasladaba URLs externas al contrato de PDF/Excel. Una URL que puede verse en una etiqueta HTML no es necesariamente un recurso embebible por jsPDF o ExcelJS y podía romper los reportes generados en el navegador.

## Corrección

- El backend conserva la protección SSRF y nunca descarga URLs externas. Cuando la configuración contiene una URL remota, una ruta ausente o una imagen local inválida, utiliza el PNG canónico embebido como fallback controlado, conserva nombre/sistema/color y registra únicamente un motivo técnico sanitizado.
- Una imagen publicada en `/uploads/...` continúa siendo la fuente activa de PDF/XLSX backend cuando es válida.
- Angular usa el activo local para la reportería del navegador y evita entregar URLs `http`, `https` o `data` al generador. Si la configuración es remota, utiliza el activo canónico local para que PDF y Excel no fallen.
- La vista previa compartida lee la misma identidad de reportería en lugar de consultar una URL independiente.
- La corrección no cambia Oracle, no reemplaza la configuración almacenada y no altera campos de seguridad, datos, fórmulas ni contratos de módulos.

## Verificaciones ejecutadas

| Verificación | Resultado |
|---|---|
| Resolver local publicado y hash | PASS |
| Resolver URL remota sin romper reportería | PASS |
| Resolver archivo local inválido sin romper reportería | PASS |
| Exportación backend Matrices PDF/XLSX y reportes vacíos | PASS, 7 pruebas focalizadas |
| Angular configuración + estándar PDF + preview | PASS, 22 pruebas focalizadas |
| Build Angular | PASS; warnings preexistentes de SCSS/CommonJS |
| ESLint | PASS |
| Flujo autenticado real LOGO_A/LOGO_B contra la base del propietario | NOT_VERIFIED en esta intervención |
| Playwright visual autenticado | NOT_VERIFIED en esta intervención |
| Oracle institucional | NO EJECUTADO |

El estado técnico queda abierto hasta ejecutar el flujo real autenticado y la aceptación visual del propietario. Este expediente no convierte una URL externa en una fuente válida para el backend ni oculta un error de seguridad.

## Recuperacion de generacion de reportes — 2026-11-16

La configuración que contenía una URL externa no debe dejar sin reportes al sistema. El resolver backend conserva la protección SSRF y aplica un fallback canónico controlado para que los reportes PDF/XLSX sigan disponibles mientras el administrador publica un archivo local válido. Angular aplica la misma regla a su reportería directa y a la vista previa compartida. Una carga local publicada en `/uploads/...` continúa utilizándose como activo configurado.

La aceptación visual del propietario y el flujo autenticado LOGO_A/LOGO_B siguen pendientes y no se declaran PASS por inferencia.
