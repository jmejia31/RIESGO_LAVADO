# Manifiesto de scripts aprobados de base de datos

Este manifiesto define que scripts forman parte del flujo aprobado de base de datos del SGRLA-IHSS. Cualquier script no listado aqui debe considerarse experimental, borrador o utilitario y no debe ejecutarse en ambientes oficiales sin revision previa.

## Regla de separacion

- Scripts aprobados: archivos numerados en la raiz de `database` y llamados por `00_EJECUCION_PRIMERA_VEZ.sql` o `00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql`.
- Scripts experimentales: deben ubicarse en `database/_experimental_no_ejecutar` y nunca deben ser llamados por scripts maestros.
- Utilitarios: deben ubicarse en `database/_utilitarios`; sirven como plantillas o consultas de apoyo, pero no forman parte del flujo automatico.

## Orden aprobado

### Primera instalacion

Ejecutar solo en base nueva o esquema vacio aprobado:

```sql
@00_EJECUCION_PRIMERA_VEZ.sql
```

Orden interno: `01`, `02`, `03`, `04`, `05`, `06`, `08`, `09`, `10`, `11`, `12`, `13`, `14`, `15`, `16`, `17`.

### Actualizacion segura

Ejecutar sobre ambientes existentes con datos:

```sql
@00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql
```

Orden interno: `03`, `04`, `05`, `06`, `08`, `09`, `10`, `11`, `12`, `13`, `14`, `15`, `16`, `17`.

`01_create_tables.sql` y `02_seed_data.sql` no se incluyen en actualizaciones porque el primero contiene `DROP` controlados y el segundo inserta semillas iniciales directas.

## Inventario

| Script | Uso | Idempotencia | Objetos principales | Respaldo recomendado |
|---|---|---|---|---|
| `01_create_tables.sql` | Primera instalacion | No para produccion existente | `RL_DOMINIO`, `RL_ROLES`, `RL_USUARIOS`, `RL_REFRESH_TOKENS`, `RL_PASSWORD_RESET_TOKENS`, `RL_AUDITORIA`, `RL_CONFIG_SISTEMA`, `RL_LOGIN_SLIDES`, secuencias base | Respaldo completo del esquema antes de ejecutar; no usar en base con datos reales |
| `02_seed_data.sql` | Primera instalacion | No idempotente | Dominios, roles, configuracion, slides, usuarios iniciales | No ejecutar sobre base existente sin depuracion previa |
| `03_create_modules_table.sql` | Primera instalacion y actualizacion | Idempotente | `RL_MODULOS`, `RL_USUARIO_MODULOS`, `SEQ_RL_MODULOS`, modulo 2 Usuarios | Respaldo de `RL_MODULOS`, `RL_USUARIO_MODULOS` si ya existen |
| `04_alter_config_sistema.sql` | Actualizacion | Idempotente por columnas y ruta | Columnas de configuracion/login/usuarios, modulo 3 Configuracion | `RL_CONFIG_SISTEMA`, `RL_LOGIN_SLIDES`, `RL_USUARIOS`, `RL_MODULOS`, `RL_USUARIO_MODULOS` |
| `05_register_monitoreo_listas.sql` | Actualizacion | Idempotente por ruta/asignacion | Modulo 4 Monitoreo de Listas | `RL_MODULOS`, `RL_USUARIO_MODULOS` |
| `06_alter_usuarios_change_pass.sql` | Actualizacion | Idempotente | Cambio obligatorio de clave y vigencia de clave temporal | `RL_USUARIOS`, `RL_CONFIG_SISTEMA` |
| `07` | Reservado | No aplica | Numero reservado/no usado | No aplica |
| `08_register_bitacora.sql` | Actualizacion | Idempotente por ruta/asignacion | Modulo 5 Bitacora | `RL_MODULOS`, `RL_USUARIO_MODULOS` |
| `09_create_detalle_evidencia.sql` | Actualizacion | Idempotente | `RL_DETALLE_EVIDENCIA`, `SEQ_RL_DETALLE_EVIDENCIA`, indices | `RL_DETALLE_EVIDENCIA` si ya existe |
| `10_register_tipo_listas_module.sql` | Actualizacion | Idempotente por ruta/asignacion | Modulo 6 Tipo Listas | `RL_MODULOS`, `RL_USUARIO_MODULOS` |
| `11_register_cargar_listas_module.sql` | Actualizacion | Idempotente por ruta/asignacion | Modulo 7 Cargar Listas | `RL_MODULOS`, `RL_USUARIO_MODULOS` |
| `12_register_coincidencias_patrono_module.sql` | Actualizacion | Idempotente por ruta/asignacion | Modulo 8 Coincidencias Patrono | `RL_MODULOS`, `RL_USUARIO_MODULOS` |
| `13_create_calificaciones_coincidencias.sql` | Actualizacion | Idempotente | `RL_CALIF_COINCIDENCIAS`, `SEQ_RL_CALIFICACIONES`, indices | `RL_CALIF_COINCIDENCIAS` si ya existe |
| `14_register_coincidencias_empleado_module.sql` | Actualizacion | Idempotente por ruta/asignacion | Modulo 9 Coincidencias Empleado | `RL_MODULOS`, `RL_USUARIO_MODULOS` |
| `15_update_detalle_evidencia_soft_delete.sql` | Actualizacion | Controlado; modifica constraint FK | Eliminacion logica de evidencias | `RL_DETALLE_EVIDENCIA` |
| `16_alter_lista_positivos_origen_registro.sql` | Actualizacion | Idempotente | `RL_LISTA_POSITIVOS.LSP_ORIGEN_REGISTRO`, constraint e indice | `RL_LISTA_POSITIVOS` |
| `17_validate_module_ids.sql` | Validacion | Solo lectura | Valida modulos 2 a 9 contra backend/frontend | No aplica |

## Modulos registrados y permisos iniciales

| MOD_ID | Ruta | Modulo | Permiso inicial |
|---:|---|---|---|
| 2 | `/usuarios` | Usuarios del Sistema | Usuarios administradores iniciales `USR_ID` 1 y/o 2 si existen |
| 3 | `/configuracion` | Configuracion del Sistema | `USR_ID` 1 y 2 si existen |
| 4 | `/monitoreo-listas` | Monitoreo de Listas | `USR_ID` 1 y 2 si existen |
| 5 | `/bitacora` | Bitacora de Sistema | `USR_ID` 1 y 2 si existen |
| 6 | `/tipo-listas` | Tipo Listas | `USR_ID` 1 y 2 si existen |
| 7 | `/cargar-listas` | Cargar Listas | `USR_ID` 1 y 2 si existen |
| 8 | `/coincidencias-patrono` | Coincidencias Patrono | `USR_ID` 1 y 2 si existen |
| 9 | `/coincidencias-empleado` | Coincidencias Empleado | `USR_ID` 1 y 2 si existen |

## Secuencias

- `SEQ_RL_DOMINIO`
- `SEQ_RL_ROLES`
- `SEQ_RL_USUARIOS`
- `SEQ_RL_REFRESH_TOKENS`
- `SEQ_RL_RESET_TOKENS`
- `SEQ_RL_AUDITORIA`
- `SEQ_RL_MODULOS` desde 10 para futuros modulos, ya que `2` a `9` estan reservados.
- `SEQ_RL_DETALLE_EVIDENCIA`
- `SEQ_RL_CALIFICACIONES`

## Validacion final obligatoria

Despues de ejecutar scripts, confirmar:

```sql
@17_validate_module_ids.sql
```

Si falla, no continuar con nuevos modulos hasta alinear `RL_MODULOS` y `RL_USUARIO_MODULOS` con los IDs esperados por backend y Angular.
