-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGOS LA/FT - IHSS
-- Módulo: Matrices de Riesgos
-- Script: 03_seed_catalogos_iniciales.sql
-- Objetivo: Insertar catálogos base y metadatos de reglas requeridos
--           por la metodología institucional dinámica.
-- Clasificación: SCRIPT EN FASE DE DISEÑO (NO EJECUTAR SIN REVISIÓN).
-- ============================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DEFINE autorizacion = '&1';

DECLARE
  v_auth           VARCHAR2(50) := q'[&autorizacion]';
  v_esquema_actual VARCHAR2(100);
BEGIN
  SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
    INTO v_esquema_actual
    FROM DUAL;

  IF UPPER(v_esquema_actual) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(
      -20098,
      'EJECUCIÓN BLOQUEADA: Este script solo puede ejecutarse en el esquema RIESGO_LAVADO. Esquema detectado: ' || v_esquema_actual
    );
  END IF;

  IF UPPER(TRIM(v_auth)) <> 'EJECUTAR' THEN
    RAISE_APPLICATION_ERROR(
      -20100,
      'EJECUCIÓN BLOQUEADA: El DBA debe proporcionar EJECUTAR como primer argumento de SQL*Plus.'
    );
  END IF;
END;
/

DECLARE
  PROCEDURE upsert_catalogo(
    p_codigo IN VARCHAR2,
    p_nombre IN VARCHAR2
  ) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM RL_MR_CATALOGOS
     WHERE CAT_CODIGO = p_codigo;

    IF v_count = 0 THEN
      INSERT INTO RL_MR_CATALOGOS (
        CAT_ID,
        CAT_CODIGO,
        CAT_NOMBRE,
        CAT_ACTIVO
      ) VALUES (
        SEQ_RL_MR_CATALOGOS.NEXTVAL,
        p_codigo,
        p_nombre,
        1
      );
    ELSE
      UPDATE RL_MR_CATALOGOS
         SET CAT_NOMBRE = p_nombre,
             CAT_ACTIVO = 1
       WHERE CAT_CODIGO = p_codigo;
    END IF;
  END;

  PROCEDURE upsert_elemento(
    p_cat_codigo IN VARCHAR2,
    p_ele_codigo IN VARCHAR2,
    p_valor      IN VARCHAR2,
    p_orden      IN NUMBER
  ) IS
    v_cat_id NUMBER;
    v_count  NUMBER;
  BEGIN
    SELECT CAT_ID
      INTO v_cat_id
      FROM RL_MR_CATALOGOS
     WHERE CAT_CODIGO = p_cat_codigo;

    SELECT COUNT(*)
      INTO v_count
      FROM RL_MR_ELEMENTOS_CATALOGO
     WHERE ELE_CATALOGO_ID = v_cat_id
       AND ELE_CODIGO = p_ele_codigo;

    IF v_count = 0 THEN
      INSERT INTO RL_MR_ELEMENTOS_CATALOGO (
        ELE_ID,
        ELE_CATALOGO_ID,
        ELE_CODIGO,
        ELE_VALOR,
        ELE_ORDEN,
        ELE_ACTIVO
      ) VALUES (
        SEQ_RL_MR_ELEMENTOS.NEXTVAL,
        v_cat_id,
        p_ele_codigo,
        p_valor,
        p_orden,
        1
      );
    ELSE
      UPDATE RL_MR_ELEMENTOS_CATALOGO
         SET ELE_VALOR = p_valor,
             ELE_ORDEN = p_orden,
             ELE_ACTIVO = 1
       WHERE ELE_CATALOGO_ID = v_cat_id
         AND ELE_CODIGO = p_ele_codigo;
    END IF;
  END;

  PROCEDURE upsert_regla(
    p_codigo       IN VARCHAR2,
    p_version      IN VARCHAR2,
    p_nombre       IN VARCHAR2,
    p_algoritmo_id IN VARCHAR2
  ) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*)
      INTO v_count
      FROM RL_MR_REGLAS_CALCULO
     WHERE REG_CODIGO = p_codigo
       AND REG_VERSION = p_version;

    IF v_count = 0 THEN
      INSERT INTO RL_MR_REGLAS_CALCULO (
        REG_ID,
        REG_CODIGO,
        REG_VERSION,
        REG_NOMBRE,
        REG_ALGORITMO_ID,
        REG_ACTIVA
      ) VALUES (
        SEQ_RL_MR_REGLAS.NEXTVAL,
        p_codigo,
        p_version,
        p_nombre,
        p_algoritmo_id,
        1
      );
    ELSE
      UPDATE RL_MR_REGLAS_CALCULO
         SET REG_NOMBRE = p_nombre,
             REG_ALGORITMO_ID = p_algoritmo_id,
             REG_ACTIVA = 1
       WHERE REG_CODIGO = p_codigo
         AND REG_VERSION = p_version;
    END IF;
  END;
BEGIN
  -- 1. CATÁLOGOS BASE
  upsert_catalogo('CAT_FRECUENCIA', 'Escala de Frecuencia');
  upsert_catalogo('CAT_IMPACTO', 'Escala de Impacto');
  upsert_catalogo('CAT_TIPO_CONTROL', 'Tipos de Control');
  upsert_catalogo('CAT_EFECTIVIDAD_CONTROL', 'Escala de Efectividad de Control');
  upsert_catalogo('CAT_RESPUESTA_RIESGO', 'Respuestas al Riesgo (Tratamiento)');
  upsert_catalogo('CAT_AREAS', 'Áreas Organizacionales');

  -- 2. FRECUENCIA 1–5
  upsert_elemento('CAT_FRECUENCIA', '1', 'Muy Baja', 1);
  upsert_elemento('CAT_FRECUENCIA', '2', 'Baja', 2);
  upsert_elemento('CAT_FRECUENCIA', '3', 'Media', 3);
  upsert_elemento('CAT_FRECUENCIA', '4', 'Alta', 4);
  upsert_elemento('CAT_FRECUENCIA', '5', 'Muy Alta', 5);

  -- 3. IMPACTO 1–5
  upsert_elemento('CAT_IMPACTO', '1', 'Insignificante', 1);
  upsert_elemento('CAT_IMPACTO', '2', 'Menor', 2);
  upsert_elemento('CAT_IMPACTO', '3', 'Moderado', 3);
  upsert_elemento('CAT_IMPACTO', '4', 'Mayor', 4);
  upsert_elemento('CAT_IMPACTO', '5', 'Catastrófico', 5);

  -- 4. TIPOS DE CONTROL
  upsert_elemento('CAT_TIPO_CONTROL', 'PREV', 'Preventivo', 1);
  upsert_elemento('CAT_TIPO_CONTROL', 'DET', 'Detectivo', 2);
  upsert_elemento('CAT_TIPO_CONTROL', 'CORR', 'Correctivo', 3);

  -- 5. RESPUESTAS AL RIESGO
  upsert_elemento('CAT_RESPUESTA_RIESGO', 'ACEPTAR', 'Aceptar el riesgo', 1);
  upsert_elemento('CAT_RESPUESTA_RIESGO', 'EVITAR', 'Evitar el riesgo', 2);
  upsert_elemento('CAT_RESPUESTA_RIESGO', 'TRANSFERIR', 'Transferir / Compartir', 3);
  upsert_elemento('CAT_RESPUESTA_RIESGO', 'MITIGAR', 'Mitigar / Reducir', 4);

  -- 6. REGLA VERSIONADA. Los umbrales de clasificación no se fijan aquí.
  -- La fórmula institucional es VRI = Frecuencia + Impacto - 1, dominio 1–9.
  upsert_regla(
    'CALCULO_VRI_VRR',
    '1.0',
    'Cálculo institucional aditivo VRI/VRR',
    'MATRICES_VRI_ADITIVO_1_9'
  );

  DBMS_OUTPUT.PUT_LINE('Catálogos y regla versionada cargados de forma idempotente.');
END;
/

COMMIT;
