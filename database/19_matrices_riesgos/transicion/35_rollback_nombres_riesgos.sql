-- Oracle 11g / MANUAL. Restaura únicamente RIE_NOMBRE desde RL_MR_RIES_NOM_BKP_20260924.
-- Todas las referencias al backup son SQL dinámico: la tabla fue creada por 32
-- mediante EXECUTE IMMEDIATE y Oracle 11g no permite resolverla estáticamente
-- en la misma unidad PL/SQL.
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
  v_backup_exists NUMBER;
  v_backup_rows NUMBER;
  v_target_rows NUMBER;
  v_db_rows NUMBER;
  v_backup_duplicates NUMBER;
  v_restored NUMBER := 0;
  v_diffs NUMBER;
BEGIN
  SELECT COUNT(*) INTO v_backup_exists
    FROM USER_TABLES
   WHERE TABLE_NAME = 'RL_MR_RIES_NOM_BKP_20260924';
  IF v_backup_exists = 0 THEN
    RAISE_APPLICATION_ERROR(-20935, 'Rollback bloqueado: no existe RL_MR_RIES_NOM_BKP_20260924.');
  END IF;

  EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_RIES_NOM_BKP_20260924'
    INTO v_backup_rows;
  IF v_backup_rows <> 59 THEN
    RAISE_APPLICATION_ERROR(-20935, 'Backup incompleto; se esperaban 59 filas y existen ' || v_backup_rows || '.');
  END IF;
  SELECT COUNT(*) INTO v_db_rows FROM RL_MR_RIESGOS;
  IF v_db_rows <> 59 THEN
    RAISE_APPLICATION_ERROR(-20938, 'Restauración bloqueada: filas destino=' || v_db_rows || ', esperado=59.');
  END IF;
  EXECUTE IMMEDIATE q'~SELECT COUNT(*) FROM
    (SELECT RIE_ID, RIE_CODIGO
       FROM RL_MR_RIES_NOM_BKP_20260924
      GROUP BY RIE_ID, RIE_CODIGO
     HAVING COUNT(*) > 1)~'
    INTO v_backup_duplicates;
  IF v_backup_duplicates <> 0 THEN
    RAISE_APPLICATION_ERROR(-20939, 'Backup inválido: claves duplicadas=' || v_backup_duplicates || '.');
  END IF;
  EXECUTE IMMEDIATE q'~SELECT COUNT(*) FROM RL_MR_RIESGOS r
      WHERE EXISTS (SELECT 1 FROM RL_MR_RIES_NOM_BKP_20260924 b
                     WHERE b.RIE_ID = r.RIE_ID AND b.RIE_CODIGO = r.RIE_CODIGO)~'
    INTO v_target_rows;
  IF v_target_rows <> v_backup_rows THEN
    RAISE_APPLICATION_ERROR(-20936, 'No hay correspondencia exacta RIE_ID + RIE_CODIGO para restaurar.');
  END IF;

  SAVEPOINT risk_name_restore;
  EXECUTE IMMEDIATE q'~UPDATE RL_MR_RIESGOS r
       SET RIE_NOMBRE = (SELECT b.RIE_NOMBRE
                           FROM RL_MR_RIES_NOM_BKP_20260924 b
                          WHERE b.RIE_ID = r.RIE_ID AND b.RIE_CODIGO = r.RIE_CODIGO)
     WHERE EXISTS (SELECT 1 FROM RL_MR_RIES_NOM_BKP_20260924 b
                    WHERE b.RIE_ID = r.RIE_ID AND b.RIE_CODIGO = r.RIE_CODIGO)~';
  v_restored := SQL%ROWCOUNT;
  EXECUTE IMMEDIATE q'~SELECT COUNT(*) FROM RL_MR_RIESGOS r
       JOIN RL_MR_RIES_NOM_BKP_20260924 b
         ON b.RIE_ID = r.RIE_ID AND b.RIE_CODIGO = r.RIE_CODIGO
      WHERE r.RIE_NOMBRE <> b.RIE_NOMBRE
         OR (r.RIE_NOMBRE IS NULL AND b.RIE_NOMBRE IS NOT NULL)
         OR (r.RIE_NOMBRE IS NOT NULL AND b.RIE_NOMBRE IS NULL)~'
    INTO v_diffs;
  DBMS_OUTPUT.PUT_LINE('RISK_NAME_ROLLBACK_EXPECTED_ROWS=' || v_backup_rows);
  DBMS_OUTPUT.PUT_LINE('RISK_NAME_ROLLBACK_UPDATED_ROWS=' || v_restored);
  DBMS_OUTPUT.PUT_LINE('RISK_NAME_ROLLBACK_REMAINING_DIFFS=' || v_diffs);
  IF v_restored <> v_backup_rows OR v_diffs <> 0 THEN
    ROLLBACK TO risk_name_restore;
    RAISE_APPLICATION_ERROR(-20937, 'Rollback no verificó igualdad exacta.');
  END IF;
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('RISK_NAME_ROLLBACK_STATUS=PASS');
EXCEPTION WHEN OTHERS THEN
  ROLLBACK;
  RAISE;
END;
/
