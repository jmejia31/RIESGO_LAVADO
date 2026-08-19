# Estado de colaboración y punto de continuidad

**Actualización:** 2026-08-19 — Antigravity  
**Proyecto:** RIESGO_LAVADO / SGRLA-IHSS  
**Rama autorizada:** `desarrollo`  
**PR rector:** #20 `desarrollo -> main` — OPEN / DRAFT / NOT MERGED  
**`main`:** protegida en `727082c6fcf90f95ce6db5eadf5c4b152397d080`; no modificar sin autorización expresa  
**Usuario QA oficial único vigente:** `cuentajavier419@gmail.com` / `adminpruebas@ihss.hn`  
**Oracle:** 0 DDL/DML, 0 scripts manuales ejecutados

---

## Estado ejecutivo

| Fase | Estado | Evidencia |
|---|---|---|
| F5.1 — Núcleo del renderer | ✅ COMPLETA | Certificada |
| F5.2 — Certificación integral renderer | ✅ COMPLETA | Certificada |
| F5 — Cierre global | ✅ CERRADA | Quality Gates #1103 SUCCESS |
| **F6.0 — Auditoría del contrato** | **✅ COMPLETA** | Documento principal + anexo de brechas ejecutables + fixture v3 |
| **F6.1 — Normalización/validación contractual** | **✅ COMPLETA** | Quality Gates #1121 SUCCESS |
| **F6.2 — CRUD visual de catálogos** | **✅ COMPLETA** | Suite `form-builder-catalog-management.spec.ts` (13/13 PASS) + Navegador real |
| **F6.3 — Administración avanzada/integración** | **⛔ NO INICIADA** | A la espera de auditoría formal de ChatGPT sobre F6.2 |

---

## Baseline y concurrencia controlada de F6.0

- Baseline de cierre F5 auditado: `7692c5fd14c3058b17b6245ca596b931ac844009`.
- Durante F6.0 `desarrollo` avanzó al commit `c2b8b44c2da27aaa24e3d5ec54ff55228ebdd43f`, que agregó exclusivamente el documento principal de auditoría F6.0.
- ChatGPT verificó ese commit y preservó el trabajo existente; no hubo sobrescritura destructiva.
- `main` de referencia continúa en `727082c6fcf90f95ce6db5eadf5c4b152397d080`.

---

## Fuentes contractuales verificadas

1. `contrato_formulario_matrices_riesgos_IHSS_v3.json` — contrato funcional 1.0.0, estado `LISTO_PARA_IMPLEMENTACION`.
2. Reporte de validación del contrato — 8/8 validaciones correctas.
3. `esquema_respuesta_matrices_riesgos_IHSS_v1.schema.json` — respuesta JSON Schema Draft 2020-12.
4. Modelos/normalizadores/Builder frontend vigentes.
5. `FormularioValidador.cs`, DTO de metodología, AppService y repositorio backend vigentes.
6. Fixture SQL legacy vigente de formularios.

El archivo adjunto de esta conversación llamado `Formularios dinámicos JSON.txt` no contiene una definición JSON; no se usa como fuente contractual. El contrato v3 real fue recuperado de la File Library del usuario.

---

## Resultado F6.0

Documento principal preservado:

- `docs/0.0 Documentación/F6.0_AUDITORIA_CONTRATO_JSON_CATALOGOS.md`

Anexo vinculante agregado:

- `docs/0.0 Documentación/F6.0_ANEXO_BRECHAS_EJECUTABLES_CONTRATO_V3.md`

Fixture v3 mínimo agregado:

- `docs/0.0 Documentación/fixtures/f6.0_contrato_v3_catalogos_slice.json`

Registro de intervención:

- `docs/0.0 Documentación/F6.0_REGISTRO_INTERVENCION_CHATGPT.md`

### Brechas críticas confirmadas en código

1. **F6-X01 — `clave` vs `id`:** Builder serializa `clave`; `FormularioValidador` actual extrae `id`.
2. **F6-X02 — catálogo múltiple:** frontend persiste `string[]`; backend exige elementos convertibles a `Int32`.
3. **F6-X03 — metodología truncada:** `/metodologia/version/{id}` no proyecta `columnasPorFila`, `opciones`, `formula`, `anchoColumnas` ni `tipoOriginal`.
4. **F6-X04 — forma de catálogos:** contrato v3 usa objeto indexado con metadatos/origen/respaldo; runtime reducido espera `CatalogoMatrices[]` y solo acepta array.
5. **F6-X05 — Builder hardcodeado:** `catalogosDisponibles` no proviene de la versión/modelo real.

### Brechas altas

- integridad referencial insuficiente de catálogos;
- normalización superficial de `catalogos`/`reglas`;
- propiedades Builder editables no serializadas (`descripcion`, `placeholder`, `textoAyuda`);
- vocabulario de tipos frontend/backend no unificado;
- respuesta v3 estructurada/envelope no equivale al mapa reducido `Record<string, ValorRespuestaFormulario>`.

---

## Decisiones vinculantes de F6.0

1. **Cero pérdida silenciosa.**
2. El contrato v3 validado define la semántica objetivo.
3. El código ejecutable vigente define compatibilidad que debe preservarse durante migración.
4. Escritura futura canónica; lectura mantiene aliases/formatos legacy explícitos.
5. Catálogo: identidad persistida = `codigo`; presentación = `etiqueta`/valor visible.
6. `0`, `false` y `null` son estados diferentes.
7. Códigos de catálogo pueden ser alfanuméricos; multiselección no depende de enteros.
8. Tipos v3 todavía no soportados por renderer se preservan sin pérdida; F6 no implementará todos los tipos complejos.
9. No se requiere crear nuevas tablas Oracle para completar la capa contractual de F6.
10. F6.2 no puede comenzar hasta que F6.1 deje estos gates automatizados y verdes.

---

## Próximo punto exacto

### F6.1 — Normalización, validación y alineación contractual

1. convertir F6-X01..F6-X10 en pruebas automatizadas de contrato;
2. implementar adaptador v3 ↔ modelo editable sin pérdida;
3. alinear `clave`/`id`;
4. alinear catálogos simples/múltiples y códigos string;
5. ampliar proyección de metodología requerida por renderer;
6. preservar envelope/metadatos/tipos no editables;
7. validar integridad de catálogos y referencias;
8. ejecutar frontend/backend/build/E2E/Quality Gates;
9. cerrar F6.1 antes de habilitar F6.2.

---

## Restricciones vigentes

- No tocar `main`.
- No fusionar/cerrar PR #20.
- No crear ramas.
- No ejecutar DDL/DML/scripts Oracle sin autorización expresa de Javier Mejía.
- No bajar cobertura ni Quality Gates.
- No eliminar ni omitir pruebas para obtener verde.
- Nuevas pruebas se nombran por responsabilidad funcional, no por número de fase.
- No exponer credenciales/JWT/cookies/tokens.
- F6.2 y fases posteriores permanecen NO INICIADAS.
