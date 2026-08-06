# Paquete Oracle: Matrices de Riesgos

## Estado vigente

El paquete permanece en **cuarentena pre-Oracle** mientras se completa la preparación, ejecución controlada y certificación física del modelo reducido de **17 tablas y 17 secuencias**.

La Fase 9 dejó preparado el expediente técnico, el inventario de solo lectura y el formato de autorización. Esto no equivale a identificar un ambiente real, validar un respaldo ni autorizar la transición.

El archivo `00_APLICAR_MODULO_MATRICES_RIESGOS.sql` está bloqueado de forma intencional. No crea, altera ni elimina objetos y no forma parte de los maestros:

- `database/00_EJECUCION_PRIMERA_VEZ.sql`
- `database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql`

Esta exclusión evita que una instalación o actualización automática reconstruya accidentalmente el modelo heredado.

## Modelo objetivo

La única definición física objetivo se encuentra en:

```text
transicion/06_reconstruir_modelo_17_tablas.sql
```

El script `06`:

- es manual y destructivo;
- elimina objetos `RL_MR_*` de prueba antes de reconstruirlos;
- exige `CURRENT_SCHEMA = RIESGO_LAVADO`;
- exige el parámetro literal `EJECUTAR`;
- exige que exista `RL_USUARIOS`;
- no está incluido mediante `@@` en ningún maestro ni punto de entrada automático;
- no debe ejecutarse sin respaldo validado, base exclusiva de pruebas, ventana aprobada y autorización expresa de Javier Mejía y del DBA.

## Preflight Oracle de solo lectura

La Fase 9 incorporó:

```text
transicion/07_preflight_inventario_oracle_solo_lectura.sql
```

Este archivo:

- no contiene DDL ni DML;
- no incluye ni ejecuta el script `06`;
- valida `CURRENT_SCHEMA = RIESGO_LAVADO`;
- exige `RL_USUARIOS`, `RL_AUDITORIA` y `SEQ_RL_AUDITORIA`;
- identifica base, host, usuario y fecha del servidor;
- lista tablas y secuencias `RL_MR_*`;
- cuenta registros por tabla;
- reporta objetos inválidos y restricciones deshabilitadas;
- debe conservar su salida como evidencia sin secretos.

Su ejecución pertenece al diligenciamiento operativo del expediente. No fue ejecutado durante la Fase 9.

## Expediente y autorización

Documentos preparados:

```text
docs/3. Módulo Matrices de Riesgos/FASE_9_EXPEDIENTE_AUTORIZACION_ORACLE_MODELO_17_TABLAS_2026-08-06.md
docs/3. Módulo Matrices de Riesgos/FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md
```

El formato permanece en estado:

```text
AUTORIZACION FASE 10: NO OTORGADA
ORACLE EJECUTADO: NO
SCRIPT 06 EJECUTADO: NO
```

La existencia de estos archivos no constituye autorización. El ambiente, responsables, respaldo, restauración, ventana, hashes y firmas deben sustentarse con evidencia real.

## Scripts conservados para después de la transición

Los siguientes archivos permanecen preparados, pero no deben ejecutarse todavía:

1. `02_register_modulo_matrices_riesgos.sql`: registra el módulo y permisos iniciales.
2. `instalacion/03_seed_catalogos_iniciales.sql`: carga catálogos y reglas de cálculo de forma idempotente.
3. `instalacion/04_config_json_inicial_formulario.sql`: prepara la familia y versión inicial del formulario dinámico.
4. `instalacion/05_ajustes_dashboard_seguridad_reportes.sql`: aplica unicidad e índices adicionales de dashboard y reportes.

Cada ejecución posterior deberá realizarse en el orden aprobado por la fase Oracle correspondiente y con sus controles de autorización. La presencia de estos archivos no autoriza su ejecución.

## Scripts heredados retirados

Los instaladores activos que construían el modelo anterior de 34 tablas fueron eliminados de `instalacion`:

```text
01_create_rl_mr_estructura_dinamica.sql
02_create_rl_mr_restricciones_indices.sql
```

Su historial permanece disponible en Git; no deben restaurarse ni volver a incorporarse a los maestros.

## Validaciones obligatorias

Antes de solicitar autorización Oracle deben aprobar:

```powershell
./tools/validate_database_scripts.ps1
./scripts/validation/validate_matrices_preoracle_readiness.ps1
./scripts/validation/validate_matrices_phase9_oracle_dossier.ps1
./scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1
./scripts/validation/validate_matrices_17_object_inventory.ps1
./scripts/validation/test_matrices_17_object_inventory.ps1
```

La puerta de Fase 9 comprueba que:

- el preflight continúe siendo de solo lectura;
- el expediente conserve todas las secciones obligatorias;
- la autorización permanezca en `NO OTORGADA` mientras no exista evidencia externa;
- no se codifiquen cadenas Oracle o contraseñas;
- el script `06` conserve sus bloqueos;
- el Quality Gate ejecute esta validación.

Estas validaciones son exclusivamente estáticas y no equivalen a certificación física Oracle.

## Restricciones

- No ejecutar `00_APLICAR_MODULO_MATRICES_RIESGOS.sql` mientras permanezca bloqueado.
- No ejecutar el script `05` ni el script `06` sin autorización expresa.
- No incorporar `06_reconstruir_modelo_17_tablas.sql` a un flujo automático.
- No declarar identificado un ambiente sin evidencia del DBA.
- No declarar restaurable un respaldo sin prueba real.
- No publicar cadenas de conexión, contraseñas ni secretos.
- No declarar certificado el modelo hasta completar las pruebas Oracle reales.
