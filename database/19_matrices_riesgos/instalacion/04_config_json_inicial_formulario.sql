-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGOS LA/FT - IHSS
-- Módulo: Matrices de Riesgos
-- Script: 04_config_json_inicial_formulario.sql
-- Objetivo: Cargar la configuración JSON inicial del Formulario A - Versión 1
--           con referencia explícita a una regla de cálculo versionada.
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
  v_familia_id   NUMBER;
  v_version_cnt  NUMBER;
  v_regla_cnt    NUMBER;
  v_json_config  CLOB;
  v_hash         VARCHAR2(64);
  v_usr_admin_id NUMBER;
BEGIN
  -- 1. Comprobar que la regla declarada por el formulario existe y está activa.
  SELECT COUNT(*)
    INTO v_regla_cnt
    FROM RL_MR_REGLAS_CALCULO
   WHERE REG_CODIGO = 'CALCULO_VRI_VRR'
     AND REG_VERSION = '1.0'
     AND REG_ACTIVA = 1;

  IF v_regla_cnt <> 1 THEN
    RAISE_APPLICATION_ERROR(
      -20102,
      'No existe una única regla activa CALCULO_VRI_VRR versión 1.0. Ejecute y valide primero el script 03.'
    );
  END IF;

  -- 2. Obtener un usuario válido para la creación de la versión.
  BEGIN
    SELECT USR_ID
      INTO v_usr_admin_id
      FROM RL_USUARIOS
     WHERE (LOWER(USR_EMAIL) = 'admin@ihss.hn' OR UPPER(USUARIO_DOMINIO) = 'ADMIN')
       AND ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      SELECT USR_ID
        INTO v_usr_admin_id
        FROM RL_USUARIOS
       WHERE ROWNUM = 1;
  END;

  -- 3. Asegurar la familia de forma idempotente.
  BEGIN
    SELECT FAM_ID
      INTO v_familia_id
      FROM RL_MR_FAMILIAS_FORMULARIO
     WHERE FAM_CODIGO = 'MATRIZ_RIESGOS_LAFT';
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      v_familia_id := SEQ_RL_MR_FAMILIAS.NEXTVAL;

      INSERT INTO RL_MR_FAMILIAS_FORMULARIO (
        FAM_ID,
        FAM_CODIGO,
        FAM_NOMBRE,
        FAM_DESCRIPCION,
        FAM_ACTIVO
      ) VALUES (
        v_familia_id,
        'MATRIZ_RIESGOS_LAFT',
        'Matriz de Riesgos LA/FT',
        'Formularios dinámicos de Matrices de Riesgos',
        1
      );
  END;

  -- 4. Definición del Formulario A. La referencia de regla es inmutable
  --    para las evaluaciones creadas con esta versión.
  v_json_config := '{
    "codigoFormulario": "MATRIZ_RIESGOS_LAFT",
    "nombreFormulario": "Matriz de Riesgos LA/FT - Formulario A",
    "version": 1,
    "reglas": [
      {
        "codigo": "CALCULO_VRI_VRR",
        "version": "1.0"
      }
    ],
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

  -- SHA-256 del contenido UTF-8 exacto anterior. Debe recalcularse cuando cambie VER_JSON.
  v_hash := '3f7e0f7b3372bb1de700a12bebe986e5550b39417bdd9ffc69f89f5d79f7d9c7';

  -- 5. Insertar o actualizar únicamente mientras la versión permanezca en DRAFT.
  SELECT COUNT(*)
    INTO v_version_cnt
    FROM RL_MR_VERSIONES_FORMULARIO
   WHERE VER_FAMILIA_ID = v_familia_id
     AND VER_VERSION = 1;

  IF v_version_cnt = 0 THEN
    INSERT INTO RL_MR_VERSIONES_FORMULARIO (
      VER_ID,
      VER_FAMILIA_ID,
      VER_CODIGO,
      VER_VERSION,
      VER_JSON,
      VER_HASH,
      VER_ESTADO,
      VER_VIGENTE,
      VER_USR_CREACION
    ) VALUES (
      SEQ_RL_MR_VERSIONES.NEXTVAL,
      v_familia_id,
      'FORM_A',
      1,
      v_json_config,
      v_hash,
      'DRAFT',
      0,
      v_usr_admin_id
    );
  ELSE
    UPDATE RL_MR_VERSIONES_FORMULARIO
       SET VER_JSON = v_json_config,
           VER_HASH = v_hash
     WHERE VER_FAMILIA_ID = v_familia_id
       AND VER_VERSION = 1
       AND VER_ESTADO = 'DRAFT';
  END IF;

  DBMS_OUTPUT.PUT_LINE('Formulario A versión 1 preparado con referencia de regla CALCULO_VRI_VRR 1.0.');
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    RAISE_APPLICATION_ERROR(
      -20103,
      'No existe un usuario válido en RL_USUARIOS para registrar la versión inicial.'
    );
  WHEN OTHERS THEN
    IF SQLCODE BETWEEN -20199 AND -20100 THEN
      RAISE;
    END IF;

    RAISE_APPLICATION_ERROR(
      -20101,
      'ERROR CRÍTICO en la carga del JSON inicial: ' || SQLERRM
    );
END;
/

COMMIT;
