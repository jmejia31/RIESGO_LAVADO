# P07 — Identidad e inventario Oracle

## Estado

## RevisiÃ³n estÃ¡tica vigente

`SOURCE_CODE_SHA=c0cb48be2a1812b98b56ec93cda1c1509f03c4c3` es la base de esta revisiÃ³n documental. La reconciliaciÃ³n `17+8=25` estÃ¡ verificada en las fuentes versionadas; `PHYSICAL_STATUS=NOT_VERIFIED` permanece obligatorio hasta la consulta DBA autorizada.

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

## Hallazgo de alineación: reconciliado estáticamente

El preflight de despliegue deployment/matrices-riesgos/oracle/01_preflight_release.sql espera 25 tablas y 25 secuencias RL_MR_*, incluyendo objetos de fórmulas, funciones y parámetros. El script de transición 06 declara las 17 tablas y 17 secuencias operativas; el DDL 15 declara las ocho tablas y ocho secuencias de configuración de cálculo. La suma estática es 25/25.

La observación 17/17 frente a 25/25 queda resuelta a nivel de archivos versionados como dos grupos complementarios, no como dos versiones incompatibles. El preflight enumera las 25 tablas, pero verifica las secuencias mediante un conteo LIKE; el DBA debe confirmar físicamente los nombres exactos y la versión desplegada antes de cualquier ejecución.

## Evidencia pendiente

- Identidad física del ambiente y de la sesión.
- Inventario físico real y comparación con el objetivo aprobado.
- Versión exacta del paquete instalado.
- Confirmación de que la API y el DBA consultan el mismo destino.
- Dictamen del DBA sobre la correspondencia física 17+8=25 y los nombres exactos de las 25 secuencias.

## Matriz de objetos versionados

Las columnas PHYSICAL_STATUS, EVIDENCE y EXPECTED_STATUS no representan una lectura de Oracle. NOT_VERIFIED es obligatorio mientras no exista evidencia DBA del ambiente real.

| OBJECT_ID | OBJECT_NAME | OBJECT_TYPE | MODULE_OWNER | MODEL_GROUP | SOURCE_SCRIPT | VALIDATION_SCRIPT | EXPECTED_STATUS | PHYSICAL_STATUS | EVIDENCE | RISK | COMMENTS |
|---|---|---|---|---|---|---|---|---|---|---|---|
| OP-01 | RL_MR_FAMILIAS_FORMULARIO | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R12 | Parent of versions |
| OP-02 | RL_MR_VERSIONES_FORMULARIO | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Family/version lifecycle |
| OP-03 | RL_MR_CATALOGOS | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R12 | Catalog parent |
| OP-04 | RL_MR_ELEMENTOS_CATALOGO | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Catalog children |
| OP-05 | RL_MR_REGLAS_CALCULO | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Calculation rule metadata |
| OP-06 | RL_MR_RIESGOS | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Risk parent |
| OP-07 | RL_MR_EVALUACIONES_RIESGO | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Evaluation root |
| OP-08 | RL_MR_PROYECCIONES_EVALUACION | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Evaluation projection |
| OP-09 | RL_MR_FLUJOS_EVALUACION | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Evaluation workflow |
| OP-10 | RL_MR_CONTROLES_RIESGO | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Controls |
| OP-11 | RL_MR_EVALUACIONES_CONTROL | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Control evaluations |
| OP-12 | RL_MR_PLANES | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Mitigation plans |
| OP-13 | RL_MR_ACTIVIDADES | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Plan activities |
| OP-14 | RL_MR_EVIDENCIAS | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R05 | Evidence records |
| OP-15 | RL_MR_EVIDENCIAS_VINCULOS | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R05 | Generic evidence links |
| OP-16 | RL_MR_SENALES_ALERTA | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Alert signals |
| OP-17 | RL_MR_AUTOMONITOREO | TABLE | Matrices | 17_OPERATIVE_OBJECTS | 06_reconstruir_modelo_17_tablas.sql | validate_matrices_17_object_inventory.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Monitoring |
| CFG-01 | RL_MR_FORMULAS | TABLE | Matrices | 8_CALCULATION_OBJECTS | 15_ddl_configuracion_calculo_312.sql | validate_matrices_preoracle_readiness.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R01 | Formula catalog |
| CFG-02 | RL_MR_FORMULA_USOS | TABLE | Matrices | 8_CALCULATION_OBJECTS | 15_ddl_configuracion_calculo_312.sql | validate_matrices_preoracle_readiness.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Formula-to-form field use |
| CFG-03 | RL_MR_FORMULA_VERSIONES | TABLE | Matrices | 8_CALCULATION_OBJECTS | 15_ddl_configuracion_calculo_312.sql | validate_matrices_preoracle_readiness.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Versioned formula DSL |
| CFG-04 | RL_MR_FUNCION_ARGUMENTOS | TABLE | Matrices | 8_CALCULATION_OBJECTS | 15_ddl_configuracion_calculo_312.sql | validate_matrices_preoracle_readiness.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Typed function arguments |
| CFG-05 | RL_MR_FUNCIONES | TABLE | Matrices | 8_CALCULATION_OBJECTS | 15_ddl_configuracion_calculo_312.sql | validate_matrices_preoracle_readiness.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Function catalog |
| CFG-06 | RL_MR_FUNCION_VERSIONES | TABLE | Matrices | 8_CALCULATION_OBJECTS | 15_ddl_configuracion_calculo_312.sql | validate_matrices_preoracle_readiness.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Versioned functions |
| CFG-07 | RL_MR_PARAMETROS_CALCULO | TABLE | Matrices | 8_CALCULATION_OBJECTS | 15_ddl_configuracion_calculo_312.sql | validate_matrices_preoracle_readiness.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Parameter catalog |
| CFG-08 | RL_MR_PARAMETRO_VERSIONES | TABLE | Matrices | 8_CALCULATION_OBJECTS | 15_ddl_configuracion_calculo_312.sql | validate_matrices_preoracle_readiness.ps1 | DECLARED | NOT_VERIFIED | Static DDL | R04 | Versioned parameter values |

## Secuencias y grupos compartidos

La fuente 06 declara 17 secuencias operativas. La fuente 15 declara ocho secuencias de cálculo: SEQ_RL_MR_FORMULAS, SEQ_RL_MR_FORMULA_USOS, SEQ_RL_MR_FORMULA_VERSIONES, SEQ_RL_MR_FUNCION_ARGUMENTOS, SEQ_RL_MR_FUNCIONES, SEQ_RL_MR_FUNCION_VERSIONES, SEQ_RL_MR_PARAMETROS y SEQ_RL_MR_PARAMETRO_VERSIONES. La secuencia SEQ_RL_MR_PARAMETROS corresponde al catálogo RL_MR_PARAMETROS_CALCULO porque ese es el nombre exacto del DDL; no se renombra documentalmente.

    OPERATIVE_TABLES_COUNT=17
    CALCULATION_TABLES_COUNT=8
    TOTAL_TABLES_UNIQUE=25
    OPERATIVE_SEQUENCES_COUNT=17
    CALCULATION_SEQUENCES_COUNT=8
    TOTAL_SEQUENCES_UNIQUE=25
    P07_MODEL_RECONCILIATION=CONSISTENT_STATICALLY

## Evidencia fisica reportada por propietario y DBA (no reejecutada por COD)

La siguiente evidencia fue suministrada por el propietario y el DBA mediante consultas de solo lectura. Se incorpora con su procedencia y no se presenta como una consulta ejecutada por COD en esta intervencion.

```ini
EVIDENCE_SOURCE=OWNER_AND_DBA_REPORTED
ORACLE_IDENTITY_MATCHES_P06=REPORTED_PASS
OFFICIAL_TABLES=25
OFFICIAL_SEQUENCES=25
FOREIGN_KEYS_ENABLED_VALIDATED=33
RL_AUDITORIA_STATUS=VALID
SEQ_RL_AUDITORIA_STATUS=VALID
AUDIT_COUNT_BEFORE=1727
AUDIT_COUNT_AFTER=1728
PHYSICAL_EXECUTION_BY_COD=NO
```

El inventario de 25 tablas, 25 secuencias y el estado VALID de los objetos de auditoria son resultados reportados de la comprobacion fisica indicada. No se amplian a objetos, grants o ambientes no incluidos en la evidencia recibida.

SHARED_INSTITUTIONAL_OBJECTS: RL_USUARIOS, RL_AUDITORIA y SEQ_RL_AUDITORIA. Estos objetos no pertenecen al conjunto Matrices y requieren protección institucional; su existencia física no se verificó.

RETIRED_OBJECTS: los objetos listados en modelo_17_objetos.json bajo retired_tables y retired_sequences. Su ausencia física requiere evidencia DBA; no se concluye desde el inventario de archivos.

La validación estática demuestra la procedencia complementaria 17+8. No demuestra instalación, estado, grants, índices ni existencia física en Oracle.
