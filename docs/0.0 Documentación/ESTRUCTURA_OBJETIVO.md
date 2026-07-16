# Estructura objetivo del repositorio

## Decisión

El proyecto adoptará una arquitectura híbrida: organización principal por funcionalidad y separación interna por responsabilidad. Se conserva un monolito modular; no se crearán microservicios ni proyectos .NET adicionales por cada módulo.

La estructura debe permitir responder rápidamente estas preguntas:

1. ¿A qué funcionalidad pertenece el archivo?
2. ¿Qué responsabilidad técnica cumple?
3. ¿De qué capas puede depender?
4. ¿Qué prueba protege su comportamiento?

No se utilizarán carpetas numeradas como `01-domain` o `02-application`. El nombre de cada carpeta debe explicar directamente su propósito.

## Raíz del repositorio

```text
RIESGO_LAVADO/
├── backend/                 API y pruebas .NET
├── frontend/                Aplicación Angular
├── database/                Instalación, actualizaciones y validaciones Oracle
├── docs/                    Documentación técnica, funcional y evidencias
├── tools/                   Validadores y generadores del repositorio
├── README.md                Entrada principal para desarrolladores
└── RIESGO_LAVADO.sln        Solución backend y pruebas
```

No deben aparecer en la raíz archivos temporales, evidencias de ejecución, binarios, logs ni documentos técnicos sueltos. `README.md` es el único documento general que debe permanecer en la raíz.

## Backend objetivo

```text
backend/RL.API/
├── App/
│   ├── DependencyInjection/
│   └── Configuration/
├── Core/
│   ├── Security/
│   ├── Errors/
│   └── Abstractions/
├── Infrastructure/
│   ├── Database/
│   ├── ActiveDirectory/
│   ├── Email/
│   └── Storage/
├── Features/
│   ├── Auth/
│   ├── Usuarios/
│   ├── Configuracion/
│   ├── Auditoria/
│   ├── Catalogos/
│   ├── Listas/
│   └── MatricesRiesgos/
├── Middleware/
└── Program.cs
```

Cada módulo dentro de `Features` crea solamente las carpetas que necesite:

```text
Features/MatricesRiesgos/
├── MatricesRiesgosController.cs       Contrato HTTP del módulo
├── Contracts/                         Request, response y DTO públicos
│   ├── Matrices/                      Matrices, criterios y solicitudes
│   ├── PlanesAccion/                  Planes y cambios de estado
│   ├── Evidencias/                    Registro, consulta y descarga
│   └── Reporteria/                    Dashboard, reportes y exportaciones
├── Application/                       Casos de uso y validaciones funcionales
├── Domain/                            Cálculos y reglas puras sin Oracle/HTTP
└── Persistence/                       Repositorios y consultas Oracle del módulo
```

### Reglas de dependencia del backend

- `Controller` depende de `Application`, nunca directamente de Oracle.
- `Application` coordina casos de uso y depende de abstracciones de persistencia.
- `Domain` no depende de ASP.NET Core, Oracle, archivos ni servicios externos.
- `Persistence` implementa el acceso Oracle y no contiene decisiones de presentación HTTP.
- Un módulo no accede a la carpeta `Persistence` de otro módulo.
- Código compartido se mueve a `Core` únicamente cuando tiene dos o más consumidores reales.
- `Infrastructure` contiene adaptadores técnicos transversales, no reglas de negocio de un módulo.

### Convención de archivos backend

| Sufijo | Responsabilidad |
|---|---|
| `Controller.cs` | Rutas, autorización, binding y respuesta HTTP |
| `ApplicationService.cs` | Orquestación de casos de uso |
| `Service.cs` | Regla de dominio o servicio técnico bien delimitado |
| `Repository.cs` | Persistencia Oracle |
| `RequestDto.cs` | Entrada de API |
| `ResponseDto.cs` | Salida de API |
| `Dto.cs` | Transporte interno cuando request/response no aplica |
| `Tests.cs` | Pruebas del tipo o caso de uso correspondiente |

Se prefiere un tipo público principal por archivo. Los DTO pequeños pueden agruparse cuando forman un único contrato y nunca se usan por separado.

## Frontend objetivo

```text
frontend/rl-app/src/app/
├── core/
│   ├── auth/
│   ├── guards/
│   ├── interceptors/
│   ├── configuration/
│   └── http/
├── shared/
│   ├── components/
│   ├── directives/
│   ├── pipes/
│   ├── models/
│   └── utils/
├── features/
│   ├── auth/
│   ├── usuarios/
│   ├── configuracion/
│   ├── auditoria/
│   ├── listas/
│   └── matrices-riesgos/
├── app.config.ts
└── app.routes.ts
```

Estructura interna recomendada para una funcionalidad:

```text
features/matrices-riesgos/
├── pages/                  Componentes asociados a rutas
├── components/             Componentes visuales propios del módulo
├── data-access/            Servicios HTTP, adaptación de respuestas y estado remoto
├── models/                 Tipos y contratos TypeScript del módulo
└── utils/                  Funciones puras exclusivas del módulo
```

### Reglas de dependencia del frontend

- `core` contiene infraestructura global de una sola instancia; no contiene servicios de negocio de matrices o listas.
- `shared` contiene elementos reutilizables sin conocimiento de una funcionalidad concreta.
- Cada `feature` posee sus páginas, componentes, modelos y acceso HTTP.
- Un feature no importa archivos internos de otro feature.
- Si dos features necesitan el mismo contrato, se extrae una abstracción mínima a `shared/models`.
- Los componentes de página coordinan; la lógica reutilizable se extrae a componentes, servicios o funciones puras.
- Las rutas deben migrar progresivamente a carga diferida cuando la separación del módulo lo permita.

### Convención de archivos frontend

| Patrón | Responsabilidad |
|---|---|
| `*.page.ts` | Componente asociado directamente a una ruta |
| `*.component.ts` | Componente visual reutilizable dentro del alcance definido |
| `*.service.ts` | Acceso HTTP, estado o lógica inyectable |
| `*.models.ts` | Interfaces y tipos relacionados |
| `*.util.ts` | Funciones puras sin inyección |
| `*.guard.ts` | Protección de rutas |
| `*.interceptor.ts` | Política HTTP transversal |
| `*.spec.ts` | Pruebas unitarias |

## Base de datos objetivo

```text
database/
├── 00_EJECUCION_PRIMERA_VEZ.sql
├── 00_EJECUCION_ACTUALIZACIONES_SEGURAS.sql
├── 00_MANIFIESTO_SCRIPTS_APROBADOS.md
├── 01_...sql a 18_...sql           Scripts históricos activos
├── 19_matrices_riesgos/           Paquete modular activo
│   ├── 00_APLICAR_MODULO_MATRICES_RIESGOS.sql
│   ├── 01_...sql
│   └── 05_...sql
├── _experimental_no_ejecutar/
└── _utilitarios/
```

Los scripts históricos de la raíz se preservan para evitar romper instalaciones existentes. Los módulos nuevos se incorporan como paquetes numerados con un único punto de entrada llamado desde ambos maestros.

### Reglas de base de datos

- Todo script activo debe aparecer en el manifiesto y ser alcanzable desde un maestro.
- Los includes usan `@@` para resolver rutas relativas al script que los contiene.
- Cada paquete modular debe tener un punto de entrada `00_APLICAR_*.sql`.
- La actualización segura no puede ejecutar `DROP TABLE`, `TRUNCATE` ni eliminación masiva.
- El último paso es siempre una validación de solo lectura que falle explícitamente ante desalineaciones.
- Evidencias y copias de ejecución pertenecen a `docs`, no a `database`.

## Criterio para crear carpetas compartidas

No se crea una abstracción por anticipación. Un archivo se mueve a `Core` o `shared` cuando:

- tiene al menos dos consumidores reales;
- su nombre no depende de un módulo funcional;
- puede probarse sin preparar el contexto de una funcionalidad específica; y
- su extracción reduce dependencias, no solamente líneas duplicadas.

## Definición de archivo comprensible

Un archivo está correctamente ubicado cuando su ruta y su nombre permiten anticipar su propósito sin abrirlo. Además:

- contiene una responsabilidad principal;
- sus dependencias respetan las reglas anteriores;
- evita nombres genéricos como `Helpers`, `Utils` o `Service` sin contexto;
- mantiene el contrato público separado de detalles Oracle o visuales; y
- tiene una prueba cercana cuando contiene una regla funcional crítica.
