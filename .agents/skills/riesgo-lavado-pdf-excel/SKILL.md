---
name: riesgo-lavado-pdf-excel
description: Implementa y revisa reportes PDF y Excel de RIESGO_LAVADO. Usar para exportaciones, formatos institucionales, encabezados, columnas, paginación, impresión, datos, estilos y paridad funcional entre PDF y Excel.
---

# PDF y Excel institucional

## Principio

PDF y Excel que representan el mismo reporte deben conservar paridad de datos, filtros, totales, orden y semántica aunque su presentación sea distinta.

## Flujo

1. Identificar fuente de datos y filtros compartidos.
2. Determinar implementación actual antes de crear un generador nuevo.
3. Preservar nombres institucionales, unidades, fechas, totales y criterios de ordenamiento.
4. Verificar columnas largas, saltos de página, encabezados repetidos, impresión y ancho de contenido.
5. No corregir solo una salida si el defecto proviene de una fuente común.
6. No introducir cálculos divergentes entre UI, PDF y Excel.
7. Añadir regresión cuando sea automatizable.

## Checklist

- mismos registros y conteos;
- mismos filtros y periodo;
- mismos totales/cálculos;
- textos y acentos correctos;
- encabezado institucional;
- fechas y zonas horarias coherentes;
- Excel imprimible y usable;
- PDF sin recortes/solapamientos;
- archivo generado abre correctamente.

Si la comparación visual requiere revisión humana, declararla separadamente de las pruebas automatizadas.
