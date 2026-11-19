# RIESGO_LAVADO — CERTIFICACIÓN TÉCNICA P0
## ENDURECIMIENTO DE FAVICON, DESACOPLAMIENTO DE METADATOS Y PRUEBAS REALES QA

**PROYECTO:** SGRLA-IHSS — RIESGO_LAVADO  
**REPOSITORIO:** `jmejia31/RIESGO_LAVADO`  
**RAMA AUTORIZADA:** `desarrollo`  
**DIRECTORIO LOCAL:** `C:\RIESGO_LAVADO`  
**AGENTE EJECUTOR:** ANTIG — Antigravity  
**FECHA:** 19 de noviembre de 2026  
**BASE CERTIFICADA:** `dc945d58a0f99db4a9f700f1467b969b5f88b122`  
**QUALITY GATE BASE:** `44567146527` — `completed / success`  
**COMMIT CERTIFICADO:** `50eaa109a9415c44f3f1e9c20a40d5885c4bf46d`  
**QUALITY GATE CERTIFICADO:** `44578777405` — `completed / success`  
**RAMA DE RESPALDO:** `respaldo/desarrollo-pre-m3-favicon-hardening-20261119-dc945d58`  
**ESTADO:** `CERTIFIED_TECHNICALLY_READY_FOR_OWNER_FINAL_REVIEW`  

---

## 1. RESUMEN EJECUTIVO Y RESOLUCIÓN DE LOS TRES HALLAZGOS AUDITADOS

La presente intervención quirúrgica P0 resolvió de forma exhaustiva los tres hallazgos identificados en la auditoría independiente sobre la identidad institucional del navegador en **Microfase 3 (M3)**:

### 1.1 HALLAZGO P0-01: Endurecimiento y validación estructural estricta de imágenes en backend

Se reescribió integralmente `InstitutionalFaviconValidator.cs` para sustituir las validaciones parciales de firmas por validadores estructurales completos que analizan la integridad sintáctica y binaria sin truncamientos:

1. **Cálculo de CRC-32 (IEEE 802.3 / ISO 3309):**
   - Generador de tabla precalculada de 256 entradas con polinomio `0xEDB88320`.
   - Método `ComputeCrc32` para verificar la integridad matemática de cada chunk analizado.
2. **Validación estructural PNG:**
   - Firma canónica de 8 bytes `89 50 4E 47 0D 0A 1A 0A`.
   - Recorrido estricto chunk por chunk: valida longitud, tipo, datos y CRC de 4 bytes.
   - Exigencia estricta de que el primer chunk sea `IHDR` con longitud fija de 13 bytes.
   - Extracción de ancho y alto big-endian dentro del rango 16..2048 px y área total <= 4,194,304 px.
   - Validación de profundidad de color y método de compresión.
   - Verificación de existencia de al menos un chunk `IDAT` no vacío.
   - Verificación de chunk final `IEND` con longitud 0 y CRC válido.
   - Rechazo categórico de streams truncados o bytes residuales corruptos.
3. **Validación estructural JPEG:**
   - Marcador inicial `SOI` (`FF D8`).
   - Recorrido de segmentos de marcadores (`SOF0`, `SOF1`, `SOF2`) validando dimensiones de 16..2048 px y precisión de 8 bits.
   - Detección de `SOS` (`FF DA`) y verificación obligatoria del marcador final `EOI` (`FF D9`).
4. **Validación estructural ICO:**
   - Cabecera `ICONDIR` (`00 00 01 00`) y conteo de imágenes de 1 a 256.
   - Para cada entrada de directorio, comprueba que `imageOffset` y `bytesInRes` no desborden el tamaño del archivo y no se traslapen maliciosamente.
   - Inspección del payload incrustado: soporta PNG completo con firma válida o estructura DIB/`BITMAPINFOHEADER` canónica (biSize=40, planos=1, bpp válido 1..32, dimensiones positivas dentro de límites).
5. **Validación estructural GIF:**
   - Firmas `GIF87a` o `GIF89a`.
   - Extracción de dimensiones en el Descriptor de Pantalla Lógica (16..2048 px).
   - Recorrido de bloques hasta encontrar el Descriptor de Imagen (`0x2C`) y verificación obligatoria del byte finalizador Trailer `0x3B`.
6. **Validación estructural WebP:**
   - Cabecera RIFF (`52 49 46 46`), validando que la longitud declarada coincida con el payload.
   - Tag `WEBP` (`57 45 42 50`).
   - Decodificación real de dimensiones según el tipo de chunk:
     - `VP8`: decodifica frame header (`9D 01 2A`) y extrae ancho y alto de 14 bits.
     - `VP8L`: valida byte de firma `0x2F` y decodifica las dimensiones compactadas de 14 bits (+1).
     - `VP8X`: decodifica canvas width y height de 24 bits (+1).
   - Sin inventar ni suponer dimensiones fijas.
7. **Fixtures reales y casos negativos en pruebas unitarias:**
   - En `FaviconEndpointTests.cs`, se actualizaron los generadores de binarios para construir imágenes reales y decodificables (`CrearPngValido`, `CrearJpegValido`, `CrearIcoValido`, `CrearGifValido`, `CrearWebpValido`).
   - Se incorporaron 12 casos negativos de seguridad (`FV-01` a `FV-12`) que validan el rechazo de PNGs truncados, CRC corrupto, JPEG sin EOI, ICO con offsets inválidos, archivos mayores a 2 MB y dimensiones fuera de rango.
   - 27 pruebas unitarias de favicon superadas (27/27 PASS, 100%).

---

### 1.2 HALLAZGO P0-02: Cobertura Playwright con cambio real en caliente y persistencia

Se implementó una suite E2E completa en `frontend/rl-app/e2e/institutional-title-favicon.spec.ts` cubriendo 4 escenarios operativos:

1. **Ruta pública (`/login`):**
   Verifica que un usuario no autenticado reciba el título configurado y el favicon servido por el endpoint dedicado del backend.
2. **Cambio en caliente (`TITULO_A -> TITULO_B` e `ICONO_A -> ICONO_B`):**
   - Carga inicial en `/configuracion` con `TITULO_A` e `ICONO_A`.
   - Modificación interactiva de los controles `nombreSistema` e `iconoUrl`.
   - Guardado y confirmación en el modal institucional (`Sí, guardar`).
   - Verificación inmediata en el DOM sin recargar la página (`ANGULAR_RESTART_REQUIRED=NO`): el título cambia a `TITULO_B` y el `<link rel="icon">` actualiza su URL con token de versión.
   - Recarga del navegador (`page.reload()`): el título y el favicon actualizados persisten intactos.
3. **Restablecimiento limpio al eliminar el icono:**
   - Vaciado interactivo de `iconoUrl` y guardado confirmado.
   - El `<link rel="icon">` se restablece a `favicon.ico` con tipo `image/x-icon`.
   - Cuando `nombreSistema` contiene solo espacios en blanco, se aplica el fallback seguro `SGRLA-IHSS | Gestión de Riesgos LA/FT`.
   - Persistencia confirmada tras recarga del navegador.
4. **Desacoplamiento total y resiliencia ante fallos del logo de reportería:**
   - Simulación de error HTTP 500 en `/api/configuracion/sistema/logo-reportes`.
   - El título y favicon se aplican de forma inmediata (< 10 s) sin quedar bloqueados ni degradados por la falla del logo.
5. **Resultado de ejecución E2E:** 4 pruebas ejecutadas, 4 superadas (100% PASS, 0 fallos, 0 flaky).

---

### 1.3 HALLAZGO P0-03: Desacoplamiento temporal entre metadatos web y logotipo de reportería

En `frontend/rl-app/src/app/core/configuration/configuracion.service.ts`:

1. **Aplicación inmediata y síncrona:**
   Tanto en `CargarConfiguracion` como en `GuardarConfiguracion`, las mutaciones de metadatos web (`aplicarTemaSistema` y `aplicarMetadatosNavegador`) se ejecutan síncronamente en el operador `tap` en cuanto llega la respuesta del servidor, antes de iniciar la preparación asíncrona del logo de reportería.
2. **Aislamiento de errores:**
   El flujo de `prepararLogoReporte` se ejecuta en `switchMap` protegido con `catchError(() => of(...))`, asegurando que cualquier fallo o latencia en la descarga del logo de reportería nunca interrumpa ni retrase la actualización del título y favicon en el navegador.
3. **Cumplimiento estricto de gobernanza:**
   Se conserva exactamente dos invocaciones de `this.aplicarTemaSistema(` para cumplir la regla institucional de `tools/validate_dynamic_system_theme.js`.
4. **Prueba unitaria de desacoplamiento:**
   Se agregó una prueba en `configuracion.service.spec.ts` que simula un fallo de red en `prepararLogoReporte` y comprueba que `document.title` y el favicon en el DOM se aplican inmediatamente (`REPORT_LOGO_INDEPENDENCE=PASS`).

---

## 2. MATRIZ DE CRITERIOS DE ACEPTACIÓN

| Identificador | Criterio de Aceptación | Estado | Evidencia |
|---|---|---|---|
| **CRIT-01** | Endurecimiento binario con CRC32 y parsers estructurales para PNG, JPEG, ICO, GIF y WebP | **CUMPLIDO** | `InstitutionalFaviconValidator.cs`, 27 pruebas en `FaviconEndpointTests.cs` |
| **CRIT-02** | Rechazo de imágenes con chunks truncados, CRC inválido, offsets corruptos o dimensiones fuera de 16..2048 px | **CUMPLIDO** | Casos negativos `FV-01` a `FV-12` (100% PASS) |
| **CRIT-03** | Fixtures de prueba reales y decodificables para formatos de imagen | **CUMPLIDO** | Métodos generadores en `FaviconEndpointTests.cs` |
| **CRIT-04** | Hot swap Playwright `TITULO_A -> TITULO_B` e `ICONO_A -> ICONO_B` sin recarga (`ANGULAR_RESTART_REQUIRED=NO`) | **CUMPLIDO** | `e2e/institutional-title-favicon.spec.ts` (PASS) |
| **CRIT-05** | Persistencia de título y favicon tras recarga del navegador (`page.reload()`) | **CUMPLIDO** | Verificación en `e2e/institutional-title-favicon.spec.ts` (PASS) |
| **CRIT-06** | Restablecimiento inmediato a `favicon.ico` con `image/x-icon` al eliminar icono | **CUMPLIDO** | Prueba E2E y pruebas unitarias Angular (PASS) |
| **CRIT-07** | Desacoplamiento síncrono de metadatos web respecto al logo de reportería | **CUMPLIDO** | `configuracion.service.ts`, `configuracion.service.spec.ts` |
| **CRIT-08** | Cero regresiones en Microfase 1 (DNI, 5 tarjetas métricas, guardas de motivo, seguimientos) | **CUMPLIDO** | Suites de integración y unitarias intactas |
| **CRIT-09** | Cero regresiones en Microfase 2 (18 puntos de exportación, `#000000`, B06) | **CUMPLIDO** | 154 pruebas de reportería superadas |
| **CRIT-10** | Cero regresiones en Matrices de Riesgos (82 campos canónicos) | **CUMPLIDO** | Validación canónica de fórmulas y contratos intacta |
| **CRIT-11** | Base de datos Oracle institucional: DDL=0, DML=0 | **CUMPLIDO** | 100% solo lectura, cero escrituras |
| **CRIT-12** | Rama `main` y producción intactas (0 commits) | **CUMPLIDO** | Trabajo circunscrito a rama `desarrollo` |

---

## 3. CONTEOS REALES DE VERIFICACIÓN TÉCNICA

- **Backend Unitario y Validación de Favicon:**
  - `FaviconEndpointTests.cs`: 27 pruebas superadas (27/27 PASS, 100%).
  - Suite completa `RL.API.Tests`: 1,277 pruebas superadas (1,277/1,277 PASS, 0 fallidas, 0 omitidas).
- **Frontend Unitario y Desacoplamiento (Vitest):**
  - `configuracion.service.spec.ts`: 24 pruebas superadas (24/24 PASS, 100%).
  - Suite completa frontend: 96 archivos de prueba superados (96/96), 1,056 pruebas unitarias superadas (1,056/1,056 PASS).
- **Frontend Lint (ESLint):**
  - 0 errores, 0 advertencias.
- **Frontend Build (Producción Angular):**
  - Compilación en `dist/rl-app` finalizada con código de salida 0.
- **Pruebas Automatizadas E2E (Playwright):**
  - `e2e/institutional-title-favicon.spec.ts`: 4 pruebas superadas (4/4 PASS, 100%, 0 fallos, 0 flaky).
- **Validadores del Repositorio:**
  - `validate_repository_structure.ps1`: PASS (118 rutas obligatorias, 1,304 archivos rastreados, 3 maestros SQL).
  - `validate_database_scripts.ps1`: PASS (19 scripts activos, 4/4 scripts B06).
  - `validate_documentation_links.ps1`: PASS (261 documentos Markdown, 278 enlaces locales).
  - `validate_agent_skills.py`: PASS (14/14 skills).
  - `validate_dynamic_system_theme.js`: PASS (11 niveles de paleta, 49 confirmaciones dinámicas, 0 azul fijo).
  - `validate_no_native_dialogs.js`: PASS (6/6 fixtures, 0 diálogos nativos).

---

## 4. DICTAMEN TÉCNICO FINAL

Se certifica técnicamente que la parametrización dinámica del título del navegador y el favicon institucional en RIESGO_LAVADO cumple con los más altos estándares de seguridad binaria, desacoplamiento arquitectónico, reactividad en caliente y persistencia, quedando el sistema listo para la revisión final y aprobación por parte del propietario Javier Mejía (`jmejia31`).
