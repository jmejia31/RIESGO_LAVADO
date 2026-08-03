# Plan de Implementación por Fases — Módulo Matrices de Riesgos

## Control del documento

| Campo | Valor |
|---|---|
| Documento | Plan de implementación por fases del modelo dinámico de Matrices de Riesgos |
| Fecha | 3 de agosto de 2026 |
| Rama obligatoria | `desarrollo` |
| Rama estable | `main` — no modificar sin autorización expresa de Javier Mejía |
| Estado | Ejecución parcial controlada; pendiente de revisión de Codex |
| Aprobador final | Javier Mejía |

## 1. Objetivo

Alinear de extremo a extremo el Módulo Matrices de Riesgos con el modelo dinámico Oracle definitivo, eliminando dependencias del modelo retirado y corrigiendo las incompatibilidades detectadas entre DDL, repositorio, servicios, contratos, frontend, reportes y pruebas.

Queda prohibido consultar, reinstalar o reintroducir:

- `RL_MR_MODELOS`
- `RL_MR_FACTORES`
- `RL_MR_VARIABLES`
- `RL_MR_ESCALAS`
- `RL_MR_CRITERIOS`

La solución deberá operar sobre las tablas dinámicas `RL_MR_*` aprobadas, especialmente `RL_MR_RIESGOS`, `RL_MR_EVALUACIONES_RIESGO`, `RL_MR_FLUJOS_EVALUACION`, `RL_MR_PROYECCIONES_EVALUACION`, `RL_MR_VERSIONES_FORMULARIO`, `RL_MR_CAMPOS_FORMULARIO`, `RL_MR_CATALOGOS`, `RL_MR_ELEMENTOS_CATALOGO`, `RL_MR_REGLAS_CALCULO` y `RL_MR_TRAZAS_CALCULO`.

## 2. Corrección metodológica obligatoria

La fórmula institucional vigente es:

```text
VRI = Frecuencia + Impacto - 1
```

Con frecuencia e impacto entre 1 y 5, el dominio de VRI es de 1 a 9. No se utilizará la fórmula multiplicativa frecuencia × impacto.

Los rangos de clasificación de nivel no se codificarán rígidamente en C#. Deberán proceder de parámetros versionados asociados a la versión publicada del formulario y a las reglas activas, con validaciones para impedir huecos, solapamientos, valores fuera del dominio 1–9 y configuraciones sin trazabilidad.

## 3. Evidencia ya publicada en `desarrollo`

| Intervención | Estado | Commit |
|---|---|---|
| Corrección de `05_ajustes_dashboard_seguridad_reportes.sql` | Ejecutada en código; pendiente de prueba Oracle real | `8dc6ac691dad8d22cba6d1434acdc666cfd51caa` |
| Contratos neutros de metodología dinámica | Creados; pendientes de integración | `4894ad469fb521ccc831f64b970f063e3d9d1a76` |
| Contratos tipados de dashboard y reportes | Creados; pendientes de integración | `a5bdf73355386a58698cc3274850890dc6c049e9` |
| Validador de alineación DDL | Creado; no puede declararse aprobado hasta corregir el repositorio | `1ba643d8ec7ba4e607517bdecfddf084dc507dcf` |

No se declara que las suites hayan sido ejecutadas después de estos commits. Tampoco se declara validación Oracle real.

---

# 4. Fases de trabajo

## Fase 1.1 — Infraestructura Oracle segura

### Alcance

1. Corregir el script `05` para SQL*Plus y Oracle 11g.
2. Mantener `PROMPT` fuera de bloques PL/SQL.
3. Exigir el argumento externo `EJECUTAR`.
4. Validar `CURRENT_SCHEMA = RIESGO_LAVADO`.
5. Detectar y listar proyecciones duplicadas.
6. Bloquear la creación de unicidad si existen duplicados no conciliados.
7. Crear idempotentemente `UQ_RL_MR_PROY_EVA`.
8. Crear idempotentemente el índice de dashboard.

### Estado

**Implementación de código realizada. Prueba Oracle real pendiente.**

### Pruebas de salida

- Sin argumento: ejecución bloqueada.
- Argumento incorrecto: ejecución bloqueada.
- Esquema incorrecto: ejecución bloqueada.
- Con `EJECUTAR`: ejecución controlada.
- Segunda ejecución: idempotente.
- Con duplicados: bloqueo y detalle de IDs.
- Sin duplicados: unicidad creada.

---

## Fase 1.2 — Alineación completa del repositorio con el DDL

### Bloqueantes actuales

El repositorio no puede continuar utilizando:

- `FLU_ESTADO_NUEVO`
- `FLU_ESTADO_ANTERIOR`
- `EVA_VRI`
- `EVA_ETP`
- `EVA_VRR`
- `EVA_FECHA_EVAL`
- `EVA_USR_EVAL`
- `PROY_ETP`

### Implementación requerida

1. **Lectura de estado:** obtener el estado actual desde el último `RL_MR_FLUJOS_EVALUACION.FLU_ESTADO`, ordenado por `FLU_FECHA DESC, FLU_ID DESC`.
2. **Creación de evaluación:** insertar exclusivamente columnas físicas existentes en `RL_MR_EVALUACIONES_RIESGO`.
3. **Primer flujo:** insertar `BORRADOR` en `RL_MR_FLUJOS_EVALUACION` dentro de la misma transacción.
4. **Proyección:** insertar todas las columnas obligatorias de `RL_MR_PROYECCIONES_EVALUACION`; no utilizar `PROY_ETP`.
5. **Actualización:** modificar datos dinámicos y control optimista de fila; actualizar la única proyección asociada.
6. **Transición:** insertar una nueva fila de flujo; no actualizar columnas de estado inexistentes.
7. **Trazas:** persistir versión y resultados de cálculo en `RL_MR_TRAZAS_CALCULO`.
8. **Atomicidad:** evaluación, flujo, proyección y traza deberán confirmar o revertir juntas cuando pertenezcan al mismo caso de uso.

### Estado

**Pendiente. Es la siguiente fase de codificación y debe revisarse con Codex antes de declararse terminada.**

### Criterios de salida

- Cero referencias ejecutables a las columnas incompatibles.
- Creación, actualización, lectura y transición compatibles con el DDL.
- Una sola proyección por evaluación.
- Rollback integral ante fallo intermedio.
- Validador `validate_matrices_dynamic_ddl_alignment.ps1` aprobado.

---

## Fase 1.3 — Contratos neutros y retiro conceptual del modelo anterior

### Implementación requerida

Integrar y completar contratos basados en:

- `MetodologiaFormularioDto`
- `SeccionFormularioDto`
- `CampoFormularioDto`
- `CatalogoMatricesDto`
- `ReglaCalculoMatricesDto`
- `RiesgoReporteFilaDto`
- `ReporteMatricesPaginadoDto`
- `MatrizRiesgoDashboardDinamicoDto`

Retirar de los contratos funcionales nuevos:

- `ModeloId`
- `ModeloVersion`
- `FactorId`
- `VariableId`
- `FactorInstitucionalDto`
- `VariableMetodologiaRespuestaDto`
- `PorFactor`
- `List<Dictionary<string, object>>` como contrato público

`JsonElement` se reservará para las partes realmente dinámicas del formulario, no para filas ejecutivas, totales, filtros ni paginación.

### Estado

**DTOs base creados; integración pendiente.**

---

## Fase 1.4 — Metodología dinámica y reglas versionadas

### Implementación requerida

Reconstruir `ObtenerMetodologiaVigenteAsync` utilizando exclusivamente:

- versión `PUBLISHED` y `VER_VIGENTE = 1`;
- definición `VER_JSON`;
- campos canónicos de `RL_MR_CAMPOS_FORMULARIO`;
- catálogos activos y sus elementos;
- reglas activas y versión de algoritmo;
- parámetros de clasificación asociados a la versión publicada.

### Validaciones obligatorias

- Frecuencia e impacto dentro de 1–5.
- VRI y VRR dentro de 1–9.
- Rangos completos sin huecos ni solapamientos.
- Código y versión de regla existentes y activos.
- Algoritmo permitido por el backend.
- Trazabilidad del algoritmo y parámetros utilizados.
- Prohibición del tipo dinámico `archivo` en nuevas publicaciones.

### Decisiones pendientes para aprobación funcional

1. Ubicación definitiva de los parámetros de clasificación.
2. Intervalos exactos de Bajo, Moderado, Alto y Crítico.
3. Uso de `ELE_CODIGO`, `ELE_ORDEN` u otro atributo como valor numérico del catálogo.
4. Política cuando una versión publicada referencia una regla inactiva.

### Estado

**Pendiente. No se aceptarán rangos rígidos en C#.**

---

## Fase 1.5 — Evaluación oficial, evaluación operativa y consultas Oracle

### Evaluación oficial vigente

Última evaluación activa cuyo último flujo sea `APROBADA`. Se utilizará para dashboard, Matriz Consolidada, PDF y Excel oficiales.

### Evaluación operativa actual

Última evaluación activa por fecha e ID cuyo último flujo no sea `RECHAZADA` ni `CERRADA`. Se utilizará en bandejas de captura, revisión y seguimiento.

### Riesgos sin aprobación

No se sustituirá una evaluación oficial con un borrador. Los riesgos sin evaluación aprobada se clasificarán como `Sin evaluación oficial` y se contabilizarán separadamente.

### Consultas requeridas

- listado operativo paginado;
- total real con los mismos filtros;
- dashboard ejecutivo;
- mapa 5×5;
- riesgos sin evaluación oficial;
- evaluaciones pendientes;
- reporte consolidado;
- ficha por `evaluacionId`.

Todos los filtros, conteos, agrupaciones y paginación se resolverán en Oracle 11g mediante SQL parametrizado. Se utilizarán `ROWNUM` y funciones analíticas compatibles.

### Estado

**Pendiente.**

---

## Fase 1.6 — Endpoints, reportes, exportaciones y auditoría

### Endpoints canónicos

- `GET /api/matrices-riesgos/evaluaciones`
- `GET /api/matrices-riesgos/dashboard`
- `GET /api/matrices-riesgos/reportes`
- `GET /api/matrices-riesgos/reportes/exportar`
- `GET /api/matrices-riesgos/evaluaciones/{evaluacionId}/reportes/ficha`

`GET /api/matrices-riesgos` podrá conservarse temporalmente como alias de compatibilidad, con retiro documentado.

### Exportaciones

- PDF y Excel deberán contener la misma información funcional.
- Máximo provisional: 10,000 registros, sujeto a aprobación funcional.
- Aplicar `CancellationToken`, timeout y validación de tamaño.
- No cargar registros ilimitados en memoria.
- Registrar exportaciones con el método real `IAuditoriaRepository.RegistrarAsync`.

### Auditoría

Debe registrar:

- entidad `MATRICES_RIESGOS_REPORTE`;
- acción `EXPORTAR` o `DESCARGAR_FICHA`;
- formato;
- filtros técnicos;
- total exportado;
- usuario;
- correo;
- IP;
- resultado;
- identificador de correlación cuando el contrato institucional lo permita.

No se utilizará un `AUD_EVALUACION_ID` ficticio para reportes globales.

### Estado

**Pendiente.**

---

## Fase 1.7 — Frontend Angular y accesibilidad

### Alcance

1. Retirar `Archivo adjunto` del diseñador.
2. Rechazar en backend nuevas plantillas que contengan `tipo = archivo`.
3. Abrir versiones históricas con ese tipo en modo de solo lectura.
4. Mantener evidencias exclusivamente mediante `RL_MR_EVI_*`.
5. Sustituir textos visibles `Editar JSON` y `Editor de Esquema JSON`.
6. Mantener JSON en contratos, persistencia y documentación técnica.
7. Compactar el mapa con celdas mínimas de 44 × 44 px.
8. Mantener texto secundario mínimo de 10 px, preferiblemente 11 px.
9. Actualizar modelos TypeScript a contratos neutros y tipados.

### Estado

**Pendiente.**

---

## Fase 1.8 — Seguridad y autorización HTTP real

### Alcance

1. Confirmar el claim real de rol emitido por autenticación.
2. Centralizar únicamente roles comprobados.
3. Mantener `[ModuloAuthorize(10)]`.
4. Separar pruebas unitarias de integración HTTP.

### Casos mínimos

- rol autorizado + módulo 10 → 200;
- rol común + módulo 10 → 403;
- rol autorizado sin módulo 10 → 403;
- sin token → 401;
- token vencido → 401;
- claim de módulos vacío o inválido → 403.

### Estado

**Pendiente.**

---

## Fase 1.9 — Certificación integral

### Pruebas Oracle reales

- protección e idempotencia del script 05;
- creación y actualización de evaluación;
- transición de estado;
- consulta del último `FLU_ESTADO`;
- unicidad de `PROY_EVALUACION_ID`;
- rollback transaccional;
- metodología publicada y vigente;
- dashboard oficial;
- riesgo sin evaluación oficial;
- paginación y total real;
- límite y cancelación de exportación;
- auditoría de exportación.

### Suites generales

```powershell
dotnet test backend/RL.API.Tests/RL.API.Tests.csproj --configuration Release
cd frontend/rl-app
npm test -- --watch=false
npm run build
npm run e2e
```

No se reducirán umbrales de Quality Gates para conseguir aprobación.

### Estado

**Pendiente.**

---

# 5. Definition of Done

La Fase 1 solo podrá cerrarse cuando:

1. el script 05 sea válido, protegido, idempotente y probado en Oracle real;
2. no existan referencias ejecutables a columnas o tablas retiradas;
3. CRUD, flujo, proyección y trazas estén alineados con el DDL;
4. exista una proyección única por evaluación;
5. metodología, catálogos y reglas sean dinámicos y versionados;
6. no existan rangos de riesgo rígidos en C#;
7. contratos públicos sean neutros y tipados;
8. dashboard y reportes usen solo evaluaciones oficiales aprobadas;
9. riesgos no aprobados aparezcan como `Sin evaluación oficial`;
10. paginación y totales se calculen en Oracle;
11. los cinco endpoints respondan correctamente;
12. PDF y Excel conserven paridad funcional;
13. exportaciones tengan límites, cancelación y auditoría;
14. tipo `archivo` no pueda publicarse nuevamente;
15. la interfaz no muestre terminología JSON al usuario funcional;
16. el mapa cumpla el mínimo táctil de 44 px;
17. roles y módulo 10 estén certificados mediante HTTP real;
18. pruebas Oracle, backend, frontend, build y E2E estén aprobadas;
19. documentación y bitácora estén actualizadas;
20. Codex revise el resultado técnico;
21. Javier Mejía autorice el cierre;
22. `main` permanezca intacta.

## 6. Solicitud de revisión para Codex

Codex deberá revisar este plan y los commits de evidencia antes de aprobar la siguiente intervención. En particular, deberá validar:

- corrección del script 05;
- secuencia de alineación CRUD antes de dashboard;
- fórmula VRI de 1–9;
- ausencia de rangos rígidos;
- contratos neutros y tipados;
- uso de `RegistrarAsync`;
- cobertura de pruebas Oracle reales;
- separación entre evaluación oficial y operativa.

Hasta completar dicha revisión, el estado es:

```text
Ejecución parcial controlada.
Fase 1.1 implementada en código y pendiente de Oracle real.
Fases 1.2 a 1.9 pendientes.
Módulo no cerrado.
main no modificada.
```
