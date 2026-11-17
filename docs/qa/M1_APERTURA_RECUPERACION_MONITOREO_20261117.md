# RIESGO_LAVADO — Microfase 1: apertura de conciliación y contrato de identidad

**Fecha:** 2026-11-17 · **Autor:** CHAT / ChatGPT · **Operador GitHub:** jmejia31  
**Rama autorizada:** `desarrollo` · **SHA base:** `d7d3556958176108ed41bebd045f2de42547ad9e`  
**Estado:** `M1_STARTED_READ_ONLY_DIAGNOSTIC` · **Implementación funcional:** pendiente de evidencia Oracle  
**Archivo de consultas:** [M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql](M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql)

## 1. Hallazgos confirmados por lectura de código

1. En `ListasRepository.ConstruirConsultaMonitoreoNaturales()`, el CTE `REPORTE_AGG` conserva `GROUP BY DNI, LISTA_CONCIDENCIA` y pierde la individualidad de relaciones persona–empresa. El resultado representa **grupos**, no necesariamente personas distintas ni filas fuente.
2. `ObtenerDetalleNaturalAsync()` usa `V_SOCIOS_REPRESENTANTES INNER JOIN REPORTE_COINCIDENCIAS` por DNI para documentos ordinarios; si el documento aparece en reportes pero no en esa vista, puede devolver una lista vacía. El caso marcador usa una consulta diferente que deja empresa/patrono nulos y puede mezclar varias filas con igual marcador.
3. `ConstruirConsultaMonitoreoEmpleados()` une `V_EMPLEADOS_IHSS_PLANILLAS` y `REPORTE_COINCIDENCIAS` por `IDENTIDAD = DNI`, luego agrupa sin `PERIODO` y realiza `COUNT(*)`. Existe riesgo de multiplicación al atravesar históricos.
4. El listado y los detalles se consultan principalmente por documento. Aunque socios y representantes contienen IDs propios, no hay en la respuesta actual referencia técnica individual desde Angular hasta el detalle.
5. Las consultas utilizan `TIPO_CALIFICACION_ID = 1`; la presencia/ausencia de `FECHA_CALIFICO` no se ha conciliado con la semántica de calificación institucional.
6. `RL_LISTA_POSITIVOS` contiene registros activos manuales o de seguimiento que deben clasificarse separadamente de los orígenes DNP.

**Evidencia del usuario:** la captura muestra 190 naturales, cero empleados y detalles vacíos en algunos sujetos. El antecedente es una diferencia histórica de 242 frente a 190 (52), así como siete empleados por contrastar. Las imágenes no prueban borrado de datos ni definen por sí solas la cardinalidad correcta.

## 2. Acciones realizadas en esta apertura

- Se inspeccionó el SHA remoto y los puntos de consulta/listado/detalle del backend, rutas de API, DTOs y consumidores Angular.
- Se prepararon doce consultas `SELECT` reproducibles para inventario de claves, cardinalidad, filas agrupadas, cobertura del detalle, registros especiales, calificaciones y empleados históricos.
- Se preservó íntegramente el código funcional mientras se obtiene evidencia de origen. **No se ha modificado Oracle, no se han consultado datos institucionales y no se ha reconciliado el valor 242/190.**
- El último Quality Gate heredado, `44154367078`, estaba en `FAILURE` por `npm audit`. No se declara restaurado ni superado.

## 3. Cómo ejecutar el diagnóstico

1. Utilizar DBeaver en una cuenta/rol **de lectura exclusivamente**, contra la base autorizada y con supervisión del DBA si las vistas históricas pueden resultar costosas.
2. Abrir el archivo SQL del repositorio. **Ejecutar cada SELECT individualmente**, comenzando por Q01–Q05. No ejecutar a ciegas todo el archivo.
3. Los bloques Q06, Q07, Q11 y Q12 dependen de vistas grandes: analizar su coste/autorización antes de ejecutarlos.
4. Registrar fecha, versión de esquema y **solo las cifras agregadas**. No pegar identidades, nombres, patronos, cadenas de conexión, capturas que contengan PII ni resultados fila a fila en chats públicos o GitHub.
5. Si una consulta falla por permisos, esquema o estructura, registrar el error **sanitizado**; no cambiar DDL/DML ni forzar privilegios.

## 4. Puerta de decisión antes del primer parche de persistencia

- Determinar qué representaban exactamente los antiguos 242: filas fuente, relaciones persona–empresa, grupos o documentos.
- Verificar contra qué conjunto se obtienen los 190 actuales y desglosar **sin inventar** la diferencia de 52.
- Identificar clave individual real de `REPORTE_COINCIDENCIAS`, `SOCIOS`, `REPRESENTANTES` y el identificador histórico de planilla.
- Distinguir registros sin identificación y registros manuales en una categoría propia.
- Identificar si la ausencia de empleado se debe a una vista vacía, clasificación, calificación o relación de identidad.
- Diseñar contrato tipado con referencia de origen para listado → detalle → reporte, con autorización/seguimiento seguros.

**No se autoriza reconstruir relaciones por `N/A(3)` ni hacer JOIN masivos por DNI.** La corrección se desarrollará sobre un diseño basado en claves verificadas, conservando las lecturas de origen cuando no exista una asociación confiable.

## 5. Continuación exacta

Tras recibir los resultados **agregados y sanitizados** Q02–Q12, crear la matriz de conciliación de cardinalidades y aplicar el primer parche de consulta/DTO/Angular con pruebas focales. Mantener los controles de seguridad y no intervenir el branding, Matrices, B07 ni Bloque 10 dentro de M1.

```ini
PROJECT=RIESGO_LAVADO
PHASE=M1_RECUPERACION_REGISTROS_IDENTIDAD
M1_STATUS=STARTED_DIAGNOSTIC_ONLY
BRANCH=desarrollo
BASE_SHA=d7d3556958176108ed41bebd045f2de42547ad9e
ORACLE_INSTITUTIONAL_QUERIES=NOT_EXECUTED
ORACLE_INSTITUTIONAL_WRITES=0
DNP_IHSS_WRITES=0
NATURAL_242_190_RECONCILIATION=PENDING_ORACLE
EMPLOYEE_7_RECONCILIATION=PENDING_ORACLE
SOURCE_PRIMARY_KEYS=PENDING_ORACLE_METADATA
PRODUCTION_CODE_CHANGED=NO
E2E_AUTHENTICATED=NOT_EXECUTED
OWNER_ACCEPTANCE=PENDING
```
