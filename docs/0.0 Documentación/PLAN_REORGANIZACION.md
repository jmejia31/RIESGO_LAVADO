# Plan controlado de reorganización

## Objetivo

Migrar el repositorio hacia la estructura híbrida definida en `ESTRUCTURA_OBJETIVO.md` sin cambiar contratos REST, reglas de negocio, IDs de módulos ni datos Oracle.

Cada fase debe terminar con código compilable, pruebas aprobadas, árbol Git limpio y un commit independiente en `main`.

## Fases

| Fase | Alcance | Riesgo | Estado |
|---:|---|---|---|
| 0 | Cierre técnico, seguridad, Oracle y pruebas base | Bajo | Completada en `4811290` |
| 1 | Gobierno de estructura, responsabilidades, nombres y validación del repositorio | Bajo | Completada |
| 2 | Frontend: mover modelos y servicios específicos a sus features | Medio | Completada |
| 3 | Frontend: dividir componentes grandes y preparar rutas diferidas | Medio/alto | Completada |
| 4 | Backend: crear módulos verticales comenzando por un piloto pequeño | Medio | Completada |
| 5 | Backend: dividir Listas y Matrices de Riesgos | Alto | Completada |
| 6 | Base de datos: empaquetar módulos históricos sin romper maestros | Medio | Completada |
| 7 | Limpieza final, documentación, cobertura y validación integral | Bajo | Completada |
| 8 | Calidad: cobertura reproducible y suite E2E inicial no destructiva | Medio | Completada |
| 9 | Backend: migrar Configuración a módulo vertical con pruebas de caracterización | Medio | Completada |
| 10 | Backend: migrar Auditoría como módulo vertical y capacidad transversal | Medio/alto | Completada |
| 11 | Backend: migrar Auth y Usuarios como contexto de Identidad | Alto | Completada |
| 12 | Cierre Backend: encapsular refresh tokens y eliminar carpetas heredadas | Medio | Completada |
| 13 | Calidad Frontend: guards, interceptor JWT y servicios HTTP críticos | Medio | Completada |

## Orden de migración frontend

1. `auth` como piloto para validar las reglas de `core` y `features`.
2. `usuarios`, `configuracion` y `auditoria`.
3. Funcionalidades de listas pequeñas: tipos, carga y coincidencias.
4. `monitoreo-listas`.
5. `matrices-riesgos`.

En cada módulo se mueven primero modelos y `data-access`; después se dividen componentes. No se mezclan ambos trabajos en un solo commit cuando el componente supera 500 líneas.

### Resultado de la fase 2

- Autenticación, configuración global y catálogos quedaron agrupados por responsabilidad dentro de `core`.
- Active Directory y auditoría quedaron bajo las funcionalidades de usuarios y bitácora.
- Listas y matrices de riesgos ahora poseen carpetas independientes de `data-access` y `models`.
- Se eliminaron las ubicaciones genéricas rastreadas `core/services` y `core/models`.
- No se modificaron rutas Angular, endpoints, IDs de módulo ni lógica de componentes.

### Resultado de la fase 3

- Las plantillas de monitoreo de listas, configuración y bitácora quedaron separadas de su lógica TypeScript.
- La tabla de matrices filtradas se convirtió en un componente presentacional con entradas explícitas.
- Las pantallas enrutadas usan `loadComponent`; se conservaron URLs, guards e IDs de módulo.
- El paquete inicial de producción se redujo de aproximadamente `1.25 MB` a `375 KB`; las pantallas se descargan bajo demanda.
- Se añadieron pruebas de rutas diferidas y del componente extraído, además de reglas estructurales para evitar regresiones.

## Orden de migración backend

1. `Catalogos` como piloto por su superficie reducida.
2. `Auditoria` y `Configuracion` para uniformar `Controller → Application → Persistence`.
3. `Auth` y `Usuarios`, conservando JWT y Active Directory como integraciones explícitas.
4. `Listas`, separando carga, monitoreo, coincidencias y evidencias.
5. `MatricesRiesgos`, separando cálculo puro, casos de uso, reportes y persistencia.

### Resultado de la fase 4

- `Catalogos` quedó migrado como módulo vertical piloto bajo `Features/Catalogos`.
- El controlador conserva `api/Catalogos`, autorización y contratos de respuesta existentes.
- `Application` coordina el caso de uso mediante una abstracción; `Persistence` implementa Oracle y `Domain` contiene `Modulo`.
- La composición de dependencias continúa centralizada en `Program.cs` sin crear proyectos ni servicios desplegables adicionales.
- Se añadieron pruebas unitarias del servicio y los contratos públicos del controlador.

### Resultado de la fase 5

- `Listas` quedó agrupado bajo `Features/Listas` con controlador, contratos, aplicación y persistencia.
- Carga de listas, coincidencias y evidencias conservan servicios separados dentro del límite funcional de Listas.
- `MatricesRiesgos` quedó agrupado bajo `Features/MatricesRiesgos` con contratos HTTP, orquestación, dominio de cálculo y persistencia Oracle.
- Las interfaces se separaron de sus implementaciones y el motor de cálculo puro permanece aislado de ASP.NET Core y Oracle.
- Se conservaron rutas, IDs de módulo, SQL, respuestas públicas y reglas de cálculo existentes.
- Se añadieron pruebas estructurales de rutas y autorización para ambos módulos.

### Resultado de la fase 6

- Los scripts históricos `01` a `18` permanecen en la raíz para conservar los puntos de ejecución usados por instalaciones existentes.
- `19_matrices_riesgos` quedó documentado como paquete modular activo con un único punto de entrada y orden interno explícito.
- Se añadió una validación automática del orden exacto de los maestros, includes relativos, cobertura del manifiesto y alcance completo de los paquetes.
- El flujo de actualización segura queda protegido contra `DROP TABLE`, `TRUNCATE` y `DELETE FROM`, incluyendo sus dependencias recursivas.
- `17_validate_module_ids.sql` continúa como último paso y se verifica automáticamente que sea de solo lectura.
- La validación contra Oracle confirmó en modo lectura que los módulos `2` a `10` existen, están activos y conservan las rutas esperadas.

### Resultado de la fase 7

- Se actualizó el informe de cierre con el estado real de Git, Backend, Frontend, Oracle y documentación.
- Se confirmó que Git no rastrea compilaciones, dependencias, resultados de pruebas, configuración local ni patrones de secretos de alta confianza.
- Se documentaron 10 pruebas Backend y 6 pruebas Frontend por capacidad cubierta, sin declarar porcentajes de línea no verificables.
- Se añadió una validación automática de enlaces Markdown locales y se integró al control estructural.
- El README del frontend quedó alineado con Angular 22, Node y npm declarados por el proyecto.
- Se registraron explícitamente los riesgos residuales y los módulos heredados que requieren futuras fases funcionales independientes.

### Resultado de la fase 8

- Backend genera cobertura Cobertura mediante `coverlet.collector`; Frontend genera HTML y resumen JSON mediante V8.
- Se añadieron pisos anti-regresión sobre todo `RL.API` y `src/app`, documentando claramente que la línea base todavía es baja.
- Playwright ejecuta cinco recorridos E2E de login y enrutamiento sin credenciales ni escrituras sobre la API.
- El ejecutor E2E administra el ciclo de vida de Angular en Windows y termina el servidor creado por la prueba.
- `tools/run_quality_gates.ps1` concentra pruebas, cobertura, umbrales y E2E en una sola puerta reproducible.
- La instalación de dependencias Frontend terminó con cero vulnerabilidades reportadas por npm.

### Resultado de la fase 9

- `Configuracion` quedó agrupado bajo `Features/Configuracion` con controlador, contratos, aplicación y persistencia.
- El controlador depende exclusivamente de `IConfiguracionService`; la aplicación coordina persistencia y auditoría mediante abstracciones.
- Se conservaron `api/Configuracion`, las respuestas públicas, el rol `ADMINISTRADOR`, el módulo `3`, el SQL Oracle y la escritura de imágenes.
- Se incorporaron cinco pruebas de caracterización para contratos públicos, límites HTTP y auditoría ante éxito o fallo de persistencia.
- El Backend alcanzó 15 pruebas y la cobertura global subió a 3.88% de líneas y 3.21% de ramas.
- El validador estructural exige el nuevo módulo e impide que sus archivos regresen a `Controllers`, `Repositories` o `Models` heredados.

### Resultado de la fase 10

- `Auditoria` quedó agrupado bajo `Features/Auditoria` con controlador, contratos, aplicación y persistencia.
- El controlador depende exclusivamente de `IAuditoriaService`; la clasificación de exportaciones y su registro se trasladaron a la capa de aplicación.
- `IAuditoriaRepository` permanece como puerto transversal consumido por `Auth`, `Configuracion` y `Listas`, con una única implementación Oracle propiedad del módulo.
- Se conservaron `api/Auditoria`, los módulos autorizados `4`, `5`, `7`, `8` y `9`, filtros, paginación, respuesta segura, SQL e identificación de IP.
- Se incorporaron ocho casos de caracterización; el Backend alcanzó 23 pruebas y 4.62% de líneas y 4.36% de ramas.
- El validador estructural exige el módulo e impide que sus archivos regresen a `Controllers`, `Repositories` o `DTOs` heredados.

### Resultado de la fase 11

- Auth y Usuarios quedaron agrupados bajo `Features/Identidad` con controlador, aplicación, contratos, dominio y persistencia.
- Active Directory y SMTP quedaron declarados como integraciones explícitas dentro del contexto de Identidad.
- Se conservaron `api/Auth`, los endpoints anónimos, el rol `ADMINISTRADOR`, el módulo `2`, claims JWT, vigencias, BCrypt, bloqueo y recuperación de contraseña.
- `UsuarioRepository` conserva sus consultas y comandos Oracle; solo se separó su interfaz y se actualizó el namespace.
- Cuatro pruebas de caracterización verifican permisos HTTP, perfil, JWT firmado y control de intentos; el Backend alcanzó 27 pruebas.
- La cobertura global Backend subió a 6.93% de líneas y 6.41% de ramas, con pisos anti-regresión elevados.
- `Controllers`, `Repositories` y `DTOs` ya no contienen archivos heredados; `Services` conserva únicamente infraestructura compartida aún consumida por Listas y Matrices.

### Resultado de la fase 12

- La búsqueda Oracle del propietario de un refresh token se trasladó desde `AuthService` a `IUsuarioRepository` y `UsuarioRepository`.
- La capa de aplicación coordina validación, revocación y rotación sin crear conexiones Oracle ni conocer SQL.
- Tres pruebas nuevas cubren rotación exitosa, token inexistente y revocación cuando el usuario está inactivo; el Backend alcanzó 30 pruebas.
- `ServiceResult` se movió de la carpeta heredada `Services` a `Shared/Results` sin cambiar su contrato.
- `Controllers`, `DTOs`, `Models`, `Repositories` y `Services` quedaron sin archivos; el validador rechaza genéricamente cualquier reintroducción.
- La cobertura Backend alcanzó 7.11% de líneas y 6.78% de ramas, con pisos elevados a 7.1% y 6.7%.

### Resultado de la fase 13

- Se añadieron ocho pruebas de guards para sesión, cambio obligatorio de contraseña, roles y módulos.
- El interceptor JWT quedó cubierto para adjuntar token, solicitudes anónimas, renovación ante `401` y redirección segura ante `403`.
- `AuthService` quedó probado con login, claims JWT, almacenamiento local, renovación y contratos de contraseña.
- `ConfiguracionService` valida defaults, señales, colores institucionales y resolución de imágenes; `AuditoriaService` valida paginación y filtros.
- La suite Frontend pasó de 6 a 31 pruebas en ocho archivos, sin solicitudes reales ni escrituras sobre la API.
- La cobertura Frontend alcanzó 5.85% de sentencias, 6.79% de ramas, 4.47% de funciones y 4.91% de líneas.

## Controles obligatorios por fase

```powershell
dotnet test RIESGO_LAVADO.sln --configuration Release
cd frontend/rl-app
npm.cmd run build
npm.cmd test -- --watch=false
```

Además:

- verificar `git diff --check`;
- ejecutar `tools/validate_repository_structure.ps1`;
- ejecutar `tools/validate_database_scripts.ps1` cuando se modifique `database` o sus puntos de entrada;
- comprobar que no cambien rutas HTTP ni IDs `2` a `10` salvo aprobación explícita;
- validar Oracle en modo lectura cuando el cambio toque contratos de persistencia; y
- mantener `main` como única rama local/remota, según la política solicitada para este repositorio.

## Regla de reversión

Cada fase corresponde a un commit autocontenido. Si una validación funcional falla, se corrige dentro de la fase antes de continuar. No se inicia la siguiente fase con cambios pendientes ni se mezclan reorganizaciones estructurales con nuevas funcionalidades.
