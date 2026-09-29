# Estándar institucional de filtros y selectores UI

## Objetivo

Este documento define patrones reutilizables para evitar diferencias de comportamiento entre listados, filtros, paginación y selectores extensos. Las interfaces nuevas deben elegir el patrón más simple que cubra su contrato funcional, sin crear variantes visuales ad hoc.

## Estándar A — Filtro completo de listado

Usar cuando el listado admite búsqueda y al menos un criterio de estado o clasificación.

Composición obligatoria:

1. Campo **Buscar** con consulta de servidor; cuando la búsqueda es reactiva, aplicar debounce de aproximadamente 300 ms.
2. Selector de **Estado** o criterio equivalente, también resuelto en servidor.
3. Acción **Limpiar filtros** que restaure criterios, página 1 y vuelva a consultar.
4. `app-page-size-selector` con el valor real autoritativo de la consulta.
5. `app-data-pagination` con total de registros y navegación de servidor.

Convención por defecto para listados administrativos: `10 / 25 / 50` registros y página inicial `10`, salvo que el dominio documente otro tamaño.

Reglas:

- El valor mostrado en «Mostrar» debe coincidir siempre con `tamanoPagina/registrosPorPagina` realmente enviado al backend.
- Cambiar búsqueda, estado o tamaño reinicia a página 1.
- El total y la página devueltos por servidor son autoritativos.
- No se debe solicitar una página de N registros y luego mostrar más de N por mezcla con una carga no paginada.
- No filtrar únicamente en cliente cuando existe paginación de servidor, porque falsea totales y páginas.
- Identificador recomendado para pruebas: `data-ui-filter-standard="full"`.

Aplicación actual: **Evaluaciones de Riesgo** es la referencia visual; **Gestión de riesgos** adopta este contrato con búsqueda por código/nombre, estado Todos/Activos/Inactivos, limpiar filtros, tamaño y paginación.

## Estándar B — Selector acotado de lista extensa

Usar para listas dinámicas largas, especialmente evaluaciones, riesgos, usuarios u otras entidades donde un `<select>` nativo pueda abrir hacia arriba o fuera del viewport.

Componente canónico: `app-bounded-select`.

Contrato:

- El panel se renderiza **debajo** del control y permanece en flujo normal; no depende de la decisión del navegador de abrir arriba/abajo.
- La lista de opciones tiene límite `max-height: min(14rem, 28dvh)` y scroll interno.
- El panel conserva borde inferior, sombra, contador de opciones y cierre visual completo.
- A partir de 8 opciones se habilita búsqueda interna.
- Escape cierra; click externo cierra; al seleccionar se devuelve el foco al disparador.
- El panel no usa posición `fixed` ni `absolute`, por lo que no cubre el pie institucional.
- Identificadores de prueba: `data-ui-bounded-select`, `data-ui-bounded-select-panel`, `data-ui-bounded-select-scroll`.

Aplicación actual: **Mitigación → Evaluación** y **Monitoreo → Evaluación**.

## Estándar C — Paginación compacta

Usar cuando el listado no necesita búsqueda ni criterios adicionales.

Composición:

- `app-page-size-selector`.
- `app-data-pagination`.
- Total de registros proveniente de servidor.

No convertir este patrón en filtro completo si el dominio no lo necesita; si posteriormente se añade búsqueda/estado, migrar al Estándar A en lugar de agregar controles aislados.

## Regla transversal de consistencia

`app-page-size-selector` usa binding controlado con `ngModel` standalone para reflejar el valor real recibido. No se debe volver al patrón de `[value]` sobre un `<select>` cuyos `<option>` se generan dinámicamente, porque puede dejar visible la primera opción aunque la consulta real esté usando otro tamaño.

## Verificación mínima requerida

Para cualquier nueva interfaz que implemente estos patrones:

- Unit test del estado inicial y de cambios de filtro/tamaño.
- Contrato de servidor para búsqueda/estado cuando aplique.
- E2E que confirme cantidad inicial mostrada y parámetros enviados.
- Para selectores extensos: E2E con suficientes opciones para forzar scroll y comprobar que el panel nace debajo del disparador y queda acotado al viewport.
