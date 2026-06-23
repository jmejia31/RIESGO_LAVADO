# Origen del Registro en Lista Positivos

## Objetivo

Este cambio permite identificar de donde nace un registro en `RL_LISTA_POSITIVOS` sin crear un modulo nuevo de seguimiento de noticias.

El caso de noticias queda cubierto como un origen operativo del positivo:

- `DNP_LISTAS`: coincidencia proveniente de DNP/listas de cautela.
- `MANUAL_CUMPLIMIENTO`: registro manual realizado por Cumplimiento.
- `NOTICIA_PRENSA`: noticia, prensa o medio externo.
- `OTRO`: origen no clasificado en las opciones anteriores.

## Base de datos

La informacion se guarda en:

`RIESGO_LAVADO.RL_LISTA_POSITIVOS.LSP_ORIGEN_REGISTRO`

El script incremental es:

`database/16_alter_lista_positivos_origen_registro.sql`

El script es idempotente: agrega la columna, constraint e indice solo si no existen. No elimina datos y no modifica registros anteriores.

## Backend

El campo viaja en `RegistrarPositivoDto` y `ExistingPositivoDto`.

El repositorio:

- guarda el origen al crear un positivo;
- actualiza el origen cuando el frontend lo envia;
- conserva el origen existente si un cliente viejo no envia el campo;
- lo incluye en auditoria de `INSERT` y `UPDATE`.

## Frontend

El modal `Registrar Motivo en Lista de Positivos` muestra el selector `Origen del Registro`.

Valores por defecto:

- coincidencias DNP/listas: `DNP_LISTAS`;
- registros manuales: `MANUAL_CUMPLIMIENTO`;
- registros existentes: se muestra el valor ya guardado.

## Reportes

La ficha individual PDF y Excel muestra el origen del registro en la seccion de informacion general.

## Alcance

No se crea modulo nuevo de noticias.

No se escribe en `DNP_IHSS`.

No se elimina informacion existente.
