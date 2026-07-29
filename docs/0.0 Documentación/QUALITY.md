# Calidad, cobertura y pruebas E2E

## Puerta integral

Ejecutar desde la raíz del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/run_quality_gates.ps1
```

El comando ejecuta pruebas Backend con Coverlet, pruebas Frontend con cobertura V8, verifica los pisos anti-regresión y finaliza con la suite E2E de Playwright.

Para omitir temporalmente E2E durante trabajo unitario local puede usarse `-SkipE2E`; esa opción no es válida para cerrar una fase o publicar un cambio funcional.

## Regla de evidencia

Los conteos de pruebas y resultados de compilación cambian con el proyecto. Por tanto:

- cada intervención debe registrar el resultado exacto en [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md);
- el estado consolidado debe reflejarse en [`ESTADO_COLABORACION.md`](ESTADO_COLABORACION.md);
- un resultado reportado por otro agente no se considera reproducido hasta ejecutar nuevamente el comando;
- no se deben reducir pruebas, cobertura o pisos para hacer pasar una modificación.

La última ejecución local **reportada por Antigravity** el 2026-07-24 indicó 226 pruebas Backend, 165 pruebas Frontend en 18 archivos y build Frontend aprobado. Esa evidencia no dispone de una ejecución CI asociada a sus commits y debe identificarse como **reportada, no reproducida por la auditoría documental posterior**.

## Línea base histórica de cobertura

La siguiente medición corresponde al cierre automatizado del 2026-07-16 y se conserva como referencia histórica; debe reemplazarse por la salida real de la próxima ejecución integral:

| Componente | Métrica | Resultado histórico | Piso automático histórico |
|---|---|---:|---:|
| Backend | Líneas | 15.35% | 15.3% |
| Backend | Ramas | 16.36% | 16.3% |
| Frontend | Sentencias | 31.07% | 31.0% |
| Frontend | Ramas | 26.97% | 26.9% |
| Frontend | Funciones | 29.94% | 29.9% |
| Frontend | Líneas | 31.26% | 31.2% |

Estos valores son pisos contra regresiones, no objetivos suficientes de calidad. La medición incluye todo `RL.API` y todo `src/app`, no solo los archivos cargados por las pruebas. Los pisos no deben reducirse; cada fase funcional debe agregar pruebas y elevarlos gradualmente.

Los reportes se generan bajo `backend/RL.API.Tests/TestResults` y `frontend/rl-app/coverage`. Ambos directorios están ignorados por Git.

## Suite E2E

La suite `frontend/rl-app/e2e/login-and-routing.spec.ts` mantiene recorridos no destructivos para:

1. renderizado del formulario institucional de acceso;
2. validación de campos obligatorios sin enviar credenciales;
3. alternancia segura de visibilidad de contraseña;
4. redirección de rutas protegidas sin sesión;
5. redirección de rutas desconocidas;
6. apertura autenticada y autorizada de Matrices de Riesgos con JWT efímero y API simulada;
7. escenarios adicionales incorporados por fases posteriores, cuando estén presentes en el archivo E2E vigente.

La cantidad exacta de escenarios debe obtenerse de la ejecución actual, no de este listado. Las respuestas de configuración y los escenarios autenticados se simulan dentro del navegador para que las pruebas no dependan de Oracle ni escriban datos. No se incluyen usuarios, contraseñas o tokens reales.

## Navegador E2E

En Windows local, Playwright usa Microsoft Edge instalado en el sistema. En CI usa Chromium administrado por Playwright; preparar el navegador con:

```powershell
cd frontend/rl-app
npm run e2e:install
```

El ejecutor `scripts/run-e2e.mjs` inicia Angular en `127.0.0.1:4200`, espera que responda, ejecuta Playwright y termina exclusivamente el servidor creado por la prueba.

## Siguiente incremento recomendado

- Ejecutar la puerta integral sobre `desarrollo` después de cada cambio funcional y antes de solicitar integración a `main`.
- Mantener todas las pruebas existentes y agregar casos junto con cada modificación.
- Conservar el recorrido autenticado simulado y agregar recorridos contra un ambiente de integración solo cuando existan credenciales efímeras y datos semilla administrados fuera de Git.
- Mantener operaciones destructivas o de escritura fuera de la suite no destructiva hasta disponer de limpieza transaccional aprobada.
- Actualizar esta línea base únicamente con artefactos reproducibles de una ejecución integral.
