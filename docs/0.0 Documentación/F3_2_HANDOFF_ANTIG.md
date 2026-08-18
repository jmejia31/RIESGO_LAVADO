# F3.2 — Handoff técnico a Antigravity

## Objetivo

Cerrar los residuos productivos identificados por ChatGPT durante **F3.2 — Cierre funcional y semántico de la Tabla de Evaluaciones**, sin adelantar F4 ni modificar Oracle.

## Baseline verificado

- Rama: `desarrollo`.
- HEAD auditado antes de este handoff: `d9ba2928c9c1f680383877ebdeb3de1cc781d5c4`.
- PR #20: debe permanecer Draft / no merged.
- `main`: fuera de alcance.
- Oracle / SQL: **0 cambios y 0 ejecuciones** para este handoff.

## Hallazgos confirmados

### F3.2-H01 — Normalización incompleta del contrato paginado

Archivo:

`frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts`

La carga visible usa actualmente:

```ts
this.evaluaciones.set(paginado.items || []);
```

Esto protege `null`/`undefined`, pero no un valor truthy inválido. El contenedor operativo ya usa `Array.isArray(...)`; la tabla visible debe aplicar la misma defensa para impedir que `evaluaciones` deje de ser Array y evitar una regresión del fallo histórico `filter is not a function`.

### F3.2-H02 — Metadatos paginados obsoletos después de error

En el `error` de `cargarEvaluaciones()` se limpia `evaluaciones`, pero no `totalRegistros` ni `totalPaginas`. Una consulta fallida posterior a una carga válida puede dejar en pantalla totales de una respuesta que ya no está vigente.

### F3.2-H03 — KPI global semánticamente incorrecto

Archivo:

`frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html`

La tarjeta **Total Evaluaciones** muestra `evaluaciones().length` y afirma `Total consolidado del módulo`, aunque la tabla usa paginación server-side. La metadata correcta disponible para el total de la consulta es `totalRegistros()`.

Los KPIs **En Borrador**, **En Revisión** y **Aprobadas** cuentan exclusivamente el Array de la página visible. El API actual no ofrece agregados globales para los tres estados: no fabricar totales ni disparar consultas adicionales. Debe declararse explícitamente que esos tres conteos pertenecen a la página visible.

## Cambios exactos solicitados a AntiG

### 1. `cargarEvaluaciones()` — defensa de Array y metadata coherente

En `next`:

```ts
const items = Array.isArray(paginado?.items) ? paginado.items : [];
this.evaluaciones.set(items);
this.totalRegistros.set(Number.isFinite(paginado?.totalRegistros) ? Math.max(0, paginado.totalRegistros) : 0);
this.totalPaginas.set(Number.isFinite(paginado?.totalPaginas) ? Math.max(0, paginado.totalPaginas) : 0);
```

Mantener `cargandoEvaluaciones.set(false)`.

En `error`, además de conservar el mensaje real y limpiar filas:

```ts
this.evaluaciones.set([]);
this.totalRegistros.set(0);
this.totalPaginas.set(0);
this.cargandoEvaluaciones.set(false);
```

No silenciar el error ni convertir un error HTTP en respuesta exitosa vacía.

### 2. KPI — semántica verdadera

En `matrices-riesgos.component.html`:

- **Total Evaluaciones** debe renderizar `totalRegistros()`.
- Sustituir `Total consolidado del módulo` por un texto que no mienta cuando existan filtros, por ejemplo: `Total según la consulta actual`.
- Mantener los valores de Borrador/Revisión/Aprobadas calculados sobre `evaluaciones()` únicamente si el texto visible deja explícito `Página visible` / `En la página actual`.
- No crear un endpoint nuevo, no agregar tres consultas por estado y no tocar backend salvo que aparezca evidencia técnica nueva que lo haga imprescindible.

### 3. Completar los dos TODO de la suite F3

Archivo:

`frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.f3.spec.ts`

Convertir en pruebas ejecutables los dos `it.todo` dejados por ChatGPT:

1. `items` truthy no-array se normaliza a `[]` y un error posterior resetea `totalRegistros` / `totalPaginas` a `0`.
2. `Total Evaluaciones` renderiza `totalRegistros` y los tres KPIs por estado declaran expresamente que son conteos de página visible.

No reducir ni eliminar las pruebas F3.2 ya agregadas.

## QA requerida en el checkout local de AntiG

Después de aplicar los cambios:

1. `npm test -- --watch=false`
2. `npm run build`
3. `git diff --check`
4. QA gráfica autenticada en `Matrices de Riesgos -> Evaluaciones`:
   - 9 columnas visibles;
   - código y nombre de riesgo correctos;
   - versión correcta;
   - seis estados y badges;
   - VRI / VRR, incluido cero y ausencia;
   - BAJO / MEDIO / ALTO / CRITICO / ausencia;
   - fecha;
   - Ver siempre, Editar solo BORRADOR, Seguimiento disponible;
   - estado vacío;
   - loading/error/reintento;
   - total global coherente con metadata server-side;
   - KPIs por estado identificados como página visible;
   - DevTools Console sin `filter is not a function`;
   - Network sin la precarga redundante `registrosPorPagina=200` al entrar a Matriz/Evaluaciones.
5. Publicar todo en `origin/desarrollo`.
6. Actualizar `BITACORA_COLABORACION.md` y `docs/0.0 Documentación/ESTADO_COLABORACION.md` con SHA, conteos reales y QA ejecutada.

## Criterio de retorno a ChatGPT

AntiG debe devolver un commit publicado en `desarrollo` con los cambios anteriores y evidencia real de pruebas/QA. ChatGPT verificará el commit remoto, la suite/CI y cerrará F3.2 antes de pasar a F3.3.
