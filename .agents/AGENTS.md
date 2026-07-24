# Protocolo Transversal de Colaboración Multi-Agente y Desarrollo (Antigravity, Codex, ChatGPT, Usuario)

> [!IMPORTANT]
> **REGLA MANDATORIA DE PRIMERA LECTURA**
> Antes de realizar cualquier inspección, análisis, modificación o ejecución de código en este repositorio, **TODO AGENTE DE IA (Antigravity, Codex, ChatGPT)** y el **Usuario** DEBEN consultar este documento `AGENTS.md` y la `BITACORA_COLABORACION.md`.

---

## 1. Integrantes del Equipo de Desarrollo
- **Antigravity**: Agente de IA para pair programming, arquitectura y refactorización.
- **Codex**: Agente de IA para desarrollo, autocompletado y asistencia técnica.
- **ChatGPT**: Agente de IA para análisis, diseño, generación de código y asistencia estratégica.
- **Usuario (Francisco Pérez / jmejia31)**: Propietario del proyecto, líder de requerimientos y aprobador final.

---

## 2. Flujo de Trabajo Obligatorio para Cada Sesión

### Paso 1: Revisión Inicial Obligatoria
1. Leer `AGENTS.md` (este archivo) para validar estándares y reglas.
2. Leer la bitácora activa en `BITACORA_COLABORACION.md` para conocer:
   - ¿Quién trabajó por última vez? (Antigravity, Codex, ChatGPT o Usuario).
   - ¿Qué cambios exactos se hicieron?
   - ¿En qué estado quedaron las pruebas y la compilación?
   - ¿Cuáles son los siguientes pasos específicos para continuar?
3. Verificar la sincronización con Git:
   ```bash
   git status
   git fetch --all
   git pull origin <rama-actual>
   ```

### Paso 2: Ejecución de Cambios y Estándares
1. **Sin Parches Superficiales**: No silenciar excepciones, ni omitir pruebas fallidas, ni enmascarar errores.
2. **Paridad y Calidad**: Respetar contratos institucionales de reportería Excel y PDF (hoja única, estilos ejecutivos).
3. **Verificación de Pruebas**:
   - Backend C#: `dotnet test backend/RL.API.Tests/RL.API.Tests.csproj` (226+ pruebas).
   - Frontend Angular: `npx ng test --watch=false` en `frontend/rl-app` (27+ pruebas).
   - Compilación Frontend: `npm run build` en `frontend/rl-app`.

### Paso 3: Handoff / Cierre de Turno Obligatorio
Antes de finalizar la intervención o responder al usuario, el agente o participante activo DEBE actualizar `BITACORA_COLABORACION.md` registrando:
1. **Fecha y Hora**: `AAAA-MM-DD HH:MM`.
2. **Autor**: [Antigravity | Codex | ChatGPT | Usuario].
3. **Resumen de Trabajo Realizado**: Lista clara de funcionalidades agregadas, bugs corregidos o artefactos generados.
4. **Archivos Modificados / Creados**: Lista con enlaces o rutas relativas.
5. **Resultado de Verificación**: Estado de compilación y pruebas (`dotnet test`, `ng test`, `ng build`).
6. **Sincronización Git**: Confirmación de `git push` a `https://github.com/jmejia31/RIESGO_LAVADO`.
7. **Punto de Continuación y Próximos Pasos**: Instrucciones exactas para el siguiente agente o usuario sobre qué tarea tomar a continuación.

---

## 3. Repositorio Oficial y Sincronización
- **URL Remote**: `https://github.com/jmejia31/RIESGO_LAVADO.git`
- **Regla de Sincronía**: El entorno local y la rama remota en GitHub DEBEN mantenerse en la misma versión más actualizada. Nunca deben dejarse cambios locales sin empujar o sin confirmar si están validados.