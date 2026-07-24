# Estado de colaboración y punto de continuidad

> Documento vivo. Debe actualizarse al finalizar cada intervención junto con [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

## 1. Línea base vigente

- **Repositorio**: `jmejia31/RIESGO_LAVADO`.
- **Rama de trabajo obligatoria**: `desarrollo`.
- **Rama estable**: `main`.
- **Aprobador final**: Javier Mejía (`jmejia31`).
- **Regla**: no integrar ni modificar `main` sin autorización expresa.
- **Arquitectura**: monolito modular con Angular, ASP.NET Core y Oracle.
- **Frontend verificado en código**: Angular `22.0.3`, Angular CLI `22.0.4`, TypeScript `6.0.3`, Node `24.18.0`, npm `11.12.1`.
- **Backend verificado en código**: .NET `10.0`, `Oracle.ManagedDataAccess.Core` `23.4.0`.

## 2. Última auditoría colaborativa

- **Fecha**: 2026-07-24 11:24, hora de Honduras.
- **Autor**: Antigravity.
- **Alcance**: verificación del estado técnico y de fase del proyecto; incorporación de la regla de publicación obligatoria al protocolo.
- **Commit inicial de `desarrollo`**: `d693dd740acc7622c4a401160506f5f881186a85`.
- **Relación `desarrollo`/`main`**: `desarrollo` está 11 commits documentales adelante de `main`; `main` conserva 2 commits de merge que no están en `desarrollo`. Los 10 archivos de diferencia son exclusivamente documentales. No debe usarse `push --force`.

## 3. Estado técnico confirmado en la Intervención #4

### Módulos Backend (`Features/`)

| Módulo | Estado |
|---|---|
| Auditoria | Módulo vertical activo |
| Catalogos | Módulo vertical activo |
| Configuracion | Módulo vertical activo |
| Identidad | Módulo vertical activo (Auth + Usuarios) |
| Listas | Módulo vertical activo |
| MatricesRiesgos | Módulo vertical activo — reporte Excel en 1 hoja |

### Módulos de prueba Backend (`RL.API.Tests/Features/`)

`Auditoria`, `Catalogos`, `Configuracion`, `Identidad`, `Listas`, `MatricesRiesgos` + `ModuleBoundariesTests.cs`.

### Frontend (`features/`)

`admin`, `auth`. Carga bajo demanda desde la Fase 3.

### Fases completadas

- Fases 1–12: reorganización arquitectónica completa.
- Fases 13–21: calidad, cobertura, E2E y cierre.
- **Cierre formal Fase 10 Matrices de Riesgos**: aprobado por Javier Mejía el 2026-07-16.
- **Reporte Excel ejecutivo de Matrices**: unificado en una sola hoja (prueba de regresión activa).

### Evidencia técnica verificada en la Intervención #4

- Estructura de directorios Backend, Tests y Frontend confirmada.
- Log de commits de `desarrollo` y `main` consultados.
- Divergencia de ramas verificada con `git diff --stat`.
- Estado local con un archivo modificado sin confirmar: `tools/validate_repository_structure.ps1`.

### No ejecutado en la Intervención #4

- `dotnet test`, `npm test`, `npm run build`, E2E, validadores PowerShell, Oracle institucional, AD, SMTP.
- Razón: intervención de alcance documental/protocolo.

## 4. Cambios de Antigravity en Intervenciones #1 y #2

### Intervención #1

Antigravity creó:

- [`AGENTS.md`](../../AGENTS.md).
- [`.agents/AGENTS.md`](../../.agents/AGENTS.md).
- [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

También reportó inspección integral, sincronización de la rama de Fase 12 y ejecución local de pruebas Backend y Frontend.

### Intervención #2

Antigravity reportó:

- integración de Fase 12 en `main`;
- creación de `desarrollo` como rama activa;
- eliminación de ramas obsoletas;
- actualización de la prueba `MatricesRiesgosApplicationTests.cs` para exigir un solo worksheet en el Excel ejecutivo;
- ejecución local de 226 pruebas Backend, 165 pruebas Frontend en 18 archivos y build Frontend aprobado;
- corrección documental de Angular 19 a Angular 22.

La aserción de una sola hoja coincide con el comportamiento vigente del reporte ejecutivo de Matrices de Riesgos.

## 5. Cambios publicados en `desarrollo` — Intervenciones #3 y #4

### Intervención #3 (ChatGPT)

- Fortalecimiento del protocolo multiagente y política de ramas.
- Sincronización de la copia operativa `.agents/AGENTS.md`.
- Publicación del flujo colaborativo en `README.md`.
- Alineación de `CONTRIBUTING.md` con la rama `desarrollo`.
- Corrección de la ubicación vigente de controladores y contratos en `API.md`.
- Separación entre calidad histórica y validaciones actuales en `QUALITY.md`.
- Clasificación de `CLEANUP_REPORT.md` como evidencia histórica.
- Corrección del estándar PDF/Excel según la capa que genera el archivo.
- Creación de `ESTADO_COLABORACION.md`.
- Actualización de `BITACORA_COLABORACION.md` con la Intervención #3.

### Intervención #4 (Antigravity)

- Nueva sección 5 en `AGENTS.md` y `.agents/AGENTS.md`: «Publicación obligatoria al finalizar cada intervención» — todo colaborador debe hacer `git push origin desarrollo` antes de cerrar su turno.
- Actualización de `BITACORA_COLABORACION.md` con la Intervención #4.
- Actualización de este documento.

## 6. Hallazgos históricos de la auditoría de ChatGPT (Intervención #3)

| Hallazgo | Estado | Tratamiento |
|---|---|---|
| `BITACORA_COLABORACION.md` contenía enlaces `file:///c:/...` | Confirmado | Sustituidos por enlaces relativos |
| La Intervención #1 identificaba el frontend como Angular 19 | Confirmado | El código declara Angular 22; nota correctiva |
| `AGENTS.md` fijaba conteos de 226+ y 27+ pruebas | Confirmado | El protocolo ya no fija cifras |
| `CONTRIBUTING.md` ordenaba trabajar directamente en `main` | Confirmado | Alineado con `desarrollo` activa |
| `CLEANUP_REPORT.md` afirmaba que solo existía `main` | Confirmado como dato histórico | Marcado como evidencia histórica |
| `QUALITY.md` recomendaba mantener 77/123 pruebas | Confirmado como línea base antigua | Separado de evidencia vigente |
| `API.md` apuntaba a la carpeta global retirada `Controllers` | Confirmado | Corregido hacia `Features/<Modulo>` |
| El estándar PDF/Excel exigía utilitario Angular incluso para Matrices | Confirmado | Corregido por capa propietaria |
| `desarrollo` afirmada como sincronizada siendo 2 commits detrás | Parcialmente correcto | Nota de divergencia registrada |
| Resultados de pruebas de Antigravity | Reportados, no reproducidos | Sin ejecuciones CI asociadas |

## 7. Restricciones que permanecen

- No alterar DNP.
- No alterar `CONTROL_ALMACEN.PROVEEDOR`.
- No modificar el motor de cálculo sin requerimiento aprobado.
- No modificar la estructura Oracle sin respaldo, revisión DBA y autorización.
- Mantener separados Monitoreo de Listas y Matrices de Riesgos.
- Mantener auditoría obligatoria de exportaciones.
- No declarar cierre o aprobación sin Javier Mejía.

## 8. Punto exacto de continuación

1. Iniciar desde `desarrollo`: `git pull --ff-only origin desarrollo`.
2. Leer `AGENTS.md`, la bitácora y este estado.
3. Revisar si el cambio en `tools/validate_repository_structure.ps1` es intencional; confirmarlo si corresponde.
4. Ejecutar `dotnet build`, `dotnet test`, `npm run build`, `npm test` y E2E.
5. Ejecutar los cuatro validadores PowerShell: estructura, base de datos, enlaces y calidad integral.
6. Registrar conteos y commits reales como Intervención #5.
7. Planificar la reconciliación controlada de `main` y `desarrollo` con autorización de Javier Mejía.
8. No integrar a `main` sin autorización expresa.


## 3. Cambios revisados de Antigravity

### Intervención #1

Antigravity creó:

- [`AGENTS.md`](../../AGENTS.md).
- [`.agents/AGENTS.md`](../../.agents/AGENTS.md).
- [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

También reportó inspección integral, sincronización de la rama de Fase 12 y ejecución local de pruebas Backend y Frontend.

### Intervención #2

Antigravity reportó:

- integración de Fase 12 en `main`;
- creación de `desarrollo` como rama activa;
- eliminación de ramas obsoletas;
- actualización de la prueba `MatricesRiesgosApplicationTests.cs` para exigir un solo worksheet en el Excel ejecutivo;
- ejecución local de 226 pruebas Backend, 165 pruebas Frontend en 18 archivos y build Frontend aprobado;
- corrección documental de Angular 19 a Angular 22.

La aserción de una sola hoja coincide con el comportamiento vigente del reporte ejecutivo de Matrices de Riesgos.

## 4. Hallazgos de la auditoría

| Hallazgo | Estado | Tratamiento |
|---|---|---|
| `BITACORA_COLABORACION.md` contenía enlaces `file:///c:/...` inutilizables desde GitHub | Confirmado | Sustituidos por enlaces relativos |
| La Intervención #1 identificaba el frontend como Angular 19 | Confirmado | El código declara Angular 22; se dejó nota correctiva |
| `AGENTS.md` fijaba conteos de 226+ y 27+ pruebas | Confirmado | El protocolo ya no fija cifras; los conteos pertenecen a cada ejecución |
| `CONTRIBUTING.md` ordenaba trabajar directamente en `main` | Confirmado | Alineado con `desarrollo` activa y `main` estable |
| `CLEANUP_REPORT.md` afirmaba que solo existía `main` | Confirmado como dato histórico | Marcado como evidencia histórica y enlazado al estado vigente |
| `QUALITY.md` recomendaba mantener 77 pruebas Backend y 123 Frontend | Confirmado como línea base antigua | Separado de la evidencia vigente y clasificado como histórico |
| `API.md` apuntaba a la carpeta global retirada `backend/RL.API/Controllers` | Confirmado | Corregido hacia `Features/<Modulo>` y `Contracts` |
| El estándar PDF/Excel exigía el utilitario Angular incluso para Matrices, cuyo archivo oficial se genera en Backend | Confirmado | Corregido por capa propietaria |
| La bitácora afirmaba sincronización total entre `main` y `desarrollo` | Parcialmente correcto | Los archivos coincidían al iniciar, pero `desarrollo` estaba dos commits detrás |
| Resultados de pruebas de Antigravity | Reportados, no reproducidos | No existían ejecuciones CI asociadas a los commits revisados |

## 5. Documentación central revisada

Se revisaron directamente:

- `README.md`.
- `AGENTS.md` y `.agents/AGENTS.md`.
- `BITACORA_COLABORACION.md`.
- `ARCHITECTURE.md`.
- `ESTRUCTURA_OBJETIVO.md`.
- `PLAN_REORGANIZACION.md`.
- `CONTRIBUTING.md`.
- `QUALITY.md`.
- `CHANGELOG.md`.
- `API.md`.
- `DATABASE.md`.
- `DEPLOYMENT.md`.
- `SECURITY.md`.
- `CLEANUP_REPORT.md`.
- `REPORT_PARITY_STANDARD.md`.
- Proyecto Frontend y Backend para comprobar versiones reales.
- Prueba Backend de exportación de Matrices para comprobar la exigencia de una única hoja.

Los documentos históricos mantienen sus métricas de la fecha de cierre, pero deben identificarse como históricos y no como estado vigente.

## 6. Correcciones publicadas en `desarrollo`

- Fortalecimiento del protocolo multiagente y política de ramas.
- Sincronización de la copia operativa `.agents/AGENTS.md`.
- Publicación del flujo colaborativo en `README.md`.
- Alineación de `CONTRIBUTING.md` con la rama `desarrollo`.
- Corrección de la ubicación vigente de controladores y contratos en `API.md`.
- Separación entre calidad histórica y validaciones actuales en `QUALITY.md`.
- Clasificación de `CLEANUP_REPORT.md` como evidencia histórica.
- Corrección del estándar PDF/Excel según la capa que genera el archivo.
- Creación de este documento vivo de continuidad.
- Actualización de `BITACORA_COLABORACION.md` con la Intervención #3.

## 7. Evidencia técnica

### Verificado en esta auditoría

- Existencia y contenido de los documentos colaborativos.
- Versiones declaradas en `frontend/rl-app/package.json`.
- Target Framework y proveedor Oracle en `backend/RL.API/RL.API.csproj`.
- Cambio de prueba para exigir exactamente una hoja Excel.
- Divergencia inicial de dos commits entre `desarrollo` y `main`, sin diferencias de archivos.
- Divergencia final controlada por los commits documentales publicados en `desarrollo`.
- Ausencia de estados y ejecuciones CI asociados a los commits de Antigravity consultados.

### No ejecutado en esta auditoría

- `dotnet test`.
- `npm test`.
- `npm run build`.
- E2E.
- Validadores PowerShell.
- Oracle institucional, Active Directory y SMTP.

Razón: la intervención se realizó mediante revisión y actualización directa del repositorio remoto; no se dispuso de un checkout ejecutable autenticado del repositorio privado. Los resultados heredados no se presentan como reproducidos.

## 8. Restricciones que permanecen

- No alterar DNP.
- No alterar `CONTROL_ALMACEN.PROVEEDOR`.
- No modificar el motor de cálculo sin requerimiento aprobado.
- No modificar la estructura Oracle sin respaldo, revisión DBA y autorización.
- Mantener separados Monitoreo de Listas y Matrices de Riesgos.
- Mantener auditoría obligatoria de exportaciones.
- No declarar cierre o aprobación sin Javier Mejía.

## 9. Punto exacto de continuación

1. Iniciar siempre desde `desarrollo` actualizada.
2. Leer `AGENTS.md`, la bitácora y este estado.
3. Ejecutar validación de enlaces y estructura después de estos cambios documentales.
4. Ejecutar Backend, Frontend, build y E2E antes de una integración.
5. Registrar los resultados reales, incluidos conteos y commits, en ambos documentos colaborativos.
6. Reconciliar la historia de `main` y `desarrollo` mediante una operación revisada; no usar `push --force`.
7. Solicitar autorización de Javier Mejía antes de integrar `desarrollo` en `main`.
