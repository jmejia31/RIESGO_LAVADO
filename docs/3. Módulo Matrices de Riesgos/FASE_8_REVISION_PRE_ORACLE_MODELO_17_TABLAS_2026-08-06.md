# Fase 8 — Revisión final no Oracle previa a autorización física

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha:** 2026-08-06.
- **Repositorio:** `jmejia31/RIESGO_LAVADO`.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **PR de revisión:** #20 — abierto, en borrador y sin fusionar.
- **Oracle:** no ejecutado.
- **Script `05`:** no ejecutado.
- **Script `06`:** no ejecutado.
- **Resultado:** Fase 8 completada; preparación estática pre-Oracle aprobada.

---

## 1. Objetivo

Realizar la revisión final no Oracle de las Fases 1–7 antes de solicitar cualquier preparación o autorización física, verificando:

- alineación entre Backend, Frontend y DDL objetivo;
- inventario exacto de 17 tablas y 17 secuencias;
- seguridad del script de transición `06`;
- aislamiento respecto de instaladores automáticos;
- ausencia de credenciales Oracle codificadas;
- bloqueo por variables de entorno de la suite de integración;
- inexistencia de DDL dentro de las pruebas Oracle;
- compilación Release, pruebas Backend, Frontend y E2E;
- estado de ramas y del PR de revisión.

Esta fase no instala, migra ni certifica físicamente el esquema Oracle.

---

## 2. Hallazgo crítico detectado

La revisión encontró una incompatibilidad de alto impacto que no había quedado expuesta por las validaciones anteriores:

```text
database/00_EJECUCION_PRIMERA_VEZ.sql
  → incluía 19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql

database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql
  → incluía 19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql

19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql
  → incluía instalacion/01_create_rl_mr_estructura_dinamica.sql
  → incluía instalacion/02_create_rl_mr_restricciones_indices.sql
```

Los scripts `01` y `02` todavía construían y configuraban el modelo heredado de **34 tablas y 24 secuencias**, incluyendo objetos expresamente retirados del modelo objetivo.

Por tanto, una instalación o actualización automática podía intentar reconstruir el modelo equivocado. El validador general de base de datos también exigía esa ruta y, en consecuencia, no podía detectar el defecto.

### Riesgo evitado

De haberse ejecutado el flujo automático en un ambiente oficial, habría existido riesgo de:

- creación del modelo físico incorrecto;
- reintroducción de tablas y secuencias heredadas;
- incompatibilidad inmediata con Backend y Frontend;
- divergencia frente al manifiesto de 17 tablas;
- necesidad de retiro físico adicional;
- mayor exposición a fallos parciales por commits implícitos de DDL Oracle.

No existe evidencia de que estos scripts hayan sido ejecutados durante esta intervención.

---

## 3. Remediación aplicada

### 3.1 Exclusión de Matrices de los maestros automáticos

El paquete 19 fue eliminado de:

```text
database/00_EJECUCION_PRIMERA_VEZ.sql
database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql
```

Ambos maestros declaran ahora que Matrices de Riesgos permanece fuera del flujo automático hasta completar la transición y certificación Oracle.

### 3.2 Bloqueo del punto de entrada modular

El archivo:

```text
database/19_matrices_riesgos/00_APLICAR_MODULO_MATRICES_RIESGOS.sql
```

fue transformado en un punto de entrada de cuarentena que:

- no contiene `@@include`;
- no contiene DDL ni DML;
- valida `CURRENT_SCHEMA = RIESGO_LAVADO`;
- siempre bloquea la ejecución con un error controlado;
- remite a la transición manual del script `06`.

### 3.3 Retiro de instaladores heredados activos

Fueron eliminados de la ruta activa:

```text
database/19_matrices_riesgos/instalacion/01_create_rl_mr_estructura_dinamica.sql
database/19_matrices_riesgos/instalacion/02_create_rl_mr_restricciones_indices.sql
```

El historial permanece disponible en Git, pero ambos archivos están prohibidos en la instalación vigente y no deben restaurarse.

### 3.4 Documentación y manifiesto

Se actualizaron:

```text
database/19_matrices_riesgos/README.md
database/00_MANIFIESTO_SCRIPTS_APROBADOS.md
```

La documentación distingue ahora claramente entre:

- flujos automáticos aprobados;
- paquete 19 en cuarentena;
- script `06` manual y destructivo;
- scripts posteriores de registro, semillas y ajustes todavía no autorizados.

---

## 4. Controles automáticos incorporados

### 4.1 Validador general de base de datos

`tools/validate_database_scripts.ps1` fue reforzado para fallar si ocurre cualquiera de estos eventos:

- el paquete 19 vuelve a un maestro automático;
- el script `06` se agrega mediante `@@include`;
- se restauran los instaladores de 34 tablas;
- el punto de entrada bloqueado contiene includes, DDL o DML;
- el flujo seguro alcanza operaciones destructivas;
- el script `06` pierde sus controles de esquema, autorización u objetos institucionales.

### 4.2 Nueva puerta pre-Oracle

Se agregó:

```text
scripts/validation/validate_matrices_preoracle_readiness.ps1
```

Esta puerta valida estáticamente:

- existencia y aislamiento del script `06`;
- exactamente 17 sentencias `CREATE TABLE` objetivo;
- exactamente 17 sentencias `CREATE SEQUENCE` objetivo;
- ausencia de creación de tablas retiradas;
- bloqueo del punto de entrada modular;
- exclusión de ambos maestros;
- inexistencia de includes al script `06`;
- bloqueo de la suite por `RL_ORACLE_INTEGRATION_REQUIRED`;
- obtención segura de la conexión mediante variables de entorno o User Secrets;
- comprobación de `CURRENT_SCHEMA = RIESGO_LAVADO`;
- ausencia de DDL dentro de la suite Oracle;
- presencia de las cuatro pruebas de contrato no conectadas;
- ausencia de credenciales Oracle codificadas;
- ausencia de workflows auxiliares temporales.

### 4.3 Integración al Quality Gate

El workflow institucional ejecuta ahora, antes de compilar:

```powershell
./tools/validate_database_scripts.ps1
./scripts/validation/validate_matrices_preoracle_readiness.ps1
./scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1
./scripts/validation/validate_matrices_17_object_inventory.ps1
./scripts/validation/test_matrices_17_object_inventory.ps1
```

Cualquier reintroducción del flujo heredado o debilitamiento del bloqueo pre-Oracle detendrá el CI.

---

## 5. Revisión del script `06`

El archivo objetivo continúa siendo:

```text
database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql
```

La revisión estática confirmó:

- ejecución exclusivamente manual;
- advertencia expresa de respaldo y autorización;
- `WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK`;
- parámetro obligatorio `&1` con valor `EJECUTAR`;
- validación de `CURRENT_SCHEMA = RIESGO_LAVADO`;
- exigencia de `RL_USUARIOS`;
- retiro controlado de objetos `RL_MR_*` existentes;
- creación de exactamente 17 tablas;
- creación de exactamente 17 secuencias;
- uso de `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`;
- uso de `RL_MR_EVIDENCIAS_VINCULOS`;
- ausencia de creación de auditoría local, trazas y tablas puente específicas.

### Restricción crítica

El script `06` contiene operaciones destructivas y DDL con commits implícitos de Oracle. La aprobación estática de esta fase no autoriza su ejecución.

---

## 6. Revisión de la suite Oracle

La suite de certificación permanece deshabilitada por defecto y solo intenta abrir una conexión cuando existe:

```text
RL_ORACLE_INTEGRATION_REQUIRED=true
```

La cadena debe proceder de:

```text
ConnectionStrings__OracleDB
```

mediante variable de entorno o User Secrets.

La suite:

- valida primero el esquema `RIESGO_LAVADO`;
- compara las 17 tablas y 17 secuencias exactas;
- exige objetos institucionales reutilizados;
- exige ausencia de objetos retirados;
- valida índices y restricciones principales;
- prepara escenarios de contrato físico, commit y rollback;
- no ejecuta `CREATE`, `ALTER`, `DROP` ni `TRUNCATE`;
- utiliza registros aislados y limpieza controlada.

Las cuatro pruebas de contrato no conectadas verifican:

1. inventario 17/17 sin duplicados;
2. separación entre objetos activos y retirados;
3. presencia de los cuatro escenarios Oracle;
4. índices y restricciones sin duplicados.

---

## 7. Resultado de Quality Gates

### Ejecución técnica aprobada

```text
Run: 31114220642
Commit evaluado: 540a958fc1fb96018f6d88d9046c4d714130f5e8
Resultado: success
```

### Resultados exactos

| Control | Resultado |
|---|---:|
| Validador general de base de datos | **Correcto** |
| Preparación pre-Oracle | **Correcta** |
| Alineación dinámica Backend/Frontend/DDL | **Correcta** |
| Tablas activas objetivo | **17** |
| Secuencias activas objetivo | **17** |
| Pruebas negativas del inventario | **9 aprobadas** |
| Compilación Release | **0 advertencias / 0 errores** |
| Pruebas Backend | **222 aprobadas** |
| Pruebas Frontend | **123 aprobadas** |
| Recorridos E2E | **8 aprobados** |
| Cobertura Backend — líneas | **16.72 %** |
| Cobertura Backend — ramas | **17.18 %** |
| Cobertura Frontend — sentencias | **34.41 %** |
| Cobertura Frontend — ramas | **31.52 %** |
| Cobertura Frontend — funciones | **31.69 %** |
| Cobertura Frontend — líneas | **33.87 %** |

---

## 8. Estado de gobierno del repositorio

La revisión verificó:

- solo existen las ramas remotas `main` y `desarrollo`;
- todo el trabajo se publicó en `desarrollo`;
- `main` permanece en `727082c6fcf90f95ce6db5eadf5c4b152397d080`;
- el PR #20 permanece abierto, en borrador y sin fusionar;
- el PR no tiene comentarios pendientes registrados;
- no se habilitó auto-merge;
- no se ejecutó despliegue;
- no se publicaron credenciales.

---

## 9. Observaciones no resueltas por esta fase

`npm ci` continúa informando:

```text
13 vulnerabilidades
- 6 moderadas
- 6 altas
- 1 crítica
```

Este hallazgo corresponde al inventario global de dependencias Frontend y requiere una intervención separada de análisis de seguridad. No se aplicó `npm audit fix --force`, porque podría introducir cambios incompatibles o romper el sistema.

Este pendiente no invalida la cuarentena Oracle ni la preparación estática del modelo, pero debe resolverse antes de una liberación productiva.

---

## 10. Dictamen de Fase 8

### Aprobado

El repositorio queda **técnicamente apto para iniciar la preparación controlada de la Fase 9**, porque:

- el modelo objetivo está congelado en 17 tablas y 17 secuencias;
- la ruta automática heredada fue eliminada;
- el script `06` está aislado y protegido;
- la suite Oracle está preparada y deshabilitada por defecto;
- los validadores son vinculantes en CI;
- todas las puertas no Oracle aprobaron.

### No autorizado todavía

Este dictamen no autoriza:

- ejecutar Oracle;
- ejecutar el script `05`;
- ejecutar el script `06`;
- ejecutar `DROP TABLE`, `CREATE TABLE` o migraciones;
- preparar o modificar Producción;
- fusionar el PR #20;
- modificar `main`.

### Estado correcto

```text
Preparación estática pre-Oracle: APROBADA
Certificación física Oracle: PENDIENTE
Autorización de ejecución Oracle: NO OTORGADA
```

---

## 11. Siguiente fase

Corresponde la **Fase 9 — preparación del ambiente Oracle exclusivo y expediente de autorización**, todavía sin ejecutar el script `06`.

La Fase 9 deberá exigir, como mínimo:

1. identificación formal de la base exclusiva de pruebas;
2. confirmación de que no contiene datos productivos;
3. respaldo completo y restauración validada;
4. identificación del DBA y responsables presentes;
5. ventana de cambio aprobada;
6. inventario físico previo del esquema;
7. plan de ejecución paso a paso;
8. plan de contingencia y recuperación;
9. método seguro para suministrar la conexión;
10. checklist de evidencias sin secretos;
11. autorización expresa y separada antes de cualquier ejecución.

Hasta completar esos requisitos, el repositorio debe conservar exactamente la cuarentena establecida en esta fase.

---

## 12. Commits principales de la Fase 8

```text
7fc8c5eee0284dd5947dddc7368ec02d091571f8
fix(database): excluir matrices del maestro de actualizaciones

e2f00ccefe0ab7755481c55033a634667fa8d818
fix(database): excluir matrices del maestro de primera instalacion

080be8b56e032093d9c8f33fa72e2a46f40ed682
fix(matrices): bloquear punto de entrada automatico pre Oracle

5618aa46774c6bf9ddf0ce7c575891673318e70e
refactor(matrices): retirar instalador heredado de 34 tablas

9caa255c2810d54bd1d3289ed8ff628f8918960d
refactor(matrices): retirar indices del modelo heredado

75979f2903a35e5448a04647d77d75efedeb0e7e
docs(database): alinear paquete matrices con cuarentena pre Oracle

9d03e9a9332bc0e0c7283a87c720cf5ede1505f6
docs(database): actualizar manifiesto para cuarentena de matrices

de5d0746935c4083e8d2e620d98ebea7992bbcdb
test(database): bloquear reintroduccion automatica del modelo heredado

9b8de8ff15963093a4718085ea398afaf6edd90b
test(matrices): agregar puerta de preparacion pre Oracle

6903b1a84ab8527e8f487d3153107e63989d206e
ci(matrices): exigir cuarentena pre Oracle en Quality Gates

540a958fc1fb96018f6d88d9046c4d714130f5e8
test(matrices): alinear puerta pre Oracle con contratos reales
```
