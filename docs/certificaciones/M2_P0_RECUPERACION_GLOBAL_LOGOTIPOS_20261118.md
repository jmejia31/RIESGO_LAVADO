# Certificación Técnica M2 — Recuperación Global de Logotipos Institucionales en PDF, Excel y Vistas Previas

**Fecha:** 18 de noviembre de 2026
**Proyecto:** SGRLA-FT IHSS — RIESGO_LAVADO
**Repositorio:** `jmejia31/RIESGO_LAVADO`
**Rama:** `desarrollo`
**Autor:** ANTIG — Antigravity
**Aprobador Final:** Javier Mejía (`jmejia31`)
**Prioridad:** P0 — Defecto transversal de visualización de logotipo y sincronización de previsualizaciones
**Estado:** `CERTIFIED_TECHNICALLY_READY_FOR_OWNER_VISUAL_REVIEW`

---

## 1. Resumen Ejecutivo

Durante la revisión visual por parte del propietario del sistema se identificaron dos defectos visuales en reportería:
1. **Ausencia del Logotipo Institucional en exportaciones y previsualizaciones:** A pesar de estar configurada una URL HTTPS válida en `SFS_LOGO_URL` (`https://www.ihss.hn/wp-content/uploads/2020/06/logo-del-IHSS.jpg`), los reportes PDF y Excel generados en el frontend (Monitoreo de Listas, Coincidencias de Patrono, Coincidencias de Empleado, Carga de Listas) se generaban sin el logotipo institucional.
2. **Icono de Imagen Rota en Vista Previa de Excel:** En la vista previa modal de libros Excel cargados desde backend (como la Matriz Consolidada de Riesgos), se renderizaba un icono de imagen rota con texto alternativo "Logotipo institucional".

### 1.1 Causas Raíz Confirmadas

1. **Rechazo preventivo excesivo en frontend:** En `frontend/rl-app/src/app/core/configuration/configuracion.service.ts`, el método `prepararLogoReporte` contenía un filtro regex que únicamente aceptaba rutas locales que comenzaran con `/uploads/...`. Al encontrar la URL HTTPS institucional `https://www.ihss.hn/...`, descartaba la imagen retornando `NO_REPORT_LOGO` (`dataUrl: ''`), por lo que el objeto global `InstitutionalReportBranding` quedaba con `logoUrl: ''`.
2. **Desincronización en la vista previa Excel:** `report-preview.component.html` mostraba la etiqueta `<img>` si `state.logoEmbedded` era verdadero. Sin embargo, la URL provista por `institutionalLogoUrl()` estaba vacía (`""`), provocando que el navegador intentara renderizar `<img src="" alt="Logotipo institucional">`, produciendo el icono de imagen rota.
3. **Restricción de tipo de imagen en generadores OpenXML y jsPDF:** Los utilitarios de exportación requerían soporte explícito para datos JPEG además de PNG, y no debían depender del contexto de Canvas en entornos sin aceleración 2D.

---

## 2. Acciones y Correcciones Implementadas

### 2.1 Backend (`backend/RL.API`)
- `ConfiguracionController.cs`:
  - `GET /api/configuracion/sistema/logo-reportes`: Se agregó cabecera `Cache-Control: no-store` y telemetría `X-Logo-Status: available / {source}`.
  - `GET /api/configuracion/sistema/logo-reportes/estado`: Nuevo endpoint diagnóstico institucional de solo lectura que expone el estado de resolución del logotipo (`configurado`, `disponible`, `tipo`, `estado`, dimensiones y relación de aspecto).

### 2.2 Frontend Core (`frontend/rl-app/src/app/core`)
- `institutional-report-standard.ts`: Se añadieron campos de trazabilidad `logoStatus` y `logoErrorMessage` al contrato `InstitutionalReportBranding`.
- `configuracion.service.ts`:
  - Método `clasificarOrigenLogo`: Clasifica la URL en `SIN_LOGO`, `LOGO_LOCAL`, `LOGO_HTTPS` o `LOGO_INVALIDO`.
  - Método `prepararLogoReporte`: Para orígenes `LOGO_LOCAL` y `LOGO_HTTPS`, solicita la imagen validada y sanitizada al endpoint institucional del backend (`/api/configuracion/sistema/logo-reportes`).
  - Método `convertirLogoEnPng`: Validación estricta de MIME (`image/png`, `image/jpeg`), dimensiones (máx 4096×4096 px), tamaño (máx 10 MB). Incorpora fallback seguro mediante `FileReader` / ArrayBuffer en caso de ausencia de Canvas 2D.
- `excel-export.util.ts`:
  - Soporte universal de formatos PNG y JPEG en data URLs (`/^data:image\/(png|jpeg|jpg)/i`).
  - Integración directa en `construirLibroInstitucional(workbook)`: todo libro generado incorpora el logotipo institucional preservando su relación de aspecto y dimensiones canónicas B06.
- `institutional-report.util.ts`:
  - Soporte universal PNG y JPEG en `dibujarLogoInstitucionalPdf`.

### 2.3 Componente de Vista Previa (`frontend/rl-app/src/app/shared/report-preview`)
- `report-preview.service.ts`:
  - En `openExcelBlob`, cuando el libro OpenXML contiene imágenes, extrae directamente la imagen embebida (`workbook.getImage(firstImage.imageId)`), garantizando paridad exacta `PREVIEW_LOGO_HASH = DOCUMENT_LOGO_HASH`.
  - Pasa `logoUrl`, `logoStatus` y `logoErrorMessage` al estado de la vista previa.
- `report-preview.component.ts`:
  - Señal computada `previewLogoUrl`: Prioriza la imagen real extraída del archivo OpenXML o la provista por el branding institucional.
  - Señal `logoFailed` con manejador `onLogoError()` para suprimir cualquier intento fallido sin mostrar icono de imagen rota.
- `report-preview.component.html`:
  - Si no hay imagen válida o la carga falla, no se renderiza la etiqueta `<img>`.
  - Si el estado es `inaccesible` o `invalido`, muestra una alerta institucional estilizada y accesible en lugar de un elemento HTML roto.

---

## 3. Cobertura de Pruebas y Evidencias de Verificación

| Ámbito | Archivo de Prueba | Pruebas Ejecutadas | Resultado | Observaciones |
| :--- | :--- | :---: | :---: | :--- |
| **Frontend Config** | `configuracion.service.spec.ts` | 15 / 15 | **PASS** | Verifica solicitud segura al backend para HTTPS y local, manejo de 204 y estados `disponible`/`inaccesible`. |
| **Frontend Preview** | `report-preview.service.spec.ts` | 5 / 5 | **PASS** | Verifica estado de previsualización Excel y extracción de imagen. |
| **Frontend Excel** | `excel-export.util.spec.ts` | 15 / 15 | **PASS** | Incrustación de logotipo PNG y JPEG en OpenXML `xl/media/` y preservación de geometría. |
| **Frontend Reporting** | `institutional-report-standard.spec.ts` | 13 / 13 | **PASS** | Contratos de colores, orientación y branding institucional. |
| **Frontend PDF** | `institutional-report.util.spec.ts` | 10 / 10 | **PASS** | Renderizado de reportes PDF con cabecera institucional. |
| **Frontend Monitoreo** | `monitoreo-listas/*.spec.ts` | 48 / 48 | **PASS** | Paridad Excel/PDF y presentación de listas de cautela. |
| **Frontend Regresión** | `export-preview.regression.spec.ts` | 5 / 5 | **PASS** | Descarga y vista previa institucional. |
| **Backend Reporting** | `InstitutionalReportStandardTests.cs` | 21 / 21 | **PASS** | Incrustación de logotipo `/ImIHSS` en PDF y dibujos OpenXML en XLSX para PNG y JPEG. |
| **Backend Colors** | `InstitutionalColorIndependenceTests.cs` | 7 / 7 | **PASS** | Independencia absoluta de color de contenido (`#000000`) frente al banner de encabezado. |
| **Backend SSRF** | `InstitutionalLogoDownloaderTests.cs` | 15 / 15 | **PASS** | Whitelist de dominios IHSS y bloqueo de IPs privadas/restringidas. |
| **Backend Config** | `ConfiguracionModuleCharacterizationTests.cs` | 59 / 59 | **PASS** | Controladores, persistencia y contratos de configuración. |
| **Validadores** | 10 scripts de repositorio | 10 / 10 | **PASS** | Base de datos, enlaces, estructura, skills, modales, menús y temas. |

---

## 4. Preservación Institucional y Restricciones

- **Microfase 1:** Preservada al 100% (agrupación por DNI, 5 tarjetas de filtro, motivos universales y seguimientos documentales).
- **Colores de Contenido:** Preservados estrictamente en `#000000` (negro) y negrita para títulos (`FILTROS APLICADOS`, códigos de evaluación, etiquetas clave/valor).
- **Geometría B06:** Ancho canónico 72.644 mm (205.92 pt), altura máxima 90.72 pt y proporciones de aspecto preservadas.
- **Base de Datos Oracle:** DDL = 0, DML = 0. Cero cambios en esquema o tablas institucionales.
- **Rama `main`:** Cero modificaciones. Todo trabajo se mantiene en `desarrollo`.
