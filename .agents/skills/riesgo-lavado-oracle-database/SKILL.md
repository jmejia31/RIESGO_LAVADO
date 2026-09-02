---
name: riesgo-lavado-oracle-database
description: Diseña, revisa y valida cambios Oracle de RIESGO_LAVADO. Usar para scripts SQL, tablas, constraints, índices, datos semilla, migraciones, consultas, compatibilidad de esquemas y cualquier cambio que afecte database/.
---

# Oracle Database — RIESGO_LAVADO

## Objetivo

Proteger integridad, trazabilidad e idempotencia de la base institucional.

## Fuente de verdad

- Scripts versionados: `database/`.
- Revisar `database/00_MANIFIESTO_SCRIPTS_APROBADOS.md` y los scripts de ejecución segura antes de agregar o alterar DDL/DML.
- No asumir que una base local representa Oracle institucional.

## Reglas obligatorias

1. Determinar si el cambio requiere DDL, DML, seed, índice, constraint o solo lectura.
2. Preferir cambios aditivos y compatibles cuando sea posible.
3. Scripts de actualización deben ser deterministas, repetibles o explícitamente protegidos contra doble ejecución.
4. Mantener claves, constraints e índices coherentes con reglas de negocio.
5. No borrar, renombrar ni transformar datos históricos sin autorización explícita y plan de reversión.
6. No ejecutar DML/DDL en Oracle institucional desde una intervención que solo dispone de acceso de lectura.
7. Nunca presentar una validación sintáctica local como certificación Oracle real.
8. Documentar orden de ejecución y pre/postcondiciones.

## Validación

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/validate_database_scripts.ps1
```

Además, cuando exista acceso autorizado, ejecutar preflight/postflight y registrar resultados reales. Si Oracle no está disponible, el estado queda `PENDIENTE POR DEPENDENCIA EXTERNA`.
