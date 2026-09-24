-- Oracle 11g / MANUAL. Ejecutar después de 36 y 37.
-- Alcance exclusivo: RL_MR_RIESGOS.RIE_DESCRIPCION.
-- Es una reparación determinista de codificación; NO sincroniza descripciones desde Excel.
-- Requiere RL_MR_RIES_DESC_BKP_20260924 antes de cualquier UPDATE.
SET SERVEROUTPUT ON SIZE UNLIMITED
DECLARE
  expected_updates NUMBER := 0;
  actual_updates NUMBER := 0;
  v_fixed VARCHAR2(32767);
  backup_table NUMBER;
  backup_rows NUMBER;
  db_rows NUMBER;
  duplicate_codes NUMBER;
  FUNCTION fix_description(p_text VARCHAR2) RETURN VARCHAR2 IS
    v VARCHAR2(32767) := p_text;
  BEGIN
    IF v IS NULL THEN RETURN NULL; END IF;
    -- U+FFFD/mojibake sin contexto se conserva para que 39 lo bloquee.
    -- No se convierte a U+00BF porque eso perdería evidencia del carácter original.
    v := REPLACE(v, UNISTR('\00EF\00BFo'), UNISTR('\00F1o'));
    v := REPLACE(v, UNISTR('Descripci\00BFn'), UNISTR('Descripci\00F3n'));
    v := REPLACE(v, UNISTR('descripci\00BFn'), UNISTR('descripci\00F3n'));
    v := REPLACE(v, UNISTR('informaci\00BFn'), UNISTR('informaci\00F3n'));
    v := REPLACE(v, UNISTR('vinculaci\00BFn'), UNISTR('vinculaci\00F3n'));
    v := REPLACE(v, UNISTR('P\00BFrdidas'), UNISTR('P\00E9rdidas'));
    v := REPLACE(v, UNISTR('p\00BFrdidas'), UNISTR('p\00E9rdidas'));
    v := REPLACE(v, UNISTR('econ\00BFmicas'), UNISTR('econ\00F3micas'));
    v := REPLACE(v, UNISTR('verificaci\00BFn'), UNISTR('verificaci\00F3n'));
    v := REPLACE(v, UNISTR('validaci\00BFn'), UNISTR('validaci\00F3n'));
    v := REPLACE(v, UNISTR('instituci\00BFn'), UNISTR('instituci\00F3n'));
    v := REPLACE(v, UNISTR('autom\00BFticos'), UNISTR('autom\00E1ticos'));
    v := REPLACE(v, UNISTR('il\00BFcitas'), UNISTR('il\00EDcitas'));
    v := REPLACE(v, UNISTR('evaluaci\00BFn'), UNISTR('evaluaci\00F3n'));
    v := REPLACE(v, UNISTR('gesti\00BFn'), UNISTR('gesti\00F3n'));
    v := REPLACE(v, UNISTR('aprobaci\00BFn'), UNISTR('aprobaci\00F3n'));
    v := REPLACE(v, UNISTR('adjudicaci\00BFn'), UNISTR('adjudicaci\00F3n'));
    v := REPLACE(v, UNISTR('contrataci\00BFn'), UNISTR('contrataci\00F3n'));
    v := REPLACE(v, UNISTR('licitaci\00BFn'), UNISTR('licitaci\00F3n'));
    v := REPLACE(v, UNISTR('pensi\00BFn'), UNISTR('pensi\00F3n'));
    v := REPLACE(v, UNISTR('prestaci\00BFn'), UNISTR('prestaci\00F3n'));
    v := REPLACE(v, UNISTR('definici\00BFn'), UNISTR('definici\00F3n'));
    v := REPLACE(v, UNISTR('ejecuci\00BFn'), UNISTR('ejecuci\00F3n'));
    v := REPLACE(v, UNISTR('supervisi\00BFn'), UNISTR('supervisi\00F3n'));
    v := REPLACE(v, UNISTR('prevenci\00BFn'), UNISTR('prevenci\00F3n'));
    v := REPLACE(v, UNISTR('documentaci\00BFn'), UNISTR('documentaci\00F3n'));
    v := REPLACE(v, UNISTR('capacitaci\00BFn'), UNISTR('capacitaci\00F3n'));
    v := REPLACE(v, UNISTR('operaci\00BFn'), UNISTR('operaci\00F3n'));
    v := REPLACE(v, UNISTR('organizaci\00BFn'), UNISTR('organizaci\00F3n'));
    v := REPLACE(v, UNISTR('funci\00BFn'), UNISTR('funci\00F3n'));
    v := REPLACE(v, UNISTR('situaci\00BFn'), UNISTR('situaci\00F3n'));
    v := REPLACE(v, UNISTR('administraci\00BFn'), UNISTR('administraci\00F3n'));
    v := REPLACE(v, UNISTR('identificaci\00BFn'), UNISTR('identificaci\00F3n'));
    v := REPLACE(v, UNISTR('protecci\00BFn'), UNISTR('protecci\00F3n'));
    v := REPLACE(v, UNISTR('calificaci\00BFn'), UNISTR('calificaci\00F3n'));
    v := REPLACE(v, UNISTR('relaci\00BFn'), UNISTR('relaci\00F3n'));
    v := REPLACE(v, UNISTR('p\00BAblica'), UNISTR('p\00FAblica'));
    v := REPLACE(v, UNISTR('m\00BFs'), UNISTR('m\00E1s'));
    v := REPLACE(v, UNISTR('t\00BFrmin'), UNISTR('t\00E9rmin'));
    v := REPLACE(v, UNISTR('t\00BFcnica'), UNISTR('t\00E9cnica'));
    v := REPLACE(v, UNISTR('Due\00BFo'), UNISTR('Due\00F1o'));
    v := REPLACE(v, UNISTR('due\00BFo'), UNISTR('due\00F1o'));
    v := REPLACE(v, UNISTR('v\00BFnculo'), UNISTR('v\00EDnculo'));
    -- Catálogo completo de los 180 tokens observados en el diagnóstico
    -- read-only 20260924. Cada reemplazo es literal y contextual.
    v := REPLACE(v, UNISTR('\00BFreas'), UNISTR('\00E1reas'));
    v := REPLACE(v, UNISTR('\00BFtica'), UNISTR('\00E9tica'));
    v := REPLACE(v, UNISTR('Acci\00BFn'), UNISTR('Acci\00F3n'));
    v := REPLACE(v, UNISTR('Aceptaci\00BFn'), UNISTR('Aceptaci\00F3n'));
    v := REPLACE(v, UNISTR('actualizaci\00BFn'), UNISTR('actualizaci\00F3n'));
    v := REPLACE(v, UNISTR('admisi\00BFn'), UNISTR('admisi\00F3n'));
    v := REPLACE(v, UNISTR('adquisici\00BFn'), UNISTR('adquisici\00F3n'));
    v := REPLACE(v, UNISTR('Afectaci\00BFn'), UNISTR('Afectaci\00F3n'));
    v := REPLACE(v, UNISTR('afectaci\00BFn'), UNISTR('afectaci\00F3n'));
    v := REPLACE(v, UNISTR('afiliaci\00BFn'), UNISTR('afiliaci\00F3n'));
    v := REPLACE(v, UNISTR('Ampliaci\00BFn'), UNISTR('Ampliaci\00F3n'));
    v := REPLACE(v, UNISTR('an\00BFlisis'), UNISTR('an\00E1lisis'));
    v := REPLACE(v, UNISTR('anal\00BFtica'), UNISTR('anal\00EDtica'));
    v := REPLACE(v, UNISTR('anticorrupci\00BFn'), UNISTR('anticorrupci\00F3n'));
    v := REPLACE(v, UNISTR('aplicaci\00BFn'), UNISTR('aplicaci\00F3n'));
    v := REPLACE(v, UNISTR('apropiaci\00BFn'), UNISTR('apropiaci\00F3n'));
    v := REPLACE(v, UNISTR('asesor\00BFas'), UNISTR('asesor\00EDas'));
    v := REPLACE(v, UNISTR('atenci\00BFn'), UNISTR('atenci\00F3n'));
    v := REPLACE(v, UNISTR('auditor\00BFa'), UNISTR('auditor\00EDa'));
    v := REPLACE(v, UNISTR('auditor\00BFas'), UNISTR('auditor\00EDas'));
    v := REPLACE(v, UNISTR('autenticaci\00BFn'), UNISTR('autenticaci\00F3n'));
    v := REPLACE(v, UNISTR('autom\00BFticas'), UNISTR('autom\00E1ticas'));
    v := REPLACE(v, UNISTR('biom\00BFtrica'), UNISTR('biom\00E9trica'));
    v := REPLACE(v, UNISTR('c\00BFclicos'), UNISTR('c\00EDclicos'));
    v := REPLACE(v, UNISTR('c\00BFdigo'), UNISTR('c\00F3digo'));
    v := REPLACE(v, UNISTR('c\00BFnyuge'), UNISTR('c\00F3nyuge'));
    v := REPLACE(v, UNISTR('Calificaci\00BFn'), UNISTR('Calificaci\00F3n'));
    v := REPLACE(v, UNISTR('canalizaci\00BFn'), UNISTR('canalizaci\00F3n'));
    v := REPLACE(v, UNISTR('cancelaci\00BFn'), UNISTR('cancelaci\00F3n'));
    v := REPLACE(v, UNISTR('ciudadan\00BFa'), UNISTR('ciudadan\00EDa'));
    v := REPLACE(v, UNISTR('cl\00BFnicas'), UNISTR('cl\00EDnicas'));
    v := REPLACE(v, UNISTR('cl\00BFnico'), UNISTR('cl\00EDnico'));
    v := REPLACE(v, UNISTR('clasificaci\00BFn'), UNISTR('clasificaci\00F3n'));
    v := REPLACE(v, UNISTR('Colusi\00BFn'), UNISTR('Colusi\00F3n'));
    v := REPLACE(v, UNISTR('colusi\00BFn'), UNISTR('colusi\00F3n'));
    v := REPLACE(v, UNISTR('comercializaci\00BFn'), UNISTR('comercializaci\00F3n'));
    v := REPLACE(v, UNISTR('comit\00BF'), UNISTR('comit\00E9'));
    v := REPLACE(v, UNISTR('comit\00BFs'), UNISTR('comit\00E9s'));
    v := REPLACE(v, UNISTR('Comunicaci\00BFn'), UNISTR('Comunicaci\00F3n'));
    v := REPLACE(v, UNISTR('comunicaci\00BFn'), UNISTR('comunicaci\00F3n'));
    v := REPLACE(v, UNISTR('Concentraci\00BFn'), UNISTR('Concentraci\00F3n'));
    v := REPLACE(v, UNISTR('concesi\00BFn'), UNISTR('concesi\00F3n'));
    v := REPLACE(v, UNISTR('conciliaci\00BFn'), UNISTR('conciliaci\00F3n'));
    v := REPLACE(v, UNISTR('condici\00BFn'), UNISTR('condici\00F3n'));
    v := REPLACE(v, UNISTR('confirmaci\00BFn'), UNISTR('confirmaci\00F3n'));
    v := REPLACE(v, UNISTR('conformaci\00BFn'), UNISTR('conformaci\00F3n'));
    v := REPLACE(v, UNISTR('contin\00BFe'), UNISTR('contin\00FAe'));
    v := REPLACE(v, UNISTR('Contrataci\00BFn'), UNISTR('Contrataci\00F3n'));
    v := REPLACE(v, UNISTR('correcci\00BFn'), UNISTR('correcci\00F3n'));
    v := REPLACE(v, UNISTR('corrupci\00BFn'), UNISTR('corrupci\00F3n'));
    v := REPLACE(v, UNISTR('cotizaci\00BFn'), UNISTR('cotizaci\00F3n'));
    v := REPLACE(v, UNISTR('cr\00BFtica'), UNISTR('cr\00EDtica'));
    v := REPLACE(v, UNISTR('cr\00BFticas'), UNISTR('cr\00EDticas'));
    v := REPLACE(v, UNISTR('cr\00BFticos'), UNISTR('cr\00EDticos'));
    v := REPLACE(v, UNISTR('creaci\00BFn'), UNISTR('creaci\00F3n'));
    v := REPLACE(v, UNISTR('D\00BFbil'), UNISTR('D\00E9bil'));
    v := REPLACE(v, UNISTR('d\00BFbil'), UNISTR('d\00E9bil'));
    v := REPLACE(v, UNISTR('d\00BFbiles'), UNISTR('d\00E9biles'));
    v := REPLACE(v, UNISTR('d\00BFdivas'), UNISTR('d\00E1divas'));
    v := REPLACE(v, UNISTR('da\00BFo'), UNISTR('da\00F1o'));
    v := REPLACE(v, UNISTR('decisi\00BFn'), UNISTR('decisi\00F3n'));
    v := REPLACE(v, UNISTR('Declaraci\00BFn'), UNISTR('Declaraci\00F3n'));
    v := REPLACE(v, UNISTR('depuraci\00BFn'), UNISTR('depuraci\00F3n'));
    v := REPLACE(v, UNISTR('designaci\00BFn'), UNISTR('designaci\00F3n'));
    v := REPLACE(v, UNISTR('despu\00BFs'), UNISTR('despu\00E9s'));
    v := REPLACE(v, UNISTR('digitalizaci\00BFn'), UNISTR('digitalizaci\00F3n'));
    v := REPLACE(v, UNISTR('dise\00BFen'), UNISTR('dise\00F1en'));
    v := REPLACE(v, UNISTR('dise\00BFo'), UNISTR('dise\00F1o'));
    v := REPLACE(v, UNISTR('dispensaci\00BFn'), UNISTR('dispensaci\00F3n'));
    v := REPLACE(v, UNISTR('distorsi\00BFn'), UNISTR('distorsi\00F3n'));
    v := REPLACE(v, UNISTR('distribuci\00BFn'), UNISTR('distribuci\00F3n'));
    v := REPLACE(v, UNISTR('econ\00BFmica'), UNISTR('econ\00F3mica'));
    v := REPLACE(v, UNISTR('econ\00BFmicos'), UNISTR('econ\00F3micos'));
    v := REPLACE(v, UNISTR('efect\00BFen'), UNISTR('efect\00FAen'));
    v := REPLACE(v, UNISTR('elaboraci\00BFn'), UNISTR('elaboraci\00F3n'));
    v := REPLACE(v, UNISTR('espec\00BFfico'), UNISTR('espec\00EDfico'));
    v := REPLACE(v, UNISTR('espec\00BFficos'), UNISTR('espec\00EDficos'));
    v := REPLACE(v, UNISTR('estandarizaci\00BFn'), UNISTR('estandarizaci\00F3n'));
    v := REPLACE(v, UNISTR('f\00BFsica'), UNISTR('f\00EDsica'));
    v := REPLACE(v, UNISTR('f\00BFsicamente'), UNISTR('f\00EDsicamente'));
    v := REPLACE(v, UNISTR('f\00BFsicos'), UNISTR('f\00EDsicos'));
    v := REPLACE(v, UNISTR('facturaci\00BFn'), UNISTR('facturaci\00F3n'));
    v := REPLACE(v, UNISTR('Falsificaci\00BFn'), UNISTR('Falsificaci\00F3n'));
    v := REPLACE(v, UNISTR('falsificaci\00BFn'), UNISTR('falsificaci\00F3n'));
    v := REPLACE(v, UNISTR('filtraci\00BFn'), UNISTR('filtraci\00F3n'));
    v := REPLACE(v, UNISTR('financiaci\00BFn'), UNISTR('financiaci\00F3n'));
    v := REPLACE(v, UNISTR('formalizaci\00BFn'), UNISTR('formalizaci\00F3n'));
    v := REPLACE(v, UNISTR('garant\00BFa'), UNISTR('garant\00EDa'));
    v := REPLACE(v, UNISTR('garant\00BFas'), UNISTR('garant\00EDas'));
    v := REPLACE(v, UNISTR('gu\00BFas'), UNISTR('gu\00EDas'));
    v := REPLACE(v, UNISTR('id\00BFneos'), UNISTR('id\00F3neos'));
    v := REPLACE(v, UNISTR('il\00BFcito'), UNISTR('il\00EDcito'));
    v := REPLACE(v, UNISTR('il\00BFcitos'), UNISTR('il\00EDcitos'));
    v := REPLACE(v, UNISTR('implementaci\00BFn'), UNISTR('implementaci\00F3n'));
    v := REPLACE(v, UNISTR('Infiltraci\00BFn'), UNISTR('Infiltraci\00F3n'));
    v := REPLACE(v, UNISTR('inform\00BFticos'), UNISTR('inform\00E1ticos'));
    v := REPLACE(v, UNISTR('ingenier\00BFa'), UNISTR('ingenier\00EDa'));
    v := REPLACE(v, UNISTR('inspecci\00BFn'), UNISTR('inspecci\00F3n'));
    v := REPLACE(v, UNISTR('integraci\00BFn'), UNISTR('integraci\00F3n'));
    v := REPLACE(v, UNISTR('Intenci\00BFn'), UNISTR('Intenci\00F3n'));
    v := REPLACE(v, UNISTR('inter\00BFs'), UNISTR('inter\00E9s'));
    v := REPLACE(v, UNISTR('interacci\00BFn'), UNISTR('interacci\00F3n'));
    v := REPLACE(v, UNISTR('interpretaci\00BFn'), UNISTR('interpretaci\00F3n'));
    v := REPLACE(v, UNISTR('Interrupci\00BFn'), UNISTR('Interrupci\00F3n'));
    v := REPLACE(v, UNISTR('interrupci\00BFn'), UNISTR('interrupci\00F3n'));
    v := REPLACE(v, UNISTR('Intervenci\00BFn'), UNISTR('Intervenci\00F3n'));
    v := REPLACE(v, UNISTR('inversi\00BFn'), UNISTR('inversi\00F3n'));
    v := REPLACE(v, UNISTR('investigaci\00BFn'), UNISTR('investigaci\00F3n'));
    v := REPLACE(v, UNISTR('jer\00BFrquica'), UNISTR('jer\00E1rquica'));
    v := REPLACE(v, UNISTR('jur\00BFdicas'), UNISTR('jur\00EDdicas'));
    v := REPLACE(v, UNISTR('leg\00BFtimo'), UNISTR('leg\00EDtimo'));
    v := REPLACE(v, UNISTR('m\00BFdica'), UNISTR('m\00E9dica'));
    v := REPLACE(v, UNISTR('m\00BFdicas'), UNISTR('m\00E9dicas'));
    v := REPLACE(v, UNISTR('m\00BFdicos'), UNISTR('m\00E9dicos'));
    v := REPLACE(v, UNISTR('m\00BFltiples'), UNISTR('m\00FAltiples'));
    v := REPLACE(v, UNISTR('Manipulaci\00BFn'), UNISTR('Manipulaci\00F3n'));
    v := REPLACE(v, UNISTR('manipulaci\00BFn'), UNISTR('manipulaci\00F3n'));
    v := REPLACE(v, UNISTR('materializaci\00BFn'), UNISTR('materializaci\00F3n'));
    v := REPLACE(v, UNISTR('medi\00BFtica'), UNISTR('medi\00E1tica'));
    v := REPLACE(v, UNISTR('medi\00BFticos'), UNISTR('medi\00E1ticos'));
    v := REPLACE(v, UNISTR('Metodolog\00BFa'), UNISTR('Metodolog\00EDa'));
    v := REPLACE(v, UNISTR('modificaci\00BFn'), UNISTR('modificaci\00F3n'));
    v := REPLACE(v, UNISTR('n\00BFmero'), UNISTR('n\00FAmero'));
    v := REPLACE(v, UNISTR('n\00BFmina'), UNISTR('n\00F3mina'));
    v := REPLACE(v, UNISTR('obligaci\00BFn'), UNISTR('obligaci\00F3n'));
    v := REPLACE(v, UNISTR('Omisi\00BFn'), UNISTR('Omisi\00F3n'));
    v := REPLACE(v, UNISTR('omisi\00BFn'), UNISTR('omisi\00F3n'));
    v := REPLACE(v, UNISTR('p\00BFblica'), UNISTR('p\00FAblica'));
    v := REPLACE(v, UNISTR('p\00BFblicas'), UNISTR('p\00FAblicas'));
    v := REPLACE(v, UNISTR('p\00BFblico'), UNISTR('p\00FAblico'));
    v := REPLACE(v, UNISTR('p\00BFblicos'), UNISTR('p\00FAblicos'));
    v := REPLACE(v, UNISTR('p\00BFlizas'), UNISTR('p\00F3lizas'));
    v := REPLACE(v, UNISTR('P\00BFrdida'), UNISTR('P\00E9rdida'));
    v := REPLACE(v, UNISTR('p\00BFrdida'), UNISTR('p\00E9rdida'));
    v := REPLACE(v, UNISTR('Parametrizaci\00BFn'), UNISTR('Parametrizaci\00F3n'));
    v := REPLACE(v, UNISTR('per\00BFodos'), UNISTR('per\00EDodos'));
    v := REPLACE(v, UNISTR('Percepci\00BFn'), UNISTR('Percepci\00F3n'));
    v := REPLACE(v, UNISTR('peri\00BFdica'), UNISTR('peri\00F3dica'));
    v := REPLACE(v, UNISTR('peri\00BFdicas'), UNISTR('peri\00F3dicas'));
    v := REPLACE(v, UNISTR('peri\00BFdico'), UNISTR('peri\00F3dico'));
    v := REPLACE(v, UNISTR('planificaci\00BFn'), UNISTR('planificaci\00F3n'));
    v := REPLACE(v, UNISTR('podr\00BFa'), UNISTR('podr\00EDa'));
    v := REPLACE(v, UNISTR('pol\00BFtica'), UNISTR('pol\00EDtica'));
    v := REPLACE(v, UNISTR('Pol\00BFticamente'), UNISTR('Pol\00EDticamente'));
    v := REPLACE(v, UNISTR('Pol\00BFticas'), UNISTR('Pol\00EDticas'));
    v := REPLACE(v, UNISTR('pol\00BFticas'), UNISTR('pol\00EDticas'));
    v := REPLACE(v, UNISTR('Pr\00BFcticas'), UNISTR('Pr\00E1cticas'));
    v := REPLACE(v, UNISTR('pr\00BFcticas'), UNISTR('pr\00E1cticas'));
    v := REPLACE(v, UNISTR('pr\00BFrrogas'), UNISTR('pr\00F3rrogas'));
    v := REPLACE(v, UNISTR('Presi\00BFn'), UNISTR('Presi\00F3n'));
    v := REPLACE(v, UNISTR('presi\00BFn'), UNISTR('presi\00F3n'));
    v := REPLACE(v, UNISTR('prop\00BFsito'), UNISTR('prop\00F3sito'));
    v := REPLACE(v, UNISTR('qu\00BF'), UNISTR('qu\00E9'));
    v := REPLACE(v, UNISTR('r\00BFpidamente'), UNISTR('r\00E1pidamente'));
    v := REPLACE(v, UNISTR('realizaci\00BFn'), UNISTR('realizaci\00F3n'));
    v := REPLACE(v, UNISTR('recepci\00BFn'), UNISTR('recepci\00F3n'));
    v := REPLACE(v, UNISTR('revisi\00BFn'), UNISTR('revisi\00F3n'));
    v := REPLACE(v, UNISTR('rotaci\00BFn'), UNISTR('rotaci\00F3n'));
    v := REPLACE(v, UNISTR('s\00BF'), UNISTR('s\00ED'));
    v := REPLACE(v, UNISTR('sanci\00BFn'), UNISTR('sanci\00F3n'));
    v := REPLACE(v, UNISTR('se\00BFalamientos'), UNISTR('se\00F1alamientos'));
    v := REPLACE(v, UNISTR('Secci\00BFn'), UNISTR('Secci\00F3n'));
    v := REPLACE(v, UNISTR('secci\00BFn'), UNISTR('secci\00F3n'));
    v := REPLACE(v, UNISTR('segregaci\00BFn'), UNISTR('segregaci\00F3n'));
    v := REPLACE(v, UNISTR('selecci\00BFn'), UNISTR('selecci\00F3n'));
    v := REPLACE(v, UNISTR('simulaci\00BFn'), UNISTR('simulaci\00F3n'));
    v := REPLACE(v, UNISTR('simult\00BFneamente'), UNISTR('simult\00E1neamente'));
    v := REPLACE(v, UNISTR('sobrevaloraci\00BFn'), UNISTR('sobrevaloraci\00F3n'));
    v := REPLACE(v, UNISTR('suplantaci\00BFn'), UNISTR('suplantaci\00F3n'));
    v := REPLACE(v, UNISTR('sustracci\00BFn'), UNISTR('sustracci\00F3n'));
    v := REPLACE(v, UNISTR('t\00BFcnicos'), UNISTR('t\00E9cnicos'));
    v := REPLACE(v, UNISTR('tecnol\00BFgicas'), UNISTR('tecnol\00F3gicas'));
    v := REPLACE(v, UNISTR('tecnolog\00BFa'), UNISTR('tecnolog\00EDa'));
    v := REPLACE(v, UNISTR('Tesorer\00BFa'), UNISTR('Tesorer\00EDa'));
    v := REPLACE(v, UNISTR('tr\00BFmites'), UNISTR('tr\00E1mites'));
    v := REPLACE(v, UNISTR('trav\00BFs'), UNISTR('trav\00E9s'));
    v := REPLACE(v, UNISTR('uni\00BFn'), UNISTR('uni\00F3n'));
    v := REPLACE(v, UNISTR('utilizaci\00BFn'), UNISTR('utilizaci\00F3n'));
    v := REPLACE(v, UNISTR('Vinculaci\00BFn'), UNISTR('Vinculaci\00F3n'));
    v := REPLACE(v, UNISTR('violaci\00BFn'), UNISTR('violaci\00F3n'));
    -- U+FFFD no se sustituye a ciegas: sin contexto se desconoce el carácter original.
    RETURN v;
  END;
BEGIN
  SELECT COUNT(*) INTO backup_table FROM USER_TABLES WHERE TABLE_NAME = 'RL_MR_RIES_DESC_BKP_20260924';
  IF backup_table = 0 THEN
    RAISE_APPLICATION_ERROR(-20947, 'Corrección bloqueada: ejecutar primero 37_backup_rl_mr_riesgos_descripciones.sql.');
  END IF;
  EXECUTE IMMEDIATE 'SELECT COUNT(*) FROM RL_MR_RIES_DESC_BKP_20260924'
    INTO backup_rows;
  IF backup_rows <> 59 THEN
    RAISE_APPLICATION_ERROR(-20948, 'Corrección bloqueada: backup incompleto; filas=' || backup_rows || ', esperado=59.');
  END IF;
  SELECT COUNT(*) INTO db_rows FROM RL_MR_RIESGOS;
  SELECT COUNT(*) INTO duplicate_codes FROM
   (SELECT RIE_CODIGO FROM RL_MR_RIESGOS GROUP BY RIE_CODIGO HAVING COUNT(*) > 1);
  IF db_rows <> 59 OR duplicate_codes <> 0 THEN
    RAISE_APPLICATION_ERROR(-20949, 'Corrección bloqueada: destino inválido; filas=' || db_rows || ', duplicados=' || duplicate_codes || '.');
  END IF;
  FOR r IN (SELECT RIE_DESCRIPCION FROM RL_MR_RIESGOS) LOOP
    v_fixed := fix_description(r.RIE_DESCRIPCION);
    IF v_fixed <> r.RIE_DESCRIPCION
       OR (v_fixed IS NULL AND r.RIE_DESCRIPCION IS NOT NULL)
       OR (v_fixed IS NOT NULL AND r.RIE_DESCRIPCION IS NULL) THEN
      expected_updates := expected_updates + 1;
    END IF;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('EXPECTED_DESCRIPTION_UPDATES=' || expected_updates);
  SAVEPOINT risk_description_encoding;
  FOR r IN (SELECT RIE_ID, RIE_CODIGO, RIE_DESCRIPCION FROM RL_MR_RIESGOS FOR UPDATE) LOOP
    v_fixed := fix_description(r.RIE_DESCRIPCION);
    IF v_fixed <> r.RIE_DESCRIPCION
       OR (v_fixed IS NULL AND r.RIE_DESCRIPCION IS NOT NULL)
       OR (v_fixed IS NOT NULL AND r.RIE_DESCRIPCION IS NULL) THEN
      UPDATE RL_MR_RIESGOS
         SET RIE_DESCRIPCION = v_fixed
       WHERE RIE_ID = r.RIE_ID AND RIE_CODIGO = r.RIE_CODIGO;
      actual_updates := actual_updates + SQL%ROWCOUNT;
    END IF;
  END LOOP;
  DBMS_OUTPUT.PUT_LINE('ACTUAL_DESCRIPTION_UPDATES=' || actual_updates);
  IF actual_updates <> expected_updates THEN
    ROLLBACK TO risk_description_encoding;
    RAISE_APPLICATION_ERROR(-20938, 'Actualizaciones inesperadas: expected=' || expected_updates || ', actual=' || actual_updates);
  END IF;
  COMMIT;
  DBMS_OUTPUT.PUT_LINE('RISK_DESCRIPTION_CORRECTION_STATUS=PASS');
EXCEPTION WHEN OTHERS THEN
  ROLLBACK;
  RAISE;
END;
/
