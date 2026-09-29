# Auditoría post-ANTIG — Bloque 2 de Matriz completa

## Propósito

Este documento reconcilia la certificación H1–H5/H5-B de ANTIG con la evidencia remota de GitHub Actions y deja una referencia técnica reproducible para continuidad. No sustituye ni reescribe la bitácora histórica: identifica qué evidencia pertenece a ejecuciones locales, qué evidencia está registrada en un run remoto exacto y qué se recertificó posteriormente.

## Resultado consolidado

`BLOCK_1_STATUS=CLOSED`
`BLOCK_2_STATUS=CLOSED`
`FULL_MATRIX_82=PENDING_BLOCKS_3_6`
`MAIN_TOUCHED=NO`
`ORACLE_CONNECTION_ATTEMPTED_BY_CHATGPT=NO`
`ORACLE_DML_EXECUTED_BY_CHATGPT=NO`
`ORACLE_DDL_EXECUTED_BY_CHATGPT=NO`

La referencia funcional vigente de esta auditoría es:

- SHA: `0e5fea6595ae6316e60936f630b5af6e9f34b4a1`
- Quality Gate: `36613506733` / run #1672
- Resultado: `completed/success`
- Backend: `702/702 PASS`
- Frontend: `82/82` archivos, `817/817 PASS`
- E2E Chromium: `40/40 PASS`, sin flaky
- npm audit: `0 vulnerabilities`
- Cobertura backend: líneas `37.06%`, ramas `40.28%`
- Cobertura frontend: sentencias `62.21%`, ramas `55.33%`, funciones `58.60%`, líneas `63.07%`

## Reconciliación del resultado ANTIG

### SHA `0c8259dba6b99ab854d6d279dae92186cdadb057`

El Quality Gate remoto `36609368915` terminó `completed/success`. La implementación que soporta H1–H5 existe en el repositorio y el cierre funcional no se revierte.

Las precisiones de evidencia son:

1. **Frontend unit remoto:** el run exacto registra `813/813` tests en 82 archivos. El valor `812/812` documentado en la intervención posterior de ANTIG no corresponde al conteo final del run remoto exacto.
2. **E2E remoto:** Playwright ejecutó 40 casos. `MCV.2 Constructor regresa al mismo Detalle y conserva Versiones` falló inicialmente porque el detalle no contenía un elemento enfocado dentro de 5 segundos; el retry pasó. El resumen remoto fue `1 flaky` + `39 passed`. El workflow fue exitoso, pero ese run no se usa como prueba de un 40/40 remoto limpio.
3. **H1 fail-fast:** los constructores productivos exigen sus dependencias y lanzan `ArgumentNullException` al recibir null. La forma concreta predominante es `x ?? throw new ArgumentNullException(...)`; esto es fail-fast equivalente, aunque la redacción histórica mencionó literalmente `ArgumentNullException.ThrowIfNull`.
4. **H2/H3 atomicidad:** existe `GovernedControlMutationExecutor`, seam transaccional de pruebas, rollback ante fallos inyectados, y la implementación Oracle utiliza una sola conexión/transacción, `FOR UPDATE`, commit/rollback y auditorías dentro de la unidad de trabajo. No se ejecutó una conexión o transacción contra Oracle institucional durante ANTIG ni durante esta auditoría.
5. **H4 concurrencia:** la implementación verifica `EVA_VERSION_ROW`, incrementa la versión de fila, convierte `DBConcurrencyException` a `409 Conflict`, bloquea la evaluación gobernada y rechaza re-parenting/estado no BORRADOR.
6. **H5 visual:** los E2E verifican Chromium real, 1280px/390px y ausencia de overflow horizontal. Los nombres de screenshots citados por ANTIG no están versionados en el repositorio ni aparecen como artifacts recuperables del workflow; por tanto, la inspección manual de esas capturas se conserva como evidencia declarada de ANTIG, mientras que los asserts programáticos sí son reproducibles desde CI.
7. **Cobertura:** los gates pasan, pero la cobertura no es 100%. Debe distinguirse “100% de gates/casos ejecutados” de “100% de cobertura”.

## Recertificación limpia posterior

### SHA `6c33a2e13ee4a755183bb035807db9c5a7d981d7`

El run `36612036100` terminó `completed/success` y eliminó la ambigüedad del flaky remoto:

- Backend `702/702`
- Frontend `815/815`
- E2E `40/40` limpio
- `1 flaky`: ausente
- npm audit: `0 vulnerabilities`

Este SHA también alineó las regresiones con la semántica UI vigente de acciones “Guardar nuevo …” y con los cinco estados válidos del plan.

## Hardening residual cerrado

### SHA `0e5fea6595ae6316e60936f630b5af6e9f34b4a1`

Durante la auditoría se detectó que los mapeos `.items` ya toleraban null en parte, pero Familias todavía accedía luego a `resultado.items.some(...)`, y Riesgos/Consolidado podían fallar ante un `items` truthy no-array. Se cerró ese borde sin cambiar contratos de negocio:

- Familias: `items` se normaliza con `Array.isArray`; selección y KPIs usan el arreglo normalizado; metadatos/totales tienen defaults seguros.
- Riesgos: `items` no-array se normaliza a `[]`.
- Consolidado: `items` no-array se normaliza a `[]`; totales/metadatos tienen defaults seguros; un tamaño de página fuera de `10|20|50` no reemplaza el valor válido actual.
- Se agregaron dos regresiones unitarias para payloads malformados.

El Quality Gate `36613506733` certifica este estado con `817/817` unit tests y `40/40` E2E limpios.

## Decisión de continuidad

No se reabre Bloque 2. H1–H5/H5-B se consideran cerrados con evidencia corregida y recertificada. La referencia técnica para nuevas ramas o análisis debe ser el SHA funcional `0e5fea6595ae6316e60936f630b5af6e9f34b4a1` y su run `36613506733`, salvo que exista un HEAD posterior con gates equivalentes o superiores.

Bloque 3 (`34–39`) no se inició en esta auditoría.
