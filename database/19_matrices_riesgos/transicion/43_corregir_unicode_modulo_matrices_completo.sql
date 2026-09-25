-- Oracle 11g / MANUAL. Ejecutar únicamente después de 41 y 42.
-- Corrige sólo mappings deterministas observados; no sincroniza semántica ni usa U+00BF global.
-- Recorre todas las columnas textuales RL_MR de las 25 tablas. Requiere el backup existente.
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
  l_backup_rows NUMBER;
  l_current_cells NUMBER;
  l_unmapped_cells NUMBER;
  l_unbacked_targets NUMBER;
  l_required_tables NUMBER;
  l_savepoint_created BOOLEAN := FALSE;
  l_updates NUMBER := 0;
  l_mapping_bad SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_mapping_good SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_ambiguous_mapping SYS.ODCIVARCHAR2LIST := SYS.ODCIVARCHAR2LIST();
  l_validate_catalog BOOLEAN := TRUE;
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
  @@_predicado_unicode_sospechoso.sql
  FUNCTION count_current_cells RETURN NUMBER IS
    total NUMBER := 0; n NUMBER; expr VARCHAR2(4000); pred VARCHAR2(12000); sqlx VARCHAR2(32767);
  BEGIN
    FOR t IN 1..l_tables.COUNT LOOP
      FOR c IN (SELECT column_name FROM user_tab_columns WHERE table_name=l_tables(t)
                AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')) LOOP
        pred := suspicious_predicate(c.column_name);
        sqlx := 'SELECT COUNT(*) FROM '||DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t))||' WHERE '||pred;
        EXECUTE IMMEDIATE sqlx INTO n; total := total + n;
      END LOOP;
    END LOOP; RETURN total;
  END;
  FUNCTION coverage_mismatches RETURN NUMBER IS
    n NUMBER := 0; x NUMBER; q VARCHAR2(32767); p VARCHAR2(12000);
  BEGIN
    FOR t IN 1..l_tables.COUNT LOOP
      FOR c IN (SELECT column_name FROM user_tab_columns WHERE table_name=l_tables(t)
                AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')) LOOP
        p:=suspicious_predicate(c.column_name, 'x');
        q:='SELECT COUNT(*) FROM '||DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t))||' x WHERE '||p||
           ' AND NOT EXISTS (SELECT 1 FROM RL_MR_UNI_BKP_20260924 b WHERE b.UBK_TABLE_NAME=:1 AND b.UBK_COLUMN_NAME=:2 AND b.UBK_ROWID_TEXT=ROWIDTOCHAR(x.ROWID))';
        EXECUTE IMMEDIATE q INTO x USING l_tables(t),c.column_name; n:=n+x;
        q:='SELECT COUNT(*) FROM RL_MR_UNI_BKP_20260924 b WHERE b.UBK_TABLE_NAME=:1 AND b.UBK_COLUMN_NAME=:2 AND NOT EXISTS (SELECT 1 FROM '||DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t))||' x WHERE ROWIDTOCHAR(x.ROWID)=b.UBK_ROWID_TEXT AND '||p||')';
        EXECUTE IMMEDIATE q INTO x USING l_tables(t),c.column_name; n:=n+x;
      END LOOP;
    END LOOP;
    RETURN n;
  END;
  FUNCTION unbacked_mapping_targets RETURN NUMBER IS
    n NUMBER := 0; x NUMBER; q VARCHAR2(32767);
  BEGIN
    FOR i IN 1..l_mapping_bad.COUNT LOOP
      FOR t IN 1..l_tables.COUNT LOOP
        FOR c IN (SELECT column_name FROM user_tab_columns WHERE table_name=l_tables(t)
                  AND data_type IN ('VARCHAR2','CHAR','NVARCHAR2','NCHAR','CLOB')) LOOP
          q:='SELECT COUNT(*) FROM '||DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t))||' x WHERE INSTR(TO_CLOB(x.'||
             DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name)||'),:probe)>0 AND NOT EXISTS ('||
             'SELECT 1 FROM RL_MR_UNI_BKP_20260924 b WHERE b.UBK_TABLE_NAME=:1 AND b.UBK_COLUMN_NAME=:2 '||
             'AND b.UBK_ROWID_TEXT=ROWIDTOCHAR(x.ROWID))';
          EXECUTE IMMEDIATE q INTO x USING l_mapping_bad(i),l_tables(t),c.column_name; n:=n+x;
        END LOOP;
      END LOOP;
    END LOOP;
    RETURN n;
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
          DBMS_ASSERT.SIMPLE_SQL_NAME(c.column_name) || ', :probe) > 0 AND EXISTS (' ||
          'SELECT 1 FROM RL_MR_UNI_BKP_20260924 b WHERE b.UBK_TABLE_NAME=:table_name '||
          'AND b.UBK_COLUMN_NAME=:column_name AND b.UBK_ROWID_TEXT = ROWIDTOCHAR(' ||
          DBMS_ASSERT.SQL_OBJECT_NAME(l_tables(t)) || '.ROWID))';
        EXECUTE IMMEDIATE l_sql USING p_bad, p_good, p_bad, l_tables(t), c.column_name;
        l_updates := l_updates + SQL%ROWCOUNT;
      END LOOP;
    END LOOP;
  END;
  PROCEDURE register_mapping(p_bad VARCHAR2, p_good VARCHAR2) IS
  BEGIN
    FOR i IN 1..l_mapping_bad.COUNT LOOP
      IF l_mapping_bad(i)=p_bad AND l_mapping_good(i)<>p_good THEN
        FOR j IN 1..l_ambiguous_mapping.COUNT LOOP IF l_ambiguous_mapping(j)=p_bad THEN RETURN; END IF; END LOOP;
        l_ambiguous_mapping.EXTEND; l_ambiguous_mapping(l_ambiguous_mapping.COUNT):=p_bad;
      END IF;
    END LOOP;
    l_mapping_bad.EXTEND; l_mapping_bad(l_mapping_bad.COUNT):=p_bad;
    l_mapping_good.EXTEND; l_mapping_good(l_mapping_good.COUNT):=p_good;
    IF NOT l_validate_catalog THEN apply_mapping(p_bad, p_good); END IF;
  END;
BEGIN
  @@_catalogo_unicode_modulo_matrices.sql
  SELECT COUNT(*) INTO l_required_tables FROM user_tables
   WHERE table_name IN (SELECT COLUMN_VALUE FROM TABLE(l_tables));
  DBMS_OUTPUT.PUT_LINE('REQUIRED_RL_MR_TABLES_FOUND='||l_required_tables);
  IF l_required_tables <> l_tables.COUNT THEN
    RAISE_APPLICATION_ERROR(-20744, 'Las 25 tablas RL_MR requeridas no están disponibles; DML bloqueado.');
  END IF;
  BEGIN
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_UNI_BKP_20260924' INTO l_backup_rows;
  EXCEPTION WHEN OTHERS THEN
    RAISE_APPLICATION_ERROR(-20743, 'Backup RL_MR_UNI_BKP_20260924 inexistente o ilegible; no se ejecuta DML.');
  END;
  l_current_cells := count_current_cells;
  DBMS_OUTPUT.PUT_LINE('CURRENT_SUSPICIOUS_CELLS='||l_current_cells);
  DBMS_OUTPUT.PUT_LINE('BACKUP_CELLS='||l_backup_rows);
  IF coverage_mismatches <> 0 OR l_current_cells <> l_backup_rows OR (l_current_cells > 0 AND l_backup_rows = 0) THEN
    RAISE_APPLICATION_ERROR(-20745, 'BACKUP_COVERAGE=FAIL: inventario actual y backup no corresponden exactamente.');
  END IF;
  DBMS_OUTPUT.PUT_LINE('BACKUP_COVERAGE=PASS');
  DBMS_OUTPUT.PUT_LINE('AMBIGUOUS_TOKENS='||l_ambiguous_mapping.COUNT);
  IF l_ambiguous_mapping.COUNT <> 0 THEN
    RAISE_APPLICATION_ERROR(-20748, 'AMBIGUOUS_TOKENS no es cero; catálogo bloqueado.');
  END IF;
  l_unbacked_targets := unbacked_mapping_targets;
  DBMS_OUTPUT.PUT_LINE('UNBACKED_MAPPING_TARGETS='||l_unbacked_targets);
  IF l_unbacked_targets <> 0 THEN
    RAISE_APPLICATION_ERROR(-20749, 'UNBACKED_MAPPING_TARGETS no es cero; DML bloqueado.');
  END IF;
  EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_UNI_BKP_20260924 WHERE DBMS_LOB.INSTR(UBK_OLD_VALUE,UNISTR(''\FFFD''))>0 OR DBMS_LOB.INSTR(UBK_OLD_VALUE,UNISTR(''\00C3''))>0 OR DBMS_LOB.INSTR(UBK_OLD_VALUE,UNISTR(''\00C2''))>0' INTO l_unmapped_cells;
  DBMS_OUTPUT.PUT_LINE('UNMAPPED_TOKENS='||l_unmapped_cells);
  IF l_unmapped_cells <> 0 THEN
    RAISE_APPLICATION_ERROR(-20746, 'UNMAPPED_TOKENS no es cero; U+FFFD/U+00C3/U+00C2 bloquean DML.');
  END IF;
  SAVEPOINT MATRICES_UNICODE_CORRECTION;
  l_savepoint_created := TRUE;
  l_validate_catalog := FALSE;
  FOR i IN 1..l_mapping_bad.COUNT LOOP apply_mapping(l_mapping_bad(i),l_mapping_good(i)); END LOOP;
  l_current_cells := count_current_cells;
  DBMS_OUTPUT.PUT_LINE('CURRENT_SUSPICIOUS_CELLS_POST='||l_current_cells);
  IF l_current_cells <> 0 THEN
    DBMS_OUTPUT.PUT_LINE('UNMAPPED_TOKENS='||l_current_cells);
    ROLLBACK TO MATRICES_UNICODE_CORRECTION;
    RAISE_APPLICATION_ERROR(-20747, 'CURRENT_SUSPICIOUS_CELLS_POST no es cero después del catálogo; corrección revertida.');
  END IF;
  DBMS_OUTPUT.PUT_LINE('UNMAPPED_TOKENS='||l_current_cells);
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_CORRECTION_UPDATES=' || l_updates);
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_CORRECTION_STATUS=PASS');
EXCEPTION WHEN OTHERS THEN
  IF l_savepoint_created THEN
    ROLLBACK TO MATRICES_UNICODE_CORRECTION;
  ELSE
    ROLLBACK;
  END IF;
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_CORRECTION_STATUS=FAIL ' || SQLERRM);
  RAISE;
END;
/
