-- =============================================================================
-- Matrices de Riesgos - validacion read-only de codificacion del catalogo maestro
-- Oracle 11g compatible. No ejecuta DDL ni DML.
-- =============================================================================

SET SERVEROUTPUT ON
SET PAGESIZE 500
SET LINESIZE 320
SET TRIMSPOOL ON
SET VERIFY OFF

PROMPT ============================================================
PROMPT VALIDACION CODIFICACION RL_MR_RIESGOS
PROMPT ============================================================

COLUMN RIE_CODIGO FORMAT A30
COLUMN RIE_NOMBRE FORMAT A180

SELECT RIE_CODIGO,
       RIE_NOMBRE
  FROM RL_MR_RIESGOS
 WHERE INSTR(NVL(RIE_NOMBRE, ' '), UNISTR('\FFFD')) > 0
    OR INSTR(NVL(RIE_DESCRIPCION, ' '), UNISTR('\FFFD')) > 0
    OR INSTR(NVL(RIE_NOMBRE, ' '), UNISTR('\00EF') || UNISTR('\00BF') || UNISTR('\00BD')) > 0
    OR INSTR(NVL(RIE_DESCRIPCION, ' '), UNISTR('\00EF') || UNISTR('\00BF') || UNISTR('\00BD')) > 0
    OR INSTR(NVL(RIE_NOMBRE, ' '), UNISTR('\00C3')) > 0
    OR INSTR(NVL(RIE_DESCRIPCION, ' '), UNISTR('\00C3')) > 0
    OR INSTR(NVL(RIE_NOMBRE, ' '), UNISTR('\00C2')) > 0
    OR INSTR(NVL(RIE_DESCRIPCION, ' '), UNISTR('\00C2')) > 0
 ORDER BY RIE_CODIGO;

DECLARE
  v_total       NUMBER := 0;
  v_sospechosos NUMBER := 0;
BEGIN
  SELECT COUNT(*)
    INTO v_total
    FROM RL_MR_RIESGOS;

  SELECT COUNT(*)
    INTO v_sospechosos
    FROM RL_MR_RIESGOS
   WHERE INSTR(NVL(RIE_NOMBRE, ' '), UNISTR('\FFFD')) > 0
      OR INSTR(NVL(RIE_DESCRIPCION, ' '), UNISTR('\FFFD')) > 0
      OR INSTR(NVL(RIE_NOMBRE, ' '), UNISTR('\00EF') || UNISTR('\00BF') || UNISTR('\00BD')) > 0
      OR INSTR(NVL(RIE_DESCRIPCION, ' '), UNISTR('\00EF') || UNISTR('\00BF') || UNISTR('\00BD')) > 0
      OR INSTR(NVL(RIE_NOMBRE, ' '), UNISTR('\00C3')) > 0
      OR INSTR(NVL(RIE_DESCRIPCION, ' '), UNISTR('\00C3')) > 0
      OR INSTR(NVL(RIE_NOMBRE, ' '), UNISTR('\00C2')) > 0
      OR INSTR(NVL(RIE_DESCRIPCION, ' '), UNISTR('\00C2')) > 0;

  DBMS_OUTPUT.PUT_LINE('RISK_TEXT_DB_TOTAL=' || v_total);
  DBMS_OUTPUT.PUT_LINE('RISK_TEXT_DB_MOJIBAKE=' || v_sospechosos);
  DBMS_OUTPUT.PUT_LINE(
    'RISK_TEXT_DB_STATUS=' ||
    CASE WHEN v_sospechosos = 0 THEN 'PASS' ELSE 'NEEDS_REPAIR' END
  );
END;
/

PROMPT ============================================================
PROMPT FIN VALIDACION CODIFICACION RL_MR_RIESGOS
PROMPT ============================================================
