# UAT local de Matrices de Riesgos

## UAT CDP en el escritorio interactivo

Para conservar la sesión del Chromium visible de Javier, ejecutar desde una PowerShell interactiva:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/uat/start-matrices-uat-browser.ps1
```

El script usa únicamente `%TEMP%\RIESGO_LAVADO_UAT\playwright-profile-final-d1-2`, solicita un puerto efímero (`--remote-debugging-port=0`), enlaza CDP a `127.0.0.1` y escribe solo el endpoint en `%TEMP%\RIESGO_LAVADO_UAT\cdp-endpoint.txt`. Después, el runner Codex se conecta al navegador ya abierto:

```powershell
node tools/uat/matrices-uat-cdp.mjs
```

El runner usa exclusivamente `chromium.connectOverCDP`; no lanza Chromium, no crea contextos persistentes y no lee contraseñas, tokens, cookies ni almacenamiento sensible. La ventana debe permanecer abierta durante toda la UAT.

El bootstrap histórico `matrices-uat-session.mjs` queda reservado para diagnóstico local; la UAT autorizada debe usar el flujo CDP descrito arriba para conservar la sesión del Chromium visible de Javier.

```powershell
node tools/uat/matrices-uat-session.mjs
```

La cuenta operativa de UAT es `cuentajavier419@gmail.com`. El login es manual únicamente cuando la sesión ya no puede renovarse; el bootstrap no contiene ni solicita contraseñas y no imprime tokens, cookies o almacenamiento sensible.

El perfil se guarda fuera del repositorio. Se intenta `%LOCALAPPDATA%\RIESGO_LAVADO_UAT\playwright-profile`; si esa ubicación no es escribible, se usa `%TEMP%\RIESGO_LAVADO_UAT\playwright-profile`. Puede establecerse `UAT_PROFILE_DIR` para una ruta externa controlada por el operador.

Requisitos de ejecución: backend en `http://localhost:5043` con Swagger disponible y frontend en `http://localhost:4200`. El script valida ambos servicios antes de abrir Chromium y deja la ventana abierta para continuar la UAT.
