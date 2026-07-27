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

- **Intervención**: #6.
- **Fecha**: 2026-07-27 08:17, hora de Honduras.
- **Autor**: Codex.
- **Rama**: `desarrollo`.
- **Objetivo**: actualizar el checkout local desde `origin/desarrollo`, confirmar el avance publicado de la Intervención #5 y ejecutar la validación técnica reproducible del plan de cierre de Fase 12.
- **Commit de inicio verificado**: `8ccf973822cfeea3adb8dbccdf43d4075ba741d9`.
- **Estado**: validación técnica local reproducida; validaciones funcionales e institucionales siguen pendientes.

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

## 7. Validación técnica reproducida en la Intervención #6

### 7.1 Ejecutado

- `git fetch --all --prune`.
- `git switch desarrollo`.
- `git pull --ff-only origin desarrollo`.
- Verificación de que `desarrollo` local y `origin/desarrollo` apuntan a `8ccf973822cfeea3adb8dbccdf43d4075ba741d9`.
- Verificación de que los commits `22a5f29`, `cdfde9f` y `8ccf973` están publicados en `desarrollo`.
- `git diff --check`.
- `dotnet restore RIESGO_LAVADO.sln --configfile NuGet.Config`.
- `dotnet build RIESGO_LAVADO.sln --no-restore`.
- `dotnet test RIESGO_LAVADO.sln --configuration Release --no-restore`.
- `npm ci`.
- `npm run build`.
- `npm test -- --watch=false`.
- `npm run e2e`.
- `tools/validate_repository_structure.ps1`.
- `tools/validate_database_scripts.ps1`.
- `tools/validate_documentation_links.ps1`.
- `tools/run_quality_gates.ps1`.

### 7.2 Resultados

| Validación | Resultado |
|---|---|
| Backend build | Correcto, 0 errores, 2 advertencias xUnit2009 |
| Backend tests | 96 aprobadas, 0 fallidas, 0 omitidas |
| Frontend build | Correcto, advertencia conocida por `exceljs` CommonJS |
| Frontend tests | 18 archivos aprobados, 165 pruebas aprobadas |
| E2E | 7 aprobadas |
| Estructura | Correcta; 119 rutas obligatorias, 439 archivos rastreados, 3 maestros SQL |
| Scripts Oracle | Correctos; 19 scripts activos raíz, 1 paquete modular, 22 scripts alcanzables |
| Enlaces documentación | Correctos; 34 Markdown revisados, 41 enlaces locales |
| Quality Gates | Correctos |

### 7.3 Cobertura reportada por Quality Gates

- Backend:
  - líneas: 22.15%;
  - ramas: 21.21%.
- Frontend:
  - sentencias: 38.99%;
  - ramas: 33.51%;
  - funciones: 36.00%;
  - líneas: 39.20%.

### 7.4 Observaciones

- `npm ci` requirió un segundo intento con permisos del entorno por error `EPERM` sobre la caché local de npm.
- `npm ci` reportó 17 vulnerabilidades transitivas. No se ejecutó `npm audit fix` porque no forma parte del cierre y puede modificar dependencias.
- No se detectó daño real de codificación en los documentos colaborativos; la visualización incorrecta de acentos correspondió a salida de consola.
- `.agents/AGENTS.md` difiere de `AGENTS.md` solo por rutas relativas, diferencia permitida por el protocolo.

## 8. Plan formal de cierre

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

## 9. Responsabilidades

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

## 10. Restricciones vigentes

- No alterar DNP.
- No alterar `CONTROL_ALMACEN.PROVEEDOR`.
- No modificar el motor de cálculo sin requerimiento aprobado.
- No modificar Oracle sin respaldo, revisión DBA y autorización.
- Mantener separados Monitoreo de Listas y Matrices de Riesgos.
- Mantener auditoría obligatoria de exportaciones.
- No reducir pruebas o cobertura para aprobar un cambio.
- No declarar cierre o aprobación sin Javier Mejía.

## 11. Punto exacto de continuación

La siguiente intervención debe:

1. ejecutar `git pull --ff-only origin desarrollo`;
2. revisar este documento, la bitácora y el plan de cierre;
3. revisar los resultados técnicos reproducidos en la Intervención #6;
4. ejecutar validación funcional con Excel Desktop y PDF real;
5. coordinar validación Oracle institucional con DBA autorizado;
6. actualizar Documento Maestro de Fase 12 y regenerar checksum;
7. solicitar aprobación formal de Javier Mejía para cerrar Fase 12;
8. no modificar `main` sin autorización expresa.
