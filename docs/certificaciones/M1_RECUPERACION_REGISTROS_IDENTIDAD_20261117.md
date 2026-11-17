# RIESGO_LAVADO — Microfase 1: recuperación de registros, identidad y detalle

**Fecha:** 17 de noviembre de 2026  
**Autor:** CHAT / ChatGPT; cuenta de publicación `jmejia31`; **sin Codex**.  
**Rama:** `desarrollo`  
**Punto inicial:** `d7d3556958176108ed41bebd045f2de42547ad9e`  
**Diagnóstico inicial:** `a95ec97c6c83470543c23b09c8228fb725025762`  
**Último código funcional de M1:** `f744afa636c5f000b7b62079f7a03e5aa4c92a08`  
**Estado:** IMPLEMENTACIÓN DEL CÓDIGO REALIZADA; CERTIFICACIÓN INSTITUCIONAL ABIERTA

> Esta evidencia no certifica los conteos reales `242 / 190 / 52` ni los siete empleados. Aquí no existe conexión a Oracle institucional. No se ejecutaron SELECT sobre datos personales ni se escribieron datos Oracle. Las pruebas automatizadas son sintéticas/de contrato y no sustituyen la conciliación con la fuente institucional autorizada.

## 1. Diagnóstico confirmado en el código previo

1. **Naturales:** `REPORTE_AGG` agrupaba por `DNI, LISTA_CONCIDENCIA`, produciendo grupos en lugar de filas de coincidencia; podían desaparecer relaciones individuales del listado.
2. **Detalle natural:** `INNER JOIN V_SOCIOS_REPRESENTANTES` por DNI podía eliminar detalles cuyos registros de coincidencia no tuvieran correspondencia en la vista, o multiplicar/asociar datos de forma indebida.
3. **Empleados:** `V_EMPLEADOS_IHSS_PLANILLAS` era el conductor del listado y un JOIN por DNI a `REPORTE_COINCIDENCIAS` multiplicaba filas entre períodos. Un registro sin correspondencia en planillas tampoco figuraba.
4. **Registros manuales:** existían duplicaciones potenciales y ocultamiento por coincidencia de DNI, sin referencia de origen diferenciada.
5. **Marcadores:** `N/A(3)` y documentos no atribuibles se trataban de forma insuficientemente diferenciada, con lectura por DNI compartido.

## 2. Correcciones implementadas en M1

| Control | Implementación |
|---|---|
| Unidad de listado natural | Una fila por `REPORTE_COINCIDENCIA_ID`, sin agrupar por DNI/lista; se preservan nombre/lista/fecha/patrono del reporte de origen. |
| Referencia individual | `IdRegistroFuente` y `TipoFuente` en DTO, API, Angular, consulta de detalle y exportación individual. |
| Identificación no atribuible | `CategoriaIdentidad` y selector UI `identificadas / sin_identificacion`. Los marcadores `N/A*` son consultables sin declararlos personas verificadas. |
| Detalle fuente natural | La fila DNP constituye el conductor y no desaparece por faltar socio/representante. Enriquecimiento empresarial solo con DNI, patrono y nombre únicos; no atribuir empresas por marcador ni DNI compartido. |
| Detalle empleado | Recupera la coincidencia seleccionada mediante ID, conserva `PERIODO` de planillas coincidentes; permite seleccionar mes sin multiplicar coincidencias por historial. |
| Listado empleado | Conductor desde coincidencias clasificadas como empleado en `V_REPORTE_COINCIDENCIA`, con `EXISTS` en lugar de JOIN multiplicador a planillas. |
| Manuales | Detalle individual por `LSP_POSITIVO_ID`, sin atribuir empresas; fuente `POSITIVO_DNP` diferenciada de `MANUAL_CUMPLIMIENTO`. Evita duplicar positivos DNP cuando existe la coincidencia fuente. |
| Identidad legada | Rutas antiguas por documento se conservan únicamente si el documento identifica una coincidencia única; si tiene varias, se devuelve conflicto informativo, no una unión arbitraria. |
| Nulos/vacíos | Consulta de detalle por ID fuente admite documento vacío/nulo, con referencia obligatoria y fuente comprobada. |
| Históricos/filtros | `PeriodoPlanilla` como filtro explícito y búsqueda correlacionada en planilla por DNI, patrono, nombre y período. Se preserva la consulta sin filtro temporal. |
| Operaciones sensibles | No se eliminan comprobaciones de identidad/roles/auditoría de mutaciones. Las mejoras son de lectura. |

**Advertencia de semántica:** El nuevo contador de filas registra *coincidencias fuente*; no debe interpretarse automáticamente como número de personas distintas, de relaciones empresariales ni como conciliación de los 242 históricos. El estado `TIPO_CALIFICACION_ID = 1` continúa siendo el mismo filtro funcional heredado; su significado debe corroborarse en la fuente institucional.

## 3. Superficies modificadas

- `backend/RL.API/Features/Listas/Persistence/ListasRepository.cs`.
- `backend/RL.API/Features/Listas/Contracts/{ListasModels.cs,ConsultasPaginadasDtos.cs,IdentidadMonitoreo.cs}`.
- `backend/RL.API/Features/Listas/{ListasController.cs,Application/ListasService.cs,Application/IListasService.cs,Persistence/IListasRepository.cs}`.
- `frontend/rl-app/src/app/features/admin/listas/{models/listas.models.ts,data-access/listas.service.ts,pages/monitoreo-listas/monitoreo-listas.component.impl.ts,pages/monitoreo-listas/monitoreo-listas.component.html}`.
- Pruebas `MonitoreoFuenteM1Tests.cs`, `ListasApplicationTests.cs`, `ListasControllerTests.cs`, `listas.service.spec.ts`, `monitoreo-listas.component.spec.ts`.
- Workflow complementario `.github/workflows/m1-regresion-focal.yml`. No se modifica el Quality Gate institucional original.

## 4. Evidencia de pruebas CI

**Evidencia primaria de SHA anterior cercano (no del SHA definitivo):**

- [M1 focal #44173253550](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44173253550), SHA `b8976843aba8170b9366e28c8f393b9f53a56859`: backend `dotnet build Release` y `39` pruebas focales **PASS**; frontend `npm ci`, ESLint, Angular `96 archivos / 1020 pruebas PASS` y `npm run build` **PASS**. **El job Angular terminó FAILURE por `npm audit`, no por tests/build.**
- [M1 focal #44172156719](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44172156719): se detectó y reparó el texto UX genérico; la regresión `ux-copy-context` quedó en PASS en la ejecución posterior.
- [M1 focal en SHA de última corrección](https://github.com/jmejia31/RIESGO_LAVADO/actions/workflows/m1-regresion-focal.yml): pendiente resultado exacto posterior a la adición de casos para DNI vacío y controladores.
- El Quality Gate institucional permanece **sin certificación SUCCESS**. `npm audit` identifica la dependencia transitoria `picomatch` como severidad alta (`GHSA-fc5q-39xc-vjpf`) y reporta `No fix available` para el árbol evaluado. El análisis/remediación corresponde a M3, sin reducir controles.

**No ejecutar a ciegas consultas Oracle en este repositorio:** el archivo [diagnóstico de solo lectura](../qa/M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql) requiere operador autorizado y supervisión según el costo de las vistas. Los resultados publicados solo deben ser métricas agregadas sanitizadas.

## 5. Verificación externa obligatoria: registro de límites

| Asunto | Evidencia actual | Estado |
|---|---|---|
| `242 vs 190` y desglose de 52 | Consulta antes agrupada identificada y corregida por ID fuente; **sin conteo Oracle real** | PENDIENTE |
| Siete empleados esperados vs cero visibles | JOIN multiplicador retirado y clasificación fuente explícita; **sin conteo Oracle real** | PENDIENTE |
| `N/A(3)` | Lectura por ID fuente, categoría separada, asociación no atribuible | IMPLEMENTADO EN CÓDIGO; pendiente E2E real |
| Patrones persona–empresa | Solo asociación mediante vínculo único comprobable en consulta | IMPLEMENTADO EN CÓDIGO; pendiente contraste DBA |
| Calificaciones y fechas | No se suprimen filas únicamente por `FECHA_CALIFICO IS NULL`; `TIPO_CALIFICACION_ID=1` heredado | SEMÁNTICA DE ORIGEN PENDIENTE |
| Seguridad mutaciones y auditoría | No se relajaron mutaciones. Se preserva autorización módulo 4 | Regresiones focales, E2E pendiente |
| Reportes PDF/Excel | Detalles individuales ya piden ID fuente; branding/colores pertenecen a M2 | PARIDAD REAL PENDIENTE |
| Oracle institucional | Sin conexión de este cliente; no se ejecutaron consultas | NO VALIDADO |
| Oracle/DNP escrituras | No se ejecutaron escrituras | 0 |

## 6. Puerta de cierre

La **implementación de código M1 está publicada**, pero el cierre institucional requiere:
- Resultado íntegro del workflow de SHA final, incluidas pruebas backend/Angular y sin llamarlo verde cuando `npm audit` falle.
- Ejecutar de manera autorizada las consultas agregadas Q01–Q12 y explicar 242/190 y empleados con sus cardinalidades reales.
- E2E autenticada sobre registros fuente seleccionados (incluidos DNI duplicado, `N/A(3)`, sin DNI, manuales y empleados históricos).
- Confirmación visual/funcional del propietario.

No crear falsos registros, no escribir Oracle, no desactivar `npm audit`, no reclamar certificación por inferencia.

```ini
PROJECT=RIESGO_LAVADO
PHASE=M1_RECUPERACION_REGISTROS_IDENTIDAD
AUTHOR=CHAT
SOURCE_CODE_SHA=f744afa636c5f000b7b62079f7a03e5aa4c92a08
BRANCH=desarrollo
M1_CODE=IMPLEMENTED
M1_REAL_ORACLE_RECONCILIATION=PENDING_EXTERNAL_READ_ONLY
M1_242_190_DIFFERENCE=NOT_VERIFIED
M1_EMPLOYEE_7=NOT_VERIFIED
M1_OWNER_VISUAL_E2E=PENDING
M1_FINAL_CERTIFICATION=OPEN_WITH_LIMITATIONS
ORACLE_INSTITUTIONAL_WRITES=0
DNP_IHSS_WRITES=0
MAIN_TOUCHED=NO
PRODUCTION_TOUCHED=NO
B07_TOUCHED=NO
BLOCK10_TOUCHED=NO
MIGRATION_54_TOUCHED=NO
```
