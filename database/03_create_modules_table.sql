-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 03_create_modules_table.sql
-- Objetivo: Crear tablas de Módulos y Accesos de Usuario (Compatible con Oracle 11g)
-- ============================================================

-- Eliminar tablas y secuencias si ya existen
DECLARE
  PROCEDURE drop_table(p_table_name IN VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE ' || p_table_name || ' CASCADE CONSTRAINTS';
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
        RAISE;
      END IF;
  END;

  PROCEDURE drop_sequence(p_seq_name IN VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE ' || p_seq_name;
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
        RAISE;
      END IF;
  END;
BEGIN
  drop_table('RL_USUARIO_MODULOS');
  drop_table('RL_MODULOS');
  drop_sequence('SEQ_RL_MODULOS');
END;
/

-- 1. TABLA: RL_MODULOS
CREATE TABLE RL_MODULOS (
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
);

COMMENT ON TABLE RL_MODULOS IS 'Catálogo de módulos y accesos del sistema.';
COMMENT ON COLUMN RL_MODULOS.MOD_ID IS 'Clave primaria del módulo.';
COMMENT ON COLUMN RL_MODULOS.MOD_NOMBRE IS 'Nombre descriptivo del módulo.';
COMMENT ON COLUMN RL_MODULOS.MOD_RUTA IS 'Ruta de navegación en el frontend (ej. /usuarios).';
COMMENT ON COLUMN RL_MODULOS.MOD_ICONO IS 'Nombre del icono asociado (ej. users).';
COMMENT ON COLUMN RL_MODULOS.MOD_SECCION IS 'Sección del menú lateral (ej. Administración).';
COMMENT ON COLUMN RL_MODULOS.MOD_ACTIVO IS 'Estado: 1=Activo, 0=Inactivo.';

CREATE SEQUENCE SEQ_RL_MODULOS START WITH 1 INCREMENT BY 1 NOCACHE;

-- 2. TABLA: RL_USUARIO_MODULOS (Tabla de unión)
CREATE TABLE RL_USUARIO_MODULOS (
    USM_USR_ID      NUMBER(10)      NOT NULL,
    USM_MOD_ID      NUMBER(5)       NOT NULL,
    CONSTRAINT PK_RL_USUARIO_MODULOS PRIMARY KEY (USM_USR_ID, USM_MOD_ID),
    CONSTRAINT FK_USM_USR_ID FOREIGN KEY (USM_USR_ID) REFERENCES RL_USUARIOS(USR_ID) ON DELETE CASCADE,
    CONSTRAINT FK_USM_MOD_ID FOREIGN KEY (USM_MOD_ID) REFERENCES RL_MODULOS(MOD_ID) ON DELETE CASCADE
);

COMMENT ON TABLE RL_USUARIO_MODULOS IS 'Tabla intermedia para control de accesos de usuario a módulos.';
COMMENT ON COLUMN RL_USUARIO_MODULOS.USM_USR_ID IS 'ID del usuario en RL_USUARIOS.';
COMMENT ON COLUMN RL_USUARIO_MODULOS.USM_MOD_ID IS 'ID del módulo en RL_MODULOS.';

-- Insertar módulo por defecto: Usuarios del Sistema
INSERT INTO RL_MODULOS (MOD_ID, MOD_NOMBRE, MOD_DESCRIPCION, MOD_RUTA, MOD_ICONO, MOD_SECCION, MOD_ACTIVO)
VALUES (SEQ_RL_MODULOS.NEXTVAL, 'Usuarios del Sistema', 'Administración y gestión de usuarios', '/usuarios', 'users', 'Administración', 1);

-- Asignar el módulo al usuario administrador por defecto (USR_ID = 1)
INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID)
SELECT 1, MOD_ID FROM RL_MODULOS WHERE MOD_RUTA = '/usuarios';

COMMIT;
