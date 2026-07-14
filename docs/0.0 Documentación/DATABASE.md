# Base de datos

## Motor y alcance

El sistema usa Oracle mediante `Oracle.ManagedDataAccess.Core`. Los scripts activos están en `database`; los documentos y evidencias históricas permanecen en `docs` y no constituyen por sí mismos un flujo ejecutable.

## Orden maestro

| Escenario | Entrada | Condición |
|---|---|---|
| Esquema nuevo | `database/00_EJECUCION_PRIMERA_VEZ.sql` | Base vacía, respaldo y aprobación DBA |
| Esquema existente | `database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql` | Respaldo validado, revisión de scripts y ventana aprobada |

El inventario, orden interno, objetos afectados e idempotencia están en `database/00_MANIFIESTO_SCRIPTS_APROBADOS.md`. El paquete activo `database/19_matrices_riesgos` crea y parametriza el módulo de matrices desde ambos maestros. El script `17_validate_module_ids.sql`, ejecutado al final, verifica la alineación de los módulos 2 a 10.

## Procedimiento

1. Identificar esquema y versión instalada.
2. Revisar cada script aplicable y sus privilegios.
3. Crear respaldo institucional o adaptar `_utilitarios/99_respaldo_pre_actualizacion_template.sql`.
4. Ejecutar primero en pruebas con `WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK`.
5. Registrar salida, validar objetos, constraints, índices, permisos y funcionalidad.
6. Ejecutar en producción solo con aprobación.

Ejemplo:

```sql
sqlplus usuario/password@servicio @database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql
```

No almacenar el comando con credenciales reales en archivos o logs compartidos.

## Reversión y riesgos

La reversión preferida es restaurar el respaldo aprobado. Cuando un script tenga rollback específico, debe probarse con la misma versión. `01_create_tables.sql` y `02_seed_data.sql` no son actualizaciones productivas; su ejecución sobre un esquema existente puede ser destructiva o duplicar datos.
