# P07 — Identidad e inventario Oracle

## Estado

| Campo | Valor |
|---|---|
| Expediente | P07 — preparación DBA |
| Revisión | Estática, ejecutada sobre SHA db701c7456ca83568479933175a4283e73d955f8 |
| Evidencia primaria | Código, scripts SQL y documentación versionada del repositorio |
| Oracle institucional contactado | NO |
| Resultado | READY_FOR_DBA_REVIEW |

Este documento no certifica que el esquema desplegado tenga el inventario descrito. La comprobación física requiere ejecución controlada por un DBA autorizado en el ambiente identificado.

## Fuentes revisadas

- DATABASE.md: perfiles PRODUCTION y LOCAL_XE, resolución fail-closed y prohibición de secretos versionados.
- database/00_MANIFIESTO_SCRIPTS_APROBADOS.md: alcance de los maestros y cuarentena del paquete de Matrices.
- database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql: consultas previstas de identidad e inventario, sin autorización de instalación.
- database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql: definición histórica del objetivo de 17 tablas y 17 secuencias.
- backend/RL.API/Infrastructure/Database/DatabaseProfileResolver.cs: correspondencia de perfil de la API y rechazo de perfiles desconocidos.

## Identidad que debe aportar el DBA

La evidencia de ejecución debe conservar el resultado sanitizado de consultas de solo lectura que demuestren, como mínimo:

    SESSION_USER
    CURRENT_SCHEMA
    SERVICE_NAME
    DB_UNIQUE_NAME
    DB_NAME
    INSTANCE_NAME
    SERVER_HOST o identificador equivalente controlado

Los valores deben corresponder al ambiente autorizado y a la conexión efectiva utilizada por la API. No se aceptan capturas sin contexto, declaraciones manuales ni cadenas de conexión publicadas.

## Inventario estático objetivo

El script de transición declara 17 tablas y 17 secuencias para el modelo reducido. El inventario exacto está documentado en el script y en el manifiesto database/19_matrices_riesgos/transicion/modelo_17_objetos.json. Su presencia en archivos no demuestra que se encuentre instalado.

El preflight 07 está diseñado para informar, mediante consultas de solo lectura:

- esquema y sesión;
- objetos base requeridos: RL_USUARIOS, RL_AUDITORIA y SEQ_RL_AUDITORIA;
- tablas y secuencias RL_MR_*;
- conteos de registros;
- objetos inválidos;
- restricciones deshabilitadas.

## Hallazgo de alineación bloqueante

El preflight de despliegue deployment/matrices-riesgos/oracle/01_preflight_release.sql espera 25 tablas y 25 secuencias RL_MR_*, incluyendo objetos de fórmulas, funciones y parámetros. En cambio, el script de transición 06 declara 17 tablas y 17 secuencias y no declara esas ocho familias adicionales.

Esto es una discrepancia documental/deployment que debe resolver el DBA y el responsable técnico antes de cualquier ejecución. No se resuelve suponiendo que ambos inventarios representan la misma versión.

## Evidencia pendiente

- Identidad física del ambiente y de la sesión.
- Inventario físico real y comparación con el objetivo aprobado.
- Versión exacta del paquete instalado.
- Confirmación de que la API y el DBA consultan el mismo destino.
- Dictamen del DBA sobre la discrepancia 17/17 frente a 25/25.
