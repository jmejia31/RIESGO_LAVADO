-- ROLLBACK MANUAL Y EXPLÍCITO. Retira únicamente las tres columnas de Bloque 4.
-- No ejecutar como parte de instalación/actualización automática.
-- No se ejecutó contra Oracle durante la implementación.
SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
  v_count NUMBER;
  v_type USER_TAB_COLUMNS.DATA_TYPE%TYPE;
  v_length USER_TAB_COLUMNS.CHAR_LENGTH%TYPE;
  v_char_used USER_TAB_COLUMNS.CHAR_USED%TYPE;
  v_nullable USER_TAB_COLUMNS.NULLABLE%TYPE;
  PROCEDURE validar_columna(p_columna VARCHAR2) IS
  BEGIN
    SELECT COUNT(*) INTO v_count FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = 'RL_MR_PLANES' AND COLUMN_NAME = p_columna;
    IF v_count = 1 THEN
      SELECT DATA_TYPE, CHAR_LENGTH, CHAR_USED, NULLABLE
        INTO v_type, v_length, v_char_used, v_nullable
        FROM USER_TAB_COLUMNS WHERE TABLE_NAME = 'RL_MR_PLANES' AND COLUMN_NAME = p_columna;
      IF v_type <> 'VARCHAR2' OR v_length <> 1000 OR v_char_used <> 'C' OR v_nullable <> 'Y' THEN
        RAISE_APPLICATION_ERROR(-20649, 'Rollback bloqueado: ' || p_columna || ' no coincide con el contrato añadido.');
      END IF;
    END IF;
  END;
  PROCEDURE retirar_columna(p_columna VARCHAR2) IS
  BEGIN
    SELECT COUNT(*) INTO v_count FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = 'RL_MR_PLANES' AND COLUMN_NAME = p_columna;
    IF v_count = 1 THEN EXECUTE IMMEDIATE 'ALTER TABLE RL_MR_PLANES DROP COLUMN ' || p_columna; END IF;
  END;
BEGIN
  IF UPPER(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(-20649, 'Rollback bloqueado: CURRENT_SCHEMA debe ser RIESGO_LAVADO.');
  END IF;
  -- Validar todo el alcance antes del primer DDL para evitar una retirada parcial por conflicto de contrato.
  validar_columna('PLA_MONITOREO_SEGUIMIENTO');
  validar_columna('PLA_RESPONSABLES');
  validar_columna('PLA_RECURSOS');
  retirar_columna('PLA_MONITOREO_SEGUIMIENTO');
  retirar_columna('PLA_RESPONSABLES');
  retirar_columna('PLA_RECURSOS');
  DBMS_OUTPUT.PUT_LINE('BLOCK4_PLAN_COLUMNS_ROLLBACK=READY');
END;
/
