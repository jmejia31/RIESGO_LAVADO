-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGOS LA/FT - IHSS
-- Módulo: Matrices de Riesgos
-- Script: 05_ajustes_dashboard_seguridad_reportes.sql
-- Objetivo: aplicar de forma controlada la unicidad de proyecciones
--           e índices requeridos por dashboard y reportes.
-- Compatibilidad: Oracle 11g / SQL*Plus.
-- ============================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DEFINE autorizacion = '&1';

PROMPT Validando autorización, esquema y consistencia de proyecciones...

DECLARE
    v_auth           VARCHAR2(50) := q'[&autorizacion]';
    v_esquema_actual VARCHAR2(128);
BEGIN
    SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
      INTO v_esquema_actual
      FROM DUAL;

    IF UPPER(v_esquema_actual) <> 'RIESGO_LAVADO' THEN
        RAISE_APPLICATION_ERROR(
            -20098,
            'EJECUCIÓN BLOQUEADA: el script solo puede ejecutarse en el esquema RIESGO_LAVADO. Esquema detectado: ' || v_esquema_actual
        );
    END IF;

    IF UPPER(TRIM(v_auth)) <> 'EJECUTAR' THEN
        RAISE_APPLICATION_ERROR(
            -20100,
            'EJECUCIÓN BLOQUEADA: debe proporcionar EJECUTAR como primer argumento de SQL*Plus.'
        );
    END IF;
END;
/

DECLARE
    v_existe_constraint NUMBER := 0;
    v_existe_indice     NUMBER := 0;
    v_duplicados        NUMBER := 0;
BEGIN
    SELECT COUNT(*)
      INTO v_existe_constraint
      FROM USER_CONSTRAINTS
     WHERE CONSTRAINT_NAME = 'UQ_RL_MR_PROY_EVA'
       AND TABLE_NAME = 'RL_MR_PROYECCIONES_EVALUACION'
       AND CONSTRAINT_TYPE = 'U';

    SELECT COUNT(*)
      INTO v_duplicados
      FROM (
          SELECT PROY_EVALUACION_ID
            FROM RL_MR_PROYECCIONES_EVALUACION
           GROUP BY PROY_EVALUACION_ID
          HAVING COUNT(*) > 1
      );

    IF v_duplicados > 0 THEN
        DBMS_OUTPUT.PUT_LINE('ERROR BLOQUEANTE: se detectaron ' || v_duplicados || ' evaluaciones con proyecciones duplicadas.');

        FOR r IN (
            SELECT PROY_EVALUACION_ID, COUNT(*) AS CANTIDAD
              FROM RL_MR_PROYECCIONES_EVALUACION
             GROUP BY PROY_EVALUACION_ID
            HAVING COUNT(*) > 1
             ORDER BY PROY_EVALUACION_ID ASC
        ) LOOP
            DBMS_OUTPUT.PUT_LINE(
                'Evaluación conflictiva: ' || r.PROY_EVALUACION_ID ||
                ' | cantidad de proyecciones: ' || r.CANTIDAD
            );
        END LOOP;

        RAISE_APPLICATION_ERROR(
            -20001,
            'No se puede aplicar la unicidad hasta conciliar las proyecciones duplicadas.'
        );
    END IF;

    IF v_existe_constraint = 0 THEN
        EXECUTE IMMEDIATE /* NOSONAR: DDL fijo, ejecutado solo tras prevalidar el esquema y la integridad de proyecciones. */ q'[
            ALTER TABLE RL_MR_PROYECCIONES_EVALUACION
            ADD CONSTRAINT UQ_RL_MR_PROY_EVA
            UNIQUE (PROY_EVALUACION_ID)
        ]';
        DBMS_OUTPUT.PUT_LINE('Restricción UQ_RL_MR_PROY_EVA creada correctamente.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Restricción UQ_RL_MR_PROY_EVA ya existente; no se recrea.');
    END IF;

    SELECT COUNT(*)
      INTO v_existe_indice
      FROM USER_INDEXES
     WHERE INDEX_NAME = 'IX_RL_MR_PROY_DASHBOARD'
       AND TABLE_NAME = 'RL_MR_PROYECCIONES_EVALUACION';

    IF v_existe_indice = 0 THEN
        EXECUTE IMMEDIATE /* NOSONAR: DDL fijo, ejecutado solo tras comprobar que el índice no existe. */ q'[
            CREATE INDEX IX_RL_MR_PROY_DASHBOARD
            ON RL_MR_PROYECCIONES_EVALUACION (
                PROY_NIVEL_INHERENTE,
                PROY_NIVEL_RESIDUAL,
                PROY_ESTADO_EVALUACION,
                PROY_FECHA_EVAL
            )
        ]';
        DBMS_OUTPUT.PUT_LINE('Índice IX_RL_MR_PROY_DASHBOARD creado correctamente.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('Índice IX_RL_MR_PROY_DASHBOARD ya existente; no se recrea.');
    END IF;
END;
/

PROMPT Script 05 completado correctamente.
