-- Oracle 11g / FUENTE COMPARTIDA. No ejecutar directamente sin argumento.
-- Invocadores autorizados:
--   33_corregir_nombres_riesgos_desde_fuente_canonica.sql -> CORRECT
--   34_postcheck_nombres_riesgos.sql -> POSTCHECK
--
-- RL_MR_RIESGOS.RIE_NOMBRE es VARCHAR2(250). La fuente institucional se
-- representa exactamente como la migración: cuando el título supera 250
-- caracteres, se conserva SUBSTR(título, 1, 250). ROTR-COMPRAS-7 y
-- ROTR-RRHH-8 aplican explícitamente esa regla y ambos invocadores usan
-- esta misma fuente, evitando divergencia entre correctivo y postcheck.
SET SERVEROUTPUT ON SIZE UNLIMITED
SET LINESIZE 260
SET PAGESIZE 200
DECLARE
  v_mode VARCHAR2(20) := UPPER('&1');
  TYPE t_item IS RECORD (code VARCHAR2(30), name VARCHAR2(250));
  TYPE t_items IS TABLE OF t_item INDEX BY PLS_INTEGER;
  items t_items;
  db_rows NUMBER;
  duplicate_codes NUMBER;
  matched_codes NUMBER := 0;
  missing_codes NUMBER := 0;
  diffs NUMBER := 0;
  code_rows NUMBER;
  source_rows NUMBER;
  backup_table NUMBER;
  backup_rows NUMBER;
  expected_updates NUMBER := 0;
  actual_updates NUMBER := 0;
  sample_rows NUMBER := 0;
  sample_name VARCHAR2(250);

  FUNCTION canonical_name(p_name VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    IF LENGTH(p_name) > 250 THEN
      RETURN SUBSTR(p_name, 1, 250);
    END IF;
    RETURN p_name;
  END;

  PROCEDURE load_source IS
  BEGIN
    items(1).code := 'ROTR-AFIL-1'; items(1).name := UNISTR('Creaci\00F3n y activaci\00F3n de registros en el sistema correspondientes a identidades falsas, suplantadas o personas que no cumplen criterios normativos, contractuales o internos para su afiliacion.');
    items(2).code := 'ROTR-AFIL-2'; items(2).name := UNISTR('Emisi\00F3n de autorizaci\00F3n o prestaci\00F3n efectiva de un servicio de salud a una persona que, no figura como afiliado al sistema o tiene afiliacion suspendida');
    items(3).code := 'ROTR-PENS-3'; items(3).name := UNISTR('Aprobaci\00F3n y reconocimiento de una prestaci\00F3n pensional a una persona que no cumple los requisitos legales');
    items(4).code := 'ROTR-PENS-4'; items(4).name := UNISTR('Pago de una o m\00E1s cuotas de pension, posteriores a la fecha real de fallecimiento del pensionado, sin que el sistema del IHSS haya bloqueado oportunamente el beneficio');
    items(5).code := 'ROTR-SUBS-5'; items(5).name := UNISTR('Reconocimiento y pago de subsidios o pension por invalidez, derivados de un accidente laboral que no ocurrio, ocurrio fuera del ambito laboral o fue respaldado con informaci\00F3n falsa.');
    items(6).code := 'ROTR-SUBS-6'; items(6).name := UNISTR('Aprobaci\00F3n y pago de incapacidades prolongadas que no cuenten con soporte medico adecuado, emitidas de manera reiterada por el mismo profesional o superan tiempos est\00E1ndar para el diagn\00F3stico reportado');
    items(7).code := 'ROTR-COMPRAS-7'; items(7).name := canonical_name(UNISTR('Pago de cuentas m\00E9dicas o registros contables que incluyen medicamentos no dispensados al derechohabiente, entregados parcialmente pero facturados en su totalidad, sustituidos por otros de menor valor, registrados sin soporte de entrega o sin firma de recibido.'));
    items(8).code := 'ROTR-RRHH-8'; items(8).name := canonical_name(UNISTR('Adjudicaci\00F3n o celebraci\00F3n de contratos en los que se dise\00F1an pliegos o t\00E9rminos de referencia a la medida de un proponente, se omiten requisitos legales o t\00E9cnicos, se alteran evaluaciones, se permite participaci\00F3n de oferentes no elegibles, se manipulan estudios previos o an\00E1lisis de mercado.'));
    items(9).code := 'ROTR-TESO-9'; items(9).name := UNISTR('Desembolso de recursos cuando se modifica la cuenta bancaria sin validaci\00F3n robusta, se registran cuentas falsas en el sistema, existe suplantaci\00F3n de beneficiario o proveedor o se realizan pagos a terceros no autorizados.');
    items(10).code := 'ROTR-PENS-10'; items(10).name := UNISTR('Reconocimiento o pago de pension por viudez o ascendencia a quien no acredita v\00EDnculo v\00E1lido, pensi\00F3n de orfandad sin cumplir requisitos de edad o dependencia o representantes que cobran sin legitimaci\00F3n.');
    items(11).code := 'ROTR-TESO-11'; items(11).name := UNISTR('Transferencias a zonas de alto riesgo');
    items(12).code := 'ROTR-PATRONAL-12'; items(12).name := UNISTR('Empresas afiliadas que no tiene operaciones reales, no cuenta con infraestructura, empleados o actividad verificable, declara planillas ficticias o presenta aportes m\00EDnimos previos a reconocimiento pensional o incapacidad.');
    items(13).code := 'ROTR-IVM-13'; items(13).name := UNISTR('Reconocimiento de prestacion economica cuando se adicionan periodos no cotizadas realmente, se duplican per\00EDodos ya contabilizados, se modifican historiales laborales en el sistema o se corrigen registros sin trazabilidad.');
    items(14).code := 'ROTR-RRHH-14'; items(14).name := UNISTR('Pago de planilla o desembolso de salarios, a empleados inexistentes dentro del IHSS (empleados fantasma) o a cuentas bancarias controladas por tercerros');
    items(15).code := 'ROTR-RRHH-15'; items(15).name := UNISTR('Aplicaci\00F3n de incrementos salariales sin respaldo presupuestario ni aprobaci\00F3n formal conforme a normativa interna');
    items(16).code := 'ROTR-RRHH-16'; items(16).name := UNISTR('Adjudicaci\00F3n de contratos incumpliendo requisitos de transparencia, competencia y aprobaci\00F3n interna.');
    items(17).code := 'ROTR-PATRONALCOMP-17'; items(17).name := UNISTR('Adjudicaci\00F3n y contrataci\00F3n con proveedor inexistente, ficticio, inhabilitado o sin capacidad real para prestar el servicio.');
    items(18).code := 'ROTR-COMPRAS-18'; items(18).name := UNISTR('Riesgo de pago indebido a proveedores por bienes o servicios no recibidos o no ejecutados');
    items(19).code := 'ROTR-RRHHTESORERI-19'; items(19).name := UNISTR('Riesgo de pagos indebidos a trav\00E9s de planilla a una misma cuenta bancaria');
    items(20).code := 'ROTR-COMPRASTESOR-20'; items(20).name := UNISTR('Pagos indebidos por ejecuci\00F3n anticipada sin soporte o incumpliendo condiciones contractuales');
    items(21).code := 'ROTR-COMPRASGAYF-21'; items(21).name := UNISTR('Fraccionamiento recurrente de compras');
    items(22).code := 'ROTR-USIGTICTRANS-22'; items(22).name := UNISTR('Divulgaci\00F3n indebida de informaci\00F3n confidencial');
    items(23).code := 'ROTR-ALMACENBIENE-23'; items(23).name := UNISTR('Robo y hurto de inventarios (medicamentos, insumos m\00E9dicos, equipos o materiales medicos, mobiliario)');
    items(24).code := 'RCUMP-COMPRAS-24'; items(24).name := UNISTR('Registro de proveedores con informaci\00F3n inconsistente');
    items(25).code := 'RCUMP-COMPRAS-25'; items(25).name := UNISTR('Empresas que no estan inscritas como proveedores en el IHSS y si inscritos en ONCAE y se realizan compras.');
    items(26).code := 'RCUMP-COMPRASRRHH-26'; items(26).name := UNISTR('Recepci\00F3n indebida de donaciones, regalos o beneficios por parte de funcionarios en el ejercicio de sus funciones');
    items(27).code := 'RCUMP-COMPRAS-27'; items(27).name := UNISTR('Ser una Persona Expuesta Pol\00EDticamente (PEP) , ser su c\00F3nyuge o persona vinculada por uni\00F3n de hecho o parientes dentro del cuarto grado de consanguinidad o segundo de afinidad del proveedor ofertante');
    items(28).code := 'RCUMP-COMPRAS-28'; items(28).name := UNISTR('Conflicto de inter\00E9s por v\00EDnculos familiares en procesos de contrataci\00F3n con proveedores');
    items(29).code := 'RCUMP-COMPRAS-29'; items(29).name := UNISTR('Imposisicon de empleados, en procesos de evaluacion de lictaciones');
    items(30).code := 'RCUMP-COMPRAS-30'; items(30).name := UNISTR('Fragmentaci\00F3n indebida de procesos de contrataci\00F3n y/o licitaciones para evadir controles legales y administrativos');
    items(31).code := 'RCUMP-COMPRAS-31'; items(31).name := UNISTR('Falta de segregaci\00F3n de funciones entre el dise\00F1o de pliegos y la evaluaci\00F3n de ofertas en procesos de contrataci\00F3n');
    items(32).code := 'RCUMP-COMPRAS-32'; items(32).name := UNISTR('Participaci\00F3n de empresas vinculadas en procesos de contrataci\00F3n, generando colusi\00F3n y simulaci\00F3n de competencia');
    items(33).code := 'RCUMP-COMPRAS-33'; items(33).name := UNISTR('Proveedor carece de experiencia con el producto, servicio, sector o industria, cuenta con personal insuficiente o mal calificado, no dispone de instalaciones adecuadas, o de alguna otra forma parece ser incapaz de cumplir con la operaci\00F3n propuesta');
    items(34).code := 'RCUMP-COMPRAS-34'; items(34).name := UNISTR('Adjudicaci\00F3n de contratos sin cumplimiento del procedimiento de contrataci\00F3n y evaluaci\00F3n t\00E9cnica\2013econ\00F3mica');
    items(35).code := 'RCUMP-COMPRAS-35'; items(35).name := UNISTR('Definici\00F3n y otorgamiento de anticipos contractuales desproporcionados sin garant\00EDas adecuadas de ejecuci\00F3n');
    items(36).code := 'RCUMP-COMPRAS-36'; items(36).name := UNISTR('Inconsistencias documentales entre orden de compra, recepci\00F3n, facturaci\00F3n y orden de pago que evidencian posibles alteraciones');
    items(37).code := 'RCUMP-COMPRAS-37'; items(37).name := UNISTR('Pagos anticipados');
    items(38).code := 'RCUMP-COMPRAS-38'; items(38).name := UNISTR('Ausencia de respaldos que evidencien una supervisi\00F3n efectiva y en norma respecto de la ejecuci\00F3n del contrato.');
    items(39).code := 'RCUMP-COMPRASGTIC-39'; items(39).name := UNISTR('Debilidad en los sistemas de informaci\00F3n que impide la trazabilidad de los procesos de contrataci\00F3n y ejecuci\00F3n contractual');
    items(40).code := 'RCUMP-COMPRAS-40'; items(40).name := UNISTR('Manipulaci\00F3n de procesos de contrataci\00F3n mediante t\00E9rminos de referencia ambiguos y modificaciones injustificadas que restringen la libre concurrencia de oferentes');
    items(41).code := 'RCUMP-COMPRAS-41'; items(41).name := UNISTR('Modificaciones sustanciales e injustificadas en las condiciones y/o requisitos contractuales establecidos inicialmente para el cumplimiento del contrato.');
    items(42).code := 'RCUMP-COMPRAS-42'; items(42).name := UNISTR('Revelacion, no autorizada, de informacion a determinada(s) empresa(s), en el marco de una licitaci\00F3n p\00FAblica.');
    items(43).code := 'RCUMP-COMPRAS-43'; items(43).name := UNISTR('Aprobaci\00F3n, permanencia o contrataci\00F3n de proveedores que presentan informaci\00F3n incompleta, falsa o manipulada respecto a su identidad, beneficiarios finales, situaci\00F3n financiera, antecedentes legales o cumplimiento normativo');
    items(44).code := 'RCUMP-COMPRASCONTR-44'; items(44).name := UNISTR('Vinculaci\00F3n o permanencia de una PEP sin clasificaci\00F3n como tal en los sistemas internos, sin aplicaci\00F3n de debida diligencia incrementada ni aprobaci\00F3n de instancias superiores');
    items(45).code := 'RCUMP-CONTROLPATRO-45'; items(45).name := UNISTR('Aprobaci\00F3n y activaci\00F3n de clientes sin validaci\00F3n adecuada de identidad, actividad econ\00F3mica, origen de fondos, beneficiario final o informaci\00F3n legal requerida');
    items(46).code := 'RCUMP-CONTROLPATRO-46'; items(46).name := UNISTR('Vinculaci\00F3n y operaci\00F3n de varios clientes aparentemente independientes que en realidad est\00E1n relacionados estructural o econ\00F3micamente, sin aplicaci\00F3n de controles de debida diligencia ni an\00E1lisis de vinculaci\00F3n');
    items(47).code := 'RCUMP-CONTROLPATRO-47'; items(47).name := UNISTR('Cliente activo con documentaci\00F3n vencida o informaci\00F3n desactualizada que contin\00FAa operando sin completar el proceso de actualizaci\00F3n requerido');
    items(48).code := 'RCUMP-CONTROLPATRO-48'; items(48).name := UNISTR('Detecci\00F3n posterior a la vinculaci\00F3n de que un cliente, socio, beneficiario final o representante legal se encuentra reportado en listas de sanciones, listas vinculantes o listas de monitoreo');
    items(49).code := 'ROP-CUMP-49'; items(49).name := UNISTR('Incumplimiento a normativa de la CNBS por no tener implementado el Programa de Prevencion de LAFT completo.');
    items(50).code := 'ROP-CUMP-50'; items(50).name := UNISTR('Fallas en el software de Datos normativos de patronos');
    items(51).code := 'ROP-CUMP-51'; items(51).name := UNISTR('Incumplimiento de responsabilidad  de ejecutar el proceso comparativo entre listas de cautelas y base de datos patronal');
    items(52).code := 'ROP-CUMP-52'; items(52).name := UNISTR('Incumplimiento en el desarrollo del Plan de Capacitaciones en Temas de Prevenci\00F3n de Lavado de Activos y Financiamiento del Terrorismo');
    items(53).code := 'ROP-CUMP-53'; items(53).name := UNISTR('Expedientes de Funcionarios y Empleados inexistentes o incompletos');
    items(54).code := 'ROP-CUMP-54'; items(54).name := UNISTR('Incumplimiento en la implementaci\00F3n de la Declaraci\00F3n Jurada de Ingresos, Activos y Pasivos');
    items(55).code := 'ROP-CUMP-55'; items(55).name := UNISTR('Inexistencia de un sistema de calificaci\00F3n de Riesgos de Proveedores');
    items(56).code := 'ROP-CUMP-56'; items(56).name := UNISTR('Expedientes de Proveedores inexistentes o incompletos');
    items(57).code := 'ROP-CUMP-57'; items(57).name := UNISTR('Expedientes de Clientes ( Patronos) inexistentes o incompletos');
    items(58).code := 'ROP-CUMP-58'; items(58).name := UNISTR('Errores en los datos de derechohabientes y/o Patronos en el sistema de informacion');
    items(59).code := 'ROP-CUMP-59'; items(59).name := UNISTR('Multas y Sanciones por respuestas fuera de tiempo');
  END;

  PROCEDURE validate_inventory IS
  BEGIN
    source_rows := items.COUNT;
    SELECT COUNT(*) INTO db_rows FROM RL_MR_RIESGOS;
    SELECT COUNT(*) INTO duplicate_codes FROM
      (SELECT RIE_CODIGO FROM RL_MR_RIESGOS GROUP BY RIE_CODIGO HAVING COUNT(*) > 1);
    FOR i IN 1..items.COUNT LOOP
      SELECT COUNT(*) INTO code_rows FROM RL_MR_RIESGOS WHERE RIE_CODIGO = items(i).code;
      IF code_rows = 1 THEN matched_codes := matched_codes + 1;
      ELSIF code_rows = 0 THEN missing_codes := missing_codes + 1;
      END IF;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('SOURCE_ROWS=' || source_rows);
    DBMS_OUTPUT.PUT_LINE('DB_ROWS=' || db_rows);
    DBMS_OUTPUT.PUT_LINE('MATCHED_CODES=' || matched_codes);
    DBMS_OUTPUT.PUT_LINE('MISSING_CODES=' || missing_codes);
    DBMS_OUTPUT.PUT_LINE('DUPLICATE_CODES=' || duplicate_codes);
    IF source_rows <> 59 OR db_rows <> 59 OR matched_codes <> 59 OR missing_codes <> 0 OR duplicate_codes <> 0 THEN
      RAISE_APPLICATION_ERROR(-20935, 'Inventario bloqueado: fuente y RL_MR_RIESGOS no coinciden exactamente.');
    END IF;
  END;

  PROCEDURE run_correction IS
  BEGIN
    SELECT COUNT(*) INTO backup_table FROM USER_TABLES WHERE TABLE_NAME = 'RL_MR_RIES_NOM_BKP_20260924';
    IF backup_table = 0 THEN
      RAISE_APPLICATION_ERROR(-20936, 'Corrección bloqueada: ejecutar primero 32_backup_rl_mr_riesgos_nombres.sql.');
    END IF;
    EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_RIES_NOM_BKP_20260924'
      INTO backup_rows;
    IF backup_rows <> 59 THEN
      RAISE_APPLICATION_ERROR(-20937, 'Corrección bloqueada: backup incompleto; filas=' || backup_rows || ', esperado=59.');
    END IF;
    FOR i IN 1..items.COUNT LOOP
      SELECT expected_updates + COUNT(*) INTO expected_updates
        FROM RL_MR_RIESGOS
       WHERE RIE_CODIGO = items(i).code
         AND (RIE_NOMBRE <> items(i).name OR RIE_NOMBRE IS NULL);
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('EXPECTED_UPDATES=' || expected_updates);
    SAVEPOINT risk_name_sync;
    FOR i IN 1..items.COUNT LOOP
      UPDATE RL_MR_RIESGOS
         SET RIE_NOMBRE = items(i).name
       WHERE RIE_CODIGO = items(i).code
         AND (RIE_NOMBRE <> items(i).name OR RIE_NOMBRE IS NULL);
      actual_updates := actual_updates + SQL%ROWCOUNT;
    END LOOP;
    DBMS_OUTPUT.PUT_LINE('ACTUAL_UPDATES=' || actual_updates);
    IF actual_updates <> expected_updates THEN
      ROLLBACK TO risk_name_sync;
      RAISE_APPLICATION_ERROR(-20933, 'Actualizaciones inesperadas: expected=' || expected_updates || ', actual=' || actual_updates);
    END IF;
    COMMIT;
    DBMS_OUTPUT.PUT_LINE('RISK_NAME_CORRECTION_STATUS=PASS');
  END;

  PROCEDURE run_postcheck IS
  BEGIN
    FOR i IN 1..items.COUNT LOOP
      SELECT COUNT(*) INTO code_rows FROM RL_MR_RIESGOS
       WHERE RIE_CODIGO = items(i).code
         AND (RIE_NOMBRE <> items(i).name OR RIE_NOMBRE IS NULL);
      diffs := diffs + code_rows;
    END LOOP;
    SELECT COUNT(*) INTO sample_rows FROM RL_MR_RIESGOS WHERE RIE_CODIGO = 'RCUMP-COMPRAS-24';
    IF sample_rows = 1 THEN
      SELECT RIE_NOMBRE INTO sample_name FROM RL_MR_RIESGOS WHERE RIE_CODIGO = 'RCUMP-COMPRAS-24';
    END IF;
    DBMS_OUTPUT.PUT_LINE('RISK_NAME_POSTCHECK_DB_ROWS=' || db_rows);
    DBMS_OUTPUT.PUT_LINE('RISK_NAME_POSTCHECK_DIFFS=' || diffs);
    DBMS_OUTPUT.PUT_LINE('RCUMP-COMPRAS-24_NAME=' || NVL(sample_name, '<NULL>'));
    DBMS_OUTPUT.PUT_LINE('RCUMP-COMPRAS-24_HAS_U+00F3=' || CASE WHEN INSTR(NVL(sample_name, ''), UNISTR('\00F3')) > 0 THEN 'YES' ELSE 'NO' END);
    DBMS_OUTPUT.PUT_LINE('RCUMP-COMPRAS-24_HAS_U+00BF=' || CASE WHEN INSTR(NVL(sample_name, ''), UNISTR('\00BF')) > 0 THEN 'YES' ELSE 'NO' END);
    IF sample_rows <> 1 OR diffs <> 0 OR sample_name IS NULL
       OR sample_name <> UNISTR('Registro de proveedores con informaci\00F3n inconsistente')
       OR INSTR(sample_name, UNISTR('\00F3')) = 0
       OR INSTR(sample_name, UNISTR('\00BF')) > 0 THEN
      DBMS_OUTPUT.PUT_LINE('RISK_NAME_POSTCHECK_STATUS=FAIL');
      RAISE_APPLICATION_ERROR(-20939, 'Postcheck bloqueado: inventario, nombres o codepoints no coinciden.');
    END IF;
    DBMS_OUTPUT.PUT_LINE('RISK_NAME_POSTCHECK_STATUS=PASS');
  END;
BEGIN
  load_source;
  IF v_mode = 'CORRECT' THEN
    validate_inventory;
    run_correction;
  ELSIF v_mode = 'POSTCHECK' THEN
    validate_inventory;
    run_postcheck;
  ELSE
    RAISE_APPLICATION_ERROR(-20934, 'Modo inválido. Use CORRECT o POSTCHECK.');
  END IF;
EXCEPTION WHEN OTHERS THEN
  IF v_mode = 'CORRECT' THEN ROLLBACK; END IF;
  RAISE;
END;
/
