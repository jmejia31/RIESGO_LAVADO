-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGOS LA/FT - IHSS
-- Módulo: Matrices de Riesgos
-- Script: 04_config_json_inicial_formulario.sql
-- Objetivo: Cargar la configuración JSON inicial del Formulario A - Versión 1 de manera idempotente.
-- Clasificación: SCRIPT EN FASE DE DISEÑO (BLOQUEADO HASTA FASE 5 DE INSTALACIÓN).
-- ============================================================

-- DIRECTIVA OBLIGATORIA PARA SQL*PLUS: ABORTAR TRANSACCIÓN Y EJECUCIÓN ANTE ERROR
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

-- PARÁMETRO DE ENTRADA OBLIGATORIO PARA SQL*PLUS: &1 (Palabra clave 'EJECUTAR')
DEFINE autorizacion = '&1';

-- BLOQUE DE SEGURIDAD EXPLICITO - IMPEDIR EJECUCIÓN ACCIDENTAL
DECLARE
  v_auth VARCHAR2(50) := q'[&autorizacion]';
BEGIN
  IF UPPER(v_auth) <> 'EJECUTAR' THEN
    RAISE_APPLICATION_ERROR(-20100, 'EJECUCIÓN BLOQUEADA: Script en fase de diseño. El DBA debe pasar "EJECUTAR" como primer argumento de SQL*Plus en la Fase 5.');
  END IF;
END;
/

DECLARE
  v_familia_id   NUMBER;
  v_version_cnt  NUMBER;
  v_json_config  CLOB;
  v_hash         VARCHAR2(64);
  v_usr_admin_id NUMBER;
BEGIN
  -- 1. Obtener un ID de usuario administrador válido para auditoría
  BEGIN
    SELECT USR_ID INTO v_usr_admin_id 
      FROM RL_USUARIOS 
     WHERE (LOWER(USR_EMAIL) = 'admin@ihss.hn' OR UPPER(USUARIO_DOMINIO) = 'ADMIN') AND ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      -- Fallback al primer usuario activo si no existe 'ADMIN'
      SELECT USR_ID INTO v_usr_admin_id 
        FROM RL_USUARIOS 
       WHERE ROWNUM = 1;
  END;

  -- 2. Asegurar existencia de la familia de forma idempotente
  BEGIN
    SELECT FAM_ID INTO v_familia_id 
      FROM RL_MR_FAMILIAS_FORMULARIO 
     WHERE FAM_CODIGO = 'MATRIZ_RIESGOS_LAFT';
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      v_familia_id := SEQ_RL_MR_FAMILIAS.NEXTVAL;
      INSERT INTO RL_MR_FAMILIAS_FORMULARIO (
        FAM_ID, FAM_CODIGO, FAM_NOMBRE, FAM_DESCRIPCION, FAM_ACTIVO
      )
      VALUES (
        v_familia_id, 'MATRIZ_RIESGOS_LAFT', 'Matriz de Riesgos LA/FT', 'Formularios dinámicos de Matrices de Riesgos', 1
      );
      DBMS_OUTPUT.PUT_LINE('Familia creada de forma idempotente: MATRIZ_RIESGOS_LAFT');
  END;

  -- 3. Definición del JSON inicial del Formulario A
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

  -- Calcular Hash SHA-256 inmutable del JSON
  v_hash := LOWER(RAWTOHEX(DBMS_CRYPTO.HASH(UTL_I18N.STRING_TO_RAW(v_json_config, 'AL32UTF8'), DBMS_CRYPTO.HASH_SH256)));

  -- 4. Insertar la Versión 1 únicamente si no existe
  SELECT COUNT(*) INTO v_version_cnt 
    FROM RL_MR_VERSIONES_FORMULARIO 
   WHERE VER_FAMILIA_ID = v_familia_id AND VER_VERSION = 1;

  IF v_version_cnt = 0 THEN
    INSERT INTO RL_MR_VERSIONES_FORMULARIO (
      VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, VER_HASH, VER_ESTADO, VER_VIGENTE, VER_USR_CREACION
    )
    VALUES (
      SEQ_RL_MR_VERSIONES.NEXTVAL, v_familia_id, 'FORM_A', 1, v_json_config, v_hash, 'DRAFT', 0, v_usr_admin_id
    );
    DBMS_OUTPUT.PUT_LINE('Configuración JSON del Formulario A - Versión 1 cargada con éxito.');
  ELSE
    -- Si ya existe y está en DRAFT, se actualiza el contenido
    UPDATE RL_MR_VERSIONES_FORMULARIO
       SET VER_JSON = v_json_config,
           VER_HASH = v_hash
     WHERE VER_FAMILIA_ID = v_familia_id 
       AND VER_VERSION = 1 
       AND VER_ESTADO = 'DRAFT';
    DBMS_OUTPUT.PUT_LINE('Configuración JSON del Formulario A - Versión 1 actualizada de forma idempotente.');
  END IF;

EXCEPTION
  WHEN OTHERS THEN
    -- Propagar el error para abortar ejecución y gatillar ROLLBACK en SQL*Plus
    RAISE_APPLICATION_ERROR(-20101, 'ERROR CRÍTICO en la carga del JSON inicial: ' || SQLERRM);
END;
/

COMMIT;
