-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGOS LA/FT - IHSS
-- Módulo: Matrices de Riesgos
-- Script: 03_seed_catalogos_iniciales.sql
-- Objetivo: Insertar catálogos base requeridos por la metodología institucional.
-- Clasificación: SCRIPT EN FASE DE DISEÑO (BLOQUEADO HASTA FASE 5 DE INSTALACIÓN).
-- ============================================================

-- DIRECTIVA OBLIGATORIA PARA SQL*PLUS: ABORTAR TRANSACCIÓN Y EJECUCIÓN ANTE ERROR
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK;

-- BLOQUE DE SEGURIDAD EXPLICITO - IMPEDIR EJECUCIÓN ACCIDENTAL
DECLARE
  v_fase_aprobada VARCHAR2(10) := 'NO'; -- CAMBIAR A 'SI' CUANDO SE AUTORICE LA FASE 5 DE INSTALACIÓN
BEGIN
  IF v_fase_aprobada <> 'SI' THEN
    RAISE_APPLICATION_ERROR(-20100, 'EJECUCIÓN BLOQUEADA: Este script está en fase de diseño. El DBA debe cambiar v_fase_aprobada a ''SI'' en la Fase 5.');
  END IF;
END;
/

DECLARE
  PROCEDURE upsert_catalogo(p_codigo IN VARCHAR2, p_nombre IN VARCHAR2) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count FROM RL_MR_CATALOGOS WHERE CAT_CODIGO = p_codigo;
    IF v_count = 0 THEN
      INSERT INTO RL_MR_CATALOGOS (CAT_ID, CAT_CODIGO, CAT_NOMBRE, CAT_ACTIVO)
      VALUES (SEQ_RL_MR_CATALOGOS.NEXTVAL, p_codigo, p_nombre, 1);
    END IF;
  END;

  PROCEDURE upsert_elemento(p_cat_codigo IN VARCHAR2, p_ele_codigo IN VARCHAR2, p_valor IN VARCHAR2, p_orden IN NUMBER) IS
    v_cat_id NUMBER;
    v_count NUMBER;
  BEGIN
    SELECT CAT_ID INTO v_cat_id FROM RL_MR_CATALOGOS WHERE CAT_CODIGO = p_cat_codigo;
    SELECT COUNT(*) INTO v_count FROM RL_MR_ELEMENTOS_CATALOGO WHERE ELE_CATALOGO_ID = v_cat_id AND ELE_CODIGO = p_ele_codigo;
    
    IF v_count = 0 THEN
      INSERT INTO RL_MR_ELEMENTOS_CATALOGO (ELE_ID, ELE_CATALOGO_ID, ELE_CODIGO, ELE_VALOR, ELE_ORDEN, ELE_ACTIVO)
      VALUES (SEQ_RL_MR_ELEMENTOS.NEXTVAL, v_cat_id, p_ele_codigo, p_valor, p_orden, 1);
    END IF;
  END;
BEGIN
  -- 1. CREACIÓN DE CATÁLOGOS BASE
  upsert_catalogo('CAT_FRECUENCIA', 'Escala de Frecuencia');
  upsert_catalogo('CAT_IMPACTO', 'Escala de Impacto');
  upsert_catalogo('CAT_TIPO_CONTROL', 'Tipos de Control');
  upsert_catalogo('CAT_EFECTIVIDAD_CONTROL', 'Escala de Efectividad de Control');
  upsert_catalogo('CAT_RESPUESTA_RIESGO', 'Respuestas al Riesgo (Tratamiento)');
  upsert_catalogo('CAT_AREAS', 'Áreas Organizacionales');

  -- 2. ELEMENTOS DE CATÁLOGOS
  -- Frecuencia
  upsert_elemento('CAT_FRECUENCIA', '1', 'Muy Baja', 1);
  upsert_elemento('CAT_FRECUENCIA', '2', 'Baja', 2);
  upsert_elemento('CAT_FRECUENCIA', '3', 'Media', 3);
  upsert_elemento('CAT_FRECUENCIA', '4', 'Alta', 4);
  upsert_elemento('CAT_FRECUENCIA', '5', 'Muy Alta', 5);

  -- Impacto
  upsert_elemento('CAT_IMPACTO', '1', 'Insignificante', 1);
  upsert_elemento('CAT_IMPACTO', '2', 'Menor', 2);
  upsert_elemento('CAT_IMPACTO', '3', 'Moderado', 3);
  upsert_elemento('CAT_IMPACTO', '4', 'Mayor', 4);
  upsert_elemento('CAT_IMPACTO', '5', 'Catastrófico', 5);

  -- Tipos de Control
  upsert_elemento('CAT_TIPO_CONTROL', 'PREV', 'Preventivo', 1);
  upsert_elemento('CAT_TIPO_CONTROL', 'DET', 'Detectivo', 2);
  upsert_elemento('CAT_TIPO_CONTROL', 'CORR', 'Correctivo', 3);

  -- Respuestas al Riesgo
  upsert_elemento('CAT_RESPUESTA_RIESGO', 'ACEPTAR', 'Aceptar el riesgo', 1);
  upsert_elemento('CAT_RESPUESTA_RIESGO', 'EVITAR', 'Evitar el riesgo', 2);
  upsert_elemento('CAT_RESPUESTA_RIESGO', 'TRANSFERIR', 'Transferir / Compartir', 3);
  upsert_elemento('CAT_RESPUESTA_RIESGO', 'MITIGAR', 'Mitigar / Reducir', 4);

  DBMS_OUTPUT.PUT_LINE('Semillas de catálogos base insertadas con éxito.');
END;
/

COMMIT;
