# Configuración por ambiente y drift

Solo se documentan ambientes representados por el repositorio: `LOCAL`, `DEVELOPMENT` y el contenedor `PRODUCTION` como configuración objetivo. No se publican valores secretos.

| VARIABLE_NAME | PURPOSE | REQUIRED | SOURCE | EXAMPLE_SAFE_VALUE |
|---|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | perfil ASP.NET | sí | runtime/compose | `Production` |
| `ConnectionStrings__OracleDB` | conexión Oracle | sí | secret store | `User Id=<secret>;Data Source=<alias>` |
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
