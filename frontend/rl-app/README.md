# Frontend SGRLA-IHSS

Aplicacion web Angular 22 del Sistema de Gestion de Riesgo de Lavado de Activos. La interfaz se organiza en `core`, `features` y `shared`; las pantallas enrutadas se cargan bajo demanda y consumen la API mediante `/api` en produccion.

## Requisitos

- Node.js `24.18.0`.
- npm `11.12.1`.
- Angular CLI `22.0.4`, instalado localmente por `npm ci`.

## Desarrollo

```powershell
npm ci
npm start
```

La aplicacion queda disponible normalmente en `http://localhost:4200`. El ambiente de desarrollo consume `http://localhost:5043/api`.

## Compilacion y pruebas

```powershell
npm run build
npm test -- --watch=false
npm run test:coverage
npm run e2e
```

El build de produccion se genera en `dist/`, ignorado por Git. Las pruebas unitarias usan Vitest mediante el builder oficial de Angular; la cobertura se genera en `coverage/` y los recorridos E2E usan Playwright con Edge local.

## Convenciones

- Funcionalidades en `src/app/features`.
- Autenticacion y configuracion global en `src/app/core`.
- Layout y elementos transversales en `src/app/shared`.
- Rutas principales y permisos por modulo en `src/app/app.routes.ts`.
- No versionar `node_modules`, `.angular`, `dist`, `coverage` ni configuracion local.

La documentacion general, arquitectura y proceso de contribucion se encuentran en el `README.md` de la raiz del repositorio.
