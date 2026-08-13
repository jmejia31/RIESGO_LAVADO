-- ============================================================
-- FASE 11 - BLOQUE 3
-- Flujos de Evaluación - validación de solo lectura
-- Estados oficiales: BORRADOR, EN_REVISION, OBSERVADA, APROBADA,
-- RECHAZADA, CERRADA.
-- ============================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

PROMPT === DISTRIBUCION DE ESTADOS ===
SELECT FLU_ESTADO, COUNT(*) AS TOTAL
  FROM RL_MR_FLUJOS_EVALUACION
 GROUP BY FLU_ESTADO
 ORDER BY FLU_ESTADO ASC;

PROMPT === ULTIMO ESTADO POR EVALUACION ===
SELECT FLU_EVALUACION_ID, FLU_ESTADO, FLU_MOTIVO, FLU_USR_ID, FLU_FECHA
  FROM (
        SELECT f.*,
               ROW_NUMBER() OVER (
                   PARTITION BY FLU_EVALUACION_ID
                   ORDER BY FLU_FECHA DESC, FLU_ID DESC
                ) AS RN
          FROM RL_MR_FLUJOS_EVALUACION f
       )
 WHERE RN = 1
 ORDER BY FLU_EVALUACION_ID ASC;

DECLARE
    v_count NUMBER;
    PROCEDURE exigir(p_cond BOOLEAN, p_code NUMBER, p_message VARCHAR2) IS
    BEGIN
        IF NOT p_cond THEN RAISE_APPLICATION_ERROR(p_code, p_message); END IF;
    END;
BEGIN
    exigir(UPPER(SYS_CONTEXT('USERENV','CURRENT_SCHEMA')) = 'RIESGO_LAVADO', -20401,
           'CURRENT_SCHEMA debe ser RIESGO_LAVADO.');

    SELECT COUNT(*) INTO v_count
      FROM RL_MR_FLUJOS_EVALUACION
     WHERE FLU_ESTADO NOT IN ('BORRADOR','EN_REVISION','OBSERVADA','APROBADA','RECHAZADA','CERRADA');
    exigir(v_count = 0, -20402, 'Existen estados de flujo fuera del dominio oficial.');

    SELECT COUNT(*) INTO v_count
      FROM RL_MR_FLUJOS_EVALUACION f
      LEFT JOIN RL_MR_EVALUACIONES_RIESGO e ON e.EVA_ID = f.FLU_EVALUACION_ID
      LEFT JOIN RL_USUARIOS u ON u.USR_ID = f.FLU_USR_ID
     WHERE e.EVA_ID IS NULL OR u.USR_ID IS NULL;
    exigir(v_count = 0, -20403, 'Existen flujos con referencias inválidas.');

    SELECT COUNT(*) INTO v_count
      FROM RL_MR_EVALUACIONES_RIESGO e
      LEFT JOIN RL_MR_FLUJOS_EVALUACION f ON f.FLU_EVALUACION_ID = e.EVA_ID
     WHERE e.EVA_ACTIVO = 1 AND f.FLU_EVALUACION_ID IS NULL;
    exigir(v_count = 0, -20404, 'Existen evaluaciones activas sin historial de flujo.');

    SELECT COUNT(*) INTO v_count
      FROM (
            SELECT p.PROY_ESTADO_EVALUACION,
                   f.FLU_ESTADO AS ULTIMO_ESTADO
              FROM RL_MR_EVALUACIONES_RIESGO e
              JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID = e.EVA_ID
              LEFT JOIN (
                  SELECT FLU_EVALUACION_ID, FLU_ESTADO,
                         ROW_NUMBER() OVER (PARTITION BY FLU_EVALUACION_ID ORDER BY FLU_FECHA DESC, FLU_ID DESC) RN
                    FROM RL_MR_FLUJOS_EVALUACION
              ) f ON f.FLU_EVALUACION_ID = e.EVA_ID AND f.RN = 1
             WHERE e.EVA_ACTIVO = 1
           )
     WHERE NVL(PROY_ESTADO_EVALUACION, '#') <> NVL(ULTIMO_ESTADO, '#');
    exigir(v_count = 0, -20405, 'La proyección no coincide con el último estado del flujo.');

    DBMS_OUTPUT.PUT_LINE('VALIDACION FASE 11 BLOQUE 3: CORRECTA');
END;
/
