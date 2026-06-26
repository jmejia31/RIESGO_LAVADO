-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 09_create_detalle_evidencia.sql
-- Objetivo: Crear metadatos de evidencia fisica para seguimientos.
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
    'RL_DETALLE_EVIDENCIA',
    'CREATE TABLE RL_DETALLE_EVIDENCIA (
      EVI_ID              NUMBER(20)      NOT NULL,
      EVI_DETALLE_ID      NUMBER(20)      NOT NULL,
      EVI_NOMBRE_ARCHIVO  VARCHAR2(255)   NOT NULL,
      EVI_TIPO_MIME       VARCHAR2(100)   NOT NULL,
      EVI_RUTA_ARCHIVO    VARCHAR2(500)   NOT NULL,
      EVI_FECHA_CREACION  DATE            DEFAULT SYSDATE NOT NULL,
      EVI_USR_CREACION_ID NUMBER(10)      NOT NULL,
      EVI_ESTADO_REGISTRO NUMBER(1)       DEFAULT 1 NOT NULL,
      EVI_USR_INACTIVO_ID NUMBER(10),
      EVI_FECHA_INACTIVO  DATE,
      CONSTRAINT PK_RL_DETALLE_EVIDENCIA PRIMARY KEY (EVI_ID),
      CONSTRAINT FK_EVI_DETALLE_LISTA FOREIGN KEY (EVI_DETALLE_ID) REFERENCES RL_DETALLE_LISTA(DLL_DETALLE_LISTA_ID) ON DELETE CASCADE
    )'
  );

  create_sequence_if_missing(
    'SEQ_RL_DETALLE_EVIDENCIA',
    'CREATE SEQUENCE SEQ_RL_DETALLE_EVIDENCIA START WITH 1 INCREMENT BY 1 NOCACHE'
  );

  create_index_if_missing(
    'IDX_EVI_DETALLE',
    'CREATE INDEX IDX_EVI_DETALLE ON RL_DETALLE_EVIDENCIA(EVI_DETALLE_ID)'
  );

  create_index_if_missing(
    'IDX_EVI_DETALLE_ESTADO',
    'CREATE INDEX IDX_EVI_DETALLE_ESTADO ON RL_DETALLE_EVIDENCIA(EVI_DETALLE_ID, EVI_ESTADO_REGISTRO)'
  );
END;
/

COMMENT ON TABLE RL_DETALLE_EVIDENCIA IS 'Almacen de metadatos y rutas fisicas de archivos cargados como evidencia de seguimiento.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_ID IS 'Clave primaria.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_DETALLE_ID IS 'Clave foranea a la nota de seguimiento en RL_DETALLE_LISTA.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_NOMBRE_ARCHIVO IS 'Nombre original del archivo.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_TIPO_MIME IS 'Tipo de contenido MIME del archivo.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_RUTA_ARCHIVO IS 'Nombre o ruta fisica del archivo almacenado.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_FECHA_CREACION IS 'Fecha de carga.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_USR_CREACION_ID IS 'ID del usuario que subio la evidencia.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_ESTADO_REGISTRO IS 'Estado del registro: 1=Activo, 0=Inactivo por eliminacion logica.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_USR_INACTIVO_ID IS 'ID del usuario que realizo la eliminacion logica.';
COMMENT ON COLUMN RL_DETALLE_EVIDENCIA.EVI_FECHA_INACTIVO IS 'Fecha en que se realizo la eliminacion logica.';

COMMIT;
