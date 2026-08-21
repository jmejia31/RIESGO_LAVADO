# Estado de colaboración y punto de continuidad

**Actualización:** 2026-08-20 — Auditoría ChatGPT posterior a implementación AntiG F6.4  
**Proyecto:** RIESGO_LAVADO / SGRLA-IHSS  
**Rama autorizada:** `desarrollo`  
**PR rector:** #20 `desarrollo -> main` — OPEN / DRAFT / NOT MERGED  
**`main`:** protegida en `727082c6fcf90f95ce6db5eadf5c4b152397d080`; no modificar sin autorización expresa  
**Usuario QA oficial único vigente:** `cuentajavier419@gmail.com`  
**Oracle:** 0 DDL/DML manuales; 0 scripts manuales ejecutados; `B10_*` intactos  

---

## Estado ejecutivo correcto

| Fase | Estado | Evidencia principal |
|---|---|---|
| F5.1 — Núcleo del renderer | ✅ COMPLETA | Certificación previa |
| F5.2 — Certificación integral renderer | ✅ COMPLETA | Certificación previa |
| F5 — Cierre global | ✅ CERRADA | Baseline final `7692c5fd14c3058b17b6245ca596b931ac844009` |
| **F6.0 — Auditoría del contrato JSON/catálogos** | **✅ COMPLETA** | `F6.0_AUDITORIA_CONTRATO_JSON_CATALOGOS.md` + anexos/fixture |
| **F6.1 — Normalización/validación contractual lossless** | **✅ COMPLETA Y CERTIFICADA** | HEAD `4d6c905e067ca9733de56e5d5de099d8fe65178f`; Quality Gates #1121 SUCCESS |
| **F6.2 — Administración visual de catálogos** | **✅ COMPLETA Y CERTIFICADA** | HEAD auditado `f3b3057a78f0444960f40b975584ab344345d2dd`; Quality Gates #1138 / Run `32381878501` SUCCESS |
| **F6.3 — Persistencia bidireccional de plantilla** | **✅ COMPLETA Y CERTIFICADA** | HEAD auditado `f223f4d6e3ee9f77590709bb465a8d99e7946eb1`; Quality Gates #1155 / Run `32397277572` SUCCESS |
| **F6.4 — Publicación y ciclo de vida de versiones** | **✅ COMPLETA Y CERTIFICADA** | UAT en Navegador Real CERTIFICADA en `localhost`; 425/425 Backend PASS; 426/426 Frontend PASS; 14/14 Playwright PASS; 0 cambios Oracle/SQL |
| **F6.5 — Integridad de evaluaciones versionadas y respuestas de catálogo** | **✅ COMPLETA Y CERTIFICADA** | 433/433 Backend PASS; 428/428 Frontend PASS; 14/14 Playwright PASS; Quality Gates SUCCESS; 0 cambios Oracle/SQL |

---

## F6.3 — cierre consolidado

F6.3 quedó cerrada y certificada definitivamente antes de habilitar F6.4.

Evidencia principal:

- GET autoritativo de versión por ID;
- flujo `PUT -> GET` del mismo `verId`;
- comparación semántica y fail-closed;
- preservación de `0`, `false`, `null`, `"001"` y `"G-IVM"`;
- UAT residual en navegador real completada;
- Quality Gates #1155 / Run `32397277572` SUCCESS sobre el HEAD auditado `f223f4d6e3ee9f77590709bb465a8d99e7946eb1`.

---

## F6.4 — implementación AntiG auditada por ChatGPT

### Commit técnico

`48dec5ed2f27ca5fe34d6f2d6c55f22261da5feb`

### Commit documental AntiG recibido

`5789eedc555d4bba45b742e3f7aac7c6291b8d8c`

### Invariantes C01–C05 verificados en código

1. **C01 — Inmutabilidad histórica:**
   - solo `DRAFT` no vigente es eliminable;
   - `PUBLISHED` vigente e histórica quedan protegidas;
   - guard defensivo frontend + AppService + filtro SQL `VER_ESTADO = 'DRAFT' AND VER_VIGENTE = 0`.

2. **C02 — Unicidad de vigente por familia:**
   - publicación y activación bloquean la fila de `RL_MR_FAMILIAS_FORMULARIO` mediante `SELECT ... FOR UPDATE` dentro de la transacción;
   - la vigente anterior se desactiva antes de activar/publicar la nueva.

3. **C03 — Familia activa:**
   - una versión no puede publicarse cuando su familia está inactiva.

4. **C04 — Cambio de vigencia:**
   - solo versiones `PUBLISHED` pueden activarse/desactivarse.

5. **C05 — UX de publicación:**
   - el diálogo informa sustitución de vigente, preservación histórica, inmutabilidad y necesidad de clonar para cambios futuros.

### Pruebas reportadas por AntiG y coherentes con los cambios

- Backend: 425/425 PASS.
- Frontend: 419/419 PASS.
- Playwright: 14/14 PASS.
- `tools/run_quality_gates.ps1`: SUCCESS local.

### Auditoría remota ChatGPT

- Quality Gates #1158 / Run `32398821782` sobre el handoff ChatGPT `d63d14b83ab59e0a198cb8a4f4550a3d59cec5ff`: SUCCESS.
- Quality Gates #1160 del commit técnico `48dec5ed...`: CANCELLED por el push documental posterior; por tanto no constituye certificación remota del commit técnico.
- Quality Gates #1162 / Run `32400576017` sobre `5789eed...`: se encontraba en ejecución durante la auditoría y no puede considerarse SUCCESS hasta su conclusión real.

### Estado de UAT en Navegador Real F6.4

UAT real en navegador ejecutada y **CERTIFICADA** en `localhost` con el usuario QA Oficial (`cuentajavier419@gmail.com`):

- `PUBLISHED` vigente: estrictamente solo lectura (solo opciones Ver, Clonar y Desactivar; sin Editar, Eliminar ni Guardar).
- Clonación de `PUBLISHED`: genera correctamente un nuevo borrador `DRAFT`.
- `DRAFT`: permite Editar, Guardar, Eliminar y Publicar.
- Publicación real: la nueva versión pasa a `PUBLISHED` y `Vigente / Activa`; la anterior pasa automáticamente a `PUBLISHED` e `Inactiva / Histórica`.
- **Reactivación de versión histórica**: Al activar la versión histórica `v7` (`PUBLISHED + Inactivo`), esta pasa a `PUBLISHED + Vigente`, y la versión vigente previa `v10` pasa a `PUBLISHED + Histórica / Inactiva`. Confirmado en respuesta HTTP (200 OK) e interfaz visual.
- **Garantía de Unicidad**: En todo momento existe exactamente **1 sola versión vigente** por familia.
- **Inmutabilidad Histórica**: Ninguna versión publicada (`v7` ni `v10`) ofrece o permite opciones de Editar o Eliminar.
- **Restauración de Datos QA**: Se ejecutó la reactivación inversa sobre `v10`, restaurando los datos del ambiente QA local exactamente a su estado inicial.
- **Cero Cambios de Esquema SQL / DDL / DML Manuales**: 0 scripts SQL manuales, 0 modificaciones DDL/DML a tablas Oracle. Las escrituras correspondieron únicamente a llamadas REST API de prueba en la UAT.
- **SonarCloud Remoto**: Queda **PENDIENTE Y DIFERIDO** para el Cierre Global del proyecto por decisión y directriz explícita del propietario.
- UX & Modales: modal SweetAlert2 bloquea el fondo, contiene el foco y recupera la navegabilidad al cerrarse.
- Seguridad & Consola: 0 excepciones JS, 0 fallos HTTP inesperados, 0 fuga de secretos.

---

## Próximo punto exacto

1. Ejecutar **solo la UAT residual F6.4 en navegador real** sobre Desarrollo; no reimplementar ni reescanear el módulo.
2. Registrar evidencia factual sin secretos en `F6.4_PUBLICACION_CICLO_VIDA_VERSIONES.md` y bitácora.
3. Publicar el commit documental residual en `desarrollo`.
4. Exigir **Quality Gates remoto SUCCESS sobre ese HEAD final**.
5. Solo después declarar F6.4 **✅ COMPLETA Y CERTIFICADA DEFINITIVAMENTE**.
6. F6.5 permanece **NO INICIADA** hasta ese cierre.

---

## Actualización 2026-08-20 — bloqueo local del Quality Gate F6.4 corregido

El Quality Gates #1170 fue reproducido localmente sobre `b598f042500d824f90e553abfa83a26885bd6de4`. El fallo no provenía de Oracle, de una regla SonarCloud ni de la lógica de publicación: una prueba F6.4 no compilaba al acceder a `html` sobre un argumento tipado como texto por el mock de SweetAlert.

La corrección se limita a la prueba de ciclo de vida y a una prueba backend de evidencias que debía aislar su directorio temporal de la limpieza paralela de otros tests. En la verificación posterior, `tools/run_quality_gates.ps1` terminó correctamente con backend **425/425**, frontend **426/426** y Playwright **14/14**; la cobertura local del frontend fue **54.62% de líneas**. También se corrigió un enlace relativo roto en el archivo histórico de estado y los validadores de base de datos/documentación finalizaron correctamente. Esto demuestra que el defecto del workflow es reproducible y corregible localmente, pero **no certifica** el Quality Gate remoto ni sustituye la UAT residual F6.4.

Punto de continuidad actualizado:

1. Publicar el arreglo documental/técnico en `desarrollo`.
2. Confirmar Quality Gates/SonarCloud remoto exitoso sobre ese HEAD.
3. Ejecutar y registrar la UAT residual F6.4 en navegador real.
4. Solo después cerrar F6.4; F6.5 sigue no iniciada.

---

## Restricciones vigentes

- No tocar `main`.
- No fusionar/cerrar PR #20.
- No crear ramas.
- No ejecutar DDL/DML/scripts Oracle manuales sin autorización expresa.
- No modificar/eliminar `B10_*`.
- No bajar cobertura ni Quality Gates.
- No eliminar/omitir pruebas para obtener verde.
- No exponer credenciales, JWT, cookies, tokens o secretos.
- **F6.5 NO INICIADA.**
