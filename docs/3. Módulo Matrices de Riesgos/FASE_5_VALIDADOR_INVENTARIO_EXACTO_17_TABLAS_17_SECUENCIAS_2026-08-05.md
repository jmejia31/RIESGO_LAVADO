# Fase 5 — Validador exclusivo del inventario exacto

## Módulo Matrices de Riesgos — 17 tablas y 17 secuencias

- **Fecha:** 2026-08-05.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** completada y validada sin ejecución Oracle.

---

## 1. Objetivo

Establecer una validación independiente y obligatoria que garantice que el DDL de transición del Módulo Matrices de Riesgos contenga exactamente:

```text
17 CREATE TABLE RL_MR_*
17 CREATE SEQUENCE SEQ_RL_MR_*
```

El validador debe fallar ante:

- una tabla ausente;
- una tabla adicional;
- una tabla duplicada;
- una secuencia ausente;
- una secuencia adicional;
- una secuencia duplicada;
- reintroducción activa de objetos heredados;
- nombres heredados fuera de la sección autorizada de retiro;
- un manifiesto que no declare exactamente 17 tablas y 17 secuencias.

---

## 2. Fuente única de verdad

Se agregó:

```text
database/19_matrices_riesgos/transicion/modelo_17_objetos.json
```

El manifiesto contiene cuatro inventarios cerrados:

1. tablas activas aprobadas;
2. secuencias activas aprobadas;
3. tablas heredadas que únicamente pueden aparecer en el retiro controlado;
4. secuencias heredadas prohibidas en la creación activa.

### 2.1 Tablas activas aprobadas

```text
RL_MR_FAMILIAS_FORMULARIO
RL_MR_VERSIONES_FORMULARIO
RL_MR_CATALOGOS
RL_MR_ELEMENTOS_CATALOGO
RL_MR_REGLAS_CALCULO
RL_MR_RIESGOS
RL_MR_EVALUACIONES_RIESGO
RL_MR_PROYECCIONES_EVALUACION
RL_MR_FLUJOS_EVALUACION
RL_MR_CONTROLES_RIESGO
RL_MR_EVALUACIONES_CONTROL
RL_MR_PLANES
RL_MR_ACTIVIDADES
RL_MR_EVIDENCIAS
RL_MR_EVIDENCIAS_VINCULOS
RL_MR_SENALES_ALERTA
RL_MR_AUTOMONITOREO
```

### 2.2 Secuencias activas aprobadas

```text
SEQ_RL_MR_FAMILIAS
SEQ_RL_MR_VERSIONES
SEQ_RL_MR_CATALOGOS
SEQ_RL_MR_ELEMENTOS
SEQ_RL_MR_REGLAS
SEQ_RL_MR_RIESGOS
SEQ_RL_MR_EVALUACIONES
SEQ_RL_MR_PROYECCIONES
SEQ_RL_MR_FLUJOS
SEQ_RL_MR_CONTROLES
SEQ_RL_MR_EVAL_CONTROLES
SEQ_RL_MR_PLANES
SEQ_RL_MR_ACTIVIDADES
SEQ_RL_MR_EVIDENCIAS
SEQ_RL_MR_EVI_VINCULOS
SEQ_RL_MR_SENALES
SEQ_RL_MR_AUTOMONITOREO
```

---

## 3. Validador independiente

Se agregó:

```text
scripts/validation/validate_matrices_17_object_inventory.ps1
```

El validador:

1. carga el manifiesto JSON;
2. exige exactamente 17 tablas y 17 secuencias en el manifiesto;
3. rechaza nombres inválidos o duplicados;
4. elimina comentarios SQL para identificar únicamente sentencias activas;
5. extrae `CREATE TABLE RL_MR_*` y `CREATE SEQUENCE SEQ_RL_MR_*`;
6. compara conjuntos exactos, sin tolerar faltantes ni elementos adicionales;
7. detecta sentencias `CREATE` duplicadas;
8. separa la sección de retiro controlado de la creación activa;
9. exige que la lista de retiro contenga las 17 tablas vigentes y las 18 tablas heredadas declaradas;
10. exige el retiro genérico de todas las secuencias `SEQ_RL_MR_%`;
11. impide que tablas o secuencias heredadas aparezcan después del marcador de creación activa.

El script `06_reconstruir_modelo_17_tablas.sql` no fue modificado durante esta fase.

---

## 4. Pruebas negativas por mutación

Se agregó:

```text
scripts/validation/test_matrices_17_object_inventory.ps1
```

La suite ejecuta nueve casos:

| Caso | Resultado esperado |
|---|---|
| DDL original | Aprueba |
| Eliminar `RL_MR_AUTOMONITOREO` | Falla por tabla ausente |
| Agregar `RL_MR_TABLA_18` | Falla por tabla adicional |
| Duplicar `RL_MR_RIESGOS` | Falla por `CREATE TABLE` duplicado |
| Eliminar `SEQ_RL_MR_AUTOMONITOREO` | Falla por secuencia ausente |
| Agregar `SEQ_RL_MR_EXTRA` | Falla por secuencia adicional |
| Crear `RL_MR_AUDITORIA` | Falla por objeto heredado activo |
| Mencionar `RL_MR_TRAZAS_CALCULO` fuera del retiro | Falla por ubicación no autorizada |
| Manifiesto con 16 tablas | Falla por cardinalidad incorrecta |

Los fixtures se generan en una carpeta temporal y se eliminan al finalizar. No modifican el DDL real.

---

## 5. Integración con Quality Gates

Se actualizó:

```text
.github/workflows/quality-gates.yml
```

La nueva etapa obligatoria ejecuta:

```powershell
./scripts/validation/validate_matrices_17_object_inventory.ps1
./scripts/validation/test_matrices_17_object_inventory.ps1
```

La compilación y las pruebas restantes no comienzan cuando el inventario falla.

---

## 6. Commits principales

```text
370cad4a3f0023361556e6d800eb6904840fc1de
feat(matrices): validar inventario exacto de 17 objetos

4140734b2b46ebadcd603de0be97221becc93e7b
fix(matrices): normalizar salida de pruebas del inventario
```

El segundo commit corrigió únicamente el código de salida de PowerShell. En la primera ejecución, el validador y los nueve casos habían aprobado, pero el proceso conservó el código `1` del último fixture negativo esperado. Se agregó `exit 0` al cierre exitoso de la suite.

---

## 7. Validación ejecutada

Quality Gate definitivo:

```text
Run: 31050344761
Commit validado: 4140734b2b46ebadcd603de0be97221becc93e7b
Resultado: success
```

Etapas aprobadas:

- restauración del Backend;
- instalación de dependencias Frontend;
- validador dinámico existente;
- inventario exacto de 17 tablas y 17 secuencias;
- nueve pruebas negativas del inventario;
- compilación Release;
- instalación de Playwright Chromium;
- pruebas Backend;
- cobertura Backend;
- pruebas Frontend;
- cobertura Frontend;
- build Angular;
- pruebas E2E;
- puertas completas del repositorio.

---

## 8. Criterios de cierre

| Criterio | Resultado |
|---|---|
| Manifiesto cerrado de 17 tablas | Cumplido |
| Manifiesto cerrado de 17 secuencias | Cumplido |
| Comparación exacta contra el DDL | Cumplido |
| Detección de objetos faltantes | Cumplido |
| Detección de objetos adicionales | Cumplido |
| Detección de duplicados | Cumplido |
| Prohibición de objetos heredados activos | Cumplido |
| Restricción de nombres heredados al retiro | Cumplido |
| Pruebas negativas automatizadas | Cumplido |
| Ejecución obligatoria en CI | Cumplido |
| Quality Gates completos | Correctos |
| Oracle sin ejecutar | Cumplido |
| Script `06` sin ejecutar | Cumplido |
| `main` intacta | Cumplido |

---

## 9. Restricciones vigentes

- `main` permanece intacta;
- el PR #20 permanece abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se eliminaron ni crearon objetos físicos;
- no se publicaron credenciales.

---

## 10. Resultado y siguiente fase

La **Fase 5 queda completada**.

La siguiente intervención es la **Fase 6 — consolidación de pruebas automatizadas no Oracle**, orientada a cerrar cobertura funcional del corte definitivo en Backend, Angular y E2E antes de preparar la suite Oracle de la Fase 7.
