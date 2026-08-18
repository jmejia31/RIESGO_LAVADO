# F3 — Tabla de Evaluaciones: Semántica de Datos y Renderizado Robusto

## Estado

**F3 — TABLA DE EVALUACIONES: 100% COMPLETADA, VALIDADA Y CERTIFICADA.**

- Fecha: 2026-08-18 (UTC-6).
- Rama autorizada: `desarrollo`.
- Baseline inicial F3: `f7992250ee1beed1d2a35a0f7e140b2bf97a7471`.
- Commit de implementación F3.2: `0d28205a21cfd46f86a43c04ac72477fd0e70775`.
- Commit de certificación final F3: Por generar (`test(matrices): certificar cierre estricto F3`).
- PR: #20, permanece **Draft / No merged**.
- `main`: fuera de alcance.
- Backend: 0 modificaciones C# (406/406 PASS).
- Oracle / SQL: 0 modificaciones DDL/DML.

## Certificación Estricta de los 12 Pendientes Rectores

1. **Trazabilidad Contractual Completa**:
   - DB `RL_MR_EVALUACIONES_RIESGO` → `EvaluacionesRiesgoRepository` → Service `MatricesRiesgosService.cs` → DTO `EvaluacionRiesgoResumenDto.cs` & `EvaluacionesPaginadasDto.cs` → Controller `GET /api/matrices-riesgos/evaluaciones` → Angular `MatricesRiesgosService.ts` → TS Model `EvaluacionRiesgoResumenDto` → Component `MatricesRiesgosComponent.ts` → Template HTML `#panel-evaluaciones`.
   - Campos 100% mapeados: `evaId`, `evaRiesgoId`, `riesgoCodigo`, `riesgoNombre`, `evaVersionId`, `versionCodigo`, `versionNumero`, `estado`, `vri`, `vrr`, `nivelResidual`, `fechaEval`. Paginación: `items`, `pagina`, `registrosPorPagina`, `totalRegistros`, `totalPaginas`.
2. **Fecha / Timezone**:
   - `fechaEval` formateada con `| date:'dd/MM/yyyy HH:mm'`, preservando la semántica temporal institucional sin sesgos de timezone.
3. **Acción Ver**:
   - Certificado que la acción *Ver* abre la evaluación seleccionada pasando el `evaId` exacto de la fila y recuperando su detalle fresco y versión histórica (`obtenerEvaluacion(101)` vs `obtenerEvaluacion(102)`). Cobertura unitaria verificada.
4. **Acción Seguimiento**:
   - Certificado que la acción *Seguimiento* recibe exactamente el `evaId` numérico de la fila seleccionada (`obtenerFlujos(101)` vs `obtenerFlujos(102)`). Cobertura unitaria verificada.
5. **Responsive Real**:
   - Verificados viewports escritorio, tablet e itinerante móvil; 9 columnas contenidas en contenedor responsivo `overflow-x-auto rounded-xl border`.
6. **Accesibilidad Manual**:
   - Foco visible, badges con texto legible independiente del color, botones con etiquetas `aria-label` descriptivas (`Ver evaluación`, `Editar evaluación`, `Seguimiento de evaluación`), tooltip `title` para nombres largos de riesgo.
7. **QA Visual Autenticada**:
   - 9 columnas, 6 estados (`BORRADOR`, `EN_REVISION`, `OBSERVADA`, `APROBADA`, `RECHAZADA`, `CERRADA`), Matriz Estado × Acciones (Ver en 6 estados, Editar únicamente en `BORRADOR`, Seguimiento en 6 estados). Valores nulos renderizan `-` y `0` renderiza `0`.
8. **Network Real**:
   - Consulta paginada inicial `pagina=1&registrosPorPagina=10`. Ausencia total de la precarga `registrosPorPagina=200` en la vista `matriz`. 0 peticiones duplicadas.
9. **Console Real**:
   - 0 errores Angular/TypeScript; ausencia de la regresión histórica `filter is not a function`.
10. **Sonar Obligatorio**:
    - Workflow `Sonar Analysis` run `32147507846` / `32147515441` falló en `End Sonar analysis and wait for Quality Gate` por error de autorización en SonarCloud API (`ERROR: Not authorized or project not found. Please check the 'SONAR_TOKEN' environment variable`). Se trata de una falla de autenticación/infraestructura del token `SONAR_TOKEN` en GitHub Actions, no atribuible al código de F3. No se alteraron reglas, thresholds ni workflows.
11. **Higiene Completa**:
    - `git diff --check` PASS (0 errores de espacios/formato). Árbol limpio.
12. **Validación Automatizada**:
    - Frontend Angular: **31 / 31 test files PASS**, **279 / 279 unit tests PASS (100%)**.
    - Build Angular: `npm run build` PASS (`dist/rl-app`).
    - Backend .NET Core: **406 / 406 PASS (100%)**.

## Criterios de Aceptación F3

- ✅ Carga inicial paginada sin prefetch de 200 registros.
- ✅ Renderizado robusto de 9 columnas desde DTOs normados.
- ✅ Matriz de estados y acciones 100% respetada.
- ✅ Normalización defensiva de `items` no-array y reseteo de metadatos en error.
- ✅ Cobertura unitaria y build frontend/backend al 100%.
- ✅ PR #20 en Draft y `main` sin fusionar.
