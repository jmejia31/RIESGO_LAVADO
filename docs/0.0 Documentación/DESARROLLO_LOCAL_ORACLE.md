# Guía de Entorno de Desarrollo Local con Oracle XE y Hardening de Seguridad

> [!IMPORTANT]
> **PERFIL DE BASE DE DATOS EXPLÍCITO**
> El entorno no determina por sí solo el destino. `Database:Profile` debe ser `PRODUCTION` o `LOCAL_XE`, y cada perfil usa únicamente su cadena nombrada. Perfil o cadena ausentes, valores desconocidos y errores de conexión fallan sin probar otro destino.
> QA, Oracle integration y E2E deben usar `LOCAL_XE`. El uso diario institucional selecciona `PRODUCTION` explícitamente; ese perfil no es un fallback de las pruebas.

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

## 3. Perfil y secretos sin versionar

Las contraseñas **NUNCA** deben guardarse en archivos rastreados por Git. Para desarrollo local puede usarse el almacén .NET User Secrets; producción debe recibir su cadena exclusivamente del secret store/environment source aprobado por la institución. No pasar contraseñas literales en comandos, terminales compartidas o logs.

### Seleccionar LOCAL_XE en la máquina local:

```powershell
cd backend/RL.API
dotnet user-secrets set "Database:Profile" "LOCAL_XE"
dotnet user-secrets set "ConnectionStrings:OracleDB_LocalXE" "<set-through-approved-secret-source>"
```

El segundo valor es la cadena local completa y debe ingresarse por el mecanismo local aprobado; no publicar el valor real en documentación, shell history o Git.

### Runtime diario institucional

Para iniciar la API local con Oracle de producción, seleccionar `Database:Profile=PRODUCTION` y suministrar `ConnectionStrings:OracleDB_Production` desde el secret store aprobado. No reutilizar el perfil `LOCAL_XE`. Si falta cualquiera de los dos valores, la API debe detener el arranque con un error de configuración sin mostrar credenciales.

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
- `Hosting environment: Development`.
- Log de perfil `DatabaseProfile=LOCAL_XE`.
- El resolver solo puede seleccionar `ConnectionStrings:OracleDB_LocalXE`; su destino debe ser localhost/127.0.0.1 con servicio XE.

### 4.3 Frontend Angular (rl-app)
```powershell
cd frontend/rl-app
npm start
```
El frontend estará escuchando en `http://localhost:4200`.

---

## 5. Verificación y Pruebas E2E

### Ejecución de pruebas unitarias de perfiles:
```powershell
dotnet test backend/RL.API.Tests/RL.API.Tests.csproj --filter "FullyQualifiedName~DatabaseEnvironmentGuardTests"
```

### Ejecución de E2E con Oracle XE:
Inicie Oracle XE, establezca `Database__Profile=LOCAL_XE` y configure la cadena local aprobada antes de levantar API y frontend:
```powershell
cd frontend/rl-app
npm run e2e -- --workers 1
```

Los tests Oracle/E2E requeridos deben ejecutarse con el perfil `LOCAL_XE`. Una indisponibilidad de XE debe fallar la ejecución; no debe cambiar a producción.
