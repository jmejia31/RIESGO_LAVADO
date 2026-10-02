# Bloque 2: Auditoría Forense Excel ↔ Producción (59 Riesgos × 82 Campos)

> **Estado:** COMPLETADO — PASE CORRECTIVO FINAL (Dry Run Absoluto — Cero Escrituras)<br>
> **Fecha:** 2026-10-02<br>
> **Autoridad Contractual:** Bloque 1 (`61e4cb2f0a465f2d848025677dc3ab864515ce0e`) y Pase Semántico (`66dad25c96b2bffc0b357c006ed42896c971919e`)<br>
> **Workbook Oficial:** `Matrices de Riesgos.xlsx` (Hoja: `Matriz Consolidada`, Rango: `A1:CD60` y Hoja: `Otras Tablas`)<br>
> **Hash SHA-256 Workbook:** `5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385`<br>
> **Entorno Productivo:** Oracle Database `HPPROD1` (`RIESGO_LAVADO`) — `DATABASE_TARGET_CLASS=PRODUCTION`

---

## 1. Resumen Ejecutivo y Metodología

El **Bloque 2 de 12** ejecutó una auditoría forense integral, determinista y estrictamente de sólo lectura sobre la totalidad del universo contractual: **59 riesgos institucionales × 82 campos canónicos = 4,838 posiciones contractuales**.

Tras la ejecución inicial, se aplicó el **Pase Correctivo Final de Semántica de Comparación y Clasificación** con base estricta en las reglas del Instructivo institucional y las tablas oficiales de `Otras Tablas` del mismo workbook:
1. **Semántica Canónica de Ausencia de Controles:** Cuando `CONTROL_COUNT_OF_TYPE = 0`, el estado institucional es `NO_CONTROLS_OF_TYPE`, la descripción es `"No hay"`, la escala es `"Inexistente"`, el nivel es `0` y el porcentaje es `0%`. Si el Excel reporta `"No hay"` y la DB tiene 0 controles, la auditoría proyecta la semántica canónica y clasifica como `MATCH` (`NO_CONTROLS_CANONICAL_MATCH`), eliminando falsos faltantes.
2. **Semántica de Controles 1:N y Escala Combinada:** La descripción de controles es 1:N (`CONTROL_DESCRIPTION_CARDINALITY=ONE_TO_MANY_RENDERED`, campos 20, 24, 28) mientras que la escala de efectividad es un único valor combinado por tipo (`CONTROL_EFFECTIVENESS_SCALE_CARDINALITY=ONE_COMBINED_SCALE_PER_CONTROL_TYPE`, campos 21, 25, 29). Se normalizaron las escalas contra la tabla institucional (`0 = Inexistente / 0%`, `1 = Es inefectivo / 0%`, `2 = Razonable / 30%`, `3 = Parcialmente Efectivo / 50%`, `4 = Moderado / 85%`, `5 = Alta Efectividad / 90%`). Cuando el Excel contiene `"Alta Efectividad"` y la base contiene `90`, la comparación semántica arroja `MATCH` (`EFFECTIVENESS_SCALE_SEMANTIC_MATCH`), eliminando 39 falsas discrepancias.
3. **Distinción entre Ausencia de Dato vs Defecto de Mapeo:** Se separó nítidamente la ausencia de datos operativos en tablas hijas poblables (`MISSING_IN_DB` con acción `IMPORT_BASELINE`) del defecto de persistencia técnica (campos 03, 05, 06, 07, 15, 16, 32 clasificados con `technicalMappingStatus=BROKEN`, `reasonCode=MISSING_PERSISTENCE_MAPPING`, `recommendedNextAction=FIX_MAPPING`).
4. **Dry Run Absoluto — Cero Modificaciones:** Transacción única `SET TRANSACTION READ ONLY` con `ROLLBACK`. Cero DML, cero DDL y cero escrituras en producción.

---

## 2. Métricas Globales y Verificación de Totales

| Métrica | Valor Esperado | Valor Obtenido | Estado |
|---|---|---|---|
| Riesgos Auditados | 59 | 59 | PASS |
| Campos Auditados | 82 | 82 | PASS |
| Universo Total de Posiciones | 4,838 | 4,838 | PASS |
| Claves Únicas de Auditoría (`<CODIGO>\|<CAMPO>`) | 4,838 | 4,838 | PASS |
| Claves Duplicadas | 0 | 0 | PASS |
| Clasificaciones Primarias Desconocidas (`UNKNOWN`) | 0 | 0 | PASS |
| Diferencias Sin Explicación (`UNEXPLAINED`) | 0 | 0 | PASS |
| DML / DDL / Procedimientos Ejecutados en Producción | 0 | 0 | PASS |

### Distribución de la Taxonomía Primaria Definitiva (Total = 4,838)

```
┌─────────────────────────────────────────────────────────────┬───────────┬───────────┐
│ Clasificación Primaria                                      │ Cantidad  │ % Total   │
├─────────────────────────────────────────────────────────────┼───────────┼───────────┤
│ MATCH                                                       │       648 │    13.39% │
│ MISSING_IN_DB                                               │       457 │     9.45% │
│ DIFFERENT                                                   │        95 │     1.96% │
│ LEGITIMATELY_BLANK_IN_EXCEL                                 │     1,193 │    24.66% │
│ DB_HAS_NEWER_OPERATIONAL_DATA                               │         0 │     0.00% │
│ CALCULATED_FIELD                                            │     2,124 │    43.90% │
│ NOT_APPLICABLE                                              │       321 │     6.64% │
├─────────────────────────────────────────────────────────────┼───────────┼───────────┤
│ TOTAL                                                       │     4,838 │   100.00% │
└─────────────────────────────────────────────────────────────┴───────────┴───────────┘
```

---

## 3. Tabla Comparativa: Antes vs Después del Pase Correctivo

| Clasificación Primaria | Auditoría Inicial | Pase Correctivo Final | Variación Neta | Explicación Técnica de la Variación |
|---|:---:|:---:|:---:|---|
| **MATCH** | 597 | **648** | **+51** | +39 por normalización de escalas semánticas (F21, F25, F29) y +12 por proyección canónica de ausencia "No hay" (F20, F24, F28). |
| **MISSING_IN_DB** | 469 | **457** | **-12** | -12 posiciones con "No hay" en Excel que corresponden a ausencia canónica válida (coincidencia semántica con 0 controles en DB). |
| **DIFFERENT** | 134 | **95** | **-39** | -39 falsos diferentes eliminados al contrastar escalas textuales vs numéricas normalizadas contra `Otras Tablas` (e.g. "Alta Efectividad" vs 90, "Inexistente" vs 0). |
| **LEGITIMATELY_BLANK_IN_EXCEL** | 1,193 | **1,193** | **0** | Sin variación; celdas legítimamente en blanco en el baseline. |
| **DB_HAS_NEWER_OPERATIONAL_DATA**| 0 | **0** | **0** | Sin variación; 0 datos operativos en DB que superen baseline. |
| **CALCULATED_FIELD** | 2,124 | **2,124** | **0** | Sin variación; 34 fórmulas institucionales + 2 contadores agregados por riesgo. |
| **NOT_APPLICABLE** | 321 | **321** | **0** | Sin variación; 177 campos GTIC + 144 mitigaciones donde no aplica mitigar. |
| **TOTAL** | **4,838** | **4,838** | **0** | Universo 100% preservado sin posiciones duplicadas ni omitidas. |

---

## 4. Resumen por Bloque Funcional Definitivo

| Bloque | Rango | Nombre del Bloque | Posiciones | MATCH | MISSING | DIFF | BLANK | OPER | CALC | N/A |
|---|---|---|---|---|---|---|---|---|---|---|
| **B1** | 01-19 | 1. Identificación y Riesgo Inherente | 1,121 | 385 | 303 | 87 | 51 | 0 | 118 | 177 |
| **B2** | 20-33 | 2. Controles | 826 | 189 | 138 | 0 | 86 | 0 | 413 | 0 |
| **B3** | 34-39 | 3. Riesgo Residual y Respuesta | 354 | 55 | 0 | 4 | 0 | 0 | 295 | 0 |
| **B4** | 40-49 | 4. Plan de Mitigación / Acciones Correctivas | 590 | 0 | 16 | 0 | 312 | 0 | 118 | 144 |
| **B5** | 50-69 | 5. Cálculos Auxiliares y Verificaciones | 1,180 | 0 | 0 | 0 | 0 | 0 | 1,180 | 0 |
| **B6** | 70-82 | 6. Monitoreo, Efectividad y Observaciones | 767 | 19 | 0 | 4 | 744 | 0 | 0 | 0 |
| **TOTAL** | **01-82** | **Matriz Consolidada Completa** | **4,838** | **648** | **457** | **95** | **1,193** | **0** | **2,124** | **321** |

---

## 5. Tabla Cruzada de Reconciliación: Clasificación Primaria × Acción Siguiente

La tabla cruzada demuestra la reconciliación exacta del 100% de las 4,838 posiciones contractuales y aclara el destino operativo de cada una:

```
┌──────────────────────────────┬──────────────────────────────┬───────────┐
│ Clasificación Primaria       │ Acción Siguiente Recomendada │ Posiciones│
├──────────────────────────────┼──────────────────────────────┼───────────┤
│ MATCH                        │ NO_ACTION                    │       648 │
│ MISSING_IN_DB                │ FIX_MAPPING                  │       316 │
│ MISSING_IN_DB                │ IMPORT_BASELINE              │       141 │
│ DIFFERENT                    │ DATA_REMEDIATION_REQUIRED    │        95 │
│ LEGITIMATELY_BLANK_IN_EXCEL  │ NO_ACTION                    │     1,193 │
│ CALCULATED_FIELD             │ NO_ACTION                    │       312 │
│ CALCULATED_FIELD             │ RECALCULATE_IN_BACKEND       │     1,776 │
│ CALCULATED_FIELD             │ NOT_APPLICABLE               │        36 │
│ NOT_APPLICABLE               │ NOT_APPLICABLE               │       321 │
├──────────────────────────────┴──────────────────────────────┼───────────┤
│ TOTAL UNIVERSO RECONCILIADO                                 │     4,838 │
└─────────────────────────────────────────────────────────────┴───────────┘
```

### Reconciliación Específica de Candidatos a Importación (`BASELINE_IMPORT_CANDIDATES = 141`)
- **Total de registros en `baseline_import_candidates.csv`:** Exactamente **141 posiciones**.
- **Composición:**
  - **125 descripciones de controles** en campos 20, 24, 28 (riesgos que poseen controles institucionales en Excel mientras la tabla hija relacional `RL_MR_CONTROLES_RIESGO` en producción está vacía).
  - **16 planes de mitigación** en campos 40, 42, 44, 45, 46, 47, 48, 49 (para riesgos donde la respuesta es "Mitigar" y el Excel posee contenido baseline, mientras las tablas hijas `RL_MR_PLANES` y `RL_MR_ACTIVIDADES` están vacías).
- **Cero confusión:** Los 316 casos de campos no mapeados (campos 06, 07, 15, 16, 32 en riesgos con valor en Excel) fueron direccionados a `FIX_MAPPING`, garantizando que la importación de baseline contenga únicamente datos estructurables en tablas hijas.

---

## 6. Taxonomía Separada de Defectos e Inconsistencias

| Tipo de Defecto / Hallazgo | Cantidad | Descripción Técnica y Alcance |
|---|:---:|---|
| **TECHNICAL_MAPPING_ERROR** | **413** | 59 riesgos × 7 campos sin persistencia en producción actual (Campos 03, 05, 06, 07, 15, 16, 32). Clasificados con `technicalMappingStatus=BROKEN`, `reasonCode=MISSING_PERSISTENCE_MAPPING`, `recommendedNextAction=FIX_MAPPING`. |
| **DATA_ABSENCE_CASES** | **141** | 141 posiciones con contenido en Excel pero ausencia física en tablas hijas relacionales (125 controles + 16 mitigaciones). Destino: `baseline_import_candidates.csv`. |
| **VALID_EMPTY_COLLECTIONS** | **52** | 52 posiciones donde la colección hija vacía en DB es semánticamente válida: 12 coincidencias exactas con `"No hay"` (`MATCH`) y 40 coincidencias con celda en blanco (`LEGITIMATELY_BLANK_IN_EXCEL`). |
| **REQUIRED_VALUES_MISSING** | **118** | 118 posiciones que corresponden a campos obligatorios según el manifiesto (`requirementLevel=REQUIRED`) pero que no poseen persistencia en la DB actual (Campo 03: 59 riesgos; Campo 05: 59 riesgos). |

---

## 7. Conflictos de Catálogos entre Fuentes del Proyecto

Durante la auditoría forense se identificaron formalmente los siguientes conflictos de catálogos:
- **`INTERNAL_SOURCE_CATALOG_CONFLICTS: 1`**: Conflicto interno entre hojas del workbook institucional:
  - En `Matriz Consolidada` y `Listas`: las opciones para Respuesta al Riesgo son `['Evitar', 'Transferir', 'Aceptar', 'Mitigar']`.
  - En la hoja `Instructivo`: el texto indica `['Reducir', 'Aceptar', 'Transferir', 'Evitar']` (utilizando "Reducir" en lugar de "Mitigar").
  - **Decisión de Gobernanza:** `CATALOG_SOURCE_CONFLICT_RESPONSE_RISK=YES`, asignado al **Bloque 3** para estandarización y sellado normativo formal.
- **`DB_VS_EXCEL_CATALOG_CONFLICTS: 4`**: En el Campo 39 (`Respuesta al riesgo`), existen 4 riesgos donde el valor persistido en DB difiere del valor registrado en el Excel oficial (`DIFFERENT`, acción `DATA_REMEDIATION_REQUIRED`).

---

## 8. Caso de Control Canónico Reauditado: ROP-CUMP-59

El riesgo de cumplimiento **ROP-CUMP-59** fue reauditado exhaustivamente con la semántica institucional corregida:

| Campo | Etiqueta | Excel Oficial | Producción Actual | Clasificación | Motivo Técnico | Acción Siguiente |
|:---:|---|---|---|:---:|---|:---:|
| **03** | Macroproceso | Apoyo | *(sin mapeo)* | `MISSING_IN_DB` | `MISSING_PERSISTENCE_MAPPING` | `FIX_MAPPING` |
| **04** | Área Consolidada | Sección de Cumplimiento | Sección de Cumplimiento | `MATCH` | `EXACT_RAW_MATCH` | `NO_ACTION` |
| **05** | Tipo de Riesgo | Operativo | *(sin mapeo)* | `MISSING_IN_DB` | `MISSING_PERSISTENCE_MAPPING` | `FIX_MAPPING` |
| **06** | Procedimiento | Atención a requerimientos... | *(sin mapeo)* | `MISSING_IN_DB` | `MISSING_PERSISTENCE_MAPPING` | `FIX_MAPPING` |
| **07** | Objetivo(s) Estratégico(s) | Suficiencia y sostenibilidad... | *(sin mapeo)* | `MISSING_IN_DB` | `MISSING_PERSISTENCE_MAPPING` | `FIX_MAPPING` |
| **12** | Valor del Riesgo Inherente | *(fórmula)* | 3 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **13** | Nivel de Riesgo Inherente | *(fórmula)* | BAJO | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **15** | Régimen afectado | IVM | *(sin mapeo)* | `MISSING_IN_DB` | `MISSING_PERSISTENCE_MAPPING` | `FIX_MAPPING` |
| **16** | Transversalidad o Interrelación | Riesgo de Cumplimiento | *(sin mapeo)* | `MISSING_IN_DB` | `MISSING_PERSISTENCE_MAPPING` | `FIX_MAPPING` |
| **20** | Descripción de Control Preventivo | Preventivo: Existencia de manual... | *(vacío)* | `MISSING_IN_DB` | `CHILD_COLLECTION_EMPTY` | `IMPORT_BASELINE` |
| **21** | Escala de efectividad preventivo | Alta Efectividad | 90 | `MATCH` | `EFFECTIVENESS_SCALE_SEMANTIC_MATCH` | `NO_ACTION` |
| **24** | Descripción de Control Detectivo | No hay | No hay *(proyectado)* | `MATCH` | `NO_CONTROLS_CANONICAL_MATCH` | `NO_ACTION` |
| **25** | Escala de efectividad detectivo | Inexistente | 0 *(normalizado)* | `MATCH` | `EFFECTIVENESS_SCALE_SEMANTIC_MATCH` | `NO_ACTION` |
| **28** | Descripción de Control Correctivo | No hay | No hay *(proyectado)* | `MATCH` | `NO_CONTROLS_CANONICAL_MATCH` | `NO_ACTION` |
| **29** | Escala de efectividad correctivo | Inexistente | 0 *(normalizado)* | `MATCH` | `EFFECTIVENESS_SCALE_SEMANTIC_MATCH` | `NO_ACTION` |
| **32** | Nivel de Automatización | Manual | *(sin mapeo)* | `MISSING_IN_DB` | `MISSING_PERSISTENCE_MAPPING` | `FIX_MAPPING` |
| **33** | Efectividad Total Ponderada (ETP)| *(fórmula)* | 63.00 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **34** | Riesgo Residual | *(fórmula)* | Multas y Sanciones por... | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **35** | Frecuencia Residual | *(fórmula)* | 1 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **36** | Impacto Residual | *(fórmula)* | 1 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **37** | Valor del Riesgo Residual | *(fórmula)* | 1 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **38** | Nivel del Riesgo Residual | *(fórmula)* | BAJO | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **41** | No. Acciones de Mitigación | *(fórmula)* | 0 | `CALCULATED_FIELD` | `MITIGATION_NOT_REQUIRED` | `RECALCULATE_IN_BACKEND` |
| **43** | Cantidad de Actividades | *(fórmula)* | 0 | `CALCULATED_FIELD` | `MITIGATION_NOT_REQUIRED` | `RECALCULATE_IN_BACKEND` |

---

## 9. Inventario de Artefactos Forenses Definitivos (%TEMP%)

Los 11 artefactos fueron regenerados determinísticamente y almacenados fuera del repositorio para garantizar cero exposición de datos sensibles:

- **Directorio de Artefactos:** `%TEMP%\RIESGO_LAVADO_BLOCK2_AUDIT_20261002_134545`

| Archivo | SHA-256 | Filas | Descripción |
|---|---|:---:|---|
| `audit_59x82_full.json` | `66366dc16567568cfdd7c45bac437cbf67a854021e663415c931267e26ca097e` | 4,838 | Universo completo 59×82 con metadatos forenses y taxonomía corregida |
| `audit_59x82_full.csv` | `17ef42269c92ca3e68e706192f3bf74aa0e7d66ca84fd211522872eee14e475f` | 4,838 | Matriz tabular de auditoría forense |
| `summary_by_risk.csv` | `16d1933835710a536171c21d1fca4fdb59dd14aa7372b4e1d8630ee270496e07` | 59 | Conteo de clasificaciones por cada uno de los 59 riesgos institucionales |
| `summary_by_field.csv` | `7b48d840bc625aed934cd3ad1c2149de8f13e88316b17f69db670ba0f7b70ac3` | 82 | Conteo de clasificaciones por cada uno de los 82 campos canónicos |
| `summary_by_block.csv` | `75670a19c0e4819e223aa550c0367cc13bde24ed513fe0eba71f208ec9c426aa` | 6 | Resumen por bloque funcional con métricas consolidadas |
| `calculated_parity.csv` | `342ba79846aedce323c7fe333e37dafd22e658eb1d54dd4eae4dffa957ab88d0` | 2,124 | Diagnóstico de paridad para los 36 campos calculados/derivados |
| `operational_values_to_preserve.csv` | `984f6ac3b439fc591d7ab8bf53d9f30d87a6464005a17ac07f8624be0369a1d2` | 0 | Valores productivos operacionales vigentes a proteger contra sobreescritura |
| `baseline_import_candidates.csv` | `f7e331148e0c8826bd9e098561a6206792e5cce7084ae60db4832bcabc10fd39` | 141 | 141 posiciones candidatas a carga relacional en tablas hijas (Bloque 4) |
| `data_conflicts.csv` | `ddfe7c126d4ba7cce4753de9fc883effca4f0d2239814cc75d1415593058937b` | 95 | 95 discrepancias reales de contenido a remediar en Bloque 3/4 |
| `technical_defects.csv` | `6df839eb8de17cd29fd2f27fc9e91b54dc9bf2171532de30b4d23175d89eb8b4` | 413 | 413 posiciones con defecto técnico de persistencia (`technicalMappingStatus=BROKEN`) |
| `production_risk_inventory.csv` | `94b8ea6e564faf3921d3952fb4ed5d58cdfc7d4ea0338c134b8a5f1b5d8cdedc` | 59 | Inventario formal de los 59 riesgos activos en producción |

---

## 10. Confirmación de Scripts de Soporte a la Auditoría

- **`tools/export_excel_matrix_82.js`:** Es una utilidad de extracción fuera de línea de uso exclusivo para alimentar la auditoría forense (`AUDIT_SUPPORT_ONLY=YES`). No forma parte del runtime de la aplicación Angular ni del backend ASP.NET Core (`EXPORT_RUNTIME_BEHAVIOR_CHANGED=NO`).

---

## 11. Conclusión y Cierre de Bloque 2

El **Bloque 2 queda cerrado con éxito definitivo y certificación forense incontrovertible**.
- **CERO DML / CERO DDL / CERO ESCRITURAS EN PRODUCCIÓN** (`DATABASE_WRITES=0`).
- **4,838 posiciones clasificadas al 100%** con reconciliación determinista y taxonomía separada.
- **NO INICIAR BLOQUE 3.** Se detiene la ejecución para aguardar la aprobación expresa del usuario.
