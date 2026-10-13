# Bloque 7 — Editor de evaluación en seis bloques

**Estado:** `BLOCK7_STATUS=PENDING_CORRECTION`
**Última intervención:** COD — 2026-10-13, America/Tegucigalpa
**Alcance:** crear y editar borradores desde el editor de Matrices de Riesgos, sin migración de los 59 casos de referencia y sin escrituras productivas.

## Implementación de esta intervención

- Se conservó el modal de evaluación existente y se organizó su edición en seis bloques con rangos 01–19, 20–33, 34–39, 40–49, 50–69 y 70–82.
- Se agregaron `POST /api/matrices-riesgos/evaluaciones/borrador` y `PUT /api/matrices-riesgos/evaluaciones/{id}/borrador`. Aceptan respuestas estructuralmente válidas con campos obligatorios aún pendientes; continúan rechazando valores inválidos y las identidades de riesgo o versión manipuladas. Los cálculos enviados por el cliente no se toman como autoridad.
- Se agregó `POST /api/matrices-riesgos/evaluaciones/calculo-preview`, que reutiliza el runtime de cálculo del backend y no persiste. La UI solicita la vista previa con debounce y cancela la petición previa al editar otro valor.
- Después de guardar, el editor vuelve a leer el detalle y la proyección institucional 82/82. Las escrituras de controles, planes, actividades y seguimiento notifican al editor para refrescar esa proyección. Las entidades repetibles continúan usando sus DTOs y endpoints estructurados existentes.
- Se retiró la llamada de recálculo del frontend al cambiar una respuesta. Los campos calculados/sistema se vuelven de solo lectura a partir del contrato `MATRIX_FIELDS`; GTIC se mantiene opcional porque no se encontró una regla autoritativa para inferir su activación.
- `tools/validate_block7_ui_contract.js` compara las 82 etiquetas visibles y los seis rangos con el manifest backend. El workflow de Quality Gates ejecutará este validador.

## Evidencia ejecutada localmente

- Release build del API: PASS.
- Suite backend: 955/955 PASS.
- Contract validator: 82/82 PASS.
- Catalog validator: 14 catálogos / 91 elementos, drift 0.
- Frontend aislado, fuera del checkout compartido: `npm ci`, build, lint y auditoría npm (0 vulnerabilidades) PASS; tests completos 846/846 PASS.
- Playwright: el flujo de crear borrador parcial, continuar y recargar pasó contra respuestas de API simuladas. Esta prueba no ejecuta Oracle ni acredita persistencia de una API real.
- Validadores de UTF-8, estructura del repositorio, scripts de base de datos y enlaces de documentación: PASS.

## Pendientes para el cierre

- El checkout no tiene Oracle XE aislado. El workflow actual ejecuta integración Oracle XE del GET de Bloque 6, no el POST/PUT de borradores ni el editor Playwright contra una API conectada a XE. Por tanto, `E2E_CREATE_EDIT_ORACLE` y `ORACLE_XE_OR_CI_INTEGRATION` para las escrituras de Bloque 7 siguen sin evidencia.
- Falta una prueba de integración de los endpoints de borrador/preview contra el repositorio Oracle y un recorrido de navegador conectado a esa API.
- No se certificaron aún pruebas de responsive 1280×900/390×844, accesibilidad de errores inline/foco, conflictos concurrentes de edición ni todos los estados de validación por bloque. La vista previa y navegación sí tienen pruebas unitarias/build, pero estos gates no se consideran cubiertos por ello.
- El trigger de GTIC permanece sin resolver en las fuentes institucionales revisadas; los campos 17–19 no se hacen obligatorios ni se inventan valores.

No se declara `BLOCK7_STATUS=CLOSED` mientras permanezcan estos pendientes. Bloques 1–6 continúan cerrados; Bloque 8 no se inició. Producción no se consultó ni modificó: `PRODUCTION_DML_EXECUTED=0`, `PRODUCTION_DDL_EXECUTED=0`, `PRODUCTION_APPLY_EXECUTED=NO`.

## Seguimiento de CI y pruebas E2E

- Quality Gates run `38453709129` (SHA `1379b5fe72d5d7d7ecb081ee6230ede663357608`) ejecutó con éxito validadores, analizadores, build Release y la integración Oracle XE de Bloque 6. Falló en tres E2E que todavía buscaban el formulario anterior: el botón "Guardar cambios", el botón "Crear evaluación" dentro del formulario y el campo F39 visible sin navegar al Bloque 3.
- Se corrigió la navegación de edición para permitir acceso directo a cualquier bloque de una evaluación existente y se actualizaron los E2E al flujo de borrador/bloques. En copia frontend aislada pasaron tests 846/846, lint, build y las suites Playwright `login-and-routing.spec.ts` + `matrices-uat-integral.spec.ts`: 22/22.
- El CI del cambio correctivo aún debe ejecutarse y concluir. Estas pruebas Playwright usan API simulada; no acreditan escritura real Oracle. Por ello se mantiene `BLOCK7_STATUS=PENDING_CORRECTION` hasta incorporar y pasar el E2E browser→API→Oracle y los gates de accesibilidad/responsive pendientes.
