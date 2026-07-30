-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGOS LA/FT - IHSS
-- Módulo: Matrices de Riesgos
-- Script: 00_retiro_controlado_modelo_prueba.sql
-- Objetivo: Retiro ordenado de tablas y secuencias previas del modelo de prueba.
-- Clasificación: SCRIPT DESTELECTIVO MANUAL (EXCLUSIVO PARA DBA).
-- ¡PRECAUCIÓN!: Este script elimina datos y estructuras. No debe correr de forma automática.
-- ============================================================

-- DIRECTIVA OBLIGATORIA PARA SQL*PLUS: ABORTAR TRANSACCIÓN Y EJECUCIÓN ANTE ERROR
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

-- BLOQUE DE SEGURIDAD EXPLICITO - IMPEDIR EJECUCIÓN ACCIDENTAL
DECLARE
  v_permiso_ejecucion VARCHAR2(10) := 'NO'; -- CAMBIAR A 'SI' UNICAMENTE POR EL DBA AUTORIZADO
  v_esquema_actual     VARCHAR2(100);
BEGIN
  SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') INTO v_esquema_actual FROM DUAL;
  
  IF UPPER(v_esquema_actual) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(-20098, 'EJECUCIÓN BLOQUEADA: Este script solo puede ejecutarse en el esquema RIESGO_LAVADO. Esquema detectado: ' || v_esquema_actual);
  END IF;
  
  IF v_permiso_ejecucion <> 'SI' THEN
    RAISE_APPLICATION_ERROR(-20099, 'EJECUCIÓN BLOQUEADA: Este script es destructivo. El DBA debe cambiar v_permiso_ejecucion a ''SI''.');
  END IF;
END;
/

DECLARE
  PROCEDURE drop_table_if_exists(p_table_name IN VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE ' || p_table_name || ' CASCADE CONSTRAINTS';
    DBMS_OUTPUT.PUT_LINE('Tabla eliminada: ' || p_table_name);
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE != -942 THEN
        RAISE;
      END IF;
  END;

  PROCEDURE drop_sequence_if_exists(p_seq_name IN VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE ' || p_seq_name;
    DBMS_OUTPUT.PUT_LINE('Secuencia eliminada: ' || p_seq_name);
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN
        RAISE;
      END IF;
  END;
BEGIN
  DBMS_OUTPUT.PUT_LINE('--- INICIANDO RETIRO CONTROLADO DE ESTRUCTURAS DE PRUEBA RL_MR_* ---');

  -- 1. Eliminar tablas de relaciones y evidencias (Nivel 3)
  drop_table_if_exists('RL_MR_EVIDENCIAS');
  drop_table_if_exists('RL_MR_HISTORIAL');
  drop_table_if_exists('RL_MR_INTEGRACION_DNP');

  -- 2. Eliminar tablas transaccionales (Nivel 2)
  drop_table_if_exists('RL_MR_PLANES_ACCION');
  drop_table_if_exists('RL_MR_CONTROLES');
  drop_table_if_exists('RL_MR_RESULTADOS');
  drop_table_if_exists('RL_MR_DETALLE');

  -- 3. Eliminar tablas maestras y configuraciones (Nivel 1)
  drop_table_if_exists('RL_MR_MATRICES');
  drop_table_if_exists('RL_MR_CRITERIOS');
  drop_table_if_exists('RL_MR_ESCALAS');
  drop_table_if_exists('RL_MR_VARIABLES');
  drop_table_if_exists('RL_MR_FACTORES');
  drop_table_if_exists('RL_MR_MODELOS');

  -- 4. Eliminar secuencias asociadas
  drop_sequence_if_exists('SEQ_RL_MR_MODELOS');
  drop_sequence_if_exists('SEQ_RL_MR_FACTORES');
  drop_sequence_if_exists('SEQ_RL_MR_VARIABLES');
  drop_sequence_if_exists('SEQ_RL_MR_ESCALAS');
  drop_sequence_if_exists('SEQ_RL_MR_CRITERIOS');
  drop_sequence_if_exists('SEQ_RL_MR_MATRICES');
  drop_sequence_if_exists('SEQ_RL_MR_DETALLE');
  drop_sequence_if_exists('SEQ_RL_MR_CONTROLES');
  drop_sequence_if_exists('SEQ_RL_MR_RESULTADOS');
  drop_sequence_if_exists('SEQ_RL_MR_PLANES_ACCION');
  drop_sequence_if_exists('SEQ_RL_MR_EVIDENCIAS');
  drop_sequence_if_exists('SEQ_RL_MR_HISTORIAL');
  drop_sequence_if_exists('SEQ_RL_MR_INTEGRACION_DNP');

  DBMS_OUTPUT.PUT_LINE('--- RETIRO CONTROLADO COMPLETADO EXITOSAMENTE ---');
END;
/

COMMIT;
