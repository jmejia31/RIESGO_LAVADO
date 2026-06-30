-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 04_alter_config_sistema.sql
-- Objetivo: Extender configuración + registrar módulo Configuración
-- ============================================================

-- 1. Agregar columnas faltantes en RL_CONFIG_SISTEMA
DECLARE
  PROCEDURE add_col(p_table IN VARCHAR2, p_col IN VARCHAR2, p_def IN VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE 'ALTER TABLE ' || p_table || ' ADD ' || p_col || ' ' || p_def;
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE != -1430 THEN RAISE; END IF; -- ORA-01430: column already exists
  END;
BEGIN
  add_col('RL_CONFIG_SISTEMA', 'SFS_ACUERDO_LEGAL',  'CLOB');
  add_col('RL_CONFIG_SISTEMA', 'SFS_MAX_INTENTOS',   'NUMBER DEFAULT 5');
  add_col('RL_LOGIN_SLIDES', 'SGL_TITULO',       'VARCHAR2(100)');
  add_col('RL_LOGIN_SLIDES', 'SGL_DESCRIPCION',   'VARCHAR2(500)');
  add_col('RL_LOGIN_SLIDES', 'SGL_IMAGEN_ICONO',   'VARCHAR2(100)');
  add_col('RL_USUARIOS', 'USR_INTENTOS_FALLIDOS', 'NUMBER DEFAULT 0 NOT NULL');
  add_col('RL_USUARIOS', 'USR_FECHA_BLOQUEO',     'DATE');
END;
/

-- 2. Registrar módulo "Configuración del Sistema" (MOD_ID = 3)
-- Si ya existe con otra ID, el UNIQUE en MOD_RUTA lo protege
DECLARE
  v_cnt NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_cnt FROM RL_MODULOS WHERE MOD_RUTA = '/configuracion';
  IF v_cnt = 0 THEN
    INSERT INTO RL_MODULOS (MOD_ID, MOD_NOMBRE, MOD_DESCRIPCION, MOD_RUTA, MOD_ICONO, MOD_SECCION, MOD_ACTIVO)
    VALUES (3, 'Configuración del Sistema', 'Configuración general, apariencia y slides del login', '/configuracion', 'cog', 'Administración', 1);
  END IF;
END;
/

-- 3. Asignar módulo Configuración al administrador (USR_ID = 1 y 2)
DECLARE
  v_mod_id NUMBER;
  v_cnt    NUMBER;
BEGIN
  SELECT MOD_ID INTO v_mod_id FROM RL_MODULOS WHERE MOD_RUTA = '/configuracion';

  -- Para USR_ID = 1
  SELECT COUNT(*) INTO v_cnt FROM RL_USUARIO_MODULOS WHERE USM_USR_ID = 1 AND USM_MOD_ID = v_mod_id;
  IF v_cnt = 0 THEN
    INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID) VALUES (1, v_mod_id);
  END IF;

  -- Para USR_ID = 2 (edgar.barahona)
  SELECT COUNT(*) INTO v_cnt FROM RL_USUARIO_MODULOS WHERE USM_USR_ID = 2 AND USM_MOD_ID = v_mod_id;
  IF v_cnt = 0 THEN
    INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID) VALUES (2, v_mod_id);
  END IF;
END;
/

COMMIT;

-- Verificar resultado
SELECT MOD_ID, MOD_NOMBRE, MOD_RUTA, MOD_ICONO, MOD_SECCION FROM RL_MODULOS ORDER BY MOD_ID;
SELECT USM_USR_ID, USM_MOD_ID FROM RL_USUARIO_MODULOS ORDER BY USM_USR_ID, USM_MOD_ID;
