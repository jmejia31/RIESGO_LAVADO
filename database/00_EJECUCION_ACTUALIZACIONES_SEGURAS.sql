-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Script maestro: actualizaciones seguras
-- Uso: Ambientes existentes con informacion que debe conservarse.
-- Importante: No contiene scripts de reseteo ni borrado masivo.
-- Proceso DBA: ejecutar solo despues de respaldo validado, ventana aprobada
-- y revision del orden de scripts. Cada archivo incluido debe ser idempotente
-- o estar documentado como correctivo seguro para ambientes existentes.
--
-- MATRICES DE RIESGOS:
-- El paquete 19 permanece excluido de este flujo durante la preparacion y
-- certificacion Oracle del modelo reducido de 17 tablas. Su transicion fisica
-- solo puede realizarse manualmente mediante el script 06, con respaldo y
-- autorizacion expresa. No agregarlo a este maestro antes del cierre Oracle.
-- ============================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

PROMPT ============================================================
PROMPT EJECUCION DE ACTUALIZACIONES SEGURAS
PROMPT No se ejecutan DROP TABLE, TRUNCATE ni DELETE masivo.
PROMPT Valide respaldo y ventana de cambio antes de continuar.
PROMPT Matrices de Riesgos permanece fuera del flujo automatico.
PROMPT ============================================================

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
PROMPT Actualizaciones seguras finalizadas.
PROMPT ============================================================

EXIT
