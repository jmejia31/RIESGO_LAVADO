-- ============================================================================
-- Proyecto: SGRLA IHSS
-- Modulo: Matrices de Riesgos
-- Fase: 3. Modelo de datos y arquitectura Oracle
-- Script: 05_F3_align_estado_en_evaluacion_matrices.sql
-- Tipo: Aprobable / incremental controlado
-- Responsable: Javier Mejia
-- Objetivo:
--   Alinear la restriccion fisica CK_RL_MR_MAT_ESTADO con el flujo funcional
--   aprobado en Fase 4, incorporando el estado EN_EVALUACION.
--
-- Seguridad:
--   - No elimina informacion productiva.
--   - No contiene DROP/TRUNCATE/DELETE de tablas o datos.
--   - Solo recrea una restriccion CHECK despues de validar estados existentes.
--   - Debe ejecutarse despues de los scripts 01, 02, 03 y 04, si aplica.
-- ============================================================================

SET DEFINE OFF;
SET SERVEROUTPUT ON SIZE UNLIMITED;

PROMPT Validando estados existentes en RL_MR_MATRICES...

DECLARE
  v_invalidos NUMBER;
  v_existe_constraint NUMBER;
BEGIN
  SELECT COUNT(*)
    INTO v_invalidos
    FROM RL_MR_MATRICES
   WHERE MRMAT_ESTADO NOT IN (
          'BORRADOR',
          'EN_EVALUACION',
          'CALCULADA',
          'EN_REVISION',
          'OBSERVADA',
          'APROBADA',
          'CERRADA',
          'INACTIVA'
        );

  IF v_invalidos > 0 THEN
    RAISE_APPLICATION_ERROR(
      -20160,
      'Existen estados de matriz no compatibles con la restriccion propuesta.'
    );
  END IF;

  SELECT COUNT(*)
    INTO v_existe_constraint
    FROM USER_CONSTRAINTS
   WHERE TABLE_NAME = 'RL_MR_MATRICES'
     AND CONSTRAINT_NAME = 'CK_RL_MR_MAT_ESTADO';

  IF v_existe_constraint > 0 THEN
    EXECUTE IMMEDIATE
      'ALTER TABLE RL_MR_MATRICES DROP CONSTRAINT CK_RL_MR_MAT_ESTADO';
  END IF;

  EXECUTE IMMEDIATE q'[
    ALTER TABLE RL_MR_MATRICES ADD CONSTRAINT CK_RL_MR_MAT_ESTADO
    CHECK (
      MRMAT_ESTADO IN (
        'BORRADOR',
        'EN_EVALUACION',
        'CALCULADA',
        'EN_REVISION',
        'OBSERVADA',
        'APROBADA',
        'CERRADA',
        'INACTIVA'
      )
    )
  ]';

  DBMS_OUTPUT.PUT_LINE('Restriccion CK_RL_MR_MAT_ESTADO alineada correctamente.');
END;
/

COMMIT;

PROMPT Validacion posterior...

SELECT
  CASE
    WHEN INSTR(DBMS_METADATA.GET_DDL('CONSTRAINT', 'CK_RL_MR_MAT_ESTADO'), 'EN_EVALUACION') > 0
    THEN 'SI'
    ELSE 'NO'
  END AS EN_EVALUACION_PRESENTE
FROM DUAL;

SELECT MRMAT_ESTADO, COUNT(*) TOTAL
  FROM RL_MR_MATRICES
 GROUP BY MRMAT_ESTADO
 ORDER BY MRMAT_ESTADO;

SELECT OBJECT_TYPE, OBJECT_NAME, STATUS
  FROM USER_OBJECTS
 WHERE STATUS <> 'VALID'
 ORDER BY OBJECT_TYPE, OBJECT_NAME;

PROMPT Script 05 finalizado.
