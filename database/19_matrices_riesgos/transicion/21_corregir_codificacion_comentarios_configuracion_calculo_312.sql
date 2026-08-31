set echo off
set verify off
set termout on
whenever sqlerror exit sql.sqlcode
whenever oserror exit failure

PROMPT CORREGIR_CODIFICACION_COMENTARIOS_CONFIGURACION_CALCULO_312

COMMENT ON TABLE RL_MR_FORMULAS IS 'Catálogo maestro de fórmulas administrables utilizadas en la configuración de cálculo de las matrices de riesgos.';
COMMENT ON TABLE RL_MR_FORMULA_VERSIONES IS 'Versiones inmutables de las definiciones DSL asociadas a las fórmulas administrables de matrices de riesgos.';
COMMENT ON TABLE RL_MR_FORMULA_USOS IS 'Relaciones entre versiones de fórmulas y campos de versiones de formularios donde son utilizadas.';
COMMENT ON TABLE RL_MR_FUNCIONES IS 'Catálogo maestro de funciones disponibles para la configuración administrable del motor de cálculo de matrices de riesgos.';
COMMENT ON TABLE RL_MR_FUNCION_VERSIONES IS 'Versiones de funciones nativas o compuestas disponibles para la configuración del motor de cálculo de matrices de riesgos.';
COMMENT ON TABLE RL_MR_FUNCION_ARGUMENTOS IS 'Definición tipada y ordenada de los argumentos pertenecientes a cada versión de función.';
COMMENT ON TABLE RL_MR_PARAMETROS_CALCULO IS 'Catálogo maestro de parámetros administrables utilizados por las fórmulas y reglas de cálculo de matrices de riesgos.';
COMMENT ON TABLE RL_MR_PARAMETRO_VERSIONES IS 'Versiones tipadas e históricamente reproducibles de los valores asociados a parámetros de cálculo.';

COMMENT ON COLUMN RL_MR_FORMULAS.FOR_ID IS 'Identificador interno único de la fórmula administrable.';
COMMENT ON COLUMN RL_MR_FORMULAS.FOR_CODIGO IS 'Código funcional único, canónico y estable de la fórmula.';
COMMENT ON COLUMN RL_MR_FORMULAS.FOR_NOMBRE IS 'Nombre descriptivo de la fórmula administrable.';
COMMENT ON COLUMN RL_MR_FORMULAS.FOR_DESCRIPCION IS 'Descripción funcional del cálculo realizado por la fórmula.';
COMMENT ON COLUMN RL_MR_FORMULAS.FOR_ESTADO IS 'Estado del ciclo de vida de la fórmula maestra.';
COMMENT ON COLUMN RL_MR_FORMULAS.FOR_METADATA_JSON IS 'Metadata funcional adicional de la fórmula en formato JSON, sin código ejecutable.';
COMMENT ON COLUMN RL_MR_FORMULAS.FOR_FECHA_CREACION IS 'Fecha y hora de creación del registro maestro de fórmula.';
COMMENT ON COLUMN RL_MR_FORMULAS.FOR_USR_CREACION IS 'Usuario responsable de crear el registro maestro de fórmula.';
COMMENT ON COLUMN RL_MR_FORMULAS.FOR_VERSION_ROW IS 'Versión de concurrencia optimista del registro maestro de fórmula.';

COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_ID IS 'Identificador interno único de la versión de fórmula.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_FORMULA_ID IS 'Fórmula maestra a la que pertenece esta versión.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_VERSION IS 'Número secuencial de versión dentro de la fórmula maestra.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_EXPRESION IS 'Expresión declarativa DSL de la fórmula versionada; se almacena como dato y no se ejecuta en Oracle.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_TIPO_RESULTADO IS 'Tipo funcional del resultado producido por la fórmula.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_ESTADO IS 'Estado del ciclo de vida de la versión de fórmula.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_HASH IS 'Huella SHA-256 hexadecimal de 64 caracteres de la definición versionada.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_FECHA_INICIO IS 'Fecha de inicio de vigencia de la versión de fórmula.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_FECHA_FIN IS 'Fecha de finalización de vigencia de la versión de fórmula, cuando aplique.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_FECHA_CREACION IS 'Fecha y hora de creación de la versión de fórmula.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_USR_CREACION IS 'Usuario responsable de crear la versión de fórmula.';
COMMENT ON COLUMN RL_MR_FORMULA_VERSIONES.FOV_VERSION_ROW IS 'Versión de concurrencia optimista del registro de versión de fórmula.';

COMMENT ON COLUMN RL_MR_FORMULA_USOS.FUS_ID IS 'Identificador interno único de la relación de uso de fórmula.';
COMMENT ON COLUMN RL_MR_FORMULA_USOS.FUS_VERSION_FORMULARIO_ID IS 'Versión de formulario cuyo campo utiliza la fórmula.';
COMMENT ON COLUMN RL_MR_FORMULA_USOS.FUS_CAMPO_CLAVE IS 'Clave contractual del campo dentro de la versión del formulario.';
COMMENT ON COLUMN RL_MR_FORMULA_USOS.FUS_FORMULA_VERSION_ID IS 'Versión de fórmula asignada al campo del formulario.';
COMMENT ON COLUMN RL_MR_FORMULA_USOS.FUS_FECHA_CREACION IS 'Fecha y hora de creación de la relación de uso.';
COMMENT ON COLUMN RL_MR_FORMULA_USOS.FUS_USR_CREACION IS 'Usuario responsable de registrar la relación de uso.';

COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_ID IS 'Identificador interno único de la función administrable.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_CODIGO IS 'Código funcional único, canónico y estable de la función.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_NOMBRE IS 'Nombre descriptivo de la función administrable.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_DESCRIPCION IS 'Descripción funcional de la función dentro del motor de cálculo.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_CATEGORIA IS 'Categoría administrativa de la función.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_ESTADO IS 'Estado del ciclo de vida de la función maestra.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_METADATA_JSON IS 'Metadata funcional adicional de la función en formato JSON, sin código ejecutable.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_FECHA_CREACION IS 'Fecha y hora de creación del registro maestro de función.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_USR_CREACION IS 'Usuario responsable de crear el registro maestro de función.';
COMMENT ON COLUMN RL_MR_FUNCIONES.FUN_VERSION_ROW IS 'Versión de concurrencia optimista del registro maestro de función.';

COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_ID IS 'Identificador interno único de la versión de función.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_FUNCION_ID IS 'Función maestra a la que pertenece esta versión.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_VERSION IS 'Número secuencial de versión dentro de la función maestra.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_TIPO IS 'Tipo de implementación declarativa de la versión: NATIVA o COMPUESTA.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_TIPO_RESULTADO IS 'Tipo funcional del resultado producido por la función.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_SIGNATURE_JSON IS 'Contrato JSON versionado de tipos, aridad y argumentos de la función.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_DEFINICION_DSL IS 'Cuerpo DSL seguro de una función compuesta; se almacena como dato y no se ejecuta en Oracle.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_HANDLER_KEY IS 'Clave declarativa del manejador nativo seguro implementado por el backend.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_MIN_ARITY IS 'Cantidad mínima de argumentos admitidos por la versión de función.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_MAX_ARITY IS 'Cantidad máxima de argumentos admitidos por la versión; nulo representa aridad abierta.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_ESTADO IS 'Estado del ciclo de vida de la versión de función.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_HASH IS 'Huella SHA-256 hexadecimal de 64 caracteres del contrato versionado de función.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_FECHA_CREACION IS 'Fecha y hora de creación de la versión de función.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_USR_CREACION IS 'Usuario responsable de crear la versión de función.';
COMMENT ON COLUMN RL_MR_FUNCION_VERSIONES.FUV_VERSION_ROW IS 'Versión de concurrencia optimista del registro de versión de función.';

COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_ID IS 'Identificador interno único del argumento de función.';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_FUNCION_VERSION_ID IS 'Versión de función cuyo contrato contiene el argumento.';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_POSICION IS 'Posición ordinal del argumento dentro de la firma versionada.';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_CODIGO IS 'Código funcional único del argumento dentro de la firma.';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_NOMBRE IS 'Nombre descriptivo del argumento de función.';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_TIPO IS 'Tipo funcional esperado para el argumento.';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_REQUERIDO IS 'Indicador de argumento obligatorio (1) u opcional (0).';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_VARIADIC IS 'Indicador de argumento repetible para funciones de aridad variable (1) o fijo (0).';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_DEFAULT_JSON IS 'Valor predeterminado declarativo del argumento en formato JSON, cuando aplique.';
COMMENT ON COLUMN RL_MR_FUNCION_ARGUMENTOS.FUA_DESCRIPCION IS 'Descripción funcional del argumento de función.';

COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_ID IS 'Identificador interno único del parámetro de cálculo.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_CODIGO IS 'Código funcional único, canónico y estable del parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_NOMBRE IS 'Nombre descriptivo del parámetro de cálculo.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_DESCRIPCION IS 'Descripción funcional del parámetro utilizado por fórmulas y reglas.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_TIPO IS 'Tipo funcional del valor administrado por el parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_ESTADO IS 'Estado del ciclo de vida del parámetro maestro.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_METADATA_JSON IS 'Metadata funcional adicional del parámetro en formato JSON.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_FECHA_CREACION IS 'Fecha y hora de creación del registro maestro de parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_USR_CREACION IS 'Usuario responsable de crear el registro maestro de parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETROS_CALCULO.PAC_VERSION_ROW IS 'Versión de concurrencia optimista del registro maestro de parámetro.';

COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_ID IS 'Identificador interno único de la versión de parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_PARAMETRO_ID IS 'Parámetro maestro al que pertenece esta versión.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_VERSION IS 'Número secuencial de versión dentro del parámetro maestro.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_TIPO IS 'Tipo funcional del valor persistido en esta versión de parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_VALOR_ENTERO IS 'Valor entero tipado del parámetro, cuando el tipo es INTEGER.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_VALOR_DECIMAL IS 'Valor decimal tipado del parámetro, cuando el tipo es DECIMAL.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_VALOR_BOOLEANO IS 'Valor booleano tipado del parámetro como 1 o 0, cuando el tipo es BOOLEAN.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_VALOR_TEXTO IS 'Valor textual tipado del parámetro, cuando el tipo es TEXT.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_VALOR_FECHA IS 'Valor de fecha tipado del parámetro, cuando el tipo es DATE.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_ESTADO IS 'Estado del ciclo de vida de la versión de parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_HASH IS 'Huella SHA-256 hexadecimal de 64 caracteres del valor tipado versionado.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_FECHA_CREACION IS 'Fecha y hora de creación de la versión de parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_USR_CREACION IS 'Usuario responsable de crear la versión de parámetro.';
COMMENT ON COLUMN RL_MR_PARAMETRO_VERSIONES.PAV_VERSION_ROW IS 'Versión de concurrencia optimista del registro de versión de parámetro.';

PROMPT CORRECCION_CODIFICACION_COMENTARIOS_312_APPLIED
EXIT SUCCESS
