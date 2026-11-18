# RIESGO_LAVADO — M1 · cierre de pendientes corregibles sin Oracle institucional
**Fecha de intervención:** 2026-11-18 08:28 -06:00  
**Autor:** CHAT / ChatGPT, cuenta GitHub `jmejia31`.  
**Rama:** `desarrollo`.  
**BASE_SHA:** `b378f42224a8b543b4e9e70a8769829a1549f410`  
**SOURCE_CODE_SHAS:** `634f790a089c2423120b8f4bbde2b7be82c97e6c` → `e77fdad3300e4b6c5ef8ff5af6f5057123a3efe8` → **`0f1126b0ae41e5674717ac80441321bf621c3b72`**.  
**EVIDENCE_COMMIT_SHA:** documentar leyendo el commit final publicado; no autorreferenciar una huella imposible dentro de su propio blob.  
**ESTADO:** `CODE_PUBLISHED_PENDING_EXACT_SHA_CI_AND_INSTITUTIONAL_RECONCILIATION`.

## Correcciones adicionales aplicadas
1. **Aislamiento de motivos y seguimientos por origen (M1 1.1, 1.2, 1.5):** la lectura de identidad de natural y empleado ahora exige una coincidencia fuente única (no solo un DNI o un sujeto único); dos coincidencias con el mismo DNI se clasifican como `AMBIGUOUS` para mutaciones. Un documento con 0 coincidencias DNP no permite operaciones sensibles aunque aparezca en una vista de personas. La UI distingue `Identidad verificada / Asociación ambigua` y conserva la consulta del origen.
2. **Contadores fuente sin falsear coincidencias (M1 1.1):** para naturales se obtiene `COINCIDENCIAS_FUENTE` mediante `REPORTE_DOC_AGG` con una fila por DNI y `LEFT JOIN`, sin reintroducir `COUNT(*) OVER` en el hot path del módulo. La lectura sigue siendo una fila por `REPORTE_COINCIDENCIA_ID`. Para empleados se conserva la cardinalidad del reporte clasificado mediante `COUNT(*) OVER`; no la cardinalidad de JOIN con planillas.
3. **Origen positivo y tipo protegidos (M1 1.2, 1.5):** `POSITIVOS_AGG` para natural/empleado solo incluye positivos activos `DNP_LISTAS`. `RegistrarPositivoAsync` rechaza convertir silenciosamente un positivo existente a otro tipo de sujeto/origen: guard en servicio y condición en `UPDATE` dentro de transacción auditable. No se mutan datos ni esquemas institucionales.
4. **Históricos de empleados por rango (M1 1.4):** nuevos filtros opcionales `PeriodoPlanillaDesde` y `PeriodoPlanillaHasta`, inclusive por mes; el mes exacto y el rango son excluyentes. Se preservan paginación, cache por filtros y exportación; SQL parametrizado con `EXISTS` evita multiplicar coincidencias por meses. La UI muestra selección Desde/Hasta y rechazo explícito de rangos invertidos.
5. **Corrección SQL Oracle importante (M1 1.2):** `ResolverIdentidadAsync` ya no mezcla `COUNT` agregado y subquery escalar no agrupada en la misma proyección (riesgo `ORA-00937`). Se implementan agregados independientes `I/E` y `F` mediante `CROSS JOIN` de resultados unitarios; cada agregado conserva resultado 0 en caso de vacío. **Aún no validado en el Oracle institucional.**

## Pruebas y estado de evidencia
- Pruebas agregadas: nuevas regresiones xUnit para política `EstadoFuenteParaOperacion`, protección de tipo/origen, cardinalidad de fuente, rangos y endpoints 400; Vitest para rangos, exportaciones y etiquetas `AMBIGUOUS`.
- CI **heredada, previa al código actual**: [run 44187356574](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44187356574), `1172 PASS / 1 SKIP` backend; `1026 PASS` Angular; `79 PASS / 15 SKIP` E2E; `44 pasos SUCCESS`, seguridad integral `FAIL` por `GHSA-fc5q-39xc-vjpf`. **No se atribuyen esos números al SOURCE_CODE_SHA actual**.
- [run 44320027918](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44320027918) del primer parche: `FAIL` por guard de performance naturales (reintroducción de `COUNT(*) OVER`) y `npm audit`; fue **reparado** en `e77fdad` mediante `REPORTE_DOC_AGG`.
- Runs de parches intermedios: [44321932572](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44321932572) y [44324345135](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44324345135). Deben distinguirse por SHA; podrían ser cancelados por concurrencia de nuevas publicaciones.
- **RESULTADO FINAL QG DEL SHA DOCUMENTAL: PENDIENTE POSTPUBLICACIÓN.** Revisar log, conteos, build, npm audit, Oracle XE sintético, contenedores y E2E antes de dictamen.
- `npm audit` completo sigue siendo bloqueante; `picomatch@4.0.7`, CVE-2026-112366, sin cambio de policy, waiver, `--force` ni eliminación de control.

## Estado exacto por criterio M1
| Criterio | Código | Validación institucional |
|---|---|---|
| 1.1 242/190/52 | Consulta por ID y agrupación segura; no recuenta personas ni genera registros. | **PENDIENTE Q04–Q07 y agregados reales.** |
| 1.2 Identificación individual | DTO, ruta, fuente, barrera de asociación DNP única, bloqueo de convertibilidad. | **PENDIENTE relación persistida fuente–positivo/motivo para DNI compartido.** |
| 1.3 Sin identificación | Marcadores y NULL consultables por ID; reportes no inventan asociación. | **PENDIENTE muestra autorizada en UI real.** |
| 1.4 Empleados e históricos | Conductor fuente, mes exacto, rango mensual, exportación, paginación. | **PENDIENTE comprobar los siete empleados institucionales.** |
| 1.5 Detalle y operaciones | Detalle fuente consultable; acciones sensibles bloqueadas sin asociación única. | **PENDIENTE prueba end-to-end autenticada con registros reales y empresas distintas.** |
| 1.6 Fechas/calificaciones | `FECHA_CALIFICO` NULL no se infiere como calificada; filtro `TIPO_CALIFICACION_ID=1` conservado. | **PENDIENTE validar el significado institucional real de calificación 1.** |

## Dependencias fuera de este acceso
**Oracle institucional:** no existe conexión autorizada a la fuente desde este cliente; no se han ejecutado los SELECT Q01–Q12 ni se verifica que existan exactamente 242, 190, 52 o 7; no se reporta recuperación numérica real. La consulta se encuentra en [diagnóstico M1](../qa/M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql). Solo operadores autorizados pueden ejecutarla con revisión DBA; publicar únicamente agregados sanitizados.

**Propietario:** se requiere aprobación visual/funcional sobre pantalla real, búsqueda, detalles de doble DNI y asociaciones patronales, PDF y Excel con marcadores y empleados históricos. Seguimiento formal [issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41), permanece abierto.

**Seguridad externa:** `picomatch` afecta auditoría completa en el árbol de desarrollo. No degradar auditoría; recertificar tras versión oficial o remedio verificable.

```ini
PROJECT=RIESGO_LAVADO
PHASE=M1_RECUPERACION_REGISTROS_IDENTIDAD
AUTHOR=CHAT
BRANCH=desarrollo
BASE_SHA=b378f42224a8b543b4e9e70a8769829a1549f410
SOURCE_CODE_SHA=0f1126b0ae41e5674717ac80441321bf621c3b72
M1_CODE_REMEDIATIONS=IMPLEMENTED_PENDING_EXACT_SHA_CI
M1_INSTITUTIONAL_CERTIFICATION=OPEN
ORACLE_INSTITUTIONAL_READS=NOT_EXECUTED
ORACLE_INSTITUTIONAL_WRITES=0
DNP_IHSS_WRITES=0
PRODUCTION_TOUCHED=NO
MAIN_TOUCHED=NO
OWNER_ACCEPTANCE=PENDING
SECURITY_AUDIT=BLOCKING_NO_WAIVER
```
