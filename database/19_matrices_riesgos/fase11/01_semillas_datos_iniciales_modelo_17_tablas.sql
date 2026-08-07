-- ============================================================
-- FASE 11 - BLOQUE 1
-- Datos iniciales y configuración del modelo dinámico de Matrices de Riesgos
-- Compatible con Oracle 11g
-- Idempotente: no elimina datos, no trunca tablas y preserva objetos ajenos.
-- ============================================================

SET DEFINE OFF
SET SERVEROUTPUT ON SIZE UNLIMITED
SET FEEDBACK ON
SET VERIFY ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
    c_familia_codigo CONSTANT VARCHAR2(50) := 'MATRIZ_RIESGOS_LAFT';
    c_version_codigo CONSTANT VARCHAR2(30) := 'MATRIZ_RIESGOS_LAFT_V1';
    c_version_numero CONSTANT NUMBER := 1;
    c_hash            CONSTANT VARCHAR2(64) := 'f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90';

    v_schema          VARCHAR2(128);
    v_usuario_id      NUMBER(10);
    v_familia_id      NUMBER(15);
    v_version_id      NUMBER(15);
    v_json_existente  CLOB;
    v_hash_existente  VARCHAR2(64);
    v_conteo          NUMBER;
    v_json            CLOB := q'~{"codigoFormulario":"MATRIZ_RIESGOS_LAFT","nombreFormulario":"Matriz de Riesgos LA/FT","version":"1.0","secciones":[{"id":"identificacion","clave":"identificacion","titulo":"Identificaci\u00f3n del riesgo","orden":1,"campos":[{"id":"area_principal","clave":"area_principal","etiqueta":"\u00c1rea principal","tipo":"texto","obligatorio":true,"soloLectura":false},{"id":"dueno_riesgo","clave":"dueno_riesgo","etiqueta":"Due\u00f1o del riesgo","tipo":"texto","obligatorio":true,"soloLectura":false}]},{"id":"riesgo_inherente","clave":"riesgo_inherente","titulo":"Valoraci\u00f3n del riesgo inherente","orden":2,"campos":[{"id":"frecuencia_inherente","clave":"frecuencia_inherente","etiqueta":"Frecuencia inherente","tipo":"selector-catalogo","codigoCatalogo":"MR_FRECUENCIA_1_5","obligatorio":true,"soloLectura":false},{"id":"impacto_inherente","clave":"impacto_inherente","etiqueta":"Impacto inherente","tipo":"selector-catalogo","codigoCatalogo":"MR_IMPACTO_1_5","obligatorio":true,"soloLectura":false},{"id":"nivel_inherente","clave":"nivel_inherente","etiqueta":"Nivel inherente","tipo":"selector-catalogo","codigoCatalogo":"MR_NIVEL_RIESGO","obligatorio":true,"soloLectura":false}]},{"id":"controles","clave":"controles","titulo":"Efectividad de controles","orden":3,"campos":[{"id":"controles_preventivo","clave":"controles_preventivo","etiqueta":"Control preventivo (%)","tipo":"numero","obligatorio":true,"soloLectura":false},{"id":"controles_detectivo","clave":"controles_detectivo","etiqueta":"Control detectivo (%)","tipo":"numero","obligatorio":true,"soloLectura":false},{"id":"controles_correctivo","clave":"controles_correctivo","etiqueta":"Control correctivo (%)","tipo":"numero","obligatorio":true,"soloLectura":false}]},{"id":"riesgo_residual","clave":"riesgo_residual","titulo":"Valoraci\u00f3n y respuesta del riesgo residual","orden":4,"campos":[{"id":"frecuencia_residual","clave":"frecuencia_residual","etiqueta":"Frecuencia residual","tipo":"selector-catalogo","codigoCatalogo":"MR_FRECUENCIA_1_5","obligatorio":true,"soloLectura":false},{"id":"impacto_residual","clave":"impacto_residual","etiqueta":"Impacto residual","tipo":"selector-catalogo","codigoCatalogo":"MR_IMPACTO_1_5","obligatorio":true,"soloLectura":false},{"id":"nivel_residual","clave":"nivel_residual","etiqueta":"Nivel residual","tipo":"selector-catalogo","codigoCatalogo":"MR_NIVEL_RIESGO","obligatorio":true,"soloLectura":false},{"id":"respuesta_riesgo","clave":"respuesta_riesgo","etiqueta":"Respuesta al riesgo","tipo":"selector-catalogo","codigoCatalogo":"MR_RESPUESTA_RIESGO","obligatorio":true,"soloLectura":false}]}],"catalogos":[{"codigo":"MR_FRECUENCIA_1_5","nombre":"Frecuencia de riesgo (1\u20135)","elementos":[{"codigo":"1","valor":"1 - Rara","orden":1},{"codigo":"2","valor":"2 - Improbable","orden":2},{"codigo":"3","valor":"3 - Posible","orden":3},{"codigo":"4","valor":"4 - Probable","orden":4},{"codigo":"5","valor":"5 - Casi segura","orden":5}]},{"codigo":"MR_IMPACTO_1_5","nombre":"Impacto de riesgo (1\u20135)","elementos":[{"codigo":"1","valor":"1 - Insignificante","orden":1},{"codigo":"2","valor":"2 - Menor","orden":2},{"codigo":"3","valor":"3 - Moderado","orden":3},{"codigo":"4","valor":"4 - Mayor","orden":4},{"codigo":"5","valor":"5 - Catastr\u00f3fico","orden":5}]},{"codigo":"MR_NIVEL_RIESGO","nombre":"Nivel de riesgo","elementos":[{"codigo":"BAJO","valor":"Bajo","orden":1},{"codigo":"MODERADO","valor":"Moderado","orden":2},{"codigo":"ALTO","valor":"Alto","orden":3},{"codigo":"CRITICO","valor":"Cr\u00edtico","orden":4}]},{"codigo":"MR_RESPUESTA_RIESGO","nombre":"Respuesta al riesgo","elementos":[{"codigo":"EVITAR","valor":"Evitar","orden":1},{"codigo":"MITIGAR","valor":"Mitigar","orden":2},{"codigo":"TRANSFERIR","valor":"Transferir","orden":3},{"codigo":"ACEPTAR","valor":"Aceptar","orden":4}]}],"reglas":[{"codigo":"CALCULO_VRI_VRR","version":"1.0","algoritmoId":"MATRICES_VRI_ADITIVO_1_9","parametros":{"frecuenciaMin":1,"frecuenciaMax":5,"impactoMin":1,"impactoMax":5,"vriMin":1,"vriMax":9,"pesoPreventivo":0.7,"pesoDetectivo":0.15,"pesoCorrectivo":0.15,"vrrMin":1}}]}~';

    PROCEDURE exigir(p_condicion BOOLEAN, p_codigo NUMBER, p_mensaje VARCHAR2) IS
    BEGIN
        IF NOT p_condicion THEN
            RAISE_APPLICATION_ERROR(p_codigo, p_mensaje);
        END IF;
    END;

    PROCEDURE asegurar_catalogo(p_codigo VARCHAR2, p_nombre VARCHAR2) IS
    BEGIN
        MERGE INTO RL_MR_CATALOGOS destino
        USING (SELECT p_codigo CAT_CODIGO, p_nombre CAT_NOMBRE FROM DUAL) fuente
           ON (destino.CAT_CODIGO = fuente.CAT_CODIGO)
        WHEN MATCHED THEN
            UPDATE SET destino.CAT_NOMBRE = fuente.CAT_NOMBRE,
                       destino.CAT_ACTIVO = 1
        WHEN NOT MATCHED THEN
            INSERT (CAT_ID, CAT_CODIGO, CAT_NOMBRE, CAT_ACTIVO)
            VALUES (SEQ_RL_MR_CATALOGOS.NEXTVAL, fuente.CAT_CODIGO, fuente.CAT_NOMBRE, 1);
    END;

    PROCEDURE asegurar_elemento(
        p_catalogo_codigo VARCHAR2,
        p_codigo          VARCHAR2,
        p_valor           VARCHAR2,
        p_orden           NUMBER
    ) IS
        v_catalogo_id NUMBER(15);
    BEGIN
        SELECT CAT_ID
          INTO v_catalogo_id
          FROM RL_MR_CATALOGOS
         WHERE CAT_CODIGO = p_catalogo_codigo;

        MERGE INTO RL_MR_ELEMENTOS_CATALOGO destino
        USING (
            SELECT v_catalogo_id ELE_CATALOGO_ID,
                   p_codigo ELE_CODIGO,
                   p_valor ELE_VALOR,
                   p_orden ELE_ORDEN
              FROM DUAL
        ) fuente
           ON (destino.ELE_CATALOGO_ID = fuente.ELE_CATALOGO_ID
               AND destino.ELE_CODIGO = fuente.ELE_CODIGO)
        WHEN MATCHED THEN
            UPDATE SET destino.ELE_VALOR = fuente.ELE_VALOR,
                       destino.ELE_ORDEN = fuente.ELE_ORDEN,
                       destino.ELE_ACTIVO = 1
        WHEN NOT MATCHED THEN
            INSERT (
                ELE_ID,
                ELE_CATALOGO_ID,
                ELE_CODIGO,
                ELE_VALOR,
                ELE_ORDEN,
                ELE_ACTIVO
            )
            VALUES (
                SEQ_RL_MR_ELEMENTOS.NEXTVAL,
                fuente.ELE_CATALOGO_ID,
                fuente.ELE_CODIGO,
                fuente.ELE_VALOR,
                fuente.ELE_ORDEN,
                1
            );
    END;

    PROCEDURE asegurar_regla(
        p_codigo       VARCHAR2,
        p_version      VARCHAR2,
        p_nombre       VARCHAR2,
        p_algoritmo_id VARCHAR2
    ) IS
    BEGIN
        MERGE INTO RL_MR_REGLAS_CALCULO destino
        USING (
            SELECT p_codigo REG_CODIGO,
                   p_version REG_VERSION,
                   p_nombre REG_NOMBRE,
                   p_algoritmo_id REG_ALGORITMO_ID
              FROM DUAL
        ) fuente
           ON (destino.REG_CODIGO = fuente.REG_CODIGO
               AND destino.REG_VERSION = fuente.REG_VERSION)
        WHEN MATCHED THEN
            UPDATE SET destino.REG_NOMBRE = fuente.REG_NOMBRE,
                       destino.REG_ALGORITMO_ID = fuente.REG_ALGORITMO_ID,
                       destino.REG_ACTIVA = 1
        WHEN NOT MATCHED THEN
            INSERT (
                REG_ID,
                REG_CODIGO,
                REG_VERSION,
                REG_NOMBRE,
                REG_ALGORITMO_ID,
                REG_ACTIVA
            )
            VALUES (
                SEQ_RL_MR_REGLAS.NEXTVAL,
                fuente.REG_CODIGO,
                fuente.REG_VERSION,
                fuente.REG_NOMBRE,
                fuente.REG_ALGORITMO_ID,
                1
            );
    END;
BEGIN
    SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
      INTO v_schema
      FROM DUAL;

    exigir(
        UPPER(v_schema) = 'RIESGO_LAVADO',
        -20101,
        'EJECUCION BLOQUEADA: CURRENT_SCHEMA debe ser RIESGO_LAVADO.'
    );

    SELECT COUNT(*)
      INTO v_conteo
      FROM USER_TABLES
     WHERE TABLE_NAME IN (
        'RL_USUARIOS',
        'RL_MR_FAMILIAS_FORMULARIO',
        'RL_MR_VERSIONES_FORMULARIO',
        'RL_MR_CATALOGOS',
        'RL_MR_ELEMENTOS_CATALOGO',
        'RL_MR_REGLAS_CALCULO'
     );
    exigir(v_conteo = 6, -20102, 'EJECUCION BLOQUEADA: faltan tablas obligatorias.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM USER_SEQUENCES
     WHERE SEQUENCE_NAME IN (
        'SEQ_RL_MR_FAMILIAS',
        'SEQ_RL_MR_VERSIONES',
        'SEQ_RL_MR_CATALOGOS',
        'SEQ_RL_MR_ELEMENTOS',
        'SEQ_RL_MR_REGLAS'
     );
    exigir(v_conteo = 5, -20103, 'EJECUCION BLOQUEADA: faltan secuencias obligatorias.');

    SELECT MIN(USR_ID)
      INTO v_usuario_id
      FROM RL_USUARIOS
     WHERE USR_ACTIVO = 1;
    exigir(v_usuario_id IS NOT NULL, -20104, 'EJECUCION BLOQUEADA: no existe un usuario institucional activo.');

    exigir(DBMS_LOB.GETLENGTH(v_json) > 0, -20105, 'EJECUCION BLOQUEADA: la definicion JSON esta vacia.');
    exigir(LENGTH(c_hash) = 64, -20106, 'EJECUCION BLOQUEADA: el hash SHA-256 no tiene 64 caracteres.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM (
        SELECT FAM_CODIGO
          FROM RL_MR_FAMILIAS_FORMULARIO
         GROUP BY FAM_CODIGO
        HAVING COUNT(*) > 1
      );
    exigir(v_conteo = 0, -20107, 'EJECUCION BLOQUEADA: existen codigos de familia duplicados.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM (
        SELECT CAT_CODIGO
          FROM RL_MR_CATALOGOS
         GROUP BY CAT_CODIGO
        HAVING COUNT(*) > 1
      );
    exigir(v_conteo = 0, -20108, 'EJECUCION BLOQUEADA: existen codigos de catalogo duplicados.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM (
        SELECT REG_CODIGO, REG_VERSION
          FROM RL_MR_REGLAS_CALCULO
         GROUP BY REG_CODIGO, REG_VERSION
        HAVING COUNT(*) > 1
      );
    exigir(v_conteo = 0, -20109, 'EJECUCION BLOQUEADA: existen reglas duplicadas.');

    MERGE INTO RL_MR_FAMILIAS_FORMULARIO destino
    USING (
        SELECT c_familia_codigo FAM_CODIGO,
               'Matriz de Riesgos LA/FT' FAM_NOMBRE,
               'Formulario institucional dinamico para identificacion, valoracion, controles y respuesta del riesgo LA/FT.' FAM_DESCRIPCION
          FROM DUAL
    ) fuente
       ON (destino.FAM_CODIGO = fuente.FAM_CODIGO)
    WHEN MATCHED THEN
        UPDATE SET destino.FAM_NOMBRE = fuente.FAM_NOMBRE,
                   destino.FAM_DESCRIPCION = fuente.FAM_DESCRIPCION,
                   destino.FAM_ACTIVO = 1
    WHEN NOT MATCHED THEN
        INSERT (
            FAM_ID,
            FAM_CODIGO,
            FAM_NOMBRE,
            FAM_DESCRIPCION,
            FAM_ACTIVO,
            FAM_FECHA_CREACION
        )
        VALUES (
            SEQ_RL_MR_FAMILIAS.NEXTVAL,
            fuente.FAM_CODIGO,
            fuente.FAM_NOMBRE,
            fuente.FAM_DESCRIPCION,
            1,
            SYSDATE
        );

    SELECT FAM_ID
      INTO v_familia_id
      FROM RL_MR_FAMILIAS_FORMULARIO
     WHERE FAM_CODIGO = c_familia_codigo;

    asegurar_catalogo('MR_FRECUENCIA_1_5', 'Frecuencia de riesgo (1-5)');
    asegurar_catalogo('MR_IMPACTO_1_5', 'Impacto de riesgo (1-5)');
    asegurar_catalogo('MR_NIVEL_RIESGO', 'Nivel de riesgo');
    asegurar_catalogo('MR_RESPUESTA_RIESGO', 'Respuesta al riesgo');

    asegurar_elemento('MR_FRECUENCIA_1_5', '1', '1 - Rara', 1);
    asegurar_elemento('MR_FRECUENCIA_1_5', '2', '2 - Improbable', 2);
    asegurar_elemento('MR_FRECUENCIA_1_5', '3', '3 - Posible', 3);
    asegurar_elemento('MR_FRECUENCIA_1_5', '4', '4 - Probable', 4);
    asegurar_elemento('MR_FRECUENCIA_1_5', '5', '5 - Casi segura', 5);

    asegurar_elemento('MR_IMPACTO_1_5', '1', '1 - Insignificante', 1);
    asegurar_elemento('MR_IMPACTO_1_5', '2', '2 - Menor', 2);
    asegurar_elemento('MR_IMPACTO_1_5', '3', '3 - Moderado', 3);
    asegurar_elemento('MR_IMPACTO_1_5', '4', '4 - Mayor', 4);
    asegurar_elemento('MR_IMPACTO_1_5', '5', '5 - Catastrofico', 5);

    asegurar_elemento('MR_NIVEL_RIESGO', 'BAJO', 'Bajo', 1);
    asegurar_elemento('MR_NIVEL_RIESGO', 'MODERADO', 'Moderado', 2);
    asegurar_elemento('MR_NIVEL_RIESGO', 'ALTO', 'Alto', 3);
    asegurar_elemento('MR_NIVEL_RIESGO', 'CRITICO', 'Critico', 4);

    asegurar_elemento('MR_RESPUESTA_RIESGO', 'EVITAR', 'Evitar', 1);
    asegurar_elemento('MR_RESPUESTA_RIESGO', 'MITIGAR', 'Mitigar', 2);
    asegurar_elemento('MR_RESPUESTA_RIESGO', 'TRANSFERIR', 'Transferir', 3);
    asegurar_elemento('MR_RESPUESTA_RIESGO', 'ACEPTAR', 'Aceptar', 4);

    asegurar_regla(
        'CALCULO_VRI_VRR',
        '1.0',
        'Calculo institucional de VRI, ETP y VRR',
        'MATRICES_VRI_ADITIVO_1_9'
    );

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_VERSIONES_FORMULARIO
     WHERE VER_FAMILIA_ID = v_familia_id
       AND VER_VERSION = c_version_numero;

    exigir(v_conteo <= 1, -20110, 'EJECUCION BLOQUEADA: existe mas de una version 1 para la familia.');

    IF v_conteo = 0 THEN
        SELECT COUNT(*)
          INTO v_conteo
          FROM RL_MR_VERSIONES_FORMULARIO
         WHERE VER_FAMILIA_ID = v_familia_id
           AND VER_VIGENTE = 1;

        exigir(
            v_conteo = 0,
            -20111,
            'EJECUCION BLOQUEADA: ya existe otra version vigente; no se reemplazara automaticamente.'
        );

        v_version_id := SEQ_RL_MR_VERSIONES.NEXTVAL;

        INSERT INTO RL_MR_VERSIONES_FORMULARIO (
            VER_ID,
            VER_FAMILIA_ID,
            VER_CODIGO,
            VER_VERSION,
            VER_JSON,
            VER_HASH,
            VER_ESTADO,
            VER_VIGENTE,
            VER_FECHA_INICIO,
            VER_FECHA_FIN,
            VER_FECHA_CREACION,
            VER_USR_CREACION
        )
        VALUES (
            v_version_id,
            v_familia_id,
            c_version_codigo,
            c_version_numero,
            v_json,
            c_hash,
            'PUBLISHED',
            1,
            SYSDATE,
            NULL,
            SYSDATE,
            v_usuario_id
        );
    ELSE
        SELECT VER_ID, VER_JSON, VER_HASH
          INTO v_version_id, v_json_existente, v_hash_existente
          FROM RL_MR_VERSIONES_FORMULARIO
         WHERE VER_FAMILIA_ID = v_familia_id
           AND VER_VERSION = c_version_numero
         FOR UPDATE;

        exigir(
            DBMS_LOB.COMPARE(v_json_existente, v_json) = 0,
            -20112,
            'EJECUCION BLOQUEADA: la version 1 existente tiene una definicion diferente.'
        );
        exigir(
            LOWER(v_hash_existente) = c_hash,
            -20113,
            'EJECUCION BLOQUEADA: la version 1 existente tiene un hash diferente.'
        );

        SELECT COUNT(*)
          INTO v_conteo
          FROM RL_MR_VERSIONES_FORMULARIO
         WHERE VER_FAMILIA_ID = v_familia_id
           AND VER_VIGENTE = 1
           AND VER_ID <> v_version_id;

        exigir(
            v_conteo = 0,
            -20114,
            'EJECUCION BLOQUEADA: existe otra version vigente para la familia.'
        );

        UPDATE RL_MR_VERSIONES_FORMULARIO
           SET VER_CODIGO = c_version_codigo,
               VER_ESTADO = 'PUBLISHED',
               VER_VIGENTE = 1,
               VER_FECHA_INICIO = NVL(VER_FECHA_INICIO, SYSDATE),
               VER_FECHA_FIN = NULL
         WHERE VER_ID = v_version_id;
    END IF;

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_VERSIONES_FORMULARIO
     WHERE VER_FAMILIA_ID = v_familia_id
       AND VER_ESTADO = 'PUBLISHED'
       AND VER_VIGENTE = 1;
    exigir(v_conteo = 1, -20115, 'VALIDACION FALLIDA: debe existir exactamente una version publicada y vigente.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_CATALOGOS
     WHERE CAT_CODIGO IN (
        'MR_FRECUENCIA_1_5',
        'MR_IMPACTO_1_5',
        'MR_NIVEL_RIESGO',
        'MR_RESPUESTA_RIESGO'
     )
       AND CAT_ACTIVO = 1;
    exigir(v_conteo = 4, -20116, 'VALIDACION FALLIDA: faltan catalogos requeridos.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_ELEMENTOS_CATALOGO e
      JOIN RL_MR_CATALOGOS c ON c.CAT_ID = e.ELE_CATALOGO_ID
     WHERE c.CAT_CODIGO IN (
        'MR_FRECUENCIA_1_5',
        'MR_IMPACTO_1_5',
        'MR_NIVEL_RIESGO',
        'MR_RESPUESTA_RIESGO'
     )
       AND e.ELE_ACTIVO = 1;
    exigir(v_conteo = 18, -20117, 'VALIDACION FALLIDA: los catalogos requieren exactamente 18 elementos activos.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_REGLAS_CALCULO
     WHERE REG_CODIGO = 'CALCULO_VRI_VRR'
       AND REG_VERSION = '1.0'
       AND REG_ALGORITMO_ID = 'MATRICES_VRI_ADITIVO_1_9'
       AND REG_ACTIVA = 1;
    exigir(v_conteo = 1, -20118, 'VALIDACION FALLIDA: falta la regla de calculo requerida.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_ELEMENTOS_CATALOGO e
     WHERE NOT EXISTS (
        SELECT 1
          FROM RL_MR_CATALOGOS c
         WHERE c.CAT_ID = e.ELE_CATALOGO_ID
     );
    exigir(v_conteo = 0, -20119, 'VALIDACION FALLIDA: existen elementos de catalogo huerfanos.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_VERSIONES_FORMULARIO v
     WHERE NOT EXISTS (
        SELECT 1
          FROM RL_MR_FAMILIAS_FORMULARIO f
         WHERE f.FAM_ID = v.VER_FAMILIA_ID
     );
    exigir(v_conteo = 0, -20120, 'VALIDACION FALLIDA: existen versiones huerfanas.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_VERSIONES_FORMULARIO v
     WHERE v.VER_USR_CREACION IS NULL
        OR NOT EXISTS (
            SELECT 1
              FROM RL_USUARIOS u
             WHERE u.USR_ID = v.VER_USR_CREACION
        );
    exigir(v_conteo = 0, -20121, 'VALIDACION FALLIDA: existe una version sin usuario institucional valido.');

    SELECT COUNT(*)
      INTO v_conteo
      FROM RL_MR_VERSIONES_FORMULARIO
     WHERE VER_ID = v_version_id
       AND LOWER(VER_HASH) = c_hash
       AND DBMS_LOB.COMPARE(VER_JSON, v_json) = 0;
    exigir(v_conteo = 1, -20122, 'VALIDACION FALLIDA: JSON y hash no corresponden al contrato oficial.');

    COMMIT;

    DBMS_OUTPUT.PUT_LINE('FAMILIA_ID=' || v_familia_id);
    DBMS_OUTPUT.PUT_LINE('VERSION_ID=' || v_version_id);
    DBMS_OUTPUT.PUT_LINE('USUARIO_CREACION_ID=' || v_usuario_id);
    DBMS_OUTPUT.PUT_LINE('HASH_SHA256=' || c_hash);
    DBMS_OUTPUT.PUT_LINE('SEMILLAS FASE 11 BLOQUE 1: APLICADAS Y VALIDADAS');
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('SEMILLAS FASE 11 BLOQUE 1: ERROR - ' || SQLERRM);
        RAISE;
END;
/

SET DEFINE ON
