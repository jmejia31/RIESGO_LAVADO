# Fase 4 — Retiro definitivo de adaptadores y contratos heredados

## Módulo Matrices de Riesgos — Modelo reducido de 17 tablas

- **Fecha:** 2026-08-05.
- **Rama de trabajo:** `desarrollo`.
- **Rama estable:** `main` — no modificada.
- **Estado:** implementación completada; Quality Gate institucional en ejecución.

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

Archivo modificado:

```text
backend/RL.API/Features/MatricesRiesgos/Contracts/Evidencias/EvidenciaDtos.cs
```

Se eliminó `AsociarEvidenciaAprobacionDto`. El contrato vigente conserva solamente:

- `EvidenciaDto`;
- `EvidenciaRegistroDto`;
- `EvidenciaDescargaDto`;
- `EvidenciaUploadFormDto`;
- `TipoEntidadEvidencia`;
- `VincularEvidenciaDto`.

### 2.3 Permisos granulares huérfanos

Se eliminó:

```text
backend/RL.API/Features/MatricesRiesgos/Contracts/Configuracion/PermisoFormularioDto.cs
```

El módulo continúa utilizando la seguridad institucional por usuario, rol y módulo. No se conserva un contrato de permisos por formulario, sección o campo.

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

No se permite recibir desde el cliente nombres de tablas, nombres de columnas ni fragmentos SQL.

---

## 3. Validador fortalecido

Archivo:

```text
scripts/validation/validate_matrices_dynamic_ddl_alignment.ps1
```

El validador ahora comprueba:

- ausencia física de `PermisoFormularioDto.cs`;
- ausencia de `AsociarEvidenciaAprobacionDto` en contratos productivos;
- ausencia del adaptador de aprobación;
- ausencia del helper dinámico de tablas puente;
- ausencia de las nueve tablas puente específicas en Backend y Frontend activos;
- ausencia de identificadores para construir SQL dinámico;
- presencia obligatoria de `VincularEvidenciaAsync`;
- presencia obligatoria de `RL_MR_EVIDENCIAS_VINCULOS`;
- presencia de la lista cerrada `ObtenerConsultaEntidadEvidencia`;
- presencia de `SEQ_RL_MR_EVI_VINCULOS`.

El script de transición `06` puede conservar los nombres antiguos únicamente en su sección controlada de retiro físico; no se ejecutó en esta fase.

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

## 5. Commit de implementación

```text
47e880b1e17205ae2f00864e32426142e0b1eb22
refactor(matrices): retirar adaptadores y contratos heredados [phase4-done]
```

El workflow y el script auxiliares utilizados para aplicar el corte fueron eliminados dentro del mismo commit y no permanecen en el repositorio.

---

## 6. Verificaciones previas al Quality Gate

Se comprobó en la rama publicada:

- `VincularEvidenciaAprobacionAsync`: ausente;
- `EjecutarVinculoEvidenciaAsync`: ausente;
- `AsociarEvidenciaAprobacionDto`: ausente;
- `PermisoFormularioDto.cs`: ausente;
- workflow temporal: ausente;
- script temporal: ausente;
- `VincularEvidenciaAsync`: presente;
- `RL_MR_EVIDENCIAS_VINCULOS`: presente;
- validador de Fase 4: presente;
- pruebas de contrato de evidencias: presentes.

El Quality Gate del commit generado por GitHub Actions quedó en `action_required` sin ejecutar jobs. Este documento genera un commit institucional posterior para validar exactamente el mismo estado funcional mediante el flujo completo del PR.

---

## 7. Restricciones vigentes

- `main` permanece intacta;
- el PR #20 permanece abierto y en borrador;
- Oracle no fue ejecutado;
- el script `05` no fue ejecutado;
- el script `06` no fue ejecutado;
- no se eliminaron tablas físicas;
- no se publicaron credenciales.

---

## 8. Criterio pendiente de cierre

La Fase 4 se declarará cerrada únicamente cuando el Quality Gate institucional apruebe:

- validador;
- compilación Release;
- pruebas Backend;
- pruebas Frontend y cobertura;
- build Angular;
- pruebas E2E;
- puertas completas del repositorio.

La siguiente fase, después del cierre verificable, será la **Fase 5 — validador exclusivo del inventario exacto de 17 tablas y 17 secuencias**.
