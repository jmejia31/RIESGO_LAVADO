# Fase 11 — Bloques 2 a 6

## Implementación funcional y paquete de certificación Oracle

- **Repositorio:** `jmejia31/RIESGO_LAVADO`.
- **Rama exclusiva:** `desarrollo`.
- **Producción:** no modificada.
- **`main`:** no modificar ni fusionar sin autorización expresa.
- **PR #20:** debe permanecer abierto y en borrador.
- **Respaldos `B10_*`:** no modificar.
- **Scripts 05/06 de transición:** no ejecutar.

> Este documento distingue deliberadamente entre implementación en repositorio y certificación física Oracle. Ningún resultado Oracle se declara ejecutado hasta obtener evidencia real desde la estación autorizada.

## Bloque 2 — Gestión de Riesgos y Evaluaciones

Se incorporó mantenimiento operativo de `RL_MR_RIESGOS`, eliminando la dependencia de ingresar manualmente un ID de riesgo desconocido desde la interfaz.

### Backend

- `MatricesRiesgosGestionRepository`: listar, consultar, crear y actualizar riesgos.
- `MatricesRiesgosGestionService`: validaciones de código, nombre y descripción.
- `MatricesRiesgosGestionController`: API protegida por autenticación y módulo 10.
- Creación/actualización transaccional con `RL_AUDITORIA` en la misma transacción.
- No se implementa eliminación física de riesgos.

### Frontend

- Contratos tipados de riesgo.
- Servicio Angular para consultar/crear/actualizar riesgos.
- Captura dinámica utiliza selector de riesgos reales en vez de ID manual.

### Validación Oracle

```text
database/19_matrices_riesgos/fase11/03_validar_gestion_riesgos_bloque2_solo_lectura.sql
```

Valida referencias, JSON de evaluación, proyección 1:1, huérfanos, duplicados y dominio VRI/VRR 1–9.

## Bloque 3 — Flujos de Evaluación

La máquina de estados existente se conserva como contrato oficial:

```text
BORRADOR
EN_REVISION
OBSERVADA
APROBADA
RECHAZADA
CERRADA
```

El historial se mantiene en `RL_MR_FLUJOS_EVALUACION` y el último estado debe coincidir con `RL_MR_PROYECCIONES_EVALUACION.PROY_ESTADO_EVALUACION`.

Validación:

```text
database/19_matrices_riesgos/fase11/04_validar_flujos_bloque3_solo_lectura.sql
```

## Bloque 4 — Mitigación

Se implementaron operaciones reales sobre:

- `RL_MR_CONTROLES_RIESGO`;
- `RL_MR_EVALUACIONES_CONTROL`;
- `RL_MR_PLANES`;
- `RL_MR_ACTIVIDADES`.

La infraestructura de evidencias ya certificada se conserva sobre:

- `RL_MR_EVIDENCIAS`;
- `RL_MR_EVIDENCIAS_VINCULOS`.

### Contratos funcionales

- Controles: PREVENTIVO, DETECTIVO, CORRECTIVO.
- Automatización: MANUAL, SEMIAUTOMATICO, AUTOMATICO.
- Efectividad: 0–100.
- Avance de planes/actividades: 0–100.
- Presupuesto: no negativo.
- Fecha fin: no anterior a fecha inicio.

Todas las mutaciones agregadas trabajan con transacción Oracle y auditoría institucional compartida.

Validación:

```text
database/19_matrices_riesgos/fase11/05_validar_mitigacion_bloque4_solo_lectura.sql
```

## Bloque 5 — Alertas, Automonitoreo y Reportería

Se implementaron operaciones reales sobre:

- `RL_MR_SENALES_ALERTA`;
- `RL_MR_AUTOMONITOREO`.

Se agregó resumen operativo con:

- riesgos activos;
- evaluaciones activas;
- evaluaciones aprobadas;
- riesgos residuales alto/crítico;
- alertas activas;
- planes abiertos;
- actividades vencidas;
- automonitoreos de los últimos 30 días.

### Reportería

Endpoints:

```text
GET /api/matrices-riesgos/reportes/consolidado.xlsx
GET /api/matrices-riesgos/reportes/consolidado.pdf
```

El XLSX se genera mediante OpenXML mínimo empaquetado con componentes estándar de .NET. El PDF se genera en formato PDF 1.4 paginado. No se agregaron dependencias de terceros para esta capacidad.

El Frontend ofrece las descargas Excel y PDF desde el consolidado.

Validación:

```text
database/19_matrices_riesgos/fase11/06_validar_alertas_automonitoreo_bloque5_solo_lectura.sql
```

## Bloque 6 — Auditoría, transacciones y fallos

La persistencia agregada en Bloques 2, 4 y 5 usa `BeginTransaction()` y la sobrecarga institucional de `IAuditoriaRepository.RegistrarAsync(connection, transaction, ...)`, garantizando que dato funcional y auditoría participan en la misma transacción.

Prueba Oracle controlada:

```text
database/19_matrices_riesgos/fase11/07_validar_auditoria_transacciones_bloque6.sql
```

La prueba:

1. valida `RL_AUDITORIA` y `SEQ_RL_AUDITORIA`;
2. inserta un riesgo efímero y su auditoría dentro de la misma transacción;
3. provoca un fallo controlado;
4. ejecuta `ROLLBACK`;
5. demuestra que no persistió ni el riesgo ni la auditoría;
6. no usa `COMMIT`, `DELETE`, `DROP` ni `TRUNCATE`.

## Quality Gate

El workflow ejecuta adicionalmente:

```text
scripts/validation/validate_matrices_phase11_block1.ps1
scripts/validation/validate_matrices_phase11_remaining_blocks.ps1
```

El segundo validador bloquea:

- DML/DDL en validadores marcados como solo lectura;
- `COMMIT`, `DELETE`, `DROP` o `TRUNCATE` en la prueba de rollback;
- referencias a `B10_*`;
- referencias a scripts 05/06 prohibidos;
- falta de registros DI;
- repositorios de escritura sin transacción/auditoría compartida;
- ausencia de generación XLSX/PDF.

## Secuencia de certificación física local

Una vez sincronizado `desarrollo` al HEAD final certificado por GitHub Actions:

```text
@database/19_matrices_riesgos/fase11/01_semillas_datos_iniciales_modelo_17_tablas.sql
@database/19_matrices_riesgos/fase11/01_semillas_datos_iniciales_modelo_17_tablas.sql
@database/19_matrices_riesgos/fase11/02_validar_semillas_bloque1_solo_lectura.sql
@database/19_matrices_riesgos/fase11/03_validar_gestion_riesgos_bloque2_solo_lectura.sql
@database/19_matrices_riesgos/fase11/04_validar_flujos_bloque3_solo_lectura.sql
@database/19_matrices_riesgos/fase11/05_validar_mitigacion_bloque4_solo_lectura.sql
@database/19_matrices_riesgos/fase11/06_validar_alertas_automonitoreo_bloque5_solo_lectura.sql
@database/19_matrices_riesgos/fase11/07_validar_auditoria_transacciones_bloque6.sql
```

La ejecución debe realizarse únicamente en Oracle Desarrollo con mecanismo de credencial seguro y spools fuera del repositorio. No copiar contraseñas, cadenas completas, TNS sensibles, IP internas ni hostnames a GitHub o al chat.

## Criterio de cierre

**Implementación de repositorio** y **certificación física Oracle** son dos hitos distintos.

La Fase 11 puede marcarse como físicamente completada únicamente cuando existan evidencias reales de:

- B1 ejecutado dos veces con idempotencia;
- B2 con ciclo Riesgo → Evaluación → Proyección correcto;
- B3 con transiciones y sincronización de estado correctas;
- B4 con controles/evaluaciones/planes/actividades/evidencias correctos;
- B5 con alertas/automonitoreo/resumen/Excel/PDF correctos;
- B6 con rollback atómico dato + auditoría correcto;
- suites Oracle y endpoints reales correctos;
- Quality Gate remoto exitoso sobre el mismo HEAD.

## Seguridad pendiente independiente

`npm ci` mantiene 13 vulnerabilidades reportadas previamente: 6 moderadas, 6 altas y 1 crítica. No se autoriza `npm audit fix --force`; la remediación debe ejecutarse en una fase de seguridad separada antes de Producción.
