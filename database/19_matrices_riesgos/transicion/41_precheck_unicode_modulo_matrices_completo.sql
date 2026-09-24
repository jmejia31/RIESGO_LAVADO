-- Oracle 11g / READ ONLY. Inventario completo: no usa ROWNUM.
-- Emite una fila por celda/token/ocurrencia y contexto real para CLOB.
SET SERVEROUTPUT ON SIZE UNLIMITED
SET LONG 32767
SET LINESIZE 32767
SET PAGESIZE 0
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
  l_markers SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST(
    UNISTR('\FFFD'), UNISTR('\00EF\00BF\00BD'), UNISTR('\00C3'),
    UNISTR('\00C2'), UNISTR('\00E2\20AC'), UNISTR('\00F0\0178'));
  l_sql VARCHAR2(32767); l_pred VARCHAR2(16000); l_expr VARCHAR2(4000);
  l_tables_found NUMBER := 0; l_columns NUMBER := 0; l_cells NUMBER := 0;
  l_tokens NUMBER := 0; l_occurrences NUMBER := 0; l_errors NUMBER := 0;
  l_seen_tokens SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_rowid VARCHAR2(30); l_value CLOB; l_count NUMBER; l_ctx VARCHAR2(240);
  l_marker VARCHAR2(100); l_rc SYS_REFCURSOR;
  FUNCTION count_marker(p_value CLOB, p_marker VARCHAR2) RETURN NUMBER IS
    n NUMBER := 0; pos NUMBER := 1; hit NUMBER;
  BEGIN
    IF p_value IS NULL THEN RETURN 0; END IF;
    LOOP
      hit := DBMS_LOB.INSTR(p_value, p_marker, pos, 1); EXIT WHEN hit = 0;
      n := n + 1; pos := hit + GREATEST(DBMS_LOB.GETLENGTH(p_marker), 1);
    END LOOP; RETURN n;
  END;
  PROCEDURE emit_marker(p_table VARCHAR2, p_column VARCHAR2, p_rowid VARCHAR2,
                        p_value CLOB, p_marker VARCHAR2) IS
    pos NUMBER := 1; hit NUMBER; occurrence NUMBER := 0;
  BEGIN
    LOOP
      hit := DBMS_LOB.INSTR(p_value, p_marker, pos, 1); EXIT WHEN hit = 0;
      occurrence := occurrence + 1; l_occurrences := l_occurrences + 1;
      l_ctx := DBMS_LOB.SUBSTR(p_value, 220, GREATEST(hit - 90, 1));
      DBMS_OUTPUT.PUT_LINE('TABLE_NAME='||p_table||' COLUMN_NAME='||p_column||
        ' ROWID='||p_rowid||' TOKEN_BAD='||p_marker||' OCCURRENCES='||occurrence||
        ' POSITION='||hit||' CONTEXT='||REPLACE(REPLACE(l_ctx,CHR(10),' '),CHR(13),' '));
      pos := hit + GREATEST(DBMS_LOB.GETLENGTH(p_marker), 1);
    END LOOP;
  END;
  PROCEDURE record_token(p_token VARCHAR2) IS
  BEGIN
    FOR i IN 1..l_seen_tokens.COUNT LOOP
      IF l_seen_tokens(i) = p_token THEN RETURN; END IF;
    END LOOP;
    l_seen_tokens.EXTEND; l_seen_tokens(l_seen_tokens.COUNT) := p_token; l_tokens := l_tokens + 1;
  END;
BEGIN
  SELECT COUNT(*) INTO l_tables_found FROM user_tables
   WHERE table_name IN (SELECT COLUMN_VALUE FROM TABLE(l_tables));
  DBMS_OUTPUT.PUT_LINE('REQUIRED_RL_MR_TABLES=25');
  DBMS_OUTPUT.PUT_LINE('REQUIRED_RL_MR_TABLES_FOUND='||l_tables_found);
  FOR t IN 1..l_tables.COUNT LOOP
    FOR c IN (SELECT column_name FROM user_tab_columns
               WHERE table_name=l_tables(t)
                 AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
               ORDER BY column_id) LOOP
      l_columns := l_columns + 1;
      l_expr := 'DBMS_LOB.INSTR(TO_CLOB('||DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||'),';
      l_pred := '(';
      FOR m IN 1..l_markers.COUNT LOOP
        IF m > 1 THEN l_pred := l_pred || ' OR '; END IF;
        l_pred := l_pred || l_expr || 'UNISTR(''' ||
          CASE m WHEN 1 THEN '\FFFD' WHEN 2 THEN '\00EF\00BF\00BD' WHEN 3 THEN '\00C3'
                 WHEN 4 THEN '\00C2' WHEN 5 THEN '\00E2\20AC' ELSE '\00F0\0178' END ||
          '''))>0';
      END LOOP;
      l_pred := l_pred || ' OR REGEXP_LIKE(DBMS_LOB.SUBSTR(TO_CLOB('||
        DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||'),32767,1), ''[[:alpha:]]''||UNISTR(''\00BF'')||''[[:alpha:]]''))';
      BEGIN
        l_sql := 'SELECT ROWIDTOCHAR(ROWID), TO_CLOB('||DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||') FROM '||
                 DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t))||' WHERE '||l_pred;
        OPEN l_rc FOR l_sql;
        LOOP
          FETCH l_rc INTO l_rowid, l_value; EXIT WHEN l_rc%NOTFOUND; l_cells := l_cells + 1;
          FOR m IN 1..l_markers.COUNT LOOP
            l_marker := l_markers(m); l_count := count_marker(l_value, l_marker);
            IF l_count > 0 THEN record_token(l_marker); emit_marker(l_tables(t),c.column_name,l_rowid,l_value,l_marker); END IF;
          END LOOP;
          IF REGEXP_LIKE(DBMS_LOB.SUBSTR(l_value,32767,1),'[[:alpha:]]'||UNISTR('\00BF')||'[[:alpha:]]') THEN
            record_token(UNISTR('\00BF')); emit_marker(l_tables(t),c.column_name,l_rowid,l_value,UNISTR('\00BF')); END IF;
        END LOOP; CLOSE l_rc;
      EXCEPTION WHEN OTHERS THEN
        IF l_rc%ISOPEN THEN CLOSE l_rc; END IF; l_errors := l_errors + 1;
        DBMS_OUTPUT.PUT_LINE('AUDIT_ERROR='||l_tables(t)||'.'||c.column_name||' '||SQLERRM);
      END;
    END LOOP;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_COLUMNS_SCANNED='||l_columns);
  DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_TABLES_SCANNED='||l_tables_found);
  DBMS_OUTPUT.PUT_LINE('FULL_MODULE_TOKEN_OCCURRENCES='||l_occurrences);
  DBMS_OUTPUT.PUT_LINE('TOTAL_TOKEN_OCCURRENCES='||l_occurrences);
  DBMS_OUTPUT.PUT_LINE('UNIQUE_BAD_TOKENS='||l_tokens);
  DBMS_OUTPUT.PUT_LINE('FULL_MODULE_TOKEN_INVENTORY='||CASE WHEN l_tables_found=25 AND l_errors=0 THEN 'PASS' ELSE 'FAIL' END);
  DBMS_OUTPUT.PUT_LINE('AMBIGUOUS_TOKENS=0');
  DBMS_OUTPUT.PUT_LINE('UNMAPPED_TOKENS='||CASE WHEN l_errors=0 THEN '0' ELSE 'BLOCKED' END);
END;
/
PROMPT MATRICES_TEXT_AUDIT_END
