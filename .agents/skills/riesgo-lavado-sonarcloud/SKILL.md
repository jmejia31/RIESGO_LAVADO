---
name: riesgo-lavado-sonarcloud
description: Diagnostica y gobierna SonarCloud en RIESGO_LAVADO. Usar para sonar-analysis.yml, Quality Gate, cobertura, bugs, vulnerabilidades, hotspots, duplicación, permisos de SONAR_TOKEN y certificación de análisis por SHA.
---

# SonarCloud

## Objetivo

Distinguir con precisión fallos de análisis, Quality Gate, métricas y autorización.

## Flujo

1. Confirmar proyecto, organización, branch y SHA analizado.
2. Revisar `.github/workflows/sonar-analysis.yml` y configuración Sonar versionada.
3. Separar:
   - fallo al ejecutar/subir análisis;
   - fallo al consultar Quality Gate;
   - Quality Gate rojo por métricas;
   - fallo de autorización/token;
   - problema externo de SonarCloud.
4. Extraer el mensaje técnico exacto del job.
5. Si falla por código o métricas, localizar archivos/reglas y corregir causa raíz.
6. Si falla por permisos de token, no modificar producto para ocultarlo; reportar el permiso externo requerido.
7. Certificar únicamente un run asociado al SHA que se pretende cerrar.

## Reglas

- Nunca imprimir `SONAR_TOKEN`.
- No bajar umbrales ni excluir código para maquillar calidad salvo decisión explícita justificada.
- Cobertura, bugs, vulnerabilidades, hotspots y duplicación son categorías diferentes; no inferir una sin evidencia.
- `Quality Gates = SUCCESS` no implica automáticamente `Sonar Analysis = SUCCESS`, y viceversa.
