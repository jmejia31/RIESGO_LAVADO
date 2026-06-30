# Scripts experimentales - NO EJECUTAR

Esta carpeta queda reservada para borradores, pruebas locales o scripts aun no aprobados por el responsable tecnico/DBA.

Reglas:

- Ningun archivo de esta carpeta debe ser llamado por scripts maestros.
- Ningun archivo de esta carpeta debe ejecutarse en produccion.
- Para aprobar un script, primero debe revisarse, documentarse, probarse en ambiente controlado y moverse al flujo numerado de `database`.
- Si el script toca datos reales, debe incluir plan de respaldo, validacion posterior y rollback operativo.
