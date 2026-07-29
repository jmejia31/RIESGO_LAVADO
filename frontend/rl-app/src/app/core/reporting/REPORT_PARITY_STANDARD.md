# Estándar institucional de paridad PDF/Excel

Todo reporte publicado simultáneamente en PDF y Excel debe construir ambos formatos desde la misma definición lógica: título, institución, sistema, fecha de generación, secciones, datos generales, párrafos, encabezados, filas, filtros, resúmenes, rangos y mensajes sin registros.

## Reglas obligatorias

1. El PDF aprobado no debe perder información ni cambiar su terminología funcional.
2. Excel debe reproducir el mismo contenido, orden y nivel de detalle del PDF.
3. Identificadores como RTN, DNI, identidad, documento y número patronal deben conservarse como texto.
4. La auditoría debe completarse antes de descargar el archivo.
5. Las tablas deben usar encabezado institucional, panel congelado, anchos controlados y ajuste de impresión. El autofiltro se aplica cuando existe una tabla principal continua y no debe abarcar secciones heterogéneas.
6. Cada módulo debe reutilizar el generador institucional correspondiente a la capa propietaria del archivo:
   - Monitoreo de Listas en Angular: `institutional-report-parity.util.ts` y `excel-export.util.ts`.
   - Matrices de Riesgos en Backend: `MatricesRiesgosReportRenderer` e `InstitutionalXlsxWorkbook`.
7. Un reporte ejecutivo equivalente a un PDF continuo debe salir en una sola hoja Excel, salvo requerimiento funcional expreso que autorice múltiples hojas.
8. Toda incorporación debe incluir una prueba de regresión que compare secciones, encabezados, filas, valores, mensajes vacíos y cantidad de hojas.
9. Los archivos oficiales se generan desde la fuente de datos del reporte; no se reconstruyen manualmente en el cliente con contratos distintos.

## Cobertura implementada

- Monitoreo de Listas — Coincidencias jurídicas: lista principal y ficha integral.
- Monitoreo de Listas — Personas naturales: lista principal, ficha integral y reporte detallado.
- Monitoreo de Listas — Empleados: lista principal, ficha integral y reporte detallado.
- Matrices de Riesgos — Reporte ejecutivo PDF/Excel con siete secciones en el mismo orden y Excel de una sola hoja.
- Matrices de Riesgos — Ficha individual PDF generada en Backend.

Este contrato queda disponible para las siguientes migraciones de reportería del sistema. Cada ampliación debe registrarse en la bitácora y en el estado colaborativo vigente.
