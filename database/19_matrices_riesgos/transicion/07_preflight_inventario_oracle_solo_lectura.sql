-- ============================================================
-- MODULO MATRICES DE RIESGOS - PREFLIGHT ORACLE DE SOLO LECTURA
-- Script: 07_preflight_inventario_oracle_solo_lectura.sql
-- Fase: 9 - Preparacion del ambiente y expediente de autorizacion
-- Uso: manual, sin credenciales embebidas y antes de cualquier DDL.
-- Este archivo NO autoriza ni ejecuta el script 06.
-- Compatibilidad objetivo: Oracle 11g / SQL*Plus.
-- ============================================================

SET SERVEROUTPUT ON SIZE UNLIMITED
SET FEEDBACK ON
SET VERIFY OFF
SET PAGESIZE 500
SET LINESIZE 220
SET TRIMSPOOL ON
WHENEVER SQLERROR EXIT SQL.SQLCODE

PROMPT ============================================================
PROMPT PREFLIGHT ORACLE DE MATRICES DE RIESGOS - SOLO LECTURA
PROMPT No crea, altera ni elimina objetos. No modifica datos.
PROMPT ============================================================

DECLARE
  v_schema_actual       VARCHAR2(128);
  v_usuario_sesion      VARCHAR2(128);
  v_nombre_base         VARCHAR2(128);
  v_host                VARCHAR2(256);
  v_tabla_usuarios      NUMBER := 0;
  v_tabla_auditoria     NUMBER := 0;
  v_secuencia_auditoria NUMBER := 0;
  v_tablas_matrices     NUMBER := 0;
  v_secuencias_matrices NUMBER := 0;
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
      -20301,
      'PREFLIGHT BLOQUEADO: el esquema actual no es RIESGO_LAVADO. Detectado: ' || v_schema_actual
    );
  END IF;

  SELECT COUNT(*)
    INTO v_tabla_usuarios
    FROM USER_TABLES
   WHERE TABLE_NAME = 'RL_USUARIOS';

  SELECT COUNT(*)
    INTO v_tabla_auditoria
    FROM USER_TABLES
   WHERE TABLE_NAME = 'RL_AUDITORIA';

  SELECT COUNT(*)
    INTO v_secuencia_auditoria
    FROM USER_SEQUENCES
   WHERE SEQUENCE_NAME = 'SEQ_RL_AUDITORIA';

  SELECT COUNT(*)
    INTO v_tablas_matrices
    FROM USER_TABLES
   WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\';

  SELECT COUNT(*)
    INTO v_secuencias_matrices
    FROM USER_SEQUENCES
   WHERE SEQUENCE_NAME LIKE 'SEQ\_RL\_MR\_%' ESCAPE '\';

  IF v_tabla_usuarios <> 1 THEN
    RAISE_APPLICATION_ERROR(-20302, 'PREFLIGHT BLOQUEADO: no existe exactamente una tabla RL_USUARIOS.');
  END IF;

  IF v_tabla_auditoria <> 1 OR v_secuencia_auditoria <> 1 THEN
    RAISE_APPLICATION_ERROR(
      -20303,
      'PREFLIGHT BLOQUEADO: faltan RL_AUDITORIA o SEQ_RL_AUDITORIA institucionales.'
    );
  END IF;

  DBMS_OUTPUT.PUT_LINE('IDENTIDAD DEL AMBIENTE');
  DBMS_OUTPUT.PUT_LINE('  Base de datos    : ' || NVL(v_nombre_base, '(no disponible)'));
  DBMS_OUTPUT.PUT_LINE('  Host             : ' || NVL(v_host, '(no disponible)'));
  DBMS_OUTPUT.PUT_LINE('  Usuario de sesion: ' || v_usuario_sesion);
  DBMS_OUTPUT.PUT_LINE('  Esquema actual   : ' || v_schema_actual);
  DBMS_OUTPUT.PUT_LINE('  Fecha servidor   : ' || TO_CHAR(SYSDATE, 'YYYY-MM-DD HH24:MI:SS'));
  DBMS_OUTPUT.PUT_LINE('');
  DBMS_OUTPUT.PUT_LINE('OBJETOS INSTITUCIONALES');
  DBMS_OUTPUT.PUT_LINE('  RL_USUARIOS       : ' || v_tabla_usuarios);
  DBMS_OUTPUT.PUT_LINE('  RL_AUDITORIA      : ' || v_tabla_auditoria);
  DBMS_OUTPUT.PUT_LINE('  SEQ_RL_AUDITORIA  : ' || v_secuencia_auditoria);
  DBMS_OUTPUT.PUT_LINE('');
  DBMS_OUTPUT.PUT_LINE('INVENTARIO RL_MR_* ACTUAL');
  DBMS_OUTPUT.PUT_LINE('  Tablas            : ' || v_tablas_matrices);
  DBMS_OUTPUT.PUT_LINE('  Secuencias        : ' || v_secuencias_matrices);
END;
/

PROMPT ============================================================
PROMPT TABLAS RL_MR_* EXISTENTES
PROMPT ============================================================
COLUMN TABLE_NAME FORMAT A45
COLUMN NUM_ROWS FORMAT 999999999999
COLUMN LAST_ANALYZED FORMAT A20
SELECT TABLE_NAME,
       NUM_ROWS,
       TO_CHAR(LAST_ANALYZED, 'YYYY-MM-DD HH24:MI:SS') AS LAST_ANALYZED
  FROM USER_TABLES
 WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
 ORDER BY TABLE_NAME;

PROMPT ============================================================
PROMPT SECUENCIAS RL_MR_* EXISTENTES
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
 ORDER BY SEQUENCE_NAME;

PROMPT ============================================================
PROMPT CONTEO REAL DE REGISTROS EN TABLAS RL_MR_*
PROMPT ============================================================
DECLARE
  v_cantidad       NUMBER := 0;
  v_total_registros NUMBER := 0;
BEGIN
  FOR r IN (
    SELECT TABLE_NAME
      FROM USER_TABLES
     WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
     ORDER BY TABLE_NAME
  ) LOOP
    EXECUTE IMMEDIATE
      'SELECT COUNT(*) FROM ' || DBMS_ASSERT.ENQUOTE_NAME(r.TABLE_NAME, FALSE)
      INTO v_cantidad;

    v_total_registros := v_total_registros + v_cantidad;
    DBMS_OUTPUT.PUT_LINE(RPAD(r.TABLE_NAME, 45) || ' = ' || TO_CHAR(v_cantidad));
  END LOOP;

  DBMS_OUTPUT.PUT_LINE('TOTAL REGISTROS RL_MR_* = ' || TO_CHAR(v_total_registros));
  IF v_total_registros > 0 THEN
    DBMS_OUTPUT.PUT_LINE(
      'BLOQUEANTE DOCUMENTAL: existen datos RL_MR_*. Debe existir una decision escrita sobre su disposicion antes de la Fase 10.'
    );
  ELSE
    DBMS_OUTPUT.PUT_LINE('RESULTADO: no se detectaron registros RL_MR_* en el esquema.');
  END IF;
END;
/

PROMPT ============================================================
PROMPT OBJETOS INVALIDOS DEL ESQUEMA
PROMPT ============================================================
COLUMN OBJECT_NAME FORMAT A45
COLUMN OBJECT_TYPE FORMAT A25
COLUMN STATUS FORMAT A12
SELECT OBJECT_NAME,
       OBJECT_TYPE,
       STATUS
  FROM USER_OBJECTS
 WHERE STATUS <> 'VALID'
 ORDER BY OBJECT_TYPE, OBJECT_NAME;

PROMPT ============================================================
PROMPT RESTRICCIONES DESHABILITADAS EN OBJETOS RL_MR_*
PROMPT ============================================================
COLUMN CONSTRAINT_NAME FORMAT A45
COLUMN CONSTRAINT_TYPE FORMAT A5
SELECT TABLE_NAME,
       CONSTRAINT_NAME,
       CONSTRAINT_TYPE,
       STATUS
  FROM USER_CONSTRAINTS
 WHERE TABLE_NAME LIKE 'RL\_MR\_%' ESCAPE '\'
   AND STATUS <> 'ENABLED'
 ORDER BY TABLE_NAME, CONSTRAINT_NAME;

PROMPT ============================================================
PROMPT RESUMEN DEL PREFLIGHT
PROMPT ============================================================
PROMPT 1. Conservar la salida completa como evidencia sin secretos.
PROMPT 2. Registrar instancia, esquema, fecha, DBA y hash del archivo ejecutado.
PROMPT 3. No continuar si existen datos sin disposicion autorizada.
PROMPT 4. No ejecutar el script 06 sin respaldo restaurable y autorizacion separada.
PROMPT 5. Este resultado no certifica el modelo de 17 tablas.
PROMPT ============================================================

EXIT SUCCESS
