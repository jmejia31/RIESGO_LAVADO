# Transición manual al modelo reducido

Esta carpeta contiene el paquete controlado para la transición física del Módulo Matrices de Riesgos al modelo aprobado de 17 tablas y 17 secuencias.

## Orden previsto

1. `prepare_matrices_phase10_evidence.ps1`
   - Ubicación: `scripts/operations/prepare_matrices_phase10_evidence.ps1`.
   - Verifica rama, commit y árbol limpio.
   - Calcula hashes SHA-256.
   - No conecta ni ejecuta Oracle.

2. `07_preflight_inventario_oracle_solo_lectura.sql`
   - Identifica el ambiente.
   - Verifica el esquema `RIESGO_LAVADO` y los objetos institucionales.
   - Inventaría tablas, secuencias y registros existentes.
   - No contiene DDL ni DML.

3. `06_reconstruir_modelo_17_tablas.sql`
   - Reconstruye el esquema de Matrices de Riesgos con las 17 tablas aprobadas.
   - Es destructivo sobre objetos `RL_MR_*`.
   - Solo puede ejecutarse manualmente con el parámetro literal `EJECUTAR`.

4. `08_postflight_verificacion_modelo_17_tablas_solo_lectura.sql`
   - Exige exactamente 17 tablas y 17 secuencias.
   - Detecta objetos faltantes, inesperados o heredados.
   - Revisa claves primarias, restricciones y objetos inválidos.
   - No contiene DDL ni DML.

5. Fase 11
   - Ejecuta la certificación física y funcional Oracle.
   - El postflight de Fase 10 no sustituye la suite de integración.

6. `09_limpieza_tablas_respaldo_b10.sql`
   - Limpieza idempotente y segura de respaldos temporales (`B10_001` .. `B10_041`, `BKP_F10_MAP`, `BKP_F10_SECUENCIAS`).
   - Solo se ejecuta manualmente tras autorizar la eliminación definitiva de respaldos del modelo previo.


## Restricciones

El script `06` no está incluido en `00_APLICAR_MODULO_MATRICES_RIESGOS.sql` ni en instaladores automáticos.

Antes de ejecutarlo deben existir:

- ambiente Oracle exclusivo de pruebas;
- confirmación de no Producción;
- respaldo completo;
- prueba de restauración;
- responsables designados;
- preflight revisado;
- decisión escrita sobre datos existentes;
- hashes del commit autorizado;
- Quality Gate en `success`;
- autorización expresa separada.

La conexión debe suministrarse por un mecanismo seguro y nunca almacenarse en Git, archivos SQL, documentación o logs.

**No existe autorización implícita.** La presencia de estos archivos no autoriza Oracle, SQL*Plus, el script `06`, migraciones ni cambios sobre `main`.
