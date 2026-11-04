# DICTAMEN TÉCNICO INDEPENDIENTE — P0 n.º 2

**Origen:** comentario del propietario en la [issue #23](https://github.com/jmejia31/RIESGO_LAVADO/issues/23#issuecomment-6633540601). Se archiva fielmente; Codex no emite ni modifica la firma de ChatGPT.

**Proyecto:** RIESGO_LAVADO
**Objeto:** integridad transaccional de actualización, publicación y eliminación de versiones de formularios (backend).
**Fecha de revisión:** 4 de noviembre de 2026.
**Rama y SHA del código objeto de la certificación:** `desarrollo` — `168ffa783942d11097a2ebaad5bba74f4454de9b`.
**Expediente presentado:** `RIESGO_LAVADO_P0_2_CERTIFICACION_CONSOLIDADA_FINAL_20261104_003000.zip`.
**SHA-256 verificado:** `f340f536e4cd23ec3fc0e763148c94ec7484fcce816f5c5c038ff90ee320967f`.

## Dictamen

**APROBADO DOCUMENTALMENTE CON OBSERVACIONES, EXCLUSIVAMENTE PARA EL P0 n.º 2 DE INTEGRIDAD TRANSACCIONAL DEL BACKEND.** La revisión independiente de las evidencias primarias disponibles respalda los cambios confirmados y los rollback de actualización, publicación y eliminación, así como la protección de dependencias durante los conflictos de eliminación. No se demostró un defecto funcional en esos escenarios.

Este dictamen **no es una prueba independiente en una instancia Oracle viva**, no certifica frontend, GTIC, H08, seguridad operacional completa ni implica autorización de Bloque 10. Tampoco constituye una firma digital criptográfica ni sustituye un procedimiento institucional adicional de aprobación cuando resulte exigible.

## Verificación independiente realmente ejecutada

| Control | Resultado |
|---|---|
| SHA-256 global del ZIP | Coincide exactamente |
| Entradas y CRC | 1.153 entradas; CRC PASS; 0 duplicados |
| Catálogo criptográfico interno | 1.152/1.152 hashes correctos; sin autoinclusión |
| Documentos JSON | 592/592 parseables |
| Índice final | 10/10 referencias existentes |
| Orígenes históricos | 5/5 paquetes originales verificados por SHA-256 y CRC |
| Revisión independiente TX-10 | Sexto paquete localizado en esta revisión y SHA-256 coincidente |
| Copias `sources/` | 180/180 archivos idénticos byte a byte con archivos de los ZIP originales |
| Verificación independiente de hechos transaccionales | **68/68 aserciones PASS** |
| Control positivo del verificador independiente | **PASS** |
| Mutaciones negativas en memoria de capturas primarias | **12/12 rechazadas** |
| GitHub `desarrollo` | SHA certificado conservado |
| Quality Gates históricos de ese SHA | 5/5 `success`; ninguno nuevo acreditado |

El verificador independiente y su salida íntegra se adjuntan por separado. Ninguna prueba independiente modificó el repositorio, los ZIP originales, Oracle ni la API. Los negativos fueron mutaciones en memoria de los datos recibidos; no son reejecuciones HTTP.

## Matriz de aceptación por evidencia

| Control | Operación | Resultado |
|---|---|---|
| TX-01 | PUT correcto, JSON/hash actualizados y auditoría UPDATE | PASS |
| TX-02 | PUT fallido por `ORA-20991`, rollback completo | PASS |
| TX-12 backend | PUT fallido por `ORA-20991`, rollback adicional | PASS |
| TX-05 | Publicación fallida y borrador intacto | PASS |
| TX-07 | Fallo de publicación, vigente anterior y borrador conservados | PASS, con reserva de conteo global |
| TX-13 | PUT confirmado preservado tras POST de publicación fallido | PASS |
| TX-08 | DELETE 200, GET 404, ausencia Oracle y auditoría DELETE persistida | PASS |
| TX-09 | DELETE 500 por `ORA-20991`, fila restaurada y sin auditoría parcial | PASS |
| TX-10A | DELETE 409, uso de fórmula y versión intactos | PASS, cuerpo HTTP ausente |
| TX-10B | DELETE 409, evaluación y versión intactas | PASS, cuerpo HTTP ausente |

Para TX-02 y TX-12 se utilizó también el ZIP original externo para corroborar las trazas `ORA-20991` con el ID de la solicitud; esas trazas no están incorporadas al ZIP consolidado. Para TX-09 se cotejó directamente la traza incluida en el expediente.

## Defectos documentales del ZIP consolidado

1. `validation/CONSOLIDATED_VALIDATOR_SOURCE.ps1` consta de un comentario de procedencia de 181 bytes, no de un script ejecutable. Sus reportes no prueban por sí solos cómo se calcularon; esa pretensión se sustituye por el validador independiente adjunto.
2. No incluye carpeta `independent_reviews/`; es una deficiencia de autosuficiencia documental, no evidencia de corrupción ni error backend.
3. No integra los ZIP originales completos; contiene 180 copias primarias cotejadas byte a byte frente a cinco paquetes originales, que fueron accesibles fuera del ZIP.

## Reservas abiertas

- TX-07 no tiene consulta SQL global del conteo de todas las versiones vigentes.
- TX-10A/B no conserva cuerpo HTTP 409 completo; se verifican estado y dependencias mediante filas Oracle antes/después.
- TX-12 frontend no fue verificado por esta campaña.
- La eliminación local completa de temporales sensibles requiere procedimiento autorizado.
- H08_M03, aceptación manual y GTIC siguen pendientes separados.

## Decisión y alcance

```text
P0_2_UPDATE_BACKEND=APPROVED_DOCUMENTALLY
P0_2_PUBLICATION_BACKEND=APPROVED_DOCUMENTALLY_WITH_OBSERVATIONS
P0_2_DELETE_BACKEND=APPROVED_DOCUMENTALLY_WITH_OBSERVATIONS
P0_2_BACKEND_TRANSACTIONAL_INTEGRITY=SIGNED_OFF_DOCUMENTALLY_WITH_OBSERVATIONS
P0_2_INDEPENDENT_SIGNOFF=ISSUED_SCOPE_LIMITED_BACKEND
P0_2_FUNCTIONAL_BLOCKERS_DEMONSTRATED=0
P0_2_CRITICAL_TRANSACTION_EVIDENCE_GAPS=0
CODEX_CONSOLIDATED_VALIDATOR=NOT_REPRODUCIBLE_FROM_DISTRIBUTED_SOURCE
INDEPENDENT_VALIDATOR=POSITIVE_PASS_68_CHECKS_NEGATIVE_12_OF_12
CONSOLIDATED_ZIP_CRYPTOGRAPHIC_INTEGRITY=PASS
CONSOLIDATED_ZIP_SELF_CONTAINMENT=INCOMPLETE
FULL_PROJECT_TECHNICAL_CERTIFICATION=BLOCKED
BLOCK10_AUTHORIZED=NO
```

Se puede cerrar documentalmente P0 n.º 2 backend y avanzar a pendientes institucionales autorizados sin repetir campañas. No se marca el proyecto global como certificado, no se activa producción ni se inicia Bloque 10.

## Metodología y limitaciones

Auditoría de solo lectura sobre archivos adjuntos y originales accesibles en la conversación. Se recalcularon hashes SHA-256, CRC, correspondencia de archivos, estados y auditorías, coherencia JSON/hash, resultados HTTP, trazas y mutaciones adversariales en memoria. No hubo conexión de esta revisión a Oracle, reproducción de solicitudes, verificación de limpieza de Windows ni revisión exhaustiva del sistema.

**Fin del dictamen original; no modificar su alcance.**
