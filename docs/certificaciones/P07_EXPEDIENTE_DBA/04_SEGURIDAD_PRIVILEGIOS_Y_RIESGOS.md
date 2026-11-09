# P07 — Seguridad, privilegios y riesgos

## Revisión estática

La revisión se limitó a código, scripts y documentación versionada. No se accedió a Oracle, secretos, credenciales, producción ni servicios institucionales.

## Controles observados

- La resolución de perfil de la API es fail-closed: únicamente reconoce PRODUCTION y LOCAL_XE, y rechaza perfiles desconocidos o combinaciones incompatibles.
- La documentación prohíbe cadenas de conexión y secretos en archivos versionados.
- El paquete de Matrices permanece fuera de los maestros automáticos y el punto de entrada está bloqueado durante la preparación.
- El script destructivo 06 requiere autorización explícita, esquema RIESGO_LAVADO y marcador literal EJECUTAR.
- Los preflight/postflight previstos son de solo lectura; la clasificación estática debe complementarse con revisión DBA de permisos efectivos.
- El rollback de deployment está guardado y no se debe ejecutar sin autorización y respaldo validado.

## Riesgos abiertos

### R1 — Inventario incompatible

El deployment preflight espera 25 objetos, mientras que la transición documenta 17. Riesgo: validar o ejecutar un paquete contra una versión física distinta.

### R2 — Privilegios efectivos no demostrados

Los scripts muestran qué operaciones pretenden ejecutar, pero no demuestran los privilegios efectivos del usuario en el ambiente. El DBA debe aportar grants directos, roles efectivos y permisos sobre procedimientos, secuencias, triggers y auditoría.

### R3 — Segregación de funciones

La preparación no sustituye la aprobación separada del DBA, responsable técnico y propietario. La ejecución destructiva debe quedar fuera de una sesión automatizada no autorizada.

### R4 — Evidencia de backup/rollback ausente

Sin identificador de respaldo y prueba de restauración/rollback, el riesgo operativo permanece abierto.

## Evidencia requerida del DBA

- Usuario de ejecución y roles efectivos, sanitizados.
- Privilegios mínimos y justificación por operación.
- Objetos sensibles y triggers de auditoría involucrados.
- Confirmación de que no se otorgaron privilegios adicionales para producir la prueba.
- Evidencia de revisión independiente y aprobación.

## Estado

P07_SECURITY_STATIC_REVIEW=PASS

P07_EFFECTIVE_PRIVILEGES=NOT_VERIFIED

P07_DBA_SIGNOFF=PENDING

## Matriz de riesgos

| RISK_ID | ASSET | DESCRIPTION | ROOT_CAUSE | TECHNICAL_IMPACT | SEVERITY | EXISTING_CONTROL | MISSING_CONTROL | EVIDENCE | OWNER_ROLE | RECOMMENDATION | STATUS |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R01 | Release de Matrices | Inventario o release incompatible | Dos grupos de objetos no reconciliados inicialmente | Validar un paquete contra una versión equivocada | HIGH | Reconciliación estática 17+8 y preflight | Confirmación física del release | DDL 06, DDL 15, preflight 01 | Responsable técnico/DBA | Aprobar un único inventario físico | OPEN_DBA |
| R02 | Privilegios Oracle | Permisos efectivos excesivos | Grants y roles no verificados | DDL/DML fuera de alcance | HIGH | Perfiles explícitos y scripts guardados | Inventario de grants efectivos | Pendiente DBA | DBA/Seguridad | Revisar privilegios mínimos | OPEN_DBA |
| R03 | Destino Oracle | Conexión hacia base incorrecta | Perfil o destino no demostrado físicamente | Afectación de otra instancia | CRITICAL | Resolver perfil fail-closed y LOCAL_XE en pruebas | Evidencia de identidad de instancia | Pendiente DBA | DBA/Operaciones | Confirmar servicio, DB_UNIQUE_NAME y sesión | OPEN_DBA |
| R04 | Dependencias | FK, índice o constraint ausente/deshabilitado | Diferencia entre fuente y esquema físico | Huérfanos, fallos de borrado o integridad | HIGH | DDL y matriz de dependencias | Consulta física de metadatos y huérfanos | Documento 02 | DBA | Ejecutar preflight autorizado | OPEN_DBA |
| R05 | Auditoría | Auditoría compartida afectada | Uso incorrecto de objetos institucionales | Pérdida de trazabilidad | CRITICAL | RL_AUDITORIA fuera del DDL 06; auditoría de aplicación | Confirmación de triggers y registros | Pendiente DBA | DBA/Auditoría | Verificar preservación sin modificar datos | OPEN_DBA |
| R06 | Respaldo | Backup no verificado | Evidencia documental sin restore | Recuperación imposible o incompleta | HIGH | Guía DBA y controles documentados | Identificador, hash y prueba de restore | Documento 03 | DBA/Backup | Probar restauración aislada | OPEN_DBA |
| R07 | Restauración | Restore no probado | No existe ejecución DBA | RPO/RTO desconocidos | HIGH | Checklist de recuperación | Medición y aceptación institucional | Documento 03 | DBA/Operaciones | Ejecutar simulacro autorizado | OPEN_DBA |
| R08 | DDL | Ejecución accidental de DDL | Script manual/destructivo fuera de control | Cambios irreversibles | CRITICAL | 06 fuera de maestros, autorización literal, preflight estático | Aprobación y ventana DBA | Manifiesto y validator | DBA/Responsable técnico | Mantener cuarentena | CONTROLLED |
| R09 | Históricos | Uso de scripts retirados | Fuentes históricas coexistentes | Objetos duplicados o modelo incorrecto | HIGH | Inventario de retirados y validadores | Confirmación física de ausencia | modelo_17_objetos.json | DBA | No ejecutar históricos | OPEN_DBA |
| R10 | Información sensible | Credenciales o datos expuestos | Logs/configuración sin sanitizar | Compromiso de secretos | CRITICAL | Escaneo estático y no publicación de secretos | Revisión del entorno custodio | Validadores | Seguridad | Mantener evidencia sanitizada | CONTROLLED |
| R11 | Automatización | Ejecución no autorizada | Watcher o cola activados indebidamente | Cambios sin aprobación | HIGH | Watcher deshabilitado y cola sin cambios | Revisión operativa antes de activar | Estado colaborativo | Propietario/Operaciones | Mantener deshabilitado | CONTROLLED |
| R12 | Esquema físico | Documentación distinta a Oracle | Solo existe evidencia versionada | Decisiones de release erróneas | HIGH | Matrices documentales y preflight | Lectura física firmada por DBA | Pendiente DBA | DBA | Comparar objeto por objeto | OPEN_DBA |

La severidad CRITICAL se reserva para riesgos que pueden afectar otra base, auditoría, secretos o cambios irreversibles; HIGH corresponde a pérdida de integridad, recuperación o alineación de release; MEDIUM/LOW no se asignan sin una evidencia adicional que los fundamente.

## Matriz de privilegios esperados

| ROLE_OR_ACCOUNT_TYPE | OBJECT_SCOPE | PERMISSION_TYPE | EXPECTED_POLICY | VERIFICATION_METHOD | ACTUAL_GRANT_STATUS | EVIDENCE |
|---|---|---|---|---|---|---|
| Usuario de aplicación | Objetos de runtime aprobados | SELECT/INSERT/UPDATE/DELETE según contrato funcional | Mínimo necesario; no asumir solo lectura | DBA: grants directos, roles y objetos | NOT_VERIFIED | PENDING_DBA |
| Usuario DBA | Esquema y metadatos del release | DDL/DML solo en ventana autorizada | Segregado y trazable | DBA: roles efectivos y auditoría | NOT_VERIFIED | PENDING_DBA |
| Cuenta de lectura | Metadatos Oracle y agregados | SELECT | No debe ejecutar DDL/DML | DBA: system/object privileges | NOT_VERIFIED | PENDING_DBA |
| Permisos de sistema | Instancia y sesión | CREATE SESSION y mínimos aprobados | Sin privilegios amplios no justificados | DBA: SESSION_PRIVS/ROLE_PRIVS | NOT_VERIFIED | PENDING_DBA |
| Procedimientos/triggers | Objetos de autenticación/auditoría | EXECUTE o activación implícita según diseño | No modificar sin aprobación | DBA: ALL_PROCEDURES/ALL_TRIGGERS | NOT_VERIFIED | PENDING_DBA |
| Auditoría compartida | RL_AUDITORIA y SEQ_RL_AUDITORIA | Acceso estrictamente contractual | Preservar trazabilidad | DBA: metadatos y pruebas controladas | NOT_VERIFIED | PENDING_DBA |

No se otorgan ni revocan permisos en P07. La diferencia entre operación SELECT y privilegios efectivos debe permanecer explícita.
