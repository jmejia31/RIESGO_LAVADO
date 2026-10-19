# Block 12 P1: durable database and filesystem recovery

## Operation inventory

| Operation | Durable marker | Oracle reference used during recovery | Recovery decision |
|---|---|---|---|
| Matrix evidence upload | `App_Data/Evidencias/.operations` | `RL_MR_EVIDENCIAS.EVI_RUTA` | Keep/promote the file when the row exists; remove an incomplete upload when it does not. |
| Matrix evidence delete | `App_Data/Evidencias/.operations` | `RL_MR_EVIDENCIAS.EVI_ID` | Restore quarantined content when the row exists; finish deleting quarantined content when it does not. |
| Monitoring-list evidence upload | Configured evidence storage `/.operations` | `RL_DETALLE_EVIDENCIA.EVI_RUTA_ARCHIVO` | Keep/promote the file when metadata exists; discard the incomplete upload when it does not. Logical evidence deletion retains its file by contract. |
| Configuration image upload | `App_Data/Evidencias/.configuration-image-operations` | `RL_AUDITORIA` insert for the stored image name | Keep/promote the staged image when its upload audit exists; discard an unrecorded partial upload otherwise. |

The journal is flushed before the filesystem rename. A recovery worker checks the Oracle row after a process restart, so a commit exception is not treated as proof of rollback. Journal entries remain available for retry if Oracle cannot be queried or if reconciliation detects conflicting file content.

## Storage requirement

Evidence journals are stored beside the files they reconcile. The configuration-image journal is kept under the persistent evidence volume, outside the public static-file directory. Production hosting must keep both the evidence directory and `wwwroot/uploads` on persistent storage that survives API process/container restarts. Each pending file and its final file share a filesystem so file moves remain atomic. The evidence hosting volume is listed in [FASE_7_INVENTARIO_RELEASE.md](FASE_7_INVENTARIO_RELEASE.md).

## Verification

Focused journal tests cover incomplete upload removal, upload promotion, deletion restoration, repeat recovery, and a child process that exits while holding an unfinished journal. The disposable Oracle XE CI test exercises the same restart recovery against present and absent Oracle references using a temporary filesystem. It does not access production data.

Configuration image upload audit records use the `INSERT` action allowed by the current `RL_AUDITORIA.AUD_ACCION` constraint. The previous `UPLOAD` action was outside that constraint and could fail after the image file had been written.
