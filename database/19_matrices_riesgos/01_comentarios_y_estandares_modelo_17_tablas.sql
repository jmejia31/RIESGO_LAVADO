-- ============================================================
-- Sistema de Gestión de Riesgos LA/FT - IHSS
-- Script: 01_comentarios_y_estandares_modelo_17_tablas.sql
-- Objetivo: Agregar comentarios institucionales a las 17 tablas y columnas del Módulo Matrices de Riesgos (RL_MR_*).
-- Clasificación: Estándar documental y metadatos de base de datos (ASCII-safe NLS).
-- Responsable documental: Javier Mejía
-- Reglas: solo COMMENT ON; sin cambios estructurales, sin DML, sin DROP, sin TRUNCATE.
-- ============================================================

SET DEFINE OFF;

PROMPT === Comentarios de Tablas (RL_MR_*) ===
COMMENT ON TABLE RL_MR_FAMILIAS_FORMULARIO IS 'Catalogo institucional de familias de formularios de evaluacion de riesgos LA/FT.';
COMMENT ON TABLE RL_MR_VERSIONES_FORMULARIO IS 'Versiones publicadas y borradores de formularios dinamicos con definicion de campos en CLOB JSON.';
COMMENT ON TABLE RL_MR_CATALOGOS IS 'Encabezado de catalogos institucionales parametrizables para formularios de riesgos.';
COMMENT ON TABLE RL_MR_ELEMENTOS_CATALOGO IS 'Elementos o items pertenecientes a cada catalogo parametrizable.';
COMMENT ON TABLE RL_MR_REGLAS_CALCULO IS 'Reglas y algoritmos para el calculo del riesgo inherente y residual.';
COMMENT ON TABLE RL_MR_RIESGOS IS 'Registro de riesgos institucionales identificados para evaluacion LA/FT.';
COMMENT ON TABLE RL_MR_EVALUACIONES_RIESGO IS 'Instancias de evaluacion registradas para los riesgos institucionales.';
COMMENT ON TABLE RL_MR_PROYECCIONES_EVALUACION IS 'Proyeccion optimizada en columnas de los valores consolidados de la evaluacion (VRI, VRR, niveles).';
COMMENT ON TABLE RL_MR_FLUJOS_EVALUACION IS 'Historial de transiciones de flujo de trabajo y estados de las evaluaciones de riesgo.';
COMMENT ON TABLE RL_MR_CONTROLES_RIESGO IS 'Controles de mitigacion asociados a las evaluaciones de riesgo.';
COMMENT ON TABLE RL_MR_EVALUACIONES_CONTROL IS 'Calificacion de efectividad y pruebas de los controles de mitigacion.';
COMMENT ON TABLE RL_MR_PLANES IS 'Planes de accion para la mitigacion y tratamiento de riesgos.';
COMMENT ON TABLE RL_MR_ACTIVIDADES IS 'Actividades especificas componentes de cada plan de accion de mitigacion.';
COMMENT ON TABLE RL_MR_EVIDENCIAS IS 'Repositorio de archivos fisicos subidos como evidencia documental.';
COMMENT ON TABLE RL_MR_EVIDENCIAS_VINCULOS IS 'Relacion polimorfica entre archivos de evidencia y entidades funcionales.';
COMMENT ON TABLE RL_MR_SENALES_ALERTA IS 'Senales de alerta e indicadores de monitoreo continuo de riesgo LA/FT.';
COMMENT ON TABLE RL_MR_AUTOMONITOREO IS 'Registro de automonitoreo operativo efectuado por las areas institucionales.';

PROMPT === Comentarios de Columnas (RL_MR_*) ===

-- 1. RL_MR_FAMILIAS_FORMULARIO
COMMENT ON COLUMN RL_MR_FAMILIAS_FORMULARIO.FAM_ID IS 'Identificador unico de la familia de formulario.';
COMMENT ON COLUMN RL_MR_FAMILIAS_FORMULARIO.FAM_CODIGO IS 'Codigo institucional unico de la familia (ej. LAFT_MATRIZ_INSTITUCIONAL).';
COMMENT ON COLUMN RL_MR_FAMILIAS_FORMULARIO.FAM_NOMBRE IS 'Nombre descriptivo de la familia de formulario.';
COMMENT ON COLUMN RL_MR_FAMILIAS_FORMULARIO.FAM_DESCRIPCION IS 'Descripcion detallada del ambito y proposito de la familia.';
COMMENT ON COLUMN RL_MR_FAMILIAS_FORMULARIO.FAM_ACTIVO IS 'Indicador de estado activo (1) o inactivo (0).';
COMMENT ON COLUMN RL_MR_FAMILIAS_FORMULARIO.FAM_FECHA_CREACION IS 'Fecha y hora de creacion de la familia.';

-- 2. RL_MR_VERSIONES_FORMULARIO
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_ID IS 'Identificador unico de la version del formulario.';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_FAMILIA_ID IS 'Familia de formulario a la cual pertenece la version.';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_CODIGO IS 'Codigo de control de la version (ej. V1.0, V2.0).';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_VERSION IS 'Numero entero correlativo de la version.';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_JSON IS 'Definicion JSON de secciones, campos, ponderaciones y factores del formulario.';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_HASH IS 'Hash SHA-256 de verificacion de integridad del contenido JSON.';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_ESTADO IS 'Estado del ciclo de vida (DRAFT, IN_REVIEW, APPROVED, PUBLISHED, RETIRED, ARCHIVED).';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_VIGENTE IS 'Indicador de version vigente activa (1) o no vigente (0).';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_FECHA_INICIO IS 'Fecha de inicio de vigencia oficial de la version.';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_FECHA_FIN IS 'Fecha de termino o retiro de vigencia oficial de la version.';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_FECHA_CREACION IS 'Fecha de creacion de la version.';
COMMENT ON COLUMN RL_MR_VERSIONES_FORMULARIO.VER_USR_CREACION IS 'Usuario responsable de la creacion de la version.';

-- 3. RL_MR_CATALOGOS
COMMENT ON COLUMN RL_MR_CATALOGOS.CAT_ID IS 'Identificador unico del catalogo.';
COMMENT ON COLUMN RL_MR_CATALOGOS.CAT_CODIGO IS 'Codigo de referencia unico del catalogo (ej. CAT_DEPARTAMENTOS, CAT_CANALES).';
COMMENT ON COLUMN RL_MR_CATALOGOS.CAT_NOMBRE IS 'Nombre descriptivo del catalogo.';
COMMENT ON COLUMN RL_MR_CATALOGOS.CAT_ACTIVO IS 'Indicador de estado activo (1) o inactivo (0).';

-- 4. RL_MR_ELEMENTOS_CATALOGO
COMMENT ON COLUMN RL_MR_ELEMENTOS_CATALOGO.ELE_ID IS 'Identificador unico del elemento del catalogo.';
COMMENT ON COLUMN RL_MR_ELEMENTOS_CATALOGO.ELE_CATALOGO_ID IS 'Catalogo al que pertenece el elemento.';
COMMENT ON COLUMN RL_MR_ELEMENTOS_CATALOGO.ELE_CODIGO IS 'Codigo interno del elemento dentro del catalogo.';
COMMENT ON COLUMN RL_MR_ELEMENTOS_CATALOGO.ELE_VALOR IS 'Valor o etiqueta visible del elemento.';
COMMENT ON COLUMN RL_MR_ELEMENTOS_CATALOGO.ELE_ORDEN IS 'Orden de presentacion en listas desplegables o selecciones.';
COMMENT ON COLUMN RL_MR_ELEMENTOS_CATALOGO.ELE_ACTIVO IS 'Indicador de estado activo (1) o inactivo (0).';

-- 5. RL_MR_REGLAS_CALCULO
COMMENT ON COLUMN RL_MR_REGLAS_CALCULO.REG_ID IS 'Identificador unico de la regla de calculo.';
COMMENT ON COLUMN RL_MR_REGLAS_CALCULO.REG_CODIGO IS 'Codigo identificador de la regla (ej. REG_MATRIZ_CUADRANTE).';
COMMENT ON COLUMN RL_MR_REGLAS_CALCULO.REG_VERSION IS 'Version de la regla de calculo.';
COMMENT ON COLUMN RL_MR_REGLAS_CALCULO.REG_NOMBRE IS 'Nombre descriptivo del algoritmo de calculo.';
COMMENT ON COLUMN RL_MR_REGLAS_CALCULO.REG_ALGORITMO_ID IS 'Identificador tecnico del algoritmo o motor de calculo asociado.';
COMMENT ON COLUMN RL_MR_REGLAS_CALCULO.REG_ACTIVA IS 'Indicador de regla activa (1) o inactiva (0).';

-- 6. RL_MR_RIESGOS
COMMENT ON COLUMN RL_MR_RIESGOS.RIE_ID IS 'Identificador unico del riesgo institucional.';
COMMENT ON COLUMN RL_MR_RIESGOS.RIE_CODIGO IS 'Codigo correlativo unico del riesgo (ej. RIE-LAFT-001).';
COMMENT ON COLUMN RL_MR_RIESGOS.RIE_NOMBRE IS 'Nombre o titulo corto del riesgo.';
COMMENT ON COLUMN RL_MR_RIESGOS.RIE_DESCRIPCION IS 'Descripcion detallada del riesgo, causas y consecuencias.';
COMMENT ON COLUMN RL_MR_RIESGOS.RIE_ACTIVO IS 'Indicador de estado activo (1) o inactivo (0).';
COMMENT ON COLUMN RL_MR_RIESGOS.RIE_USR_CREACION IS 'Usuario que registro el riesgo.';
COMMENT ON COLUMN RL_MR_RIESGOS.RIE_FECHA_CREACION IS 'Fecha y hora de registro del riesgo.';

-- 7. RL_MR_EVALUACIONES_RIESGO
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_ID IS 'Identificador unico de la evaluacion de riesgo.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_RIESGO_ID IS 'Riesgo institucional evaluado.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_VERSION_ID IS 'Version del formulario dinamico utilizada en la evaluacion.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_DATOS_JSON IS 'Datos estructurados capturados en la evaluacion en formato JSON.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_CALCULOS_JSON IS 'Resultados consolidados de ponderaciones, VRI y VRR en formato JSON.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_FECHA_REGISTRO IS 'Fecha y hora en que se registro la evaluacion.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_USR_REGISTRO IS 'Usuario que registro la evaluacion.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_VERSION_ROW IS 'Control de concurrencia optimista para actualizacion de filas.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_RIESGO.EVA_ACTIVO IS 'Indicador de estado activo (1) o inactivo (0).';

-- 8. RL_MR_PROYECCIONES_EVALUACION
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_ID IS 'Identificador unico de la proyeccion.';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_EVALUACION_ID IS 'Evaluacion de riesgo proyectada.';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_CODIGO_RIESGO IS 'Codigo del riesgo asociado para rapida indexacion.';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_AREA_PRINCIPAL IS 'Area institucional principal responsable del riesgo.';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_VRI IS 'Valor del Riesgo Inherente (VRI) calculado.';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_VRR IS 'Valor del Riesgo Residual (VRR) calculated.';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_NIVEL_INHERENTE IS 'Nivel cualitativo del riesgo inherente (BAJO, MEDIO, ALTO, CRITICO).';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_NIVEL_RESIDUAL IS 'Nivel cualitativo del riesgo residual (BAJO, MEDIO, ALTO, CRITICO).';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_RESPUESTA_RIESGO IS 'Estrategia institucional ante el riesgo (ACEPTAR, MITIGAR, EVITAR, TRANSFERIR).';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_ESTADO_EVALUACION IS 'Estado actual de la evaluacion (BORRADOR, EN_REVISION, APROBADA, etc.).';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_DUENO_RIESGO IS 'Nombre o cargo del dueno del riesgo.';
COMMENT ON COLUMN RL_MR_PROYECCIONES_EVALUACION.PROY_FECHA_EVAL IS 'Fecha oficial de la evaluacion proyectada.';

-- 9. RL_MR_FLUJOS_EVALUACION
COMMENT ON COLUMN RL_MR_FLUJOS_EVALUACION.FLU_ID IS 'Identificador unico del movimiento de flujo.';
COMMENT ON COLUMN RL_MR_FLUJOS_EVALUACION.FLU_EVALUACION_ID IS 'Evaluacion de riesgo sujeta al cambio de estado.';
COMMENT ON COLUMN RL_MR_FLUJOS_EVALUACION.FLU_ESTADO IS 'Estado resultante (BORRADOR, EN_REVISION, OBSERVADA, APROBADA, RECHAZADA, CERRADA).';
COMMENT ON COLUMN RL_MR_FLUJOS_EVALUACION.FLU_MOTIVO IS 'Observaciones, justificacion o comentarios de la transicion.';
COMMENT ON COLUMN RL_MR_FLUJOS_EVALUACION.FLU_USR_ID IS 'Usuario que realizo la transicion de estado.';
COMMENT ON COLUMN RL_MR_FLUJOS_EVALUACION.FLU_FECHA IS 'Fecha y hora de la transicion.';

-- 10. RL_MR_CONTROLES_RIESGO
COMMENT ON COLUMN RL_MR_CONTROLES_RIESGO.CON_ID IS 'Identificador unico del control.';
COMMENT ON COLUMN RL_MR_CONTROLES_RIESGO.CON_EVALUACION_ID IS 'Evaluacion a la cual se aplica el control.';
COMMENT ON COLUMN RL_MR_CONTROLES_RIESGO.CON_TIPO IS 'Tipo de control (PREVENTIVO, DETECTIVO, CORRECTIVO).';
COMMENT ON COLUMN RL_MR_CONTROLES_RIESGO.CON_DESCRIPCION IS 'Descripcion detallada del mecanismo de control.';
COMMENT ON COLUMN RL_MR_CONTROLES_RIESGO.CON_AUTOMATIZACION IS 'Nivel de automatizacion (MANUAL, SEMIAUTOMATICO, AUTOMATICO).';
COMMENT ON COLUMN RL_MR_CONTROLES_RIESGO.CON_ESTADO IS 'Estado operativo del control (ACTIVO, INACTIVO, EN_REVISION).';

-- 11. RL_MR_EVALUACIONES_CONTROL
COMMENT ON COLUMN RL_MR_EVALUACIONES_CONTROL.ECO_ID IS 'Identificador unico de la evaluacion del control.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_CONTROL.ECO_CONTROL_ID IS 'Control evaluado.';
COMMENT ON COLUMN RL_MR_EVALUACIONES_CONTROL.ECO_EFECTIVIDAD IS 'Porcentaje de efectividad asignado al control (0 a 100%).';
COMMENT ON COLUMN RL_MR_EVALUACIONES_CONTROL.ECO_COMENTARIO IS 'Comentario o justificacion de la efectividad evaluada.';

-- 12. RL_MR_PLANES
COMMENT ON COLUMN RL_MR_PLANES.PLA_ID IS 'Identificador unico del plan de accion.';
COMMENT ON COLUMN RL_MR_PLANES.PLA_EVALUACION_ID IS 'Evaluacion de riesgo a la que esta vinculado el plan.';
COMMENT ON COLUMN RL_MR_PLANES.PLA_DESCRIPCION IS 'Descripcion del objetivo y alcance del plan de accion.';
COMMENT ON COLUMN RL_MR_PLANES.PLA_AVANCE IS 'Porcentaje de avance global del plan de accion (0 a 100%).';
COMMENT ON COLUMN RL_MR_PLANES.PLA_PRESUPUESTO IS 'Presupuesto financiero asignado al plan de accion.';
COMMENT ON COLUMN RL_MR_PLANES.PLA_FECHA_INICIO IS 'Fecha programada de inicio del plan.';
COMMENT ON COLUMN RL_MR_PLANES.PLA_FECHA_FIN IS 'Fecha programada de finalizacion del plan.';
COMMENT ON COLUMN RL_MR_PLANES.PLA_ESTADO IS 'Estado del plan (PLANIFICADO, EN_EJECUCION, COMPLETADO, CANCELADO).';

-- 13. RL_MR_ACTIVIDADES
COMMENT ON COLUMN RL_MR_ACTIVIDADES.ACT_ID IS 'Identificador unico de la actividad.';
COMMENT ON COLUMN RL_MR_ACTIVIDADES.ACT_PLAN_ID IS 'Plan de accion al cual pertenece la actividad.';
COMMENT ON COLUMN RL_MR_ACTIVIDADES.ACT_DESCRIPCION IS 'Descripcion de la tarea o actividad a realizar.';
COMMENT ON COLUMN RL_MR_ACTIVIDADES.ACT_RESPONSABLE IS 'Nombre o cargo del responsable de la ejecucion.';
COMMENT ON COLUMN RL_MR_ACTIVIDADES.ACT_AVANCE IS 'Porcentaje de avance de la actividad (0 a 100%).';
COMMENT ON COLUMN RL_MR_ACTIVIDADES.ACT_FECHA_INICIO IS 'Fecha de inicio programada o real de la actividad.';
COMMENT ON COLUMN RL_MR_ACTIVIDADES.ACT_FECHA_FIN IS 'Fecha de termino programada o real de la actividad.';
COMMENT ON COLUMN RL_MR_ACTIVIDADES.ACT_ESTADO IS 'Estado de la actividad (PENDIENTE, EN_PROCESO, FINALIZADA, CANCELADA).';

-- 14. RL_MR_EVIDENCIAS
COMMENT ON COLUMN RL_MR_EVIDENCIAS.EVI_ID IS 'Identificador unico del archivo de evidencia.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.EVI_NOMBRE_ARCHIVO IS 'Nombre original del archivo cargado.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.EVI_EXTENSION IS 'Extension del archivo (pdf, xlsx, docx, png, etc.).';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.EVI_TAMANO IS 'Tamano del archivo en bytes.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.EVI_HASH IS 'Hash SHA-256 para verificacion de integridad del archivo.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.EVI_RUTA IS 'Ruta relativa o almacenamiento fisico en el servidor.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.EVI_USR_CREACION IS 'Usuario que cargo la evidencia.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS.EVI_FECHA_CREACION IS 'Fecha y hora de carga de la evidencia.';

-- 15. RL_MR_EVIDENCIAS_VINCULOS
COMMENT ON COLUMN RL_MR_EVIDENCIAS_VINCULOS.EVV_ID IS 'Identificador unico del vinculo de evidencia.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS_VINCULOS.EVV_EVIDENCIA_ID IS 'Archivo de evidencia vinculado.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS_VINCULOS.EVV_TIPO_ENTIDAD IS 'Tipo de entidad funcional (RIESGO, EVALUACION, CONTROL, PLAN, ACTIVIDAD, ALERTA, AUTOMONITOREO).';
COMMENT ON COLUMN RL_MR_EVIDENCIAS_VINCULOS.EVV_ENTIDAD_ID IS 'Identificador unico de la entidad receptora de la evidencia.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS_VINCULOS.EVV_USR_CREACION IS 'Usuario que asocio el vinculo.';
COMMENT ON COLUMN RL_MR_EVIDENCIAS_VINCULOS.EVV_FECHA_CREACION IS 'Fecha y hora de creacion del vinculo.';

-- 16. RL_MR_SENALES_ALERTA
COMMENT ON COLUMN RL_MR_SENALES_ALERTA.ALE_ID IS 'Identificador unico de la senal de alerta.';
COMMENT ON COLUMN RL_MR_SENALES_ALERTA.ALE_EVALUACION_ID IS 'Evaluacion de riesgo monitoreada por la alerta.';
COMMENT ON COLUMN RL_MR_SENALES_ALERTA.ALE_CODIGO IS 'Codigo identificador de la senal de alerta.';
COMMENT ON COLUMN RL_MR_SENALES_ALERTA.ALE_INDICADOR IS 'Nombre o descripcion del indicador gatillador.';
COMMENT ON COLUMN RL_MR_SENALES_ALERTA.ALE_ESTADO IS 'Estado del indicador (ACTIVO, INACTIVO).';
COMMENT ON COLUMN RL_MR_SENALES_ALERTA.ALE_FECHA_DISPARO IS 'Fecha y hora en que se disparo o activo la alerta.';

-- 17. RL_MR_AUTOMONITOREO
COMMENT ON COLUMN RL_MR_AUTOMONITOREO.MON_ID IS 'Identificador unico del registro de automonitoreo.';
COMMENT ON COLUMN RL_MR_AUTOMONITOREO.MON_EVALUACION_ID IS 'Evaluacion de riesgo sobre la cual se automonitorea.';
COMMENT ON COLUMN RL_MR_AUTOMONITOREO.MON_ESTADO_RIESGO IS 'Estado del riesgo al momento del automonitoreo.';
COMMENT ON COLUMN RL_MR_AUTOMONITOREO.MON_ESTADO_CONTR IS 'Estado de los controles al momento del automonitoreo.';
COMMENT ON COLUMN RL_MR_AUTOMONITOREO.MON_RESULTADO IS 'Resultado, hallazgos u observaciones del automonitoreo.';
COMMENT ON COLUMN RL_MR_AUTOMONITOREO.MON_USR_ID IS 'Usuario que realizo la sesion de automonitoreo.';
COMMENT ON COLUMN RL_MR_AUTOMONITOREO.MON_FECHA IS 'Fecha y hora del registro de automonitoreo.';
