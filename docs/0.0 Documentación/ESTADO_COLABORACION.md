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

- **Fecha**: 2026-07-24 10:55, hora de Honduras.
- **Autor**: ChatGPT.
- **Alcance**: revisión de cambios atribuidos a Antigravity, protocolo multiagente, bitácora, documentación central, políticas de ramas, estándar de reportería y prueba de Excel de una hoja.
- **Commit inicial de `desarrollo`**: `d737c3ba1147873a0863d24f9f6383330c611636`.
- **Relación inicial con `main`**: `desarrollo` estaba dos commits detrás de `main`, sin diferencias de archivos. El contenido era equivalente, pero el historial no estaba sincronizado al mismo commit.

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
| `BITACORA_COLABORACION.md` contenía enlaces `file:///c:/...` inutilizables desde GitHub | Confirmado | Sustituir por enlaces relativos |
| La Intervención #1 identificaba el frontend como Angular 19 | Confirmado | El código declara Angular 22; se deja nota correctiva |
| `AGENTS.md` fijaba conteos de 226+ y 27+ pruebas | Confirmado | El protocolo ya no fija cifras; los conteos pertenecen a cada ejecución |
| `CONTRIBUTING.md` ordenaba trabajar directamente en `main` | Confirmado | Alineado con `desarrollo` activa y `main` estable |
| `CLEANUP_REPORT.md` afirmaba que solo existía `main` | Confirmado como dato histórico | Marcado como evidencia histórica y enlazado al estado vigente |
| `QUALITY.md` recomendaba mantener 77 pruebas Backend y 123 Frontend | Confirmado como línea base antigua | Separado de la evidencia vigente y clasificado como histórico |
| `API.md` apuntaba a la carpeta global retirada `backend/RL.API/Controllers` | Confirmado | Corregido hacia `Features/<Modulo>` y `Contracts` |
| El estándar PDF/Excel exigía el utilitario Angular incluso para Matrices, cuyo archivo oficial se genera en Backend | Confirmado | Corregido por capa propietaria |
| La bitácora afirmaba sincronización total entre `main` y `desarrollo` | Parcialmente correcto | Los archivos coincidían, pero `desarrollo` estaba dos commits detrás al iniciar la auditoría |
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

Los documentos históricos mantienen sus métricas de la fecha de cierre, pero ahora deben identificarse como históricos y no como estado vigente.

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

## 7. Evidencia técnica

### Verificado en esta auditoría

- Existencia y contenido de los documentos colaborativos.
- Versiones declaradas en `frontend/rl-app/package.json`.
- Target Framework y proveedor Oracle en `backend/RL.API/RL.API.csproj`.
- Cambio de prueba para exigir exactamente una hoja Excel.
- Divergencia de dos commits entre `desarrollo` y `main` al inicio, sin diferencias de archivos.
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
6. Solicitar autorización de Javier Mejía antes de integrar `desarrollo` en `main`.
