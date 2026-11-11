# B03 - Estandar institucional de tablas y ordenamiento

## Estado de esta intervencion

`SOURCE_CODE_SHA=b9b914449cae5006d032b07e7e1061c465fef2f3`

La implementacion B03 de esta intervencion establece el contrato reutilizable de encabezados ordenables y aplica el ordenamiento server-side al nucleo de Matrices de Riesgos. El inventario global se conserva en estado `PARTIAL` hasta extender y verificar los listados administrativos restantes.

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
| Monitoreo de Listas | RTN / identidad / numero patronal | Si | `REQUIRES_BACKEND_EXTENSION` | requiere contrato especifico por entidad |
| Usuarios | `USR_ID` | Si | `REQUIRES_BACKEND_EXTENSION` | orden historico por nombre; no alterado |
| Tipo de Listas | `TIPO_LISTA_CAUTELA_ID` | No confirmado | `CLIENT_ONLY_LIST` | no alterado |
| Bitacora | `AUD_ID` | Si | `REQUIRES_BACKEND_EXTENSION` | requiere contrato de consulta sin cambiar auditoria |
| Cargar Listas y coincidencias | clave de dominio | Mixta | `NON_SORTABLE_BY_DOMAIN` / revision pendiente | no alterado |
| Configuracion de calculo | ids propios y orden funcional | Mixta | `REQUIRES_CANONICAL_KEY_REVIEW` | no alterado |
| Reporte consolidado / preview | orden metodologico del reporte | No | `NON_SORTABLE_BY_DOMAIN` | no alterado |

No se agregan controles a menus, secuencias del Form Builder, catalogos estaticos ni acciones. La matriz distingue lo implementado de lo que requiere una extension posterior; por eso `B03_GLOBAL_INVENTORY` no se declara completo.

## Seguridad y paginacion

Los nombres de columna no se concatenan directamente desde el cliente. Cada repositorio convierte el campo recibido mediante una whitelist y la direccion solo admite `ASC` o `DESC`; cualquier valor desconocido cae a la clave canonica. El `ORDER BY` se genera antes de `ROWNUM`, de modo que el orden se aplica al conjunto filtrado completo antes de paginar y los empates se resuelven por el ID correspondiente.

No se modificaron Oracle, migraciones, auditoria, permisos, autenticacion, P06, P07, main, produccion ni el watcher.

## Verificacion local de esta intervencion

Ejecutado en `C:\RIESGO_LAVADO`:

- componente compartido: `3/3 PASS`;
- regresiones Matrices/Gestion y componente compartido: `17 archivos, 274/274 PASS`;
- suite Angular completa posterior a la limpieza de encabezados: `93 archivos, 978/978 PASS`;
- build Angular: `PASS` (warnings preexistentes de presupuesto/CommonJS);
- build aislado `backend/RL.API`: `PASS`, 0 warnings, 0 errors; el proceso API existente mantuvo bloqueada la salida Debug y no fue detenido;
- ESLint frontend: `PASS`;
- se eliminaron del HTML los bloques muertos y las filas duplicadas; `B03_CORE_VISIBLE_HEADERS=PASS`;
- la carga ordinaria conserva filas validas durante la solicitud; `B03_SILENT_REFRESH=PASS` en los consumidores modificados;
- `git diff --check`: se verificara sobre el stage selectivo B03; los archivos ajenos conservan sus diferencias preexistentes;
- Oracle institucional: no tocado;
- Playwright visual B03: pendiente de servidor y datos autorizados;
- Quality Gate remoto: no se espera indefinidamente; la cola externa se registra sin convertirla en PASS.

## Pendientes y aceptacion

`B03_TECHNICAL_STATUS=PARTIAL`

`B03_OWNER_VISUAL_ACCEPTANCE=PENDING`

La extension a Usuarios, Bitacora, Monitoreo y demas listados administrativos queda como trabajo B03 pendiente; no se declara completitud global ni aprobacion del propietario.
