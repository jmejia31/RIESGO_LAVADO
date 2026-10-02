# Bloque 2: Auditoría Forense Excel ↔ Producción (59 Riesgos × 82 Campos)

> **Estado:** COMPLETADO (Dry Run Absoluto — Cero Escrituras)<br>
> **Fecha:** 2026-10-02<br>
> **Autoridad Contractual:** Bloque 1 (`61e4cb2f0a465f2d848025677dc3ab864515ce0e`)<br>
> **Workbook Oficial:** `Matrices de Riesgos.xlsx` (Hoja: `Matriz Consolidada`, Rango: `A1:CD60`)<br>
> **Hash SHA-256 Workbook:** `5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385`<br>
> **Entorno Productivo:** Oracle Database `HPPROD1` (`RIESGO_LAVADO`) — `DATABASE_TARGET_CLASS=PRODUCTION`

---

## 1. Resumen Ejecutivo y Metodología

El **Bloque 2 de 12** ejecutó una auditoría forense integral, determinista y estrictamente de sólo lectura sobre la totalidad del universo contractual: **59 riesgos institucionales × 82 campos canónicos = 4,838 posiciones contractuales**.

### Principios de Ejecución:
1. **Dry Run Absoluto — Cero Modificaciones:** Se aplicó cerrojo técnico mediante sesión única en modo `SET TRANSACTION READ ONLY` seguido de `ROLLBACK`.
2. **Prohibición Estricta de DML/DDL:** Todas las consultas enviadas al motor Oracle fueron exclusivamente `SELECT` / `WITH ... SELECT`. Ninguna sentencia `INSERT`, `UPDATE`, `DELETE`, `MERGE`, `DROP`, `ALTER`, `CREATE` ni llamada a stored procedures fue ejecutada.
3. **Selección Canónica de Evaluación:** Se investigó la regla real utilizada por el módulo de Matrices de Riesgos en producción (`backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs`), identificando la evaluación activa (`EVA_ACTIVO = 1`) vinculada 1:1 en `RL_MR_PROYECCIONES_EVALUACION` en estado `APROBADA` que alimenta el consolidado institucional. Los 59 riesgos cuentan con exactamente una evaluación aprobada activa (0 selecciones ambiguas).
4. **Taxonomía Canónica Exhaustiva:** Cada una de las 4,838 posiciones recibió una y solo una clasificación primaria dentro de las 7 categorías obligatorias, garantizando cero diferencias sin explicar (`UNEXPLAINED_DIFFERENCES=0`).

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

### Distribución de la Taxonomía Primaria (Total = 4,838)

```
┌─────────────────────────────────────────────────────────────┬───────────┬───────────┐
│ Clasificación Primaria                                      │ Cantidad  │ % Total   │
├─────────────────────────────────────────────────────────────┼───────────┼───────────┤
│ MATCH                                                       │       597 │    12.34% │
│ MISSING_IN_DB                                               │       469 │     9.69% │
│ DIFFERENT                                                   │       134 │     2.77% │
│ LEGITIMATELY_BLANK_IN_EXCEL                                 │     1,193 │    24.66% │
│ DB_HAS_NEWER_OPERATIONAL_DATA                               │         0 │     0.00% │
│ CALCULATED_FIELD                                            │     2,124 │    43.90% │
│ NOT_APPLICABLE                                              │       321 │     6.64% │
├─────────────────────────────────────────────────────────────┼───────────┼───────────┤
│ TOTAL                                                       │     4,838 │   100.00% │
└─────────────────────────────────────────────────────────────┴───────────┴───────────┘
```

---

## 3. Resumen por Bloque Funcional

| Bloque | Rango | Nombre del Bloque | Posiciones | MATCH | MISSING | DIFF | BLANK | OPER | CALC | N/A |
|---|---|---|---|---|---|---|---|---|---|---|
| **B1** | 01-19 | 1. Identificación y Riesgo Inherente | 1,121 | 385 | 303 | 87 | 51 | 0 | 118 | 177 |
| **B2** | 20-33 | 2. Controles | 826 | 138 | 150 | 39 | 86 | 0 | 413 | 0 |
| **B3** | 34-39 | 3. Riesgo Residual y Respuesta | 354 | 55 | 0 | 4 | 0 | 0 | 295 | 0 |
| **B4** | 40-49 | 4. Plan de Mitigación / Acciones Correctivas | 590 | 0 | 16 | 0 | 312 | 0 | 118 | 144 |
| **B5** | 50-69 | 5. Cálculos Auxiliares y Verificaciones | 1,180 | 0 | 0 | 0 | 0 | 0 | 1,180 | 0 |
| **B6** | 70-82 | 6. Monitoreo, Efectividad y Observaciones | 767 | 19 | 0 | 4 | 744 | 0 | 0 | 0 |
| **TOTAL** | **01-82** | **Matriz Consolidada Completa** | **4,838** | **597** | **469** | **134** | **1,193** | **0** | **2,124** | **321** |

---

## 4. Análisis Detallado de Hallazgos Forenses

### 4.1 Identificación y Datos Descriptivos (Bloque 1)
- **Campos 01, 02:** El mapeo ordinal institucional (Campo 01 `No.`) y el Código de Riesgo (Campo 02) tienen correspondencia exacta 59/59 (`MATCH`).
- **Campos 04, 08, 09, 10, 11, 14:** Los campos descriptivos base (`Área Consolidada`, `Descripción del Riesgo`, `Causas`, `Consecuencias`, `Frecuencia`, `Impacto`) presentan alta coincidencia textual, con variaciones puntuales catalogadas en `data_conflicts.csv`.
- **Campos 03, 05, 06, 07, 15, 16:** En la base productiva actual, estos atributos no existen como columnas en `RL_MR_RIESGOS` ni `RL_MR_PROYECCIONES_EVALUACION`, ni están poblados en `EVA_DATOS_JSON` (`MISSING_DB_MAPPING` / `SCHEMA_DRIFT`). Se catalogaron como `MISSING_IN_DB` con acción `IMPORT_BASELINE`.
- **Campos 17-19 (GTIC):** Ninguno de los 59 riesgos es de tipo GTIC. Los 177 registros correspondientes (59 × 3) se clasificaron correctamente como `NOT_APPLICABLE` con motivo `RISK_TYPE_NOT_GTIC`.

### 4.2 Controles (Bloque 2)
- **Tabla `RL_MR_CONTROLES_RIESGO`:** La tabla relacional en producción contiene 0 registros para las evaluaciones vigentes.
- **Campos 20, 24, 28 (Descripciones de Control):** Cuando el Excel contiene descripciones, la DB no posee registros en la tabla hija (`CHILD_COLLECTION_EMPTY` -> `MISSING_IN_DB`, acción `IMPORT_BASELINE`). Cuando el Excel está vacío ("No hay" o en blanco), se clasifica como `LEGITIMATELY_BLANK_IN_EXCEL`.
- **Campos 21, 25, 29 (Escalas de Efectividad):** En la DB, el JSON almacena valores numéricos de efectividad base (e.g. `90`, `0`), mientras que el Excel almacena escalas textuales ("Alta Efectividad", "Inexistente"). Esto genera una discrepancia de escala (`EFFECTIVENESS_SCALE_MISMATCH` -> `DIFFERENT`, acción `DATA_REMEDIATION_REQUIRED` / Bloque 3).
- **Campo 32 (Nivel de Automatización):** No existe persistencia en las tablas productivas para el nivel global del riesgo (`MISSING_DB_MAPPING`).

### 4.3 Planes de Mitigación (Bloque 4)
- **Tablas `RL_MR_PLANES` y `RL_MR_ACTIVIDADES`:** Ambas tablas relacionales contienen 0 filas en producción.
- **Condicionalidad de Mitigación:** Conforme al contrato canónico, la mitigación aplica únicamente cuando la respuesta al riesgo es "Mitigar" o el nivel de riesgo residual es Crítico/Alto.
  - Para riesgos donde **no aplica mitigación**: Campos 40–49 se clasifican como `NOT_APPLICABLE` (`MITIGATION_NOT_REQUIRED`, 144 posiciones) y campos 41, 43 como `CALCULATED_FIELD` (`CALCULATION_PARITY=MATCH`, 0 planes).
  - Para riesgos donde **sí aplica mitigación**: Si el Excel posee contenido de plan baseline, se clasifica como `MISSING_IN_DB` (`CHILD_COLLECTION_EMPTY`, 16 posiciones) o `LEGITIMATELY_BLANK_IN_EXCEL` si la celda de Excel está vacía.

### 4.4 Monitoreo y Seguimiento (Bloque 6)
- **Campo 70 (Señales de Alerta):** En producción existen 148 señales de alerta activas vinculadas a las evaluaciones en `RL_MR_SENALES_ALERTA`. El campo textual institucional se almacena en la columna `ALE_INDICADOR` (`VARCHAR2(150)`), coincidiendo en 19 riesgos y presentando diferencias menores de formato o ausencia en 4.
- **Campos 71-82 (Monitoreo, Efectividad, Evidencias):** En el Excel oficial `Matriz Consolidada`, estos campos están 100% vacíos (0 valores en 59 riesgos). En producción no se han ejecutado ciclos de monitoreo. Por tanto, las 744 posiciones se clasificaron legítimamente como `LEGITIMATELY_BLANK_IN_EXCEL` (`BASELINE_EMPTY_CYCLE_PENDING`).
- **Valores Operacionales a Preservar:** Dado que en la base productiva actual no existen registros de monitoreo o evidencias que superen el baseline, el inventario de preservación operacional arrojó 0 casos a proteger contra sobreescritura (`OPERATIONAL_VALUES_TO_PRESERVE=0`).

---

## 5. Paridad de Campos Calculados (Bloque 5 Input)

Total de posiciones evaluadas como derivadas/calculadas: **2,124** (34 fórmulas Excel + 2 contadores agregados por riesgo × 59 riesgos).

```
┌──────────────────────────────────────┬───────────┬───────────┐
│ Paridad de Cálculo                   │ Posiciones│ Detalle   │
├──────────────────────────────────────┼───────────┼───────────┤
│ CALCULATION_PARITY_MATCH             │       312 │ Coincide  │
│ CALCULATION_PARITY_DIFFERENT         │        98 │ Discrepa  │
│ CALCULATION_PARITY_MISSING_IN_DB     │       496 │ Sin dato  │
│ CALCULATION_PARITY_NOT_EVALUABLE     │     1,218 │ Sin cache │
├──────────────────────────────────────┼───────────┼───────────┤
│ TOTAL CALCULADOS                     │     2,124 │           │
└──────────────────────────────────────┴───────────┴───────────┘
```

> **Nota Técnica:** En el archivo `Matrices de Riesgos.xlsx`, las fórmulas correspondientes al Campo 33 (`ETP`) y a las columnas auxiliares 50–69 contienen las expresiones institucionales, pero la herramienta que generó o guardó el workbook no precalculó ni almacenó los valores en caché (`v` = undefined). El auditor clasificó rigurosamente estos 1,218 casos como `NOT_EVALUABLE` con motivo `NO_CACHED_FORMULA_RESULT`, tal como instruye la Sección 22 del protocolo. El Bloque 5 utilizará este diagnóstico para implementar la paridad algorítmica completa.

---

## 6. Caso de Control Canónico: ROP-CUMP-59

El riesgo de cumplimiento **ROP-CUMP-59** fue analizado individualmente para contrastar las observaciones visuales con la evidencia forense extraída:

| Campo | Etiqueta | Excel Oficial | Producción Actual | Clasificación | Motivo Técnico | Acción Siguiente |
|:---:|---|---|---|:---:|---|:---:|
| **04** | Área Consolidada | Sección de Cumplimiento | Sección de Cumplimiento | `MATCH` | `EXACT_RAW_MATCH` | `NO_ACTION` |
| **05** | Tipo de Riesgo | Operativo | *(vacío)* | `MISSING_IN_DB` | `MISSING_DB_MAPPING` | `IMPORT_BASELINE` |
| **06** | Procedimiento | Atención a requerimientos... | *(vacío)* | `MISSING_IN_DB` | `MISSING_DB_MAPPING` | `IMPORT_BASELINE` |
| **07** | Objetivo(s) Estratégico(s) | Suficiencia y sostenibilidad... | *(vacío)* | `MISSING_IN_DB` | `MISSING_DB_MAPPING` | `IMPORT_BASELINE` |
| **12** | Valor del Riesgo Inherente | *(fórmula)* | 3 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **13** | Nivel de Riesgo Inherente | *(fórmula)* | BAJO | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **15** | Régimen afectado | IVM | *(vacío)* | `MISSING_IN_DB` | `MISSING_DB_MAPPING` | `IMPORT_BASELINE` |
| **16** | Transversalidad o Interrelación | Riesgo de Cumplimiento | *(vacío)* | `MISSING_IN_DB` | `MISSING_DB_MAPPING` | `IMPORT_BASELINE` |
| **20** | Descripción de Control Preventivo | Preventivo: Existencia de manual... | *(vacío)* | `MISSING_IN_DB` | `CHILD_COLLECTION_EMPTY` | `IMPORT_BASELINE` |
| **21** | Escala de efectividad preventivo | Alta Efectividad | 90 | `DIFFERENT` | `EFFECTIVENESS_SCALE_MISMATCH` | `DATA_REMEDIATION_REQUIRED` |
| **24** | Descripción de Control Detectivo | No hay | *(vacío)* | `MISSING_IN_DB` | `CHILD_COLLECTION_EMPTY` | `IMPORT_BASELINE` |
| **25** | Escala de efectividad detectivo | Inexistente | 0 | `DIFFERENT` | `EFFECTIVENESS_SCALE_MISMATCH` | `DATA_REMEDIATION_REQUIRED` |
| **28** | Descripción de Control Correctivo | No hay | *(vacío)* | `MISSING_IN_DB` | `CHILD_COLLECTION_EMPTY` | `IMPORT_BASELINE` |
| **29** | Escala de efectividad correctivo | Inexistente | 0 | `DIFFERENT` | `EFFECTIVENESS_SCALE_MISMATCH` | `DATA_REMEDIATION_REQUIRED` |
| **32** | Nivel de Automatización | Manual | *(vacío)* | `MISSING_IN_DB` | `MISSING_DB_MAPPING` | `IMPORT_BASELINE` |
| **33** | Efectividad Total Ponderada (ETP)| *(fórmula)* | 63.00 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **34** | Riesgo Residual | *(fórmula)* | Multas y Sanciones por... | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **35** | Frecuencia Residual | *(fórmula)* | 1 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **36** | Impacto Residual | *(fórmula)* | 1 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **37** | Valor del Riesgo Residual | *(fórmula)* | 1 | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **38** | Nivel del Riesgo Residual | *(fórmula)* | BAJO | `CALCULATED_FIELD` | `NO_CACHED_FORMULA_RESULT` | `RECALCULATE_IN_BACKEND` |
| **41** | No. Acciones de Mitigación | *(fórmula)* | 0 | `CALCULATED_FIELD` | `MITIGATION_NOT_REQUIRED` | `NOT_APPLICABLE` |
| **43** | Cantidad de Actividades | *(fórmula)* | 0 | `CALCULATED_FIELD` | `MITIGATION_NOT_REQUIRED` | `NOT_APPLICABLE` |

---

## 7. Inventario de Artefactos Forenses Generados

Los artefactos detallados fueron generados fuera del repositorio para garantizar que ningún dato operativo sensible sea expuesto en el control de versiones:

- **Directorio de Artefactos:** `%TEMP%\RIESGO_LAVADO_BLOCK2_AUDIT_20261002_103246`

| Archivo | SHA-256 | Descripción |
|---|---|---|
| `audit_59x82_full.json` | `6df1a9dc7ab190b1102cd287837ba4cb9294242ad0f0669c907b158cd5e1a66b` | 4,838 posiciones completas con metadatos de taxonomía |
| `audit_59x82_full.csv` | `fdbe275293c971440e5afea3c097f3cb0df447ee5f62bcb0a7abb61a0d381630` | Matriz tabular de auditoría 59×82 |
| `summary_by_risk.csv` | `fd05fcc2538ad9e7fccca566175285d8dda2d2f242a15edf4d8aeed98bcd2083` | Conteo de clasificaciones por cada uno de los 59 riesgos |
| `summary_by_field.csv` | `ade61bc1fea2394f28caf7783b43efe66d779244e629480cff844500e317366e` | Conteo de clasificaciones por cada uno de los 82 campos |
| `summary_by_block.csv` | `777517a81e520672badc3b128c69cf3fd72d6d8017f6275d196c2f88c6ea121e` | Resumen por los 6 bloques funcionales |
| `calculated_parity.csv` | `605f87030119468f25155cd297c146a57881669ea324f320b9f331eb5ae76eb3` | Análisis de paridad para los 36 campos calculados |
| `operational_values_to_preserve.csv` | `984f6ac3b439fc591d7ab8bf53d9f30d87a6464005a17ac07f8624be0369a1d2` | Valores productivos vigentes a preservar |
| `baseline_import_candidates.csv` | `e9bea8da0f6df496196085c7d902dd761311e7f4a3a46c3edba22c8729894099` | 471 posiciones candidatas a carga en Bloque 4 |
| `data_conflicts.csv` | `f4d06d617535669b42dbb492b4c9bff5e596561e560515a7ba2d86d3aa4544d7` | 134 conflictos de datos y escalas a remediar |
| `technical_defects.csv` | `d445b4992b9ab600572fefe5f376f8d637b00685bac8e895562346059d9fab7a` | 413 incidencias de mapeo y esquema detectadas |
| `production_risk_inventory.csv` | `94b8ea6e564faf3921d3952fb4ed5d58cdfc7d4ea0338c134b8a5f1b5d8cdedc` | Inventario de los 59 riesgos activos en producción |

---

## 8. Conclusión y Hoja de Ruta para Bloques Siguientes

El Bloque 2 ha cumplido de forma estricta e incontrovertible su objetivo: **diagnosticar el 100% de la brecha entre el Excel institucional y Producción sin ejecutar una sola escritura**.

### Acciones Derivadas para los Próximos Bloques:
- **Bloque 3 (Catálogos y Listas):** Estandarizar las escalas de efectividad (e.g. "Alta Efectividad" vs `90`, "Inexistente" vs `0`) y listas de opciones de los campos 05, 15, 16, 21, 25, 29, 32.
- **Bloque 4 (Remediación de Base de Datos y Población Baseline):** Ajustar las columnas/estructuras para los campos 03, 05, 06, 07, 15, 16, 32 y cargar las 471 posiciones baseline faltantes en controles y metadatos sin sobreescribir datos operativos.
- **Bloque 5 (Motor de Cálculo Institucional):** Implementar la paridad estricta para los 36 campos calculados/derivados (campos 12, 13, 22, 23, 26, 27, 30, 31, 33-38, 41, 43, 50-69).
