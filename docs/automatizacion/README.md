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

El procedimiento de aceptación futura está en [ACEPTACION_LOCAL.md](ACEPTACION_LOCAL.md). No se ejecuta ni activa el watcher como parte de este bootstrap.

## Transicion posterior

Antes de usar `-AllowExecution`, el propietario debe revisar el alcance de la tarea, sus rutas permitidas, pruebas y autorizacion. Codex nunca autoaprueba: el resultado queda `IMPLEMENTED_PENDING_REVIEW` y requiere revision externa para pasar a `APPROVED`.

## Operación autónoma autorizada (Issue #24)

Tras completar la prueba sintética manual, el propietario autoriza un watcher controlado para procesar únicamente tareas **LOW**, explícitamente `READY`, aprobadas por `jmejia31`, sin dependencias pendientes y con archivos exactos. La cola de GitHub determina si hay trabajo; el watcher de Windows consulta cada cinco minutos. La primera instalación controlada es **una sola intervención local del propietario**; los siguientes commits de estado y tareas aprobadas no requieren comandos manuales a Codex.

**Comando único local**, exclusivamente en `C:\RIESGO_LAVADO_CODEX_WORKER`, después de publicar el modo `CONTROLLED` y validar CI de seguridad del worker:

```powershell
Set-Location 'C:\RIESGO_LAVADO_CODEX_WORKER'
powershell.exe -NoProfile -File tools/automatizacion/Install-CodexQueueWatcher.ps1 -Controlled -Apply -OwnerAuthorization 'jmejia31:ISSUE24-CONTROLLED-ACCEPTANCE'
```

El instalador exige rama `desarrollo`, worktree íntegramente limpio, GitHub sincronizado, cola canónica CONTROLLED, `codex exec --sandbox workspace-write`, scanner `rg`, estado seguro y que el watcher preexistente sea exclusivamente DRY_RUN. Sustituye la programación, verifica acción real `-AllowExecution`, y en caso de error deja el watcher deshabilitado. No se admite instalar en el checkout principal.

El ejecutor selecciona como máximo una tarea por intervalo, deja `CLAIMED` y `RUNNING` publicados, ejecuta Codex en sandbox, verifica rutas exactas y secretos, publica selectivamente y deja `IMPLEMENTED_PENDING_REVIEW`. Un workflow de GitHub crea un Issue asignado al propietario para revisar la tarea terminada o bloqueada. **No hay autoaprobación**; tareas dependientes de una aprobación siguen esperando. Las tareas independientes LOW pueden continuar mientras el CI integral corre asincrónicamente; un CI rojo nunca cuenta como aceptación.

Las rutas automáticas admitidas son archivos explícitos de código bajo `frontend/rl-app/src/`, `frontend/rl-app/e2e/`, `backend/RL.API/`, `backend/RL.API.Tests/` y documentación no certificatoria, excluyendo seguridad, autenticación, permisos, Oracle, migraciones, infraestructura, secretos, configuraciones sensibles y directorios completos. La única excepción de `docs/automatizacion/` son comprobantes sintéticos `ISSUE24_*PROOF.txt`. Una tarea MEDIUM/HIGH/CRITICAL **no se ejecuta sin revisión adicional**. Los fallos persistentes quedan bloqueados y requieren intervención del propietario.

Para detener nuevas ejecuciones, usar `tools/automatizacion/Pause-CodexQueue.ps1` o `Stop-CodexQueue.ps1` en el worker y revisar el estado; no eliminar manualmente los registros. El modo seguro anterior `DRY_RUN` puede restablecerse en la cola de GitHub, pero el cambio requiere detener/convertir la tarea programada si sigue usando `-AllowExecution`. La ejecución solo actúa cuando cola=CONTROLLED; si vuelve a DRY_RUN no inicia Codex.
