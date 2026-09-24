-- Oracle 11g / READ ONLY.
-- Inventario y detección contextual en las 25 tablas RL_MR. No modifica datos.
SET SERVEROUTPUT ON SIZE UNLIMITED
SET LINESIZE 260
SET PAGESIZE 500
DECLARE
  l_tables SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST(
    'RL_MR_FAMILIAS_FORMULARIO','RL_MR_VERSIONES_FORMULARIO','RL_MR_CATALOGOS',
    'RL_MR_ELEMENTOS_CATALOGO','RL_MR_REGLAS_CALCULO','RL_MR_RIESGOS',
    'RL_MR_EVALUACIONES_RIESGO','RL_MR_PROYECCIONES_EVALUACION','RL_MR_FLUJOS_EVALUACION',
    'RL_MR_CONTROLES_RIESGO','RL_MR_EVALUACIONES_CONTROL','RL_MR_PLANES',
    'RL_MR_ACTIVIDADES','RL_MR_EVIDENCIAS','RL_MR_EVIDENCIAS_VINCULOS',
    'RL_MR_SENALES_ALERTA','RL_MR_AUTOMONITOREO','RL_MR_FORMULAS',
    'RL_MR_FORMULA_USOS','RL_MR_FORMULA_VERSIONES','RL_MR_FUNCION_ARGUMENTOS',
    'RL_MR_FUNCIONES','RL_MR_FUNCION_VERSIONES','RL_MR_PARAMETROS_CALCULO',
    'RL_MR_PARAMETRO_VERSIONES');
  l_sql VARCHAR2(32767);
  l_expr VARCHAR2(4000);
  l_pred VARCHAR2(12000);
  l_rows NUMBER;
  l_suspicious NUMBER;
  l_columns NUMBER := 0;
  l_suspicious_columns NUMBER := 0;
  l_suspicious_rows NUMBER := 0;
  l_failed NUMBER := 0;
  l_sample VARCHAR2(240);
BEGIN
  DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_AUDIT_BEGIN');
  FOR t IN 1..l_tables.COUNT LOOP
    FOR c IN (SELECT column_name, data_type
                FROM user_tab_columns
               WHERE table_name = l_tables(t)
                 AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
               ORDER BY column_id) LOOP
      l_columns := l_columns + 1;
      IF c.data_type = 'CLOB' THEN
        l_expr := 'DBMS_LOB.INSTR(' || DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ',';
      ELSE
        l_expr := 'INSTR(' || DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ',';
      END IF;
      l_pred := '(' || l_expr || 'UNISTR(''\FFFD'')) > 0 OR ' ||
                l_expr || 'UNISTR(''\00EF\00BF\00BD'')) > 0 OR ' ||
                l_expr || 'UNISTR(''\00C3'')) > 0 OR ' ||
                l_expr || 'UNISTR(''\00C2'')) > 0 OR ' ||
                l_expr || 'UNISTR(''\00E2\20AC'')) > 0 OR ' ||
                l_expr || 'UNISTR(''\00F0\0178'')) > 0 OR ' ||
                'REGEXP_LIKE(' || DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) ||
                ', ''[[:alpha:]]'' || UNISTR(''\00BF'') || ''[[:alpha:]]'')';
      BEGIN
        l_sql := 'SELECT COUNT(*) FROM ' || DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t));
        EXECUTE IMMEDIATE l_sql INTO l_rows;
        l_sql := 'SELECT COUNT(*) FROM ' || DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t)) ||
                 ' WHERE ' || l_pred;
        EXECUTE IMMEDIATE l_sql INTO l_suspicious;
        IF l_suspicious > 0 THEN
          l_suspicious_columns := l_suspicious_columns + 1;
          l_suspicious_rows := l_suspicious_rows + l_suspicious;
          l_sql := 'SELECT ' || CASE WHEN c.data_type = 'CLOB'
                   THEN 'DBMS_LOB.SUBSTR(' || DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ',240,1)'
                   ELSE 'SUBSTR(' || DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ',1,240)' END ||
                   ' FROM ' || DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t)) ||
                   ' WHERE ' || l_pred || ' AND ROWNUM = 1';
          EXECUTE IMMEDIATE l_sql INTO l_sample;
          DBMS_OUTPUT.PUT_LINE('TABLE_NAME=' || l_tables(t) || ' COLUMN_NAME=' ||
            c.column_name || ' DATA_TYPE=' || c.data_type || ' TOTAL_ROWS=' ||
            l_rows || ' SUSPICIOUS_ROWS=' || l_suspicious || ' TOKENS/VALORES=' ||
            REPLACE(l_sample, CHR(10), ' '));
        END IF;
      EXCEPTION WHEN OTHERS THEN
        l_failed := l_failed + 1;
        DBMS_OUTPUT.PUT_LINE('AUDIT_ERROR=' || l_tables(t) || '.' || c.column_name || ' ' || SQLERRM);
      END;
    END LOOP;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_COLUMNS_SCANNED=' || l_columns);
  DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_TABLES_SCANNED=25');
  DBMS_OUTPUT.PUT_LINE('MATRICES_SUSPICIOUS_COLUMNS=' || l_suspicious_columns);
  DBMS_OUTPUT.PUT_LINE('MATRICES_SUSPICIOUS_ROWS=' || l_suspicious_rows);
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_PRECHECK_STATUS=' ||
    CASE WHEN l_failed = 0 THEN 'PASS' ELSE 'FAIL' END);
END;
/
PROMPT MATRICES_TEXT_AUDIT_END
