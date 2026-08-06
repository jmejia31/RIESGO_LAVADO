# Manifiesto de scripts aprobados de base de datos

Este manifiesto define los scripts que forman parte de los flujos automáticos aprobados del SGRLA-IHSS. Cualquier archivo no alcanzable desde los maestros debe considerarse manual, experimental, de transición o de apoyo y requiere autorización específica.

## Reglas generales

- Los scripts automáticos aprobados son los incluidos mediante `@@` por `00_EJECUCION_PRIMERA_VEZ.sql` o `00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql`.
- Los scripts destructivos o de transición nunca deben ser llamados desde un maestro automático.
- El flujo de actualización segura no puede alcanzar `DROP TABLE`, `TRUNCATE` ni `DELETE FROM`.
- Todo punto de entrada debe conservar `WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK`.
- Ningún script puede contener credenciales o cadenas de conexión reales.

## Primera instalación aprobada

Ejecutar únicamente sobre una base nueva o un esquema vacío aprobado:

```sql
@00_EJECUCION_PRIMERA_VEZ.sql
```

Orden interno:

```text
01_create_tables.sql
02_seed_data.sql
03_create_modules_table.sql
04_alter_config_sistema.sql
05_register_monitoreo_listas.sql
06_alter_usuarios_change_pass.sql
08_register_bitacora.sql
09_create_detalle_evidencia.sql
10_register_tipo_listas_module.sql
11_register_cargar_listas_module.sql
12_register_coincidencias_patrono_module.sql
13_create_calificaciones_coincidencias.sql
14_register_coincidencias_empleado_module.sql
15_update_detalle_evidencia_soft_delete.sql
16_alter_lista_positivos_origen_registro.sql
18_add_missing_comments.sql
17_validate_module_ids.sql
```

## Actualización segura aprobada

Ejecutar sobre ambientes existentes con datos:

```sql
@00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql
```

Orden interno:

```text
03_create_modules_table.sql
04_alter_config_sistema.sql
05_register_monitoreo_listas.sql
06_alter_usuarios_change_pass.sql
08_register_bitacora.sql
09_create_detalle_evidencia.sql
10_register_tipo_listas_module.sql
11_register_cargar_listas_module.sql
12_register_coincidencias_patrono_module.sql
13_create_calificaciones_coincidencias.sql
14_register_coincidencias_empleado_module.sql
15_update_detalle_evidencia_soft_delete.sql
16_alter_lista_positivos_origen_registro.sql
18_add_missing_comments.sql
17_validate_module_ids.sql
```

`01_create_tables.sql` y `02_seed_data.sql` no se incluyen en actualizaciones porque pertenecen exclusivamente a primera instalación.

## Inventario de scripts raíz

| Script | Uso aprobado | Observación |
|---|---|---|
| `00_EJECUCION_PRIMERA_VEZ.sql` | Maestro de primera instalación | Base nueva o esquema vacío. |
| `00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql` | Maestro de actualización | No debe alcanzar operaciones destructivas. |
| `01_create_tables.sql` | Primera instalación | No usar en una base con datos reales. |
| `02_seed_data.sql` | Primera instalación | Semillas iniciales directas. |
| `03_create_modules_table.sql` | Ambos flujos | Estructura y registro base de módulos. |
| `04_alter_config_sistema.sql` | Ambos flujos | Ajustes idempotentes de configuración. |
| `05_register_monitoreo_listas.sql` | Ambos flujos | Registra Monitoreo de Listas. |
| `06_alter_usuarios_change_pass.sql` | Ambos flujos | Vigencia y cambio obligatorio de contraseña. |
| `08_register_bitacora.sql` | Ambos flujos | Registra Bitácora. |
| `09_create_detalle_evidencia.sql` | Ambos flujos | Estructura de evidencia base. |
| `10_register_tipo_listas_module.sql` | Ambos flujos | Registra Tipo Listas. |
| `11_register_cargar_listas_module.sql` | Ambos flujos | Registra Cargar Listas. |
| `12_register_coincidencias_patrono_module.sql` | Ambos flujos | Registra Coincidencias Patrono. |
| `13_create_calificaciones_coincidencias.sql` | Ambos flujos | Calificaciones de coincidencias. |
| `14_register_coincidencias_empleado_module.sql` | Ambos flujos | Registra Coincidencias Empleado. |
| `15_update_detalle_evidencia_soft_delete.sql` | Ambos flujos | Eliminación lógica de evidencias. |
| `16_alter_lista_positivos_origen_registro.sql` | Ambos flujos | Origen de registros positivos. |
| `17_validate_module_ids.sql` | Validación final | Solo lectura. |
| `18_add_missing_comments.sql` | Ambos flujos | Comentarios de tablas y columnas. |

## Paquete 19 — Matrices de Riesgos

El paquete:

```text
19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql
```

permanece **fuera de los dos maestros automáticos** y su punto de entrada está bloqueado intencionalmente. Esta exclusión es obligatoria hasta completar la preparación, ejecución controlada y certificación física Oracle del modelo reducido.

La definición física objetivo es exclusivamente:

```text
19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql
```

El script `06` es manual, destructivo y no aprobado para ejecución automática. Solo podrá ejecutarse con:

- base Oracle exclusiva de pruebas;
- respaldo validado;
- esquema `RIESGO_LAVADO`;
- parámetro literal `EJECUTAR`;
- autorización expresa de Javier Mejía y del DBA.

Los instaladores heredados de 34 tablas fueron retirados de `19_matrices_riesgos/instalacion` y no deben restaurarse:

```text
01_create_rl_mr_estructura_dinamica.sql
02_create_rl_mr_restricciones_indices.sql
```

Los scripts `02_register_modulo_matrices_riesgos.sql` y `instalacion/03` a `05` permanecen preparados para una fase Oracle posterior, pero no forman parte del flujo automático vigente.

## Módulos registrados

| MOD_ID | Ruta | Módulo |
|---:|---|---|
| 2 | `/usuarios` | Usuarios del Sistema |
| 3 | `/configuracion` | Configuración del Sistema |
| 4 | `/monitoreo-listas` | Monitoreo de Listas |
| 5 | `/bitacora` | Bitácora de Sistema |
| 6 | `/tipo-listas` | Tipo Listas |
| 7 | `/cargar-listas` | Cargar Listas |
| 8 | `/coincidencias-patrono` | Coincidencias Patrono |
| 9 | `/coincidencias-empleado` | Coincidencias Empleado |
| 10 | `/matrices-riesgos` | Matrices de Riesgos — registro bloqueado hasta la fase Oracle correspondiente |

## Validaciones obligatorias

Desde la raíz del repositorio:

```powershell
./tools/validate_database_scripts.ps1
./scripts/validation/validate_matrices_preoracle_readiness.ps1
```

Además deben aprobar las validaciones dinámicas, el inventario exacto de 17 objetos, compilación, Backend, Frontend, cobertura y E2E.

La aprobación estática de estas puertas no equivale a certificación física Oracle.
