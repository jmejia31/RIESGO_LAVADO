-- ============================================================
-- SISTEMA DE GESTION DE RIESGO DE LAVADO DE ACTIVOS
-- Paquete: Modulo Matrices de Riesgos
-- Estado: BLOQUEADO DURANTE PREPARACION Y CERTIFICACION ORACLE.
--
-- Este punto de entrada no crea, altera ni elimina objetos. El modelo objetivo
-- de 17 tablas requiere una transicion manual y destructiva mediante:
--
--   transicion/06_reconstruir_modelo_17_tablas.sql
--
-- Ese script solo puede ejecutarse con respaldo validado, base exclusiva de
-- pruebas, ventana aprobada y autorizacion expresa de Javier Mejia y del DBA.
-- No incorporar el script 06 ni ningun DDL de Matrices a los maestros
-- automaticos antes de completar la certificacion fisica Oracle.
-- ============================================================

WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

DECLARE
    v_esquema_actual VARCHAR2(128);
BEGIN
    SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA')
      INTO v_esquema_actual
      FROM DUAL;

    IF UPPER(v_esquema_actual) <> 'RIESGO_LAVADO' THEN
        RAISE_APPLICATION_ERROR(
            -20401,
            'EJECUCION BLOQUEADA: esquema distinto de RIESGO_LAVADO.'
        );
    END IF;

    RAISE_APPLICATION_ERROR(
        -20402,
        'EJECUCION BLOQUEADA: Matrices de Riesgos esta en cuarentena pre-Oracle. Consulte el acta de Fase 8.'
    );
END;
/
