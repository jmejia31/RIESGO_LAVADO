-- Oracle 11g / MANUAL. Ejecutar después de 31 y 32.
-- Alcance exclusivo: RL_MR_RIESGOS.RIE_NOMBRE, emparejado por RIE_CODIGO.
-- No modifica RIE_ID, RIE_DESCRIPCION, evaluaciones, proyecciones, formularios, V1/V2 ni hashes.
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
  TYPE t_item IS RECORD (code VARCHAR2(30), name VARCHAR2(250));
  TYPE t_items IS TABLE OF t_item INDEX BY PLS_INTEGER;
  TYPE t_code IS RECORD (code VARCHAR2(30));
  TYPE t_codes IS TABLE OF t_code INDEX BY PLS_INTEGER;
  items t_items;
  all_codes t_codes;
  db_rows NUMBER;
  matched_codes NUMBER := 0;
  missing_codes NUMBER := 0;
  duplicate_codes NUMBER := 0;
  backup_table NUMBER;
  backup_rows NUMBER;
  source_rows NUMBER;
  code_rows NUMBER;
  expected_updates NUMBER := 0;
  actual_updates NUMBER := 0;
BEGIN
  SELECT COUNT(*) INTO backup_table FROM USER_TABLES WHERE TABLE_NAME = 'RL_MR_RIESGOS_NOMBRES_BKP_20260923';
  IF backup_table = 0 THEN
    RAISE_APPLICATION_ERROR(-20936, 'Corrección bloqueada: ejecutar primero 32_backup_rl_mr_riesgos_nombres.sql.');
  END IF;
  SELECT COUNT(*) INTO backup_rows FROM RL_MR_RIESGOS_NOMBRES_BKP_20260923;
  IF backup_rows <> 59 THEN
    RAISE_APPLICATION_ERROR(-20937, 'Corrección bloqueada: backup incompleto; filas=' || backup_rows || ', esperado=59.');
  END IF;
  all_codes(1).code := 'ROTR-AFIL-1'; all_codes(2).code := 'ROTR-AFIL-2';
  all_codes(3).code := 'ROTR-PENS-3'; all_codes(4).code := 'ROTR-PENS-4';
  all_codes(5).code := 'ROTR-SUBS-5'; all_codes(6).code := 'ROTR-SUBS-6';
  all_codes(7).code := 'ROTR-COMPRAS-7'; all_codes(8).code := 'ROTR-RRHH-8';
  all_codes(9).code := 'ROTR-TESO-9'; all_codes(10).code := 'ROTR-PENS-10';
  all_codes(11).code := 'ROTR-TESO-11'; all_codes(12).code := 'ROTR-PATRONAL-12';
  all_codes(13).code := 'ROTR-IVM-13'; all_codes(14).code := 'ROTR-RRHH-14';
  all_codes(15).code := 'ROTR-RRHH-15'; all_codes(16).code := 'ROTR-RRHH-16';
  all_codes(17).code := 'ROTR-PATRONALCOMP-17'; all_codes(18).code := 'ROTR-COMPRAS-18';
  all_codes(19).code := 'ROTR-RRHHTESORERI-19'; all_codes(20).code := 'ROTR-COMPRASTESOR-20';
  all_codes(21).code := 'ROTR-COMPRASGAYF-21'; all_codes(22).code := 'ROTR-USIGTICTRANS-22';
  all_codes(23).code := 'ROTR-ALMACENBIENE-23'; all_codes(24).code := 'RCUMP-COMPRAS-24';
  all_codes(25).code := 'RCUMP-COMPRAS-25'; all_codes(26).code := 'RCUMP-COMPRASRRHH-26';
  all_codes(27).code := 'RCUMP-COMPRAS-27'; all_codes(28).code := 'RCUMP-COMPRAS-28';
  all_codes(29).code := 'RCUMP-COMPRAS-29'; all_codes(30).code := 'RCUMP-COMPRAS-30';
  all_codes(31).code := 'RCUMP-COMPRAS-31'; all_codes(32).code := 'RCUMP-COMPRAS-32';
  all_codes(33).code := 'RCUMP-COMPRAS-33'; all_codes(34).code := 'RCUMP-COMPRAS-34';
  all_codes(35).code := 'RCUMP-COMPRAS-35'; all_codes(36).code := 'RCUMP-COMPRAS-36';
  all_codes(37).code := 'RCUMP-COMPRAS-37'; all_codes(38).code := 'RCUMP-COMPRAS-38';
  all_codes(39).code := 'RCUMP-COMPRASGTIC-39'; all_codes(40).code := 'RCUMP-COMPRAS-40';
  all_codes(41).code := 'RCUMP-COMPRAS-41'; all_codes(42).code := 'RCUMP-COMPRAS-42';
  all_codes(43).code := 'RCUMP-COMPRAS-43'; all_codes(44).code := 'RCUMP-COMPRASCONTR-44';
  all_codes(45).code := 'RCUMP-CONTROLPATRO-45'; all_codes(46).code := 'RCUMP-CONTROLPATRO-46';
  all_codes(47).code := 'RCUMP-CONTROLPATRO-47'; all_codes(48).code := 'RCUMP-CONTROLPATRO-48';
  all_codes(49).code := 'ROP-CUMP-49'; all_codes(50).code := 'ROP-CUMP-50';
  all_codes(51).code := 'ROP-CUMP-51'; all_codes(52).code := 'ROP-CUMP-52';
  all_codes(53).code := 'ROP-CUMP-53'; all_codes(54).code := 'ROP-CUMP-54';
  all_codes(55).code := 'ROP-CUMP-55'; all_codes(56).code := 'ROP-CUMP-56';
  all_codes(57).code := 'ROP-CUMP-57'; all_codes(58).code := 'ROP-CUMP-58';
  all_codes(59).code := 'ROP-CUMP-59';
  source_rows := all_codes.COUNT;
  SELECT COUNT(*) INTO db_rows FROM RL_MR_RIESGOS;
  SELECT COUNT(*) INTO duplicate_codes FROM
   (SELECT RIE_CODIGO FROM RL_MR_RIESGOS GROUP BY RIE_CODIGO HAVING COUNT(*) > 1);
  FOR i IN 1..all_codes.COUNT LOOP
    SELECT COUNT(*) INTO code_rows FROM RL_MR_RIESGOS WHERE RIE_CODIGO = all_codes(i).code;
    IF code_rows = 1 THEN matched_codes := matched_codes + 1;
    ELSIF code_rows = 0 THEN missing_codes := missing_codes + 1;
    END IF;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('SOURCE_ROWS=' || source_rows);
  DBMS_OUTPUT.PUT_LINE('DB_ROWS=' || db_rows);
  DBMS_OUTPUT.PUT_LINE('MATCHED_CODES=' || matched_codes);
  DBMS_OUTPUT.PUT_LINE('MISSING_CODES=' || missing_codes);
  DBMS_OUTPUT.PUT_LINE('DUPLICATE_CODES=' || duplicate_codes);
  IF db_rows <> 59 OR source_rows <> 59 OR matched_codes <> 59 OR missing_codes <> 0 OR duplicate_codes <> 0 THEN
    RAISE_APPLICATION_ERROR(-20935, 'Inventario bloqueado: la fuente y RL_MR_RIESGOS no coinciden exactamente.');
  END IF;
  items(1).code := 'ROTR-AFIL-1'; items(1).name := UNISTR('Creaci\00F3n y activaci\00F3n de registros en el sistema correspondientes a identidades falsas, suplantadas o personas que no cumplen criterios normativos, contractuales o internos para su afiliacion.');
  items(2).code := 'ROTR-AFIL-2'; items(2).name := UNISTR('Emisi\00F3n de autorizaci\00F3n o prestaci\00F3n efectiva de un servicio de salud a una persona que, no figura como afiliado al sistema o tiene afiliacion suspendida');
  items(3).code := 'ROTR-PENS-3'; items(3).name := UNISTR('Aprobaci\00F3n y reconocimiento de una prestaci\00F3n pensional a una persona que no cumple los requisitos legales');
  items(4).code := 'ROTR-PENS-4'; items(4).name := UNISTR('Pago de una o m\00E1s cuotas de pension, posteriores a la fecha real de fallecimiento del pensionado, sin que el sistema del IHSS haya bloqueado oportunamente el beneficio');
  items(5).code := 'ROTR-SUBS-5'; items(5).name := UNISTR('Reconocimiento y pago de subsidios o pension por invalidez, derivados de un accidente laboral que no ocurrio, ocurrio fuera del ambito laboral o fue respaldado con informaci\00F3n falsa.');
  items(6).code := 'ROTR-SUBS-6'; items(6).name := UNISTR('Aprobaci\00F3n y pago de incapacidades prolongadas que no cuenten con soporte medico adecuado, emitidas de manera reiterada por el mismo profesional o superan tiempos est\00E1ndar para el diagn\00F3stico reportado');
  items(7).code := 'ROTR-COMPRAS-7'; items(7).name := UNISTR('Pago de cuentas m\00E9dicas o registros contables que incluyen medicamentos no dispensados al derechohabiente, entregados parcialmente pero facturados en su totalidad, sustituidos por otros de menor valor, registrados sin soporte de entrega o sin firma d');
  items(8).code := 'ROTR-RRHH-8'; items(8).name := UNISTR('Adjudicaci\00F3n o celebraci\00F3n de contratos en los que se dise\00F1an pliegos o t\00E9rminos de referencia a la medida de un proponente, se omiten requisitos legales o t\00E9cnicos, se alteran evaluaciones, se permite participaci\00F3n de oferentes no elegibles, se mani');
  items(9).code := 'ROTR-TESO-9'; items(9).name := UNISTR('Desembolso de recursos cuando se modifica la cuenta bancaria sin validaci\00F3n robusta, se registran cuentas falsas en el sistema, existe suplantaci\00F3n de beneficiario o proveedor o se realizan pagos a terceros no autorizados.');
  items(10).code := 'ROTR-PENS-10'; items(10).name := UNISTR('Reconocimiento o pago de pension por viudez o ascendencia a quien no acredita v\00EDnculo v\00E1lido, pensi\00F3n de orfandad sin cumplir requisitos de edad o dependencia o representantes que cobran sin legitimaci\00F3n.');
  items(11).code := 'ROTR-PATRONAL-12'; items(11).name := UNISTR('Empresas afiliadas que no tiene operaciones reales, no cuenta con infraestructura, empleados o actividad verificable, declara planillas ficticias o presenta aportes m\00EDnimos previos a reconocimiento pensional o incapacidad.');
  items(12).code := 'ROTR-IVM-13'; items(12).name := UNISTR('Reconocimiento de prestacion economica cuando se adicionan periodos no cotizadas realmente, se duplican per\00EDodos ya contabilizados, se modifican historiales laborales en el sistema o se corrigen registros sin trazabilidad.');
  items(13).code := 'ROTR-RRHH-15'; items(13).name := UNISTR('Aplicaci\00F3n de incrementos salariales sin respaldo presupuestario ni aprobaci\00F3n formal conforme a normativa interna');
  items(14).code := 'ROTR-RRHH-16'; items(14).name := UNISTR('Adjudicaci\00F3n de contratos incumpliendo requisitos de transparencia, competencia y aprobaci\00F3n interna.');
  items(15).code := 'ROTR-PATRONALCOMP-17'; items(15).name := UNISTR('Adjudicaci\00F3n y contrataci\00F3n con proveedor inexistente, ficticio, inhabilitado o sin capacidad real para prestar el servicio.');
  items(16).code := 'ROTR-RRHHTESORERI-19'; items(16).name := UNISTR('Riesgo de pagos indebidos a trav\00E9s de planilla a una misma cuenta bancaria');
  items(17).code := 'ROTR-COMPRASTESOR-20'; items(17).name := UNISTR('Pagos indebidos por ejecuci\00F3n anticipada sin soporte o incumpliendo condiciones contractuales');
  items(18).code := 'ROTR-USIGTICTRANS-22'; items(18).name := UNISTR('Divulgaci\00F3n indebida de informaci\00F3n confidencial');
  items(19).code := 'ROTR-ALMACENBIENE-23'; items(19).name := UNISTR('Robo y hurto de inventarios (medicamentos, insumos m\00E9dicos, equipos o materiales medicos, mobiliario)');
  items(20).code := 'RCUMP-COMPRAS-24'; items(20).name := UNISTR('Registro de proveedores con informaci\00F3n inconsistente');
  items(21).code := 'RCUMP-COMPRASRRHH-26'; items(21).name := UNISTR('Recepci\00F3n indebida de donaciones, regalos o beneficios por parte de funcionarios en el ejercicio de sus funciones');
  items(22).code := 'RCUMP-COMPRAS-27'; items(22).name := UNISTR('Ser una Persona Expuesta Pol\00EDticamente (PEP) , ser su c\00F3nyuge o persona vinculada por uni\00F3n de hecho o parientes dentro del cuarto grado de consanguinidad o segundo de afinidad del proveedor ofertante');
  items(23).code := 'RCUMP-COMPRAS-28'; items(23).name := UNISTR('Conflicto de inter\00E9s por v\00EDnculos familiares en procesos de contrataci\00F3n con proveedores');
  items(24).code := 'RCUMP-COMPRAS-30'; items(24).name := UNISTR('Fragmentaci\00F3n indebida de procesos de contrataci\00F3n y/o licitaciones para evadir controles legales y administrativos');
  items(25).code := 'RCUMP-COMPRAS-31'; items(25).name := UNISTR('Falta de segregaci\00F3n de funciones entre el dise\00F1o de pliegos y la evaluaci\00F3n de ofertas en procesos de contrataci\00F3n');
  items(26).code := 'RCUMP-COMPRAS-32'; items(26).name := UNISTR('Participaci\00F3n de empresas vinculadas en procesos de contrataci\00F3n, generando colusi\00F3n y simulaci\00F3n de competencia');
  items(27).code := 'RCUMP-COMPRAS-33'; items(27).name := UNISTR('Proveedor carece de experiencia con el producto, servicio, sector o industria, cuenta con personal insuficiente o mal calificado, no dispone de instalaciones adecuadas, o de alguna otra forma parece ser incapaz de cumplir con la operaci\00F3n propuesta');
  items(28).code := 'RCUMP-COMPRAS-34'; items(28).name := UNISTR('Adjudicaci\00F3n de contratos sin cumplimiento del procedimiento de contrataci\00F3n y evaluaci\00F3n t\00E9cnica\2013econ\00F3mica');
  items(29).code := 'RCUMP-COMPRAS-35'; items(29).name := UNISTR('Definici\00F3n y otorgamiento de anticipos contractuales desproporcionados sin garant\00EDas adecuadas de ejecuci\00F3n');
  items(30).code := 'RCUMP-COMPRAS-36'; items(30).name := UNISTR('Inconsistencias documentales entre orden de compra, recepci\00F3n, facturaci\00F3n y orden de pago que evidencian posibles alteraciones');
  items(31).code := 'RCUMP-COMPRAS-38'; items(31).name := UNISTR('Ausencia de respaldos que evidencien una supervisi\00F3n efectiva y en norma respecto de la ejecuci\00F3n del contrato.');
  items(32).code := 'RCUMP-COMPRASGTIC-39'; items(32).name := UNISTR('Debilidad en los sistemas de informaci\00F3n que impide la trazabilidad de los procesos de contrataci\00F3n y ejecuci\00F3n contractual');
  items(33).code := 'RCUMP-COMPRAS-40'; items(33).name := UNISTR('Manipulaci\00F3n de procesos de contrataci\00F3n mediante t\00E9rminos de referencia ambiguos y modificaciones injustificadas que restringen la libre concurrencia de oferentes');
  items(34).code := 'RCUMP-COMPRAS-42'; items(34).name := UNISTR('Revelacion, no autorizada, de informacion a determinada(s) empresa(s), en el marco de una licitaci\00F3n p\00FAblica.');
  items(35).code := 'RCUMP-COMPRAS-43'; items(35).name := UNISTR('Aprobaci\00F3n, permanencia o contrataci\00F3n de proveedores que presentan informaci\00F3n incompleta, falsa o manipulada respecto a su identidad, beneficiarios finales, situaci\00F3n financiera, antecedentes legales o cumplimiento normativo');
  items(36).code := 'RCUMP-COMPRASCONTR-44'; items(36).name := UNISTR('Vinculaci\00F3n o permanencia de una PEP sin clasificaci\00F3n como tal en los sistemas internos, sin aplicaci\00F3n de debida diligencia incrementada ni aprobaci\00F3n de instancias superiores');
  items(37).code := 'RCUMP-CONTROLPATRO-45'; items(37).name := UNISTR('Aprobaci\00F3n y activaci\00F3n de clientes sin validaci\00F3n adecuada de identidad, actividad econ\00F3mica, origen de fondos, beneficiario final o informaci\00F3n legal requerida');
  items(38).code := 'RCUMP-CONTROLPATRO-46'; items(38).name := UNISTR('Vinculaci\00F3n y operaci\00F3n de varios clientes aparentemente independientes que en realidad est\00E1n relacionados estructural o econ\00F3micamente, sin aplicaci\00F3n de controles de debida diligencia ni an\00E1lisis de vinculaci\00F3n');
  items(39).code := 'RCUMP-CONTROLPATRO-47'; items(39).name := UNISTR('Cliente activo con documentaci\00F3n vencida o informaci\00F3n desactualizada que contin\00FAa operando sin completar el proceso de actualizaci\00F3n requerido');
  items(40).code := 'RCUMP-CONTROLPATRO-48'; items(40).name := UNISTR('Detecci\00F3n posterior a la vinculaci\00F3n de que un cliente, socio, beneficiario final o representante legal se encuentra reportado en listas de sanciones, listas vinculantes o listas de monitoreo');
  items(41).code := 'ROP-CUMP-52'; items(41).name := UNISTR('Incumplimiento en el desarrollo del Plan de Capacitaciones en Temas de Prevenci\00F3n de Lavado de Activos y Financiamiento del Terrorismo');
  items(42).code := 'ROP-CUMP-54'; items(42).name := UNISTR('Incumplimiento en la implementaci\00F3n de la Declaraci\00F3n Jurada de Ingresos, Activos y Pasivos');
  items(43).code := 'ROP-CUMP-55'; items(43).name := UNISTR('Inexistencia de un sistema de calificaci\00F3n de Riesgos de Proveedores');
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
EXCEPTION WHEN OTHERS THEN
  ROLLBACK;
  RAISE;
END;
/
