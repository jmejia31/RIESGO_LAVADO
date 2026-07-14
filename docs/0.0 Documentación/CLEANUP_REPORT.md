# Informe de limpieza integral

Fecha: 2026-07-13

Rama: `chore/limpieza-integral-segura`

Base: `main` en `03ca014`

## Alcance y criterio

Se inspeccionaron los 265 archivos rastreados, estructura Git, Angular, ASP.NET Core, Oracle, configuración y documentación. Se aplicó un criterio conservador: solo se elimina lo respaldado por búsqueda de referencias y se preservan entregables históricos o duplicados cuyo contexto documental puede ser relevante.

## Estado inicial

- Git limpio, `main` siguiendo `origin/main`.
- Backend: restauración válida usando el `NuGet.Config` del repositorio; compilación correcta a una salida aislada. La salida habitual estaba bloqueada por una instancia preexistente de la API (PID 40152).
- Frontend: Node `24.18.0`; npm instalado `11.16.0` frente a `11.12.1` declarado. La instalación inicial en el árbol de trabajo no pudo reemplazar un binario LMDB ocupado, por lo que la validación se trasladó a una copia temporal limpia.
- No existe proyecto .NET de pruebas en la solución.
- No había artefactos generados rastreados por Git.

## Cambios

### Eliminados

- `coincidencias-empleado.ts`, `coincidencias-empleado.html` y `coincidencias-empleado.css`: scaffold vacío sin imports, rutas ni referencias. La ruta usa `coincidencias-empleado.component.ts` y su plantilla.
- Logs vacíos de `tmp` y artefactos temporales creados para las validaciones aisladas.

### Modificados

- `.gitignore`: cobertura de build, publicación, resultados, caches, temporales, logs, `.env` y configuración local.
- `frontend/rl-app/src/app/app.ts`: retirada la señal `title` sin consumidores.
- `frontend/rl-app/src/app/app.spec.ts`: sustituida la expectativa obsoleta del scaffold por la presencia real de `router-outlet`.

### Documentación creada

- `README.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `SECURITY.md`, `ARCHITECTURE.md`, `DEPLOYMENT.md`, `DATABASE.md`, `API.md` y este informe.

### Dependencias

No se eliminan paquetes ni se cambian versiones: todos los paquetes declarados requieren auditoría funcional antes de una retirada. `npm ci` informó 6 vulnerabilidades transitivas (3 bajas, 1 moderada y 2 altas); no se aplicó `npm audit fix --force` porque puede introducir cambios incompatibles y debe abordarse en una rama de actualización dedicada.

## Seguridad

La configuración real `backend/RL.API/appsettings.json` permanece ignorada. La plantilla contiene solo marcadores. La búsqueda del contenido rastreado no confirmó claves privadas, tokens o contraseñas reales; referencias documentales y nombres de campos se conservaron.

## Elementos conservados por falta de evidencia

- Documentos DOCX idénticos ubicados en carpetas de cierre, módulos y archivo: pueden representar entregables canónicos en contextos diferentes.
- Scripts y evidencias SQL históricas bajo `docs`.
- Scripts `18_add_missing_comments.sql` y paquetes de matrices que no forman parte de los dos maestros generales: requieren aprobación DBA antes de reclasificarse.
- Código o dependencias sin referencias superficiales: no se elimina sin compilación, pruebas y revisión funcional específica.

## Matriz antes/después

| Componente | Estado antes | Estado después | Resultado |
|---|---|---|---|
| Frontend | Instalación local bloqueada; prueba scaffold obsoleta | Build correcto; 2/2 pruebas correctas en copia limpia | Sin regresiones detectadas |
| Backend | Restauración y compilación aislada correctas | Restore y build correctos, 0 advertencias/errores | Sin cambios funcionales |
| API | Instancia preexistente activa; rutas inventariadas | `GET /api/Configuracion/sistema` respondió HTTP 200 | Contratos sin cambios |
| Base de datos | Maestros e inventario existentes | Documentación raíz añadida; scripts intactos | Integridad conservada |
| Documentación | Sin índice raíz mínimo | Documentación técnica raíz completa | Actualizada |
| Git | Limpio en `main` | Rama `chore/limpieza-integral-segura` con commits publicados en remoto | Lista para integración a `main` tras validación final |

## Validación final

| Validación | Resultado |
|---|---|
| `npm ci` en copia limpia | Correcto; 574 paquetes instalados |
| `npm run build` | Correcto; bundle inicial 1.54 MB |
| `npm test -- --watch=false` | Correcto; 1 archivo y 2 pruebas aprobadas |
| `dotnet restore ... --configfile NuGet.Config` | Correcto usando cache aislada |
| `dotnet build backend/RL.API/RL.API.csproj --no-restore` | Correcto; 0 advertencias y 0 errores |
| Validación 2026-07-13 posterior a revisión de prompts | Frontend build correcto, pruebas Angular 2/2 correctas y backend build correcto con 0 advertencias/errores |
| `dotnet test RIESGO_LAVADO.sln` | No hay proyectos .NET de pruebas que ejecutar |
| Smoke test API | HTTP 200 en configuración pública de la instancia existente |
| Maestros Oracle | Todas las referencias `@script.sql` existen; actualización segura no contiene DDL destructivo |
| Secretos rastreados | Sin patrones de claves privadas o tokens conocidos |
| `git diff --check` | Sin errores de whitespace |

La comunicación se validó estáticamente por la URL de desarrollo `http://localhost:5043/api`, por la compilación Angular y por el smoke test HTTP de esa API. No se ejecutaron scripts contra Oracle para evitar cambios de estado; la validación fue estructural y de referencias.
