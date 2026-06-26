-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 06_alter_usuarios_change_pass.sql
-- Objetivo: Agregar soporte para cambio obligatorio de clave.
-- Tipo: Actualizacion idempotente, sin eliminacion de datos.
-- ============================================================

DECLARE
  PROCEDURE add_column_if_missing(
    p_table_name   IN VARCHAR2,
    p_column_name  IN VARCHAR2,
    p_definition   IN VARCHAR2
  ) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = UPPER(p_table_name)
       AND COLUMN_NAME = UPPER(p_column_name);

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE 'ALTER TABLE ' || p_table_name || ' ADD (' || p_definition || ')';
    END IF;
  END;

  PROCEDURE add_constraint_if_missing(
    p_table_name       IN VARCHAR2,
    p_constraint_name  IN VARCHAR2,
    p_sql              IN VARCHAR2
  ) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_CONSTRAINTS
     WHERE TABLE_NAME = UPPER(p_table_name)
       AND CONSTRAINT_NAME = UPPER(p_constraint_name);

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE p_sql;
    END IF;
  END;
BEGIN
  add_column_if_missing(
    'RL_USUARIOS',
    'USR_DEBE_CAMBIAR_PASS',
    'USR_DEBE_CAMBIAR_PASS NUMBER(1) DEFAULT 0 NOT NULL'
  );

  add_constraint_if_missing(
    'RL_USUARIOS',
    'CK_RL_USR_DEBE_CAMBIAR',
    'ALTER TABLE RL_USUARIOS ADD CONSTRAINT CK_RL_USR_DEBE_CAMBIAR CHECK (USR_DEBE_CAMBIAR_PASS IN (0, 1))'
  );

  add_column_if_missing(
    'RL_USUARIOS',
    'USR_FECHA_CLAVE_TEMP',
    'USR_FECHA_CLAVE_TEMP DATE'
  );

  add_column_if_missing(
    'RL_CONFIG_SISTEMA',
    'SFS_VALIDEZ_CLAVE_TEMP',
    'SFS_VALIDEZ_CLAVE_TEMP NUMBER DEFAULT 15 NOT NULL'
  );
END;
/

COMMENT ON COLUMN RL_USUARIOS.USR_DEBE_CAMBIAR_PASS IS 'Indica si el usuario debe cambiar su contrasena en el proximo inicio de sesion: 1=Si, 0=No.';
COMMENT ON COLUMN RL_USUARIOS.USR_FECHA_CLAVE_TEMP IS 'Fecha y hora en que se genero la ultima clave provisional.';
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_VALIDEZ_CLAVE_TEMP IS 'Tiempo de validez de la clave provisional en minutos.';

COMMIT;
