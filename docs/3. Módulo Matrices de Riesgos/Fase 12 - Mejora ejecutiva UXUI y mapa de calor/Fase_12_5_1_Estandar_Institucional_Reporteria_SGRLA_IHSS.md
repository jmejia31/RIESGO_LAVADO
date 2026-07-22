# Fase 12.5.1 — Estándar Institucional Compartido de Reportería

## Estado

**En implementación y validación técnica.** Esta subfase define el patrón común; no sustituye todavía los generadores de Monitoreo de Listas ni Matrices de Riesgos.

## Inventario automatizado

- Archivos relacionados con reportería detectados: **60**.
- Monitoreo de Listas: **23**.
- Matrices de Riesgos: **19**.
- Compartidos u otros módulos: **18**.
- Posibles textos con caracteres dañados: **174**.

El detalle se conserva en `Evidencia_Fase_12_5_1/inventario_reporteria.json`.

## Referencias visuales aprobadas

1. El reporte horizontal de Monitoreo de Listas define la identidad base: encabezado azul marino, institución, título, fecha, filtros, resumen, tabla con encabezado azul, filas alternadas y numeración.
2. El reporte vertical de detalle conserva la misma identidad, con secciones numeradas y tablas de continuación.
3. El formato actual de Matrices de Riesgos se reemplazará en 12.5.3; no se reutilizará el diseño de texto plano.

## Reglas institucionales obligatorias

### Encabezado
- Franja azul marino de ancho completo.
- `INSTITUTO HONDUREÑO DE SEGURIDAD SOCIAL`.
- Título propio del reporte.
- `SGRLA-IHSS` y fecha/hora de generación.
- Usuario generador cuando aplique.

### Tablas
- Encabezado azul con texto blanco.
- Filas alternadas blanco/gris claro.
- Bordes discretos y texto legible.
- Encabezado repetido en cada página.
- Ninguna fila se divide entre páginas.
- Fechas e identificadores no se parten.
- El título de sección permanece con la primera fila.

### Orientación
- Vertical hasta 8 columnas visibles.
- Horizontal desde 9 columnas visibles o cuando el contenido no sea legible en vertical.
- Ambas orientaciones usan el mismo encabezado, paleta, pie y numeración.

### Pie
- Nombre del sistema.
- Fecha de generación cuando corresponda.
- `Página X de Y` cuando el motor soporte total de páginas.

### Excel
- Salida `.xlsx` real.
- Encabezado institucional, título, fecha y filtros.
- Autofiltro, panel congelado, ajuste de texto y anchos controlados.
- Configuración de impresión, orientación y filas repetidas.
- Sin HTML disfrazado de `.xls`.

## Arquitectura aprobada

La fuente compartida comienza en:

`backend/RL.API/Infrastructure/Reporting/InstitutionalReportStandard.cs`

En 12.5.2 y 12.5.3 se agregarán adaptadores concretos para el motor PDF y el motor Excel realmente utilizados por el repositorio. Monitoreo y Matrices seguirán separados funcionalmente; únicamente compartirán identidad y reglas documentales.

## Política de migración

1. Inventariar generadores actuales.
2. Normalizar Monitoreo como patrón en 12.5.2.
3. Reemplazar Matrices usando el mismo patrón en 12.5.3.
4. Prohibir nuevas implementaciones locales de encabezados, paletas o paginación.
5. Validar visualmente cada PDF renderizado y cada archivo Excel.

## Criterios de salida de 12.5.1

- Inventario generado.
- Estándar documentado.
- Paleta y reglas centralizadas en backend.
- Política de orientación y no partición probada.
- Sin cambios funcionales en Monitoreo o Matrices.
- Quality Gates aprobados.
