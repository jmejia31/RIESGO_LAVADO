-- FASE 3.1.5 - recovery preparado, no ejecutar automáticamente.
-- La recuperación requiere autorización explícita separada.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
WHENEVER OSERROR EXIT FAILURE
SET ECHO OFF
SET VERIFY OFF
SET TERMOUT ON

PROMPT RECOVERY_FORMULA_INSTITUCIONAL_315_PREPARED_ONLY
PROMPT NO SE EJECUTA SIN AUTORIZACION EXPLICITA
PROMPT
PROMPT El paquete de recuperación debe eliminar únicamente, en orden hijo-padre,
PROMPT las versiones FOV/FUA/PAV y maestros con metadata de esta carga.
PROMPT No se incluye DML ejecutable por defecto para evitar borrar datos
PROMPT institucionales coincidentes. Se conserva este archivo como procedimiento
PROMPT documentado para una eventual recuperación autorizada.
EXIT SUCCESS
