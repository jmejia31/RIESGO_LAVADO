-- ============================================================
-- Sistema de Gestión de Riesgos LA/FT - IHSS
-- Fase 3. Modelo de datos y arquitectura Oracle
-- Script: 03_seed_metodologia_matrices_riesgos.sql
-- Objetivo: Parametrización inicial de metodología base para Matrices de Riesgos.
-- Clasificación: Script activo idempotente. Ejecutar con respaldo y aprobación DBA.
-- Responsable documental: Javier Mejía
-- Reglas: idempotente por modelo/factor/variable/escala, sin DROP, sin TRUNCATE, sin DELETE.
-- ============================================================

SET DEFINE OFF;

DECLARE
  v_modelo_id NUMBER;
  v_total     NUMBER;

  PROCEDURE upsert_factor(
    p_codigo IN VARCHAR2,
    p_nombre IN VARCHAR2,
    p_descripcion IN VARCHAR2,
    p_peso IN NUMBER,
    p_orden IN NUMBER
  ) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count
      FROM RL_MR_FACTORES
     WHERE MRF_MODELO_ID = v_modelo_id
       AND MRF_CODIGO = p_codigo;

    IF v_count = 0 THEN
      INSERT INTO RL_MR_FACTORES (
        MRF_ID,
        MRF_MODELO_ID,
        MRF_CODIGO,
        MRF_NOMBRE,
        MRF_DESCRIPCION,
        MRF_PESO_INSTITUCIONAL,
        MRF_ORDEN,
        MRF_ESTADO_REGISTRO,
        MRF_USR_CREACION_ID,
        MRF_FECHA_CREACION
      )
      VALUES (
        SEQ_RL_MR_FACTORES.NEXTVAL,
        v_modelo_id,
        p_codigo,
        p_nombre,
        p_descripcion,
        p_peso,
        p_orden,
        1,
        1,
        SYSDATE
      );
    ELSE
      UPDATE RL_MR_FACTORES
         SET MRF_NOMBRE = p_nombre,
             MRF_DESCRIPCION = p_descripcion,
             MRF_PESO_INSTITUCIONAL = p_peso,
             MRF_ORDEN = p_orden,
             MRF_ESTADO_REGISTRO = 1,
             MRF_MOTIVO_INACTIVO = NULL,
             MRF_USR_MODIF_ID = 1,
             MRF_FECHA_MODIF = SYSDATE
       WHERE MRF_MODELO_ID = v_modelo_id
         AND MRF_CODIGO = p_codigo;
    END IF;
  END;

  PROCEDURE upsert_variable(
    p_factor_codigo IN VARCHAR2,
    p_codigo IN VARCHAR2,
    p_nombre IN VARCHAR2,
    p_descripcion IN VARCHAR2,
    p_peso IN NUMBER,
    p_orden IN NUMBER
  ) IS
    v_factor_id NUMBER;
    v_count     NUMBER;
  BEGIN
    SELECT MRF_ID INTO v_factor_id
      FROM RL_MR_FACTORES
     WHERE MRF_MODELO_ID = v_modelo_id
       AND MRF_CODIGO = p_factor_codigo;

    SELECT COUNT(*) INTO v_count
      FROM RL_MR_VARIABLES
     WHERE MRV_FACTOR_ID = v_factor_id
       AND MRV_CODIGO = p_codigo;

    IF v_count = 0 THEN
      INSERT INTO RL_MR_VARIABLES (
        MRV_ID,
        MRV_FACTOR_ID,
        MRV_CODIGO,
        MRV_NOMBRE,
        MRV_DESCRIPCION,
        MRV_PESO_INTERNO,
        MRV_TIPO_DATO,
        MRV_FUENTE_DATO,
        MRV_OBLIGATORIA,
        MRV_ORDEN,
        MRV_ESTADO_REGISTRO,
        MRV_USR_CREACION_ID,
        MRV_FECHA_CREACION
      )
      VALUES (
        SEQ_RL_MR_VARIABLES.NEXTVAL,
        v_factor_id,
        p_codigo,
        p_nombre,
        p_descripcion,
        p_peso,
        'NUMERICO',
        'CAPTURA',
        1,
        p_orden,
        1,
        1,
        SYSDATE
      );
    ELSE
      UPDATE RL_MR_VARIABLES
         SET MRV_NOMBRE = p_nombre,
             MRV_DESCRIPCION = p_descripcion,
             MRV_PESO_INTERNO = p_peso,
             MRV_TIPO_DATO = 'NUMERICO',
             MRV_FUENTE_DATO = 'CAPTURA',
             MRV_OBLIGATORIA = 1,
             MRV_ORDEN = p_orden,
             MRV_ESTADO_REGISTRO = 1,
             MRV_MOTIVO_INACTIVO = NULL,
             MRV_USR_MODIF_ID = 1,
             MRV_FECHA_MODIF = SYSDATE
       WHERE MRV_FACTOR_ID = v_factor_id
         AND MRV_CODIGO = p_codigo;
    END IF;
  END;

  PROCEDURE upsert_escala(
    p_tipo IN VARCHAR2,
    p_min IN NUMBER,
    p_max IN NUMBER,
    p_nivel IN VARCHAR2,
    p_color IN VARCHAR2,
    p_descripcion IN VARCHAR2,
    p_orden IN NUMBER
  ) IS
    v_count NUMBER;
  BEGIN
    SELECT COUNT(*) INTO v_count
      FROM RL_MR_ESCALAS
     WHERE MRE_MODELO_ID = v_modelo_id
       AND MRE_TIPO = p_tipo
       AND MRE_VALOR_MIN = p_min
       AND MRE_VALOR_MAX = p_max
       AND MRE_NIVEL = p_nivel;

    IF v_count = 0 THEN
      INSERT INTO RL_MR_ESCALAS (
        MRE_ID,
        MRE_MODELO_ID,
        MRE_TIPO,
        MRE_VALOR_MIN,
        MRE_VALOR_MAX,
        MRE_NIVEL,
        MRE_COLOR_HEX,
        MRE_DESCRIPCION,
        MRE_ORDEN,
        MRE_ESTADO_REGISTRO
      )
      VALUES (
        SEQ_RL_MR_ESCALAS.NEXTVAL,
        v_modelo_id,
        p_tipo,
        p_min,
        p_max,
        p_nivel,
        p_color,
        p_descripcion,
        p_orden,
        1
      );
    ELSE
      UPDATE RL_MR_ESCALAS
         SET MRE_COLOR_HEX = p_color,
             MRE_DESCRIPCION = p_descripcion,
             MRE_ORDEN = p_orden,
             MRE_ESTADO_REGISTRO = 1,
             MRE_MOTIVO_INACTIVO = NULL
       WHERE MRE_MODELO_ID = v_modelo_id
         AND MRE_TIPO = p_tipo
         AND MRE_VALOR_MIN = p_min
         AND MRE_VALOR_MAX = p_max
         AND MRE_NIVEL = p_nivel;
    END IF;
  END;

  PROCEDURE cargar_variables_factor(p_factor_codigo IN VARCHAR2) IS
  BEGIN
    upsert_variable(p_factor_codigo, 'V01', 'Perfil del sujeto evaluado', 'Condiciones generales del sujeto evaluado según factor institucional.', 15, 1);
    upsert_variable(p_factor_codigo, 'V02', 'Actividad, rubro o función', 'Actividad económica, rubro, función o naturaleza operativa relacionada con el factor.', 15, 2);
    upsert_variable(p_factor_codigo, 'V03', 'Ubicación geográfica', 'Exposición por ubicación, zona, municipio, país o jurisdicción aplicable.', 10, 3);
    upsert_variable(p_factor_codigo, 'V04', 'Antecedentes y coincidencias', 'Historial, coincidencias, alertas, sanciones, observaciones o eventos relevantes.', 20, 4);
    upsert_variable(p_factor_codigo, 'V05', 'Comportamiento transaccional u operativo', 'Comportamiento, volumen, recurrencia, variación o señales operativas relevantes.', 15, 5);
    upsert_variable(p_factor_codigo, 'V06', 'Canal, producto o relación institucional', 'Canal de vinculación, relación institucional, servicio, proceso o modalidad de interacción.', 10, 6);
    upsert_variable(p_factor_codigo, 'V07', 'Control interno y evidencia disponible', 'Nivel de documentación, soporte, trazabilidad y evidencia disponible para sustentar la evaluación.', 15, 7);
  END;
BEGIN
  SELECT COUNT(*) INTO v_total
    FROM RL_MR_MODELOS
   WHERE MRM_NOMBRE = 'Metodología base LA/FT IHSS'
     AND MRM_VERSION = '1.0';

  IF v_total = 0 THEN
    INSERT INTO RL_MR_MODELOS (
      MRM_ID,
      MRM_NOMBRE,
      MRM_VERSION,
      MRM_DESCRIPCION,
      MRM_ESTADO,
      MRM_FECHA_VIGENCIA,
      MRM_APROBADO_POR,
      MRM_FECHA_APROBACION,
      MRM_MOTIVO_ESTADO,
      MRM_USR_CREACION_ID,
      MRM_FECHA_CREACION,
      MRM_ESTADO_REGISTRO
    )
    VALUES (
      SEQ_RL_MR_MODELOS.NEXTVAL,
      'Metodología base LA/FT IHSS',
      '1.0',
      'Modelo inicial aprobado metodológicamente en Fase 2 para factores institucionales, variables internas, escalas base y rangos de riesgo.',
      'APROBADO',
      TRUNC(SYSDATE),
      1,
      SYSDATE,
      'Metodología base alineada con Fase 2 aprobada.',
      1,
      SYSDATE,
      1
    );
  END IF;

  SELECT MRM_ID INTO v_modelo_id
    FROM RL_MR_MODELOS
   WHERE MRM_NOMBRE = 'Metodología base LA/FT IHSS'
     AND MRM_VERSION = '1.0';

  UPDATE RL_MR_MODELOS
     SET MRM_ESTADO = 'APROBADO',
         MRM_FECHA_VIGENCIA = NVL(MRM_FECHA_VIGENCIA, TRUNC(SYSDATE)),
         MRM_APROBADO_POR = NVL(MRM_APROBADO_POR, 1),
         MRM_FECHA_APROBACION = NVL(MRM_FECHA_APROBACION, SYSDATE),
         MRM_MOTIVO_ESTADO = 'Metodología base alineada con Fase 2 aprobada.',
         MRM_ESTADO_REGISTRO = 1,
         MRM_USR_MODIF_ID = 1,
         MRM_FECHA_MODIF = SYSDATE
   WHERE MRM_ID = v_modelo_id;

  upsert_factor('PROVEEDORES', 'Proveedores', 'Factor institucional de proveedores. Peso fijo definido por requerimiento del cliente.', 50, 1);
  upsert_factor('CLIENTES_PATRONOS', 'Clientes/Patronos', 'Factor institucional de clientes o patronos. Peso fijo definido por requerimiento del cliente.', 25, 2);
  upsert_factor('EMPLEADOS', 'Empleados', 'Factor institucional de empleados. Peso fijo definido por requerimiento del cliente.', 25, 3);

  cargar_variables_factor('PROVEEDORES');
  cargar_variables_factor('CLIENTES_PATRONOS');
  cargar_variables_factor('EMPLEADOS');

  upsert_escala('VARIABLE', 1, 1, 'Muy bajo', '#00B050', 'Exposición mínima o condición favorable.', 1);
  upsert_escala('VARIABLE', 2, 2, 'Bajo', '#92D050', 'Exposición baja controlable.', 2);
  upsert_escala('VARIABLE', 3, 3, 'Medio', '#FFC000', 'Exposición media que requiere seguimiento.', 3);
  upsert_escala('VARIABLE', 4, 4, 'Alto', '#FF6600', 'Exposición alta que requiere control reforzado.', 4);
  upsert_escala('VARIABLE', 5, 5, 'Crítico', '#FF0000', 'Exposición crítica que requiere acción prioritaria.', 5);

  upsert_escala('INHERENTE', 1.00, 1.80, 'Muy bajo', '#00B050', 'Riesgo inherente muy bajo.', 1);
  upsert_escala('INHERENTE', 1.81, 2.60, 'Bajo', '#92D050', 'Riesgo inherente bajo.', 2);
  upsert_escala('INHERENTE', 2.61, 3.40, 'Medio', '#FFC000', 'Riesgo inherente medio.', 3);
  upsert_escala('INHERENTE', 3.41, 4.20, 'Alto', '#FF6600', 'Riesgo inherente alto.', 4);
  upsert_escala('INHERENTE', 4.21, 5.00, 'Crítico', '#FF0000', 'Riesgo inherente crítico.', 5);

  upsert_escala('RESIDUAL', 1.00, 1.80, 'Muy bajo', '#00B050', 'Riesgo residual muy bajo.', 1);
  upsert_escala('RESIDUAL', 1.81, 2.60, 'Bajo', '#92D050', 'Riesgo residual bajo.', 2);
  upsert_escala('RESIDUAL', 2.61, 3.40, 'Medio', '#FFC000', 'Riesgo residual medio que requiere seguimiento.', 3);
  upsert_escala('RESIDUAL', 3.41, 4.20, 'Alto', '#FF6600', 'Riesgo residual alto; requiere plan de acción.', 4);
  upsert_escala('RESIDUAL', 4.21, 5.00, 'Crítico', '#FF0000', 'Riesgo residual crítico; requiere plan prioritario.', 5);

  upsert_escala('CONTROL', 0, 0, 'Sin control', '#C00000', 'Sin mitigación reconocida para el cálculo residual.', 1);
  upsert_escala('CONTROL', 10, 10, 'Débil', '#FF0000', 'Mitigación del 10% por control con baja solidez o evidencia insuficiente.', 2);
  upsert_escala('CONTROL', 25, 25, 'Moderado', '#FFC000', 'Mitigación del 25% por control parcialmente efectivo.', 3);
  upsert_escala('CONTROL', 40, 40, 'Fuerte', '#92D050', 'Mitigación del 40% por control efectivo y documentado.', 4);
  upsert_escala('CONTROL', 55, 55, 'Muy fuerte', '#00B050', 'Mitigación máxima sugerida del 55% por control sólido, evidenciado y oportuno.', 5);

  SELECT SUM(MRF_PESO_INSTITUCIONAL) INTO v_total
    FROM RL_MR_FACTORES
   WHERE MRF_MODELO_ID = v_modelo_id
     AND MRF_ESTADO_REGISTRO = 1;

  IF v_total <> 100 THEN
    RAISE_APPLICATION_ERROR(-20301, 'Validación fallida: la ponderación institucional debe totalizar 100%.');
  END IF;

  SELECT COUNT(*) INTO v_total
    FROM RL_MR_FACTORES
   WHERE MRF_MODELO_ID = v_modelo_id
     AND MRF_CODIGO = 'PROVEEDORES'
     AND MRF_PESO_INSTITUCIONAL = 50;

  IF v_total = 0 THEN
    RAISE_APPLICATION_ERROR(-20302, 'Validación fallida: Proveedores debe tener peso 50%.');
  END IF;

  SELECT COUNT(*) INTO v_total
    FROM RL_MR_FACTORES
   WHERE MRF_MODELO_ID = v_modelo_id
     AND MRF_CODIGO = 'CLIENTES_PATRONOS'
     AND MRF_PESO_INSTITUCIONAL = 25;

  IF v_total = 0 THEN
    RAISE_APPLICATION_ERROR(-20303, 'Validación fallida: Clientes/Patronos debe tener peso 25%.');
  END IF;

  SELECT COUNT(*) INTO v_total
    FROM RL_MR_FACTORES
   WHERE MRF_MODELO_ID = v_modelo_id
     AND MRF_CODIGO = 'EMPLEADOS'
     AND MRF_PESO_INSTITUCIONAL = 25;

  IF v_total = 0 THEN
    RAISE_APPLICATION_ERROR(-20304, 'Validación fallida: Empleados debe tener peso 25%.');
  END IF;

  FOR factor_var IN (
    SELECT MRF.MRF_CODIGO, MRF.MRF_NOMBRE, NVL(SUM(MRV.MRV_PESO_INTERNO), 0) TOTAL
      FROM RL_MR_FACTORES MRF
      LEFT JOIN RL_MR_VARIABLES MRV
        ON MRV.MRV_FACTOR_ID = MRF.MRF_ID
       AND MRV.MRV_ESTADO_REGISTRO = 1
     WHERE MRF.MRF_MODELO_ID = v_modelo_id
       AND MRF.MRF_ESTADO_REGISTRO = 1
     GROUP BY MRF.MRF_CODIGO, MRF.MRF_NOMBRE
  ) LOOP
    IF factor_var.TOTAL <> 100 THEN
      RAISE_APPLICATION_ERROR(-20305, 'Validación fallida: las variables internas del factor ' || factor_var.MRF_NOMBRE || ' deben totalizar 100%.');
    END IF;
  END LOOP;
END;
/

COMMIT;

SELECT MRM_ID, MRM_NOMBRE, MRM_VERSION, MRM_ESTADO
  FROM RL_MR_MODELOS
 WHERE MRM_NOMBRE = 'Metodología base LA/FT IHSS'
   AND MRM_VERSION = '1.0';

SELECT MRF_CODIGO, MRF_NOMBRE, MRF_PESO_INSTITUCIONAL
  FROM RL_MR_FACTORES
 WHERE MRF_MODELO_ID = (
       SELECT MRM_ID
         FROM RL_MR_MODELOS
        WHERE MRM_NOMBRE = 'Metodología base LA/FT IHSS'
          AND MRM_VERSION = '1.0'
 )
 ORDER BY MRF_ORDEN;

SELECT MRF.MRF_CODIGO, MRF.MRF_NOMBRE, SUM(MRV.MRV_PESO_INTERNO) PESO_INTERNO_TOTAL
  FROM RL_MR_FACTORES MRF
  JOIN RL_MR_VARIABLES MRV
    ON MRV.MRV_FACTOR_ID = MRF.MRF_ID
 WHERE MRF.MRF_MODELO_ID = (
       SELECT MRM_ID
         FROM RL_MR_MODELOS
        WHERE MRM_NOMBRE = 'Metodología base LA/FT IHSS'
          AND MRM_VERSION = '1.0'
 )
   AND MRV.MRV_ESTADO_REGISTRO = 1
 GROUP BY MRF.MRF_CODIGO, MRF.MRF_NOMBRE
 ORDER BY MRF.MRF_CODIGO;

SELECT MRE_TIPO, MRE_VALOR_MIN, MRE_VALOR_MAX, MRE_NIVEL
  FROM RL_MR_ESCALAS
 WHERE MRE_MODELO_ID = (
       SELECT MRM_ID
         FROM RL_MR_MODELOS
        WHERE MRM_NOMBRE = 'Metodología base LA/FT IHSS'
          AND MRM_VERSION = '1.0'
 )
 ORDER BY MRE_TIPO, MRE_ORDEN;
