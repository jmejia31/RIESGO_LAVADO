-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Paquete activo: modulo Matrices de Riesgos
-- Uso: instalacion inicial y actualizacion segura.
-- Reglas: scripts idempotentes, sin DROP/TRUNCATE/DELETE de tablas o datos.
-- Importante: ejecutar con respaldo, ambiente de pruebas y aprobacion DBA.
-- ============================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

PROMPT ============================================================
PROMPT APLICANDO MODULO MATRICES DE RIESGOS
PROMPT ============================================================

@@01_create_rl_mr_estructura.sql
@@02_register_modulo_matrices_riesgos.sql
@@03_seed_metodologia_matrices_riesgos.sql
@@04_fix_encoding_textos_oracle.sql
@@05_align_estado_en_evaluacion.sql

PROMPT Modulo Matrices de Riesgos aplicado correctamente.
