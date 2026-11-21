# Certificacion P0 Oracle XE

## Resultado ejecutivo

La integración P0 fue corregida en la rama `desarrollo` y publicada en el commit funcional `e87fbaccb699775d343fda3c4d8f9695ceb2d337`. La intervención ajustó únicamente fixtures y pruebas de integración Oracle para evitar colisiones entre ejecuciones y respetar las columnas reales de los esquemas de prueba.

El resultado no puede certificarse todavía. El Quality Gate exacto del commit funcional continúa en ejecución en GitHub Actions. La ejecución local comprobó los escenarios focalizados disponibles, pero no sustituye la certificación en el Oracle XE limpio de CI ni la prueba Browser/API completa.

**Dictamen vigente: `BLOCKED`**

## Identificación y trazabilidad

| Elemento | Valor |
|---|---|
| Repositorio | `jmejia31/RIESGO_LAVADO` |
| Rama | `desarrollo` |
| Base de la intervención | `ec77b091fce5a08cd39a64467f998da499c3f734` |
| SHA funcional | `e87fbaccb699775d343fda3c4d8f9695ceb2d337` |
| Integración previa | `2b93ef2a7f70a8b4c5ea1248d5e97ed2abf1f6e3` |
| PR integrado | [PR 45](https://github.com/jmejia31/RIESGO_LAVADO/pull/45) |
| Quality Gate exacto | [run 44946443638](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44946443638) |
| Evento del Quality Gate | `push` a `desarrollo` |
| Estado al emitir este expediente | `in_progress` |
| Certificación Oracle XE | `PENDING` |
| Aceptación del propietario | `PENDING` |

## Cambios realizados

### Fixtures del bloque 7

`tools/Block7OracleE2EHost/Program.cs` dejó de depender de una familia fija con ID `1`. Cada ejecución crea una familia y un código únicos dentro del ámbito sintético y elimina la familia por su ID al finalizar. La siembra de usuarios sintéticos ocurre dentro del alcance protegido de limpieza, por lo que una inicialización parcial también queda sujeta al rollback controlado.

### Pruebas Oracle de bloques 6 y 7

`backend/RL.API.Tests/Features/MatricesRiesgos/InstitutionalProjectionOracleIntegrationTests.cs` dejó de usar el riesgo fijo `ROP-CUMP-59`; usa un código único por ejecución. Las inserciones de automonitoreo y vínculos de evidencia ahora declaran sus columnas explícitamente y reciben el usuario de fixture como parámetro. Esto evita depender del orden físico de columnas y evita escribir valores en campos equivocados.

No se modificaron contratos REST, reglas de negocio, motor de cálculo, `main`, Oracle institucional, producción, migraciones protegidas ni las interfaces compartidas del frontend.

## Verificación local ejecutada

| Control | Resultado real | Observación |
|---|---|---|
| `dotnet restore RIESGO_LAVADO.sln --configfile NuGet.Config` | PASS | Restauración completada |
| `dotnet build RIESGO_LAVADO.sln --configuration Release --no-restore` | PASS | 0 errores |
| Prueba Block 7 focalizada | PASS 1/1 | Rechazo de token obsoleto confirmado |
| Prueba Block 6 focalizada | PASS 1/1 | Proyección de 82 campos |
| Prueba Block 10 focalizada | PASS 1/1 | Proyección consolidada aprobada |
| Pruebas P0-F | PASS 9/9 | Sin omisiones ni fallos |
| Pruebas Block 12 focalizadas | PASS 2/4 | 30/30 transiciones y 2460/2460 campos pasaron; dos pruebas quedaron bloqueadas por el libro Excel abierto |
| Suite Block 5 aislada | PASS 22/22 | Ejecutada con copia temporal porque `Matrices de Riesgos.xlsx` estaba bloqueado por `EXCEL.EXE` |
| Suite backend completa local | 1343 PASS, 19 SKIP, 4 FAIL | Los cuatro fallos corresponden al bloqueo externo del libro Excel, no a una excepción Oracle del código modificado |
| `npm run build` | PASS | Advertencias existentes de presupuesto SCSS y CommonJS `exceljs` |
| `npm run lint` | PASS | Sin errores ESLint |
| `npm test -- --watch=false` | PASS 102 archivos / 1086 pruebas | Regresión frontend completa |
| Validador estructural | PASS | 118 rutas, 1325 archivos rastreados, 3 maestros SQL |
| Validador de scripts de base de datos | PASS | Controles de paridad, rollback y Block 6 4/4 |
| Validador de enlaces documentales | PASS | 266 Markdown y 284 enlaces locales |
| Validador de Agent Skills | PASS | 14 skills |

## Pruebas locales no equivalentes a certificación

El escenario local `OracleIntegration` general no se tomó como certificación porque el contenedor XE existente no es un esquema limpio: nueve pruebas encontraron un orden distinto en tablas de matrices y una prueba de auditoría encontró una longitud de columna menor que la esperada por el fixture. Es una limitación de la instancia local heredada, no una razón para alterar el esquema institucional.

La ejecución `tools/run_block7_oracle_e2e.sh` no se ejecutó localmente porque exige el puerto `5043`, que estaba ocupado por el backend manual del propietario. No se detuvo ni se reinició ese servicio. El script debe ejecutarse en el job de CI, que crea el Oracle XE descartable, levanta su propio host y verifica los marcadores fail-closed de limpieza.

## Oracle y seguridad

Todas las operaciones de base de datos realizadas durante esta intervención fueron contra el contenedor local descartable `rl-oracle-xe-local`. Se agregaron únicamente columnas auxiliares necesarias para reproducir P0-F en esa instancia antigua y se retiró el usuario sintético `987654` al finalizar. No se ejecutaron DDL, DML ni migraciones contra Oracle institucional o producción.

No se publicaron contraseñas, cadenas de conexión, dumps, datos personales ni logs operativos. Las pruebas conservan las validaciones de limpieza, auditoría, control de identidad, rollback y separación de capacidades existentes.

## Condiciones obligatorias de cierre

El cierre requiere que el Quality Gate de [e87fbacc](https://github.com/jmejia31/RIESGO_LAVADO/commit/e87fbaccb699775d343fda3c4d8f9695ceb2d337) concluya correctamente y que sus jobs verifiquen, en el Oracle XE descartable, `59/59` identidades, `2006/2006` cálculos, cero residuos de riesgos, dependencias, evidencias y metadatos, además de los escenarios Browser/API de bloques 7, 9 y 12. Si algún job falla o queda pendiente, el dictamen permanece `BLOCKED`.

La aceptación visual y funcional del propietario permanece pendiente. Este expediente no la infiere a partir de builds verdes ni de pruebas focalizadas.

## Estado de workspace y publicación

El commit funcional fue publicado en `origin/desarrollo`. Se preservaron los archivos no rastreados preexistentes del workspace y no se incluyeron en el commit. La bitácora y el estado colaborativo se actualizan con este expediente antes del cierre.

**Estado final de esta etapa: `INTEGRATED_NOT_CERTIFIED` / `BLOCKED`.**
