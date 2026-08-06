-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script maestro: instalacion primera vez
-- Uso: Ambiente nuevo o base vacia aprobada por DBA.
-- Importante: No ejecutar sobre produccion con informacion real.
-- Proceso DBA: usar exclusivamente cuando la base no contiene informacion
-- productiva. Este flujo puede crear estructura y datos iniciales, por eso
-- no debe mezclarse con actualizaciones seguras de ambientes existentes.
--
-- MATRICES DE RIESGOS:
-- El paquete 19 permanece excluido durante la preparacion y certificacion
-- Oracle del modelo reducido de 17 tablas. No debe instalarse automaticamente
-- hasta completar la transicion manual, la certificacion fisica y la
-- autorizacion expresa correspondiente.
-- ============================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

PROMPT ============================================================
PROMPT EJECUCION PRIMERA VEZ - AMBIENTE NUEVO
PROMPT Valide respaldo y aprobacion DBA antes de continuar.
PROMPT Matrices de Riesgos permanece fuera del flujo automatico.
PROMPT ============================================================

@@01_create_tables.sql
@@02_seed_data.sql
@@03_create_modules_table.sql
@@04_alter_config_sistema.sql
@@05_register_monitoreo_listas.sql
@@06_alter_usuarios_change_pass.sql
@@08_register_bitacora.sql
@@09_create_detalle_evidencia.sql
@@10_register_tipo_listas_module.sql
@@11_register_cargar_listas_module.sql
@@12_register_coincidencias_patrono_module.sql
@@13_create_calificaciones_coincidencias.sql
@@14_register_coincidencias_empleado_module.sql
@@15_update_detalle_evidencia_soft_delete.sql
@@16_alter_lista_positivos_origen_registro.sql
@@18_add_missing_comments.sql
@@17_validate_module_ids.sql

PROMPT ============================================================
PROMPT Instalacion primera vez finalizada.
PROMPT ============================================================

EXIT
