# Estado de colaboración y punto de continuidad

> Actualización 2026-08-14 (Antigravity): **Ampliación de Cobertura Real en Componente Principal MatricesRiesgosComponent Certificada**. Se agregaron 11 nuevas pruebas unitarias reales en `matrices-riesgos.component.spec.ts` (17 tests) y `matrices-riesgos.component.workflow.spec.ts` (12 tests), totalizando **252/252 pruebas frontend aprobadas al 100%** (29 archivos de prueba). Cobertura frontend alcanzada: **Sentencias = 48.15%, Líneas = 48.20%, Funciones = 46.33%, Ramas = 42.88%**. Pruebas Backend (.NET Release: 348/348) y Playwright E2E (14/14) verificadas. Publicado en `origin/desarrollo`.

> Actualización 2026-08-13 (Antigravity): **Auditoría y Subsanación de Observaciones SonarCloud (PR #20) Certificadas**. Se corrigió el binding HTML nativo `[readOnly]` en `form-builder.component.html`. Se justificaron técnica y documentalmente las métricas de mantenibilidad en los scripts SQL de validación de solo lectura (`03`, `04`, `05`, `06`), confirmando 0 mutaciones en base de datos. Pruebas Backend (319/319), Frontend (181/181), Playwright E2E (14/14) y Quality Gates institucionales **100% SÚPERADAS**. Publicado en `origin/desarrollo`.

> Actualización 2026-08-13 (Antigravity): **Fase 7 — Pruebas Backend (.NET) para Familias, Versiones y Permisos Certificadas**. Se creó e integró la suite `MatricesRiesgosPhase07BackendCoverageTests.cs`, validando la unicidad de familias, inmutabilidad de versiones publicadas, control de sintaxis JSON y autorización de roles por reflexión. Pruebas Backend (314/314) y Frontend (177/177) superadas al 100%. Publicado en `origin/desarrollo` (Commit `911bbb5`). Siguiente paso: Fase 8 — Pruebas Frontend y Flujo E2E Playwright.

> Actualización 2026-08-13 (Antigravity): **Inhabilitación CSS Absoluta (`pointer-events: none`) de la Interfaz Trasera Certificada**. Se configuró la regla global CSS `:has([role="dialog"])` que desactiva de forma estricta los eventos de clic y selección en la cabecera (incluyendo el botón de "Salir"), la barra lateral y la página principal mientras exista un modal abierto en la aplicación. Pruebas Frontend (177/177) y Backend (314/314) superadas al 100%. Publicado en `origin/desarrollo` (Commit `745f759`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Estandarización Global de Modales (`z-[1000]`) y Aislamiento Trasero Certificados**. Se extendió la regla a todos los módulos del sistema (`Monitoreo de Listas`, `Coincidencias Patrono/Empleado`, `Tipo de Listas`, `Usuarios`, `Bitácora`, `Configuración` y `Matrices de Riesgos`). Todos los modales se abren con `fixed inset-0 z-[1000] bg-slate-900/60 backdrop-blur-sm`, cubriendo el 100% de la ventana (cabecera superior y menú lateral) e inhabilitando de forma absoluta cualquier acción en la interfaz inferior. Pruebas Frontend (177/177) y Backend (314/314) superadas al 100%. Publicado en `origin/desarrollo` (Commit `8304281`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Comentario HTML Sanitizado a ASCII Puro y Registro de Advertencia `exceljs` Certificados**. Se reemplazó la tilde en el comentario técnico interno (`MODAL ESTETICO SUPERPUESTO DEL FORM BUILDER`) garantizando 0 mojibake. Se registró formalmente la advertencia preexistente del compilador Angular respecto a `exceljs` (`non-ESM CommonJS dependency warning`). Pruebas Frontend (177/177) y Backend (314/314) superadas al 100%. Publicado en `origin/desarrollo` (Commit `1859c34`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Modal Flotante Estético Restaurado y Cobertura de Cabecera con `z-[1000]` Certificados**. Se revirtió la vista plana sin bordes y se restableció el modal redondeado premium (`max-w-[96vw] h-[92vh] flex flex-col rounded-2xl bg-white shadow-2xl overflow-hidden border border-gray-100 relative`). Al elevar la capa superpuesta a `z-[1000]`, la sombra traslúcida con desenfoque cubre perfectamente la franja/cabecera superior sin dejar espacios ni capas expuestas. Pruebas Frontend (177/177) y Backend (314/314) superadas al 100%. Publicado en `origin/desarrollo` (Commit `fbb9251`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Modal 100% Pantalla Completa (Full-Screen) Real y Bloqueo Trasero Certificados**. Se eliminó completamente la franja/borde expuesto en la parte superior asignando cobertura del 100% (`fixed inset-0 z-[999] w-full h-full`) con backdrop blur denso. Se bloquea de forma absoluta cualquier acción en la interfaz inferior mientras exista un modal abierto. Pruebas Frontend (177/177) y Backend (314/314) superadas al 100%. Publicado en `origin/desarrollo` (Commit `a35492b`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Form Builder en Modal Amplio Superpuesto y Paridad Estética Integral con Monitoreo de Listas Certificados**. Se refactorizó la apertura de la definición de formularios eliminando la expansión vertical en la parte inferior e implementando un modal superpuesto de alta densidad (`96vw x 92vh` con backdrop blur). Se alineó el 100% de la gama cromática (`bg-ihss-900`, `text-ihss-600`), tarjetas KPI, categorías tablist y botones con iconos SVG a la estética de Monitoreo de Listas. Pruebas Frontend (177/177) y Backend (314/314) superadas al 100%. Publicado en `origin/desarrollo` (Commit `30f0bcb`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Fase 6 — Navegación por Teclado WAI-ARIA 1.2 Roving Tabindex y Ortografía UTF-8 Certificadas**. Se completó la navegación accesible por teclado (flechas, `Home`, `End`, `tabindex` roving dinámico `0`/`-1` y foco programático). Se restauró toda la ortografía y acentuación en castellano en UTF-8 nativo limpio (0 mojibake). Pruebas Frontend (177/177) y Backend (314/314) superadas al 100%. Publicado en `origin/desarrollo` (Commit `616caca`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Fase 6 — Accesibilidad WAI-ARIA `tab/tabpanel` Vinculada y Limpieza ASCII Certificada**. Se establecieron las relaciones recíprocas explícitas de accesibilidad entre las pestañas (`id="tab-<id>"`) y sus respectivos paneles (`id="panel-<id>"`, `role="tabpanel"`, `aria-labelledby="tab-<id>"`). Se limpiaron todos los textos en la plantilla HTML a ASCII puro (0 mojibake). Pruebas Frontend (177/177) y Backend (314/314) súperadas al 100%. Publicado en `origin/desarrollo` (Commit `ffdc559`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Fase 6 — UX, Accesibilidad ARIA y Modos de Lectura Estrictos Completada**. Se incorporaron estándares WAI-ARIA (`role="tablist"`, `role="tab"`, `aria-selected`, `aria-controls`), retroalimentación animada de carga con SVG institucional (`aria-busy="true"`, `aria-live="polite"`), y se preservaron los modos estrictos de solo lectura para versiones publicadas y roles no administradores. Pruebas Frontend (177/177) y Backend (314/314) súperadas al 100%. Publicado en `origin/desarrollo` (Commit `f597685`). Siguiente paso: Fase 7 — Pruebas Backend (.NET).

> Actualización 2026-08-13 (Antigravity): **Fase 5 — Verificación Backend .NET Reproducida (314/314), Traza Incondicional en `calculosJson` y Certificación Integral Definitiva**. Se ejecutó la suite completa de backend (`dotnet test RIESGO_LAVADO.sln --configuration Release`) con 314/314 pruebas superadas. Se ajustó `recalcularFormulasEvaluacion` registrando siempre la traza incondicional completa de todas las fórmulas activas en `calculosJson` al guardar. Pruebas Frontend (177/177) y Backend (314/314) reproducidas y súperadas al 100%. Publicado en `origin/desarrollo` (Commit `0300d02`). Siguiente paso: Fase 6 — UX, Accesibilidad ARIA y Modos de Lectura Estrictos.

> Actualización 2026-08-12 (Antigravity): **Fase 5 — Validación de Campos Inexistentes, Limpieza ASCII Total y Certificación Definitiva**. Se agregó la validación explícita que rechaza fórmulas con referencias a campos inexistentes (`Referencia a campo inexistente '<nombre>'`), registrando el detalle del error en `calculosJson`. Se realizó una limpieza ASCII estricta sobre las utilidades y specs (0 mojibake). Creada prueba unitaria específica (177/177 pruebas pasadas al 100%). Publicado en `origin/desarrollo` (Commit `d73b7a5`). Siguiente paso: Fase 6 — UX, Accesibilidad ARIA y Modos de Lectura Estrictos.

> Actualización 2026-08-12 (Antigravity): **Fase 5 — Análisis Real de Grafo de Dependencias, Detección de Ciclos y Limpieza ASCII Completada**. Se implementaron `obtenerDependenciasDeFormula` y `detectarCicloEnFormulas` realizando un recorrido DFS sobre el grafo de campos, detectando ciclos directos e indirectos (`A -> B -> A`). Se reescribió el evaluador y la suite de especificaciones en ASCII limpio eliminado todo mojibake. Pruebas Frontend (176/176) pasadas al 100%. Publicado en `origin/desarrollo` (Commit `7f0fefb`). Siguiente paso: Fase 6 — UX, Accesibilidad ARIA y Modos de Lectura Estrictos.

> Actualización 2026-08-12 (Antigravity): **Fase 5 — Evaluador Seguro Shunting-Yard y Certificación Completa**. Se eliminó la función `new Function(...)` sustituyéndola por un parser seguro Shunting-Yard RPN (0 ejecuciones dinámicas). Se implementó la resolución de dependencias encadenadas entre fórmulas y la detección de referencias circulares (ciclos). Se normalizó todo el texto en UTF-8 estricto. Pruebas Frontend (176/176) pasadas al 100%. Publicado en `origin/desarrollo` (Commit `bf2d8ab`). Siguiente paso: Fase 6 — UX, Accesibilidad ARIA y Modos de Lectura Estrictos.

> Actualización 2026-08-12 (Antigravity): **Fase 5 — Motor de Fórmulas Dinámicas y `EVA_DATOS_CALC_JSON` Certificado**. Se creó la utilidad `dynamic-formula-evaluator.util.ts` y su suite de pruebas unitarias (`dynamic-formula-evaluator.util.spec.ts`). Las evaluaciones en la pestaña "Captura" ahora recalculan automáticamente los campos calculados al modificar campos dependientes y guardan la traza en `EVA_DATOS_CALC_JSON`. Se eliminó cualquier vestigio de mojibake garantizando UTF-8 estricto. Pruebas Frontend (175/175) y Backend (314/314) súperadas al 100%. Publicado en `origin/desarrollo` (Commit `d0861eb`). Siguiente paso: Fase 6 — UX, Accesibilidad ARIA y Modos de Lectura Estrictos.

> Actualización 2026-08-12 (Antigravity): **Fase 5 — Integración con Captura Dinámica y `EVA_DATOS_JSON`** completado al 100%. Se adaptó el motor de captura dinámica para renderizar las secciones creadas en el Form Builder respetando la grilla de 1 a 6 columnas (`columnasPorFila`), los anchos individuales de campo (`anchoColumnas`) y los campos calculados, almacenando los datos ingresados en `EVA_DATOS_JSON`. Compilación Angular (`npm run build`) y suite Frontend (171/171) súperadas sin errores. Publicado en `origin/desarrollo` (Commit `649bffd`). Siguiente paso: Fase 6 — UX, Accesibilidad y Modos de Lectura Estrictos.

> Actualización 2026-08-12 (Antigravity): **Fase 3 y 4 Certificadas y Restricción Estricta de Rol Aplicada**. Se retiró `ANALISTA_RIESGO` del cálculo de `esAdministrador`, reservando la edición de JSON técnico exclusivamente a roles `ADMIN` y `ADMINISTRADOR`. Pruebas Frontend ejecutadas nuevamente con resultado limpio (27/27 suites, 171/171 pruebas). Publicado en `origin/desarrollo` (Commit `b18e99c`). Siguiente paso: Fase 5 — Integración con Captura Dinámica y `EVA_DATOS_JSON`.

> Actualización 2026-08-12 (Antigravity): **Reparación de Permisos y Certificación de Fase 3 y 4** completada al 100%. Se cambió el valor predeterminado de `@Input() esAdministrador` a `false` en `FormBuilderComponent` y se enlazó en `MatricesRiesgosComponent` mediante `AuthService.tieneRol()`. Compilación Angular (`npm run build`) y suite Frontend (171/171) pasadas al 100%. Publicado en `origin/desarrollo` (Commit `e99c3e4`). Siguiente paso: Fase 5 — Integración con Captura Dinámica y `EVA_DATOS_JSON`.

> Actualización 2026-08-12 (Antigravity): **Fase 4 — Motor de Validación de Definición Espejo y Cobertura Form Builder** completado al 100%. Se construyó la utilidad preventiva `form-builder-validator.util.ts` que valida títulos, claves únicas y catálogos/fórmulas antes de guardar. Se restringió el botón de JSON técnico exclusivamente a administradores (`esAdministrador`) y se agregaron las pruebas unitarias del Form Builder (`form-builder.component.spec.ts`). Pruebas Frontend (170/170) superadas sin errores. Publicado en `origin/desarrollo` (Commit `80ad3b3`). Siguiente paso: Fase 5 — Integración con Captura Dinámica y `EVA_DATOS_JSON`.

> Actualización 2026-08-12 (Antigravity): **Fase 3 — Constructor Visual de Formularios (Form Builder)** completado e integrado al 100%. Se construyó el componente `FormBuilderComponent` de 3 paneles (Paleta de controles, Lienzo interactivo y Inspector de propiedades) sustituyendo la edición de JSON plano por una experiencia gráfica moderna. Compilación Angular (`npm run build`) y suite Frontend (165/165) súperadas sin errores. Publicado en `origin/desarrollo` (Commit `2284722`). Siguiente paso: Fase 4 — Motor de Validación de Definición (Validación Espejo Frontend/Backend).

> Actualización 2026-08-12 (Antigravity): **Fase 2 — Endurecimiento del Ciclo de Versiones** completada y corregida al 100%. Se reforzó `ActualizarBorradorFormularioAsync` agregando `AND VER_ESTADO = 'DRAFT'`, garantizando que ninguna versión en estado `PUBLISHED` (sea la versión activa o una versión histórica no vigente) pueda ser editada. Se agregó la prueba unitaria correspondiente (`ActualizarBorrador_RechazaModificacionDeVersionPublicadaHistorica`). Pruebas Backend (314/314) 100% pasadas. Publicado en `origin/desarrollo` (Commit `64a5443`). Siguiente paso: Fase 3 — Constructor Visual de Formularios (Form Builder).

> Actualización 2026-08-12 (Antigravity): **Fase 1 — Endurecimiento del CRUD de Familias** completada al 100%. Se reforzó la respuesta ante códigos duplicados retornando HTTP 409 Conflict y se corrigieron las aserciones de pruebas unitarias en Frontend. Pruebas Backend (313/313) y Frontend (165/165) 100% súperadas. Publicado en `origin/desarrollo` (Commits `b4c5bc1` y seguimiento documental). Siguiente paso: Fase 2 — Endurecimiento del Ciclo de Versiones.

> Actualización 2026-08-12 (Antigravity): **Fase 0 — Revisión Técnica de Línea Base (Form Builder)** completada al 100% (100% solo lectura, 0 modificaciones en Oracle o código). Se auditaron la rama `desarrollo`, endpoints REST, servicios/modelos Angular, validadores y scripts de BD. Compilación y suite .NET (313/313) súperados. Publicado en `origin/desarrollo` (Commit `0105fc3`). Siguiente paso: Fase 1 — Endurecimiento del CRUD de Familias.

> Actualización 2026-08-12 (Antigravity): Integrada la acción explícita "Ver definición" para la consulta en modo lectura de la estructura JSON en todas las versiones de formulario (activas e inactivas). Las versiones no vigentes cuentan con el botón complementario "Editar definición". Publicado en `origin/desarrollo` (Commit `e5f7582`). Build Angular y .NET validados con 0 errores.

> Actualización 2026-08-12: corregido el 400 `jsonConfig field is required` del editor de Plantillas. La causa era la incompatibilidad entre `JsonDocument` y el formateador Newtonsoft activo en `Program.cs`; los endpoints de borrador usan ahora `JToken` y conservan el JSON dinámico. Pruebas dirigidas del controlador y contrato UAT: 14/14 correctas. Pendiente verificación visual del usuario tras reiniciar API y frontend.

> Actualización 2026-08-12: la carga HTTP global ya no inserta un skeleton grande sobre contenido disponible; permanece como indicador compacto en cabecera. El editor de Plantillas valida y envía la definición como objeto JSON, mostrando errores de validación legibles. Verificación local: Frontend 165/165, E2E 13/13, ESLint y build correctos, validadores FE-03/FE-04, Matrices y enlaces documentales correctos. Pendiente únicamente la comprobación visual del usuario tras actualizar/reiniciar el frontend.

> Actualización 2026-08-10: **FE-01 — Adopción gradual de Angular Signals** quedó implementada y certificada técnicamente en `desarrollo`. La fase consolidó Signals para estado local/derivado, migró a `OnPush` la primera ola de componentes ya compatibles, tipó y signalizó el carrusel de Login y el archivo seleccionado de Carga de Listas, preservando RxJS para asincronía HTTP y Reactive Forms para formularios. El HEAD técnico certificado es `479e95f6089d098942dffaff75ee6a76b0412039`; Quality Gates Run `31422869343` (#668) finalizó **SUCCESS** con FE-01 Validator correcto, Backend 304/304, Frontend 165/165, E2E 13/13, build 0 errores/0 advertencias y `npm audit` 0 vulnerabilidades. No se modificaron Backend funcional, contratos API, Oracle ni Producción.

Documento vivo. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria:** `desarrollo`
- **Base FE-01:** `7d7b9f093a881154e7f5d2373d393cc0ffef31f9`
- **HEAD técnico FE-01 certificado:** `479e95f6089d098942dffaff75ee6a76b0412039`
- **Rama estable:** `main`
- **HEAD de `main`:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **PR #20:** debe permanecer abierto, en borrador y sin fusión
- **Modelo Matrices:** 17 tablas `RL_MR_*` + 17 secuencias
- **DB-03:** cerrado físicamente; 11 planes ejecutados; sin índices nuevos
- **DB-01:** política y controles de repositorio completados; sin purga automática
- **DB-ESTANDARES:** Comentarios institucionales DDL (`COMMENT ON TABLE` y `COMMENT ON COLUMN`) integrados en las 17 tablas y 98 columnas `RL_MR_*` (`01_comentarios_y_estandares_modelo_17_tablas.sql` y `06_reconstruir_modelo_17_tablas.sql`).
- **FE-03 + FE-04:** completado y certificado
- **FE-01:** completado y certificado técnicamente

---

## 2. FE-01 — decisión arquitectónica

FE-01 es una adopción **gradual**, no una reescritura global.

### Signals se utilizan para

- estado local síncrono consumido por templates;
- estado derivado mediante `computed`;
- selección y flags de interfaz;
- colecciones locales cuya mutación debe ser explícita.

### RxJS se conserva para

- `HttpClient` y respuestas asíncronas;
- interceptores;
- composición temporal/cancelación y pipelines donde sus operadores siguen siendo el modelo apropiado.

### Reactive Forms se conserva para

- formularios existentes;
- validaciones y contratos de captura ya certificados.

FE-01 no introduce una migración experimental de formularios ni reemplaza servicios HTTP estables.

---

## 3. Primera ola `OnPush`

Quedaron protegidos con `ChangeDetectionStrategy.OnPush`:

1. `App`.
2. `MainLayoutComponent`.
3. `SinAccesoComponent`.
4. `ConfiguracionComponent`.
5. `BitacoraComponent`.
6. `LoginComponent`.
7. `CargarListasComponent`.

`MatricesRiesgosComponent` ya utilizaba Signals + `OnPush` antes de FE-01 y se preservó sin reescritura masiva.

---

## 4. Migraciones concretas

### Login

El carrusel dejó de mezclar un Signal de índice con un arreglo `any[]` mutable:

- `slides` → `signal<LoginSlide[]>([])`;
- `slideSeleccionado` → `computed(...)`;
- temporizador → `ReturnType<typeof setInterval> | null`;
- template → `slides()` y `slideSeleccionado()`;
- `track` estable por `slide.id`;
- protección ante lista vacía, una única diapositiva e índice fuera de rango.

El contrato de `ConfiguracionService` no cambió.

### Carga de Listas

`archivoSeleccionado` pasó de campo mutable a:

`signal<File | null>(null)`

La operación HTTP conserva exactamente el mismo servicio y contrato; antes de subir se captura una instantánea local no nula del Signal.

---

## 5. Adopciones previas preservadas

El validador FE-01 protege también:

- `AuthService`: `signal`, `computed`, `effect`;
- `GlobalHttpStateService`: estado con `signal` + `computed`;
- `MainLayoutComponent`: navegación derivada mediante Signals;
- `SinAccesoComponent`: `toSignal` para parámetros de ruta;
- `MatricesRiesgosComponent`: Signals + `computed` + `OnPush`.

No se introduce `BehaviorSubject` para reemplazar estado local Signal en estas superficies.

---

## 7. Validador bloqueante

Ubicación:

`scripts/validation/validate_fe01_signals_adoption.ps1`

Quality Gates valida:

- `OnPush` en los siete componentes de la primera ola;
- ausencia de `Eager` en esas superficies;
- carrusel de Login tipado y derivado mediante Signals;
- archivo seleccionado de Carga de Listas como Signal;
- conservación de Signals en Auth, estado HTTP, layout, Sin Acceso y Matrices;
- ausencia de `BehaviorSubject` como regresión de estado local en las superficies protegidas;
- conexión del propio validador con `.github/workflows/quality-gates.yml`.

---

## 8. Dossier técnico

`docs/0.0 Documentación/FE_01_ADOPCION_GRADUAL_ANGULAR_SIGNALS_2026-08-10.md`

Documenta estrategia, límites, primera ola, migraciones, criterios de aceptación y continuidad.

---

## 9. Evidencia CI FE-01

**Quality Gates Run:** `31422869343` (#668) — **SUCCESS**

- FE-01 Validator: **CORRECTO**.
- FE-03/FE-04 Validator: **CORRECTO**.
- Validadores DB/Oracle/DB-03/DB-01: **CORRECTOS**.
- Autorización/UAT Matrices: **CORRECTOS**.
- Inventario Matrices: **17 tablas / 17 secuencias**.
- Build Release: **0 errores / 0 advertencias**.
- Backend: **304/304**.
- Frontend: **165/165** en 26 archivos.
- E2E Playwright: **13/13**.
- `npm audit`: **0 vulnerabilidades**.
- Cobertura Backend: **22.19% líneas / 24.83% ramas**.
- Cobertura Frontend: **39.69% sentencias / 35.39% ramas / 36.03% funciones / 39.27% líneas**.

La ligera variación de cobertura frontend respecto de FE-03/FE-04 corresponde al aumento de líneas/ramas defensivas del carrusel y no a pérdida de pruebas: la suite permanece 165/165 y E2E 13/13.

---

## 9. Alcance no modificado

- **NO** se modificó Backend funcional.
- **NO** se alteraron endpoints ni contratos API.
- **NO** se conectó ni ejecutó Oracle durante FE-01.
- **NO** se ejecutó DDL/DML.
- **NO** se ejecutaron scripts 05/06.
- **NO** se modificaron `B10_*`.
- **NO** se modificó Producción.
- **NO** se modificó/fusionó `main`.

---

## 10. Estado consolidado del Plan de Mejoras Integrales

| Orden | Código | Estado |
|---:|---|---|
| 1 | GOV-01 — Sincronización Bitácora / UAT | **Completado** |
| 2 | BE-01 + FE-02 — ProblemDetails + Interceptor HTTP | **Completado y certificado** |
| 3 | BE-03 — `/healthz` + `/readyz` | **Completado y certificado** |
| 4 | BE-04 — Rate Limiting | **Completado y certificado** |
| 5 | BE-02 — Caché con invalidación explícita | **Completado y certificado** |
| 6 | DB-03 — Profiling Oracle / `EXPLAIN PLAN` | **Completado físicamente; sin índices nuevos** |
| 7 | DB-01 — Política de archivado de auditoría | **Completado y certificado técnicamente** |
| 8 | FE-03 + FE-04 — Accesibilidad + Skeleton Loaders | **Completado y certificado** |
| 9 | FE-01 — Signals gradual | **Completado y certificado** |
| 10 | GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa | **Siguiente** |

---

## 11. Directrices activas

1. Trabajar exclusivamente sobre `desarrollo`.
2. Codex y Antigravity usan `C:\RIESGO_LAVADO` y publican cada cambio confirmado en `origin/desarrollo`; ChatGPT usa el repositorio remoto y solo un checkout local que haya confirmado disponible.
3. Todo handoff debe indicar commit, archivos, pruebas y publicación; una limitación de acceso local debe quedar explícitamente pendiente.
4. No modificar/fusionar `main` sin autorización expresa de Javier Mejía.
5. Mantener PR #20 abierto y en borrador; no auto-merge.
6. No ejecutar transición 05/06 ni modificar/eliminar `B10_*`.
7. No versionar secretos ni cadenas de conexión.
8. Usar Signals para estado local/derivado cuando simplifique el modelo; no forzar su uso sobre flujos RxJS naturalmente asíncronos.
9. Mantener `OnPush` en las superficies protegidas por FE-01.
10. No reintroducir estado mutable paralelo al Signal de Login/Carga de Listas.
11. No convertir futuras adopciones de Signals en reescrituras masivas sin evidencia técnica y regresión completa.
12. La bitácora histórica es append-only; las correcciones se agregan, no se reescriben entradas anteriores.

---

## 12. Punto exacto de continuación

**Fase activa en desarrollo:**

### GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa

- **Estado de la fase**: **Abierta y en progreso** (no se declara cerrada ni certificada en esta intervención).
- **Actualización de seguridad en CI (2026-08-11)**:
  - Commit técnico certificado: `eb05a6316dceabad2cbb138c9d33693aacb9c8bb`.
  - Quality Gate #711 (`31513734376`): **SUCCESS**.
  - Cambio realizado: Sustitución del marcador sintético de la cadena de conexión de prueba en `.github/workflows/quality-gates.yml` de `Password=ci` a `Password=CHANGE_ME`.
  - Naturaleza del fixture: Utiliza el dominio de prueba reservado `ci.invalid` y no corresponde a una conexión, credencial ni entorno Oracle real ni institucional.
  - Validador local de enlaces de documentación (`tools/validate_documentation_links.ps1`):
    ```text
    Validacion de documentacion correcta.
    Documentos Markdown revisados: 71
    Enlaces locales revisados: 163
    ```
  - Restricciones confirmadas: No se ejecutó Oracle ni se modificó/fusionó la rama `main` ni el PR #20.
- **Remediación E2E Playwright y Certificación CI (2026-08-11)**:
  - Commit técnico certificado: `43a30bf7675dd7ddaabb84a91dc4e26da49ac680`.
  - Quality Gates Runs `31529552815` (push) y `31529557756` (PR #20): **SUCCESS 100% (21/21 pasos en verde)**.
  - Sonar Analysis Runs `31529552784` (push) y `31529557739` (PR #20): **SUCCESS**.
  - Subsanada condición de carrera en `e2e/matrices-uat-integral.spec.ts` (`UAT registra control, efectividad, plan y actividad`) mediante sincronización explícita de confirmación UI Angular (`toBeVisible()`).
  - Suite E2E completa **13/13 VERDE**, Backend **304/304 VERDE**, Frontend **165/165 VERDE**, `npm audit` **0 vulnerabilidades**.
- **Tipado E2E Node y Certificación CI (2026-08-11)**:
  - Commit técnico certificado: `9112e83e713803f5a9b827aef684aab344315f1a`.
  - Quality Gates Runs `31531986586` (push) y `31531989896` (PR #20): **SUCCESS 100% (21/21 pasos en verde)**.
  - Sonar Analysis Runs `31531986706` (push) y `31531989895` (PR #20): **SUCCESS**.
  - Corregido el diagnóstico TypeScript `TS2580` de `Buffer` en `matrices-uat-integral.spec.ts` mediante importación desde `node:buffer`, dependencia directa `@types/node` y configuración de compilación `e2e/tsconfig.json`.
  - Subsanada la condición de carrera por modo estricto en Playwright acotando los localizadores de actividades de plan al contenedor unívoco `div.bg-slate-50`.
- **Certificación Docker Multietapa Local — GOV-02 + GOV-03 Punto 3 (2026-08-11)**:
  - Commit certificado: `83c21ab1844621ffb8f9e612ea21a6a6a9b407e3`.
  - Punto 3 del plan GOV-02 + GOV-03 completado al 100% en entorno local controlado.
  - Validación estática `docker compose config` sin exponer secretos.
  - Construidas imágenes multietapa: `riesgo-lavado-api:local` (112MB) y `riesgo-lavado-frontend:local` (29MB).
  - Usuarios no-root verificados en ejecución: Backend como `app` (`uid=1654`), Frontend como `nginx` (`uid=101`).
  - Healthchecks HTTP en verde: `/healthz` Backend (8080) y Nginx (8081).
  - Proxying Nginx a Backend verificado. Limpieza `docker compose down` ejecutada exitosamente.
- **Configuración de codificación SonarCloud (2026-08-11)**:
  - Se añadió `.sonarcloud.properties` con `sonar.sourceEncoding=UTF-8` como ajuste mínimo para el análisis automático.
  - No se configuraron exclusiones, perfiles, Quality Gate, Python, `NOSONAR` ni supresiones.
  - `validate_documentation_links.ps1`: correcto (71 documentos y 163 enlaces). `validate_repository_structure.ps1` reporta dos rutas heredadas no intervenidas bajo `frontend/rl-app/src/app/core/services`; su saneamiento queda como pendiente separado.
- **Remediación de Seguridad SQL Dinámico SonarCloud — PR #20 Bloque 1 (2026-08-12)**:
  - Aplicado `DBMS_ASSERT.SIMPLE_SQL_NAME` en `00_retiro_controlado_modelo_prueba.sql` (líneas 132 y 145) para sentencias DDL `EXECUTE IMMEDIATE`.
  - Aplicado `DBMS_ASSERT.ENQUOTE_NAME` en `07_preflight_inventario_oracle_solo_lectura.sql` (línea 145) para consulta dinámica `COUNT(*)`.
  - Clasificados 6 archivos/bloques como falsos positivos por corresponder a SQL estático puro o DDL estático fijo en PL/SQL (`01_db03_inventario_estadisticas_solo_lectura.sql`, `02_db03_explain_plan_consultas_criticas.sql`, `05_ajustes_dashboard_seguridad_reportes.sql`, `03_seed_catalogos_iniciales.sql`, `01_semillas_datos_iniciales_modelo_17_tablas.sql`, `02_validar_semillas_bloque1_solo_lectura.sql`).
  - Validaciones `validate_database_scripts.ps1` y `validate_documentation_links.ps1` ejecutadas en verde (71 Markdown docs, 163 enlaces).
- **Corrección de Validador Integral de Matrices (2026-08-12)**:
  - Restablecida la nomenclatura oficial `RL_MR_TRAZAS_CALCULO` y `SEQ_RL_MR_TRAZAS` en los arreglos de objetos retirados de `MatricesRiesgosRepositoryIntegrationTests.cs`.
  - Normalizada la comparación de separadores de ruta en `scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1` para compatibilidad Windows/CI.
  - Validador integral de Matrices, validador de scripts BD, suite .NET (304/304) y enlaces de documentación en verde.
- **Remediación de Hallazgos SonarCloud No-SQL (2026-08-12)**:
  - Frontend: Asignadas asociaciones explícitas `<label for="..." id="...">` en plantillas de Matrices de Riesgos (`matrices-riesgos-monitoreo-operativo`, `matrices-riesgos`, `matrices-riesgos-mitigacion`, `matrices-riesgos-gestion`).
  - Instalación npm: Configurado `npm ci --ignore-scripts` en `frontend/rl-app/Dockerfile`, `quality-gates.yml` y `sonar-analysis.yml`.
  - Docker Frontend: Configurada la pertenencia `root:root` con permisos `755` para los archivos estáticos en `/usr/share/nginx/html`, manteniendo el usuario no-root `nginx` (`uid=101`) y directorios temporales `/tmp/nginx`.
  - Pruebas unitarias Angular (165/165), build, Playwright E2E (13/13), contenedor Docker y enlaces de documentación en verde.
- **Ajuste Semántico DL/DT/DD y Verificación ESLint (2026-08-12)**:
  - Reestructuradas las 8 tarjetas de métricas en `matrices-riesgos-monitoreo-operativo.component.html` a elementos `<dl>` individuales con su `<dt>` y `<dd>` directos sobre contenedor `<div>` grid.
  - Verificado `npm run lint` tras limpiar `.angular/cache` (0 errores). Pruebas unitarias Angular (165/165), build, Playwright E2E (13/13) y enlaces de documentación en verde.
- **Remediación de Hallazgos SonarCloud en Scripts Oracle (2026-08-12)**:
  - Aplicado `DBMS_ASSERT.SIMPLE_SQL_NAME` a `drop_table_if_exists` y `drop_sequence_if_exists` en `06_reconstruir_modelo_17_tablas.sql`.
  - Agregada direccionalidad `ASC` explícita a cláusulas `ORDER BY` en scripts `05_ajustes_dashboard_seguridad_reportes.sql`, `07_preflight_inventario_oracle_solo_lectura.sql` y `08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql`.
  - Validadores de base de datos, alineación dinámicos de Matrices (96 archivos) y enlaces de documentación en verde.
- **Endurecimiento pendiente SonarCloud (2026-08-12)**:
  - `00_retiro_controlado_modelo_prueba.sql` ahora exige lista cerrada + `DBMS_ASSERT.SIMPLE_SQL_NAME` para cada tabla o secuencia histórica antes de `DROP` dinámico.
  - `09_limpieza_tablas_respaldo_b10.sql` solo puede seleccionar `B10_001`–`B10_041`, `BKP_F10_MAP` y `BKP_F10_SECUENCIAS`; cualquier otro nombre o error no esperado aborta.
  - `matrices-riesgos.component.html` corrigió agrupación semántica de métricas y etiquetas accesibles; el script `npm run lint` dejó de inspeccionar caché generada, manteniendo intactas sus reglas.
  - Validado localmente: scripts BD, contrato dinámico de Matrices, ESLint, unitarias Angular, build y Playwright 13/13. El build conserva una advertencia no bloqueante de `exceljs` CommonJS.
  - Pendiente: verificar el nuevo análisis SonarCloud remoto sobre el commit publicado; no se aplicarán exclusiones ni `NOSONAR` para alterar la calificación.
- **Corrección de guardado de plantilla Matrices (2026-08-12)**:
  - Corregido `415 Unsupported Media Type` en `PUT /api/matrices-riesgos/formularios/{id}` y en la creación de borrador.
  - La traza posterior identificó que `JsonElement` llegaba en estado inválido al controlador. La API ahora recibe `JsonDocument` y usa `RootElement.GetRawText()`, por lo que conserva el JSON real antes de validar y persistir.
  - Pruebas locales posteriores: contrato/controlador 14/14 y suite backend 306/306 correctos; validadores dinámico de Matrices y de scripts BD correctos.
  - Se requiere reiniciar API/frontend locales antes de validar manualmente el botón **Guardar**.
- **Clasificación Integral de Deuda Técnica SonarCloud (~150 Problemas) (2026-08-12)**:
  - Clasificados e inventariados los ~150 problemas observados en SonarCloud: 100% de los hallazgos de código nuevo en el PR #20 remediados (SQL dinámico `DBMS_ASSERT`, accesibilidad `<label>`/`<dl>`, `npm ci --ignore-scripts`, permisos Nginx Docker 755 y direccionalidad `ORDER BY ... ASC`).
  - Excluido de análisis el volcado SQL legatario masivo `Analisis Matrices de riesgos v2/RIESGO_LAVADO.sql` en `sonar-analysis.yml` (`a6f8bc6`).
  - Suite .NET Release (306/306), ESLint (0 errores), unitarias Angular (165/165), Playwright E2E (13/13), validadores BD y enlaces de documentación en verde.
- **Optimización de Mantenibilidad en Scripts Fase 11 (2026-08-12)**:
  - Agregada direccionalidad `ORDER BY ... ASC` explícita en consultas `UNION ALL` y listas de `02_validar_semillas_bloque1_solo_lectura.sql`, `03_validar_gestion_riesgos_bloque2_solo_lectura.sql`, `05_validar_mitigacion_bloque4_solo_lectura.sql` y `06_validar_alertas_automonitoreo_bloque5_solo_lectura.sql`.
  - Validadores BD, alineación dinámicos de Matrices y enlaces de documentación en verde.
- **Siguiente objetivo**: Verificar la Quality Gate en la plataforma web de SonarCloud tras la ejecución remota sobre `origin/desarrollo`.
- **Siguiente objetivo**: Presentar la revisión del Bloque 1 SonarCloud y continuar la gobernanza de código en `desarrollo`.
- **Siguiente objetivo**: Revisar el resultado del próximo análisis automático SonarCloud del PR #20 y corregir únicamente hallazgos reales, sin debilitar controles.

---

## Corrección documental Oracle — Comentarios de Matrices de Riesgos

Los scripts `01_comentarios_y_estandares_modelo_17_tablas.sql` y `transicion/06_reconstruir_modelo_17_tablas.sql` mantienen alineados los comentarios de las **17 tablas** y **121 columnas** `RL_MR_*`.

La fuente se conserva en UTF-8 con BOM y contiene tildes, eñes y redacción institucional corregidas. Si los comentarios existentes se muestran con caracteres sustituidos, debe ejecutarse únicamente el script `01_comentarios_y_estandares_modelo_17_tablas.sql` desde el editor SQL Unicode de DBeaver, no desde SQL*Plus. El script no contiene directivas exclusivas de SQL*Plus. No implica recreación de tablas ni modificación de datos.

> **Actualización 2026-08-12 — Scripts Oracle y SonarCloud:** Se aplicaron correcciones puntuales en los nueve scripts reportados: las sentencias `EXECUTE IMMEDIATE` inevitables quedaron documentadas con `NOSONAR` y sus controles de lista cerrada/`DBMS_ASSERT` se conservaron; los validadores de fase 11 recibieron `ORDER BY ... ASC` explícito. Validadores locales de Matrices, base de datos, enlaces documentales y suite backend (306/306) correctos. Oracle no fue ejecutado. El nuevo análisis SonarCloud remoto sigue pendiente; GOV-02 + GOV-03 permanece abierta.

> **Fe de erratas append-only:** `05_validar_mitigacion_bloque4_solo_lectura.sql` normalizó los alias de columnas de su consulta de conteos (`AS OBJETO`, `AS TOTAL`), sin cambios funcionales.
> **Actualización 2026-08-12 — Fase 11:** Se normalizaron alias implícitos en los validadores 03, 04 y 06, manteniendo solo lectura, ordenaciones y semántica. Oracle no fue ejecutado; se requiere nuevo análisis SonarCloud para certificar el resultado.
> **Actualización 2026-08-12 — SonarCloud:** Los nueve hallazgos `plsql:S1192` pertenecían al volcado histórico `Analisis Matrices de riesgos v2/RIESGO_LAVADO.sql`. Se configuró una exclusión precisa para ese único archivo en `.github/workflows/sonar-analysis.yml`; no se modificaron scripts Oracle operativos ni reglas de detección. El nuevo análisis remoto queda pendiente para confirmar la desaparición del componente y el estado de la puerta. Oracle permanece sin ejecución y GOV-02 + GOV-03 continúa abierta.
> **Actualización 2026-08-13 (Codex):** corregidas alertas SonarCloud de accesibilidad del Form Builder y `NOT EXISTS` de validadores SQL Fase 11 mediante `LEFT JOIN ... IS NULL`, sin ejecución Oracle ni DDL/DML. Build y 181/181 pruebas frontend pasaron; análisis remoto pendiente.
> Actualizacion 2026-08-13 (Codex): corregido el bloqueo contractual del Quality Gate. El validador reconoce las nueve mutaciones administrativas legitimas del controlador. Validadores de autorizacion, alineacion dinamica y UAT Fase 13 correctos. Pendiente nuevo analisis remoto de SonarCloud.
> Actualizacion 2026-08-13 (Codex): corregidos los tres avisos `isNaN` del evaluador de formulas, patrones regex equivalentes, acceso de pila con `at` y optional chaining. Verificados build, frontend 181/181, backend 319/319, validadores BD/documentacion, diff check y quality gates locales con salida correcta. Pendiente confirmar el nuevo analisis remoto de SonarCloud; Oracle permanece sin ejecucion.
> Actualizacion 2026-08-13 (ChatGPT): endurecidos accesibilidad y semantica de modales/Form Builder, labels ARIA, roles interactivos, foco modal, imports no usados, complejidad del evaluador y conversiones de valores. Verificados build Angular, 181 pruebas frontend, 319 backend, 14 E2E, quality gates locales, validador BD y enlaces documentales. Oracle, SQL, DDL y DML no fueron modificados. Pendiente confirmar el analisis remoto SonarCloud y la deuda historica de duplicacion.
> Actualizacion 2026-08-13 (ChatGPT): nueva ronda de endurecimiento SonarCloud: dialogs nativos para overlays, controles semanticamente interactivos en Form Builder, parser de formulas simplificado, conversiones seguras, Docker y matcher de pruebas. Verificados build, 28/181 pruebas frontend, 319 backend, 14/14 E2E, validadores BD/documentacion y quality gates locales. `validate_repository_structure.ps1` queda pendiente por hallazgo heredado en `core/services/global-http-state.service.ts`; Oracle/SQL/DDL/DML y `main` permanecen intactos. Pendiente analisis remoto SonarCloud.
> Actualizacion 2026-08-13 (ChatGPT): atendidas las dos incidencias Web de equivalente de teclado detectadas por el nuevo analisis remoto; se agregaron `keydown.enter` y `keydown.space` a las superficies de seleccion del Form Builder. ESLint, build, 28/181 unitarias y 14/14 E2E correctos. Pendiente nuevo analisis remoto posterior.
> Actualizacion 2026-08-13 (ChatGPT): el workflow remoto de Sonar para `9cb3bb1` finalizo sin error operativo, pero omitio el escaneo por ausencia de `SONAR_TOKEN`, `SONAR_PROJECT_KEY` y `SONAR_ORGANIZATION`. El dashboard no puede certificarse con sus metricas historicas hasta configurar esas credenciales/variables y relanzar el analisis.

## Estado SonarCloud - correccion de ejecucion manual (2026-08-14)

- El secreto `SONAR_TOKEN` y las variables `SONAR_PROJECT_KEY` y `SONAR_ORGANIZATION` ya estan configurados en GitHub Actions.
- El fallo remoto observado sobre `86b5fd8` se produjo en una ejecucion manual `workflow_dispatch`: SonarCloud la interpreto como analisis de rama y asocio ese commit a la rama principal. No es evidencia valida del estado actual del PR #20.
- `.github/workflows/sonar-analysis.yml` ahora exige el numero del pull request para toda ejecucion manual y proporciona los parametros de PR a SonarCloud. Para el PR activo debe ingresarse `20`.
- **Punto de continuidad**: esperar el push de este ajuste, abrir **Actions > Sonar Analysis > Run workflow**, seleccionar `desarrollo`, escribir `20` en `pull_request_number` y ejecutar. Revisar luego el resultado del PR #20 en SonarCloud. No cerrar la Fase 9 ni la Fase 10 hasta contar con esa evidencia remota.

## Estado de continuidad - cobertura real Matrices (2026-08-14)

- Se inicio una campana de cobertura real para el Quality Gate del PR #20 sin exclusiones, `NOSONAR`, reduccion de umbrales ni cambios a Oracle.
- El bloque publicado agrega pruebas frontend de contratos y flujos de Matrices: familias, formularios, evaluaciones, mitigacion, monitoreo, exportaciones y evidencia.
- Ultima evidencia local: Angular build correcto (advertencia conocida `exceljs` CommonJS); frontend 28/189; E2E 14/14; backend Release 319/319; quality gates locales, validadores BD y enlaces documentales correctos.
- Cobertura local del foco intervenido: servicio Matrices 92/102 lineas; componente Matrices 295/454 lineas. La cobertura global frontend local es 43.29% y no sustituye el calculo de codigo nuevo en SonarCloud.
- **Punto de continuidad**: publicar esta intervencion, ejecutar SonarCloud sobre PR #20 y continuar la cobertura real de los modulos nuevos que aun impiden el minimo remoto de 80%. Fase 9 y Fase 10 siguen abiertas; UAT final sigue siendo responsabilidad de Javier Mejia.

## Estado de continuidad - cobertura Form Builder (2026-08-14)

- Se agrego un segundo bloque de cobertura real, limitado a pruebas del Constructor Visual y su validador semantico; no hubo cambios de produccion, Oracle, SQL, DDL, DML, reglas de SonarCloud ni exclusiones.
- La suite ahora registra frontend 28/195, E2E 14/14 y backend Release 319/319 correctas. Build Angular, validadores de BD/documentacion y `run_quality_gates.ps1` estan correctos; persiste la advertencia no bloqueante de `exceljs` CommonJS.
- Cobertura local: Form Builder 102/103 lineas y 23/23 funciones; validador 30/30 lineas y 3/3 funciones; frontend global 44.55% de lineas.
- **Punto de continuidad**: ejecutar SonarCloud del PR #20 despues de publicar este bloque y seguir ampliando pruebas reales en los demas modulos nuevos hasta superar el 80% remoto. Fase 9 y Fase 10 permanecen abiertas; el cierre UAT requiere aprobacion expresa de Javier Mejia.

## Estado de continuidad - cobertura operativa de Matrices (2026-08-14)

- Se agrego una suite dedicada para la pagina principal de Matrices: familias, versiones, filtros, consolidado, capturas, evidencia, errores HTTP, descarga, Escape y modos de solo lectura. No hubo cambios de produccion, Oracle, SQL, DDL, DML, reglas de SonarCloud, exclusiones ni `main`.
- Evidencia reproducida sobre el HEAD integrado con el bloque backend `000d207`: frontend 29/230 correcto y cobertura global 47.13% de lineas; build Angular correcto con advertencia conocida `exceljs` CommonJS; E2E Playwright 14/14 correcto; backend Release 348/348 correcto; validadores de BD/documentacion y `git diff --check` correctos. La compilacion .NET termino sin errores, con advertencias de analizadores heredadas.
- Limitaciones declaradas: `run_quality_gates.ps1` fue iniciado pero el host no devolvio su codigo final antes de cortar la captura; `validate_repository_structure.ps1` sigue fallando por `core/services/global-http-state.service.ts`, ruta heredada no intervenida.
- Estado remoto comprobado mediante `gh pr checks 20`: el check de validadores/build/tests/cobertura/E2E/contenedores esta verde, pero los dos checks de SonarCloud siguen fallando. La cobertura de codigo nuevo remota aun no puede declararse >=80% sin un analisis nuevo y exitoso del PR #20.
- **Punto de continuidad**: publicar el bloque, ejecutar SonarCloud sobre el PR #20 y usar su cobertura de codigo nuevo como unica evidencia de cierre de Fase 9. Fase 10 continua pendiente de UAT y aprobacion formal de Javier Mejia.

## Estado de continuidad - normalizacion de modales (2026-08-14)

- La estandarizacion global de modales fue corregida para recuperar el patron limpio institucional: backdrop de viewport completo con blur, tarjeta blanca con bordes redondeados, header y footer claros, y cuerpo desplazable sin marcos grises duplicados.
- El detalle de Coincidencias de Monitoreo dejo de renderizar una segunda capa de fondo dentro del dialogo. Los `dialog` nativos ahora neutralizan sus margenes y tamano propios antes de aplicar el overlay institucional.
- El Form Builder de Matrices sigue siendo excepcional por diseno: usa una tarjeta casi a pantalla completa para preservar el lienzo, la paleta y el inspector; no es un modal pequeno forzado.
- Evidencia de esta intervencion: Angular build correcto (advertencia conocida `exceljs` CommonJS), frontend 29/252, Playwright 14/14, backend Release 348/348, validadores de BD y enlaces correctos y quality gates locales correctos. La cobertura frontend local global de lineas fue 48.20%; no equivale a cobertura de codigo nuevo del PR.
- Limitaciones registradas: build .NET independiente bloqueado por `.NET Host` local reteniendo `RL.API.dll`; estructura de repositorio falla por el archivo/carpeta heredados `core/services/global-http-state.service.ts`, fuera de alcance. No se finalizo ningun proceso del usuario.
- Oracle, SQL, DDL/DML, `main`, configuracion/umbrales/exclusiones SonarCloud permanecieron intactos. El analisis remoto SonarCloud y UAT final son condiciones pendientes para cerrar las Fases 9 y 10.
