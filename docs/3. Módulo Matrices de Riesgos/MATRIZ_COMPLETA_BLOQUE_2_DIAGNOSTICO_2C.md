# Diagnóstico forense 2C — Bloque 2

## Alcance y método

Diagnóstico de solo lectura en `desarrollo`, base `3c2e8594ed95c3470767f30f276840e2b0f53629`. No se modificó código productivo, formularios, Oracle ni datos. Se inspeccionó `Matrices de Riesgos.xlsx` como ZIP/OpenXML con `System.IO.Compression` y `System.Xml`; se resolvieron relaciones de hoja, celdas, estilos, validaciones, defined names, tablas y external links. La llamada abrió el archivo con `FileShare.ReadWrite` y acceso `Read`, porque Excel lo mantenía abierto.

## Resultado A — mapping de efectividad en el Excel

La relación de `Matriz Consolidada` es `rId2`, resuelta desde `xl/workbook.xml` y `xl/_rels/workbook.xml.rels` a `xl/worksheets/sheet2.xml`; la hoja está visible. El libro tiene 6 hojas: 5 visibles, 1 oculta (`Listas Automonitoreo`) y 0 `veryHidden`. La hoja oculta alimenta validaciones de monitoreo (rangos BS–CA), no la escala de efectividad.

No existen `definedName` en el libro. La matriz contiene la tabla estructurada `Matriz_Riesgos` (`A1:CD60`). La tabla de lookup es `t_efectividad`, en `Listas!A29:C35`, con columnas `Escala`, `Nivel`, `%`. Las fórmulas de nivel consultan la segunda columna de esa tabla y las de porcentaje la tercera; en algunas filas Excel conserva la referencia estructurada `t_efectividad[]` y en otras el rango equivalente `Listas!$A$30:$C$35`. No son fórmulas shared: los elementos `<f>` inspeccionados son fórmulas normales.

La tabla de lookup es la fuente primaria completa. Los cached values coinciden con ella. Las columnas de porcentaje W, AA y AE tienen `number format=0%`, por lo que valores fuente como `0.9` son ratios que Excel presenta como `90%`. No se convirtió por magnitud.

| SCALE | LEVEL | PERCENT_RAW | PERCENT_RUNTIME | SOURCE | SOURCE_REF |
|---|---:|---:|---:|---|---|
| Alta Efectividad | 5 | 0.9 | 0.9 | Tabla `t_efectividad` | `Listas!A29:C35`, fila 35 |
| Inefectivo | 1 | 0 | 0 | Tabla `t_efectividad` | `Listas!A29:C35`, fila 31 |
| Inexistente | 0 | 0 | 0 | Tabla `t_efectividad` | `Listas!A29:C35`, fila 30 |
| Moderado | 4 | 0.85 | 0.85 | Tabla `t_efectividad` | `Listas!A29:C35`, fila 34 |
| Parcialmente Efectivo | 3 | 0.5 | 0.5 | Tabla `t_efectividad` | `Listas!A29:C35`, fila 33 |
| Razonable | 2 | 0.3 | 0.3 | Tabla `t_efectividad` | `Listas!A29:C35`, fila 32 |

Las tres clases de control usan los mismos lookups: nivel en V/Z/AD con índice de columna 2 y porcentaje en W/AA/AE con índice 3. `CROSS_TYPE_CONFLICTS=0`. Las seis escalas de la tabla coinciden literalmente con las seis observadas en las celdas de escala del Excel (`SCALE_LIST_PARITY=PASS`).

**No hay reglas de validación tipo lista para U2:U60, Y2:Y60 ni AC2:AC60.** Las validaciones encontradas en esas columnas no apuntan a un catálogo de escala; las reglas tipo lista del libro aplican a otros campos/rangos. Por ello el Excel define el mapping por lookup pero no demuestra un dropdown restringido en U/Y/AC (`VALIDATION_SCALE_COUNT=0`; `LOOKUP_MAPPING_SCALE_COUNT=6`).

`EXTERNAL_WORKBOOK_DEPENDENCY=YES`: el paquete incluye dos `externalLink` con rutas históricas a `Matriz SC Revisada y Corregida2 (3).xlsx` y `Matriz en blanco V5 (UGR 02-2026).xlsx` (incluyendo alias de ruta de usuario/OneDrive y UNC en sus relationships). Las fórmulas de efectividad inspeccionadas apuntan a la tabla interna `t_efectividad`, no a esos libros externos; no se usó contenido externo para completar el mapping.

Por tanto, `EFFECTIVENESS_CANONICAL_MAPPING=PASS`. La limitación del inspector de celdas cacheadas ya no aplica al mapping; el mapping completo está en OpenXML y fue trazado a la tabla interna.

## Contraste con catálogos versionados del repositorio

- `03_seed_catalogos_iniciales.sql` declara el maestro `CAT_EFECTIVIDAD_CONTROL`, pero no contiene `upsert_elemento` para ese catálogo. No se encontró seed de elementos para él.
- No se encontró carga de datos de `CAT_EFECTIVIDAD_NIVEL`, `CAT_EFECTIVIDAD_PORCENTAJE` ni `CAT_EFECTIVIDAD_ESCALA` en scripts del repositorio. El mapping verificable está en el Excel, no en catálogos Oracle versionados.
- El V2 DRAFT actual no contiene los tres catálogos ni `escala_preventivo`, `escala_detectivo` o `escala_correctivo`. Sí contiene `pesoPreventivo=0.7`, `pesoDetectivo=0.15`, `pesoCorrectivo=0.15` en `reglas[].parametros`.
- La transición `25_carga_formula_institucional_315.sql` también siembra los parámetros versionados `PESO_PREVENTIVO=0.70`, `PESO_DETECTIVO=0.15`, `PESO_CORRECTIVO=0.15`; es una duplicidad semántica con los valores de `VER_JSON`. No se decide aquí cuál debe ser autoridad para una futura versión.

## Resultado B — camino real de cálculo

### Evaluación

`CrearEvaluacionAsync` y `ActualizarEvaluacionAsync` recuperan el `VerJson` de `dto.EvaVersionId` (actualización valida que coincida con el ID ya persistido) y llaman `ValidarYCalcularEvaluacionAsync`. Este valida las respuestas y llama `new FormulaEngine().Evaluate(definicionFormulario, dto.EvaDataJson)`, overload que usa `new(DefaultRegistry)`. No se pasan `FormulaRuntimeOptions`, `Lookup`, parámetros ni pinning. El JSON de respuesta es el contexto de valores.

`FormulaEngine.ReadFields` obtiene expresiones inline declaradas en campos de `VER_JSON`; no lee `InstitutionalFormulaDataset`, `RL_MR_FORMULA_VERSIONES` ni `RL_MR_FORMULA_USOS`. El V1 y V2 DRAFT inspeccionados no declaran fórmulas inline F03–F09. Si el motor no encuentra expresiones, el servicio ejecuta `LegacyCalculator` con `frecuencia_inherente`, `impacto_inherente`, `controles_preventivo/detectivo/correctivo` y campos residuales; serializa ese resultado a `EVA_CALCULOS_JSON` y lo entrega al repositorio/proyección.

`InstitutionalFormulaDataset` contiene definiciones F03–F09, pero `rg` no encontró usos productivos de la clase fuera de su propia declaración. El SQL 25 contiene `seed_formula` para versiones de fórmula, pero no inserta asociaciones `RL_MR_FORMULA_USOS` para V1/V2. La existencia de un seed/definición no conecta la fórmula al cálculo de evaluación.

### Factory, lookup, pinning y fórmula-uso

- `DbDrivenCalculationRuntimeFactory` está registrado con DI (`Program.cs`) pero no tiene llamada desde el camino productivo de evaluación (`CreatePublishedAsync` sin consumidores). Acepta `CalculationPinning` y `CatalogSnapshot` desde quien lo invoque; carga funciones y versiones de parámetros para formar `FormulaRuntimeOptions`, pero no construye el pinning ni obtiene snapshots desde `VerJson`.
- No hay construcción productiva de `CalculationPinning`, almacenamiento/recuperación por `EVA_VERSION_ID` ni constructor productivo de `CatalogSnapshot` desde metodología versionada. La clase modela los mapas de versiones/hash, pero esos mapas no están conectados a la evaluación.
- `CatalogCalculationLookup` se instancia dentro del factory, que no es invocado por evaluación. Uso productivo conectado al cálculo de evaluación: 0.
- `RL_MR_FORMULA_USOS` tiene rutas administrativas de escritura (crear/reemplazar, transaccionales y auditadas) y consulta por fórmula en `CalculoConfiguracionRepository`. No existe lectura desde `ValidarYCalcularEvaluacionAsync`. No se halló seed SQL de asociaciones FUS para V1 ni V2; el estado real de Oracle no se inspeccionó.
- `PublicationGate` implementa validaciones de pinning, pero no tiene usos productivos. `PublicarVersionFormularioAsync` usa `FormularioValidador.ValidarDefinicionPublicableAsync`, que valida estructura de campos, tipos, catálogo declarado en el JSON y sintaxis/referencias inline con FormulaEngine/default registry. No valida formula usages exactos/versiones, pinning de funciones o parámetros, snapshots/hash de catálogos, ni construye `CalculationPinning`.

### F09 y controles relacionales

F09 usa `control_preventivo`, `control_detectivo` y `control_correctivo`. No hay código que los derive de `RL_MR_CONTROLES_RIESGO`. El contexto de FormulaEngine es solamente `EVA_DATOS_JSON`; las claves históricas `controles_preventivo/detectivo/correctivo` son otras variables, porcentajes y solo entran al camino `LegacyCalculator`. El motor no lee controles relacionales (`F09_RELATIONAL_CONTROL_CONTEXT=ABSENT`).

`MatricesRiesgosMitigacionService.CrearControlAsync` y `ActualizarControlAsync` delegan al repositorio y devuelven resultado; el repositorio confirma cada mutación y su auditoría dentro de la transacción de control. Ninguno recalcula, invalida ni actualiza `EVA_CALCULOS_JSON` o la proyección. `CREATE_CONTROL_RECALCULATES_EVALUATION=NO`; `UPDATE_CONTROL_RECALCULATES_EVALUATION=NO`.

Una futura secuencia `COMMIT CONTROL → RECALC EVALUATION` en transacciones separadas puede dejar el control guardado si falla el recálculo, exponer un periodo con ETP anterior y producir desalineación entre controles, `EVA_CALCULOS_JSON` y proyección. `ATOMIC_RECALC_REQUIRED=YES` para preservar consistencia; este diagnóstico no diseña ni implementa el cambio.

## Mapa del flujo actual

```text
EVA_VERSION_ID → VER_JSON de esa versión                 CONNECTED
VER_JSON fields/formulas inline → FormulaEngine         CONNECTED
VER_JSON → formula usages/versiones RL_MR_FORMULA_USOS   MISSING
Formula versions → evaluación                           MISSING
FormulaEngine → DbDrivenCalculationRuntimeFactory       MISSING
Factory → pinning construido y ligado a esa versión      MISSING
Factory → snapshots de catálogo del mismo VER_JSON       MISSING
Runtime → Lookup/parameters/function versions pinneados  MISSING
FormulaEngine → EVA_CALCULOS_JSON                        CONNECTED (si hay fórmulas inline)
Sin fórmulas inline → LegacyCalculator                   CONNECTED
EVA_CALCULOS_JSON → persistencia/proyección              CONNECTED
RL_MR_CONTROLES_RIESGO → variables de presencia F09      MISSING
Mutación de control → invalidación/recalculo ETP           MISSING
```

Conclusión: el mapping institucional se resuelve completamente desde el Excel, pero aún no existe su snapshot DB/versionado y el cálculo productivo no conecta fórmulas F03–F09, catálogos, parámetros ni pinning al `EVA_VERSION_ID`. `BLOCK_2_STATUS=PARTIAL_BLOCKED`; `NEXT_ACTION=BLOCK_2_RUNTIME_REMEDIATION`. No iniciar Bloque 3.

## Límites

Diagnóstico local read-only. No se conectó Oracle ni se inspeccionó el contenido real de las tablas institucionales. No se modificaron V1/V2, fórmulas, datos históricos, código productivo o scripts SQL. No se ejecutó suite backend porque no hubo cambios de código. Los vínculos externos OpenXML se reportan como referencias del paquete; no se intentó acceder a esos archivos.
