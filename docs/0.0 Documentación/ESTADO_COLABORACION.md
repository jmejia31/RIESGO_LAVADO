# Estado de colaboración y punto de continuidad

**Actualización:** 2026-08-20 — Auditoría ChatGPT  
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
| **F6.3 — Persistencia bidireccional de plantilla** | **✅ IMPLEMENTADA + UAT REAL PASS; cierre auditado en curso sobre documentación final** | Código AntiG `a13e1a1aadc188d018fe4e5f50cd430295aba248`; UAT residual documentado; Quality Gate de `61e7b89fb433ad0c5670acd208410d1a312e98ff` = #1154 / Run `32396816868` SUCCESS |
| **F6.4** | **⛔ NO INICIADA** | No iniciar hasta cierre formal de auditoría ChatGPT de F6.3 |

---

## F6.3 — evidencia consolidada

### Backend/API

ChatGPT dejó preparada la lectura autoritativa de una versión por ID y su suite contractual:

- `07629509a25670f0f7289baafea8b36080eb5fb3` — GET de versión de formulario por ID.
- `5bd040177ffaf35ffa40697fd99eaf95ecb37714` — pruebas backend de round-trip/JSON rico.
- `93d8a10ab26467dd76a9dff36ea0988214702e87` — handoff técnico a AntiG.

### Frontend/runtime

AntiG implementó:

- `MatricesRiesgosService.obtenerVersionFormulario(id)`;
- apertura autoritativa por `verId`;
- flujo `PUT -> GET` del mismo `verId`;
- comparación semántica recursiva de JSON;
- arrays con orden contractual;
- preservación estricta de `0`, `false`, `null`, `"001"` y `"G-IVM"`;
- fail-closed ante discrepancia semántica o error de GET post-save.

Commit real de implementación AntiG:

`a13e1a1aadc188d018fe4e5f50cd430295aba248`

### UAT residual en navegador real

Certificación ejecutada sobre Desarrollo con el usuario QA oficial único. Evidencia documentada:

- DRAFT `PRUEBA_FORMULARIO · v5`;
- GET autoritativo por `verId`;
- modificación controlada con códigos `001` y `G-IVM`;
- PUT al mismo `verId`;
- GET post-save al mismo `verId`;
- cerrar y reabrir la misma versión;
- persistencia visual y contractual confirmada;
- 0 TypeErrors y 0 excepciones Angular no controladas;
- sin exposición de JWT, cookies, Authorization, contraseñas ni tokens.

Documento rector:

`docs/0.0 Documentación/F6.3_PERSISTENCIA_BIDIRECCIONAL_PLANTILLA.md`

### Pruebas y gates reportados/verificados

- Frontend: 414 / 414 PASS.
- Backend: 414 / 414 PASS.
- Playwright: 14 / 14 PASS.
- Quality Gates sobre `d31e25e0a7ad272212a06c5931fd265b27a89f4f`: Run `32387555389` SUCCESS.
- Quality Gates residual documental sobre `c9113b6e28d1ed723d425d4d0e4910c817e6d58c`: Run `32395987728` SUCCESS.
- Commit posterior `61e7b89fb433ad0c5670acd208410d1a312e98ff` registró ese Run ID.
- Quality Gates sobre `61e7b89fb433ad0c5670acd208410d1a312e98ff`: #1154 / Run `32396816868` SUCCESS, 21 controles funcionales/CI completos.

---

## Corrección documental vinculante

La edición anterior de `ESTADO_COLABORACION.md` introdujo una tabla incorrecta que:

- omitía F6.0;
- renombraba F6.1 y F6.2 de forma incompatible con el plan rector;
- asociaba evidencia errónea a F6.2;
- marcaba F6.4 como habilitada antes de la auditoría final de ChatGPT;
- mantenía secciones históricas diciendo que F6.2 y posteriores no habían iniciado.

Esta versión reemplaza esas inconsistencias y prevalece como punto de continuidad operativo.

---

## Próximo punto exacto

1. Certificar el commit que contiene esta corrección documental mediante Quality Gates.
2. Si ese HEAD queda SUCCESS y PR/main/Oracle siguen intactos, declarar F6.3 **COMPLETA Y CERTIFICADA DEFINITIVAMENTE**.
3. Solo después habilitar F6.4.

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
- F6.4 permanece NO INICIADA hasta la certificación final de esta corrección documental.
