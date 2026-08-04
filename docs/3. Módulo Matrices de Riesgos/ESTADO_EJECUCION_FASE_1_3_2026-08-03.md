# Estado de Ejecución — Fase 1.3
## Contratos neutros y retiro del modelo heredado

**Fecha:** 3 de agosto de 2026  
**Rama:** `desarrollo`  
**Rama estable:** `main` — intacta  
**Estado:** implementación publicada y Quality Gates aprobados en CI; pendiente de aprobación funcional formal de Javier Mejía.

## 1. Dependencias y restricciones

- La Fase 1.2 permanece abierta y pendiente de pruebas Oracle controladas.
- Oracle y el script `05` no se ejecutaron durante la Fase 1.3.
- Ningún cambio fue aplicado a `main`.
- La comparación remota registra `desarrollo` 92 commits por delante y 0 por detrás de `main` al momento de esta actualización.

## 2. Criterios obligatorios

1. Ningún contrato, consulta, endpoint, prueba ni componente Angular conserva referencias funcionales al modelo heredado.
2. Los reportes utilizan DTOs tipados; no permanece `List<Dictionary<string, object>>`.
3. Los DTO heredados se eliminan únicamente después de retirar sus consumidores.
4. Los contratos dinámicos conservan versión de formulario, secciones, campos, catálogos y reglas.
5. Se actualizan las pruebas backend y frontend afectadas.
6. Deben aprobar el validador, la compilación Release y la suite backend.
7. Las pruebas Oracle permanecen pendientes de la Fase 1.2.

## 3. Implementación publicada

### 3.1 Backend

- `IMatricesRiesgosRepository` expone directamente:
  - `ObtenerConsolidadoTipadoAsync`;
  - `ObtenerMetodologiaDinamicaVigenteAsync`.
- `MatricesRiesgosRepository` materializa `RiesgoReporteFilaDto` desde evaluaciones y proyecciones.
- La metodología dinámica conserva:
  - `VersionFormularioId`;
  - código y versión;
  - secciones;
  - campos;
  - catálogos;
  - reglas y parámetros dinámicos.
- `IMatricesRiesgosAppService`, `MatricesRiesgosAppService` y `MatricesRiesgosController` utilizan únicamente contratos neutros y tipados para consolidado y metodología.
- Se conservaron los cambios transaccionales de evidencias, flujos, proyecciones, trazas y auditoría de la Fase 1.2.

Commits principales:

| Cambio | Commit |
|---|---|
| Repositorio tipado y metodología dinámica | `9f3fed59f1f40d606e59bdf38a88e0104b24bbe0` |
| Interfaz definitiva del repositorio | `c0db213a9ea0d5392a69549a52f56e06e0fd9b6b` |
| Contrato definitivo de AppService | `8593f99416e7330332496ce2e78d6e1a8296148c` |
| AppService migrado | `9c63bab95db7343f544d863e221773310332afc7` |
| Controlador migrado | `3a698af849421fe27600a06f27f70ad1923aa4f8` |

### 3.2 Retiro físico del modelo anterior

Se eliminaron:

- `Contracts/Matrices/MatrizRiesgoDtos.cs`;
- `Contracts/Reporteria/ReporteriaDtos.cs`;
- `Application/MatricesRiesgosReportRenderer.cs`;
- la prueba ficticia del renderizador anterior.

Commits:

| Retiro | Commit |
|---|---|
| DTOs de matriz basada en modelos/factores/variables | `849e10e696c29a409393781887920ab14962c9a6` |
| DTOs de reportería basada en sujetos y factores | `28b4b2f4eb79834c90a656b601f63091b9a53afa` |
| Renderizador heredado sin endpoints activos | `6193c5b72478e79a26d466621353613064e28bb6` |
| Prueba heredada del renderizador | `5444aa2b0738b4d8e64488cb11b4e794a84cdd09` |

### 3.3 Angular

- Los modelos TypeScript ahora representan formularios versionados, secciones, campos, catálogos, reglas, evaluaciones, evidencias y filas tipadas.
- El servicio Angular consume exclusivamente endpoints dinámicos.
- El componente principal renderiza campos a partir de la versión y la metodología.
- Se eliminó la clasificación rígida de niveles en cliente.
- La tabla auxiliar usa `RiesgoReporteFila`.
- Las pruebas del servicio y del componente fueron migradas.

Commits principales:

| Cambio | Commit |
|---|---|
| Modelos neutros Angular | `b5dd8caf204e35a41a676f23e7b63c003b35e440` |
| Alineación de revisiones dinámicas | `c54dcaeb80f6a5054de188482bcc731bc038f094` |
| Servicio Angular dinámico | `2c698699726cf3c4ea6125256301fd41c21772d4` |
| Componente dinámico y reactividad | `d6ae136ee09a96d464584c50bcea66ea300a2b63`, `a7031cf1b99908abc788d912b9fd3a20ee942d28` |
| Vista dinámica final | `6a42a040c75fb55a38559f4ba48214605434d2bd` |
| Tabla auxiliar tipada | `b1a9029e774e30a4272a2a52a7aca52061d1c94f`, `fe53233d16693c22d2bd811e3aa35e9b8b03d2a7` |
| Pruebas Angular migradas | `f807dcc1ee59205c893d457a7bcc351ea034b2eb`, `697bc7d0affae2b5a5bdeb4193418b021df3163f` |

### 3.4 Pruebas y validador

- Suite de aplicación migrada a versiones publicadas, evaluación dinámica, consolidado tipado, metodología neutra, transiciones y nueve vinculaciones.
- Suite del controlador migrada.
- Pruebas de reflexión verifican la forma de los contratos y la ausencia de métodos públicos anteriores.
- El validador bloquea columnas incompatibles, tablas retiradas, DTOs anteriores, `List<Dictionary<string, object>>`, clasificaciones rígidas y credenciales codificadas.

Commits principales:

| Cambio | Commit |
|---|---|
| Suite de aplicación neutra | `22030fa370677f28d57f4ea0df02394093641a90` |
| Suite de controlador neutra | `f733c54f6639ac83f02db491992e49912e3ff529` |
| Contratos neutros reforzados | `b18902ceddb1f9619bfd5ffff345777198a7ceaf` |
| Validador Fase 1.3 | `8ed2e2178894de82d810c13e132cbd07c63b0b71` |

## 4. Quality Gates

El workflow existente fue ampliado de forma segura para ejecutarse en `desarrollo` con:

- `permissions: contents: read`;
- validador dinámico;
- compilación completa en Release;
- pruebas backend con cobertura;
- pruebas frontend con cobertura;
- pruebas E2E.

No realiza commits, pushes, despliegues ni cambios autónomos.

Commits:

- `2818cc81ef7355403dea7f271c5468f0c4e70d59` — habilitación en `desarrollo`.
- `3ce4eb8e2033fef369db628322e5760083bb8f9c` — validador y build Release explícitos.

## 5. Validaciones ejecutadas

La ejecución de GitHub Actions `30855978597` (workflow run 201) finalizó correctamente sobre `desarrollo` y el commit `8c0bc3f`. Se verificaron de forma observable:

1. validador dinámico correcto;
2. compilación Release sin errores ni advertencias;
3. 188 pruebas backend correctas;
4. 122 pruebas frontend correctas en 19 archivos;
5. build Angular correcto;
6. 7 pruebas E2E correctas;
7. umbrales de cobertura aprobados: backend 16.22 % de líneas y 16.60 % de ramas.

La certificación técnica automatizada no sustituye la aprobación funcional de Javier Mejía ni las pruebas Oracle pendientes de la Fase 1.2.

## 6. Estado real

```text
Fase 1.3: IMPLEMENTADA Y CERTIFICADA TÉCNICAMENTE EN CI; PENDIENTE DE APROBACIÓN FUNCIONAL
Repositorio tipado directo: IMPLEMENTADO
AppService tipado directo: IMPLEMENTADO
Controlador tipado: IMPLEMENTADO
Angular neutro: IMPLEMENTADO
DTO y renderizador heredados: ELIMINADOS
List<Dictionary<string, object>> en reportería: ELIMINADO
Validador Fase 1.3: ACTUALIZADO
Quality Gates de solo lectura: CONFIGURADOS
Resultados observables de Quality Gates: APROBADOS EN LA EJECUCIÓN 30855978597
Oracle / script 05: NO EJECUTADOS
Fase 1.2: ABIERTA
main: INTACTA
```
