-- ============================================================
-- SCRIPT DE ALTERACIÓN: AGREGAR CAMPO DE CAMBIO DE CONTRASEÑA OBLIGATORIO
-- ============================================================

-- 1. Agregar la columna USR_DEBE_CAMBIAR_PASS a la tabla RL_USUARIOS
ALTER TABLE RL_USUARIOS ADD USR_DEBE_CAMBIAR_PASS NUMBER(1) DEFAULT 0 NOT NULL;

-- 2. Agregar la restricción check para asegurar que solo acepte 0 o 1
ALTER TABLE RL_USUARIOS ADD CONSTRAINT CK_RL_USR_DEBE_CAMBIAR CHECK (USR_DEBE_CAMBIAR_PASS IN (0, 1));

COMMENT ON COLUMN RL_USUARIOS.USR_DEBE_CAMBIAR_PASS IS 'Indica si el usuario debe cambiar su contraseña en el próximo inicio de sesión: 1=Sí, 0=No.';

-- 3. Agregar columna USR_FECHA_CLAVE_TEMP a la tabla RL_USUARIOS
ALTER TABLE RL_USUARIOS ADD USR_FECHA_CLAVE_TEMP DATE;
COMMENT ON COLUMN RL_USUARIOS.USR_FECHA_CLAVE_TEMP IS 'Fecha y hora en que se generó la última clave provisional.';

-- 4. Agregar columna SFS_VALIDEZ_CLAVE_TEMP a la tabla RL_CONFIG_SISTEMA
ALTER TABLE RL_CONFIG_SISTEMA ADD SFS_VALIDEZ_CLAVE_TEMP NUMBER DEFAULT 15 NOT NULL;
COMMENT ON COLUMN RL_CONFIG_SISTEMA.SFS_VALIDEZ_CLAVE_TEMP IS 'Tiempo de validez de la clave provisional en minutos.';
