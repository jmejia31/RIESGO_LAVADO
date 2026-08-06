# Estado de colaboración y punto de continuidad

> Actualización 2026-08-06: la Fase 9 del Módulo Matrices de Riesgos dejó preparados el expediente de autorización Oracle, el preflight de inventario de solo lectura, el formato separado de autorización para la Fase 10 y una puerta automática de validación. Oracle y los scripts `05` y `06` no fueron ejecutados. El ambiente, respaldo, restauración, responsables, ventana y firmas continúan pendientes de evidencia externa. `main` permanece intacta y el PR #20 debe continuar abierto y en borrador.

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

- **Intervención:** Fase 9 — Ambiente Oracle exclusivo y expediente de autorización
- **Fecha:** 2026-08-06
- **Rama:** `desarrollo`
- **Estado técnico:** entregables preparados; ejecución física bloqueada
- **Expediente:** [`FASE_9_EXPEDIENTE_AUTORIZACION_ORACLE_MODELO_17_TABLAS_2026-08-06.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_9_EXPEDIENTE_AUTORIZACION_ORACLE_MODELO_17_TABLAS_2026-08-06.md)
- **Formato de autorización:** [`FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md`](../3.%20Módulo%20Matrices%20de%20Riesgos/FASE_9_FORMATO_AUTORIZACION_EJECUCION_ORACLE_FASE_10_2026-08-06.md)

### Entregables preparados

1. `database/19_matrices_riesgos/transicion/07_preflight_inventario_oracle_solo_lectura.sql`.
2. Expediente integral de identificación, responsables, respaldo, restauración, permisos, ejecución, contingencia y evidencias.
3. Formato separado de autorización para la Fase 10, actualmente en `NO OTORGADA`.
4. `scripts/validation/validate_matrices_phase9_oracle_dossier.ps1`.
5. Integración de la puerta de Fase 9 al Quality Gate.
6. Actualización del README del paquete Oracle.

### Controles del preflight

El script `07`:

- valida `CURRENT_SCHEMA = RIESGO_LAVADO`;
- exige `RL_USUARIOS`, `RL_AUDITORIA` y `SEQ_RL_AUDITORIA`;
- identifica base, host, usuario y fecha del servidor;
- lista tablas y secuencias `RL_MR_*`;
- cuenta registros por tabla;
- reporta objetos inválidos y restricciones deshabilitadas;
- no contiene DDL ni DML;
- no incluye ni ejecuta el script `06`;
- no fue ejecutado contra Oracle.

### Estado de validación

- La primera ejecución del nuevo gate detectó tres comparaciones literales incorrectas en el validador; fueron corregidas sin modificar el expediente ni el preflight.
- Una ejecución posterior no alcanzó el checkout ni los validadores porque GitHub Actions devolvió `Service Unavailable` al descargar acciones del propio servicio.
- Debe obtenerse un Quality Gate completo en `success` antes del cierre definitivo de esta fase.

---

## 3. Estado de fases del Módulo Matrices de Riesgos

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
| **Fase 9** | Ambiente Oracle exclusivo y expediente de autorización | **Entregables preparados; validación final en curso** |
| **Fase 10** | Transición física controlada | **Bloqueada; autorización no otorgada** |
| **Fase 11** | Certificación física y funcional Oracle | **Bloqueada** |
| **Fase 12** | Documentación y cierre técnico | **Pendiente** |

---

## 4. Estado actual de autorización

```text
EXPEDIENTE TECNICO: PREPARADO
PREFLIGHT DE SOLO LECTURA: PREPARADO, NO EJECUTADO
AMBIENTE ORACLE EXCLUSIVO: PENDIENTE DE IDENTIFICACION Y EVIDENCIA
RESPALDO: PENDIENTE
PRUEBA DE RESTAURACION: PENDIENTE
AUTORIZACION FASE 10: NO OTORGADA
SCRIPT 05: NO EJECUTADO
SCRIPT 06: NO EJECUTADO
CERTIFICACION ORACLE: PENDIENTE
```

La existencia del expediente o del formato de autorización no permite inferir aprobación.

---

## 5. Bloqueantes vigentes

1. Falta un Quality Gate completo de cierre de Fase 9 por una indisponibilidad externa de GitHub Actions.
2. No se ha identificado ni aprobado formalmente una base Oracle exclusiva de pruebas.
3. No existe confirmación escrita de ausencia de datos productivos.
4. No se han designado DBA ejecutor, DBA revisor, responsable funcional y custodio de evidencias.
5. No existe evidencia de respaldo completo.
6. No existe evidencia de restauración validada.
7. El preflight `07` no se ha ejecutado contra Oracle.
8. No existe decisión escrita sobre datos `RL_MR_*` que pudieran encontrarse.
9. No se ha otorgado autorización expresa para ejecutar el script `06`.
10. El modelo no ha sido instalado ni certificado físicamente en Oracle.
11. La suite Oracle no se ha ejecutado con `RL_ORACLE_INTEGRATION_REQUIRED=true`.

---

## 6. Directrices activas

1. Trabajar únicamente en `desarrollo`.
2. No modificar ni fusionar `main`.
3. Mantener el PR #20 abierto y en borrador.
4. No habilitar auto-merge.
5. No ejecutar Oracle.
6. No ejecutar los scripts `05` o `06`.
7. No ejecutar `CREATE`, `DROP`, `TRUNCATE` ni migraciones.
8. El preflight `07` solo podrá ejecutarse cuando el DBA proporcione el ambiente mediante un mecanismo seguro.
9. No incorporar el paquete 19 a los maestros automáticos.
10. No restaurar los instaladores heredados de 34 tablas.
11. No versionar credenciales o cadenas de conexión.
12. No declarar certificado el modelo antes de las pruebas Oracle reales.
13. No cambiar `NO OTORGADA` a `OTORGADA` sin evidencia externa verificable.
14. Mantener commits identificables, validación y documentación por fase.

---

## 7. Punto exacto de continuación

1. Obtener un Quality Gate completo en `success` para los entregables de Fase 9.
2. Registrar el commit y el run aprobados en este documento.
3. Solicitar al DBA el diligenciamiento del expediente, sin compartir secretos.
4. Recibir evidencia de ambiente exclusivo, respaldo y restauración.
5. Ejecutar únicamente el preflight `07` de solo lectura cuando exista ambiente autorizado.
6. Revisar inventario y datos existentes.
7. Completar el formato separado de autorización.
8. No iniciar la Fase 10 hasta recibir una autorización expresa posterior.

---

## 8. Commits principales de la intervención

```text
42c4dd554b7bc8db1911d2a91fb1db3514bdceef
feat(matrices): preparar preflight Oracle de solo lectura fase 9

57ab5d6cebac58d616f6707b4ef00950cfa5acd4
docs(matrices): preparar expediente Oracle fase 9

5403887b1985fe6d338d7a59288f16ddc98b1874
docs(matrices): agregar formato de autorizacion Oracle fase 10

2798677224ceb412e2565bdbb60805917121445e
test(matrices): validar expediente Oracle fase 9

1beec70e85d4d8e6a360e908fbd1ac286a3346ac
ci(matrices): exigir expediente Oracle fase 9

f1368c6c06b62292db4130ea7016be678e5979f1
fix(matrices): alinear validador del expediente Oracle fase 9

02329345d5336b209d249b0a74a0dace7a95b121
docs(matrices): documentar controles Oracle fase 9
```

---

## 9. Pendiente independiente de seguridad

`npm ci` continúa reportando:

```text
13 vulnerabilidades
6 moderadas
6 altas
1 crítica
```

No se aplicó `npm audit fix --force`, porque podría introducir cambios incompatibles. Este pendiente requiere una fase de seguridad separada antes de Producción y debe recordarse al final de cada fase hasta su resolución formal.
