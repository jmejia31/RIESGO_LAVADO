# Fase 1-R — Diseño físico y transición al modelo de 17 tablas

## Estado y límites

- **Estado:** diseño técnico preparado; pendiente de aprobación para iniciar codificación.
- **No ejecutado:** DDL Oracle, script `05`, eliminación de tablas y migración física.
- **Base de decisión:** [Fase 0-R](FASE_0_REDISENO_MODELO_17_TABLAS.md), aprobada por Javier Mejía.

Esta fase reemplaza el diseño físico de 34 tablas para el Módulo Matrices de Riesgos. No modifica la relación institucional de seguridad ni genera ninguna dependencia con Monitoreo de Listas.

## 1. Modelo objetivo

| # | Tabla | Función física y reglas clave |
|---:|---|---|
| 1 | `RL_MR_FAMILIAS_FORMULARIO` | Identifica cada familia de formularios. Código único, nombre, descripción, activo y fechas. |
| 2 | `RL_MR_VERSIONES_FORMULARIO` | Guarda `VER_JSON`, hash, número, estado, vigencia y creador. Una sola versión vigente por familia. El JSON contiene secciones y campos. |
| 3 | `RL_MR_CATALOGOS` | Cabeceras de listas reutilizables como frecuencia, impacto y áreas. |
| 4 | `RL_MR_ELEMENTOS_CATALOGO` | Valores ordenados, activos y únicos por catálogo. |
| 5 | `RL_MR_REGLAS_CALCULO` | Código, versión, algoritmo backend y estado de la regla VRI/ETP/VRR. |
| 6 | `RL_MR_RIESGOS` | Identidad estable del riesgo: código único, nombre, descripción, activo y creador. |
| 7 | `RL_MR_EVALUACIONES_RIESGO` | Instancia histórica de captura. Conserva la versión de formulario, `EVA_DATOS_JSON`, `EVA_CALCULOS_JSON`, usuario, fecha, activo y versión de concurrencia. |
| 8 | `RL_MR_PROYECCIONES_EVALUACION` | Datos planos derivados de la evaluación: área, VRI, VRR, niveles, respuesta, dueño, fecha y estado actual. Una proyección por evaluación. |
| 9 | `RL_MR_FLUJOS_EVALUACION` | Historial de estados, usuario, fecha y motivo/observación. Sustituye las revisiones independientes. |
| 10 | `RL_MR_CONTROLES_RIESGO` | Controles asociados a una evaluación. |
| 11 | `RL_MR_EVALUACIONES_CONTROL` | Efectividad y comentario de cada control; origen del cálculo ETP. |
| 12 | `RL_MR_PLANES` | Planes de mitigación por evaluación. |
| 13 | `RL_MR_ACTIVIDADES` | Actividades, responsable, avance, fechas y estado de cada plan. |
| 14 | `RL_MR_EVIDENCIAS` | Archivo físico, hash, tamaño, ruta, usuario y fecha. |
| 15 | `RL_MR_EVIDENCIAS_VINCULOS` | Vínculo genérico de evidencia con riesgo, evaluación, control, plan, actividad, alerta o automonitoreo. |
| 16 | `RL_MR_SENALES_ALERTA` | Alertas individualmente consultables por evaluación, estado y fecha. |
| 17 | `RL_MR_AUTOMONITOREO` | Registros de automonitoreo individualmente consultables por evaluación y fecha. |

## 2. Especificación de integridad y rendimiento

### Versiones, JSON y auditoría

- `RL_MR_VERSIONES_FORMULARIO`: `UQ(VER_FAMILIA_ID, VER_VERSION)`; índice compuesto de consulta por familia, vigencia y estado.
- Los campos nuevos, eliminados, ordenados u ocultos se guardan solamente en `VER_JSON`; no se hará `ALTER TABLE` por un campo de formulario.
- Una evaluación guarda una copia de sus respuestas en `EVA_DATOS_JSON` y de sus cálculos oficiales en `EVA_CALCULOS_JSON`. La versión enlazada permite reconstruir su formulario histórico.
- Oracle 11g no valida JSON nativamente; backend valida sintaxis, esquema, campos permitidos y reglas antes de persistir.
- Publicación de versión, transición de evaluación, vínculo de evidencia y eliminación de evidencia deben registrar el evento en `RL_AUDITORIA` dentro de la misma transacción cuando aplique.

### Evaluación y consolidado

- `RL_MR_PROYECCIONES_EVALUACION` tendrá `UQ(PROY_EVALUACION_ID)` y FK a evaluación.
- Índices mínimos: `(PROY_FECHA_EVAL)`, `(PROY_NIVEL_RESIDUAL)`, `(PROY_ESTADO_EVALUACION)`, `(PROY_AREA_PRINCIPAL)`, `(PROY_DUENO_RIESGO)` y un índice compuesto para filtros frecuentes `(PROY_ESTADO_EVALUACION, PROY_NIVEL_RESIDUAL, PROY_FECHA_EVAL)`.
- El estado actual proviene de `RL_MR_FLUJOS_EVALUACION`; se replica en proyección para filtros rápidos. Backend actualiza ambos dentro de una transacción.
- Índices de relación: evaluación-riesgo, evaluación-versión, flujo-evaluación-fecha, control-evaluación, plan-evaluación, actividad-plan, alerta-evaluación y automonitoreo-evaluación-fecha.

### Evidencias genéricas

Propuesta de columnas de `RL_MR_EVIDENCIAS_VINCULOS`:

```text
EVV_ID                 PK numérica
EVV_EVIDENCIA_ID       FK -> RL_MR_EVIDENCIAS(EVI_ID)
EVV_TIPO_ENTIDAD       RIESGO | EVALUACION | CONTROL | PLAN | ACTIVIDAD | ALERTA | AUTOMONITOREO
EVV_ENTIDAD_ID         identificador del registro destino
EVV_USR_CREACION       FK -> RL_USUARIOS(USR_ID)
EVV_FECHA_CREACION     fecha de vínculo
```

- Restricción `CHECK` para la lista cerrada de tipos y `UQ(EVV_EVIDENCIA_ID, EVV_TIPO_ENTIDAD, EVV_ENTIDAD_ID)`.
- Índices: `(EVV_TIPO_ENTIDAD, EVV_ENTIDAD_ID)` y `(EVV_EVIDENCIA_ID)`.
- La FK al archivo es física. La existencia de la entidad destino se validará en backend mediante lista blanca y consulta parametrizada; Oracle no admite una FK polimórfica única hacia siete tablas.

## 3. Mapeo de retiro y reemplazo

| Objeto actual | Destino en el modelo reducido |
|---|---|
| `RL_MR_CAMPOS_FORMULARIO` | Estructura, orden y propiedades en `VER_JSON`. |
| `RL_MR_APROBACIONES_FORMULARIO` | Estado/usuario/fecha de versión + evento en `RL_AUDITORIA`. |
| `RL_MR_PERMISOS_FORMULARIO` | `ModuloAuthorize(10)`, `RL_USUARIO_MODULOS` y roles institucionales. |
| `RL_MR_RELACIONES_RIESGO` | Retirada; no hay requisito aprobado de jerarquía de riesgos. |
| `RL_MR_REVISIONES_EVALUACION` | `RL_MR_FLUJOS_EVALUACION.FLU_MOTIVO` y auditoría institucional. |
| `RL_MR_TRAZAS_CALCULO` | Regla/versionado en cálculo JSON y evento de auditoría; no traza por fila independiente. |
| `RL_MR_LOTES_IMPORTACION`, `RL_MR_DETALLES_IMPORTACION` | Retiradas; no se migrarán datos de prueba de Excel. |
| `RL_MR_AUDITORIA` | `RL_AUDITORIA` institucional. |
| Nueve tablas `RL_MR_EVI_*` | `RL_MR_EVIDENCIAS_VINCULOS`. |

## 4. Cambios de contrato obligatorios

### Backend

1. Sustituir las nueve solicitudes `AsociarEvidencia*Dto` por `VincularEvidenciaDto { EvidenciaId, TipoEntidad, EntidadId }` y un enum cerrado.
2. Reemplazar nueve métodos de repositorio y servicio por `VincularEvidenciaAsync`; adaptar eliminación segura para consultar la tabla única de vínculos.
3. Retirar endpoint y DTO de revisiones; mostrar observaciones desde el historial de flujos.
4. Retirar DTOs, consultas y secuencias de campos, aprobaciones, permisos, trazas, importaciones y auditoría local.
5. Mantener `ModuloAuthorize(10)` y agregar políticas de rol institucional para administrar formularios, publicar y capturar; la interfaz no sustituye esta validación.
6. Cambiar el mapeo físico de `EVA_DATA_JSON`/`EVA_DATA_CALC_JSON` a los nombres definitivos `EVA_DATOS_JSON`/`EVA_CALCULOS_JSON` si el DDL reducido los adopta.

### Frontend

1. Sustituir nueve métodos de vínculo y sus pruebas por un servicio único con selector de tipo de entidad permitido.
2. Reemplazar la pantalla de revisiones por la línea de tiempo de `RL_MR_FLUJOS_EVALUACION`.
3. Quitar pantallas/acciones de aprobación granular y permiso por campo; mantener administración de versiones solo para el rol autorizado.
4. Conservar constructor JSON, catálogo, reglas, captura dinámica, alertas y automonitoreo.
5. Corregir servicios y rutas para que coincidan con los endpoints realmente publicados antes de reiniciar el frontend.

## 5. Secuencia de implementación

1. Crear DDL de destino y script de prevalidación **sin añadirlo al ejecutor automático**.
2. Refactorizar backend y pruebas contra una base Oracle de pruebas con el modelo de 17 tablas.
3. Refactorizar frontend y pruebas unitarias/E2E.
4. Ejecutar validadores, compilación Release, suites backend/frontend/E2E y pruebas de rendimiento de proyecciones.
5. Preparar respaldo y ventana de mantenimiento.
6. Ejecutar transición en Oracle solo con autorización expresa: crear/ajustar objetos, validar, desplegar aplicación y realizar pruebas de humo.
7. Ejecutar el retiro controlado de objetos descartados únicamente después de validar la aplicación contra el nuevo esquema.

## 6. Criterios de salida de Fase 1-R

- DDL de 17 tablas, índices y secuencias diseñado y revisado, sin ejecución.
- Mapeo completo de cada tabla/contrato actual a destino o retiro.
- Estrategia explícita para evidencia genérica, auditoría institucional y consultas de alto rendimiento.
- Lista de cambios backend/frontend y pruebas definida.
- Ninguna modificación Oracle ni eliminación realizada.
