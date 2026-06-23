-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 15_update_detalle_evidencia_soft_delete.sql
-- Objetivo: Formalizar eliminacion logica de evidencias.
-- ============================================================

DECLARE
  PROCEDURE add_column_if_missing(p_column_name IN VARCHAR2, p_definition IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = 'RL_DETALLE_EVIDENCIA'
       AND COLUMN_NAME = p_column_name;

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE 'ALTER TABLE RL_DETALLE_EVIDENCIA ADD (' || p_definition || ')';
    END IF;
  END;

  PROCEDURE drop_constraint_if_exists(p_constraint_name IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_CONSTRAINTS
     WHERE TABLE_NAME = 'RL_DETALLE_EVIDENCIA'
       AND CONSTRAINT_NAME = p_constraint_name;

    IF v_count > 0 THEN
      EXECUTE IMMEDIATE 'ALTER TABLE RL_DETALLE_EVIDENCIA DROP CONSTRAINT ' || p_constraint_name;
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
  add_column_if_missing('EVI_ESTADO_REGISTRO', 'EVI_ESTADO_REGISTRO NUMBER(1) DEFAULT 1');
  add_column_if_missing('EVI_USR_INACTIVO_ID', 'EVI_USR_INACTIVO_ID NUMBER(10)');
  add_column_if_missing('EVI_FECHA_INACTIVO', 'EVI_FECHA_INACTIVO DATE');

  EXECUTE IMMEDIATE '
    UPDATE RL_DETALLE_EVIDENCIA
       SET EVI_ESTADO_REGISTRO = 1
     WHERE EVI_ESTADO_REGISTRO IS NULL';

  EXECUTE IMMEDIATE 'ALTER TABLE RL_DETALLE_EVIDENCIA MODIFY (EVI_ESTADO_REGISTRO DEFAULT 1 NOT NULL)';

  drop_constraint_if_exists('FK_EVI_DETALLE_LISTA');

  EXECUTE IMMEDIATE '
    ALTER TABLE RL_DETALLE_EVIDENCIA
    ADD CONSTRAINT FK_EVI_DETALLE_LISTA
    FOREIGN KEY (EVI_DETALLE_ID)
    REFERENCES RL_DETALLE_LISTA(DLL_DETALLE_LISTA_ID)
    ON DELETE CASCADE';

  create_index_if_missing(
    'IDX_EVI_DETALLE_ESTADO',
    'CREATE INDEX IDX_EVI_DETALLE_ESTADO ON RL_DETALLE_EVIDENCIA(EVI_DETALLE_ID, EVI_ESTADO_REGISTRO)'
  );
END;
/

COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_ESTADO_REGISTRO IS 'Estado del registro: 1=Activo, 0=Inactivo por eliminacion logica.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_USR_INACTIVO_ID IS 'ID del usuario que realizo la eliminacion logica.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_FECHA_INACTIVO IS 'Fecha en que se realizo la eliminacion logica.';

COMMIT;
