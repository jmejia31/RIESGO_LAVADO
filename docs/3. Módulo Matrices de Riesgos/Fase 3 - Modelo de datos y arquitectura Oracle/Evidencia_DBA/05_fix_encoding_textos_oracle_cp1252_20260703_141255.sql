-- ============================================================
-- Sistema de Gestión de Riesgos LA/FT - IHSS
-- Fase 3. Modelo de datos y arquitectura Oracle
-- Script: 04_F3_fix_encoding_textos_oracle.sql
-- Objetivo: Corregir codificación de tildes y caracteres especiales en comentarios y textos descriptivos RL_MR_*.
-- Clasificaci?n: Correctivo aprobado para cierre DBA controlado.
-- Responsable documental: Javier Mejía
-- Reglas: sin DROP, sin TRUNCATE, sin DELETE, sin renombrar tablas ni columnas.
-- Nota t?cnica SQLPlus: ejecutar con NLS_LANG=AMERICAN_AMERICA.WE8MSWIN1252.
-- ============================================================

SET DEFINE OFF;
SET SERVEROUTPUT ON SIZE UNLIMITED;

PROMPT === Corrección de comentarios Oracle RL_MR_* ===
COMMENT ON TABLE RL_MR_MODELOS IS 'Versiones metodol�gicas aprobables/aprobadas del m�dulo Matrices de Riesgos.';
COMMENT ON TABLE RL_MR_FACTORES IS 'Factores institucionales obligatorios: Proveedores, Clientes/Patronos y Empleados.';
COMMENT ON TABLE RL_MR_VARIABLES IS 'Variables internas por factor institucional, con ponderaci�n interna totalizable al 100% por factor.';
COMMENT ON TABLE RL_MR_ESCALAS IS 'Rangos y niveles de calificaci�n para variables, riesgo inherente, residual y controles.';
COMMENT ON TABLE RL_MR_CRITERIOS IS 'Criterios de calificaci�n por variable y escala.';
COMMENT ON TABLE RL_MR_MATRICES IS 'Encabezado de matrices generadas por sujeto evaluado o matriz institucional.';
COMMENT ON TABLE RL_MR_DETALLE IS 'Detalle de variables evaluadas en una matriz, con snapshot de peso y puntaje.';
COMMENT ON TABLE RL_MR_CONTROLES IS 'Controles mitigantes asociados a la matriz y a factores cuando aplique.';
COMMENT ON TABLE RL_MR_RESULTADOS IS 'Resultados de riesgo inherente, mitigaci�n y riesgo residual por factor e institucional.';
COMMENT ON TABLE RL_MR_PLANES_ACCION IS 'Planes de acci�n obligatorios o voluntarios asociados al resultado de la matriz.';
COMMENT ON TABLE RL_MR_EVIDENCIAS IS 'Metadatos de evidencias protegidas asociadas a matrices, controles o planes.';
COMMENT ON TABLE RL_MR_HISTORIAL IS 'Historial funcional del m�dulo; complementa RL_AUDITORIA para trazabilidad espec�fica de matrices.';
COMMENT ON TABLE RL_MR_INTEGRACION_DNP IS 'Bandeja local para integraci�n futura obligatoria hacia DNP, sin escritura directa hasta aprobaci�n t�cnica.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ID IS 'Identificador �nico del modelo metodol�gico de matrices de riesgos.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_NOMBRE IS 'Nombre del modelo metodol�gico.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_VERSION IS 'Versi�n funcional y t�cnica del modelo metodol�gico.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_DESCRIPCION IS 'Descripci�n general del alcance metodol�gico del modelo.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ESTADO IS 'Estado del modelo: BORRADOR, EN_REVISION, APROBADO, CERRADO o INACTIVO.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_VIGENCIA IS 'Fecha desde la cual la versi�n metodol�gica puede aplicarse.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_CIERRE IS 'Fecha de cierre o retiro de la versi�n metodol�gica.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_APROBADO_POR IS 'Usuario responsable de aprobar la versi�n metodol�gica.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_APROBACION IS 'Fecha de aprobaci�n de la versi�n metodol�gica.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_MOTIVO_ESTADO IS 'Motivo funcional del cambio de estado del modelo.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_USR_CREACION_ID IS 'Usuario que registr� el modelo.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_CREACION IS 'Fecha de creaci�n del registro.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_USR_MODIF_ID IS '�ltimo usuario que modific� el registro.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_MODIF IS 'Fecha de �ltima modificaci�n del registro.';
COMMENT ON COLUMN RL_MR_MODELOS.MRM_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminaci�n l�gica.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ID IS 'Identificador �nico del factor institucional.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_MODELO_ID IS 'Modelo metodol�gico al que pertenece el factor.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_CODIGO IS 'C�digo funcional del factor institucional.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_NOMBRE IS 'Nombre del factor institucional.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_DESCRIPCION IS 'Descripci�n funcional del factor institucional.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_PESO_INSTITUCIONAL IS 'Peso institucional fijo del factor dentro del riesgo total.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ORDEN IS 'Orden de presentaci�n y c�lculo del factor.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminaci�n l�gica.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando el factor se inactive l�gicamente.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_USR_CREACION_ID IS 'Usuario que registr� el factor.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_FECHA_CREACION IS 'Fecha de creaci�n del registro.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_USR_MODIF_ID IS '�ltimo usuario que modific� el registro.';
COMMENT ON COLUMN RL_MR_FACTORES.MRF_FECHA_MODIF IS 'Fecha de �ltima modificaci�n del registro.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ID IS 'Identificador �nico de la variable interna.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FACTOR_ID IS 'Factor institucional al que pertenece la variable.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_CODIGO IS 'C�digo funcional de la variable dentro del factor.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_NOMBRE IS 'Nombre de la variable de riesgo.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_DESCRIPCION IS 'Descripci�n funcional de la variable de riesgo.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_PESO_INTERNO IS 'Peso interno de la variable dentro del factor; debe totalizar 100% por factor.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_TIPO_DATO IS 'Tipo de dato esperado para capturar o calcular la variable.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FUENTE_DATO IS 'Origen funcional del dato: captura, consulta o integraci�n autorizada.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_OBLIGATORIA IS 'Indica si la variable es obligatoria para completar la matriz.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ORDEN IS 'Orden de presentaci�n y c�lculo de la variable.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminaci�n l�gica.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando la variable se inactive l�gicamente.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_USR_CREACION_ID IS 'Usuario que registr� la variable.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FECHA_CREACION IS 'Fecha de creaci�n del registro.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_USR_MODIF_ID IS '�ltimo usuario que modific� el registro.';
COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FECHA_MODIF IS 'Fecha de �ltima modificaci�n del registro.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ID IS 'Identificador �nico de la escala metodol�gica.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_MODELO_ID IS 'Modelo metodol�gico al que pertenece la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_TIPO IS 'Tipo de escala: VARIABLE, INHERENTE, RESIDUAL o CONTROL.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_VALOR_MIN IS 'Valor m�nimo del rango de la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_VALOR_MAX IS 'Valor m�ximo del rango de la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_NIVEL IS 'Nivel funcional asignado al rango de la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_COLOR_HEX IS 'Color sugerido para representar visualmente el nivel.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_DESCRIPCION IS 'Descripci�n funcional del rango o nivel de escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ORDEN IS 'Orden de presentaci�n del nivel dentro de la escala.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminaci�n l�gica.';
COMMENT ON COLUMN RL_MR_ESCALAS.MRE_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando la escala se inactive l�gicamente.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ID IS 'Identificador �nico del criterio de calificaci�n.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VARIABLE_ID IS 'Variable de riesgo a la que pertenece el criterio.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ESCALA_ID IS 'Escala relacionada con el criterio cuando aplique.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VALOR_DESDE IS 'Valor inicial del rango del criterio.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VALOR_HASTA IS 'Valor final del rango del criterio.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_PUNTAJE IS 'Puntaje asignado cuando el criterio se cumple.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_DESCRIPCION IS 'Descripci�n funcional del criterio de calificaci�n.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminaci�n l�gica.';
COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando el criterio se inactive l�gicamente.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ID IS 'Identificador �nico de la matriz generada.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_MODELO_ID IS 'Modelo metodol�gico usado por la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SUJETO_TIPO IS 'Tipo de sujeto evaluado por la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SUJETO_ID_EXT IS 'Identificador externo del sujeto evaluado cuando aplique.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_DOCUMENTO IS 'Documento, c�digo o n�mero de referencia del sujeto evaluado.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_NOMBRE_SUJETO IS 'Nombre o descripci�n del sujeto evaluado.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ORIGEN_DATOS IS 'Origen de datos usado para construir la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ESTADO IS 'Estado funcional de la matriz: BORRADOR, CALCULADA, EN_REVISION, OBSERVADA, APROBADA, CERRADA o INACTIVA.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_EVALUACION IS 'Fecha en que se registra o calcula la evaluaci�n.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_CIERRE IS 'Fecha de cierre formal de la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_CERRADO_POR IS 'Usuario que cerr� formalmente la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_MOTIVO_ESTADO IS 'Motivo funcional del cambio de estado de la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SNAPSHOT_METODO IS 'Snapshot de la metodolog�a usada para proteger matrices cerradas.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_USR_CREACION_ID IS 'Usuario que registr� la matriz.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_CREACION IS 'Fecha de creaci�n del registro.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_USR_MODIF_ID IS '�ltimo usuario que modific� el registro.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_MODIF IS 'Fecha de �ltima modificaci�n del registro.';
COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminaci�n l�gica.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_ID IS 'Identificador �nico del detalle evaluado.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_MATRIZ_ID IS 'Matriz a la que pertenece el detalle.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_VARIABLE_ID IS 'Variable evaluada dentro de la matriz.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_VALOR_CAPTURADO IS 'Valor capturado, consultado o calculado para la variable.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PUNTAJE IS 'Puntaje asignado a la variable.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PESO_SNAPSHOT IS 'Peso de la variable al momento del c�lculo.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_PUNTAJE_PONDERADO IS 'Resultado ponderado de la variable.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_JUSTIFICACION IS 'Justificaci�n funcional del valor o puntaje asignado.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_FUENTE_DATO IS 'Fuente del dato usado para evaluar la variable.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_SNAPSHOT_VARIABLE IS 'Snapshot de la variable y criterio usados en el c�lculo.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_USR_CREACION_ID IS 'Usuario que registr� el detalle.';
COMMENT ON COLUMN RL_MR_DETALLE.MRD_FECHA_CREACION IS 'Fecha de creaci�n del detalle.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_ID IS 'Identificador �nico del control mitigante.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_MATRIZ_ID IS 'Matriz a la que pertenece el control mitigante.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FACTOR_ID IS 'Factor institucional al que se asocia el control cuando aplique.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_NOMBRE IS 'Nombre del control mitigante.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_DESCRIPCION IS 'Descripci�n funcional del control mitigante.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_PERIODICIDAD IS 'Periodicidad con la que opera o se revisa el control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_OPORTUNIDAD IS 'Oportunidad del control respecto al evento de riesgo.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_AUTOMATIZACION IS 'Nivel de automatizaci�n del control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_PROCEDIMIENTOS IS 'Nivel de formalizaci�n de procedimientos del control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_CALIDAD IS 'Calidad general del control seg�n metodolog�a aprobada.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_EFECTIVIDAD_PCT IS 'Porcentaje de efectividad o mitigaci�n calculada para el control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_RESPONSABLE IS 'Responsable funcional del control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_ESTADO IS 'Estado funcional del control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando el control se inactive l�gicamente.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_EVIDENCIA_OBL IS 'Indica si el control requiere evidencia documental obligatoria.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_USR_CREACION_ID IS 'Usuario que registr� el control.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FECHA_CREACION IS 'Fecha de creaci�n del registro.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_USR_MODIF_ID IS '�ltimo usuario que modific� el registro.';
COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FECHA_MODIF IS 'Fecha de �ltima modificaci�n del registro.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_ID IS 'Identificador �nico del resultado de c�lculo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MATRIZ_ID IS 'Matriz a la que pertenece el resultado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_FACTOR_ID IS 'Factor institucional asociado cuando el resultado es por factor.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_TIPO_RESULTADO IS 'Tipo de resultado: por factor o institucional.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_VERSION_CALCULO IS 'Versi�n del algoritmo o regla de c�lculo usada.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_ES_VIGENTE IS 'Indica si el resultado es el vigente para la matriz, factor y tipo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_PUNTAJE_INHERENTE IS 'Puntaje de riesgo inherente calculado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_NIVEL_INHERENTE IS 'Nivel de riesgo inherente calculado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MITIGACION_PCT IS 'Porcentaje de mitigaci�n aplicado por controles.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_PUNTAJE_RESIDUAL IS 'Puntaje de riesgo residual calculado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_NIVEL_RESIDUAL IS 'Nivel de riesgo residual calculado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_REQUIERE_PLAN IS 'Indica si el resultado exige plan de acci�n.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MOTIVO_RECALCULO IS 'Motivo funcional cuando se genera un rec�lculo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_RESULTADO_ANTERIOR_ID IS 'Resultado anterior relacionado con el rec�lculo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_SNAPSHOT_CALCULO IS 'Snapshot de entradas, reglas y salida del c�lculo.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_FECHA_CALCULO IS 'Fecha en que se gener� el resultado.';
COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_USR_CALCULO_ID IS 'Usuario que ejecut� o solicit� el c�lculo.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ID IS 'Identificador �nico del plan de acci�n.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MATRIZ_ID IS 'Matriz asociada al plan de acci�n.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_RESULTADO_ID IS 'Resultado que origin� el plan de acci�n cuando aplique.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ACTIVIDAD IS 'Actividad o acci�n correctiva definida.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_RESPONSABLE IS 'Responsable funcional de ejecutar la actividad.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_PERIODICIDAD IS 'Periodicidad de ejecuci�n o seguimiento del plan.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_INICIO IS 'Fecha planificada de inicio.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_FIN IS 'Fecha planificada o real de finalizaci�n.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MEDIO_PRUEBA IS 'Medio de prueba requerido para evidenciar cumplimiento.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_OBSERVACIONES IS 'Observaciones funcionales del plan de acci�n.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ESTADO IS 'Estado funcional del plan de acci�n.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MOTIVO_CIERRE IS 'Motivo o justificaci�n del cierre del plan.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_USR_CREACION_ID IS 'Usuario que registr� el plan de acci�n.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_CREACION IS 'Fecha de creaci�n del registro.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_USR_CIERRE_ID IS 'Usuario que cerr� el plan de acci�n.';
COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_CIERRE IS 'Fecha de cierre del plan de acci�n.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_ID IS 'Identificador �nico de la evidencia documental.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_MATRIZ_ID IS 'Matriz asociada a la evidencia.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_CONTROL_ID IS 'Control asociado a la evidencia cuando aplique.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_PLAN_ID IS 'Plan de acci�n asociado a la evidencia cuando aplique.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_NOMBRE_ORIGINAL IS 'Nombre original del archivo cargado por el usuario.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_NOMBRE_FISICO IS 'Nombre f�sico seguro asignado al archivo.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_TIPO_MIME IS 'Tipo MIME identificado para el archivo.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_EXTENSION IS 'Extensi�n validada del archivo.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_TAMANO_BYTES IS 'Tama�o del archivo en bytes.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_RUTA_FISICA IS 'Ruta protegida de almacenamiento f�sico.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_HASH_SHA256 IS 'Huella SHA-256 del archivo cuando aplique.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_ESTADO_REGISTRO IS 'Indicador de registro activo o inactivo para eliminaci�n l�gica.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_MOTIVO_INACTIVO IS 'Motivo obligatorio cuando la evidencia se inactive l�gicamente.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_USR_CREACION_ID IS 'Usuario que carg� o registr� la evidencia.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_FECHA_CREACION IS 'Fecha de creaci�n del registro.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_USR_INACTIVO_ID IS 'Usuario que realiz� la eliminaci�n l�gica.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_FECHA_INACTIVO IS 'Fecha de eliminaci�n l�gica de la evidencia.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ID IS 'Identificador �nico del evento hist�rico funcional.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_MATRIZ_ID IS 'Matriz asociada al evento hist�rico cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_TABLA IS 'Tabla funcional sobre la que ocurri� el evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_REGISTRO_ID IS 'Identificador del registro afectado por el evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ACCION IS 'Acci�n funcional registrada.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ESTADO_ANTERIOR IS 'Estado anterior del registro cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ESTADO_NUEVO IS 'Estado nuevo del registro cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_MOTIVO IS 'Motivo funcional del evento hist�rico.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_DATOS_ANT IS 'Snapshot de datos anteriores cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_DATOS_NVO IS 'Snapshot de datos nuevos cuando aplique.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_USR_ID IS 'Usuario responsable del evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_USR_EMAIL IS 'Correo del usuario responsable del evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_IP IS 'Direcci�n IP registrada para el evento.';
COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_FECHA IS 'Fecha y hora del evento hist�rico.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_ID IS 'Identificador �nico del registro de integraci�n futura con DNP.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_MATRIZ_ID IS 'Matriz asociada a la calificaci�n que se integrar�.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_NUMERO_PATRONO IS 'N�mero de patrono relacionado cuando aplique.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_CALIFICACION IS 'Calificaci�n de riesgo preparada para integraci�n futura.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_PUNTAJE_RESIDUAL IS 'Puntaje residual asociado a la calificaci�n.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_ESTADO_ENVIO IS 'Estado de la integraci�n futura: PENDIENTE, ENVIADO, ERROR o ANULADO.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_INTENTOS IS 'Cantidad de intentos de env�o registrados.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_RESPUESTA IS 'Respuesta t�cnica recibida de la integraci�n cuando aplique.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_FECHA_CREACION IS 'Fecha de creaci�n del registro de integraci�n.';
COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_FECHA_ENVIO IS 'Fecha de env�o hacia la integraci�n cuando aplique.';

PROMPT === Corrección de textos descriptivos del módulo Matrices de Riesgos ===
UPDATE RL_MODULOS
   SET MOD_NOMBRE = 'Matrices de Riesgos',
       MOD_DESCRIPCION = 'M�dulo para evaluaci�n, c�lculo, seguimiento y reporter�a de matrices de riesgos LA/FT.',
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
    upd_variable(p_factor_codigo, 'V01', 'Perfil del sujeto evaluado', 'Condiciones generales del sujeto evaluado seg�n factor institucional.');
    upd_variable(p_factor_codigo, 'V02', 'Actividad, rubro o funci�n', 'Actividad econ�mica, rubro, funci�n o naturaleza operativa relacionada con el factor.');
    upd_variable(p_factor_codigo, 'V03', 'Ubicaci�n geogr�fica', 'Exposici�n por ubicaci�n, zona, municipio, pa�s o jurisdicci�n aplicable.');
    upd_variable(p_factor_codigo, 'V04', 'Antecedentes y coincidencias', 'Historial, coincidencias, alertas, sanciones, observaciones o eventos relevantes.');
    upd_variable(p_factor_codigo, 'V05', 'Comportamiento transaccional u operativo', 'Comportamiento, volumen, recurrencia, variaci�n o se�ales operativas relevantes.');
    upd_variable(p_factor_codigo, 'V06', 'Canal, producto o relaci�n institucional', 'Canal de vinculaci�n, relaci�n institucional, servicio, proceso o modalidad de interacci�n.');
    upd_variable(p_factor_codigo, 'V07', 'Control interno y evidencia disponible', 'Nivel de documentaci�n, soporte, trazabilidad y evidencia disponible para sustentar la evaluaci�n.');
  END;
BEGIN
  SELECT MIN(MRM_ID)
    INTO v_modelo_id
    FROM RL_MR_MODELOS
   WHERE MRM_VERSION = '1.0'
     AND MRM_ESTADO = 'APROBADO'
     AND MRM_ESTADO_REGISTRO = 1;

  IF v_modelo_id IS NULL THEN
    RAISE_APPLICATION_ERROR(-20140, 'No se encontr? el modelo base aprobado de Matrices de Riesgos para corregir textos.');
  END IF;

  UPDATE RL_MR_MODELOS
     SET MRM_NOMBRE = 'Metodolog�a base LA/FT IHSS',
         MRM_DESCRIPCION = 'Modelo inicial aprobado metodol�gicamente en Fase 2 para factores institucionales, variables internas, escalas base y rangos de riesgo.',
         MRM_MOTIVO_ESTADO = 'Metodolog�a base alineada con Fase 2 aprobada.',
         MRM_USR_MODIF_ID = 1,
         MRM_FECHA_MODIF = SYSDATE
   WHERE MRM_ID = v_modelo_id;

  upd_factor('PROVEEDORES', 'Proveedores', 'Factor institucional de proveedores. Peso fijo definido por requerimiento del cliente.');
  upd_factor('CLIENTES_PATRONOS', 'Clientes/Patronos', 'Factor institucional de clientes o patronos. Peso fijo definido por requerimiento del cliente.');
  upd_factor('EMPLEADOS', 'Empleados', 'Factor institucional de empleados. Peso fijo definido por requerimiento del cliente.');

  upd_variables_factor('PROVEEDORES');
  upd_variables_factor('CLIENTES_PATRONOS');
  upd_variables_factor('EMPLEADOS');

  upd_escala('VARIABLE', 1, 1, 'Muy bajo', 'Exposici�n m�nima o condici�n favorable.');
  upd_escala('VARIABLE', 2, 2, 'Bajo', 'Exposici�n baja controlable.');
  upd_escala('VARIABLE', 3, 3, 'Medio', 'Exposici�n media que requiere seguimiento.');
  upd_escala('VARIABLE', 4, 4, 'Alto', 'Exposici�n alta que requiere control reforzado.');
  upd_escala('VARIABLE', 5, 5, 'Cr�tico', 'Exposici�n cr�tica que requiere acci�n prioritaria.');
  upd_escala('INHERENTE', 1.00, 1.80, 'Muy bajo', 'Riesgo inherente muy bajo.');
  upd_escala('INHERENTE', 1.81, 2.60, 'Bajo', 'Riesgo inherente bajo.');
  upd_escala('INHERENTE', 2.61, 3.40, 'Medio', 'Riesgo inherente medio.');
  upd_escala('INHERENTE', 3.41, 4.20, 'Alto', 'Riesgo inherente alto.');
  upd_escala('INHERENTE', 4.21, 5.00, 'Cr�tico', 'Riesgo inherente cr�tico.');
  upd_escala('RESIDUAL', 1.00, 1.80, 'Muy bajo', 'Riesgo residual muy bajo.');
  upd_escala('RESIDUAL', 1.81, 2.60, 'Bajo', 'Riesgo residual bajo.');
  upd_escala('RESIDUAL', 2.61, 3.40, 'Medio', 'Riesgo residual medio que requiere seguimiento.');
  upd_escala('RESIDUAL', 3.41, 4.20, 'Alto', 'Riesgo residual alto; requiere plan de acci�n.');
  upd_escala('RESIDUAL', 4.21, 5.00, 'Cr�tico', 'Riesgo residual cr�tico; requiere plan prioritario.');
  upd_escala('CONTROL', 0, 0, 'Sin control', 'Sin mitigaci�n reconocida para el c�lculo residual.');
  upd_escala('CONTROL', 10, 10, 'D�bil', 'Mitigaci�n del 10% por control con baja solidez o evidencia insuficiente.');
  upd_escala('CONTROL', 25, 25, 'Moderado', 'Mitigaci�n del 25% por control parcialmente efectivo.');
  upd_escala('CONTROL', 40, 40, 'Fuerte', 'Mitigaci�n del 40% por control efectivo y documentado.');
  upd_escala('CONTROL', 55, 55, 'Muy fuerte', 'Mitigaci�n m�xima sugerida del 55% por control s�lido, evidenciado y oportuno.');

END;
/

COMMIT;

PROMPT === Validaci?n r?pida de caracteres corregidos ===
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
