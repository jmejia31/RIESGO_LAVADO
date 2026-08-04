# Fase 0-R — Aprobación del rediseño reducido

## Módulo Matrices de Riesgos

- **Fecha:** 2026-08-04.
- **Estado:** aprobado funcionalmente por Javier Mejía.
- **Alcance:** decisión de diseño e inventario técnico. No modifica Oracle, código ni datos.

## Decisión aprobada

El modelo objetivo se reduce de 34 a 17 tablas `RL_MR_*`. Se mantienen las tablas institucionales `RL_USUARIOS`, `RL_ROLES`, `RL_USUARIO_MODULOS`, `RL_MODULOS` y `RL_AUDITORIA`; no se crea relación con Monitoreo de Listas.

### Tablas objetivo

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

`RL_MR_PROYECCIONES_EVALUACION` se conserva obligatoriamente: soporta consultas, filtros, mapa de calor, dashboard y Matriz Consolidada sin procesar CLOB JSON en Oracle 11g.

## Retiro previsto

Se retirarán en la fase de transición: `RL_MR_CAMPOS_FORMULARIO`, `RL_MR_APROBACIONES_FORMULARIO`, `RL_MR_PERMISOS_FORMULARIO`, `RL_MR_RELACIONES_RIESGO`, `RL_MR_REVISIONES_EVALUACION`, `RL_MR_TRAZAS_CALCULO`, `RL_MR_LOTES_IMPORTACION`, `RL_MR_DETALLES_IMPORTACION`, `RL_MR_AUDITORIA` y las nueve tablas `RL_MR_EVI_*`.

Las nueve tablas puente se reemplazarán por `RL_MR_EVIDENCIAS_VINCULOS`. Esta tabla deberá tener una lista blanca de tipos de entidad y validación transaccional en backend; por ser una relación polimórfica no puede usar una FK Oracle hacia todas las entidades. La integridad del archivo sí continuará con FK a `RL_MR_EVIDENCIAS`.

## Confirmación de datos

Javier Mejía confirmó que los registros de las tablas previstas para retiro son pruebas prescindibles. Aun así, ningún objeto se elimina en esta fase: el retiro se hará solo después de contar con DDL, backend, frontend, pruebas y respaldo aprobados.

## Hallazgos del inventario local

- El DDL actual define 34 tablas y 24 secuencias; no corresponde todavía al objetivo de 17 tablas.
- `MatricesRiesgosRepository`, DTOs de evidencias y pruebas de integración usan directamente revisiones, trazas, auditoría local y las nueve tablas `RL_MR_EVI_*`.
- El controlador está protegido por `ModuloAuthorize(10)` y la autorización institucional procede de `RL_USUARIO_MODULOS`; el modelo reducido no requerirá `RL_MR_PERMISOS_FORMULARIO`.
- No se ejecutó Oracle ni el script `05` en esta fase.

## Criterio de salida y continuación

La Fase 0-R queda aprobada como decisión de alcance. Continúa la **Fase 1 — Diseño físico y de transición**: DDL de 17 tablas, índices, secuencias, script de prevalidación, estrategia de retiro posterior y actualización de contratos, todo sin ejecución Oracle.
