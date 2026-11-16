# B06 — Parametrizacion global de identidad institucional

Esta actualizacion agrega una unica resolucion de identidad para los reportes institucionales. La configuracion administrativa puede definir nombre de institucion, nombre de sistema, URL/ruta del logo y color del encabezado; el snapshot resuelto se reutiliza en PDF, XLSX y sus previews. El tamano fisico certificado del logo no cambia: `72.644 mm` en PDF Angular y `205.92 pt` en PDF ASP.NET.

La carga de imagen conserva la politica institucional existente, normaliza JPG/JPEG/WEBP a PNG en el navegador, limita el archivo a 5 MB y usa una operacion auditada de imagen. El backend valida el PNG, dimensiones, cantidad de pixeles y SHA-256 antes de embebido. Las URL remotas no se descargan desde el backend: se rechazan para evitar SSRF; los reportes de servidor solo aceptan rutas locales bajo `wwwroot`. La interfaz puede mostrar una URL same-origin valida y el backend usa el fallback canonico si no puede verificarla.

El color se valida como hexadecimal de seis digitos y se calcula automaticamente el texto blanco/oscuro mediante contraste. La resolucion falla cerrada al logo canonico si la configuracion, ruta o imagen no son validas. La columna Oracle `SFS_COLOR_ENCABEZADO_REPORTES` queda preparada mediante scripts controlados en `database/_utilitarios/b06_dynamic_branding/`; Codex no ejecuta DDL/DML contra Oracle institucional.

```ini
PHASE=B06_DYNAMIC_INSTITUTIONAL_BRANDING
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=167d5f72ea20e72000553ad190c7089eaea41a85
SOURCE_CODE_SHA=331e9617c9ee2dba9e008ae3f3b7071b9b8551b5
EVIDENCE_COMMIT_SHA=PENDING_DOCUMENTAL_COMMIT
LOGO_CANONICAL_SHA256=D57799214B441EA834320A1069D46A6930590ED4325F5A5EFAFCC65D115B7543
LOGO_CANONICAL_DIMENSIONS=2034x773
LOGO_PHYSICAL_WIDTH_MM=72.644
BACKEND_PDF_LOGO_WIDTH_POINTS=205.92
B06_DYNAMIC_LOGO_UPLOAD=PASS
B06_DYNAMIC_HEADER_COLOR=PASS
B06_AUTOMATIC_CONTRAST=PASS
B06_PDF_BACKEND_PROPAGATION=PASS
B06_XLSX_BACKEND_PROPAGATION=PASS
B06_ANGULAR_PDF_PROPAGATION=PASS
B06_ANGULAR_XLSX_PROPAGATION=PASS
B06_PREVIEW_PROPAGATION=PASS
B06_AUDIT_POLICY=PASS
B06_EXTERNAL_URL_BACKEND=SAFE_REJECT_SSRF
B06_ORACLE_MIGRATION=PREPARED_NOT_EXECUTED
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
SECURITY_SCAN=PASS_LOCAL_SCOPE_PENDING_GITHUB_ACTIONS
GITHUB_REMOTE_VERIFIED=YES_FOR_SOURCE_COMMIT
REVIEW_READY=PENDING_DOCUMENTAL_GATE
```

## Verificaciones reproducidas

| Verificacion | Resultado | Conteo/limite |
|---|---|---|
| Build backend | PASS | 0 errores; warnings de analizadores preexistentes |
| Regresion backend B06 | PASS | 18/18 |
| Suite Angular | PASS | 95 archivos; 1000/1000 pruebas |
| Build Angular | PASS | warnings preexistentes de presupuesto SCSS/CommonJS |
| ESLint | PASS | `npm run lint` |
| Validacion de base de datos | PASS | scripts B06 fuera de raiz activa; pre/postcheck read-only |
| Oracle institucional | NOT_EXECUTED | requiere ventana DBA, respaldo y aprobacion expresa |
| Aceptacion visual del propietario | PENDING | requiere regenerar PDF/XLSX despues de guardar configuracion |

El cambio funcional esta publicado en [`331e9617`](https://github.com/jmejia31/RIESGO_LAVADO/commit/331e9617c9ee2dba9e008ae3f3b7071b9b8551b5). Este expediente no cierra B06 por inferencia tecnica: la revision visual del propietario y el gate remoto del SHA documental siguen siendo pendientes.
