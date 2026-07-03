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
DECLARE
BEGIN
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_MODELOS IS ''' || REPLACE(UNISTR('Versiones metodol\00F3gicas aprobables/aprobadas del m\00F3dulo Matrices de Riesgos.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_FACTORES IS ''' || REPLACE(UNISTR('Factores institucionales obligatorios: Proveedores, Clientes/Patronos y Empleados.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_VARIABLES IS ''' || REPLACE(UNISTR('Variables internas por factor institucional, con ponderaci\00F3n interna totalizable al 100% por factor.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_ESCALAS IS ''' || REPLACE(UNISTR('Rangos y niveles de calificaci\00F3n para variables, riesgo inherente, residual y controles.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_CRITERIOS IS ''' || REPLACE(UNISTR('Criterios de calificaci\00F3n por variable y escala.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_MATRICES IS ''' || REPLACE(UNISTR('Encabezado de matrices generadas por sujeto evaluado o matriz institucional.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_DETALLE IS ''' || REPLACE(UNISTR('Detalle de variables evaluadas en una matriz, con snapshot de peso y puntaje.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_CONTROLES IS ''' || REPLACE(UNISTR('Controles mitigantes asociados a la matriz y a factores cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_RESULTADOS IS ''' || REPLACE(UNISTR('Resultados de riesgo inherente, mitigaci\00F3n y riesgo residual por factor e institucional.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_PLANES_ACCION IS ''' || REPLACE(UNISTR('Planes de acci\00F3n obligatorios o voluntarios asociados al resultado de la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_EVIDENCIAS IS ''' || REPLACE(UNISTR('Metadatos de evidencias protegidas asociadas a matrices, controles o planes.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_HISTORIAL IS ''' || REPLACE(UNISTR('Historial funcional del m\00F3dulo; complementa RL_AUDITORIA para trazabilidad espec\00EDfica de matrices.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON TABLE RL_MR_INTEGRACION_DNP IS ''' || REPLACE(UNISTR('Bandeja local para integraci\00F3n futura obligatoria hacia DNP, sin escritura directa hasta aprobaci\00F3n t\00E9cnica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del modelo metodol\00F3gico de matrices de riesgos.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_NOMBRE IS ''' || REPLACE(UNISTR('Nombre del modelo metodol\00F3gico.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_VERSION IS ''' || REPLACE(UNISTR('Versi\00F3n funcional y t\00E9cnica del modelo metodol\00F3gico.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_DESCRIPCION IS ''' || REPLACE(UNISTR('Descripci\00F3n general del alcance metodol\00F3gico del modelo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_ESTADO IS ''' || REPLACE(UNISTR('Estado del modelo: BORRADOR, EN_REVISION, APROBADO, CERRADO o INACTIVO.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_VIGENCIA IS ''' || REPLACE(UNISTR('Fecha desde la cual la versi\00F3n metodol\00F3gica puede aplicarse.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_CIERRE IS ''' || REPLACE(UNISTR('Fecha de cierre o retiro de la versi\00F3n metodol\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_APROBADO_POR IS ''' || REPLACE(UNISTR('Usuario responsable de aprobar la versi\00F3n metodol\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_APROBACION IS ''' || REPLACE(UNISTR('Fecha de aprobaci\00F3n de la versi\00F3n metodol\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_MOTIVO_ESTADO IS ''' || REPLACE(UNISTR('Motivo funcional del cambio de estado del modelo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_USR_CREACION_ID IS ''' || REPLACE(UNISTR('Usuario que registr\00F3 el modelo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_USR_MODIF_ID IS ''' || REPLACE(UNISTR('\00DAltimo usuario que modific\00F3 el registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_FECHA_MODIF IS ''' || REPLACE(UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MODELOS.MRM_ESTADO_REGISTRO IS ''' || REPLACE(UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del factor institucional.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_MODELO_ID IS ''' || REPLACE(UNISTR('Modelo metodol\00F3gico al que pertenece el factor.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_CODIGO IS ''' || REPLACE(UNISTR('C\00F3digo funcional del factor institucional.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_NOMBRE IS ''' || REPLACE(UNISTR('Nombre del factor institucional.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_DESCRIPCION IS ''' || REPLACE(UNISTR('Descripci\00F3n funcional del factor institucional.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_PESO_INSTITUCIONAL IS ''' || REPLACE(UNISTR('Peso institucional fijo del factor dentro del riesgo total.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_ORDEN IS ''' || REPLACE(UNISTR('Orden de presentaci\00F3n y c\00E1lculo del factor.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_ESTADO_REGISTRO IS ''' || REPLACE(UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_MOTIVO_INACTIVO IS ''' || REPLACE(UNISTR('Motivo obligatorio cuando el factor se inactive l\00F3gicamente.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_USR_CREACION_ID IS ''' || REPLACE(UNISTR('Usuario que registr\00F3 el factor.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_USR_MODIF_ID IS ''' || REPLACE(UNISTR('\00DAltimo usuario que modific\00F3 el registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_FACTORES.MRF_FECHA_MODIF IS ''' || REPLACE(UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico de la variable interna.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FACTOR_ID IS ''' || REPLACE(UNISTR('Factor institucional al que pertenece la variable.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_CODIGO IS ''' || REPLACE(UNISTR('C\00F3digo funcional de la variable dentro del factor.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_NOMBRE IS ''' || REPLACE(UNISTR('Nombre de la variable de riesgo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_DESCRIPCION IS ''' || REPLACE(UNISTR('Descripci\00F3n funcional de la variable de riesgo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_PESO_INTERNO IS ''' || REPLACE(UNISTR('Peso interno de la variable dentro del factor; debe totalizar 100% por factor.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_TIPO_DATO IS ''' || REPLACE(UNISTR('Tipo de dato esperado para capturar o calcular la variable.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FUENTE_DATO IS ''' || REPLACE(UNISTR('Origen funcional del dato: captura, consulta o integraci\00F3n autorizada.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_OBLIGATORIA IS ''' || REPLACE(UNISTR('Indica si la variable es obligatoria para completar la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ORDEN IS ''' || REPLACE(UNISTR('Orden de presentaci\00F3n y c\00E1lculo de la variable.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_ESTADO_REGISTRO IS ''' || REPLACE(UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_MOTIVO_INACTIVO IS ''' || REPLACE(UNISTR('Motivo obligatorio cuando la variable se inactive l\00F3gicamente.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_USR_CREACION_ID IS ''' || REPLACE(UNISTR('Usuario que registr\00F3 la variable.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_USR_MODIF_ID IS ''' || REPLACE(UNISTR('\00DAltimo usuario que modific\00F3 el registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_VARIABLES.MRV_FECHA_MODIF IS ''' || REPLACE(UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico de la escala metodol\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_MODELO_ID IS ''' || REPLACE(UNISTR('Modelo metodol\00F3gico al que pertenece la escala.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_TIPO IS ''' || REPLACE(UNISTR('Tipo de escala: VARIABLE, INHERENTE, RESIDUAL o CONTROL.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_VALOR_MIN IS ''' || REPLACE(UNISTR('Valor m\00EDnimo del rango de la escala.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_VALOR_MAX IS ''' || REPLACE(UNISTR('Valor m\00E1ximo del rango de la escala.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_NIVEL IS ''' || REPLACE(UNISTR('Nivel funcional asignado al rango de la escala.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_COLOR_HEX IS ''' || REPLACE(UNISTR('Color sugerido para representar visualmente el nivel.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_DESCRIPCION IS ''' || REPLACE(UNISTR('Descripci\00F3n funcional del rango o nivel de escala.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ORDEN IS ''' || REPLACE(UNISTR('Orden de presentaci\00F3n del nivel dentro de la escala.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_ESTADO_REGISTRO IS ''' || REPLACE(UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_ESCALAS.MRE_MOTIVO_INACTIVO IS ''' || REPLACE(UNISTR('Motivo obligatorio cuando la escala se inactive l\00F3gicamente.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del criterio de calificaci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VARIABLE_ID IS ''' || REPLACE(UNISTR('Variable de riesgo a la que pertenece el criterio.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ESCALA_ID IS ''' || REPLACE(UNISTR('Escala relacionada con el criterio cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VALOR_DESDE IS ''' || REPLACE(UNISTR('Valor inicial del rango del criterio.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_VALOR_HASTA IS ''' || REPLACE(UNISTR('Valor final del rango del criterio.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_PUNTAJE IS ''' || REPLACE(UNISTR('Puntaje asignado cuando el criterio se cumple.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_DESCRIPCION IS ''' || REPLACE(UNISTR('Descripci\00F3n funcional del criterio de calificaci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_ESTADO_REGISTRO IS ''' || REPLACE(UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CRITERIOS.MRC_MOTIVO_INACTIVO IS ''' || REPLACE(UNISTR('Motivo obligatorio cuando el criterio se inactive l\00F3gicamente.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico de la matriz generada.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_MODELO_ID IS ''' || REPLACE(UNISTR('Modelo metodol\00F3gico usado por la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SUJETO_TIPO IS ''' || REPLACE(UNISTR('Tipo de sujeto evaluado por la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SUJETO_ID_EXT IS ''' || REPLACE(UNISTR('Identificador externo del sujeto evaluado cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_DOCUMENTO IS ''' || REPLACE(UNISTR('Documento, c\00F3digo o n\00FAmero de referencia del sujeto evaluado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_NOMBRE_SUJETO IS ''' || REPLACE(UNISTR('Nombre o descripci\00F3n del sujeto evaluado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ORIGEN_DATOS IS ''' || REPLACE(UNISTR('Origen de datos usado para construir la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ESTADO IS ''' || REPLACE(UNISTR('Estado funcional de la matriz: BORRADOR, CALCULADA, EN_REVISION, OBSERVADA, APROBADA, CERRADA o INACTIVA.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_EVALUACION IS ''' || REPLACE(UNISTR('Fecha en que se registra o calcula la evaluaci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_CIERRE IS ''' || REPLACE(UNISTR('Fecha de cierre formal de la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_CERRADO_POR IS ''' || REPLACE(UNISTR('Usuario que cerr\00F3 formalmente la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_MOTIVO_ESTADO IS ''' || REPLACE(UNISTR('Motivo funcional del cambio de estado de la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_SNAPSHOT_METODO IS ''' || REPLACE(UNISTR('Snapshot de la metodolog\00EDa usada para proteger matrices cerradas.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_USR_CREACION_ID IS ''' || REPLACE(UNISTR('Usuario que registr\00F3 la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_USR_MODIF_ID IS ''' || REPLACE(UNISTR('\00DAltimo usuario que modific\00F3 el registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_FECHA_MODIF IS ''' || REPLACE(UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_MATRICES.MRMAT_ESTADO_REGISTRO IS ''' || REPLACE(UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del detalle evaluado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_MATRIZ_ID IS ''' || REPLACE(UNISTR('Matriz a la que pertenece el detalle.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_VARIABLE_ID IS ''' || REPLACE(UNISTR('Variable evaluada dentro de la matriz.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_VALOR_CAPTURADO IS ''' || REPLACE(UNISTR('Valor capturado, consultado o calculado para la variable.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_PUNTAJE IS ''' || REPLACE(UNISTR('Puntaje asignado a la variable.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_PESO_SNAPSHOT IS ''' || REPLACE(UNISTR('Peso de la variable al momento del c\00E1lculo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_PUNTAJE_PONDERADO IS ''' || REPLACE(UNISTR('Resultado ponderado de la variable.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_JUSTIFICACION IS ''' || REPLACE(UNISTR('Justificaci\00F3n funcional del valor o puntaje asignado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_FUENTE_DATO IS ''' || REPLACE(UNISTR('Fuente del dato usado para evaluar la variable.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_SNAPSHOT_VARIABLE IS ''' || REPLACE(UNISTR('Snapshot de la variable y criterio usados en el c\00E1lculo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_USR_CREACION_ID IS ''' || REPLACE(UNISTR('Usuario que registr\00F3 el detalle.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_DETALLE.MRD_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del detalle.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del control mitigante.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_MATRIZ_ID IS ''' || REPLACE(UNISTR('Matriz a la que pertenece el control mitigante.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FACTOR_ID IS ''' || REPLACE(UNISTR('Factor institucional al que se asocia el control cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_NOMBRE IS ''' || REPLACE(UNISTR('Nombre del control mitigante.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_DESCRIPCION IS ''' || REPLACE(UNISTR('Descripci\00F3n funcional del control mitigante.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_PERIODICIDAD IS ''' || REPLACE(UNISTR('Periodicidad con la que opera o se revisa el control.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_OPORTUNIDAD IS ''' || REPLACE(UNISTR('Oportunidad del control respecto al evento de riesgo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_AUTOMATIZACION IS ''' || REPLACE(UNISTR('Nivel de automatizaci\00F3n del control.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_PROCEDIMIENTOS IS ''' || REPLACE(UNISTR('Nivel de formalizaci\00F3n de procedimientos del control.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_CALIDAD IS ''' || REPLACE(UNISTR('Calidad general del control seg\00FAn metodolog\00EDa aprobada.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_EFECTIVIDAD_PCT IS ''' || REPLACE(UNISTR('Porcentaje de efectividad o mitigaci\00F3n calculada para el control.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_RESPONSABLE IS ''' || REPLACE(UNISTR('Responsable funcional del control.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_ESTADO IS ''' || REPLACE(UNISTR('Estado funcional del control.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_MOTIVO_INACTIVO IS ''' || REPLACE(UNISTR('Motivo obligatorio cuando el control se inactive l\00F3gicamente.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_EVIDENCIA_OBL IS ''' || REPLACE(UNISTR('Indica si el control requiere evidencia documental obligatoria.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_USR_CREACION_ID IS ''' || REPLACE(UNISTR('Usuario que registr\00F3 el control.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_USR_MODIF_ID IS ''' || REPLACE(UNISTR('\00DAltimo usuario que modific\00F3 el registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_CONTROLES.MRCTRL_FECHA_MODIF IS ''' || REPLACE(UNISTR('Fecha de \00FAltima modificaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del resultado de c\00E1lculo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MATRIZ_ID IS ''' || REPLACE(UNISTR('Matriz a la que pertenece el resultado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_FACTOR_ID IS ''' || REPLACE(UNISTR('Factor institucional asociado cuando el resultado es por factor.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_TIPO_RESULTADO IS ''' || REPLACE(UNISTR('Tipo de resultado: por factor o institucional.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_VERSION_CALCULO IS ''' || REPLACE(UNISTR('Versi\00F3n del algoritmo o regla de c\00E1lculo usada.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_ES_VIGENTE IS ''' || REPLACE(UNISTR('Indica si el resultado es el vigente para la matriz, factor y tipo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_PUNTAJE_INHERENTE IS ''' || REPLACE(UNISTR('Puntaje de riesgo inherente calculado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_NIVEL_INHERENTE IS ''' || REPLACE(UNISTR('Nivel de riesgo inherente calculado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MITIGACION_PCT IS ''' || REPLACE(UNISTR('Porcentaje de mitigaci\00F3n aplicado por controles.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_PUNTAJE_RESIDUAL IS ''' || REPLACE(UNISTR('Puntaje de riesgo residual calculado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_NIVEL_RESIDUAL IS ''' || REPLACE(UNISTR('Nivel de riesgo residual calculado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_REQUIERE_PLAN IS ''' || REPLACE(UNISTR('Indica si el resultado exige plan de acci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_MOTIVO_RECALCULO IS ''' || REPLACE(UNISTR('Motivo funcional cuando se genera un rec\00E1lculo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_RESULTADO_ANTERIOR_ID IS ''' || REPLACE(UNISTR('Resultado anterior relacionado con el rec\00E1lculo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_SNAPSHOT_CALCULO IS ''' || REPLACE(UNISTR('Snapshot de entradas, reglas y salida del c\00E1lculo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_FECHA_CALCULO IS ''' || REPLACE(UNISTR('Fecha en que se gener\00F3 el resultado.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_RESULTADOS.MRR_USR_CALCULO_ID IS ''' || REPLACE(UNISTR('Usuario que ejecut\00F3 o solicit\00F3 el c\00E1lculo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del plan de acci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MATRIZ_ID IS ''' || REPLACE(UNISTR('Matriz asociada al plan de acci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_RESULTADO_ID IS ''' || REPLACE(UNISTR('Resultado que origin\00F3 el plan de acci\00F3n cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ACTIVIDAD IS ''' || REPLACE(UNISTR('Actividad o acci\00F3n correctiva definida.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_RESPONSABLE IS ''' || REPLACE(UNISTR('Responsable funcional de ejecutar la actividad.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_PERIODICIDAD IS ''' || REPLACE(UNISTR('Periodicidad de ejecuci\00F3n o seguimiento del plan.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_INICIO IS ''' || REPLACE(UNISTR('Fecha planificada de inicio.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_FIN IS ''' || REPLACE(UNISTR('Fecha planificada o real de finalizaci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MEDIO_PRUEBA IS ''' || REPLACE(UNISTR('Medio de prueba requerido para evidenciar cumplimiento.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_OBSERVACIONES IS ''' || REPLACE(UNISTR('Observaciones funcionales del plan de acci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_ESTADO IS ''' || REPLACE(UNISTR('Estado funcional del plan de acci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_MOTIVO_CIERRE IS ''' || REPLACE(UNISTR('Motivo o justificaci\00F3n del cierre del plan.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_USR_CREACION_ID IS ''' || REPLACE(UNISTR('Usuario que registr\00F3 el plan de acci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_USR_CIERRE_ID IS ''' || REPLACE(UNISTR('Usuario que cerr\00F3 el plan de acci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_PLANES_ACCION.MRPA_FECHA_CIERRE IS ''' || REPLACE(UNISTR('Fecha de cierre del plan de acci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico de la evidencia documental.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_MATRIZ_ID IS ''' || REPLACE(UNISTR('Matriz asociada a la evidencia.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_CONTROL_ID IS ''' || REPLACE(UNISTR('Control asociado a la evidencia cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_PLAN_ID IS ''' || REPLACE(UNISTR('Plan de acci\00F3n asociado a la evidencia cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_NOMBRE_ORIGINAL IS ''' || REPLACE(UNISTR('Nombre original del archivo cargado por el usuario.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_NOMBRE_FISICO IS ''' || REPLACE(UNISTR('Nombre f\00EDsico seguro asignado al archivo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_TIPO_MIME IS ''' || REPLACE(UNISTR('Tipo MIME identificado para el archivo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_EXTENSION IS ''' || REPLACE(UNISTR('Extensi\00F3n validada del archivo.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_TAMANO_BYTES IS ''' || REPLACE(UNISTR('Tama\00F1o del archivo en bytes.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_RUTA_FISICA IS ''' || REPLACE(UNISTR('Ruta protegida de almacenamiento f\00EDsico.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_HASH_SHA256 IS ''' || REPLACE(UNISTR('Huella SHA-256 del archivo cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_ESTADO_REGISTRO IS ''' || REPLACE(UNISTR('Indicador de registro activo o inactivo para eliminaci\00F3n l\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_MOTIVO_INACTIVO IS ''' || REPLACE(UNISTR('Motivo obligatorio cuando la evidencia se inactive l\00F3gicamente.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_USR_CREACION_ID IS ''' || REPLACE(UNISTR('Usuario que carg\00F3 o registr\00F3 la evidencia.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del registro.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_USR_INACTIVO_ID IS ''' || REPLACE(UNISTR('Usuario que realiz\00F3 la eliminaci\00F3n l\00F3gica.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_EVIDENCIAS.MREV_FECHA_INACTIVO IS ''' || REPLACE(UNISTR('Fecha de eliminaci\00F3n l\00F3gica de la evidencia.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del evento hist\00F3rico funcional.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_MATRIZ_ID IS ''' || REPLACE(UNISTR('Matriz asociada al evento hist\00F3rico cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_TABLA IS ''' || REPLACE(UNISTR('Tabla funcional sobre la que ocurri\00F3 el evento.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_REGISTRO_ID IS ''' || REPLACE(UNISTR('Identificador del registro afectado por el evento.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ACCION IS ''' || REPLACE(UNISTR('Acci\00F3n funcional registrada.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ESTADO_ANTERIOR IS ''' || REPLACE(UNISTR('Estado anterior del registro cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_ESTADO_NUEVO IS ''' || REPLACE(UNISTR('Estado nuevo del registro cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_MOTIVO IS ''' || REPLACE(UNISTR('Motivo funcional del evento hist\00F3rico.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_DATOS_ANT IS ''' || REPLACE(UNISTR('Snapshot de datos anteriores cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_DATOS_NVO IS ''' || REPLACE(UNISTR('Snapshot de datos nuevos cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_USR_ID IS ''' || REPLACE(UNISTR('Usuario responsable del evento.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_USR_EMAIL IS ''' || REPLACE(UNISTR('Correo del usuario responsable del evento.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_IP IS ''' || REPLACE(UNISTR('Direcci\00F3n IP registrada para el evento.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_HISTORIAL.MRH_FECHA IS ''' || REPLACE(UNISTR('Fecha y hora del evento hist\00F3rico.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_ID IS ''' || REPLACE(UNISTR('Identificador \00FAnico del registro de integraci\00F3n futura con DNP.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_MATRIZ_ID IS ''' || REPLACE(UNISTR('Matriz asociada a la calificaci\00F3n que se integrar\00E1.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_NUMERO_PATRONO IS ''' || REPLACE(UNISTR('N\00FAmero de patrono relacionado cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_CALIFICACION IS ''' || REPLACE(UNISTR('Calificaci\00F3n de riesgo preparada para integraci\00F3n futura.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_PUNTAJE_RESIDUAL IS ''' || REPLACE(UNISTR('Puntaje residual asociado a la calificaci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_ESTADO_ENVIO IS ''' || REPLACE(UNISTR('Estado de la integraci\00F3n futura: PENDIENTE, ENVIADO, ERROR o ANULADO.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_INTENTOS IS ''' || REPLACE(UNISTR('Cantidad de intentos de env\00EDo registrados.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_RESPUESTA IS ''' || REPLACE(UNISTR('Respuesta t\00E9cnica recibida de la integraci\00F3n cuando aplique.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_FECHA_CREACION IS ''' || REPLACE(UNISTR('Fecha de creaci\00F3n del registro de integraci\00F3n.'), '''', '''''') || '''';
  EXECUTE IMMEDIATE 'COMMENT ON COLUMN RL_MR_INTEGRACION_DNP.MRDNP_FECHA_ENVIO IS ''' || REPLACE(UNISTR('Fecha de env\00EDo hacia la integraci\00F3n cuando aplique.'), '''', '''''') || '''';
END;
/


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
     SET MRM_NOMBRE = UNISTR('Metodolog\00EDa base LA/FT IHSS'),
         MRM_DESCRIPCION = UNISTR('Modelo inicial aprobado metodol\00F3gicamente en Fase 2 para factores institucionales, variables internas, escalas base y rangos de riesgo.'),
         MRM_MOTIVO_ESTADO = UNISTR('Metodolog\00EDa base alineada con Fase 2 aprobada.'),
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
