# Catalogo de variables y secretos QA

Estado: `IMPLEMENTED_PENDING_REVIEW`

Este catalogo contiene nombres, finalidad y estado de verificacion. No contiene valores, contrasenas, tokens, hosts, puertos efectivos, esquemas reales ni URLs internas.

## Variables no secretas

| Nombre | Finalidad | Estado |
|---|---|---|
| `RL_QA_APP_URL` | URL HTTPS del sistema QA | `NO_VERIFICADO` |
| `RL_QA_WEB_USERNAME` | Login tecnico de Javier QA | `NO_VERIFICADO` |
| `RL_QA_DB_PORT` | Puerto autorizado Oracle QA | `NO_VERIFICADO` |
| `RL_QA_DB_SERVICE` | Servicio o SID autorizado | `NO_VERIFICADO` |
| `RL_QA_DB_ALLOWED_SCHEMA` | Esquema permitido | `NO_VERIFICADO` |
| `RL_QA_DB_ALLOWED_DB_UNIQUE_NAME` | Identidad unica de la base | `NO_VERIFICADO` |
| `RL_QA_ALLOWED_MUTATION_MODULE` | Allowlist: `MATRICES_RIESGOS` | `DECLARADO_EN_CONTRATO_NO_VERIFICADO` |
| `RL_QA_DATA_MUTATIONS_ENABLED` | Interruptor de mutaciones | `false` |
| `RL_QA_EXECUTION_MODE` | Modo de ejecucion | `DRY_RUN` |

## Secretos

| Nombre | Finalidad | Estado |
|---|---|---|
| `RL_QA_WEB_PASSWORD` | Contrasena exclusiva de Javier QA | `NO_INSTALADO_NO_VERIFICADO` |
| `RL_QA_DB_READONLY_USERNAME` | Usuario Oracle dedicado de solo lectura | `NO_INSTALADO_NO_VERIFICADO` |
| `RL_QA_DB_READONLY_PASSWORD` | Credencial del usuario Oracle de solo lectura | `NO_INSTALADO_NO_VERIFICADO` |
| `RL_QA_DB_HOST` | Host Oracle si la topologia lo requiere | `NO_INSTALADO_NO_VERIFICADO` |

## Roles opcionales no requeridos

`RL_QA_ADMIN_USER`, `RL_QA_ADMIN_PASSWORD`, `RL_QA_ANALYST_USER`, `RL_QA_ANALYST_PASSWORD`, `RL_QA_AUDITOR_USER` y `RL_QA_AUDITOR_PASSWORD` no se solicitan ni se crean. Solo podrian evaluarse con autorizacion adicional y cuentas independientes.

```ini
QA_SECRET_ACCESS_GATE=BLOCKED
QA_SECRETS_INSTALLED=NOT_VERIFIED
QA_DB_TARGET_VERIFIED=NO
QA_WEB_LOGIN_VERIFIED=NO
QA_READONLY_ACCESS=NOT_VERIFIED
QA_MUTATIONS=DISABLED
QA_AUTONOMOUS_MUTATIONS=DISABLED
```

La presencia de un nombre no demuestra que exista en GitHub. Los valores deben cargarse por el propietario o mecanismo aprobado, sin argumentos visibles, historial, logs, issues, artefactos ni ChatGPT.

## Aclaración de alcance QA 2026-11-06

El propietario confirmó que el destino es una **base de desarrollo/QA en infraestructura de producción**, sin uso de clientes, y autorizó las pruebas y el borrado total de **los datos propios de Matrices de Riesgos** sin modificar otros módulos. Ver [ALCANCE_QA_DESARROLLO_AUTORIZADO.md](ALCANCE_QA_DESARROLLO_AUTORIZADO.md).

**Cuenta Oracle adicional opcional, solo para pruebas directas permitidas de Matrices:** si existe una cuenta QA de base de datos que tenga únicamente privilegios apropiados sobre objetos exclusivos de Matrices, documentar sus credenciales en GitHub como secrets `RL_QA_DB_TEST_USERNAME` y `RL_QA_DB_TEST_PASSWORD`. **No sustituir** los secrets `RL_QA_DB_READONLY_*` con una cuenta que pueda escribir. Si la cuenta Oracle actual tiene privilegios sobre otros módulos, no exponerla a un workflow mutable; usar el flujo de API QA autenticada para mutaciones o preparar una cuenta menos privilegiada.

La URL, puerto, servicio, esquema y `DB_UNIQUE_NAME` son sus **valores técnicos reales**, no el nombre amigable “RIESGOS LAVADO”. `RL_QA_ALLOWED_MUTATION_MODULE=MATRICES_RIESGOS`, `RL_QA_DATA_MUTATIONS_ENABLED=false` y `RL_QA_EXECUTION_MODE=DRY_RUN` mientras el preflight técnico no esté validado. Los nombres no son evidencia de instalación de secretos.

**Seguridad:** `desarrollo` no está protegida; los secrets no deben ser consumidos por workflows de código mutable antes de revisar una ruta de ejecución confiable. No reutilizar credenciales de producción operativa.
