# Base de datos

## Motor y alcance

El sistema usa Oracle mediante `Oracle.ManagedDataAccess.Core`. Los scripts activos están en `database`; los documentos y evidencias históricas permanecen en `docs` y no constituyen por sí mismos un flujo ejecutable.

## Orden maestro

| Escenario | Entrada | Condición |
|---|---|---|
| Esquema nuevo | `database/00_EJECUCION_PRIMERA_VEZ.sql` | Base vacía, respaldo y aprobación DBA |
| Esquema existente | `database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql` | Respaldo validado, revisión de scripts y ventana aprobada |

El inventario, orden interno, objetos afectados e idempotencia están en `database/00_MANIFIESTO_SCRIPTS_APROBADOS.md`. El paquete activo `database/19_matrices_riesgos` crea y parametriza el módulo de matrices desde ambos maestros. El script `17_validate_module_ids.sql`, ejecutado al final, verifica la alineación de los módulos 2 a 10.

Los scripts históricos `01` a `18` permanecen en la raíz por compatibilidad. Los módulos posteriores se agregan como directorios numerados, con un único archivo `00_APLICAR_*.sql` invocado desde ambos maestros.

## Validación estática

Antes de entregar cualquier cambio de base de datos, ejecutar:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_database_scripts.ps1
```

El control comprueba el orden exacto de los maestros, la existencia y resolución relativa de cada `@@include`, la cobertura del manifiesto, la integridad de los paquetes modulares y el cierre con `17_validate_module_ids.sql`. También rechaza `DROP TABLE`, `TRUNCATE` y `DELETE FROM` si son alcanzables desde `00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql`.

## Selección de runtime

RL.API requiere `Database:Profile` en todos los entornos. Los únicos valores son `PRODUCTION` y `LOCAL_XE`; el primero lee exclusivamente `ConnectionStrings:OracleDB_Production` y el segundo exclusivamente `ConnectionStrings:OracleDB_LocalXE`. La clave genérica `OracleDB`, alias de perfil y fallback entre destinos no se aceptan. Un perfil desconocido, una cadena/credencial ausente o un error de conexión hacen fallar el arranque o la operación solicitada sin reintentar contra otra base.

La selección usa la precedencia estándar de `WebApplication.CreateBuilder`: argumentos de línea de comandos, variables de entorno, User Secrets (Development), `appsettings.{Environment}.json` y `appsettings.json`. El nombre de variable para el perfil es `Database__Profile`; las cadenas externas usan `ConnectionStrings__OracleDB_Production` o `ConnectionStrings__OracleDB_LocalXE`. No mantener otro selector en repositorios.

El uso diario puede seleccionar explícitamente `PRODUCTION`; QA, CI, Oracle integration y E2E seleccionan explícitamente `LOCAL_XE`. `LOCAL_XE` solo admite loopback y servicio `XE`; `PRODUCTION` rechaza loopback/XE. CI no necesita secretos productivos.

Producción debe suministrar su cadena/credencial desde el secret store de despliegue aprobado. En local, User Secrets puede almacenar los valores no versionados. No incluir contraseñas ni cadenas reales en archivos tracked, comandos compartidos, logs o documentación.

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
