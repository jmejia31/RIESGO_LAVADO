# Ejecucion segura de scripts SQL

Este directorio queda organizado para diferenciar instalacion inicial y actualizaciones sobre bases con informacion existente.

## Reglas de cierre

- Solo se consideran aprobados los scripts numerados y paquetes modulares llamados por los maestros y documentados en `00_MANIFIESTO_SCRIPTS_APROBADOS.md`.
- Los scripts experimentales deben permanecer en `_experimental_no_ejecutar` y no deben ser llamados por scripts maestros.
- Los utilitarios de apoyo deben permanecer en `_utilitarios` y ejecutarse manualmente solo con aprobacion DBA.
- No ejecutar scripts de instalacion inicial sobre una base con datos reales sin respaldo y aprobacion DBA.
- No ejecutar `DROP TABLE`, `TRUNCATE` ni `DELETE` masivo para aplicar mejoras funcionales.
- No eliminar informacion de `RIESGO_LAVADO` ni de esquemas institucionales como `DNP_IHSS`.
- Ejecutar primero en ambiente de pruebas, validar resultados y luego coordinar ventana para produccion.
- Registrar fecha, usuario, ambiente y salida de SQLPlus para trazabilidad.

## Primera vez

Usar solo en ambiente nuevo o base vacia aprobada:

```sql
sqlplus usuario/password@servicio @00_EJECUCION_PRIMERA_VEZ.sql
```

Este flujo aplica la estructura base, semillas y mejoras del modulo. Aunque los scripts principales fueron blindados en lo posible, el archivo `01_create_tables.sql` sigue siendo un script de construccion inicial y no debe usarse para actualizar produccion.

## Actualizaciones seguras

Usar sobre ambientes existentes:

```sql
sqlplus usuario/password@servicio @00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql
```

Este flujo ejecuta scripts idempotentes o de registro controlado. Su objetivo es conservar informacion existente y evitar errores por reejecucion.

Antes de ejecutar en un ambiente con datos, usar respaldo institucional o la plantilla:

```sql
@_utilitarios/99_respaldo_pre_actualizacion_template.sql
```

La plantilla debe copiarse o ajustarse con un sufijo real de fecha/hora antes de usarla. No se ejecuta desde los scripts maestros.

## Validaciones recomendadas

Antes de ejecutar:

- Confirmar respaldo vigente de la base.
- Confirmar usuario Oracle y esquema destino.
- Confirmar permisos sobre tablas propias `RL_*` y lecturas necesarias hacia `DNP_IHSS`.
- Revisar que la aplicacion no este ejecutando operaciones criticas durante la ventana.

Despues de ejecutar:

- Validar acceso al sistema.
- Validar carga y consulta de listas.
- Validar coincidencias de patronos, empleados y personas naturales/juridicas.
- Validar registro de positivos, evidencia, origen `NOTICIA_PRENSA` y seguimiento inicial.
- Validar bitacora y permisos por modulo.
- Validar alineacion de modulos con:

```sql
@17_validate_module_ids.sql
```

Si la validacion falla, no avanzar al siguiente modulo hasta corregir `RL_MODULOS` y `RL_USUARIO_MODULOS` con apoyo DBA. La validacion final cubre los modulos 2 a 10, incluido Matrices de Riesgos.
