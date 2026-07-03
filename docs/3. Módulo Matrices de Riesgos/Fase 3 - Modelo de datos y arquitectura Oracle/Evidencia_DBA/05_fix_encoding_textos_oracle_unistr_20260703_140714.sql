-- ============================================================
-- Sistema de Gestion de Riesgos LA/FT - IHSS
-- Fase 3. Modelo de datos y arquitectura Oracle
-- Script: 04_F3_fix_encoding_textos_oracle.sql
-- Objetivo: Corregir codificacion de tildes y caracteres especiales en textos descriptivos RL_MR_*.
-- Clasificacion: Correctivo aprobado para cierre DBA controlado.
-- Responsable documental: Javier Mejia
-- Reglas: sin DROP, sin TRUNCATE, sin DELETE, sin renombrar tablas ni columnas.
-- Nota tecnica: el script usa UNISTR para evitar dependencia de la codificacion del cliente SQLPlus.
-- ============================================================

SET DEFINE OFF;
SET SERVEROUTPUT ON SIZE UNLIMITED;

PROMPT === Correccion de comentarios Oracle RL_MR_* ===
COMMENT ON TABLE RL_MR_MODELOS IS UNISTR('Versiones metodol\00F3gicas aprobables/aprobadas del m\00F3dulo Matrices de Riesgos.');
COMMENT ON TABLE RL_MR_FACTORES IS UNISTR('Factores institucionales obligatorios: Proveedores, Clientes/Patronos y Empleados.');
COMMENT ON TABLE RL_MR_VARIABLES IS UNISTR('Variables internas por factor institucional, con ponderaci\00F3n interna totalizable al 100% por factor.');
COMMENT ON TABLE RL_MR_ESCALAS IS UNISTR('Rangos y niveles de calificaci\00F3n para variables, riesgo inherente, residual y controles.');
COMMENT ON TABLE RL_MR_CRITERIOS IS UNISTR('Criterios de calificaci\00F3n por variable y escala.');
COMMENT ON TABLE RL_MR_MATRICES IS UNISTR('Encabezado de matrices generadas por sujeto evaluado o matriz institucional.');
COMMENT ON TABLE RL_MR_DETALLE IS UNISTR('Detalle de variables evaluadas en una matriz, con snapshot de peso y puntaje.');
COMMENT ON TABLE RL_MR_CONTROLES IS UNISTR('Controles mitigantes asociados a la matriz y a factores cuando aplique.');
COMMENT ON TABLE RL_MR_RESULTADOS IS UNISTR('Resultados de riesgo inherente, mitigaci\00F3n y riesgo residual por factor e institucional.');
COMMENT ON TABLE RL_MR_PLANES_ACCION IS UNISTR('Planes de acci\00F3n obligatorios o voluntarios asociados al resultado de la matriz.');
COMMENT ON TABLE RL_MR_EVIDENCIAS IS UNISTR('Metadatos de evidencias protegidas asociadas a matrices, controles o planes.');
COMMENT ON TABLE RL_MR_HISTORIAL IS UNISTR('Historial funcional del m\00F3dulo; complementa RL_AUDITORIA para trazabilidad espec\00EDfica de matrices.');
COMMENT ON TABLE RL_MR_INTEGRACION_DNP IS UNISTR('Bandeja local para integraci\00F3n futura obligatoria hacia DNP, sin escritura directa hasta aprobaci\00F3n t\00E9cnica.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ID IS UNISTR('Identificador \00FAnico del modelo metodol\00F3gico de matrices de riesgos.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_NOMBRE IS UNISTR('Nombre del modelo metodol\00F3gico.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_VERSION IS UNISTR('Versi\00F3n funcional y t\00E9cnica del modelo metodol\00F3gico.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_DESCRIPCION IS UNISTR('Descripci\00F3n general del alcance metodol\00F3gico del modelo.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ESTADO IS UNISTR('Estado del modelo: BORRADOR, EN_REVISION, APROBADO, CERRADO o INACTIVO.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_VIGENCIA IS UNISTR('Fecha desde la cual la versi\00F3n metodol\00F3gica puede aplicarse.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_CIERRE IS UNISTR('Fecha de cierre o retiro de la versi\00F3n metodol\00F3gica.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_APROBADO_POR IS UNISTR('Usuario responsable de aprobar la versi\00F3n metodol\00F3gica.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_APROBACION IS UNISTR('Fecha de aprobaci\00F3n de la versi\00F3n metodol\00F3gica.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_MOTIVO_ESTADO IS UNISTR('Motivo funcional del cambio de estado del modelo.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_USR_CREACION_ID IS UNISTR('Usuario que registr\00F3 el modelo.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_USR_MODIF_ID IS UNISTR('\00DAltimo usuario que modific\00F3 el registro.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_MODIF IS UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ESTADO_REGISTRO IS UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ID IS UNISTR('Identificador \00FAnico del factor institucional.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_MODELO_ID IS UNISTR('Modelo metodol\00F3gico al que pertenece el factor.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_CODIGO IS UNISTR('C\00F3digo funcional del factor institucional.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_NOMBRE IS UNISTR('Nombre del factor institucional.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_DESCRIPCION IS UNISTR('Descripci\00F3n funcional del factor institucional.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_PESO_INSTITUCIONAL IS UNISTR('Peso institucional fijo del factor dentro del riesgo total.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ORDEN IS UNISTR('Orden de presentaci\00F3n y c\00E1lculo del factor.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ESTADO_REGISTRO IS UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_MOTIVO_INACTIVO IS UNISTR('Motivo obligatorio cuando el factor se inactive l\00F3gicamente.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_USR_CREACION_ID IS UNISTR('Usuario que registr\00F3 el factor.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_USR_MODIF_ID IS UNISTR('\00DAltimo usuario que modific\00F3 el registro.');
COMMENT ON COLUMN RL_MR_FACTORES.MRF_FECHA_MODIF IS UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ID IS UNISTR('Identificador \00FAnico de la variable interna.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FACTOR_ID IS UNISTR('Factor institucional al que pertenece la variable.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_CODIGO IS UNISTR('C\00F3digo funcional de la variable dentro del factor.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_NOMBRE IS UNISTR('Nombre de la variable de riesgo.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_DESCRIPCION IS UNISTR('Descripci\00F3n funcional de la variable de riesgo.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_PESO_INTERNO IS UNISTR('Peso interno de la variable dentro del factor; debe totalizar 100% por factor.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_TIPO_DATO IS UNISTR('Tipo de dato esperado para capturar o calcular la variable.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FUENTE_DATO IS UNISTR('Origen funcional del dato: captura, consulta o integraci\00F3n autorizada.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_OBLIGATORIA IS UNISTR('Indica si la variable es obligatoria para completar la matriz.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ORDEN IS UNISTR('Orden de presentaci\00F3n y c\00E1lculo de la variable.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ESTADO_REGISTRO IS UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_MOTIVO_INACTIVO IS UNISTR('Motivo obligatorio cuando la variable se inactive l\00F3gicamente.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_USR_CREACION_ID IS UNISTR('Usuario que registr\00F3 la variable.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_USR_MODIF_ID IS UNISTR('\00DAltimo usuario que modific\00F3 el registro.');
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FECHA_MODIF IS UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ID IS UNISTR('Identificador \00FAnico de la escala metodol\00F3gica.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_MODELO_ID IS UNISTR('Modelo metodol\00F3gico al que pertenece la escala.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_TIPO IS UNISTR('Tipo de escala: VARIABLE, INHERENTE, RESIDUAL o CONTROL.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_VALOR_MIN IS UNISTR('Valor m\00EDnimo del rango de la escala.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_VALOR_MAX IS UNISTR('Valor m\00E1ximo del rango de la escala.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_NIVEL IS UNISTR('Nivel funcional asignado al rango de la escala.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_COLOR_HEX IS UNISTR('Color sugerido para representar visualmente el nivel.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_DESCRIPCION IS UNISTR('Descripci\00F3n funcional del rango o nivel de escala.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ORDEN IS UNISTR('Orden de presentaci\00F3n del nivel dentro de la escala.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ESTADO_REGISTRO IS UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.');
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_MOTIVO_INACTIVO IS UNISTR('Motivo obligatorio cuando la escala se inactive l\00F3gicamente.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ID IS UNISTR('Identificador \00FAnico del criterio de calificaci\00F3n.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VARIABLE_ID IS UNISTR('Variable de riesgo a la que pertenece el criterio.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ESCALA_ID IS UNISTR('Escala relacionada con el criterio cuando aplique.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VALOR_DESDE IS UNISTR('Valor inicial del rango del criterio.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VALOR_HASTA IS UNISTR('Valor final del rango del criterio.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_PUNTAJE IS UNISTR('Puntaje asignado cuando el criterio se cumple.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_DESCRIPCION IS UNISTR('Descripci\00F3n funcional del criterio de calificaci\00F3n.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ESTADO_REGISTRO IS UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.');
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_MOTIVO_INACTIVO IS UNISTR('Motivo obligatorio cuando el criterio se inactive l\00F3gicamente.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ID IS UNISTR('Identificador \00FAnico de la matriz generada.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_MODELO_ID IS UNISTR('Modelo metodol\00F3gico usado por la matriz.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SUJETO_TIPO IS UNISTR('Tipo de sujeto evaluado por la matriz.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SUJETO_ID_EXT IS UNISTR('Identificador externo del sujeto evaluado cuando aplique.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_DOCUMENTO IS UNISTR('Documento, c\00F3digo o n\00FAmero de referencia del sujeto evaluado.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_NOMBRE_SUJETO IS UNISTR('Nombre o descripci\00F3n del sujeto evaluado.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ORIGEN_DATOS IS UNISTR('Origen de datos usado para construir la matriz.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ESTADO IS UNISTR('Estado funcional de la matriz: BORRADOR, CALCULADA, EN_REVISION, OBSERVADA, APROBADA, CERRADA o INACTIVA.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_EVALUACION IS UNISTR('Fecha en que se registra o calcula la evaluaci\00F3n.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_CIERRE IS UNISTR('Fecha de cierre formal de la matriz.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_CERRADO_POR IS UNISTR('Usuario que cerr\00F3 formalmente la matriz.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_MOTIVO_ESTADO IS UNISTR('Motivo funcional del cambio de estado de la matriz.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SNAPSHOT_METODO IS UNISTR('Snapshot de la metodolog\00EDa usada para proteger matrices cerradas.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_USR_CREACION_ID IS UNISTR('Usuario que registr\00F3 la matriz.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_USR_MODIF_ID IS UNISTR('\00DAltimo usuario que modific\00F3 el registro.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_MODIF IS UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ESTADO_REGISTRO IS UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_ID IS UNISTR('Identificador \00FAnico del detalle evaluado.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_MATRIZ_ID IS UNISTR('Matriz a la que pertenece el detalle.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_VARIABLE_ID IS UNISTR('Variable evaluada dentro de la matriz.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_VALOR_CAPTURADO IS UNISTR('Valor capturado, consultado o calculado para la variable.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PUNTAJE IS UNISTR('Puntaje asignado a la variable.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PESO_SNAPSHOT IS UNISTR('Peso de la variable al momento del c\00E1lculo.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PUNTAJE_PONDERADO IS UNISTR('Resultado ponderado de la variable.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_JUSTIFICACION IS UNISTR('Justificaci\00F3n funcional del valor o puntaje asignado.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_FUENTE_DATO IS UNISTR('Fuente del dato usado para evaluar la variable.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_SNAPSHOT_VARIABLE IS UNISTR('Snapshot de la variable y criterio usados en el c\00E1lculo.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_USR_CREACION_ID IS UNISTR('Usuario que registr\00F3 el detalle.');
COMMENT ON COLUMN RL_MR_DETALLE.MRD_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del detalle.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_ID IS UNISTR('Identificador \00FAnico del control mitigante.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_MATRIZ_ID IS UNISTR('Matriz a la que pertenece el control mitigante.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FACTOR_ID IS UNISTR('Factor institucional al que se asocia el control cuando aplique.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_NOMBRE IS UNISTR('Nombre del control mitigante.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_DESCRIPCION IS UNISTR('Descripci\00F3n funcional del control mitigante.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_PERIODICIDAD IS UNISTR('Periodicidad con la que opera o se revisa el control.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_OPORTUNIDAD IS UNISTR('Oportunidad del control respecto al evento de riesgo.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_AUTOMATIZACION IS UNISTR('Nivel de automatizaci\00F3n del control.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_PROCEDIMIENTOS IS UNISTR('Nivel de formalizaci\00F3n de procedimientos del control.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_CALIDAD IS UNISTR('Calidad general del control seg\00FAn metodolog\00EDa aprobada.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_EFECTIVIDAD_PCT IS UNISTR('Porcentaje de efectividad o mitigaci\00F3n calculada para el control.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_RESPONSABLE IS UNISTR('Responsable funcional del control.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_ESTADO IS UNISTR('Estado funcional del control.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_MOTIVO_INACTIVO IS UNISTR('Motivo obligatorio cuando el control se inactive l\00F3gicamente.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_EVIDENCIA_OBL IS UNISTR('Indica si el control requiere evidencia documental obligatoria.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_USR_CREACION_ID IS UNISTR('Usuario que registr\00F3 el control.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_USR_MODIF_ID IS UNISTR('\00DAltimo usuario que modific\00F3 el registro.');
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FECHA_MODIF IS UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_ID IS UNISTR('Identificador \00FAnico del resultado de c\00E1lculo.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MATRIZ_ID IS UNISTR('Matriz a la que pertenece el resultado.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_FACTOR_ID IS UNISTR('Factor institucional asociado cuando el resultado es por factor.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_TIPO_RESULTADO IS UNISTR('Tipo de resultado: por factor o institucional.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_VERSION_CALCULO IS UNISTR('Versi\00F3n del algoritmo o regla de c\00E1lculo usada.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_ES_VIGENTE IS UNISTR('Indica si el resultado es el vigente para la matriz, factor y tipo.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_PUNTAJE_INHERENTE IS UNISTR('Puntaje de riesgo inherente calculado.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_NIVEL_INHERENTE IS UNISTR('Nivel de riesgo inherente calculado.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MITIGACION_PCT IS UNISTR('Porcentaje de mitigaci\00F3n aplicado por controles.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_PUNTAJE_RESIDUAL IS UNISTR('Puntaje de riesgo residual calculado.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_NIVEL_RESIDUAL IS UNISTR('Nivel de riesgo residual calculado.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_REQUIERE_PLAN IS UNISTR('Indica si el resultado exige plan de acci\00F3n.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MOTIVO_RECALCULO IS UNISTR('Motivo funcional cuando se genera un rec\00E1lculo.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_RESULTADO_ANTERIOR_ID IS UNISTR('Resultado anterior relacionado con el rec\00E1lculo.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_SNAPSHOT_CALCULO IS UNISTR('Snapshot de entradas, reglas y salida del c\00E1lculo.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_FECHA_CALCULO IS UNISTR('Fecha en que se gener\00F3 el resultado.');
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_USR_CALCULO_ID IS UNISTR('Usuario que ejecut\00F3 o solicit\00F3 el c\00E1lculo.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ID IS UNISTR('Identificador \00FAnico del plan de acci\00F3n.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MATRIZ_ID IS UNISTR('Matriz asociada al plan de acci\00F3n.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_RESULTADO_ID IS UNISTR('Resultado que origin\00F3 el plan de acci\00F3n cuando aplique.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ACTIVIDAD IS UNISTR('Actividad o acci\00F3n correctiva definida.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_RESPONSABLE IS UNISTR('Responsable funcional de ejecutar la actividad.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_PERIODICIDAD IS UNISTR('Periodicidad de ejecuci\00F3n o seguimiento del plan.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_INICIO IS UNISTR('Fecha planificada de inicio.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_FIN IS UNISTR('Fecha planificada o real de finalizaci\00F3n.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MEDIO_PRUEBA IS UNISTR('Medio de prueba requerido para evidenciar cumplimiento.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_OBSERVACIONES IS UNISTR('Observaciones funcionales del plan de acci\00F3n.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ESTADO IS UNISTR('Estado funcional del plan de acci\00F3n.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MOTIVO_CIERRE IS UNISTR('Motivo o justificaci\00F3n del cierre del plan.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_USR_CREACION_ID IS UNISTR('Usuario que registr\00F3 el plan de acci\00F3n.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_USR_CIERRE_ID IS UNISTR('Usuario que cerr\00F3 el plan de acci\00F3n.');
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_CIERRE IS UNISTR('Fecha de cierre del plan de acci\00F3n.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_ID IS UNISTR('Identificador \00FAnico de la evidencia documental.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_MATRIZ_ID IS UNISTR('Matriz asociada a la evidencia.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_CONTROL_ID IS UNISTR('Control asociado a la evidencia cuando aplique.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_PLAN_ID IS UNISTR('Plan de acci\00F3n asociado a la evidencia cuando aplique.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_NOMBRE_ORIGINAL IS UNISTR('Nombre original del archivo cargado por el usuario.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_NOMBRE_FISICO IS UNISTR('Nombre f\00EDsico seguro asignado al archivo.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_TIPO_MIME IS UNISTR('Tipo MIME identificado para el archivo.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_EXTENSION IS UNISTR('Extensi\00F3n validada del archivo.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_TAMANO_BYTES IS UNISTR('Tama\00F1o del archivo en bytes.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_RUTA_FISICA IS UNISTR('Ruta protegida de almacenamiento f\00EDsico.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_HASH_SHA256 IS UNISTR('Huella SHA-256 del archivo cuando aplique.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_ESTADO_REGISTRO IS UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_MOTIVO_INACTIVO IS UNISTR('Motivo obligatorio cuando la evidencia se inactive l\00F3gicamente.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_USR_CREACION_ID IS UNISTR('Usuario que carg\00F3 o registr\00F3 la evidencia.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del registro.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_USR_INACTIVO_ID IS UNISTR('Usuario que realiz\00F3 la eliminaci\00F3n l\00F3gica.');
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_FECHA_INACTIVO IS UNISTR('Fecha de eliminaci\00F3n l\00F3gica de la evidencia.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ID IS UNISTR('Identificador \00FAnico del evento hist\00F3rico funcional.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_MATRIZ_ID IS UNISTR('Matriz asociada al evento hist\00F3rico cuando aplique.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_TABLA IS UNISTR('Tabla funcional sobre la que ocurri\00F3 el evento.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_REGISTRO_ID IS UNISTR('Identificador del registro afectado por el evento.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ACCION IS UNISTR('Acci\00F3n funcional registrada.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ESTADO_ANTERIOR IS UNISTR('Estado anterior del registro cuando aplique.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ESTADO_NUEVO IS UNISTR('Estado nuevo del registro cuando aplique.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_MOTIVO IS UNISTR('Motivo funcional del evento hist\00F3rico.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_DATOS_ANT IS UNISTR('Snapshot de datos anteriores cuando aplique.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_DATOS_NVO IS UNISTR('Snapshot de datos nuevos cuando aplique.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_USR_ID IS UNISTR('Usuario responsable del evento.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_USR_EMAIL IS UNISTR('Correo del usuario responsable del evento.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_IP IS UNISTR('Direcci\00F3n IP registrada para el evento.');
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_FECHA IS UNISTR('Fecha y hora del evento hist\00F3rico.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_ID IS UNISTR('Identificador \00FAnico del registro de integraci\00F3n futura con DNP.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_MATRIZ_ID IS UNISTR('Matriz asociada a la calificaci\00F3n que se integrar\00E1.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_NUMERO_PATRONO IS UNISTR('N\00FAmero de patrono relacionado cuando aplique.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_CALIFICACION IS UNISTR('Calificaci\00F3n de riesgo preparada para integraci\00F3n futura.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_PUNTAJE_RESIDUAL IS UNISTR('Puntaje residual asociado a la calificaci\00F3n.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_ESTADO_ENVIO IS UNISTR('Estado de la integraci\00F3n futura: PENDIENTE, ENVIADO, ERROR o ANULADO.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_INTENTOS IS UNISTR('Cantidad de intentos de env\00EDo registrados.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_RESPUESTA IS UNISTR('Respuesta t\00E9cnica recibida de la integraci\00F3n cuando aplique.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_FECHA_CREACION IS UNISTR('Fecha de creaci\00F3n del registro de integraci\00F3n.');
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_FECHA_ENVIO IS UNISTR('Fecha de env\00EDo hacia la integraci\00F3n cuando aplique.');

PROMPT === Correccion de textos descriptivos del modulo Matrices de Riesgos ===
UPDATE RL_MODULOS
   SET MOD_NOMBRE = UNISTR('Matrices de Riesgos'),
       MOD_DESCRIPCION = UNISTR('M\00F3dulo para evaluaci\00F3n, c\00E1lculo, seguimiento y reporter\00EDa de matrices de riesgos LA/FT.'),
       MOD_RUTA = '/matrices-riesgos',
       MOD_ICONO = 'chart-column',
       MOD_SECCION = UNISTR('Riesgos LA/FT'),
       MOD_ACTIVO = 1
 WHERE MOD_ID = 10
    OR MOD_RUTA = '/matrices-riesgos';

PROMPT === Correccion de textos semilla RL_MR_* ===
DECLARE
  v_modelo_id NUMBER;

  PROCEDURE upd_factor(
    p_codigo IN VARCHAR2,
    p_nombre IN VARCHAR2,
    p_descripcion IN VARCHAR2
  ) IS
  BEGIN
    UPDATE RL_MR_FACTORES
       SET MRF_NOMBRE = p_nombre,
           MRF_DESCRIPCION = p_descripcion,
           MRF_USR_MODIF_ID = 1,
           MRF_FECHA_MODIF = SYSDATE
     WHERE MRF_MODELO_ID = v_modelo_id
       AND MRF_CODIGO = p_codigo;
  END;

  PROCEDURE upd_variable(
    p_factor_codigo IN VARCHAR2,
    p_codigo IN VARCHAR2,
    p_nombre IN VARCHAR2,
    p_descripcion IN VARCHAR2
  ) IS
  BEGIN
    UPDATE RL_MR_VARIABLES v
       SET v.MRV_NOMBRE = p_nombre,
           v.MRV_DESCRIPCION = p_descripcion,
           v.MRV_USR_MODIF_ID = 1,
           v.MRV_FECHA_MODIF = SYSDATE
     WHERE v.MRV_CODIGO = p_codigo
       AND EXISTS (
             SELECT 1
               FROM RL_MR_FACTORES f
              WHERE f.MRF_ID = v.MRV_FACTOR_ID
                AND f.MRF_MODELO_ID = v_modelo_id
                AND f.MRF_CODIGO = p_factor_codigo
           );
  END;

  PROCEDURE upd_escala(
    p_tipo IN VARCHAR2,
    p_min IN NUMBER,
    p_max IN NUMBER,
    p_nivel IN VARCHAR2,
    p_descripcion IN VARCHAR2
  ) IS
  BEGIN
    UPDATE RL_MR_ESCALAS
       SET MRE_NIVEL = p_nivel,
           MRE_DESCRIPCION = p_descripcion
     WHERE MRE_MODELO_ID = v_modelo_id
       AND MRE_TIPO = p_tipo
       AND MRE_VALOR_MIN = p_min
       AND MRE_VALOR_MAX = p_max;
  END;

  PROCEDURE upd_variables_factor(p_factor_codigo IN VARCHAR2) IS
  BEGIN
    upd_variable(p_factor_codigo, 'V01', UNISTR('Perfil del sujeto evaluado'), UNISTR('Condiciones generales del sujeto evaluado seg\00FAn factor institucional.'));
    upd_variable(p_factor_codigo, 'V02', UNISTR('Actividad, rubro o funci\00F3n'), UNISTR('Actividad econ\00F3mica, rubro, funci\00F3n o naturaleza operativa relacionada con el factor.'));
    upd_variable(p_factor_codigo, 'V03', UNISTR('Ubicaci\00F3n geogr\00E1fica'), UNISTR('Exposici\00F3n por ubicaci\00F3n, zona, municipio, pa\00EDs o jurisdicci\00F3n aplicable.'));
    upd_variable(p_factor_codigo, 'V04', UNISTR('Antecedentes y coincidencias'), UNISTR('Historial, coincidencias, alertas, sanciones, observaciones o eventos relevantes.'));
    upd_variable(p_factor_codigo, 'V05', UNISTR('Comportamiento transaccional u operativo'), UNISTR('Comportamiento, volumen, recurrencia, variaci\00F3n o se\00F1ales operativas relevantes.'));
    upd_variable(p_factor_codigo, 'V06', UNISTR('Canal, producto o relaci\00F3n institucional'), UNISTR('Canal de vinculaci\00F3n, relaci\00F3n institucional, servicio, proceso o modalidad de interacci\00F3n.'));
    upd_variable(p_factor_codigo, 'V07', UNISTR('Control interno y evidencia disponible'), UNISTR('Nivel de documentaci\00F3n, soporte, trazabilidad y evidencia disponible para sustentar la evaluaci\00F3n.'));
  END;
BEGIN
  SELECT MIN(MRM_ID)
    INTO v_modelo_id
    FROM RL_MR_MODELOS
   WHERE MRM_VERSION = '1.0'
     AND MRM_ESTADO = 'APROBADO'
     AND MRM_ESTADO_REGISTRO = 1;

  IF v_modelo_id IS NULL THEN
    RAISE_APPLICATION_ERROR(-20140, 'No se encontro el modelo base aprobado de Matrices de Riesgos para corregir textos.');
  END IF;

  UPDATE RL_MR_MODELOS
     SET MRM_NOMBRE = UNISTR('Metodolog?a base LA/FT IHSS'),
         MRM_DESCRIPCION = UNISTR('Modelo inicial aprobado metodol?gicamente en Fase 2 para factores institucionales, variables internas, escalas base y rangos de riesgo.'),
         MRM_MOTIVO_ESTADO = UNISTR('Metodolog?a base alineada con Fase 2 aprobada.'),
         MRM_USR_MODIF_ID = 1,
         MRM_FECHA_MODIF = SYSDATE
   WHERE MRM_ID = v_modelo_id;

  upd_factor('PROVEEDORES', UNISTR('Proveedores'), UNISTR('Factor institucional de proveedores. Peso fijo definido por requerimiento del cliente.'));
  upd_factor('CLIENTES_PATRONOS', UNISTR('Clientes/Patronos'), UNISTR('Factor institucional de clientes o patronos. Peso fijo definido por requerimiento del cliente.'));
  upd_factor('EMPLEADOS', UNISTR('Empleados'), UNISTR('Factor institucional de empleados. Peso fijo definido por requerimiento del cliente.'));

  upd_variables_factor('PROVEEDORES');
  upd_variables_factor('CLIENTES_PATRONOS');
  upd_variables_factor('EMPLEADOS');

  upd_escala('VARIABLE', 1, 1, UNISTR('Muy bajo'), UNISTR('Exposici\00F3n m\00EDnima o condici\00F3n favorable.'));
  upd_escala('VARIABLE', 2, 2, UNISTR('Bajo'), UNISTR('Exposici\00F3n baja controlable.'));
  upd_escala('VARIABLE', 3, 3, UNISTR('Medio'), UNISTR('Exposici\00F3n media que requiere seguimiento.'));
  upd_escala('VARIABLE', 4, 4, UNISTR('Alto'), UNISTR('Exposici\00F3n alta que requiere control reforzado.'));
  upd_escala('VARIABLE', 5, 5, UNISTR('Cr\00EDtico'), UNISTR('Exposici\00F3n cr\00EDtica que requiere acci\00F3n prioritaria.'));
  upd_escala('INHERENTE', 1.00, 1.80, UNISTR('Muy bajo'), UNISTR('Riesgo inherente muy bajo.'));
  upd_escala('INHERENTE', 1.81, 2.60, UNISTR('Bajo'), UNISTR('Riesgo inherente bajo.'));
  upd_escala('INHERENTE', 2.61, 3.40, UNISTR('Medio'), UNISTR('Riesgo inherente medio.'));
  upd_escala('INHERENTE', 3.41, 4.20, UNISTR('Alto'), UNISTR('Riesgo inherente alto.'));
  upd_escala('INHERENTE', 4.21, 5.00, UNISTR('Cr\00EDtico'), UNISTR('Riesgo inherente cr\00EDtico.'));
  upd_escala('RESIDUAL', 1.00, 1.80, UNISTR('Muy bajo'), UNISTR('Riesgo residual muy bajo.'));
  upd_escala('RESIDUAL', 1.81, 2.60, UNISTR('Bajo'), UNISTR('Riesgo residual bajo.'));
  upd_escala('RESIDUAL', 2.61, 3.40, UNISTR('Medio'), UNISTR('Riesgo residual medio que requiere seguimiento.'));
  upd_escala('RESIDUAL', 3.41, 4.20, UNISTR('Alto'), UNISTR('Riesgo residual alto; requiere plan de acci\00F3n.'));
  upd_escala('RESIDUAL', 4.21, 5.00, UNISTR('Cr\00EDtico'), UNISTR('Riesgo residual cr\00EDtico; requiere plan prioritario.'));
  upd_escala('CONTROL', 0, 0, UNISTR('Sin control'), UNISTR('Sin mitigaci\00F3n reconocida para el c\00E1lculo residual.'));
  upd_escala('CONTROL', 10, 10, UNISTR('D\00E9bil'), UNISTR('Mitigaci\00F3n del 10% por control con baja solidez o evidencia insuficiente.'));
  upd_escala('CONTROL', 25, 25, UNISTR('Moderado'), UNISTR('Mitigaci\00F3n del 25% por control parcialmente efectivo.'));
  upd_escala('CONTROL', 40, 40, UNISTR('Fuerte'), UNISTR('Mitigaci\00F3n del 40% por control efectivo y documentado.'));
  upd_escala('CONTROL', 55, 55, UNISTR('Muy fuerte'), UNISTR('Mitigaci\00F3n m\00E1xima sugerida del 55% por control s\00F3lido, evidenciado y oportuno.'));

END;
/

COMMIT;

PROMPT === Validacion rapida de caracteres corregidos ===
SELECT COUNT(*) AS COMENTARIOS_TABLA_DANADOS
  FROM USER_TAB_COMMENTS
 WHERE TABLE_NAME LIKE 'RL_MR_%'
   AND (ASCIISTR(COMMENTS) LIKE '%\00BF%' OR COMMENTS LIKE '%?%');

SELECT COUNT(*) AS COMENTARIOS_COLUMNA_DANADOS
  FROM USER_COL_COMMENTS
 WHERE TABLE_NAME LIKE 'RL_MR_%'
   AND (ASCIISTR(COMMENTS) LIKE '%\00BF%' OR COMMENTS LIKE '%?%');

SELECT MOD_ID, ASCIISTR(MOD_DESCRIPCION) AS MOD_DESCRIPCION_ASCIISTR
  FROM RL_MODULOS
 WHERE MOD_ID = 10 OR MOD_RUTA = '/matrices-riesgos';

SELECT ASCIISTR(MRM_NOMBRE) AS MRM_NOMBRE_ASCIISTR,
       ASCIISTR(MRM_DESCRIPCION) AS MRM_DESCRIPCION_ASCIISTR
  FROM RL_MR_MODELOS
 WHERE MRM_VERSION = '1.0'
   AND MRM_ESTADO_REGISTRO = 1;

DECLARE
  v_count NUMBER;
  v_sql   VARCHAR2(4000);
BEGIN
  FOR c IN (
    SELECT TABLE_NAME, COLUMN_NAME
      FROM USER_TAB_COLUMNS
     WHERE TABLE_NAME LIKE 'RL_MR_%'
       AND DATA_TYPE IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
     ORDER BY TABLE_NAME, COLUMN_ID
  ) LOOP
    v_sql := 'SELECT COUNT(*) FROM ' || c.TABLE_NAME ||
             ' WHERE ASCIISTR(' || c.COLUMN_NAME || ') LIKE ''%\00BF%'' OR ' || c.COLUMN_NAME || ' LIKE ''%?%''';
    EXECUTE IMMEDIATE v_sql INTO v_count;
    IF v_count > 0 THEN
      DBMS_OUTPUT.PUT_LINE(c.TABLE_NAME || '.' || c.COLUMN_NAME || ' = ' || v_count);
    END IF;
  END LOOP;
END;
/
