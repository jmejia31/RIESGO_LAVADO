-- ============================================================
-- Sistema de Gestión de Riesgos LA/FT - IHSS
-- Fase 3. Modelo de datos y arquitectura Oracle
-- Script: 02_F3_register_modulo_matrices_riesgos.sql
-- Objetivo: Registrar el módulo Matrices de Riesgos en RL_MODULOS.
-- Clasificación: Aprobado, ejecutado y validado en cierre DBA controlado.
-- Responsable documental: Javier Mejía
-- Reglas: idempotente por ruta, sin DROP, sin TRUNCATE, sin DELETE.
-- ============================================================

SET DEFINE OFF;

DECLARE
  v_count      NUMBER;
  v_mod_id     NUMBER;
  v_id_ocupado NUMBER;
  v_req_cols   NUMBER;
  v_bad_cols   NUMBER;

  PROCEDURE validar_estructura_permisos IS
  BEGIN
    SELECT COUNT(*) INTO v_req_cols
      FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = 'RL_USUARIO_MODULOS'
       AND COLUMN_NAME IN ('USM_USR_ID', 'USM_MOD_ID')
       AND NULLABLE = 'N';

    IF v_req_cols <> 2 THEN
      RAISE_APPLICATION_ERROR(-20111, 'La tabla RL_USUARIO_MODULOS no tiene la estructura obligatoria USM_USR_ID/USM_MOD_ID.');
    END IF;

    SELECT COUNT(*) INTO v_bad_cols
      FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME = 'RL_USUARIO_MODULOS'
       AND NULLABLE = 'N'
       AND COLUMN_NAME NOT IN ('USM_USR_ID', 'USM_MOD_ID');

    IF v_bad_cols > 0 THEN
      RAISE_APPLICATION_ERROR(-20112, 'RL_USUARIO_MODULOS tiene columnas obligatorias adicionales. Ajustar script antes de ejecutar.');
    END IF;
  END;

  PROCEDURE asignar_mod_usuario(p_usr_id IN NUMBER, p_mod_id IN NUMBER) IS
    v_usr_count NUMBER;
    v_asig_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_usr_count
      FROM RL_USUARIOS
     WHERE USR_ID = p_usr_id;

    IF v_usr_count > 0 THEN
      SELECT COUNT(*) INTO v_asig_count
        FROM RL_USUARIO_MODULOS
       WHERE USM_USR_ID = p_usr_id
         AND USM_MOD_ID = p_mod_id;

      IF v_asig_count = 0 THEN
        INSERT INTO RL_USUARIO_MODULOS (USM_USR_ID, USM_MOD_ID)
        VALUES (p_usr_id, p_mod_id);
      END IF;
    END IF;
  END;
BEGIN
  validar_estructura_permisos;

  SELECT COUNT(*) INTO v_count
    FROM RL_MODULOS
   WHERE MOD_RUTA = '/matrices-riesgos';

  IF v_count = 0 THEN
    SELECT COUNT(*) INTO v_id_ocupado
      FROM RL_MODULOS
     WHERE MOD_ID = 10;

    IF v_id_ocupado > 0 THEN
      RAISE_APPLICATION_ERROR(-20110, 'El MOD_ID 10 ya está ocupado. Validar RL_MODULOS antes de registrar Matrices de Riesgos.');
    END IF;

    INSERT INTO RL_MODULOS (
      MOD_ID,
      MOD_NOMBRE,
      MOD_DESCRIPCION,
      MOD_RUTA,
      MOD_ICONO,
      MOD_SECCION,
      MOD_ACTIVO
    )
    VALUES (
      10,
      'Matrices de Riesgos',
      'Módulo para evaluación, cálculo, seguimiento y reportería de matrices de riesgos LA/FT.',
      '/matrices-riesgos',
      'chart-column',
      'Riesgos LA/FT',
      1
    );
  END IF;

  SELECT MOD_ID INTO v_mod_id
    FROM RL_MODULOS
   WHERE MOD_RUTA = '/matrices-riesgos';

  asignar_mod_usuario(1, v_mod_id);
  asignar_mod_usuario(2, v_mod_id);
END;
/

COMMIT;

SELECT MOD_ID, MOD_NOMBRE, MOD_RUTA, MOD_ICONO, MOD_SECCION, MOD_ACTIVO
  FROM RL_MODULOS
 WHERE MOD_RUTA = '/matrices-riesgos';
