# UI-FORM-V2 — Handoff de preparación para AntiG

## Propósito

Esta carpeta prepara la evolución visual del **Constructor de Formularios Dinámicos** sin reescribir ni duplicar la lógica funcional ya certificada.

El componente productivo vigente continúa siendo:

- `../form-builder.component.ts`
- `../form-builder.component.html`
- `../form-builder.component.scss`

Los archivos `workspace/*` son **shells presentacionales sin HTTP, persistencia ni reglas de negocio**. AntiG debe migrar el HTML/UX existente por zonas y conectar estos shells al `FormBuilderComponent` actual, no crear un segundo motor de formularios.

## Decisión de arquitectura

No iniciar desde cero el motor actual. Deben preservarse:

- `normalizarJsonABuilderModel` y `serializarBuilderModelAJson`;
- `metadatosOriginales` y la preservación lossless de propiedades no editadas;
- CRUD e integridad referencial de catálogos;
- normalización de aliases de tipos;
- validación del builder;
- JSON técnico;
- `soloLectura`;
- `guardarJson` y `cerrar` como contrato con el componente padre;
- persistencia autoritativa `PUT -> GET` y comparación semántica existente en `MatricesRiesgosComponent`.

## Backend / Oracle

**No se requiere cambio de backend ni de base de datos para UI-FORM-V2.**

El backend actual ya expone y protege:

- `GET /api/matrices-riesgos/formularios/{id}`;
- `POST /api/matrices-riesgos/formularios/borrador`;
- `PUT /api/matrices-riesgos/formularios/{id}`;
- `POST /api/matrices-riesgos/formularios/{id}/clonar`;
- `POST /api/matrices-riesgos/formularios/{id}/publicar`;
- `PUT /api/matrices-riesgos/formularios/{id}/estado`;
- `DELETE /api/matrices-riesgos/formularios/{id}`;
- historial y versión vigente por familia.

El cuerpo de la definición continúa siendo JSON genérico. UI-FORM-V2 no debe solicitar tablas, columnas, migraciones, DDL/DML, DTO de presentación ni endpoints decorativos.

## Regiones definitivas

1. `FormBuilderToolbarV2Component`: identidad de versión, modo, navegación y acciones permitidas.
2. `FormBuilderPaletteV2Component`: biblioteca de campos en edición; árbol/estructura en solo lectura.
3. `FormBuilderCanvasV2Component`: secciones, Field Cards, selección, orden y drop zones.
4. `FormBuilderInspectorV2Component`: inspector contextual.
5. `FormBuilderStatusbarV2Component`: estado de cambios y acciones persistentes.
6. `FormBuilderWorkspaceV2Component`: composición responsive de las cinco regiones.

## Inspector: fuente de verdad

AntiG solo debe exponer propiedades ya soportadas por el `FormBuilderModel` o incorporadas al modelo mediante una ampliación frontend con prueba de round-trip lossless.

### General

- clave/identificador según contrato actual;
- etiqueta;
- descripción;
- obligatorio;
- solo lectura;
- placeholder;
- texto de ayuda.

### Datos

- catálogo;
- opciones;
- fórmula, cuando el tipo lo soporte.

### Presentación

- ancho de campo (`anchoColumnas`);
- columnas de sección (`columnasPorFila`).

### Reglas avanzadas

No inventar `visibleCuando`, `obligatorioCuando`, rangos, regex u otras propiedades en UI hasta comprobar su forma real en el contrato JSON y agregar primero pruebas de normalización/serialización. Si ya existen como metadatos originales, deben seguir preservándose aunque todavía no sean editables visualmente.

## Modos

### DRAFT editable

- biblioteca activa;
- edición de secciones/campos/catálogos;
- inspector editable;
- guardar borrador habilitado según validación.

### Solo lectura

- no mostrar controles que aparenten edición;
- la zona izquierda cambia conceptualmente de **Agregar campos** a **Estructura del formulario**;
- inspector informativo;
- sin Guardar, drag & drop ni edición de JSON.

## Fases de implementación AntiG

- **UI-FORM.1**: workspace, toolbar y responsive, sin cambiar negocio.
- **UI-FORM.2**: biblioteca/estructura y búsqueda local.
- **UI-FORM.3**: lienzo, Field Cards, selección y secciones.
- **UI-FORM.4**: inspector General/Datos/Presentación con propiedades actuales.
- **UI-FORM.5**: coherencia editable/solo lectura, dirty state y acciones.
- **UI-FORM.6**: vista previa y refinamiento de JSON técnico.
- **UI-FORM.QA**: accesibilidad, teclado, responsive, regresión, round-trip y E2E.

Cada fase debe conservar verdes los tests existentes antes de retirar markup legacy de la región correspondiente.

## Regla de migración

No eliminar el markup vigente por anticipado. Migrar una región, conectar sus eventos al estado existente, validar, y únicamente después retirar la región legacy equivalente. El cierre final puede simplificar `form-builder.component.html` cuando todas las regiones V2 estén integradas y certificadas.
