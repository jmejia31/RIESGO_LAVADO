# FE-01 — Adopción gradual de Angular Signals

**Fecha:** 2026-08-10  
**Repositorio:** `jmejia31/RIESGO_LAVADO`  
**Rama:** `desarrollo`  
**Base funcional de la fase:** `7d7b9f093a881154e7f5d2373d393cc0ffef31f9`  
**Producción:** fuera de alcance  

## 1. Objetivo

Adoptar Angular Signals de forma incremental para estado síncrono de presentación y estado derivado, mejorando claridad y granularidad del cambio de detección sin ejecutar una reescritura masiva ni modificar contratos HTTP, API, Backend u Oracle.

FE-01 no pretende reemplazar RxJS donde su modelo asíncrono sigue siendo natural ni sustituir Reactive Forms. El objetivo es establecer y proteger un patrón de estado frontend coherente.

## 2. Línea base observada

Antes de FE-01 el frontend ya utilizaba Signals de manera significativa:

- `AuthService`: sesión con `signal`, estados derivados con `computed` y ciclo de inactividad con `effect`.
- `GlobalHttpStateService`: solicitudes activas con `signal` y estado global de carga con `computed`.
- `MainLayoutComponent`: sidebar, módulos y navegación derivados mediante `signal`/`computed`.
- `SinAccesoComponent`: conversión Observable→Signal con `toSignal`.
- `ConfiguracionComponent` y `BitacoraComponent`: múltiples estados locales ya signalizados.
- `MatricesRiesgosComponent`: estado local y derivado ampliamente basado en Signals y `ChangeDetectionStrategy.OnPush`.

Por ello, una migración global habría añadido riesgo sin beneficio proporcional.

## 3. Estrategia aprobada

### Signals

Usar para:

- estado local consumido por templates;
- estado síncrono derivado;
- selección UI;
- flags y colecciones locales cuya mutación debe ser explícita.

### RxJS

Se conserva para:

- `HttpClient` y operaciones asíncronas;
- interceptores y pipelines HTTP;
- flujos donde cancelación, composición temporal o operadores RxJS aportan valor.

### Reactive Forms

Se conserva para formularios existentes. FE-01 no introduce una migración experimental de formularios a Signals ni modifica validadores funcionales.

## 4. Primera ola FE-01

Se migran a `ChangeDetectionStrategy.OnPush` superficies que ya estaban sustentadas principalmente por Signals:

1. `App`.
2. `MainLayoutComponent`.
3. `SinAccesoComponent`.
4. `ConfiguracionComponent`.
5. `BitacoraComponent`.
6. `LoginComponent`.
7. `CargarListasComponent`.

La migración evita alterar servicios de datos y el módulo central de Matrices, cuya adopción previa de Signals/OnPush ya era adecuada.

## 5. Login — carrusel

El carrusel mantenía una mezcla de estado reactivo e imperativo:

- `slideActual` ya era Signal;
- `slides` era `any[]` mutable;
- el slide visible se resolvía repetidamente desde el template;
- el temporizador utilizaba `any`.

FE-01 normaliza el patrón:

- `slides` → `signal<LoginSlide[]>([])`;
- `slideSeleccionado` → `computed(...)`;
- temporizador tipado `ReturnType<typeof setInterval> | null`;
- lectura del template mediante `slides()` y `slideSeleccionado()`;
- `track` estable por `slide.id`;
- protección ante colección vacía, una sola diapositiva e índices fuera de rango.

No se altera el contrato de `ConfiguracionService.ObtenerSlides()`.

## 6. Carga de Listas — archivo seleccionado

`archivoSeleccionado` era un campo mutable paralelo al resto del estado signalizado del componente.

FE-01 lo convierte en:

`signal<File | null>(null)`

La lógica de carga obtiene una instantánea local no nula antes de iniciar la operación HTTP. El servicio, formato permitido, formulario, endpoint y flujo funcional permanecen intactos.

## 7. Change Detection

La primera ola utiliza `ChangeDetectionStrategy.OnPush` porque las superficies seleccionadas ya exponen estado mediante Signals o inputs/eventos compatibles con el modelo OnPush.

FE-01 no realiza una sustitución ciega de `Eager` en todo el repositorio. Cada componente futuro debe migrarse solo cuando su fuente de estado sea compatible y exista evidencia de regresión en verde.

## 8. Validador bloqueante

Se incorpora:

`scripts/validation/validate_fe01_signals_adoption.ps1`

El validador protege:

- `OnPush` en los componentes de la primera ola;
- carrusel de Login tipado con Signals y estado derivado con `computed`;
- archivo seleccionado de Carga de Listas como Signal;
- adopciones previas de Signals en Auth, estado HTTP, layout, Sin Acceso y Matrices;
- ausencia de `BehaviorSubject` como sustituto de estado local en las superficies protegidas;
- ejecución del propio validador desde Quality Gates.

## 9. Criterios de aceptación

FE-01 se considera técnicamente cerrada cuando el HEAD final de la fase cumpla simultáneamente:

1. validador FE-01 correcto;
2. build Release sin errores ni advertencias del Backend;
3. suite Backend completa en verde;
4. suite Frontend completa en verde;
5. E2E Playwright completa en verde;
6. `npm audit` sin vulnerabilidades;
7. validadores FE-03/FE-04, Oracle, Matrices, autorización y UAT sin regresiones;
8. PR #20 abierto y en borrador;
9. `main` sin cambios;
10. workflow temporal de migración retirado.

## 10. Restricciones preservadas

- No modificar ni fusionar `main`.
- No modificar Producción.
- No modificar Backend funcional ni contratos API como parte de FE-01.
- No conectar ni ejecutar Oracle.
- No ejecutar DDL/DML.
- No ejecutar scripts de transición 05/06.
- No modificar/eliminar `B10_*`.
- No versionar secretos.
- No convertir FE-01 en una reescritura masiva del frontend.

## 11. Continuidad

Una vez certificada FE-01, la siguiente fase del Plan de Mejoras Integrales es:

**GOV-02 + GOV-03 — Analyzers/Sonar + Docker multietapa.**
