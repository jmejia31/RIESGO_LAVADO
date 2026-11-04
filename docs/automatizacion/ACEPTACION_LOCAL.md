# Procedimiento de aceptacion local de Issue #24

Este procedimiento no se ejecuta durante el bootstrap. Requiere una autorizacion local expresa del propietario y una tarea sintetica que solo pueda modificar archivos bajo `docs/automatizacion/`. No usar QA, Oracle, credenciales, produccion, `main`, migraciones ni `PRUEBA_FORMULARIO`.

## Preflight

1. Confirmar `git switch desarrollo`, `git pull --ff-only origin desarrollo` y worktree sin cambios tracked.
2. Confirmar `codex exec --help` y que existe `--sandbox`; no usar `--yolo`, bypass ni `danger-full-access`.
3. Leer la cola desde GitHub y confirmar que el SHA local coincide con `origin/desarrollo`.
4. Ejecutar `tools/automatizacion/Test-CodexQueue.ps1`, `Test-CodexQueue-Scenarios.ps1` y `Get-CodexQueueStatus.ps1`.
5. Ejecutar `Install-CodexQueueWatcher.ps1 -Controlled` solo como preview. No usar `-Apply` todavía.
6. Confirmar que el ejecutor resuelve `rg` antes de reclamar la tarea. El escaneo de secretos revisa el delta efectivo de archivos tracked, nuevos o untracked modificados por Codex; código 0 bloquea por coincidencia, 1 permite continuar y cualquier otro código bloquea por error. Las coincidencias preexistentes se revisan sin imprimir valores.

## Tarea sintetica

El propietario debe publicar en GitHub una tarea `READY` con `authorization.approved=true`, `authorization.approved_by=jmejia31`, riesgo `LOW`, rama `desarrollo`, dependencia vacia, rutas limitadas a `docs/automatizacion/`, criterios y comandos allowlisted (`queue-schema`, `git diff --check`, `secret-scan`). La tarea debe crear o modificar exclusivamente un archivo de evidencia sintética y no puede ejecutar comandos de shell contenidos en sus campos.

## Compuerta controlada

Solo después de revisar el diff de la tarea y autorizarlo expresamente:

```powershell
git pull --ff-only origin desarrollo
powershell -NoProfile -File tools/automatizacion/Test-CodexQueue.ps1
powershell -NoProfile -File tools/automatizacion/Install-CodexQueueWatcher.ps1 -Controlled -Apply -OwnerAuthorization 'jmejia31:ISSUE24-CONTROLLED-ACCEPTANCE'
```

El watcher controlado no acepta `QueuePath` alternativo ni `SkipRemoteSync`; exige la cola canónica de GitHub y `queue_mode=CONTROLLED`. Si la cola queda vacia, termina sin iniciar Codex. Para una prueba manual de una sola iteracion, detener el watcher y ejecutar el invocador canonico con `-AllowExecution` solo con la misma autorizacion vigente.

Las rutas de `allowed_paths` se interpretan como archivos exactos cuando apuntan a un archivo y como directorios con límite de separador cuando apuntan a un directorio. No se aceptan sufijos parecidos, traversal, rutas absolutas ni reparse points. El contrato de Codex se transmite por stdin mediante un proceso sin shell; no se concatena el JSON al argumento de proceso.

## Evidencia y rollback

Registrar `REMOTE_HEAD`, hash de cola, tarea reclamada, estados `CLAIMED`/`RUNNING`/`IMPLEMENTED_PENDING_REVIEW`, gates, rutas publicadas, commit y URL remota. Si falla cualquier gate, detener, dejar `FAILED` y reintentar como maximo una vez; el segundo fallo deja `BLOCKED` con motivo persistido. Una tarea `IMPLEMENTED_PENDING_REVIEW` no vuelve a ejecutarse aunque siga `READY` en la cola. Los tests no deben limpiar flags PAUSE/STOP preexistentes. Codex nunca escribe `APPROVED`; la revision externa es obligatoria.

## Aceptación de ejecución autónoma de tareas LOW, posterior a la prueba manual

La primera ejecución real sintética (ISSUE24-SYNTHETIC-001) se completó y quedó en `IMPLEMENTED_PENDING_REVIEW` con intento 1; el watcher DRY_RUN se reactivó y completó correctamente un turno programado. La puesta en marcha continua exige una **única acción local** con el instalador endurecido (véase `README.md`). No usar `Invoke-CodexQueue.ps1 -AllowExecution` manualmente después de activar el watcher.

El nuevo comprobante `ISSUE24-AUTO-ACCEPTANCE-002` puede demostrar que el watcher CONTROLLED reclama una tarea LOW autorizada, crea exclusivamente `docs/automatizacion/ISSUE24_AUTO_ACCEPTANCE_PROOF.txt`, publica el estado y genera una Issue de revisión automáticamente. El propietario debe confirmar el nuevo commit y la Issue; no se da aprobación automática ni se autoriza Block 10.

La activación se niega si el worker tiene cambios, no coincide con `origin/desarrollo`, tiene un watcher CONTROLLED previo o inválido, la cola no está en `CONTROLLED`, hay un STOP/PAUSE activo o faltan herramientas. Las configuraciones previas seguras se preservan hasta que la activación se valida; cualquier error durante el reemplazo deshabilita el watcher. Después de activar, no es necesario esperar Quality Gates administrativos para que se seleccionen tareas LOW independientes. El control de resultados y la revisión humana siguen siendo obligatorios.
