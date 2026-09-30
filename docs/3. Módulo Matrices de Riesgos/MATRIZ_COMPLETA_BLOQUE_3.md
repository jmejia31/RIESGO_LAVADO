# Matriz institucional completa — Bloque 3: Riesgo Residual y Respuesta

## Estado de certificación técnica

```text
BLOCK_3_STATUS=CLOSED
FIELDS_34_39_VISIBLE=6/6
ORDER_34_39=PASS
EXACT_EXCEL_LABELS=PASS
F10_F14=PASS
RESIDUAL_SERVER_AUTHORITATIVE=PASS
RESIDUAL_CLIENT_TAMPERING=REJECTED_OR_RECALCULATED
CALCULATED_FIELDS_NOT_CLIENT_AUTHORITATIVE=PASS
RESIDUAL_VERSION_AWARE=PASS
RESPONSE_CATALOG=PASS
RESPONSE_OPTIONS=EVITAR|MITIGAR|TRANSFERIR|ACEPTAR
INVALID_RESPONSE_VALUE_CONTROLLED_4XX=PASS
RESPONSE_PERSISTENCE=PASS
BLOCK_3_RESPONSE_ROUNDTRIP_E2E=PASS
RESPONSE_SAVE_PAYLOAD=PASS
RESPONSE_REHYDRATION_AFTER_REOPEN=PASS
MATRIX_FIELD_39_PERSISTED_PROJECTION=PASS
BLOCK_3_DESKTOP_VISUAL=PASS
BLOCK_3_MOBILE_VISUAL=PASS
BLOCK_3_HORIZONTAL_OVERFLOW=0
BLOCK_1_REGRESSION=PASS
BLOCK_2_REGRESSION=PASS
FIELDS_01_33_STILL_PRESENT=PASS
ORDER_01_39=PASS
FULL_MATRIX_IMPLEMENTED_FIELDS=39/82
FULL_MATRIX_PENDING_FIELDS=43
FULL_MATRIX_82=PENDING_BLOCKS_4_6
MAIN_TOUCHED=NO
ORACLE_CONNECTION_ATTEMPTED=NO
ORACLE_DML_EXECUTED=NO
ORACLE_DDL_EXECUTED=NO
```

---

## 1. Trazabilidad exacta de fórmulas institucionales (F10–F14)

| Fórmula | Etiqueta institucional exacta | Celda fuente Excel | Expresión canónica autoritativa | Archivo / Implementación | Prueba unitaria |
|---|---|---|---|---|---|
| **F10** | Riesgo Residual | `Matriz Consolidada!AH2` | `IF(riesgo_inherente_descripcion="","",riesgo_inherente_descripcion)` | `InstitutionalFormulaDataset.cs` (nº 10) / `FormulaEngine.cs` / `matrices-riesgos.component.ts` | `F10_RiesgoResidualDescripcion_ParidadAutoritativaExcel` |
| **F11** | Frecuencia Residual | `Matriz Consolidada!AI2` | `IFERROR(IF(OR(frecuencia="",impacto="",valor_riesgo_inherente="",valor_riesgo_residual=""),"",IF(valor_riesgo_inherente=valor_riesgo_residual,frecuencia,MIN(tope_f,f_base+incremento_f_aux))),"")` | `InstitutionalFormulaDataset.cs` (nº 11) / `MatricesRiesgosAppService.cs` / `FormulaEngine.cs` | `F11_F12_FrecuenciaEImpactoResidual_IdentidadCuandoVRIIgualAVRR` |
| **F12** | Impacto Residual | `Matriz Consolidada!AJ2` | `IFERROR(IF(OR(frecuencia="",impacto="",valor_riesgo_inherente="",valor_riesgo_residual=""),"",IF(valor_riesgo_inherente=valor_riesgo_residual,impacto,MIN(tope_i,i_base+incremento_i_aux))),"")` | `InstitutionalFormulaDataset.cs` (nº 12) / `MatricesRiesgosAppService.cs` / `FormulaEngine.cs` | `F11_F12_FrecuenciaEImpactoResidual_IdentidadCuandoVRIIgualAVRR` |
| **F13** | Valor del Riesgo Residual | `Matriz Consolidada!AK2` | `IFERROR(ROUND(MAX(1,valor_riesgo_inherente*(1-efectividad_total_ponderada)),0),"")` | `InstitutionalFormulaDataset.cs` (nº 13) / `MatricesRiesgosAppService.cs` / `FormulaEngine.cs` / `EVA_VRR` | `F13_ValorRiesgoResidual_CalculoAutoritativoVRR` |
| **F14** | Nivel del Riesgo Residual | `Matriz Consolidada!AL2` | `IFERROR(LOOKUP("CAT_NIVEL_RIESGO",valor_riesgo_residual),"")` | `InstitutionalFormulaDataset.cs` (nº 14) / `CatalogCalculationLookup` / `FormulaEngine.cs` | `F14_NivelRiesgoResidual_ClasificacionCatalogoInstitucional` |

---

## 2. Inventario de campos del Bloque 3 (Campos 34–39)

El bloque `3. Riesgo Residual y Respuesta` consta de exactamente seis campos, en orden inmutable 34–39:

| Ordinal | Etiqueta visible institucional | Columna Excel | Clave técnica | Modo | Origen autoritativo |
|:---:|---|:---:|---|---|---|
| **34** | `Riesgo Residual` | AH | `riesgo_residual_descripcion` | COMPUTED (solo lectura) | Proyección F10 / Nombre del riesgo inherente |
| **35** | `Frecuencia Residual` | AI | `frecuencia_residual` | COMPUTED (solo lectura) | Backend F11 / Runtime institucional |
| **36** | `Impacto Residual` | AJ | `impacto_residual` | COMPUTED (solo lectura) | Backend F12 / Runtime institucional |
| **37** | `Valor del Riesgo Residual` | AK | `valor_riesgo_residual` | COMPUTED (solo lectura) | Backend F13 / `EVA_VRR` persistido |
| **38** | `Nivel del Riesgo Residual` | AL | `nivel_riesgo_residual` | COMPUTED (solo lectura) | Backend F14 / Catálogo institucional `CAT_NIVEL_RIESGO` |
| **39** | `Respuesta al riesgo` | AM | `respuesta_riesgo` | INPUT / SELECTOR | Selección institucional autorizada (`MR_RESPUESTA_RIESGO`) |

---

## 3. Autoridad del Servidor y Prevención de Tampering

1. **Cálculo autoritativo exclusivo:** Angular no calcula F10–F14 ni VRR. El frontend únicamente proyecta los valores provenientes de `evaDataCalcJson`, `evaVrr` y el catálogo oficial.
2. **Rechazo / Recálculo de valores forjados:** Si un cliente envía valores alterados (p. ej. `valor_riesgo_residual: 999` o `evaVrr = 999`), el backend ignora los valores forjados y recalcula autoritativamente a partir de las fuentes legítimas de evaluación (`RESIDUAL_CLIENT_TAMPERING=REJECTED_OR_RECALCULATED`).
3. **Catálogo canónico de respuesta:** Se validan estrictamente las cuatro opciones institucionales en backend:
   - `EVITAR`
   - `MITIGAR`
   - `TRANSFERIR`
   - `ACEPTAR`
   Cualquier valor fuera de este catálogo (o vacío) produce una respuesta funcional HTTP 400 controlada (`INVALID_RESPONSE_VALUE_CONTROLLED_4XX=PASS`).
4. **Matriz Completa strictly read-only:** La Matriz Completa no es un editor. Los campos 34–38 son de solo lectura y el campo 39 proyecta la respuesta actualmente persistida.

---

## 4. Remediación E2E y Round-Trip del Campo 39

La intervención inicial implementó correctamente las fórmulas F10–F14, los modelos, el backend autoritativo y los componentes visuales del Bloque 3. Una auditoría independiente identificó la necesidad de cerrar la evidencia conductual del round-trip completo del campo 39:

1. **Flujo E2E implementado y certificado (`BLOCK 3 campo 39 persiste Respuesta al riesgo después de guardar y reabrir`):**
   - Carga de `/matrices-riesgos`.
   - Apertura del flujo real de edición de una evaluación en estado `BORRADOR` (#20).
   - Localización del selector `Respuesta al riesgo` (`#campo-edit-respuesta_riesgo`).
   - Verificación de las cuatro opciones canónicas institucionales (`EVITAR`, `MITIGAR`, `TRANSFERIR`, `ACEPTAR`) con sus textos visibles correspondientes.
   - Selección de la opción canónica `MITIGAR`.
   - Guardado mediante el botón real de la interfaz.
   - Captura y verificación del payload PUT real enviado al backend: `evaDataJson` contiene `respuesta_riesgo = "MITIGAR"`.
   - Simulación con mock stateful que incrementa `evaVersionRow` y almacena el estado persistido del servidor.
   - Cierre del modal de edición tras confirmación exitosa de persistencia.
   - Reapertura de la misma evaluación desde la interfaz: rehidratación fresca desde el API confirmando que el selector presenta `MITIGAR`.
   - Cierre del editor y apertura de `Ver Matriz completa` de esa misma evaluación.
   - Verificación de `[data-matrix-field="39"]`: etiqueta `Respuesta al riesgo` y proyección visible de `MITIGAR`.
   - Verificación de que campos 34–38 continúan en modo solo lectura (`aria-readonly="true"`).
   - Verificación de que la Matriz completa mantiene exactamente 39 campos implementados y Bloque 4 permanece marcado como pendiente.
   - Validación de consola y red: 0 errores de consola, 0 errores de página, 0 respuestas HTTP 4xx/5xx inesperadas y 0 peticiones a `/familias/1`.

---

## 5. Evidencia de Calidad y Pruebas

- **Línea base previa (Run remoto 36626072573, SHA `9ddfca41ea704cd8690d04bcb15c0ff4d9d3ea8f`):**
  - Backend tests: 740/740 PASS.
  - Frontend unit tests: 820/820 PASS en 82 archivos.
  - Playwright E2E: 41/41 PASS (0 flaky).
  - Cobertura remota exacta: Backend líneas `37.19%`, ramas `40.46%`; Frontend sentencias `62.40%`, ramas `55.79%`, funciones `58.68%`, líneas `63.26%`.
  - npm audit: `0 vulnerabilities`.

- **Evidencia con remediación final (Suite completa local verificada):**
  - **Backend tests:** 740/740 tests superados (0 errores, 0 omitidos).
  - **Frontend unit tests:** 820/820 tests superados en 82 archivos (0 errores).
  - **Frontend lint:** `0` errores (`eslint src e2e scripts`).
  - **Frontend build:** Generado exitosamente en `dist/rl-app` (producción).
  - **Playwright E2E:** 42/42 tests superados (0 flaky, 0 fallos).
    - Incorpora el test conductual de round-trip de persistencia del campo 39.
    - Preserva la prueba de Matriz completa con 39 campos, etiquetas exactas, orden 01–39, `aria-readonly="true"` en 34–38, proyección de respuesta 39, responsive 1280x900 y 390x844 sin horizontal overflow (`BLOCK_3_HORIZONTAL_OVERFLOW=0`).
  - **npm audit:** `0 vulnerabilities`.
  - **Validadores de repositorio:** Estructura (118 rutas PASS), Database scripts (PASS), Documentation links (188 enlaces PASS).
  - **Puertas de calidad:** `tools/run_quality_gates.ps1` -> PASS (`Puertas de calidad correctas`).
