# Estado de colaboración y punto de continuidad

> Actualización 2026-08-10: **FE-03 + FE-04 — Accesibilidad / WAI-ARIA + Skeleton Loaders** quedó implementada y certificada técnicamente en `desarrollo`. Se incorporaron semántica y navegación accesible transversal, gestión de foco para SPA, estados `aria-busy`/regiones vivas sin competir con los mensajes funcionales `role="status"`, foco visible, reducción de movimiento y un componente reusable de skeleton loader. El HEAD técnico `59757b3af5cf5ad89c841ee0f7a7d93b8fc0e0fc` fue certificado por Quality Gates Run `31420468597` (#647) en **SUCCESS**: FE-03/FE-04 Validator correcto, Backend 304/304, Frontend 165/165 en 26 archivos, E2E 13/13, build Release 0 errores/0 advertencias y `npm audit` 0 vulnerabilidades. No se modificaron Backend, contratos API, Oracle ni datos.

Documento vivo. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria:** `desarrollo`
- **Base FE-03 + FE-04:** `a0793fe8d56b09be6bdfb4caf022e5acdd07fbcc`
- **HEAD técnico FE-03 + FE-04 certificado:** `59757b3af5cf5ad89c841ee0f7a7d93b8fc0e0fc`
- **Rama estable:** `main`
- **HEAD de `main`:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **PR #20:** debe permanecer abierto, en borrador y sin fusión
- **Modelo Matrices:** 17 tablas `RL_MR_*` + 17 secuencias
- **DB-03:** cerrado físicamente; 11 planes ejecutados; sin índices nuevos
- **DB-01:** política y controles de repositorio completados; sin purga automática
- **FE-03 + FE-04:** completado y certificado técnicamente

---

## 2. FE-03 — Accesibilidad / WAI-ARIA

### Semántica y navegación

1. El documento principal declara `lang="es-HN"`.
2. Se incorporó skip-link: **Saltar al contenido principal**.
3. El layout identifica navegación principal y módulos mediante landmarks y etiquetas accesibles.
4. El contenido principal tiene `id="contenido-principal"` y `tabindex="-1"` para gestión programática de foco sin introducir `tabindex` positivo.
5. Las rutas activas anuncian `aria-current="page"` mediante `ariaCurrentWhenActive`.
6. El control del sidebar expone `aria-controls`, `aria-expanded` y etiqueta dinámica.
7. Los enlaces del sidebar colapsado conservan nombre accesible para tecnologías asistivas.
8. Los íconos puramente decorativos se excluyen del árbol accesible con `aria-hidden="true"` y `focusable="false"`.
9. El botón de cierre de sesión tiene nombre accesible explícito.
10. Los errores globales conservan `role="alert"` y anuncio asertivo.

### Gestión de foco SPA

- Cada activación del `router-outlet` reubica el foco en `#contenido-principal` mediante `focus({ preventScroll: true })`.
- Esto permite que navegación por teclado/lector de pantalla perciba el cambio de vista sin depender del puntero.
- No se introdujo ningún `tabindex` positivo.

### Foco visible y movimiento

- Se añadió estilo global `:focus-visible` de alto contraste.
- Se respeta `prefers-reduced-motion: reduce`:
  - scroll suave desactivado;
  - transiciones/animaciones reducidas;
  - skeleton sin animación.

### Estados dinámicos

- `<main>` expone `[attr.aria-busy]="globalState.cargando()"`.
- El indicador global de carga utiliza región viva `aria-live="polite"` + `aria-atomic="true"`.
- Los estados funcionales existentes que usan `role="status"` se preservan sin interferencia.

---

## 3. FE-04 — Skeleton Loaders

### Componente reusable

Ubicación:

`frontend/rl-app/src/app/shared/components/skeleton-loader/skeleton-loader.component.ts`

Variantes disponibles:

- `content`
- `table`
- `cards`
- `form`

Características:

- standalone component;
- `ChangeDetectionStrategy.OnPush`;
- filas configurables y limitadas entre 1 y 12;
- geometría del skeleton marcada como decorativa con `aria-hidden="true"`;
- etiqueta accesible `sr-only`;
- `aria-live="polite"`, `aria-atomic="true"` y `aria-busy="true"`;
- no utiliza `role="status"`, para no competir con confirmaciones funcionales;
- animación visual controlada por CSS;
- modo estático cuando el usuario solicita reducción de movimiento.

### Integración transversal

El skeleton se integra al `MainLayoutComponent` utilizando el estado HTTP global ya existente (`GlobalHttpStateService`). No se duplicó lógica de carga ni se alteraron interceptores o contratos HTTP.

### Pruebas unitarias

`frontend/rl-app/src/app/shared/components/skeleton-loader/skeleton-loader.component.spec.ts`

Cubre:

1. región viva accesible sin colisión con estados funcionales;
2. número de filas solicitado;
3. límites seguros de filas 1..12.

---

## 4. Hallazgo de regresión y corrección

La primera certificación candidata sobre `d1515471185bd3fd5f58abac9f5762a6b0cc6017` detectó un problema semántico en Quality Gates Run `31420010414` (#645):

- Backend, build, unit tests y validador FE-03/FE-04 estaban correctos;
- dos E2E fallaron porque los nuevos indicadores de carga habían agregado dos `role="status"` globales;
- los selectores accesibles existentes `getByRole('status')` dejaron de ser únicos cuando coexistían con mensajes funcionales como `Estado actualizado correctamente.` y `Versión clonada como borrador.`.

Corrección aplicada:

- las regiones de carga mantienen `aria-live`, `aria-atomic` y `aria-busy`;
- se retiró `role="status"` exclusivamente de la infraestructura nueva de carga;
- se preservaron intactos los `role="status"` funcionales existentes;
- el validador FE-03/FE-04 ahora bloquea explícitamente una futura reintroducción de esa colisión.

Resultado final: Quality Gates #647 volvió a **SUCCESS** con E2E 13/13.

---

## 5. Validador bloqueante FE-03 + FE-04

Ubicación:

`scripts/validation/validate_fe03_fe04_accessibility_loading.ps1`

Quality Gates verifica:

- `lang="es-HN"`;
- skip-link;
- landmark principal y foco programático;
- `aria-busy` global;
- contrato accesible del sidebar;
- ruta activa anunciable;
- región viva de carga sin colisión de `role="status"`;
- skeleton transversal;
- skeleton accesible y decorativo para lector de pantalla;
- foco visible;
- `prefers-reduced-motion`;
- animación skeleton controlada;
- ausencia de `tabindex` positivo.

El workflow `.github/workflows/quality-gates.yml` ejecuta este validador como puerta bloqueante.

---

## 6. Evidencia CI FE-03 + FE-04

**Quality Gates Run:** `31420468597` (#647) — **SUCCESS**

- FE-03/FE-04 Validator: **CORRECTO**.
- Build Release: **0 errores / 0 advertencias**.
- Backend: **304/304** pruebas aprobadas.
- Frontend: **165/165** pruebas aprobadas en 26 archivos.
- Skeleton loader: **3/3** pruebas aprobadas.
- E2E Playwright: **13/13** aprobadas.
- `npm audit`: **0 vulnerabilidades**.
- Cobertura Backend: **22.19% líneas / 24.83% ramas**.
- Cobertura Frontend: **39.92% sentencias / 35.65% ramas / 36.10% funciones / 39.48% líneas**.
- Inventario exacto Matrices: **17 tablas / 17 secuencias**.
- Contrato autorización/UAT Matrices: **correcto**.

### Alcance no modificado

- **NO** se modificó Backend funcional.
- **NO** se alteraron endpoints ni contratos API.
- **NO** se conectó ni ejecutó Oracle durante FE-03/FE-04.
- **NO** se ejecutó DDL/DML.
- **NO** se ejecutaron scripts 05/06.
- **NO** se modificaron `B10_*`.
- **NO** se modificó Producción.

---

## 7. Estado consolidado del Plan de Mejoras Integrales

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
| 9 | FE-01 — Signals gradual | **Siguiente** |
| 10 | GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa | Pendiente |

---

## 8. Directrices activas

1. Trabajar exclusivamente sobre `desarrollo`.
2. No modificar/fusionar `main` sin autorización expresa de Javier Mejía.
3. Mantener PR #20 abierto y en borrador; no auto-merge.
4. No ejecutar transición 05/06 ni modificar/eliminar `B10_*`.
5. No versionar secretos ni cadenas de conexión.
6. Mantener HTML semántico como primera opción; usar ARIA únicamente cuando agrega información que el HTML nativo no expresa.
7. No introducir `tabindex` positivo.
8. No reutilizar `role="status"` para infraestructura global de carga si puede coexistir con mensajes funcionales que ya usan ese rol.
9. Todo estado de carga animado debe respetar `prefers-reduced-motion`.
10. Mantener el componente skeleton independiente de la lógica de negocio y del contrato HTTP.
11. La bitácora histórica es append-only; las correcciones se agregan, no reescriben entradas anteriores.

---

## 9. Punto exacto de continuación

**FE-03 + FE-04 queda cerrada técnicamente con accesibilidad transversal, skeleton loaders reutilizables y regresión UAT completa en verde.**

La siguiente fase de la secuencia aprobada es:

### FE-01 — Adopción gradual de Angular Signals

Debe realizarse de forma incremental, priorizando estado local y derivado donde Signals aporte claridad/rendimiento, sin reescritura masiva ni alteración de contratos API/servicios estables.
