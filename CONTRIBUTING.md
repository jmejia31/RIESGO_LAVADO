# Contribución

## Flujo

1. Partir de `main` actualizado y crear una rama temática (`feature/`, `fix/`, `refactor/`, `docs/` o `chore/`).
2. Mantener cambios pequeños, trazables y sin secretos.
3. Preservar contratos REST, IDs de módulo, reglas de negocio y compatibilidad de scripts salvo que el cambio aprobado indique lo contrario.
4. Compilar y ejecutar pruebas del componente afectado.
5. Abrir Pull Request con alcance, riesgos, validaciones y plan de reversión.

## Calidad mínima

```powershell
dotnet restore RIESGO_LAVADO.sln --configfile NuGet.Config
dotnet build RIESGO_LAVADO.sln --no-restore
dotnet test RIESGO_LAVADO.sln --no-build --no-restore
cd frontend/rl-app
npm ci
npm run build
npm test -- --watch=false
```

No confirmar `node_modules`, `bin`, `obj`, `dist`, logs, cargas, evidencias de ejecución locales ni `appsettings.json`. Las migraciones o actualizaciones Oracle deben ser idempotentes cuando sea posible, incluir validación y documentar respaldo/reversión.

## Convenciones

- C#: tipos y miembros públicos en PascalCase; variables locales y parámetros en camelCase.
- Angular: archivos en kebab-case y símbolos en PascalCase; lógica compartida en servicios.
- SQL: conservar el prefijo y estilo de los objetos existentes; incluir propósito, precondiciones y objetos afectados.
- Commits: mensajes imperativos tipo Conventional Commits, por ejemplo `chore: mejorar exclusiones locales`.

No agregar comentarios que repitan el código. Documentar reglas, restricciones, decisiones y efectos secundarios no evidentes.
