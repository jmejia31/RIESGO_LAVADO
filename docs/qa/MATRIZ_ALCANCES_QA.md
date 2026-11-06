# Matriz de alcances y limites QA

Estado: `IMPLEMENTED_PENDING_REVIEW`

| Area | Solo lectura | Mutacion | Condicion |
|---|---:|---:|---|
| Usuarios y roles | Si | No | No crear usuarios, cambiar roles ni inferir privilegios |
| Configuracion | Si | No | No cambiar parametros institucionales |
| Auditoria | Si | No | Consultar con permiso; no insertar eventos manuales |
| Catalogos y modulos | Si | No | No alterar catalogos ni asociaciones |
| Modulos 2-9 | Si | No | Navegacion y consultas sanitizadas |
| Matrices, lectura | Si | No | Verificar permisos y alcance |
| Matrices, fixtures sinteticos | Si | Solo con autorizacion posterior | IDs propios, etiqueta, dependencias, auditoria y limpieza |
| Oracle institucional | No ejecutado | No | Destino no verificado |
| Oracle XE descartable | No ejecutado | No | No sustituye identidad QA |
| Produccion y `main` | No | No | Fuera de alcance |
| Migracion 54 | No | No | Prohibida |
| Credenciales QA | No verificadas | No | No instalar ni usar |

## Reglas de datos

- La base QA institucional no es disposable.
- No ejecutar `TRUNCATE`, `DROP`, DDL, migraciones, purgas, borrado masivo ni cascadas.
- No deshabilitar constraints ni modificar historicos.
- Solo una autorizacion futura y separada podria permitir fixtures sinteticos en Matrices.
- Toda limpieza futura seleccionara exactamente IDs creados por esa ejecucion y comprobara dependencias.
- Ante ambiguedad, fallar cerrado y conservar `QA_MUTATIONS=DISABLED`.

```ini
QA_SECRET_ACCESS_GATE=BLOCKED
QA_SECRETS_INSTALLED=NOT_VERIFIED
QA_DB_TARGET_VERIFIED=NO
QA_WEB_LOGIN_VERIFIED=NO
QA_READONLY_ACCESS=NOT_VERIFIED
QA_MUTATIONS=DISABLED
QA_AUTONOMOUS_MUTATIONS=DISABLED
QA_EXECUTION_MODE=DRY_RUN
```

La publicacion no autoriza conexion, login, escritura, limpieza, despliegue, P07, P08 ni Bloque 10.
