# RIESGO_LAVADO — Intento CI P0 Oracle XE — 2026-11-21

## Dictamen

`BLOCKED` — la integración está publicada, pero no está certificada. Este expediente registra el único intento CI ejecutado después de la corrección de aislamiento de fixtures. Por instrucción del propietario no se realizó otra corrección ni otra corrida después del fallo.

## Trazabilidad

| Campo | Valor |
|---|---|
| Repositorio | `jmejia31/RIESGO_LAVADO` |
| Rama | `desarrollo` |
| SHA funcional evaluado | `87e22d04f2f8f9082246a96f00eb1f76cba15878` |
| Commit base del intento | `e4f839a515ecd8b6ef922d2ce3f6b19ea9f6b0b6` |
| Run CI | [44975074761](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44975074761) |
| Job principal | [137063068350](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44975074761/job/137063068350) |
| Fecha local | `2026-11-21 20:13–20:42 -06:00` |
| EVIDENCE_COMMIT_SHA | `b9fde1b48bf3c23328ca5f987c437dc6353713ba` |
| GitHub remoto verificado | `YES` |
| Aceptación del propietario | `PENDING` |

## Alcance seguro

La prueba utilizó únicamente el Oracle XE descartable administrado por CI. No se modificaron Oracle institucional, producción, `main`, migraciones protegidas ni datos institucionales. El intento no publicó secretos, dumps, PII ni logs operativos.

## Resultado observado

- Job de detección: `SUCCESS`.
- Job principal: `FAILURE`.
- E2E completo del repositorio: `82 PASS`, `7 SKIP`, `12 FAIL`.
- La API Oracle permaneció disponible durante el E2E completo; por tanto, el fallo anterior de ejecutar E2E después de apagar el host quedó resuelto.
- La limpieza del fixture descartable pasó: `CLEANUP_EXIT=0`, `RISK_REMAINING=0`, `DEPENDENCIES_REMAINING=0`, `METADATA_REMAINING=0`, `CLEANUP_ERRORS=0`.
- No se declara certificación `59/59`, `2006/2006` ni aceptación funcional porque el Quality Gate terminó fallido.

## Fallos reproducidos en CI

Los doce fallos pertenecen al E2E genérico y se agrupan así:

1. Cuatro escenarios de `institutional-title-favicon.spec.ts` relacionados con título/favicon, eliminación con fallback, conversión de imagen local/`.ico` y coexistencia directa HTTPS.
2. Ocho escenarios de `matrices-uat-integral.spec.ts`, incluyendo esperas agotadas sobre `#plan-descripcion` y ausencia del feedback `[data-uat="feedback-edicion-exito"]`.

El log también contiene el `ORA-20991` intencional del escenario TX12 de fallo de auditoría; ese error forma parte de la prueba negativa y no se trató como éxito del Quality Gate.

## Cambios de esta etapa

El único cambio funcional de la etapa fue de orquestación de pruebas en `tools/run_block7_oracle_e2e.sh`: se cerró el primer host descartable, se verificó su limpieza y se levantó un segundo host con un ámbito de fixtures nuevo antes del E2E genérico. No se realizó ningún cambio adicional después del fallo de CI.

## Límites y continuación

El estado máximo demostrable es `BLOCKED`. No se solicita al propietario una prueba manual como sustituto de CI: primero debe revisarse el conjunto de doce fallos en una intervención posterior explícitamente autorizada. La aceptación visual y funcional del propietario permanece pendiente y no se infiere de los `82` casos aprobados.

## Veredicto

`P0_ORACLE_XE_STATUS=BLOCKED`

`P0_ORACLE_XE_CERTIFIED=NO`

`P0_OWNER_ACCEPTANCE=PENDING`
