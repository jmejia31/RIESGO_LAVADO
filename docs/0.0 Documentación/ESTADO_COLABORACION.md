# Estado de colaboración y punto de continuidad

> Actualización 2026-08-06: la Fase 10 del Módulo Matrices de Riesgos inició en su alcance técnico no destructivo. Se prepararon el postflight Oracle de solo lectura, el manifiesto local de hashes, el acta operativa pendiente, la validación automática del paquete y su integración al Quality Gate. La transición física continúa bloqueada: Oracle, el preflight `07`, el script `06` y el postflight `08` no fueron ejecutados. La autorización permanece en `NO OTORGADA`. `main` continúa intacta y el PR #20 debe permanecer abierto y en borrador.

Documento vivo. Debe actualizarse al finalizar cada intervención.

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria:** `desarrollo`
- **Rama estable:** `main` — no modificar ni integrar sin autorización expresa de Javier Mejía
- **Ramas remotas permitidas:** únicamente `main` y `desarrollo`
- **PR de revisión:** #20, abierto, borrador y sin autorización de fusión
- **Arquitectura:** monolito modular con Angular, ASP.NET Core y Oracle 11g
- **Modelo objetivo de Matrices:** 17 tablas y 17 secuencias

---

## 2. Última intervención

- **Intervención**: Alineación Interna y Cierre Documental de Fase 10
- **Fecha**: 2026-08-06 (Hora local)
- **Autor**: Antigravity
- **Rama**: `desarrollo`
- **Commit certificado**: `4cc3a1f154546d8d4b547ac301fdf0a44d742025`
- **Quality Gate CI**: Run `31126687057` en **SUCCESS**.
- **Estado**: Se corrigió la Sección 11 del Plan Operativo de Fase 10 para alineación interna exacta con la preparación técnica completada y certificada. La transición física Oracle permanece **NO INICIADA**, el ambiente de pruebas permanece **PENDIENTE DEL DBA**, el preflight `07`, el script `05`, el script `06` y el postflight `08` **NO FUERON EJECUTADOS**, la autorización de Fase 10 permanece **NO OTORGADA** y la Fase 11 permanece **BLOQUEADA**.
- **Plan**: [`FASE_10_PLAN_TRANSICION_FISICA_ORACLE_MODELO_17_TABLAS_PREPARADO_NO_AUTORIZADO_2026-08-06.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_10_PLAN_TRANSICION_FISICA_ORACLE_MODELO_17_TABLAS_PREPARADO_NO_AUTORIZADO_2026-08-06.md)
- **Acta:** [`FASE_10_ACTA_EJECUCION_TRANSICION_ORACLE_MODELO_17_TABLAS_PENDIENTE_2026-08-06.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_10_ACTA_EJECUCION_TRANSICION_ORACLE_MODELO_17_TABLAS_PENDIENTE_2026-08-06.md)
- **Autorización:** [`FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md)

### Entregables técnicos preparados

1. `database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql`.
2. `database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql`.
3. `database/19_matrices_riesgos/transicion/08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql`.
4. `database/19_matrices_riesgos/transicion/modelo_17_objetos.json`.
5. `scripts/operations/prepare_matrices_phase10_evidence.ps1`.
6. `scripts/validation/validate_matrices_phase10_transition_package.ps1`.
7. Acta de ejecución e incidente pendiente de diligenciamiento.
8. Integración de la puerta de Fase 10 al Quality Gate.
9. README operativo actualizado con el orden `manifiesto → preflight → transición → postflight → Fase 11`.

---

## 3. Controles incorporados en Fase 10

### Preparador de evidencias

El script `prepare_matrices_phase10_evidence.ps1`:

- exige la rama `desarrollo`;
- exige árbol de trabajo limpio;
- registra el commit exacto;
- calcula hashes SHA-256 de los artefactos autorizables;
- genera un manifiesto y resumen en una carpeta temporal;
- no lee cadenas de conexión;
- no conecta a Oracle;
- no ejecuta SQL*Plus;
- registra que la autorización y la ejecución permanecen negativas.

### Postflight `08`

El script `08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql`:

- exige `CURRENT_SCHEMA = RIESGO_LAVADO`;
- confirma `RL_USUARIOS`, `RL_AUDITORIA` y `SEQ_RL_AUDITORIA`;
- exige exactamente 17 tablas `RL_MR_*`;
- exige exactamente 17 secuencias `SEQ_RL_MR_*`;
- detecta objetos faltantes e inesperados;
- detecta tablas y secuencias heredadas;
- exige claves primarias habilitadas;
- exige cero restricciones inactivas;
- exige cero objetos inválidos `RL_MR_*`;
- lista tablas, secuencias, restricciones e índices;
- no contiene DDL ni DML;
- no autoriza ni ejecuta el script `06`.

### Validador automático

`validate_matrices_phase10_transition_package.ps1` controla:

- presencia de todos los artefactos;
- permanencia de las salvaguardas del script `06`;
- carácter de solo lectura de `07` y `08`;
- correspondencia con `modelo_17_objetos.json`;
- inventario exacto 17/17;
- control de objetos retirados;
- estado negativo de autorización y ejecución;
- ausencia de credenciales codificadas;
- integración de la puerta al workflow.

---

## 4. Estado de fases del Módulo Matrices de Riesgos

| Fase | Descripción | Estado real |
|---|---|---|
| **Fase 0-R** | Aprobación funcional del modelo reducido | **Aprobada** |
| **Fase 0-C** | Congelamiento técnico y línea base | **Completada** |
| **Fase 1** | Alineación de columnas JSON | **Completada** |
| **Fase 2** | Retiro de trazas de cálculo | **Completada** |
| **Fase 3** | Auditoría institucional | **Completada** |
| **Fase 4** | Retiro de adaptadores y contratos heredados | **Completada** |
| **Fase 5** | Inventario exacto de 17 tablas y 17 secuencias | **Completada** |
| **Fase 6** | Pruebas automatizadas no Oracle | **Completada** |
| **Fase 7** | Suite Oracle del modelo reducido | **Completada en código; certificación física pendiente** |
| **Fase 8** | Revisión final no Oracle y cuarentena | **Completada** |
| **Fase 9** | Ambiente Oracle exclusivo y expediente de autorización | **Completada técnicamente** |
| **Fase 10** | Transición física controlada | **Preparación técnica en curso; ejecución física bloqueada** |
| **Fase 11** | Certificación física y funcional Oracle | **Bloqueada** |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** |

---

## 5. Estado actual de ejecución y autorización

```text
FASE 9: COMPLETADA
FASE 10: PREPARACION TECNICA EN CURSO
TRANSICION FISICA: NO INICIADA
AMBIENTE ORACLE EXCLUSIVO: PENDIENTE
CONFIRMACION DE NO PRODUCCION: PENDIENTE
RESPALDO: PENDIENTE
PRUEBA DE RESTAURACION: PENDIENTE
RESPONSABLES: PENDIENTES
VENTANA DE CAMBIO: PENDIENTE
AUTORIZACION FASE 10: NO OTORGADA
PREFLIGHT 07: NO EJECUTADO
SCRIPT 05: NO EJECUTADO
SCRIPT 06: NO EJECUTADO
POSTFLIGHT 08: NO EJECUTADO
CERTIFICACION ORACLE: PENDIENTE
```

La instrucción del usuario para continuar la Fase 10 habilita el trabajo técnico no destructivo, pero no sustituye las evidencias de ambiente, respaldo, restauración, participantes ni la autorización separada requerida antes del DDL.

---

## 6. Bloqueantes vigentes para la transición física

1. No se ha identificado ni aprobado una base Oracle exclusiva de pruebas.
2. No existe confirmación escrita de ausencia de datos productivos.
3. No se han designado DBA ejecutor, DBA revisor, responsable funcional y custodio de evidencias.
4. No existe evidencia de respaldo completo.
5. No existe evidencia de restauración validada.
6. El preflight `07` no se ha ejecutado contra Oracle.
7. No existe decisión escrita sobre objetos o datos `RL_MR_*` existentes.
8. No se ha definido ni aprobado la ventana de cambio.
9. No se ha suministrado la conexión mediante un mecanismo seguro y no versionado.
10. No se han calculado los hashes definitivos dentro de la ventana autorizada.
11. No se ha diligenciado ni firmado el acta operativa.
12. No se ha otorgado autorización expresa separada para ejecutar el script `06`.
13. El modelo no ha sido instalado ni verificado físicamente mediante el postflight `08`.
14. La suite Oracle de Fase 11 no se ha ejecutado.

---

## 7. Directrices activas

1. Trabajar únicamente en `desarrollo`.
2. No modificar ni fusionar `main`.
3. Mantener el PR #20 abierto y en borrador.
4. No habilitar auto-merge.
5. No ejecutar Oracle sin el ambiente y la autorización requeridos.
6. No ejecutar los scripts `05` o `06`.
7. No ejecutar `CREATE`, `DROP`, `TRUNCATE` ni migraciones.
8. Los scripts `07` y `08` solo podrán ejecutarse en la ventana autorizada y con custodia de evidencias.
9. No incorporar el paquete 19 a maestros automáticos.
10. No restaurar instaladores heredados de 34 tablas.
11. No versionar credenciales o cadenas de conexión.
12. No declarar certificado el modelo antes de las pruebas Oracle reales.
13. No cambiar `NO OTORGADA` a `OTORGADA` sin evidencia externa verificable.
14. Detenerse ante cualquier discrepancia de esquema, hashes, datos, inventario o participantes.

---

## 8. Punto exacto de continuación

El siguiente paso no es ejecutar el script `06`. El orden obligatorio es:

1. Esperar el resultado completo del Quality Gate sobre el paquete de Fase 10.
2. Corregir cualquier hallazgo del nuevo validador.
3. Recibir identificación formal de la base Oracle exclusiva de pruebas.
4. Recibir declaración escrita de no Producción y ausencia de datos productivos.
5. Designar los responsables de la ventana.
6. Obtener respaldo completo y prueba de restauración.
7. Definir y aprobar la ventana de cambio.
8. Proporcionar la conexión mediante un secreto temporal.
9. Generar el manifiesto de hashes desde el commit autorizado.
10. Ejecutar únicamente el preflight `07`.
11. Revisar y resolver el inventario previo.
12. Completar y firmar la autorización y el acta.
13. Recibir autorización expresa separada.
14. Solo después ejecutar manualmente el script `06`.
15. Ejecutar el postflight `08`.
16. Entregar el ambiente a la Fase 11.

---

## 9. Commits principales de la preparación Fase 10

```text
7a10692267e5ecebf912af265d11305ebff9ecc4
feat(matrices): agregar postflight Oracle de fase 10

7a23b69feeb8105bd5d0be8b26f51b44a22dc8d5
docs(matrices): preparar acta operativa de fase 10

ad4f997204a81aa1e046f5adbce0c99ed9b90a9f
feat(matrices): preparar manifiesto de evidencias fase 10

cb5eb2f4962a5d0c92c8d629325cd0b233715a6c
test(matrices): validar paquete operativo de fase 10

12335ecdabf51c39c94e8e596abda5a2ceb49944
docs(matrices): iniciar preparacion tecnica fase 10

ce1d4cc4bfd448635d474dd1c28bb2d783bd96da
docs(matrices): documentar paquete de transicion fase 10

6b720f6166d3449dbd3dbbd3250baf0a8a9f9292
ci(matrices): exigir paquete operativo fase 10
```

---

## 10. Pendiente independiente de seguridad

`npm ci` continúa reportando:

```text
13 vulnerabilidades
6 moderadas
6 altas
1 crítica
```

No se aplicó `npm audit fix --force`, porque podría introducir cambios incompatibles. Este pendiente requiere una fase de seguridad separada antes de Producción y debe recordarse al final de cada fase hasta su resolución formal.
