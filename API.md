# API

## Convenciones

La API expone controladores bajo `/api`. Salvo endpoints marcados anónimos, se requiere `Authorization: Bearer <token>`. Swagger se habilita en ambiente Development en `/swagger`.

## Superficies

| Base | Propósito | Acceso |
|---|---|---|
| `/api/Auth` | login, refresh, logout, perfil, contraseña y usuarios | mixto; administración requiere rol `ADMINISTRADOR` |
| `/api/Catalogos` | roles, dominios y módulos | autenticado |
| `/api/Configuracion` | configuración pública del login y administración de slides/sistema | mixto; escritura administrativa |
| `/api/Auditoria` | consulta y registro de exportaciones | autenticado |
| `/api/Listas` | listas, positivos, seguimientos, evidencias, coincidencias y cargas | autenticado y autorizado por módulo |
| `/api/matrices-riesgos` | matrices, cálculo, estados, historial, reportes y criterios | autenticado y autorizado por módulo |

Los contratos detallados se derivan de los DTO y atributos en `backend/RL.API/Controllers`; no deben duplicarse manualmente si Swagger está disponible. Cambiar rutas, payloads, códigos o nombres requiere actualizar frontend, documentación y pruebas en el mismo cambio.

## Errores y seguridad

`ErrorHandlingMiddleware` normaliza excepciones no controladas. Los endpoints aplican roles, módulos y auditoría según su sensibilidad. No incluir tokens, contraseñas, cadenas Oracle ni contenido sensible de evidencias en registros o ejemplos.
