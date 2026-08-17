# F3 — Tabla de Evaluaciones: Semántica de Datos y Renderizado Robusto

## Estado

**F3.1 CERTIFICADA EN CI — F3 permanece activa hasta completar el cierre documental/QA residual aplicable.**

- Fecha: 2026-08-17 (UTC-6).
- Rama autorizada: `desarrollo`.
- Baseline F3: `f7992250ee1beed1d2a35a0f7e140b2bf97a7471`.
- Commit de implementación F3.1: `17b799377c7d268d68ca53a11b4e94deafe234dc`.
- Commit de ajuste de prueba F3.1: `cd94563f3c819bfa8ae9b2d9a4589aa6e5bca7d4`.
- Quality Gates exitosos: run `32072078960`, job `95517285843`.
- PR: #20, debe permanecer **Draft / No merged**.
- `main`: fuera de alcance.
- Backend: sin modificaciones F3.1.
- Oracle / SQL: sin modificaciones F3.1.

## Alcance rector recuperado de F1/F1-R

F3 corresponde a la **tabla de Evaluaciones**, su **semántica de datos** y su **renderizado robusto**. También recibe el residual de `DEF-02`: durante la vista inicial el contenedor `MatricesRiesgosCicloIntegralComponent` ejecutaba una consulta operativa de 200 registros al mismo tiempo que `MatricesRiesgosComponent` consultaba la página visible (10/20 registros), generando dos cargas con propósitos diferentes sobre el mismo endpoint.

## F3.1 — Cambios implementados

1. Se elimina la precarga automática de 200 evaluaciones cuando la vista inicial es `matriz`.
2. La consulta operativa de 200 registros queda reservada a las vistas que realmente consumen ese arreglo: `mitigacion` y `monitoreo`.
3. La respuesta operativa normaliza defensivamente `items` mediante `Array.isArray(...)`.
4. Ante error de la carga operativa se limpia el arreglo para no conservar datos obsoletos como si fueran vigentes.
5. Se actualiza la cobertura del contenedor integral para verificar ausencia de doble carga inicial, lazy-load por vista, normalización y manejo de error.
6. Se agrega una suite dedicada `matrices-riesgos.component.f3.spec.ts` para certificar:
   - una sola consulta paginada de 10 registros en la vista inicial;
   - las nueve columnas de la tabla de Evaluaciones;
   - semántica de filas y acciones por estado;
   - separación entre filas visibles y metadatos server-side (`totalRegistros`, `totalPaginas`);
   - normalización de `items = null` a `[]`.

## Incidencia de validación y corrección

El primer Quality Gate de F3.1 (run `32071496911`) falló únicamente en una aserción de la suite F3 que dependía del espaciado entre nodos HTML: el DOM renderizó semánticamente `de` + `37` + `registros`, mientras `textContent` concatenó los nodos como `de37registros`. La respuesta paginada y `totalRegistros = 37` eran correctos.

Se corrigió exclusivamente la aserción para normalizar espacios antes de verificar la semántica del paginador. No se modificó lógica productiva para ocultar el fallo de prueba.

## Validación automatizada certificada

El Quality Gate del SHA `cd94563f3c819bfa8ae9b2d9a4589aa6e5bca7d4` finalizó **SUCCESS**. Quedaron aprobadas las puertas institucionales de:

- instalación reproducible y auditoría npm;
- validadores GOV-02/GOV-03 y controles de base de datos/Oracle;
- analyzers .NET con warnings bloqueantes;
- ESLint frontend;
- accesibilidad FE-03 y contrato FE-04;
- adopción de Angular Signals FE-01;
- contratos dinámicos de Matrices, autorización e inventario exacto;
- build Release;
- Playwright Chromium;
- repository quality gates (pruebas, cobertura y E2E);
- configuración compose sin secretos;
- construcción multistage de contenedores backend y frontend;
- verificación de usuarios finales non-root.

## Criterios de aceptación F3.1

- ✅ La entrada a `Matriz y evaluaciones` no dispara la consulta operativa `registrosPorPagina=200`.
- ✅ La tabla mantiene nueve columnas y renderiza las filas desde `EvaluacionRiesgoResumenDto[]`.
- ✅ `totalRegistros` y `totalPaginas` provienen de la respuesta paginada, independientemente del tamaño de la página visible.
- ✅ Un `items` inválido/nulo no rompe la tabla ni convierte el signal `evaluaciones` en un objeto no-array.
- ✅ Mitigación y Monitoreo conservan su carga operativa cuando el usuario entra a esas vistas.
- ✅ Quality Gates completos en verde para la implementación F3.1.
- ✅ PR #20 continúa Draft y `main` no se fusiona.

## Continuidad

F3.1 queda técnicamente certificada. Antes de declarar **F3 cerrada** y habilitar F4, corresponde registrar el corte documental final y ejecutar únicamente la QA visual residual que el plan rector exija y que pueda realizarse sobre el entorno autorizado. No se debe iniciar F4 por inferencia ni mezclar en F3 los requisitos de buscador, filtros o límites del paginador asignados a fases posteriores.
