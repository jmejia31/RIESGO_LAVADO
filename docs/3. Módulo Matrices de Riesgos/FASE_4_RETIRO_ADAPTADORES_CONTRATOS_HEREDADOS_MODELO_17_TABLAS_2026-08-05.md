# Fase 4 — Retiro definitivo de adaptadores y contratos heredados

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha:** 2026-08-05.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** completada y validada sin ejecución Oracle.

---

## 1. Objetivo

Eliminar del código activo los últimos adaptadores y contratos vinculados al modelo anterior de tablas puente específicas, dejando exclusivamente el vínculo genérico de evidencias aprobado para el modelo de 17 tablas.

Contrato definitivo:

```text
VincularEvidenciaAsync
RL_MR_EVIDENCIAS_VINCULOS
```

Objetos retirados:

```text
VincularEvidenciaAprobacionAsync
AsociarEvidenciaAprobacionDto
EjecutarVinculoEvidenciaAsync
RL_MR_EVI_APROBACION
PermisoFormularioDto
```

---

## 2. Cambios implementados

### 2.1 Repositorio de Matrices de Riesgos

Archivo:

```text
backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs
```

Se eliminaron:

- el método público temporal `VincularEvidenciaAprobacionAsync`;
- la escritura directa en `RL_MR_EVI_APROBACION`;
- el helper `EjecutarVinculoEvidenciaAsync`;
- la construcción dinámica de nombres de tablas y columnas mediante `tablaPuente`, `columnaEntidad` y `columnaEvidencia`.

Permanece únicamente `VincularEvidenciaAsync`, que:

1. recibe `VincularEvidenciaDto`;
2. limita el destino mediante `TipoEntidadEvidencia`;
3. valida la existencia de la evidencia;
4. valida la entidad mediante consultas cerradas y parametrizadas;
5. inserta en `RL_MR_EVIDENCIAS_VINCULOS`;
6. registra `VINCULAR_EVIDENCIA` en `RL_AUDITORIA` dentro de la misma transacción.

### 2.2 DTO temporal de aprobación

En `EvidenciaDtos.cs` se eliminó `AsociarEvidenciaAprobacionDto`. El contrato vigente conserva solamente los DTO de registro, consulta, descarga y carga de evidencias, además de `TipoEntidadEvidencia` y `VincularEvidenciaDto`.

### 2.3 Permisos granulares huérfanos

Se eliminó:

```text
backend/RL.API/Features/MatricesRiesgos/Contracts/Configuracion/PermisoFormularioDto.cs
```

El módulo continúa utilizando seguridad institucional por usuario, rol y módulo. No se conserva un contrato de permisos por formulario, sección o campo.

### 2.4 Lista cerrada de entidades

Los únicos tipos de destino permitidos son:

```text
Riesgo
Evaluacion
Control
Plan
Actividad
Alerta
Automonitoreo
```

No se reciben desde el cliente nombres de tablas, columnas ni fragmentos SQL.

---

## 3. Validador fortalecido

El archivo `scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1` ahora comprueba:

- ausencia física de `PermisoFormularioDto.cs`;
- ausencia de `AsociarEvidenciaAprobacionDto`;
- ausencia del adaptador de aprobación;
- ausencia del helper dinámico de tablas puente;
- ausencia de las nueve tablas puente específicas en Backend y Frontend activos;
- ausencia de identificadores utilizados para construir SQL dinámico;
- presencia obligatoria de `VincularEvidenciaAsync`;
- presencia obligatoria de `RL_MR_EVIDENCIAS_VINCULOS`;
- presencia de `ObtenerConsultaEntidadEvidencia` como lista cerrada;
- presencia de `SEQ_RL_MR_EVI_VINCULOS`.

El script `06` puede conservar los nombres anteriores únicamente en su sección controlada de retiro físico. No fue ejecutado.

---

## 4. Pruebas automatizadas

Se agregó:

```text
backend/RL.API.Tests/Features/MatricesRiesgos/MatricesRiesgosEvidenceContractTests.cs
```

Las pruebas verifican que:

1. `IMatricesRiesgosRepository` exponga solamente `VincularEvidenciaAsync` para vinculación;
2. `MatricesRiesgosRepository` exponga solamente el vínculo genérico;
3. el ensamblado no contenga los DTO retirados;
4. `TipoEntidadEvidencia` conserve exactamente los siete destinos aprobados.

---

## 5. Commits principales

```text
47e880b1e17205ae2f00864e32426142e0b1eb22
refactor(matrices): retirar adaptadores y contratos heredados [phase4-done]

9096e5f56dbc66d879043e1b3b66bca0c75898ed
docs(matrices): registrar implementacion de fase 4
```

Los archivos auxiliares utilizados para aplicar el corte fueron eliminados dentro del mismo proceso y no permanecen en el repositorio.

---

## 6. Verificación ejecutada

Quality Gate institucional:

```text
Run: 31048708788
Resultado: success
Commit validado: 9096e5f56dbc66d879043e1b3b66bca0c75898ed
```

Etapas aprobadas:

- restauración del backend;
- instalación de dependencias frontend;
- validador de alineación dinámica;
- compilación de la solución en Release;
- instalación de Playwright Chromium;
- pruebas Backend;
- pruebas Frontend y cobertura;
- build Angular;
- pruebas E2E;
- puertas completas del repositorio.

---

## 7. Criterios de cierre

| Criterio | Resultado |
|---|---|
| Adaptador de aprobación eliminado | Cumplido |
| DTO temporal de aprobación eliminado | Cumplido |
| Helper dinámico de tablas puente eliminado | Cumplido |
| Referencias activas a `RL_MR_EVI_*` retiradas | Cumplido |
| `PermisoFormularioDto` eliminado | Cumplido |
| Vínculo genérico conservado | Cumplido |
| Lista cerrada de siete destinos conservada | Cumplido |
| Validador actualizado | Cumplido |
| Pruebas automatizadas incorporadas | Cumplido |
| Compilación Release | Correcta |
| Quality Gates | Correctos |
| Oracle sin ejecutar | Cumplido |
| `main` intacta | Cumplido |

---

## 8. Restricciones vigentes

- `main` permanece intacta;
- el PR #20 permanece abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se eliminaron tablas físicas;
- no se publicaron credenciales.

---

## 9. Resultado y siguiente fase

La **Fase 4 queda completada** en código y validada sin Oracle.

La siguiente intervención es la **Fase 5 — validador exclusivo del inventario exacto de 17 tablas y 17 secuencias**, que deberá comparar el DDL contra una lista cerrada, fallar ante cualquier ausencia o elemento adicional y prohibir la reintroducción de objetos heredados.
