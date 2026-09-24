-- Oracle 11g / MANUAL. Restaura únicamente celdas capturadas por 42.
-- Fail-closed: no modifica nada si el backup falta, está duplicado o no coincide.
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
  l_backup_rows NUMBER;
  l_mismatch NUMBER;
  l_restored NUMBER := 0;
  l_sql VARCHAR2(32767);
  l_cursor SYS_REFCURSOR;
  l_table_name VARCHAR2(30);
  l_column_name VARCHAR2(30);
  l_rowid_text VARCHAR2(30);
  l_old_value CLOB;
  l_savepoint_created BOOLEAN := FALSE;
BEGIN
  BEGIN
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_UNI_BKP_20260924' INTO l_backup_rows;
  EXCEPTION WHEN OTHERS THEN
    RAISE_APPLICATION_ERROR(-20745, 'RL_MR_UNI_BKP_20260924 inexistente; rollback cancelado.');
  END;
  IF l_backup_rows=0 THEN
    DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_ROLLBACK_STATUS=PASS_NO_ROWS');
    RETURN;
  END IF;
  OPEN l_cursor FOR 'SELECT UBK_TABLE_NAME, UBK_COLUMN_NAME, UBK_ROWID_TEXT, UBK_OLD_VALUE FROM RL_MR_UNI_BKP_20260924';
  LOOP
    FETCH l_cursor INTO l_table_name, l_column_name, l_rowid_text, l_old_value;
    EXIT WHEN l_cursor%NOTFOUND;
    l_sql := 'SELECT COUNT(*) FROM '||DBMS_ASSERT.SQL_OBJECT_NAME(l_table_name)||
      ' WHERE ROWID=CHARTOROWID(:rid)';
    EXECUTE IMMEDIATE l_sql INTO l_mismatch USING l_rowid_text;
    IF l_mismatch<>1 THEN
      CLOSE l_cursor;
      RAISE_APPLICATION_ERROR(-20746, 'Fila de backup inexistente: '||l_table_name||'.'||l_column_name||'.'||l_rowid_text);
    END IF;
  END LOOP;
  CLOSE l_cursor;
  SAVEPOINT MATRICES_UNICODE_ROLLBACK;
  l_savepoint_created := TRUE;
  OPEN l_cursor FOR 'SELECT UBK_TABLE_NAME, UBK_COLUMN_NAME, UBK_ROWID_TEXT, UBK_OLD_VALUE FROM RL_MR_UNI_BKP_20260924';
  LOOP
    FETCH l_cursor INTO l_table_name, l_column_name, l_rowid_text, l_old_value;
    EXIT WHEN l_cursor%NOTFOUND;
    l_sql := 'UPDATE '||DBMS_ASSERT.SQL_OBJECT_NAME(l_table_name)||
      ' SET '||DBMS_ASSERT.SIMPLE_SQL_NAME(l_column_name)||'=:old_value WHERE ROWID=CHARTOROWID(:rid)';
    EXECUTE IMMEDIATE l_sql USING l_old_value, l_rowid_text;
    IF SQL%ROWCOUNT<>1 THEN RAISE_APPLICATION_ERROR(-20747, 'Rollback no restauró una fila.'); END IF;
    l_restored:=l_restored+1;
  END LOOP;
  CLOSE l_cursor;
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_ROLLBACK_ROWS='||l_restored);
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_ROLLBACK_STATUS=PASS');
EXCEPTION WHEN OTHERS THEN
  IF l_savepoint_created THEN ROLLBACK TO MATRICES_UNICODE_ROLLBACK; ELSE ROLLBACK; END IF;
  DBMS_OUTPUT.PUT_LINE('MATRICES_UNICODE_ROLLBACK_STATUS=FAIL '||SQLERRM);
  RAISE;
END;
/
