# Fase 2 — Motor dinámico de fórmulas

## Contrato técnico

Las fórmulas de `VER_JSON` usan un DSL propio. El flujo es lexer, parser de precedencia, AST, validación semántica, grafo de dependencias y evaluación determinista. No se ejecutan strings como código y no se usan `eval`, `new Function`, compilación dinámica, reflection ejecutable, SQL ni acceso a red o filesystem.

El subconjunto implementado soporta `+`, `-`, `*`, `/`, `^`, `=`, `<>`, `<`, `<=`, `>` y `>=`, paréntesis, números, referencias contractuales y `IF`, `IFERROR`, `ROUND`, `ROUNDDOWN`, `MAX`, `MOD` y `OR`. Los errores se clasifican mediante códigos estables `FORMULA_*`.

`ROUND` usa midpoint AwayFromZero; `ROUNDDOWN` trunca hacia cero; `MOD` sigue el signo del divisor mediante la identidad Excel `a - divisor * floor(a/divisor)`. `IF` es lazy e `IFERROR` solo captura errores de evaluación permitidos, nunca errores estructurales, referencias o ciclos.

## Autoridad y versionado

El backend valida y recalcula desde `EVA_DATOS_JSON` usando el `VER_JSON` exacto fijado por `EVA_VERSION_ID`. El JSON de cálculos recibido del cliente se ignora para obtener resultados oficiales. Las evaluaciones mantienen `EVA_VERSION_ID` y `EVA_VERSION_ROW`; las versiones publicadas y su hash son inmutables.

El Publication Gate reutiliza el validador semántico del motor antes de publicar. Las reglas continúan resolviéndose por `REG_CODIGO` + `REG_VERSION` en `RL_MR_REGLAS_CALCULO`; `REG_ALGORITMO_ID` solo selecciona algoritmos permitidos en backend.

Los vectores sin secretos están en [FASE_2_FORMULA_TEST_VECTORS.json](FASE_2_FORMULA_TEST_VECTORS.json). La implementación Angular conserva el mismo subconjunto para preview y validación de UX; el resultado persistido siempre lo determina el backend.

## Corrección de auditoría y evidencia de ejecución

El auditor ahora reutiliza `FormulaEngine`, consulta todas las versiones y termina con exit code no cero ante indisponibilidad Oracle. La ejecución reproducible de esta intervención produjo `ORACLE_SEMANTIC_AUDIT=EXTERNAL_BLOCKER`, `ERROR=50201`, sin excepción CLR no controlada ni falso PASS.

Se corrigieron los avisos CA1707 del nuevo código mediante una supresión local justificada sobre el enum de códigos contractuales; no se desactivaron Quality Gates globales. Backend integral: `504/504 PASS`; frontend: `705/705 PASS`; lint y build: PASS. E2E queda sujeto a la disponibilidad del servidor/browser configurado.

Evidencia posterior: E2E integral `29/29 PASS`. El único fallo previo del escenario JSON técnico se corrigió ajustando su fixture a una fórmula válida del DSL (`1 + 2`); no se relajó la validación.

Auditoría Oracle final ejecutada en solo lectura: `VERSIONS_INSPECTED=24`, `HASH_INVALID=0`, `FORMULA_REFERENCE_UNKNOWN=0`, `FORMULA_OPERATOR_UNSUPPORTED=0`, `FORMULA_FUNCTION_UNSUPPORTED=0`; VER_ID 24 y 53 quedaron `CLASS=VALID` sin modificar su JSON/hash. Postflight: todos los invariantes consultados en cero (`INVALID_OBJECTS=0`, `DISABLED_CONSTRAINTS=0`, `TEMPORAL_OVERLAPS=0`, `BAD_VERSION_ROW=0`). VER_ID 27/28 conservan deuda catalogal histórica no reconstruible determinísticamente.

## Alcance pendiente

La parametrización completa de proyecciones y la auditoría semántica Oracle de las 24 versiones requieren completar la integración del contrato de mappings y la ejecución institucional de postflight. Las versiones históricas 24, 27, 28 y 53 no deben modificarse; cualquier deuda de catálogo no reconstruible determinísticamente se mantiene para la fase de administración de catálogos.
