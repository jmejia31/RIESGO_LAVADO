# Estado de Ejecución — Fase 1.3
## Contratos neutros y retiro del modelo heredado

**Fecha:** 3 de agosto de 2026  
**Rama:** `desarrollo`  
**Rama estable:** `main` — intacta  
**Estado:** en ejecución; no aprobada ni cerrada.

## 1. Dependencias y restricciones

- La Fase 1.2 permanece abierta y pendiente de pruebas Oracle controladas.
- Oracle y el script `05` no se ejecutan durante la Fase 1.3.
- Ningún cambio de esta fase se aplica a `main`.
- Los DTO heredados solo se eliminarán después de confirmar que no quedan consumidores.

## 2. Criterios obligatorios de terminado

1. Ningún contrato, consulta, endpoint, prueba ni componente Angular conserva referencias al modelo heredado.
2. Los reportes utilizan DTOs tipados; no permanece `List<Dictionary<string, object>>`.
3. Los DTO heredados se eliminan únicamente después de confirmar que no existen consumidores.
4. Los contratos dinámicos conservan versión de formulario, secciones, campos, catálogos y reglas.
5. Se actualizan las pruebas backend y frontend afectadas.
6. Deben aprobar el validador, la compilación Release y la suite backend.
7. Las pruebas Oracle permanecen pendientes de la Fase 1.2.

## 3. Entregas controladas

### Fase 1.3.1 — Inventario y frontera de contratos

- Identificar contratos heredados en backend y Angular.
- Identificar endpoints, repositorios, AppService, componentes y pruebas consumidores.
- Publicar métodos neutros y tipados sin retirar todavía el contrato anterior.
- Marcar el contrato anterior como transitorio y obsoleto.

**Estado:** iniciada.

### Fase 1.3.2 — Migración backend

- Migrar repositorio a `RiesgoReporteFilaDto`.
- Migrar AppService y controlador a contratos tipados.
- Migrar metodología a `MetodologiaFormularioDto`.
- Actualizar pruebas backend.
- Eliminar puentes backend tras confirmar cero consumidores.

**Estado:** controlador migrado; repositorio y AppService pendientes de implementación directa.

### Fase 1.3.3 — Migración Angular

- Sustituir modelos de factores, variables, modelos y sujetos por contratos de formulario dinámico.
- Tipar consolidado, metodología, dashboard y reportería.
- Actualizar servicio, componente, HTML y pruebas.
- Eliminar modelos TypeScript heredados tras confirmar cero consumidores.

**Estado:** pendiente.

### Fase 1.3.4 — Retiro definitivo y certificación

- Eliminar DTOs y métodos obsoletos.
- Ampliar el validador para bloquear reintroducciones.
- Confirmar cero coincidencias funcionales del modelo anterior.
- Ejecutar compilación Release, pruebas backend y pruebas frontend afectadas.
- Documentar resultados reales y solicitar revisión de Codex.

**Estado:** pendiente.

## 4. Commits iniciales

| Cambio | Commit | Estado |
|---|---|---|
| Contrato tipado inicial del repositorio | `4f9c87272008d4f58958d14139fcc03b9d60ce3b` | reemplazado por migración controlada |
| Contrato tipado inicial de AppService | `35d8118eaa10bb814e57ebbc406dfedb49bc1b92` | reemplazado por migración controlada |
| Puente controlado de repositorio | `ca8c39b96492d0d79fa79525ff87de6c35d9945f` | temporal; debe eliminarse antes del cierre |
| Puente controlado de AppService | `3feb1df0f41f7e6eca22f4224d4bcd8b296680db` | temporal; debe eliminarse antes del cierre |
| Controlador migrado a contratos tipados | `3a698af849421fe27600a06f27f70ad1923aa4f8` | implementado; pendiente de compilación |

## 5. Inventario inicial confirmado

### Backend heredado

- `MatrizRiesgoDtos.cs` contiene `ModeloId`, `ModeloVersion`, `FactorId`, `VariableId`, `FactorInstitucionalDto`, `VariableMetodologiaRespuestaDto`, escalas y criterios del modelo retirado.
- `MatricesRiesgosRepository` todavía materializa el consolidado como `List<Dictionary<string, object>>`.
- `MatricesRiesgosAppService` todavía implementa los métodos heredados de consolidado y metodología.

### Angular heredado

- `matrices-riesgos.models.ts` mantiene modelos, factores, variables, criterios y agrupaciones `porFactor`.
- `matrices-riesgos.service.ts` consume `MetodologiaMatrices`, `MatrizRiesgoResumen`, `MatrizRiesgoDetalle` y devuelve `any[]` para el consolidado dinámico.
- `matrices-riesgos.component.ts` consume directamente los tipos heredados.

## 6. Estado real

```text
Fase 1.3: EN EJECUCIÓN
Controlador tipado: IMPLEMENTADO, NO VALIDADO
Puentes temporales: PRESENTES
Repositorio tipado directo: PENDIENTE
AppService tipado directo: PENDIENTE
Angular neutro: PENDIENTE
DTO heredados eliminados: NO
Validador Fase 1.3: PENDIENTE
Compilación y pruebas posteriores: NO EJECUTADAS
Oracle / script 05: BLOQUEADOS
main: INTACTA
```
