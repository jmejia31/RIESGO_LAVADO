-- Bloque 4: agrega campos opcionales de plan 44, 45 y 48.
-- Ejecución manual controlada; NO incluir en maestros automáticos.
-- No realiza backfill y conserva NULL en registros históricos.
SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
  v_count NUMBER;
  v_type USER_TAB_COLUMNS.DATA_TYPE%TYPE;
  v_length USER_TAB_COLUMNS.CHAR_LENGTH%TYPE;
  v_char_used USER_TAB_COLUMNS.CHAR_USED%TYPE;
  v_nullable USER_TAB_COLUMNS.NULLABLE%TYPE;
  PROCEDURE agregar_columna(p_columna VARCHAR2) IS
  BEGIN
    SELECT COUNT(*) INTO v_count FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = 'RL_MR_PLANES' AND COLUMN_NAME = p_columna;
    IF v_count = 0 THEN
      EXECUTE IMMEDIATE 'ALTER TABLE RL_MR_PLANES ADD (' || p_columna || ' VARCHAR2(1000 CHAR))';
    ELSE
      SELECT DATA_TYPE, CHAR_LENGTH, CHAR_USED, NULLABLE
        INTO v_type, v_length, v_char_used, v_nullable
        FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'RL_MR_PLANES' AND COLUMN_NAME = p_columna;
      IF v_type <> 'VARCHAR2' OR v_length <> 1000 OR v_char_used <> 'C' OR v_nullable <> 'Y' THEN
        RAISE_APPLICATION_ERROR(-20647, p_columna || ' existe con contrato incompatible; no se modificó.');
      END IF;
    END IF;
  END;
BEGIN
  IF UPPER(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(-20647, 'DDL bloqueado: CURRENT_SCHEMA debe ser RIESGO_LAVADO.');
  END IF;
  SELECT COUNT(*) INTO v_count FROM USER_TABLES WHERE TABLE_NAME = 'RL_MR_PLANES';
  IF v_count <> 1 THEN RAISE_APPLICATION_ERROR(-20647, 'No existe RL_MR_PLANES en el esquema actual.'); END IF;

  agregar_columna('PLA_MONITOREO_SEGUIMIENTO');
  agregar_columna('PLA_RESPONSABLES');
  agregar_columna('PLA_RECURSOS');

  EXECUTE IMMEDIATE q'[COMMENT ON COLUMN RL_MR_PLANES.PLA_MONITOREO_SEGUIMIENTO IS 'Monitoreo y seguimiento institucional del plan.']';
  EXECUTE IMMEDIATE q'[COMMENT ON COLUMN RL_MR_PLANES.PLA_RESPONSABLES IS 'Responsable(s) institucional(es) del plan.']';
  EXECUTE IMMEDIATE q'[COMMENT ON COLUMN RL_MR_PLANES.PLA_RECURSOS IS 'Recursos requeridos o asociados al plan de mitigación.']';
  DBMS_OUTPUT.PUT_LINE('BLOCK4_PLAN_COLUMNS_DDL=READY');
END;
/
