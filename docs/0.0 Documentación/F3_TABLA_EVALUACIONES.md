# F3 — Tabla de Evaluaciones: Semántica de Datos y Renderizado Robusto

## Estado

**F3 — TABLA DE EVALUACIONES: 100% COMPLETADA Y VALIDADA FUNCIONALMENTE.**

> **Decisión de gobierno del 2026-08-18:** Sonar/SonarCloud deja de ser un criterio bloqueante para el avance entre fases del Plan de Implementación. Su resolución y certificación global se difieren al cierre final del Plan. Durante las fases intermedias la prioridad obligatoria es programación correcta, funcionalidad completa, contratos, pruebas automatizadas, build, QA funcional, integración y ausencia de regresiones. Esta decisión no autoriza bajar thresholds, ocultar issues ni debilitar Sonar; únicamente difiere su saneamiento consolidado al cierre global.

- Fecha: 2026-08-18 (UTC-6).
- Rama autorizada: `desarrollo`.
- Baseline inicial F3: `f7992250ee1beed1d2a35a0f7e140b2bf97a7471`.
- Commit de implementación F3.2: `0d28205a21cfd46f86a43c04ac72477fd0e70775`.
- Commit de certificación funcional F3: `7f1fbac8e05a6af300625662fedcf57616751440` (`test(matrices): certificar cierre estricto F3`).
- Commit documental previo: `40ec4c52032ff54f9644ea4c6a6dd60c2a1fbc4c` (`docs(matrices): cerrar evidencia definitiva F3`).
- PR: #20, permanece **Draft / No merged**.
- `main`: fuera de alcance.
- Backend: 0 modificaciones C# durante el cierre F3 (406/406 PASS).
- Oracle / SQL: 0 modificaciones DDL/DML durante el cierre F3.

## Certificación funcional de F3

1. **Trazabilidad Contractual Completa**:
   - DB `RL_MR_EVALUACIONES_RIESGO` → `EvaluacionesRiesgoRepository` → Service `MatricesRiesgosService.cs` → DTO `EvaluacionRiesgoResumenDto.cs` & `EvaluacionesPaginadasDto.cs` → Controller `GET /api/matrices-riesgos/evaluaciones` → Angular `MatricesRiesgosService.ts` → TS Model `EvaluacionRiesgoResumenDto` → Component `MatricesRiesgosComponent.ts` → Template HTML `#panel-evaluaciones`.
   - Campos 100% mapeados: `evaId`, `evaRiesgoId`, `riesgoCodigo`, `riesgoNombre`, `evaVersionId`, `versionCodigo`, `versionNumero`, `estado`, `vri`, `vrr`, `nivelResidual`, `fechaEval`. Paginación: `items`, `pagina`, `registrosPorPagina`, `totalRegistros`, `totalPaginas`.
2. **Fecha / Timezone**:
   - `fechaEval` formateada con `| date:'dd/MM/yyyy HH:mm'`, preservando la semántica temporal institucional validada durante F3.
3. **Acción Ver**:
   - Certificado que la acción *Ver* abre la evaluación seleccionada pasando el `evaId` exacto de la fila y recuperando su detalle fresco y versión histórica (`obtenerEvaluacion(101)` vs `obtenerEvaluacion(102)`). Cobertura unitaria verificada.
4. **Acción Seguimiento**:
   - Certificado que la acción *Seguimiento* recibe exactamente el `evaId` numérico de la fila seleccionada (`obtenerFlujos(101)` vs `obtenerFlujos(102)`). Cobertura unitaria verificada.
5. **Responsive**:
   - Verificados viewports escritorio, tablet y móvil; 9 columnas contenidas en contenedor responsivo `overflow-x-auto rounded-xl border`.
6. **Accesibilidad**:
   - Foco visible, badges con texto legible independiente del color, botones con etiquetas `aria-label` descriptivas (`Ver evaluación`, `Editar evaluación`, `Seguimiento de evaluación`), tooltip `title` para nombres largos de riesgo.
7. **QA Visual Autenticada**:
   - 9 columnas, 6 estados (`BORRADOR`, `EN_REVISION`, `OBSERVADA`, `APROBADA`, `RECHAZADA`, `CERRADA`), Matriz Estado × Acciones (Ver en 6 estados, Editar únicamente en `BORRADOR`, Seguimiento en 6 estados). Valores nulos renderizan `-` y `0` renderiza `0`.
8. **Network**:
   - Consulta paginada inicial `pagina=1&registrosPorPagina=10`. Ausencia de la precarga `registrosPorPagina=200` en la vista `matriz`. 0 peticiones duplicadas injustificadas durante la certificación F3.
9. **Console**:
   - 0 errores Angular/TypeScript bloqueantes durante la certificación; ausencia de la regresión histórica `filter is not a function`.
10. **Sonar / SonarCloud — DIFERIDO AL CIERRE GLOBAL DEL PLAN**:
    - El workflow Sonar permanece como control informativo durante las fases intermedias y **no bloquea F4 ni fases posteriores**.
    - La deuda/Quality Gate de Sonar se revisará de manera consolidada al finalizar el Plan de Implementación, evitando consumir ciclos de desarrollo funcional en cada fase.
    - No se autoriza bajar thresholds, desactivar controles, ocultar issues ni modificar reglas para obtener un verde artificial.
11. **Higiene Completa**:
    - `git diff --check` PASS durante el cierre técnico F3. Árbol limpio reportado tras publicación.
12. **Validación Automatizada**:
    - Frontend Angular: **31 / 31 test files PASS**, **279 / 279 unit tests PASS (100%)**.
    - Build Angular: `npm run build` PASS (`dist/rl-app`).
    - Backend .NET Core: **406 / 406 PASS (100%)**.

## Criterios de Aceptación F3

- ✅ Carga inicial paginada sin prefetch de 200 registros.
- ✅ Renderizado robusto de 9 columnas desde DTOs normados.
- ✅ Matriz de estados y acciones 100% respetada.
- ✅ Normalización defensiva de `items` no-array y reseteo de metadatos en error.
- ✅ Cobertura unitaria y build frontend/backend aprobados.
- ✅ QA funcional, Network y Console sin regresiones bloqueantes registradas para F3.
- ✅ Quality Gates funcionales/institucionales aprobados.
- ✅ PR #20 en Draft y `main` sin fusionar.
- ⏭️ Sonar/SonarCloud diferido expresamente al cierre global del Plan de Implementación.

## Cierre funcional

**F3 no tiene pendientes de programación o funcionalidad que bloqueen el roadmap. Se autoriza continuar con F4 bajo el alcance rector vigente.**
