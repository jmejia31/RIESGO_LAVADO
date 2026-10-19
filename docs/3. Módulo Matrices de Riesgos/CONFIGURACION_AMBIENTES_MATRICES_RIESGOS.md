# Configuración por ambiente y drift

Solo se documentan ambientes representados por el repositorio: `LOCAL`, `DEVELOPMENT` y el contenedor `PRODUCTION` como configuración objetivo. No se publican valores secretos.

| VARIABLE_NAME | PURPOSE | REQUIRED | SOURCE | EXAMPLE_SAFE_VALUE |
|---|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | perfil ASP.NET | sí | runtime/compose | `Production` |
| `Database__Profile` | selección explícita del destino Oracle | sí | runtime/secret store | `PRODUCTION` o `LOCAL_XE` |
| `ConnectionStrings__OracleDB_Production` | conexión del perfil productivo | solo para `PRODUCTION` | secret store | `<set-through-approved-secret-source>` |
| `ConnectionStrings__OracleDB_LocalXE` | conexión del perfil QA/E2E | solo para `LOCAL_XE` | User Secrets/entorno aislado | `<set-through-approved-secret-source>` |
| `Jwt__SecretKey` | firma JWT | sí | secret store | `<secret-not-committed>` |
| `Jwt__Issuer` | issuer | sí | environment seguro | `RIESGO_LAVADO` |
| `Jwt__Audience` | audience | sí | environment seguro | `RIESGO_LAVADO_CLIENT` |
| `Cors__AllowedOrigins__0` | origen frontend | sí | environment | `http://localhost:8081` |
| `EvidenceStorage__RootPath` | ruta evidencias | sí | volumen/runtime | `/app/App_Data/Evidencias` |
| `EvidenceStorage__MaxFileSizeBytes` | límite upload | sí | configuración aprobada | `10485760` |
| `Health__OracleTimeoutSeconds` | readiness Oracle | sí | configuración | `3` |
| `Logging__LogLevel__Default` | severidad logs | sí | configuración | `Information` |
| `RL_API_URL` | smoke/API local | no | shell de pruebas | `http://localhost:5043/api` |

El frontend productivo usa `/api` y `/hubs`; no se hardcodea un host institucional. El alias Oracle real nunca se imprime en tickets junto con credenciales. `validate-config-drift.ps1` comprueba URL, ruta, nivel de log y que no se pasen secretos como alias.

`Database:Profile` es obligatorio e independiente de `ASPNETCORE_ENVIRONMENT`. No existe fallback entre perfiles ni se admite la clave genérica `ConnectionStrings__OracleDB`; una selección ausente o inválida detiene el arranque.
