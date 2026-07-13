# Seguridad

## Reporte

No publique vulnerabilidades, credenciales ni datos institucionales en issues públicos. Repórtelos al responsable de seguridad y al mantenedor del sistema mediante el canal institucional autorizado, indicando versión, impacto y pasos de reproducción mínimos.

## Secretos y configuración

- Nunca confirmar `backend/RL.API/appsettings.json`, `.env`, tokens, certificados privados ni volcados de base de datos.
- Usar `appsettings.example.json` solo como plantilla de nombres y valores ficticios.
- Entregar secretos mediante el mecanismo aprobado del entorno de despliegue y rotarlos ante cualquier exposición.
- Revisar el diff y el historial nuevo antes de publicar una rama.

## Controles relevantes

La API usa JWT, autorización por roles/módulos, CORS configurable, auditoría y validación de tipos/tamaño/firma de evidencias. Estos controles no sustituyen TLS, mínimos privilegios Oracle/AD/SMTP, protección del almacenamiento, monitoreo ni rotación de claves en infraestructura.

Las cargas bajo `App_Data`, `Uploads` o `wwwroot/uploads` pueden contener datos sensibles y no deben incorporarse al repositorio ni copiarse a ambientes no autorizados.

## Dependencias

Las actualizaciones de paquetes deben revisarse por compatibilidad, avisos de seguridad y pruebas. No aplicar actualizaciones mayores automáticas directamente en producción.
