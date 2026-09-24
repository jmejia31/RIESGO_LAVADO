# ACTA INSTITUCIONAL DE APROBACIÓN DEL RTO Y RPO DEL SGRLA-IHSS

**Sistema:** SGRLA-IHSS  
**Documento:** Continuidad Operativa - RTO / RPO  
**Versión:** 2.0  
**Fecha:** 24/09/2026  
**Unidad técnica:** GTIC / Desarrollo  
**Propietario funcional:** Negocio / área usuaria  
**Estado:** APROBADO Y CERRADO POR AUTORIZACIÓN EXPRESA DEL PROPIETARIO DEL PROYECTO  
**Clasificación:** Uso institucional

> Esta versión sustituye, para efectos de estado de cierre del proyecto, la propuesta RTO v1.1. La decisión de aprobación se registra por autorización expresa del propietario del proyecto en la sesión de trabajo del 24/09/2026. No se atribuyen ni inventan firmas de terceros no aportadas como evidencia.

## 1. Objeto y alcance

Formalizar los parámetros institucionales de continuidad y recuperación del SGRLA-IHSS para cerrar los puntos RTO y RPO del proyecto, estableciendo valores verificables y criterios de aplicación ante indisponibilidad o recuperación de datos.

Aplica al aplicativo SGRLA-IHSS, API, frontend, Oracle, infraestructura, red, autenticación y demás dependencias tecnológicas necesarias para prestar el servicio.

## 2. RTO institucional aprobado

```text
RTO_DEFINED=TRUE
RTO_VALUE=4 horas
RTO_MAXIMUM_TOLERABLE=4 horas
INCIDENT_RESPONSE_TARGET=0 minutos
CATASTROPHIC_EXCEPTION=TRUE
RTO_PENDING_INSTITUTIONAL_SIGNATURE=FALSE
RTO_STATUS=CLOSED
```

- Atención, diagnóstico y escalamiento: desde minuto 0.
- Recuperación máxima normal: 4 horas.
- El minuto 0 es inicio de gestión, no recuperación instantánea.
- Ante fuerza mayor, ciberincidente o dependencia crítica externa, aplica el tiempo técnicamente necesario con escalamiento y evidencia.
- Superar el RTO activa revisión, pero no determina por sí solo responsabilidad individual.

## 3. RPO institucional aprobado

```text
RPO_DEFINED=TRUE
RPO_TARGET=15 minutos
RPO_VALUE=15 minutos
RPO_MAXIMUM_TOLERABLE=60 minutos
RPO_PENDING_INSTITUTIONAL_SIGNATURE=FALSE
RPO_STATUS=CLOSED
```

- Objetivo: información con antigüedad no superior a 15 minutos cuando los mecanismos institucionales lo permitan.
- Pérdida máxima tolerable: 60 minutos.
- Aplica a pérdida, corrupción o recuperación de datos aun sin indisponibilidad total.
- Backup, archive logs, replicación, retención u otros mecanismos deberán administrarse de forma compatible con estos parámetros.
- Pruebas posteriores verifican la capacidad operativa y no reabren la definición institucional.

## 4. Responsabilidad institucional

La continuidad y recuperación son responsabilidades institucionales compartidas. El encargado del sistema debe actuar, escalar, coordinar, comunicar y conservar evidencia dentro de sus facultades y accesos, sin sustituir funciones propias de Infraestructura, DBA, Seguridad, Negocio o proveedores.

## 5. Evidencia mínima por incidente

- Fecha y hora de detección/notificación.
- Inicio de atención y escalamiento.
- Síntoma e impacto.
- Área(s) involucrada(s).
- Causa raíz, cuando pueda determinarse.
- Acciones ejecutadas y responsables.
- Hora de recuperación técnica.
- Hora de validación funcional.
- Punto de recuperación de datos alcanzado, cuando aplique.
- Logs, tickets, correos, respaldos, commits, despliegues y demás evidencia relevante.

## 6. Aprobación y cierre

Por autorización expresa del propietario del proyecto, registrada el `24/09/2026`, los parámetros RTO y RPO quedan adoptados para efectos de gobierno y cierre del proyecto.

No se declara que existan firmas físicas de Negocio, Infraestructura, DBA, Seguridad o Asesoría Legal si dichas firmas no han sido aportadas. Su eventual archivo administrativo podrá incorporarse posteriormente sin reabrir estos dos puntos, salvo decisión institucional expresa que modifique los valores.

```text
RTO_DEFINED=TRUE
RTO_VALUE=4 horas
RTO_MAXIMUM_TOLERABLE=4 horas
RTO_STATUS=CLOSED

RPO_DEFINED=TRUE
RPO_VALUE=15 minutos
RPO_MAXIMUM_TOLERABLE=60 minutos
RPO_STATUS=CLOSED

BUSINESS_CONTINUITY_PARAMETERS_DEFINED=TRUE
RTO_RPO_PENDING_ITEMS=0
RTO_RPO_PROJECT_BLOCKERS=0
```

## 7. Control documental

```text
DOCUMENT_VERSION=2.0
DOCUMENT_DATE=2026-09-24
DOCUMENT_STATUS=APPROVED_CLOSED
APPROVAL_SOURCE=EXPRESS_PROJECT_OWNER_AUTHORIZATION
SUPERSEDES_RTO_DOCUMENT=ACTA_RTO_INSTITUCIONAL_SGRLA_IHSS_v1_1
RTO_PENDING_INSTITUTIONAL_SIGNATURE=FALSE
RPO_PENDING_INSTITUTIONAL_SIGNATURE=FALSE
```

## 8. Registro de firmas administrativas (opcional para archivo posterior)

| Rol / unidad | Nombre | Cargo | Firma | Fecha |
| --- | --- | --- | --- | --- |
| Propietario del proyecto | Javier Mejía |  | Autorización expresa registrada | 24/09/2026 |
| Negocio / propietario funcional |  |  |  |  |
| Infraestructura |  |  |  |  |
| DBA |  |  |  |  |
| Desarrollo / GTIC |  |  |  |  |
| Asesoría Legal / Compliance (si aplica) |  |  |  |  |
