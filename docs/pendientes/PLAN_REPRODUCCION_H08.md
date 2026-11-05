# Plan estático de reproducción H08 y TX12 frontend

**Tarea:** `P0-H08-TX12-PLAN-001`  
**Autor:** `COD`  
**Estado:** `IMPLEMENTED_PENDING_REVIEW` / `READY_FOR_INDEPENDENT_REVIEW` solo para revisión documental del plan  
**Objetivo:** definir una reproducción futura, no conectada en esta intervención, que permita cerrar las evidencias faltantes sin confundirlas con el signoff documental P0-2.

## 1. Reglas de seguridad y alcance

El plan solo aplica a un checkout de `desarrollo` y a un Oracle XE disposable explícitamente autorizado. No usar credenciales institucionales, QA compartido, producción, `main`, migración 54, `PRUEBA_FORMULARIO`, GTIC ni Bloque 10. No ejecutar DDL/DML fuera del fixture disposable. No modificar el expediente P0-2 para fabricar evidencia.

La prueba debe ser fail-closed: si falta una precondición, se registra `STOP` y no se continúa. La evidencia debe ser sintética y sanitizada; nunca incluir secretos, JWT, cadenas de conexión, PII, dumps ni logs operativos.

## 2. Preflight obligatorio

1. **Identidad del código.** Confirmar rama `desarrollo`, checkout limpio y SHA exacto que será probado. Registrar `SOURCE_CODE_SHA`, árbol de archivos funcionales, versión de Node/npm/.NET y versión de Oracle XE. El SHA debe quedar congelado antes de iniciar el host.
2. **Separación de cambios.** Confirmar que la modificación bajo revisión no altera migración 54, `database`, configuración de producción, workflows no relacionados o archivos de certificación. Si el delta no es exclusivamente el alcance aprobado, `STOP`.
3. **Guardas de entorno.** Exigir perfil explícito `LOCAL_XE`, host disposable y bandera de opt-in del escenario. Rechazar ausencia de perfil, fallback de conexión, host no local, secreto en texto, conexión a QA/producción o cualquier bypass de autenticación.
4. **Fixtures.** Crear únicamente la familia/versiones/evaluación/evidencia sintéticas previstas por el runner. Registrar IDs y códigos sintéticos, conteos iniciales y hashes; no reutilizar datos institucionales.
5. **Salud no funcional.** Comprobar que API y frontend apuntan al host disposable, que la ruta `/api` corresponde al checkout fijado y que el runner H08/P0 no está saltando por falta de sus variables obligatorias. Un test omitido por `test.skip` es `NOT_EXECUTED`, nunca `PASS`.
6. **Instrumentación.** Preparar registro de método, ruta, status, orden de requests, `traceId` sanitizado, snapshot antes/después y auditoría. No capturar tokens ni cuerpos con datos reales.

## 3. Oráculos y resultados esperados

### 3.1 Oráculos transaccionales de TX12 frontend

| Paso | Oráculo | Resultado esperado |
|---|---|---|
| Guardar borrador | `PUT /api/matrices-riesgos/formularios/{id}` | HTTP 2xx, JSON normalizado y hash persistidos; auditoría `UPDATE`; lectura posterior coincide |
| Sincronizar fórmulas | `PUT /api/matrices-riesgos/configuracion-calculo/formula-usos/versiones-formulario/{id}` | HTTP 2xx antes de publicar; no hay uso duplicado ni referencia huérfana |
| Lectura | `GET /api/matrices-riesgos/formularios/{id}` | HTTP 2xx; la definición leída es la que se guardó, no una respuesta mock |
| Publicar | `POST /api/matrices-riesgos/formularios/{id}/publicar` | HTTP 2xx; estado `PUBLICADA`; lectura posterior y hash conservados |
| Fallo inducido autorizado | operación negativa que produzca el error contractual documentado | status/error original, rollback de JSON/hash/estado y ausencia de auditoría parcial; no sustituir el error por una aserción genérica |
| Dependencia de eliminación | `DELETE /api/matrices-riesgos/formularios/{id}` sobre versión con fórmula/evaluación | HTTP 409, versión y dependencias intactas; auditoría sin falso `DELETE` |

TX12 queda `PASS` únicamente si el navegador real ejecuta el flujo y se conservan requests, respuestas, lectura posterior, auditoría y snapshots. El expediente P0-2 backend no satisface estos oráculos frontend.

### 3.2 Oráculos H08 automatizados

Ejecutar el flujo secuencial sobre la familia sintética: baseline → desactivar → archivar con motivo → consultar filtro archivadas → restaurar con motivo → activar → editar/publicar desde Form Builder → retirar vigencia → registrar evaluaciones antes/después → consultar snapshots profundos → auditar ambos ciclos.

Los invariantes mínimos son:

- `ARCHIVADA` es reversible y no elimina versiones, evaluaciones, evidencias ni auditoría.
- Restaurar devuelve `INACTIVA`; no publica ni activa una versión automáticamente.
- Una familia archivada no permite editar, publicar, activar ni eliminar indebidamente.
- La publicación conserva JSON/hash y el orden de sincronización de fórmula antes de publicar.
- La evaluación nueva referencia una versión vigente válida y cada evaluación tiene ID distinto.
- Los snapshots antes/después no muestran pérdida, duplicación, huérfanos ni mutaciones no previstas.
- La auditoría conserva usuario, evento, fecha/orden y motivo donde corresponda.
- El browser usa controles Angular reales; las rutas críticas no son mockeadas.

### 3.3 Aceptación manual H08

Después del E2E técnico, un usuario autorizado ejecuta M01–M08 según `docs/3. Módulo Matrices de Riesgos/GUIA_VALIDACION_MANUAL_H08.md`. Debe aportar resultado real, referencia de captura o registro, usuario/fecha y validación. M03 requiere retest específico de publicación/retirada de vigencia. Cualquier estado inesperado, auditoría ausente o definición vacía produce `MANUAL_VALIDATION_REQUIRED` o `STOP`.

## 4. Quality Gates y conteo correcto

| Señal | Uso | No significa |
|---|---|---|
| Quality Gate fast | Validación rápida de JSON/contrato para deltas administrativos elegibles | No prueba backend, frontend, Oracle XE, browser, E2E ni certificación P0 |
| Quality Gate integral | Build, validadores, suites, E2E/Oracle XE y demás pasos del workflow aplicables al SHA probado | No autoriza producción, Oracle institucional ni Bloque 10 |
| Cinco gates históricos P0-2 | `41469328699`, `41472933613`, `41477196537`, `41480850420`, `41483797292`, reportados sobre el SHA certificado | No son ejecuciones de esta intervención ni cierre funcional H08/TX12 |

Para una certificación futura se exige el gate integral del SHA exacto, sus artefactos y el resultado de cada job. Un fast gate `SUCCESS`, un job integral `skipped` o un run de otro SHA se registra como evidencia insuficiente.

## 5. Criterios de STOP

Detener inmediatamente y registrar causa, SHA y último oráculo observado si ocurre cualquiera de estos casos:

1. rama distinta de `desarrollo`, árbol sucio no explicado o SHA cambia durante la corrida;
2. perfil/host no es `LOCAL_XE` disposable, aparece producción/QA compartido, o se solicita credencial;
3. falta una bandera de opt-in y el runner queda omitido, hay mocks en una ruta crítica o el browser no es real;
4. status HTTP distinto al esperado, hash/JSON divergente, rollback incompleto, auditoría parcial, duplicado, huérfano o pérdida de fila;
5. familia archivada permite una mutación prohibida, restauración activa/publica automáticamente o M03 no puede verificarse;
6. no se puede correlacionar request, response, snapshot y auditoría sin inventar un `traceId`;
7. el workflow selecciona solo fast gate, omite el integral o el SHA del artefacto no coincide;
8. aparece secreto, PII, dump, log operativo restringido o evidencia primaria que no puede sanitizarse;
9. se requiere tocar migración 54, Oracle institucional, `PRUEBA_FORMULARIO`, producción, `main` o Bloque 10.

Un `STOP` no se convierte en `PASS`, no se “reintenta” cambiando el criterio y no se declara bloqueo 10 autorizado.

## 6. Paquete de evidencia necesario

La futura certificación debe publicar texto sanitizado bajo el expediente autorizado o enlazar un artefacto remoto verificable con:

- `SOURCE_CODE_SHA`, `EVIDENCE_COMMIT_SHA`, rama, fecha/hora y autor;
- versión de herramientas, perfil `LOCAL_XE`, declaración de disposable y guardas activas, sin secretos;
- inventario de fixtures sintéticos e IDs anonimizados, conteos y hashes antes/después;
- tabla request/response con método, ruta, status, orden, latencia opcional y `traceId` sanitizado;
- snapshots JSON antes/después de familia, versiones, evaluaciones, evidencias, dependencias y auditoría;
- salida exacta de los oráculos H08/TX12, conteos reales, tests omitidos y razones;
- artefactos Playwright y capturas solo si son sintéticos, legibles y sin credenciales;
- resultado del Quality Gate integral del SHA exacto, distinguiendo fast gate y cinco gates históricos;
- acta de M01–M08, referencias de evidencia y validación humana para H08-M03;
- límites, `NOT_VERIFIED`/`PENDING_*`, observaciones, `BLOCK10_AUTHORIZED=NO`, `GITHUB_REMOTE_VERIFIED` y `REVIEW_READY`.

No se acepta un ZIP sin índice legible, un resumen `PASS`, una captura sin procedencia, un resultado heredado sin SHA o una evidencia reconstruida.

## 7. Estado de este plan

Este archivo es diseño estático. En esta intervención no se ejecutaron preflight conectado, Oracle, API, Angular, Playwright, CI ni Quality Gates; tampoco se autenticó ningún usuario. La ejecución futura, la revisión externa y la eventual certificación permanecen pendientes.
