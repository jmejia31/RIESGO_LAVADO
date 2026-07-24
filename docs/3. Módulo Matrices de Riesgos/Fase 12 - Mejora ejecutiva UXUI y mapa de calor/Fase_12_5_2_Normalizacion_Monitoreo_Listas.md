# Fase 12.5.2 — Normalización de Monitoreo de Listas

## Objetivo

Convertir Monitoreo de Listas en el patrón institucional definitivo para PDF y Excel antes de migrar Matrices de Riesgos.

## PDF institucional

- Encabezado único azul marino para orientación vertical y horizontal.
- Nombre institucional, título propio, sistema y fecha/hora.
- Caja de filtros en reportes generales.
- Encabezado compacto en páginas de continuación.
- Encabezados de tabla repetidos.
- `rowPageBreak: avoid` para impedir que un registro se divida entre páginas.
- Pie institucional con fecha y `Página X de Y`.
- Vista previa y descarga utilizan la misma composición visual.

## Excel institucional

Se sustituyó el SpreadsheetML/XML renombrado como `.xls` por un libro OpenXML `.xlsx` real mediante `exceljs@4.4.0`.

Cada hoja aplica:

- Propiedades institucionales del libro.
- Encabezado azul marino.
- Texto blanco y tipografía Arial.
- Filas alternadas.
- Bordes y ajuste de texto.
- Autofiltro.
- Panel congelado.
- Orientación vertical u horizontal según columnas.
- Ajuste a una página de ancho.
- Encabezado y pie de impresión.
- Extensión `.xlsx` real.

## Alcance técnico

- Tablas normalizadas detectadas: **10**.
- Encabezados compartidos en el componente: **2**.
- Finalizaciones institucionales de PDF: **3**.

## Restricciones

No se modifica DNP, `CONTROL_ALMACEN.PROVEEDOR`, el cálculo de Matrices de Riesgos ni `main`.

## Criterios de salida

- Backend, frontend, build y E2E aprobados.
- Pruebas del estándar PDF aprobadas.
- Excel real generado sin conversión a `.xls`.
- Sin scripts, workflows ni activadores temporales en el commit funcional.
