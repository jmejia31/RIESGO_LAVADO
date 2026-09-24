-- Oracle 11g / READ ONLY. Inventario completo por celda, token contextual y ocurrencia.
-- La deteccion se ejecuta sobre el CLOB completo; SUBSTR solo se usa para contexto visual.
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
  l_bad_catalog SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_good_catalog SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_sql VARCHAR2(32767); l_pred VARCHAR2(16000); l_expr VARCHAR2(4000);
  l_tables_found NUMBER := 0; l_columns NUMBER := 0; l_cells NUMBER := 0;
  l_occurrences NUMBER := 0; l_errors NUMBER := 0;
  l_unique_tokens SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_unmapped_tokens SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_ambiguous_tokens SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_rowid VARCHAR2(30); l_value CLOB; l_ctx VARCHAR2(240); l_rc SYS_REFCURSOR;
  PROCEDURE register_mapping(p_bad VARCHAR2, p_good VARCHAR2) IS
  BEGIN
    l_bad_catalog.EXTEND; l_bad_catalog(l_bad_catalog.COUNT) := p_bad;
    l_good_catalog.EXTEND; l_good_catalog(l_good_catalog.COUNT) := p_good;
  END;
  FUNCTION mapping_count(p_bad VARCHAR2, p_good OUT VARCHAR2) RETURN NUMBER IS
    n NUMBER := 0;
  BEGIN
    p_good := NULL;
    FOR i IN 1..l_bad_catalog.COUNT LOOP
      IF l_bad_catalog(i)=p_bad THEN n:=n+1; IF p_good IS NULL THEN p_good:=l_good_catalog(i); END IF; END IF;
    END LOOP;
    RETURN n;
  END;
  PROCEDURE add_unique(p_list IN OUT NOCOPY SYS.ODCIVARCHAR2LIST, p_value VARCHAR2) IS
  BEGIN
    IF p_value IS NULL THEN RETURN; END IF;
    FOR i IN 1..p_list.COUNT LOOP IF p_list(i)=p_value THEN RETURN; END IF; END LOOP;
    p_list.EXTEND; p_list(p_list.COUNT):=p_value;
  END;
  PROCEDURE emit_token(p_table VARCHAR2,p_column VARCHAR2,p_rowid VARCHAR2,p_value CLOB,p_pattern VARCHAR2,p_occurrence NUMBER) IS
    l_token VARCHAR2(4000); l_good VARCHAR2(4000); l_mappings NUMBER; l_pos NUMBER;
  BEGIN
    l_token:=REGEXP_SUBSTR(p_value,p_pattern,1,p_occurrence); IF l_token IS NULL THEN RETURN; END IF;
    add_unique(l_unique_tokens,l_token); l_mappings:=mapping_count(l_token,l_good);
    IF l_mappings=0 THEN add_unique(l_unmapped_tokens,l_token); ELSIF l_mappings>1 THEN add_unique(l_ambiguous_tokens,l_token); END IF;
    l_pos:=REGEXP_INSTR(p_value,p_pattern,1,p_occurrence); l_ctx:=DBMS_LOB.SUBSTR(p_value,220,GREATEST(l_pos-90,1));
    DBMS_OUTPUT.PUT_LINE('TABLE_NAME='||p_table||' COLUMN_NAME='||p_column||' ROWID='||p_rowid||
      ' TOKEN_BAD='||l_token||' OCCURRENCES='||p_occurrence||' POSITION='||l_pos||' CONTEXT='||REPLACE(REPLACE(l_ctx,CHR(10),' '),CHR(13),' '));
    l_occurrences:=l_occurrences+1;
  END;
  PROCEDURE emit_pattern(p_table VARCHAR2,p_column VARCHAR2,p_rowid VARCHAR2,p_value CLOB,p_marker VARCHAR2,p_contextual BOOLEAN) IS
    l_pattern VARCHAR2(4000); l_count NUMBER;
  BEGIN
    IF p_contextual THEN l_pattern:='[[:alpha:]][[:alpha:]]*'||p_marker||'[[:alpha:]][[:alpha:]]*';
    ELSE l_pattern:='[[:alpha:][:digit:]¿'||UNISTR('\FFFD\00C3\00C2\00EF')||']*'||p_marker||'[[:alpha:][:digit:]¿'||UNISTR('\FFFD\00C3\00C2\00EF')||']*'; END IF;
    l_count:=REGEXP_COUNT(p_value,l_pattern); FOR i IN 1..l_count LOOP emit_token(p_table,p_column,p_rowid,p_value,l_pattern,i); END LOOP;
  END;
BEGIN
  @@_catalogo_unicode_modulo_matrices.sql
  SELECT COUNT(*) INTO l_tables_found FROM user_tables WHERE table_name IN (SELECT COLUMN_VALUE FROM TABLE(l_tables));
  DBMS_OUTPUT.PUT_LINE('REQUIRED_RL_MR_TABLES=25'); DBMS_OUTPUT.PUT_LINE('REQUIRED_RL_MR_TABLES_FOUND='||l_tables_found);
  FOR t IN 1..l_tables.COUNT LOOP
    FOR c IN (SELECT column_name FROM user_tab_columns WHERE table_name=l_tables(t) AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB') ORDER BY column_id) LOOP
      l_columns:=l_columns+1; l_expr:='DBMS_LOB.INSTR(TO_CLOB('||DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||'),'; l_pred:='(';
      FOR m IN 1..l_markers.COUNT LOOP
        IF m>1 THEN l_pred:=l_pred||' OR '; END IF;
        l_pred:=l_pred||l_expr||'UNISTR('''||CASE m WHEN 1 THEN '\FFFD' WHEN 2 THEN '\00EF\00BF\00BD' WHEN 3 THEN '\00C3' WHEN 4 THEN '\00C2' WHEN 5 THEN '\00E2\20AC' ELSE '\00F0\0178' END||'''))>0';
      END LOOP;
      l_pred:=l_pred||' OR REGEXP_LIKE(TO_CLOB('||DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||'),''[[:alpha:]][[:alpha:]]*''||UNISTR(''\00BF'')||''[[:alpha:]][[:alpha:]]*''))';
      BEGIN
        l_sql:='SELECT ROWIDTOCHAR(ROWID),TO_CLOB('||DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||') FROM '||DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t))||' WHERE '||l_pred; OPEN l_rc FOR l_sql;
        LOOP FETCH l_rc INTO l_rowid,l_value; EXIT WHEN l_rc%NOTFOUND; l_cells:=l_cells+1;
          FOR m IN 1..l_markers.COUNT LOOP emit_pattern(l_tables(t),c.column_name,l_rowid,l_value,l_markers(m),FALSE); END LOOP;
          emit_pattern(l_tables(t),c.column_name,l_rowid,l_value,UNISTR('\00BF'),TRUE);
        END LOOP; CLOSE l_rc;
      EXCEPTION WHEN OTHERS THEN IF l_rc%ISOPEN THEN CLOSE l_rc; END IF; l_errors:=l_errors+1; DBMS_OUTPUT.PUT_LINE('AUDIT_ERROR='||l_tables(t)||'.'||c.column_name||' '||SQLERRM); END;
    END LOOP;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_COLUMNS_SCANNED='||l_columns); DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_TABLES_SCANNED='||l_tables_found);
  DBMS_OUTPUT.PUT_LINE('FULL_MODULE_TOKEN_OCCURRENCES='||l_occurrences); DBMS_OUTPUT.PUT_LINE('TOTAL_TOKEN_OCCURRENCES='||l_occurrences);
  DBMS_OUTPUT.PUT_LINE('UNIQUE_BAD_TOKENS='||l_unique_tokens.COUNT); DBMS_OUTPUT.PUT_LINE('DETERMINISTIC_MAPPINGS='||l_bad_catalog.COUNT);
  DBMS_OUTPUT.PUT_LINE('AMBIGUOUS_TOKENS='||l_ambiguous_tokens.COUNT); DBMS_OUTPUT.PUT_LINE('UNMAPPED_TOKENS='||l_unmapped_tokens.COUNT);
  DBMS_OUTPUT.PUT_LINE('FULL_MODULE_TOKEN_INVENTORY='||CASE WHEN l_tables_found=25 AND l_errors=0 THEN 'PASS' ELSE 'FAIL' END);
END;
/
PROMPT MATRICES_TEXT_AUDIT_END
