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
 ORDER BY TABLE_NAME ASC;

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
 ORDER BY SEQUENCE_NAME ASC;

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
     ORDER BY TABLE_NAME ASC
  ) LOOP
    EXECUTE IMMEDIATE -- NOSONAR: consulta dinámica de solo lectura; tabla tomada de USER_TABLES y delimitada con DBMS_ASSERT.ENQUOTE_NAME.
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
PROMPT INTEGRIDAD DE VERSIONES - SOLO LECTURA
PROMPT ============================================================
PROMPT Estados contractuales: DRAFT, IN_REVIEW, APPROVED, PUBLISHED, RETIRED, ARCHIVED
PROMPT PUBLISHED historica no vigente es una metrica informativa, no un error.

COLUMN VER_ESTADO FORMAT A12
COLUMN VER_VIGENTE FORMAT 9
COLUMN CANTIDAD FORMAT 999999999999
SELECT VER_ESTADO,
       VER_VIGENTE,
       COUNT(*) AS CANTIDAD
  FROM RL_MR_VERSIONES_FORMULARIO
 GROUP BY VER_ESTADO, VER_VIGENTE
 ORDER BY VER_ESTADO, VER_VIGENTE;

DECLARE
  v_total                         NUMBER;
  v_invalid_state                 NUMBER;
  v_invalid_vigente               NUMBER;
  v_vigente_non_published         NUMBER;
  v_multiple_vigente              NUMBER;
  v_pub_hist_nonvigente           NUMBER;
  v_invalid_hash_format           NUMBER;
  v_hash_uncheckable              NUMBER;
BEGIN
  SELECT COUNT(*)
    INTO v_total
    FROM RL_MR_VERSIONES_FORMULARIO;

  SELECT COUNT(*)
    INTO v_invalid_state
    FROM RL_MR_VERSIONES_FORMULARIO
   WHERE VER_ESTADO NOT IN (
           'DRAFT', 'IN_REVIEW', 'APPROVED', 'PUBLISHED', 'RETIRED', 'ARCHIVED'
         )
      OR VER_ESTADO IS NULL;

  SELECT COUNT(*)
    INTO v_invalid_vigente
    FROM RL_MR_VERSIONES_FORMULARIO
   WHERE VER_VIGENTE NOT IN (0, 1)
      OR VER_VIGENTE IS NULL;

  SELECT COUNT(*)
    INTO v_vigente_non_published
    FROM RL_MR_VERSIONES_FORMULARIO
   WHERE VER_VIGENTE = 1
     AND VER_ESTADO <> 'PUBLISHED';

  SELECT COUNT(*)
    INTO v_multiple_vigente
    FROM (
      SELECT VER_FAMILIA_ID
        FROM RL_MR_VERSIONES_FORMULARIO
       WHERE VER_VIGENTE = 1
       GROUP BY VER_FAMILIA_ID
      HAVING COUNT(*) > 1
    );

  SELECT COUNT(*)
    INTO v_pub_hist_nonvigente
    FROM RL_MR_VERSIONES_FORMULARIO
   WHERE VER_ESTADO = 'PUBLISHED'
     AND VER_VIGENTE = 0;

  -- Solo se valida formato. No se afirma SHA-256 del contenido con un
  -- fragmento de CLOB: la equivalencia exacta con UTF-8/canonicalizacion
  -- del Backend requiere leer el CLOB completo fuera de esta auditoria SQL.
  SELECT COUNT(*)
    INTO v_invalid_hash_format
    FROM RL_MR_VERSIONES_FORMULARIO
   WHERE VER_HASH IS NULL
      OR LENGTH(VER_HASH) <> 64
      OR NOT REGEXP_LIKE(LOWER(VER_HASH), '^[0-9a-f]{64}$');

  v_hash_uncheckable := v_total;

  DBMS_OUTPUT.PUT_LINE('RESUMEN INTEGRIDAD VERSIONES');
  DBMS_OUTPUT.PUT_LINE('  INVALID_STATE=' || TO_CHAR(v_invalid_state));
  DBMS_OUTPUT.PUT_LINE('  INVALID_VIGENTE=' || TO_CHAR(v_invalid_vigente));
  DBMS_OUTPUT.PUT_LINE('  VIGENTE_NO_PUBLISHED=' || TO_CHAR(v_vigente_non_published));
  DBMS_OUTPUT.PUT_LINE('  MULTIPLES_VIGENTES_POR_FAMILIA=' || TO_CHAR(v_multiple_vigente));
  DBMS_OUTPUT.PUT_LINE('  PUBLISHED_HISTORICAL_NON_VIGENTE=' || TO_CHAR(v_pub_hist_nonvigente));
  DBMS_OUTPUT.PUT_LINE('  HASH_INVALID=' || TO_CHAR(v_invalid_hash_format));
  DBMS_OUTPUT.PUT_LINE('  HASH_CHECKED_FULL=0');
  DBMS_OUTPUT.PUT_LINE('  HASH_UNCHECKABLE=' || TO_CHAR(v_hash_uncheckable));
END;
/

PROMPT ============================================================
PROMPT FORMATO DE HASH Y LONGITUDES - SIN CONTENIDO JSON
PROMPT ============================================================
SELECT VER_ID,
       LENGTH(VER_JSON) AS VER_JSON_LENGTH,
       LENGTH(VER_HASH) AS VER_HASH_LENGTH,
       VER_ESTADO,
       VER_VIGENTE
  FROM RL_MR_VERSIONES_FORMULARIO
 ORDER BY VER_ID;

PROMPT ============================================================
PROMPT REGLA DE VIGENCIA - SOLO LA INVARIANTE CONTRACTUAL
PROMPT ============================================================
PROMPT Debe cumplirse: VER_VIGENTE=1 implica VER_ESTADO='PUBLISHED'.
PROMPT PUBLISHED con VER_VIGENTE=0 se conserva como historico informativo.
SELECT VER_FAMILIA_ID,
       COUNT(*) AS VIGENTES
  FROM RL_MR_VERSIONES_FORMULARIO
 WHERE VER_VIGENTE = 1
 GROUP BY VER_FAMILIA_ID
HAVING COUNT(*) > 1
 ORDER BY VER_FAMILIA_ID;

PROMPT ============================================================
PROMPT REFERENCIAS E INVARIANTES RELACIONALES - SOLO LECTURA
PROMPT ============================================================

DECLARE
  v_orphan_family       NUMBER;
  v_orphan_creator      NUMBER;
  v_bad_dates           NUMBER;
  v_duplicate_version   NUMBER;
  v_total_evaluations   NUMBER;
  v_orphan_risk         NUMBER;
  v_orphan_version      NUMBER;
  v_bad_version_row     NUMBER;
  v_orphan_eval_creator NUMBER;
  v_dup_catalog         NUMBER;
  v_dup_element         NUMBER;
BEGIN
  SELECT COUNT(*)
    INTO v_orphan_family
    FROM RL_MR_VERSIONES_FORMULARIO v
    LEFT JOIN RL_MR_FAMILIAS_FORMULARIO f ON f.FAM_ID = v.VER_FAMILIA_ID
   WHERE f.FAM_ID IS NULL;

  SELECT COUNT(*)
    INTO v_orphan_creator
    FROM RL_MR_VERSIONES_FORMULARIO v
    LEFT JOIN RL_USUARIOS u ON u.USR_ID = v.VER_USR_CREACION
   WHERE u.USR_ID IS NULL;

  SELECT COUNT(*)
    INTO v_bad_dates
    FROM RL_MR_VERSIONES_FORMULARIO
   WHERE VER_FECHA_FIN IS NOT NULL
     AND VER_FECHA_INICIO IS NOT NULL
     AND VER_FECHA_FIN < VER_FECHA_INICIO;

  SELECT COUNT(*)
    INTO v_duplicate_version
    FROM (
      SELECT VER_FAMILIA_ID, VER_VERSION
        FROM RL_MR_VERSIONES_FORMULARIO
       GROUP BY VER_FAMILIA_ID, VER_VERSION
      HAVING COUNT(*) > 1
    );

  SELECT COUNT(*)
    INTO v_total_evaluations
    FROM RL_MR_EVALUACIONES_RIESGO;

  SELECT COUNT(*)
    INTO v_orphan_risk
    FROM RL_MR_EVALUACIONES_RIESGO e
    LEFT JOIN RL_MR_RIESGOS r ON r.RIE_ID = e.EVA_RIESGO_ID
   WHERE r.RIE_ID IS NULL;

  SELECT COUNT(*)
    INTO v_orphan_version
    FROM RL_MR_EVALUACIONES_RIESGO e
    LEFT JOIN RL_MR_VERSIONES_FORMULARIO v ON v.VER_ID = e.EVA_VERSION_ID
   WHERE v.VER_ID IS NULL;

  SELECT COUNT(*)
    INTO v_bad_version_row
    FROM RL_MR_EVALUACIONES_RIESGO
   WHERE EVA_VERSION_ROW IS NULL
      OR EVA_VERSION_ROW < 1;

  SELECT COUNT(*)
    INTO v_orphan_eval_creator
    FROM RL_MR_EVALUACIONES_RIESGO e
    LEFT JOIN RL_USUARIOS u ON u.USR_ID = e.EVA_USR_REGISTRO
   WHERE u.USR_ID IS NULL;

  SELECT COUNT(*)
    INTO v_dup_catalog
    FROM (
      SELECT CAT_CODIGO
        FROM RL_MR_CATALOGOS
       GROUP BY CAT_CODIGO
      HAVING COUNT(*) > 1
    );

  SELECT COUNT(*)
    INTO v_dup_element
    FROM (
      SELECT ELE_CATALOGO_ID, ELE_CODIGO
        FROM RL_MR_ELEMENTOS_CATALOGO
       GROUP BY ELE_CATALOGO_ID, ELE_CODIGO
      HAVING COUNT(*) > 1
    );

  DBMS_OUTPUT.PUT_LINE('RELACIONES E INVARIANTES');
  DBMS_OUTPUT.PUT_LINE('  ORPHAN_FAMILY=' || TO_CHAR(v_orphan_family));
  DBMS_OUTPUT.PUT_LINE('  ORPHAN_CREATOR=' || TO_CHAR(v_orphan_creator));
  DBMS_OUTPUT.PUT_LINE('  BAD_DATES=' || TO_CHAR(v_bad_dates));
  DBMS_OUTPUT.PUT_LINE('  DUPLICATE_VERSION=' || TO_CHAR(v_duplicate_version));
  DBMS_OUTPUT.PUT_LINE('  TOTAL_EVALUATIONS=' || TO_CHAR(v_total_evaluations));
  DBMS_OUTPUT.PUT_LINE('  ORPHAN_RISK=' || TO_CHAR(v_orphan_risk));
  DBMS_OUTPUT.PUT_LINE('  ORPHAN_VERSION=' || TO_CHAR(v_orphan_version));
  DBMS_OUTPUT.PUT_LINE('  BAD_VERSION_ROW=' || TO_CHAR(v_bad_version_row));
  DBMS_OUTPUT.PUT_LINE('  ORPHAN_EVAL_CREATOR=' || TO_CHAR(v_orphan_eval_creator));
  DBMS_OUTPUT.PUT_LINE('  DUPLICATE_CATALOG=' || TO_CHAR(v_dup_catalog));
  DBMS_OUTPUT.PUT_LINE('  DUPLICATE_ELEMENT=' || TO_CHAR(v_dup_element));
  DBMS_OUTPUT.PUT_LINE('  JSON_VALIDATION=NOT_AVAILABLE_IN_ORACLE_11G_READ_ONLY_SCRIPT');
  DBMS_OUTPUT.PUT_LINE('  FULL_HASH_VALIDATION=REQUIRES_BACKEND_READ_ONLY_CHECK');
END;
/

PROMPT ============================================================
PROMPT REFERENCIAS POR EVA_VERSION_ID - SIN CONTENIDO JSON
PROMPT ============================================================
SELECT EVA_VERSION_ID,
       COUNT(*) AS EVALUATION_REFERENCES
  FROM RL_MR_EVALUACIONES_RIESGO
 GROUP BY EVA_VERSION_ID
 ORDER BY EVA_VERSION_ID;

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
