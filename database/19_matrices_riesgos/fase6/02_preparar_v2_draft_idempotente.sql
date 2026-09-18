-- FASE 6 - Preparación segura de MATRIZ_RIESGOS_LAFT_V2
-- Ejecutar únicamente en hpprod1 / RIESGO_LAVADO por el DBA autorizado.
-- No publica la versión, no cambia V1 y es idempotente por código/hash.

SET DEFINE OFF
SET SERVEROUTPUT ON SIZE UNLIMITED
SET FEEDBACK ON
SET VERIFY ON
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
    c_familia_codigo CONSTANT VARCHAR2(50) := 'MATRIZ_RIESGOS_LAFT';
    c_v1_codigo     CONSTANT VARCHAR2(30) := 'MATRIZ_RIESGOS_LAFT_V1';
    c_v2_codigo     CONSTANT VARCHAR2(30) := 'MATRIZ_RIESGOS_LAFT_V2';
    c_v2_version    CONSTANT NUMBER := 2;
    c_v2_hash       CONSTANT VARCHAR2(64) := '769b5b25cd7cbb03b69782b5864828fb53155c070483b6d22ef5adf36d295651';
    c_v1_hash       CONSTANT VARCHAR2(64) := 'f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90';
    v_schema        VARCHAR2(128);
    v_familia_id    NUMBER(15);
    v_v1_id         NUMBER(15);
    v_v2_id         NUMBER(15);
    v_usuario_id    NUMBER(10);
    v_hash          VARCHAR2(64);
    v_v2_estado     VARCHAR2(30);
    v_v2_vigente    NUMBER(1);
    v_json_existente CLOB;
    v_conteo        NUMBER;
v_json CLOB;

    PROCEDURE exigir(p_condicion BOOLEAN, p_codigo NUMBER, p_mensaje VARCHAR2) IS
    BEGIN
        IF NOT p_condicion THEN
            RAISE_APPLICATION_ERROR(p_codigo, p_mensaje);
        END IF;
    END;
BEGIN
    v_json := q'~{"codigoFormulario":"MATRIZ_RIESGOS_LAFT","nombreFormulario":"Matriz de Riesgos LA/FT","version":"2.0","secciones":[{"id":"identificacion","clave":"identificacion","titulo":"Identificación del riesgo","orden":1,"campos":[{"id":"area_principal","clave":"area_principal","etiqueta":"Área principal","tipo":"texto","obligatorio":true,"soloLectura":false},{"id":"dueno_riesgo","clave":"dueno_riesgo","etiqueta":"Dueño del riesgo","tipo":"texto","obligatorio":true,"soloLectura":false},{"id":"tipo_riesgo","clave":"tipo_riesgo","etiqueta":"Tipo de riesgo","tipo":"texto","obligatorio":false,"soloLectura":false},{"id":"procedimiento","clave":"procedimiento","etiqueta":"Procedimiento","tipo":"texto","obligatorio":false,"soloLectura":false},{"id":"objetivos_estrategicos","clave":"objetivos_estrategicos","etiqueta":"Objetivo(s) estratégico(s)","tipo":"texto","obligatorio":false,"soloLectura":false},{"id":"regimen_afectado","clave":"regimen_afectado","etiqueta":"Régimen afectado","tipo":"texto","obligatorio":false,"soloLectura":false},{"id":"transversalidad","clave":"transversalidad","etiqueta":"Transversalidad o interrelación con otros riesgos","tipo":"texto","obligatorio":false,"soloLectura":false}]},{"id":"riesgo_inherente","clave":"riesgo_inherente","titulo":"Valoración del riesgo inherente","orden":2,"campos":[{"id":"frecuencia_inherente","clave":"frecuencia_inherente","etiqueta":"Frecuencia inherente","tipo":"selector-catalogo","codigoCatalogo":"MR_FRECUENCIA_1_5","obligatorio":true,"soloLectura":false},{"id":"impacto_inherente","clave":"impacto_inherente","etiqueta":"Impacto inherent~' ||
              q'~e","tipo":"selector-catalogo","codigoCatalogo":"MR_IMPACTO_1_5","obligatorio":true,"soloLectura":false},{"id":"nivel_inherente","clave":"nivel_inherente","etiqueta":"Nivel inherente","tipo":"selector-catalogo","codigoCatalogo":"MR_NIVEL_RIESGO","obligatorio":true,"soloLectura":false}]},{"id":"controles","clave":"controles","titulo":"Efectividad de controles","orden":3,"campos":[{"id":"controles_preventivo","clave":"controles_preventivo","etiqueta":"Control preventivo (%)","tipo":"numero","obligatorio":true,"soloLectura":false},{"id":"controles_detectivo","clave":"controles_detectivo","etiqueta":"Control detectivo (%)","tipo":"numero","obligatorio":true,"soloLectura":false},{"id":"controles_correctivo","clave":"controles_correctivo","etiqueta":"Control correctivo (%)","tipo":"numero","obligatorio":true,"soloLectura":false}]},{"id":"riesgo_residual","clave":"riesgo_residual","titulo":"Valoración y respuesta del riesgo residual","orden":4,"campos":[{"id":"frecuencia_residual","clave":"frecuencia_residual","etiqueta":"Frecuencia residual","tipo":"selector-catalogo","codigoCatalogo":"MR_FRECUENCIA_1_5","obligatorio":true,"soloLectura":false},{"id":"impacto_residual","clave":"impacto_residual","etiqueta":"Impacto residual","tipo":"selector-catalogo","codigoCatalogo":"MR_IMPACTO_1_5","obligatorio":true,"soloLectura":false},{"id":"nivel_residual","clave":"nivel_residual","etiqueta":"Nivel residual","tipo":"selector-catalogo","codigoCatalogo":"MR_NIVEL_RIESGO","obligatorio":true,"soloLectura":false},{"id":"respuesta_riesgo","clave":"respuesta_riesgo","etiqueta":"Respuesta al riesgo"~' ||
              q'~,"tipo":"selector-catalogo","codigoCatalogo":"MR_RESPUESTA_RIESGO","obligatorio":true,"soloLectura":false}]}],"catalogos":[{"codigo":"MR_FRECUENCIA_1_5","nombre":"Frecuencia de riesgo (1–5)","elementos":[{"codigo":"1","valor":"1 - Rara","orden":1},{"codigo":"2","valor":"2 - Improbable","orden":2},{"codigo":"3","valor":"3 - Posible","orden":3},{"codigo":"4","valor":"4 - Probable","orden":4},{"codigo":"5","valor":"5 - Casi segura","orden":5}]},{"codigo":"MR_IMPACTO_1_5","nombre":"Impacto de riesgo (1–5)","elementos":[{"codigo":"1","valor":"1 - Insignificante","orden":1},{"codigo":"2","valor":"2 - Menor","orden":2},{"codigo":"3","valor":"3 - Moderado","orden":3},{"codigo":"4","valor":"4 - Mayor","orden":4},{"codigo":"5","valor":"5 - Catastrófico","orden":5}]},{"codigo":"MR_NIVEL_RIESGO","nombre":"Nivel de riesgo","elementos":[{"codigo":"BAJO","valor":"Bajo","orden":1},{"codigo":"MODERADO","valor":"Moderado","orden":2},{"codigo":"ALTO","valor":"Alto","orden":3},{"codigo":"CRITICO","valor":"Crítico","orden":4}]},{"codigo":"MR_RESPUESTA_RIESGO","nombre":"Respuesta al riesgo","elementos":[{"codigo":"EVITAR","valor":"Evitar","orden":1},{"codigo":"MITIGAR","valor":"Mitigar","orden":2},{"codigo":"TRANSFERIR","valor":"Transferir","orden":3},{"codigo":"ACEPTAR","valor":"Aceptar","orden":4}]}],"reglas":[{"codigo":"CALCULO_VRI_VRR","version":"1.0","algoritmoId":"MATRICES_VRI_ADITIVO_1_9","parametros":{"frecuenciaMin":1,"frecuenciaMax":5,"impactoMin":1,"impactoMax":5,"vriMin":1,"vriMax":9,"pesoPreventivo":0.7,"pesoDetectivo":0.15,"pesoCorrectivo":0.15,"vrrMin":1}}]}~';
    SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') INTO v_schema FROM DUAL;
    exigir(v_schema = 'RIESGO_LAVADO', -20601, 'ESQUEMA INCORRECTO: se esperaba RIESGO_LAVADO.');

    SELECT FAM_ID INTO v_familia_id
      FROM RL_MR_FAMILIAS_FORMULARIO
     WHERE FAM_CODIGO = c_familia_codigo
       AND FAM_PREDETERMINADA = 1
       AND FAM_ACTIVO = 1;

    SELECT VER_ID, VER_HASH INTO v_v1_id, v_hash
      FROM RL_MR_VERSIONES_FORMULARIO
     WHERE VER_FAMILIA_ID = v_familia_id
       AND VER_CODIGO = c_v1_codigo
       AND VER_VERSION = 1
       AND VER_ESTADO = 'PUBLISHED'
       AND VER_VIGENTE = 1;
    exigir(LOWER(v_hash) = c_v1_hash, -20602, 'V1 MUTADA O HASH DIFERENTE: no se creará V2.');

    SELECT MIN(USR_ID) INTO v_usuario_id FROM RL_USUARIOS WHERE USR_ACTIVO = 1;
    exigir(v_usuario_id IS NOT NULL, -20603, 'No existe usuario institucional activo para auditoría.');
    exigir(DBMS_LOB.GETLENGTH(v_json) > 0, -20604, 'JSON V2 vacío.');

    SELECT COUNT(*) INTO v_conteo
      FROM RL_MR_VERSIONES_FORMULARIO
     WHERE VER_FAMILIA_ID = v_familia_id
       AND (VER_CODIGO = c_v2_codigo OR VER_VERSION = c_v2_version);
    exigir(v_conteo <= 1, -20605, 'Existe más de un candidato para V2; revisión manual requerida.');

    BEGIN
        SELECT VER_ID, VER_HASH, VER_JSON, VER_ESTADO, VER_VIGENTE
          INTO v_v2_id, v_hash, v_json_existente, v_v2_estado, v_v2_vigente
          FROM RL_MR_VERSIONES_FORMULARIO
         WHERE VER_FAMILIA_ID = v_familia_id
           AND VER_CODIGO = c_v2_codigo
           AND VER_VERSION = c_v2_version;
        exigir(LOWER(v_hash) = c_v2_hash, -20606, 'V2 existente con hash diferente.');
        exigir(DBMS_LOB.COMPARE(v_json_existente, v_json) = 0, -20607, 'V2 existente con JSON diferente.');
        exigir(v_v2_estado = 'DRAFT', -20608, 'V2 existente no está DRAFT.');
        exigir(v_v2_vigente = 0, -20609, 'V2 existente no puede estar vigente.');
        DBMS_OUTPUT.PUT_LINE('V2_EXISTENTE_ID=' || v_v2_id || ' | ACCION=NO_OP');
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_v2_id := SEQ_RL_MR_VERSIONES.NEXTVAL;
            INSERT INTO RL_MR_VERSIONES_FORMULARIO (
                VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, VER_HASH,
                VER_ESTADO, VER_VIGENTE, VER_FECHA_INICIO, VER_FECHA_FIN,
                VER_FECHA_CREACION, VER_USR_CREACION
            ) VALUES (
                v_v2_id, v_familia_id, c_v2_codigo, c_v2_version, v_json, c_v2_hash,
                'DRAFT', 0, NULL, NULL, SYSDATE, v_usuario_id
            );
            DBMS_OUTPUT.PUT_LINE('V2_CREADA_ID=' || v_v2_id || ' | ESTADO=DRAFT | VIGENTE=0');
    END;

    COMMIT;
END;
/

-- POSTCHECK (read-only)
SELECT VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_ESTADO, VER_VIGENTE,
       VER_HASH, DBMS_LOB.GETLENGTH(VER_JSON) AS JSON_LENGTH
  FROM RL_MR_VERSIONES_FORMULARIO
 WHERE VER_CODIGO IN ('MATRIZ_RIESGOS_LAFT_V1', 'MATRIZ_RIESGOS_LAFT_V2')
 ORDER BY VER_VERSION;

-- ROLLBACK CONTROLADO (ejecutar sólo si se requiere revertir esta preparación):
-- DELETE FROM RL_MR_VERSIONES_FORMULARIO
--  WHERE VER_FAMILIA_ID = (SELECT FAM_ID FROM RL_MR_FAMILIAS_FORMULARIO WHERE FAM_CODIGO = 'MATRIZ_RIESGOS_LAFT')
--    AND VER_CODIGO = 'MATRIZ_RIESGOS_LAFT_V2'
--    AND VER_VERSION = 2
--    AND VER_ESTADO = 'DRAFT'
--    AND VER_VIGENTE = 0
--    AND LOWER(VER_HASH) = '769b5b25cd7cbb03b69782b5864828fb53155c070483b6d22ef5adf36d295651';
-- COMMIT;
