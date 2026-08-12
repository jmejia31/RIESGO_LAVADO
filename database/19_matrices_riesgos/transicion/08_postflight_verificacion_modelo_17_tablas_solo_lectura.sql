-- ============================================================
-- MODULO MATRICES DE RIESGOS - POSTFLIGHT ORACLE DE SOLO LECTURA
-- Script: 08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql
-- Fase: 10 - Verificacion inmediata posterior a la transicion fisica
-- Uso: manual, despues del script 06 y antes de la certificacion Fase 11.
-- Este archivo NO crea, altera, elimina ni modifica datos.
-- Compatibilidad objetivo: Oracle 11g / SQL*Plus.
-- ============================================================

SET SERVEROUTPUT ON SIZE UNLIMITED
SET FEEDBACK ON
SET VERIFY OFF
SET PAGESIZE 500
SET LINESIZE 240
SET TRIMSPOOL ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

PROMPT ============================================================
PROMPT POSTFLIGHT ORACLE - MODELO REDUCIDO DE 17 TABLAS
PROMPT Verificacion de solo lectura. No ejecuta DDL ni DML.
PROMPT ============================================================

DECLARE
  v_schema_actual             VARCHAR2(128);
  v_usuario_sesion            VARCHAR2(128);
  v_nombre_base               VARCHAR2(128);
  v_host                      VARCHAR2(256);
  v_tabla_usuarios            NUMBER := 0;
  v_tabla_auditoria           NUMBER := 0;
  v_secuencia_auditoria       NUMBER := 0;
  v_tablas_matrices           NUMBER := 0;
  v_secuencias_matrices       NUMBER := 0;
  v_tablas_faltantes          NUMBER := 0;
  v_tablas_inesperadas        NUMBER := 0;
  v_secuencias_faltantes      NUMBER := 0;
  v_secuencias_inesperadas    NUMBER := 0;
  v_tablas_retiradas          NUMBER := 0;
  v_secuencias_retiradas      NUMBER := 0;
  v_tablas_sin_pk             NUMBER := 0;
  v_restricciones_inactivas   NUMBER := 0;
  v_objetos_invalidos         NUMBER := 0;
BEGIN
  SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA'),
         SYS_CONTEXT('USERENV', 'SESSION_USER'),
         SYS_CONTEXT('USERENV', 'DB_NAME'),
         SYS_CONTEXT('USERENV', 'SERVER_HOST')
    INTO v_schema_actual,
         v_usuario_sesion,
         v_nombre_base,
         v_host
    FROM DUAL;

  IF UPPER(v_schema_actual) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(
      -20401,
      'POSTFLIGHT BLOQUEADO: el esquema actual no es RIESGO_LAVADO. Detectado: ' || v_schema_actual
    );
  END IF;

  SELECT COUNT(*) INTO v_tabla_usuarios
    FROM USER_TABLES
   WHERE TABLE_NAME = 'RL_USUARIOS';

  SELECT COUNT(*) INTO v_tabla_auditoria
    FROM USER_TABLES
   WHERE TABLE_NAME = 'RL_AUDITORIA';

  SELECT COUNT(*) INTO v_secuencia_auditoria
    FROM USER_SEQUENCES
   WHERE SEQUENCE_NAME = 'SEQ_RL_AUDITORIA';

  IF v_tabla_usuarios <> 1
     OR v_tabla_auditoria <> 1
     OR v_secuencia_auditoria <> 1 THEN
    RAISE_APPLICATION_ERROR(
      -20402,
      'POSTFLIGHT BLOQUEADO: faltan objetos institucionales RL_USUARIOS, RL_AUDITORIA o SEQ_RL_AUDITORIA.'
    );
  END IF;

  SELECT COUNT(*) INTO v_tablas_matrices
    FROM USER_TABLES
   WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\';

  SELECT COUNT(*) INTO v_secuencias_matrices
    FROM USER_SEQUENCES
   WHERE SEQUENCE_NAME LIKE 'SEQ\_RL\_MR\_%' ESCAPE '\';

  SELECT COUNT(*) INTO v_tablas_faltantes
    FROM (
      SELECT COLUMN_VALUE AS NOMBRE
        FROM TABLE(sys.odcivarchar2list(
          'RL_MR_FAMILIAS_FORMULARIO',
          'RL_MR_VERSIONES_FORMULARIO',
          'RL_MR_CATALOGOS',
          'RL_MR_ELEMENTOS_CATALOGO',
          'RL_MR_REGLAS_CALCULO',
          'RL_MR_RIESGOS',
          'RL_MR_EVALUACIONES_RIESGO',
          'RL_MR_PROYECCIONES_EVALUACION',
          'RL_MR_FLUJOS_EVALUACION',
          'RL_MR_CONTROLES_RIESGO',
          'RL_MR_EVALUACIONES_CONTROL',
          'RL_MR_PLANES',
          'RL_MR_ACTIVIDADES',
          'RL_MR_EVIDENCIAS',
          'RL_MR_EVIDENCIAS_VINCULOS',
          'RL_MR_SENALES_ALERTA',
          'RL_MR_AUTOMONITOREO'
        ))
      MINUS
      SELECT TABLE_NAME
        FROM USER_TABLES
       WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
    );

  SELECT COUNT(*) INTO v_tablas_inesperadas
    FROM (
      SELECT TABLE_NAME
        FROM USER_TABLES
       WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
      MINUS
      SELECT COLUMN_VALUE AS NOMBRE
        FROM TABLE(sys.odcivarchar2list(
          'RL_MR_FAMILIAS_FORMULARIO',
          'RL_MR_VERSIONES_FORMULARIO',
          'RL_MR_CATALOGOS',
          'RL_MR_ELEMENTOS_CATALOGO',
          'RL_MR_REGLAS_CALCULO',
          'RL_MR_RIESGOS',
          'RL_MR_EVALUACIONES_RIESGO',
          'RL_MR_PROYECCIONES_EVALUACION',
          'RL_MR_FLUJOS_EVALUACION',
          'RL_MR_CONTROLES_RIESGO',
          'RL_MR_EVALUACIONES_CONTROL',
          'RL_MR_PLANES',
          'RL_MR_ACTIVIDADES',
          'RL_MR_EVIDENCIAS',
          'RL_MR_EVIDENCIAS_VINCULOS',
          'RL_MR_SENALES_ALERTA',
          'RL_MR_AUTOMONITOREO'
        ))
    );

  SELECT COUNT(*) INTO v_secuencias_faltantes
    FROM (
      SELECT COLUMN_VALUE AS NOMBRE
        FROM TABLE(sys.odcivarchar2list(
          'SEQ_RL_MR_FAMILIAS',
          'SEQ_RL_MR_VERSIONES',
          'SEQ_RL_MR_CATALOGOS',
          'SEQ_RL_MR_ELEMENTOS',
          'SEQ_RL_MR_REGLAS',
          'SEQ_RL_MR_RIESGOS',
          'SEQ_RL_MR_EVALUACIONES',
          'SEQ_RL_MR_PROYECCIONES',
          'SEQ_RL_MR_FLUJOS',
          'SEQ_RL_MR_CONTROLES',
          'SEQ_RL_MR_EVAL_CONTROLES',
          'SEQ_RL_MR_PLANES',
          'SEQ_RL_MR_ACTIVIDADES',
          'SEQ_RL_MR_EVIDENCIAS',
          'SEQ_RL_MR_EVI_VINCULOS',
          'SEQ_RL_MR_SENALES',
          'SEQ_RL_MR_AUTOMONITOREO'
        ))
      MINUS
      SELECT SEQUENCE_NAME
        FROM USER_SEQUENCES
       WHERE SEQUENCE_NAME LIKE 'SEQ\_RL\_MR\_%' ESCAPE '\'
    );

  SELECT COUNT(*) INTO v_secuencias_inesperadas
    FROM (
      SELECT SEQUENCE_NAME
        FROM USER_SEQUENCES
       WHERE SEQUENCE_NAME LIKE 'SEQ\_RL\_MR\_%' ESCAPE '\'
      MINUS
      SELECT COLUMN_VALUE AS NOMBRE
        FROM TABLE(sys.odcivarchar2list(
          'SEQ_RL_MR_FAMILIAS',
          'SEQ_RL_MR_VERSIONES',
          'SEQ_RL_MR_CATALOGOS',
          'SEQ_RL_MR_ELEMENTOS',
          'SEQ_RL_MR_REGLAS',
          'SEQ_RL_MR_RIESGOS',
          'SEQ_RL_MR_EVALUACIONES',
          'SEQ_RL_MR_PROYECCIONES',
          'SEQ_RL_MR_FLUJOS',
          'SEQ_RL_MR_CONTROLES',
          'SEQ_RL_MR_EVAL_CONTROLES',
          'SEQ_RL_MR_PLANES',
          'SEQ_RL_MR_ACTIVIDADES',
          'SEQ_RL_MR_EVIDENCIAS',
          'SEQ_RL_MR_EVI_VINCULOS',
          'SEQ_RL_MR_SENALES',
          'SEQ_RL_MR_AUTOMONITOREO'
        ))
    );

  SELECT COUNT(*) INTO v_tablas_retiradas
    FROM USER_TABLES
   WHERE TABLE_NAME IN (
     'RL_MR_EVI_APROBACION',
     'RL_MR_EVI_REVISION',
     'RL_MR_EVI_AUTOMONITOREO',
     'RL_MR_EVI_ALERTA',
     'RL_MR_EVI_ACTIVIDAD',
     'RL_MR_EVI_PLAN',
     'RL_MR_EVI_CONTROL',
     'RL_MR_EVI_EVALUACION',
     'RL_MR_EVI_RIESGO',
     'RL_MR_DETALLES_IMPORTACION',
     'RL_MR_LOTES_IMPORTACION',
     'RL_MR_TRAZAS_CALCULO',
     'RL_MR_AUDITORIA',
     'RL_MR_PERMISOS_FORMULARIO',
     'RL_MR_APROBACIONES_FORMULARIO',
     'RL_MR_CAMPOS_FORMULARIO',
     'RL_MR_RELACIONES_RIESGO',
     'RL_MR_REVISIONES_EVALUACION',
     'RL_MR_CRITERIOS',
     'RL_MR_DETALLE',
     'RL_MR_ESCALAS',
     'RL_MR_FACTORES',
     'RL_MR_MATRICES',
     'RL_MR_MODELOS',
     'RL_MR_VARIABLES'
   );

  SELECT COUNT(*) INTO v_secuencias_retiradas
    FROM USER_SEQUENCES
   WHERE SEQUENCE_NAME IN (
     'SEQ_RL_MR_AUDITORIA',
     'SEQ_RL_MR_TRAZAS',
     'SEQ_RL_MR_REVISIONES'
   );

  SELECT COUNT(*) INTO v_tablas_sin_pk
    FROM (
      SELECT TABLE_NAME
        FROM USER_TABLES
       WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
      MINUS
      SELECT TABLE_NAME
        FROM USER_CONSTRAINTS
       WHERE CONSTRAINT_TYPE = 'P'
         AND STATUS = 'ENABLED'
         AND TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
    );

  SELECT COUNT(*) INTO v_restricciones_inactivas
    FROM USER_CONSTRAINTS
   WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
     AND STATUS <> 'ENABLED';

  SELECT COUNT(*) INTO v_objetos_invalidos
    FROM USER_OBJECTS
   WHERE OBJECT_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
     AND STATUS <> 'VALID';

  DBMS_OUTPUT.PUT_LINE('IDENTIDAD DEL AMBIENTE');
  DBMS_OUTPUT.PUT_LINE('  Base de datos              : ' || NVL(v_nombre_base, '(no disponible)'));
  DBMS_OUTPUT.PUT_LINE('  Host                       : ' || NVL(v_host, '(no disponible)'));
  DBMS_OUTPUT.PUT_LINE('  Usuario de sesion          : ' || v_usuario_sesion);
  DBMS_OUTPUT.PUT_LINE('  Esquema actual             : ' || v_schema_actual);
  DBMS_OUTPUT.PUT_LINE('  Fecha servidor             : ' || TO_CHAR(SYSDATE, 'YYYY-MM-DD HH24:MI:SS'));
  DBMS_OUTPUT.PUT_LINE('');
  DBMS_OUTPUT.PUT_LINE('RESULTADO DEL MODELO');
  DBMS_OUTPUT.PUT_LINE('  Tablas RL_MR_*             : ' || v_tablas_matrices || ' / 17');
  DBMS_OUTPUT.PUT_LINE('  Secuencias SEQ_RL_MR_*     : ' || v_secuencias_matrices || ' / 17');
  DBMS_OUTPUT.PUT_LINE('  Tablas faltantes           : ' || v_tablas_faltantes);
  DBMS_OUTPUT.PUT_LINE('  Tablas inesperadas         : ' || v_tablas_inesperadas);
  DBMS_OUTPUT.PUT_LINE('  Secuencias faltantes       : ' || v_secuencias_faltantes);
  DBMS_OUTPUT.PUT_LINE('  Secuencias inesperadas     : ' || v_secuencias_inesperadas);
  DBMS_OUTPUT.PUT_LINE('  Tablas retiradas presentes : ' || v_tablas_retiradas);
  DBMS_OUTPUT.PUT_LINE('  Secuencias retiradas       : ' || v_secuencias_retiradas);
  DBMS_OUTPUT.PUT_LINE('  Tablas sin PK habilitada   : ' || v_tablas_sin_pk);
  DBMS_OUTPUT.PUT_LINE('  Restricciones inactivas    : ' || v_restricciones_inactivas);
  DBMS_OUTPUT.PUT_LINE('  Objetos invalidos RL_MR_*  : ' || v_objetos_invalidos);

  IF v_tablas_matrices <> 17
     OR v_secuencias_matrices <> 17
     OR v_tablas_faltantes <> 0
     OR v_tablas_inesperadas <> 0
     OR v_secuencias_faltantes <> 0
     OR v_secuencias_inesperadas <> 0
     OR v_tablas_retiradas <> 0
     OR v_secuencias_retiradas <> 0
     OR v_tablas_sin_pk <> 0
     OR v_restricciones_inactivas <> 0
     OR v_objetos_invalidos <> 0 THEN
    RAISE_APPLICATION_ERROR(
      -20403,
      'POSTFLIGHT FALLIDO: el inventario fisico no coincide con el modelo reducido aprobado.'
    );
  END IF;

  DBMS_OUTPUT.PUT_LINE('');
  DBMS_OUTPUT.PUT_LINE('POSTFLIGHT CORRECTO: inventario fisico 17/17 sin objetos heredados detectados.');
END;
/

PROMPT ============================================================
PROMPT TABLAS DEL MODELO REDUCIDO
PROMPT ============================================================
COLUMN TABLE_NAME FORMAT A45
COLUMN NUM_ROWS FORMAT 999999999999
COLUMN LAST_ANALYZED FORMAT A20
SELECT TABLE_NAME,
       NUM_ROWS,
       TO_CHAR(LAST_ANALYZED, 'YYYY-MM-DD HH24:MI:SS') AS LAST_ANALYZED
  FROM USER_TABLES
 WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
 ORDER BY TABLE_NAME ASC;

PROMPT ============================================================
PROMPT SECUENCIAS DEL MODELO REDUCIDO
PROMPT ============================================================
COLUMN SEQUENCE_NAME FORMAT A45
COLUMN LAST_NUMBER FORMAT 99999999999999999999
SELECT SEQUENCE_NAME,
       MIN_VALUE,
       INCREMENT_BY,
       LAST_NUMBER,
       CACHE_SIZE
  FROM USER_SEQUENCES
 WHERE SEQUENCE_NAME LIKE 'SEQ\_RL\_MR\_%' ESCAPE '\'
 ORDER BY SEQUENCE_NAME ASC;

PROMPT ============================================================
PROMPT RESTRICCIONES DEL MODELO REDUCIDO
PROMPT ============================================================
COLUMN CONSTRAINT_NAME FORMAT A45
COLUMN CONSTRAINT_TYPE FORMAT A5
COLUMN STATUS FORMAT A12
SELECT TABLE_NAME,
       CONSTRAINT_NAME,
       CONSTRAINT_TYPE,
       STATUS,
       VALIDATED
  FROM USER_CONSTRAINTS
 WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
 ORDER BY TABLE_NAME ASC, CONSTRAINT_TYPE ASC, CONSTRAINT_NAME ASC;

PROMPT ============================================================
PROMPT INDICES DEL MODELO REDUCIDO
PROMPT ============================================================
COLUMN INDEX_NAME FORMAT A45
COLUMN UNIQUENESS FORMAT A12
SELECT TABLE_NAME,
       INDEX_NAME,
       UNIQUENESS,
       STATUS
  FROM USER_INDEXES
 WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
 ORDER BY TABLE_NAME ASC, INDEX_NAME ASC;

PROMPT ============================================================
PROMPT RESUMEN DEL POSTFLIGHT
PROMPT ============================================================
PROMPT 1. Conservar la salida completa como evidencia sin secretos.
PROMPT 2. Asociar la salida al commit y hashes autorizados.
PROMPT 3. No declarar certificacion funcional; corresponde a la Fase 11.
PROMPT 4. Si falla el inventario, detener el proceso y aplicar contingencia.
PROMPT 5. Este archivo no ejecuta ni autoriza el script 06.
PROMPT ============================================================

EXIT SUCCESS
