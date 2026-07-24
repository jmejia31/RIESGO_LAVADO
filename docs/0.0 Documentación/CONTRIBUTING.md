# Contribución

Antes de contribuir, leer obligatoriamente:

- [`AGENTS.md`](../../AGENTS.md).
- [`BITACORA_COLABORACION.md`](../../BITACORA_COLABORACION.md).
- [`ESTADO_COLABORACION.md`](ESTADO_COLABORACION.md).

## Flujo de ramas

1. Actualizar la rama de trabajo:

   ```bash
   git fetch --all --prune
   git switch desarrollo
   git pull --ff-only origin desarrollo
   ```

2. Trabajar y confirmar cambios únicamente en `desarrollo`, salvo autorización expresa de Javier Mejía.
3. Mantener cambios pequeños, trazables y sin secretos.
4. Preservar contratos REST, IDs de módulo, reglas de negocio, motor de cálculo, separación funcional y compatibilidad Oracle, salvo que el requerimiento aprobado indique lo contrario.
5. Compilar y ejecutar las pruebas del componente afectado y las puertas integrales aplicables.
6. Actualizar la bitácora y el estado colaborativo antes de cerrar la intervención.
7. Integrar a `main` únicamente mediante revisión controlada y autorización expresa de Javier Mejía. No hacer commits funcionales directos en `main`.

`main` representa la versión estable; `desarrollo` concentra el trabajo activo. No crear o eliminar ramas adicionales sin autorización y sin verificar previamente que no contengan trabajo pendiente.

## Trazabilidad del cambio

Cada commit o conjunto de commits debe registrar:

- objetivo y alcance;
- archivos modificados;
- riesgos y restricciones preservadas;
- pruebas ejecutadas con resultado real;
- pruebas no ejecutadas y motivo;
- plan de reversión cuando el cambio sea funcional, de infraestructura o de datos.

Los resultados heredados de una intervención anterior deben identificarse como **no reproducidos** cuando no se ejecutaron nuevamente.

## Calidad mínima

```powershell
dotnet restore RIESGO_LAVADO.sln --configfile NuGet.Config
dotnet build RIESGO_LAVADO.sln --no-restore
dotnet test RIESGO_LAVADO.sln --configuration Release --no-restore
cd frontend/rl-app
npm ci
npm run build
npm test -- --watch=false
npm run e2e
cd ../..
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_repository_structure.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_database_scripts.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_documentation_links.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/run_quality_gates.ps1
```

No confirmar `node_modules`, `bin`, `obj`, `dist`, logs, cargas, evidencias de ejecución locales, resultados de cobertura, credenciales ni `appsettings.json`. Las migraciones o actualizaciones Oracle deben ser idempotentes cuando sea posible, incluir validación y documentar respaldo y reversión.

## Convenciones

- C#: tipos y miembros públicos en PascalCase; variables locales y parámetros en camelCase.
- Angular: archivos en kebab-case y símbolos en PascalCase; lógica compartida en servicios o utilidades con consumidores reales.
- SQL: conservar el prefijo y estilo de los objetos existentes; incluir propósito, precondiciones y objetos afectados.
- Commits: mensajes imperativos tipo Conventional Commits, por ejemplo `fix: preservar identificadores en exportación Excel`.
- Arquitectura: respetar [ESTRUCTURA_OBJETIVO.md](ESTRUCTURA_OBJETIVO.md) y no crear carpetas globales por tipo sin justificar su alcance.
- Documentación: usar enlaces relativos del repositorio; no confirmar enlaces `file:///` dependientes de una computadora local.

No agregar comentarios que repitan el código. Documentar reglas, restricciones, decisiones y efectos secundarios no evidentes.
