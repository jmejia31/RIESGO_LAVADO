> **DOCUMENTO HISTÓRICO / SUPERSEDIDO.** La versión vigente y aprobada para cierre es `ACTA_RTO_RPO_INSTITUCIONAL_SGRLA_IHSS_v2_0.md` del 24/09/2026. Este archivo v1.1 se conserva únicamente como antecedente de la propuesta previa y sus estados `RTO_DEFINED=FALSE` ya no representan el estado vigente del proyecto.

# ACTA INSTITUCIONAL DE DEFINICIÓN DEL RTO Y CONTINUIDAD OPERATIVA DEL SGRLA-IHSS

**Sistema:** SGRLA-IHSS  
**Documento:** RTO / Continuidad  
**Versión:** 1.1  
**Fecha:** 23/09/2026  
**Unidad técnica:** GTIC / Desarrollo  
**Propietario funcional:** Negocio / área usuaria  
**Estado:** PARA APROBACIÓN Y FIRMA INSTITUCIONAL  
**Clasificación:** Uso institucional

> Este documento constituye la copia canónica versionada en el repositorio. La definición entra en vigor únicamente después de aprobación, firma y archivo institucional por las autoridades competentes.

## 1. Objeto y alcance

**Objeto.** Formalizar el objetivo institucional de recuperación del SGRLA-IHSS y establecer un marco de actuación, evidencia, escalamiento y asignación de responsabilidades ante interrupciones del servicio.

**Alcance.** Aplica a incidentes que afecten la disponibilidad del aplicativo SGRLA-IHSS, su API, frontend, base de datos, infraestructura, red, autenticación y dependencias tecnológicas requeridas para prestar el servicio.

## 2. Definición propuesta del RTO

### Decisión propuesta para aprobación institucional

```text
RTO_INSTITUCIONAL_PROPUESTO=4 horas
INCIDENT_RESPONSE_TARGET=0 minutos
CATASTROPHIC_EXCEPTION=TRUE
RTO_DEFINED=FALSE
RTO_PENDING_INSTITUTIONAL_SIGNATURE=TRUE
```

**RTO institucional propuesto: cuatro (4) horas.** La institución establecerá, una vez aprobado y firmado, que el SGRLA-IHSS debe ser restablecido, bajo condiciones normales de recuperación y con las dependencias técnicas disponibles, en un plazo máximo objetivo de cuatro (4) horas desde la detección o notificación verificable de la indisponibilidad.

**Objetivo de respuesta inmediata: 0 minutos.** Toda caída o interrupción no planificada se considera incidente prioritario desde el primer momento. El valor de 0 minutos se utiliza únicamente como objetivo de inicio de atención, diagnóstico y escalamiento. **No constituye un RTO de 0 minutos, no representa una garantía de disponibilidad absoluta y no obliga a una resolución instantánea.**

El RTO de cuatro horas es una propuesta institucional de gobierno y debe ser ratificado por las autoridades correspondientes. Si posteriormente un BIA aprobado establece otro valor y existe capacidad técnica para cumplirlo, el RTO deberá actualizarse mediante control documental.

## 3. Excepción por evento catastrófico o dependencia externa crítica

Cuando la recuperación dentro del objetivo resulte materialmente imposible por evento catastrófico, fuerza mayor, ciberincidente, falla de infraestructura crítica o dependencia externa fuera del control directo de Desarrollo/GTIC, el incidente continuará siendo prioritario, pero el restablecimiento utilizará **el tiempo técnicamente necesario para diagnosticar, contener, recuperar y validar el servicio de manera segura, íntegra y verificable**.

Ejemplos:

| Categoría | Ejemplos |
| --- | --- |
| Infraestructura | Falla total de centro de datos, almacenamiento, virtualización, energía, red o equipos críticos. |
| Base de datos | Corrupción grave, pérdida de disponibilidad de Oracle, recuperación desde respaldo o contingencia administrada por DBA. |
| Ciberseguridad | Incidente que requiera aislamiento, contención, análisis forense, rotación de credenciales o recuperación controlada. |
| Terceros / telecomunicaciones | Caídas de enlaces, autenticación, proveedores, telecomunicaciones o componentes externos requeridos. |
| Fuerza mayor | Eventos naturales, incendios, inundaciones, disturbios u otras circunstancias que impidan físicamente la operación normal. |

## 4. Responsabilidad institucional y protección operativa

La continuidad del SGRLA-IHSS es una **responsabilidad institucional compartida**. El objetivo RTO no debe interpretarse como una asignación automática de responsabilidad personal al encargado del sistema, a un desarrollador individual o al Departamento de Desarrollo/GTIC por toda interrupción o por el tiempo total de recuperación.

La evaluación de cualquier incidente deberá considerar como mínimo:

- causa raíz;
- control efectivo de cada unidad;
- disponibilidad y estado de infraestructura;
- disponibilidad, respaldo y recuperación de Oracle;
- dependencias externas;
- autorizaciones y accesos;
- medidas de seguridad;
- cambios aprobados;
- recursos disponibles;
- tiempos atribuibles a terceros;
- evidencia objetiva del incidente y del escalamiento.

La actuación de Desarrollo/GTIC y del encargado del sistema deberá valorarse según **diligencia técnica razonable, atención oportuna, escalamiento, coordinación, trazabilidad y cumplimiento de los procedimientos aplicables**.

El tiempo técnicamente indispensable para diagnosticar, contener, recuperar y validar un incidente **no constituye por sí mismo incumplimiento** cuando exista evidencia de atención oportuna y la duración se encuentre justificada por la complejidad o por dependencias fuera del control efectivo de Desarrollo/GTIC o del encargado del sistema.

### Nota de gobierno y revisión jurídica

Este documento define responsabilidades operativas, criterios de escalamiento y evidencia de gestión. No sustituye la normativa aplicable ni puede eliminar responsabilidades legales que correspondan por dolo, negligencia grave o incumplimiento de obligaciones expresamente establecidas.

**La superación del RTO, por sí sola, no constituye prueba de negligencia ni determina automáticamente responsabilidad disciplinaria, civil, administrativa o personal.** Cualquier evaluación debe considerar causa raíz, control efectivo, recursos y accesos disponibles, dependencias institucionales o de terceros, autorizaciones, escalamiento y evidencia objetiva.

Para efectos laborales, disciplinarios, administrativos o probatorios, la versión adoptada debe ser aprobada por las autoridades competentes y, cuando corresponda, revisada por Asesoría Legal/Compliance.

## 5. Distribución mínima de responsabilidades

| Rol / unidad | Responsabilidad principal | Evidencia esperada |
| --- | --- | --- |
| Negocio / propietario funcional | Definir criticidad, prioridad de servicio y criterios de continuidad. | Aprobación del RTO y priorización del servicio. |
| Infraestructura | Disponibilidad de servidores, virtualización, red, almacenamiento, energía y servicios de plataforma. | Monitoreo, bitácoras, cambios y acciones de recuperación. |
| DBA | Disponibilidad, integridad, respaldo y recuperación de Oracle. | Backups, logs, restore/recovery y validaciones. |
| Desarrollo / GTIC | Diagnóstico y corrección del aplicativo, despliegue, validación técnica y coordinación de incidentes de software. | Tickets, commits, logs, pruebas, despliegues y bitácora. |
| Seguridad / soporte relacionado | Contención y autorización cuando exista incidente de seguridad o acceso. | Registro del incidente, medidas adoptadas y liberación. |
| Encargado del sistema | Coordinar atención, escalar oportunamente, conservar evidencia y comunicar estado. No sustituye funciones propias de Infraestructura, DBA, Seguridad o Negocio. | Cronología, comunicaciones, solicitudes, tickets y cierre. |

## 6. Regla de atención y escalamiento

1. **Minuto 0 - detección/notificación:** registrar el incidente y comenzar diagnóstico.
2. **Respuesta inmediata:** aplicar acciones seguras bajo el control y atribuciones del equipo responsable.
3. **Escalamiento:** solicitar sin demora Infraestructura, DBA, Seguridad, proveedor u otra unidad cuando la causa o recuperación dependa de ella.
4. **Contingencia extraordinaria:** documentar causa, dependencia, decisiones y estimación tan pronto exista información confiable.
5. **Cierre:** no cerrar el incidente hasta restablecer el servicio, validar funcionamiento e integridad y completar la trazabilidad.

## 7. Medición y atribución del tiempo

**Tiempo de indisponibilidad:** se medirá desde la detección o notificación verificable hasta el restablecimiento validado del servicio.

**Atribución:** el tiempo total y el tiempo atribuible a cada causa deberán registrarse por separado. La duración asociada a espera de infraestructura, recuperación de base de datos, autorización de seguridad, indisponibilidad de terceros, telecomunicaciones u otros factores externos **no deberá imputarse automáticamente a Desarrollo/GTIC ni al encargado del sistema**.

La obligación del encargado del sistema es actuar, escalar, coordinar y documentar oportunamente dentro del ámbito de sus facultades y accesos. No se le exigirá ejecutar funciones, accesos o decisiones que correspondan formalmente a otras unidades o para las cuales no tenga autorización.

## 8. Evidencia obligatoria por incidente

- [ ] Fecha y hora de detección/notificación.
- [ ] Fecha y hora de inicio de atención.
- [ ] Síntoma e impacto observado.
- [ ] Clasificación/severidad.
- [ ] Áreas notificadas y hora de escalamiento.
- [ ] Causa raíz, cuando pueda determinarse.
- [ ] Acciones ejecutadas y responsable de cada acción.
- [ ] Dependencias o autorizaciones pendientes.
- [ ] Fecha y hora de recuperación técnica.
- [ ] Fecha y hora de validación funcional.
- [ ] Logs, tickets, correos, commits, despliegues y demás evidencia relevante.
- [ ] Acciones preventivas o correctivas posteriores.

## 9. Prevención y mejora continua

La atención inmediata y el RTO propuesto de cuatro horas requieren priorizar medidas preventivas y capacidades de recuperación. Las áreas responsables deberán evaluar, según criticidad y recursos aprobados, mecanismos de alta disponibilidad, monitoreo, redundancia, respaldo, recuperación, pruebas periódicas, documentación operativa, gestión de cambios y capacidad de soporte.

La ausencia de una medida preventiva que requiera presupuesto, infraestructura, privilegios, autorización o decisión fuera de Desarrollo/GTIC deberá registrarse como **riesgo o dependencia institucional** y no como responsabilidad individual del encargado del sistema.

Si el RTO se supera por causas no controlables directamente por Desarrollo/GTIC o por el encargado del sistema -incluyendo infraestructura, Oracle/DBA, seguridad, telecomunicaciones, proveedores, fuerza mayor, restricciones de acceso o decisiones institucionales pendientes-, se deberá documentar el motivo y mantener el escalamiento. La superación del RTO activa gestión y revisión del incidente, pero **no presume por sí misma responsabilidad personal**.

## 10. Entrada en vigor

Con la firma de la versión física/electrónica autorizada se aprobarán:

```text
RTO_DEFINED=TRUE
RTO_VALUE=4 horas
INCIDENT_RESPONSE_TARGET=0 minutos
CATASTROPHIC_EXCEPTION=TRUE
```

Hasta que existan aprobación, firma y archivo institucional:

```text
RTO_DEFINED=FALSE
RTO_PROPOSED=4 horas
RTO_PENDING_INSTITUTIONAL_SIGNATURE=TRUE
```

## 11. Hoja de aprobaciones y firmas

**Observaciones / excepciones:**

________________________________________________________________________________

________________________________________________________________________________

| Rol / unidad | Nombre | Cargo | Firma | Fecha |
| --- | --- | --- | --- | --- |
| Negocio / propietario funcional |  |  |  |  |
| Infraestructura |  |  |  |  |
| DBA |  |  |  |  |
| Desarrollo / GTIC |  |  |  |  |
| Encargado del sistema |  |  |  |  |
| Asesoría Legal / Compliance (si aplica) |  |  |  |  |

## 12. Control de estado

```text
RTO_DOCUMENT_VERSION=1.1
RTO_DOCUMENT_STATUS=PARA_APROBACION
RTO_PROPOSED=4 horas
INCIDENT_RESPONSE_TARGET=0 minutos
CATASTROPHIC_EXCEPTION=TRUE
RTO_DEFINED=FALSE
RTO_PENDING_INSTITUTIONAL_SIGNATURE=TRUE
```
