# Fase 0 — Congelamiento técnico y línea base del corte definitivo

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha:** 2026-08-05.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — prohibido modificar o fusionar sin autorización expresa de Javier Mejía.
- **Estado:** completada como fase de control, inventario y congelamiento. No representa corrección de los bloqueantes ni ejecución Oracle.

---

## 1. Objetivo

Congelar el estado verificable del repositorio antes de corregir las incompatibilidades entre el backend y el DDL reducido de 17 tablas. Esta fase evita ejecutar Oracle, el script `05`, el script `06` o cualquier eliminación física mientras el código continúe usando contratos del modelo anterior.

---

## 2. Línea base verificada en GitHub

| Control | Resultado |
|---|---|
| Repositorio | `jmejia31/RIESGO_LAVADO` |
| Ramas remotas | Únicamente `main` y `desarrollo` |
| `main` | `727082c6fcf90f95ce6db5eadf5c4b152397d080` |
| `desarrollo` | `35ec4c7f358f20c1052fc2b5bf991c3265620dcf` |
| Comparación | `desarrollo` está 130 commits por delante y 0 por detrás de `main` |
| PR de revisión | PR #20 abierto y en borrador, `desarrollo` → `main` |
| Merge | No realizado |
| Auto-merge | No autorizado |
| Último Quality Gate | Correcto (`success`) |

`main` permanece intacta. Todo cambio posterior debe publicarse exclusivamente en `desarrollo`.

---

## 3. Inventario físico objetivo congelado

Las únicas tablas específicas permitidas en el modelo objetivo del módulo son:

1. `RL_MR_FAMILIAS_FORMULARIO`
2. `RL_MR_VERSIONES_FORMULARIO`
3. `RL_MR_CATALOGOS`
4. `RL_MR_ELEMENTOS_CATALOGO`
5. `RL_MR_REGLAS_CALCULO`
6. `RL_MR_RIESGOS`
7. `RL_MR_EVALUACIONES_RIESGO`
8. `RL_MR_PROYECCIONES_EVALUACION`
9. `RL_MR_FLUJOS_EVALUACION`
10. `RL_MR_CONTROLES_RIESGO`
11. `RL_MR_EVALUACIONES_CONTROL`
12. `RL_MR_PLANES`
13. `RL_MR_ACTIVIDADES`
14. `RL_MR_EVIDENCIAS`
15. `RL_MR_EVIDENCIAS_VINCULOS`
16. `RL_MR_SENALES_ALERTA`
17. `RL_MR_AUTOMONITOREO`

Se reutilizan las estructuras institucionales de usuarios, roles, módulos y `RL_AUDITORIA`. No se creará una auditoría local duplicada para Matrices de Riesgos.

---

## 4. Bloqueantes congelados para las siguientes fases

La revisión previa identificó los siguientes bloqueantes que no se corrigen en esta fase:

1. El DDL usa `EVA_DATOS_JSON` y `EVA_CALCULOS_JSON`, mientras el repositorio todavía usa `EVA_DATA_JSON` y `EVA_DATA_CALC_JSON`.
2. El backend conserva escrituras y llamadas relacionadas con `RL_MR_TRAZAS_CALCULO`.
3. El backend conserva escrituras relacionadas con `RL_MR_AUDITORIA` y `SEQ_RL_MR_AUDITORIA`.
4. Permanecen adaptadores internos asociados a `RL_MR_EVI_APROBACION` y tablas puente heredadas.
5. Permanecen contratos temporales como `AsociarEvidenciaAprobacionDto` y `PermisoFormularioDto`.
6. La suite Oracle no valida todavía las 17 tablas, 17 secuencias, índices, restricciones y el ciclo completo del modelo reducido.

Estos hallazgos impiden ejecutar el script `06`.

---

## 5. Restricciones activas

Queda expresamente prohibido durante el corte técnico:

- modificar o fusionar `main`;
- cerrar o fusionar el PR #20;
- ejecutar Oracle;
- ejecutar el script `05`;
- ejecutar el script `06`;
- ejecutar `DROP TABLE` o retirar objetos físicamente;
- introducir nuevamente tablas, endpoints, DTOs o columnas del modelo retirado;
- publicar credenciales, cadenas de conexión o secretos;
- declarar el modelo de 17 tablas como certificado antes de las pruebas Oracle.

---

## 6. Criterios de salida de la Fase 0

| Criterio | Estado |
|---|---|
| Solo existen `main` y `desarrollo` | Cumplido |
| `main` permanece intacta | Cumplido |
| `desarrollo` está actualizada en GitHub | Cumplido |
| PR #20 permanece abierto y en borrador | Cumplido |
| Quality Gate observable en la cabecera actual | Cumplido |
| Inventario de 17 tablas congelado | Cumplido |
| Bloqueantes documentados | Cumplido |
| Oracle y scripts `05`/`06` sin ejecutar | Cumplido según estado documentado del proyecto |

---

## 7. Resultado y siguiente fase

La **Fase 0 queda completada** exclusivamente como congelamiento técnico, línea base e inventario. No cierra la transición del modelo reducido.

El siguiente trabajo autorizado es la **Fase 1 — alineación del contrato físico y nombres de columnas**, comenzando por:

```text
EVA_DATA_JSON       → EVA_DATOS_JSON
EVA_DATA_CALC_JSON  → EVA_CALCULOS_JSON
```

La Fase 1 deberá modificar consultas, inserciones, actualizaciones, lecturas, pruebas y validador; deberá compilar y pasar pruebas sin ejecutar Oracle.