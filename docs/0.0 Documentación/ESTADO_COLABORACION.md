# Estado de colaboración y punto de continuidad

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

## 6. Validador bloqueante

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

## 7. Dossier técnico

`docs/0.0 Documentación/FE_01_ADOPCION_GRADUAL_ANGULAR_SIGNALS_2026-08-10.md`

Documenta estrategia, límites, primera ola, migraciones, criterios de aceptación y continuidad.

---

## 8. Evidencia CI FE-01

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
2. No modificar/fusionar `main` sin autorización expresa de Javier Mejía.
3. Mantener PR #20 abierto y en borrador; no auto-merge.
4. No ejecutar transición 05/06 ni modificar/eliminar `B10_*`.
5. No versionar secretos ni cadenas de conexión.
6. Usar Signals para estado local/derivado cuando simplifique el modelo; no forzar su uso sobre flujos RxJS naturalmente asíncronos.
7. Mantener `OnPush` en las superficies protegidas por FE-01.
8. No reintroducir estado mutable paralelo al Signal de Login/Carga de Listas.
9. No convertir futuras adopciones de Signals en reescrituras masivas sin evidencia técnica y regresión completa.
10. La bitácora histórica es append-only; las correcciones se agregan, no se reescriben entradas anteriores.

---

## 12. Punto exacto de continuación

**FE-01 queda cerrada técnicamente con adopción gradual de Signals, primera ola `OnPush`, contratos existentes preservados y regresión completa en verde.**

La siguiente fase aprobada es:

### GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa

Debe elevar análisis estático, observabilidad de calidad y reproducibilidad de empaquetado sin modificar Producción ni fusionar `main`.
