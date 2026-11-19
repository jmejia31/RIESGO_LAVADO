# RIESGO_LAVADO — Microfase 2: apertura controlada de alcance

**Fecha:** 18/11/2026 · **Autor:** CHAT · **Rama:** `desarrollo`  
**Estado:** `M2_STARTED_SCOPING_ONLY` — inicio documental y análisis de dependencias. **Sin implementación ni cambio de código en este acto.**  
**Línea base M1:** código `3b22ebedf727a7ba4ffbd82540bfe98b05bb060b`, Quality Gate [44424145245](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44424145245) SUCCESS; expediente `efb6a892e6f2e9891c4d45667bfa3b8dc7363b5f`, Quality Gate [44426253435](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44426253435) SUCCESS; aprobación funcional del propietario.  
**Pendientes M1 diferidos:** [inventario único y punto de reanudación](../pendientes/M1_OBSERVACIONES_DIFERIDAS_Y_HANDOFF_M2_20261118.md).

## 1. Alcance inicial de esta apertura

1. Confirmar la **definición funcional exacta** de `Microfase 2` con el roadmap vigente del proyecto; no equipararla sin evidencia a las intervenciones históricas `Fase 2A`, ni a B07, Bloque 10 o Matrices.
2. Revisar contratos REST, Angular, seguridad, autorización, Oracle, auditoría y reportes afectados por el objetivo que se identifique, antes de cambiar archivos.
3. Establecer entregables verificables, reglas de negocio, casos de prueba y criterios de aceptación del propietario para la M2 concreta.
4. Registrar matriz de impacto/dependencias, archivos previstos, pruebas focales, Oracle XE descartable, Playwright y Quality Gate exact-SHA.
5. Proteger lo entregado en M1; **no reabrir** agrupación, filtros, motivos ni seguimientos salvo nuevo defecto confirmado.
6. Mantener los históricos pendientes y el mantenimiento de `picomatch` en su inventario separado; no tratarlos como bloqueos artificiales para empezar la planificación M2.

## 2. Puertas antes de cambiar código

- `M2_FUNCTIONAL_SCOPE=TO_BE_ESTABLISHED_FROM_OWNER_OR_VERSIONED_PLAN`.
- `M2_ACCEPTANCE_CRITERIA=NOT_YET_APPROVED`.
- `M2_ANALYSIS=OPEN`; `M2_IMPLEMENTATION=NOT_STARTED`.
- Antes de implementar, cumplir `AGENTS.md`, `BITACORA_COLABORACION.md`, `ESTADO_COLABORACION.md` y skills relevantes del repositorio.
- Preservar rama `respaldo/desarrollo-20261118-5e5dc8c0`; no cambiar `main`, producción, Oracle institucional ni `DNP_IHSS` y no activar tareas Codex automáticamente.
- Las acciones futuras deberán publicarse exclusivamente en `desarrollo` y certificar su SHA real.

## 3. Resultado de esta intervención

```ini
M1_ACCEPTED=YES
M2_SCOPING_STARTED=YES
M2_SCOPE_CONFIRMED=NO
M2_CODE_CHANGED=NO
M2_DATABASE_WRITES=0
M2_NEXT_STEP=DEFINE_FUNCTIONAL_SCOPE_AND_IMPACT
```
