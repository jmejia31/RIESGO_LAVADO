# Despliegue

## Preparación

1. Aprobar la versión y validar el commit a desplegar.
2. Restaurar dependencias con las versiones bloqueadas.
3. Ejecutar compilaciones y pruebas.
4. Proveer configuración real fuera de Git y validar Oracle, JWT, CORS, AD, SMTP y almacenamiento de evidencias.
5. Respaldar base de datos y aplicación; acordar ventana y reversión.

## Artefactos

```powershell
dotnet publish backend/RL.API/RL.API.csproj -c Release -o publish/backend
cd frontend/rl-app
npm ci
npm run build -- --configuration production
```

Publique el contenido generado por el pipeline, no la carpeta de trabajo. Sirva el frontend por HTTPS y enrute `/api` hacia la API. Configure permisos de escritura solo para logs y almacenamiento requerido.

## Base de datos

Para esquemas existentes use exclusivamente `database/00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql` tras respaldo y revisión DBA. No use el maestro de primera instalación en producción.

## Verificación

- Comprobar salud del proceso, Swagger solo donde esté permitido y ausencia de errores de arranque.
- Validar login, perfil, permisos, rutas principales, carga/consulta de listas, evidencias y matrices.
- Confirmar conectividad Oracle, AD y SMTP sin registrar secretos.
- Revisar logs y auditoría.

## Reversión

Detener la nueva versión, restaurar el artefacto anterior y, si hubo cambios de datos, aplicar el plan DBA aprobado o restaurar el respaldo. No improvisar `DROP`, `TRUNCATE` ni borrados masivos.
