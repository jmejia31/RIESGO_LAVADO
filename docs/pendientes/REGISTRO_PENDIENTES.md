# Registro central de pendientes — RIESGO_LAVADO

**Rama de referencia:** `desarrollo`  
**Creado:** 2026-11-09 13:59:19 (America/Tegucigalpa)  
**Estado:** REGISTRO DOCUMENTAL; NO ES ORDEN DE IMPLEMENTACION.

## Propósito y controles

Este registro concentra pendientes que deberán evaluarse en un próximo plan de implementación. No sustituye los inventarios históricos de `docs/pendientes/`, el estado colaborativo ni la cola ejecutable `docs/automatizacion/TAREAS_CODEX.json`. **Ninguna entrada de este documento autoriza ejecución automática, modificación de código, ejecución de Oracle, repetición de certificaciones ni apertura del Bloque 10.**

**Línea base de referencia:** P06 real autenticado `6/6 PASS` sobre `SOURCE_CODE_SHA=4872c537324c51982fb613808526a7f8743a6762`, Quality Gate `42682601643` `SUCCESS`; evidencia registrada en el commit documental `758647d4c8fb9d3a7b871b198f1dfa8284014bf2`. La certificación P06 obtenida no cambia de estado por registrar mejoras diferidas.

| ID | Asunto | Estado | Momento de revisión | Autorización de implementación |
| --- | --- | --- | --- | --- |
| `PEND-P06-NAV-001` | No aceptar `/home` como destino final de login autenticado | `PENDIENTE_PLANIFICACION` | Próximo plan de implementación | NO |
| `PEND-P06-NAV-002` | No aceptar `/sin-acceso` como éxito cuando existen módulos autorizados | `PENDIENTE_PLANIFICACION` | Próximo plan de implementación | NO |

## PEND-P06-NAV-001 — Validar el destino final autorizado; `/home` es transitorio

**Hallazgo:** la función compartida `login(page)` en `frontend/rl-app/e2e/p06-qa-real-auth.spec.ts` permite que la expectativa de URL posterior al `POST /api/auth/login` se satisfaga en `/home`. Sin embargo, el guard de Angular puede redirigir desde esa ruta técnica hacia el primer módulo permitido.

**Trabajo a planificar:** mantener la comprobación explícita del `POST` de login con HTTP `200`, y esperar de forma determinista que se complete la navegación a una ruta **final** correspondiente a un módulo autorizado por los permisos efectivos del usuario. No se debe sustituir este control por un timeout mayor ni por una expresión regular que acepte cualquier ruta.

**Criterios de aceptación futuros:**
- Un login con respuesta HTTP `200` no se considera suficiente por sí solo para certificar enrutamiento autorizado.
- El guard completa la redirección; `/home` no cuenta como evidencia de destino final.
- La URL final corresponde a un módulo realmente autorizado, comprobado contra el contrato de permisos de la sesión y no solo por pertenecer a una lista estática de rutas.
- Existen pruebas positivas y negativas para redirecciones y permisos, sin falsos `PASS` por estados transitorios.
- Se conservan las comprobaciones vigentes de login, CSRF, sesiones, refresh, logout y concurrencia.

**Alcance estimado:** pruebas Playwright P06 y, solo si las evidencias lo justifican y existe aprobación adicional, guard/enrutamiento Angular. **No ejecutar todavía.**

## PEND-P06-NAV-002 — Tratar `/sin-acceso` como denegación, no como éxito

**Hallazgo:** la expectativa de navegación compartida también acepta `/sin-acceso`, aunque esa pantalla significa denegación. Una autenticación válida con módulos autorizados no debe considerarse navegación exitosa si termina allí.

**Trabajo a planificar:** separar el contrato positivo de navegación a módulos autorizados del contrato negativo de denegación. Validar que el usuario con módulos permitidos llega a una ruta accesible y que los intentos a módulos prohibidos producen la denegación esperada.

**Criterios de aceptación futuros:**
- Si el perfil autenticado dispone de módulos autorizados, finalizar en `/sin-acceso` provoca `FAIL` en el escenario positivo.
- Los usuarios sin módulos autorizados o que intenten entrar a rutas prohibidas se verifican mediante escenarios negativos diferenciados, con comportamiento de autorización explícito.
- El resultado respeta el rol y los IDs de módulos efectivos, sin inventar permisos ni relajar guards.
- La regresión confirma que no se filtran páginas protegidas, sesiones ni recursos de módulos no autorizados.
- No se alteran políticas de seguridad para lograr un resultado verde.

**Alcance estimado:** pruebas Playwright P06 y validación de guards/routing Angular cuando se autorice el plan. **No ejecutar todavía.**

## Condiciones para llevar estos pendientes al siguiente plan

1. Revisar impacto, rutas reales y dependencias del guard antes de modificar código.
2. Solicitar autorización explícita del propietario para implementar cada pendiente y definir responsables, pruebas y compuertas.
3. Integrarlos en un plan nuevo; no insertarlos como `READY` ni `APPROVED` en `docs/automatizacion/TAREAS_CODEX.json` por este simple registro.
4. Mantener `P06_REAL_CERTIFICATION=PASS` como evidencia histórica del SHA certificado. Una implementación futura tendrá su propio SHA, Quality Gate y pruebas; no heredará automáticamente el resultado 6/6.
5. Preservar `main`, producción, Oracle institucional y todas las restricciones vigentes. `BLOCK10_AUTHORIZED=NO`.

**Última acción registrada:** únicamente alta documental de estos dos pendientes; sin cambios funcionales, sin ejecución real de autenticación, sin conexión a Oracle y sin habilitar el watcher.

## 2026-11-18 — Observaciones diferidas M1 y transferencia a Microfase 2 (CHAT)

La Microfase 1 de Monitoreo de Listas conserva la **aprobación funcional del propietario**, el código certificado `3b22ebed` (QG `44424145245` SUCCESS), expediente `efb6a892` (QG `44426253435` SUCCESS) y [Issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) cerrado. **No se reabre M1 por registrar observaciones retrospectivas.**

Se archivan en el inventario específico [`M1_OBSERVACIONES_DIFERIDAS_Y_HANDOFF_M2_20261118.md`](M1_OBSERVACIONES_DIFERIDAS_Y_HANDOFF_M2_20261118.md) los controles `M1-HIST-01..07`, `M1-QA-01`, `SEC-PICO-01..02`: equivalencia histórica 242/190/52 con mismo corte, empleados 7/13, clasificación de cifras fuente vs positivas, orígenes NULL, denominadores de empresas, semántica de fechas, consultas DBA no ejecutadas, aislamiento, parche oficial y mantenimiento del fork `picomatch`. El [Issue #42](https://github.com/jmejia31/RIESGO_LAVADO/issues/42) permanece abierto por upstream. Este registro **no crea una orden automática** ni autoriza cambios.

**Continuación nueva:** [`docs/qa/M2_APERTURA_ALCANCE_20261118.md`](../qa/M2_APERTURA_ALCANCE_20261118.md) inicia el análisis controlado de Microfase 2 sin inventar su alcance ni tocar código o bases de datos.
