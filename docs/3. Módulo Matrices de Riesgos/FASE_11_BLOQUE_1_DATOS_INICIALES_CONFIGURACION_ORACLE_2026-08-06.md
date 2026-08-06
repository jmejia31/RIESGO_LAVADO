# Fase 11 — Bloque 1: datos iniciales y configuración real sobre Oracle

## Módulo Matrices de Riesgos

- **Fecha:** 2026-08-06.
- **Repositorio:** `jmejia31/RIESGO_LAVADO`.
- **Rama:** `desarrollo`.
- **HEAD de entrada:** `33649a078c2290e37314df9cdc00ec27e424d32b`.
- **Quality Gate de entrada:** Run `31128384102` — `success`.
- **Estado del desarrollo en repositorio:** IMPLEMENTADO.
- **Estado de ejecución física Oracle:** PENDIENTE DE EJECUCIÓN LOCAL CONTROLADA.
- **Producción:** NO MODIFICADA.
- **Respaldos `B10_*`:** NO MODIFICADOS.

## 1. Alcance implementado

El Bloque 1 incorpora una configuración inicial idempotente para:

1. `RL_MR_FAMILIAS_FORMULARIO`;
2. `RL_MR_VERSIONES_FORMULARIO`;
3. `RL_MR_CATALOGOS`;
4. `RL_MR_ELEMENTOS_CATALOGO`;
5. `RL_MR_REGLAS_CALCULO`.

La familia oficial es `MATRIZ_RIESGOS_LAFT`. La versión inicial se identifica como
`MATRIZ_RIESGOS_LAFT_V1`, versión `1`, estado `PUBLISHED` y vigencia activa.

## 2. Contrato dinámico oficial

La definición se conserva en:

```text
database/19_matrices_riesgos/fase11/formulario_matriz_riesgos_laft_v1.json
```

Contiene:

- 4 secciones;
- 12 campos dinámicos;
- 4 catálogos;
- 18 elementos de catálogo;
- 1 regla de cálculo;
- algoritmo `MATRICES_VRI_ADITIVO_1_9`;
- hash SHA-256 `f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90`.

Los campos responden al contrato realmente consumido por Backend y Frontend:
`area_principal`, `dueno_riesgo`, `frecuencia_inherente`, `impacto_inherente`,
`nivel_inherente`, `controles_preventivo`, `controles_detectivo`,
`controles_correctivo`, `frecuencia_residual`, `impacto_residual`,
`nivel_residual` y `respuesta_riesgo`.

Cada campo conserva simultáneamente `id` y `clave` con el mismo valor para
mantener compatibilidad con el validador Backend y el renderizador Angular.

## 3. Script idempotente

Archivo:

```text
database/19_matrices_riesgos/fase11/01_semillas_datos_iniciales_modelo_17_tablas.sql
```

Controles:

- exige `CURRENT_SCHEMA = RIESGO_LAVADO`;
- selecciona un `USR_ID` institucional con `USR_ACTIVO = 1`;
- usa únicamente las cinco secuencias oficiales;
- utiliza `MERGE` y códigos funcionales;
- conserva registros ajenos;
- no contiene `DROP`, `TRUNCATE` ni `DELETE`;
- no toca `B10_*`;
- bloquea una sustitución automática si ya existe otra versión vigente;
- compara el CLOB y el hash cuando la versión 1 ya existe;
- confirma exactamente una versión publicada y vigente;
- confirma catálogos, elementos, regla, integridad referencial y ausencia de duplicados;
- ejecuta `COMMIT` solo después de validar;
- ejecuta `ROLLBACK` ante cualquier error.

Resultado esperado:

```text
SEMILLAS FASE 11 BLOQUE 1: APLICADAS Y VALIDADAS
```

## 4. Validación de solo lectura

Archivo:

```text
database/19_matrices_riesgos/fase11/02_validar_semillas_bloque1_solo_lectura.sql
```

Comprueba familia, versión, hash, usuario activo, catálogos, 18 elementos,
regla activa, huérfanos, duplicados, objetos inválidos y restricciones
inactivas.

## 5. Pruebas agregadas

- Pruebas de contrato sin Oracle:
  `MatricesRiesgosPhase11Block1ContractTests.cs`.
- Certificación de solo lectura contra Oracle, habilitada únicamente con
  `RL_ORACLE_INTEGRATION_REQUIRED=true` y conexión segura:
  `MatricesRiesgosPhase11Block1OracleIntegrationTests.cs`.
- Validador estático:
  `scripts/validation/validate_matrices_phase11_block1.ps1`.
- Quality Gate actualizado para ejecutar el nuevo validador.

## 6. Ejecución física pendiente

La ejecución física no puede realizarse desde el conector GitHub porque no
tiene acceso de red a la instancia Oracle local. Debe ejecutarse desde la
estación autorizada sin exponer credenciales:

```text
@database/19_matrices_riesgos/fase11/01_semillas_datos_iniciales_modelo_17_tablas.sql
@database/19_matrices_riesgos/fase11/01_semillas_datos_iniciales_modelo_17_tablas.sql
@database/19_matrices_riesgos/fase11/02_validar_semillas_bloque1_solo_lectura.sql
```

La segunda ejecución demuestra idempotencia. Después debe activarse la suite
Oracle con las variables seguras institucionales.

## 7. Criterio de cierre

El Bloque 1 solo podrá marcarse como `COMPLETADO` cuando existan evidencias de:

- primera ejecución exitosa;
- segunda ejecución exitosa sin duplicados;
- validación SQL correcta;
- prueba Backend ↔ Oracle correcta;
- endpoint `formulario/version-vigente` correcto;
- endpoint `metodologia/vigente` correcto;
- Quality Gate del HEAD final en `success`.

## 8. Seguridad pendiente independiente

`npm ci` mantiene 13 vulnerabilidades: 6 moderadas, 6 altas y 1 crítica.
No se autoriza `npm audit fix --force`; la remediación corresponde a una fase
de seguridad separada antes de Producción.
