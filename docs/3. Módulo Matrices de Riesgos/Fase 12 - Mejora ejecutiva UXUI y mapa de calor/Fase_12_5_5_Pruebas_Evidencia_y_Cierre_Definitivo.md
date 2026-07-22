# Fase 12.5.5 - Pruebas, evidencia y cierre definitivo

## Estado

**Cierre técnico aprobado.** Pendiente únicamente de validación Oracle en un entorno institucional autorizado, aprobación formal de Javier Mejía y posterior integración autorizada a `main`.

## Validación automatizada

- Backend: 96 aprobadas, 0 fallidas, 0 omitidas.
- Frontend: 156 aprobadas en 15 archivos.
- E2E: 7 aprobadas, 0 fallidas.
- Build Angular: aprobado.
- Cobertura backend: líneas 20.56 %, ramas 20.86 %.
- Cobertura frontend: sentencias 36.59 %, ramas 30.24 %, funciones 33.68 %, líneas 37.06 %.

## Archivos oficiales reales

Los artefactos de validación fueron generados directamente por `MatricesRiesgosReportRenderer`:

- `reporte_ejecutivo_matrices.pdf`.
- `reporte_matrices.xlsx`.
- `ficha_individual_matriz.pdf`.

Se validaron estructura, contenido extraíble, hojas OpenXML, tamaño, checksum y capacidad de renderizado.

## Oracle

Validación Oracle real no ejecutada en CI por ausencia de conectividad, credenciales y autorización institucional; dependencia externa documentada.

No se declara una ejecución contra Oracle institucional porque el runner no dispone de conectividad, credenciales ni autorización para ese entorno. Los scripts SQL de validación y las pruebas de persistencia permanecen disponibles para su ejecución controlada.

## Restricciones

- No se modificó DNP.
- No se tocó `CONTROL_ALMACEN.PROVEEDOR`.
- No se integró funcionalmente Monitoreo con Matrices.
- No se modificó el motor de cálculo.
- No se fusionó a `main`.

## Próxima decisión

Aprobación formal de Javier Mejía. Solo después de esa autorización podrá evaluarse la integración del PR principal a `main`.
