set echo off
set verify off
set termout on
set pagesize 0
set heading off
set feedback off
set serveroutput off
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
SELECT 'NEW_TABLES_EXPECTED=' || COUNT(*) ||
       ' TABLES_PRESENT=' || (SELECT COUNT(*) FROM user_tables u JOIN expected_tables e ON e.table_name = u.table_name)
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

WITH expected_comments AS (
    SELECT 'RL_MR_FORMULAS' table_name, UNISTR('Cat\00E1logo maestro de f\00F3rmulas administrables utilizadas en la configuraci\00F3n de c\00E1lculo de las matrices de riesgos.') comments FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES', UNISTR('Versiones inmutables de las definiciones DSL asociadas a las f\00F3rmulas administrables de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS', UNISTR('Relaciones entre versiones de f\00F3rmulas y campos de versiones de formularios donde son utilizadas.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES', UNISTR('Cat\00E1logo maestro de funciones disponibles para la configuraci\00F3n administrable del motor de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES', UNISTR('Versiones de funciones nativas o compuestas disponibles para la configuraci\00F3n del motor de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS', UNISTR('Definici\00F3n tipada y ordenada de los argumentos pertenecientes a cada versi\00F3n de funci\00F3n.') FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO', UNISTR('Cat\00E1logo maestro de par\00E1metros administrables utilizados por las f\00F3rmulas y reglas de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES', UNISTR('Versiones tipadas e hist\00F3ricamente reproducibles de los valores asociados a par\00E1metros de c\00E1lculo.') FROM dual
)
SELECT 'TABLE_COMMENT_TEXT_MATCH=' || NVL(SUM(CASE WHEN c.comments = e.comments THEN 1 ELSE 0 END), 0) || '/8 ' ||
       CASE WHEN NVL(SUM(CASE WHEN c.comments = e.comments THEN 1 ELSE 0 END), 0) = 8 THEN 'PASS' ELSE 'FAIL' END
FROM expected_comments e
LEFT JOIN user_tab_comments c ON c.table_name = e.table_name;

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
       ' COLUMN_COMMENTS_PRESENT=' || SUM(CASE WHEN cc.comments IS NOT NULL AND LENGTH(TRIM(cc.comments)) > 0 THEN 1 ELSE 0 END) ||
       ' MISSING_COLUMN_COMMENTS=' || SUM(CASE WHEN cc.comments IS NULL OR LENGTH(TRIM(cc.comments)) = 0 THEN 1 ELSE 0 END)
FROM user_tab_columns tc
JOIN expected_tables e ON e.table_name = tc.table_name
LEFT JOIN user_col_comments cc ON cc.table_name = tc.table_name AND cc.column_name = tc.column_name;

SELECT 'CORRUPTED_TABLE_COMMENTS=' || COUNT(*)
FROM user_tab_comments
WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                     'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
  AND (INSTR(comments, UNISTR('\FFFD')) > 0
       OR INSTR(comments, UNISTR('\00C3')) > 0
       OR INSTR(comments, UNISTR('\00C2')) > 0
       OR INSTR(comments, UNISTR('\00BF')) > 0);

SELECT 'CORRUPTED_COLUMN_COMMENTS=' || COUNT(*)
FROM user_col_comments
WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                     'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
  AND (INSTR(comments, UNISTR('\FFFD')) > 0
       OR INSTR(comments, UNISTR('\00C3')) > 0
       OR INSTR(comments, UNISTR('\00C2')) > 0
       OR INSTR(comments, UNISTR('\00BF')) > 0);

WITH comment_text AS (
    SELECT comments text_value
    FROM user_tab_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
    UNION ALL
    SELECT comments
    FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'U+FFFD=' || COUNT(*) FROM comment_text WHERE INSTR(text_value, UNISTR('\FFFD')) > 0;

WITH comment_text AS (
    SELECT comments text_value FROM user_tab_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
    UNION ALL
    SELECT comments FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'U+00C3=' || COUNT(*) FROM comment_text WHERE INSTR(text_value, UNISTR('\00C3')) > 0;

WITH comment_text AS (
    SELECT comments text_value FROM user_tab_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
    UNION ALL
    SELECT comments FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'U+00C2=' || COUNT(*) FROM comment_text WHERE INSTR(text_value, UNISTR('\00C2')) > 0;

WITH comment_text AS (
    SELECT comments text_value FROM user_tab_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
    UNION ALL
    SELECT comments FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'U+00BF=' || COUNT(*) FROM comment_text WHERE INSTR(text_value, UNISTR('\00BF')) > 0;

WITH comment_text AS (
    SELECT comments text_value
    FROM user_tab_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
    UNION ALL
    SELECT comments
    FROM user_col_comments
    WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                         'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
)
SELECT 'SPANISH_DIACRITICS=' ||
       CASE WHEN
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
SELECT 'TABLE_FORMAT_STANDARD=' ||
       CASE WHEN
           (SELECT COUNT(*) FROM user_tables u JOIN expected_tables t ON t.table_name = u.table_name) = 8
           AND (SELECT COUNT(*) FROM user_tab_columns c JOIN expected_tables t ON t.table_name = c.table_name) = 86
       THEN 'PASS' ELSE 'FAIL' END
FROM dual;

WITH expected_objects AS (
    SELECT 'RL_MR_FORMULAS' object_name FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES' FROM dual UNION ALL
    SELECT 'SEQ_RL_MR_FORMULAS' FROM dual UNION ALL
    SELECT 'SEQ_RL_MR_FORMULA_VERSIONES' FROM dual UNION ALL
    SELECT 'SEQ_RL_MR_FORMULA_USOS' FROM dual UNION ALL
    SELECT 'SEQ_RL_MR_FUNCIONES' FROM dual UNION ALL
    SELECT 'SEQ_RL_MR_FUNCION_VERSIONES' FROM dual UNION ALL
    SELECT 'SEQ_RL_MR_FUNCION_ARGUMENTOS' FROM dual UNION ALL
    SELECT 'SEQ_RL_MR_PARAMETROS' FROM dual UNION ALL
    SELECT 'SEQ_RL_MR_PARAMETRO_VERSIONES' FROM dual
)
SELECT 'INVALID_OBJECTS=' || COUNT(*)
FROM user_objects u
JOIN expected_objects e ON e.object_name = u.object_name
WHERE u.status <> 'VALID';

SELECT 'DISABLED_CONSTRAINTS=' || COUNT(*)
FROM user_constraints
WHERE table_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                     'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES')
  AND status = 'DISABLED';

WITH expected_tables AS (
    SELECT 'RL_MR_FORMULAS' table_name FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES' FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO' FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES' FROM dual
),
expected_comments AS (
    SELECT 'RL_MR_FORMULAS' table_name, UNISTR('Cat\00E1logo maestro de f\00F3rmulas administrables utilizadas en la configuraci\00F3n de c\00E1lculo de las matrices de riesgos.') comments FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_VERSIONES', UNISTR('Versiones inmutables de las definiciones DSL asociadas a las f\00F3rmulas administrables de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FORMULA_USOS', UNISTR('Relaciones entre versiones de f\00F3rmulas y campos de versiones de formularios donde son utilizadas.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCIONES', UNISTR('Cat\00E1logo maestro de funciones disponibles para la configuraci\00F3n administrable del motor de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_VERSIONES', UNISTR('Versiones de funciones nativas o compuestas disponibles para la configuraci\00F3n del motor de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_FUNCION_ARGUMENTOS', UNISTR('Definici\00F3n tipada y ordenada de los argumentos pertenecientes a cada versi\00F3n de funci\00F3n.') FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETROS_CALCULO', UNISTR('Cat\00E1logo maestro de par\00E1metros administrables utilizados por las f\00F3rmulas y reglas de c\00E1lculo de matrices de riesgos.') FROM dual UNION ALL
    SELECT 'RL_MR_PARAMETRO_VERSIONES', UNISTR('Versiones tipadas e hist\00F3ricamente reproducibles de los valores asociados a par\00E1metros de c\00E1lculo.') FROM dual
),
table_metrics AS (
    SELECT
        (SELECT COUNT(*) FROM user_tables u JOIN expected_tables t ON t.table_name = u.table_name) existing_tables,
        (SELECT COUNT(*) FROM expected_tables) expected_tables,
        (SELECT COUNT(*) FROM expected_tables t JOIN user_tab_comments c ON c.table_name = t.table_name AND c.comments IS NOT NULL) table_comments,
        (SELECT COUNT(*) FROM expected_comments e JOIN user_tab_comments c ON c.table_name = e.table_name AND c.comments = e.comments) table_text_match
    FROM dual
),
column_metrics AS (
    SELECT COUNT(*) column_count,
           SUM(CASE WHEN c.comments IS NOT NULL AND LENGTH(TRIM(c.comments)) > 0 THEN 1 ELSE 0 END) column_comments
    FROM user_tab_columns tc
    JOIN expected_tables t ON t.table_name = tc.table_name
    LEFT JOIN user_col_comments c ON c.table_name = tc.table_name AND c.column_name = tc.column_name
),
table_corruption AS (
    SELECT COUNT(*) corrupted_table_comments
    FROM user_tab_comments
    WHERE table_name IN (SELECT table_name FROM expected_tables)
      AND (INSTR(comments, UNISTR('\FFFD')) > 0 OR INSTR(comments, UNISTR('\00C3')) > 0
           OR INSTR(comments, UNISTR('\00C2')) > 0 OR INSTR(comments, UNISTR('\00BF')) > 0)
),
column_corruption AS (
    SELECT COUNT(*) corrupted_column_comments
    FROM user_col_comments
    WHERE table_name IN (SELECT table_name FROM expected_tables)
      AND (INSTR(comments, UNISTR('\FFFD')) > 0 OR INSTR(comments, UNISTR('\00C3')) > 0
           OR INSTR(comments, UNISTR('\00C2')) > 0 OR INSTR(comments, UNISTR('\00BF')) > 0)
),
diacritics AS (
    SELECT CASE WHEN
        EXISTS (SELECT 1 FROM user_tab_comments WHERE INSTR(comments, UNISTR('Cat\00E1logo')) > 0) AND
        EXISTS (SELECT 1 FROM user_tab_comments WHERE INSTR(comments, UNISTR('f\00F3rmulas')) > 0) AND
        EXISTS (SELECT 1 FROM user_tab_comments WHERE INSTR(comments, UNISTR('configuraci\00F3n')) > 0) AND
        EXISTS (SELECT 1 FROM user_tab_comments WHERE INSTR(comments, UNISTR('c\00E1lculo')) > 0) AND
        EXISTS (SELECT 1 FROM user_col_comments WHERE INSTR(comments, UNISTR('Definici\00F3n')) > 0) AND
        EXISTS (SELECT 1 FROM user_col_comments WHERE INSTR(comments, UNISTR('versi\00F3n')) > 0) AND
        EXISTS (SELECT 1 FROM user_col_comments WHERE INSTR(comments, UNISTR('par\00E1metros')) > 0) AND
        EXISTS (SELECT 1 FROM user_col_comments WHERE INSTR(comments, UNISTR('hist\00F3ricamente')) > 0)
      THEN 1 ELSE 0 END diacritics_ok
    FROM dual
),
object_metrics AS (
    SELECT COUNT(*) invalid_objects
    FROM user_objects
    WHERE status <> 'VALID'
      AND object_name IN ('RL_MR_FORMULAS','RL_MR_FORMULA_VERSIONES','RL_MR_FORMULA_USOS','RL_MR_FUNCIONES',
                          'RL_MR_FUNCION_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES',
                          'SEQ_RL_MR_FORMULAS','SEQ_RL_MR_FORMULA_VERSIONES','SEQ_RL_MR_FORMULA_USOS','SEQ_RL_MR_FUNCIONES',
                          'SEQ_RL_MR_FUNCION_VERSIONES','SEQ_RL_MR_FUNCION_ARGUMENTOS','SEQ_RL_MR_PARAMETROS','SEQ_RL_MR_PARAMETRO_VERSIONES')
),
constraint_metrics AS (
    SELECT COUNT(*) disabled_constraints
    FROM user_constraints
    WHERE status = 'DISABLED'
      AND table_name IN (SELECT table_name FROM expected_tables)
)
SELECT CASE WHEN
       tm.expected_tables = 8
   AND tm.existing_tables = 8
   AND tm.table_comments = 8
   AND tm.table_text_match = 8
   AND cm.column_count = 86
   AND cm.column_comments = 86
   AND tc.corrupted_table_comments = 0
   AND cc.corrupted_column_comments = 0
   AND d.diacritics_ok = 1
   AND om.invalid_objects = 0
   AND xm.disabled_constraints = 0
   THEN 'POSTCHECK_ASSERTION=PASS'
   ELSE TO_CHAR(1/0)
   END
FROM table_metrics tm
CROSS JOIN column_metrics cm
CROSS JOIN table_corruption tc
CROSS JOIN column_corruption cc
CROSS JOIN diacritics d
CROSS JOIN object_metrics om
CROSS JOIN constraint_metrics xm;

SELECT 'COMMENT_ENCODING_POSTCHECK=PASS' FROM dual;
SELECT 'ORACLE_11G_POSTCHECK=PASS' FROM dual;

EXIT
