-- Oracle 11g / READ ONLY. Ejecutar después de 33.
-- Comparte la fuente y la regla VARCHAR2(250) con el correctivo 33.
PROMPT RISK_NAME_POSTCHECK_BEGIN
@@30_fuente_canonica_nombres_riesgos.sql POSTCHECK
PROMPT RISK_NAME_POSTCHECK_END
