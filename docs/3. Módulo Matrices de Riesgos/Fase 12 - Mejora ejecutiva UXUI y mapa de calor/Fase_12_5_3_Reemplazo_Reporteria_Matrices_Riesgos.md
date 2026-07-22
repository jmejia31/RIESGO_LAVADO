# Fase 12.5.3 — Reemplazo de reportería de Matrices de Riesgos

## Objetivo

Reemplazar las salidas heredadas de Matrices de Riesgos por archivos institucionales generados exclusivamente en backend.

## Entregables

- PDF ejecutivo horizontal.
- Ficha individual vertical por matriz.
- Libro `.xlsx` OpenXML real.
- Descarga frontend del archivo exacto producido por la API.
- Auditoría obligatoria para reporte general y ficha individual.

## PDF ejecutivo

El reporte ejecutivo usa encabezado y pie institucional, numeración, tablas con encabezados repetidos y filas indivisibles. Incluye filtros, indicadores, matrices filtradas, factores, mapa de transición, matrices críticas y planes.

## Ficha individual

La ficha vertical contiene identificación, resultado consolidado, variables evaluadas, controles, resultados vigentes, planes y evidencias.

## Excel

El libro OpenXML incluye hojas separadas para resumen, matrices, factores, mapa de transición, matrices críticas y planes. Cada hoja incorpora autofiltro, panel congelado, estilo institucional y configuración de impresión.

## Arquitectura

El backend es la fuente única de los bytes PDF y XLSX. Angular no reconstruye el documento; únicamente solicita, recibe y descarga el blob auditado.

## Restricciones

- No se modifica DNP.
- No se toca `CONTROL_ALMACEN.PROVEEDOR`.
- No se integra Monitoreo de Listas con Matrices de Riesgos.
- No se modifica el cálculo de riesgo.
- No se fusiona a `main`.
