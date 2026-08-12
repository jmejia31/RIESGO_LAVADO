-- ============================================================
-- SISTEMA DE GESTIÓN DE RIESGOS LA/FT - IHSS
-- Módulo: Matrices de Riesgos
-- Script: 00_retiro_controlado_modelo_prueba.sql
-- Objetivo: Retiro ordenado de tablas y secuencias PREVIAS del modelo de prueba
--           (iteración preliminar de la Fase 1) del esquema RIESGO_LAVADO.
-- Clasificación: SCRIPT DESTRUCTIVO MANUAL (EXCLUSIVO PARA DBA).
-- ============================================================
--
-- ╔══════════════════════════════════════════════════════════════╗
-- ║  REQUISITOS PREVIOS OBLIGATORIOS (VERIFICAR ANTES DE CORRER)  ║
-- ╠══════════════════════════════════════════════════════════════╣
-- ║  1. RESPALDO DBA: El DBA debe ejecutar un export/backup     ║
-- ║     completo del esquema RIESGO_LAVADO antes de correr      ║
-- ║     este script. Comando de referencia:                     ║
-- ║       expdp RIESGO_LAVADO/*** directory=BACKUPS             ║
-- ║       dumpfile=pre_retiro_mr_%date%.dmp                     ║
-- ║       schemas=RIESGO_LAVADO logfile=pre_retiro_mr.log       ║
-- ║                                                              ║
-- ║  2. CONFIRMACIÓN DE OBJETOS DE PRUEBA: Los objetos listados ║
-- ║     en este script pertenecen EXCLUSIVAMENTE al modelo       ║
-- ║     preliminar de prueba (Fase 1, iteración antigua).        ║
-- ║     NO son tablas del modelo definitivo de 34 tablas.        ║
-- ║     Inventario de objetos de prueba a retirar:               ║
-- ║       TABLAS (13): RL_MR_EVIDENCIAS, RL_MR_HISTORIAL,       ║
-- ║         RL_MR_INTEGRACION_DNP, RL_MR_PLANES_ACCION,         ║
-- ║         RL_MR_CONTROLES, RL_MR_RESULTADOS, RL_MR_DETALLE,   ║
-- ║         RL_MR_MATRICES, RL_MR_CRITERIOS, RL_MR_ESCALAS,     ║
-- ║         RL_MR_VARIABLES, RL_MR_FACTORES, RL_MR_MODELOS.     ║
-- ║       SECUENCIAS (13): SEQ_RL_MR_MODELOS a                  ║
-- ║         SEQ_RL_MR_INTEGRACION_DNP (ver inventario abajo).    ║
-- ║                                                              ║
-- ║  3. EXCLUSIÓN DEL FLUJO AUTOMÁTICO: Este script NO forma    ║
-- ║     parte de la cadena automática de instalación. Debe ser   ║
-- ║     ejecutado de forma síncrona y manual por el DBA.         ║
-- ║                                                              ║
-- ║  4. NOTA SOBRE DDL DE ORACLE: Las sentencias DDL (DROP      ║
-- ║     TABLE, DROP SEQUENCE) realizan un COMMIT implícito en    ║
-- ║     Oracle. Si ocurre un error a mitad de ejecución, los     ║
-- ║     objetos ya eliminados NO se pueden recuperar mediante    ║
-- ║     ROLLBACK. El respaldo DBA previo es la única salvaguarda.║
-- ╚══════════════════════════════════════════════════════════════╝
--
-- COMANDO AUTORIZADO PARA EJECUCIÓN POR EL DBA:
--   sqlplus RIESGO_LAVADO/***@INSTANCIA @00_retiro_controlado_modelo_prueba.sql EJECUTAR
--
-- ============================================================

-- DIRECTIVA OBLIGATORIA PARA SQL*PLUS: ABORTAR EJECUCIÓN ANTE ERROR
-- Nota: No revertirá DDL ya ejecutado (commit implícito de Oracle).
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK

-- PARÁMETRO DE ENTRADA OBLIGATORIO PARA SQL*PLUS: &1 (Palabra clave 'EJECUTAR')
DEFINE autorizacion = '&1';

-- BLOQUE DE SEGURIDAD EXPLICITO - IMPEDIR EJECUCIÓN ACCIDENTAL
DECLARE
  v_esquema_actual     VARCHAR2(100);
  v_auth               VARCHAR2(50) := q'[&autorizacion]';
BEGIN
  SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') INTO v_esquema_actual FROM DUAL;
  
  IF UPPER(v_esquema_actual) <> 'RIESGO_LAVADO' THEN
    RAISE_APPLICATION_ERROR(-20098, 'EJECUCIÓN BLOQUEADA: Este script solo puede ejecutarse en el esquema RIESGO_LAVADO. Esquema detectado: ' || v_esquema_actual);
  END IF;
  
  IF UPPER(v_auth) <> 'EJECUTAR' THEN
    RAISE_APPLICATION_ERROR(-20099, 'EJECUCIÓN BLOQUEADA: Script destructivo. El DBA debe pasar "EJECUTAR" como primer argumento de SQL*Plus.');
  END IF;
END;
/

-- ============================================================
-- VERIFICACIÓN PREVIA: Confirmar que solo existen objetos de prueba
-- ============================================================
DECLARE
  v_count_definitivas NUMBER := 0;
  v_evi_es_definitiva NUMBER := 0;
BEGIN
  -- Verificar que NO existan tablas del modelo definitivo (Fase 3).
  -- Si alguna tabla definitiva existe, este retiro no debe ejecutarse.
  SELECT COUNT(*) INTO v_count_definitivas
    FROM USER_TABLES
   WHERE TABLE_NAME IN (
     'RL_MR_FAMILIAS_FORMULARIO', 'RL_MR_VERSIONES_FORMULARIO',
     'RL_MR_CAMPOS_FORMULARIO', 'RL_MR_APROBACIONES_FORMULARIO',
     'RL_MR_PERMISOS_FORMULARIO', 'RL_MR_EVALUACIONES_RIESGO',
     'RL_MR_REVISIONES_EVALUACION', 'RL_MR_PROYECCIONES_EVALUACION',
     'RL_MR_FLUJOS_EVALUACION', 'RL_MR_CONTROLES_RIESGO',
     'RL_MR_EVALUACIONES_CONTROL', 'RL_MR_PLANES',
     'RL_MR_ACTIVIDADES', 'RL_MR_SENALES_ALERTA',
     'RL_MR_AUTOMONITOREO', 'RL_MR_CATALOGOS',
     'RL_MR_ELEMENTOS_CATALOGO', 'RL_MR_REGLAS_CALCULO',
     'RL_MR_TRAZAS_CALCULO', 'RL_MR_LOTES_IMPORTACION',
     'RL_MR_DETALLES_IMPORTACION', 'RL_MR_AUDITORIA'
   );

  IF v_count_definitivas > 0 THEN
    RAISE_APPLICATION_ERROR(-20097,
      'EJECUCIÓN BLOQUEADA: Se detectaron ' || v_count_definitivas ||
      ' tabla(s) del modelo DEFINITIVO (Fase 3) en el esquema. ' ||
      'Este script solo debe ejecutarse cuando existen objetos de PRUEBA previos. ' ||
      'Verifique manualmente antes de proceder.');
  END IF;

  -- Verificación adicional: RL_MR_EVIDENCIAS existe en AMBOS modelos
  -- (antiguo y definitivo). La versión definitiva tiene la columna EVI_HASH;
  -- la antigua no. Si EVI_HASH existe, es la tabla definitiva y NO debe eliminarse.
  SELECT COUNT(*) INTO v_evi_es_definitiva
    FROM USER_TAB_COLUMNS
   WHERE TABLE_NAME = 'RL_MR_EVIDENCIAS'
     AND COLUMN_NAME = 'EVI_HASH';

  IF v_evi_es_definitiva > 0 THEN
    RAISE_APPLICATION_ERROR(-20096,
      'EJECUCIÓN BLOQUEADA: La tabla RL_MR_EVIDENCIAS pertenece al modelo DEFINITIVO ' ||
      '(se detectó la columna EVI_HASH). Este script solo retira objetos del modelo ' ||
      'de prueba preliminar. NO se eliminará esta tabla.');
  END IF;

  DBMS_OUTPUT.PUT_LINE('VERIFICACIÓN OK: No se detectaron tablas del modelo definitivo. Procediendo con retiro de objetos de prueba.');
END;
/

-- ============================================================
-- RETIRO EN ORDEN INVERSO DE DEPENDENCIAS
-- Nivel 3 (hojas) → Nivel 2 (transaccionales) → Nivel 1 (maestras)
-- ============================================================
DECLARE
  FUNCTION nombre_tabla_prueba_permitido(p_table_name IN VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    IF p_table_name NOT IN (
      'RL_MR_INTEGRACION_DNP', 'RL_MR_HISTORIAL', 'RL_MR_EVIDENCIAS',
      'RL_MR_PLANES_ACCION', 'RL_MR_CONTROLES', 'RL_MR_RESULTADOS', 'RL_MR_DETALLE',
      'RL_MR_MATRICES', 'RL_MR_CRITERIOS', 'RL_MR_ESCALAS', 'RL_MR_VARIABLES',
      'RL_MR_FACTORES', 'RL_MR_MODELOS'
    ) THEN
      RAISE_APPLICATION_ERROR(-20095, 'Objeto de tabla no autorizado para retiro: ' || p_table_name);
    END IF;

    RETURN DBMS_ASSERT.SIMPLE_SQL_NAME(p_table_name);
  END;

  FUNCTION nombre_secuencia_prueba_permitido(p_seq_name IN VARCHAR2) RETURN VARCHAR2 IS
  BEGIN
    IF p_seq_name NOT IN (
      'SEQ_RL_MR_INTEGRACION_DNP', 'SEQ_RL_MR_HISTORIAL', 'SEQ_RL_MR_EVIDENCIAS',
      'SEQ_RL_MR_PLANES_ACCION', 'SEQ_RL_MR_RESULTADOS', 'SEQ_RL_MR_CONTROLES',
      'SEQ_RL_MR_DETALLE', 'SEQ_RL_MR_MATRICES', 'SEQ_RL_MR_CRITERIOS',
      'SEQ_RL_MR_ESCALAS', 'SEQ_RL_MR_VARIABLES', 'SEQ_RL_MR_FACTORES', 'SEQ_RL_MR_MODELOS'
    ) THEN
      RAISE_APPLICATION_ERROR(-20094, 'Objeto de secuencia no autorizado para retiro: ' || p_seq_name);
    END IF;

    RETURN DBMS_ASSERT.SIMPLE_SQL_NAME(p_seq_name);
  END;

  PROCEDURE drop_table_if_exists(p_table_name IN VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE ' || nombre_tabla_prueba_permitido(p_table_name) || ' CASCADE CONSTRAINTS'; -- NOSONAR: DDL dinámico obligatorio para retiro condicional; nombre validado contra lista cerrada y DBMS_ASSERT.
    DBMS_OUTPUT.PUT_LINE('Tabla eliminada: ' || p_table_name);
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE != -942 THEN -- ORA-00942: table or view does not exist
        RAISE;
      ELSE
        DBMS_OUTPUT.PUT_LINE('Tabla no encontrada (ignorada): ' || p_table_name);
      END IF;
  END;

  PROCEDURE drop_sequence_if_exists(p_seq_name IN VARCHAR2) IS
  BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE ' || nombre_secuencia_prueba_permitido(p_seq_name); -- NOSONAR: DDL dinámico obligatorio para retiro condicional; nombre validado contra lista cerrada y DBMS_ASSERT.
    DBMS_OUTPUT.PUT_LINE('Secuencia eliminada: ' || p_seq_name);
  EXCEPTION
    WHEN OTHERS THEN
      IF SQLCODE != -2289 THEN -- ORA-02289: sequence does not exist
        RAISE;
      ELSE
        DBMS_OUTPUT.PUT_LINE('Secuencia no encontrada (ignorada): ' || p_seq_name);
      END IF;
  END;
BEGIN
  DBMS_OUTPUT.PUT_LINE('--- INICIANDO RETIRO CONTROLADO DE ESTRUCTURAS DE PRUEBA RL_MR_* ---');

  -- Nivel 3: Tablas hoja (sin dependencias entrantes)
  drop_table_if_exists('RL_MR_INTEGRACION_DNP');
  drop_table_if_exists('RL_MR_HISTORIAL');
  drop_table_if_exists('RL_MR_EVIDENCIAS');

  -- Nivel 2: Tablas transaccionales (dependientes del Nivel 1)
  drop_table_if_exists('RL_MR_PLANES_ACCION');
  drop_table_if_exists('RL_MR_CONTROLES');
  drop_table_if_exists('RL_MR_RESULTADOS');
  drop_table_if_exists('RL_MR_DETALLE');

  -- Nivel 1: Tablas maestras y de configuración (raíces)
  drop_table_if_exists('RL_MR_MATRICES');
  drop_table_if_exists('RL_MR_CRITERIOS');
  drop_table_if_exists('RL_MR_ESCALAS');
  drop_table_if_exists('RL_MR_VARIABLES');
  drop_table_if_exists('RL_MR_FACTORES');
  drop_table_if_exists('RL_MR_MODELOS');

  -- Secuencias asociadas (orden inverso al de creación)
  drop_sequence_if_exists('SEQ_RL_MR_INTEGRACION_DNP');
  drop_sequence_if_exists('SEQ_RL_MR_HISTORIAL');
  drop_sequence_if_exists('SEQ_RL_MR_EVIDENCIAS');
  drop_sequence_if_exists('SEQ_RL_MR_PLANES_ACCION');
  drop_sequence_if_exists('SEQ_RL_MR_RESULTADOS');
  drop_sequence_if_exists('SEQ_RL_MR_CONTROLES');
  drop_sequence_if_exists('SEQ_RL_MR_DETALLE');
  drop_sequence_if_exists('SEQ_RL_MR_MATRICES');
  drop_sequence_if_exists('SEQ_RL_MR_CRITERIOS');
  drop_sequence_if_exists('SEQ_RL_MR_ESCALAS');
  drop_sequence_if_exists('SEQ_RL_MR_VARIABLES');
  drop_sequence_if_exists('SEQ_RL_MR_FACTORES');
  drop_sequence_if_exists('SEQ_RL_MR_MODELOS');

  DBMS_OUTPUT.PUT_LINE('--- RETIRO CONTROLADO COMPLETADO EXITOSAMENTE ---');
END;
/

COMMIT;

-- ============================================================
-- SCRIPT DE REVERSIÓN (REFERENCIA)
-- ============================================================
-- Si es necesario restaurar los objetos eliminados por este script,
-- el DBA debe ejecutar la importación del backup previo:
--
--   impdp RIESGO_LAVADO/***@INSTANCIA directory=BACKUPS
--     dumpfile=pre_retiro_mr_<fecha>.dmp
--     schemas=RIESGO_LAVADO
--     table_exists_action=SKIP
--     logfile=restauracion_mr.log
--
-- IMPORTANTE: No existe un script DDL de re-creación para las tablas
-- de prueba porque el modelo definitivo (Fase 3) las reemplaza por
-- completo con 34 tablas nuevas. La restauración solo aplica si se
-- necesita recuperar datos de prueba previos a la migración.
-- ============================================================
