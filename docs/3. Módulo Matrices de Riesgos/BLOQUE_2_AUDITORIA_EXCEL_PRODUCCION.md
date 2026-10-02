# Bloque 2 — Auditoría Excel ↔ Producción (sellado final)

> **Estado técnico:** gates de código y auditoría read-only aprobados; publicación final en `desarrollo` registrada por el cierre de esta intervención. Bloque 3 no iniciado.
> **Workbook:** `Matrices de Riesgos.xlsx`, hoja `Matriz Consolidada`, rango `A1:CD60`.
> **SHA-256:** `5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385`.
> **Contrato:** manifest canónico 82 campos. La intervención corrigió metadatos técnicos de persistencia de los campos 03, 05, 06, 07, 15, 16 y 70; no cambió la secuencia, claves canónicas ni etiquetas Excel.
> **RUN_ID:** `20261002_142828_041` (artefactos fuera de Git en `%TEMP%\RIESGO_LAVADO_BLOCK2_AUDIT_20261002_142828_041`).

## Metodología y fuente

Se verificó el SHA-256 del workbook contra el valor institucional congelado. Una lectura compartida respetó el bloqueo de Excel; la copia temporal se extrajo directamente con ExcelJS. El resultado fue 82 encabezados, 59 filas de riesgos y 59 códigos únicos. Para `ROP-CUMP-59`, la lectura de la columna C/F03 fue `Sección de Cumplimiento`, D/F04 `Sección de Cumplimiento` y E/F05 `Operativo`.

El exportador auxiliar conserva el ordinal de columna (`fieldNumber = c`) y el valor original de celda. El auditor valida hash actual del workbook, hash de la extracción, etiquetas del manifest contra encabezados y tamaño del universo antes de abrir Oracle. Cualquier diferencia de etiqueta o sustitución de workbook detiene la ejecución.

La extracción de producción usó una sesión Oracle con `SET TRANSACTION READ ONLY`, consultas `SELECT` y `ROLLBACK`. Identidad comprobada: `HPPROD1 / hpprod1 / hpprod1 / RIESGO_LAVADO / RIESGO_LAVADO` (`DB_NAME / SERVICE_NAME / INSTANCE_NAME / CURRENT_SCHEMA / SESSION_USER`). La auditoría seleccionó la evaluación activa vinculada a la proyección consolidada. No ejecutó DML, DDL ni procedimientos.

## Hallazgo y corrección del Campo 03

La fuente equivocada `Macroproceso / Apoyo` estaba en la tabla del informe sanitizado anterior. El manifest actual ya declaraba `Área`, el helper leía la celda C directamente y la extracción con el hash oficial confirma `Sección de Cumplimiento`. **Causa raíz: `REPORT_ONLY_BUG`.** Se corrigió el reporte y se añadió una aserción regresiva al helper de auditoría para `ROP-CUMP-59 / FIELD_03`.

El mapping productivo existe en `EVA_DATOS_JSON.area_principal`: 59/59 valores comparan `MATCH`; el estado técnico es `VALID_JSON`. La etiqueta del Campo 03 es exactamente `Área`; no se encontró mapping roto para este campo.

## Reauditoría de mappings reportados

`AFFECTED_POSITIONS` indica posiciones con defecto técnico de mapping. Los faltantes bajo mappings válidos son datos no presentes en producción, con acción baseline cuando corresponde.

| Campo | Etiqueta | Excel lleno / blanco | Mapping persistente | Estado | Posiciones con defecto técnico | Faltante en DB | Próxima acción |
|---:|---|---:|---|---|---:|---:|---|
| 03 | Área | 59 / 0 | `EVA_DATOS_JSON.area_principal` | `VALID_JSON` | 0 | 0 | `NO_ACTION` |
| 05 | Tipo de Riesgo | 59 / 0 | `EVA_DATOS_JSON.tipo_riesgo` | `VALID_JSON` | 0 | 59 | `IMPORT_BASELINE` |
| 06 | Procedimiento | 59 / 0 | `EVA_DATOS_JSON.procedimiento` | `VALID_JSON` | 0 | 59 | `IMPORT_BASELINE` |
| 07 | Objetivo(s) Estratégico(s) | 11 / 48 | `EVA_DATOS_JSON.objetivos_estrategicos` | `VALID_JSON` | 0 | 11 | `IMPORT_BASELINE` / `NO_ACTION` |
| 15 | Régimen afectado | 56 / 3 | `EVA_DATOS_JSON.regimen_afectado` | `VALID_JSON` | 0 | 56 | `IMPORT_BASELINE` / `NO_ACTION` |
| 16 | Transversalidad o Interrelación con otros Riesgos | 59 / 0 | `EVA_DATOS_JSON.transversalidad` | `VALID_JSON` | 0 | 59 | `IMPORT_BASELINE` |
| 32 | Nivel de Automatización de los Controles | 13 / 46 | `RL_MR_CONTROLES_RIESGO.CON_AUTOMATIZACION` | `VALID_RELATION` | 0 | 13 | `IMPORT_BASELINE` / `NO_ACTION` |

Los siete destinos existen en JSON persistido o relación Oracle. `TECHNICAL_MAPPING_ERRORS=0`. El manifest y esta tabla identifican `EVA_DATOS_JSON` (nombre real en el DDL versionado) y `ALE_INDICADOR` para señales; las descripciones del contrato se alinearon con el esquema y los DTOs existentes.

`REQUIRED_VALUES_MISSING=59`, todos del Campo 05 (requiredness leído del manifest). Son faltantes de valor, no defectos de mapping; tienen destino JSON válido. No se alteraron datos productivos.

## Campo 70 — Señales de Alerta

Política leída del manifest vigente:

| Propiedad | Valor |
|---|---|
| `fieldNumber` / `label` | `70` / `Señales de Alerta` |
| `initialBaselineImportRule` | `IMPORT_IF_NOT_EMPTY` (crear señales desde el baseline cuando aplique) |
| `subsequentReconciliationRule` | `PRESERVE_OPERATIONAL_DB` |
| `preserveExistingOperationalValue` | `true` |
| `excelNullBehavior` | `KEEP_EXISTING_DB_RECORDS` |
| `source` / `mode` | `MONITORING` / `OPERATIONAL` |

El mapping físico correcto es `RL_MR_SENALES_ALERTA.ALE_INDICADOR` (`VARCHAR2(150)`); la fecha disponible es `ALE_FECHA_DISPARO`. El DDL versionado no define `CREATED_AT` ni `UPDATED_AT`, por lo que no se usaron. Se extrajeron IDs, indicadores, fecha disponible y valores estructurados para los casos operacionales al artefacto temporal `field70_operational_authority.csv`.

| Métrica | Conteo |
|---|---:|
| No vacías en Excel | 23 |
| `MATCH` | 19 |
| `DIFFERENT` final | 0 |
| `DB_HAS_NEWER_OPERATIONAL_DATA` | 4 |
| En blanco legítimo | 36 |

En los cuatro casos que antes figuraban `DIFFERENT`, Excel y la lista estructurada de producción difieren. El contrato atribuye autoridad a producción para la reconciliación posterior al baseline; sin timestamp requerido para establecer precedencia, `newnessEvidence=CONTRACT_AUTHORITY_RULE`, `operationalAuthority=PRODUCTION` y `recommendedNextAction=PRESERVE_PRODUCTION`.

| Riesgo | Clasificación anterior | Clasificación final | Evidencia de autoridad | Acción |
|---|---|---|---|---|
| `ROTR-COMPRAS-18` (EVA 46) | `DIFFERENT` | `DB_HAS_NEWER_OPERATIONAL_DATA` | `CONTRACT_AUTHORITY_RULE` | `PRESERVE_PRODUCTION` |
| `RCUMP-COMPRASRRHH-26` (EVA 54) | `DIFFERENT` | `DB_HAS_NEWER_OPERATIONAL_DATA` | `CONTRACT_AUTHORITY_RULE` | `PRESERVE_PRODUCTION` |
| `RCUMP-COMPRAS-28` (EVA 56) | `DIFFERENT` | `DB_HAS_NEWER_OPERATIONAL_DATA` | `CONTRACT_AUTHORITY_RULE` | `PRESERVE_PRODUCTION` |
| `RCUMP-COMPRAS-32` (EVA 60) | `DIFFERENT` | `DB_HAS_NEWER_OPERATIONAL_DATA` | `CONTRACT_AUTHORITY_RULE` | `PRESERVE_PRODUCTION` |

`FIELD_70_OPERATIONAL_AUTHORITY=PASS`. El cambio de clasificación no afirma que un timestamp sea posterior ni recomienda sobrescribir producción.

## Campo 39 — Conflicto de catálogo delegado

El workbook contiene un conflicto institucional entre `Matriz Consolidada/Listas` (`Evitar`, `Transferir/Compartir`, `Aceptar`, `Mitigar`) y `Instructivo` (`Reducir`, `Aceptar`, `Transferir`, `Evitar`). No se eligió catálogo en Bloque 2. Se conservaron las cuatro posiciones como `DIFFERENT`, con `reasonCode=CATALOG_SOURCE_CONFLICT_RESPONSE_RISK`, `recommendedNextAction=FIX_CATALOG` y `targetBlock=3`; no se recomienda remediar directamente el dato productivo.

Riesgos: `RCUMP-COMPRAS-37`, `ROP-CUMP-50`, `ROP-CUMP-53` y `ROP-CUMP-54`. `FIELD_39_CONFLICTS_DEFERRED_TO_BLOCK3=4/4`.

## Universo, clasificación y acciones

`59 × 82 = 4,838`; 59 riesgos/59 y 82 etiquetas/82 auditados. `EXPECTED_POSITIONS=4,838`, `ACTUAL_POSITIONS=4,838`, `UNIQUE_AUDIT_KEYS=4,838`, `DUPLICATE_AUDIT_KEYS=0`, `UNCLASSIFIED_POSITIONS=0`, `UNEXPLAINED_DIFFERENCES=0`.

| Clasificación primaria | Posiciones |
|---|---:|
| `MATCH` | 804 |
| `MISSING_IN_DB` | 398 |
| `DIFFERENT` | 91 |
| `LEGITIMATELY_BLANK_IN_EXCEL` | 1,096 |
| `DB_HAS_NEWER_OPERATIONAL_DATA` | 4 |
| `CALCULATED_FIELD` | 2,124 |
| `NOT_APPLICABLE` | 321 |
| **Total** | **4,838** |

| Clasificación primaria | Acción siguiente | Posiciones |
|---|---|---:|
| `MATCH` | `NO_ACTION` | 804 |
| `MISSING_IN_DB` | `IMPORT_BASELINE` | 398 |
| `DIFFERENT` | `DATA_REMEDIATION_REQUIRED` | 87 |
| `DIFFERENT` | `FIX_CATALOG` | 4 |
| `LEGITIMATELY_BLANK_IN_EXCEL` | `NO_ACTION` | 1,096 |
| `DB_HAS_NEWER_OPERATIONAL_DATA` | `PRESERVE_PRODUCTION` | 4 |
| `CALCULATED_FIELD` | `NO_ACTION` | 312 |
| `CALCULATED_FIELD` | `NOT_APPLICABLE` | 36 |
| `CALCULATED_FIELD` | `RECALCULATE_IN_BACKEND` | 1,776 |
| `NOT_APPLICABLE` | `NOT_APPLICABLE` | 321 |
| **Total** |  | **4,838** |

La taxonomía conserva por separado `NO_ACTION`, `IMPORT_BASELINE`, `PRESERVE_PRODUCTION`, `RECALCULATE_IN_BACKEND`, `FIX_MAPPING`, `FIX_CATALOG`, `NOT_APPLICABLE`, `SOURCE_REVIEW_REQUIRED` y `DATA_REMEDIATION_REQUIRED`. No se generaron acciones `FIX_MAPPING` ni `SOURCE_REVIEW_REQUIRED` en este run: los destinos revisados existen y el conflicto de Campo 39 sí tiene `FIX_CATALOG` en la taxonomía.

`BASELINE_IMPORT_CANDIDATES=398` sólo incluye destinos persistentes válidos; `DATA_CONFLICTS=87` excluye los cuatro conflictos de catálogo; `OPERATIONAL_VALUES_TO_PRESERVE=4`; `TECHNICAL_MAPPING_ERRORS=0`. `FALSE_CONTROL_IMPORT_CANDIDATES=0` y `FALSE_SCALE_DATA_CONFLICTS=0`. La semántica de controles sigue en vigor: sin controles de tipo, descripción `No hay`, escala `Inexistente`, nivel 0 y porcentaje 0%; descripciones 20/24/28 1:N y escalas 21/25/29 combinadas.

## Campos calculados

Se mantienen como `CALCULATED_FIELD`; no se implementó el motor completo. La suma de paridad reconcilia las 2,124 posiciones:

| Resultado | Posiciones |
|---|---:|
| `CALCULATION_PARITY_MATCH` | 312 |
| `CALCULATION_PARITY_DIFFERENT` | 98 |
| `CALCULATION_PARITY_MISSING_IN_DB` | 496 |
| `CALCULATION_PARITY_NOT_EVALUABLE` | 1,218 |
| **Total** | **2,124** |

Las posiciones sin resultado de fórmula en caché permanecen como `NO_CACHED_FORMULA_RESULT`/`NOT_EVALUABLE`, no como error de fórmula.

## Caso de control ROP-CUMP-59

| Campo | Etiqueta | Excel | Producción | Clasificación | Motivo | Acción |
|---:|---|---|---|---|---|---|
| 03 | Área | Sección de Cumplimiento | Sección de Cumplimiento | `MATCH` | `EXACT_RAW_MATCH` | `NO_ACTION` |
| 04 | Área Consolidada | Sección de Cumplimiento | Sección de Cumplimiento | `MATCH` | `EXACT_RAW_MATCH` | `NO_ACTION` |
| 05 | Tipo de Riesgo | Operativo | — | `MISSING_IN_DB` | `DB_NULL` | `IMPORT_BASELINE` |
| 06 | Procedimiento | Atención a requerimientos de Información por parte del Ente Regulador | — | `MISSING_IN_DB` | `DB_NULL` | `IMPORT_BASELINE` |
| 07 | Objetivo(s) Estratégico(s) | Suficiencia y sostenibilidad del Régimen IVM; entrega oportuna de beneficios | — | `MISSING_IN_DB` | `DB_NULL` | `IMPORT_BASELINE` |
| 15 | Régimen afectado | IVM | — | `MISSING_IN_DB` | `DB_NULL` | `IMPORT_BASELINE` |
| 16 | Transversalidad o Interrelación | Riesgo de Cumplimiento | — | `MISSING_IN_DB` | `DB_NULL` | `IMPORT_BASELINE` |
| 20 | Descripción de Control Preventivo | Control preventivo registrado en Excel | — | `MISSING_IN_DB` | `CHILD_COLLECTION_EMPTY` | `IMPORT_BASELINE` |
| 21 | Escala de efectividad preventiva | Alta Efectividad | 90 | `MATCH` | `EFFECTIVENESS_SCALE_SEMANTIC_MATCH` | `NO_ACTION` |
| 24 | Descripción de Control Detectivo | No hay | No hay | `MATCH` | `NO_CONTROLS_CANONICAL_MATCH` | `NO_ACTION` |
| 25 | Escala de efectividad detectiva | Inexistente | 0 | `MATCH` | `EFFECTIVENESS_SCALE_SEMANTIC_MATCH` | `NO_ACTION` |
| 28 | Descripción de Control Correctivo | No hay | No hay | `MATCH` | `NO_CONTROLS_CANONICAL_MATCH` | `NO_ACTION` |
| 29 | Escala de efectividad correctiva | Inexistente | 0 | `MATCH` | `EFFECTIVENESS_SCALE_SEMANTIC_MATCH` | `NO_ACTION` |
| 32 | Nivel de Automatización de los Controles | Manual | — | `MISSING_IN_DB` | `DB_NULL` | `IMPORT_BASELINE` |
| 39 | Respuesta al riesgo | Aceptar | ACEPTAR | `MATCH` | `NORMALIZED_SEMANTIC_MATCH` | `NO_ACTION` |
| 70 | Señales de Alerta | — | — | `LEGITIMATELY_BLANK_IN_EXCEL` | `NO_ALERTS_BASELINE_OR_DB` | `NO_ACTION` |

## Artefactos de auditoría

Los archivos completos contienen datos operativos y permanecen únicamente en `%TEMP%`; no se incorporan a Git. SHA-256 y filas de datos del RUN_ID `20261002_142828_041`:

| Archivo | Filas | SHA-256 |
|---|---:|---|
| `audit_59x82_full.json` | 4,838 | `cc12fa8eb9199cf2843d45c53c3c96ee2a54ecb9f3fd7aa3981f293a65f4ad60` |
| `audit_59x82_full.csv` | 4,838 | `380f6f742f5fd063ac4058fe92abf9829a52190ead7ba5884afd9e4aada5e29a` |
| `summary_by_risk.csv` | 59 | `7f058c0b2b7084558960629ab205c965ea33e838512b03cffc1a4b79ec9c3701` |
| `summary_by_field.csv` | 82 | `902b2756cfca3131ed412e713857f756c5c468c197adfff421969433156cdee5` |
| `summary_by_block.csv` | 6 | `455c8bb5fae1b8c6727fd91f867533440d1871148745ebdad93c2d8af0ee2847` |
| `calculated_parity.csv` | 2,124 | `342ba79846aedce323c7fe333e37dafd22e658eb1d54dd4eae4dffa957ab88d0` |
| `operational_values_to_preserve.csv` | 4 | `04e003688e770450ff3f12a1c9e6c3c273d025480160404c868e08de5550d2aa` |
| `baseline_import_candidates.csv` | 398 | `06772eb693f3ab4fe7564e04c7c14de2fb5ca137089c95b57ee3e338d8313cb6` |
| `data_conflicts.csv` | 87 | `b28eba977ab9e7fa83d8a23d2e831ee13f6346e240b23690b1e93b2177360f4f` |
| `technical_defects.csv` | 0 | `60d47d0b38e8beb9e6ab2666a08a5a897fa2c67b68a68ba5bd1666be70f8f5d1` |
| `production_risk_inventory.csv` | 59 | `94b8ea6e564faf3921d3952fb4ed5d58cdfc7d4ea0338c134b8a5f1b5d8cdedc` |
| `field70_operational_authority.csv` | 24 | `4ff24b1da25c5e2842e801fc4eb302a698b56aff8cf78599682e8494fd3b5a28` |
| `field39_catalog_conflicts.csv` | 4 | `83af4619c577b1f679c6379e4729434ad8ad25e1050b37eb6f930311a71f4db8` |
| `field03_reaudit.csv` | 59 | `afb7e2dfa0c2b6d34561b26c29c21eba1950254d85974627a652b9f8b4e507b9` |
| `classification_action_crosstab.csv` | 10 | `c3d0868e42468b9563d1edee49cfab06374057bc29db067cd7e9bb03df35803e` |

## Verificación, alcance y cierre

- `node tools/validate_contract_82_fields.js`: PASS, 82/82.
- `dotnet test ... --filter FullyQualifiedName~MatrizRiesgosContract82FieldsTests`: PASS, 17/17.
- `dotnet test ... --filter FullyQualifiedName~MatrizRiesgosBlock2ForensicAuditTests`: PASS, 15/15.
- `dotnet build tools/AuditMatricesExcelVsProduction/AuditMatricesExcelVsProduction.csproj`: PASS; 0 errores, 14 advertencias de analizadores.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_documentation_links.ps1`: PASS (173 Markdown / 199 enlaces); `git diff --check`: PASS.
- `PRODUCTION_DML_EXECUTED=0`, `PRODUCTION_DDL_EXECUTED=0`, `PRODUCTION_PROCEDURES_EXECUTED=0`, `PRODUCTION_DATA_MUTATION=0`, `DATABASE_WRITES=0`.
- `tools/export_excel_matrix_82.js` sigue siendo `AUDIT_SUPPORT_ONLY`; la aserción agregada valida la fuente de F03. `EXPORT_RUNTIME_BEHAVIOR_CHANGED=NO`.

La auditoría queda técnicamente reconciliada: etiquetas/universo completos, todo faltante o diferencia tiene clasificación, causa y acción, sin defectos técnicos de mapping ni diferencias inexplicadas. Los 87 conflictos de datos y 398 valores baseline quedan cuantificados para sus acciones correspondientes; los cuatro conflictos de catálogo esperan Bloque 3. **No iniciar Bloque 3.**