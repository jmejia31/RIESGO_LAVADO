-- FASE 3.1.2 - RECOVERY DOCUMENTADO. NO EJECUTAR EN CAMINO FELIZ.
-- Requiere autorización explícita y una verificación independiente de dependencias.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
WHENEVER OSERROR EXIT FAILURE
SET ECHO OFF
SET VERIFY OFF
SET TERMOUT ON
SET SERVEROUTPUT ON

DECLARE
  v_exists NUMBER;
  v_external NUMBER;
  PROCEDURE drop_table_if_safe(p_name VARCHAR2) IS
  BEGIN
    SELECT COUNT(*) INTO v_exists FROM USER_TABLES WHERE TABLE_NAME = p_name;
    IF v_exists = 1 THEN
      SELECT COUNT(*) INTO v_external
        FROM USER_CONSTRAINTS child
        JOIN USER_CONSTRAINTS parent ON parent.CONSTRAINT_NAME = child.R_CONSTRAINT_NAME
       WHERE parent.TABLE_NAME = p_name
         AND child.TABLE_NAME <> p_name;
      IF v_external <> 0 THEN
        RAISE_APPLICATION_ERROR(-20931, 'Dependencia externa detectada para ' || p_name || '; recovery cancelado.');
      END IF;
      EXECUTE IMMEDIATE 'DROP TABLE ' || p_name;
      DBMS_OUTPUT.PUT_LINE('DROPPED_TABLE=' || p_name);
    END IF;
  END;
  PROCEDURE drop_sequence_if_safe(p_name VARCHAR2) IS
  BEGIN
    SELECT COUNT(*) INTO v_exists FROM USER_SEQUENCES WHERE SEQUENCE_NAME = p_name;
    IF v_exists = 1 THEN
      EXECUTE IMMEDIATE 'DROP SEQUENCE ' || p_name;
      DBMS_OUTPUT.PUT_LINE('DROPPED_SEQUENCE=' || p_name);
    END IF;
  END;
BEGIN
  drop_table_if_safe('RL_MR_FUNCION_ARGUMENTOS');
  drop_table_if_safe('RL_MR_FORMULA_USOS');
  drop_table_if_safe('RL_MR_FUNCION_VERSIONES');
  drop_table_if_safe('RL_MR_FORMULA_VERSIONES');
  drop_table_if_safe('RL_MR_PARAMETRO_VERSIONES');
  drop_table_if_safe('RL_MR_FUNCIONES');
  drop_table_if_safe('RL_MR_FORMULAS');
  drop_table_if_safe('RL_MR_PARAMETROS_CALCULO');
  drop_sequence_if_safe('SEQ_RL_MR_FUNCION_ARGUMENTOS');
  drop_sequence_if_safe('SEQ_RL_MR_FORMULA_USOS');
  drop_sequence_if_safe('SEQ_RL_MR_FUNCION_VERSIONES');
  drop_sequence_if_safe('SEQ_RL_MR_FORMULA_VERSIONES');
  drop_sequence_if_safe('SEQ_RL_MR_PARAMETRO_VERSIONES');
  drop_sequence_if_safe('SEQ_RL_MR_FUNCIONES');
  drop_sequence_if_safe('SEQ_RL_MR_FORMULAS');
  drop_sequence_if_safe('SEQ_RL_MR_PARAMETROS');
END;
/
PROMPT RECOVERY_NOT_EXECUTED_BY_DEFAULT
EXIT SUCCESS
