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

## Quality Gates proporcionales (Issue #24)

- La fuente de verdad permanece en GitHub, rama `desarrollo`. El enrutador `tools/automatizacion/ci_gate_scope.py` permite una comprobacion rapida **solo** si un push no forzado a `desarrollo` cambia exclusivamente los archivos exactos `TAREAS_CODEX.json`, `ESTADO_TAREAS.json` y/o `ISSUE24_ACCEPTANCE_PROOF.txt` bajo `docs/automatizacion/`. No admite prefijos similares, archivos de codigo, schemas, cambios de workflows o documentacion de certificacion.
- El fast gate ejecuta 25 regresiones del enrutador, verifica JSON y controles de no autoaprobacion, y pasa por el validador canonico `Test-CodexQueue.ps1`. Fallas de lectura, Git, SHA, historia de ancestros, diff, ramas, Unicode o modificaciones mezcladas son **fail-closed hacia Quality Gates completos**. Errores de validacion no se declaran PASS.
- Los pushes a `main`, PR a `main`, `workflow_dispatch` y cualquier push funcional o ambiguo a `desarrollo` mantienen el Quality Gate completo: .NET, Angular, Oracle XE disposable, E2E y contenedores. No se reduce ninguna asercion ni piso de calidad.
- La concurrencia `cancel-in-progress` vive solamente en el job integral, no en el workflow. Un commit administrativo fast-only **no cancela** un Quality Gate funcional en curso. Un cambio funcional posterior si puede sustituir una ejecucion integral obsoleta, conservando trazabilidad por SHA.
- **Una ejecucion de workflow con job integral `skipped` NO cuenta como certificacion institucional ni como una de las cinco corridas P0**. Para un cierre formal, comprobar el job integral `success`, su SHA exacto, cobertura Oracle requerida y la aprobacion externa. `workflow_dispatch` fuerza siempre la via integral.
- La automatizacion puede continuar con tareas independientes autorizadas sin esperar cada commit de estado, pero no debe autoaprobar, habilitar produccion, ejecutar tareas de alto riesgo ni certificar un lote sin sus gates obligatorios. El watcher Windows continua en `-DryRun` hasta nueva autorizacion.
