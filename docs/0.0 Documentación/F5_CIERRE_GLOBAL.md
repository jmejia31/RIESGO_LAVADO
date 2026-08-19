# F5 — Cierre Global del Renderer Dinámico

## 1. Propósito

Consolidar la auditoría final de **F5.1 — Núcleo del Renderer Dinámico** y **F5.2 — Certificación Integral del Renderer Dinámico**, dejando una única referencia vigente para continuidad antes de F6.

## 2. Estado consolidado

- **F5.1**: **COMPLETA Y CERTIFICADA**.
- **F5.2**: **COMPLETA Y CERTIFICADA**.
- **Quality Gates F5.2 previo a la corrección documental**: `#1099`, Run ID `32289758348` — **SUCCESS** sobre `509c6ba30cab0fb9f1a1703e5b6e542d2d2ccb35`.
- **Corrección documental de política QA**: `a9e39484f553442f2053c3ad02f0003e7cd1b40e`.
- **F6**: **NO INICIADA** durante este cierre.

## 3. Política QA vigente — corrección vinculante

A partir de la instrucción expresa de Javier Mejía del **2026-08-19**, el **nuevo y único usuario QA oficial vigente** del proyecto es:

`cuentajavier419@gmail.com`

Esta regla sustituye cualquier referencia operativa previa a `adminpruebas@ihss.hn` u otros usuarios QA. Las menciones anteriores que permanezcan en entradas históricas de bitácora se consideran exclusivamente **antecedentes históricos** y no autorización vigente para nuevas certificaciones.

La contraseña continúa siendo introducida personalmente por Javier Mejía. Ningún agente debe solicitarla, leerla, copiarla, capturarla, automatizarla, almacenarla ni registrar tokens, JWT, cookies o secretos derivados de la sesión.

## 4. Fe de erratas sobre los SHA de F5.2

Para eliminar la ambigüedad documental:

- `eb83633cdd82d4a6ce323b69a2c285d5becb8e94` es el **commit de cierre documental inicial de F5.2**.
- `509c6ba30cab0fb9f1a1703e5b6e542d2d2ccb35` es el **HEAD posterior de F5.2 que registró el SHA de cierre en bitácora y fue certificado remotamente por Quality Gates #1099**.
- `a9e39484f553442f2053c3ad02f0003e7cd1b40e` corrige la certificación F5.2 para dejar a `cuentajavier419@gmail.com` como único usuario QA oficial vigente.

Los SHA abreviados o completos distintos comunicados anteriormente para estos mismos commits quedan sustituidos por los SHA reales verificados en GitHub indicados arriba.

## 5. Evidencia funcional heredada y auditada

F5.2 certificó en navegador gráfico real:

- los tipos `texto`, `numero`, `fecha`, `texto-largo`, `selector-catalogo`, `radio`, `catalogo-multiple`, `checkbox`, `formula` y fallback `desconocido`;
- preservación de `0`, `false` y `null`;
- solo lectura;
- JSON defensivo;
- layout dinámico;
- responsive desktop/tablet/móvil;
- navegación por teclado y accesibilidad;
- Console sin errores bloqueantes;
- Network sin bucles o duplicaciones anómalas;
- round-trip de consulta de evaluación;
- frontend `379/379` PASS;
- backend `409/409` PASS;
- Playwright `14/14` PASS;
- Quality Gates local SUCCESS.

No se realizaron cambios productivos durante F5.2.

## 6. Gobernanza preservada

- Rama activa: `desarrollo`.
- `main` permanece intacta en `727082c6fcf90f95ce6db5eadf5c4b152397d080`.
- PR #20 debe permanecer **OPEN / DRAFT / NOT MERGED**.
- 0 ejecuciones DDL/DML manuales.
- 0 scripts Oracle ejecutados durante F5.2 y esta corrección documental.
- Esta corrección no modifica renderer, frontend productivo, backend, contratos, cobertura, umbrales ni pruebas.
- Sonar mantiene la política de diferimiento/no bloqueo ya vigente para el cierre intermedio; no se alteran sus reglas.

## 7. Criterio de cierre definitivo

El cierre global de F5 queda condicionado únicamente a que el **HEAD final resultante de esta documentación** obtenga **Quality Gates remoto SUCCESS**.

Una vez cumplida esa condición:

- **F5 = CERRADA**;
- **F6 = HABILITADA PARA INICIO**, pero no iniciada por esta intervención.

Este documento constituye la fe de erratas y referencia vigente para las discrepancias documentales detectadas durante la auditoría final de F5.
