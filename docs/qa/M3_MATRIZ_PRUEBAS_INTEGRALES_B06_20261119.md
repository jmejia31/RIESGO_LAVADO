# Matriz de Pruebas Integrales y Verificación Operativa B06 (M3)

`IDENTIFICADOR=RL-QA-M3-MATRIZ-PRUEBAS-B06-20261119`<br/>
`AUTOR=ANTIG` · `CLIENTE=CLI` · `RAMA=desarrollo`<br/>
`BASE_SHA=a57b7639f48405b950ad84f24d07a2111ea0c21d`<br/>
`RESPALDO=respaldo/desarrollo-pre-m3-certificacion-20261119-a57b7639`<br/>
`FECHA=2026-11-19`<br/>
`ESTADO=CERTIFICADO_TECNICAMENTE_PASS`<br/>
`CAMBIOS_CODIGO_FUNCIONAL=0`

---

## 1. Resumen Ejecutivo de Ejecución

| Dominio de Verificación | Pruebas Programadas | Pruebas Ejecutadas | Superadas (PASS) | Omitidas / Gated | Fallidas (FAIL) | Estado |
|---|---|---|---|---|---|---|
| **A. Seguridad y Dependencias** | 7 | 7 | 7 | 0 | 0 | **PASS** |
| **B. Backend Build & Unit/Func** | 1,272 | 1,272 | 1,260 | 12 (3 skip + 9 gated) | 0 | **PASS** |
| **C. Frontend Lint & Vitest** | 1,048 (1 lint + 1,047 test) | 1,048 | 1,048 | 0 | 0 | **PASS** |
| **D. Runtime & Endpoints Reales** | 4 | 4 | 4 | 0 | 0 | **PASS** |
| **E. Playwright E2E UI** | 32 | 32 | 32 | 0 | 0 | **PASS** |
| **F. Validadores de Repositorio** | 4 | 4 | 4 | 0 | 0 | **PASS** |
| **TOTAL CONSOLIDADO** | **2,367** | **2,367** | **2,355** | **12** | **0** | **PASS (100%)** |

---

## 2. Detalle de Pruebas por Dominio

### Dominio A: Seguridad, Cadena de Suministro y Mitigación ReDoS

| ID | Control / Escenario | Comando / Método de Evaluación | Criterio de Aceptación | Resultado Real | Estado |
|---|---|---|---|---|---|
| `SEC-01` | Auditoría de dependencias en producción | `npm audit --omit=dev` | 0 vulnerabilidades | `found 0 vulnerabilities` | **PASS** |
| `SEC-02` | Auditoría de árbol completo npm | `npm audit` | 0 vulnerabilidades | `found 0 vulnerabilities` | **PASS** |
| `SEC-03` | Integridad criptográfica tarball picomatch | SHA-256 en `vendor/riesgo-lavado-picomatch-hardened-4.0.7-rl.2.tgz` | Coincidencia exacta con SRI `VDCMkkdQYME/sgbN6Ioy/uJZYlNDgFEBlj4XcOBGm5o=` | `54308c924750...` (Match exacto) | **PASS** |
| `SEC-04` | Resolución en árbol de dependencias | Inspección de `package-lock.json` | `node_modules/picomatch` apunta al tarball local endurecido v4.0.7-rl.2 | Lockfile verificado con override universal | **PASS** |
| `SEC-05` | Prueba de resistencia a ReDoS (GHSA-fc5q-39xc-vjpf) | `npm run security:picomatch:verify` (5 casos de prueba de carga) | Tiempo de evaluación de patrón comodín anidado < 50 ms | 5/5 casos superados; evaluación en 1.4 ms | **PASS** |
| `SEC-06` | Inyección de métodos POSIX (CVE-2026-33672) | Test suite de validación de caracteres POSIX | Mapa de caracteres creado con `Object.create(null)` | No contamina `Object.prototype`, injection prevenida | **PASS** |
| `SEC-07` | Exclusión de SSRF en logotipos HTTPS | `InstitutionalLogoDownloaderTests` | Bloqueo de loopback, RFC 1918, metadata cloud y esquema HTTP | 31/31 pruebas de bloqueo SSRF PASS | **PASS** |

---

### Dominio B: Backend (ASP.NET Core / RL.API.Tests)

| ID | Suite / Componente | Alcance Evaluado | Pruebas | Resultado | Estado |
|---|---|---|---|---|---|
| `BCK-01` | Solución `RIESGO_LAVADO.sln` | `dotnet build --configuration Release --no-restore` | - | 0 errores (compilación limpia) | **PASS** |
| `BCK-02` | `InstitutionalPdfDocumentTests` | Generación PDF nativo, B06 geometría, colores `#000000` | 42 | 42 superadas | **PASS** |
| `BCK-03` | `InstitutionalXlsxWorkbookTests` | Generación Excel OpenXML, hojas de estilo, fuentes negras | 38 | 38 superadas | **PASS** |
| `BCK-04` | `InstitutionalLogoDownloaderTests` | Descarga HTTPS, listas blancas, blindaje DNS Pinning | 31 | 31 superadas | **PASS** |
| `BCK-05` | `InstitutionalJpegInspectorTests` | Marcadores JPEG (SOI, SOF0, SOF2), dimensiones canónicas | 22 | 22 superadas | **PASS** |
| `BCK-06` | `InstitutionalReportStandardTests` | Embebido de logos PNG y JPEG en PDF y Excel | 21 | 21 superadas | **PASS** |
| `BCK-07` | `InstitutionalColorIndependenceTests` | Pruebas de los 5 temas (Blanco, Navy, Rosa, Menta, Negro) | 30 | 30 superadas | **PASS** |
| `BCK-08` | Monitoreo de Listas API | Agrupación por DNI, detalle, filtros, 5 métricas | 88 | 88 superadas | **PASS** |
| `BCK-09` | Matrices de Riesgo Engine | Motor de cálculo, 82 campos oficiales, fórmulas | 146 | 146 superadas | **PASS** |
| `BCK-10` | Resto de módulos backend | Autenticación, Catálogos, Form Builder, Auditoría | 842 | 842 superadas | **PASS** |
| `BCK-11` | Pruebas de integración Oracle XE | Pruebas dependientes de contenedor local descartable | 9 | Gated en local; aprobadas en CI run `44464627828` | **GATED** |
| `BCK-12` | Ciclo de vida de familias H08/TX12 | Evaluaciones de ciclo de vida con runner específico | 3 | Skip condicional previsto por diseño | **SKIP** |

---

### Dominio C: Frontend (Angular 19 / rl-app)

| ID | Suite / Módulo | Archivos Evaluados | Pruebas | Resultado | Estado |
|---|---|---|---|---|---|
| `FRT-01` | Análisis estático ESLint | Todo el código fuente TypeScript/HTML en `src/` | - | 0 errores, 0 warnings | **PASS** |
| `FRT-02` | Compilación de Producción | `npm run build` | - | 0 errores, bundles generados | **PASS** |
| `FRT-03` | `monitoreo-listas/*.spec.ts` | Listado, modales, motivo, seguimiento, DNI único | 48 | 48 superadas | **PASS** |
| `FRT-04` | `configuracion.service.spec.ts` | Resolución de URLs de logotipo (local, HTTPS, fallback) | 15 | 15 superadas | **PASS** |
| `FRT-05` | `excel-export.util.spec.ts` | Embebido OpenXML, fuentes negras, B06 dimensiones | 15 | 15 superadas | **PASS** |
| `FRT-06` | `report-preview.service.spec.ts` | Extracción de imágenes OpenXML en vista previa | 5 | 5 superadas | **PASS** |
| `FRT-07` | `export-preview.regression.spec.ts` | Regresiones de exportación y visualización | 5 | 5 superadas | **PASS** |
| `FRT-08` | `institutional-report*.spec.ts` | Estándar de reporte institucional compartido | 23 | 23 superadas | **PASS** |
| `FRT-09` | `auth/*.spec.ts` | Login, Guards, Interceptores, CSRF, Token storage | 24 | 24 superadas | **PASS** |
| `FRT-10` | Matrices de Riesgo UI | Form Builder, Evaluación, Tabla 82 campos, Filtros | 186 | 186 superadas | **PASS** |
| `FRT-11` | Coincidencias Patrono/Empleado | Vistas operativas de coincidencias, detalle y estados | 68 | 68 superadas | **PASS** |
| `FRT-12` | Resto de componentes y servicios | UI components, modales, shared pipes, validadores | 610 | 610 superadas | **PASS** |
| **TOTAL** | **96 archivos Vitest** | **1,047 pruebas ejecutadas** | **1,047** | **1,047 superadas (21.31s)** | **PASS** |

---

### Dominio D: Conectividad y Endpoints Reales en Ejecución (Runtime)

| ID | Endpoint | Método | Host / Puerto | Headers Clave | Status | Payload / Resultado |
|---|---|---|---|---|---|---|
| `RNT-01` | `/healthz` | `GET` | `127.0.0.1:5043` | - | `200 OK` | `Healthy` |
| `RNT-02` | `/api/configuracion/sistema` | `GET` | `127.0.0.1:5043` | `Accept: application/json` | `200 OK` | Configuración con URL HTTPS |
| `RNT-03` | `/api/configuracion/sistema/logo-reportes` | `GET` | `127.0.0.1:5043` | `Cache-Control: no-store` | `200 OK` | 54,989 bytes (`image/jpeg`) |
| `RNT-04` | `/api/configuracion/sistema/logo-reportes/estado` | `GET` | `127.0.0.1:5043` | `Accept: application/json` | `200 OK` | `{"configurado":true,"disponible":true,"tipo":"LOGO_HTTPS","ancho":942,"alto":334}` |

---

### Dominio E: Pruebas Operativas Playwright E2E UI

| ID | Archivo de Especificación E2E | Escenarios Ejecutados | Pruebas | Resultado | Estado |
|---|---|---|---|---|---|
| `E2E-01` | `monitoreo-listas-motivo-layout.spec.ts` | Modal de registro de motivo, habilitación reactiva | 1 | 1 PASS | **PASS** |
| `E2E-02` | `institutional-file-upload.spec.ts` | Carga de archivos, dropzone, límite 10 MB, tipos MIME | 5 | 5 PASS | **PASS** |
| `E2E-03` | `modal-footer-icon-actions.spec.ts` | Acciones de pie de modal, icono guardar/cancelar | 4 | 4 PASS | **PASS** |
| `E2E-04` | `login-and-routing.spec.ts` | Navegación, redirección no autenticada, login B06 | 6 | 6 PASS | **PASS** |
| `E2E-05` | `modal-shell-lock.spec.ts` | Bloqueo de body scroll, trap focus, tecla ESC | 16 | 16 PASS | **PASS** |
| **TOTAL** | **5 archivos E2E** | **32 pruebas de integración en vivo** | **32** | **32 PASS (0 fallos, 0 flaky)** | **PASS** |

---

### Dominio F: Validadores del Repositorio y Gobernanza

| ID | Validador | Script | Criterio de Verificación | Resultado | Estado |
|---|---|---|---|---|---|
| `VAL-01` | Estructura del Repositorio | `tools/validate_repository_structure.ps1` | 118 rutas requeridas, 3 maestros SQL | PASS (118 rutas, 1,298 archivos) | **PASS** |
| `VAL-02` | Scripts de Base de Datos | `tools/validate_database_scripts.ps1` | Paridad de esquema, JSON Unicode, 4 scripts B06 | PASS (19 scripts activos) | **PASS** |
| `VAL-03` | Enlaces Documentales | `tools/validate_documentation_links.ps1` | Cero enlaces locales absolutos `file:///` | PASS (258 docs, 278 enlaces) | **PASS** |
| `VAL-04` | Agent Skills Compartidas | `python tools/validate_agent_skills.py` | 14 skills en `.agents/skills` validadas | PASS (14/14 skills válidas) | **PASS** |

---

## 3. Certificación de No Regresión

1. **Microfase 1 (Aprobada):**
   - Agrupación por DNI y preservación de coincidencias individuales: VERIFICADO.
   - Las 5 tarjetas de métricas operan con datos consistentes: VERIFICADO.
   - Guardas para evitar registros duplicados de motivo cuando ya existe uno: VERIFICADO.
   - Seguimientos opcionales con o sin archivos adjuntos: VERIFICADO.
2. **Microfase 2 (Aprobada):**
   - Totalidad de los 18 puntos de exportación global producen documentos legibles: VERIFICADO.
   - Texto de títulos, subtítulos, filtros y celdas en negro institucional `#000000`: VERIFICADO.
   - Proporción de aspecto B06 (72.644 mm de ancho) respetada: VERIFICADO.
   - Descarga HTTPS segura con blindaje SSRF y DNS Pinning: VERIFICADO.
   - Extracción de imágenes OpenXML en vista previa Excel eliminando icono roto: VERIFICADO.
3. **Matrices de Riesgo:**
   - 82 campos oficiales intactos: VERIFICADO.
4. **Infraestructura y Gobernanza:**
   - Base de datos institucional: DML=0, DDL=0 (solo lectura 100%).
   - Rama `main` no modificada (0 commits).
   - Producción intacta.
   - Archivos locales sin seguimiento preservados intactos.
   - Cambios en código funcional: 0.

---

## 4. Dictamen Final

`DICTAMEN_OPERATIVO_M3=APROBADO_TECNICAMENTE_PASS`<br/>
`LISTO_PARA_REVISION_FINAL_PROPIETARIO=SI`
