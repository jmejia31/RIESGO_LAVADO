# Matriz completa — Bloque 6 (campos 70–82)

## Alcance y contrato

El Bloque 6 completa la proyección institucional de Monitoreo, Efectividad y Observaciones. Sus trece ordinales y etiquetas se mantienen exactamente en el orden 70–82. Matriz completa permanece como superficie de lectura; los cambios de Observaciones se realizan mediante comandos separados y autorizados por el backend.

| Ordinal | Etiqueta contractual | Fuente / persistencia | Cardinalidad |
|---:|---|---|---|
| 70 | Señales de Alerta | `RL_MR_SENALES_ALERTA` (`ALE_EVALUACION_ID`) | Repetible por evaluación |
| 71 | Estado del Riesgo | `RL_MR_AUTOMONITOREO.MON_ESTADO_RIESGO` | Valor del seguimiento más reciente |
| 72 | Estado del Control Preventivo | `RL_MR_CONTROLES_RIESGO.CON_ESTADO_MONITOREO`, controles `PREVENTIVO` | Por control/evaluación |
| 73 | Evaluación de la Efectividad del Control Preventivo | `CON_EFECTIVIDAD_MONITOREO`, controles `PREVENTIVO` | Por control/evaluación |
| 74 | Evidencia(s) del Control Preventivo | `RL_MR_EVIDENCIAS` + `RL_MR_EVIDENCIAS_VINCULOS`, control `PREVENTIVO` | Repetible por control |
| 75 | Estado del Control Detectivo | `CON_ESTADO_MONITOREO`, controles `DETECTIVO` | Por control/evaluación |
| 76 | Evaluación de la Efectividad del Control Detectivo | `CON_EFECTIVIDAD_MONITOREO`, controles `DETECTIVO` | Por control/evaluación |
| 77 | Evidencia(s) del Control Detectivo | `RL_MR_EVIDENCIAS` + `RL_MR_EVIDENCIAS_VINCULOS`, control `DETECTIVO` | Repetible por control |
| 78 | Estado del Control Correctivo | `CON_ESTADO_MONITOREO`, controles `CORRECTIVO` | Por control/evaluación |
| 79 | Evaluación de la Efectividad del Control Correctivo | `CON_EFECTIVIDAD_MONITOREO`, controles `CORRECTIVO` | Por control/evaluación |
| 80 | Evidencia(s) del Control Correctivo | `RL_MR_EVIDENCIAS` + `RL_MR_EVIDENCIAS_VINCULOS`, control `CORRECTIVO` | Repetible por control |
| 81 | Observaciones del Área | `RL_MR_AUTOMONITOREO.MON_OBSERVACIONES_AREA` | Por registro de automonitoreo |
| 82 | Observaciones UGR | `RL_MR_AUTOMONITOREO.MON_OBSERVACIONES_UGR` | Por registro de automonitoreo |

## Decisiones arquitectónicas

### Estado del riesgo y workflow

El campo 71 se proyecta desde `MON_ESTADO_RIESGO`, que describe el estado del riesgo en el automonitoreo. El estado del workflow se conserva en `RL_MR_FLUJOS_EVALUACION.FLU_ESTADO` / la proyección vigente de evaluación y no alimenta el ordinal 71. El E2E usa `ALTO` para el seguimiento y `BORRADOR` para el workflow para comprobar que son valores separados.

### Monitoreo por control

`CON_ESTADO` y `ECO_EFECTIVIDAD` representan los datos operativos/base ya existentes; no se reasignan a monitoreo posterior. Se agregaron `CON_ESTADO_MONITOREO` y `CON_EFECTIVIDAD_MONITOREO` a `RL_MR_CONTROLES_RIESGO`, que ya está asociado a una evaluación (`CON_EVALUACION_ID`) y tiene un tipo canónico de control (`CON_TIPO`). De este modo, el estado y la efectividad posterior son propios del control en esa evaluación, y no alteran el control maestro ni la evaluación base. El tipo determina qué par ordinal se proyecta: Preventivo 72–74, Detectivo 75–77 y Correctivo 78–80.

La efectividad de monitoreo admite `NULL` y, cuando se informa, el backend valida 0–100; la columna usa `NUMBER(5,2)`. El estado admite `NULL` y está limitado a 30 caracteres. No se introdujo un catálogo porque la inspección no encontró catálogo vigente de estado/efectividad de monitoreo que pudiera reutilizarse sin cambiar significado.

### Evidencias

Se reutilizan los objetos existentes `RL_MR_EVIDENCIAS` y `RL_MR_EVIDENCIAS_VINCULOS`, con vínculo genérico `CONTROL` hacia `CON_ID`. Los controles ya pertenecen a una evaluación; la lectura del Bloque 6 filtra primero controles por `CON_EVALUACION_ID` y agrupa los vínculos por control. La familia se deriva de `CON_TIPO`; no se copia una clasificación redundante al vínculo. La Matriz presenta identidad y nombre del archivo; carga y vínculo permanecen en los endpoints existentes de evidencias y no se crea un flujo paralelo.

### Observaciones y permisos

El agregado de automonitoreo ya contiene eventos por evaluación y es la fuente de `MON_ESTADO_RIESGO`; se amplió con dos columnas nullable de 2000 caracteres, una para Área y otra para UGR. Cada endpoint escribe solamente una propiedad, valida su permiso en backend dentro de la misma transacción y registra el cambio en auditoría. El frontend obtiene capacidades de lectura y después de guardar hace un GET nuevo para confirmar la persistencia.

La tabla `RL_USUARIO_CAPACIDADES` proporciona asignación explícita por usuario y códigos de acción independientes. No crea roles, no infiere Área/UGR desde roles existentes y no siembra permisos productivos. La ausencia de una capacidad deniega la escritura por defecto. La asignación productiva queda pendiente de aprobación institucional; `PRODUCTION_PERMISSION_ASSIGNMENTS_CREATED=NO`.

## Esquema

Se requiere una migración aditiva: columnas nullable de monitoreo en controles y observaciones en automonitoreo, más la tabla de capacidades. No se hizo backfill ni se inventaron datos históricos. El esquema de instalación limpia también incluye las mismas columnas. La transición es:

1. [`50_precheck_bloque6_solo_lectura.sql`](../../database/19_matrices_riesgos/transicion/50_precheck_bloque6_solo_lectura.sql)
2. [`51_ddl_bloque6.sql`](../../database/19_matrices_riesgos/transicion/51_ddl_bloque6.sql)
3. [`52_postcheck_bloque6_solo_lectura.sql`](../../database/19_matrices_riesgos/transicion/52_postcheck_bloque6_solo_lectura.sql)
4. [`53_rollback_bloque6.sql`](../../database/19_matrices_riesgos/transicion/53_rollback_bloque6.sql)

La migración no concede capacidades a usuarios ni cambia datos. Los prechecks/postchecks son read-only. Estos scripts no se ejecutaron en Oracle institucional.

## Pruebas y evidencia de esta intervención

Base de trabajo: `ea030a46bdfcd7e3256ba73a8b90f8f75a544be7`. Estado de cierre y SHA final se agregarán cuando estén disponibles. Verificaciones locales ejecutadas en el checkout/copia aislada de frontend:

- Backend Release build: PASS, 0 errores; 1054 warnings de analizadores existentes.
- Backend tests: `753/753`, 0 fallidos, 0 omitidos.
- Backend coverage: líneas 36.85%, ramas 40.21%.
- Frontend unit/coverage: `83/83` archivos, `829/829` pruebas. Cobertura: statements 62.39%, branches 55.92%, functions 58.49%, lines 63.37%.
- Frontend lint, build production y `npm audit`: PASS / 0 vulnerabilidades, en copia aislada con dependencias instaladas.
- E2E Chromium: `45/45` PASS, 0 fallidos, 0 flaky. Incluye el fixture con 2 señales, workflow `BORRADOR` frente a riesgo `ALTO`, tres tipos de control, seis evidencias sin mezcla, y round-trip stateful de las observaciones UI por GET/PUT/GET/reload.
- Matriz completa comprobada en 1280×900 y 390×844; capturas de los controles y observaciones de Bloque 6 conservan el modal dentro del viewport y no muestran overflow horizontal.
- SQL validator: PASS; 4 scripts presentes, pre/post read-only y paridad estática de instalación limpia.
- `tools/run_quality_gates.ps1` completó backend `753/753` y generó cobertura; el paso frontend no pudo arrancar en el checkout principal porque su `node_modules` quedó incompleto cuando `npm ci` no pudo reemplazar un `esbuild.exe` bloqueado por el `ng serve` preexistente. No se detuvo ese proceso. Las suites frontend y E2E se ejecutaron en copia aislada con dependencias instaladas.
- El SQL fue validado estáticamente, no aplicado. `ORACLE_CONNECTION_ATTEMPTED=NO`, `ORACLE_DML_EXECUTED=NO`, `ORACLE_DDL_EXECUTED=NO`.

La interfaz y los contratos están implementados; no se declara round-trip contra almacenamiento Oracle real ni autorización integrada contra una base de pruebas, porque la restricción de esta intervención impide Oracle institucional y el repositorio no provee aquí una instancia Oracle de prueba conectada. El cierre definitivo depende de ejecutar la migración en la infraestructura autorizada y de un Quality Gate remoto exitoso para el SHA final.

## Certificación remota y continuidad

- **Bloqueo de la primera intervención:** la falta inicial de permisos granulares y persistencia de observaciones/monitoreo se resolvió con infraestructura mínima; no se asignaron capacidades a roles ni usuarios productivos. La implementación deny-by-default queda lista para asignación institucional aprobada.
- **Mitigación de advisories:** tras el fallo de audit del run 1684 por advisories publicados en `@angular/router` y `dompurify`, se actualizaron los paquetes Angular alineados a `22.2.0` y se fijó el override transitivo de `dompurify` en `3.4.16`. No se usó `npm audit fix --force`; instalación reproducible y audit remoto reportaron 0 vulnerabilidades.
- **Prueba E2E intermitente:** una corrida local tuvo un timeout aislado en la prueba de preview de exportaciones; la prueba aislada pasó y una corrida completa posterior pasó 45/45. El run remoto final también pasó 45/45, sin flaky.
- **Quality Gate remoto exacto del código:** run `36760578575` (#1685), SHA `8ca9d3c9b49caa7bb6f117f5cd478682a5d99154`, `completed/success`. Conteos: backend 753/753, fallidos 0, omitidos 0; frontend 83 archivos/829 pruebas; E2E 45/45; audit 0. Cobertura backend 36.85% líneas / 40.21% ramas; frontend 63.19% statements / 57.77% branches / 59.41% functions / 63.95% lines.
- **Avisos no bloqueantes del gate:** continúan avisos de analizadores .NET existentes, presupuestos SCSS del inspector y Configuración de cálculo, dependencia CommonJS `exceljs` y aviso de deprecación Node 20 de actions. Los gates completaron correctamente.
- **Alcance de verificación de datos:** las pruebas API/E2E validan contratos y round-trip contra el repositorio/mocks stateful disponible; los scripts de migración pasaron validación estática. No hubo instancia de Oracle de prueba y no se afirma aplicación ni round-trip Oracle real. `ORACLE_CONNECTION_ATTEMPTED=NO`, `ORACLE_DML_EXECUTED=NO`, `ORACLE_DDL_EXECUTED=NO`.
- **Gobernanza:** implementación publicada en `desarrollo`; `MAIN_TOUCHED=NO`, `PRODUCTION_TOUCHED=NO`. Los archivos no rastreados preexistentes se preservaron y no se incluyeron en commits.
- **Pendiente exacto:** publicar esta actualización documental y obtener el Quality Gate `completed/success` correspondiente a su SHA final. La asignación productiva de permisos sigue bajo aprobación institucional; no se inventaron asignaciones.

Aclaración de continuidad: el Quality Gate de desarrollo certifica el código y sus contratos; la migración queda versionada y validada para aplicación posterior bajo el procedimiento institucional autorizado. No se afirma que el esquema se haya aplicado. La ausencia de acceso a Oracle de prueba no impide cerrar la implementación en desarrollo, pero una instalación real debe aplicar y verificar la migración antes de habilitar la función sobre esa base.
