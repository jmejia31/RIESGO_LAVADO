# FASE 3.1.3 — Formula Engine, Function Registry y Publication Gate

## 1. Identificación y alcance

Esta documentación certifica la implementación de la Subfase 3.1.3 de la evolución del módulo Matrices de Riesgos.

- Fase: `3.1`.
- Subfase: `3.1.3`.
- Rama: `desarrollo`.
- Baseline original: `8813f539ca280aeb6bf6c81c10331f38f39e4107`.
- Commit técnico: `0688a6696640bfdd618880186755fc4e8dd60092`.

El alcance ejecutado comprende el Function Registry DB-driven, la resolución segura de funciones, las funciones compuestas mediante el DSL existente, `MIN`, `AND`, `LOOKUP` semántico, análisis de dependencias derivado, detección de ciclos, límites de ejecución, pinning reproducible y la extensión del único Publication Gate. No se inició la UI administrativa, la importación de las 34 fórmulas ni la paridad Excel de 3.1.4/3.1.5.

## 2. Arquitectura preservada

La implementación extiende el único `FormulaEngine`, el único `PublicationGate` y la auditoría existente dentro del bounded context de MatricesRiesgos.

| Control | Resultado |
|---|---:|
| `NEW_ENGINE` | 0 |
| `NEW_PUBLICATION_GATE` | 0 |
| `NEW_AUDIT_SYSTEM` | 0 |
| `NEW_CATALOG_SYSTEM` | 0 |
| `NEW_DEPENDENCY_TABLE` | 0 |
| `NEW_TABLES` | 0 |
| `PARALLEL_RULE_SYSTEM` | 0 |
| `RL_MR_CAMPOS_FORMULARIO_REINTRODUCED` | 0 |
| `RBAC_CHANGES` | 0 |

Se reutilizan las entidades administrativas de 3.1.2, `RL_MR_CATALOGOS`, `RL_MR_ELEMENTOS_CATALOGO`, las reglas existentes, la auditoría única y los contratos de versiones. Las dependencias se derivan en memoria del AST y no se persisten en una tabla paralela.

## 3. Function Registry DB-driven

El registry carga de los repositorios administrativos la identidad maestra, el estado activo, la versión exacta, el tipo, el hash, la aridad y los argumentos de cada `FunctionVersion`. Para runtime publicado exige una versión publicada y una referencia pinneada; no consulta la última versión activa de forma implícita.

La base de datos define metadata, lifecycle y contrato. Los algoritmos NATIVOS permanecen en handlers compilados y registrados explícitamente mediante allowlist. Una `HANDLER_KEY` desconocida o arbitraria se rechaza fail-closed. No se usa reflexión arbitraria, carga dinámica de assemblies ni compilación de código.

Las diez funciones nativas/registrables mínimas son: `IF`, `IFERROR`, `ROUND`, `ROUNDDOWN`, `MAX`, `MOD`, `OR`, `MIN`, `AND` y `LOOKUP`.

## 4. Funciones compuestas

Las funciones `COMPOSITE` reutilizan el mismo lexer, parser, AST y evaluador del Formula Engine único. Su cuerpo se trata como DSL seguro; no se ejecuta como SQL, JavaScript, C# ni script.

El scope de una función compuesta contiene únicamente sus argumentos declarados, parámetros runtime autorizados y funciones registradas. No acepta `outerValues` arbitrarios ni acceso a filesystem, red, procesos, entorno o reflexión. La validación rechaza funciones desconocidas, argumentos desconocidos, contratos inválidos y referencias fuera de scope.

El grafo de dependencias se deriva del AST y detecta autorrecursión, ciclos indirectos y ciclos de cualquier profundidad permitida antes de evaluar o publicar.

## 5. MIN, AND y LOOKUP

- `MIN` tiene contrato tipado y aridad validada, con resultados deterministas para positivos, negativos, cero y decimales.
- `AND` exige contrato booleano explícito y conserva la semántica institucional del engine para combinaciones booleanas, blanks y valores numéricos.
- `LOOKUP` es semántico: recibe código de catálogo, valor de entrada y campo de resultado, y consulta repositorios parametrizados de catálogos y elementos activos. No interpreta hojas, rangos, coordenadas A1, índices de columna ni archivos Excel.

`LOOKUP` requiere catálogo válido y semántica pinneada cuando corresponde. Un catálogo inexistente, inactivo, sin coincidencia o con coincidencia ambigua produce rechazo fail-closed; nunca se escoge arbitrariamente entre duplicados.

## 6. Límites y protección de ejecución

Se conservan los límites del engine:

- `MaxExpressionLength=4096`.
- `MaxTokens=512`.
- `MaxAstDepth=64`.
- `MaxOperations=2048`.

El runtime incorpora límites explícitos de registry/composición:

- `MaxFunctionDepth=32`.
- `MaxFunctionCalls=256`.
- `MaxDependencyDepth=32`.

Las pruebas cubren el valor exacto en el límite y el primer valor por encima del límite, además de profundidad, llamadas, operaciones y ciclos.

## 7. Pinning y reproducibilidad

`PINNING_BY_LATEST_VERSION=0`.

Una configuración publicada no resuelve versiones por “latest”. La resolución reproducible conserva o exige referencias inmutables para `FormulaVersion`, `FunctionVersion`, `ParameterVersion` y la semántica/versionado de catálogo utilizada por `LOOKUP`.

El Publication Gate rechaza dependencias no pinneadas, hashes inválidos, versiones no publicadas o cambios de contrato incompatibles. Las versiones y snapshots históricos existentes no se modifican.

## 8. Publication Gate único

Se amplió el `PublicationGate` existente; no se creó un gate V2. No existe un endpoint genérico que permita publicar `FormulaVersion` o `FunctionVersion` sin validación semántica.

Antes de publicar se validan sintaxis, semántica, contratos, aridad, tipos, argumentos, handlers, funciones, parámetros, catálogos, dependencias, ciclos, límites, hashes y pinning. También se rechazan referencias desconocidas, funciones inactivas, lookup inválido o ambiguo, errores de tipo, profundidad/llamadas excedidas y cualquier dependencia no resuelta.

La publicación es atómica y fail-closed: una validación fallida no deja una publicación parcial.

## 9. Auditoría y seguridad

Las operaciones relevantes reutilizan `IAuditoriaRepository`, `AuditoriaRepository` y `RL_AUDITORIA`; `AUDIT_SYSTEMS=1` y `NEW_AUDIT_SYSTEMS=0`.

| Control | Resultado |
|---|---:|
| `DYNAMIC_CSHARP` | 0 |
| `DYNAMIC_JS` | 0 |
| `DYNAMIC_SQL` | 0 |
| `EVAL` | 0 |
| `NEW_FUNCTION` | 0 |
| `REFLECTION_DYNAMIC_EXECUTION` | 0 |
| `ASSEMBLY_DYNAMIC_LOAD` | 0 |
| `PROCESS_EXECUTION` | 0 |
| `DB_EXECUTABLE_CODE` | 0 |

Los handlers NATIVOS se resuelven por allowlist compilada. El DSL compuesto es dato validado por el parser común y no se convierte en código ejecutable.

## 10. Oracle e histórico

Esta subfase no requirió persistencia adicional: `ORACLE_EXECUTED=NO`, `NEW_TABLES=0`, `DDL=0`, `DML=0` y `RECOVERY=0`.

La ausencia de mutaciones históricas se certifica por diff y arquitectura de esta intervención, no por una nueva lectura Oracle: `VER_JSON`, `VER_HASH`, `EVA_VERSION_ID` y `EVA_CALCULOS_JSON` no fueron modificados; `VER_ID` 24, 27, 28 y 53 no presentan mutación en el diff.

## 11. Evidencia de pruebas

Resultados ejecutados y certificados:

- Focal de comportamiento: `54/54 PASS`.
- Focal de contratos: `7/7 PASS`.
- Bloque focal previo: `67/67 PASS`.
- `CalculationRuntimeTests`: `25/25 PASS`.
- Regresión backend: `577/577 PASS`, `FAIL=0`, `SKIPPED=0`, `ExitCode=0`.
- Regresión frontend: `707/707 PASS`.
- Lint: `PASS`.
- Build backend: `PASS`.
- Build frontend: `PASS`.
- E2E: `29/29 PASS`.
- Quality gates locales: `PASS`.

Cobertura observada, informativa y no presentada como un gate adicional: backend líneas `30.17%`, ramas `33.93%`; frontend statements `61.35%`, branches `55.55%`, functions `57.84%`, lines `61.54%`.

## 12. Git y estados de fase

El commit técnico contiene únicamente runtime, integración y pruebas de 3.1.3. La documentación de cierre se publica en el commit documental de esta misma intervención, sin persistir estados transitorios de CI ni SHA futuro.

Estado certificado:

- `FASE 3.1=EN PROGRESO`.
- `SUBFASE 3.1.1=CERRADA/CONGELADA`.
- `SUBFASE 3.1.2=CERRADA/RECERTIFICADA/FINAL`.
- `SUBFASE 3.1.3=CERRADA/CERTIFICADA`.
- `SUBFASE 3.1.4=HABILITADA/NO INICIADA`.
- `SUBFASE 3.1.5=NO INICIADA`.
- `P0=0`.
- `P1=0`.

El entorno conserva tres untracked preexistentes fuera de scope: `.vscode/`, `agosto_rest.txt` y el PDF de requisitos. `agosto_capturas/` no está presente; se clasifica como `PREEXISTING_ENVIRONMENT_DRIFT=1`, `INTERVENTION_ATTRIBUTION=NONE`, `BLOCKER=NO`.

## 13. Definition of Done y continuación

Los gates propios de 3.1.3 quedan cubiertos por la implementación, las pruebas y la regresión certificada: registry DB-driven, allowlist nativa, funciones compuestas, `MIN`, `AND`, `LOOKUP`, grafo/ciclos, límites, pinning, Publication Gate único, seguridad, histórico y preservación arquitectónica.

La siguiente continuación habilitada es 3.1.4 para UI administrativa e integración visual con Builder. Esta documentación no inicia 3.1.4 ni 3.1.5.
