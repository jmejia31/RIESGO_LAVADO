-- FASE 3.1.2 - seed declarativo idempotente de las siete funciones ya soportadas.
-- No contiene código ejecutable; los handlers son claves declarativas para 3.1.3.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
WHENEVER OSERROR EXIT FAILURE
SET ECHO OFF
SET VERIFY OFF
SET TERMOUT ON
SET SERVEROUTPUT ON

DECLARE
  v_user NUMBER;
  v_function_id NUMBER;
  v_version_id NUMBER;
  v_count NUMBER;
  v_result VARCHAR2(20);

  PROCEDURE seed_native(
    p_code VARCHAR2, p_name VARCHAR2, p_handler VARCHAR2,
    p_signature CLOB, p_hash VARCHAR2, p_min NUMBER, p_max NUMBER) IS
  BEGIN
    v_result := CASE WHEN p_code = 'OR' THEN 'BOOLEAN' ELSE 'DECIMAL' END;
    SELECT MIN(USR_ID) INTO v_user FROM RL_USUARIOS;
    SELECT COUNT(*) INTO v_count FROM RL_MR_FUNCIONES WHERE FUN_CODIGO = p_code;
    IF v_count = 0 THEN
      SELECT SEQ_RL_MR_FUNCIONES.NEXTVAL INTO v_function_id FROM DUAL;
      INSERT INTO RL_MR_FUNCIONES(FUN_ID,FUN_CODIGO,FUN_NOMBRE,FUN_CATEGORIA,FUN_ESTADO,FUN_FECHA_CREACION,FUN_USR_CREACION,FUN_VERSION_ROW)
      VALUES(v_function_id,p_code,p_name,'CALCULO','ACTIVE',SYSDATE,v_user,1);
    ELSE
      SELECT FUN_ID INTO v_function_id FROM RL_MR_FUNCIONES WHERE FUN_CODIGO = p_code;
    END IF;
    SELECT COUNT(*) INTO v_count FROM RL_MR_FUNCION_VERSIONES WHERE FUV_FUNCION_ID = v_function_id AND FUV_VERSION = 1;
    IF v_count = 0 THEN
      SELECT SEQ_RL_MR_FUNCION_VERSIONES.NEXTVAL INTO v_version_id FROM DUAL;
      INSERT INTO RL_MR_FUNCION_VERSIONES(FUV_ID,FUV_FUNCION_ID,FUV_VERSION,FUV_TIPO,FUV_TIPO_RESULTADO,FUV_SIGNATURE_JSON,FUV_HANDLER_KEY,FUV_MIN_ARITY,FUV_MAX_ARITY,FUV_ESTADO,FUV_HASH,FUV_FECHA_CREACION,FUV_USR_CREACION,FUV_VERSION_ROW)
      VALUES(v_version_id,v_function_id,1,'NATIVE',v_result,p_signature,p_handler,p_min,p_max,'DRAFT',p_hash,SYSDATE,v_user,1);
    ELSE
      SELECT FUV_ID INTO v_version_id FROM RL_MR_FUNCION_VERSIONES WHERE FUV_FUNCION_ID = v_function_id AND FUV_VERSION = 1;
      SELECT COUNT(*) INTO v_count FROM RL_MR_FUNCION_VERSIONES
       WHERE FUV_ID = v_version_id AND FUV_TIPO = 'NATIVE' AND FUV_TIPO_RESULTADO = v_result AND FUV_HANDLER_KEY = p_handler
         AND FUV_DEFINICION_DSL IS NULL AND FUV_HASH = p_hash
         AND FUV_MIN_ARITY = p_min AND NVL(FUV_MAX_ARITY,-1) = NVL(p_max,-1);
      IF v_count <> 1 THEN
        RAISE_APPLICATION_ERROR(-20911, 'Seed incompatible para función ' || p_code || ' v1.');
      END IF;
    END IF;
  END;

  PROCEDURE seed_argument(p_function_code VARCHAR2, p_position NUMBER, p_code VARCHAR2, p_name VARCHAR2, p_type VARCHAR2, p_required NUMBER, p_variadic NUMBER) IS
    v_existing NUMBER;
  BEGIN
    SELECT FUV_ID INTO v_version_id
      FROM RL_MR_FUNCION_VERSIONES v JOIN RL_MR_FUNCIONES f ON f.FUN_ID = v.FUV_FUNCION_ID
     WHERE f.FUN_CODIGO = p_function_code AND v.FUV_VERSION = 1;
    SELECT COUNT(*) INTO v_existing FROM RL_MR_FUNCION_ARGUMENTOS WHERE FUA_FUNCION_VERSION_ID = v_version_id AND FUA_POSICION = p_position;
    IF v_existing = 0 THEN
      INSERT INTO RL_MR_FUNCION_ARGUMENTOS(FUA_ID,FUA_FUNCION_VERSION_ID,FUA_POSICION,FUA_CODIGO,FUA_NOMBRE,FUA_TIPO,FUA_REQUERIDO,FUA_VARIADIC)
      VALUES(SEQ_RL_MR_FUNCION_ARGUMENTOS.NEXTVAL,v_version_id,p_position,p_code,p_name,p_type,p_required,p_variadic);
    ELSE
      SELECT COUNT(*) INTO v_existing FROM RL_MR_FUNCION_ARGUMENTOS
       WHERE FUA_FUNCION_VERSION_ID = v_version_id AND FUA_POSICION = p_position
         AND FUA_CODIGO = p_code AND FUA_TIPO = p_type AND FUA_REQUERIDO = p_required AND FUA_VARIADIC = p_variadic;
      IF v_existing <> 1 THEN
        RAISE_APPLICATION_ERROR(-20912, 'Argumento incompatible para función ' || p_function_code || '.');
      END IF;
    END IF;
  END;
BEGIN
  SELECT MIN(USR_ID) INTO v_user FROM RL_USUARIOS;
  IF v_user IS NULL THEN RAISE_APPLICATION_ERROR(-20913, 'No existe usuario técnico para el seed.'); END IF;
  seed_native('IF','IF','IF_V1',TO_CLOB(q'[{"minArity":3,"maxArity":3,"arguments":[{"position":1,"code":"CONDITION","type":"BOOLEAN","required":true,"variadic":false},{"position":2,"code":"TRUE_VALUE","type":"DECIMAL","required":true,"variadic":false},{"position":3,"code":"FALSE_VALUE","type":"DECIMAL","required":true,"variadic":false}]}]'),'fd25bf9350474d13ff4bbc7562e258be1ff14c229a2ee32a7258ca1d25ef61b3',3,3);
  seed_native('IFERROR','IFERROR','IFERROR_V1',TO_CLOB(q'[{"minArity":2,"maxArity":2,"arguments":[{"position":1,"code":"VALUE","type":"DECIMAL","required":true,"variadic":false},{"position":2,"code":"FALLBACK","type":"DECIMAL","required":true,"variadic":false}]}]'),'335f7fb8cc3f8ee0829818b84323e1471269042d827da5cab229809ce5cb220f',2,2);
  seed_native('ROUND','ROUND','ROUND_V1',TO_CLOB(q'[{"minArity":2,"maxArity":2,"arguments":[{"position":1,"code":"VALUE","type":"DECIMAL","required":true,"variadic":false},{"position":2,"code":"DIGITS","type":"INTEGER","required":true,"variadic":false}]}]'),'56ee2229c707a0166345bdc813482a20e0648bdbbc803dd4d26eab443bd69688',2,2);
  seed_native('ROUNDDOWN','ROUNDDOWN','ROUNDDOWN_V1',TO_CLOB(q'[{"minArity":2,"maxArity":2,"arguments":[{"position":1,"code":"VALUE","type":"DECIMAL","required":true,"variadic":false},{"position":2,"code":"DIGITS","type":"INTEGER","required":true,"variadic":false}]}]'),'e29582a29be2e87a2c236487ac0b5e8f17680a3d5cffec53b49b2500de857109',2,2);
  seed_native('MAX','MAX','MAX_V1',TO_CLOB(q'[{"minArity":1,"maxArity":null,"arguments":[{"position":1,"code":"VALUES","type":"DECIMAL","required":true,"variadic":true}]}]'),'12681943536541d004c3a8a56a79bed3018e085b4694a93f1a004031b0abb150',1,NULL);
  seed_native('MOD','MOD','MOD_V1',TO_CLOB(q'[{"minArity":2,"maxArity":2,"arguments":[{"position":1,"code":"VALUE","type":"DECIMAL","required":true,"variadic":false},{"position":2,"code":"DIVISOR","type":"DECIMAL","required":true,"variadic":false}]}]'),'69cfbcd651795db938d873375648d26e353f0fba7e8464bc01353bff0bc45389',2,2);
  seed_native('OR','OR','OR_V1',TO_CLOB(q'[{"minArity":1,"maxArity":null,"arguments":[{"position":1,"code":"VALUES","type":"BOOLEAN","required":true,"variadic":true}]}]'),'ea5a66ad735d99d2841a65c920da187c2aca7dc2cb294dc08aaec5f64b8da4e1',1,NULL);

  seed_argument('IF',1,'CONDITION','Condition','BOOLEAN',1,0);
  seed_argument('IF',2,'TRUE_VALUE','True value','DECIMAL',1,0);
  seed_argument('IF',3,'FALSE_VALUE','False value','DECIMAL',1,0);
  seed_argument('IFERROR',1,'VALUE','Value','DECIMAL',1,0);
  seed_argument('IFERROR',2,'FALLBACK','Fallback','DECIMAL',1,0);
  seed_argument('ROUND',1,'VALUE','Value','DECIMAL',1,0);
  seed_argument('ROUND',2,'DIGITS','Digits','INTEGER',1,0);
  seed_argument('ROUNDDOWN',1,'VALUE','Value','DECIMAL',1,0);
  seed_argument('ROUNDDOWN',2,'DIGITS','Digits','INTEGER',1,0);
  seed_argument('MAX',1,'VALUES','Values','DECIMAL',1,1);
  seed_argument('MOD',1,'VALUE','Value','DECIMAL',1,0);
  seed_argument('MOD',2,'DIVISOR','Divisor','DECIMAL',1,0);
  seed_argument('OR',1,'VALUES','Values','BOOLEAN',1,1);
  DBMS_OUTPUT.PUT_LINE('SEED_NATIVE_EXPECTED=7');
  COMMIT;
END;
/
EXIT SUCCESS
