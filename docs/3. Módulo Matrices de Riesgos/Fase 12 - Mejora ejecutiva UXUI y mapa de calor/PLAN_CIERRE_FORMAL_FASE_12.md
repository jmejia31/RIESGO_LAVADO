# Plan operativo de cierre formal — Fase 12

## 1. Propósito

Este documento define el orden obligatorio para cerrar formalmente la Fase 12 del módulo Matrices de Riesgos después de la subfase 12.5.6. No constituye aprobación ni cierre por sí mismo.

La aprobación final corresponde exclusivamente a Javier Mejía.

## 2. Estado de partida

- Rama activa: `desarrollo`.
- Rama estable: `main`.
- `main` no debe modificarse sin autorización expresa.
- El reporte ejecutivo Excel de Matrices de Riesgos genera una sola hoja denominada `Reporte Ejecutivo`.
- El Excel replica las siete secciones funcionales del PDF.
- La evidencia 12.5.6 conserva pendiente la ejecución CI del estado final.
- El Documento Maestro y su checksum deben reconciliarse con las correcciones posteriores.
- Las validaciones con Excel Desktop, datos reales y Oracle institucional no están sustituidas por pruebas documentales.

## 3. Criterios de cierre

La Fase 12 solo puede declararse cerrada cuando todos los controles siguientes estén aprobados y documentados.

| Control | Responsable principal | Estado inicial |
|---|---|---|
| Reconciliación controlada de `main` y `desarrollo` | Desarrollo + Javier | Pendiente |
| Build Backend | Desarrollo/CI | Pendiente |
| Pruebas Backend | Desarrollo/CI | Pendiente |
| Build Frontend | Desarrollo/CI | Pendiente |
| Pruebas Frontend | Desarrollo/CI | Pendiente |
| E2E Playwright | Desarrollo/CI | Pendiente |
| Validador de estructura | Desarrollo/CI | Pendiente |
| Validador de scripts Oracle | Desarrollo/CI | Pendiente |
| Validador de enlaces | Desarrollo/CI | Pendiente |
| Quality Gates completos | Desarrollo/CI | Pendiente |
| Excel Desktop con archivo real | Javier/usuario funcional | Pendiente |
| PDF con datos reales | Javier/usuario funcional | Pendiente |
| Oracle institucional | DBA autorizado | Pendiente |
| Documento Maestro final | ChatGPT/colaborador técnico | Pendiente |
| Checksum SHA-256 final | ChatGPT/colaborador técnico | Pendiente |
| Aprobación funcional | Javier Mejía | Pendiente |

## 4. Bloque A — Reconciliación de ramas

### 4.1 Situación actual

`main` y `desarrollo` tienen historias divergentes. `desarrollo` contiene las actualizaciones colaborativas y documentales; `main` conserva commits de merge que no forman parte de la historia de `desarrollo`.

### 4.2 Reglas

- No usar `push --force`.
- No mover referencias manualmente para ocultar la divergencia.
- No integrar `desarrollo` en `main` sin autorización de Javier.
- Revisar individualmente los commits exclusivos de ambas ramas.
- Ejecutar nuevamente todas las pruebas después de reconciliar.

### 4.3 Procedimiento recomendado

```powershell
git fetch --all --prune
git switch desarrollo
git pull --ff-only origin desarrollo
git log --oneline --left-right --cherry-pick main...desarrollo
git diff --stat main...desarrollo
```

La estrategia de merge o rebase debe aprobarse después de revisar conflictos potenciales en documentación colaborativa.

## 5. Bloque B — Validación técnica reproducible

Ejecutar desde un checkout limpio y actualizado.

### 5.1 Backend

```powershell
dotnet restore RIESGO_LAVADO.sln --configfile NuGet.Config
dotnet build RIESGO_LAVADO.sln --no-restore
dotnet test RIESGO_LAVADO.sln --configuration Release --no-restore
```

Registrar:

- versión de .NET;
- cantidad total de pruebas;
- cantidad aprobada, omitida y fallida;
- duración;
- commit exacto;
- errores y correcciones aplicadas.

### 5.2 Frontend

```powershell
cd frontend/rl-app
npm ci
npm run build
npm test -- --watch=false
npm run e2e
cd ../..
```

Registrar:

- versiones de Node, npm, Angular y TypeScript;
- cantidad de archivos y pruebas;
- tamaño del bundle;
- cantidad de recorridos E2E;
- commit exacto.

### 5.3 Validadores

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_repository_structure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_database_scripts.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_documentation_links.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/run_quality_gates.ps1
```

No se permite reducir pisos de cobertura, omitir E2E ni eliminar pruebas para aprobar la puerta.

## 6. Bloque C — Validación de reportería

### 6.1 Excel Desktop

Validar archivos reales generados por el sistema:

- una única hoja `Reporte Ejecutivo`;
- apertura sin mensaje de reparación;
- siete secciones en el mismo orden que el PDF;
- filtros y resumen completos;
- documentos e identificadores largos como texto;
- ceros iniciales conservados;
- ausencia de notación científica;
- anchos, combinaciones de celdas y saltos correctos;
- impresión horizontal, una página de ancho y altura automática;
- datos coincidentes con el PDF.

### 6.2 PDF

Validar con datos reales:

- encabezado y pie institucional;
- filtros aplicados;
- totales;
- matrices filtradas;
- resultados por factor;
- mapa de transición;
- matrices alto/crítico;
- planes de acción;
- paginación y continuidad de tablas;
- ausencia de cortes o superposiciones.

### 6.3 Casos mínimos

Probar al menos:

1. sin registros;
2. un registro;
3. múltiples registros;
4. documento largo;
5. niveles alto y crítico;
6. planes vencidos;
7. filtros combinados.

## 7. Bloque D — Oracle institucional

Responsable: DBA o técnico Oracle autorizado.

Requisitos:

- respaldo validado;
- ambiente de pruebas;
- credenciales fuera de Git;
- ejecución de validaciones de solo lectura;
- comprobación de objetos `RL_MR_*`;
- módulos 2 a 10 alineados;
- permisos y secuencias;
- consultas de reportería;
- auditoría de exportaciones;
- rendimiento con datos reales.

No modificar estructura ni datos productivos fuera de los scripts aprobados.

## 8. Bloque E — Documento Maestro y checksum

Actualizar el Documento Maestro con:

- subfase 12.5.6;
- estandarización de reportería;
- correcciones posteriores;
- commits finales;
- resultados reproducidos;
- validación Excel Desktop;
- validación PDF;
- validación Oracle;
- riesgos residuales;
- aprobación formal.

Después de generar la versión final:

```powershell
Get-FileHash "Cierre_Tecnico_Fase_12_Matrices_Riesgos_SGRLA_IHSS.docx" -Algorithm SHA256
```

El archivo `.sha256` debe corresponder exactamente al documento final. Cualquier cambio posterior invalida el checksum y obliga a regenerarlo.

## 9. Bloque F — Aprobación

La Fase 12 solo puede cerrarse cuando Javier Mejía confirme expresamente:

- conformidad funcional;
- conformidad visual;
- aceptación de los archivos reales;
- autorización del Documento Maestro final;
- autorización de integración a `main`, cuando corresponda.

## 10. Evidencias obligatorias

- logs de Backend;
- logs de Frontend;
- resultado E2E;
- resultado de los cuatro validadores;
- archivos PDF y Excel reales;
- capturas de Excel Desktop;
- validación DBA;
- Documento Maestro;
- checksum;
- bitácora actualizada;
- commit final de `desarrollo`;
- aprobación de Javier.

## 11. Restricciones vigentes

- No alterar DNP.
- No alterar `CONTROL_ALMACEN.PROVEEDOR`.
- No cambiar el motor de cálculo sin autorización.
- No modificar Oracle sin respaldo y aprobación DBA.
- Mantener separados Monitoreo de Listas y Matrices de Riesgos.
- Mantener auditoría obligatoria de exportaciones.
- No declarar Fase 13 hasta cerrar formalmente la Fase 12.

## 12. Punto de continuación

La siguiente intervención técnica debe comenzar por un checkout limpio de `desarrollo`, revisar la divergencia con `main` y ejecutar la validación reproducible completa. Los resultados deben registrarse en `BITACORA_COLABORACION.md` y `docs/0.0 Documentación/ESTADO_COLABORACION.md`.
