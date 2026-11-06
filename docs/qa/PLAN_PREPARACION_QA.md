# Plan de preparacion QA institucional

Estado del documento: `IMPLEMENTED_PENDING_REVIEW`

Este documento es un diseno estatico. No representa una conexion, un login, una autorizacion de escritura ni una certificacion del ambiente QA.

## Compuertas obligatorias

| Control | Estado actual | Evidencia requerida antes de avanzar |
|---|---|---|
| Proteccion efectiva de rama y workflow | `BLOCKED_PENDING_REVIEW` | Reglas aplicables a `desarrollo` y workflow que manejaria secretos |
| `QA_SECRET_ACCESS_GATE` | `BLOCKED` | Mecanismo aprobado contra exfiltracion desde codigo no confiable |
| Secretos QA instalados | `NOT_VERIFIED` | Verificacion booleana y no reveladora en GitHub |
| Identidad exacta del destino QA | `NO` | Servicio, base, esquema y huella confirmados por infraestructura |
| Cuenta Javier QA | `NO` | Login tecnico y permisos efectivos confirmados |
| Oracle solo lectura | `NO` | Cuenta dedicada, minimo privilegio y destino exacto |
| Mutaciones autonomas | `DISABLED` | Autorizacion separada, fixtures y recuperacion |
| Pruebas autenticadas | `PENDING` | Entorno, credenciales y runner aprobados |

## Secuencia segura propuesta

1. Confirmar la revision humana de Issue #25 y la rama `desarrollo`.
2. Confirmar que el repositorio y su plan ofrecen controles efectivos para proteger secretos. Un Environment QA no equivale a revision obligatoria.
3. Registrar solo nombres y finalidad de variables y secretos en el catalogo separado.
4. Confirmar con infraestructura la identidad del destino llamado “RIESGOS LAVADO”. No inferir host, puerto, SID, servicio, esquema ni URL.
5. Validar el login tecnico “Javier QA” y sus permisos reales. El nombre visible no prueba el login ni el rol.
6. Verificar primero una cuenta Oracle dedicada de solo lectura. No usar `SYS`, `SYSTEM`, DBA ni una cuenta de aplicacion con mutacion indiscriminada.
7. Seleccionar un runner aprobado y evaluar conectividad y aislamiento.
8. Ejecutar inicialmente solo navegacion y lecturas sanitizadas en modulos distintos de Matrices de Riesgos.
9. Para Matrices, si una autorizacion posterior habilita escenarios sinteticos, etiquetar cada fixture, registrar IDs, dependencias, auditoria y limpieza selectiva.
10. Ante cualquier identidad, permiso, dependencia o resultado ambiguo, detenerse y dejar el control en `BLOCKED` o `PENDING`.

## Cuenta y permisos

La cuenta “Javier QA” se trata como una identidad pendiente de verificacion. No se asume `ADMINISTRADOR`, acceso a todos los modulos ni permiso para modificar datos.

La autorizacion de operaciones sinteticas se limita al modulo Matrices de Riesgos y no incluye usuarios, roles, configuracion, auditoria, catalogos, migraciones ni historicos.

## Base de datos y solo lectura

El destino QA descrito por Issue #25 esta en infraestructura institucional y no debe tratarse como Oracle XE descartable. Durante esta preparacion no se conecta Oracle, no se ejecuta DDL, DML, migracion, `TRUNCATE`, `DROP`, purga ni borrado masivo, y no se cambian usuarios, roles, configuracion ni datos de otros modulos.

## GitHub y secretos

El workflow existente `.github/workflows/qa-config-preflight.yml` es manual y de lectura. Esta tarea no lo modifica ni crea otro workflow. Los valores de variables y secretos no deben entrar al repositorio, issues, logs, capturas ni conversacion.

```ini
QA_SECRET_ACCESS_GATE=BLOCKED
QA_SECRETS_INSTALLED=NOT_VERIFIED
QA_DB_TARGET_VERIFIED=NO
QA_WEB_LOGIN_VERIFIED=NO
QA_READONLY_ACCESS=NOT_VERIFIED
QA_MUTATIONS=DISABLED
QA_AUTONOMOUS_MUTATIONS=DISABLED
QA_EXECUTION_MODE=DRY_RUN
```

La preparacion no autoriza P07, P08, Bloque 10 ni despliegues.
