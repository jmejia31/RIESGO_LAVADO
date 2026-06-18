-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 13_create_calificaciones_coincidencias.sql
-- Objetivo: Crear tabla propia para calificaciones de coincidencias.
--           El usuario RIESGO_LAVADO no tiene UPDATE sobre
--           DNP_IHSS.REPORTE_COINCIDENCIAS, por lo que se almacena
--           la calificación en una tabla del schema RIESGO_LAVADO.
-- ============================================================

-- Eliminar si ya existe (safe drop)
DECLARE
  v_cnt NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_cnt
    FROM USER_TABLES
    WHERE TABLE_NAME = 'RL_CALIF_COINCIDENCIAS';
  IF v_cnt > 0 THEN
    EXECUTE IMMEDIATE 'DROP TABLE RL_CALIF_COINCIDENCIAS CASCADE CONSTRAINTS';
  END IF;

  SELECT COUNT(*) INTO v_cnt
    FROM USER_SEQUENCES
   WHERE SEQUENCE_NAME = 'SEQ_RL_CALIFICACIONES';
  IF v_cnt > 0 THEN
    EXECUTE IMMEDIATE 'DROP SEQUENCE SEQ_RL_CALIFICACIONES';
  END IF;
END;
/

-- Tabla de calificaciones
CREATE TABLE RL_CALIF_COINCIDENCIAS (
    CAL_ID                  NUMBER(15)      NOT NULL,
    CAL_REPORTE_COINCIDENCIA_ID NUMBER(15)  NOT NULL,   -- FK lógica a DNP_IHSS.REPORTE_COINCIDENCIAS
    CAL_TIPO_CALIFICACION_ID    NUMBER(3)   NOT NULL,   -- 1=Positivo, 2=Falso Positivo, etc.
    CAL_USUARIO_ID          NUMBER(10)      NOT NULL,   -- FK a RL_USUARIOS
    CAL_FECHA               DATE            DEFAULT SYSDATE NOT NULL,
    CAL_OBSERVACION         VARCHAR2(1000),
    CONSTRAINT PK_RL_CALIFICACIONES PRIMARY KEY (CAL_ID),
    CONSTRAINT UQ_RL_CAL_REPORTE UNIQUE (CAL_REPORTE_COINCIDENCIA_ID)  -- una calificación por reporte
);

COMMENT ON TABLE  RL_CALIF_COINCIDENCIAS                        IS 'Calificaciones de coincidencias de patronos (Positivo / Falso Positivo).';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_ID                 IS 'Clave primaria.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_REPORTE_COINCIDENCIA_ID IS 'ID del registro en DNP_IHSS.REPORTE_COINCIDENCIAS.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_TIPO_CALIFICACION_ID    IS '1 = Positivo, 2 = Falso Positivo.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_USUARIO_ID         IS 'Usuario que realizó la calificación.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_FECHA              IS 'Fecha y hora de la calificación.';
COMMENT ON COLUMN RL_CALIF_COINCIDENCIAS.CAL_OBSERVACION        IS 'Observaciones opcionales.';

CREATE SEQUENCE SEQ_RL_CALIFICACIONES START WITH 1 INCREMENT BY 1 NOCACHE;
CREATE INDEX IDX_RL_CAL_REPORTE ON RL_CALIF_COINCIDENCIAS(CAL_REPORTE_COINCIDENCIA_ID);
CREATE INDEX IDX_RL_CAL_USUARIO ON RL_CALIF_COINCIDENCIAS(CAL_USUARIO_ID);

COMMIT;
