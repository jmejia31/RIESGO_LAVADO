# Remediación NPM audit - 2026-09-11

## Resultado

`NPM_AUDIT_BEFORE=9 moderate vulnerabilities`
`NPM_AUDIT_AFTER=0 vulnerabilities`

La corrección actualiza toda la familia Angular desde `22.0.3` a `22.1.6`
y las herramientas `@angular/build`/`@angular/cli` desde `22.0.4` a
`22.1.6`. Es una actualización compatible dentro de Angular 22; no se hizo
salto mayor, no se bajaron versiones y no se ejecutó `npm audit fix --force`.

## Hallazgos y resolución

Los nueve hallazgos eran moderados y afectaban los paquetes Angular directos:
`@angular/animations`, `common`, `compiler`, `compiler-cli`, `core`, `forms`,
`platform-browser`, `platform-browser-dynamic` y `router`.

- Avisos de sanitización: `GHSA-hh8m-fm6v-7cvg` en la línea Angular menor anterior.
- Aviso de `HttpTransferCache`: `GHSA-p297-fm68-3q8c` en `@angular/common`.
- npm identificó `22.1.6` como versión corregida para la familia afectada.

## Reproducibilidad

Ejecutado desde `frontend/rl-app`:

```text
npm install --package-lock-only --ignore-scripts
npm ci
npm audit --audit-level=moderate
```

Resultado final: `found 0 vulnerabilities`.

Regresión ejecutada después de la actualización:

- `npm run lint`: PASS.
- `npm run build`: PASS; sólo advertencias de presupuesto SCSS y CommonJS de `exceljs` ya existentes.
- `npm test -- --watch=false --no-progress`: `78/78` archivos y `781/781` pruebas PASS.
- `npm run e2e`: `36/36` PASS.

Archivos modificados: `frontend/rl-app/package.json` y
`frontend/rl-app/package-lock.json`. No se cambiaron contratos, backend,
Oracle, reglas de negocio ni dependencias fuera del objetivo de seguridad.
