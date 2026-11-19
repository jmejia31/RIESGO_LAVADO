# B06 — Acta de cierre funcional M1, M2 y M3 (19/11/2026)

**Autor:** CHAT · **Repositorio:** `jmejia31/RIESGO_LAVADO` · **Rama:** `desarrollo`  
**SHA de código evaluado:** `6e658f20a1866aa6725b2e9ff0afd93443e739ee`  
**Quality Gate del SHA de código:** [44603836071](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44603836071) — `completed/success`; 2/2 jobs aprobados.  
**Acta pública de conformidad del propietario:** [Issue #44, CLOSED/completed](https://github.com/jmejia31/RIESGO_LAVADO/issues/44). La declaración expresa del propietario en esta conversación aprueba las tres microfases y la apariencia final del favicon y del PDF de Matrices: «NO ES LO QUE DEBERÍA HABER SUCEDIDO PERO ME GUSTA, LO APRUEBO ASÍ ... CERTIFICAMOS LAS 3 MICROFASES».

## Dictamen por microfase

- **M1 Monitoreo:** `OWNER_ACCEPTANCE=APPROVED`; [conciliación histórica documentada](M1_CONCILIACION_HISTORICA_INSTITUCIONAL_20261118.md); [Issue #41 cerrado](https://github.com/jmejia31/RIESGO_LAVADO/issues/41). Las cifras históricas con distintas unidades de agrupación no se equiparan sin evidencia.
- **M2 Reportería:** `OWNER_ACCEPTANCE=APPROVED`; [matriz de cobertura institucional](M2_MATRIZ_COBERTURA_REPORTERIA_GLOBAL_20261118.md) de 18 puntos, [Acta #43 cerrada](https://github.com/jmejia31/RIESGO_LAVADO/issues/43).
- **M3 Seguridad/QA/favicon:** `OWNER_ACCEPTANCE=APPROVED`; [expediente M3](M3_CERTIFICACION_INTEGRAL_B06_20261119.md); favicon visible en vista previa de tamaños 16/32/48 y logotipo en PDF Matrices según capturas del propietario, aceptados **tal como quedaron**.
- **B06:** `CERRADA_FUNCIONALMENTE_CON_SALVEDADES_ACEPTADAS`; no equivale a `FULL_INSTITUTIONAL_E2E_QA=PASS` ni autorización de despliegue a producción.

## Evidencia y correcciones de precisión

| Control | Resultado defendible |
| --- | --- |
| Gate SHA vigente | **PASS**; [run 44603836071](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44603836071), mismo `6e658f20a1866aa6725b2e9ff0afd93443e739ee`, jobs de clasificación y de validación integral en `success`, controles de Oracle XE descartable y auditoría npm exitosos. |
| Nueve pruebas Oracle XE M3 **local** | **NO EJECUTADAS LOCALMENTE** (`GATED_LOCAL_XE=9`). Hay pasos de Oracle XE descartable aprobados en CI, pero no hay evidencia de que esas nueve se ejecutaran individualmente en local. |
| QA autenticada institucional M3 | **NO DEMOSTRADA INTEGRALMENTE**. Pruebas E2E previas incluyen mocks; las capturas prueban aceptación visual, no recorrido exhaustivo con Oracle institucional. |
| Paridad real PDF/Excel en M3 | **NO REVALIDADA COMPLETAMENTE** con todo el universo Oracle. Se hereda matriz y aceptación visual M2, sin inventar nueva prueba. |
| Conteos expediente M3 | 2,367 controles contabilizados = **2,355 PASS + 12 SKIP/GATED + 0 FAIL**; el «100%» es de los controles con resultado PASS/FAIL, no del total programado. |
| Desglose frontend M3 | Subcategorías suman **999** de las 1,047 pruebas reportadas; **48 no desglosadas**. No adjudicar artificialmente esas 48 a ninguna categoría. |
| Ruta del fork | Correcta: `frontend/rl-app/vendor/picomatch-hardened-4.0.7-rl.2.tgz`; la ruta histórica `vendor/riesgo-lavado-picomatch-hardened-4.0.7-rl.2.tgz` era incorrecta. |
| Cambios funcionales M3 | La frase `FUNCTIONAL_CODE_CHANGES=0` describía la intervención documental inicial, no las correcciones posteriores del favicon integradas en el SHA actual. |
| picomatch upstream | Mitigación auditada y gate verde; seguimiento de versión oficial permanece en [Issue #42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42). |

## Decisión expresa de aceptación con límites

El propietario **acepta estas salvedades como no bloqueantes del cierre del alcance funcional entregado**. No se marcará `PASS` a verificaciones no ejecutadas; cualquier certificación de despliegue o seguridad que exija pruebas institucionales reales deberá ejecutarlas antes de emitir su propio dictamen.

```ini
M1_OWNER_ACCEPTANCE=APPROVED
M2_OWNER_ACCEPTANCE=APPROVED
M3_OWNER_ACCEPTANCE=APPROVED
M3_FAVICON_VISUAL_ACCEPTANCE=APPROVED_AS_DELIVERED
B06_OWNER_ACCEPTANCE=APPROVED
B06_FUNCTIONAL_STATUS=CLOSED_WITH_EXPLICIT_ACCEPTED_LIMITATIONS
SOURCE_CODE_SHA=6e658f20a1866aa6725b2e9ff0afd93443e739ee
CODE_QUALITY_GATE=44603836071_SUCCESS
M3_ORACLE_9_TESTS_LOCAL=NOT_EXECUTED
ORACLE_XE_CI_STEPS=PASS
M3_REAL_AUTHENTICATED_INSTITUTIONAL_E2E=NOT_DEMONSTRATED
M3_NEW_FULL_PDF_EXCEL_PARITY=NOT_FULLY_DEMONSTRATED
B06_INTEGRAL_INSTITUTIONAL_E2E_CERTIFIED=NO
PICOMATCH_UPSTREAM_REPLACEMENT=PENDING_ISSUE_42
MAIN_MODIFIED_BY_SIGNOFF=NO
PRODUCTION_MODIFIED_BY_SIGNOFF=NO
ORACLE_INSTITUTIONAL_WRITES_BY_SIGNOFF=0
```

**Procedencia:** GitHub API (HEAD, jobs, runs, Issues), expedientes históricos y aceptación/capturas del propietario en esta conversación. **No se ejecutaron desde CHAT** pruebas locales .NET/Angular, Oracle, E2E institucional ni comparación de XLSX reales. Cualquier commit posterior de solo documentación requiere comprobar su propio Gate exacto; no atribuirle el run anterior.
