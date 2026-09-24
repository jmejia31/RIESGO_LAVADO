-- Oracle 11g / MANUAL. Ejecutar después de 36 y 37.
-- Alcance exclusivo: RL_MR_RIESGOS.RIE_DESCRIPCION.
-- Es una reparación determinista de codificación; NO sincroniza descripciones desde Excel.
-- Requiere RL_MR_RIES_DESC_BKP_20260924 antes de cualquier UPDATE.
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
  expected_updates NUMBER := 0;
  actual_updates NUMBER := 0;
  v_fixed VARCHAR2(32767);
  backup_table NUMBER;
  backup_rows NUMBER;
  db_rows NUMBER;
  duplicate_codes NUMBER;
  FUNCTION fix_description(p_text VARCHAR2) RETURN VARCHAR2 IS
    v VARCHAR2(32767) := p_text;
  BEGIN
    IF v IS NULL THEN RETURN NULL; END IF;
    -- U+FFFD/mojibake sin contexto se conserva para que 39 lo bloquee.
    -- No se convierte a U+00BF porque eso perdería evidencia del carácter original.
    v := REPLACE(v, UNISTR('\00EF\00BFo'), UNISTR('\00F1o'));
    v := REPLACE(v, UNISTR('Descripci\00BFn'), UNISTR('Descripci\00F3n'));
    v := REPLACE(v, UNISTR('descripci\00BFn'), UNISTR('descripci\00F3n'));
    v := REPLACE(v, UNISTR('informaci\00BFn'), UNISTR('informaci\00F3n'));
    v := REPLACE(v, UNISTR('vinculaci\00BFn'), UNISTR('vinculaci\00F3n'));
    v := REPLACE(v, UNISTR('P\00BFrdidas'), UNISTR('P\00E9rdidas'));
    v := REPLACE(v, UNISTR('p\00BFrdidas'), UNISTR('p\00E9rdidas'));
    v := REPLACE(v, UNISTR('econ\00BFmicas'), UNISTR('econ\00F3micas'));
    v := REPLACE(v, UNISTR('verificaci\00BFn'), UNISTR('verificaci\00F3n'));
    v := REPLACE(v, UNISTR('validaci\00BFn'), UNISTR('validaci\00F3n'));
    v := REPLACE(v, UNISTR('instituci\00BFn'), UNISTR('instituci\00F3n'));
    v := REPLACE(v, UNISTR('autom\00BFticos'), UNISTR('autom\00E1ticos'));
    v := REPLACE(v, UNISTR('il\00BFcitas'), UNISTR('il\00EDcitas'));
    v := REPLACE(v, UNISTR('evaluaci\00BFn'), UNISTR('evaluaci\00F3n'));
    v := REPLACE(v, UNISTR('gesti\00BFn'), UNISTR('gesti\00F3n'));
    v := REPLACE(v, UNISTR('aprobaci\00BFn'), UNISTR('aprobaci\00F3n'));
    v := REPLACE(v, UNISTR('adjudicaci\00BFn'), UNISTR('adjudicaci\00F3n'));
    v := REPLACE(v, UNISTR('contrataci\00BFn'), UNISTR('contrataci\00F3n'));
    v := REPLACE(v, UNISTR('licitaci\00BFn'), UNISTR('licitaci\00F3n'));
    v := REPLACE(v, UNISTR('pensi\00BFn'), UNISTR('pensi\00F3n'));
    v := REPLACE(v, UNISTR('prestaci\00BFn'), UNISTR('prestaci\00F3n'));
    v := REPLACE(v, UNISTR('definici\00BFn'), UNISTR('definici\00F3n'));
    v := REPLACE(v, UNISTR('ejecuci\00BFn'), UNISTR('ejecuci\00F3n'));
    v := REPLACE(v, UNISTR('supervisi\00BFn'), UNISTR('supervisi\00F3n'));
    v := REPLACE(v, UNISTR('prevenci\00BFn'), UNISTR('prevenci\00F3n'));
    v := REPLACE(v, UNISTR('documentaci\00BFn'), UNISTR('documentaci\00F3n'));
    v := REPLACE(v, UNISTR('capacitaci\00BFn'), UNISTR('capacitaci\00F3n'));
    v := REPLACE(v, UNISTR('operaci\00BFn'), UNISTR('operaci\00F3n'));
    v := REPLACE(v, UNISTR('organizaci\00BFn'), UNISTR('organizaci\00F3n'));
    v := REPLACE(v, UNISTR('funci\00BFn'), UNISTR('funci\00F3n'));
    v := REPLACE(v, UNISTR('situaci\00BFn'), UNISTR('situaci\00F3n'));
    v := REPLACE(v, UNISTR('administraci\00BFn'), UNISTR('administraci\00F3n'));
    v := REPLACE(v, UNISTR('identificaci\00BFn'), UNISTR('identificaci\00F3n'));
    v := REPLACE(v, UNISTR('protecci\00BFn'), UNISTR('protecci\00F3n'));
    v := REPLACE(v, UNISTR('calificaci\00BFn'), UNISTR('calificaci\00F3n'));
    v := REPLACE(v, UNISTR('relaci\00BFn'), UNISTR('relaci\00F3n'));
    v := REPLACE(v, UNISTR('p\00BAblica'), UNISTR('p\00FAblica'));
    v := REPLACE(v, UNISTR('m\00BFs'), UNISTR('m\00E1s'));
    v := REPLACE(v, UNISTR('t\00BFrmin'), UNISTR('t\00E9rmin'));
    v := REPLACE(v, UNISTR('t\00BFcnica'), UNISTR('t\00E9cnica'));
    v := REPLACE(v, UNISTR('Due\00BFo'), UNISTR('Due\00F1o'));
    v := REPLACE(v, UNISTR('due\00BFo'), UNISTR('due\00F1o'));
    v := REPLACE(v, UNISTR('v\00BFnculo'), UNISTR('v\00EDnculo'));
    -- Tokens residuales observados en el diagnóstico posterior a 38/39.
    -- Cada reemplazo es literal y contextual; no se reemplaza U+00BF globalmente.
    v := REPLACE(v, UNISTR('Pol\00BFticamente'), UNISTR('Pol\00EDticamente'));
    v := REPLACE(v, UNISTR('pol\00BFticamente'), UNISTR('pol\00EDticamente'));
    v := REPLACE(v, UNISTR('actualizaci\00BFn'), UNISTR('actualizaci\00F3n'));
    v := REPLACE(v, UNISTR('Actualizaci\00BFn'), UNISTR('Actualizaci\00F3n'));
    v := REPLACE(v, UNISTR('tecnol\00BFgicas'), UNISTR('tecnol\00F3gicas'));
    v := REPLACE(v, UNISTR('Tecnol\00BFgicas'), UNISTR('Tecnol\00F3gicas'));
    v := REPLACE(v, UNISTR('dise\00BFo'), UNISTR('dise\00F1o'));
    v := REPLACE(v, UNISTR('Dise\00BFo'), UNISTR('Dise\00F1o'));
    v := REPLACE(v, UNISTR('auditor\00BFa'), UNISTR('auditor\00EDa'));
    v := REPLACE(v, UNISTR('Auditor\00BFa'), UNISTR('Auditor\00EDa'));
    v := REPLACE(v, UNISTR('corrupci\00BFn'), UNISTR('corrupci\00F3n'));
    v := REPLACE(v, UNISTR('Corrupci\00BFn'), UNISTR('Corrupci\00F3n'));
    v := REPLACE(v, UNISTR('p\00BFrdida'), UNISTR('p\00E9rdida'));
    v := REPLACE(v, UNISTR('P\00BFrdida'), UNISTR('P\00E9rdida'));
    v := REPLACE(v, UNISTR('c\00BFnyuge'), UNISTR('c\00F3nyuge'));
    v := REPLACE(v, UNISTR('C\00BFnyuge'), UNISTR('C\00F3nyuge'));
    v := REPLACE(v, UNISTR('uni\00BFn'), UNISTR('uni\00F3n'));
    v := REPLACE(v, UNISTR('Uni\00BFn'), UNISTR('Uni\00F3n'));
    v := REPLACE(v, UNISTR('inter\00BFs'), UNISTR('inter\00E9s'));
    v := REPLACE(v, UNISTR('Inter\00BFs'), UNISTR('Inter\00E9s'));
    v := REPLACE(v, UNISTR('da\00BFo'), UNISTR('da\00F1o'));
    v := REPLACE(v, UNISTR('Da\00BFo'), UNISTR('Da\00F1o'));
    v := REPLACE(v, UNISTR('\00BFtica'), UNISTR('\00E9tica'));
    v := REPLACE(v, UNISTR('\00BFreas'), UNISTR('\00E1reas'));
    v := REPLACE(v, UNISTR('m\00BFdicos'), UNISTR('m\00E9dicos'));
    v := REPLACE(v, UNISTR('M\00BFdicos'), UNISTR('M\00E9dicos'));
    v := REPLACE(v, UNISTR('n\00BFmina'), UNISTR('n\00F3mina'));
    v := REPLACE(v, UNISTR('N\00BFmina'), UNISTR('N\00F3mina'));
    v := REPLACE(v, UNISTR('se\00BFalamientos'), UNISTR('se\00F1alamientos'));
    v := REPLACE(v, UNISTR('Se\00BFalamientos'), UNISTR('Se\00F1alamientos'));
    -- U+FFFD no se sustituye a ciegas: sin contexto se desconoce el carácter original.
    RETURN v;
  END;
BEGIN
  SELECT COUNT(*) INTO backup_table FROM USER_TABLES WHERE TABLE_NAME = 'RL_MR_RIES_DESC_BKP_20260924';
  IF backup_table = 0 THEN
    RAISE_APPLICATION_ERROR(-20947, 'Corrección bloqueada: ejecutar primero 37_backup_rl_mr_riesgos_descripciones.sql.');
  END IF;
  EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_RIES_DESC_BKP_20260924'
    INTO backup_rows;
  IF backup_rows <> 59 THEN
    RAISE_APPLICATION_ERROR(-20948, 'Corrección bloqueada: backup incompleto; filas=' || backup_rows || ', esperado=59.');
  END IF;
  SELECT COUNT(*) INTO db_rows FROM RL_MR_RIESGOS;
  SELECT COUNT(*) INTO duplicate_codes FROM
   (SELECT RIE_CODIGO FROM RL_MR_RIESGOS GROUP BY RIE_CODIGO HAVING COUNT(*) > 1);
  IF db_rows <> 59 OR duplicate_codes <> 0 THEN
    RAISE_APPLICATION_ERROR(-20949, 'Corrección bloqueada: destino inválido; filas=' || db_rows || ', duplicados=' || duplicate_codes || '.');
  END IF;
  FOR r IN (SELECT RIE_DESCRIPCION FROM RL_MR_RIESGOS) LOOP
    v_fixed := fix_description(r.RIE_DESCRIPCION);
    IF v_fixed <> r.RIE_DESCRIPCION
       OR (v_fixed IS NULL AND r.RIE_DESCRIPCION IS NOT NULL)
       OR (v_fixed IS NOT NULL AND r.RIE_DESCRIPCION IS NULL) THEN
      expected_updates := expected_updates + 1;
    END IF;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('EXPECTED_DESCRIPTION_UPDATES=' || expected_updates);
  SAVEPOINT risk_description_encoding;
  FOR r IN (SELECT RIE_ID, RIE_CODIGO, RIE_DESCRIPCION FROM RL_MR_RIESGOS FOR UPDATE) LOOP
    v_fixed := fix_description(r.RIE_DESCRIPCION);
    IF v_fixed <> r.RIE_DESCRIPCION
       OR (v_fixed IS NULL AND r.RIE_DESCRIPCION IS NOT NULL)
       OR (v_fixed IS NOT NULL AND r.RIE_DESCRIPCION IS NULL) THEN
      UPDATE RL_MR_RIESGOS
         SET RIE_DESCRIPCION = v_fixed
       WHERE RIE_ID = r.RIE_ID AND RIE_CODIGO = r.RIE_CODIGO;
      actual_updates := actual_updates + SQL%ROWCOUNT;
    END IF;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('ACTUAL_DESCRIPTION_UPDATES=' || actual_updates);
  IF actual_updates <> expected_updates THEN
    ROLLBACK TO risk_description_encoding;
    RAISE_APPLICATION_ERROR(-20938, 'Actualizaciones inesperadas: expected=' || expected_updates || ', actual=' || actual_updates);
  END IF;
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('RISK_DESCRIPTION_CORRECTION_STATUS=PASS');
EXCEPTION WHEN OTHERS THEN
  ROLLBACK;
  RAISE;
END;
/
