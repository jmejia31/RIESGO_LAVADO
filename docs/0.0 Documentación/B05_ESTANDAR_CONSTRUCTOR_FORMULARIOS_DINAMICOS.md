# B05 — Estándar del constructor de formularios dinámicos

## Expediente de la intervención

- `PHASE=B05_DYNAMIC_FORM_BUILDER`
- `BASE_SHA=e9992bd9f646a86f00801e77ae747a6c59694542`
- `BRANCH=desarrollo`
- `AUTHOR=COD / CODEX`
- `CLIENT=CLI`
- `OWNER_SIGNOFF_B04=https://github.com/jmejia31/RIESGO_LAVADO/issues/39`
- `B05_OWNER_VISUAL_ACCEPTANCE=PENDING`
- `B05_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE`

Esta intervención continúa el B04 aprobado técnicamente y mantiene su SHA histórico. El alcance B05 aplicado es la corrección P0 de presentación de expresiones largas en el constructor y en el renderer dinámico, sin reconstruir el Form Builder ni alterar su contrato.

## Regla visual canónica

El contenido técnico extenso usa la clase compartida `.form-builder-long-content`. La regla:

- conserva el texto original completo;
- permite encogimiento seguro dentro de grids y flex (`min-width: 0`);
- distribuye palabras y tokens largos (`white-space: normal`, `overflow-wrap: anywhere`, `word-break: break-word`);
- no usa `text-overflow: ellipsis` ni `overflow: hidden` para ocultar expresiones;
- mantiene la geometría del contenedor y evita desbordamiento horizontal del documento.

La misma regla se consume en la tarjeta de fórmula del lienzo y en `DynamicFieldRendererComponent`. El inspector conserva su preview y bounded select B04; no se duplicó estado, renderer, serializador ni motor matemático.

## Contratos preservados

No se modificaron valores, eventos, identificadores, persistencia ni contratos REST. En particular permanecen sin transformación:

`formulaVersionId`, `formulaId`, `formulaCodigo`, `formulaVersion`, `formula`, `formulaFuente`.

Tampoco se modificaron backend, Oracle institucional, producción, `main`, migración 54, B06 ni Block 10. Los cambios locales ajenos al alcance permanecieron fuera del stage.

## Evidencia fresca de esta intervención

### Pruebas automatizadas

- Unit focalizadas de renderer, canvas, inspector y roundtrip: `56/56 PASS`.
- Suite Angular completa posterior a la corrección: `95 archivos / 993 pruebas PASS`.
- ESLint: `PASS`.
- Build Angular: `PASS`; permanecen únicamente warnings preexistentes de presupuesto SCSS y CommonJS (`exceljs`), sin error de compilación.
- Estructura del repositorio: `PASS`.
- Enlaces documentales: `PASS`.
- Validador de diálogos nativos: `6/6 PASS`; usos productivos `0`.

La regresión unitaria verifica fórmulas de 50, 100, 250 y 500 caracteres y comprueba que el texto técnico se conserve exactamente. La regresión del canvas verifica una fórmula extensa dentro de una tarjeta de dos columnas sin mutar el modelo.

### Navegador real

La suite Playwright focalizada del Form Builder cubre selección, teclado, Escape, clic externo, JSON técnico, duplicación, modo solo lectura y contención de fórmula. La aserción B05 mide texto completo, `scrollWidth <= clientWidth`, wrapping y ausencia de overflow horizontal en:

- `390x844`
- `768x1024`
- `1366x768`
- `1536x864`

Resultado local final de la suite focalizada: `15/15 PASS`.

## Resultado B05

```ini
B05_LONG_FORMULA_FULL_TEXT=PASS
B05_FORMULA_VALUE_PRESERVED=PASS
B05_FORMULA_HORIZONTAL_OVERFLOW=0
B05_DOCUMENT_HORIZONTAL_OVERFLOW=0
B05_SHARED_LONG_CONTENT_RULE=PASS
B05_FORM_BUILDER_REGRESSION=PASS
B05_FORMULA_BINDINGS_PRESERVED=PASS
B05_UNIT_LENGTHS_50_100_250_500=PASS
B05_PLAYWRIGHT_VISUAL=PASS
B01_PRESERVED=YES
B02_PRESERVED=YES
B03_PRESERVED=YES
B04_PRESERVED=YES
B05_TECHNICAL_STATUS=PARTIAL
B05_OWNER_VISUAL_ACCEPTANCE=PENDING
B05_FINAL_STATUS=PENDING_OWNER_ACCEPTANCE
```

`PARTIAL` identifica que esta entrega certifica la corrección P0 de contenido largo y sus consumidores directos; no sustituye la revisión visual global del propietario sobre todas las familias, tipos y superficies del expediente B05. No se emite cierre visual ni aprobación por parte de Codex.

## Publicación y verificación remota

- `SOURCE_CODE_SHA=390a5f3b5249129cfe424433b1d02f4a8cd9dacf`
- `EVIDENCE_COMMIT_SHA=17000efc99f6c1c72f91771e6a8f7ad53ef5290b`
- `GITHUB_EVIDENCE_INDEX_URL=https://github.com/jmejia31/RIESGO_LAVADO/blob/17000efc99f6c1c72f91771e6a8f7ad53ef5290b/docs/0.0%20Documentaci%C3%B3n/B05_ESTANDAR_CONSTRUCTOR_FORMULARIOS_DINAMICOS.md`
- `GITHUB_SIGNOFF_URL=https://github.com/jmejia31/RIESGO_LAVADO/issues/39`
- `GITHUB_REMOTE_VERIFIED=YES`
- `SECURITY_SCAN=PENDING_GITHUB_ACTIONS`
- `REVIEW_READY=PENDING_GITHUB_ACTIONS`

Este expediente se completa después del stage selectivo, los commits B05, el push fast-forward a `origin/desarrollo` y la lectura remota del Quality Gate exacto del SHA publicado.
