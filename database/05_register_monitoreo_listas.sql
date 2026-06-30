-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 05_register_monitoreo_listas.sql
-- Objetivo: Registrar módulo Monitoreo de Listas (MOD_ID = 4)
-- ============================================================

DECLARE
  v_cnt NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_cnt FROM RL_MODULOS WHERE MOD_RUTA = '/monitoreo-listas';
  IF v_cnt = 0 THEN
    INSERT INTO RL_MODULOS (MOD_ID, MOD_NOMBRE, MOD_DESCRIPCION, MOD_RUTA, MOD_ICONO, MOD_SECCION, MOD_ACTIVO)
    VALUES (4, 'Monitoreo de Listas', 'Reportes de coincidencias de listas para Jurídicas, Naturales y Empleados', '/monitoreo-listas', 'shield', 'Administración', 1);
  END IF;
END;
/

-- Asignar módulo al administrador (USR_ID = 1 y 2)
DECLARE
  v_mod_id NUMBER;
  v_cnt    NUMBER;
BEGIN
  SELECT MOD_ID INTO v_mod_id FROM RL_MODULOS WHERE MOD_RUTA = '/monitoreo-listas';

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
