# Estado de colaboración y punto de continuidad

> Actualización 2026-08-10: **BE-02 — Caché con invalidación explícita** fue implementado y certificado técnicamente en `desarrollo`. Se incorporó una abstracción de caché en memoria por instancia, con TTL acotados, prevención de cache stampede, invalidación explícita por alcance y protección frente a la carrera lectura/invalidation para impedir repoblación obsoleta. Solo se cachean lecturas estables de versiones/metodología de formularios de Matrices, configuración del sistema y slides de login. Evaluaciones, evidencias, flujos, auditoría, consolidado/reportes dinámicos y demás información transaccional permanecen fuera de caché. El HEAD técnico `a81e9a2747b9e1097baee0cc7773c4b8eedcbd1f` fue certificado por GitHub Actions Quality Gates Run `31408706366` (#607) en **SUCCESS**: Backend 304/304, Frontend 162/162, E2E 13/13, build Release 0 errores/0 advertencias y `npm audit` con 0 vulnerabilidades. Oracle no fue conectado ni ejecutado; `main` permanece intacta y el PR #20 debe continuar abierto y en borrador.

Documento vivo. Debe actualizarse al finalizar cada intervención. Los antecedentes históricos permanecen en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).

---

## 1. Línea base vigente

- **Repositorio:** `jmejia31/RIESGO_LAVADO`
- **Rama obligatoria de trabajo:** `desarrollo`
- **Base BE-02:** `79fe291b133de880d7d20830837eace0b72d1f91`
- **HEAD técnico BE-02 certificado:** `a81e9a2747b9e1097baee0cc7773c4b8eedcbd1f`
- **Rama estable:** `main`
- **HEAD de `main`:** `727082c6fcf90f95ce6db5eadf5c4b152397d080`
- **Política `main`:** no modificar ni integrar sin autorización expresa de Javier Mejía
- **Ramas remotas permitidas por protocolo:** únicamente `main` y `desarrollo`
- **PR de revisión:** #20 — abierto, en borrador y sin autorización de fusión
- **Arquitectura:** monolito modular con Angular, ASP.NET Core y Oracle 11g
- **Modelo vigente de Matrices:** 17 tablas `RL_MR_*` y 17 secuencias

---

## 2. Última intervención — BE-02

- **Intervención:** BE-02 — Caché con invalidación explícita
- **Fecha:** 2026-08-10 (UTC-6)
- **Autor:** ChatGPT
- **Rama:** `desarrollo`
- **Base de inicio:** `79fe291b133de880d7d20830837eace0b72d1f91`
- **HEAD técnico certificado:** `a81e9a2747b9e1097baee0cc7773c4b8eedcbd1f`
- **Quality Gate técnico:** Run `31408706366` (#607) — **SUCCESS**
- **Estado:** **BE-02 completado y certificado técnicamente**.

### Cambios certificados

1. **Infraestructura de caché explícita**
   - `IApplicationCache` desacopla negocio de la implementación concreta.
   - `ApplicationMemoryCache` usa `IMemoryCache` por instancia.
   - Claves deterministas por alcance + clave funcional.
   - TTL configurables y normalizados entre 5 y 900 segundos.
   - Bloqueo por alcance para prevenir `cache stampede`.
   - Resultados fallidos/no encontrados no se cachean donde corresponda.

2. **Invalidación por alcance**
   - Alcance `be02:matrices-formularios` para definiciones/versiones/metodología dinámica de Matrices.
   - Alcance `be02:configuracion-sistema` para configuración institucional del sistema.
   - Alcance `be02:login-slides` para slides activos/todos.
   - Cada mutación exitosa invalida explícitamente el alcance relacionado.
   - Las mutaciones fallidas no destruyen entradas válidas.

3. **Protección contra repoblación obsoleta concurrente**
   - La generación/token del alcance se captura antes de consultar el origen.
   - Si una invalidación ocurre mientras una lectura está en vuelo, ese resultado puede completar la solicitud original pero **no puede repoblar la nueva generación de caché**.
   - Se añadió una prueba específica de esta carrera.

4. **Matrices — superficie cacheada**
   - Versión vigente por familia.
   - Versión por ID.
   - Historial de versiones por familia.
   - Metodología dinámica vigente.
   - Invalidación después de crear borrador, clonar, actualizar borrador, publicar y cambiar vigencia.
   - **No se cachean:** evaluaciones, paginación de evaluaciones, flujos, evidencias, consolidado tipado ni reportes transaccionales.

5. **Configuración — superficie cacheada**
   - Configuración del sistema.
   - Slides activos.
   - Todos los slides.
   - Invalidación después de guardar configuración y crear/actualizar/eliminar slides.

6. **Catálogos**
   - No se cachearon en BE-02 porque no se identificó una ruta de mantenimiento/mutación con invalidación explícita dentro del alcance revisado.
   - Permanecen consultados desde su fuente de verdad; una futura caché de catálogos deberá incorporar primero los puntos reales de escritura e invalidación.

7. **Configuración operativa**
   - `FormularioVersionTtlSeconds`: 120 segundos por defecto.
   - `ConfiguracionSistemaTtlSeconds`: 120 segundos por defecto.
   - `LoginSlidesTtlSeconds`: 60 segundos por defecto.
   - Valores efectivos acotados a 5–900 segundos.

8. **Restricción de topología**
   - La implementación actual es caché local por proceso, adecuada al monolito/instancia actual.
   - Si el API se escala horizontalmente a múltiples instancias, la invalidación local no invalida procesos hermanos; antes de ese despliegue deberá migrarse la abstracción a caché/invalidation distribuida.

---

## 3. Evidencia de verificación BE-02

### GitHub Actions — Quality Gates

- **Run:** `31408706366`
- **Número:** #607
- **Conclusión:** **SUCCESS**
- **Build Release:** 0 errores, 0 advertencias
- **Backend:** 304/304 pruebas aprobadas; 0 fallidas; 0 omitidas
- **Frontend:** 162/162 pruebas aprobadas en 25 archivos
- **E2E Playwright:** 13/13 aprobadas
- **NPM audit:** 0 vulnerabilidades
- **Cobertura Backend:** líneas 22.19%; ramas 24.83%
- **Cobertura Frontend:** sentencias 39.53%; ramas 35.24%; funciones 35.99%; líneas 39.15%
- **Validadores de BD, preparación Oracle, inventario 17/17, autorización y contrato UAT:** aprobados

### Pruebas BE-02 añadidas

- Reutilización de valor dentro del TTL.
- Invalidación selectiva por alcance.
- No-cache cuando el predicado de seguridad rechaza el resultado.
- Prevención de `cache stampede` con solicitudes concurrentes.
- Carrera lectura/invalidation: una lectura previa no puede repoblar caché después de una mutación.
- Acotamiento de TTL.
- Configuración cacheada + invalidación tras guardado exitoso.
- Slides cacheados + invalidación tras mutación.
- Mutación fallida conserva caché vigente.

### Oracle

Durante desarrollo y certificación CI de BE-02:

- **NO** se abrió conexión a Oracle real;
- **NO** se ejecutó DDL;
- **NO** se ejecutó DML;
- **NO** se ejecutaron scripts de transición;
- **NO** se modificó el esquema;
- **NO** se modificaron respaldos `B10_*`.

Los validadores Oracle ejecutados por CI son controles estáticos/de preparación y no constituyen una ejecución física de Oracle.

---

## 4. Estado consolidado del Plan de Mejoras Integrales

| Orden | Código | Estado |
|---:|---|---|
| 1 | GOV-01 — Sincronización Bitácora / UAT | **Completado** |
| 2 | BE-01 + FE-02 — ProblemDetails + Interceptor HTTP | **Completado y certificado** |
| 3 | BE-03 — `/healthz` + `/readyz` | **Completado y certificado** |
| 4 | BE-04 — Rate Limiting | **Completado y certificado** |
| 5 | BE-02 — Caché con invalidación explícita | **Completado y certificado técnicamente** |
| 6 | DB-03 — Profiling Oracle / `EXPLAIN PLAN` | **Siguiente; sujeto a autorización/ambiente Oracle** |
| 7 | DB-01 — Política de archivado de auditoría | Pendiente de diseño; sin borrado automático |
| 8 | FE-03 + FE-04 — Accesibilidad + Skeleton Loaders | Pendiente |
| 9 | FE-01 — Signals gradual | Pendiente |
| 10 | GOV-02 + GOV-03 — Linter/Sonar + Docker multietapa | Pendiente |

---

## 5. Estado consolidado de Matrices de Riesgos

| Bloque | Estado vigente |
|---|---|
| Modelo reducido Oracle | **17 tablas + 17 secuencias** |
| Fase 10 — transición física | **Completada según evidencia histórica del proyecto** |
| Fase 11 — certificación funcional/Oracle | **Completada y certificada según evidencia histórica registrada** |
| Fase 12 — hardening NPM | **Completada — 0 vulnerabilidades** |
| Fase 13 — contrato/UAT automatizado | **Certificación de repositorio completada; validación local residual pendiente según PR #20** |

La validación local residual de UAT permanece como actividad funcional/operativa independiente y no fue sustituida por BE-02.

---

## 6. Directrices activas e inviolables

1. Trabajar únicamente sobre `desarrollo`.
2. No modificar, fusionar ni publicar en `main` sin autorización expresa de Javier Mejía.
3. Mantener el PR #20 abierto y en borrador.
4. No habilitar auto-merge.
5. No ejecutar Oracle, DDL ni DML salvo autorización formal y alcance específico.
6. No exponer credenciales, cadenas de conexión, secretos, errores Oracle ni detalles internos al cliente.
7. `/healthz` debe permanecer independiente de Oracle y dependencias externas.
8. `/readyz` debe conservar respuesta agregada mínima y una comprobación de solo lectura.
9. Las políticas BE-04 deben conservar particiones separadas y no confiar en cabeceras de forwarding hasta configurar proxies confiables.
10. Reintentos automáticos HTTP únicamente para `GET` y solo ante `0/503/504`.
11. Nunca reintentar automáticamente `POST`, `PUT`, `DELETE` o `PATCH`.
12. Toda superficie agregada a caché debe tener TTL acotado e invalidación explícita en sus mutaciones relacionadas.
13. No cachear datos transaccionales sensibles/dinámicos sin una justificación y contrato de invalidación verificables.
14. Antes de crear índices Oracle, ejecutar y documentar profiling/`EXPLAIN PLAN` en el ambiente autorizado.
15. La bitácora es histórica e inmutable: las correcciones futuras se agregan mediante una nueva entrada, no reescribiendo registros previos.
16. `ESTADO_COLABORACION.md` es el documento vivo y puede consolidarse conforme cambie el estado real.

---

## 7. Pendientes independientes

- Validación UAT local residual indicada en PR #20.
- Si la cuenta Oracle de Desarrollo continúa bloqueada, el desbloqueo corresponde exclusivamente al DBA/administrador autorizado.
- Configurar proxies confiables antes de habilitar `ForwardedHeaders` como fuente de IP cliente.
- Si en el futuro existen múltiples instancias del API, sustituir la caché local por una implementación distribuida mediante `IApplicationCache` antes de depender de invalidación cross-node.

---

## 8. Punto exacto de continuación

**GOV-01, BE-01, FE-02, BE-03, BE-04 y BE-02 quedan cerrados técnicamente.**

El siguiente paso de la secuencia aprobada es:

### DB-03 — Profiling Oracle / `EXPLAIN PLAN`

DB-03 deberá medir consultas reales y documentar planes de ejecución antes de proponer índices. **No ejecutar `EXPLAIN PLAN`, SQL de profiling, DDL, DML ni conexiones Oracle hasta contar con autorización formal de Javier Mejía y un ambiente Oracle autorizado.**
