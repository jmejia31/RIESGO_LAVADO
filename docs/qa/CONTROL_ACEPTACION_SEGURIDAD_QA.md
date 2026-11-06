# P06 — Checklist de aceptación y STOP del acceso QA

**Estado:** `PREPARATION_ONLY`. Fuente: [Decisión de seguridad](DECISION_SEGURIDAD_EJECUCION_QA.md), [Issue #25](https://github.com/jmejia31/RIESGO_LAVADO/issues/25).

| Control | Responsable | Estado verificado | Prueba necesaria, siempre sanitizada |
|---|---|---|---|
| P06-G01 — Separación GitHub/secretos | Propietario/Seguridad | `DESIGN_ONLY` | Aprobar explícitamente ruta aislada; comprobar que jobs de GitHub no reciben secretos QA |
| P06-G02 — Equipo/identidad QA aislados | Infraestructura | `NOT_VERIFIED` | Acta de ACL, ausencia de Codex/watchers y mínimo privilegio |
| P06-G03 — Paquete de tests revisado y fijado | Seguridad/revisor | `PENDING` | Código auditado, identidad/hash SHA-256, dependencias fijas, custodia |
| P06-G04 — Origen HTTPS y release autorizado | Infraestructura | `NOT_VERIFIED` | Acta de TLS/origen y release revisado, sin URL interna pública |
| P06-G05 — Credenciales bajo custodia segura | Propietario/Seguridad | `NOT_VERIFIED` | Procedimiento de entrada protegida, rotación y revocación |
| P06-G06 — Identidad técnica exacta Oracle | DBA | `NO` | Comparar host/servicio/DB_UNIQUE_NAME/esquema con contrato restringido |
| P06-G07 — Usuario Oracle solo lectura | DBA | `NOT_VERIFIED` | Grants por objeto; excluir DBA, `SELECT ANY TABLE`, DML/DDL |
| P06-G08 — Login técnico Javier QA y rol efectivo | Responsable QA | `NO` | Identidad y módulos observados sin credenciales ni PII |
| P06-G09 — Primera navegación/lectura | QA | `NOT_EXECUTED` | Estado/rutas/conteos sanitizados de módulos permitidos |
| P06-G10 — E2E login/CSRF/refresh/logout | QA | `NOT_EXECUTED` | Evidencia HTTP/navegador, sin cookies/tokens |
| P06-G11 — Revisión de resultados | Propietario/revisor | `PENDING` | Firma y cadena de custodia de resultados |
| P06-G12 — Matrices con mutaciones | Propietario/DBA | `DISABLED` | Solo otra orden separada con fixtures, IDs, auditoría y rollback |

### Orden obligatorio

1. **Documental y sin credenciales:** cerrar diseño y aceptación de la ruta aislada (G01–G05). No modificar plan de GitHub ni instalar secretos.
2. **Seguridad e identidad fuera del repositorio:** custodio de red/DBA certifica equipo, HTTPS, destino, esquema y grants (G06–G07). No ejecutar DML/DDL de prueba ni leer datos si falla una identificación.
3. **QA mínimo:** login autorizado con Javier QA y lectura exclusiva (G08–G09), seguido de autenticación real (G10) solo con paquete auditado.
4. **Cierre:** evidencias sanitizadas e independientes con aprobación del propietario (G11). G12 permanece `DISABLED` hasta nueva autorización.

**STOP inmediato** ante: base o esquema ambiguos; sesión con privilegios inesperados; deploy no verificado; falla TLS; credenciales visibles; código mutable accediendo a secretos; orígenes/egress no autorizados; captura PII/JWT/cookies; cualquier tentativa de modificar datos fuera de Matrices.

```ini
P06_DOCUMENTATION_GATE=42266632011_SUCCESS
QA_SECURITY_MODEL_APPROVED_BY_OWNER=NO
QA_SECRET_ACCESS_GATE=BLOCKED
QA_DB_TARGET_VERIFIED=NO
QA_WEB_LOGIN_VERIFIED=NO
QA_AUTH_REAL_E2E=NOT_EXECUTED
QA_MUTATIONS=DISABLED
QA_AUTONOMOUS_MUTATIONS=DISABLED
P06_REAL_QA_CERTIFICATION=NOT_EXECUTED
BLOCK10_AUTHORIZED=NO
```
