# FASE 3.1.2 — Oracle + backend administrativo de configuración de cálculo

## 1. Identificación y alcance

- **Fase:** 3.1 — Plataforma administrable de cálculo + compatibilidad Excel.
- **Subfase:** 3.1.2 — Oracle + backend administrativo de configuración de cálculo.
- **Fecha:** 2026-08-31 (UTC-6).
- **Rama:** `desarrollo`.
- **BASE_SHA:** `5cf62dc80fc22729a3ad3bb52317ff5a1f8fe60a`.
- **COMMIT_TÉCNICO:** `cb63807` (`feat(matrices): add calculation admin persistence`), publicado en `origin/desarrollo`.
- **FINAL_SHA:** se certifica en el commit documental final de esta intervención y en el cierre Git.
- **Fuente arquitectónica:** `FASE_3_1_1_ARQUITECTURA_MODELO_PERSISTENCIA.md`.

Esta subfase implementa únicamente persistencia Oracle, backend administrativo, validación estructural, lifecycle, versionado, auditoría y pruebas de Fórmulas, Funciones y Parámetros. No implementa el Function Registry runtime, la ejecución de funciones compuestas, la UI administrativa ni la carga de las 34 fórmulas.

## 2. Arquitectura heredada de 3.1.1

Se conserva una sola fuente administrativa por dominio: Fórmulas, Funciones, Parámetros, Reglas y Catálogos. Se mantiene un único Formula Engine, un único Publication Gate y la auditoría general existente. `VER_JSON`, `VER_HASH`, `EVA_VERSION_ID` y `EVA_CALCULOS_JSON` publicados continúan siendo snapshots históricos inmutables.

La persistencia nueva aprobada es:

- `RL_MR_FORMULAS` + `RL_MR_FORMULA_VERSIONES` + `RL_MR_FORMULA_USOS`.
- `RL_MR_FUNCIONES` + `RL_MR_FUNCION_VERSIONES` + `RL_MR_FUNCION_ARGUMENTOS`.
- `RL_MR_PARAMETROS_CALCULO` + `RL_MR_PARAMETRO_VERSIONES`.

No se crea grafo persistente de dependencias: `DEPENDENCY_TABLE_REQUIRED=NO`; las dependencias se derivan de la definición/AST en la evolución de 3.1.3.

## 3. Evidencia Oracle ejecutada

Transporte utilizado en todas las sesiones: `System.Diagnostics.ProcessStartInfo`, `sqlplus.exe` 11.2.0.3, `-L /nolog`, entrada UTF-8 sin BOM por `StandardInput.BaseStream.Write`, lectura concurrente de stdout/stderr mediante `ReadToEndAsync`, `ExitCode` real preservado y credenciales solo en memoria. No se imprimieron conexión, usuario, password ni data source sensible.

Scripts ejecutados en orden y con fail-closed:

1. `14_precheck_configuracion_calculo_312_solo_lectura.sql` — PASS, Oracle `11.2.0.1.0`.
2. `15_ddl_configuracion_calculo_312.sql` — `ExitCode=0`, `DDL_CONFIGURACION_CALCULO_312_APPLIED`.
3. `16_seed_funciones_nativas_312.sql` — `ExitCode=0`, `SEED_NATIVE_EXPECTED=7`.
4. `17_postcheck_configuracion_calculo_312_solo_lectura.sql` — `ExitCode=0`, `ORACLE_POSTCHECK=PASS`.

`18_recovery_configuracion_calculo_312.sql` queda preparado y documentado, pero no fue ejecutado.

El precheck registró, sin exponer CLOBs ni datos personales innecesarios, 24 versiones de formulario, 24 hashes, 14 evaluaciones con versión y resultado, 1 regla, 4 catálogos, 18 elementos y 1.292 registros en `RL_AUDITORIA`. El postcheck confirmó los mismos valores históricos.

## 4. Objetos Oracle creados

### 4.1 Tablas

Se crearon las ocho tablas autorizadas, todas `VALID`:

| Tabla | Propósito | Integridad principal |
|---|---|---|
| `RL_MR_FORMULAS` | Identidad estable y código canónico de fórmula | PK, código único, lifecycle |
| `RL_MR_FORMULA_VERSIONES` | DSL, tipo, estado, hash y autoría versionados | FK a fórmula, versión única por fórmula |
| `RL_MR_FORMULA_USOS` | Uso por versión de formulario y campo contractual | FK a versión de formulario y fórmula; uso único |
| `RL_MR_FUNCIONES` | Identidad estable de función | PK, código único, lifecycle |
| `RL_MR_FUNCION_VERSIONES` | Contrato NATIVA/COMPUESTA versionado | FK, versión única, checks cruzados |
| `RL_MR_FUNCION_ARGUMENTOS` | Argumentos del contrato de una FunctionVersion | FK, posición y código únicos |
| `RL_MR_PARAMETROS_CALCULO` | Identidad estable de parámetro | PK, código único, lifecycle |
| `RL_MR_PARAMETRO_VERSIONES` | Valor tipado y estado versionados | FK, versión única, checks de tipo/valor |

Las expresiones DSL y los cuerpos compuestos usan `CLOB`; los límites funcionales permanecen en backend y Oracle no ejecuta esos datos. Los hashes son SHA-256 hexadecimales de 64 caracteres.

### 4.2 Secuencias

Se crearon exactamente ocho secuencias Oracle 11g, sin identity columns ni triggers de generación de ID:

`SEQ_RL_MR_FORMULAS`, `SEQ_RL_MR_FORMULA_VERSIONES`, `SEQ_RL_MR_FORMULA_USOS`, `SEQ_RL_MR_FUNCIONES`, `SEQ_RL_MR_FUNCION_VERSIONES`, `SEQ_RL_MR_FUNCION_ARGUMENTOS`, `SEQ_RL_MR_PARAMETROS` y `SEQ_RL_MR_PARAMETRO_VERSIONES`.

Todos los identificadores del DDL respetan el límite de 30 caracteres de Oracle 11g. No se alteraron tablas existentes.

## 5. Seed autorizado

El seed es idempotente, no sobrescribe definiciones incompatibles y contiene exactamente siete funciones NATIVAS ya existentes en el engine certificado:

`IF`, `IFERROR`, `ROUND`, `ROUNDDOWN`, `MAX`, `MOD`, `OR`.

El postcheck confirmó `SEED_NATIVE_COUNT=7`, siete masters, siete versiones y trece argumentos, sin duplicados. `AND`, `MIN`, `LOOKUP`, funciones compuestas, parámetros de negocio y las 34 fórmulas no fueron cargados.

## 6. Modelo backend

El bounded context permanece en `backend/RL.API/Features/MatricesRiesgos/`.

- **Contracts:** `Contracts/Configuracion/CalculoConfiguracionDtos.cs` contiene DTOs tipados para Fórmula, FormulaVersion, FormulaUsage, Función, FunctionVersion, FunctionArgument, Parámetro y ParameterVersion.
- **Domain:** `Domain/CalculoConfiguracionValidation.cs` valida códigos canónicos, límites DSL, lifecycle, contratos NATIVA/COMPUESTA, aridad, argumentos, valores tipados y SHA-256.
- **Persistence:** `Persistence/ICalculoConfiguracionRepository.cs` y `CalculoConfiguracionRepository.cs` separan repositorio y Oracle. Los SQL son estáticos y parametrizados con binds; no hay SQL generado desde DSL.
- **Application:** `Application/ICalculoConfiguracionService.cs` y `CalculoConfiguracionService.cs` aplican reglas de aplicación, normalización, conflictos, not found y lifecycle.
- **API:** `CalculoConfiguracionController.cs` expone una única API administrativa bajo `api/matrices-riesgos/configuracion-calculo`.
- **DI:** `Program.cs` registra una implementación de repositorio y una de servicio para el bounded context.

No se exponen entidades de persistencia como contrato universal ni se usa `Dictionary<string, object>` como API administrativa.

## 7. Endpoints implementados

La API única cubre, con el patrón de autorización vigente del módulo:

- Fórmulas: list, detail, creación de master con versión draft inicial, nueva versión server-side, actualización de draft, versiones, usos y cambio de estado.
- Funciones: list, detail, creación, nueva versión server-side, actualización de draft, versiones, argumentos y cambio de estado.
- Parámetros: list, detail, creación, nueva versión server-side, actualización de draft, versiones y cambio de estado.

No existe `FormulaApiLegacy`, `ExcelFormulaApi`, `FunctionAdminV2` ni `CalculationConfigurationV2`. No se agregaron permisos RBAC granulares.

## 8. Lifecycle, versionado y concurrencia

- Los códigos master se normalizan a uppercase y se protegen con unique constraints.
- Las versiones son generadas por el backend; el cliente no decide el siguiente número.
- La creación de una versión bloquea el master, calcula el siguiente número dentro de la transacción y mantiene la unique constraint como defensa final.
- Las versiones `PUBLISHED` no se actualizan. Los cambios se realizan en una nueva versión.
- Los drafts se actualizan con control de concurrencia y el repositorio comprueba filas afectadas para devolver conflicto en vez de perder cambios silenciosamente.
- Operaciones multi-step de master + versión + argumentos + auditoría utilizan una transacción única.
- No existe delete físico funcional para históricos o referencias; el lifecycle usa deprecación/desactivación.

El version pinning futuro conservará la FormulaVersion, FunctionVersion, ParameterVersion y snapshot de catálogo usados por una publicación. No se resolverá nunca contra “la última versión” de una función.

## 9. Auditoría

`AUDIT_SYSTEMS=1` y `NEW_AUDIT_SYSTEMS=0`. Las operaciones administrativas reutilizan `IAuditoriaRepository`, `AuditoriaRepository` y `RL_AUDITORIA`; no se creó ninguna tabla de auditoría paralela.

Los eventos previstos son equivalentes a `CREAR_FORMULA`, `CREAR_VERSION_FORMULA`, `ACTUALIZAR_BORRADOR_FORMULA`, `DEPRECAR_FORMULA`, y sus correspondencias para Funciones y Parámetros. La auditoría se registra en la misma transacción cuando la operación de persistencia es multi-step. No se escriben credenciales, tokens, connection strings ni SQL de conexión.

## 10. Seguridad

- `DB_EXECUTABLE_CODE=0`.
- `DYNAMIC_CSHARP_FROM_DB=0`.
- `DYNAMIC_JAVASCRIPT_FROM_DB=0`.
- `DYNAMIC_SQL_FROM_FORMULAS=0`.
- `eval=0`, `new Function=0`, reflexión arbitraria=0.
- NATIVA almacena solo metadata y `HANDLER_KEY`; el handler seguro compilado corresponde a 3.1.3.
- COMPUESTA almacena DSL como dato; no se ejecuta en 3.1.2.
- Todos los valores de usuario usan bind parameters.

## 11. Reutilización y límites de subfase

Se reutilizan `RL_MR_REGLAS_CALCULO`, `RL_MR_CATALOGOS`, `RL_MR_ELEMENTOS_CATALOGO`, `RL_MR_VERSIONES_FORMULARIO`, `RL_MR_FAMILIAS_FORMULARIO`, `RL_MR_EVALUACIONES_RIESGO`, `RL_AUDITORIA`, el Formula Engine actual y el Publication Gate actual. No se crearon versiones V2, catálogos Excel, campos de formulario paralelos, un segundo motor ni un segundo gate.

Quedan correctamente diferidos a 3.1.3: Function Registry runtime, `MIN`, `AND`, `LOOKUP`, ejecución de funciones compuestas, pinning runtime completo y extensión semántica del Publication Gate. La UI y la integración del Builder corresponden a 3.1.4; carga y paridad de las 34 fórmulas corresponden a 3.1.5.

## 12. Integridad histórica

El DDL y el seed no actualizaron ninguna tabla histórica. El postcheck verificó:

- `PUBLISHED_VER_JSON_CHANGED=0`.
- `PUBLISHED_VER_HASH_CHANGED=0`.
- `HISTORICAL_EVA_VERSION_ID_CHANGED=0`.
- `HISTORICAL_EVA_CALCULOS_JSON_CHANGED=0`.
- `VER_ID_24_MUTATION=0`, `VER_ID_53_MUTATION=0`, `VER_ID_27_MUTATION=0`, `VER_ID_28_MUTATION=0`.
- `HISTORICAL_INTEGRITY=PASS`.

Los snapshots publicados se preservan; cualquier configuración futura se publicará como nueva versión y nuevas evaluaciones. La deuda histórica de catálogos de VER_ID 27/28 no se modifica.

## 13. Scripts y recovery

Directorio: `database/19_matrices_riesgos/transicion/`.

- `14_precheck_configuracion_calculo_312_solo_lectura.sql` — solo lectura.
- `15_ddl_configuracion_calculo_312.sql` — DDL idempotente de objetos nuevos.
- `16_seed_funciones_nativas_312.sql` — seed idempotente de siete funciones.
- `17_postcheck_configuracion_calculo_312_solo_lectura.sql` — integridad y histórico, solo lectura.
- `18_recovery_configuracion_calculo_312.sql` — preparado, no ejecutado; solo contempla objetos nuevos y exige autorización adicional.

No se usó `WHENEVER SQLERROR CONTINUE`, no se ejecutó recovery y no hubo `ALTER` sobre tablas existentes, `DELETE`, `DROP`, `TRUNCATE`, `GRANT` ni `REVOKE`.

## 14. Pruebas y quality gates locales

Pruebas verificadas en esta intervención:

- Backend focalizado 3.1.2: `17/17 PASS`.
- Backend completo Release: `535/535 PASS`.
- Frontend: `707/707 PASS`.
- E2E: `29/29 PASS`.
- Build backend: PASS, 0 errores.
- Build frontend: PASS.
- Lint frontend: PASS.
- Oracle postcheck: PASS, `ExitCode=0`.
- `git diff --check`: PASS antes del cierre documental.

Los warnings de analyzers y budgets reportados son preexistentes/no bloqueantes; no se deshabilitaron reglas, cobertura ni tests.

## 15. Objetos prohibidos y hard gates

| Gate | Resultado |
|---|---|
| `FORMULA_ADMIN_SOURCES` | 1 |
| `FUNCTION_ADMIN_SOURCES` | 1 |
| `PARAMETER_ADMIN_SOURCES` | 1 |
| `RULE_ADMIN_SOURCES` | 1 |
| `CATALOG_ADMIN_SOURCES` | 1 |
| `FORMULA_ENGINES` | 1 |
| `PUBLICATION_GATES` | 1 |
| `AUDIT_SYSTEMS` | 1 |
| `NEW_AUDIT_SYSTEMS` | 0 |
| `RL_MR_DEPENDENCIAS_CALCULO` | no creado |
| `RL_MR_REGLAS_CALCULO_V2` | no creado |
| auditoría paralela | no creada |
| `FormulaEngineV2` | no creado |
| `PublicationGateV2` | no creado |
| `EXCEL_FORMULAS_IMPORTED` | 0 |
| `RBAC_CHANGES` | 0 |

## 16. Recovery documentado

El recovery 18 no forma parte del camino feliz y no fue ejecutado. Si alguna vez se autoriza, deberá comprobar que cada objeto pertenece a 3.1.2, que no tiene dependencias externas ni usos funcionales posteriores y que no toca históricos. No usa `CASCADE CONSTRAINTS` a ciegas.

## 17. Git y publicación

Los tres untracked visibles preexistentes (`.vscode/`, `agosto_rest.txt` y el PDF de requisitos) quedan fuera de scope, no se stagean y no se incluyen en commits. `agosto_capturas/` no está presente y se registra como `PREEXISTING_ENVIRONMENT_DRIFT=1`, `INTERVENTION_ATTRIBUTION=NONE`, sin blocker. El cierre final certifica por separado el worktree tracked limpio. `main` no se modifica ni se publica.

## 18. Punto exacto de continuación: 3.1.3

3.1.3 queda habilitada, pero no iniciada en esta intervención. Debe continuar sobre estas tablas y contratos, implementando el registry/resolver seguro dentro del único Formula Engine, disponibilidad de handlers, evaluación de DSL compuesto, `MIN`, `AND`, `LOOKUP`, ciclos y Publication Gate DB-driven. No debe crear un segundo motor, registry paralelo, gate V2, catálogo Excel ni auditoría nueva.

## 19. Estado

`SUBFASE_3_1_2=CERRADA` al completarse los gates Oracle, backend, regresión, histórico, seguridad y Git de esta intervención. `P0=0`. `P1_PROPIOS_3_1_2=0`. El commit técnico `cb63807` fue publicado; el commit documental de cierre se certifica al final de esta intervención.

## 20. Corrección documental Oracle

Después de la aplicación inicial de comentarios, el postcheck 20 detectó correctamente que `EVA_CALCULOS_JSON` es `CLOB`; su uso directo en `COUNT` produjo `ORA-00932` y el proceso se detuvo fail-closed. El script 20 original se conserva sin reescritura.

Se incorporaron los scripts complementarios 21 y 22. El script 21 reaplicó los mismos 94 comentarios con entrada UTF-8 y `NLS_LANG=.AL32UTF8` aislado al proceso SQL*Plus. El script 22 usa `SUM(CASE WHEN EVA_CALCULOS_JSON IS NOT NULL THEN 1 ELSE 0 END)` para el conteo compatible con Oracle 11g y comprobaciones `UNISTR` para acentos.

Postcheck complementario verificado en Oracle real:

- `TABLE_COMMENTS=8/8 PASS`, `TABLE_COMMENT_TEXT_MATCH=8/8 PASS`.
- `COLUMN_COMMENTS=86/86 PASS`.
- `MISSING_TABLE_COMMENTS=0`, `MISSING_COLUMN_COMMENTS=0`.
- `CORRUPTED_TABLE_COMMENTS=0`, `CORRUPTED_COLUMN_COMMENTS=0`.
- `SPANISH_DIACRITICS=PASS`, `ORACLE_11G_POSTCHECK=PASS`, `ORA_00932=RESOLVED`.
- `STRUCTURAL_CHANGES_FROM_COMMENT_FIX=0`, `HISTORICAL_DATA_CHANGED=0` e `HISTORICAL_INTEGRITY=PASS`.

La consulta visual read-only de `USER_TAB_COMMENTS` devolvió las ocho descripciones con ortografía española correcta. No se ejecutó recovery ni se modificaron datos funcionales o históricos.

## 21. Recertificación de cierre tras correcciones dirigidas

- Fecha de recertificación: 2026-09-01 (UTC-6). Rama: `desarrollo`.
- La corrección técnica fue publicada en `99337b41f054ce83f7691d4c5441a456f9b60df8` (`fix(matrices): harden calculation admin lifecycle`). No se reabrió la arquitectura 3.1.1 ni se inició 3.1.3.
- La evidencia remota de cierre se verifica externamente en GitHub Actions sobre el commit final publicado `2996a6342aebbe231256e612db4f941d681b54b0`: run `33526999211`, `status=completed`, `conclusion=success`, `head_sha` coincidente. No se persisten estados transitorios de CI que requieran un commit documental posterior.
- `P1-312-01=RESOLVED`: el repositorio mantiene una allowlist estática completa de las ocho secuencias Oracle, incluida `SEQ_RL_MR_FORMULA_USOS`; `EXPECTED_SEQUENCES=8`, `RESOLVABLE_SEQUENCES=8`, `MISSING_SEQUENCE_MAPPINGS=0`, `FORMULA_USAGE_SEQUENCE=PASS`.
- `P1-312-02=RESOLVED`: las transiciones se validan centralmente por estado actual y destino. La matriz permite `DRAFT -> IN_REVIEW`, `IN_REVIEW -> DRAFT/APPROVED`, `APPROVED -> DRAFT/PUBLISHED`, `PUBLISHED -> RETIRED` y `RETIRED -> ARCHIVED`; `ARCHIVED` no tiene transición. `PUBLISHED_TO_DRAFT=REJECTED`, `PUBLISHED_TO_IN_REVIEW=REJECTED`, `PUBLISHED_TO_APPROVED=REJECTED`, `PUBLISHED_CONTENT_MUTATION_PATHS=0`, `IN_REVIEW_NOT_STUCK=PASS`.
- `P1-312-03=RESOLVED`: la creación de versiones bloquea con `SELECT ... FOR UPDATE` el master de fórmula, función o parámetro antes de calcular el siguiente número. `FORMULA_VERSION_MASTER_LOCK=FOR_UPDATE`, `FUNCTION_VERSION_MASTER_LOCK=FOR_UPDATE`, `PARAMETER_VERSION_MASTER_LOCK=FOR_UPDATE`. La restricción única continúa siendo defensa adicional.
- Concurrencia: las transiciones verifican identificador, estado actual y `EXPECTED_VERSION_ROW` antes de incrementar la fila; un valor obsoleto produce `CONFLICT` y no se aplica last-write-wins.
- Pruebas verificadas: focalizadas `33/33 PASS`; regresión backend `551/551 PASS`; frontend `707/707 PASS`; E2E `29/29 PASS`; lint, builds y puertas locales PASS. No se repitió Oracle en esta corrección.
- Oracle: los postchecks 17 y 22 permanecen como evidencia válida y PASS (`ORACLE_POSTCHECK=PASS`, comentarios `8/8` y `86/86`, codificación y `HISTORICAL_INTEGRITY=PASS`). El postcheck complementario 23 quedó `POSTCHECK_23_STATUS=BLOCKED_EXTERNAL_ENVIRONMENT`, `POSTCHECK_23_EXECUTED=NO`, `POSTCHECK_23_CODE_DEFECT=NO`, `POSTCHECK_23_BLOCKER=NO`, por `ORA-12546`/restricción externa de red. No se intentó nuevamente. `DDL_ADDITIONAL=0`, `DML_ADDITIONAL=0`, `RECOVERY=0`.
- Histórico: `VER_JSON`, `VER_HASH`, `EVA_VERSION_ID` y `EVA_CALCULOS_JSON` no fueron modificados; VER_ID 24, 27, 28 y 53 no presentan mutación.
- Estado del entorno: se observan tres untracked preexistentes (`.vscode/`, `agosto_rest.txt` y el PDF de requisitos). `agosto_capturas/` no está presente. Esta diferencia se clasifica como `PREEXISTING_ENVIRONMENT_DRIFT=1`, `INTERVENTION_ATTRIBUTION=NONE`; no es P0, P1, deuda, contaminación del worktree ni blocker de 3.1.2. `WORKTREE_TRACKED_CLEAN=TRUE`, `STAGED_CHANGES=0`, `UNEXPECTED_UNTRACKED=0`, `PENDING_312_FILES=0`, `SCOPE_WORKTREE_CLEAN=TRUE`.
- `P0=0`, `P1_PROPIOS_3_1_2=0`, `RBAC_CHANGES=0`, `NEW_AUDIT_SYSTEMS=0`, `DB_EXECUTABLE_CODE=0`.
- Cierre: `SUBFASE_3_1_2=CERRADA/RECERTIFICADA`; `FASE_3.1=EN PROGRESO`; `3.1.3=HABILITADA/NO INICIADA`. Function Registry runtime, MIN, AND, LOOKUP y composite runtime permanecen correctamente diferidos a 3.1.3.
