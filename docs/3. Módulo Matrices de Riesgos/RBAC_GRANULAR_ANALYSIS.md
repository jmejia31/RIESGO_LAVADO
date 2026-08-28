# Análisis RBAC granular — Fase 3

## CURRENT_MODEL

La autorización vigente combina `[Authorize]`, roles y `ModuloAuthorize` por
módulo. Fase 3 no modifica roles, permisos ni tablas de autorización.

## GAPS

Las acciones administrativas de catálogos, publicación de formularios y
transiciones de evaluación se protegen por módulo, sin códigos de acción
granulares independientes.

## PROPOSED_PERMISSION_MODEL

Permisos por módulo y acción, evaluados en backend, conservando guards de
seguridad, ownership y concurrencia como invariantes de código.

## PROPOSED_ACTION_CODES

Propuesta para decisión funcional: `CATALOG_READ`, `CATALOG_WRITE`,
`FORM_PUBLISH`, `EVALUATION_TRANSITION`, `AUDIT_READ`.

## BACKEND_IMPACT

Requeriría handlers/policies por acción y una matriz de autorización central.

## FRONTEND_IMPACT

Requeriría ocultar/deshabilitar acciones según claims de permisos, sin usar el
frontend como autoridad.

## DATABASE_IMPACT

No se crean ni modifican `RL_PERMISOS` o `RL_ROL_PERMISOS` en Fase 3.

## MIGRATION_IMPACT

Pendiente de definir estrategia de asignación inicial y compatibilidad con roles
existentes.

## SECURITY_IMPACT

La implementación futura debe conservar deny-by-default, autorización backend,
auditoría y protección contra escalamiento de privilegios.

## DECISIONS_REQUIRED_FROM_BOSS

Confirmar el modelo de acciones, responsables de asignación y política de
migración. `RBAC_GRANULAR_IMPLEMENTATION=0` y `RBAC_CHANGES=0`.
