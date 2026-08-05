# Revisión de copia local — 5 de agosto de 2026

## Alcance

Se revisó la copia comprimida de `C:\RIESGO_LAVADO` proporcionada por Javier Mejía y se comparó con la rama remota `desarrollo`.

## Resultado de la comparación

La copia local se encontraba en el commit `6e77ee3e390f6578da889ea1e0a455130d80d1ee`, ya existente en GitHub. La rama remota había avanzado posteriormente, por lo que no era seguro publicar el estado local completo ni reemplazar archivos remotos con versiones anteriores.

Los cambios aparentes por finales de línea de Windows fueron descartados de la comparación. Las diferencias reales encontradas fueron:

1. `BITACORA_COLABORACION.md` con caracteres de control y estado local inconsistente. No se publicó para evitar corrupción documental.
2. Eliminación local de `Matrices de Riesgos.xlsx` sin evidencia suficiente de que fuera un retiro intencional. El archivo se conserva en el repositorio.
3. Cambios locales en `MatricesRiesgosRepositoryIntegrationTests.cs` que intentaban ampliar la certificación Oracle.

## Decisión técnica sobre la prueba Oracle

La prueba local no se publicó literalmente porque:

- desactivaba la omisión controlada de Oracle durante las pruebas ordinarias;
- utilizaba identificadores maestros fijos;
- dependía de tablas y secuencias retiradas;
- asumía registros preexistentes;
- no estaba alineada con `RL_MR_EVIDENCIAS_VINCULOS`.

Se realizó una migración segura de su intención funcional:

- vínculo genérico de evidencia a riesgo;
- auditoría transversal con la misma transacción Oracle;
- caso de commit conjunto;
- caso de rollback conjunto inducido después del insert de auditoría;
- datos aislados generados mediante secuencias;
- usuario real obtenido del esquema;
- limpieza explícita al finalizar;
- bloqueo obligatorio mediante `RL_ORACLE_INTEGRATION_REQUIRED=true`;
- conexión solo mediante variables de entorno o User Secrets;
- ausencia de DDL, migraciones automáticas y ejecución del script `05`.

## Commits publicados

- `b3a90680fe1c7cff8e04012201111c62e6fe3b8f` — migración de la prueba Oracle al vínculo genérico.
- `d86656732a4191bb3dc786eb51bb2b374bb60a5a` — actualización del validador para el contrato genérico y los controles de seguridad de la prueba.

## Restricciones vigentes

```text
Oracle: NO EJECUTADO
Script 05: NO EJECUTADO
Matrices de Riesgos.xlsx: CONSERVADO
main: INTACTA
Rama modificada: desarrollo
Certificación Oracle de Fase 1.2: PENDIENTE DE EJECUCIÓN AUTORIZADA
```
