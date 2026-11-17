# RIESGO_LAVADO — GitHub Actions: corrección de diagnóstico sin degradar seguridad

**Fecha:** 17 de noviembre de 2026  
**Autor de intervención:** CHAT / ChatGPT, conexión exclusiva `jmejia31`  
**Rama:** `desarrollo`  
**SHA inicial remoto:** `1ecd85629fee5dff25ebc1b697df58cc5e3a7cc5`  
**SHA código candidato:** `ab25620e24cf83197a9a2c62d8febaf650d6fd71`  
**Estado:** código de observabilidad CI preparado; ejecución de Quality Gates en nuevo SHA y remediación externa de paquete aún NO verificadas.

## 1. Fallo original: evidencia primaria

- [Quality Gate #44173995200](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44173995200), SHA `1ecd85629fee5dff25ebc1b697df58cc5e3a7cc5`: `completed/failure`. Job `Validator, analyzers, build, tests, coverage, E2E and containers`; primer paso con fallo: **Verify npm security audit and reproducible installation**. El resto de verificaciones obligatorias quedó `skipped`, no `PASS`.
- [Quality Gate #44173494661](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44173494661), SHA `f744afa636c5f000b7b62079f7a03e5aa4c92a08`: mismo bloqueo temprano por `npm audit`.
- [Regresión focal #44173494757](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44173494757), SHA `f744afa636c5f000b7b62079f7a03e5aa4c92a08`: build .NET + pruebas backend focales, Angular ESLint + pruebas + build en `SUCCESS`; **resultado global `FAILURE`** por auditoría npm, no se declara gate verde.
- Dependencia principal identificada en el `package-lock.json`: `node_modules/picomatch` **4.0.7** (transitiva/dev). `npm audit` registra **GHSA-fc5q-39xc-vjpf** (ReDoS, severidad HIGH), con mensaje `No fix available`. Reportó **21 vulnerabilidades altas** propagadas a herramientas como `@angular/build`, Vite, Vitest y `tinyglobby`; no son necesariamente 21 fallos independientes.
- La imagen final del frontend es Nginx que sirve archivos compilados, pero la dependencia continúa siendo riesgo de cadena de suministro del entorno de compilación y **no se exonera**.

## 2. Corrección quirúrgica CI aplicada en el candidato

| Archivo | Cambio |
|---|---|
| `.github/workflows/quality-gates.yml` | Divide `npm ci`, auditoría de producción `npm audit --omit=dev` y auditoría completa `npm audit`. La auditoría completa se registra con `id: full_npm_audit` y `continue-on-error: true` únicamente para completar los validadores siguientes. **El paso final `if: always()` aplica `exit 1` si `steps.full_npm_audit.outcome != success`, manteniendo rojo el gate ante cualquier vulnerabilidad.** |
| `.github/workflows/m1-regresion-focal.yml` | El workflow M1 complementario queda manual `workflow_dispatch` para no duplicar `npm ci/audit` por cada push; la suite institucional `quality-gates.yml` sigue ejecutándose automáticamente en `desarrollo`. |
| `tools/automatizacion/test_ci_gate_audit_contract.py` | Añade pruebas contra silenciamiento o retiro de auditoría y contra disparo duplicado del M1. Se amplía el patrón de descubrimiento a `test_ci_*.py`. |

## 3. Garantías y límites

- **NO** se desactivan `npm audit`, análisis de seguridad, cobertura, pruebas E2E, ni validadores Oracle XE descartables.
- **NO** se sustituye `npm audit` por `audit-level=critical`, `--omit=dev` exclusivamente, ni `|| true` que convierta el gate global en verde.
- La auditoría de producción es adicional; el resultado de auditoría completa continúa siendo **bloqueante**.
- El cambio permite recopilar fallos posteriores (lint, código, tests, XE descartable, contenedores), pero **no remedia la vulnerabilidad upstream**. Se requiere actualización oficial segura de `picomatch` y resolución reproducible del `package-lock.json`, seguida de tests y nueva auditoría integral. No forzar paquetes ni reemplazarlos por una fuente no verificada.
- El `Quality Gate` del candidato **no es SUCCESS hasta observar su ejecución real**. No existe certificación Oracle institucional ni conciliación 242/190/52/7 derivada de este cambio.

## 4. Acciones de verificación

1. Verificar la rama remota en el SHA publicado y ejecutar los tests del router `python3 -m unittest discover -s tools/automatizacion -p 'test_ci_*.py'`.
2. Inspeccionar el último `Quality Gates`: comprobar `npm ci`, auditoría de producción, `npm audit` completo, la ejecución del resto de validaciones y el resultado final bloqueante por auditoría.
3. Si aparecen errores funcionales adicionales en los pasos posteriores, corregir causa raíz sin rebajar controles.
4. Reabrir el aviso oficial GHSA-fc5q-39xc-vjpf cuando haya versión corregida; aplicar solo actualización validada con `npm ci`, pruebas, build, contenedores y auditoría completa.
5. La aceptación del propietario y Oracle institucional siguen ajenos a este diagnóstico CI.

```ini
PROJECT=RIESGO_LAVADO
BRANCH=desarrollo
AUTHOR=CHAT
INITIAL_SHA=1ecd85629fee5dff25ebc1b697df58cc5e3a7cc5
CI_SOURCE_CANDIDATE_SHA=ab25620e24cf83197a9a2c62d8febaf650d6fd71
SECURITY_SCAN=FAIL_PREVIOUS_SHA_PICOMATCH_4.0.7_HIGH
ORIGINAL_QUALITY_GATE=44173995200_FAILURE
CODE_CHANGE_SCOPE=CI_WORKFLOW_AND_TESTS
QUALITY_GATE_NEW_SHA=PENDING_EXECUTION
ORACLE_INSTITUTIONAL_WRITES=0
DNP_IHSS_WRITES=0
MAIN_TOUCHED=NO
PRODUCTION_TOUCHED=NO
```

## 5. Resultado CI funcional posterior — 17 de noviembre de 2026, 15:53 -06:00

- `SOURCE_CODE_SHA=d216edf4c581c5724bfb582f5e05bebea9ff8669`.
- [Quality Gate 44181990671](https://github.com/jmejia31/RIESGO_LAVADO/actions/runs/44181990671): `completed/failure` y **44 pasos SUCCESS / 1 FAIL**, el último `Enforce full npm audit result (blocking)`.
- Backend `1170 PASS / 1 SKIPPED`; Angular `1023 PASS` en `96` archivos; E2E `79 PASS / 15 SKIPPED` (incluye carga institucional de archivos corregida); pruebas Oracle XE, Docker y checks no-root `SUCCESS`.
- No confundir `npm audit --omit=dev: PASS` con `npm audit: FAIL`. `SECURITY_SCAN=FAIL` y `QUALITY_GATE_GLOBAL=FAIL` hasta parche verificable.
- Contrato real jurídico recuperado en `d216edf4c581c5724bfb582f5e05bebea9ff8669`, con validación de identidad/relación de fuente. Expediente de detalle: [M1 CI funcional](M1_CI_FUNCIONAL_CORREGIDO_SEGURIDAD_ABIERTA_20261117.md).
