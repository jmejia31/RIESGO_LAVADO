-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 10_register_tipo_listas_module.sql
-- Objetivo: Registrar módulo Tipo Listas (Mantenimiento de TIPO_LISTAS_CAUTELA)
-- ============================================================

DECLARE
  v_cnt NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_cnt FROM RL_MODULOS WHERE MOD_RUTA = '/tipo-listas';
  IF v_cnt = 0 THEN
    INSERT INTO RL_MODULOS (MOD_ID, MOD_NOMBRE, MOD_DESCRIPCION, MOD_RUTA, MOD_ICONO, MOD_SECCION, MOD_ACTIVO)
    VALUES (6, 'Tipo Listas', 'Mantenimiento de los tipos de listas de cautela en DNP_IHSS.TIPO_LISTAS_CAUTELA', '/tipo-listas', 'list', 'Listas de Cautela', 1);
  END IF;
END;
/

-- Asignar módulo al administrador (USR_ID = 1 y 2)
DECLARE
  v_mod_id NUMBER;
  v_cnt    NUMBER;
BEGIN
  SELECT MOD_ID INTO v_mod_id FROM RL_MODULOS WHERE MOD_RUTA = '/tipo-listas';

  -- Para USR_ID = 1
  SELECT COUNT(*) INTO v_cnt FROM RL_USUARIO_MODULOS WHERE USM_USR_ID = 1 AND USM_MOD_ID = v_mod_id;
  IF v_cnt = 0 THEN
    INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID) VALUES (1, v_mod_id);
  END IF;

  -- Para USR_ID = 2
  SELECT COUNT(*) INTO v_cnt FROM RL_USUARIO_MODULOS WHERE USM_USR_ID = 2 AND USM_MOD_ID = v_mod_id;
  IF v_cnt = 0 THEN
    INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID) VALUES (2, v_mod_id);
  END IF;
END;
/

COMMIT;
