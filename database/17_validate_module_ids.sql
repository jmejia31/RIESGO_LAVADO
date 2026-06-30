-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script: 17_validate_module_ids.sql
-- Objetivo: Validar que RL_MODULOS este alineada con backend y Angular.
-- Tipo: Validacion idempotente, solo lectura, sin cambios de datos.
-- ============================================================

SET SERVEROUTPUT ON

DECLARE
  v_count  NUMBER;
  v_errors NUMBER := 0;

  PROCEDURE assert_module(
    p_expected_id IN NUMBER,
    p_route       IN VARCHAR2,
    p_label       IN VARCHAR2
  ) IS
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM RL_MODULOS
     WHERE MOD_ID = p_expected_id
       AND MOD_RUTA = p_route
       AND MOD_ACTIVO = 1;

    IF v_count = 0 THEN
      v_errors := v_errors + 1;
      DBMS_OUTPUT.PUT_LINE('ERROR: modulo esperado no encontrado o desalineado. ID=' || p_expected_id || ' Ruta=' || p_route || ' Nombre=' || p_label);
    ELSE
      DBMS_OUTPUT.PUT_LINE('OK: ' || p_expected_id || ' - ' || p_route || ' - ' || p_label);
    END IF;
  END;
BEGIN
  assert_module(2, '/usuarios', 'Usuarios del Sistema');
  assert_module(3, '/configuracion', 'Configuracion del Sistema');
  assert_module(4, '/monitoreo-listas', 'Monitoreo de Listas');
  assert_module(5, '/bitacora', 'Bitacora de Sistema');
  assert_module(6, '/tipo-listas', 'Tipo Listas');
  assert_module(7, '/cargar-listas', 'Cargar Listas');
  assert_module(8, '/coincidencias-patrono', 'Coincidencias Patrono');
  assert_module(9, '/coincidencias-empleado', 'Coincidencias Empleado');

  IF v_errors > 0 THEN
    RAISE_APPLICATION_ERROR(-20080, 'Validacion de modulos fallida. Revise RL_MODULOS y RL_USUARIO_MODULOS antes de continuar.');
  END IF;

  DBMS_OUTPUT.PUT_LINE('Validacion de modulos finalizada correctamente.');
END;
/

SELECT MOD_ID, MOD_NOMBRE, MOD_RUTA, MOD_SECCION, MOD_ACTIVO
  FROM RL_MODULOS
 WHERE MOD_ID BETWEEN 2 AND 9
 ORDER BY MOD_ID;
