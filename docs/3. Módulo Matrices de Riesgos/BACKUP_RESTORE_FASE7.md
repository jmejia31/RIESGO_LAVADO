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

`RESTORE_INSTRUCTIONS_COMPLETE=TRUE`. Como política general, un restore institucional debe preferir un objetivo aislado y privilegios DBA. Para esta Fase 7 se ejecutó posteriormente una prueba excepcional sobre `hpprod1/RIESGO_LAVADO` con autorización expresa y `PHYSICAL_ISOLATION=FALSE`; el resultado y la excepción están registrados abajo. No se afirma restore productivo.


## EJECUCION_REAL_2026_09_22_23

La prueba de continuidad finalmente se ejecutó sobre `hpprod1/RIESGO_LAVADO` con respaldo institucional disponible y autorización expresa del DBA/Javier.

> Excepción documentada: el objetivo **no era físicamente aislado**. `RIESGO_LAVADO` y `DNP_IHSS` comparten la misma base. La ejecución fue aceptada de forma excepcional y se limitó a `RL_MR_*`; no debe reutilizarse este antecedente como autorización genérica para ejecutar transiciones destructivas sobre bases compartidas.

Resultado aportado por la ejecución:

```text
RL_MR_TABLES=25
RL_MR_SEQUENCES=25
TOTAL_RL_MR_ROWS=465
RL_MR_RIESGOS=59
RL_MR_EVALUACIONES_RIESGO=59
RL_MR_PROYECCIONES_EVALUACION=59
RL_MR_SENALES_ALERTA=148
INVALID_OBJECTS=0
DISABLED_CONSTRAINTS=0
ORPHAN_USER_REFERENCES=0
FAMILY_22_DEFAULT=1
V1_61=1
V2_63=1
RESTORE_DATOS_RL_MR_V2=PASS
PREFLIGHT=PASS
POSTFLIGHT=PASS
```

La copia institucional garantizaba 25 tablas y datos; la validación final comprobó además las 25 secuencias requeridas. `RESTORE_VALIDATION=PASS_WITH_NON_ISOLATED_ENVIRONMENT_EXCEPTION`.
