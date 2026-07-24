# Estado de colaboración y punto de continuidad

> Documento vivo. Debe actualizarse al finalizar cada intervención junto con [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

## 1. Línea base vigente

- **Repositorio**: `jmejia31/RIESGO_LAVADO`.
- **Rama de trabajo obligatoria**: `desarrollo`.
- **Rama estable**: `main`.
- **Aprobador final**: Javier Mejía (`jmejia31`).
- **Regla**: no modificar ni integrar `main` sin autorización expresa.
- **Arquitectura**: monolito modular con Angular, ASP.NET Core y Oracle.
- **Frontend declarado**: Angular `22.0.3`, Angular CLI `22.0.4`, TypeScript `6.0.3`, Node `24.18.0`, npm `11.12.1`.
- **Backend declarado**: .NET `10.0`, `Oracle.ManagedDataAccess.Core` `23.4.0`.

## 2. Última intervención

- **Intervención**: #5.
- **Fecha**: 2026-07-24 11:56, hora de Honduras.
- **Autor**: ChatGPT.
- **Rama**: `desarrollo`.
- **Objetivo**: iniciar el cierre formal de la Fase 12, revisar el handoff recibido, limpiar el estado colaborativo duplicado y establecer un plan operativo único.
- **Commit de inicio verificado**: `4887801d53a5310117d6642cd34b66f1afa50b73`.
- **Primer commit de esta intervención**: `22a5f29e78daeacd4822dd704b82d1a878b029c0`.

## 3. Estado de fases

### 3.1 Programa de reorganización

Las fases de reorganización arquitectónica y calidad 1–21 están documentadas como completadas. No corresponde abrir una nueva fase de reorganización por continuidad numérica.

### 3.2 Matrices de Riesgos

- La Fase 10 histórica del módulo fue cerrada y aprobada el 2026-07-16.
- La Fase 12 de mejora ejecutiva y reportería tiene desarrollo técnico avanzado.
- La última subfase documentada es **12.5.6**.
- El Excel ejecutivo de Matrices genera una sola hoja llamada `Reporte Ejecutivo`.
- El Excel conserva las siete secciones funcionales del PDF.
- El PDF aprobado no fue sustituido ni modificado por la corrección de hoja única.

### 3.3 Dictamen vigente

La fase que continúa es:

**Fase 12 — cierre formal y validación institucional posterior a 12.5.6.**

No debe declararse una Fase 13 hasta completar validaciones, Documento Maestro, checksum y aprobación de Javier Mejía.

## 4. Estado de componentes

| Componente | Estado conocido |
|---|---|
| Backend modular | Activo |
| Frontend Angular | Activo |
| Oracle | Scripts aprobados y validadores disponibles |
| Monitoreo de Listas | Reporterías PDF/Excel estandarizadas en código |
| Matrices de Riesgos | Excel ejecutivo de una sola hoja implementado |
| Auditoría de exportaciones | Debe permanecer obligatoria |
| Documento Maestro Fase 12 | Requiere actualización final |
| Checksum final | Pendiente de regeneración después del documento definitivo |

## 5. Relación entre ramas

La comparación al inicio de la Intervención #5 confirmó que:

- `desarrollo` estaba **12 commits adelante** de `main`;
- `desarrollo` estaba **2 commits detrás** de `main`;
- las ramas estaban divergidas;
- la diferencia incluía documentación colaborativa y un ajuste en `tools/validate_repository_structure.ps1`;
- no debe usarse `push --force`.

La reconciliación debe realizarse mediante revisión controlada y validación completa. Integrar a `main` requiere autorización expresa de Javier.

## 6. Cambios de la Intervención #5

### 6.1 Ejecutado

- Lectura de `AGENTS.md`, bitácora y estado colaborativo.
- Revisión del handoff de Antigravity.
- Confirmación del commit remoto de la Intervención #4.
- Confirmación de la divergencia `main`/`desarrollo`.
- Detección de contenido duplicado dentro de este documento vivo.
- Creación del plan operativo:
  - [`PLAN_CIERRE_FORMAL_FASE_12.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/Fase%2012%20-%20Mejora%20ejecutiva%20UXUI%20y%20mapa%20de%20calor/PLAN_CIERRE_FORMAL_FASE_12.md).
- Reconstrucción de este documento sin bloques históricos duplicados.

### 6.2 No ejecutado

- `dotnet restore`, build y pruebas Backend.
- `npm ci`, build, pruebas Frontend y E2E.
- Validadores PowerShell.
- Quality Gates.
- Excel Desktop.
- PDF con datos institucionales reales.
- Oracle institucional, Active Directory y SMTP.

Razón: esta intervención se ejecuta mediante revisión y publicación remota del repositorio; no existe un checkout local ejecutable conectado a los servicios institucionales.

## 7. Plan formal de cierre

El plan operativo completo está en:

[`docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor/PLAN_CIERRE_FORMAL_FASE_12.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/Fase%2012%20-%20Mejora%20ejecutiva%20UXUI%20y%20mapa%20de%20calor/PLAN_CIERRE_FORMAL_FASE_12.md)

Orden obligatorio:

1. revisar y reconciliar ramas;
2. ejecutar Backend;
3. ejecutar Frontend y E2E;
4. ejecutar validadores y Quality Gates;
5. validar Excel Desktop y PDF real;
6. validar Oracle institucional;
7. actualizar Documento Maestro;
8. regenerar checksum;
9. obtener aprobación formal de Javier;
10. integrar a `main` únicamente si Javier lo autoriza.

## 8. Responsabilidades

| Actividad | Responsable |
|---|---|
| Auditoría de código y documentación | ChatGPT/colaborador técnico |
| Correcciones y regresiones | ChatGPT/colaborador técnico |
| Ejecución local y CI | Desarrollador con checkout o CI |
| Excel Desktop | Javier o usuario funcional |
| Reportes con datos reales | Usuario institucional autorizado |
| Oracle | DBA autorizado |
| AD/SMTP | Infraestructura institucional |
| Aprobación y cierre | Javier Mejía |

## 9. Restricciones vigentes

- No alterar DNP.
- No alterar `CONTROL_ALMACEN.PROVEEDOR`.
- No modificar el motor de cálculo sin requerimiento aprobado.
- No modificar Oracle sin respaldo, revisión DBA y autorización.
- Mantener separados Monitoreo de Listas y Matrices de Riesgos.
- Mantener auditoría obligatoria de exportaciones.
- No reducir pruebas o cobertura para aprobar un cambio.
- No declarar cierre o aprobación sin Javier Mejía.

## 10. Punto exacto de continuación

La siguiente intervención con entorno ejecutable debe:

1. ejecutar `git pull --ff-only origin desarrollo`;
2. revisar este documento, la bitácora y el plan de cierre;
3. inspeccionar el ajuste de `tools/validate_repository_structure.ps1`;
4. ejecutar Backend, Frontend, E2E y los cuatro validadores;
5. registrar resultados reales y el commit exacto;
6. no modificar `main` sin autorización.
