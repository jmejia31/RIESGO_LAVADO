---
name: riesgo-lavado-frontend-angular
description: Implementa y revisa cambios del frontend Angular de RIESGO_LAVADO en frontend/rl-app. Usar para componentes, rutas, formularios, servicios, guards, interceptores, estilos, accesibilidad, pruebas unitarias, E2E, UX y correcciones visuales.
---

# Frontend Angular — RIESGO_LAVADO

## Objetivo

Modificar el frontend sin romper contratos, navegación, permisos, diseños aprobados ni regresiones existentes.

## Alcance base

- Aplicación: `frontend/rl-app`.
- Stack vigente se obtiene de `package.json`; no asumir versiones por memoria.
- Antes de cambiar arquitectura, consultar CodexGraph cuando esté disponible y después inspeccionar solo los archivos sugeridos.
- Activar también `riesgo-lavado-ui-ux-ihss` si el cambio es visual.
- Activar `riesgo-lavado-testing-regresion` antes del cierre.

## Flujo obligatorio

1. Confirmar `desarrollo` y sincronización con `origin/desarrollo`.
2. Leer `AGENTS.md`, estado colaborativo y documentación del módulo.
3. Identificar componente, ruta, servicio y pruebas realmente afectados.
4. Preservar contratos HTTP, IDs funcionales, guards, RBAC, modelos y estados existentes salvo requerimiento explícito.
5. Reutilizar componentes, renderers, servicios y patrones existentes; no crear implementaciones paralelas por comodidad.
6. Mantener estados `loading`, vacío, error, sin permiso y éxito de forma explícita.
7. Para formularios dinámicos, respetar el renderer, serialización, normalización, validaciones y contratos JSON ya existentes.
8. Toda corrección funcional debe incluir o actualizar regresión automatizada.

## Reglas visuales

- Un prototipo aprobado es contrato visual hasta nueva autorización.
- No sustituir una interfaz aprobada por una reinterpretación simplificada.
- Evitar duplicidad de títulos, filtros, acciones o información.
- Mantener jerarquía, espaciado, responsive, navegación por teclado, foco y legibilidad.
- No ocultar errores funcionales mediante CSS.

## Validación mínima según alcance

```powershell
cd frontend/rl-app
npm ci
npm run lint
npm run build
npm test -- --watch=false
npm run e2e
```

Si una prueba no aplica o no puede ejecutarse, registrar la razón exacta. No declarar `PASS` heredado como prueba fresca.
