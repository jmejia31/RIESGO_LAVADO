# DB-01 — Archivado de `RL_AUDITORIA`

Este directorio contiene únicamente artefactos de **diagnóstico y diseño** para la política de archivado de la auditoría global.

## Estado

- Retención institucional aprobada: **NO DEFINIDA**.
- Borrado automático: **PROHIBIDO**.
- Purga manual: **NO AUTORIZADA por DB-01**.
- Tabla histórica física: **NO CREADA**.
- Movimiento físico de registros: **NO EJECUTADO**.
- Índices nuevos: **NO RECOMENDADOS con el volumen actual según DB-03**.

## Principio operativo

DB-01 adopta una estrategia `COPY_ONLY`: cualquier implementación física futura deberá copiar un lote elegible a un destino histórico autorizado, reconciliarlo y certificarlo antes de considerar cualquier fase posterior. El registro fuente en `RL_AUDITORIA` permanece intacto durante DB-01.

No se crea ningún `DBMS_SCHEDULER`, `DBMS_JOB`, trigger de purga ni proceso periódico de borrado.

## Diagnóstico de solo lectura

El archivo:

`01_db01_diagnostico_rl_auditoria_solo_lectura.sql`

obtiene únicamente métricas agregadas de volumen y crecimiento. No muestra CLOB de auditoría, correo de usuario, IP ni valores de negocio.

Ejecutar manualmente en el esquema autorizado y comprobar primero que `CURRENT_SCHEMA` sea `RIESGO_LAVADO`:

```text
@database/auditoria/archivado/01_db01_diagnostico_rl_auditoria_solo_lectura.sql
```

CI valida el contenido estáticamente; **no conecta ni ejecuta Oracle**.

## Bloqueos obligatorios antes de un archivo físico futuro

1. Cumplimiento/Legal debe aprobar por escrito el plazo de retención y la fecha de corte.
2. Deben definirse y revisar retenciones extraordinarias o `LEGAL_HOLD`.
3. Cualquier tabla/esquema histórico requiere autorización DDL separada.
4. La copia física requiere autorización DML separada y manifiesto de lote.
5. Debe reconciliarse origen contra histórico por cantidad e identificadores antes de certificar el lote.
6. DB-01 no autoriza eliminación automática ni manual de la fuente.
7. La consulta histórica debe conservar las mismas restricciones de acceso que la auditoría activa.
