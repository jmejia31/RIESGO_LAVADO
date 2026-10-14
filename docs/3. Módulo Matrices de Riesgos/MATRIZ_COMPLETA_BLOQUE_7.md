# Bloque 7 — Editor de evaluaciones en seis bloques

**Estado: `BLOCK7_STATUS=CLOSED`**
**Cierre técnico COD:** 2026-10-13 19:17 -06:00
**Rama:** `desarrollo`
**Commit funcional certificado:** `d3c0766e30981f8101accb90bf812b490a3cebc5`
**Quality Gates:** [run 38494209621](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/38494209621) — `completed / success`
**Bloque 8 al cierre de este documento:** `NOT_STARTED` (el cierre posterior está registrado en `MATRIZ_COMPLETA_BLOQUE_8.md`).

## Alcance certificado

El editor de evaluaciones existente trabaja en seis bloques funcionales y conserva la proyección institucional de lectura 01–82 como fuente backend. Crear y editar utiliza borradores parciales, validación por bloque, preview del motor backend, refresco después de guardar y DTOs estructurados para controles, planes y actividades. Los campos de sistema, calculados y derivados son de sólo lectura; la UI no contiene una segunda calculadora.

La suite mock de Playwright se mantiene para regresión rápida. El cierre añade integración real contra el Oracle XE desechable del Quality Gates actual y un E2E específico con Angular/API reales. El log del run certifica `BLOCK7_ORACLE_WRITE_INTEGRATION=PASS`, `BLOCK7_BROWSER_API_ORACLE_E2E=PASS`, `BLOCK7_REAL_BROWSER_TESTS_EXECUTED=1` y `BLOCK7_CRITICAL_MATRIX_API_ROUTES_MOCKED=0`.

## Contratos y bloques

| Bloque | Campos | Presentación y modo |
| --- | --- | --- |
| 1 | 01–19 | Identificación y riesgo inherente; F01 es de sistema, F12/F13 calculados y F17–F19 opcionales/condicionales. |
| 2 | 20–33 | Controles estructurados por tipo; ausencia representada sin filas “No hay”. |
| 3 | 34–39 | F34–F38 calculados y de sólo lectura; F39 usa la respuesta institucional. |
| 4 | 40–49 | Planes y actividades estructurados, múltiples y asociados a su plan; F41/F43 son derivados. |
| 5 | 50–69 | Detalle calculado, colapsable y sólo lectura. |
| 6 | 70–82 | Seguimiento posterior; no es requisito para la creación inicial. |

Las etiquetas y la partición UI se comparan con `matriz_riesgos_82_campos_manifest.json` mediante `tools/validate_block7_ui_contract.js` (`82/82`).

## Persistencia y cálculos

- `POST /api/matrices-riesgos/evaluaciones/borrador` y `PUT /api/matrices-riesgos/evaluaciones/{id}/borrador` persistieron drafts parciales reales en Oracle XE y se leyeron de vuelta.
- La actualización conserva la versión esperada del cliente. La prueba Oracle confirmó versión 1→2, rechazo de versión vieja con HTTP 409 sin pérdida de la escritura A, y recarga/guardado posterior hasta versión 3.
- `POST /api/matrices-riesgos/evaluaciones/calculo-preview` pasó con datos incompletos y completos. Alterar la identidad devolvió 400. Las capturas antes/después de preview confirmaron cero cambios a evaluación, cálculos, proyección, flujo, auditoría y version row.
- El motor canónico calculó los valores de prueba; valores de cálculo enviados manipulados no se conservaron como autoridad. No se modificaron las fórmulas del Bloque 5.
- El navegador recargó la página y reconstruyó la evaluación por API. El caso real cubrió dos controles preventivos, dos planes con fechas/responsables/presupuestos distintos y tres actividades; la lectura posterior verificó estructura y ordinales de plan.

## Accesibilidad y responsive

El navegador comprobó navegación por teclado de los seis bloques, nombre accesible y cierre del diálogo de matriz, `aria-expanded` de Bloque 5 (`false → true`) y 20 campos 50–69 de sólo lectura. La suite Playwright de regresión con API simulada también verifica `aria-invalid`, relación `aria-describedby` y foco en el primer campo inválido.

La suite real midió `scrollWidth <= clientWidth` en 1280×900 y 390×844 para Bloques 1, 2, 4 y 5 expandido. Resultado `HORIZONTAL_OVERFLOW=0`. Las ocho capturas de CI se conservaron fuera de Git en el directorio de auditoría de esta ejecución.

## GTIC

`GTIC_TRIGGER_SOURCE=NOT_DEFINED_IN_AUTHORIZED_SOURCES`. F17–F19 permanecen `CONDITIONAL_OPTIONAL`: blanks permitidos, no bloquean continuar, no se infiere aplicabilidad y el sistema no genera valores. El test Oracle probó round-trip de blanks y valores GTIC introducidos por usuario. No se afirma una regla institucional de activación automática.

## Verificación y CI

El Quality Gates `38494209621` publicó estos resultados:

- 1/1 integración opt-in de borrador/preview contra Oracle XE; no se omitió por falta de servicio.
- 1/1 Playwright real Angular→API→Oracle→API→Browser, con rutas críticas de Matrices sin interceptar.
- Backend: 959/959; frontend: 847/847; suite Playwright agregada del gate: 48 pasadas.
- Contract UI: 82/82; catálogos: 14 catálogos / 91 elementos, drift 0.
- Analyzers, build Release, lint frontend, `npm audit`, validadores, Oracle XE Bloque 6 y contenedores: PASS en el workflow.
- El validador UTF-8 corrió en CI: PASS. No se agregaron cambios de esquema ni secretos.

El E2E usa el service Oracle XE efímero que ya existía en Quality Gates. El host test-only se ejecuta en entorno E2E, con flag explícita y `DatabaseEnvironmentGuard` verificando perfil `LOCAL_XE`; no se alteró la autorización del API productivo. La suite Playwright mock se conserva.

## Evidencia y límites

Auditoría fuera de Git: `%USERPROFILE%\Documents\RIESGO_LAVADO\BLOCK7_EVALUATION_FLOW\20261014_0119_38494209621`. Incluye resultados JSON, ocho capturas desktop/mobile, `SHA256SUMS.json` y verificación de hashes.

Los controles, planes y actividades del E2E se escribieron a través de sus endpoints reales usando el API request de Playwright y luego se leyeron en el diálogo real; no se afirma que el test haya rellenado cada repeater del editor Angular. La integración se certificó en Oracle XE desechable, no en producción. El workbook de 59 riesgos se usó sólo como referencia; no se migraron riesgos.

## Seguridad y estado

`PRODUCTION_READ_ONLY_USED=NO` · `PRODUCTION_DML_EXECUTED=0` · `PRODUCTION_DDL_EXECUTED=0` · `PRODUCTION_APPLY_EXECUTED=NO` · `PRODUCTION_APPLY_AUTHORIZED=NO` · `SCHEMA_CHANGE_REQUIRED=NO` · `AUTH_PRODUCTION_BYPASS_ADDED=NO`.

`BLOCK1_STATUS=CLOSED` · `BLOCK2_STATUS=CLOSED` · `BLOCK3_STATUS=CLOSED` · `BLOCK4_STATUS=CLOSED` · `BLOCK5_STATUS=CLOSED` · `BLOCK6_STATUS=CLOSED` · `BLOCK7_STATUS=CLOSED` · `BLOCK8_STATUS=NOT_STARTED al cierre histórico del Bloque 7`.
