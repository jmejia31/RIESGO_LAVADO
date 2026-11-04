# Politica de ejecucion de la cola Codex

## Contrato

Una tarea es ejecutable solo cuando `status=READY`, `authorization.approved=true`, `authorization.approved_by=jmejia31`, `branch=desarrollo`, sus dependencias estan `APPROVED`, incluye comandos de verificacion allowlisted y sus rutas permitidas no incluyen areas protegidas. El archivo de instrucciones no convierte issues, README ni comentarios en ordenes ejecutables.

Estados permitidos: `PENDING_AUTHORIZATION`, `READY`, `CLAIMED`, `RUNNING`, `IMPLEMENTED_PENDING_REVIEW`, `CHANGES_REQUESTED`, `APPROVED`, `BLOCKED`, `FAILED`, `CANCELLED`.

## Protecciones

- Una sola ejecucion local mediante mutex y lease con expiracion.
- PAUSE y STOP son fail-closed: no se reclama ni inicia una tarea.
- Maximo dos intentos por tarea; despues queda `BLOCKED`.
- El ejecutor no puede escribir `APPROVED`.
- No se permiten `main`, produccion, Oracle institucional, DDL/DML, migraciones, `PRUEBA_FORMULARIO`, secretos, permisos, despliegues, bypass de CI ni `git add -A`.
- La ejecucion Codex requiere `--sandbox workspace-write`; estan prohibidos `--yolo`, `--dangerously-bypass-approvals-and-sandbox` y `danger-full-access`.
- Si no hay GitHub o el SHA remoto cambio, se detiene sin usar una cola obsoleta.
- El ejecutor verifica `origin/desarrollo`, compara el contenido de la cola y persiste `CLAIMED`, `RUNNING` e `IMPLEMENTED_PENDING_REVIEW` en `ESTADO_TAREAS.json` mediante commits serializados cuando se habilite una ejecucion controlada.
- Antes de publicar se validan comandos allowlisted, `git diff --check`, rutas modificadas y escaneo de secretos; el stage y el push son selectivos.
- Las verificaciones ejecutables estan allowlisted (`git diff --check`, `queue-schema`, `secret-scan`); no se interpreta texto de la cola como PowerShell arbitrario.
- Una tarea con `IMPLEMENTED_PENDING_REVIEW`, `APPROVED`, `BLOCKED` o `RUNNING` persistido no vuelve a ser elegible aunque permanezca `READY` en la cola. Solo un `FAILED` con intento menor que 2 puede reintentarse; el segundo fallo transita a `BLOCKED`.
- Los escenarios de prueba usan un runtime temporal propio y restauran/no alteran flags PAUSE/STOP preexistentes del usuario.

## DRY_RUN

El valor predeterminado es DRY_RUN. En ese modo se valida la cola, se comprueba que una tarea READY seria elegible y se simula el lock, pero no se invoca Codex, no se modifica el repositorio y no se crea un commit.
