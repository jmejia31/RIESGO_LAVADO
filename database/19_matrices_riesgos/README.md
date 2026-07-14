# Paquete Oracle: Matrices de Riesgos

Este directorio es una unidad modular activa de base de datos. Su unico punto de entrada aprobado es `00_APLICAR_MODULO_MATRICES_RIESGOS.sql`, invocado por los maestros de primera instalacion y actualizacion segura mediante `@@`.

## Orden interno

1. `01_create_rl_mr_estructura.sql`: crea de forma idempotente tablas, secuencias, indices y restricciones `RL_MR_*`.
2. `02_register_modulo_matrices_riesgos.sql`: registra el modulo 10 y sus permisos iniciales.
3. `03_seed_metodologia_matrices_riesgos.sql`: incorpora la metodologia, factores, variables, criterios y escalas aprobadas.
4. `04_fix_encoding_textos_oracle.sql`: normaliza comentarios y textos descriptivos del modulo.
5. `05_align_estado_en_evaluacion.sql`: alinea la restriccion de estados despues de validar los datos existentes.

## Reglas de mantenimiento

- No ejecutar archivos internos de forma aislada en ambientes oficiales.
- Todo nuevo script se agrega al final del punto de entrada y debe ser idempotente o documentar claramente su condicion de reejecucion.
- No incorporar `DROP TABLE`, `TRUNCATE` ni `DELETE FROM` al flujo de actualizacion segura.
- No modificar IDs de modulo, nombres de objetos o semillas aprobadas sin coordinacion funcional y DBA.
- Mantener `WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK` en el punto de entrada.
- Cerrar la ejecucion general con `17_validate_module_ids.sql`, que pertenece a la raiz de `database` y es de solo lectura.

Las evidencias de ejecucion y salidas de SQL*Plus pertenecen a `docs`; no deben almacenarse dentro de este paquete.
