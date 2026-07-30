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

- **Intervención**: #14.
- **Fecha**: 2026-07-30 11:45, hora local.
- **Autor**: Antigravity.
- **Rama**: `desarrollo`.
- **Objetivo**: Diseñar los borradores protegidos DDL y scripts de base de datos bajo la nomenclatura `RL_MR_*` para la Fase 1, e implementar las correcciones de diseño de paridad e integridad física.
- **Commit de inicio verificado**: `364dc60b43ff27b60e9d6df547902e88a03ca63e` (previo).
- **Estado**: Plan de Implementación Técnica 1.0 aprobado; borradores de DDL y retiro seguro creados bajo protección PL/SQL de seguridad; repositorio validado estructuralmente al 100%.

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

La fase vigente queda:

**Fase 12 — aprobada y cerrada por autorización formal de Javier Mejía.**

La continuidad posterior se ha plasmado en el [Análisis y Plan Definitivo de Implementación del Módulo Matrices de Riesgos](../3.%20Módulo%20Matrices%20de%20Riesgos/ANALISIS_PLAN_DEFINITIVO_MATRIZ_RIESGOS.md), que define la arquitectura y el desarrollo dinámico de 0 a 100% parametrizado por JSON en Oracle.

## 4. Estado de componentes

| Componente | Estado conocido |
|---|---|
| Backend modular | Activo |
| Frontend Angular | Activo |
| Oracle | Scripts aprobados y validadores disponibles |
| Monitoreo de Listas | Reporterías PDF/Excel estandarizadas en código |
| Matrices de Riesgos | Excel ejecutivo de una sola hoja implementado |
| Auditoría de exportaciones | Debe permanecer obligatoria |
| Documento final de análisis 0–100% | Versión 1.2 aprobada en `.docx` nativo |
| Documentos consolidados anteriores | Antecedentes históricos, no línea base vigente |

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

## 8. Cierre formal de Fase 12

El plan operativo de cierre quedó ejecutado con aprobación formal de Javier Mejía. El documento de referencia permanece en:

[`docs/3. Módulo Matrices de Riesgos/Fase 12 - Mejora ejecutiva UXUI y mapa de calor/PLAN_CIERRE_FORMAL_FASE_12.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/Fase%2012%20-%20Mejora%20ejecutiva%20UXUI%20y%20mapa%20de%20calor/PLAN_CIERRE_FORMAL_FASE_12.md)

Evidencia de cierre:

1. validación técnica reproducida en la Intervención #6;
2. aprobación formal recibida de Javier Mejía en la Intervención #7;
3. documento maestro actualizado con cierre formal;
4. checksum SHA-256 regenerado;
5. integración a `main` autorizada expresamente.

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

1. Tomar como línea base el [`Analisis_Definitivo_Modulo_Matrices_de_Riesgos.docx`](../../Analisis%20Matrices%20de%20riesgos%20v2/Analisis_Definitivo_Modulo_Matrices_de_Riesgos.docx), versión 1.1.
2. Iniciar el tramo 0–5%: diccionario de 82 campos, catálogos y mapeo de reglas.
3. Convertir VRI, ETP, VRR y las reglas auxiliares verificadas en el libro en casos de paridad, y obtener aprobación funcional antes de implementarlas como reglas institucionales.
4. Mantener separados los resultados históricos de Fase 12 de las pruebas del nuevo desarrollo.
5. Trabajar exclusivamente sobre `desarrollo` y actualizar la bitácora y este estado vivo en cada intervención.
