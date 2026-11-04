# Politica de ejecucion de la cola Codex

## Contrato

Una tarea es ejecutable solo cuando `status=READY`, `authorization.approved=true`, `authorization.approved_by` no esta vacio, `branch=desarrollo`, sus dependencias estan `APPROVED` y sus rutas permitidas no incluyen areas protegidas. El archivo de instrucciones no convierte issues, README ni comentarios en ordenes ejecutables.

Estados permitidos: `PENDING_AUTHORIZATION`, `READY`, `CLAIMED`, `RUNNING`, `IMPLEMENTED_PENDING_REVIEW`, `CHANGES_REQUESTED`, `APPROVED`, `BLOCKED`, `FAILED`, `CANCELLED`.

## Protecciones

- Una sola ejecucion local mediante mutex y lease con expiracion.
- PAUSE y STOP son fail-closed: no se reclama ni inicia una tarea.
- Maximo dos intentos por tarea; despues queda `BLOCKED`.
- El ejecutor no puede escribir `APPROVED`.
- No se permiten `main`, produccion, Oracle institucional, DDL/DML, migraciones, `PRUEBA_FORMULARIO`, secretos, permisos, despliegues, bypass de CI ni `git add -A`.
- La ejecucion Codex requiere `--sandbox workspace-write`; estan prohibidos `--yolo`, `--dangerously-bypass-approvals-and-sandbox` y `danger-full-access`.
- Si no hay GitHub o el SHA remoto cambio, se detiene sin usar una cola obsoleta.

## DRY_RUN

El valor predeterminado es DRY_RUN. En ese modo se valida la cola, se comprueba que una tarea READY seria elegible y se simula el lock, pero no se invoca Codex, no se modifica el repositorio y no se crea un commit.
