# UAT local de Matrices de Riesgos

El bootstrap `matrices-uat-session.mjs` abre un contexto persistente de Playwright con Chromium visible y navega a `/matrices-riesgos`.

```powershell
node tools/uat/matrices-uat-session.mjs
```

La cuenta operativa de UAT es `cuentajavier419@gmail.com`. El login es manual únicamente cuando la sesión ya no puede renovarse; el bootstrap no contiene ni solicita contraseñas y no imprime tokens, cookies o almacenamiento sensible.

El perfil se guarda fuera del repositorio. Se intenta `%LOCALAPPDATA%\RIESGO_LAVADO_UAT\playwright-profile`; si esa ubicación no es escribible, se usa `%TEMP%\RIESGO_LAVADO_UAT\playwright-profile`. Puede establecerse `UAT_PROFILE_DIR` para una ruta externa controlada por el operador.

Requisitos de ejecución: backend en `http://localhost:5043` con Swagger disponible y frontend en `http://localhost:4200`. El script valida ambos servicios antes de abrir Chromium y deja la ventana abierta para continuar la UAT.
