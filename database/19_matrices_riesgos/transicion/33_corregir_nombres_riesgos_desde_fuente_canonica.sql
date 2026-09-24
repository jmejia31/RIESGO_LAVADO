-- Oracle 11g / MANUAL. Ejecutar después de 31 y 32.
-- El correctivo real vive en 30_fuente_canonica_nombres_riesgos.sql.
-- Ese archivo es la única fuente esperada y aquí se invoca en modo CORRECT.
-- Requiere RL_MR_RIES_NOM_BKP_20260924; la fuente lo valida antes del UPDATE.
-- Actualiza solo RIE_NOMBRE; no modifica IDs, descripciones ni otras tablas.
@@30_fuente_canonica_nombres_riesgos.sql CORRECT
