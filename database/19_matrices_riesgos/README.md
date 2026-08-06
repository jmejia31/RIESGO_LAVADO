# Paquete Oracle: Matrices de Riesgos

## Estado vigente

El paquete permanece en **cuarentena pre-Oracle** mientras se completa la preparación, ejecución controlada y certificación física del modelo reducido de **17 tablas y 17 secuencias**.

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
./scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1
./scripts/validation/validate_matrices_17_object_inventory.ps1
./scripts/validation/test_matrices_17_object_inventory.ps1
./scripts/validation/validate_matrices_preoracle_readiness.ps1
```

Estas validaciones son exclusivamente estáticas y no equivalen a certificación física Oracle.

## Restricciones

- No ejecutar `00_APLICAR_MODULO_MATRICES_RIESGOS.sql` mientras permanezca bloqueado.
- No ejecutar el script `05` ni el script `06` sin autorización expresa.
- No incorporar `06_reconstruir_modelo_17_tablas.sql` a un flujo automático.
- No publicar cadenas de conexión, contraseñas ni secretos.
- No declarar certificado el modelo hasta completar las pruebas Oracle reales.
