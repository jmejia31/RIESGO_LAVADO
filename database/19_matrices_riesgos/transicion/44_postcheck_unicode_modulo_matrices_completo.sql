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
  l_parity_bad_rows NUMBER := 0; l_parity_bad_codigo NUMBER := 0;
  l_parity_bad_area NUMBER := 0; l_parity_bad_dueno NUMBER := 0;
  l_parity_bad_respuesta NUMBER := 0; l_parity_bad_inherente NUMBER := 0;
  l_parity_bad_residual NUMBER := 0;
  l_evaluations_with_flow NUMBER := 0; l_historical_without_flow NUMBER := 0; l_state_mismatches NUMBER := 0;
  l_duplicates NUMBER; l_orphan_evaluations NUMBER; l_orphan_projections NUMBER;
  l_invalid_objects NUMBER; l_disabled_constraints NUMBER; l_null_risk NUMBER;
  l_tables_found NUMBER;
  FUNCTION json_scalar(p_value VARCHAR2) RETURN VARCHAR2 IS
    v VARCHAR2(32767) := p_value;
  BEGIN
    IF p_value IS NULL THEN RETURN 'null'; END IF;
    v := REPLACE(v, '\', '\\');
    v := REPLACE(v, '"', '\"');
    v := REPLACE(v, CHR(13), '\r');
    v := REPLACE(v, CHR(10), '\n');
    RETURN '"' || v || '"';
  END;
  FUNCTION json_scalar_escaped(p_value VARCHAR2) RETURN VARCHAR2 IS
    v_res VARCHAR2(32767) := '';
    ch VARCHAR2(10);
    asch VARCHAR2(30);
    len NUMBER;
  BEGIN
    IF p_value IS NULL THEN RETURN 'null'; END IF;
    len := LENGTH(p_value);
    FOR i IN 1..len LOOP
      ch := SUBSTR(p_value, i, 1);
      IF ch = '\' THEN
        v_res := v_res || '\\';
      ELSIF ch = '"' THEN
        v_res := v_res || '\"';
      ELSIF ch = CHR(13) THEN
        v_res := v_res || '\r';
      ELSIF ch = CHR(10) THEN
        v_res := v_res || '\n';
      ELSIF ch = CHR(9) THEN
        v_res := v_res || '\t';
      ELSIF ASCII(ch) < 32 THEN
        asch := TRIM(TO_CHAR(ASCII(ch), '0XXX'));
        v_res := v_res || '\u' || LPAD(asch, 4, '0');
      ELSE
        asch := ASCIISTR(ch);
        IF SUBSTR(asch, 1, 1) = '\' THEN
          v_res := v_res || REPLACE(asch, '\', '\u');
        ELSE
          v_res := v_res || ch;
        END IF;
      END IF;
    END LOOP;
    RETURN '"' || v_res || '"';
  END;
  FUNCTION json_has(p_json CLOB, p_key VARCHAR2, p_value VARCHAR2) RETURN NUMBER IS
    v_lit VARCHAR2(32767);
    v_esc VARCHAR2(32767);
    v_esc_low VARCHAR2(32767);
    pos NUMBER;
    hex_part VARCHAR2(4);
  BEGIN
    IF p_value IS NULL THEN
      RETURN CASE WHEN DBMS_LOB.INSTR(p_json, '"' || p_key || '":null') > 0 THEN 1 ELSE 0 END;
    END IF;
    v_lit := json_scalar(p_value);
    IF DBMS_LOB.INSTR(p_json, '"' || p_key || '":' || v_lit) > 0 THEN
      RETURN 1;
    END IF;
    v_esc := json_scalar_escaped(p_value);
    IF v_esc <> v_lit THEN
      IF DBMS_LOB.INSTR(p_json, '"' || p_key || '":' || v_esc) > 0 THEN
        RETURN 1;
      END IF;
      v_esc_low := v_esc;
      pos := 1;
      LOOP
        pos := INSTR(v_esc_low, '\u', pos);
        EXIT WHEN pos = 0;
        hex_part := SUBSTR(v_esc_low, pos + 2, 4);
        v_esc_low := SUBSTR(v_esc_low, 1, pos + 1) || LOWER(hex_part) || SUBSTR(v_esc_low, pos + 6);
        pos := pos + 6;
      END LOOP;
      IF v_esc_low <> v_esc AND DBMS_LOB.INSTR(p_json, '"' || p_key || '":' || v_esc_low) > 0 THEN
        RETURN 1;
      END IF;
    END IF;
    RETURN 0;
  END;
  @@_predicado_unicode_sospechoso.sql
BEGIN
  FOR t IN 1..l_tables.COUNT LOOP
    FOR c IN (SELECT column_name, data_type FROM user_tab_columns
               WHERE table_name=l_tables(t)
                 AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
               ORDER BY column_id) LOOP
      l_total := l_total + 1;
      l_pred := suspicious_predicate(c.column_name);
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
    DECLARE
      v_row_bad BOOLEAN := FALSE;
    BEGIN
      IF NVL(x.PROY_CODIGO_RIESGO,CHR(0))<>NVL(x.RIE_CODIGO,CHR(0)) THEN
        l_parity_bad_codigo := l_parity_bad_codigo + 1;
        v_row_bad := TRUE;
      END IF;
      IF json_has(x.EVA_DATOS_JSON,'area_principal',x.PROY_AREA_PRINCIPAL)=0 THEN
        l_parity_bad_area := l_parity_bad_area + 1;
        v_row_bad := TRUE;
      END IF;
      IF json_has(x.EVA_DATOS_JSON,'dueno_riesgo',x.PROY_DUENO_RIESGO)=0 THEN
        l_parity_bad_dueno := l_parity_bad_dueno + 1;
        v_row_bad := TRUE;
      END IF;
      IF json_has(x.EVA_DATOS_JSON,'respuesta_riesgo',x.PROY_RESPUESTA_RIESGO)=0 THEN
        l_parity_bad_respuesta := l_parity_bad_respuesta + 1;
        v_row_bad := TRUE;
      END IF;
      IF json_has(x.EVA_DATOS_JSON,'nivel_inherente',x.PROY_NIVEL_INHERENTE)=0 THEN
        l_parity_bad_inherente := l_parity_bad_inherente + 1;
        v_row_bad := TRUE;
      END IF;
      IF json_has(x.EVA_DATOS_JSON,'nivel_residual',x.PROY_NIVEL_RESIDUAL)=0 THEN
        l_parity_bad_residual := l_parity_bad_residual + 1;
        v_row_bad := TRUE;
      END IF;
      IF x.PROY_ESTADO_EVALUACION IS NULL OR
         (x.FLU_EVALUACION_ID IS NOT NULL AND NVL(x.PROY_ESTADO_EVALUACION,CHR(0))<>NVL(x.FLUJO_ESTADO,CHR(0))) THEN
        v_row_bad := TRUE;
      END IF;
      IF v_row_bad THEN
        l_parity_bad_rows := l_parity_bad_rows + 1;
        l_parity := l_parity + 1;
      END IF;
    END;
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
  DBMS_OUTPUT.PUT_LINE('PARITY_BAD_ROWS='||l_parity_bad_rows);
  DBMS_OUTPUT.PUT_LINE('PARITY_BAD_CODIGO='||l_parity_bad_codigo);
  DBMS_OUTPUT.PUT_LINE('PARITY_BAD_AREA='||l_parity_bad_area);
  DBMS_OUTPUT.PUT_LINE('PARITY_BAD_DUENO='||l_parity_bad_dueno);
  DBMS_OUTPUT.PUT_LINE('PARITY_BAD_RESPUESTA='||l_parity_bad_respuesta);
  DBMS_OUTPUT.PUT_LINE('PARITY_BAD_INHERENTE='||l_parity_bad_inherente);
  DBMS_OUTPUT.PUT_LINE('PARITY_BAD_RESIDUAL='||l_parity_bad_residual);
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
