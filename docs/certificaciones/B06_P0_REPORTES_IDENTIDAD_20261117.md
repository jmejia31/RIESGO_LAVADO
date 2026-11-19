# B06 P0 — Recuperación de registros y reportería institucional

`EVENT_ID=RL-B06-P0-REPORTES-IDENTIDAD-20261117`  
`AUTHOR=COD / CODEX` · `CLIENT=CLI` · `BRANCH=desarrollo`  
`INITIAL_SHA=a8e6ac8c7979294fb775abca331505939abed47d`  
`SOURCE_CODE_SHA=f423b929c6f5303e6302d8361c06434f05d78c62`  
`EVIDENCE_COMMIT_SHA=c64a98cf`  
`OWNER_VISUAL_ACCEPTANCE=PENDING`

## Alcance

Intervención limitada a B06 y a la dependencia compartida de Monitoreo de Listas necesaria para recuperar lectura, detalle y generación de reportes. No se modificaron `main`, B07, Bloque 10, la migración 54, Oracle institucional ni `DNP_IHSS`.

## Diagnóstico reproducible en código

1. La consulta de empleados restringía la fuente a `PERIODO = TRUNC(ADD_MONTHS(SYSDATE, -1), 'MM')`; además, el detalle exigía `FECHA_CALIFICO IS NOT NULL`. Eso ocultaba registros históricos, de otros períodos y coincidencias aún no calificadas.
2. La lectura de detalle de naturales y empleados exigía resolver identidad antes de consultar. Una identidad ambigua o sin identificación podía abrir la fila en el listado, pero quedaba bloqueada antes de recuperar sus datos fuente.
3. El frontend convertía fallos de detalle/positivo/seguimiento en arreglos vacíos en algunos flujos de reporte. El usuario recibía un documento aparentemente vacío en lugar del error real.
4. Los marcadores como `N/A(3)` son valores de origen/marcadores de identificación no atribuible; no se crean ni se eliminan en esta intervención. Se mantienen visibles, se consulta su fuente cuando existe y solo se restringen acciones sensibles.
5. La resolución de branding del exportador de Matrices tenía una ruta síncrona y la geometría de logo no era una política común de encaje proporcional. El controlador de producción ya quedó migrado a un snapshot asíncrono por documento.
6. El login consumía `logoUrl` sin resolver rutas relativas `/uploads/...`; la navegación tenía un resolver distinto. Se unificó ese consumidor.

## Implementación publicada

- Se quitaron filtros implícitos de período y calificación que ocultaban filas; los filtros de fecha explícitos siguen aplicándose cuando el usuario los solicita.
- El detalle informativo ahora conserva los datos fuente para registros válidos, ambiguos, históricos y marcadores. La identidad operable continúa siendo un requisito únicamente para positivos, seguimientos y demás mutaciones.
- Los errores de API de reportes ya no se transforman en documentos vacíos; se informa el error y no se crea un archivo falso.
- PDF y Excel de Matrices resuelven un único `InstitutionalBrandingSnapshot` asíncrono, con logo local configurado, paleta común y encaje `contain` sin deformación ni solapamiento.
- PDF/Excel frontend usan la misma política de proporción, límites y centrado. El login usa `resolverUrlImagen` para el logo institucional.
- Se actualizaron regresiones backend y Angular para la lectura de marcadores, detalle de solo lectura, snapshot de branding, geometría y contratos async.

## Verificación ejecutada

| Control | Resultado |
|---|---|
| Backend focal B06/Listas | PASS: 88 tests; segundo conjunto async/branding: 28 tests |
| Build `RIESGO_LAVADO.sln` Release | PASS, 0 errores; warnings de análisis preexistentes |
| Suite Angular | PASS: 96 archivos, 1014 tests |
| Build Angular | PASS; warnings de presupuesto SCSS/CommonJS preexistentes |
| Estructura de repositorio | PASS |
| Scripts de base de datos | PASS; no ejecutados contra Oracle institucional |
| Enlaces documentales | PASS |
| Agent Skills | PASS: 14 skills |
| Servicios locales | DETENIDOS por solicitud del propietario |

## No verificado y límites

- `ORACLE_INSTITUTIONAL_RUNTIME=NOT_EXECUTED`; no se hicieron escrituras en Oracle institucional ni en `DNP_IHSS`.
- `LIVE_AUTHENTICATED_E2E=NOT_VERIFIED`; no se ejecutó la prueba visual autenticada del propietario con los datos reales, incluidos todos los registros naturales/empleados y `N/A(3)`.
- `PLAYWRIGHT_REAL_AUTHENTICATED=NOT_VERIFIED`.
- La migración B06 no fue ejecutada. Si el esquema remoto no contiene las columnas B06, el backend debe devolver `B06_BRANDING_SCHEMA=NOT_READY` y bloquear el guardado, nunca reportar éxito parcial.
- La aceptación visual del propietario sigue pendiente. Por lo tanto, el estado no es cierre final de B06.

## Integridad y preservación

`ORACLE_INSTITUTIONAL_WRITES=0`  
`DNP_IHSS_WRITES=0`  
`MIGRATION_54_TOUCHED=NO`  
`MAIN_TOUCHED=NO`  
`B07_AUTHORIZED=NO`  
`BLOCK10_AUTHORIZED=NO`  
`UNRELATED_WORKTREE_CHANGES_STAGED=NO`

## Dictamen

`B06_TECHNICAL_STATUS=CERTIFIED_TECHNICALLY_READY_FOR_OWNER_FINAL_REVIEW`<br/>
`B06_OWNER_VISUAL_ACCEPTANCE=APPROVED_M1_AND_M2_PENDING_FINAL_M3_SIGNOFF`<br/>
`B06_FINAL_STATUS=READY_FOR_OWNER_FINAL_REVIEW`

La evidencia técnica integral, resolución de logotipos, colores institucionales y validación operativa completa se certifican en `M2_P0_RECUPERACION_GLOBAL_LOGOTIPOS_20261118.md` y `M3_CERTIFICACION_INTEGRAL_B06_20261119.md`.


---

## ADENDA DE CIERRE VIGENTE — 19/11/2026 (CHAT)

La referencia histórica `OWNER_VISUAL_ACCEPTANCE=PENDING` fue válida en la fecha inicial del 17/11, pero quedó **sustituida por aprobación expresa** del propietario el 19/11 para M1/M2/M3, el favicon y el estándar de reportes tal como se entregaron. Ver [ACTA #44](https://github.com/jmejia31/RIESGO_LAVADO/issues/44) y [acta documental B06](B06_ACTA_CIERRE_FUNCIONAL_M1_M2_M3_20261119.md).

`B06_OWNER_ACCEPTANCE=APPROVED`; `B06_FUNCTIONAL_STATUS=CLOSED_WITH_EXPLICIT_ACCEPTED_LIMITATIONS`; `SOURCE_CODE_SHA=6e658f20a1866aa6725b2e9ff0afd93443e739ee`; `QUALITY_GATE=44603836071_SUCCESS`. QA institucional autenticada integral y paridad documental Oracle PDF/Excel revalidada en M3 **no demostradas**; nueve pruebas Oracle XE **no ejecutadas localmente**, pero pasos de Oracle XE descartable **PASS en CI**. No declarar `B06_FULL_INTEGRATION_QA=PASS` por esta aceptación. Sin despliegue a `main` ni producción; seguimiento `picomatch` en Issue #42.
