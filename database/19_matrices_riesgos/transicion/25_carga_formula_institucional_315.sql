-- FASE 3.1.5 - carga institucional idempotente.
-- Inserta únicamente datos administrativos versionados en estado DRAFT.
-- La publicación posterior debe pasar por el único Publication Gate del backend.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
WHENEVER OSERROR EXIT FAILURE
SET ECHO OFF
SET VERIFY OFF
SET TERMOUT ON
SET SERVEROUTPUT ON

DECLARE
  v_user NUMBER;
  v_id NUMBER;
  v_version_id NUMBER;
  v_count NUMBER;

  PROCEDURE seed_native(
    p_code VARCHAR2, p_name VARCHAR2, p_handler VARCHAR2, p_result VARCHAR2,
    p_signature CLOB, p_hash VARCHAR2, p_min NUMBER, p_max NUMBER) IS
    v_function_id NUMBER;
    v_version_id_local NUMBER;
  BEGIN
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
      SELECT SEQ_RL_MR_FUNCION_VERSIONES.NEXTVAL INTO v_version_id_local FROM DUAL;
      INSERT INTO RL_MR_FUNCION_VERSIONES(FUV_ID,FUV_FUNCION_ID,FUV_VERSION,FUV_TIPO,FUV_TIPO_RESULTADO,FUV_SIGNATURE_JSON,FUV_HANDLER_KEY,FUV_MIN_ARITY,FUV_MAX_ARITY,FUV_ESTADO,FUV_HASH,FUV_FECHA_CREACION,FUV_USR_CREACION,FUV_VERSION_ROW)
       VALUES(v_version_id_local,v_function_id,1,'NATIVE',p_result,p_signature,p_handler,p_min,p_max,'DRAFT',p_hash,SYSDATE,v_user,1);
    ELSE
      SELECT FUV_ID INTO v_version_id_local FROM RL_MR_FUNCION_VERSIONES WHERE FUV_FUNCION_ID = v_function_id AND FUV_VERSION = 1;
      SELECT COUNT(*) INTO v_count FROM RL_MR_FUNCION_VERSIONES
       WHERE FUV_ID = v_version_id_local AND FUV_TIPO = 'NATIVE' AND FUV_TIPO_RESULTADO = p_result
         AND FUV_HANDLER_KEY = p_handler AND FUV_DEFINICION_DSL IS NULL
         AND FUV_MIN_ARITY = p_min AND NVL(FUV_MAX_ARITY,-1) = NVL(p_max,-1);
      IF v_count <> 1 THEN RAISE_APPLICATION_ERROR(-20961, 'Contrato incompatible para función nativa ' || p_code || '.'); END IF;
    END IF;
  END;

  PROCEDURE seed_argument(p_function_code VARCHAR2, p_position NUMBER, p_code VARCHAR2, p_name VARCHAR2, p_type VARCHAR2, p_required NUMBER, p_variadic NUMBER) IS
    v_function_version_id NUMBER;
  BEGIN
    SELECT v.FUV_ID INTO v_function_version_id
      FROM RL_MR_FUNCION_VERSIONES v JOIN RL_MR_FUNCIONES f ON f.FUN_ID = v.FUV_FUNCION_ID
     WHERE f.FUN_CODIGO = p_function_code AND v.FUV_VERSION = 1;
    SELECT COUNT(*) INTO v_count FROM RL_MR_FUNCION_ARGUMENTOS WHERE FUA_FUNCION_VERSION_ID = v_function_version_id AND FUA_POSICION = p_position;
    IF v_count = 0 THEN
      INSERT INTO RL_MR_FUNCION_ARGUMENTOS(FUA_ID,FUA_FUNCION_VERSION_ID,FUA_POSICION,FUA_CODIGO,FUA_NOMBRE,FUA_TIPO,FUA_REQUERIDO,FUA_VARIADIC)
      VALUES(SEQ_RL_MR_FUNCION_ARGUMENTOS.NEXTVAL,v_function_version_id,p_position,p_code,p_name,p_type,p_required,p_variadic);
    ELSE
      SELECT COUNT(*) INTO v_count FROM RL_MR_FUNCION_ARGUMENTOS
       WHERE FUA_FUNCION_VERSION_ID = v_function_version_id AND FUA_POSICION = p_position
         AND FUA_CODIGO = p_code AND FUA_TIPO = p_type AND FUA_REQUERIDO = p_required AND FUA_VARIADIC = p_variadic;
      IF v_count <> 1 THEN RAISE_APPLICATION_ERROR(-20962, 'Argumento incompatible para función ' || p_function_code || '.'); END IF;
    END IF;
  END;

  PROCEDURE seed_weight(p_code VARCHAR2, p_name VARCHAR2, p_value NUMBER, p_hash VARCHAR2) IS
    v_parameter_id NUMBER;
    v_parameter_version_id NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count FROM RL_MR_PARAMETROS_CALCULO WHERE PAC_CODIGO = p_code;
    IF v_count = 0 THEN
      SELECT SEQ_RL_MR_PARAMETROS.NEXTVAL INTO v_parameter_id FROM DUAL;
      INSERT INTO RL_MR_PARAMETROS_CALCULO(PAC_ID,PAC_CODIGO,PAC_NOMBRE,PAC_TIPO,PAC_ESTADO,PAC_FECHA_CREACION,PAC_USR_CREACION,PAC_VERSION_ROW)
      VALUES(v_parameter_id,p_code,p_name,'DECIMAL','ACTIVE',SYSDATE,v_user,1);
    ELSE
      SELECT PAC_ID INTO v_parameter_id FROM RL_MR_PARAMETROS_CALCULO WHERE PAC_CODIGO = p_code;
    END IF;
    SELECT COUNT(*) INTO v_count FROM RL_MR_PARAMETRO_VERSIONES WHERE PAV_PARAMETRO_ID = v_parameter_id AND PAV_VERSION = 1;
    IF v_count = 0 THEN
      SELECT SEQ_RL_MR_PARAMETRO_VERSIONES.NEXTVAL INTO v_parameter_version_id FROM DUAL;
      INSERT INTO RL_MR_PARAMETRO_VERSIONES(PAV_ID,PAV_PARAMETRO_ID,PAV_VERSION,PAV_TIPO,PAV_VALOR_DECIMAL,PAV_ESTADO,PAV_HASH,PAV_FECHA_CREACION,PAV_USR_CREACION,PAV_VERSION_ROW)
       VALUES(v_parameter_version_id,v_parameter_id,1,'DECIMAL',p_value,'DRAFT',p_hash,SYSDATE,v_user,1);
    ELSE
      SELECT COUNT(*) INTO v_count FROM RL_MR_PARAMETRO_VERSIONES
       WHERE PAV_PARAMETRO_ID = v_parameter_id AND PAV_VERSION = 1 AND PAV_TIPO = 'DECIMAL' AND PAV_VALOR_DECIMAL = p_value;
      IF v_count <> 1 THEN RAISE_APPLICATION_ERROR(-20963, 'Parámetro institucional incompatible ' || p_code || '.'); END IF;
    END IF;
  END;

  PROCEDURE seed_formula(p_number NUMBER, p_code VARCHAR2, p_source VARCHAR2, p_expression CLOB, p_result VARCHAR2, p_hash VARCHAR2) IS
    v_formula_id NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count FROM RL_MR_FORMULAS WHERE FOR_CODIGO = p_code;
    IF v_count = 0 THEN
      SELECT SEQ_RL_MR_FORMULAS.NEXTVAL INTO v_formula_id FROM DUAL;
      INSERT INTO RL_MR_FORMULAS(FOR_ID,FOR_CODIGO,FOR_NOMBRE,FOR_DESCRIPCION,FOR_ESTADO,FOR_METADATA_JSON,FOR_FECHA_CREACION,FOR_USR_CREACION,FOR_VERSION_ROW)
      VALUES(v_formula_id,p_code,'Fórmula institucional ' || LPAD(p_number,2,'0'),'Traducción semántica segura de la fórmula institucional ' || p_source,'ACTIVE',TO_CLOB('{"sourceCell":"' || p_source || '","dataset":"Matriz Consolidada","translation":"semantic"}'),SYSDATE,v_user,1);
    ELSE
      SELECT FOR_ID INTO v_formula_id FROM RL_MR_FORMULAS WHERE FOR_CODIGO = p_code;
    END IF;
    SELECT COUNT(*) INTO v_count FROM RL_MR_FORMULA_VERSIONES WHERE FOV_FORMULA_ID = v_formula_id AND FOV_VERSION = 1;
    IF v_count = 0 THEN
      INSERT INTO RL_MR_FORMULA_VERSIONES(FOV_ID,FOV_FORMULA_ID,FOV_VERSION,FOV_EXPRESION,FOV_TIPO_RESULTADO,FOV_ESTADO,FOV_HASH,FOV_FECHA_CREACION,FOV_USR_CREACION,FOV_VERSION_ROW)
       VALUES(SEQ_RL_MR_FORMULA_VERSIONES.NEXTVAL,v_formula_id,1,p_expression,p_result,'DRAFT',p_hash,SYSDATE,v_user,1);
    ELSE
      SELECT COUNT(*) INTO v_count FROM RL_MR_FORMULA_VERSIONES
       WHERE FOV_FORMULA_ID = v_formula_id AND FOV_VERSION = 1 AND DBMS_LOB.COMPARE(FOV_EXPRESION,p_expression) = 0
         AND FOV_TIPO_RESULTADO = p_result AND FOV_HASH = p_hash;
      IF v_count <> 1 THEN RAISE_APPLICATION_ERROR(-20964, 'Fórmula institucional incompatible ' || p_code || '.'); END IF;
    END IF;
  END;
BEGIN
  SELECT MIN(USR_ID) INTO v_user FROM RL_USUARIOS;
  IF v_user IS NULL THEN RAISE_APPLICATION_ERROR(-20965, 'No existe usuario técnico para la carga institucional.'); END IF;

  seed_native('IF','IF','IF_V1','DECIMAL',TO_CLOB(q'[{"minArity":3,"maxArity":3,"arguments":[{"position":1,"code":"CONDITION","type":"BOOLEAN"},{"position":2,"code":"TRUE_VALUE","type":"VALUE"},{"position":3,"code":"FALSE_VALUE","type":"VALUE"}]}]'),'9f3ed01e28f8a0186af06d4593c547d1fbbf8d5aad7d3715acc47cd4ec74cce5',3,3);
  seed_native('IFERROR','IFERROR','IFERROR_V1','DECIMAL',TO_CLOB(q'[{"minArity":2,"maxArity":2,"arguments":[{"position":1,"code":"VALUE","type":"VALUE"},{"position":2,"code":"FALLBACK","type":"VALUE"}]}]'),'64773a17625b23dd78f0b793870d03bf39b6557b4229421143f34c92373da91e',2,2);
  seed_native('ROUND','ROUND','ROUND_V1','DECIMAL',TO_CLOB(q'[{"minArity":2,"maxArity":2,"arguments":[{"position":1,"code":"VALUE","type":"DECIMAL"},{"position":2,"code":"DIGITS","type":"INTEGER"}]}]'),'f5f79bf4c6c82242296087f0f380066b0424be21bfb1550f89e412c8af7e7b19',2,2);
  seed_native('ROUNDDOWN','ROUNDDOWN','ROUNDDOWN_V1','DECIMAL',TO_CLOB(q'[{"minArity":2,"maxArity":2,"arguments":[{"position":1,"code":"VALUE","type":"DECIMAL"},{"position":2,"code":"DIGITS","type":"INTEGER"}]}]'),'033e03ffab642f7d213c3222954979f5b9a6870bf345dee0fe90eef1f62fac88',2,2);
  seed_native('MAX','MAX','MAX_V1','DECIMAL',TO_CLOB(q'[{"minArity":1,"maxArity":null,"arguments":[{"position":1,"code":"VALUES","type":"DECIMAL","variadic":true}]}]'),'8330ba1d886ada42be00159254588d383d263bcc78236f49cbaf9deb99a871d0',1,NULL);
  seed_native('MIN','MIN','MIN_V1','DECIMAL',TO_CLOB(q'[{"minArity":1,"maxArity":null,"arguments":[{"position":1,"code":"VALUES","type":"DECIMAL","variadic":true}]}]'),'fcdb1eacde731f2e4d15d9f23839a326b20d3ec538dccbcae3083c01eac5143d',1,NULL);
  seed_native('MOD','MOD','MOD_V1','DECIMAL',TO_CLOB(q'[{"minArity":2,"maxArity":2,"arguments":[{"position":1,"code":"VALUE","type":"DECIMAL"},{"position":2,"code":"DIVISOR","type":"DECIMAL"}]}]'),'2b66d28d9d96a66a8514684f0440a3d3335a988d0b3e7fee18fbc96f6e661817',2,2);
  seed_native('OR','OR','OR_V1','BOOLEAN',TO_CLOB(q'[{"minArity":1,"maxArity":null,"arguments":[{"position":1,"code":"VALUES","type":"BOOLEAN","variadic":true}]}]'),'f95176a968ca3bb693b978c7a70ace4bd2e8de4d7208eaa073e04b2fe9dd4da0',1,NULL);
  seed_native('AND','AND','AND_V1','BOOLEAN',TO_CLOB(q'[{"minArity":1,"maxArity":null,"arguments":[{"position":1,"code":"VALUES","type":"BOOLEAN","variadic":true}]}]'),'248c542f72b5898e7258fad9239d1a2fcaf8b179100d88d43438ad6951a0523f',1,NULL);
  seed_native('LOOKUP','LOOKUP','LOOKUP_V1','VALUE',TO_CLOB(q'[{"minArity":2,"maxArity":3,"arguments":[{"position":1,"code":"CATALOG","type":"TEXT"},{"position":2,"code":"INPUT","type":"VALUE"},{"position":3,"code":"RESULT_FIELD","type":"TEXT"}]}]'),'4d8c042bd06b24c8a0e77b394fb8e9dceeabd9ea172dcd83ea03d2679389139b',2,3);

  seed_argument('IF',1,'CONDITION','Condition','BOOLEAN',1,0); seed_argument('IF',2,'TRUE_VALUE','True value','DECIMAL',1,0); seed_argument('IF',3,'FALSE_VALUE','False value','DECIMAL',1,0);
  seed_argument('IFERROR',1,'VALUE','Value','DECIMAL',1,0); seed_argument('IFERROR',2,'FALLBACK','Fallback','DECIMAL',1,0);
  seed_argument('ROUND',1,'VALUE','Value','DECIMAL',1,0); seed_argument('ROUND',2,'DIGITS','Digits','INTEGER',1,0);
  seed_argument('ROUNDDOWN',1,'VALUE','Value','DECIMAL',1,0); seed_argument('ROUNDDOWN',2,'DIGITS','Digits','INTEGER',1,0);
  seed_argument('MAX',1,'VALUES','Values','DECIMAL',1,1); seed_argument('MIN',1,'VALUES','Values','DECIMAL',1,1);
  seed_argument('MOD',1,'VALUE','Value','DECIMAL',1,0); seed_argument('MOD',2,'DIVISOR','Divisor','DECIMAL',1,0);
  seed_argument('OR',1,'VALUES','Values','BOOLEAN',1,1); seed_argument('AND',1,'VALUES','Values','BOOLEAN',1,1);
  seed_argument('LOOKUP',1,'CATALOG','Catalog','TEXT',1,0); seed_argument('LOOKUP',2,'INPUT','Input','VALUE',1,0); seed_argument('LOOKUP',3,'RESULT_FIELD','Result field','TEXT',0,0);

  seed_weight('PESO_PREVENTIVO','Peso preventivo',0.70,'143d1489ed404f91b1e986dbe4cfab1deef58d07f03d241c4963e8e6051d4a76'); seed_weight('PESO_DETECTIVO','Peso detectivo',0.15,'352c5d8cd8171821e3393510210b47f820673ea6dfb4d9636b37c905a92154ca'); seed_weight('PESO_CORRECTIVO','Peso correctivo',0.15,'352c5d8cd8171821e3393510210b47f820673ea6dfb4d9636b37c905a92154ca');

  seed_formula(1,'F01_VALOR_RIESGO_INHERENTE','Matriz Consolidada!L2','IF((frecuencia+impacto-1)=-1,"",frecuencia+impacto-1)','DECIMAL','4d18e5b0def31fe31cb2a130fd9a6c4987d4622548c3e0c0226e2f9ca3079b76');
  seed_formula(2,'F02_NIVEL_RIESGO_INHERENTE','Matriz Consolidada!M2','IFERROR(LOOKUP("CAT_NIVEL_RIESGO",valor_riesgo_inherente),"")','TEXT','e8b622e7a2e9b6aaeeeb4ab6cbbafd2bd4089cb81239a9c5819cfdca799637f4');
  seed_formula(3,'F03_NIVEL_CONTROL_PREVENTIVO','Matriz Consolidada!V2','IFERROR(LOOKUP("CAT_EFECTIVIDAD_NIVEL",escala_preventivo,"NUMBER"),"")','DECIMAL','4ab2bb371650334a4a4956ccbafa77c55e3f42ee871744272c0c7077041c1bfc');
  seed_formula(4,'F04_PORCENTAJE_CONTROL_PREVENTIVO','Matriz Consolidada!W2','IFERROR(LOOKUP("CAT_EFECTIVIDAD_PORCENTAJE",escala_preventivo,"NUMBER"),"")','DECIMAL','df068525ab4d9261cabf30e7e01b078b16ffb51134aba6526bf6e0299c768c76');
  seed_formula(5,'F05_NIVEL_CONTROL_DETECTIVO','Matriz Consolidada!Z2','IFERROR(LOOKUP("CAT_EFECTIVIDAD_NIVEL",escala_detectivo,"NUMBER"),"")','DECIMAL','aba136e7bd6b10955a29d574d22c96f0737422e73acda2c4b375f07b7f4c4de3');
  seed_formula(6,'F06_PORCENTAJE_CONTROL_DETECTIVO','Matriz Consolidada!AA2','IFERROR(LOOKUP("CAT_EFECTIVIDAD_PORCENTAJE",escala_detectivo,"NUMBER"),"")','DECIMAL','c4b13375f90d5915c7d5b44df87f7bbf1a475ec2e2bc260b2f95cae8c57facf0');
  seed_formula(7,'F07_NIVEL_CONTROL_CORRECTIVO','Matriz Consolidada!AD2','IFERROR(LOOKUP("CAT_EFECTIVIDAD_NIVEL",escala_correctivo,"NUMBER"),"")','DECIMAL','6b10a89e4bbd6dd8ee872e0d9e5c3392dbd545b5ef6f41095a32e491941ca26d');
  seed_formula(8,'F08_PORCENTAJE_CONTROL_CORRECTIVO','Matriz Consolidada!AE2','IFERROR(LOOKUP("CAT_EFECTIVIDAD_PORCENTAJE",escala_correctivo,"NUMBER"),"")','DECIMAL','cb7756582716adee8e61d41af22565d078cc2e57ece722f8b245f6a7f13097fe');
  seed_formula(9,'F09_EFECTIVIDAD_TOTAL_PONDERADA','Matriz Consolidada!AG2','IF(AND(control_preventivo="",control_detectivo="",control_correctivo=""),"",PESO_PREVENTIVO*IF(porcentaje_control_preventivo="",0,porcentaje_control_preventivo)+PESO_DETECTIVO*IF(porcentaje_control_detectivo="",0,porcentaje_control_detectivo)+PESO_CORRECTIVO*IF(porcentaje_control_correctivo="",0,porcentaje_control_correctivo))','DECIMAL','c6a70b2ef9a27b61bcfff37f730c1ab95b4655b57ef9bb48a9dfbaeeda628ebc');
  seed_formula(10,'F10_RIESGO_RESIDUAL_DESCRIPCION','Matriz Consolidada!AH2','IF(riesgo_inherente_descripcion="","",riesgo_inherente_descripcion)','TEXT','fa18c6ecf6db6a55883c88d3374e7f29609066c6aa7e34b3cb1c3d5dd4d5eb24');
  seed_formula(11,'F11_FRECUENCIA_RESIDUAL','Matriz Consolidada!AI2','IFERROR(IF(OR(frecuencia="",impacto="",valor_riesgo_inherente="",valor_riesgo_residual=""),"",IF(valor_riesgo_inherente=valor_riesgo_residual,frecuencia,MIN(tope_f,f_base+incremento_f_aux))),"")','DECIMAL','090bee0e85550f4ce01c60d26082cf87a59f634e317bc7b0767c6ec174136271');
  seed_formula(12,'F12_IMPACTO_RESIDUAL','Matriz Consolidada!AJ2','IFERROR(IF(OR(frecuencia="",impacto="",valor_riesgo_inherente="",valor_riesgo_residual=""),"",IF(valor_riesgo_inherente=valor_riesgo_residual,impacto,MIN(tope_i,i_base+incremento_i_aux))),"")','DECIMAL','b4371b28486ad3906890bdb7678ca0d494bdf5e6ab6668c37ce7b05e3a6397b0');
  seed_formula(13,'F13_VALOR_RIESGO_RESIDUAL','Matriz Consolidada!AK2','IFERROR(ROUND(MAX(1,valor_riesgo_inherente*(1-efectividad_total_ponderada)),0),"")','DECIMAL','f6848c9bb59c11c853cd0e592768d4fc4206803d09f80786b7853ac8c4732977');
  seed_formula(14,'F14_NIVEL_RIESGO_RESIDUAL','Matriz Consolidada!AL2','IFERROR(LOOKUP("CAT_NIVEL_RIESGO",valor_riesgo_residual),"")','TEXT','4c60cce6797ede52f926f1508d21dfaa0b326bef9df20464f775d10061f95cae');
  seed_formula(15,'F15_FRECUENCIA_RESIDUAL_AUX','Matriz Consolidada!AX2','IFERROR((1-efectividad_total_ponderada)*frecuencia,"")','DECIMAL','53c5cdf1f3cb537471f620ffa2ab0820dca063d609586d76b574ffb288517226');
  seed_formula(16,'F16_IMPACTO_RESIDUAL_AUX','Matriz Consolidada!AY2','IFERROR((1-efectividad_total_ponderada)*impacto,"")','DECIMAL','3846ff70e3887a79c2a66b99c9ee731db4aad812f85cc5f0153d18dd3637c6d9');
  seed_formula(17,'F17_SUMA_RESIDUAL_REDONDEADA_AUX','Matriz Consolidada!AZ2','IFERROR(valor_riesgo_residual+1,"")','DECIMAL','29231b5f2060a265fc8e7826bea77c8932aef8caee71477d0a07a2b710b78d3b');
  seed_formula(18,'F18_F_BASE_AUX','Matriz Consolidada!BA2','IFERROR(MAX(1,ROUNDDOWN(frecuencia_residual_aux,0)),"")','DECIMAL','93d3722c8433cecc920df12826e11c2ba336040eb937b80b79ba35e8be06005e');
  seed_formula(19,'F19_I_BASE_AUX','Matriz Consolidada!BB2','IFERROR(MAX(1,ROUNDDOWN(impacto_residual_aux,0)),"")','DECIMAL','efe5cd2e3a144168da27007151da7bf33beeb7c077fa3e14875e9835cd1d96a9');
  seed_formula(20,'F20_TOPE_F_AUX','Matriz Consolidada!BC2','IF(frecuencia="","",frecuencia)','DECIMAL','a4440bc9fedafbf9d7599e1b8958b6286b50cb672e1e2bab4b9ea550056bb76b');
  seed_formula(21,'F21_TOPE_I_AUX','Matriz Consolidada!BD2','IF(impacto="","",impacto)','DECIMAL','c2b5422891e028690d6d965372c3d0b1b70a0dd0c49c6d37003b454eff884982');
  seed_formula(22,'F22_CAPACIDAD_F_AUX','Matriz Consolidada!BE2','IF(OR(tope_f="",f_base=""),"",MAX(0,tope_f-f_base))','DECIMAL','e17b6e298e6e900bbdff5a87faba5eefcef2f1d64d09d244be8fa6c910dcd871');
  seed_formula(23,'F23_CAPACIDAD_I_AUX','Matriz Consolidada!BF2','IF(OR(tope_i="",i_base=""),"",MAX(0,tope_i-i_base))','DECIMAL','6b6043e35ca0df3e54eccad0b931f56a17bf4a79fd819d8a048e6c10a23e6244');
  seed_formula(24,'F24_RESTO_AUX','Matriz Consolidada!BG2','IF(OR(suma_residual_redondeada_aux="",f_base="",i_base=""),"",MAX(0,suma_residual_redondeada_aux-(f_base+i_base)))','DECIMAL','3ce9b2228b152340b86a06fc714feb929496343f2841a38128183349bafce35c');
  seed_formula(25,'F25_PREFIERE_I_AUX','Matriz Consolidada!BH2','IF(OR(impacto_residual_aux="",frecuencia_residual_aux=""),"",IF(MOD(impacto_residual_aux,1)>MOD(frecuencia_residual_aux,1),1,IF(MOD(impacto_residual_aux,1)=MOD(frecuencia_residual_aux,1),1,0)))','DECIMAL','bee24640968d48ca2ad88ee9a1701d4147a43b14ce565d25a5171f823c674451');
  seed_formula(26,'F26_INCREMENTO_I_AUX','Matriz Consolidada!BI2','IF(OR(resto_aux="",capacidad_i_aux="",capacidad_f_aux=""),"",IF(resto_aux=0,0,IF(resto_aux=1,MIN(capacidad_i_aux,IF(OR(prefiere_i_aux=1,capacidad_f_aux=0),1,0)),MIN(capacidad_i_aux,IF(prefiere_i_aux=1,1+IF(capacidad_f_aux>0,0,1),IF(capacidad_f_aux>0,1,2))))))','DECIMAL','6c56d0746308ce4feb55dce02f89a707778f52b60f9c2c3567ceff53d2ec53a1');
  seed_formula(27,'F27_INCREMENTO_F_AUX','Matriz Consolidada!BJ2','IF(OR(resto_aux="",capacidad_f_aux="",incremento_i_aux=""),"",MIN(capacidad_f_aux,MAX(0,resto_aux-incremento_i_aux)))','DECIMAL','a13de9cd39f2e89f2439a4ff64544d9d747a07c0205831b6085134b267255121');
  seed_formula(28,'F28_VALOR_RIESGO_RESIDUAL_AUX','Matriz Consolidada!BK2','IFERROR(MAX(ROUND(frecuencia_residual_aux+impacto_residual_aux-1+efectividad_total_ponderada,0),1),"")','DECIMAL','0cbe76a06c29252c7947fbd004998b92871a85ed612e525b3fe366cdb414bd26');
  seed_formula(29,'F29_VERIFICACION_RIESGO_RESIDUAL','Matriz Consolidada!BL2','IFERROR(valor_riesgo_residual-valor_riesgo_residual_aux,"")','DECIMAL','26458fbc1dbe6213aaf0991d4e94979798c5ed7dd60d2e35e7bfb6b2fd44cd1c');
  seed_formula(30,'F30_VRR_2','Matriz Consolidada!BM2','IFERROR(frecuencia_residual+impacto_residual-1,"")','DECIMAL','7d5d5f18438b5164beb90e20b71d3d78fd07e28272d6c4b9d546159653978982');
  seed_formula(31,'F31_VERIFICAR_VRR_2','Matriz Consolidada!BN2','IFERROR(valor_riesgo_residual-vrr_2,"")','DECIMAL','3987d16696bce41f691c4a52899f1b4b635a4910bc16dd11feade2b5479e07c2');
  seed_formula(32,'F32_VERIFICAR_FRECUENCIA','Matriz Consolidada!BO2','IFERROR(frecuencia-frecuencia_residual,"")','DECIMAL','6d8e37f04cd007fae24f5ce872d0e5a72ffe42248f2ef04d67a09e4422c850cb');
  seed_formula(33,'F33_VERIFICAR_IMPACTO','Matriz Consolidada!BP2','IFERROR(impacto-impacto_residual,"")','DECIMAL','b30444a0cdd1ffaa0b4cde7a1b54e51cba97eccc584104bd42ea51aead6cc6ab');
  seed_formula(34,'F34_DIFERENCIA_VRI_VRR','Matriz Consolidada!BQ2','IFERROR(valor_riesgo_inherente-valor_riesgo_residual,"")','DECIMAL','6267d4721727998c131b463adef6a58eafda2e91129b683997cf88fd83310527');

  DBMS_OUTPUT.PUT_LINE('FORMULAS_INSTITUCIONALES_SEEDED=34');
  DBMS_OUTPUT.PUT_LINE('NATIVE_FUNCTIONS_REQUIRED=10');
  DBMS_OUTPUT.PUT_LINE('WEIGHT_PARAMETERS_REQUIRED=3');
  COMMIT;
END;
/

PROMPT CARGA_FORMULA_INSTITUCIONAL_315_APPLIED
EXIT SUCCESS
