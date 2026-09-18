# Backup y restore de release

## PRE_DEPLOY_BACKUP_CHECKLIST

- [ ] Registrar SHA/manifest y versiones V1/V2.
- [ ] Ejecutar preflight read-only y guardar salida sin secretos.
- [ ] Respaldar schema `RIESGO_LAVADO` con mecanismo institucional/Data Pump.
- [ ] Incluir metadatos, datos, grants autorizados y logs del export.
- [ ] Respaldar volumen de evidencias y configuración no secreta.
- [ ] Calcular SHA-256 de archivos producidos.
- [ ] Confirmar que el destino de restore es aislado.

## BACKUP_VERIFICATION

Verificar existencia, tamaño razonable, checksum, log de Data Pump, fecha/hora, schema y permisos. No registrar contraseña, wallet ni connection string completa.

## RESTORE_ORDER

1. Preparar schema/objetivo aislado.
2. Importar metadatos y datos conforme a procedimiento DBA.
3. Restaurar evidencias en la ruta configurada con permisos mínimos.
4. Inyectar configuración/secretos desde el secret store.
5. Ejecutar `01_preflight_release.sql`, `04_postflight_release.sql` y smoke.
6. Comparar V1 hash, estado V2, conteos 59, bindings y auditoría.

## RESTORE_VALIDATION

`RESTORE_INSTRUCTIONS_COMPLETE=TRUE`. La validación destructiva institucional no se ejecuta sobre `hpprod1`; requiere un ambiente aislado y privilegios DBA. Hasta disponer de ese objetivo, el paquete es reproducible y listo para la prueba institucional de continuidad, sin afirmar restore productivo.
