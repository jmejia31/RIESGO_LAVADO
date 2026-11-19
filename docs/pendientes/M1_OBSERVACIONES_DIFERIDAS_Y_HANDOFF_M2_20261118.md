# RIESGO_LAVADO — Observaciones diferidas de Microfase 1 y transferencia a Microfase 2

**Fecha de registro:** 18 de noviembre de 2026  
**Autor:** CHAT · **Repositorio:** `jmejia31/RIESGO_LAVADO` · **Rama:** `desarrollo`  
**Estado:** `DEFERRED_NON_BLOCKING_BACKLOG`; **NO** constituye orden de ejecución automática, reapertura de Microfase 1 ni autorización para modificar código o bases de datos.  
**Fuente técnica de M1:** `3b22ebedf727a7ba4ffbd82540bfe98b05bb060b` · **cierre documental M1:** `efb6a892e6f2e9891c4d45667bfa3b8dc7363b5f`.  
**Quality Gate código:** [44424145245](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44424145245) `completed/success`.  
**Quality Gate documental:** [44426253435](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44426253435) `completed/success`, sobre `efb6a892`.  
**Aprobación funcional:** otorgada expresamente por el propietario. **Issue #41:** cerrado como `completed`. **Issue #42:** abierto por mantenimiento de `picomatch`.  
**Respaldo inmutable de referencia:** `respaldo/desarrollo-20261118-5e5dc8c0` → `5e5dc8c09c59ee702af76669409828d989c803af`; no mover esta referencia.

> Este documento conserva las **reservas de interpretación y controles de mantenimiento** que pueden revisarse después. No revoca el dictamen funcional ni los Quality Gates verdes. Distingue datos actualmente medidos, referencias históricas observadas y explicaciones que necesitan evidencia adicional.

## 1. Hechos y cifras que ya constan en GitHub

Fuentes: [expediente Oracle institucional](../certificaciones/M1_CONCILIACION_HISTORICA_INSTITUCIONAL_20261118.md), [guion Q01–Q12/R01–R12](../qa/M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql) y [expediente del fork picomatch](../certificaciones/CI_PICOMATCH_FORK_VENDORIZADO_20261118.md).

- Antigravity **reportó ejecución autorizada de solo lectura** del 18/11/2026 con cifras agregadas de `Q01–Q10, R01–R07, R09–R12`; no constituye auditoría externa independiente de la sesión.
- `Q04`: `TIPO_CALIFICACION_ID=1` → **5,752 filas fuente**; **189** grupos DNI/lista; **270** DNI/lista/nombre normalizado; **5,249** filas sin `FECHA_CALIFICO`; **7** filas `N/A(3)`; **12** filas sin identificación.
- `R01`: naturales identificadas **5,581 coincidencias fuente**, **172** grupos DNI; naturales sin identificación **12** filas. `R02`: empleados **159 coincidencias fuente** y **11** grupos DNI. **5,581 + 12 + 159 = 5,752**.
- `Q08`: positivos activos tipo natural **3**, tipo empleado **2**. `R07`: naturalezas de origen activas `Natural: DNP=1, MANUAL=1, NULL=1`; `Empleado: DNP=1, NULL=1`. **No inferir que todos los registros operativos adicionales tienen origen explícito MANUAL.**
- `Q06`: **187** documentos reportados, **20** sin vista de detalle; `R09`: **171** DNI naturales con socio/representante, **1** sin; `R10`: **165** DNI con regla legado y fecha; `R11`: **9** grupos solo regla actual. **Son universos/predicados diferentes**, no sumandos intercambiables.
- Referencias antiguas del usuario: aproximadamente **242 agrupaciones**, **190 visibles**, diferencia aritmética **52**; **7 empleados** de consulta temporal anterior. La interfaz actual mostró **174 grupos naturales** y **13 empleados**. No hay equivalencia retrospectiva uno-a-uno acreditada con el mismo corte y el mismo agrupador.
- Los reportes anteriores mencionaron **5,583 naturales** y **161 empleados**; **5,581/159** son conteos **fuente** en `R01/R02`, y el `+2` de cada vista fue reportado como registros operativos. **No etiquetar 5,583 y 161 como coincidencias fuente exclusivamente DNP.**
- `Q11`, `Q12`, `R08` **NO ejecutadas** deliberadamente: vista histórica de planillas por DB Link potencialmente masiva y costosa. Eso es protección de DBA, no un PASS de dichas consultas.
- La mitigación `@riesgo-lavado/picomatch-hardened@4.0.7-rl.2`, con SHA/SRI y pruebas ReDoS, está instalada; `npm audit` completo y CI aprobaron. **No existe release oficial corregido acreditado en el corte de este registro.**

## 2. Registro específico de pendientes diferidos

| ID | Prioridad | Estado | Control diferido | Condición concreta para su eventual cierre |
|---|---|---|---|---|
| `M1-HIST-01` | P2 | `DIFERIDO` | Reproducir las **242/190/52** agrupaciones del legado con la **misma fecha, clasificación, filtros y clave**. `Q04=189/270` mide un corte y agrupador diferentes, no reproduce 242. | Evidencia de instantánea histórica, SQL legado exacto y tabla agregada de transición disjunta; si no existe corte recuperable, documentar `HISTORICAL_SNAPSHOT_UNAVAILABLE` sin inventar una equivalencia. |
| `M1-HIST-02` | P2 | `DIFERIDO` | Explicar **7 empleados históricos vs 13 grupos actuales**. Actualmente: 11 grupos fuente/159 coincidencias más 2 positivos operativos. | Contraste equivalente de período/consulta anterior y corte actual, sin multiplicar filas de nómina. Requiere estudio DBA antes de ejecutar vistas pesadas. |
| `M1-HIST-03` | P2 | `DIFERIDO` | Conciliar las etiquetas de KPI: `5,583` naturales / `161` empleados vs `5,581` / `159` **coincidencias fuente**. | Separación formal en reporte y DTO/contrato semántico entre `coincidencias_fuente`, `positivos_operativos` y `grupos`; conservar las cifras ya aceptadas visualmente sin cambiarlas por conveniencia. |
| `M1-HIST-04` | P2 | `DIFERIDO` | Origen `NULL` en positivos históricos: `R07` desglosa tipo 2 (1 DNP, 1 MANUAL, 1 NULL), tipo 3 (1 DNP, 1 NULL); el expediente etiqueta 2 como manuales en cada vista. | Confirmar semántica funcional del origen legado `NULL` sin reescribirlo ni reclasificarlo artificialmente. |
| `M1-HIST-05` | P3 | `DIFERIDO` | Conciliar referencias documentales de **23 grupos recuperados** con `Q06=20`, `R09=1` y `R11=9`. | Matriz que alinee fecha de corte, exclusiones `INNER JOIN`, `FECHA_CALIFICO`, clasificación y normalización; explicar cada cardinalidad sin restas entre denominadores incompatibles. |
| `M1-HIST-06` | P3 | `DIFERIDO` | Validar semántica institucional de `TIPO_CALIFICACION_ID=1` y `FECHA_CALIFICO IS NULL` (`Q04=5249`). | Interpretación confirmada por responsable institucional/DBA con reglas de ETL y una muestra agregada no identificable. |
| `M1-HIST-07` | DBA | `NO_EJECUTADO_POR_PROTECCION` | `Q11/Q12/R08` de `V_EMPLEADOS_IHSS_PLANILLAS` no ejecutadas. | Autorizar individualmente solo si la conciliación futura lo exige, después de revisión de plan/costo por DBA y límites de lectura. No cargar la vista remota sin autorización. |
| `M1-QA-01` | P3 | `DIFERIDO` | Control de aislamiento `SRC:<id>` de registros `N/A(3)`, empresa compartida, razones/seguimientos, evidencia y PDF/Excel en datos institucionales. | Evidencias sanitizadas por expediente o reproducción sintética Oracle XE exact-SHA, sin atribuciones de identidad falsas ni exposición de PII; no confundir tests sintéticos con auditoría de históricos. |
| `SEC-PICO-01` | P2 | `DEPENDENCIA_UPSTREAM` | Sustituir el fork interno únicamente cuando exista versión oficial corregida de `CVE-2026-112366` o sustituto homologado. | Verificación oficial GHSA/PR/release/tarball; actualización controlada de override y lock, prueba adversarial, npm audit completo, builds y CI del nuevo SHA. Seguimiento en [#42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42). |
| `SEC-PICO-02` | P2 | `MANTENIMIENTO` | Vigilar la brecha del escáner: el nombre npm del fork no hereda advisories contra `picomatch`. | Revisión periódica de código/procedencia, patrones ReDoS nuevos, dependencia vendorizada y CVE; sin suprimir el `npm audit` completo ni usar `--force` como atajo. |

## 3. Reglas de seguimiento y límites

1. **M1 está aceptada funcionalmente y certificada técnicamente** en las ejecuciones identificadas. El [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) permanece **cerrado como completado**; este registro **no lo reabre** ni invalida el signoff del propietario.
2. La seguridad operativa de `picomatch` está `PASS`; su **mantenimiento upstream** permanece abierto por dependencia externa en [Issue #42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42).
3. Este archivo es el **punto de reanudación** de los controles históricos. Las verificaciones de M2 no deben depender de cifras retrospectivas todavía no reconciliadas bajo una única definición de grupo.
4. Toda tarea diferida requiere nuevo análisis, autorización proporcional y **su propio** SHA/Quality Gate si altera código. Nunca reutilizar un Quality Gate de otro SHA.
5. **Prohibido:** modificar `main`, producción, Oracle institucional o `DNP_IHSS`; mover/borrar la rama de respaldo; inventar resultados, exponer PII o ejecutar consultas remotas masivas sin DBA. Permitir únicamente SELECT autorizados bajo perfil de lectura.
6. Este inventario no activa `docs/automatizacion/TAREAS_CODEX.json`, watcher ni automatizaciones; no programa trabajos posteriores por sí solo.

## 4. Transferencia y habilitación de Microfase 2

- **M1:** `OWNER_FUNCTIONAL_ACCEPTANCE=APPROVED`; `CODE_QG=PASS_44424145245`; `DOCUMENTARY_QG=PASS_44426253435`; `ISSUE_41=CLOSED`.
- **M2:** `OPEN_FOR_SCOPING=YES`; revisar [apertura de M2](../qa/M2_APERTURA_ALCANCE_20261118.md) antes de iniciar mutaciones. **El alcance funcional exacto de M2 todavía no está versionado inequívocamente** y no debe confundirse con las antiguas intervenciones denominadas `FASE2A_MONITOREO_LISTAS`.
- **Punto siguiente:** concretar objetivos/criterios M2 contra módulos y roadmap vigentes, hacer inventario de dependencias y riesgos, preservar M1; después implementar únicamente el alcance aprobado.

```ini
PROJECT=RIESGO_LAVADO
AUTHOR=CHAT
BRANCH=desarrollo
M1_OWNER_ACCEPTANCE=APPROVED
M1_FUNCTIONAL_CODE_SHA=3b22ebedf727a7ba4ffbd82540bfe98b05bb060b
M1_DOCUMENTARY_SHA=efb6a892e6f2e9891c4d45667bfa3b8dc7363b5f
M1_FUNCTIONAL_QG=44424145245_SUCCESS
M1_DOCUMENTARY_QG=44426253435_SUCCESS
M1_HISTORICAL_EXACT_CUT_RECONCILIATION=DEFERRED_OBSERVATION
M1_OPERATIONAL_SECURITY=PASS
PICOMATCH_UPSTREAM_REPLACEMENT=PENDING_EXTERNAL
M1_ISSUE_41=CLOSED
M2_SCOPING=STARTED
M2_IMPLEMENTATION=NOT_STARTED
MAIN_TOUCHED=NO
PRODUCTION_TOUCHED=NO
ORACLE_INSTITUTIONAL_WRITES=0
```
