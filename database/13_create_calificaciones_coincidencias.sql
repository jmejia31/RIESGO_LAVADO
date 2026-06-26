-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 13_create_calificaciones_coincidencias.sql
-- Objetivo: Crear tabla propia para calificaciones de coincidencias.
-- Tipo: Idempotente, sin eliminacion de datos.
-- ============================================================

DECLARE
  PROCEDURE create_sequence_if_missing(p_sequence_name IN VARCHAR2, p_sql IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_SEQUENCES
     WHERE SEQUENCE_NAME = UPPER(p_sequence_name);

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE p_sql;
    END IF;
  END;

  PROCEDURE create_table_if_missing(p_table_name IN VARCHAR2, p_sql IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_TABLES
     WHERE TABLE_NAME = UPPER(p_table_name);

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
     WHERE INDEX_NAME = UPPER(p_index_name);

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE p_sql;
    END IF;
  END;
BEGIN
  create_table_if_missing(
    'RL_CALIF_COINCIDENCIAS',
    'CREATE TABLE RL_CALIF_COINCIDENCIAS (
      CAL_ID                       NUMBER(15) NOT NULL,
      CAL_REPORTE_COINCIDENCIA_ID  NUMBER(15) NOT NULL,
      CAL_TIPO_CALIFICACION_ID     NUMBER(3)  NOT NULL,
      CAL_USUARIO_ID               NUMBER(10) NOT NULL,
      CAL_FECHA                    DATE DEFAULT SYSDATE NOT NULL,
      CAL_OBSERVACION              VARCHAR2(1000),
      CONSTRAINT PK_RL_CALIFICACIONES PRIMARY KEY (CAL_ID),
      CONSTRAINT UQ_RL_CAL_REPORTE UNIQUE (CAL_REPORTE_COINCIDENCIA_ID)
    )'
  );

  create_sequence_if_missing(
    'SEQ_RL_CALIFICACIONES',
    'CREATE SEQUENCE SEQ_RL_CALIFICACIONES START WITH 1 INCREMENT BY 1 NOCACHE'
  );

  create_index_if_missing(
    'IDX_RL_CAL_REPORTE',
    'CREATE INDEX IDX_RL_CAL_REPORTE ON RL_CALIF_COINCIDENCIAS(CAL_REPORTE_COINCIDENCIA_ID)'
  );

  create_index_if_missing(
    'IDX_RL_CAL_USUARIO',
    'CREATE INDEX IDX_RL_CAL_USUARIO ON RL_CALIF_COINCIDENCIAS(CAL_USUARIO_ID)'
  );
END;
/

COMMENT ON TABLE  RL_CALIF_COINCIDENCIAS IS 'Calificaciones de coincidencias de patronos o empleados.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_ID IS 'Clave primaria.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_REPORTE_COINCIDENCIA_ID IS 'ID del registro en DNP_IHSS.REPORTE_COINCIDENCIAS.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_TIPO_CALIFICACION_ID IS '1=Positivo, 2=Falso Positivo.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_USUARIO_ID IS 'Usuario que realizo la calificacion.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_FECHA IS 'Fecha y hora de la calificacion.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_OBSERVACION IS 'Observaciones opcionales.';

COMMIT;
