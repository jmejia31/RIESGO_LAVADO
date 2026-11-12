# B04 — Estándar institucional de menús y selectores

## Estado de la intervención

`PHASE=B04_MENUS_AND_SELECTORS`
`REFERENCE_SHA=5574d4b2c215cf4cebf707f73d77218139f9c46c`
`B04_IMPLEMENTATION_AUTHORIZED=YES`
`B04_OWNER_VISUAL_ACCEPTANCE=PENDING`

B04 centraliza el comportamiento compartido de selectores extensos, menús de acciones y posicionamiento de capas sin reemplazar controles nativos pequeños que ya son compatibles con Angular Forms y el contrato visual institucional.

## Inventario estructural inicial

Auditoría ejecutada sobre `frontend/rl-app/src/app`, excluyendo pruebas y artefactos generados:

| Métrica | Resultado |
|---|---:|
| Selectores totales | 64 |
| `<select>` nativos | 49 |
| `app-bounded-select` | 3 |
| `app-page-size-selector` | 12 |
| Menús de acciones migrados al componente compartido | 2 |
| Menús contextuales independientes pendientes | 0 |
| `<details>/<summary>` de menú sin estandarizar | 0 |
| Componentes sin clasificar | 0 |

Clasificación:

- `NATIVE_SELECT_STANDARD`: 49 controles cortos, estáticos o ligados a Reactive Forms/`ngModel`; conservan su semántica nativa y el valor original.
- `BOUNDED_SELECT_REQUIRED`: 3 selectores largos o dinámicos en Matrices; conservan búsqueda, scroll, selección tipada, Escape, clic externo y contención de viewport.
- `NATIVE_PAGE_SIZE_STANDARD`: 12 controles centralizados por `app-page-size-selector`; no se alteran opciones ni tamaños autoritativos.
- `ACTION_MENU_REQUIRED`: 2 superficies del Form Builder, ahora atendidas por `app-institutional-action-menu`.
- `CONTEXT_MENU_REQUIRED`: 0 patrones independientes después de la migración del menú de acciones de sección.

Los `<select>` nativos no se migraron indiscriminadamente: hacerlo habría cambiado contratos `formControlName`, `ngModel`, valores numéricos, estados disabled y validaciones sin beneficio funcional. La excepción queda justificada por dominio y está cubierta por el inventario.

## Componentes compartidos

### `app-bounded-select`

Es el selector canónico para listas largas. Mantiene:

- `role="combobox"`, `aria-haspopup="listbox"`, `aria-expanded`, `aria-controls` y `aria-activedescendant`.
- Búsqueda interna desde el umbral configurado.
- Valores `string`, `number` y `null` sin conversión silenciosa.
- Opciones deshabilitadas.
- Navegación con flechas y confirmación con Enter.
- Cierre por Escape, clic externo, selección y destrucción.
- Reposicionamiento al hacer scroll, resize y cambios del viewport visual.
- Apertura arriba/abajo, ancho acotado y altura máxima con scroll interno.

### `app-page-size-selector`

Se conserva como fuente única de paginación compacta. B04 no cambia opciones, tamaños de página, consultas ni contratos de servidor.

### `app-institutional-action-menu`

Nuevo componente compartido para acciones secundarias y contextuales. Recibe el contenido funcional por proyección y no contiene reglas de negocio. Centraliza:

- Apertura y alternancia idempotente.
- Una sola capa activa mediante `InstitutionalOverlayRegistry`.
- Clic externo y Escape.
- Retorno de foco al disparador.
- `role="menu"`, `role="menuitem"`, `aria-expanded`, `aria-controls` y `aria-haspopup`.
- Navegación Home/End/ArrowUp/ArrowDown.
- Cierre después de seleccionar una acción.
- Posicionamiento compartido, contención horizontal y altura máxima.
- Listeners de viewport liberados al cerrar o destruir.

### Posicionamiento

`institutional-overlay-position.util.ts` es la fuente común para calcular dirección, límites, ancho, alto y coordenadas de las capas. El selector extenso y el menú de acciones utilizan la misma función; no existen algoritmos paralelos para cada consumidor.

## Migraciones realizadas

| Superficie | Antes | Después | Estado |
|---|---|---|---|
| Form Builder — toolbar | `<details>/<summary>` con popover local | `app-institutional-action-menu` | `STANDARDIZED` |
| Form Builder — acciones de sección | estado local, `@HostListener` y `position:absolute` | `app-institutional-action-menu` y registro compartido | `STANDARDIZED` |
| Matrices — Evaluación/Riesgo/Mitigación | `app-bounded-select` existente | posicionamiento compartido y registro de capa | `STANDARDIZED` |
| Paginación administrativa | `app-page-size-selector` existente | preservado | `ALREADY_COMPLIANT` |
| Selectores nativos cortos | `<select>` compatible | preservados con contrato de formulario | `JUSTIFIED_EXCEPTION` |

No se modificaron roles, permisos, backend, Oracle, fórmulas, cálculos, auditoría, autenticación ni datos institucionales.

## Accesibilidad, responsive y rendimiento

- Foco visible mediante el contrato global de `:focus-visible`.
- No se crean consultas HTTP al abrir o cerrar capas.
- Se evita la acumulación de listeners con instalación/remoción explícita.
- La capa se recalcula durante scroll y resize.
- Los paneles tienen scroll interno y no exceden el viewport disponible.
- Los formularios Angular existentes conservan `formControlName`, `ngModel`, `disabled`, validaciones y tipos.
- Las acciones siguen siendo icon-only con `aria-label` y `title` institucionales.

## Validador preventivo

`tools/validate_b04_menus_selectors.js` verifica:

- Inventario de selectores, paginadores y menús.
- Ausencia de `<details>/<summary>` no migrados.
- Presencia de contratos ARIA y cierre del selector acotado.
- Presencia del registro compartido, posicionamiento y cierre del menú institucional.
- Conteo de componentes sin clasificar.

Ejecutar:

```powershell
node tools/validate_b04_menus_selectors.js --self-test
node --test tools/validate_b04_menus_selectors.test.js
node tools/validate_b04_menus_selectors.js
```

## Regresión y límites

Pruebas frescas de esta intervención:

- `npm run build`: `PASS`, con warnings preexistentes de presupuesto/CommonJS.
- `npm test -- --watch=false`: `94` archivos y `984` pruebas `PASS`.
- `npm run lint`: `PASS`.
- Validador B04 y self-test: `PASS`; inventario `64` selectores, `2` menús, `0` sin clasificar.

Playwright real, Oracle institucional y pruebas de infraestructura requieren el runner/datos autorizados y se registran como pendientes hasta la ejecución remota correspondiente. B04 no declara aceptación visual del propietario.

## Restricciones preservadas

`ORACLE_INSTITUTIONAL_TOUCHED=NO`
`PRODUCTION_TOUCHED=NO`
`MAIN_TOUCHED=NO`
`MIGRATION_54_TOUCHED=NO`
`P06_PRESERVED=YES`
`P07_PRESERVED=YES`
`BLOCK10_AUTHORIZED=NO`

`B04_GLOBAL_INVENTORY=COMPLETE`
`B04_TECHNICAL_STATUS=PASS`
`B04_OWNER_VISUAL_ACCEPTANCE=PENDING`
`B04_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE`
