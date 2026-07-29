# Informe de cierre integral

> [!NOTE]
> Este documento es una evidencia histórica del cierre ejecutado entre el 15 y el 16 de julio de 2026. Las cifras de pruebas, cobertura, número de ramas y archivos corresponden a ese momento. Para el estado vigente consulte [`ESTADO_COLABORACION.md`](ESTADO_COLABORACION.md) y [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

Fecha: 2026-07-15

Última revalidación histórica: 2026-07-16, estandarización integral de módulos

Rama histórica de cierre: `main`

Base de la fase 7: `9d723ce`

Auditoría final: fase 21

## Alcance

La fase final revisó el estado acumulado de la reorganización: Git, estructura de carpetas, archivos rastreados, configuración sensible, Backend, Frontend, Oracle, pruebas y documentación. El criterio fue conservador: no eliminar ni mover archivos sin evidencia de que la operación preservara contratos, reglas funcionales y trazabilidad histórica.

## Estado final de la estructura en el cierre

- Frontend organizado de forma híbrida en `core`, `features` y `shared`, con pantallas enrutadas mediante carga diferida.
- Backend con módulos verticales para `Auditoria`, `Catalogos`, `Configuracion`, `Identidad`, `Listas` y `MatricesRiesgos`; Auth, Usuarios, Active Directory y SMTP pertenecen al contexto de Identidad.
- Las carpetas heredadas por tipo `Controllers`, `DTOs`, `Models`, `Repositories`, `Services`, `Security` y `Helpers` fueron eliminadas y su reaparición quedó bloqueada por validación automática.
- Se retiraron 20 directorios físicos vacíos de código, pruebas y documentación; `.agents` se conserva por pertenecer al entorno operativo multiagente.
- Oracle mantiene los scripts históricos `01` a `18` en la raíz y los módulos nuevos como paquetes numerados con un único punto de entrada.
- Documentación técnica centralizada en `docs/0.0 Documentación` y enlazada desde el README principal.

## Limpieza y seguridad

- Ningún `bin`, `obj`, `dist`, `node_modules`, `coverage`, `TestResults`, log o caché estaba rastreado por Git en el cierre.
- `backend/RL.API/appsettings.json` y las configuraciones locales permanecían ignoradas; `appsettings.Development.json` se retiró del seguimiento sin borrar la copia local.
- La búsqueda sobre archivos rastreados no encontró patrones de alta confianza para claves privadas, tokens de proveedores o credenciales publicadas.
- NuGet y npm no reportaron dependencias con vulnerabilidades conocidas en los orígenes consultados el 2026-07-15.
- En el cierre histórico solo se mantuvo `main`. Desde el 2026-07-24 la política vigente utiliza `main` como rama estable y `desarrollo` como rama de trabajo activo.
- Los entregables históricos y documentos Word se preservaron porque forman parte de la evidencia funcional del proyecto.

## Cobertura automatizada disponible en el cierre

| Componente | Archivos de prueba | Casos históricos | Capacidades verificadas |
|---|---:|---:|---|
| Backend | 9 | 77 | Aplicación de Listas/Matrices, reactivación de planes, evidencias protegidas, cálculo de matrices, Catálogos, Configuración, Auditoría, JWT/Identidad, rotación de refresh tokens, manejo uniforme de errores y límites HTTP/autorización de módulos |
| Frontend unitario | 13 | 123 | Arranque, rutas diferidas, guards, interceptores JWT/confirmación, sesión local, configuración, bitácora, contratos HTTP, planes, reactivación, vista previa, política documental, descargas, exportaciones auditadas y edición/escrituras simuladas de Listas/Matrices |
| Frontend E2E | 1 | 6 | Login, validación local, visibilidad de contraseña, redirección y acceso autenticado autorizado a Matrices sin escrituras reales |

La fase 8 incorporó medición Cobertura para .NET y V8 para Angular, con pisos anti-regresión automatizados. La línea base histórica se documenta en `QUALITY.md`; no debe interpretarse como el conteo vigente ni como objetivo suficiente.

## Validaciones finales históricas

| Validación | Resultado auditado de cierre |
|---|---|
| `dotnet test RIESGO_LAVADO.sln --configuration Release --no-restore` | 77/77 pruebas Backend |
| `npm run build` | Build de producción; paquete inicial de 375.65 KB |
| `npm test -- --watch=false` | 123/123 pruebas Frontend |
| `npm run e2e` | 6/6 recorridos Playwright no destructivos |
| `tools/run_quality_gates.ps1` | Backend 15.35%/16.36%; Frontend 31.07%/26.97%/29.94%/31.26%; pisos aprobados |
| `tools/validate_repository_structure.ps1` | 119 rutas obligatorias y 350 archivos rastreados revisados |
| `tools/validate_database_scripts.ps1` | 19 scripts raíz, un paquete modular y 22 scripts alcanzables desde actualización segura |
| `tools/validate_documentation_links.ps1` | 18 documentos Markdown y 16 enlaces locales correctos |
| `dotnet list ... --vulnerable` | 0 paquetes Backend vulnerables reportados |
| `npm audit --audit-level=low` | 0 vulnerabilidades Frontend reportadas |
| Búsqueda de secretos de alta confianza | 0 archivos coincidentes |
| `git fsck --connectivity-only` | Conectividad correcta, sin corrupción del repositorio |
| `17_validate_module_ids.sql` en Oracle | Módulos activos `2` a `10` alineados con sus rutas |
| Smoke test `GET /api/Configuracion/sistema` | Tres respuestas consecutivas HTTP 200; un 500 inicial de la instancia preexistente no se reprodujo |
| `git diff --check` | Sin errores de whitespace |

## Riesgos residuales conocidos

- La suite E2E cubría una sesión autenticada y autorizada con API simulada, pero no operaciones que escriben en Oracle; estas requieren ambiente aislado, datos semilla y credenciales efímeras.
- Los pisos cuantitativos de cobertura reflejan una línea base histórica baja que debe incrementarse antes de migrar módulos sensibles.
- Active Directory y SMTP dependen de servicios institucionales externos; las conexiones reales requieren un ambiente de integración autorizado.
- Las dependencias no presentaron avisos conocidos en la auditoría de cierre, pero deben revisarse periódicamente porque los registros de vulnerabilidades cambian.

## Conclusión histórica

La reorganización quedó cerrada y auditada sin cambios de contratos REST, IDs de módulo, datos Oracle ni reglas de negocio. Los controles añadidos permiten detectar regresiones de estructura, seguridad, dependencias, ejecución SQL, pruebas y enlaces documentales. Las intervenciones posteriores deben documentarse en la bitácora y en el estado colaborativo vigente.
