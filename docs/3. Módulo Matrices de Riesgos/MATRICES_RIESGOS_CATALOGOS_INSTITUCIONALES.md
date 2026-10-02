# Catálogos institucionales de Matrices de Riesgos

## Resultado de auditoría — Bloque 3

**Estado: PENDING.** La fuente workbook permite congelar varios catálogos, pero la auditoría encontró divergencia real en producción para los niveles de riesgo y no encontró evidencia suficiente para cerrar el mapeo de Área Consolidada. No se ejecutó DML, DDL, carga de semillas ni corrección productiva. No se inicia el Bloque 4.

## Fuentes y método

- Workbook leído sin modificar: `Matrices de Riesgos.xlsx`, SHA-256 `5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385`.
- Hojas inspeccionadas: `Matriz Consolidada`, `Listas`, `Otras Tablas`, `Listas Automonitoreo` (oculta) e `Instructivo`.
- Tablas de Excel: `Matriz_Riesgos`, `t_tipos_riesgos`, `t_resp_riesgo`, `t_valores_frec_imp`, `t_nivel_riesgo`, `t_efectividad`, `t_pesos_controles`, `t_regimenes`, `t_automatizacion`, `t_areas`.
- No hay named ranges definidos. `Matriz Consolidada` tiene validaciones conectadas a listas de automonitoreo (BS, BT/BW/BZ, BU/BX/CA); el campo 39 no tiene una regla de validación de datos. Sus valores y la tabla `t_resp_riesgo` comparten el dominio, pero esa coincidencia no demuestra una conexión de validación.
- Oracle se consultó con `SET TRANSACTION READ ONLY`, validando HPPROD1 / hpprod1 / hpprod1 / RIESGO_LAVADO / RIESGO_LAVADO; finalizó con `ROLLBACK`.

La autoridad visible de los catálogos conectados a fórmulas y listas es el workbook. `Listas!A16:B25` alimenta F02/F14; `Listas!A29:C35` (`t_efectividad`) alimenta las escalas; los pesos salen de `Otras Tablas!A3:B5`. El manifiesto JSON conserva claves, etiquetas, orden, valores numéricos y aliases con alcance explícito.

## Contrato congelado desde XLSX

La lista completa queda en [matriz_riesgos_catalogos_manifest.json](../../backend/RL.API/Features/MatricesRiesgos/Contracts/matriz_riesgos_catalogos_manifest.json). Incluye 14 catálogos y 91 ítems: `RISK_TYPE`, `RISK_RESPONSE`, `RISK_LEVEL`, `FREQUENCY`, `IMPACT`, `AREA`, `REGIME`, `CONTROL_TYPE`, `CONTROL_WEIGHT`, `CONTROL_EFFECTIVENESS`, `CONTROL_AUTOMATION`, `MONITORING_RISK_STATUS`, `MONITORING_CONTROL_STATUS` y `MONITORING_EFFECTIVENESS`.

Los niveles exactos del workbook son 1–2 `Riesgo no significativo`, 3–4 `Riesgo bajo`, 5 `Riesgo Medio`, 6–7 `Riesgo Alto`, y 8–9 `Riesgo Intolerable`. La prueba `F14_NivelRiesgoResidual_ClasificacionCatalogoInstitucional` se corrigió para no reintroducir valores contradictorios. Los valores 3 y 1 corresponden exactamente a `Riesgo bajo` y `Riesgo no significativo`.

Los pesos Preventivo/Detectivo/Correctivo son 0.70/0.15/0.15 (suma 1.00). La escala única de efectividad tiene niveles 0–5 y porcentajes 0%, 0%, 30%, 50%, 85%, 90%. `Inefectivo` es la etiqueta de `t_efectividad` y `Es inefectivo` aparece en las tablas descriptivas por tipo con el mismo nivel y porcentaje; se registra como alias explícito. Automonitoreo enlaza listas ocultas para estado de riesgo, estado de control y efectividad.

## Respuesta al riesgo y sus variantes

`Listas!D1:D5` (`t_resp_riesgo`) contiene `Evitar`, `Transferir/Compartir`, `Aceptar`, `Mitigar` (las dos celdas indicadas tienen espacios finales en el XLSX). Los valores observados de Campo 39 pertenecen a esa lista. `Instructivo!E154` enumera `Reducir`, `Aceptar`, `Transferir`, `Evitar`, pero no hay validación conectada a Campo 39. Los seeds versionados emparejan `MITIGAR` con “Mitigar / Reducir” y `TRANSFERIR` con “Transferir / Compartir”; backend y frontend usan claves estables `EVITAR`, `MITIGAR`, `TRANSFERIR`, `ACEPTAR`. El manifiesto registra `Reducir` como alias de `MITIGAR` y `Transferir` como alias de `TRANSFERIR`; la presentación toma la etiqueta del workbook, quitando únicamente padding exterior de celda.

Casos del Bloque 2: `ROP-CUMP-50`, `ROP-CUMP-53` y `ROP-CUMP-54` tienen `Transferir/Compartir` en Excel y clave productiva `TRANSFERIR`; se consideran equivalentes por alias explícito. `RCUMP-COMPRAS-37` tiene Excel vacío y producción `MITIGAR`; el alias no resuelve una ausencia, así que queda como diferencia productiva para una decisión de datos posterior. No se cambió ninguno de los cuatro registros.

Reconciliación semántica de los cuatro casos: **3 equivalentes por alias; 1 no equivalente (Excel vacío / DB `MITIGAR`)**. Por tanto, quedan **1 de los 4 conflictos de Campo 39** como candidato de revisión de datos. El conteo base de 87 conflictos del Bloque 2 queda en 87 más esta fila sin resolver = 88 diferencias reales potenciales; no se reinterpretan los 87 restantes en esta auditoría.

**Límite de evidencia:** sin una validación de Campo 39 ni aprobación documental/manual institucional que declare obsoleta una de las variantes, el orden canónico y la equivalencia institucional no quedan formalmente aprobados. El contrato preserva por ahora el dominio del XLSX y su evidencia histórica, pero no declara cerrado este gate.

## Hallazgos de DB y consumidores

La consulta Oracle read-only encontró `MR_RESPUESTA_RIESGO` con las cuatro claves estables, con etiquetas `Evitar`, `Mitigar`, `Transferir`, `Aceptar`. Esto es compatible con las claves del backend y frontend, pero la etiqueta productiva de `TRANSFERIR` omite `/Compartir`.

La misma consulta encontró `MR_NIVEL_RIESGO` con solo cuatro elementos: `BAJO/Bajo`, `MODERADO/Moderado`, `ALTO/Alto`, `CRITICO/Critico`. Los valores `PROY_NIVEL_INHERENTE` y `PROY_NIVEL_RESIDUAL` aprobados también muestran esas cuatro claves. No son el catálogo 1–9 del workbook y no pueden representarlo sin una nueva proyección/mapeo. Por eso la paridad `ROP-CUMP-59` con VRI=3/VRR=1 no puede certificarse para el valor actualmente materializado en DB.

Frecuencia e impacto en XLSX definen el dominio numérico 1–5 sin etiquetas descriptivas. El DB incluye rótulos (“Rara”, “Improbable”, etc. y “Insignificante”, etc.) que no aparecen en esa lista de workbook; no se incorporan como etiquetas oficiales. Los consumidores existentes y los exports deben verificarse contra el contrato antes de declarar paridad integral. Las opciones de tipo de control están codificadas en DDL como `PREVENTIVO`, `DETECTIVO`, `CORRECTIVO`; automatización se persiste con valores técnicos distintos de las etiquetas del XLSX (`MANUAL`, `SEMIAUTOMATICO`, `AUTOMATICO`).

`Listas!H2:I23` define 22 pares nombre completo/“nombre corto para la matriz”. No se halló fuente separada que defina jerarquía de áreas o relación institucional Área → Área Consolidada. El manifiesto no inventa ese mapeo; se registra como fuente pendiente.

## Alias e importación/exportación

La importación futura debe aceptar solo claves canónicas o aliases expresos del manifiesto y rechazar valores desconocidos (`FAIL_CLOSED`). No se permite fuzzy matching, eliminación general de acentos ni normalización general de puntuación/mayúsculas. UI y exportación deben mostrar `canonicalLabel`; DB debe persistir la clave estable cuando el modelo lo permita.

| Catálogo | Excel | DB | Backend / frontend / export | Estado | Acción |
|---|---|---|---|---|---|
| Nivel de riesgo | 1–9 labels workbook | Cuatro claves y labels distintos | Lookup formula usa catálogo | `DB_DRIFT_PENDING_BLOCK4` | Definir mapeo numérico y preparar migración controlada |
| Respuesta | Valores Listas; Campo 39 sin DV | Claves correctas; Transferir sin “/Compartir” | Keys estables en servicio y UI | `ALIAS_ONLY` parcial | Formalizar fuente/alias y resolver blank `RCUMP-COMPRAS-37` |
| Frecuencia / impacto | Dominio 1–5 | Labels descriptivos adicionales | Contrato numérico debe gobernar validación | `DB_DRIFT_PENDING_BLOCK4` | Confirmar tipo de persistencia y no exportar labels no aprobados |
| Área | 22 áreas con abreviatura | Sin catálogo área confirmado | Field 4 / área requiere mapping | `UNRESOLVED` | Obtener autoridad Área → Área Consolidada |
| Control type / weight / scale | Workbook exacto | Tipos en CHECK; efectividad numérica | Cálculos y exports requieren prueba de paridad | `PASS` de fuente, paridad end-to-end pendiente | Ejecutar tests de consumidores |
| Automatización | `Automatizado`, `Semiautomatizado`, `Manual` | claves persistidas distintas | Backend/FE keys estables | `ALIAS_ONLY` | Aplicar alias explícito solo en fronteras de entrada/salida |
| Automonitoreo | Listas ocultas vinculadas por DV | Valores persisten como texto | Mapeo de repositorio existe; paridad completa pendiente | `PASS` workbook / pendiente consumidores | Verificar DTO, UI y exportación |

## Pendientes de cierre

1. Definir/ajustar la proyección productiva del nivel 1–9 y preparar en el bloque correspondiente una remediación idempotente con precheck/postcheck. Este bloque no la ejecuta.
2. Obtener una fuente institucional que cierre Área → Área Consolidada.
3. Resolver formalmente el conflicto de respuesta de Campo 39 y documentar si las formas del manual son aliases o términos distintos.
4. Completar prueba de paridad backend, frontend, importación y exportación contra el manifiesto antes de declarar `CATALOG_DRIFT=0`.

No se ejecutó DML/DDL, no se cargaron los 398 candidatos del Bloque 2 y no se modificaron los cuatro valores operativos del campo 70.
