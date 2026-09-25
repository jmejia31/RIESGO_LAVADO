-- Fragmento SQL*Plus compartido por 41, 42, 43 y 44.
-- Se incluye dentro de cada bloque PL/SQL declarativo.
-- La expresión siempre devuelve un predicado con un paréntesis exterior balanceado.
-- Cubre corrupción física y U+00BF contextual; no reemplaza ningún carácter.
  FUNCTION suspicious_predicate(p_column VARCHAR2, p_alias VARCHAR2 DEFAULT NULL) RETURN VARCHAR2 IS
    l_column VARCHAR2(128) := DBMS_ASSERT.SIMPLE_SQL_NAME(p_column);
    l_ref VARCHAR2(300) := CASE
      WHEN p_alias IS NULL THEN l_column
      ELSE DBMS_ASSERT.SIMPLE_SQL_NAME(p_alias)||'.'||l_column
    END;
    l_clob VARCHAR2(4000) := 'TO_CLOB('||l_ref||')';
  BEGIN
    RETURN '('||
      'DBMS_LOB.INSTR('||l_clob||',UNISTR(''\FFFD''))>0 OR '||
      'DBMS_LOB.INSTR('||l_clob||',UNISTR(''\00EF\00BF\00BD''))>0 OR '||
      'DBMS_LOB.INSTR('||l_clob||',UNISTR(''\00C3''))>0 OR '||
      'DBMS_LOB.INSTR('||l_clob||',UNISTR(''\00C2''))>0 OR '||
      'DBMS_LOB.INSTR('||l_clob||',UNISTR(''\00E2\20AC''))>0 OR '||
      'DBMS_LOB.INSTR('||l_clob||',UNISTR(''\00F0\0178''))>0 OR '||
      'DBMS_LOB.INSTR('||l_clob||',UNISTR(''\00BF\00BF''))>0 OR '||
      'DBMS_LOB.INSTR('||l_clob||',UNISTR(''\00BF\00BF\00BF''))>0 OR '||
      'REGEXP_LIKE('||l_clob||',''[[:alpha:]][[:alpha:]]*''||UNISTR(''\00BF'')||''[[:alpha:]][[:alpha:]]*'') OR '||
      'REGEXP_LIKE('||l_clob||',''[[:alpha:]]''||UNISTR(''\00BF'')||''[[:alpha:]]'')'||
      ')';
  END;
