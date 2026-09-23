# Cierre institucional para producción — estado y decisiones pendientes

Fecha de consolidación: **2026-09-23**  
Rama de trabajo: `desarrollo`  
Seguimiento: [GitHub issue #22](https://github.com/jmejia31/RIESGO_LAVADO/issues/22)

## Estado ya cerrado

- `FASE_5_3=CERRADA`.
- `FASE_6=CERRADA`.
- `FASE_7=CERRADA_EN_DESARROLLO_CON_EXCEPCION_DOCUMENTADA`.
- Oracle restaurado y validado: `25/25`, `RESTORE_DATOS_RL_MR_V2=PASS`, `PREFLIGHT_AFTER_RESTORE=PASS`, `POSTFLIGHT_AFTER_RESTORE=PASS`.
- `MONITOREO_PERFORMANCE_POINT=CLOSED_BY_OWNER`.
- `MONITOREO_WARM_PAGE_CACHE=PASS`.
- `COLD_ORIGIN_LATENCY=ACCEPTED_KNOWN_RESTRICTION`.
- No se autorizan ni requieren cambios físicos adicionales sobre `DNP_IHSS` o `MMATAMOROS`.
- `main` no se modifica por este cierre institucional.

## 1. Valores de prueba y contrato del responsable — CERRADO

Decisión expresa del propietario, **2026-09-23**:

```text
PRODUCTION_TEST_PLACEHOLDERS=2
PRODUCTION_TEST_PLACEHOLDERS_BLOCKING=0
PRODUCTION_VALUES_PENDING=0
ROTR-ALMACENBIENE-23.dueno_riesgo=GTIC       # TEST_DATA_ONLY
RCUMP-COMPRAS-37.respuesta_riesgo=MITIGAR    # TEST_DATA_ONLY
PRODUCTION_VALUES_DECISION=CLOSED_AS_TEST_DATA
```

Los dos valores anteriores pertenecen a datos de prueba y **no se promueven a valores institucionales productivos ni a seeds de producción**. Por tanto, no bloquean release ni despliegue.

El criterio funcional queda definido para los **dos campos de Identificación del riesgo mostrados al usuario**:

- **Área principal**: lista desplegable administrable mediante `MR_AREA_PRINCIPAL`, con opción `Otro / Escribir manualmente…`. El catálogo es independiente y nace vacío para no inventar áreas institucionales.
- **Responsable o dueño del riesgo / área responsable**: lista desplegable administrable mediante `MR_AREA_RESPONSABLE`, también con opción `Otro / Escribir manualmente…`. El catálogo nace vacío para no convertir `GTIC` ni otro dato de prueba en valor institucional por inferencia.
- Ambos usan el tipo oficial `selector-catalogo` con `permiteValorManual=true`; por tanto conservan los 9 tipos oficiales del Form Builder y no introducen un control nuevo.
- Cada campo tiene su **propio mantenimiento de catálogo** dentro del Form Builder, aunque una institución pueda cargar valores similares en ambos.
- Al clonar una versión existente para crear un nuevo borrador, la aplicación adapta automáticamente `area_principal` y `dueno_riesgo` al patrón flexible sin modificar la versión origen.
- `respuesta_riesgo` no forma parte de esta mejora. `RCUMP-COMPRAS-37.respuesta_riesgo=MITIGAR` continúa únicamente como dato de prueba no bloqueante y el contrato vigente del campo de respuesta no se altera.
- V1/V2 históricas no se mutan retroactivamente ni cambian sus hashes. La mejora aplica a nuevos borradores/versiones administrables.

## 2. RTO institucional — PROPUESTA DOCUMENTADA / FIRMA PENDIENTE

Se documentó la propuesta institucional de continuidad en:

- `docs/3. Módulo Matrices de Riesgos/Cierre Institucional/ACTA_RTO_INSTITUCIONAL_SGRLA_IHSS_v1_1.md` (fuente canónica editable)\n- `docs/3. Módulo Matrices de Riesgos/Cierre Institucional/Acta_RTO_Institucional_SGRLA_IHSS_v1_1.pdf` (copia física imprimible y firmable)

Estado vigente:

```text
RTO_PROPOSED=4 horas
INCIDENT_RESPONSE_TARGET=0 minutos
CATASTROPHIC_EXCEPTION=TRUE
RTO_DEFINED=FALSE
RTO_PENDING_INSTITUTIONAL_SIGNATURE=TRUE
```

Criterio: toda indisponibilidad no planificada activa atención, diagnóstico y escalamiento desde el minuto 0. El objetivo de recuperación propuesto es de cuatro (4) horas bajo condiciones normales y con dependencias técnicas disponibles. El valor de 0 minutos es únicamente el objetivo de inicio de atención; no constituye un RTO de 0 minutos ni una garantía de recuperación instantánea.

La continuidad es responsabilidad institucional compartida. La superación del RTO no determina por sí sola responsabilidad personal del encargado del sistema ni de Desarrollo/GTIC; la evaluación debe considerar causa raíz, control efectivo, accesos, infraestructura, Oracle/DBA, seguridad, telecomunicaciones, terceros, autorizaciones, recursos y evidencia objetiva. Las contingencias catastróficas o dependencias externas permiten el tiempo técnicamente necesario para una recuperación segura, con trazabilidad y escalamiento.

El cierre de este punto requiere la aprobación, firma y archivo institucional de la versión adoptada. Sólo entonces corresponde registrar:

```text
RTO_DEFINED=TRUE
RTO_VALUE=4 horas
```

## 2.1. RPO institucional — PENDIENTE

Estado:

```text
RPO_DEFINED=FALSE
```

El RPO debe ser definido y aprobado institucionalmente por DBA/Infraestructura/Negocio. Debe registrar una pérdida máxima tolerable de datos con unidad y valor exactos, sin inferirla de tiempos de restore o de la arquitectura existente.

## 3. Capacitación institucional

Material disponible:

- plan de capacitación;
- material de capacitación;
- checklist;
- plantilla de asistencia;
- plantilla de evaluación.

Estado:

```text
TRAINING_MATERIAL=PASS
INSTITUTIONAL_TRAINING_EXECUTED=FALSE
```

Para cerrar este punto se debe registrar al menos fecha, responsable, asistentes y evidencia de aceptación/evaluación.

## 4. Despliegue productivo

Estado:

```text
RELEASE_READINESS=PASS
PRODUCTION_DEPLOYED=FALSE
```

Cuando exista autorización institucional, la operación debe conservar evidencia de:

1. backup previo;
2. preflight;
3. despliegue del SHA autorizado;
4. postflight;
5. smoke final;
6. resultado y responsables.

No se cambia `PRODUCTION_DEPLOYED` a `TRUE` por documentación o intención; requiere ejecución real.

## Deudas no bloqueantes

- auditoría read-only de DB links en ambos schemas;
- revisión histórica `VER_ID 27/28`;
- RBAC granular global.

Estas deudas no reabren Fase 7 ni bloquean el cierre de desarrollo ya certificado.

## No reabrir

- reconstrucción o restore de Fase 7;
- índices, estadísticas o DDL en `DNP_IHSS` / `MMATAMOROS`;
- punto de rendimiento de Monitoreo;
- Fase 5.3, Fase 6 o Fase 7 por los cuatro pendientes institucionales anteriores.

## Impacto de esta consolidación

```text
INTERFACES_CAMBIAN=NO
DATOS_CAMBIAN=NO
TENANCY_CAMBIA=NO
SEGURIDAD_CAMBIA=NO
JOBS_EVENTOS_CAMBIAN=NO
CACHE_SESION_CAMBIA=NO
MEDIA_DOCUMENTOS_CAMBIAN=NO
OBSERVABILIDAD_CAMBIA=NO
MAPA_ARQUITECTURA=NO_APLICA
```

Es una consolidación documental y de gobierno de release; no modifica código, Oracle, contratos REST ni arquitectura.
