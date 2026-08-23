# CIERRE Y CERTIFICACIÓN — UI-FAM.QA

**Fecha:** 2026-08-23  
**Módulo:** Matrices de Riesgos  
**Fase:** UI-FAM.QA — Integración y certificación  
**Rama certificada:** `desarrollo`  
**HEAD funcional/test certificado:** `adeef792bf75043a60618d09b92c0cecbc3fc754`  
**Estado:** **CERRADA Y CERTIFICADA EN SU ALCANCE DE INTEGRACIÓN/UI**

---

## 1. Objetivo del gate

UI-FAM.QA es un **gate transversal de cierre** del bloque Familias de Formularios. **No constituye una quinta pantalla**.

Integra y certifica conjuntamente:

- UI-FAM.1 — Gestor de Familias.
- UI-FAM.2 — Detalle de Familia.
- UI-FAM.3 — Crear Familia.
- UI-FAM.4 — Editar Familia + Ciclo de Vida.

Este gate es **adicional** a las pruebas backend de **F6.5.FAM.1** y **no las sustituye**.

---

## 2. Alcance y trazabilidad de escenarios

| Área | Escenario | Evidencia automatizada / contractual | Resultado |
|---|---|---|---|
| Gestor | Carga familias | `matrices-riesgos.component.familias.spec.ts` — caso 11 | PASS |
| Gestor | Calcula KPI | `matrices-riesgos.component.familias.spec.ts` — caso 12 | PASS |
| Gestor | Búsqueda | `matrices-riesgos.component.familias.spec.ts` — caso 6 | PASS |
| Gestor | Filtro activo | caso 13 | PASS |
| Gestor | Filtro inactivo | caso 14 | PASS |
| Gestor | Filtro vigente | caso 15 | PASS |
| Gestor | Filtro sin vigente | caso 16 | PASS |
| Gestor | Estado vacío | caso 17 | PASS |
| Detalle | Abre familia correcta | Gestor caso 7 + `familia-detalle-modal.component.spec.ts` | PASS |
| Detalle | Carga información | `familia-detalle-modal.component.spec.ts` | PASS |
| Detalle | Muestra versiones | `familia-detalle-modal.component.spec.ts` | PASS |
| Detalle | Estados correctos | `familia-detalle-modal.component.spec.ts` | PASS |
| Crear | Código requerido | `familia-crear-modal.component.spec.ts` — caso 4 | PASS |
| Crear | Nombre requerido | caso 9 | PASS |
| Crear | Código duplicado | caso 7 | PASS |
| Crear | Creación exitosa | caso 5 | PASS |
| Editar | Código bloqueado | `familia-editar-modal.component.spec.ts` — carga/código readonly | PASS |
| Editar | Cambia nombre | guardado descriptivo + detección separada de cambios | PASS |
| Editar | Cambia descripción | guardado descriptivo + detección separada de cambios | PASS |
| Activar | Activa inactiva | endpoint dedicado + emisión `ACTIVADA` | PASS |
| Activar | Ya activa | prueba QA de no activable / no duplicación | PASS |
| Desactivar | Activa sin vigente | endpoint dedicado + emisión `DESACTIVADA` | PASS |
| Desactivar | Rechaza vigente | bloqueo cuando existe versión publicada vigente | PASS |
| Eliminar | Permite cero versiones | eliminación segura + evento | PASS |
| Eliminar | Bloquea con versiones | botón/regla bloqueada | PASS |
| Seguridad | Acciones según autorización | Gestor sin autorización + Editar sin Administrador | PASS |
| UX | Doble clic protegido | Crear con request pendiente + ciclo de vida Editar con operación pendiente | PASS |
| UX | Loading | Gestor con `Subject` pendiente + cobertura existente de Detalle | PASS |
| UX | Error backend | Gestor / Crear / Editar / Detalle | PASS |
| UX | Cierre modal | Escape automatizado en Crear / Editar / Detalle | PASS |
| UX | Navegación teclado | Escape automatizado; focus trap Tab/Shift+Tab verificado en contrato fuente de Editar | PASS |
| Responsive | Desktop | contratos DOM/clases de Gestor / Crear / Editar / Detalle | PASS |
| Responsive | Resolución reducida | anchos, límites de alto, grids y scroll responsivo | PASS |

---

## 3. Evidencia automatizada consolidada

### Frontend

Workflow **Quality Gates** sobre `adeef792bf75043a60618d09b92c0cecbc3fc754`:

- **96/96 archivos de prueba PASS**.
- **492/492 tests PASS**.
- **0 fallos frontend**.
- Cobertura frontend: **77.59%**.
- Gate mínimo frontend: **70.0%**.
- Resultado de cobertura frontend: **PASS**.

Cobertura directa del bloque Familias dentro de los archivos modificados/dedicados de este gate:

- `matrices-riesgos.component.familias.spec.ts`: **19 PASS**.
- `familia-crear-modal.component.spec.ts`: **10 PASS**.
- `familia-editar-modal.component.spec.ts`: **16 PASS**.
- `familia-detalle-modal.component.qa.spec.ts`: **1 PASS**.
- La suite principal existente de `familia-detalle-modal.component.spec.ts` conserva su cobertura funcional adicional del detalle.

### Backend

En el mismo Quality Gate:

- UnitTests: **106 PASS**.
- Infrastructure.Tests: **84 PASS**.
- IntegrationTests: **22 PASS**.
- **0 fallos backend**.

Adicionalmente, el pipeline validó correctamente build Release, formato .NET, contratos de autorización/seguridad, contratos Angular/UX/Matices y controles de infraestructura aplicables antes de la evaluación final de cobertura.

---

## 4. Quality Gate global y residual transversal

El workflow global permanece en rojo por una condición **ajena al alcance funcional/UI de UI-FAM.QA**:

- Cobertura backend obtenida: **68.6%**.
- Umbral requerido: **70.0%**.
- Déficit global: **1.4 puntos porcentuales**.
- Mensaje causal exacto: `Backend coverage below required 70% (actual 68.6%).`

Todos los tests backend ejecutados en el job pasan; el fallo ocurre únicamente en la comprobación final del porcentaje global de cobertura.

**No se modificó ni debilitó ningún threshold, exclusión o regla de calidad para forzar un resultado verde.**

Este residual debe seguir tratándose como deuda global/transversal del repositorio y no invalida la certificación específica de integración/UI de UI-FAM.QA.

---

## 5. Integridad del alcance

La comparación entre el cierre UI-FAM.4 (`fb687954af95d34464097d2dd6760fa49ce7f767`) y el HEAD funcional/test de UI-FAM.QA (`adeef792bf75043a60618d09b92c0cecbc3fc754`) confirma que el trabajo de este gate se limitó a **archivos de pruebas del bloque Familias**.

No se modificó:

- código productivo de las cuatro interfaces;
- HTML/UX aprobado;
- contratos funcionales ya certificados;
- `main`;
- producción;
- thresholds de calidad.

---

## 6. QA visual, responsive y accesibilidad

La validación responsive se certifica mediante **contratos automatizados DOM/clases y revisión de fuente**, incluyendo:

- anchos responsive;
- `max-width` y límites de altura;
- grids adaptativos;
- contenedores con scroll seguro;
- comportamiento en resolución reducida;
- cierre por teclado;
- protección de operaciones en curso;
- contrato de focus trap Tab/Shift+Tab en Editar.

No se declara una inspección visual en navegador desplegado que no haya sido ejecutada. La certificación visual/responsive de este gate corresponde a **fuente + pruebas automatizadas**.

---

## 7. Regla de integración y repositorio

Este cierre:

- **no autoriza merge a `main`**;
- **no autoriza cambios productivos**;
- mantiene el trabajo en `desarrollo`;
- mantiene el PR #20 abierto y en borrador hasta autorización expresa de Javier Mejía.

---

## 8. Veredicto

**UI-FAM.QA queda CERRADA Y CERTIFICADA en su alcance de integración/UI.**

Las cuatro interfaces UI-FAM.1, UI-FAM.2, UI-FAM.3 y UI-FAM.4 quedan validadas de manera transversal respecto de los escenarios definidos para este gate.

La única condición global pendiente observada por el workflow es la cobertura backend del repositorio (**68.6% < 70%**), registrada de forma explícita y sin alterar las reglas de calidad.
