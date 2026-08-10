-- ============================================================
-- DB-03 - PROFILING ORACLE / EXPLAIN PLAN
-- Punto de entrada manual y autorizado.
-- Uso: @00_db03_ejecutar_profiling_autorizado.sql EJECUTAR_DB03
-- No contiene DDL de indices ni DML sobre tablas de negocio.
-- ============================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET SERVEROUTPUT ON
SET VERIFY OFF
SET FEEDBACK ON
SET PAGESIZE 200
SET LINESIZE 220
SET LONG 100000
SET LONGCHUNKSIZE 100000

DEFINE autorizacion = '&1'

DECLARE
  v_auth   VARCHAR2(50) := q'[&autorizacion]';
  v_schema VARCHAR2(128);
BEGIN
  SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
    INTO v_schema
    FROM DUAL;

  IF UPPER(v_schema) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(-20301,
      'DB-03 BLOQUEADO: CURRENT_SCHEMA debe ser RIESGO_LAVADO.');
  END IF;

  IF UPPER(v_auth) <> 'EJECUTAR_DB03' THEN
    RAISE_APPLICATION_ERROR(-20302,
      'DB-03 BLOQUEADO: invoque el paquete con el token EJECUTAR_DB03.');
  END IF;

  DBMS_OUTPUT.PUT_LINE('DB-03 autorizado para diagnostico.');
  DBMS_OUTPUT.PUT_LINE('No se ejecutara DDL de indices ni DML sobre tablas RL_*.');
END;
/

PROMPT ============================================================
PROMPT DB-03 / 1 DE 2 - INVENTARIO Y ESTADISTICAS DE SOLO LECTURA
PROMPT ============================================================
@@01_db03_inventario_estadisticas_solo_lectura.sql

PROMPT ============================================================
PROMPT DB-03 / 2 DE 2 - EXPLAIN PLAN DE CONSULTAS CRITICAS
PROMPT ============================================================
@@02_db03_explain_plan_consultas_criticas.sql

PROMPT ============================================================
PROMPT DB-03 FINALIZADO. REVISE Y SANEE LA EVIDENCIA ANTES DE COMPARTIRLA.
PROMPT NO SE CREARON INDICES NI SE MODIFICARON TABLAS DE NEGOCIO.
PROMPT ============================================================

UNDEFINE autorizacion
