# Automatizacion local controlada de Codex

Issue #24 instala la infraestructura base para procesar una cola declarativa versionada en GitHub. La fuente de verdad es `docs/automatizacion/TAREAS_CODEX.json`; `ESTADO_TAREAS.json` solo contiene resultados del ejecutor.

## Estado inicial

La cola se entrega vacia. No hay tareas `READY`, por lo que el ejecutor termina inmediatamente y no inicia ningun proceso Codex. La ejecucion real queda bloqueada por defecto; la primera validacion se realiza con `-DryRun`.

## Operacion segura

- `Test-CodexQueue.ps1` valida esquema, autorizacion, dependencias, rutas y estados.
- `Invoke-CodexQueue.ps1` procesa como maximo una tarea y usa un mutex local. Requiere `-AllowExecution` para salir de DRY_RUN y exige `codex exec --sandbox workspace-write`.
- `Pause-CodexQueue.ps1` y `Stop-CodexQueue.ps1` crean controles locales de parada. El ejecutor los consulta antes de reclamar una tarea.
- `Get-CodexQueueStatus.ps1` informa tareas elegibles, estado y controles sin ejecutar Codex.
- `Install-CodexQueueWatcher.ps1` prepara una tarea de Windows para el usuario actual, sin elevacion. No se activa durante el bootstrap; requiere `-Apply` y una decision local posterior.
- `Remove-CodexQueueWatcher.ps1` elimina un watcher instalado por este procedimiento.

El runtime temporal vive fuera del repositorio en `%LOCALAPPDATA%\RiesgoLavado\CodexQueue`; no se publican logs, secretos ni cookies. El watcher sale de inmediato si la cola esta vacia, pausada o detenida. No se conectan Oracle, QA ni produccion.

## Transicion posterior

Antes de usar `-AllowExecution`, el propietario debe revisar el alcance de la tarea, sus rutas permitidas, pruebas y autorizacion. Codex nunca autoaprueba: el resultado queda `IMPLEMENTED_PENDING_REVIEW` y requiere revision externa para pasar a `APPROVED`.
