-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 03_create_modules_table.sql
-- Objetivo: Crear catalogo de modulos y accesos de usuario.
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

  PROCEDURE create_constraint_if_missing(p_constraint_name IN VARCHAR2, p_sql IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM USER_CONSTRAINTS
     WHERE CONSTRAINT_NAME = UPPER(p_constraint_name);

    IF v_count = 0 THEN
      EXECUTE IMMEDIATE p_sql;
    END IF;
  END;
BEGIN
  create_sequence_if_missing(
    'SEQ_RL_MODULOS',
    'CREATE SEQUENCE SEQ_RL_MODULOS START WITH 1 INCREMENT BY 1 NOCACHE'
  );

  create_table_if_missing(
    'RL_MODULOS',
    'CREATE TABLE RL_MODULOS (
      MOD_ID          NUMBER(5)       NOT NULL,
      MOD_NOMBRE      VARCHAR2(100)   NOT NULL,
      MOD_DESCRIPCION VARCHAR2(255),
      MOD_RUTA        VARCHAR2(200)   NOT NULL,
      MOD_ICONO       VARCHAR2(100)   NOT NULL,
      MOD_SECCION     VARCHAR2(100)   NOT NULL,
      MOD_ACTIVO      NUMBER(1)       DEFAULT 1 NOT NULL,
      CONSTRAINT PK_RL_MODULOS PRIMARY KEY (MOD_ID),
      CONSTRAINT UQ_RL_MOD_RUTA UNIQUE (MOD_RUTA),
      CONSTRAINT CK_RL_MOD_ACTIVO CHECK (MOD_ACTIVO IN (0,1))
    )'
  );

  create_table_if_missing(
    'RL_USUARIO_MODULOS',
    'CREATE TABLE RL_USUARIO_MODULOS (
      USM_USR_ID      NUMBER(10)      NOT NULL,
      USM_MOD_ID      NUMBER(5)       NOT NULL,
      CONSTRAINT PK_RL_USUARIO_MODULOS PRIMARY KEY (USM_USR_ID, USM_MOD_ID)
    )'
  );

  create_constraint_if_missing(
    'FK_USM_USR_ID',
    'ALTER TABLE RL_USUARIO_MODULOS ADD CONSTRAINT FK_USM_USR_ID FOREIGN KEY (USM_USR_ID) REFERENCES RL_USUARIOS(USR_ID) ON DELETE CASCADE'
  );

  create_constraint_if_missing(
    'FK_USM_MOD_ID',
    'ALTER TABLE RL_USUARIO_MODULOS ADD CONSTRAINT FK_USM_MOD_ID FOREIGN KEY (USM_MOD_ID) REFERENCES RL_MODULOS(MOD_ID) ON DELETE CASCADE'
  );
END;
/

COMMENT ON TABLE RL_MODULOS IS 'Catalogo de modulos y accesos del sistema.';
COMMENT ON COLUMN RL_MODULOS.MOD_ID IS 'Clave primaria del modulo.';
COMMENT ON COLUMN RL_MODULOS.MOD_NOMBRE IS 'Nombre descriptivo del modulo.';
COMMENT ON COLUMN RL_MODULOS.MOD_RUTA IS 'Ruta de navegacion en el frontend.';
COMMENT ON COLUMN RL_MODULOS.MOD_ICONO IS 'Nombre del icono asociado.';
COMMENT ON COLUMN RL_MODULOS.MOD_SECCION IS 'Seccion del menu lateral.';
COMMENT ON COLUMN RL_MODULOS.MOD_ACTIVO IS 'Estado: 1=Activo, 0=Inactivo.';
COMMENT ON TABLE RL_USUARIO_MODULOS IS 'Tabla intermedia para control de accesos de usuario a modulos.';
COMMENT ON COLUMN RL_USUARIO_MODULOS.USM_USR_ID IS 'ID del usuario en RL_USUARIOS.';
COMMENT ON COLUMN RL_USUARIO_MODULOS.USM_MOD_ID IS 'ID del modulo en RL_MODULOS.';

DECLARE
  v_mod_id NUMBER;
  v_count  NUMBER;
BEGIN
  SELECT COUNT(*)
    INTO v_count
    FROM RL_MODULOS
   WHERE MOD_RUTA = '/usuarios';

  IF v_count = 0 THEN
    INSERT INTO RL_MODULOS (
      MOD_ID,
      MOD_NOMBRE,
      MOD_DESCRIPCION,
      MOD_RUTA,
      MOD_ICONO,
      MOD_SECCION,
      MOD_ACTIVO
    ) VALUES (
      SEQ_RL_MODULOS.NEXTVAL,
      'Usuarios del Sistema',
      'Administracion y gestion de usuarios',
      '/usuarios',
      'users',
      'Administracion',
      1
    );
  END IF;

  SELECT MOD_ID
    INTO v_mod_id
    FROM RL_MODULOS
   WHERE MOD_RUTA = '/usuarios';

  SELECT COUNT(*)
    INTO v_count
    FROM RL_USUARIOS
   WHERE USR_ID = 1;

  IF v_count > 0 THEN
    SELECT COUNT(*)
      INTO v_count
      FROM RL_USUARIO_MODULOS
     WHERE USM_USR_ID = 1
       AND USM_MOD_ID = v_mod_id;

    IF v_count = 0 THEN
      INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID)
      VALUES (1, v_mod_id);
    END IF;
  END IF;
END;
/

COMMIT;
