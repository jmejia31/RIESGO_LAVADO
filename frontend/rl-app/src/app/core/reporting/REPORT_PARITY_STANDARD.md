# Estándar institucional de paridad PDF/Excel

Todo reporte que se publique simultáneamente en PDF y Excel debe construir ambos formatos desde la misma definición lógica: título, institución, sistema, fecha de generación, secciones, datos generales, párrafos, encabezados, filas, filtros, resúmenes, rangos y mensajes sin registros.

## Reglas obligatorias

1. El PDF aprobado no debe perder información ni cambiar su terminología funcional.
2. Excel debe reproducir el mismo contenido, orden y nivel de detalle del PDF.
3. Identificadores como RTN, DNI, identidad y número patronal deben conservarse como texto.
4. La auditoría debe completarse antes de descargar el archivo.
5. Las tablas deben usar encabezado institucional, autofiltro, panel congelado y ajuste de impresión.
6. Los módulos deben utilizar `institutional-report-parity.util.ts` en lugar de construir manualmente estructuras divergentes.
7. Toda incorporación debe incluir una prueba de regresión que compare secciones, encabezados, filas y mensajes vacíos.

## Cobertura implementada

- Monitoreo de Listas - Coincidencias jurídicas: lista principal y ficha integral.
- Monitoreo de Listas - Personas naturales: lista principal, ficha integral y reporte detallado.
- Monitoreo de Listas - Empleados: lista principal, ficha integral y reporte detallado.
- Matrices de Riesgo: conserva el estándar institucional aprobado previamente.

Este contrato queda disponible para las siguientes migraciones de reportería del sistema.
