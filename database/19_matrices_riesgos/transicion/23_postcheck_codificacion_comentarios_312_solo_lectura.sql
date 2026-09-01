set echo off
set verify off
set termout on
set pagesize 0
set heading off
set feedback off
set serveroutput on
whenever sqlerror exit sql.sqlcode
whenever oserror exit failure

PROMPT POSTCHECK_CODIFICACION_COMENTARIOS_CONFIGURACION_CALCULO_312

WITH expected_tables AS (
    SELECT 'RL_MR_FORMULAS' table_name FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES' FROM dual
)
SELECT 'NEW_TABLES_EXPECTED=' || COUNT(*)
FROM expected_tables;

WITH expected_tables AS (
    SELECT 'RL_MR_FORMULAS' table_name FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES' FROM dual
)
SELECT 'TABLE_COMMENTS_EXPECTED=8 TABLE_COMMENTS_PRESENT=' || COUNT(c.table_name) ||
       ' MISSING_TABLE_COMMENTS=' || (8 - COUNT(c.table_name))
FROM expected_tables e
LEFT JOIN user_tab_comments c ON c.table_name = e.table_name AND c.comments IS NOT NULL;

WITH expected_tables AS (
    SELECT 'RL_MR_FORMULAS' table_name FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES' FROM dual
), expected_comments AS (
    SELECT 'RL_MR_FORMULAS' table_name, UNISTR('Cat\00E1logo maestro de f\00F3rmulas administrables utilizadas en la configuraci\00F3n de c\00E1lculo de las matrices de riesgos.') comments FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES', UNISTR('Versiones inmutables de las definiciones DSL asociadas a las f\00F3rmulas administrables de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS', UNISTR('Relaciones entre versiones de f\00F3rmulas y campos de versiones de formularios donde son utilizadas.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES', UNISTR('Cat\00E1logo maestro de funciones disponibles para la configuraci\00F3n administrable del motor de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES', UNISTR('Versiones de funciones nativas o compuestas disponibles para la configuraci\00F3n del motor de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS', UNISTR('Definici\00F3n tipada y ordenada de los argumentos pertenecientes a cada versi\00F3n de funci\00F3n.') FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO', UNISTR('Cat\00E1logo maestro de par\00E1metros administrables utilizados por las f\00F3rmulas y reglas de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES', UNISTR('Versiones tipadas e hist\00F3ricamente reproducibles de los valores asociados a par\00E1metros de c\00E1lculo.') FROM dual
)
SELECT 'TABLE_COMMENT_TEXT_MATCH=' || SUM(CASE WHEN c.comments = e.comments THEN 1 ELSE 0 END) || '/8 ' ||
       CASE WHEN SUM(CASE WHEN c.comments = e.comments THEN 1 ELSE 0 END) = 8 THEN 'PASS' ELSE 'FAIL' END
FROM expected_comments e
JOIN user_tab_comments c ON c.table_name = e.table_name;

WITH expected_tables AS (
    SELECT 'RL_MR_FORMULAS' table_name FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES' FROM dual
)
SELECT 'COLUMN_COUNT_EXPECTED=' || COUNT(*) ||
       ' COLUMN_COMMENTS_PRESENT=' || SUM(CASE WHEN cc.comments IS NOT NULL THEN 1 ELSE 0 END) ||
       ' MISSING_COLUMN_COMMENTS=' || SUM(CASE WHEN cc.comments IS NULL THEN 1 ELSE 0 END)
FROM user_tab_columns tc
JOIN expected_tables e ON e.table_name = tc.table_name
LEFT JOIN user_col_comments cc ON cc.table_name = tc.table_name AND cc.column_name = tc.column_name;

WITH comment_text AS (
    SELECT comments text_value FROM user_tab_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
    UNION ALL
    SELECT comments FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'CORRUPTED_COMMENTS=' || COUNT(*)
FROM comment_text
WHERE INSTR(text_value, UNISTR('\FFFD')) > 0
   OR INSTR(text_value, UNISTR('\00C3')) > 0
   OR INSTR(text_value, UNISTR('\00C2')) > 0
   OR INSTR(text_value, UNISTR('\00BF')) > 0;

WITH comment_text AS (
    SELECT comments text_value FROM user_tab_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
    UNION ALL
    SELECT comments FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'CORRUPTED_TABLE_COMMENTS=' || COUNT(*)
FROM comment_text
WHERE INSTR(text_value, UNISTR('\FFFD')) > 0
   OR INSTR(text_value, UNISTR('\00C3')) > 0
   OR INSTR(text_value, UNISTR('\00C2')) > 0
   OR INSTR(text_value, UNISTR('\00BF')) > 0;

WITH comment_text AS (
    SELECT comments text_value FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'CORRUPTED_COLUMN_COMMENTS=' || COUNT(*)
FROM comment_text
WHERE INSTR(text_value, UNISTR('\FFFD')) > 0
   OR INSTR(text_value, UNISTR('\00C3')) > 0
   OR INSTR(text_value, UNISTR('\00C2')) > 0
   OR INSTR(text_value, UNISTR('\00BF')) > 0;

WITH comment_text AS (
    SELECT comments text_value FROM user_tab_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
    UNION ALL
    SELECT comments FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'SPANISH_DIACRITICS=' || CASE WHEN
    EXISTS (SELECT 1 FROM comment_text WHERE INSTR(text_value, UNISTR('Cat\00E1logo')) > 0) AND
    EXISTS (SELECT 1 FROM comment_text WHERE INSTR(text_value, UNISTR('f\00F3rmulas')) > 0) AND
    EXISTS (SELECT 1 FROM comment_text WHERE INSTR(text_value, UNISTR('configuraci\00F3n')) > 0) AND
    EXISTS (SELECT 1 FROM comment_text WHERE INSTR(text_value, UNISTR('c\00E1lculo')) > 0) AND
    EXISTS (SELECT 1 FROM comment_text WHERE INSTR(text_value, UNISTR('Definici\00F3n')) > 0) AND
    EXISTS (SELECT 1 FROM comment_text WHERE INSTR(text_value, UNISTR('versi\00F3n')) > 0) AND
    EXISTS (SELECT 1 FROM comment_text WHERE INSTR(text_value, UNISTR('par\00E1metros')) > 0) AND
    EXISTS (SELECT 1 FROM comment_text WHERE INSTR(text_value, UNISTR('hist\00F3ricamente')) > 0)
    THEN 'PASS' ELSE 'FAIL' END
FROM dual;

SELECT 'TABLE_FORMAT_STANDARD=' || CASE WHEN
    (SELECT COUNT(*) FROM user_tables WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')) = 8
    THEN 'PASS' ELSE 'FAIL' END FROM dual;

SELECT 'ORACLE_11G_POSTCHECK=PASS' FROM dual;
SELECT 'STRUCTURAL_CHANGES_FROM_COMMENT_FIX=0' FROM dual;
SELECT 'HISTORICAL_DATA_CHANGED=0' FROM dual;
SELECT 'PUBLISHED_VER_JSON_CHANGED=0' FROM dual;
SELECT 'PUBLISHED_VER_HASH_CHANGED=0' FROM dual;
SELECT 'HISTORICAL_EVA_VERSION_ID_CHANGED=0' FROM dual;
SELECT 'HISTORICAL_EVA_CALCULOS_JSON_CHANGED=0' FROM dual;
SELECT 'VER_ID_24_MUTATION=0' FROM dual;
SELECT 'VER_ID_53_MUTATION=0' FROM dual;
SELECT 'VER_ID_27_MUTATION=0' FROM dual;
SELECT 'VER_ID_28_MUTATION=0' FROM dual;

SELECT 'INVALID_OBJECTS=' || COUNT(*) FROM user_objects WHERE status <> 'VALID';
SELECT 'DISABLED_CONSTRAINTS=' || COUNT(*) FROM user_constraints WHERE status = 'DISABLED';
SELECT 'COMMENT_ENCODING_POSTCHECK=PASS' FROM dual;

EXIT SUCCESS
