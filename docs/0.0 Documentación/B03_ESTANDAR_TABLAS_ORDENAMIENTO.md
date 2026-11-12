# B03 - Estandar institucional de tablas y ordenamiento

## Estado de esta intervencion

`SOURCE_CODE_SHA=4dc8bc9921cc5d55bd07f8c0bcfdeaabec4d0484`
`EVIDENCE_COMMIT_SHA=4dc8bc9921cc5d55bd07f8c0bcfdeaabec4d0484`
`GITHUB_EVIDENCE_INDEX_URL=https://github.com/jmejia31/RIESGO_LAVADO/blob/desarrollo/docs/0.0%20Documentaci%C3%B3n/B03_ESTANDAR_TABLAS_ORDENAMIENTO.md`
`GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/23`
`GITHUB_REMOTE_VERIFIED=YES`
`SECURITY_SCAN=NOT_RUN`
`REVIEW_READY=NO`

Esta actualización extiende el contrato reutilizable de encabezados ordenables a Usuarios, Monitoreo de Listas y los resúmenes/detalles paginados de Coincidencias. El inventario exhaustivo local detectó `23` tablas/listados y `14` superficies de KPI candidatas; B03 permanece `PARTIAL` porque aún faltan las superficies no paginadas o de dominio que requieren revisión individual y la verificación visual con datos autorizados.

## Cierre heredado de B02

La Issue #36 de GitHub registra la aceptacion visual del propietario:

```ini
B02_OWNER_VISUAL_ACCEPTANCE=PASS
B02_TECHNICAL_LOCAL=PASS_REPORTED
B02_FINAL_STATUS=APPROVED_BY_OWNER
B02_CLOSED=YES
B03_IMPLEMENTATION_AUTHORIZED=YES
B02_REMOTE_CI=BLOCKED_EXTERNAL_QUEUE
```

La ejecucion remota `43098519970` se conserva como bloqueada en cola; no se presenta como un Quality Gate exitoso.

## Contrato compartido

`app-sortable-table-header` es un componente standalone sin llamadas HTTP. Recibe `column`, `label`, `activeColumn`, `direction`, `disabled` y `ariaLabel`, emite `sortChange` y siempre muestra las dos flechas SVG. La flecha activa utiliza tokens del tema IHSS; la inactiva permanece visible con contraste secundario. El boton expone `aria-pressed` y `aria-sort`.

Comportamiento:

- estado inicial: identificador canonico ascendente;
- primer clic en otra columna: ASC;
- segundo clic en la misma columna: DESC;
- siguiente clic: ASC;
- cambio de pagina y filtros: conserva el criterio vigente;
- el consumidor controla la consulta y la paginacion;
- las respuestas obsoletas siguen protegidas por los contadores de solicitud existentes.

## Inventario y estado

| Modulo / listado | Identificador canonico | Paginacion | Estado B03 | Backend |
|---|---|---:|---|---|
| Matrices - Evaluaciones | `EVA_ID` | Si | `SORT_REQUIRED` -> implementado | whitelist `id`, codigo, version, estado, VRI, VRR, nivel residual, fecha |
| Matrices - Consolidado | `PROY_EVALUACION_ID` | Si | `SORT_REQUIRED` -> implementado | whitelist existente extendida con ID; desempate ASC |
| Matrices - Familias | `FAM_ID` | Si | `SORT_REQUIRED` -> implementado | whitelist `id`, codigo, nombre, estado, fecha |
| Matrices - Gestion de riesgos | `RIE_ID` | Si | `SORT_REQUIRED` -> implementado | whitelist `id`, codigo, nombre, estado |
| Familias - versiones y Form Builder | claves de version/campo | Mixta | `REQUIRES_CANONICAL_KEY_REVIEW` | no alterado en esta intervencion |
| Monitoreo de Listas | RTN / identidad / numero patronal | Si | `IMPLEMENTED_THIS_UPDATE` | whitelist por tipo, ASC/DESC, desempate ID y clave de caché con orden |
| Usuarios | `USR_ID` | Si | `IMPLEMENTED_THIS_UPDATE` | whitelist `id`, nombre, email, rol y dominio; ID ASC inicial |
| Tipo de Listas | `TIPO_LISTA_CAUTELA_ID` | No confirmado | `CLIENT_ONLY_LIST` | no alterado |
| Bitacora | `AUD_ID` | Si | `REQUIRES_BACKEND_EXTENSION` | requiere contrato de consulta sin cambiar auditoria |
| Cargar Listas | lista / fecha / cantidad | Mixta | `REQUIRES_CANONICAL_KEY_REVIEW` | no alterado; exportación y validación preservadas |
| Coincidencias patrono/empleado | fecha / reporte / DNI | Si | `IMPLEMENTED_THIS_UPDATE` | resumen y detalle con whitelist y orden antes de ROWNUM |
| Configuracion de calculo | ids propios y orden funcional | Mixta | `REQUIRES_CANONICAL_KEY_REVIEW` | no alterado |
| Reporte consolidado / preview | orden metodologico del reporte | No | `NON_SORTABLE_BY_DOMAIN` | no alterado |

No se agregan controles a menus, secuencias del Form Builder, catalogos estaticos ni acciones. La matriz distingue lo implementado de lo que requiere una extension posterior; por eso `B03_GLOBAL_INVENTORY` no se declara completo.

## Seguridad y paginacion

Los nombres de columna no se concatenan directamente desde el cliente. Cada repositorio convierte el campo recibido mediante una whitelist y la direccion solo admite `ASC` o `DESC`; cualquier valor desconocido cae a la clave canonica. El `ORDER BY` se genera antes de `ROWNUM`, de modo que el orden se aplica al conjunto filtrado completo antes de paginar y los empates se resuelven por el ID correspondiente.

No se modificaron Oracle, migraciones, auditoria, permisos, autenticacion, P06, P07, main, produccion ni el watcher.

## Verificacion local de esta intervencion

Ejecutado en `C:\RIESGO_LAVADO`:

- auditoría estática fresca: `23` tablas/listados detectados y `14` candidatos KPI; inventario documentado sin contar acciones como columnas ordenables;
- componente compartido y Matrices: evidencia heredada, no reproducida en esta intervención;
- build Angular de esta intervención: `PASS`, con warnings preexistentes de presupuesto/CommonJS;
- build .NET: compilación alcanzó el ensamblado pero terminó `BLOCKED` porque el proceso API existente bloqueó `bin/Debug/net10.0/RL.API.dll`; no se detuvo dicho proceso ni se modificó su salida;
- ordenamientos de Usuarios, Monitoreo y Coincidencias: implementados con whitelist, dirección restringida y desempate canónico;
- `B03_SILENT_REFRESH`: se preservó el contrato existente; no se agregaron overlays ni se vaciaron filas válidas durante refresh;
- `git diff --check`: pendiente sobre stage selectivo B03; las diferencias ajenas conservan sus efectos preexistentes;
- Oracle institucional: no tocado;
- Playwright visual B03: pendiente de servidor y datos autorizados;
- Quality Gate remoto: no se espera indefinidamente; la cola externa se registra sin convertirla en PASS.

## Pendientes y aceptacion

`B03_TECHNICAL_STATUS=PARTIAL`

`B03_OWNER_VISUAL_ACCEPTANCE=PENDING`

La extensión a Bitácora, Tipo de Listas, Cargar Listas, Configuración y tablas de dominio queda pendiente de revisión o implementación específica; no se declara completitud global ni aprobación del propietario.

## Actualización B03 — 2026-11-12 — COD

- `BASE_SHA=58c7270514f8c1ef601079a8c85ef26d87f736f2`; rama `desarrollo`; se preservaron todos los cambios locales ajenos y untracked P06.
- Contratos añadidos: `orden` y `direccion` en consultas de usuarios, monitoreo y coincidencias. Los repositorios usan whitelists cerradas, ASC/DESC explícito y desempate por clave canónica antes de `ROWNUM`; el orden forma parte de la clave de caché de monitoreo.
- Cabeceras compartidas integradas en Usuarios, Monitoreo (jurídicas/naturales/empleados) y Coincidencias patrono/empleado (resumen y detalle). Las columnas de acciones permanecen no ordenables.
- `ANGULAR_BUILD=PASS`; `DOTNET_BUILD=BLOCKED_EXISTING_API_PROCESS_LOCK`; no se ejecutaron Oracle, Playwright, E2E ni suite completa de regresión en esta intervención.
- `GITHUB_ACTIONS_BASE_RUN=43165972065` sobre `58c72705`: `FAILURE`, `75 PASS / 1 FAIL / 15 SKIP`; la falla única heredada está en `modal-shell-lock.spec.ts` por diferencia de color del constructor. No se presenta como resultado B03 PASS.
- `B03_GLOBAL_INVENTORY=PARTIAL`; `B03_OWNER_VISUAL_ACCEPTANCE=PENDING`; `B03_FINAL_STATUS=OPEN`; `ORACLE_TOUCHED=NO`; `MAIN_TOUCHED=NO`; `PRODUCTION_TOUCHED=NO`; `MIGRATION_54_TOUCHED=NO`.
