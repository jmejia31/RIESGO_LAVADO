# RIESGO_LAVADO — Microfase 1 · correcciones P0 de identidad y reportería

**Fecha:** 2026-11-17 16:09 -06:00
**Autor:** CHAT / ChatGPT (cuenta GitHub `jmejia31`).
**Rama:** `desarrollo`.
**BASE_SHA:** `fdde9577fd4beb2d48d58a9fb3dcdfe1893f258c`.
**PATCH_1_SHA:** `526c014a010e1de65c23367a0566e7c39c1ca5ab`.
**SOURCE_CODE_SHA:** `2b36640eebe51e56f8774d52fea7121a15434f33`.
**QUALITY_GATE_CANDIDATE:** [Quality Gates 44186677489](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44186677489) — **en curso a las 16:09 -06:00**; ninguna prueba del último SHA se declara `PASS` sin resultado remoto.
**ESTADO:** `CODE_PATCH_PUBLISHED_CERTIFICATION_OPEN`. No es cierre M1 ni aceptación del propietario.

## 1. Defectos comprobados en código y corrección publicada

| Hallazgo verificable | Corrección quirúrgica | Regresión agregada |
|---|---|---|
| Detalle de `RL_LISTA_POSITIVOS` rechazaba `NULL`/cadena vacía, aunque tuviera `LSP_POSITIVO_ID` válido. | Predicado nulo-seguro `NVL(TRIM(...), '__SIN_IDENTIFICACION__')`, siempre unido a ID de origen y tipo de positivo. No fusiona marcadores. | `DetallePorReferenciaManual_AdmiteDocumentoNuloOSinIdentificacion`. |
| `POSITIVO_DNP` no figuraba si compartía el `N/A(3)` u otro marcador con cualquier coincidencia de reporte. | No deduplicar por documento común para identificaciones no atribuibles. Conserva referencias `LSP_POSITIVO_ID` y `REPORTE_COINCIDENCIA_ID` como fuentes distintas. | `PositivosDnpConMarcadores_NoSeOcultanPorCompartirDocumento`. |
| Botón de seguimiento informaba falsamente «Debe registrar un motivo» para identidad/asociación ambigua o no verificada. | Etiquetas y pistas accesibles distinguen falta de motivo, identidad ambigua y asociación no verificada; las acciones siguen bloqueadas. | Regresión de texto en `monitoreo-listas.component.spec.ts`. |
| Seis rutas de reportería (tres PDF y tres Excel) buscaban positivos y seguimientos **por documento** si `tieneMotivo` era verdadero, incluso sin asociación fiable. | Solo se consulta enriquecimiento por documento cuando ambos estados `estadoIdentidad` y `estadoAsociacion` son `VERIFIED`. El detalle de fuente por ID continúa consultable y la auditoría del reporte no se omite. | Dos nuevas pruebas Angular con PDF natural sin identificación y PDF empleado con asociación no verificada; el mismo guard se aplica a las tres fichas Excel. |

Los cambios son aditivos y conservadores; no eliminan restricciones RBAC, auditoría, uso de `DNP_IHSS` ni tests existentes.

## 2. Cobertura de los criterios M1 (1.1–1.6)

- **1.1 Reconciliación naturales:** las filas individuales por `REPORTE_COINCIDENCIA_ID` ya se habían implementado en `f744afa...`; se corrige además el descarte de positivos con marcadores repetidos. **242/190/52 no conciliado:** falta fuente Oracle institucional real.
- **1.2 Clave técnica:** `TipoFuente` y `IdRegistroFuente` ya cruzan DTO, endpoint, componente y detalles; no sustituirlos por DNI. Las operaciones de motivo/seguimiento persisten históricamente por documento: se mantiene el modo fail-closed cuando asociación no es verificable; **no se declara aislamiento completo por relación fuente** sin modelo institucional validado.
- **1.3 Sin identificación:** `N/A(3)`, otros marcadores, `NULL` y vacío consultables y separados; se corrigió el detalle manual/positivo de documentos nulos. No atribuir empresas ni seguimientos por marcador compartido.
- **1.4 Empleados históricos:** conducción y filtros por período implementados en el código M1 anterior. **No se certifican los 7 empleados**: se requiere ejecución autorizada del diagnóstico agregado y prueba sobre inventario real.
- **1.5 Detalle vs operaciones sensibles:** detalle origen disponible por ID; seguimiento y motivos de una persona no verificada NO se consultan por documento al construir PDF/Excel, ni se habilitan mutaciones.
- **1.6 Calificación y fechas:** `FECHA_CALIFICO` nula no es prueba de calificación; `TIPO_CALIFICACION_ID=1` conserva interpretación funcional heredada hasta confirmar semántica con DBA/datos fuente.

## 3. Verificación y límite de evidencia

- **Verificado por lectura GitHub actual:** los dos commits de código están publicados en `desarrollo` y sus archivos existen. La ejecución inicial `44181990671` de `d216edf...` reportó `1170 PASS/1 SKIP` backend, `1023 PASS` Angular, `79 PASS/15 SKIP` E2E, Oracle XE descartable y contenedores `PASS`; **resultados heredados, NO pruebas del SHA actual**.
- **Sin ejecución local:** este cliente no dispone del checkout/runtime conectado a Oracle, .NET y npm para reproducir las nuevas pruebas fuera de Actions. El QG `44186677489` estaba en curso al registro del expediente.
- **Seguridad:** `npm audit --omit=dev` pasó en la corrida anterior, `npm audit` completo falló por `picomatch@4.0.7` / `GHSA-fc5q-39xc-vjpf` (`CVE-2026-112366`). Ningún audit o guard se desactivó. **Quality Gate global NO certificado** mientras exista este fallo.
- **Sin Oracle institucional:** `Q01-Q12` solo lectura fueron preparadas; no se ejecutaron; no existen cifras reales verificadas de 242/190/52 o siete empleados. No publicar datos personales, DNI, nombres o vistas sin sanear.
- **Sin prueba visual final ni aceptación funcional del propietario.**

## 4. Pendientes verificables

El [issue #41](https://github.com/jmejia31/RIESGO_LAVADO/issues/41) asignado al propietario contiene la matriz de conciliación Oracle autorizada, prueba de registros con igual DNI / distintas empresas, marcadores, históricos, aislamiento de motivos, PDF/Excel y signoff. La consulta preautorizable está en [M1 SQL de solo lectura](../qa/M1_DIAGNOSTICO_SOLO_LECTURA_MONITOREO_20261117.sql).

**Límite estructural explícito:** la operación histórica de positivos/seguimientos por documento no puede interpretarse por sí sola como clave de relación por `REPORTE_COINCIDENCIA_ID`. Antes de permitir su uso para dos fuentes con el mismo DNI debe validarse la relación realmente persistida en Oracle y diseñar una clave estable o bloquear la asociación. No prometer `NO_FALSE_RELATION=PASS` solo por coincidencia de DNI.

**Punto de continuación:** leer el run exacto `44186677489` y reparar nuevos fallos del SHA sin rebajar seguridad; posteriormente reconciliar Q01-Q12 mediante personal autorizado y obtener aceptación visual/institucional.

```ini
PROJECT=RIESGO_LAVADO
PHASE=M1_RECUPERACION_REGISTROS_IDENTIDAD
BRANCH=desarrollo
AUTHOR=CHAT
SOURCE_CODE_SHA=2b36640eebe51e56f8774d52fea7121a15434f33
PATCH_1_SHA=526c014a010e1de65c23367a0566e7c39c1ca5ab
QUALITY_GATE_RUN=44186677489
QUALITY_GATE_STATUS=PENDING_FINAL_RESULT_AT_PUBLICATION
SECURITY_SCAN=FAIL_UPSTREAM_PICOMATCH_HIGH_PREVIOUS_RUN
M1_242_190_52=NOT_VERIFIED_ORACLE
M1_EMPLOYEES_7=NOT_VERIFIED_ORACLE
IDENTITY_RELATION_ISOLATION=PARTIAL_FAIL_CLOSED_PENDING_INSTITUTIONAL_VALIDATION
OWNER_ACCEPTANCE=PENDING
MAIN_TOUCHED=NO
PRODUCTION_TOUCHED=NO
ORACLE_INSTITUTIONAL_READS=NOT_EXECUTED
ORACLE_INSTITUTIONAL_WRITES=0
DNP_IHSS_WRITES=0
```
