# B06 — Parametrizacion global de identidad institucional

Esta actualizacion agrega una unica resolucion de identidad para los reportes institucionales. La configuracion administrativa puede definir nombre de institucion, nombre de sistema, URL/ruta del logo y color del encabezado; el snapshot resuelto se reutiliza en PDF, XLSX y sus previews. El tamano fisico certificado del logo no cambia: `72.644 mm` en PDF Angular y `205.92 pt` en PDF ASP.NET.

La carga de imagen conserva la politica institucional existente, normaliza JPG/JPEG/WEBP a PNG en el navegador, limita el archivo a 5 MB y usa una operacion auditada de imagen. El backend valida el PNG, dimensiones, cantidad de pixeles y SHA-256 antes de embebido. Las URL remotas no se descargan desde el backend: se rechazan para evitar SSRF; los reportes de servidor solo aceptan rutas locales bajo `wwwroot`. La interfaz puede mostrar una URL same-origin valida y el backend usa el fallback canonico si no puede verificarla.

El color se valida como hexadecimal de seis digitos y se calcula automaticamente el texto blanco/oscuro mediante contraste. La resolucion usa el logo canonico como fallback seguro si la configuracion, ruta o imagen no son validas; una ruta local valida en `wwwroot/uploads` sigue siendo la fuente activa. Las URL externas nunca se descargan desde el backend. La columna Oracle `SFS_COLOR_ENCABEZADO_REPORTES` queda preparada mediante scripts controlados en `database/_utilitarios/b06_dynamic_branding/`; Codex no ejecuta DDL/DML contra Oracle institucional.

```ini
PHASE=B06_DYNAMIC_INSTITUTIONAL_BRANDING
AUTHOR=COD/CODEX
CLIENT=CLI
BRANCH=desarrollo
INITIAL_SHA=167d5f72ea20e72000553ad190c7089eaea41a85
SOURCE_CODE_SHA=331e9617c9ee2dba9e008ae3f3b7071b9b8551b5
EVIDENCE_COMMIT_SHA=55190a7a9efc25541c1bd1b9bb2bd34d965f8211
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
QUALITY_GATE_STATUS=SUCCESS
QUALITY_GATE_RUN=43920939882
QUALITY_GATE_URL=https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43920939882
SECURITY_SCAN=PASS_GITHUB_ACTIONS
GITHUB_REMOTE_VERIFIED=YES_FOR_SOURCE_COMMIT
REVIEW_READY=YES_FOR_OWNER_VISUAL_REVIEW
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

## Actualizacion B06 — correccion P0 de publicacion y persistencia — 2026-11-16

La auditoria funcional posterior al expediente anterior encontro que el flujo podia informar exito sin demostrar que el archivo cargado fuera el activo utilizado por todos los consumidores. El commit [`14b238ad`](https://github.com/jmejia31/RIESGO_LAVADO/commit/14b238ad8ef8533564012113cec621423757e821) corrige la raiz compartida: carga y resolucion usan la misma raiz `WebRootPath`, la ruta productiva de configuracion y auditoria comparte transaccion, los campos de seguridad omitidos se preservan y la interfaz relee el estado publicado antes de mostrar exito.

```ini
SOURCE_CODE_SHA=510d4bea75f8ca5bd3ac42d470cfe7e4fac97880
EVIDENCE_COMMIT_SHA=286689c9328aad2b84594f00dd86333311fd33c2
B06_CONFIGURATION_ATOMICITY=IMPLEMENTED
B06_AUDIT_TRANSACTIONALITY=IMPLEMENTED
B06_SECURITY_SETTINGS_PRESERVED=IMPLEMENTED
B06_LOGO_STORAGE_ROOT=WEB_ROOT_PATH
B06_CONTRAST_POLICY=WCAG_AA_DERIVED
B06_FRONTEND_REGRESSION=PASS_96_FILES_1007_TESTS
B06_FRONTEND_BUILD=PASS
B06_FRONTEND_LINT=PASS
B06_BACKEND_BUILD=PASS_0_ERRORS
B06_BACKEND_FULL_TESTS=BLOCKED_ACTIVE_PROCESS_LOCK
B06_REAL_LOGO_A_LOGO_B=NOT_VERIFIED
B06_REAL_PDF_EXCEL_FLOW=NOT_VERIFIED
B06_PLAYWRIGHT_REAL_FLOW=NOT_VERIFIED
B06_ORACLE_XE_THIS_INTERVENTION=NOT_EXECUTED
B06_QUALITY_GATE=PENDING_NEW_SOURCE_SHA
B06_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=BLOCKED_PENDING_REAL_FLOW_AND_OWNER_ACCEPTANCE
```

La evidencia detallada y sanitizada esta en [`docs/certificaciones/B06_dynamic_branding_14b238ad.md`](../certificaciones/B06_dynamic_branding_14b238ad.md). No se presenta `PASS` tecnico global mientras permanezcan sin verificar la prueba real LOGO_A/LOGO_B, la generacion PDF/Excel desde configuracion persistida, la prueba E2E autenticada y el Quality Gate del nuevo SHA.

El expediente documental base que contiene esta evidencia fue publicado en [`55190a7a`](https://github.com/jmejia31/RIESGO_LAVADO/commit/55190a7a9efc25541c1bd1b9bb2bd34d965f8211). El Quality Gate exacto del estado documental publicado terminó verde en el [run 43920939882](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43920939882): `STATUS=completed`, `CONCLUSION=success`. La revisión visual del propietario permanece pendiente.
- Quality Gate exacto [43936758470](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43936758470): `STATUS=completed`; `CONCLUSION=success`; la matriz integral termino verde.

## Actualizacion B06 - Quality Gate exacto del P0 - 2026-11-16 13:26:24 -06:00

```ini
SOURCE_CODE_SHA=359c9720f20af2ab8363e2650ff84d1f1c562564
QUALITY_GATE_RUN=43936758470
QUALITY_GATE_STATUS=SUCCESS
SECURITY_SCAN=PASS_GITHUB_ACTIONS
GITHUB_REMOTE_VERIFIED=YES_FOR_SOURCE_COMMIT_AND_GREEN_GATE
REVIEW_READY=BLOCKED_ENVIRONMENTAL_VERIFICATION
B06_TECHNICAL_STATUS=BLOCKED
B06_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=BLOCKED_PENDING_REAL_FLOW_AND_OWNER_ACCEPTANCE
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

El Quality Gate verde valida el SHA exacto publicado, pero no sustituye la aceptacion visual del propietario ni la verificaciÃ³n manual del flujo local autenticado de configuracion y reportes.
- `EVIDENCE_COMMIT_SHA=f841b5eedefc8e9a773888e6df2a3f77f1205f37`; indice y evidencia remota verificables en GitHub; Quality Gate documental `43939723032=SUCCESS`.

## Cierre documental remoto B06 - 2026-11-16 13:26:24 -06:00

El expediente sanitizado se consulta directamente en GitHub mediante el indice `docs/certificaciones/INDEX.json` y la evidencia B06 anclada al commit `f841b5eedefc8e9a773888e6df2a3f77f1205f37`. El gate del codigo es `43936758470=SUCCESS` y el gate del expediente es `43939723032=SUCCESS`. B06 continua abierto solo por la aceptacion visual del propietario y la validacion manual del flujo local.
## Actualización B06 — corrección del flujo de logo local — 2026-11-16

El commit funcional [`f1d42079`](https://github.com/jmejia31/RIESGO_LAVADO/commit/f1d42079bc549ded50a9d245fc1132fc5da2e08d) corrige la diferencia observada por el propietario entre cargar un enlace y arrastrar un archivo. La carga local ahora normaliza la imagen a un PNG documental compatible; carga y resolución utilizan la misma raíz `wwwroot/uploads`; y el resolver ya no reemplaza silenciosamente una configuración inválida por el logo canónico. La vista previa Excel también obtiene el logo desde la configuración compartida.

```ini
SOURCE_CODE_SHA=f1d42079bc549ded50a9d245fc1132fc5da2e08d
EVIDENCE_COMMIT_SHA=PENDING_DOCUMENTAL_COMMIT
B06_LOCAL_UPLOAD_NORMALIZATION=PASS
B06_LOCAL_ASSET_HTTP=PASS_200_IMAGE_PNG
B06_CONFIGURED_LOCAL_RESOLUTION=PASS
B06_REMOTE_SERVER_FETCH=BLOCKED_SSRF
B06_CANONICAL_FALLBACK_HIDING_CONFIG_ERROR=REMOVED
B06_BACKEND_REGRESSION=PASS_9_TESTS
B06_FRONTEND_REGRESSION=PASS_96_FILES_1007_TESTS
B06_BACKEND_BUILD=PASS_0_ERRORS
B06_FRONTEND_BUILD=PASS_WITH_PREEXISTING_WARNINGS
B06_FRONTEND_LINT=PASS
B06_ORACLE_B06_PRECHECK=PASS
B06_ORACLE_B06_POSTCHECK=PASS
B06_REAL_ADMIN_LOGO_A_LOGO_B=NOT_VERIFIED_AUTHENTICATED
B06_REAL_PDF_EXCEL_FLOW=NOT_VERIFIED_AUTHENTICATED
B06_PLAYWRIGHT_VISUAL=BLOCKED_BROWSER_AUTHENTICATION
B06_QUALITY_GATE=SUCCESS
B06_QUALITY_GATE_RUN=43955398124
B06_QUALITY_GATE_URL=https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43955398124
B06_DOCUMENTAL_QUALITY_GATE_RUN=43958425555
B06_DOCUMENTAL_QUALITY_GATE_STATUS=SUCCESS
B06_DOCUMENTAL_QUALITY_GATE_URL=https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/43958425555
B06_TECHNICAL_STATUS=PARTIAL_PENDING_AUTHENTICATED_E2E
B06_OWNER_VISUAL_ACCEPTANCE=PENDING
B06_FINAL_STATUS=OPEN_PENDING_OWNER_ACCEPTANCE
```

El expediente sanitizado es [`B06_logo_local_publicacion_f1d42079.md`](../certificaciones/B06_logo_local_publicacion_f1d42079.md). El mensaje de carga pendiente no equivale a publicación: después de arrastrar el archivo se debe pulsar Guardar y esperar la lectura posterior de la configuración. Una URL externa puede verse en el navegador, pero no es aceptada por la reportería de servidor; el flujo institucional soportado es un archivo local publicado bajo `/uploads/...`.
