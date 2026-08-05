# Fase 2 — Retiro definitivo de trazas de cálculo

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha:** 2026-08-05.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** completada y validada sin ejecución Oracle.

---

## 1. Objetivo

Eliminar del backend toda dependencia operativa de la tabla local de trazas de cálculo, retirada del modelo objetivo de 17 tablas, sin perder la identificación técnica de la regla aplicada a cada evaluación.

Los objetos retirados del código activo son:

```text
RL_MR_TRAZAS_CALCULO
SEQ_RL_MR_TRAZAS
TRA_REGLA_ID
InsertarTrazaCalculoAsync
```

La trazabilidad del cálculo se conserva dentro de `EVA_CALCULOS_JSON` mediante metadatos controlados por el servidor:

```json
{
  "reglaCodigo": "CALCULO_VRI_VRR",
  "reglaVersion": "1.0",
  "algoritmoId": "MATRICES_VRI_ADITIVO_1_9",
  "vri": 7,
  "vrr": 4
}
```

---

## 2. Cambios implementados

### 2.1 Eliminación de las escrituras de trazas

Archivo:

```text
backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs
```

Se retiraron:

- la llamada de traza durante la creación de evaluaciones;
- la llamada de traza durante la actualización de evaluaciones;
- el método privado que insertaba en `RL_MR_TRAZAS_CALCULO`;
- la utilización de `SEQ_RL_MR_TRAZAS`;
- la dependencia de `TRA_REGLA_ID`.

Crear o actualizar una evaluación ya no requiere que exista la tabla retirada.

### 2.2 Persistencia de metadatos de regla en el JSON calculado

El repositorio continúa validando que:

- la versión del formulario esté publicada;
- la versión esté vigente al crear una evaluación;
- la regla declarada exista en `RL_MR_REGLAS_CALCULO`;
- la regla esté activa;
- exista `REG_ALGORITMO_ID`.

Después de resolver la regla institucional, el servidor incorpora o sobrescribe en `EVA_CALCULOS_JSON`:

```text
reglaCodigo
reglaVersion
algoritmoId
```

Los valores enviados por el cliente para esos campos no son confiables y se sobrescriben con los obtenidos de la versión publicada y del catálogo institucional de reglas.

El resto del resultado calculado, incluidos `vri`, `vrr`, niveles y datos de proyección, se conserva.

### 2.3 Validación de la estructura JSON

Los resultados calculados deben ser un objeto JSON. Arreglos, valores escalares o `null` no pueden utilizarse como `EVA_CALCULOS_JSON`, porque no permiten incorporar de forma segura la referencia de la regla aplicada.

### 2.4 Fortalecimiento del validador

Archivo:

```text
scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1
```

El validador ahora:

- prohíbe en backend, pruebas y frontend las referencias a los objetos de trazas retirados;
- prohíbe el método de inserción de trazas;
- deja de exigir `TRA_REGLA_ID`;
- exige `REG_ALGORITMO_ID`;
- exige `reglaCodigo`, `reglaVersion` y `algoritmoId` en la implementación;
- verifica que el script `06` no cree la tabla ni la secuencia retiradas;
- conserva la comprobación de que el script `06` elimine la tabla heredada durante la reconstrucción controlada;
- impide que la suite Oracle reintroduzca objetos de trazas.

### 2.5 Pruebas automatizadas

Se agregó:

```text
backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosCalculationMetadataTests.cs
```

La prueba verifica que:

1. los metadatos enviados por el cliente sean reemplazados por los valores institucionales;
2. `vri` y `vrr` permanezcan intactos;
3. un resultado calculado que no sea objeto JSON sea rechazado.

---

## 3. Commits publicados

```text
fab207abf4eec51a9d2adf02a4906e49907d6859
refactor(matrices): retirar trazas y persistir metadatos de regla

1afa5910a3b00d2d1e5f511a7f657d32304f88cb
test(matrices): prohibir trazas y exigir metadatos de regla

1014746fe22204e7e7cf4c585e3bf9be90916e12
test(matrices): cubrir metadatos institucionales de calculo

56b1913c6abc65dc99eae73be4355a41a6170a82
test(matrices): compatibilizar prueba de metadatos de calculo
```

---

## 4. Verificación ejecutada

Quality Gate de GitHub Actions:

```text
Run: 31043691118
Resultado: success
Commit validado: 56b1913c6abc65dc99eae73be4355a41a6170a82
```

Etapas aprobadas:

- restauración del backend;
- instalación de dependencias frontend;
- validador de alineación dinámica;
- compilación de la solución en Release;
- instalación de Playwright Chromium;
- pruebas backend;
- pruebas frontend y cobertura;
- build Angular;
- pruebas E2E;
- puertas completas del repositorio.

---

## 5. Elementos no modificados

Esta fase no adelantó trabajos de fases posteriores. Permanecen pendientes:

- migración de `RL_MR_AUDITORIA` y `SEQ_RL_MR_AUDITORIA` hacia `RL_AUDITORIA`;
- retiro del adaptador interno de aprobación y de la lógica genérica para tablas puente heredadas;
- eliminación de `AsociarEvidenciaAprobacionDto`;
- eliminación de `PermisoFormularioDto`;
- certificación física Oracle de las 17 tablas.

---

## 6. Restricciones vigentes

- `main` permanece intacta;
- el PR #20 permanece abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se ejecutaron eliminaciones físicas;
- no se publicaron credenciales.

---

## 7. Criterios de cierre

| Criterio | Resultado |
|---|---|
| Llamadas de inserción de trazas retiradas | Cumplido |
| Método de inserción de trazas retirado | Cumplido |
| Tabla, secuencia y columna de trazas ausentes del código activo | Cumplido |
| Regla resuelta desde versión publicada y catálogo institucional | Cumplido |
| Código, versión y algoritmo persistidos en `EVA_CALCULOS_JSON` | Cumplido |
| Metadatos del cliente sobrescritos por el servidor | Cumplido |
| Pruebas automatizadas incorporadas | Cumplido |
| Validador actualizado | Cumplido |
| Compilación Release | Correcta |
| Quality Gates | Correctos |
| Oracle sin ejecutar | Cumplido |
| `main` intacta | Cumplido |

---

## 8. Resultado y siguiente fase

La **Fase 2 queda completada** en código y validada sin Oracle.

El siguiente trabajo autorizado es la **Fase 3 — migración de auditoría local hacia la auditoría institucional**, que deberá:

1. eliminar `InsertarAuditoriaCampoAsync`;
2. retirar las escrituras a `RL_MR_AUDITORIA` y `SEQ_RL_MR_AUDITORIA`;
3. utilizar `IAuditoriaRepository.RegistrarAsync` dentro de la misma conexión y transacción Oracle;
4. cubrir creación, actualización, transición y cualquier vínculo que todavía use auditoría local;
5. actualizar pruebas y validador;
6. mantener Oracle y el script `06` bloqueados.
