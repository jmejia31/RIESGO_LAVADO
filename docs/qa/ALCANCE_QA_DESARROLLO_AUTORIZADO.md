# P06 — Alcance QA/desarrollo autorizado por el propietario

**Fecha:** 2026-11-06. **Autoridad:** aclaración expresa del propietario `jmejia31` en la conversación de continuidad. **Estado:** `SCOPE_AUTHORIZED_EXECUTION_NOT_YET_PERFORMED`.

## Naturaleza del entorno y fin de la ambigüedad

La base de datos de **RIESGO_LAVADO** utilizada para QA está alojada en infraestructura que el propietario denomina de producción, **pero su uso es exclusivamente desarrollo y pruebas**. El propietario declara que **no la usan clientes ni usuarios operativos**. La presente autorización sustituye la presunción anterior de que cualquier prueba de escritura requiera un nuevo proceso de autorización institucional; **no convierte la base en Oracle XE descartable** ni autoriza tocar servicios externos.

**No exigir una nueva firma de DBA o QA para confirmar esta decisión de uso y alcance del propietario.** Lo imprescindible antes de ejecutar una operación irreversible es la **comprobación técnica automática del destino**, las dependencias y el perímetro. Esto evita que la cadena de conexión apunte por error a otra base.

## Alcance funcional expreso

| Superficie | Permitido | No permitido |
|---|---|---|
| Matrices de Riesgos | Pruebas reales de alta, edición, versionado, publicación, evaluación, descarga, concurrencia, rollback y limpieza. **Autorizado el borrado completo de datos propios del módulo**, luego de inventario y preflight. | Borrar datos de otros módulos, auditoría compartida protegida, objetos externos, repositorios DNP ni históricos ajenos |
| Usuarios/roles | Login con la cuenta de pruebas existente y consultas de lectura; escrituras técnicas ordinarias de sesión y auditoría propias de autenticación QA | Eliminar usuarios, reasignar roles/módulos, reset masivo, borrar usuarios existentes |
| Listas/monitoreo/DNP y demás módulos | Navegación, búsqueda, detalle y lecturas según permisos de la cuenta QA | Insertar, actualizar, borrar, purgar, recalcular o modificar sus datos |
| Oracle de desarrollo/QA | Identificación técnica del destino, comprobaciones de integridad y pruebas de Matrices dentro del perímetro confirmado | Operaciones indiscriminadas sobre todo el esquema, DDL, TRUNCATE, DROP, cascadas no verificadas, deshabilitar constraints, cambios de grant o migraciones |
| `main`/producción operativa | Ningún cambio | Despliegue, merge, borrado o modificación |

La autorización del propietario cubre **todos los registros del módulo Matrices**, incluidos los existentes antes de la sesión de QA; ya no se limita solo a fixtures creados por la misma ejecución. Esto deja sin efecto esa limitación documental anterior **únicamente para Matrices**. Para cualquier registro con dependencia ajena, el proceso debe detenerse antes de borrar y reportar el bloqueo exacto; **no** usar `ON DELETE CASCADE` sin inventario ni borrar la tabla referenciante de otro módulo.

## Ejecución del borrado completo de Matrices: compuertas técnicas, no burocráticas

1. Confirmar automáticamente y de forma no reveladora conexión al entorno esperado: host/servicio/identidad única de base/esquema, variables de contrato y revisión del SHA de pruebas. No inferir `SERVICE_NAME` de «RIESGOS LAVADO».
2. Obtener inventario y conteos por tabla exclusiva de Matrices, grafo de claves foráneas, dependencias de otras áreas, auditoría y tablas compartidas. Separar la auditoría protegida y datos externos de los registros de Matrices. No publicar datos institucionales.
3. Tomar punto de recuperación autorizado/backup del perímetro y evidencia before sanitizada. Verificar no alteración de otros módulos usando además de conteos un mecanismo de comparación de integridad adecuado.
4. Presentar un plan SQL/servicio **allowlist de objetos de Matrices verificados**, orden hijo→padre y transacción; ejecutar una simulación/preflight sin escrituras. La eliminación completa es un cambio destructivo, por lo que exige una orden manual explícita para esa ejecución, identificando SHA de pruebas y base, **aunque el permiso general de borrar Matrices ya esté concedido**.
5. Con preflight satisfactorio, efectuar exclusivamente las eliminaciones confirmadas, registrar IDs/conteos, verificar tablas de Matrices vacías, auditoría protegida preservada, integridad referencial y **otras áreas intactas**. Rollback/STOP ante discordancia. No ejecutar `DROP`, `TRUNCATE`, DDL ni apagar constraints.
6. Pruebas nuevas de Matrices, reportes PDF/Excel y recuperación pueden ejecutarse luego; el estado vacío y las exportaciones vacías se verifican con API y navegador reales.

## Credenciales y GitHub

El propietario **autoriza la configuración de secretos QA en GitHub** para este entorno de desarrollo. Esta autorización **no equivale** a que la rama `desarrollo` ya sea segura: GitHub reportó `protected=false` y los workflows de esa rama pueden modificarse. Por eso:

- Crear únicamente credenciales **específicas de QA/desarrollo**, rotables y de mínimo alcance. **Nunca** reutilizar claves de producción operativa, DNP o DBA; tampoco cuentas con acceso de escritura a otros módulos como secretos disponibles a cualquier workflow.
- Preferir `Settings > Environments > QA` para secrets cuando el plan privado los admita; de lo contrario, `Repository Secrets` **no se consideran una separación equivalente de seguridad**. Environment `QA` y una restricción `Selected branches: desarrollo` tampoco protegen frente a un workflow malicioso editado en esa misma rama.
- Mantener los secretos **sin referencias en workflows** hasta preparar una ruta de ejecución revisada, únicamente manual y con aislamiento/privilegios mínimos; usar una estación confiable separada si no existe control efectivo de cambios en workflows. No se habilita el watcher #24.
- El catálogo canónico de nombres de secretos/variables sigue siendo [CATALOGO_VARIABLES_QA.md](CATALOGO_VARIABLES_QA.md); **jamás** colocar sus valores en issues, código, logs ni chat.

## Estado objetivo

```ini
QA_ENVIRONMENT_CLASSIFICATION=DEVELOPMENT_TEST_DATABASE_ON_PRODUCTION_INFRASTRUCTURE
QA_OWNER_PERMISSION_TO_TEST_MATRICES=YES
QA_OWNER_PERMISSION_TO_PURGE_ALL_MATRICES_OWN_DATA=YES
OTHER_MODULES_MUTATIONS=FORBIDDEN
DNP_EXTERNAL_DATA_MUTATIONS=FORBIDDEN
PROTECTED_AUDIT_PRESERVATION=REQUIRED
QA_DB_TARGET_VERIFIED=NO
QA_SECRETS_INSTALLED=NOT_VERIFIED
QA_LOGIN_E2E=NOT_EXECUTED
QA_MATRICES_FULL_PURGE_EXECUTED=NO
QA_MUTATIONS=DISABLED_UNTIL_TECHNICAL_PREFLIGHT
QA_AUTONOMOUS_MUTATIONS=DISABLED
P06_REAL_QA_CERTIFICATION=NOT_EXECUTED
BLOCK10_AUTHORIZED=NO
```

La diferencia entre **autorización de alcance** y **ejecución realmente verificada** se mantiene. Las restricciones anteriores del plan documental deben interpretarse a la luz de esta autorización posterior explícita del propietario; no se permiten efectos sobre módulos vecinos.
