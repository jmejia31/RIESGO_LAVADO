# RIESGO_LAVADO — CERTIFICACIÓN TÉCNICA MICROFASE 2 (M2)
## REPORTERÍA INSTITUCIONAL, LOGOTIPOS, COLORES, LEGIBILIDAD Y PARIDAD PDF/EXCEL

**PROYECTO:** SGRLA-IHSS — RIESGO_LAVADO  
**REPOSITORIO:** `jmejia31/RIESGO_LAVADO`  
**RAMA AUTORIZADA:** `desarrollo`  
**DIRECTORIO:** `C:\RIESGO_LAVADO`  
**AUTORIDAD TÉCNICA:** ANTIG — Antigravity  
**PROPIETARIO / APROBADOR FINAL:** Javier Mejía (`jmejia31`)  
**PRIORIDAD:** P0 — Corrección e infraestructura institucional de reportería  
**FECHA:** 18 de noviembre de 2026  
**ESTADO:** `CERTIFIED_TECHNICALLY_READY_FOR_OWNER_VISUAL_REVIEW`  

---

## 1. RESUMEN EJECUTIVO Y ALCANCE

La presente intervención de **Microfase 2 (M2)** resuelve definitivamente los requerimientos institucionales de reportería compartida para el Sistema de Gestión de Riesgo de Lavado de Activos y Financiamiento del Terrorismo (SGRLA-IHSS):

1. **Independencia absoluta de color de contenido:** Corrección definitiva del defecto visual reportado en la vista previa del reporte consolidado de matrices de riesgo (`media_1795050915559_b310c573.jpg`), donde los títulos de sección (`FILTROS APLICADOS`, `ROTR-AFIL-1 — EVALUACIÓN 29`) y textos del cuerpo se renderizaban en turquesa claro heredando el color del banner del encabezado. Todo texto de contenido sobre fondo blanco se desacopló hacia negro institucional `#000000` con `font-weight: bold` en títulos y etiquetas.
2. **Descargador seguro de logotipos con protección estricta contra SSRF:** Implementación de `InstitutionalLogoDownloader`, con validación obligatoria de esquema HTTPS (puerto 443), lista blanca de dominios (`ihss.hn`, `sgrla.ihss.hn`, `www.ihss.hn`), exclusión de rangos privados (RFC 1918), loopback, enlace local / metadatos cloud (`169.254.169.254`), mitigación de DNS Rebinding mediante fijación de socket a IP validada, redirecciones deshabilitadas, límite estricto de 2 MB y timeout de 5s.
3. **Inspector nativo de imágenes JPEG:** Implementación de `InstitutionalJpegInspector` sin dependencias nativas de SO (100% C# administrado, compatible Linux CI), inspeccionando marcadores SOI, APP0 y SOF0/SOF1/SOF2 para validar dimensiones y relación de aspecto B06.
4. **Soporte documental dual (PNG y JPEG) en PDF y Excel:** Generación nativa con `/Filter /DCTDecode` para JPEG y `/Filter /FlateDecode` para PNG en PDF, e inclusión segura en drawing OpenXML para hojas de cálculo Excel.
5. **Geometría B06 unificada:** Altura fija de encabezado calculada mediante la constante estándar compartida (90.72 pt en PDF), ancho canónico del logo de 72.644 mm (205.92 pt), sin deformación (`object-fit: contain`) y preservando la altura independientemente de la presencia o ausencia de imagen.
6. **Invalidación reactiva de caché:** Método `ClearCache()` en `InstitutionalBrandingResolver`, invocado atómicamente en `ConfiguracionController.GuardarSistema()`, asegurando que cualquier cambio de logo o colores se refleje de inmediato en los reportes subsiguientes.
7. **Paridad e integridad documental:** La Matriz Consolidada de Riesgos conserva sus 82 campos oficiales intactos y ordenados, y el módulo de Monitoreo de Listas preserva su clasificación, tarjetas y filtros certificados en Microfase 1.

---

## 2. RESOLUCIÓN DEL DEFECTO DE COLOR (MEDIA_1795050915559_B310C573.JPG)

| Elemento Reportado | Estado Anterior | Estado Corregido | Verificación Automatizada |
| :--- | :--- | :--- | :--- |
| **`FILTROS APLICADOS`** | Heredaba `_branding.Palette.Navy` (turquesa en el entorno con banner menta) | Forzado a `#000000` (negro) y negrita 11pt | `InstitutionalColorIndependenceTests` (PASS) |
| **`ROTR-AFIL-1 — EVALUACIÓN 29`** | Heredaba `_branding.Palette.Navy` | Forzado a `#000000` (negro) y negrita 11pt | `InstitutionalColorIndependenceTests` (PASS) |
| **Párrafos / Descripciones** | Heredaba colores de paleta variables | `#000000` (negro regular 9pt) | `InstitutionalPdfDocument.AddParagraph` |
| **Etiquetas Key-Value Grid** | Heredaba `_branding.Palette.Navy` | `#000000` (negro negrita 7pt) | `InstitutionalPdfDocument.AddKeyValueGrid` |
| **Valores Key-Value Grid** | `_branding.Palette.BodyText` variable | `#000000` (negro regular 8.5pt) | `InstitutionalPdfDocument.AddKeyValueGrid` |
| **KPI Values** | Heredaba `_branding.Palette.Navy` | `#000000` (negro negrita 16pt) | `InstitutionalPdfDocument.AddKpis` |
| **Excel Styles (`xl/styles.xml`)** | Font 3 y Font 5 usaban Navy | Font 0, 3, 4 y 5 configuradas con `rgb="FF000000"` | `InstitutionalXlsxWorkbook.StylesXml` |

---

## 3. ARQUITECTURA DE SEGURIDAD SSRF (INSTITUTIONALLOGODOWNLOADER)

La descarga de logotipos institucionales remotos vía HTTPS implementa un mecanismo de defensa en profundidad contra Server-Side Request Forgery:

1. **Esquema Estricto:** Exclusivamente `https://`. Se rechazan esquemas `http://`, `file://`, `ftp://`, `data:` o `gopher:`.
2. **Puerto Estricto:** Puerto 443 o por defecto. Se rechazan puertos alternativos como 8080, 8443, 22.
3. **Sin Credenciales:** Si la URL contiene información de usuario (`user:pass@`), la solicitud es rechazada de inmediato.
4. **Prohibición de IP Literales:** No se admiten hosts con formato de dirección IP (ej. `https://127.0.0.1/logo.png`).
5. **Lista Blanca Institucional:** El host debe pertenecer a los dominios configurados en `Reporting:AllowedLogoDomains` (`ihss.hn`, `sgrla.ihss.hn`, `www.ihss.hn`).
6. **Resolución DNS y Filtrado de Redes:**
   - Loopback (`127.0.0.0/8`, `::1`).
   - Redes Privadas RFC 1918 (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`).
   - Enlace Local / Metadatos de Cloud Providers (`169.254.0.0/16`, `fe80::/10`).
   - Carrier-Grade NAT (`100.64.0.0/10`).
   - Espacio Reservado y Multicast (`224.0.0.0/4`, `240.0.0.0/4`).
   - IPv6 Unique Local (`fc00::/7`) y mapeos IPv4 sobre IPv6 (`::ffff:`).
7. **Mitigación de DNS Rebinding:** El socket se conecta explícitamente a la IP validada en la resolución DNS mediante `SocketsHttpHandler.ConnectCallback`.
8. **Sin Redirecciones:** `AllowAutoRedirect = false` para impedir que una URL autorizada redirija a un destino interno no autorizado.
9. **Límites de Recursos:** Timeout global de 5 segundos (conexión de 3s) y tope de payload de 2 MB con verificación de firma mágica.

---

## 4. INVENTARIO DE ARCHIVOS MODIFICADOS Y CREADOS

### Archivos Creados
- `backend/RL.API/Infrastructure/Reporting/InstitutionalJpegInspector.cs`: Inspector de segmentos JPEG SOF0/SOF1/SOF2.
- `backend/RL.API/Infrastructure/Reporting/InstitutionalLogoDownloader.cs`: Servicio de descarga segura con protección SSRF y DNS rebinding.
- `backend/RL.API.Tests/Infrastructure/Reporting/InstitutionalColorIndependenceTests.cs`: Batería de pruebas de independencia de color en PDF, Excel y Matrices.
- `backend/RL.API.Tests/Infrastructure/Reporting/InstitutionalLogoDownloaderTests.cs`: Batería de pruebas de validación SSRF, allowlist, rangos IP e inspección JPEG.

### Archivos Modificados
- `backend/RL.API/Infrastructure/Reporting/InstitutionalReportStandard.cs`: Definición de tokens constantes `#000000` y desacoplamiento de paleta.
- `backend/RL.API/Infrastructure/Reporting/InstitutionalBrandingSnapshot.cs`: Soporte para JPEG y PNG con dimensiones y MIME canónicos.
- `backend/RL.API/Infrastructure/Reporting/InstitutionalBrandingResolver.cs`: Integración de descargador HTTPS, caché en memoria y método `ClearCache()`.
- `backend/RL.API/Infrastructure/Reporting/InstitutionalPdfDocument.cs`: Texto de secciones, párrafos, tablas y grids forzado a negro `#000000` bold; soporte `/Filter /DCTDecode`.
- `backend/RL.API/Infrastructure/Reporting/InstitutionalXlsxWorkbook.cs`: Estilos OpenXML forzados a `FF000000` en fuentes de sección y KPI; relaciones y content types JPEG/PNG.
- `backend/RL.API/Features/Configuracion/ConfiguracionController.cs`: Invocación a `ClearCache()` al guardar configuración del sistema.
- `backend/RL.API/Program.cs`: Registro de `IInstitutionalLogoDownloader` en el contenedor de dependencias.
- `backend/RL.API/appsettings.json`: Configuración institucional de `Reporting:AllowedLogoDomains`.
- `frontend/rl-app/src/app/core/reporting/institutional-report-standard.ts`: Colores de cuerpo y filtros en `[0, 0, 0]` y `FF000000`.
- `frontend/rl-app/src/app/core/reporting/institutional-report.util.ts`: Caja de filtros con tipografía negrita y texto negro.

---

## 5. RESULTADOS REALES DE PRUEBAS Y VALIDACIONES

Todas las verificaciones fueron ejecutadas en esta intervención con resultados reales:

| Validación | Comando | Resultado Real |
| :--- | :--- | :--- |
| **Backend Build** | `dotnet build RIESGO_LAVADO.sln --configuration Release` | **PASS (0 errores)** |
| **Pruebas de Independencia de Color y SSRF** | `dotnet test --filter "InstitutionalColorIndependenceTests|InstitutionalLogoDownloaderTests"` | **61/61 PASS (0 errores)** |
| **Pruebas de Reportería Institucional** | `dotnet test --filter "FullyQualifiedName~Institutional"` | **154/154 PASS (0 errores, 1 skip XE)** |
| **Pruebas Unitarias de Backend** | `dotnet test --filter "FullyQualifiedName!~OracleIntegration&FullyQualifiedName!~RepositoryIntegrationTests&FullyQualifiedName!~SyntheticPurge"` | **1,246/1,246 PASS (0 errores)** |
| **Frontend Tests** | `npm test -- --watch=false` | **1,033/1,033 PASS (96 suites)** |
| **Frontend Build (Producción)** | `npm run build` | **PASS (0 errores)** |
| **Estructura del Repositorio** | `validate_repository_structure.ps1` | **PASS (118 rutas, 1,289 archivos, 3 maestros SQL)** |
| **Scripts de Base de Datos** | `validate_database_scripts.ps1` | **PASS (Paridad de esquemas y Unicode)** |
| **Enlaces de Documentación** | `validate_documentation_links.ps1` | **PASS (253 docs Markdown, 266 enlaces)** |
| **Agent Skills** | `python tools/validate_agent_skills.py` | **PASS (14 skills válidas)** |

---

## 6. GOBERNANZA, SEGURIDAD Y RESTRICCIONES

- **Rama `main`:** Preservada intacta (sin modificaciones, sin merge).
- **Oracle Institucional:** 0 escrituras (DML = 0, DDL = 0).
- **Entorno de Producción:** Intacto (0 modificaciones).
- **Contratos REST y IDs de Módulos:** 100% preservados.
- **Motor de Cálculo y Fórmulas:** Intacto (82 campos de Matrices de Riesgos preservados).
- **Archivos Untracked Locales Ajenos:** 34 archivos preservados intactos.
- **Seguridad de Dependencias (Picomatch):** Monitoreo activo en Issue #42 preservado.

---

## 7. DICTAMEN TÉCNICO

La Microfase 2 (M2) cumple integralmente con los criterios de aceptación técnicos y visuales establecidos por el Propietario. Los reportes PDF y Excel del SGRLA-IHSS garantizan legibilidad absoluta con independencia de color, soporte seguro para logotipos locales y remotos HTTPS contra SSRF, y paridad estricta entre listados, vistas detalladas y exportaciones.

Estado: **`CERTIFIED_TECHNICALLY_READY_FOR_OWNER_VISUAL_REVIEW`**  
Punto de Continuación: Revisión visual y aprobación final por parte de Javier Mejía (`jmejia31`).
