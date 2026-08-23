# CIERRE Y CERTIFICACIÓN — FASE UI-FAM.4

**Fecha:** 2026-08-23  
**Módulo:** Matrices de Riesgos  
**Fase:** UI-FAM.4 — Modal Full Pro: Editar Familia + Ciclo de Vida  
**Rama certificada:** `desarrollo`  
**Estado de la fase:** **CERRADA Y CERTIFICADA EN SU ALCANCE FUNCIONAL/UI**

## 1. Alcance certificado

Se certifica la implementación del modal de edición de Familia de Formularios y la administración separada de su ciclo de vida, manteniendo el diseño aprobado y sin introducir un rediseño funcional.

La implementación incluye:

- carga de la familia por `familiaId`;
- edición exclusiva de datos descriptivos;
- preservación del estado de ciclo de vida durante el guardado descriptivo;
- validación de nombre obligatorio y límites de longitud;
- activación de familia con confirmación;
- desactivación con confirmación y bloqueo cuando existe versión vigente/publicada;
- eliminación segura únicamente cuando no existen versiones asociadas;
- control de permisos administrativos;
- estados de carga, operación, éxito, error y no encontrado;
- cierre con Escape;
- focus trap con Tab/Shift+Tab;
- restauración del foco al destruir el modal;
- cancelación de solicitudes y protección frente a cargas obsoletas;
- emisión de eventos `guardada`, `estadoCambiado`, `eliminada` y `cerrar`.

## 2. Commits que componen UI-FAM.4

1. `c7cf408de1e5548b870647745ee583813a2db3a1` — `feat(matrices): implementar UI-FAM.4 edición y ciclo de vida`
2. `92011662f4c1070cdf92a76c0317700ecf54475e` — `test(matrices): estabilizar pruebas UI-FAM.4`
3. `1f0226ab0c4672b22d1b8ded97a683188917a6fc` — `test(matrices): desacoplar render UI-FAM.4 en Vitest`
4. `bdcbb4add7e4d29c24ac9ec255d758877ab55c2d` — `test(matrices): completar cobertura ciclo de vida UI-FAM.4`

El commit de código/pruebas utilizado para la certificación final de la fase es `bdcbb4add7e4d29c24ac9ec255d758877ab55c2d`.

## 3. Certificación de pruebas

Evidencia del workflow de GitHub Actions ejecutado sobre `bdcbb4add7e4d29c24ac9ec255d758877ab55c2d`:

- Workflow run: `32667440612`.
- Especificación dedicada `familia-editar-modal.component.spec.ts`: **12 pruebas ejecutadas y aprobadas**.
- Suite frontend completa: **94 archivos de prueba aprobados**.
- Suite frontend completa: **476 pruebas aprobadas, 0 fallidas**.
- Build de producción Angular: **PASS**.
- ESLint frontend: **PASS**.
- Cobertura frontend medida por el gate: **77.3%**, superior al mínimo del repositorio de **70%**.
- Build backend: **PASS**.
- `dotnet format --verify-no-changes`: **PASS**.
- Pruebas backend unitarias/infrastructure/integration: **PASS**.
- CodeQL Analysis: **PASS**.
- OWASP Dependency Check: **PASS**.
- Terraform Validation: **PASS**.

### Cobertura específica agregada para el ciclo de vida

La suite dedicada certifica, entre otros escenarios:

- activación exitosa mediante `activarFamiliaFormulario`;
- emisión de `estadoCambiado` con acción `ACTIVADA`;
- desactivación y emisión de `DESACTIVADA`;
- bloqueo de desactivación cuando existe versión vigente;
- eliminación permitida cuando `totalVersiones = 0`;
- bloqueo de eliminación cuando existen versiones;
- confirmación de operaciones mediante SweetAlert;
- emisión de `eliminada` al completar una eliminación segura;
- emisión de `guardada` tras guardar correctamente;
- cierre mediante Escape y prevención del evento predeterminado;
- permisos y controles de ciclo de vida;
- carga y manejo de errores.

## 4. Resultado del Quality Gate global

El workflow general finaliza en rojo por una condición **global del backend que no fue introducida por UI-FAM.4**:

- cobertura global backend obtenida: **68.6%**;
- mínimo exigido por el repositorio: **70%**;
- mensaje causal: `Backend coverage below required 70% (actual 68.6%).`;
- código de salida del script de quality gates: `18`.

Por tanto:

- **UI-FAM.4: pruebas funcionales y frontend PASS**;
- **repositorio completo: Quality Gate global NO VERDE** por déficit previo/transversal de cobertura backend de 1.4 puntos porcentuales;
- el umbral de cobertura **NO fue reducido ni alterado** para forzar un resultado verde.

Este hallazgo queda registrado como deuda transversal del repositorio y no invalida la certificación funcional de UI-FAM.4, cuyo código, pruebas dedicadas, suite frontend, build y lint pasan correctamente.

## 5. SonarCloud

El job SonarCloud del mismo workflow se mantiene en estado fallido como control global de calidad. No se modificaron reglas, exclusiones ni umbrales de SonarCloud para cerrar esta fase. Su remediación corresponde al cierre transversal de calidad del repositorio y debe conservarse visible hasta ser resuelta conforme al plan global.

## 6. QA funcional y visual

Se verificó por contrato de código la correspondencia entre el componente aprobado y el comportamiento implementado:

- modal basado en `dialog` con `aria-modal`;
- encabezado `Editar Familia de Formularios`;
- separación explícita entre edición descriptiva y ciclo de vida;
- estados de carga, éxito, error y no encontrado;
- controles de cierre bloqueados durante operaciones críticas;
- responsive modal y marcador `data-ui-fam-edit="modal"`;
- lógica de permisos y disponibilidad de acciones coherente con el estado de la familia;
- accesibilidad básica de teclado y gestión de foco implementadas.

No se realizó un rediseño de la interfaz aprobada.

**Limitación de certificación visual:** no se encontró en el repositorio una URL pública de despliegue (`vercel.app`/`azurewebsites.net`) disponible para ejecutar una inspección renderizada en navegador desde este entorno. Por ello, la certificación visual de este cierre es de **contrato fuente + pruebas automatizadas**, sin inventar una validación visual de navegador que no pudo ejecutarse.

## 7. Criterio de cierre

| Criterio | Resultado |
|---|---|
| Implementación funcional UI-FAM.4 | PASS |
| Ciclo de vida activar/desactivar/eliminar | PASS |
| Validaciones y permisos | PASS |
| Pruebas dedicadas | PASS — 12/12 |
| Suite frontend completa | PASS — 476/476 |
| Angular build | PASS |
| ESLint | PASS |
| Cobertura frontend | PASS — 77.3% >= 70% |
| Backend build/tests | PASS |
| CodeQL | PASS |
| OWASP | PASS |
| Terraform | PASS |
| QA de contrato visual/funcional | PASS |
| QA renderizado en navegador | NO EJECUTABLE — sin URL pública disponible |
| Quality Gate global del repositorio | ROJO — backend coverage 68.6% < 70% |
| Umbrales debilitados para cerrar | NO |

## 8. Dictamen

**FASE UI-FAM.4 queda CERRADA Y CERTIFICADA EN SU ALCANCE FUNCIONAL/UI.**

La fase no presenta pruebas fallidas, errores de build, errores de lint ni déficit de cobertura frontend respecto al gate institucional. El único bloqueo rojo del workflow general identificado durante esta certificación corresponde a la cobertura global del backend del repositorio (`68.6% < 70%`) y queda explícitamente separado de la fase para evitar una certificación engañosa.

La certificación no autoriza fusión a `main`, no modifica producción y no altera los umbrales de calidad del proyecto.
