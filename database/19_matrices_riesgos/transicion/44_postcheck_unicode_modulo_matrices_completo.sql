-- Oracle 11g / READ ONLY. Ejecutar después de 43.
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
  l_sql VARCHAR2(32767); l_expr VARCHAR2(4000); l_pred VARCHAR2(12000);
  l_total NUMBER := 0; l_bad_cols NUMBER := 0; l_bad_rows NUMBER := 0;
  l_fail NUMBER := 0; l_rows NUMBER; l_suspicious NUMBER;
  l_evaluations NUMBER; l_projections NUMBER; l_parity NUMBER;
  l_duplicates NUMBER; l_orphan_evaluations NUMBER; l_orphan_projections NUMBER;
  l_invalid_objects NUMBER; l_disabled_constraints NUMBER; l_null_risk NUMBER;
BEGIN
  FOR t IN 1..l_tables.COUNT LOOP
    FOR c IN (SELECT column_name, data_type FROM user_tab_columns
               WHERE table_name=l_tables(t)
                 AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
               ORDER BY column_id) LOOP
      l_total := l_total + 1;
      IF c.data_type='CLOB' THEN l_expr := 'DBMS_LOB.INSTR('||c.column_name||',';
      ELSE l_expr := 'INSTR('||c.column_name||',');
      END IF;
      l_pred := '('||l_expr||'UNISTR(''\FFFD''))>0 OR '||
        l_expr||'UNISTR(''\00EF\00BF\00BD''))>0 OR '||
        l_expr||'UNISTR(''\00C3''))>0 OR '||
        l_expr||'UNISTR(''\00C2''))>0 OR '||
        l_expr||'UNISTR(''\00E2\20AC''))>0 OR '||
        l_expr||'UNISTR(''\00F0\0178''))>0 OR REGEXP_LIKE('||
        c.column_name||',''[[:alpha:]]''||UNISTR(''\00BF'')||''[[:alpha:]]'')';
      BEGIN
        l_sql := 'SELECT COUNT(*) FROM '||l_tables(t)||' WHERE '||l_pred;
        EXECUTE IMMEDIATE l_sql INTO l_suspicious;
        IF l_suspicious>0 THEN l_bad_cols:=l_bad_cols+1; l_bad_rows:=l_bad_rows+l_suspicious; END IF;
      EXCEPTION WHEN OTHERS THEN l_fail:=l_fail+1; DBMS_OUTPUT.PUT_LINE('AUDIT_ERROR='||l_tables(t)||'.'||c.column_name||' '||SQLERRM);
      END;
    END LOOP;
  END LOOP;
  SELECT COUNT(*) INTO l_rows FROM RL_MR_RIESGOS;
  DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_COLUMNS_SCANNED='||l_total);
  DBMS_OUTPUT.PUT_LINE('MATRICES_TEXT_TABLES_SCANNED=25');
  DBMS_OUTPUT.PUT_LINE('MATRICES_SUSPICIOUS_COLUMNS='||l_bad_cols);
  DBMS_OUTPUT.PUT_LINE('MATRICES_SUSPICIOUS_ROWS='||l_bad_rows);
  SELECT COUNT(*) INTO l_evaluations FROM RL_MR_EVALUACIONES_RIESGO;
  SELECT COUNT(*) INTO l_projections FROM RL_MR_PROYECCIONES_EVALUACION;
  SELECT COUNT(*) INTO l_duplicates FROM (SELECT RIE_CODIGO FROM RL_MR_RIESGOS GROUP BY RIE_CODIGO HAVING COUNT(*)>1);
  SELECT COUNT(*) INTO l_orphan_evaluations FROM RL_MR_EVALUACIONES_RIESGO e
   WHERE NOT EXISTS (SELECT 1 FROM RL_MR_RIESGOS r WHERE r.RIE_ID=e.EVA_RIESGO_ID);
  SELECT COUNT(*) INTO l_orphan_projections FROM RL_MR_PROYECCIONES_EVALUACION p
   WHERE NOT EXISTS (SELECT 1 FROM RL_MR_EVALUACIONES_RIESGO e WHERE e.EVA_ID=p.PROY_EVALUACION_ID);
  SELECT COUNT(*) INTO l_invalid_objects FROM user_objects
   WHERE object_name LIKE 'RL_MR_%' AND status <> 'VALID';
  SELECT COUNT(*) INTO l_disabled_constraints FROM user_constraints
   WHERE table_name LIKE 'RL_MR_%' AND status <> 'ENABLED';
  SELECT COUNT(*) INTO l_null_risk FROM RL_MR_RIESGOS
   WHERE RIE_CODIGO IS NULL OR RIE_NOMBRE IS NULL OR RIE_DESCRIPCION IS NULL;
  SELECT COUNT(*) INTO l_parity FROM RL_MR_EVALUACIONES_RIESGO e
   JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID=e.EVA_ID
   WHERE DBMS_LOB.INSTR(e.EVA_DATOS_JSON, p.PROY_AREA_PRINCIPAL)=0
      OR DBMS_LOB.INSTR(e.EVA_DATOS_JSON, p.PROY_DUENO_RIESGO)=0
      OR DBMS_LOB.INSTR(e.EVA_DATOS_JSON, p.PROY_RESPUESTA_RIESGO)=0;
  DBMS_OUTPUT.PUT_LINE('RISK_ROWS='||l_rows);
  DBMS_OUTPUT.PUT_LINE('EVALUATION_ROWS='||l_evaluations);
  DBMS_OUTPUT.PUT_LINE('PROJECTION_ROWS='||l_projections);
  DBMS_OUTPUT.PUT_LINE('RISK_DUPLICATES='||l_duplicates);
  DBMS_OUTPUT.PUT_LINE('ORPHAN_EVALUATIONS='||l_orphan_evaluations);
  DBMS_OUTPUT.PUT_LINE('ORPHAN_PROJECTIONS='||l_orphan_projections);
  DBMS_OUTPUT.PUT_LINE('NULL_UNEXPECTED_ROWS='||l_null_risk);
  DBMS_OUTPUT.PUT_LINE('INVALID_OBJECTS='||l_invalid_objects);
  DBMS_OUTPUT.PUT_LINE('DISABLED_CONSTRAINTS='||l_disabled_constraints);
  DBMS_OUTPUT.PUT_LINE('PROJECTION_JSON_PARITY='||CASE WHEN l_parity=0 THEN 'PASS' ELSE 'FAIL' END);
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_RESIDUAL='||l_bad_rows);
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_POSTCHECK_STATUS='||
    CASE WHEN l_fail=0 AND l_rows=59 AND l_evaluations=59 AND l_projections=59 AND
      l_bad_cols=0 AND l_bad_rows=0 AND l_parity=0 AND l_duplicates=0 AND
      l_orphan_evaluations=0 AND l_orphan_projections=0 AND l_null_risk=0 AND
      l_invalid_objects=0 AND l_disabled_constraints=0 THEN 'PASS' ELSE 'FAIL' END);
END;
/
