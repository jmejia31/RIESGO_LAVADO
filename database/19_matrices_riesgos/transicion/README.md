# Transición manual al modelo reducido

`06_reconstruir_modelo_17_tablas.sql` reconstruye el esquema de Matrices de Riesgos con las 17 tablas aprobadas.

No está incluido en `00_APLICAR_MODULO_MATRICES_RIESGOS.sql`. Es destructivo sobre objetos `RL_MR_*` y solo puede ejecutarse manualmente, después de respaldo validado, despliegue del backend/frontend compatible y autorización expresa.

La invocación futura se realizará únicamente desde SQL*Plus con el parámetro `EJECUTAR`. Esta carpeta no autoriza ni implica su ejecución.
