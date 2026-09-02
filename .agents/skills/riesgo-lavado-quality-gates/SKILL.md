---
name: riesgo-lavado-quality-gates
description: Valida cambios de RIESGO_LAVADO con build, tests, validadores, GitHub Actions y SonarCloud. Usar antes de publicar, certificar, cerrar una UI/fase o diagnosticar un gate rojo.
---

# Quality gates

## Regla de evidencia

Un gate solo está verde cuando existe evidencia actual de la ejecución correspondiente. No inferir éxito a partir de ejecuciones anteriores.

## Secuencia por alcance

Ejecutar lo aplicable definido en `AGENTS.md`, incluyendo backend .NET, frontend Angular, validadores de estructura/base de datos/documentación y quality gates.

## Diagnóstico de fallo

Para cada fallo registrar:

- comando/job/run;
- mensaje técnico exacto;
- archivo o componente afectado cuando exista;
- si es código, configuración, credenciales/permisos, infraestructura o dependencia externa;
- corrección aplicada o acción externa requerida.

## SonarCloud

- Distinguir entre fallo de análisis, Quality Gate y fallo al consultar el gate.
- No atribuir un problema de autorización/token al código.
- Nunca exponer `SONAR_TOKEN` ni otros secretos.
- Si el análisis se cargó pero la consulta del gate falla por permisos, reportarlo exactamente como bloqueo externo y no alterar código para esconderlo.

## Cierre

Antes de certificar:

1. `git diff --check` sin errores.
2. Tests/build aplicables ejecutados.
3. Validadores del repositorio aplicables ejecutados.
4. CI del SHA final revisado.
5. Sonar/Quality Gate revisado cuando corresponda.
6. Cualquier pendiente externo explícitamente identificado.

No usar `SUCCESS` global si uno de los gates requeridos sigue rojo o no ejecutado.
