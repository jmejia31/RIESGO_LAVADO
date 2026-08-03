-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Autor: Antigravity / Codex
-- Módulo: Matrices de Riesgos
-- Script: 05_ajustes_dashboard_seguridad_reportes.sql
-- Propósito: Aplicación de restricción única en proyecciones e índices optimizados
-- ============================================================

DECLARE
    v_existe NUMBER;
    v_duplicados NUMBER;
    v_ejecutar VARCHAR2(10) := 'EJECUTAR'; -- Variable de protección contra ejecuciones accidentales
BEGIN
    PROMPT Evaluando consistencia física de la tabla RL_MR_PROYECCIONES_EVALUACION...;

    -- 1. Verificar si la restricción UQ_RL_MR_PROY_EVA ya existe
    SELECT COUNT(*)
      INTO v_existe
      FROM USER_CONSTRAINTS
     WHERE CONSTRAINT_NAME = 'UQ_RL_MR_PROY_EVA'
       AND TABLE_NAME = 'RL_MR_PROYECCIONES_EVALUACION';

    IF v_existe > 0 THEN
        DBMS_OUTPUT.PUT_LINE('La restricción única UQ_RL_MR_PROY_EVA ya se encuentra aplicada.');
    ELSE
        -- 2. Detectar duplicados de PROY_EVALUACION_ID
        SELECT COUNT(*)
          INTO v_duplicados
          FROM (
              SELECT PROY_EVALUACION_ID
                FROM RL_MR_PROYECCIONES_EVALUACION
               GROUP BY PROY_EVALUACION_ID
              HAVING COUNT(*) > 1
          );

        IF v_duplicados > 0 THEN
            DBMS_OUTPUT.PUT_LINE('ERROR BLOQUEANTE: Se detectaron ' || v_duplicados || ' evaluaciones con proyecciones duplicadas.');
            -- Listar los registros conflictivos para permitir la conciliación manual
            FOR r IN (
                SELECT PROY_EVALUACION_ID, COUNT(*) AS CANTIDAD
                  FROM RL_MR_PROYECCIONES_EVALUACION
                 GROUP BY PROY_EVALUACION_ID
                HAVING COUNT(*) > 1
            ) LOOP
                DBMS_OUTPUT.PUT_LINE(' -> Evaluación ID conflictiva: ' || r.PROY_EVALUACION_ID || ' (Cantidad: ' || r.CANTIDAD || ')');
            END LOOP;
            raise_application_error(-20001, 'No se puede aplicar el índice de unicidad debido a datos duplicados en el ambiente local.');
        ELSE
            -- 3. Crear el constraint de unicidad de forma segura
            EXECUTE IMMEDIATE 'ALTER TABLE RL_MR_PROYECCIONES_EVALUACION ADD CONSTRAINT UQ_RL_MR_PROY_EVA UNIQUE (PROY_EVALUACION_ID)';
            DBMS_OUTPUT.PUT_LINE('Restricción única UQ_RL_MR_PROY_EVA creada exitosamente.');
        END IF;
    END IF;

    -- 4. Crear índice optimizado adicional en proyecciones para acelerar el filtrado del dashboard
    SELECT COUNT(*)
      INTO v_existe
      FROM USER_INDEXES
     WHERE INDEX_NAME = 'IX_RL_MR_PROY_DASHBOARD';

    IF v_existe = 0 THEN
        EXECUTE IMMEDIATE 'CREATE INDEX IX_RL_MR_PROY_DASHBOARD ON RL_MR_PROYECCIONES_EVALUACION (PROY_NIVEL_INHERENTE, PROY_NIVEL_RESIDUAL)';
        DBMS_OUTPUT.PUT_LINE('Índice IX_RL_MR_PROY_DASHBOARD creado exitosamente.');
    END IF;
END;
/
