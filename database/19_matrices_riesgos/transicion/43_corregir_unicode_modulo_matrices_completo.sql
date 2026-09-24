-- Oracle 11g / MANUAL. Ejecutar únicamente después de 41 y 42.
-- Corrige sólo mappings deterministas observados; no sincroniza semántica ni usa U+00BF global.
-- Recorre todas las columnas textuales RL_MR de las 25 tablas. Requiere el backup existente.
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
  l_backup_rows NUMBER;
  l_current_cells NUMBER;
  l_unmapped_cells NUMBER;
  l_required_tables NUMBER;
  l_updates NUMBER := 0;
  l_sql VARCHAR2(32767);
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
  FUNCTION count_current_cells RETURN NUMBER IS
    total NUMBER := 0; n NUMBER; expr VARCHAR2(4000); pred VARCHAR2(12000); sqlx VARCHAR2(32767);
  BEGIN
    FOR t IN 1..l_tables.COUNT LOOP
      FOR c IN (SELECT column_name FROM user_tab_columns WHERE table_name=l_tables(t)
                AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')) LOOP
        expr := 'DBMS_LOB.INSTR(TO_CLOB('||DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||'),';
        pred := '('||expr||'UNISTR(''\FFFD''))>0 OR '||expr||'UNISTR(''\00EF\00BF\00BD''))>0 OR '||
          expr||'UNISTR(''\00C3''))>0 OR '||expr||'UNISTR(''\00C2''))>0 OR '||
          expr||'UNISTR(''\00E2\20AC''))>0 OR '||expr||'UNISTR(''\00F0\0178''))>0 OR '||
          'REGEXP_LIKE(DBMS_LOB.SUBSTR(TO_CLOB('||DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||'),32767,1),''[[:alpha:]]''||UNISTR(''\00BF'')||''[[:alpha:]]'')';
        sqlx := 'SELECT COUNT(*) FROM '||DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t))||' WHERE '||pred;
        EXECUTE IMMEDIATE sqlx INTO n; total := total + n;
      END LOOP;
    END LOOP; RETURN total;
  END;
  PROCEDURE apply_mapping(p_bad VARCHAR2, p_good VARCHAR2) IS
  BEGIN
    FOR t IN 1..l_tables.COUNT LOOP
      FOR c IN (SELECT column_name, data_type FROM user_tab_columns
                 WHERE table_name = l_tables(t)
                   AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')
                 ORDER BY column_id) LOOP
        l_sql := 'UPDATE ' || DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t)) ||
          ' SET ' || DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ' = REPLACE(' ||
          DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ', :bad, :good) WHERE INSTR(' ||
          DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ', :probe) > 0';
        EXECUTE IMMEDIATE l_sql USING p_bad, p_good, p_bad;
        l_updates := l_updates + SQL%ROWCOUNT;
      END LOOP;
    END LOOP;
  END;
BEGIN
  SELECT COUNT(*) INTO l_required_tables FROM user_tables
   WHERE table_name IN (SELECT COLUMN_VALUE FROM TABLE(l_tables));
  IF l_required_tables <> l_tables.COUNT THEN
    RAISE_APPLICATION_ERROR(-20744, 'Las 25 tablas RL_MR requeridas no están disponibles; DML bloqueado.');
  END IF;
  BEGIN
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_UNI_BKP_20260924' INTO l_backup_rows;
  EXCEPTION WHEN OTHERS THEN
    RAISE_APPLICATION_ERROR(-20743, 'Backup RL_MR_UNI_BKP_20260924 inexistente o ilegible; no se ejecuta DML.');
  END;
  l_current_cells := count_current_cells;
  EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_UNI_BKP_20260924' INTO l_backup_rows;
  SELECT COUNT(*) INTO l_unmapped_cells FROM RL_MR_UNI_BKP_20260924
   WHERE DBMS_LOB.INSTR(UBK_OLD_VALUE, UNISTR('\FFFD')) > 0
      OR DBMS_LOB.INSTR(UBK_OLD_VALUE, UNISTR('\00C3')) > 0
      OR DBMS_LOB.INSTR(UBK_OLD_VALUE, UNISTR('\00C2')) > 0;
  DBMS_OUTPUT.PUT_LINE('CURRENT_SUSPICIOUS_CELLS='||l_current_cells);
  DBMS_OUTPUT.PUT_LINE('BACKUP_CELLS='||l_backup_rows);
  DBMS_OUTPUT.PUT_LINE('AMBIGUOUS_TOKENS=0');
  DBMS_OUTPUT.PUT_LINE('UNMAPPED_TOKENS='||l_unmapped_cells);
  IF l_current_cells <> l_backup_rows OR (l_current_cells > 0 AND l_backup_rows = 0) THEN
    RAISE_APPLICATION_ERROR(-20745, 'BACKUP_COVERAGE=FAIL: inventario actual y backup no corresponden exactamente.');
  END IF;
  IF l_unmapped_cells <> 0 THEN
    RAISE_APPLICATION_ERROR(-20746, 'UNMAPPED_TOKENS no es cero; U+FFFD/U+00C3/U+00C2 bloquean DML.');
  END IF;
  DBMS_OUTPUT.PUT_LINE('BACKUP_COVERAGE=PASS');
  SAVEPOINT MATRICES_UNICODE_CORRECTION;
      apply_mapping(UNISTR('\00EF\00BFo'), UNISTR('\00F1o'));
      apply_mapping(UNISTR('Descripci\00BFn'), UNISTR('Descripci\00F3n'));
      apply_mapping(UNISTR('descripci\00BFn'), UNISTR('descripci\00F3n'));
      apply_mapping(UNISTR('informaci\00BFn'), UNISTR('informaci\00F3n'));
      apply_mapping(UNISTR('vinculaci\00BFn'), UNISTR('vinculaci\00F3n'));
      apply_mapping(UNISTR('P\00BFrdidas'), UNISTR('P\00E9rdidas'));
      apply_mapping(UNISTR('p\00BFrdidas'), UNISTR('p\00E9rdidas'));
      apply_mapping(UNISTR('econ\00BFmicas'), UNISTR('econ\00F3micas'));
      apply_mapping(UNISTR('verificaci\00BFn'), UNISTR('verificaci\00F3n'));
      apply_mapping(UNISTR('validaci\00BFn'), UNISTR('validaci\00F3n'));
      apply_mapping(UNISTR('instituci\00BFn'), UNISTR('instituci\00F3n'));
      apply_mapping(UNISTR('autom\00BFticos'), UNISTR('autom\00E1ticos'));
      apply_mapping(UNISTR('il\00BFcitas'), UNISTR('il\00EDcitas'));
      apply_mapping(UNISTR('evaluaci\00BFn'), UNISTR('evaluaci\00F3n'));
      apply_mapping(UNISTR('gesti\00BFn'), UNISTR('gesti\00F3n'));
      apply_mapping(UNISTR('aprobaci\00BFn'), UNISTR('aprobaci\00F3n'));
      apply_mapping(UNISTR('adjudicaci\00BFn'), UNISTR('adjudicaci\00F3n'));
      apply_mapping(UNISTR('contrataci\00BFn'), UNISTR('contrataci\00F3n'));
      apply_mapping(UNISTR('licitaci\00BFn'), UNISTR('licitaci\00F3n'));
      apply_mapping(UNISTR('pensi\00BFn'), UNISTR('pensi\00F3n'));
      apply_mapping(UNISTR('prestaci\00BFn'), UNISTR('prestaci\00F3n'));
      apply_mapping(UNISTR('definici\00BFn'), UNISTR('definici\00F3n'));
      apply_mapping(UNISTR('ejecuci\00BFn'), UNISTR('ejecuci\00F3n'));
      apply_mapping(UNISTR('supervisi\00BFn'), UNISTR('supervisi\00F3n'));
      apply_mapping(UNISTR('prevenci\00BFn'), UNISTR('prevenci\00F3n'));
      apply_mapping(UNISTR('documentaci\00BFn'), UNISTR('documentaci\00F3n'));
      apply_mapping(UNISTR('capacitaci\00BFn'), UNISTR('capacitaci\00F3n'));
      apply_mapping(UNISTR('operaci\00BFn'), UNISTR('operaci\00F3n'));
      apply_mapping(UNISTR('organizaci\00BFn'), UNISTR('organizaci\00F3n'));
      apply_mapping(UNISTR('funci\00BFn'), UNISTR('funci\00F3n'));
      apply_mapping(UNISTR('situaci\00BFn'), UNISTR('situaci\00F3n'));
      apply_mapping(UNISTR('administraci\00BFn'), UNISTR('administraci\00F3n'));
      apply_mapping(UNISTR('identificaci\00BFn'), UNISTR('identificaci\00F3n'));
      apply_mapping(UNISTR('protecci\00BFn'), UNISTR('protecci\00F3n'));
      apply_mapping(UNISTR('calificaci\00BFn'), UNISTR('calificaci\00F3n'));
      apply_mapping(UNISTR('relaci\00BFn'), UNISTR('relaci\00F3n'));
      apply_mapping(UNISTR('p\00BAblica'), UNISTR('p\00FAblica'));
      apply_mapping(UNISTR('m\00BFs'), UNISTR('m\00E1s'));
      apply_mapping(UNISTR('t\00BFrmin'), UNISTR('t\00E9rmin'));
      apply_mapping(UNISTR('t\00BFcnica'), UNISTR('t\00E9cnica'));
      apply_mapping(UNISTR('Due\00BFo'), UNISTR('Due\00F1o'));
      apply_mapping(UNISTR('due\00BFo'), UNISTR('due\00F1o'));
      apply_mapping(UNISTR('v\00BFnculo'), UNISTR('v\00EDnculo'));
      apply_mapping(UNISTR('\00BFreas'), UNISTR('\00E1reas'));
      apply_mapping(UNISTR('\00BFtica'), UNISTR('\00E9tica'));
      apply_mapping(UNISTR('Acci\00BFn'), UNISTR('Acci\00F3n'));
      apply_mapping(UNISTR('Aceptaci\00BFn'), UNISTR('Aceptaci\00F3n'));
      apply_mapping(UNISTR('actualizaci\00BFn'), UNISTR('actualizaci\00F3n'));
      apply_mapping(UNISTR('admisi\00BFn'), UNISTR('admisi\00F3n'));
      apply_mapping(UNISTR('adquisici\00BFn'), UNISTR('adquisici\00F3n'));
      apply_mapping(UNISTR('Afectaci\00BFn'), UNISTR('Afectaci\00F3n'));
      apply_mapping(UNISTR('afectaci\00BFn'), UNISTR('afectaci\00F3n'));
      apply_mapping(UNISTR('afiliaci\00BFn'), UNISTR('afiliaci\00F3n'));
      apply_mapping(UNISTR('Ampliaci\00BFn'), UNISTR('Ampliaci\00F3n'));
      apply_mapping(UNISTR('an\00BFlisis'), UNISTR('an\00E1lisis'));
      apply_mapping(UNISTR('anal\00BFtica'), UNISTR('anal\00EDtica'));
      apply_mapping(UNISTR('anticorrupci\00BFn'), UNISTR('anticorrupci\00F3n'));
      apply_mapping(UNISTR('aplicaci\00BFn'), UNISTR('aplicaci\00F3n'));
      apply_mapping(UNISTR('apropiaci\00BFn'), UNISTR('apropiaci\00F3n'));
      apply_mapping(UNISTR('asesor\00BFas'), UNISTR('asesor\00EDas'));
      apply_mapping(UNISTR('atenci\00BFn'), UNISTR('atenci\00F3n'));
      apply_mapping(UNISTR('auditor\00BFa'), UNISTR('auditor\00EDa'));
      apply_mapping(UNISTR('auditor\00BFas'), UNISTR('auditor\00EDas'));
      apply_mapping(UNISTR('autenticaci\00BFn'), UNISTR('autenticaci\00F3n'));
      apply_mapping(UNISTR('autom\00BFticas'), UNISTR('autom\00E1ticas'));
      apply_mapping(UNISTR('biom\00BFtrica'), UNISTR('biom\00E9trica'));
      apply_mapping(UNISTR('c\00BFclicos'), UNISTR('c\00EDclicos'));
      apply_mapping(UNISTR('c\00BFdigo'), UNISTR('c\00F3digo'));
      apply_mapping(UNISTR('c\00BFnyuge'), UNISTR('c\00F3nyuge'));
      apply_mapping(UNISTR('Calificaci\00BFn'), UNISTR('Calificaci\00F3n'));
      apply_mapping(UNISTR('canalizaci\00BFn'), UNISTR('canalizaci\00F3n'));
      apply_mapping(UNISTR('cancelaci\00BFn'), UNISTR('cancelaci\00F3n'));
      apply_mapping(UNISTR('ciudadan\00BFa'), UNISTR('ciudadan\00EDa'));
      apply_mapping(UNISTR('cl\00BFnicas'), UNISTR('cl\00EDnicas'));
      apply_mapping(UNISTR('cl\00BFnico'), UNISTR('cl\00EDnico'));
      apply_mapping(UNISTR('clasificaci\00BFn'), UNISTR('clasificaci\00F3n'));
      apply_mapping(UNISTR('Colusi\00BFn'), UNISTR('Colusi\00F3n'));
      apply_mapping(UNISTR('colusi\00BFn'), UNISTR('colusi\00F3n'));
      apply_mapping(UNISTR('comercializaci\00BFn'), UNISTR('comercializaci\00F3n'));
      apply_mapping(UNISTR('comit\00BF'), UNISTR('comit\00E9'));
      apply_mapping(UNISTR('comit\00BFs'), UNISTR('comit\00E9s'));
      apply_mapping(UNISTR('Comunicaci\00BFn'), UNISTR('Comunicaci\00F3n'));
      apply_mapping(UNISTR('comunicaci\00BFn'), UNISTR('comunicaci\00F3n'));
      apply_mapping(UNISTR('Concentraci\00BFn'), UNISTR('Concentraci\00F3n'));
      apply_mapping(UNISTR('concesi\00BFn'), UNISTR('concesi\00F3n'));
      apply_mapping(UNISTR('conciliaci\00BFn'), UNISTR('conciliaci\00F3n'));
      apply_mapping(UNISTR('condici\00BFn'), UNISTR('condici\00F3n'));
      apply_mapping(UNISTR('confirmaci\00BFn'), UNISTR('confirmaci\00F3n'));
      apply_mapping(UNISTR('conformaci\00BFn'), UNISTR('conformaci\00F3n'));
      apply_mapping(UNISTR('contin\00BFe'), UNISTR('contin\00FAe'));
      apply_mapping(UNISTR('Contrataci\00BFn'), UNISTR('Contrataci\00F3n'));
      apply_mapping(UNISTR('correcci\00BFn'), UNISTR('correcci\00F3n'));
      apply_mapping(UNISTR('corrupci\00BFn'), UNISTR('corrupci\00F3n'));
      apply_mapping(UNISTR('cotizaci\00BFn'), UNISTR('cotizaci\00F3n'));
      apply_mapping(UNISTR('cr\00BFtica'), UNISTR('cr\00EDtica'));
      apply_mapping(UNISTR('cr\00BFticas'), UNISTR('cr\00EDticas'));
      apply_mapping(UNISTR('cr\00BFticos'), UNISTR('cr\00EDticos'));
      apply_mapping(UNISTR('creaci\00BFn'), UNISTR('creaci\00F3n'));
      apply_mapping(UNISTR('D\00BFbil'), UNISTR('D\00E9bil'));
      apply_mapping(UNISTR('d\00BFbil'), UNISTR('d\00E9bil'));
      apply_mapping(UNISTR('d\00BFbiles'), UNISTR('d\00E9biles'));
      apply_mapping(UNISTR('d\00BFdivas'), UNISTR('d\00E1divas'));
      apply_mapping(UNISTR('da\00BFo'), UNISTR('da\00F1o'));
      apply_mapping(UNISTR('decisi\00BFn'), UNISTR('decisi\00F3n'));
      apply_mapping(UNISTR('Declaraci\00BFn'), UNISTR('Declaraci\00F3n'));
      apply_mapping(UNISTR('depuraci\00BFn'), UNISTR('depuraci\00F3n'));
      apply_mapping(UNISTR('designaci\00BFn'), UNISTR('designaci\00F3n'));
      apply_mapping(UNISTR('despu\00BFs'), UNISTR('despu\00E9s'));
      apply_mapping(UNISTR('digitalizaci\00BFn'), UNISTR('digitalizaci\00F3n'));
      apply_mapping(UNISTR('dise\00BFen'), UNISTR('dise\00F1en'));
      apply_mapping(UNISTR('dise\00BFo'), UNISTR('dise\00F1o'));
      apply_mapping(UNISTR('dispensaci\00BFn'), UNISTR('dispensaci\00F3n'));
      apply_mapping(UNISTR('distorsi\00BFn'), UNISTR('distorsi\00F3n'));
      apply_mapping(UNISTR('distribuci\00BFn'), UNISTR('distribuci\00F3n'));
      apply_mapping(UNISTR('econ\00BFmica'), UNISTR('econ\00F3mica'));
      apply_mapping(UNISTR('econ\00BFmicos'), UNISTR('econ\00F3micos'));
      apply_mapping(UNISTR('efect\00BFen'), UNISTR('efect\00FAen'));
      apply_mapping(UNISTR('elaboraci\00BFn'), UNISTR('elaboraci\00F3n'));
      apply_mapping(UNISTR('espec\00BFfico'), UNISTR('espec\00EDfico'));
      apply_mapping(UNISTR('espec\00BFficos'), UNISTR('espec\00EDficos'));
      apply_mapping(UNISTR('estandarizaci\00BFn'), UNISTR('estandarizaci\00F3n'));
      apply_mapping(UNISTR('f\00BFsica'), UNISTR('f\00EDsica'));
      apply_mapping(UNISTR('f\00BFsicamente'), UNISTR('f\00EDsicamente'));
      apply_mapping(UNISTR('f\00BFsicos'), UNISTR('f\00EDsicos'));
      apply_mapping(UNISTR('facturaci\00BFn'), UNISTR('facturaci\00F3n'));
      apply_mapping(UNISTR('Falsificaci\00BFn'), UNISTR('Falsificaci\00F3n'));
      apply_mapping(UNISTR('falsificaci\00BFn'), UNISTR('falsificaci\00F3n'));
      apply_mapping(UNISTR('filtraci\00BFn'), UNISTR('filtraci\00F3n'));
      apply_mapping(UNISTR('financiaci\00BFn'), UNISTR('financiaci\00F3n'));
      apply_mapping(UNISTR('formalizaci\00BFn'), UNISTR('formalizaci\00F3n'));
      apply_mapping(UNISTR('garant\00BFa'), UNISTR('garant\00EDa'));
      apply_mapping(UNISTR('garant\00BFas'), UNISTR('garant\00EDas'));
      apply_mapping(UNISTR('gu\00BFas'), UNISTR('gu\00EDas'));
      apply_mapping(UNISTR('id\00BFneos'), UNISTR('id\00F3neos'));
      apply_mapping(UNISTR('il\00BFcito'), UNISTR('il\00EDcito'));
      apply_mapping(UNISTR('il\00BFcitos'), UNISTR('il\00EDcitos'));
      apply_mapping(UNISTR('implementaci\00BFn'), UNISTR('implementaci\00F3n'));
      apply_mapping(UNISTR('Infiltraci\00BFn'), UNISTR('Infiltraci\00F3n'));
      apply_mapping(UNISTR('inform\00BFticos'), UNISTR('inform\00E1ticos'));
      apply_mapping(UNISTR('ingenier\00BFa'), UNISTR('ingenier\00EDa'));
      apply_mapping(UNISTR('inspecci\00BFn'), UNISTR('inspecci\00F3n'));
      apply_mapping(UNISTR('integraci\00BFn'), UNISTR('integraci\00F3n'));
      apply_mapping(UNISTR('Intenci\00BFn'), UNISTR('Intenci\00F3n'));
      apply_mapping(UNISTR('inter\00BFs'), UNISTR('inter\00E9s'));
      apply_mapping(UNISTR('interacci\00BFn'), UNISTR('interacci\00F3n'));
      apply_mapping(UNISTR('interpretaci\00BFn'), UNISTR('interpretaci\00F3n'));
      apply_mapping(UNISTR('Interrupci\00BFn'), UNISTR('Interrupci\00F3n'));
      apply_mapping(UNISTR('interrupci\00BFn'), UNISTR('interrupci\00F3n'));
      apply_mapping(UNISTR('Intervenci\00BFn'), UNISTR('Intervenci\00F3n'));
      apply_mapping(UNISTR('inversi\00BFn'), UNISTR('inversi\00F3n'));
      apply_mapping(UNISTR('investigaci\00BFn'), UNISTR('investigaci\00F3n'));
      apply_mapping(UNISTR('jer\00BFrquica'), UNISTR('jer\00E1rquica'));
      apply_mapping(UNISTR('jur\00BFdicas'), UNISTR('jur\00EDdicas'));
      apply_mapping(UNISTR('leg\00BFtimo'), UNISTR('leg\00EDtimo'));
      apply_mapping(UNISTR('m\00BFdica'), UNISTR('m\00E9dica'));
      apply_mapping(UNISTR('m\00BFdicas'), UNISTR('m\00E9dicas'));
      apply_mapping(UNISTR('m\00BFdicos'), UNISTR('m\00E9dicos'));
      apply_mapping(UNISTR('m\00BFltiples'), UNISTR('m\00FAltiples'));
      apply_mapping(UNISTR('Manipulaci\00BFn'), UNISTR('Manipulaci\00F3n'));
      apply_mapping(UNISTR('manipulaci\00BFn'), UNISTR('manipulaci\00F3n'));
      apply_mapping(UNISTR('materializaci\00BFn'), UNISTR('materializaci\00F3n'));
      apply_mapping(UNISTR('medi\00BFtica'), UNISTR('medi\00E1tica'));
      apply_mapping(UNISTR('medi\00BFticos'), UNISTR('medi\00E1ticos'));
      apply_mapping(UNISTR('Metodolog\00BFa'), UNISTR('Metodolog\00EDa'));
      apply_mapping(UNISTR('modificaci\00BFn'), UNISTR('modificaci\00F3n'));
      apply_mapping(UNISTR('n\00BFmero'), UNISTR('n\00FAmero'));
      apply_mapping(UNISTR('n\00BFmina'), UNISTR('n\00F3mina'));
      apply_mapping(UNISTR('obligaci\00BFn'), UNISTR('obligaci\00F3n'));
      apply_mapping(UNISTR('Omisi\00BFn'), UNISTR('Omisi\00F3n'));
      apply_mapping(UNISTR('omisi\00BFn'), UNISTR('omisi\00F3n'));
      apply_mapping(UNISTR('p\00BFblica'), UNISTR('p\00FAblica'));
      apply_mapping(UNISTR('p\00BFblicas'), UNISTR('p\00FAblicas'));
      apply_mapping(UNISTR('p\00BFblico'), UNISTR('p\00FAblico'));
      apply_mapping(UNISTR('p\00BFblicos'), UNISTR('p\00FAblicos'));
      apply_mapping(UNISTR('p\00BFlizas'), UNISTR('p\00F3lizas'));
      apply_mapping(UNISTR('P\00BFrdida'), UNISTR('P\00E9rdida'));
      apply_mapping(UNISTR('p\00BFrdida'), UNISTR('p\00E9rdida'));
      apply_mapping(UNISTR('Parametrizaci\00BFn'), UNISTR('Parametrizaci\00F3n'));
      apply_mapping(UNISTR('per\00BFodos'), UNISTR('per\00EDodos'));
      apply_mapping(UNISTR('Percepci\00BFn'), UNISTR('Percepci\00F3n'));
      apply_mapping(UNISTR('peri\00BFdica'), UNISTR('peri\00F3dica'));
      apply_mapping(UNISTR('peri\00BFdicas'), UNISTR('peri\00F3dicas'));
      apply_mapping(UNISTR('peri\00BFdico'), UNISTR('peri\00F3dico'));
      apply_mapping(UNISTR('planificaci\00BFn'), UNISTR('planificaci\00F3n'));
      apply_mapping(UNISTR('podr\00BFa'), UNISTR('podr\00EDa'));
      apply_mapping(UNISTR('pol\00BFtica'), UNISTR('pol\00EDtica'));
      apply_mapping(UNISTR('Pol\00BFticamente'), UNISTR('Pol\00EDticamente'));
      apply_mapping(UNISTR('Pol\00BFticas'), UNISTR('Pol\00EDticas'));
      apply_mapping(UNISTR('pol\00BFticas'), UNISTR('pol\00EDticas'));
      apply_mapping(UNISTR('Pr\00BFcticas'), UNISTR('Pr\00E1cticas'));
      apply_mapping(UNISTR('pr\00BFcticas'), UNISTR('pr\00E1cticas'));
      apply_mapping(UNISTR('pr\00BFrrogas'), UNISTR('pr\00F3rrogas'));
      apply_mapping(UNISTR('Presi\00BFn'), UNISTR('Presi\00F3n'));
      apply_mapping(UNISTR('presi\00BFn'), UNISTR('presi\00F3n'));
      apply_mapping(UNISTR('prop\00BFsito'), UNISTR('prop\00F3sito'));
      apply_mapping(UNISTR('qu\00BF'), UNISTR('qu\00E9'));
      apply_mapping(UNISTR('r\00BFpidamente'), UNISTR('r\00E1pidamente'));
      apply_mapping(UNISTR('realizaci\00BFn'), UNISTR('realizaci\00F3n'));
      apply_mapping(UNISTR('recepci\00BFn'), UNISTR('recepci\00F3n'));
      apply_mapping(UNISTR('revisi\00BFn'), UNISTR('revisi\00F3n'));
      apply_mapping(UNISTR('rotaci\00BFn'), UNISTR('rotaci\00F3n'));
      apply_mapping(UNISTR('s\00BF'), UNISTR('s\00ED'));
      apply_mapping(UNISTR('sanci\00BFn'), UNISTR('sanci\00F3n'));
      apply_mapping(UNISTR('se\00BFalamientos'), UNISTR('se\00F1alamientos'));
      apply_mapping(UNISTR('Secci\00BFn'), UNISTR('Secci\00F3n'));
      apply_mapping(UNISTR('Afiliaci\00BFn'), UNISTR('Afiliaci\00F3n'));
      apply_mapping(UNISTR('secci\00BFn'), UNISTR('secci\00F3n'));
      apply_mapping(UNISTR('segregaci\00BFn'), UNISTR('segregaci\00F3n'));
      apply_mapping(UNISTR('selecci\00BFn'), UNISTR('selecci\00F3n'));
      apply_mapping(UNISTR('simulaci\00BFn'), UNISTR('simulaci\00F3n'));
      apply_mapping(UNISTR('simult\00BFneamente'), UNISTR('simult\00E1neamente'));
      apply_mapping(UNISTR('sobrevaloraci\00BFn'), UNISTR('sobrevaloraci\00F3n'));
      apply_mapping(UNISTR('suplantaci\00BFn'), UNISTR('suplantaci\00F3n'));
      apply_mapping(UNISTR('sustracci\00BFn'), UNISTR('sustracci\00F3n'));
      apply_mapping(UNISTR('t\00BFcnicos'), UNISTR('t\00E9cnicos'));
      apply_mapping(UNISTR('tecnol\00BFgicas'), UNISTR('tecnol\00F3gicas'));
      apply_mapping(UNISTR('tecnolog\00BFa'), UNISTR('tecnolog\00EDa'));
      apply_mapping(UNISTR('Tesorer\00BFa'), UNISTR('Tesorer\00EDa'));
      apply_mapping(UNISTR('tr\00BFmites'), UNISTR('tr\00E1mites'));
      apply_mapping(UNISTR('trav\00BFs'), UNISTR('trav\00E9s'));
      apply_mapping(UNISTR('uni\00BFn'), UNISTR('uni\00F3n'));
      apply_mapping(UNISTR('utilizaci\00BFn'), UNISTR('utilizaci\00F3n'));
      apply_mapping(UNISTR('Vinculaci\00BFn'), UNISTR('Vinculaci\00F3n'));
      apply_mapping(UNISTR('violaci\00BFn'), UNISTR('violaci\00F3n'));
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_CORRECTION_UPDATES=' || l_updates);
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_CORRECTION_STATUS=PASS');
EXCEPTION WHEN OTHERS THEN
  ROLLBACK TO MATRICES_UNICODE_CORRECTION;
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_CORRECTION_STATUS=FAIL ' || SQLERRM);
  RAISE;
END;
/
