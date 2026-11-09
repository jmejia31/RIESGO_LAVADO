# P07 — Integridad referencial y dependencias

## Alcance

Revisión estática de las relaciones declaradas por el modelo de transición y de sus dependencias visibles en el repositorio. No se consultó Oracle y no se afirma que las restricciones estén creadas o habilitadas en una instancia.

## Modelo estático de 17 tablas

El script 06 declara las siguientes tablas:

    RL_MR_FAMILIAS_FORMULARIO
    RL_MR_VERSIONES_FORMULARIO
    RL_MR_CATALOGOS
    RL_MR_ELEMENTOS_CATALOGO
    RL_MR_REGLAS_CALCULO
    RL_MR_RIESGOS
    RL_MR_EVALUACIONES_RIESGO
    RL_MR_PROYECCIONES_EVALUACION
    RL_MR_FLUJOS_EVALUACION
    RL_MR_CONTROLES_RIESGO
    RL_MR_EVALUACIONES_CONTROL
    RL_MR_PLANES
    RL_MR_ACTIVIDADES
    RL_MR_EVIDENCIAS
    RL_MR_EVIDENCIAS_VINCULOS
    RL_MR_SENALES_ALERTA
    RL_MR_AUTOMONITOREO

## Dependencias declaradas

La definición estática relaciona versiones con familias y usuarios; elementos con catálogos; riesgos, evaluaciones, proyecciones, flujos, controles, planes, señales y automonitoreo con sus entidades padre; y evidencias con usuarios y vínculos. Las referencias externas principales apuntan a RL_USUARIOS.

El detalle verificable permanece en database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql. La evidencia DBA debe comprobar en ALL_CONSTRAINTS/ALL_CONS_COLUMNS o equivalente autorizado:

- nombre y estado de cada restricción;
- tabla y columna padre/hija;
- columnas NOT NULL requeridas;
- índices y unicidad;
- ausencia de referencias a objetos retirados;
- ausencia de restricciones deshabilitadas o inválidas.

## Riesgo de versión incompatible

El preflight de release documenta un inventario de 25 objetos, mientras que el script 06 y el manifiesto de transición documentan 17. Hasta que el DBA determine cuál contrato corresponde al release autorizado, no es seguro interpretar un resultado parcial como integridad certificada.

## Evidencia que debe aportar el DBA

1. Salida sanitizada del inventario físico.
2. Matriz de restricciones esperadas frente a encontradas.
3. Índices y restricciones únicas verificadas.
4. Objetos inválidos y restricciones deshabilitadas: cero o justificación aprobada.
5. Confirmación de que no se alteraron datos para producir la evidencia.
6. Dictamen independiente sobre la discrepancia 17/17–25/25.

## Resultado P07

P07_REFERENTIAL_REVIEW=STATIC_PASS

P07_PHYSICAL_REFERENTIAL_CERTIFICATION=PENDING_DBA_EVIDENCE

## Matriz verificable de dependencias declaradas

La siguiente matriz se extrajo de los dos DDL versionados. EXPECTED_STATUS=DECLARED significa que la relación aparece en la fuente; PHYSICAL_STATUS=REQUIRES_DBA_EVIDENCE significa que no se consultó Oracle.

| DEPENDENCY_ID | CHILD_TABLE | CHILD_COLUMN | PARENT_TABLE | PARENT_COLUMN | CONSTRAINT_NAME | CONSTRAINT_TYPE | MODULE_BOUNDARY | SOURCE_FILE | EXPECTED_STATUS | PHYSICAL_STATUS | RISK |
|---|---|---|---|---|---|---|---|---|---|---|---|
| OP-FK-01 | RL_MR_VERSIONES_FORMULARIO | VER_FAMILIA_ID | RL_MR_FAMILIAS_FORMULARIO | FAM_ID | FK_RL_MR_VER_FAM | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-02 | RL_MR_VERSIONES_FORMULARIO | VER_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_VER_USR | FOREIGN_KEY | Matrices/shared | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-03 | RL_MR_ELEMENTOS_CATALOGO | ELE_CATALOGO_ID | RL_MR_CATALOGOS | CAT_ID | FK_RL_MR_ELE_CAT | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-04 | RL_MR_RIESGOS | RIE_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_RIE_USR | FOREIGN_KEY | Matrices/shared | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-05 | RL_MR_EVALUACIONES_RIESGO | EVA_RIESGO_ID | RL_MR_RIESGOS | RIE_ID | FK_RL_MR_EVA_RIE | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-06 | RL_MR_EVALUACIONES_RIESGO | EVA_VERSION_ID | RL_MR_VERSIONES_FORMULARIO | VER_ID | FK_RL_MR_EVA_VER | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-07 | RL_MR_EVALUACIONES_RIESGO | EVA_USR_REGISTRO | RL_USUARIOS | USR_ID | FK_RL_MR_EVA_USR | FOREIGN_KEY | Matrices/shared | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-08 | RL_MR_PROYECCIONES_EVALUACION | PROY_EVALUACION_ID | RL_MR_EVALUACIONES_RIESGO | EVA_ID | FK_RL_MR_PROY_EVA | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-09 | RL_MR_FLUJOS_EVALUACION | FLU_EVALUACION_ID | RL_MR_EVALUACIONES_RIESGO | EVA_ID | FK_RL_MR_FLU_EVA | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-10 | RL_MR_FLUJOS_EVALUACION | FLU_USR_ID | RL_USUARIOS | USR_ID | FK_RL_MR_FLU_USR | FOREIGN_KEY | Matrices/shared | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-11 | RL_MR_CONTROLES_RIESGO | CON_EVALUACION_ID | RL_MR_EVALUACIONES_RIESGO | EVA_ID | FK_RL_MR_CON_EVA | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-12 | RL_MR_EVALUACIONES_CONTROL | ECO_CONTROL_ID | RL_MR_CONTROLES_RIESGO | CON_ID | FK_RL_MR_ECO_CON | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-13 | RL_MR_PLANES | PLA_EVALUACION_ID | RL_MR_EVALUACIONES_RIESGO | EVA_ID | FK_RL_MR_PLA_EVA | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-14 | RL_MR_ACTIVIDADES | ACT_PLAN_ID | RL_MR_PLANES | PLA_ID | FK_RL_MR_ACT_PLAN | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-15 | RL_MR_EVIDENCIAS | EVI_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_EVI_USR | FOREIGN_KEY | Matrices/shared | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R05 |
| OP-FK-16 | RL_MR_EVIDENCIAS_VINCULOS | EVV_EVIDENCIA_ID | RL_MR_EVIDENCIAS | EVI_ID | FK_RL_MR_EVV_EVI | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R05 |
| OP-FK-17 | RL_MR_EVIDENCIAS_VINCULOS | EVV_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_EVV_USR | FOREIGN_KEY | Matrices/shared | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R05 |
| OP-FK-18 | RL_MR_SENALES_ALERTA | ALE_EVALUACION_ID | RL_MR_EVALUACIONES_RIESGO | EVA_ID | FK_RL_MR_ALE_EVA | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-19 | RL_MR_AUTOMONITOREO | MON_EVALUACION_ID | RL_MR_EVALUACIONES_RIESGO | EVA_ID | FK_RL_MR_MON_EVA | FOREIGN_KEY | Matrices/operativo | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| OP-FK-20 | RL_MR_AUTOMONITOREO | MON_USR_ID | RL_USUARIOS | USR_ID | FK_RL_MR_MON_USR | FOREIGN_KEY | Matrices/shared | 06_reconstruir_modelo_17_tablas.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R04 |
| CFG-FK-01 | RL_MR_FORMULAS | FOR_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_FOR_USR | FOREIGN_KEY | Matrices/shared | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-02 | RL_MR_FORMULA_VERSIONES | FOV_FORMULA_ID | RL_MR_FORMULAS | FOR_ID | FK_RL_MR_FOV_FOR | FOREIGN_KEY | Matrices/cálculo | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-03 | RL_MR_FORMULA_VERSIONES | FOV_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_FOV_USR | FOREIGN_KEY | Matrices/shared | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-04 | RL_MR_FORMULA_USOS | FUS_VERSION_FORMULARIO_ID | RL_MR_VERSIONES_FORMULARIO | VER_ID | FK_RL_MR_FUS_VERF | FOREIGN_KEY | Matrices/cálculo/operativo | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-05 | RL_MR_FORMULA_USOS | FUS_FORMULA_VERSION_ID | RL_MR_FORMULA_VERSIONES | FOV_ID | FK_RL_MR_FUS_FOV | FOREIGN_KEY | Matrices/cálculo | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-06 | RL_MR_FORMULA_USOS | FUS_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_FUS_USR | FOREIGN_KEY | Matrices/shared | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-07 | RL_MR_FUNCIONES | FUN_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_FUN_USR | FOREIGN_KEY | Matrices/shared | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-08 | RL_MR_FUNCION_VERSIONES | FUV_FUNCION_ID | RL_MR_FUNCIONES | FUN_ID | FK_RL_MR_FUV_FUN | FOREIGN_KEY | Matrices/cálculo | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-09 | RL_MR_FUNCION_VERSIONES | FUV_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_FUV_USR | FOREIGN_KEY | Matrices/shared | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-10 | RL_MR_FUNCION_ARGUMENTOS | FUA_FUNCION_VERSION_ID | RL_MR_FUNCION_VERSIONES | FUV_ID | FK_RL_MR_FUA_FUV | FOREIGN_KEY | Matrices/cálculo | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-11 | RL_MR_PARAMETRO_VERSIONES | PAV_PARAMETRO_ID | RL_MR_PARAMETROS_CALCULO | PAC_ID | FK_RL_MR_PAV_PAC | FOREIGN_KEY | Matrices/cálculo | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-12 | RL_MR_PARAMETROS_CALCULO | PAC_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_PAC_USR | FOREIGN_KEY | Matrices/shared | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |
| CFG-FK-13 | RL_MR_PARAMETRO_VERSIONES | PAV_USR_CREACION | RL_USUARIOS | USR_ID | FK_RL_MR_PAV_USR | FOREIGN_KEY | Matrices/shared | 15_ddl_configuracion_calculo_312.sql | DECLARED | REQUIRES_DBA_EVIDENCE | R02 |

## Unicidad, índices, auditoría y criterios físicos

Unicidades declaradas relevantes: FAM_CODIGO; VER_FAMILIA_ID + VER_VERSION; CAT_CODIGO; ELE_CATALOGO_ID + ELE_CODIGO; REG_CODIGO + REG_VERSION; RIE_CODIGO; PROY_EVALUACION_ID; EVV_EVIDENCIA_ID + EVV_TIPO_ENTIDAD + EVV_ENTIDAD_ID; FOR_CODIGO; FOV_FORMULA_ID + FOV_VERSION; FUS_VERSION_FORMULARIO_ID + FUS_CAMPO_CLAVE; FUN_CODIGO; FUV_FUNCION_ID + FUV_VERSION; FUA_FUNCION_VERSION_ID + FUA_POSICION; FUA_FUNCION_VERSION_ID + FUA_CODIGO; PAC_CODIGO; PAV_PARAMETRO_ID + PAV_VERSION.

Índices adicionales declarados incluyen IDX_RL_MR_VER_VIG, IDX_RL_MR_EVA_RIE, IDX_RL_MR_EVA_VER, IDX_RL_MR_PROY_BUSQ, IDX_RL_MR_CON_EVA, IDX_RL_MR_PLA_EVA, IDX_RL_MR_EVV_ENTIDAD, IDX_RL_MR_FOV_FOR, IDX_RL_MR_FUS_VERF, IDX_RL_MR_FUS_FOV, IDX_RL_MR_FUV_FUN, IDX_RL_MR_FUA_FUV y IDX_RL_MR_PAV_PAC. La creación física y el estado válido requieren DBA.

Auditoría: los scripts del modelo no crean RL_MR_AUDITORIA ni SEQ_RL_MR_AUDITORIA. La auditoría institucional RL_AUDITORIA es compartida y debe verificarse por separado; no se interpreta como una FK local del modelo.

## Criterios de lectura para DBA, no ejecutados aquí

El DBA debe consultar únicamente metadatos y agregados: ALL_CONSTRAINTS/ALL_CONS_COLUMNS para estado y columnas; ALL_INDEXES/ALL_IND_COLUMNS para índices; ALL_OBJECTS para INVALID; USER_CONSTRAINTS para restricciones deshabilitadas; y consultas NOT EXISTS para huérfanos en cada FK. Debe registrar si existen dependencias que impidan eliminaciones y conservar evidencia de auditoría. No se ejecutaron esas consultas en esta intervención.
