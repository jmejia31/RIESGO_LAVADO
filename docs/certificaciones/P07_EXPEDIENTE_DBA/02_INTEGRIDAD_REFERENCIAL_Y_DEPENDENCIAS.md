# P07 — Integridad referencial y dependencias

## Alcance

Revisión estática de las relaciones declaradas por el modelo de transición y de sus dependencias visibles en el repositorio. No se consultó Oracle y no se afirma que las restricciones estén creadas o habilitadas en una instancia.

## Modelo estático de 17 tablas

El script 06 declara las siguientes tablas:

    RL_MR_FAMILIAS_FORMULARIO
    RL_MR_VERSIONES_FORMULARIO
    RL_MR_CATALOGOS
    RL_MR_ELEMENTOS_CATALOGO
    RL_MR_REGLAS_CALCULO
    RL_MR_RIESGOS
    RL_MR_EVALUACIONES_RIESGO
    RL_MR_PROYECCIONES_EVALUACION
    RL_MR_FLUJOS_EVALUACION
    RL_MR_CONTROLES_RIESGO
    RL_MR_EVALUACIONES_CONTROL
    RL_MR_PLANES
    RL_MR_ACTIVIDADES
    RL_MR_EVIDENCIAS
    RL_MR_EVIDENCIAS_VINCULOS
    RL_MR_SENALES_ALERTA
    RL_MR_AUTOMONITOREO

## Dependencias declaradas

La definición estática relaciona versiones con familias y usuarios; elementos con catálogos; riesgos, evaluaciones, proyecciones, flujos, controles, planes, señales y automonitoreo con sus entidades padre; y evidencias con usuarios y vínculos. Las referencias externas principales apuntan a RL_USUARIOS.

El detalle verificable permanece en database/19_matrices_riesgos/transicion/06_reconstruir_modelo_17_tablas.sql. La evidencia DBA debe comprobar en ALL_CONSTRAINTS/ALL_CONS_COLUMNS o equivalente autorizado:

- nombre y estado de cada restricción;
- tabla y columna padre/hija;
- columnas NOT NULL requeridas;
- índices y unicidad;
- ausencia de referencias a objetos retirados;
- ausencia de restricciones deshabilitadas o inválidas.

## Riesgo de versión incompatible

El preflight de release documenta un inventario de 25 objetos, mientras que el script 06 y el manifiesto de transición documentan 17. Hasta que el DBA determine cuál contrato corresponde al release autorizado, no es seguro interpretar un resultado parcial como integridad certificada.

## Evidencia que debe aportar el DBA

1. Salida sanitizada del inventario físico.
2. Matriz de restricciones esperadas frente a encontradas.
3. Índices y restricciones únicas verificadas.
4. Objetos inválidos y restricciones deshabilitadas: cero o justificación aprobada.
5. Confirmación de que no se alteraron datos para producir la evidencia.
6. Dictamen independiente sobre la discrepancia 17/17–25/25.

## Resultado P07

P07_REFERENTIAL_REVIEW=STATIC_PASS

P07_PHYSICAL_REFERENTIAL_CERTIFICATION=PENDING_DBA_EVIDENCE
