-- ============================================================
-- MODULO MATRICES DE RIESGOS - LIMPIEZA DE RESPALDOS FASE 10
-- Script: 09_limpieza_tablas_respaldo_b10.sql
-- Propósito: Eliminación segura e idempotente de las tablas temporales
--            de respaldo (B10_001 a B10_041, BKP_F10_MAP, BKP_F10_SECUENCIAS).
-- Uso: Manual en SQL*Plus / SQL Developer / PL/SQL Developer:
--      @09_limpieza_tablas_respaldo_b10.sql EJECUTAR
-- ============================================================

SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DEFINE autorizacion = '&1'

DECLARE
  v_auth   VARCHAR2(50) := q'[&autorizacion]';
  v_schema VARCHAR2(128);
  v_count  NUMBER := 0;
BEGIN
  SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') INTO v_schema FROM DUAL;
  IF UPPER(v_schema) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(-20201, 'EJECUCION BLOQUEADA: esquema distinto de RIESGO_LAVADO.');
  END IF;

  IF UPPER(v_auth) <> 'EJECUTAR' THEN
    RAISE_APPLICATION_ERROR(-20202, 'EJECUCION BLOQUEADA: invoque el script con el parametro EJECUTAR.');
  END IF;

  -- Confirmar que el modelo objetivo de 17 tablas operativas RL_MR_* este presente
  SELECT COUNT(*) INTO v_count
    FROM USER_TABLES
   WHERE TABLE_NAME LIKE 'RL_MR_%';

  IF v_count < 17 THEN
    RAISE_APPLICATION_ERROR(-20203, 'EJECUCION BLOQUEADA: no se encontraron las 17 tablas operativas RL_MR_*.');
  END IF;

  DBMS_OUTPUT.PUT_LINE('Prevalidacion de seguridad aprobada. Iniciando eliminacion de respaldos B10_...');
END;
/

DECLARE
  v_eliminadas NUMBER := 0;
  PROCEDURE drop_table_if_exists(p_name VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE ' || p_name || ' PURGE';
    DBMS_OUTPUT.PUT_LINE('Tabla de respaldo eliminada: ' || p_name);
  EXCEPTION WHEN OTHERS THEN
    IF SQLCODE <> -942 THEN
      DBMS_OUTPUT.PUT_LINE('Error al eliminar ' || p_name || ': ' || SQLERRM);
    END IF;
  END;
BEGIN
  -- Eliminar respaldos B10_001 a B10_041, BKP_F10_MAP y BKP_F10_SECUENCIAS
  FOR t IN (
    SELECT TABLE_NAME
      FROM USER_TABLES
     WHERE TABLE_NAME LIKE 'B10_%'
        OR TABLE_NAME IN ('BKP_F10_MAP', 'BKP_F10_SECUENCIAS')
     ORDER BY TABLE_NAME
  ) LOOP
    drop_table_if_exists(t.TABLE_NAME);
    v_eliminadas := v_eliminadas + 1;
  END LOOP;

  DBMS_OUTPUT.PUT_LINE('----------------------------------------------------');
  DBMS_OUTPUT.PUT_LINE('Proceso de limpieza finalizado. Total eliminadas: ' || v_eliminadas);
  DBMS_OUTPUT.PUT_LINE('----------------------------------------------------');
END;
/
