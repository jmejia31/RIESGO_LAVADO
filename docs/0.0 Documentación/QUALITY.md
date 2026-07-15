# Calidad, cobertura y pruebas E2E

## Puerta integral

Ejecutar desde la raiz del repositorio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/run_quality_gates.ps1
```

El comando ejecuta las pruebas Backend con Coverlet, las pruebas Frontend con cobertura V8, verifica los pisos anti-regresion y finaliza con la suite E2E de Playwright.

Para omitir temporalmente E2E durante trabajo unitario local puede usarse `-SkipE2E`; esa opcion no es valida para cerrar una fase o publicar un cambio funcional.

## Linea base de cobertura

| Componente | Metrica | Resultado actual | Piso automatico |
|---|---|---:|---:|
| Backend | Lineas | 7.11% | 7.1% |
| Backend | Ramas | 6.78% | 6.7% |
| Frontend | Sentencias | 28.31% | 28.3% |
| Frontend | Ramas | 24.80% | 24.8% |
| Frontend | Funciones | 26.59% | 26.5% |
| Frontend | Lineas | 28.30% | 28.3% |

Estos valores son pisos iniciales contra regresiones, no objetivos suficientes de calidad. Son bajos porque la medicion incluye todo `RL.API` y todo `src/app`, no solo los archivos cargados por las pruebas. Los pisos no deben reducirse para hacer pasar un cambio; cada fase funcional debe agregar pruebas y elevarlos de manera gradual.

Los reportes se generan bajo `backend/RL.API.Tests/TestResults` y `frontend/rl-app/coverage`. Ambos directorios estan ignorados por Git.

## Suite E2E inicial

La suite `frontend/rl-app/e2e/login-and-routing.spec.ts` ejecuta cinco escenarios no destructivos:

1. Renderizado del formulario institucional de acceso.
2. Validacion de campos obligatorios sin enviar credenciales.
3. Alternancia segura de visibilidad de la contrasena.
4. Redireccion de una ruta protegida cuando no existe sesion.
5. Redireccion de una ruta desconocida.

Las respuestas publicas de configuracion se simulan dentro del navegador para que los tests no dependan de Oracle ni escriban datos. No se incluyen usuarios, contrasenas o tokens reales.

## Navegador E2E

En Windows local, Playwright usa Microsoft Edge instalado en el sistema. En CI usa Chromium administrado por Playwright; preparar el navegador con:

```powershell
cd frontend/rl-app
npm run e2e:install
```

El ejecutor `scripts/run-e2e.mjs` inicia Angular en `127.0.0.1:4200`, espera que responda, ejecuta Playwright y termina exclusivamente el servidor creado por la prueba.

## Siguiente incremento recomendado

- Backend: ampliar la capa de aplicación de Listas/Matrices mediante pruebas unitarias y de caracterización sin Oracle real; dejar Active Directory/SMTP para un ambiente controlado.
- Frontend: mantener la línea base de 99 pruebas y agregar nuevos casos junto con cada cambio funcional futuro.
- E2E: acceso autenticado mediante un ambiente de pruebas y credenciales efimeras administradas fuera de Git.
- Mantener los recorridos que crean o modifican datos fuera de la suite no destructiva hasta disponer de datos semilla y limpieza transaccional aprobada.
