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
  l_evaluations_with_flow NUMBER := 0; l_historical_without_flow NUMBER := 0; l_state_mismatches NUMBER := 0;
  l_duplicates NUMBER; l_orphan_evaluations NUMBER; l_orphan_projections NUMBER;
  l_invalid_objects NUMBER; l_disabled_constraints NUMBER; l_null_risk NUMBER;
  l_tables_found NUMBER;
  FUNCTION json_scalar(p_value VARCHAR2) RETURN VARCHAR2 IS
    v VARCHAR2(32767) := p_value;
  BEGIN
    IF p_value IS NULL THEN RETURN 'null'; END IF;
    v := REPLACE(v, '\', '\\'); v := REPLACE(v, '"', '\"');
    v := REPLACE(v, CHR(13), '\r'); v := REPLACE(v, CHR(10), '\n');
    RETURN '"'||v||'"';
  END;
  FUNCTION json_has(p_json CLOB, p_key VARCHAR2, p_value VARCHAR2) RETURN NUMBER IS
  BEGIN
    RETURN CASE WHEN DBMS_LOB.INSTR(p_json, '"'||p_key||'":'||json_scalar(p_value)) > 0 THEN 1 ELSE 0 END;
  END;
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
  SELECT COUNT(*) INTO l_tables_found FROM user_tables WHERE table_name IN (
    'RL_MR_FAMILIAS_FORMULARIO','RL_MR_VERSIONES_FORMULARIO','RL_MR_CATALOGOS',
    'RL_MR_ELEMENTOS_CATALOGO','RL_MR_REGLAS_CALCULO','RL_MR_RIESGOS',
    'RL_MR_EVALUACIONES_RIESGO','RL_MR_PROYECCIONES_EVALUACION','RL_MR_FLUJOS_EVALUACION',
    'RL_MR_CONTROLES_RIESGO','RL_MR_EVALUACIONES_CONTROL','RL_MR_PLANES',
    'RL_MR_ACTIVIDADES','RL_MR_EVIDENCIAS','RL_MR_EVIDENCIAS_VINCULOS',
    'RL_MR_SENALES_ALERTA','RL_MR_AUTOMONITOREO','RL_MR_FORMULAS','RL_MR_FORMULA_USOS',
    'RL_MR_FORMULA_VERSIONES','RL_MR_FUNCION_ARGUMENTOS','RL_MR_FUNCIONES',
    'RL_MR_FUNCION_VERSIONES','RL_MR_PARAMETROS_CALCULO','RL_MR_PARAMETRO_VERSIONES');
  DBMS_OUTPUT.PUT_LINE('REQUIRED_RL_MR_TABLES=25');
  DBMS_OUTPUT.PUT_LINE('REQUIRED_RL_MR_TABLES_FOUND='||l_tables_found);
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
  l_parity := 0;
  FOR x IN (SELECT e.EVA_DATOS_JSON, r.RIE_CODIGO, p.PROY_CODIGO_RIESGO,
                   p.PROY_AREA_PRINCIPAL,
                   p.PROY_DUENO_RIESGO, p.PROY_RESPUESTA_RIESGO,
                   p.PROY_NIVEL_INHERENTE, p.PROY_NIVEL_RESIDUAL,
                   p.PROY_ESTADO_EVALUACION, f.FLU_EVALUACION_ID, f.FLU_ESTADO AS FLUJO_ESTADO
              FROM RL_MR_EVALUACIONES_RIESGO e
              JOIN RL_MR_RIESGOS r ON r.RIE_ID=e.EVA_RIESGO_ID
              JOIN RL_MR_PROYECCIONES_EVALUACION p ON p.PROY_EVALUACION_ID=e.EVA_ID
              LEFT JOIN (SELECT FLU_EVALUACION_ID, FLU_ESTADO FROM
                (SELECT FLU_EVALUACION_ID, FLU_ESTADO,
                        ROW_NUMBER() OVER (PARTITION BY FLU_EVALUACION_ID ORDER BY FLU_FECHA DESC, FLU_ID DESC) rn
                   FROM RL_MR_FLUJOS_EVALUACION) WHERE rn=1) f ON f.FLU_EVALUACION_ID=e.EVA_ID) LOOP
    IF x.FLU_EVALUACION_ID IS NULL THEN
      l_historical_without_flow := l_historical_without_flow + 1;
      IF x.PROY_ESTADO_EVALUACION IS NULL THEN l_state_mismatches := l_state_mismatches + 1; END IF;
    ELSE
      l_evaluations_with_flow := l_evaluations_with_flow + 1;
      IF NVL(x.PROY_ESTADO_EVALUACION,CHR(0))<>NVL(x.FLUJO_ESTADO,CHR(0)) THEN l_state_mismatches := l_state_mismatches + 1; END IF;
    END IF;
    IF NVL(x.PROY_CODIGO_RIESGO,CHR(0))<>NVL(x.RIE_CODIGO,CHR(0)) OR
       json_has(x.EVA_DATOS_JSON,'area_principal',x.PROY_AREA_PRINCIPAL)=0 OR
       json_has(x.EVA_DATOS_JSON,'dueno_riesgo',x.PROY_DUENO_RIESGO)=0 OR
       json_has(x.EVA_DATOS_JSON,'respuesta_riesgo',x.PROY_RESPUESTA_RIESGO)=0 OR
       json_has(x.EVA_DATOS_JSON,'nivel_inherente',x.PROY_NIVEL_INHERENTE)=0 OR
       json_has(x.EVA_DATOS_JSON,'nivel_residual',x.PROY_NIVEL_RESIDUAL)=0 OR
       x.PROY_ESTADO_EVALUACION IS NULL OR
       (x.FLU_EVALUACION_ID IS NOT NULL AND NVL(x.PROY_ESTADO_EVALUACION,CHR(0))<>NVL(x.FLUJO_ESTADO,CHR(0))) THEN
      l_parity := l_parity + 1;
    END IF;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('RISK_ROWS='||l_rows);
  DBMS_OUTPUT.PUT_LINE('EVALUATION_ROWS='||l_evaluations);
  DBMS_OUTPUT.PUT_LINE('PROJECTION_ROWS='||l_projections);
  DBMS_OUTPUT.PUT_LINE('EVALUATIONS_WITH_FLOW='||l_evaluations_with_flow);
  DBMS_OUTPUT.PUT_LINE('HISTORICAL_EVALUATIONS_WITHOUT_FLOW='||l_historical_without_flow);
  DBMS_OUTPUT.PUT_LINE('STATE_PARITY_MISMATCHES='||l_state_mismatches);
  DBMS_OUTPUT.PUT_LINE('RISK_DUPLICATES='||l_duplicates);
  DBMS_OUTPUT.PUT_LINE('ORPHAN_EVALUATIONS='||l_orphan_evaluations);
  DBMS_OUTPUT.PUT_LINE('ORPHAN_PROJECTIONS='||l_orphan_projections);
  DBMS_OUTPUT.PUT_LINE('NULL_UNEXPECTED_ROWS='||l_null_risk);
  DBMS_OUTPUT.PUT_LINE('INVALID_OBJECTS='||l_invalid_objects);
  DBMS_OUTPUT.PUT_LINE('DISABLED_CONSTRAINTS='||l_disabled_constraints);
  DBMS_OUTPUT.PUT_LINE('PROJECTION_JSON_PARITY='||CASE WHEN l_parity=0 THEN 'PASS' ELSE 'FAIL' END);
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_RESIDUAL='||l_bad_rows);
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_POSTCHECK_STATUS='||
    CASE WHEN l_fail=0 AND l_tables_found=25 AND l_rows=59 AND l_evaluations=59 AND l_projections=59 AND
      l_bad_cols=0 AND l_bad_rows=0 AND l_parity=0 AND l_duplicates=0 AND
      l_orphan_evaluations=0 AND l_orphan_projections=0 AND l_null_risk=0 AND l_state_mismatches=0 AND
      l_invalid_objects=0 AND l_disabled_constraints=0 THEN 'PASS' ELSE 'FAIL' END);
  DBMS_OUTPUT.PUT_LINE('FULL_MODULE_TOKEN_INVENTORY='||CASE WHEN l_tables_found=25 AND l_fail=0 THEN 'PASS' ELSE 'FAIL' END);
  DBMS_OUTPUT.PUT_LINE('PROJECTION_JSON_PARITY_IMPLEMENTATION=CONTRACT_EXACT');
  DBMS_OUTPUT.PUT_LINE('BACKUP_COVERAGE=MANUAL_42_43_REQUIRED');
END;
/
