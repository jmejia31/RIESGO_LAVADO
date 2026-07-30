-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGOS LA/FT - IHSS
-- Módulo: Matrices de Riesgos
-- Script: 04_config_json_inicial_formulario.sql
-- Objetivo: Cargar la configuración JSON inicial del Formulario A - Versión 1.
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
  v_familia_id NUMBER;
  v_json_config CLOB;
  v_hash VARCHAR2(64);
BEGIN
  -- 1. Insertar la Familia si no existe
  SELECT FAM_ID INTO v_familia_id 
    FROM RL_MR_FAMILIAS_FORMULARIO 
   WHERE FAM_CODIGO = 'MATRIZ_RIESGOS_LAFT';
   
  -- Configuración JSON inicial del Formulario A
  v_json_config := '{
    "codigoFormulario": "MATRIZ_RIESGOS_LAFT",
    "nombreFormulario": "Matriz de Riesgos LA/FT - Formulario A",
    "version": 1,
    "secciones": [
      {
        "id": "identificacion",
        "titulo": "1. Identificación y Contexto",
        "campos": [
          { "id": "area_principal", "etiqueta": "Área Responsable", "tipo": "selector-catalogo", "codigoCatalogo": "CAT_AREAS", "obligatorio": true },
          { "id": "tipo_riesgo", "etiqueta": "Tipo de Riesgo", "tipo": "texto", "obligatorio": true },
          { "id": "procedimiento", "etiqueta": "Procedimiento Vinculado", "tipo": "texto", "obligatorio": false }
        ]
      },
      {
        "id": "riesgo_inherente",
        "titulo": "2. Evaluación del Riesgo Inherente",
        "campos": [
          { "id": "descripcion_riesgo", "etiqueta": "Descripción del Evento", "tipo": "texto-largo", "obligatorio": true },
          { "id": "frecuencia_inherente", "etiqueta": "Frecuencia", "tipo": "selector-catalogo", "codigoCatalogo": "CAT_FRECUENCIA", "obligatorio": true },
          { "id": "impacto_inherente", "etiqueta": "Impacto", "tipo": "selector-catalogo", "codigoCatalogo": "CAT_IMPACTO", "obligatorio": true }
        ]
      }
    ]
  }';

  -- Calcular Hash SHA-256 preliminar del JSON en Oracle
  v_hash := LOWER(RAWTOHEX(DBMS_CRYPTO.HASH(UTL_I18N.STRING_TO_RAW(v_json_config, 'AL32UTF8'), DBMS_CRYPTO.HASH_SH256)));

  -- Insertar la Versión 1 en Borrador (DRAFT)
  INSERT INTO RL_MR_VERSIONES_FORMULARIO (
    VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, VER_HASH, VER_ESTADO, VER_VIGENTE, VER_USR_CREACION
  )
  VALUES (
    SEQ_RL_MR_VERSIONES.NEXTVAL, v_familia_id, 'FORM_A', 1, v_json_config, v_hash, 'DRAFT', 0, 1
  );

  DBMS_OUTPUT.PUT_LINE('Configuración JSON inicial del Formulario A - Versión 1 insertada con éxito.');
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    DBMS_OUTPUT.PUT_LINE('ERROR: No se encontró la familia de formulario MATRIZ_RIESGOS_LAFT. Ejecutar script 03 primero.');
END;
/

COMMIT;
