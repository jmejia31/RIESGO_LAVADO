# Dictamen de Evaluación y Análisis de Hallazgos Bloqueantes — Pruebas Oracle (Fase 1.2)

- **Fecha**: 2026-08-04
- **Módulo**: Matrices de Riesgos
- **Veredicto**: **Paso 1: NO APROBADO**
- **Fase 1.2**: **ABIERTA**
- **Pruebas Oracle**: **NO EJECUTAR CONTRA ENTORNOS FÍSICOS**
- **Script 05**: **NO EJECUTAR**

---

## 1. Resumen Ejecutivo de la Evaluación

Se realizó la revisión del commit `6e77ee3` y de los intentos locales de adecuación. La evaluación concluyó que **los 14 hallazgos bloqueantes no fueron corregidos adecuadamente en la primera iteración**, por lo que el estado se mantiene como **NO APROBADO**.

```text
Pruebas de Integración Oracle (Fase 1.2):
- Hallazgos bloqueantes detectados: 14
- Corregidos completamente: 0
- Corregidos parcialmente: 3 (requieren rehacerse)
- Pendientes: 11
- Veredicto final: Paso 1 NO APROBADO
```

---

## 2. Detalle de Diagnóstico por Hallazgo

1. **Omisión Falsa de Pruebas**: El patrón `if (!await Validar...) return;` registraba 5 pruebas como "correctas" y 0 como "omitidas", falseando el reporte de CI.
2. **Atomicidad Incompleta con `RL_AUDITORIA`**: El uso de un stub no insertaba registros reales ni verificaba la persistencia transversal con la misma conexión/transacción.
3. **Nombre de Tabla Inválido**: Se utilizaba `RL_MR_APROBACIONES` en lugar del nombre físico definitivo `RL_MR_APROBACIONES_FORMULARIO`.
4. **Columnas Inexistentes**: Se consultaban identificadores no definidos en el DDL (`TRA_FORMULA_APLICADA`, `REG_ALGORITMO`).
5. **Fixtures sin Confirmación**: Los registros de preparación se insertaban desde conexiones secundarias sin `CommitAsync()`, invisibles para el repositorio.
6. **Limpieza Incompleta y Sin Confirmación**: `DisposeAsync` no realizaba `CommitAsync()`, no borraba auditorías transversales ni las 9 asociativas, y amortiguaba excepciones.
7. **Matriz de 9 Asociaciones No Codificada**: Solamente se contaba la existencia de tablas en el diccionario de Oracle mediante `ALL_TABLES`, sin ejercitar la lógica C#.
8. **Uso de IDs Institucionales Fijos**: Se utilizaban IDs como `1` o `99999`, generando riesgos de colisión y alteración de tablas maestras.
9. **Concurrencia Optimista Mal Evaluada**: Se evaluaba `Assert.False` en lugar de capturar la excepción `DBConcurrencyException`.
10. **Residuos de Evaluaciones Creadas**: `CrearEvaluacionAsync` devolvía un ID que quedaba fuera del inventario de borrado.
11. **Exposición de Infraestructura**: `Assert.Fail` incluía `OracleException.Message`, revelando datos de conexión.
12. **Aserciones Genéricas**: Se usaba `Assert.ThrowsAnyAsync<Exception>` sin restringir el tipo o código ORA específico.
13. **Declaración Prematura de Cierre**: Se reportó la Fase 1.3 como aprobada funcionalmente sin acta firmada por Javier Mejía.
14. **Corrupción de Bitácora**: Se reescribió masivamente `BITACORA_COLABORACION.md` por problemas de formato de codificación.

---

## 3. Acciones Aprobadas y Próximos Pasos

1. Mantener `RL_ORACLE_INTEGRATION_REQUIRED=false` y no ejecutar el Script 05 contra bases de datos Oracle.
2. Aplicar el [Plan de Subsanación](PLAN_SUBSANACION_PRUEBAS_ORACLE_FASE_1_2.md) en una nueva intervención de desarrollo.
3. Validar únicamente la suite unitaria backend ordinaria (`Category!=OracleIntegration`).
4. Reconstruir la bitácora conservando codificación UTF-8 sin BOM.
