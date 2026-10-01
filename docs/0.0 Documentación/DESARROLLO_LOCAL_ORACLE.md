# Guía de Entorno de Desarrollo Local con Oracle XE y Hardening de Seguridad

> [!IMPORTANT]
> **POLÍTICA DE AISLAMIENTO ESTRICTO DE DESARROLLO**
> El entorno de desarrollo (`ASPNETCORE_ENVIRONMENT=Development`) tiene prohibido por diseño conectarse a bases de datos de producción o remotas no autorizadas.
> `DatabaseEnvironmentGuard` ejecuta una política positiva fail-closed que solo admite conexiones hacia `localhost` o `127.0.0.1` y el servicio `XE`. Cualquier intento de apuntar a hosts remotos o servicios productivos (e.g. `hpprod1`) detiene el arranque de la API (`STARTUP_REFUSED`).

---

## 1. Arquitectura y Parámetros del Entorno Local

| Parámetro | Valor Local |
|---|---|
| **Host** | `127.0.0.1` o `localhost` |
| **Puerto** | `1521` |
| **Service Name** | `XE` |
| **Usuario** | `RIESGO_LAVADO` |
| **Imagen Docker Canónica** | `gvenzl/oracle-xe:11.2.0.2-faststart` |
| **Nombre Contenedor** | `rl-oracle-xe-local` |

---

## 2. Bootstrap Local Reproducible

Para crear o verificar el contenedor Oracle XE local con el esquema completo y todas las extensiones aditivas del Módulo Matrices de Riesgos (incluyendo Bloque 4 y Bloque 6), ejecute:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/setup_local_oracle.ps1
```

Este script:
1. Comprueba la existencia del contenedor `rl-oracle-xe-local`.
2. Si no existe, lo inicializa desde la imagen base `gvenzl/oracle-xe:11.2.0.2-faststart`.
3. Crea el usuario y tablespace para `RIESGO_LAVADO`.
4. Aplica los scripts canónicos de base (`database/00_EJECUCION_PRIMERA_VEZ.sql`, modelo 17 tablas y transiciones).
5. Audita y confirma la presencia de columnas requeridas por el runtime actual (`CON_EFECTIVIDAD_MONITOREO`, `PLA_RECURSOS`, `MON_OBSERVACIONES_UGR`, `RL_USUARIO_CAPACIDADES`).

---

## 3. Configuración de Secretos sin Versionar (User Secrets)

Las contraseñas **NUNCA** deben guardarse en archivos rastreados por Git (`appsettings.json`, `appsettings.Development.json`, etc.). En su lugar, el backend utiliza el almacén de secretos de usuario de .NET (`dotnet user-secrets`).

### Configurar ConnectionString en la máquina local:

```powershell
cd backend/RL.API
dotnet user-secrets set "ConnectionStrings:OracleDB" "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT=1521))(CONNECT_DATA=(SERVER=dedicated)(SERVICE_NAME=XE)));User Id=RIESGO_LAVADO;Password=<TU_CONTRASENA_LOCAL>;"
```

### Plantilla de Referencia

Consulte [`backend/RL.API/appsettings.Development.example.json`](../../backend/RL.API/appsettings.Development.example.json) para ver la configuración de Development sin secretos.

---

## 4. Ejecución del Stack Local

### 4.1 Base de Datos (Oracle XE)
```powershell
docker start rl-oracle-xe-local
```

### 4.2 Backend ASP.NET Core (RL.API)
```powershell
cd backend/RL.API
dotnet build --configuration Debug
dotnet run --no-build
```
El backend estará escuchando en `http://localhost:5043`.

Verifique que el backend arranque con:
- `Hosting environment: Development`
- Conexión exclusiva a `127.0.0.1:1521/XE`.
- `0` conexiones hacia IPs o servidores remotos.

### 4.3 Frontend Angular (rl-app)
```powershell
cd frontend/rl-app
npm start
```
El frontend estará escuchando en `http://localhost:4200`.

---

## 5. Verificación y Pruebas E2E

### Ejecución de Pruebas Unitarias del Guard:
```powershell
dotnet test backend/RL.API.Tests/RL.API.Tests.csproj --filter "FullyQualifiedName~DatabaseEnvironmentGuardTests"
```

### Ejecución de la Suite Completa de Pruebas E2E (Playwright):
Con Oracle XE, Backend y Frontend en ejecución:
```powershell
cd frontend/rl-app
npm run e2e -- --workers 1
```

Todas las 45 pruebas deben resultar en `passed` sin errores de base de datos ni `ORA-00904`.
