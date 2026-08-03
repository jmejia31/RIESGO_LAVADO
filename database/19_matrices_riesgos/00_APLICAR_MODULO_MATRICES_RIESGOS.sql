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

@@instalacion/01_create_rl_mr_estructura_dinamica.sql
@@instalacion/02_create_rl_mr_restricciones_indices.sql
@@02_register_modulo_matrices_riesgos.sql
@@instalacion/03_seed_catalogos_iniciales.sql
@@instalacion/04_config_json_inicial_formulario.sql
@@instalacion/05_ajustes_dashboard_seguridad_reportes.sql

PROMPT Modulo Matrices de Riesgos aplicado correctamente.
