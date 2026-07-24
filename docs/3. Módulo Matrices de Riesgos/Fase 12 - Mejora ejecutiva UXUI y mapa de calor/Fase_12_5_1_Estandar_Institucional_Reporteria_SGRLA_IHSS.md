# Fase 12.5.1 — Estándar Institucional Compartido de Reportería

## Estado

**Completada y validada técnicamente.** Esta subfase define el patrón común; no sustituye todavía los generadores de Monitoreo de Listas ni Matrices de Riesgos. La adopción concreta se realizará en 12.5.2 y 12.5.3.

## Inventario automatizado

- Archivos relacionados con reportería detectados: **60**.
- Monitoreo de Listas: **23**.
- Matrices de Riesgos: **19**.
- Compartidos u otros módulos: **18**.
- Posibles textos con caracteres dañados: **174**.

El detalle se conserva en `Evidencia_Fase_12_5_1/inventario_reporteria.json`.

Los 174 hallazgos de codificación son candidatos de revisión y se concentran principalmente en evidencia histórica y documentación anterior. No significan que existan 174 defectos activos en la interfaz. Su limpieza se ejecutará de forma controlada en 12.5.4, preservando el contenido probatorio original cuando corresponda.

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

El estándar centraliza:

- Institución y nombre del sistema.
- Formatos de fecha y hora.
- Paleta institucional.
- Resolución de orientación vertical u horizontal.
- Regla preventiva para mover una fila completa a la página siguiente.
- Etiquetado uniforme de páginas.
- Metadatos y políticas de tabla.

En 12.5.2 y 12.5.3 se agregarán adaptadores concretos para el motor PDF y el motor Excel realmente utilizados por el repositorio. Monitoreo y Matrices seguirán separados funcionalmente; únicamente compartirán identidad y reglas documentales.

## Política de migración

1. Inventariar generadores actuales.
2. Normalizar Monitoreo como patrón en 12.5.2.
3. Reemplazar Matrices usando el mismo patrón en 12.5.3.
4. Prohibir nuevas implementaciones locales de encabezados, paletas o paginación.
5. Validar visualmente cada PDF renderizado y cada archivo Excel.

## Validación

- Commit funcional: `526dbb6c70739531a465a2a752dc60ff88ef910f`.
- Ejecución controlada: `29940191670`.
- Resultado: backend, frontend, build Angular y E2E aprobados.
- Pruebas nuevas: paleta institucional, orientación, no partición de filas y validación de metadatos.
- Auxiliares temporales eliminados del commit productivo.

## Criterios de salida de 12.5.1

- Inventario generado: **cumplido**.
- Estándar documentado: **cumplido**.
- Paleta y reglas centralizadas en backend: **cumplido**.
- Política de orientación y no partición probada: **cumplido**.
- Sin cambios funcionales en Monitoreo o Matrices: **cumplido**.
- Quality Gates aprobados: **cumplido**.

## Próxima subfase

**12.5.2 — Normalización de Monitoreo de Listas.** Se utilizará este estándar para corregir PDF horizontal, PDF vertical, vista previa y Excel del módulo patrón antes de migrar Matrices de Riesgos.
