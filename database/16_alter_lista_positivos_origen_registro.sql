-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 16_alter_lista_positivos_origen_registro.sql
-- Objetivo: Registrar el origen operativo de cada positivo.
-- ============================================================

DECLARE
  PROCEDURE add_column_if_missing(p_column_name IN VARCHAR2, p_definition IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = 'RL_LISTA_POSITIVOS'
       AND COLUMN_NAME = p_column_name;

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE 'ALTER TABLE RL_LISTA_POSITIVOS ADD (' || p_definition || ')';
    END IF;
  END;

  PROCEDURE add_constraint_if_missing(p_constraint_name IN VARCHAR2, p_sql IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_CONSTRAINTS
     WHERE TABLE_NAME = 'RL_LISTA_POSITIVOS'
       AND CONSTRAINT_NAME = p_constraint_name;

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE p_sql;
    END IF;
  END;

  PROCEDURE create_index_if_missing(p_index_name IN VARCHAR2, p_sql IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_INDEXES
     WHERE INDEX_NAME = p_index_name;

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE p_sql;
    END IF;
  END;
BEGIN
  add_column_if_missing('LSP_ORIGEN_REGISTRO', 'LSP_ORIGEN_REGISTRO VARCHAR2(50 CHAR)');

  add_constraint_if_missing(
    'CK_RL_LSP_ORIGEN_REG',
    'ALTER TABLE RL_LISTA_POSITIVOS ADD CONSTRAINT CK_RL_LSP_ORIGEN_REG CHECK (LSP_ORIGEN_REGISTRO IS NULL OR LSP_ORIGEN_REGISTRO IN (''DNP_LISTAS'', ''MANUAL_CUMPLIMIENTO'', ''NOTICIA_PRENSA'', ''OTRO''))'
  );

  create_index_if_missing(
    'IDX_RL_LSP_ORIGEN',
    'CREATE INDEX IDX_RL_LSP_ORIGEN ON RL_LISTA_POSITIVOS(LSP_ORIGEN_REGISTRO)'
  );
END;
/

COMMENT ON COLUMN RL_LISTA_POSITIVOS.LSP_ORIGEN_REGISTRO IS 'Origen operativo del registro: DNP_LISTAS, MANUAL_CUMPLIMIENTO, NOTICIA_PRENSA u OTRO.';

COMMIT;
