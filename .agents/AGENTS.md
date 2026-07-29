# Protocolo Transversal de Colaboración Multiagente y Desarrollo

> [!IMPORTANT]
> **REGLA MANDATORIA DE PRIMERA LECTURA**
> Antes de inspeccionar, analizar, modificar, probar o documentar este repositorio, todo participante —**Antigravity, Codex, ChatGPT y Javier Mejía (`jmejia31`)**— debe leer, en este orden:
>
> 1. `AGENTS.md`.
> 2. [`BITACORA_COLABORACION.md`](../BITACORA_COLABORACION.md).
> 3. [`docs/0.0 Documentación/ESTADO_COLABORACION.md`](../docs/0.0%20Documentación/ESTADO_COLABORACION.md).
> 4. `README.md` y la documentación específica del módulo que será intervenido.

---

## 1. Integrantes y responsabilidad

- **Antigravity**: pair programming, arquitectura, refactorización y verificación técnica.
- **Codex**: desarrollo, mantenimiento, pruebas y asistencia técnica.
- **ChatGPT**: análisis, diseño, desarrollo, revisión integral y coordinación de continuidad.
- **Javier Mejía (`jmejia31`)**: propietario, responsable de requerimientos y aprobador final.

Ningún agente puede atribuirse aprobación funcional, cierre de fase, validación Oracle institucional o autorización para publicar en `main` si Javier Mejía no la ha otorgado expresamente.

---

## 2. Política de ramas

- **`desarrollo`**: rama obligatoria para trabajo activo, documentación, correcciones y mejoras.
- **`main`**: rama estable. No se modifica, fusiona ni publica desde una intervención ordinaria sin autorización expresa de Javier Mejía.
- Antes de trabajar se debe comprobar la relación real entre ramas y registrar si existe divergencia de commits o de archivos.
- No crear, eliminar, renombrar ni limpiar ramas adicionales sin autorización expresa y evidencia previa de que no contienen trabajo pendiente.

Comandos de referencia:

```bash
git status
git fetch --all --prune
git switch desarrollo
git pull --ff-only origin desarrollo
git log --oneline --decorate -n 15
git diff --check
```

---

## 3. Flujo obligatorio de cada intervención

### 3.1 Revisión inicial

1. Leer los tres documentos obligatorios de colaboración.
2. Identificar quién trabajó por última vez, qué commits produjo y cuál fue el punto de continuación.
3. Revisar documentación central y documentación del módulo afectado.
4. Comparar lo documentado con el código, dependencias, pruebas y estado Git actuales.
5. Clasificar toda evidencia como:
   - **Ejecutada y verificada en esta intervención**.
   - **Reportada por una intervención anterior, no reproducida**.
   - **Pendiente por dependencia externa**.

No presentar como verificado un resultado heredado que no se ejecutó nuevamente.

### 3.2 Ejecución de cambios

1. Evitar parches superficiales, silenciamiento de excepciones y reducción artificial de pruebas o cobertura.
2. Preservar contratos REST, IDs de módulos, reglas de negocio, motor de cálculo, estructura Oracle y separación funcional entre módulos, salvo autorización expresa.
3. Mantener la paridad institucional entre PDF y Excel cuando aplique.
4. Agregar o actualizar pruebas de regresión para cada cambio funcional.
5. Usar enlaces relativos del repositorio; están prohibidos enlaces locales `file:///C:/...` en documentación versionada.
6. No fijar cifras de pruebas en este protocolo. Los conteos reales pertenecen a la bitácora de cada ejecución.

### 3.3 Verificación mínima

Ejecutar, según el alcance:

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

Cuando una validación no pueda ejecutarse, debe quedar declarada como **pendiente**, con la razón exacta. No se debe sustituir una prueba Oracle, AD, SMTP o de infraestructura por una afirmación documental.

---

## 4. Handoff obligatorio

Antes de finalizar una intervención se deben actualizar **ambos** archivos:

1. [`BITACORA_COLABORACION.md`](../BITACORA_COLABORACION.md): registro cronológico e inmutable de la intervención.
2. [`docs/0.0 Documentación/ESTADO_COLABORACION.md`](../docs/0.0%20Documentación/ESTADO_COLABORACION.md): estado vivo consolidado para continuar.

Cada registro debe incluir:

- fecha y hora local;
- autor;
- rama y commit inicial/final;
- objetivo y alcance;
- archivos creados o modificados;
- cambios funcionales y documentales;
- pruebas ejecutadas con conteos reales;
- pruebas no ejecutadas y motivo;
- estado de Git y publicación remota;
- riesgos, restricciones y pendientes;
- punto exacto de continuación.

La copia `.agents/AGENTS.md` debe mantenerse idéntica a este archivo, salvo las rutas relativas necesarias por su ubicación. Cualquier cambio del protocolo debe actualizar ambas copias en la misma intervención.

---

## 5. Publicación obligatoria al finalizar cada intervención

> [!IMPORTANT]
> **Todo colaborador debe publicar (`git push`) la totalidad de sus cambios en `origin/desarrollo` antes de dar por terminada su intervención.**
> Ningún trabajo puede quedar solo en el repositorio local. El repositorio remoto es la única fuente de verdad compartida.

Esta regla se aplica a:

- código fuente, pruebas y configuración;
- documentación, bitácora y estado colaborativo;
- cualquier otro archivo creado o modificado durante la intervención.

Secuencia mínima de cierre de intervención:

```powershell
git add -A
git commit -m "<tipo>(<módulo>): <descripción concisa>"
git push origin desarrollo
```

Verificar que el push fue aceptado:

```powershell
git status          # debe indicar "nothing to commit, working tree clean"
git log --oneline --decorate -n 5   # el commit HEAD debe coincidir con origin/desarrollo
```

No se considera válido un handoff si:

- existen archivos modificados sin confirmar (`git status` muestra cambios);
- existen commits locales no publicados (`git log` muestra adelanto respecto a `origin/desarrollo`);
- la bitácora o el estado colaborativo no fueron actualizados y publicados.

Cuando un colaborador recibe el turno debe ejecutar `git pull --ff-only origin desarrollo` como primer paso para obtener el estado exacto del anterior.

---

## 6. Repositorio oficial

- **Repositorio**: `https://github.com/jmejia31/RIESGO_LAVADO.git`
- No dejar cambios locales sin commit ni commits validados sin publicar en la rama autorizada.
- No declarar una rama «sincronizada» únicamente porque sus archivos coinciden: también debe revisarse la divergencia de commits.
