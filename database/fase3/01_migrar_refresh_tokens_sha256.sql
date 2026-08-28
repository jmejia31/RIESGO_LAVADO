-- DEPRECATED / FAIL-CLOSED.
-- Oracle STANDARD_HASH and DBMS_CRYPTO are unavailable in the target Oracle 11g
-- environment (ORA-00904). The authorized migration is implemented by the
-- parameterized .NET migrator under tools/phase3/RefreshTokenMigrator.
-- This guard intentionally performs no DDL/DML and preserves the failure evidence.
SET SERVEROUTPUT ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
WHENEVER OSERROR EXIT FAILURE ROLLBACK

BEGIN
  RAISE_APPLICATION_ERROR(-20992, 'Deprecated: use the parameterized .NET refresh-token migrator.');
END;
/
EXIT FAILURE
