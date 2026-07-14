# Plan controlado de reorganización

## Objetivo

Migrar el repositorio hacia la estructura híbrida definida en `ESTRUCTURA_OBJETIVO.md` sin cambiar contratos REST, reglas de negocio, IDs de módulos ni datos Oracle.

Cada fase debe terminar con código compilable, pruebas aprobadas, árbol Git limpio y un commit independiente en `main`.

## Fases

| Fase | Alcance | Riesgo | Estado |
|---:|---|---|---|
| 0 | Cierre técnico, seguridad, Oracle y pruebas base | Bajo | Completada en `4811290` |
| 1 | Gobierno de estructura, responsabilidades, nombres y validación del repositorio | Bajo | Completada |
| 2 | Frontend: mover modelos y servicios específicos a sus features | Medio | Pendiente |
| 3 | Frontend: dividir componentes grandes y preparar rutas diferidas | Medio/alto | Pendiente |
| 4 | Backend: crear módulos verticales comenzando por un piloto pequeño | Medio | Pendiente |
| 5 | Backend: dividir Listas y Matrices de Riesgos | Alto | Pendiente |
| 6 | Base de datos: empaquetar módulos históricos sin romper maestros | Medio | Pendiente |
| 7 | Limpieza final, documentación, cobertura y validación integral | Bajo | Pendiente |

## Orden de migración frontend

1. `auth` como piloto para validar las reglas de `core` y `features`.
2. `usuarios`, `configuracion` y `auditoria`.
3. Funcionalidades de listas pequeñas: tipos, carga y coincidencias.
4. `monitoreo-listas`.
5. `matrices-riesgos`.

En cada módulo se mueven primero modelos y `data-access`; después se dividen componentes. No se mezclan ambos trabajos en un solo commit cuando el componente supera 500 líneas.

## Orden de migración backend

1. `Catalogos` como piloto por su superficie reducida.
2. `Auditoria` y `Configuracion` para uniformar `Controller → Application → Persistence`.
3. `Auth` y `Usuarios`, conservando JWT y Active Directory como integraciones explícitas.
4. `Listas`, separando carga, monitoreo, coincidencias y evidencias.
5. `MatricesRiesgos`, separando cálculo puro, casos de uso, reportes y persistencia.

## Controles obligatorios por fase

```powershell
dotnet test RIESGO_LAVADO.sln --configuration Release
cd frontend/rl-app
npm.cmd run build
npm.cmd test -- --watch=false
```

Además:

- verificar `git diff --check`;
- ejecutar `tools/validate_repository_structure.ps1`;
- comprobar que no cambien rutas HTTP ni IDs `2` a `10` salvo aprobación explícita;
- validar Oracle en modo lectura cuando el cambio toque contratos de persistencia; y
- mantener `main` como única rama local/remota, según la política solicitada para este repositorio.

## Regla de reversión

Cada fase corresponde a un commit autocontenido. Si una validación funcional falla, se corrige dentro de la fase antes de continuar. No se inicia la siguiente fase con cambios pendientes ni se mezclan reorganizaciones estructurales con nuevas funcionalidades.
