# Estado de Ejecución — Fase 1.2
## Alineación del repositorio con el DDL dinámico definitivo

Fecha: 3 de agosto de 2026  
Rama: `desarrollo`  
Rama estable: `main` — intacta  
Estado: implementación técnica en revisión; no certificada en Oracle

## 1. Autorización de inicio

Codex autorizó iniciar la Fase 1.2 bajo estas condiciones:

1. Retirar el workflow temporal antes de cualquier otro cambio.
2. Ampliar el validador a todo el módulo.
3. Alinear creación, actualización, transiciones, proyección y trazas con el DDL definitivo.
4. Solicitar revisión antes de ejecutar el script `05` en Oracle.

## 2. Cambios ejecutados

| Cambio | Commit | Estado |
|---|---|---|
| Retiro del workflow temporal `.github/workflows/agent-fix-matrices-phase1.yml` | `6070c4ca793002acf6dc229c52f10df8efb62107` | Completado |
| Ampliación del validador a backend, pruebas, frontend y base de datos del módulo | `264a0cf9d163c18e850a701b006624fd072ab92b` | Completado |
| Exclusión controlada de scripts históricos de retiro para evitar falsos positivos | `291c182f2383daa5e9ae77a59789a2a76dcff0c0` | Completado |
| Alineación inicial de CRUD, flujo, proyección, auditoría y trazas con el DDL | `ca1862a942c2ed1be387d4c1955905591555cabc` | Implementado; pendiente de compilación y revisión |
| Fachada transitoria para mantener operativo el flujo de evidencias | `1ea79c8d1780a4f98eacd1a58d672017b732c6b6` | Implementada; pendiente de pruebas |
| Registro de la fachada en inyección de dependencias | `f52d4eb97ce40908a5d8815f9ccc9982d06e691f` | Implementado |

## 3. Alineación realizada

### 3.1 Evaluaciones

La creación y actualización dejan de escribir columnas inexistentes en `RL_MR_EVALUACIONES_RIESGO`.

Se utilizan exclusivamente:

- `EVA_ID`
- `EVA_RIESGO_ID`
- `EVA_VERSION_ID`
- `EVA_DATA_JSON`
- `EVA_DATA_CALC_JSON`
- `EVA_FECHA_REGISTRO`
- `EVA_USR_REGISTRO`
- `EVA_VERSION_ROW`
- `EVA_ACTIVO`

### 3.2 Estados

El estado actual se obtiene del último registro de `RL_MR_FLUJOS_EVALUACION`, utilizando:

- `FLU_ESTADO`
- `FLU_FECHA`
- `FLU_ID`

Cada transición inserta una nueva fila de flujo y actualiza la proyección relacional dentro de la misma transacción.

### 3.3 Proyecciones

Se eliminaron las dependencias de `PROY_ETP` y se utilizan las columnas físicas obligatorias:

- `PROY_CODIGO_RIESGO`
- `PROY_AREA_PRINCIPAL`
- `PROY_VRI`
- `PROY_VRR`
- `PROY_NIVEL_INHERENTE`
- `PROY_NIVEL_RESIDUAL`
- `PROY_RESPUESTA_RIESGO`
- `PROY_ESTADO_EVALUACION`
- `PROY_DUENO_RIESGO`
- `PROY_FECHA_EVAL`

La actualización exige exactamente una proyección por evaluación.

### 3.4 Trazas

La creación y actualización registran trazas en `RL_MR_TRAZAS_CALCULO` con:

- evaluación;
- regla activa;
- entradas dinámicas;
- resultados calculados;
- usuario;
- fecha.

La selección exacta de regla y versión sigue pendiente de la Fase 1.4, donde se conectará con la versión publicada del formulario.

### 3.5 Auditoría

Se alineó la auditoría específica del módulo con las columnas reales:

- `AUD_CAMPO_CLAVE`
- `AUD_VALOR_ANT`
- `AUD_VALOR_NVO`
- `AUD_IP`
- `AUD_USR_ID`
- `AUD_FECHA`

No se utilizan `AUD_ACCION` ni `AUD_DETALLE`, porque no existen en el DDL definitivo.

## 4. Validador ampliado

El validador revisa de manera recursiva:

- backend del módulo;
- pruebas backend del módulo;
- frontend Angular del módulo;
- scripts Oracle activos del módulo.

Bloquea referencias a:

- `FLU_ESTADO_NUEVO`
- `FLU_ESTADO_ANTERIOR`
- `EVA_ESTADO`
- `EVA_VRI`
- `EVA_ETP`
- `EVA_VRR`
- `EVA_FECHA_EVAL`
- `EVA_USR_EVAL`
- `PROY_ETP`
- tablas retiradas del modelo anterior;
- clasificación residual rígida en C#;
- método de auditoría inexistente `RegistrarAuditoriaAsync`.

También verifica que el workflow temporal no exista y que el script `05` conserve sus protecciones.

## 5. Pendientes antes de solicitar cierre de Fase 1.2

1. Ejecutar compilación del backend.
2. Ejecutar las pruebas unitarias existentes.
3. Corregir cualquier consumidor o prueba que todavía dependa de contratos heredados.
4. Ejecutar el validador integral en un clon actualizado.
5. Revisar la fachada transitoria y decidir su consolidación en un único repositorio antes del cierre definitivo.
6. Crear pruebas de integración Oracle para creación, actualización, transición, proyección, traza y rollback.
7. Confirmar que existe una regla activa válida para registrar trazas.
8. Solicitar revisión de Codex sobre los commits publicados.
9. No ejecutar todavía el script `05` en Oracle.

## 6. Estado real

```text
Workflow temporal: eliminado.
Validador integral: ampliado.
CRUD y flujo: alineación inicial implementada.
Proyección: alineación inicial implementada.
Trazas: inserción implementada, selección versionada pendiente.
Auditoría específica: alineada al DDL.
Compilación: no ejecutada.
Pruebas backend: no ejecutadas después de estos cambios.
Pruebas Oracle reales: no ejecutadas.
Script 05 en Oracle: no ejecutado.
Fase 1.2: en revisión, no cerrada.
main: intacta.
```

## 7. Próxima revisión solicitada a Codex

Codex deberá verificar:

- eliminación real de identificadores incompatibles;
- correspondencia de todos los `INSERT`, `UPDATE` y `SELECT` con el DDL;
- atomicidad de evaluación, flujo, proyección, traza y auditoría;
- tratamiento de la regla activa de cálculo;
- ausencia de regresiones en evidencias;
- cobertura del validador;
- necesidad o retiro de la fachada transitoria;
- pruebas requeridas antes de ejecutar Oracle.
