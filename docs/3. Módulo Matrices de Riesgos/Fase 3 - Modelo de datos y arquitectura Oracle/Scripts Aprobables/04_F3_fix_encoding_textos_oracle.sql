-- ============================================================
-- Sistema de Gestión de Riesgos LA/FT - IHSS
-- Fase 3. Modelo de datos y arquitectura Oracle
-- Script: 04_F3_fix_encoding_textos_oracle.sql
-- Objetivo: Corregir codificación de tildes y caracteres especiales en comentarios y textos descriptivos RL_MR_*.
-- Clasificación: Correctivo aprobado para cierre DBA controlado.
-- Responsable documental: Javier Mejía
-- Reglas: sin DROP, sin TRUNCATE, sin DELETE, sin renombrar tablas ni columnas.
-- Nota técnica SQLPlus: ejecutar con NLS_LANG=AMERICAN_AMERICA.WE8MSWIN1252.
-- ============================================================

SET DEFINE OFF;
SET SERVEROUTPUT ON SIZE UNLIMITED;

PROMPT === Corrección de comentarios Oracle RL_MR_* ===
COMMENT ON TABLE RL_MR_MODELOS IS 'Versiones metodológicas aprobables/aprobadas del módulo Matrices de Riesgos.';
COMMENT ON TABLE RL_MR_FACTORES IS 'Factores institucionales obligatorios: Proveedores, Clientes/Patronos y Empleados.';
COMMENT ON TABLE RL_MR_VARIABLES IS 'Variables internas por factor institucional, con ponderación interna totalizable al 100% por factor.';
COMMENT ON TABLE RL_MR_ESCALAS IS 'Rangos y niveles de calificación para variables, riesgo inherente, residual y controles.';
COMMENT ON TABLE RL_MR_CRITERIOS IS 'Criterios de calificación por variable y escala.';
COMMENT ON TABLE RL_MR_MATRICES IS 'Encabezado de matrices generadas por sujeto evaluado o matriz institucional.';
COMMENT ON TABLE RL_MR_DETALLE IS 'Detalle de variables evaluadas en una matriz, con snapshot de peso y puntaje.';
COMMENT ON TABLE RL_MR_CONTROLES IS 'Controles mitigantes asociados a la matriz y a factores cuando aplique.';
COMMENT ON TABLE RL_MR_RESULTADOS IS 'Resultados de riesgo inherente, mitigación y riesgo residual por factor e institucional.';
COMMENT ON TABLE RL_MR_PLANES_ACCION IS 'Planes de acción obligatorios o voluntarios asociados al resultado de la matriz.';
COMMENT ON TABLE RL_MR_EVIDENCIAS IS 'Metadatos de evidencias protegidas asociadas a matrices, controles o planes.';
COMMENT ON TABLE RL_MR_HISTORIAL IS 'Historial funcional del módulo; complementa RL_AUDITORIA para trazabilidad específica de matrices.';
COMMENT ON TABLE RL_MR_INTEGRACION_DNP IS 'Bandeja local para integración futura obligatoria hacia DNP, sin escritura directa hasta aprobación técnica.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ID IS 'Identificador único del modelo metodológico de matrices de riesgos.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_NOMBRE IS 'Nombre del modelo metodológico.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_VERSION IS 'Versión funcional y técnica del modelo metodológico.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_DESCRIPCION IS 'Descripción general del alcance metodológico del modelo.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ESTADO IS 'Estado del modelo: BORRADOR, EN_REVISION, APROBADO, CERRADO o INACTIVO.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_VIGENCIA IS 'Fecha desde la cual la versión metodológica puede aplicarse.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_CIERRE IS 'Fecha de cierre o retiro de la versión metodológica.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_APROBADO_POR IS 'Usuario responsable de aprobar la versión metodológica.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_APROBACION IS 'Fecha de aprobación de la versión metodológica.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_MOTIVO_ESTADO IS 'Motivo funcional del cambio de estado del modelo.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_USR_CREACION_ID IS 'Usuario que registró el modelo.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_CREACION IS 'Fecha de creación del registro.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_USR_MODIF_ID IS 'Último usuario que modificó el registro.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_MODIF IS 'Fecha de última modificación del registro.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminación lógica.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ID IS 'Identificador único del factor institucional.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_MODELO_ID IS 'Modelo metodológico al que pertenece el factor.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_CODIGO IS 'Código funcional del factor institucional.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_NOMBRE IS 'Nombre del factor institucional.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_DESCRIPCION IS 'Descripción funcional del factor institucional.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_PESO_INSTITUCIONAL IS 'Peso institucional fijo del factor dentro del riesgo total.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ORDEN IS 'Orden de presentación y cálculo del factor.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminación lógica.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando el factor se inactive lógicamente.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_USR_CREACION_ID IS 'Usuario que registró el factor.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_FECHA_CREACION IS 'Fecha de creación del registro.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_USR_MODIF_ID IS 'Último usuario que modificó el registro.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_FECHA_MODIF IS 'Fecha de última modificación del registro.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ID IS 'Identificador único de la variable interna.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FACTOR_ID IS 'Factor institucional al que pertenece la variable.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_CODIGO IS 'Código funcional de la variable dentro del factor.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_NOMBRE IS 'Nombre de la variable de riesgo.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_DESCRIPCION IS 'Descripción funcional de la variable de riesgo.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_PESO_INTERNO IS 'Peso interno de la variable dentro del factor; debe totalizar 100% por factor.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_TIPO_DATO IS 'Tipo de dato esperado para capturar o calcular la variable.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FUENTE_DATO IS 'Origen funcional del dato: captura, consulta o integración autorizada.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_OBLIGATORIA IS 'Indica si la variable es obligatoria para completar la matriz.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ORDEN IS 'Orden de presentación y cálculo de la variable.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminación lógica.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando la variable se inactive lógicamente.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_USR_CREACION_ID IS 'Usuario que registró la variable.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FECHA_CREACION IS 'Fecha de creación del registro.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_USR_MODIF_ID IS 'Último usuario que modificó el registro.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FECHA_MODIF IS 'Fecha de última modificación del registro.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ID IS 'Identificador único de la escala metodológica.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_MODELO_ID IS 'Modelo metodológico al que pertenece la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_TIPO IS 'Tipo de escala: VARIABLE, INHERENTE, RESIDUAL o CONTROL.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_VALOR_MIN IS 'Valor mínimo del rango de la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_VALOR_MAX IS 'Valor máximo del rango de la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_NIVEL IS 'Nivel funcional asignado al rango de la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_COLOR_HEX IS 'Color sugerido para representar visualmente el nivel.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_DESCRIPCION IS 'Descripción funcional del rango o nivel de escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ORDEN IS 'Orden de presentación del nivel dentro de la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminación lógica.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando la escala se inactive lógicamente.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ID IS 'Identificador único del criterio de calificación.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VARIABLE_ID IS 'Variable de riesgo a la que pertenece el criterio.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ESCALA_ID IS 'Escala relacionada con el criterio cuando aplique.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VALOR_DESDE IS 'Valor inicial del rango del criterio.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VALOR_HASTA IS 'Valor final del rango del criterio.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_PUNTAJE IS 'Puntaje asignado cuando el criterio se cumple.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_DESCRIPCION IS 'Descripción funcional del criterio de calificación.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminación lógica.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando el criterio se inactive lógicamente.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ID IS 'Identificador único de la matriz generada.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_MODELO_ID IS 'Modelo metodológico usado por la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SUJETO_TIPO IS 'Tipo de sujeto evaluado por la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SUJETO_ID_EXT IS 'Identificador externo del sujeto evaluado cuando aplique.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_DOCUMENTO IS 'Documento, código o número de referencia del sujeto evaluado.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_NOMBRE_SUJETO IS 'Nombre o descripción del sujeto evaluado.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ORIGEN_DATOS IS 'Origen de datos usado para construir la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ESTADO IS 'Estado funcional de la matriz: BORRADOR, CALCULADA, EN_REVISION, OBSERVADA, APROBADA, CERRADA o INACTIVA.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_EVALUACION IS 'Fecha en que se registra o calcula la evaluación.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_CIERRE IS 'Fecha de cierre formal de la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_CERRADO_POR IS 'Usuario que cerró formalmente la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_MOTIVO_ESTADO IS 'Motivo funcional del cambio de estado de la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SNAPSHOT_METODO IS 'Snapshot de la metodología usada para proteger matrices cerradas.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_USR_CREACION_ID IS 'Usuario que registró la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_CREACION IS 'Fecha de creación del registro.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_USR_MODIF_ID IS 'Último usuario que modificó el registro.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_MODIF IS 'Fecha de última modificación del registro.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminación lógica.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_ID IS 'Identificador único del detalle evaluado.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_MATRIZ_ID IS 'Matriz a la que pertenece el detalle.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_VARIABLE_ID IS 'Variable evaluada dentro de la matriz.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_VALOR_CAPTURADO IS 'Valor capturado, consultado o calculado para la variable.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PUNTAJE IS 'Puntaje asignado a la variable.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PESO_SNAPSHOT IS 'Peso de la variable al momento del cálculo.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PUNTAJE_PONDERADO IS 'Resultado ponderado de la variable.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_JUSTIFICACION IS 'Justificación funcional del valor o puntaje asignado.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_FUENTE_DATO IS 'Fuente del dato usado para evaluar la variable.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_SNAPSHOT_VARIABLE IS 'Snapshot de la variable y criterio usados en el cálculo.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_USR_CREACION_ID IS 'Usuario que registró el detalle.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_FECHA_CREACION IS 'Fecha de creación del detalle.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_ID IS 'Identificador único del control mitigante.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_MATRIZ_ID IS 'Matriz a la que pertenece el control mitigante.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FACTOR_ID IS 'Factor institucional al que se asocia el control cuando aplique.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_NOMBRE IS 'Nombre del control mitigante.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_DESCRIPCION IS 'Descripción funcional del control mitigante.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_PERIODICIDAD IS 'Periodicidad con la que opera o se revisa el control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_OPORTUNIDAD IS 'Oportunidad del control respecto al evento de riesgo.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_AUTOMATIZACION IS 'Nivel de automatización del control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_PROCEDIMIENTOS IS 'Nivel de formalización de procedimientos del control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_CALIDAD IS 'Calidad general del control según metodología aprobada.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_EFECTIVIDAD_PCT IS 'Porcentaje de efectividad o mitigación calculada para el control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_RESPONSABLE IS 'Responsable funcional del control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_ESTADO IS 'Estado funcional del control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando el control se inactive lógicamente.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_EVIDENCIA_OBL IS 'Indica si el control requiere evidencia documental obligatoria.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_USR_CREACION_ID IS 'Usuario que registró el control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FECHA_CREACION IS 'Fecha de creación del registro.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_USR_MODIF_ID IS 'Último usuario que modificó el registro.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FECHA_MODIF IS 'Fecha de última modificación del registro.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_ID IS 'Identificador único del resultado de cálculo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MATRIZ_ID IS 'Matriz a la que pertenece el resultado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_FACTOR_ID IS 'Factor institucional asociado cuando el resultado es por factor.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_TIPO_RESULTADO IS 'Tipo de resultado: por factor o institucional.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_VERSION_CALCULO IS 'Versión del algoritmo o regla de cálculo usada.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_ES_VIGENTE IS 'Indica si el resultado es el vigente para la matriz, factor y tipo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_PUNTAJE_INHERENTE IS 'Puntaje de riesgo inherente calculado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_NIVEL_INHERENTE IS 'Nivel de riesgo inherente calculado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MITIGACION_PCT IS 'Porcentaje de mitigación aplicado por controles.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_PUNTAJE_RESIDUAL IS 'Puntaje de riesgo residual calculado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_NIVEL_RESIDUAL IS 'Nivel de riesgo residual calculado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_REQUIERE_PLAN IS 'Indica si el resultado exige plan de acción.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MOTIVO_RECALCULO IS 'Motivo funcional cuando se genera un recálculo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_RESULTADO_ANTERIOR_ID IS 'Resultado anterior relacionado con el recálculo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_SNAPSHOT_CALCULO IS 'Snapshot de entradas, reglas y salida del cálculo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_FECHA_CALCULO IS 'Fecha en que se generó el resultado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_USR_CALCULO_ID IS 'Usuario que ejecutó o solicitó el cálculo.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ID IS 'Identificador único del plan de acción.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MATRIZ_ID IS 'Matriz asociada al plan de acción.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_RESULTADO_ID IS 'Resultado que originó el plan de acción cuando aplique.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ACTIVIDAD IS 'Actividad o acción correctiva definida.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_RESPONSABLE IS 'Responsable funcional de ejecutar la actividad.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_PERIODICIDAD IS 'Periodicidad de ejecución o seguimiento del plan.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_INICIO IS 'Fecha planificada de inicio.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_FIN IS 'Fecha planificada o real de finalización.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MEDIO_PRUEBA IS 'Medio de prueba requerido para evidenciar cumplimiento.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_OBSERVACIONES IS 'Observaciones funcionales del plan de acción.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ESTADO IS 'Estado funcional del plan de acción.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MOTIVO_CIERRE IS 'Motivo o justificación del cierre del plan.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_USR_CREACION_ID IS 'Usuario que registró el plan de acción.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_CREACION IS 'Fecha de creación del registro.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_USR_CIERRE_ID IS 'Usuario que cerró el plan de acción.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_CIERRE IS 'Fecha de cierre del plan de acción.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_ID IS 'Identificador único de la evidencia documental.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_MATRIZ_ID IS 'Matriz asociada a la evidencia.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_CONTROL_ID IS 'Control asociado a la evidencia cuando aplique.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_PLAN_ID IS 'Plan de acción asociado a la evidencia cuando aplique.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_NOMBRE_ORIGINAL IS 'Nombre original del archivo cargado por el usuario.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_NOMBRE_FISICO IS 'Nombre físico seguro asignado al archivo.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_TIPO_MIME IS 'Tipo MIME identificado para el archivo.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_EXTENSION IS 'Extensión validada del archivo.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_TAMANO_BYTES IS 'Tamaño del archivo en bytes.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_RUTA_FISICA IS 'Ruta protegida de almacenamiento físico.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_HASH_SHA256 IS 'Huella SHA-256 del archivo cuando aplique.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminación lógica.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando la evidencia se inactive lógicamente.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_USR_CREACION_ID IS 'Usuario que cargó o registró la evidencia.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_FECHA_CREACION IS 'Fecha de creación del registro.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_USR_INACTIVO_ID IS 'Usuario que realizó la eliminación lógica.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_FECHA_INACTIVO IS 'Fecha de eliminación lógica de la evidencia.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ID IS 'Identificador único del evento histórico funcional.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_MATRIZ_ID IS 'Matriz asociada al evento histórico cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_TABLA IS 'Tabla funcional sobre la que ocurrió el evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_REGISTRO_ID IS 'Identificador del registro afectado por el evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ACCION IS 'Acción funcional registrada.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ESTADO_ANTERIOR IS 'Estado anterior del registro cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ESTADO_NUEVO IS 'Estado nuevo del registro cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_MOTIVO IS 'Motivo funcional del evento histórico.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_DATOS_ANT IS 'Snapshot de datos anteriores cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_DATOS_NVO IS 'Snapshot de datos nuevos cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_USR_ID IS 'Usuario responsable del evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_USR_EMAIL IS 'Correo del usuario responsable del evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_IP IS 'Dirección IP registrada para el evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_FECHA IS 'Fecha y hora del evento histórico.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_ID IS 'Identificador único del registro de integración futura con DNP.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_MATRIZ_ID IS 'Matriz asociada a la calificación que se integrará.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_NUMERO_PATRONO IS 'Número de patrono relacionado cuando aplique.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_CALIFICACION IS 'Calificación de riesgo preparada para integración futura.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_PUNTAJE_RESIDUAL IS 'Puntaje residual asociado a la calificación.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_ESTADO_ENVIO IS 'Estado de la integración futura: PENDIENTE, ENVIADO, ERROR o ANULADO.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_INTENTOS IS 'Cantidad de intentos de envío registrados.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_RESPUESTA IS 'Respuesta técnica recibida de la integración cuando aplique.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_FECHA_CREACION IS 'Fecha de creación del registro de integración.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_FECHA_ENVIO IS 'Fecha de envío hacia la integración cuando aplique.';

PROMPT === Corrección de textos descriptivos del módulo Matrices de Riesgos ===
UPDATE RL_MODULOS
   SET MOD_NOMBRE = 'Matrices de Riesgos',
       MOD_DESCRIPCION = 'Módulo para evaluación, cálculo, seguimiento y reportería de matrices de riesgos LA/FT.',
       MOD_RUTA = '/matrices-riesgos',
       MOD_ICONO = 'chart-column',
       MOD_SECCION = 'Riesgos LA/FT',
       MOD_ACTIVO = 1
 WHERE MOD_ID = 10
    OR MOD_RUTA = '/matrices-riesgos';

PROMPT === Corrección de textos semilla RL_MR_* ===
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
    upd_variable(p_factor_codigo, 'V01', 'Perfil del sujeto evaluado', 'Condiciones generales del sujeto evaluado según factor institucional.');
    upd_variable(p_factor_codigo, 'V02', 'Actividad, rubro o función', 'Actividad económica, rubro, función o naturaleza operativa relacionada con el factor.');
    upd_variable(p_factor_codigo, 'V03', 'Ubicación geográfica', 'Exposición por ubicación, zona, municipio, país o jurisdicción aplicable.');
    upd_variable(p_factor_codigo, 'V04', 'Antecedentes y coincidencias', 'Historial, coincidencias, alertas, sanciones, observaciones o eventos relevantes.');
    upd_variable(p_factor_codigo, 'V05', 'Comportamiento transaccional u operativo', 'Comportamiento, volumen, recurrencia, variación o señales operativas relevantes.');
    upd_variable(p_factor_codigo, 'V06', 'Canal, producto o relación institucional', 'Canal de vinculación, relación institucional, servicio, proceso o modalidad de interacción.');
    upd_variable(p_factor_codigo, 'V07', 'Control interno y evidencia disponible', 'Nivel de documentación, soporte, trazabilidad y evidencia disponible para sustentar la evaluación.');
  END;
BEGIN
  SELECT MIN(MRM_ID)
    INTO v_modelo_id
    FROM RL_MR_MODELOS
   WHERE MRM_VERSION = '1.0'
     AND MRM_ESTADO = 'APROBADO'
     AND MRM_ESTADO_REGISTRO = 1;

  IF v_modelo_id IS NULL THEN
    RAISE_APPLICATION_ERROR(-20140, 'No se encontró el modelo base aprobado de Matrices de Riesgos para corregir textos.');
  END IF;

  UPDATE RL_MR_MODELOS
     SET MRM_NOMBRE = 'Metodología base LA/FT IHSS',
         MRM_DESCRIPCION = 'Modelo inicial aprobado metodológicamente en Fase 2 para factores institucionales, variables internas, escalas base y rangos de riesgo.',
         MRM_MOTIVO_ESTADO = 'Metodología base alineada con Fase 2 aprobada.',
         MRM_USR_MODIF_ID = 1,
         MRM_FECHA_MODIF = SYSDATE
   WHERE MRM_ID = v_modelo_id;

  upd_factor('PROVEEDORES', 'Proveedores', 'Factor institucional de proveedores. Peso fijo definido por requerimiento del cliente.');
  upd_factor('CLIENTES_PATRONOS', 'Clientes/Patronos', 'Factor institucional de clientes o patronos. Peso fijo definido por requerimiento del cliente.');
  upd_factor('EMPLEADOS', 'Empleados', 'Factor institucional de empleados. Peso fijo definido por requerimiento del cliente.');

  upd_variables_factor('PROVEEDORES');
  upd_variables_factor('CLIENTES_PATRONOS');
  upd_variables_factor('EMPLEADOS');

  upd_escala('VARIABLE', 1, 1, 'Muy bajo', 'Exposición mínima o condición favorable.');
  upd_escala('VARIABLE', 2, 2, 'Bajo', 'Exposición baja controlable.');
  upd_escala('VARIABLE', 3, 3, 'Medio', 'Exposición media que requiere seguimiento.');
  upd_escala('VARIABLE', 4, 4, 'Alto', 'Exposición alta que requiere control reforzado.');
  upd_escala('VARIABLE', 5, 5, 'Crítico', 'Exposición crítica que requiere acción prioritaria.');
  upd_escala('INHERENTE', 1.00, 1.80, 'Muy bajo', 'Riesgo inherente muy bajo.');
  upd_escala('INHERENTE', 1.81, 2.60, 'Bajo', 'Riesgo inherente bajo.');
  upd_escala('INHERENTE', 2.61, 3.40, 'Medio', 'Riesgo inherente medio.');
  upd_escala('INHERENTE', 3.41, 4.20, 'Alto', 'Riesgo inherente alto.');
  upd_escala('INHERENTE', 4.21, 5.00, 'Crítico', 'Riesgo inherente crítico.');
  upd_escala('RESIDUAL', 1.00, 1.80, 'Muy bajo', 'Riesgo residual muy bajo.');
  upd_escala('RESIDUAL', 1.81, 2.60, 'Bajo', 'Riesgo residual bajo.');
  upd_escala('RESIDUAL', 2.61, 3.40, 'Medio', 'Riesgo residual medio que requiere seguimiento.');
  upd_escala('RESIDUAL', 3.41, 4.20, 'Alto', 'Riesgo residual alto; requiere plan de acción.');
  upd_escala('RESIDUAL', 4.21, 5.00, 'Crítico', 'Riesgo residual crítico; requiere plan prioritario.');
  upd_escala('CONTROL', 0, 0, 'Sin control', 'Sin mitigación reconocida para el cálculo residual.');
  upd_escala('CONTROL', 10, 10, 'Débil', 'Mitigación del 10% por control con baja solidez o evidencia insuficiente.');
  upd_escala('CONTROL', 25, 25, 'Moderado', 'Mitigación del 25% por control parcialmente efectivo.');
  upd_escala('CONTROL', 40, 40, 'Fuerte', 'Mitigación del 40% por control efectivo y documentado.');
  upd_escala('CONTROL', 55, 55, 'Muy fuerte', 'Mitigación máxima sugerida del 55% por control sólido, evidenciado y oportuno.');

END;
/

COMMIT;

PROMPT === Validación rápida de caracteres corregidos ===
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

SELECT TABLE_NAME, ASCIISTR(COMMENTS) AS COMMENTS_ASCIISTR
  FROM USER_TAB_COMMENTS
 WHERE TABLE_NAME IN ('RL_MR_MODELOS','RL_MR_HISTORIAL')
 ORDER BY TABLE_NAME;
