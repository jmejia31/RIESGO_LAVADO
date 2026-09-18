-- FASE 7 - instalación segura/idempotente del release.
-- Es deliberadamente fail-closed: el modelo físico ya instalado no se recrea
-- y la transición inicial destructiva queda fuera de este paquete.
SET DEFINE ON
SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
DEFINE autorizacion = '&1'
DECLARE
  v_schema VARCHAR2(128);
  v_tables NUMBER;
BEGIN
  SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') INTO v_schema FROM DUAL;
  IF UPPER(v_schema) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(-20710, 'INSTALL bloqueado fuera de RIESGO_LAVADO.');
  END IF;
  IF UPPER(TRIM(q'[&autorizacion]')) <> 'VALIDAR' THEN
    RAISE_APPLICATION_ERROR(-20711, 'INSTALL requiere el argumento VALIDAR.');
  END IF;
  SELECT COUNT(*) INTO v_tables FROM USER_TABLES WHERE TABLE_NAME='RL_MR_RIESGOS';
  IF v_tables <> 1 THEN
    RAISE_APPLICATION_ERROR(-20712, 'Instalación limpia requiere la transición Oracle institucional aprobada; no se ejecuta DDL automáticamente.');
  END IF;
  DBMS_OUTPUT.PUT_LINE('INSTALL=PASS');
  DBMS_OUTPUT.PUT_LINE('INSTALL_MODE=IDEMPOTENT_ALREADY_INSTALLED');
END;
/
@@01_preflight_release.sql
