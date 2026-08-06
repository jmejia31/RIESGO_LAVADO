-- ============================================================
-- FASE 11 - BLOQUE 1
-- Validación de solo lectura de datos iniciales y configuración
-- Compatible con Oracle 11g
-- ============================================================

SET DEFINE OFF
SET SERVEROUTPUT ON SIZE UNLIMITED
SET PAGESIZE 200
SET LINESIZE 240
SET FEEDBACK ON
SET VERIFY OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

PROMPT === IDENTIDAD DE SESION ===
SELECT SYS_CONTEXT('USERENV', 'SESSION_USER') SESSION_USER,
       SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') CURRENT_SCHEMA,
       SYS_CONTEXT('USERENV', 'DB_NAME') DB_NAME,
       SYS_CONTEXT('USERENV', 'SERVICE_NAME') SERVICE_NAME
  FROM DUAL;

PROMPT === CONTEOS BLOQUE 1 ===
SELECT 'RL_MR_FAMILIAS_FORMULARIO' OBJETO, COUNT(*) TOTAL FROM RL_MR_FAMILIAS_FORMULARIO
UNION ALL
SELECT 'RL_MR_VERSIONES_FORMULARIO', COUNT(*) FROM RL_MR_VERSIONES_FORMULARIO
UNION ALL
SELECT 'RL_MR_CATALOGOS', COUNT(*) FROM RL_MR_CATALOGOS
UNION ALL
SELECT 'RL_MR_ELEMENTOS_CATALOGO', COUNT(*) FROM RL_MR_ELEMENTOS_CATALOGO
UNION ALL
SELECT 'RL_MR_REGLAS_CALCULO', COUNT(*) FROM RL_MR_REGLAS_CALCULO;

PROMPT === FAMILIA Y VERSION VIGENTE ===
SELECT f.FAM_ID,
       f.FAM_CODIGO,
       f.FAM_NOMBRE,
       f.FAM_ACTIVO,
       v.VER_ID,
       v.VER_CODIGO,
       v.VER_VERSION,
       v.VER_ESTADO,
       v.VER_VIGENTE,
       DBMS_LOB.GETLENGTH(v.VER_JSON) JSON_BYTES,
       v.VER_HASH,
       v.VER_USR_CREACION
  FROM RL_MR_FAMILIAS_FORMULARIO f
  JOIN RL_MR_VERSIONES_FORMULARIO v
    ON v.VER_FAMILIA_ID = f.FAM_ID
 WHERE f.FAM_CODIGO = 'MATRIZ_RIESGOS_LAFT'
 ORDER BY v.VER_VERSION;

PROMPT === CATALOGOS Y ELEMENTOS ACTIVOS ===
SELECT c.CAT_CODIGO,
       c.CAT_NOMBRE,
       c.CAT_ACTIVO,
       COUNT(CASE WHEN e.ELE_ACTIVO = 1 THEN 1 END) ELEMENTOS_ACTIVOS
  FROM RL_MR_CATALOGOS c
  LEFT JOIN RL_MR_ELEMENTOS_CATALOGO e
    ON e.ELE_CATALOGO_ID = c.CAT_ID
 WHERE c.CAT_CODIGO IN (
    'MR_FRECUENCIA_1_5',
    'MR_IMPACTO_1_5',
    'MR_NIVEL_RIESGO',
    'MR_RESPUESTA_RIESGO'
 )
 GROUP BY c.CAT_CODIGO, c.CAT_NOMBRE, c.CAT_ACTIVO
 ORDER BY c.CAT_CODIGO;

PROMPT === REGLA ACTIVA ===
SELECT REG_ID,
       REG_CODIGO,
       REG_VERSION,
       REG_NOMBRE,
       REG_ALGORITMO_ID,
       REG_ACTIVA
  FROM RL_MR_REGLAS_CALCULO
 WHERE REG_CODIGO = 'CALCULO_VRI_VRR'
   AND REG_VERSION = '1.0';

DECLARE
    c_hash CONSTANT VARCHAR2(64) := 'f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90';
    v_conteo NUMBER;

    PROCEDURE exigir(p_condicion BOOLEAN, p_codigo NUMBER, p_mensaje VARCHAR2) IS
    BEGIN
        IF NOT p_condicion THEN
            RAISE_APPLICATION_ERROR(p_codigo, p_mensaje);
        END IF;
    END;
BEGIN
    exigir(
        UPPER(SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')) = 'RIESGO_LAVADO',
        -21201,
        'CURRENT_SCHEMA debe ser RIESGO_LAVADO.'
    );

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_FAMILIAS_FORMULARIO f
      JOIN RL_MR_VERSIONES_FORMULARIO v
        ON v.VER_FAMILIA_ID = f.FAM_ID
      JOIN RL_USUARIOS u
        ON u.USR_ID = v.VER_USR_CREACION
       AND u.USR_ACTIVO = 1
     WHERE f.FAM_CODIGO = 'MATRIZ_RIESGOS_LAFT'
       AND f.FAM_ACTIVO = 1
       AND v.VER_CODIGO = 'MATRIZ_RIESGOS_LAFT_V1'
       AND v.VER_VERSION = 1
       AND v.VER_ESTADO = 'PUBLISHED'
       AND v.VER_VIGENTE = 1
       AND LOWER(v.VER_HASH) = c_hash
       AND DBMS_LOB.GETLENGTH(v.VER_JSON) > 0;
    exigir(v_conteo = 1, -21202, 'La familia/version oficial no esta publicada, vigente o integra.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_VERSIONES_FORMULARIO v
      JOIN RL_MR_FAMILIAS_FORMULARIO f
        ON f.FAM_ID = v.VER_FAMILIA_ID
     WHERE f.FAM_CODIGO = 'MATRIZ_RIESGOS_LAFT'
       AND v.VER_VIGENTE = 1;
    exigir(v_conteo = 1, -21203, 'Debe existir exactamente una version vigente para la familia.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_CATALOGOS
     WHERE CAT_CODIGO IN (
        'MR_FRECUENCIA_1_5',
        'MR_IMPACTO_1_5',
        'MR_NIVEL_RIESGO',
        'MR_RESPUESTA_RIESGO'
     )
       AND CAT_ACTIVO = 1;
    exigir(v_conteo = 4, -21204, 'Deben existir cuatro catalogos activos.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_ELEMENTOS_CATALOGO e
      JOIN RL_MR_CATALOGOS c
        ON c.CAT_ID = e.ELE_CATALOGO_ID
     WHERE c.CAT_CODIGO IN (
        'MR_FRECUENCIA_1_5',
        'MR_IMPACTO_1_5',
        'MR_NIVEL_RIESGO',
        'MR_RESPUESTA_RIESGO'
     )
       AND e.ELE_ACTIVO = 1;
    exigir(v_conteo = 18, -21205, 'Deben existir dieciocho elementos activos.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_REGLAS_CALCULO
     WHERE REG_CODIGO = 'CALCULO_VRI_VRR'
       AND REG_VERSION = '1.0'
       AND REG_ALGORITMO_ID = 'MATRICES_VRI_ADITIVO_1_9'
       AND REG_ACTIVA = 1;
    exigir(v_conteo = 1, -21206, 'La regla oficial no existe o no esta activa.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_ELEMENTOS_CATALOGO e
     WHERE NOT EXISTS (
        SELECT 1 FROM RL_MR_CATALOGOS c WHERE c.CAT_ID = e.ELE_CATALOGO_ID
     );
    exigir(v_conteo = 0, -21207, 'Existen elementos de catalogo huerfanos.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_VERSIONES_FORMULARIO v
     WHERE NOT EXISTS (
        SELECT 1 FROM RL_MR_FAMILIAS_FORMULARIO f WHERE f.FAM_ID = v.VER_FAMILIA_ID
     );
    exigir(v_conteo = 0, -21208, 'Existen versiones huerfanas.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM (
        SELECT FAM_CODIGO FROM RL_MR_FAMILIAS_FORMULARIO
         GROUP BY FAM_CODIGO HAVING COUNT(*) > 1
      );
    exigir(v_conteo = 0, -21209, 'Existen familias duplicadas.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM (
        SELECT CAT_CODIGO FROM RL_MR_CATALOGOS
         GROUP BY CAT_CODIGO HAVING COUNT(*) > 1
      );
    exigir(v_conteo = 0, -21210, 'Existen catalogos duplicados.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM (
        SELECT REG_CODIGO, REG_VERSION FROM RL_MR_REGLAS_CALCULO
         GROUP BY REG_CODIGO, REG_VERSION HAVING COUNT(*) > 1
      );
    exigir(v_conteo = 0, -21211, 'Existen reglas duplicadas.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM USER_OBJECTS
     WHERE OBJECT_NAME LIKE 'RL_MR_%'
       AND STATUS <> 'VALID';
    exigir(v_conteo = 0, -21212, 'Existen objetos RL_MR_* invalidos.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM USER_CONSTRAINTS
     WHERE TABLE_NAME LIKE 'RL_MR_%'
       AND STATUS <> 'ENABLED';
    exigir(v_conteo = 0, -21213, 'Existen restricciones RL_MR_* inactivas.');

    DBMS_OUTPUT.PUT_LINE('VALIDACION FASE 11 BLOQUE 1: CORRECTA');
END;
/

SET DEFINE ON
