---
name: riesgo-lavado-cambio-quirurgico
description: Ejecuta cambios de código o documentación de forma quirúrgica en RIESGO_LAVADO. Usar para correcciones, CRUD, UI, backend, frontend, base de datos, refactorizaciones o bugs donde debe evitarse reescanear todo el repositorio.
---

# Cambio quirúrgico

## Principio

Trabajar incrementalmente sobre la estructura ya conocida y la evidencia versionada. No reescanear el repositorio completo por defecto.

## Navegación

1. Leer `AGENTS.md` y activar `riesgo-lavado-continuidad`.
2. Consultar primero `codex-graph` para arquitectura, dependencias, símbolos, rutas, tests e impacto cuando esté disponible.
3. Inspeccionar solo los archivos sugeridos por el grafo, historial, bitácora, estado colaborativo o requerimiento.
4. Ampliar el alcance únicamente si una dependencia real lo exige.

## Ejecución

- Mantener contratos REST, IDs, reglas de negocio, estructura Oracle y comportamiento existente salvo requisito explícito.
- No introducir workarounds silenciosos ni desactivar validaciones para hacer pasar pruebas.
- Modificar la mínima superficie necesaria.
- Añadir/ajustar regresión cuando el cambio sea funcional.
- No regenerar archivos ajenos al alcance ni hacer limpieza masiva incidental.

## Verificación

- Revisar diff completo del alcance.
- Ejecutar las pruebas más cercanas primero y luego gates del alcance.
- Si aparece un fallo ajeno, clasificarlo con evidencia; no ocultarlo.
- Aplicar `riesgo-lavado-quality-gates` antes del cierre.

## Prohibiciones

- Reescaneo completo repetitivo sin causa.
- Cambios en `main`.
- Commits con temporales o artefactos generados no requeridos.
- Declarar cierre sin evidencia.
