-- DBA proposal only: read-only diagnostics identified the access-path gap.
-- DO NOT EXECUTE WITHOUT DBA APPROVAL.
-- ORACLE_DDL_EXECUTED=0
-- ORACLE_DML_EXECUTED=0
-- Validate object owner, existing prefixes, storage, tablespace and plan after approval.

-- P0 / Naturales and Empleados: filtered and grouped by TIPO_CALIFICACION_ID,
-- joined/grouped by DNI and grouped by LISTA_CONCIDENCIA.
-- Evidence: REPORTE_COINCIDENCIAS has only its primary-key index; the explain
-- plan captured 2026-09-11 shows TABLE ACCESS FULL on REPORTE_COINCIDENCIAS.
-- Expected benefit: permit a selective access path for the filtered coincidence
-- set and reduce the rows entering the group/join. PENDING_DBA_VALIDATION.
-- CREATE INDEX DNP_IHSS.IX_RCOINC_MON_NAT_DNI
--     ON DNP_IHSS.REPORTE_COINCIDENCIAS
--        (TIPO_CALIFICACION_ID, DNI, LISTA_CONCIDENCIA, FECHA_CALIFICO);
-- Rollback, only after dependency/usage review:
-- DROP INDEX DNP_IHSS.IX_RCOINC_MON_NAT_DNI;

-- P0 / Juridicas: filtered by TIPO_CALIFICACION_ID and joined by NUMERO_PATRONO.
-- Evidence: the explain plan captured 2026-09-11 shows TABLE ACCESS FULL on
-- REPORTE_COINCIDENCIAS and HASH JOIN with the 94,588-row PATRONOS source.
-- Expected benefit: reduce the coincidence driver before the enterprise join.
-- PENDING_DBA_VALIDATION.
-- CREATE INDEX DNP_IHSS.IX_RCOINC_MON_PATRONO
--     ON DNP_IHSS.REPORTE_COINCIDENCIAS
--        (TIPO_CALIFICACION_ID, NUMERO_PATRONO, FECHA_CALIFICO);
-- Rollback, only after dependency/usage review:
-- DROP INDEX DNP_IHSS.IX_RCOINC_MON_PATRONO;

-- P1 / Join support for the natural-person view expansion. No equivalent
-- non-PK index was returned by ALL_IND_COLUMNS for these columns.
-- Expected benefit: allow nested-loop/semi-join access when the reduced DNI
-- set is the driver. PENDING_DBA_VALIDATION.
-- CREATE INDEX DNP_IHSS.IX_SOCIOS_MON_IDENTIFICACION
--     ON DNP_IHSS.SOCIOS (NUMERO_IDENTIFICACION);
-- Rollback:
-- DROP INDEX DNP_IHSS.IX_SOCIOS_MON_IDENTIFICACION;

-- P1 / Same view expansion for representatives.
-- Expected benefit: allow keyed access from the reduced DNI set. PENDING_DBA_VALIDATION.
-- CREATE INDEX DNP_IHSS.IX_REPRESENTANTES_MON_IDENTIFICACION
--     ON DNP_IHSS.REPRESENTANTES (NUMERO_IDENTIFICACION);
-- Rollback:
-- DROP INDEX DNP_IHSS.IX_REPRESENTANTES_MON_IDENTIFICACION;

-- P2 / Future growth only. RL_LISTA_POSITIVOS currently has 6 rows and the
-- existing unique prefix is (LSP_TIPO_DOCUMENTO_ID,LSP_NO_DOCUMENTO); do not
-- create this index solely for the current dataset.
-- CREATE INDEX RIESGO_LAVADO.IX_RL_LSP_MON_TIPO_EST_DOC
--     ON RIESGO_LAVADO.RL_LISTA_POSITIVOS
--        (LSP_TIPO_POSITIVO_ID, LSP_ESTADO_REGISTRO, LSP_NO_DOCUMENTO);
-- Rollback:
-- DROP INDEX RIESGO_LAVADO.IX_RL_LSP_MON_TIPO_EST_DOC;

-- Statistics prerequisite, not executed here:
-- DBMS_STATS_REQUIRED=TRUE
-- Review/gather statistics under the DBA change window for the DNP base
-- objects and the remote MMATAMOROS.PLANILLAS object before comparing plans.
