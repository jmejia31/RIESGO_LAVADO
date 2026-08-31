# Fase 3.1.1 — Arquitectura definitiva y modelo de persistencia

## 1. Certificación de alcance

- **Fecha/hora local:** 2026-08-31 (UTC-6).
- **BASE_SHA / FINAL_HEAD previsto:** `1adca71caa4b4581df731d1c7a6d2d4cfdd2e183` antes del commit documental.
- **Rama:** `desarrollo`; `main` no se modifica.
- **Objetivo:** cerrar la arquitectura y el modelo de persistencia de la plataforma administrable de cálculo, sin implementar backend, frontend, DDL, DML, seeds, migraciones ni las 34 fórmulas.
- **Evidencia de esta intervención:** Git y CodexGraph ejecutados; Oracle consultado por SQL*Plus mediante `System.Diagnostics.ProcessStartInfo`, `-L /nolog`, entrada UTF-8 sin BOM y consultas SELECT/USER_* únicamente.
- **Evidencia heredada, no reproducida:** conteos completos de regresión de fases anteriores. Esta intervención no modifica código productivo y solo ejecuta `git diff --check` y validaciones documentales focalizadas.

## 2. Scope y no alcance

Esta subfase congela las decisiones para 3.1.2. No se reabre Fase 1, 2 o 3; no se cambia el Formula Engine, Publication Gate, Form Builder, contratos REST, RBAC, Oracle ni históricos. La implementación de administración pertenece a 3.1.2/3.1.3; integración visual a 3.1.4; carga y certificación de las 34 fórmulas a 3.1.5.

## 3. Mapa CodexGraph

Consultas realizadas primero: `MatricesRiesgos`, `FormulaEngine`, `FormularioValidador`, `ReglasCalculo`, `Catalogos`, `Publication`, `FormBuilder`, `Auditoria`.

El grafo encuentra principalmente TypeScript; no contiene nodos C# completos para los símbolos backend solicitados. Por ello el mapa backend se completó con inspección dirigida de los archivos compilables sugeridos.

### Archivos principales confirmados

- `backend/RL.API/Features/MatricesRiesgos/Domain/FormulaEngine.cs`
- `backend/RL.API/Features/MatricesRiesgos/Domain/FormularioValidador.cs`
- `backend/RL.API/Features/MatricesRiesgos/Application/MatricesRiesgosAppService.cs`
- `backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs`
- `backend/RL.API/Features/MatricesRiesgos/Persistence/FamiliasFormularioLifecycleRepository.cs`
- `backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosFormulariosController.cs`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/utils/dynamic-formula-evaluator.util.ts`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/utils/form-builder-validator.util.ts`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/data-access/matrices-riesgos.service.ts`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/components/form-builder/form-builder.component.ts`
- `frontend/rl-app/src/app/features/admin/matrices-riesgos/models/matrices-riesgos.models.ts`
- `backend/RL.API/Features/Auditoria/Persistence/AuditoriaRepository.cs`

### Mapa resumido real

```text
FormBuilder / modelos JSON
        -> familia de formulario
        -> RL_MR_VERSIONES_FORMULARIO (DRAFT/APPROVED/PUBLISHED, VER_JSON, VER_HASH)
        -> Validador + Publication Gate existente
        -> evaluación fija EVA_VERSION_ID
        -> FormulaEngine único (lexer -> parser -> AST/CallNode -> evaluator)
        -> reglas versionadas por código/versión y catálogos actuales
        -> EVA_CALCULOS_JSON + proyección tipada
        -> RL_AUDITORIA única
```

El frontend tiene un evaluator paralelo de UX con la misma familia de límites y funciones; no se considera una segunda fuente autoritativa. El cálculo autoritativo actual está en backend.

## 4. Modelo actual confirmado

### Form Builder, definición y versiones

`RL_MR_FAMILIAS_FORMULARIO` contiene identidad estable de familia: `FAM_ID`, código único, nombre, descripción, activo y fecha. `RL_MR_VERSIONES_FORMULARIO` contiene el número de versión por familia, estado de ciclo de vida, vigencia, usuario/fecha de creación, `VER_JSON` CLOB y `VER_HASH` SHA-256. La unicidad actual es `(VER_FAMILIA_ID, VER_VERSION)` y existe índice único funcional para una sola vigente por familia.

El builder edita el JSON de una versión draft; el servicio comprueba familia activa, estado permitido, valida la definición y delega publicación al repositorio. Una versión publicada no se elimina. La actualización de draft recalcula hash; publicación vuelve a calcularlo y activa la versión mediante el gate existente.

### Fórmulas y engine actuales

Las fórmulas actuales viven dentro de campos de `VER_JSON`, en propiedades `formula`, `calculo` o `referenciaCalculo` según compatibilidad. No existe una administración relacional central de fórmulas ni una tabla de usos.

`FormulaEngine.cs` es el único engine backend. Tiene lexer, parser, AST con `CallNode`, resolución de referencias, evaluación encadenada, caché de campos, detección de autorreferencia/ciclos y diagnósticos tipados. Funciones hardcodeadas confirmadas: `IF`, `IFERROR`, `ROUND`, `ROUNDDOWN`, `MAX`, `MOD`, `OR`. No están implementadas actualmente `AND`, `MIN` ni `LOOKUP/VLOOKUP`. El frontend UX refleja el mismo conjunto actual.

Límites confirmados: `MaxExpressionLength=4096`, `MaxTokens=512`, `MaxAstDepth=64`, `MaxOperations=2048`. No existen hoy `MaxFunctionDepth`, `MaxFunctionCalls` ni `MaxDependencyDepth`.

### Reglas

`RL_MR_REGLAS_CALCULO` es reutilizable y tiene una fila real. Su clave funcional es `(REG_CODIGO, REG_VERSION)`; guarda nombre, `REG_ALGORITMO_ID` y `REG_ACTIVA`. El JSON de formulario declara código/versión/algoritmo; el repositorio resuelve la combinación contra esta tabla y la incorpora a `EVA_CALCULOS_JSON`. Gap: no tiene un historial/lifecycle rico ni payload DSL; no se crea `V2`. La evolución futura será sobre la misma tabla, agregando solo columnas si el gate lo requiere y sin sobrescribir una versión publicada.

### Catálogos

`RL_MR_CATALOGOS` (`CAT_CODIGO` único) y `RL_MR_ELEMENTOS_CATALOGO` (`ELE_CATALOGO_ID`, código único por catálogo, valor, orden, activo) son la única fuente actual. Hay 4 catálogos y 18 elementos reales. No se crean catálogos paralelos ni tablas con forma de Excel.

La deuda histórica de referencias catalogales de VER_ID 27/28 no se corrige aquí. Para una versión futura publicada, el gate debe copiar al `VER_JSON` el snapshot mínimo de código/elementos del catálogo usado; `LOOKUP` resolverá contra ese snapshot contractual, no contra coordenadas Excel ni contra “la última versión” viva.

### Evaluaciones y resultados

`RL_MR_EVALUACIONES_RIESGO` tiene `EVA_VERSION_ID` FK a la versión de formulario, `EVA_DATOS_JSON`, `EVA_CALCULOS_JSON`, usuario/fecha, `EVA_VERSION_ROW` y activo. El backend valida respuestas, usa `FormulaEngine`, resuelve regla y persiste resultados calculados; las proyecciones mantienen valores tipados. En Oracle hay 14 evaluaciones, 3 versiones distintas referenciadas y `EVA_CALCULOS_JSON` presente en las 14.

### Publication Gate y auditoría

El gate actual está en el flujo de `MatricesRiesgosAppService`/repositorio: estado DRAFT/APPROVED, familia activa, validador de definición, hash y transición atómica de vigencia. No existe `PublicationGateV2`.

La auditoría es transversal: `IAuditoriaRepository`/`AuditoriaRepository` escribe en `RL_AUDITORIA` con tabla, registro, acción, datos anteriores/nuevos, usuario, email, IP, fecha y módulo. Matrices, familias y evaluaciones ya llegan a ese repositorio. No existe auditoría específica de fórmulas, funciones, parámetros ni otra auditoría de matrices.

## 5. Inventario Oracle read-only

### Resultado de conexión y consultas

- Conexión de desarrollo realizada con la cadena de `backend/RL.API/appsettings.json`, mantenida solo en memoria.
- Transporte: `ProcessStartInfo`, `FileName=C:\app\nahomi.martinez\product\11.2.0\client_1\sqlplus.exe`, `Arguments=-L /nolog`, shell deshabilitado, streams redirigidos, ventana inexistente, `UTF8Encoding(false)` por `StandardInput.BaseStream.Write`.
- `SQLPLUS_EXIT_CODE=0`.
- DDL ejecutado: `0`. DML ejecutado: `0`. Cambios de datos: `0`.

### Objetos y conteos confirmados

| Objeto | Tipo | Conteo Oracle | Clasificación | Decisión |
|---|---|---:|---|---|
| `RL_MR_FAMILIAS_FORMULARIO` | TABLE | 5 | BUSINESS_CONFIGURATION | KEEP/REUSE |
| `RL_MR_VERSIONES_FORMULARIO` | TABLE | 24 | HISTORY/SYSTEM | KEEP/REUSE |
| `RL_MR_CATALOGOS` | TABLE | 4 | MASTER_DATA | KEEP/REUSE |
| `RL_MR_ELEMENTOS_CATALOGO` | TABLE | 18 | MASTER_DATA | KEEP/REUSE |
| `RL_MR_REGLAS_CALCULO` | TABLE | 1 | BUSINESS_CONFIGURATION | KEEP/REUSE |
| `RL_MR_EVALUACIONES_RIESGO` | TABLE | 14 | TRANSACTIONAL | KEEP/REUSE |
| `RL_AUDITORIA` | TABLE | 1.290 | AUDIT | KEEP/REUSE |
| `SEQ_RL_MR_*` | SEQUENCE | 17 | SYSTEM | KEEP/REUSE |
| `SEQ_RL_AUDITORIA` | SEQUENCE | 1 | SYSTEM/AUDIT | KEEP/REUSE |

La consulta de `USER_OBJECTS` focalizada reportó las tablas/índices válidos; no hubo triggers en las tablas inspeccionadas. Constraints relevantes están `ENABLED`: PK/FK, unicidad de códigos, lifecycle de versión, hash de 64 hexadecimales, una vigente por familia, tipos booleanos y `EVA_VERSION_ROW>=1`. Los índices relevantes están `VALID`, incluidos `UQ_RL_MR_VER_FAM_VER`, `UX_RL_MR_VER_FAM_VIG`, FK de evaluación y catálogos.

Metadata de versiones: 24 filas, tres vigentes (`VER_ID` 24, 37, 44), 21 publicadas/draft restantes, longitudes JSON observadas sin imprimir CLOB completo. Metadata de evaluación: 14 filas, 3 `EVA_VERSION_ID` distintos y 14 cálculos presentes. No se imprimieron datos personales ni CLOBs completos.

## 6. Matriz de administrabilidad

| Dominio | Fuente actual | Persistencia actual | Administrable hoy | Versionado | Fuente futura | Reutilizar | Nuevo objeto | Gap | Decisión |
|---|---|---|---|---|---|---|---|---|---|
| FORMULAS | campos de `VER_JSON` | CLOB por versión | Parcial, vía Builder | indirecto por `VER_ID` | registro central + versión | `VER_JSON`, engine | fórmulas/versiones/usos | no identidad, hash ni uso FK | una fuente central |
| FUNCIONES | código en `FormulaEngine` | ninguna | No | No | registro + versión | engine/handlers | funciones/versiones/argumentos | no metadata, firma ni composición | una fuente central |
| PARAMETROS | literales/JSON y algoritmo | no tipada central | No | No | registro + versión | `VER_JSON` snapshot | parámetros/versiones | tipos y pinning | una fuente central |
| REGLAS | `RL_MR_REGLAS_CALCULO` | tabla relacional | Parcial | código+versión | misma tabla | Sí | Ninguno nuevo | lifecycle/metadata limitado | evolucionar misma tabla |
| CATALOGOS | `RL_MR_CATALOGOS` | tabla relacional | Sí, catálogo genérico | no propio; snapshot en VER_JSON | misma fuente + snapshot publicado | Sí | Ninguno nuevo | no versión física | no duplicar |
| ELEMENTOS CATALOGO | `RL_MR_ELEMENTOS_CATALOGO` | tabla relacional | Sí | no propio; snapshot publicado | misma fuente + snapshot | Sí | Ninguno nuevo | lookup histórico requiere snapshot | no duplicar |
| FORM VERSION | `RL_MR_VERSIONES_FORMULARIO` | CLOB + hash + lifecycle | Sí | Sí | misma tabla | Sí | Ninguno nuevo | debe incluir referencias pinneadas | conservar |
| VER_JSON | `VER_JSON` | CLOB contractual | Sí indirectamente | inmutable publicado | misma columna | Sí | Ninguno nuevo | evitar coordenadas Excel | conservar |
| EVALUACIONES | `RL_MR_EVALUACIONES_RIESGO` | CLOB datos/cálculos + FK versión | Operativa | `EVA_VERSION_ID` y row version | misma tabla | Sí | Ninguno nuevo | debe conservar contexto en JSON | conservar |
| AUDITORIA | `RL_AUDITORIA` | tabla transversal | Sí por operaciones existentes | temporal | misma tabla | Sí | Ninguno nuevo | admitir nuevas acciones | una sola |

Administraciones actuales: fórmulas `0` centralizadas (dispersas en JSON), funciones `0` (hardcode), parámetros `0` tipadas, reglas `1`, catálogos `1`. El target es `1` para cada dominio.

## 7. Matriz de objetos Oracle

| Objeto | Type | Purpose | KEEP | MODIFY_LATER | NEW | DROP | Rationale |
|---|---|---|---|---|---|---|---|
| `RL_MR_FAMILIAS_FORMULARIO` | TABLE | identidad de familias | Sí | No | No | No | base de formularios |
| `RL_MR_VERSIONES_FORMULARIO` | TABLE | draft/publicación/snapshot | Sí | integración JSON pinneada | No | No | histórico contractual |
| `RL_MR_REGLAS_CALCULO` | TABLE | reglas declaradas | Sí | lifecycle/metadata si gate lo exige | No | No | fuente existente viable |
| `RL_MR_CATALOGOS` | TABLE | catálogos | Sí | snapshot de publicación en VER_JSON | No | No | fuente única |
| `RL_MR_ELEMENTOS_CATALOGO` | TABLE | elementos | Sí | snapshot de publicación | No | No | fuente única |
| `RL_MR_EVALUACIONES_RIESGO` | TABLE | transaccional | Sí | No | No | No | `EVA_VERSION_ID` preserva historia |
| `RL_AUDITORIA` | TABLE | auditoría general | Sí | nuevas acciones | No | No | sistema único |
| `RL_MR_FORMULAS` | TABLE propuesta | identidad de fórmula | No | — | Sí | No | no existe equivalente |
| `RL_MR_FORMULA_VERSIONES` | TABLE propuesta | DSL/hash/lifecycle | No | — | Sí | No | no existe equivalente |
| `RL_MR_FORMULA_USOS` | TABLE propuesta | FK versión-formulario-campo | No | — | Sí | No | uso e impacto consultables |
| `RL_MR_FUNCIONES` | TABLE propuesta | registro function | No | — | Sí | No | no existe equivalente |
| `RL_MR_FUNCION_VERSIONES` | TABLE propuesta | native/composite version | No | — | Sí | No | pinning y lifecycle |
| `RL_MR_FUNCION_ARGUMENTOS` | TABLE propuesta | contrato posicional | No | — | Sí | No | integridad de firmas |
| `RL_MR_PARAMETROS_CALCULO` | TABLE propuesta | identidad de parámetro | No | — | Sí | No | no existe fuente tipada |
| `RL_MR_PARAMETRO_VERSIONES` | TABLE propuesta | valor tipado/versionado | No | — | Sí | No | reproducibilidad |
| `RL_MR_DEPENDENCIAS_CALCULO` | TABLE candidata | grafo materializado | No | — | No | No | derivable del AST/metadata; evitar sobre-normalización |
| `RL_MR_REGLAS_CALCULO_V2` | TABLE candidata | duplicado de reglas | No | — | No | No | prohibido; evolucionar existente |
| `RL_MR_CATALOGOS_FORMULAS` / similares | TABLE candidata | duplicados Excel | No | — | No | No | una fuente de catálogos |
| `RL_MR_AUDITORIA` | TABLE candidata | auditoría específica | No | — | No | No | auditoría única existente |

`DROP=0` para todos los objetos existentes.

## 8. Modelo lógico definitivo

```text
FORMULA 1--N FORMULA_VERSION 1--N FORMULA_USAGE N--1 FORM_VERSION
FUNCTION 1--N FUNCTION_VERSION 1--N FUNCTION_ARGUMENT
PARAMETER 1--N PARAMETER_VERSION
FORM_VERSION -> VER_JSON contractual -> usos, function pins, parameter pins, catalog snapshots, rule pins
FORM_VERSION 1--N EVALUATION; EVALUATION -> EVA_CALCULOS_JSON
CATALOG 1--N CATALOG_ITEM; catálogo vivo se snapshottea al publicar
RULE (código, versión) -> FORM_VERSION por referencia declarativa pinneada
todas las operaciones administrativas -> RL_AUDITORIA
```

### Fórmula y usos

`FORMULA` tiene identidad estable, código único, nombre, descripción, estado lógico y metadata funcional. `FORMULA_VERSION` tiene número, expresión DSL, tipo de resultado, estado, hash, vigencia, creador y fecha. `FORMULA_USAGE` contiene `FORM_VERSION_ID`, clave técnica del campo y `FORMULA_VERSION_ID`, con unicidad por versión/campo y FKs. El uso se crea al publicar y permite responder “dónde se usa” sin parsear todos los CLOBs; el JSON sigue siendo la definición contractual y el uso no sustituye el snapshot.

### Function Registry

`FUNCTION` contiene código, nombre, descripción, categoría, estado y metadata. `FUNCTION_VERSION` contiene versión, tipo `NATIVE|COMPOSITE`, tipo de resultado, firma/hash, documentación, `HANDLER_KEY` solo para nativas y DSL CLOB solo para compuestas. `FUNCTION_ARGUMENT` normaliza posición, código, nombre, tipo, requerido, default contractual opcional y descripción por versión.

Se elige combinación relacional + JSON: argumentos, posición, tipos, requerimiento y FKs son relacionales para integridad/consulta; metadata extensible y firma contractual pueden ir en JSON versionado/hash. No se guarda código ejecutable.

### Parámetros

`PARAMETER` tiene código estable, nombre, descripción, estado y tipo declarado. `PARAMETER_VERSION` tiene versión, tipo y valor en representación tipada; físicamente se recomiendan columnas separadas `VALOR_ENTERO`, `VALOR_DECIMAL`, `VALOR_BOOLEANO`, `VALOR_TEXTO`, `VALOR_FECHA`, con CHECK de exactamente un valor compatible con el tipo, en vez de `CLAVE/VALOR VARCHAR2`.

### Reglas y catálogos

Reglas reutilizan `(REG_CODIGO, REG_VERSION)` y `REG_ALGORITMO_ID`. Catálogos reutilizan encabezado/elementos existentes. No se versionan físicamente en esta subfase: una publicación captura catálogo y regla efectivos en `VER_JSON` (código, versión/algoritmo, elementos relevantes y hashes). Una sucesora puede usar nuevos datos sin alterar el registro publicado.

### Dependencias

`DEPENDENCY_TABLE_REQUIRED=NO`. Las referencias de fórmulas y llamadas de funciones se extraen del AST/metadata durante validación/publicación; el validador detecta ciclos con DFS y los usos permiten impacto. Una tabla materializada duplicaría información derivable, exigiría sincronización y no es necesaria para el volumen demostrado. Si una medición futura prueba que el impacto o la consulta excede el costo permitido, será una decisión posterior explícita, no una tabla anticipada.

## 9. Modelo físico Oracle propuesto (sin ejecutar DDL)

Las tablas nuevas son propuestas para 3.1.2/3.1.3. Los nombres son definitivos para diseño, pero no se crean en esta subfase.

### `RL_MR_FORMULAS` — NEEDED

| Column | Type | Null | Default | Semantics |
|---|---|---|---|---|
| `FOR_ID` | `NUMBER(15)` | N | sequence | identidad estable |
| `FOR_CODIGO` | `VARCHAR2(80)` | N | — | código único |
| `FOR_NOMBRE` | `VARCHAR2(150)` | N | — | nombre |
| `FOR_DESCRIPCION` | `VARCHAR2(1000)` | Y | NULL | descripción |
| `FOR_ESTADO` | `VARCHAR2(20)` | N | `'ACTIVE'` | ACTIVE/INACTIVE/RETIRED |
| `FOR_METADATA_JSON` | `CLOB` | Y | NULL | metadata no crítica |
| `FOR_FECHA_CREACION` | `DATE` | N | `SYSDATE` | fecha institucional |
| `FOR_USR_CREACION` | `NUMBER(10)` | N | — | FK `RL_USUARIOS` |
| `FOR_VERSION_ROW` | `NUMBER(10)` | N | `1` | concurrencia optimista |

PK `FOR_ID`; UQ `FOR_CODIGO`; CHECK de estado; índices por estado/código. `SEQ_RL_MR_FORMULAS` NOCACHE, como las existentes.

### `RL_MR_FORMULA_VERSIONES` — NEEDED

`FOV_ID NUMBER(15)` PK; `FOV_FORMULA_ID NUMBER(15)` FK; `FOV_VERSION NUMBER(5)`; `FOV_EXPRESION CLOB`; `FOV_TIPO_RESULTADO VARCHAR2(20)`; `FOV_ESTADO VARCHAR2(20)`; `FOV_HASH VARCHAR2(64)`; `FOV_FECHA_INICIO DATE`; `FOV_FECHA_FIN DATE`; `FOV_FECHA_CREACION DATE DEFAULT SYSDATE`; `FOV_USR_CREACION NUMBER(10)` FK; `FOV_VERSION_ROW NUMBER(10) DEFAULT 1`; `FOV_METADATA_JSON CLOB` opcional. Todo salvo fechas fin/metadata es NOT NULL. UQ `(FOV_FORMULA_ID,FOV_VERSION)`, CHECK hash hex 64, estado y fechas, índice por fórmula/estado. CLOB para DSL sin truncar expresiones.

### `RL_MR_FORMULA_USOS` — NEEDED

`FUS_ID NUMBER(15)` PK; `FUS_VERSION_FORMULARIO_ID NUMBER(15)` FK a `VER_ID`; `FUS_CAMPO_CLAVE VARCHAR2(150)`; `FUS_FORMULA_VERSION_ID NUMBER(15)` FK a `FOV_ID`; `FUS_FECHA_CREACION DATE DEFAULT SYSDATE`; `FUS_USR_CREACION NUMBER(10)`. UQ `(FUS_VERSION_FORMULARIO_ID,FUS_CAMPO_CLAVE)`; índices por formulario, fórmula y campo. Es persistencia de impacto, no otra fuente de expresión.

### `RL_MR_FUNCIONES` — NEEDED

`FUN_ID NUMBER(15)` PK; `FUN_CODIGO VARCHAR2(80)` UQ; `FUN_NOMBRE VARCHAR2(150)`; `FUN_DESCRIPCION VARCHAR2(1000)`; `FUN_CATEGORIA VARCHAR2(50)`; `FUN_ESTADO VARCHAR2(20)`; `FUN_METADATA_JSON CLOB`; `FUN_FECHA_CREACION DATE DEFAULT SYSDATE`; `FUN_USR_CREACION NUMBER(10)`; `FUN_VERSION_ROW NUMBER(10) DEFAULT 1`. CHECK de estado/categoría.

### `RL_MR_FUNCION_VERSIONES` — NEEDED

`FUV_ID NUMBER(15)` PK; `FUV_FUNCION_ID NUMBER(15)` FK; `FUV_VERSION NUMBER(5)`; `FUV_TIPO VARCHAR2(12)` CHECK `NATIVE|COMPOSITE`; `FUV_TIPO_RESULTADO VARCHAR2(20)`; `FUV_SIGNATURE_JSON CLOB`; `FUV_DEFINICION_DSL CLOB` nullable solo para COMPOSITE; `FUV_HANDLER_KEY VARCHAR2(80)` nullable solo para NATIVE; `FUV_DOCUMENTACION CLOB`; `FUV_ESTADO VARCHAR2(20)`; `FUV_HASH VARCHAR2(64)`; fechas/usuario/row version. UQ `(FUV_FUNCION_ID,FUV_VERSION)`. CHECKs impiden DSL en nativa, handler ausente en nativa y código ejecutable.

### `RL_MR_FUNCION_ARGUMENTOS` — NEEDED

`FUA_ID NUMBER(15)` PK; `FUA_FUNCION_VERSION_ID NUMBER(15)` FK; `FUA_POSICION NUMBER(3)`; `FUA_CODIGO VARCHAR2(80)`; `FUA_NOMBRE VARCHAR2(150)`; `FUA_TIPO VARCHAR2(20)`; `FUA_REQUERIDO NUMBER(1)`; `FUA_DEFAULT_JSON CLOB` opcional; `FUA_DESCRIPCION VARCHAR2(500)`. UQ `(FUA_FUNCION_VERSION_ID,FUA_POSICION)` y otra por código; CHECK de tipos, booleano y posición.

### `RL_MR_PARAMETROS_CALCULO` — NEEDED

`PAC_ID NUMBER(15)` PK; `PAC_CODIGO VARCHAR2(80)` UQ; `PAC_NOMBRE VARCHAR2(150)`; `PAC_DESCRIPCION VARCHAR2(1000)`; `PAC_TIPO VARCHAR2(20)`; `PAC_ESTADO VARCHAR2(20)`; metadata/creador/fecha/row version. Tipos permitidos: INTEGER, DECIMAL, BOOLEAN, TEXT, DATE.

### `RL_MR_PARAMETRO_VERSIONES` — NEEDED

`PAV_ID NUMBER(15)` PK; `PAV_PARAMETRO_ID NUMBER(15)` FK; `PAV_VERSION NUMBER(5)`; `PAV_TIPO VARCHAR2(20)`; `PAV_VALOR_ENTERO NUMBER(15)`; `PAV_VALOR_DECIMAL NUMBER(28,10)`; `PAV_VALOR_BOOLEANO NUMBER(1)`; `PAV_VALOR_TEXTO VARCHAR2(2000)`; `PAV_VALOR_FECHA DATE`; `PAV_ESTADO VARCHAR2(20)`; hash/fechas/usuario/row version. UQ `(PAV_PARAMETRO_ID,PAV_VERSION)`. CHECK de tipo y exactamente un valor. No se acepta solo `VALOR VARCHAR2`.

### Estrategias comunes

- **Identity:** sequences explícitas NOCACHE, siguiendo `SEQ_RL_MR_*`; no se usan identity columns sin decisión de compatibilidad Oracle 11g.
- **CLOB/VARCHAR2:** CLOB para DSL, JSON contractual y documentación larga; `VARCHAR2` para códigos, estados, hashes y nombres acotados.
- **Timestamp:** se conserva la convención Oracle actual `DATE DEFAULT SYSDATE`; no se introduce una segunda convención.
- **Hash:** SHA-256 UTF-8 en hexadecimal minúsculo, `VARCHAR2(64)`, CHECK equivalente al existente.
- **Estado:** strings acotados con CHECK y lifecycle explícito; no borrar versiones publicadas.
- **Concurrencia:** `*_VERSION_ROW` para drafts/identidades administrables; actualización con predicado de row version y `FOR UPDATE` solo dentro de la operación autorizada. Publication Gate mantiene el lock/atomicidad actual de familia.

## 10. Function Registry: nativas y compuestas

### Seed de diseño, no ejecutado

| Función | Estado actual en engine | Clasificación futura |
|---|---|---|
| IF | EXISTS_IN_ENGINE | REQUIRED_NATIVE (`IF_V1`) |
| IFERROR | EXISTS_IN_ENGINE | REQUIRED_NATIVE (`IFERROR_V1`) |
| AND | REQUIRED_NEW_NATIVE | handler seguro (`AND_V1`) |
| OR | EXISTS_IN_ENGINE | REQUIRED_NATIVE (`OR_V1`) |
| MIN | REQUIRED_NEW_NATIVE | handler seguro (`MIN_V1`) |
| MAX | EXISTS_IN_ENGINE | REQUIRED_NATIVE (`MAX_V1`) |
| ROUND | EXISTS_IN_ENGINE | REQUIRED_NATIVE (`ROUND_V1`) |
| ROUNDDOWN | EXISTS_IN_ENGINE | REQUIRED_NATIVE (`ROUNDDOWN_V1`) |
| MOD | EXISTS_IN_ENGINE | REQUIRED_NATIVE (`MOD_V1`) |
| LOOKUP | REQUIRED_NEW_NATIVE | handler seguro que consulta snapshot catalogal |

`CAN_BE_COMPOSITE` aplica a funciones de negocio como `RIESGO_BASE(frecuencia, impacto) = MAX(1, frecuencia + impacto - 1)`, no a primitivas con semántica propia. No se promete soporte operativo ni se cargan seeds en 3.1.1.

Las nativas son metadata + `HANDLER_KEY` allowlisted + handler compilado backend. Las compuestas son DSL seguro en CLOB versionado y pueden llamar solo funciones registradas/pinneadas, parámetros y catálogos permitidos. No se almacena C#, JavaScript, SQL ni reflexión ejecutable.

## 11. Version pinning y snapshot

Al publicar una versión de formulario, el gate debe validar y persistir en `VER_JSON` las referencias exactas:

```text
campo -> FORMULA_VERSION_ID/código+versión/hash
función -> FUNCTION_VERSION_ID/código+versión/hash
parámetro -> PARAMETER_VERSION_ID/código+versión/hash/valor tipado
regla -> REG_CODIGO + REG_VERSION + REG_ALGORITMO_ID
catálogo -> código + snapshot de elementos + hash del snapshot
```

`VER_JSON` sigue siendo el snapshot contractual inmutable. `FORMULA_USAGE` aporta navegación/impacto; no sustituye el pin. Evaluaciones solo apuntan a `EVA_VERSION_ID`, por lo que el contexto de cálculo queda fijado por esa versión. Nunca se resuelve “última versión” para una evaluación histórica.

## 12. Publication Gate único

Se amplía el gate actual en 3.1.3, sin crear otro, para comprobar: existencia/estado de fórmulas y versiones, firmas/argumentos, handler nativo disponible, DSL compuesto seguro, parámetros tipados, catálogo y snapshot, reglas, referencias, tipos, ciclos de funciones y fórmulas, límites de complejidad, hashes, vigencia y unicidad. La publicación debe ser atómica con la activación de versión y la auditoría única.

## 13. Auditoría única

`AUDIT_SYSTEMS=1`, `NEW_AUDIT_SYSTEMS=0`. Las operaciones futuras se registran en `RL_AUDITORIA` usando el repositorio existente y sus acciones permitidas; por ejemplo `CREATE_FORMULA`, `VERSION_FORMULA`, `PUBLISH_FORMULA`, `CREATE_FUNCTION`, `VERSION_FUNCTION`, `PUBLISH_FUNCTION`, `DEPRECATE_FUNCTION`, `CREATE_PARAMETER`, `VERSION_PARAMETER`, y equivalentes para usos/snapshots. Si el CHECK actual de `AUD_ACCION` requiere ampliación, se modifica la misma constraint en fase posterior; no se crea otra tabla.

## 14. Seguridad y límites

Hard gates: `DB_EXECUTABLE_CODE=0`, `DYNAMIC_CSHARP_FROM_DB=0`, `DYNAMIC_JAVASCRIPT_FROM_DB=0`, `DYNAMIC_SQL_FROM_FORMULA=0`, `eval=0`, `new Function=0`, reflexión arbitraria=0. El único código dinámico admisible es DSL parseado a AST allowlisted.

Los límites existentes se conservan. Se propone agregar configuración/validación futura de `MaxFunctionDepth`, `MaxFunctionCalls` y `MaxDependencyDepth` porque las funciones compuestas introducen llamadas anidadas; sus valores se decidirán con pruebas de 3.1.3, no se inventan aquí. Ciclos `FUNCTION_CYCLE` y `FORMULA_CYCLE` se detectan antes de publicar mediante grafo temporal DFS del AST/metadata.

## 15. Migración conceptual idempotente (no ejecutada)

1. **PRECHECK:** comprobar objetos, constraints, secuencias, hashes, duplicados, publicados y conteos; abortar ante anomalía.
2. **CREATE STRUCTURES:** crear únicamente tablas nuevas aprobadas y sus índices/FKs; sin tocar tablas históricas.
3. **SEED NATIVE FUNCTIONS:** insertar de forma idempotente solo metadata/handlers allowlisted; nunca código.
4. **IMPORT EXISTING ADMINISTRABLE FORMULAS:** extraer fórmulas de versiones seleccionadas, normalizar alias/expresión y deduplicar por código/hash; no tocar JSON.
5. **CREATE VERSION METADATA:** crear registros de fórmula/versiones y usos para configuraciones futuras; mantener la referencia original y hash de cada `VER_JSON`.
6. **VALIDATE:** ejecutar validador, ciclos, referencias, tipos, snapshots, conteos y hashes.
7. **POSTCHECK:** demostrar `VER_JSON`, `VER_HASH`, `EVA_VERSION_ID` y `EVA_CALCULOS_JSON` históricos sin cambios.

Cada paso deberá ser reejecutable por claves naturales/hash y transaccional por lote. La migración no reescribe publicados ni resultados.

## 16. Impacto Excel y 34 fórmulas

`MODEL_SUPPORTS_34_FORMULAS=YES` como capacidad arquitectónica, no como certificación de paridad. El diseño cubre IF/IFERROR/AND/OR/MIN/MAX/ROUND/ROUNDDOWN/MOD/LOOKUP, referencias de campos, campos auxiliares y dependencias encadenadas, blanks, decimales y redondeos. La traducción es semántica: `Matriz_Riesgos[[#This Row],[Frecuencia]]` -> `frecuencia`; `t_nivel_riesgo[]` -> `CAT_NIVEL_RIESGO` snapshot; `'Otras Tablas'!$B$3` -> parámetro versionado. No se guardan coordenadas Excel como fuente permanente. `EXCEL_PARITY=NO` en esta fase; corresponde a 3.1.5.

## 17. Histórico y casos certificados

- `PUBLISHED_VER_JSON_CHANGED=0` por diseño: la publicación futura genera otra `VER_ID`.
- `PUBLISHED_VER_HASH_CHANGED=0`: el hash se recalcula solo para drafts/nuevas versiones.
- `HISTORICAL_EVA_VERSION_ID_CHANGED=0`: la FK no se actualiza.
- `HISTORICAL_EVA_CALCULATIONS_REWRITTEN=0`: no se recalculan ni reemplazan `EVA_CALCULOS_JSON` históricos.
- `VER_ID_24_IMPACT=NONE`; `VER_ID_53_IMPACT=NONE`: permanecen intactos y siguen resolviendo por su JSON/hashes actuales.
- `VER_ID_27_28_HISTORICAL_MUTATION=0`: la deuda catalogal queda para una versión sucesora; no se modifican sus snapshots.

## 18. Frontend, backend y RBAC posteriores

No se editan componentes Angular ni endpoints en 3.1.1. 3.1.2 implementará contratos conceptuales `LIST`, `GET`, `CREATE`, `CREATE VERSION`, `UPDATE DRAFT`, `VALIDATE`, `TEST`, `PUBLISH`, `DEPRECATE`, `VERSIONS` y `USAGES`, sobre la persistencia definida y el engine/gate únicos. 3.1.4 integrará la configuración al Builder mediante modelos/servicios existentes. RBAC granular queda fuera: `RBAC_GRANULAR_IMPLEMENTATION=0`, `RBAC_CHANGES=0`; las operaciones futuras usarán autorización existente hasta una fase autorizada.

## 19. ADR

- **ADR-01:** una sola administración de Fórmulas; no `*_EXCEL`, `*_LEGACY` ni `*_ESPECIALES`.
- **ADR-02:** una sola administración de Funciones.
- **ADR-03:** nativas = metadata BD + handler seguro compilado.
- **ADR-04:** compuestas = DSL seguro versionado en BD.
- **ADR-05:** una sola administración de Parámetros, tipada y versionada.
- **ADR-06:** reutilizar `RL_MR_REGLAS_CALCULO`; no `V2`.
- **ADR-07:** reutilizar catálogos existentes; snapshot al publicar.
- **ADR-08:** `VER_JSON` permanece snapshot histórico contractual.
- **ADR-09:** un solo Formula Engine.
- **ADR-10:** un solo Publication Gate.
- **ADR-11:** una sola auditoría general.
- **ADR-12:** cero código ejecutable almacenado en BD.
- **ADR-13:** version pinning obligatorio.
- **ADR-14:** histórico inmutable; toda mejora crea sucesores.
- **ADR-15:** no persistir grafo de dependencias mientras AST/metadata derivables cubran impacto y ciclos.

## 20. Hard gates y estado final

| Gate | Resultado |
|---|---|
| `CURRENT_MODEL_MAPPED` | PASS |
| `CODEXGRAPH_MAP` | PASS |
| `ORACLE_MODEL_CONFIRMED` | PASS |
| `ADMINISTRABILITY_MATRIX` | PASS |
| `ADMINISTRABILITY_GAPS_IDENTIFIED` | PASS |
| `DUPLICATE_STRUCTURES_PROPOSED` | 0 |
| `FORMULA_ADMIN_SOURCES_TARGET` | 1 |
| `FUNCTION_ADMIN_SOURCES_TARGET` | 1 |
| `PARAMETER_ADMIN_SOURCES_TARGET` | 1 |
| `RULE_ADMIN_SOURCES_TARGET` | 1 |
| `CATALOG_ADMIN_SOURCES_TARGET` | 1 |
| `FORMULA_ENGINES_TARGET` | 1 |
| `PUBLICATION_GATES_TARGET` | 1 |
| `AUDIT_SYSTEMS_TARGET` | 1 |
| `NEW_AUDIT_SYSTEMS` | 0 |
| `DB_EXECUTABLE_CODE_TARGET` | 0 |
| `LOGICAL_MODEL` | PASS |
| `PHYSICAL_MODEL_PROPOSAL` | PASS |
| `VERSIONING_MODEL` | PASS |
| `FUNCTION_REGISTRY_MODEL` | PASS |
| `NATIVE_FUNCTION_MODEL` | PASS |
| `COMPOSITE_FUNCTION_MODEL` | PASS |
| `PARAMETER_MODEL` | PASS |
| `RULE_MODEL` | PASS |
| `CATALOG_MODEL` | PASS |
| `VER_JSON_SNAPSHOT_MODEL` | PASS |
| `FORMULA_USAGE_DECISION` | PASS — NEEDED |
| `DEPENDENCY_PERSISTENCE_DECISION` | PASS — NO |
| `HISTORICAL_INTEGRITY_DESIGN` | PASS |
| `MODEL_SUPPORTS_34_FORMULAS` | YES |
| `RBAC_CHANGES` | 0 |
| `PRODUCTIVE_CODE_CHANGES` | 0 |
| `DATABASE_MUTATIONS` | 0 |
| `DDL_CHANGES` | 0 |
| `DML_CHANGES` | 0 |
| `P0` | 0 |
| `P1_PROPIOS_3_1_1` | 0 |

**Estado documental:** `SUBFASE 3.1.1 = CERRADA`. Commit documental `851a3b59546a2d4e03b6295ab7c7cf9cf3694ae1` publicado en `origin/desarrollo`. `HEAD=origin/desarrollo`, ahead=0, behind=0, tracked worktree limpio y staged changes=0. Los cuatro untracked preexistentes están preservados fuera de scope y no son P0, P1, blocker, regression ni deuda de 3.1.1. `3.1.2` queda habilitada, sin implementarla en esta intervención.
