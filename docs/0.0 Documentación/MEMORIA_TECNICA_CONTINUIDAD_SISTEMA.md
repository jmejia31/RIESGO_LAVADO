# Memoria tecnica de continuidad del sistema

Sistema: Gestion de Riesgos de Lavado de Activos y Financiamiento del Terrorismo - IHSS  
Fecha de analisis: 2026-06-18  
Alcance: inventario tecnico, continuidad arquitectonica y preparacion del Modulo 2 - Matrices de Riesgos.  
Regla de trabajo: no modificar codigo funcional sin autorizacion expresa.

## 1. Estado arquitectonico confirmado

El sistema esta construido como una aplicacion web separada en tres capas principales:

| Capa | Evidencia | Observacion |
|---|---|---|
| Backend | `C:\RIESGO_LAVADO\backend\RL.API\RL.API.csproj` | API ASP.NET Core con `TargetFramework` `net8.0`. |
| Frontend | `C:\RIESGO_LAVADO\frontend\rl-app\package.json` | Angular standalone, dependencias Angular 21, RxJS, Tailwind, SweetAlert2, jsPDF y xlsx. |
| Base de datos | `C:\RIESGO_LAVADO\database` | Scripts SQL Oracle, objetos propios `RL_*` y consultas a objetos `DNP_IHSS.*`. |

La arquitectura real es modular por funcionalidad en frontend, por controladores/repositorios en backend y por scripts SQL incrementales en base de datos. La logica de negocio del Modulo Monitoreo de Listas esta distribuida entre componente Angular, servicio Angular, controlador API y repositorio Oracle.

## 2. Patrones que deben respetarse

### Backend

Para nuevos modulos se debe replicar el patron observado:

1. Crear o extender un controlador en `C:\RIESGO_LAVADO\backend\RL.API\Controllers`.
2. Crear o extender un repositorio en `C:\RIESGO_LAVADO\backend\RL.API\Repositories`.
3. Crear DTOs en `C:\RIESGO_LAVADO\backend\RL.API\DTOs` cuando haya contratos de entrada/salida.
4. Crear modelos en `C:\RIESGO_LAVADO\backend\RL.API\Models` solo si representan estructuras reutilizables.
5. Registrar dependencias en `C:\RIESGO_LAVADO\backend\RL.API\Program.cs` cuando se cree una nueva interfaz/repositorio/servicio.
6. Usar respuestas JSON con el patron `{ success, datos, mensaje }`.
7. Usar `OracleDbContext` para abrir conexiones y `OracleParameter` para parametros.
8. Usar `IAuditoriaRepository.RegistrarAsync` para cambios sensibles.
9. Mantener autorizacion con `[Authorize]` y roles cuando aplique.

### Frontend

Para nuevos modulos se debe replicar el patron observado:

1. Crear componente standalone en `C:\RIESGO_LAVADO\frontend\rl-app\src\app\features\admin\<modulo>`.
2. Usar `signal` y `computed` para estado local cuando el componente sea interactivo.
3. Crear o extender servicios en `C:\RIESGO_LAVADO\frontend\rl-app\src\app\core\services`.
4. Definir interfaces TypeScript cerca del servicio que consume el API, como se hace en `listas.service.ts`.
5. Registrar ruta en `C:\RIESGO_LAVADO\frontend\rl-app\src\app\app.routes.ts`.
6. Proteger ruta con `moduloGuard(<MOD_ID>)`, salvo autorizacion explicita para otro esquema.
7. Consumir `environment.apiUrl`.
8. Usar Tailwind para estilos, SweetAlert2 para confirmaciones/alertas y modales inline cuando el modulo lo requiera.
9. Mantener textos de interfaz en espanol y tono institucional.

### Base de datos

Para nuevos modulos se debe replicar el patron observado:

1. Crear script incremental numerado en `C:\RIESGO_LAVADO\database`.
2. Registrar modulo en `RL_MODULOS` con `MOD_RUTA`, `MOD_ICONO`, `MOD_SECCION` y `MOD_ACTIVO`.
3. Asignar permisos iniciales en `RL_USUARIO_MODULOS`.
4. Crear tablas propias con prefijo `RL_` cuando el schema `RIESGO_LAVADO` sea responsable.
5. Usar secuencias `SEQ_RL_*`.
6. Agregar `COMMENT ON TABLE` y `COMMENT ON COLUMN`.
7. Crear indices para llaves, busquedas y relaciones frecuentes.
8. Evitar dependencias no confirmadas sobre permisos de escritura en `DNP_IHSS.*`.

## 3. Nomenclatura detectada

| Tipo | Patron |
|---|---|
| Tablas propias | `RL_<NOMBRE>` |
| Secuencias | `SEQ_RL_<NOMBRE>` |
| Llaves primarias | `PK_RL_<NOMBRE>` |
| Llaves foraneas | `FK_<CONTEXTO>` o `FK_RL_<CONTEXTO>` |
| Checks | `CK_RL_<CONTEXTO>` |
| DTOs backend | Sufijo `Dto` |
| Servicios Angular | Sufijo `.service.ts` y clase `*Service` |
| Componentes Angular | Sufijo `.component.ts` y selector `app-*` |
| Rutas frontend | kebab-case, por ejemplo `/monitoreo-listas` |
| Metodos backend | PascalCase con sufijos `Async` en repositorios |
| Metodos frontend | camelCase descriptivo |

## 4. Modulo Monitoreo de Listas como referencia

Archivos directos y dependencias inmediatas:

| Capa | Archivo | Funcion |
|---|---|---|
| Frontend | `frontend\rl-app\src\app\features\admin\monitoreo-listas\monitoreo-listas.component.ts` | Vista principal, filtros, tablas, modales, PDF, Excel, motivos, seguimientos y evidencias. |
| Frontend | `frontend\rl-app\src\app\core\services\listas.service.ts` | Contrato HTTP del modulo de listas. |
| Frontend | `frontend\rl-app\src\app\app.routes.ts` | Ruta `/monitoreo-listas` y guard de modulo. |
| Backend | `backend\RL.API\Controllers\ListasController.cs` | Endpoints REST del modulo de listas, positivos, seguimientos, evidencia, carga y calificacion. |
| Backend | `backend\RL.API\Repositories\ListasRepository.cs` | Consultas Oracle, reglas de persistencia, auditoria y procesamiento de archivos. |
| Backend | `backend\RL.API\DTOs\CatalogoPositivosDto.cs` | DTOs de tipos de documento, tipos de lista y registro positivo. |
| Backend | `backend\RL.API\DTOs\SeguimientoDto.cs` | DTOs de seguimiento y evidencia. |
| Backend | `backend\RL.API\DTOs\ResumenListaDto.cs` | DTO de resumen de listas cargadas. |
| Backend | `backend\RL.API\DTOs\CoincidenciaPatronoDto.cs` | DTOs de resumen/detalle de coincidencias patrono/empleado. |
| Backend | `backend\RL.API\Models\ListasModels.cs` | Modelos de coincidencias juridicas, naturales, empleados y detalles. |
| Base de datos | `database\05_register_monitoreo_listas.sql` | Registro del modulo Monitoreo de Listas. |
| Base de datos | `database\09_create_detalle_evidencia.sql` | Tabla de metadatos de evidencia fisica para seguimientos. |
| Base de datos | `database\10_register_tipo_listas_module.sql` | Registro del modulo Tipo Listas. |
| Base de datos | `database\11_register_cargar_listas_module.sql` | Registro del modulo Cargar Listas. |
| Base de datos | `database\12_register_coincidencias_patrono_module.sql` | Registro del modulo Coincidencias Patrono. |
| Base de datos | `database\13_create_calificaciones_coincidencias.sql` | Tabla propia de calificaciones de coincidencias. |
| Base de datos | `database\14_register_coincidencias_empleado_module.sql` | Registro del modulo Coincidencias Empleado. |

## 5. Reglas de continuidad para nuevos modulos

1. No iniciar programacion sin confirmar requerimientos funcionales, tablas requeridas y permisos sobre objetos Oracle.
2. Crear primero o validar objetos de base de datos.
3. Registrar el modulo en `RL_MODULOS`.
4. Crear contratos DTO/backend antes de conectar la UI.
5. Implementar repositorio con consultas parametrizadas.
6. Exponer endpoints en controlador con respuestas `{ success, datos, mensaje }`.
7. Registrar dependencias en `Program.cs`.
8. Crear servicio Angular que centralice el consumo HTTP.
9. Crear componente standalone y ruta protegida con `moduloGuard`.
10. Validar permisos, auditoria, errores, estados vacios y exportaciones.
11. Documentar cualquier desviacion como decision tecnica.

## 6. Errores o riesgos que deben evitarse

| Riesgo | Motivo |
|---|---|
| Duplicar logica masiva en componentes Angular | `monitoreo-listas.component.ts` concentra UI, PDF, Excel y formularios en un archivo muy grande; conviene replicar solo si se exige continuidad estricta, pero planificar particion posterior. |
| Saltar `moduloGuard` | Algunas rutas recientes usan `canActivate: [() => true]`; esto debilita el modelo de permisos por modulo. |
| Guardar secretos en `appsettings.json` | Hay credenciales y configuraciones sensibles en texto claro; no deben copiarse ni extenderse. |
| Validar archivos solo en frontend | La evidencia valida extension y tamano en UI, pero debe reforzarse en backend. |
| No auditar cambios | El sistema ya usa `RL_AUDITORIA`; todo nuevo modulo con cambios de datos debe registrar auditoria. |
| Usar objetos `DNP_IHSS.*` sin confirmar permisos | Ya existe evidencia de limitaciones de escritura sobre `DNP_IHSS.REPORTE_COINCIDENCIAS`. |
| Crear rutas sin registro en base de datos | El menu depende de `RL_MODULOS` y `RL_USUARIO_MODULOS`. |

## 7. Preparacion del Modulo 2 - Matrices de Riesgos

El Modulo 2 debe replicar la estructura transversal de Monitoreo de Listas, pero adaptar la logica a factores, ponderaciones, puntuaciones, matrices, historicos y reportes.

### Frontend probable

| Archivo probable | Proposito |
|---|---|
| `frontend\rl-app\src\app\features\admin\matrices-riesgos\matrices-riesgos.component.ts` | Vista principal del modulo. |
| `frontend\rl-app\src\app\core\services\matrices-riesgos.service.ts` | Consumo HTTP del modulo. |
| `frontend\rl-app\src\app\app.routes.ts` | Nueva ruta `/matrices-riesgos` protegida por `moduloGuard`. |

### Backend probable

| Archivo probable | Proposito |
|---|---|
| `backend\RL.API\Controllers\MatricesRiesgosController.cs` | Endpoints del modulo. |
| `backend\RL.API\Repositories\MatricesRiesgosRepository.cs` | Consultas y persistencia Oracle. |
| `backend\RL.API\Features\MatricesRiesgos\Contracts\` | Contratos agrupados por matrices, planes de acción, evidencias y reportería. |
| `backend\RL.API\Models\MatricesRiesgosModels.cs` | Modelos si se requieren estructuras reutilizables. |
| `backend\RL.API\Program.cs` | Registro de interfaz/repositorio/servicio. |

### Base de datos probable

Objetos sugeridos, sujetos a confirmacion funcional:

| Objeto probable | Tipo | Proposito |
|---|---|---|
| `RL_MATRICES_RIESGO` | Tabla | Cabecera de matriz, version, vigencia, estado. |
| `RL_FACTORES_RIESGO` | Catalogo | Factores aplicables al IHSS. |
| `RL_VARIABLES_RIESGO` | Catalogo | Variables por factor. |
| `RL_ESCALAS_RIESGO` | Catalogo | Rangos, pesos y calificaciones. |
| `RL_EVALUACIONES_RIESGO` | Transaccional | Evaluaciones por cliente/patrono/empleado/proveedor. |
| `RL_EVALUACION_DETALLE` | Transaccional | Detalle por factor/variable. |
| `RL_MATRIZ_AUDITORIA` | Auditoria o uso de `RL_AUDITORIA` | Trazabilidad de cambios y recalculos. |

### Orden recomendado

1. Confirmar metodologia oficial de matrices, factores, pesos, rangos y formulas.
2. Definir modelo de datos Oracle y scripts incrementales.
3. Registrar modulo en `RL_MODULOS`.
4. Crear DTOs y repositorio backend.
5. Crear controlador y registrar DI.
6. Crear servicio Angular.
7. Crear vista Angular con filtros, formularios, tabla, resumen y reportes.
8. Integrar permisos, auditoria y exportaciones.
9. Validar contra datos reales y documentar evidencia.

## 8. Estado de cumplimiento del Modulo Monitoreo de Listas

Resumen de cumplimiento contra requerimientos visibles en imagenes:

| Estado | Cantidad estimada |
|---|---:|
| Completo | 4 |
| Parcial | 11 |
| No encontrado | 3 |
| No aplica | 0 |

Principales brechas:

1. Falta evidencia de filtro por rango de fechas para impresion historica.
2. Falta reporte Excel de ficha/perfil completo con historial.
3. Falta modulo o lista especifica de `Seguimiento Noticias` con estado Activo/Pasivo.
4. Falta control confirmado de eliminacion exclusiva por Seccion de Cumplimiento con motivo obligatorio.
5. Falta validacion backend robusta de tipo/tamano para evidencias cargadas.

## 9. Recomendacion final

Antes de iniciar Matrices de Riesgos, conviene cerrar o aceptar formalmente las brechas del Modulo Monitoreo de Listas. Si se inicia el Modulo 2 sin esa decision, se corre el riesgo de replicar deuda funcional, especialmente en permisos, auditoria, reportes y validaciones de archivos.
