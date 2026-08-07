-- ============================================================
-- FASE 11 - BLOQUE 6
-- Auditoría, transacciones y manejo de fallos
-- Prueba transaccional controlada: inserta y revierte; NO deja datos persistidos.
-- ============================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
    c_codigo CONSTANT VARCHAR2(30) := 'F11_B6_ROLLBACK_TEST';
    v_usuario_id NUMBER(10);
    v_riesgo_id NUMBER(15);
    v_error NUMBER;
    v_count NUMBER;

    PROCEDURE exigir(p_cond BOOLEAN, p_code NUMBER, p_message VARCHAR2) IS
    BEGIN
        IF NOT p_cond THEN RAISE_APPLICATION_ERROR(p_code, p_message); END IF;
    END;
BEGIN
    exigir(UPPER(SYS_CONTEXT('USERENV','CURRENT_SCHEMA')) = 'RIESGO_LAVADO', -20701,
           'CURRENT_SCHEMA debe ser RIESGO_LAVADO.');

    SELECT COUNT(*) INTO v_count FROM USER_TABLES WHERE TABLE_NAME = 'RL_AUDITORIA';
    exigir(v_count = 1, -20702, 'No existe RL_AUDITORIA.');
    SELECT COUNT(*) INTO v_count FROM USER_SEQUENCES WHERE SEQUENCE_NAME = 'SEQ_RL_AUDITORIA';
    exigir(v_count = 1, -20703, 'No existe SEQ_RL_AUDITORIA.');

    SELECT MIN(USR_ID) INTO v_usuario_id FROM RL_USUARIOS WHERE USR_ACTIVO = 1;
    exigir(v_usuario_id IS NOT NULL, -20704, 'No existe usuario institucional activo para la prueba.');

    SELECT COUNT(*) INTO v_count FROM RL_MR_RIESGOS WHERE RIE_CODIGO = c_codigo;
    exigir(v_count = 0, -20705, 'Existe un registro previo con el código reservado de prueba; no se continuará.');

    -- Simulación real de fallo posterior al dato funcional y a su auditoría.
    BEGIN
        v_riesgo_id := SEQ_RL_MR_RIESGOS.NEXTVAL;

        INSERT INTO RL_MR_RIESGOS (
            RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION,
            RIE_ACTIVO, RIE_USR_CREACION, RIE_FECHA_CREACION
        ) VALUES (
            v_riesgo_id, c_codigo, 'Prueba transaccional Fase 11 Bloque 6',
            'Registro efímero; debe desaparecer mediante ROLLBACK.',
            1, v_usuario_id, SYSDATE
        );

        INSERT INTO RL_AUDITORIA (
            AUD_ID, AUD_TABLA, AUD_REGISTRO_ID, AUD_ACCION,
            AUD_DATOS_ANT, AUD_DATOS_NVO, AUD_USR_ID, AUD_USR_EMAIL,
            AUD_IP, AUD_FECHA, AUD_MODULO
        ) VALUES (
            SEQ_RL_AUDITORIA.NEXTVAL,
            'RL_MR_RIESGOS', TO_CHAR(v_riesgo_id), 'INSERT',
            NULL, '{"prueba":"rollback_controlado"}', v_usuario_id, NULL,
            NULL, SYSDATE, 'MatricesRiesgos'
        );

        RAISE_APPLICATION_ERROR(-20750, 'FALLO CONTROLADO FASE 11 BLOQUE 6');
    EXCEPTION
        WHEN OTHERS THEN
            v_error := SQLCODE;
            ROLLBACK;
            IF v_error <> -20750 THEN
                RAISE;
            END IF;
    END;

    SELECT COUNT(*) INTO v_count FROM RL_MR_RIESGOS WHERE RIE_CODIGO = c_codigo;
    exigir(v_count = 0, -20706, 'ROLLBACK FALLIDO: persistió el riesgo efímero.');

    SELECT COUNT(*) INTO v_count
      FROM RL_AUDITORIA
     WHERE AUD_TABLA = 'RL_MR_RIESGOS'
       AND AUD_DATOS_NVO LIKE '%rollback_controlado%';
    exigir(v_count = 0, -20707, 'ROLLBACK FALLIDO: persistió la auditoría efímera.');

    SELECT COUNT(*) INTO v_count
      FROM USER_OBJECTS
     WHERE (OBJECT_NAME LIKE 'RL_MR_%' OR OBJECT_NAME = 'RL_AUDITORIA')
       AND STATUS <> 'VALID';
    exigir(v_count = 0, -20708, 'Existen objetos funcionales inválidos.');

    SELECT COUNT(*) INTO v_count
      FROM USER_CONSTRAINTS
     WHERE TABLE_NAME LIKE 'RL_MR_%'
       AND STATUS <> 'ENABLED';
    exigir(v_count = 0, -20709, 'Existen restricciones RL_MR_* deshabilitadas.');

    DBMS_OUTPUT.PUT_LINE('PRUEBA ROLLBACK DATO + AUDITORIA: CORRECTA');
    DBMS_OUTPUT.PUT_LINE('VALIDACION FASE 11 BLOQUE 6: CORRECTA');
END;
/
