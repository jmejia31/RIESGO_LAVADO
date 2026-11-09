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
