# FASE 3.1.4 — Angular administrabilidad de cálculo e integración visual con Builder

## 1. Identificación y alcance

- Fase: 3.1, en progreso.
- Subfase: 3.1.4, cerrada y certificada por esta intervención.
- Baseline: `47e6782acac38627860a1aad7a9e90aa2ccd2eaf`.
- Commit técnico: `dc3f78eded6381fa62e5dbd6e6b8e0e55198449f`.
- Rama: `desarrollo`.
- Fecha: 2026-09-02 (UTC-6).

Esta subfase entrega la capa Angular administrativa de Configuración de Cálculo y la integración del Builder con fórmulas centrales versionadas. No incluye la carga de las 34 fórmulas institucionales, la paridad final contra Excel ni cambios visuales de 3.1.5.

## 2. Arquitectura preservada

Se mantuvo el bounded context de Matrices de Riesgos y sus fuentes únicas: un workspace visual para Fórmulas, Funciones, Parámetros, Reglas de Cálculo y Catálogos; un único Formula Engine, Function Registry, Publication Gate y sistema de auditoría; y la reutilización de `RL_MR_REGLAS_CALCULO`, `RL_MR_CATALOGOS`, `RL_MR_ELEMENTOS_CATALOGO` y snapshots históricos.

| Control | Resultado |
|---|---:|
| `NEW_ENGINE` | 0 |
| `NEW_PUBLICATION_GATE` | 0 |
| `NEW_AUDIT_SYSTEM` | 0 |
| `NEW_CATALOG_SYSTEM` | 0 |
| `NEW_RULE_SYSTEM` | 0 |
| `NEW_DEPENDENCY_TABLE` | 0 |
| `NEW_TABLES` | 0 |
| `RBAC_CHANGES` | 0 |
| `PARALLEL_RULE_SYSTEM` | 0 |
| `RL_MR_CAMPOS_FORMULARIO_REINTRODUCED` | 0 |

## 3. Workspace y acceso de datos

`ConfiguracionCalculoComponent` se integró dentro de `frontend/rl-app/src/app/features/admin/matrices-riesgos`, con pestañas de Fórmulas, Funciones, Parámetros, Reglas y Catálogos. El servicio `CalculoConfiguracionService` consume el backend existente en `/api/matrices-riesgos/configuracion-calculo` mediante contratos TypeScript tipados; no contiene reglas de negocio paralelas.

La UI aplica Angular standalone, `OnPush`, estados de carga, vacío y error, acciones confirmadas, mensajes sanitizados, responsive, labels accesibles, foco visible y controles deshabilitados durante mutaciones. Los conflictos de concurrencia se muestran como error de operación y no se reintentan ciegamente.

## 4. Fórmulas

La pestaña permite listar, buscar, filtrar por estado, consultar detalle, crear master con versión inicial, crear nuevas versiones, editar únicamente borradores, consultar historial, hash, estado, `VersionRow` y usos/impacto. Las versiones publicadas se muestran como solo lectura y no tienen editor mutable.

El Builder selecciona para nuevos borradores una fórmula central y una `FormulaVersion` exacta. Muestra código, nombre, versión, estado, tipo de resultado, hash y descripción; la expresión DSL queda en preview. No se duplica el editor central.

## 5. Funciones y parámetros

La pestaña de Funciones administra masters, versiones NATIVA/COMPUESTA, contrato, aridad, argumentos, DSL, `HandlerKey`, hash, estado y `VersionRow`. Los handlers NATIVOS visibles están restringidos a metadata de la allowlist segura; no se acepta código ni una clave libremente ejecutable. Las funciones COMPUESTAS se editan como DSL seguro y el backend valida el contrato.

La pestaña de Parámetros administra masters y versiones tipadas, incluyendo `INTEGER`, `DECIMAL`, `BOOLEAN`, `TEXT` y `DATE`. La edición está limitada a borradores y las versiones históricas no se sobrescriben.

## 6. Reglas y catálogos

Las vistas de Reglas de Cálculo y Catálogos reutilizan las fuentes existentes. No se creó un Rule Engine, catálogo, tabla Excel ni sistema administrativo paralelo. El catálogo central continúa siendo la fuente administrativa; los snapshots de `VER_JSON` publicado continúan siendo evidencia histórica inmutable.

## 7. Compatibilidad, usos y pinning

La serialización conserva `formula`, `calculo` y `referenciaCalculo` para compatibilidad histórica y elimina metadatos centrales al desvincular una selección. Los formularios publicados se abren en `READ_ONLY=1`, sin mutación de snapshots.

`PINNING_BY_LATEST_VERSION=0`: el Builder conserva la identidad exacta de `FormulaVersion` y no resuelve la última versión al ejecutar un formulario publicado. La sincronización de `RL_MR_FORMULA_USOS` se realiza solo para una versión de formulario editable.

La sustitución de usos usa el endpoint existente ampliado, bloquea la versión de formulario con `FOR UPDATE`, exige estado DRAFT, reemplaza el conjunto de usos de forma atómica, usa binds y audita las operaciones. No hay mutación de usos sobre formularios publicados.

## 8. Publication Gate y seguridad

Existe un único Publication Gate backend. Angular ofrece únicamente acciones compatibles con el lifecycle; no puede publicar directamente fórmulas ni funciones evitando el gate. El backend mantiene como autoridad la validación de sintaxis, semántica, contratos, tipos, funciones, handlers, parámetros, catálogos, dependencias, ciclos, límites, hashes y pinning.

La integración no ejecuta DSL, JavaScript, SQL ni código arbitrario en el navegador. No usa `eval`, `new Function`, `innerHTML` inseguro, reflexión dinámica, carga de assemblies, procesos, filesystem, red, SQL generado ni credenciales.

| Control | Resultado |
|---|---:|
| `FRONTEND_EVAL` | 0 |
| `FRONTEND_NEW_FUNCTION` | 0 |
| `FRONTEND_DYNAMIC_JS` | 0 |
| `FRONTEND_DYNAMIC_SQL` | 0 |
| `FRONTEND_DYNAMIC_CSHARP` | 0 |
| `DYNAMIC_CODE_EXECUTION` | 0 |

## 9. Oracle e histórico

No se ejecutó Oracle en esta subfase: `ORACLE_EXECUTED=NO`, `DDL=0`, `DML=0`, `RECOVERY=0`, `NEW_TABLES=0`. No se requirió modificación estructural ni seed institucional.

La ausencia de mutaciones históricas se certifica por diff y arquitectura de esta intervención, no por una nueva lectura Oracle: `VER_JSON`, `VER_HASH`, `EVA_VERSION_ID` y `EVA_CALCULOS_JSON` no fueron modificados.

## 10. Evidencia de pruebas

- Frontend focal: 8/8 PASS en los tres specs de Configuración de Cálculo y selector central.
- Backend focal: 46/46 PASS.
- Frontend completo: 716/716 PASS, FAIL=0.
- Backend completo: 578/578 PASS, FAIL=0, SKIPPED=0.
- Lint: PASS.
- Build Angular aislado: PASS.
- Build backend: PASS.
- E2E: 29/29 PASS.
- `npm ci --ignore-scripts`: PASS en instalación aislada.
- `npm audit`: 0 vulnerabilidades.
- Validación de scripts de base de datos: PASS.
- `git diff --check`: PASS.

La validación de estructura conserva hallazgos preexistentes fuera del alcance de esta intervención (`dynamic-form-layout` vacío y carpetas legacy); no se modificaron ni se clasifican como P0/P1 propios de 3.1.4.

## 11. Git y entorno

El commit técnico `dc3f78eded6381fa62e5dbd6e6b8e0e55198449f` contiene únicamente código, integración y pruebas de 3.1.4. La documentación de cierre se publica en un commit documental separado.

Se preservan tres untracked ambientales preexistentes: `.vscode/`, `agosto_rest.txt` y el PDF de requisitos. `agosto_capturas/` no está presente y se clasifica como `PREEXISTING_ENVIRONMENT_DRIFT=1`, `INTERVENTION_ATTRIBUTION=NONE`, `BLOCKER=NO`. No se crean, borran, mueven ni stagean esos elementos.

## 12. Estado y continuación

- `FASE_3.1=EN_PROGRESO`.
- `SUBFASE_3_1_1=CERRADA/CONGELADA`.
- `SUBFASE_3_1_2=CERRADA/RECERTIFICADA/FINAL`.
- `SUBFASE_3_1_3=CERRADA/CERTIFICADA`.
- `SUBFASE_3_1_4=CERRADA/CERTIFICADA`.
- `SUBFASE_3_1_5=HABILITADA/NO_INICIADA`.
- `P0=0`.
- `P1=0`.

La siguiente continuación habilitada es 3.1.5 para la carga institucional y paridad Excel de las 34 fórmulas. Esta intervención no inicia 3.1.5.
