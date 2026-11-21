using System;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace RL.API.Tests.Support;

/// <summary>
/// Bootstrap utilitario seguro para inicializar y certificar el esquema
/// RIESGO_LAVADO en un Oracle XE descartable de pruebas / CI.
/// Únicamente opera si el host es 127.0.0.1 o localhost y el servicio es XE.
/// </summary>
public static class MatricesOracleBootstrap
{
    public static async Task EnsureSchemaAsync(OracleConnection connection)
    {
        ValidarDestinoDisposableXe(connection.ConnectionString);

        // 1. Asegurar tabla RL_ROLES y rol semilla
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_ROLES (
                ROL_ID NUMBER(3) PRIMARY KEY,
                ROL_NOMBRE VARCHAR2(50) NOT NULL UNIQUE,
                ROL_DESCRIPCION VARCHAR2(200),
                ROL_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                ROL_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                CONSTRAINT CK_RL_ROL_ACTIVO CHECK (ROL_ACTIVO IN (0,1))
            )");

        int rolesCount = Convert.ToInt32(await ScalarAsync(connection, "SELECT COUNT(*) FROM RL_ROLES WHERE ROL_ID = 1"));
        if (rolesCount == 0)
        {
            await ExecuteIgnoreAsync(connection, @"
                INSERT INTO RL_ROLES (ROL_ID, ROL_NOMBRE, ROL_DESCRIPCION, ROL_ACTIVO, ROL_FECHA_CREACION)
                VALUES (1, 'ADMINISTRADOR', 'Rol de Administración', 1, SYSDATE)");
        }

        // 2. Asegurar tabla RL_USUARIOS y usuarios semilla
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_USUARIOS (
                USR_ID NUMBER(10) PRIMARY KEY,
                USR_NOMBRE VARCHAR2(100) DEFAULT 'USUARIO',
                USR_APELLIDO VARCHAR2(100) DEFAULT 'INTEGRACION',
                USR_EMAIL VARCHAR2(150),
                USR_PASSWORD_HASH VARCHAR2(256) DEFAULT 'DUMMY_HASH',
                USR_PASSWORD_SALT VARCHAR2(256) DEFAULT 'DUMMY_SALT',
                USR_ROL_ID NUMBER(3) DEFAULT 1,
                USR_EMPLEADO_ID VARCHAR2(20),
                USR_DNI VARCHAR2(25),
                ES_USUARIO_DOMINIO NUMBER(1) DEFAULT 0,
                USUARIO_DOMINIO VARCHAR2(500),
                USR_DOM_ID NUMBER(5),
                USR_DEBE_CAMBIAR_PASS NUMBER(1) DEFAULT 0,
                USR_FECHA_CLAVE_TEMP DATE,
                USR_ACTIVO NUMBER(1) DEFAULT 1,
                USR_FECHA_CREACION DATE DEFAULT SYSDATE
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_USUARIOS MODIFY (USR_NOMBRE DEFAULT 'USUARIO' NULL, USR_APELLIDO DEFAULT 'INTEGRACION' NULL, USR_EMAIL NULL, USR_PASSWORD_HASH DEFAULT 'DUMMY_HASH' NULL, USR_PASSWORD_SALT DEFAULT 'DUMMY_SALT' NULL, USR_ROL_ID DEFAULT 1 NULL)");

        await ExecuteIgnoreAsync(connection, @"
            BEGIN
                INSERT INTO RL_USUARIOS (
                    USR_ID, USR_NOMBRE, USR_APELLIDO, USR_EMAIL,
                    USR_PASSWORD_HASH, USR_PASSWORD_SALT, USR_ROL_ID,
                    USR_ACTIVO, USR_FECHA_CREACION
                ) VALUES (
                    1, 'ADMIN', 'INTEGRACION', 'admin@riesgolavado.local',
                    'DUMMY_HASH', 'DUMMY_SALT', 1, 1, SYSDATE
                );
            EXCEPTION
                WHEN DUP_VAL_ON_INDEX THEN NULL;
            END;
        ");

        await ExecuteIgnoreAsync(connection, @"
            BEGIN
                INSERT INTO RL_USUARIOS (
                    USR_ID, USR_NOMBRE, USR_APELLIDO, USR_EMAIL,
                    USR_PASSWORD_HASH, USR_PASSWORD_SALT, USR_ROL_ID,
                    USR_ACTIVO, USR_FECHA_CREACION
                ) VALUES (
                    987654, 'BLOCK7', 'INTEGRACION', 'block7@riesgolavado.local',
                    'DUMMY_HASH', 'DUMMY_SALT', 1, 1, SYSDATE
                );
            EXCEPTION
                WHEN DUP_VAL_ON_INDEX THEN NULL;
            END;
        ");

        // 3. Asegurar RL_USUARIO_CAPACIDADES
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_USUARIO_CAPACIDADES (
                UCP_USR_ID NUMBER(10) NOT NULL,
                UCP_CAPACIDAD VARCHAR2(80 CHAR) NOT NULL,
                UCP_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                UCP_ASIGNADO_POR NUMBER(10),
                UCP_FECHA_ASIGNACION DATE DEFAULT SYSDATE NOT NULL,
                CONSTRAINT PK_RL_USUARIO_CAP PRIMARY KEY (UCP_USR_ID, UCP_CAPACIDAD),
                CONSTRAINT CK_RL_UCP_CAP CHECK (UCP_CAPACIDAD IN ('MATRICES_RIESGO_OBSERVACIONES_AREA_EDITAR','MATRICES_RIESGO_OBSERVACIONES_UGR_EDITAR')),
                CONSTRAINT CK_RL_UCP_ACTIVO CHECK (UCP_ACTIVO IN (0,1))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_USUARIO_CAPACIDADES DROP CONSTRAINT FK_RL_UCP_USR");
        await ExecuteIgnoreAsync(connection, "CREATE INDEX IDX_RL_UCP_CAP ON RL_USUARIO_CAPACIDADES(UCP_CAPACIDAD, UCP_ACTIVO)");

        // 4. Asegurar RL_AUDITORIA y SEQ_RL_AUDITORIA
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_AUDITORIA (
                AUD_ID NUMBER(15) PRIMARY KEY,
                AUD_TABLA VARCHAR2(128) NOT NULL,
                AUD_REGISTRO_ID VARCHAR2(100) NOT NULL,
                AUD_ACCION VARCHAR2(80) NOT NULL,
                AUD_DATOS_ANT CLOB,
                AUD_DATOS_NVO CLOB,
                AUD_USR_ID NUMBER(10),
                AUD_USR_EMAIL VARCHAR2(255),
                AUD_IP VARCHAR2(64),
                AUD_FECHA DATE DEFAULT SYSDATE NOT NULL,
                AUD_MODULO VARCHAR2(80)
            )");

        await ExecuteIgnoreAsync(connection, "CREATE SEQUENCE SEQ_RL_AUDITORIA START WITH 1 INCREMENT BY 1 NOCACHE");

        // 3. Crear secuencias del Modelo 17
        string[] secuencias17 =
        {
            "SEQ_RL_MR_FAMILIAS",
            "SEQ_RL_MR_VERSIONES",
            "SEQ_RL_MR_CATALOGOS",
            "SEQ_RL_MR_ELEMENTOS",
            "SEQ_RL_MR_REGLAS",
            "SEQ_RL_MR_RIESGOS",
            "SEQ_RL_MR_EVALUACIONES",
            "SEQ_RL_MR_PROYECCIONES",
            "SEQ_RL_MR_FLUJOS",
            "SEQ_RL_MR_CONTROLES",
            "SEQ_RL_MR_EVAL_CONTROLES",
            "SEQ_RL_MR_PLANES",
            "SEQ_RL_MR_ACTIVIDADES",
            "SEQ_RL_MR_EVIDENCIAS",
            "SEQ_RL_MR_EVI_VINCULOS",
            "SEQ_RL_MR_SENALES",
            "SEQ_RL_MR_AUTOMONITOREO"
        };
        foreach (string seq in secuencias17)
        {
            await ExecuteIgnoreAsync(connection, $"CREATE SEQUENCE {seq} START WITH 100 INCREMENT BY 1 NOCACHE");
        }

        // 4. Crear tablas del Modelo 17
        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_FAMILIAS_FORMULARIO (
                FAM_ID NUMBER(15) NOT NULL,
                FAM_CODIGO VARCHAR2(50) NOT NULL,
                FAM_NOMBRE VARCHAR2(150) NOT NULL,
                FAM_DESCRIPCION VARCHAR2(500),
                FAM_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                FAM_ARCHIVADA NUMBER(1) DEFAULT 0 NOT NULL,
                FAM_PREDETERMINADA NUMBER(1) DEFAULT 0 NOT NULL,
                FAM_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                FAM_FECHA_ARCHIVADO DATE,
                FAM_USR_ARCHIVADO NUMBER(10),
                FAM_MOTIVO_ARCHIVADO VARCHAR2(500),
                CONSTRAINT PK_RL_MR_FAMILIAS PRIMARY KEY (FAM_ID),
                CONSTRAINT UQ_RL_MR_FAM_COD UNIQUE (FAM_CODIGO),
                CONSTRAINT CK_RL_MR_FAM_ACT CHECK (FAM_ACTIVO IN (0,1)),
                CONSTRAINT CK_RL_MR_FAM_PREDET CHECK (FAM_PREDETERMINADA IN (0,1))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FAMILIAS_FORMULARIO ADD (FAM_PREDETERMINADA NUMBER(1) DEFAULT 0 NOT NULL)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FAMILIAS_FORMULARIO ADD CONSTRAINT CK_RL_MR_FAM_PREDET CHECK (FAM_PREDETERMINADA IN (0,1))");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_VERSIONES_FORMULARIO (
                VER_ID NUMBER(15) NOT NULL,
                VER_FAMILIA_ID NUMBER(15) NOT NULL,
                VER_CODIGO VARCHAR2(30) NOT NULL,
                VER_VERSION NUMBER(5) NOT NULL,
                VER_JSON CLOB NOT NULL,
                VER_HASH VARCHAR2(64) NOT NULL,
                VER_ESTADO VARCHAR2(20) DEFAULT 'DRAFT' NOT NULL,
                VER_VIGENTE NUMBER(1) DEFAULT 0 NOT NULL,
                VER_FECHA_INICIO DATE,
                VER_FECHA_FIN DATE,
                VER_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                VER_USR_CREACION NUMBER(10) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_VERSIONES PRIMARY KEY (VER_ID),
                CONSTRAINT FK_RL_MR_VER_FAM FOREIGN KEY (VER_FAMILIA_ID) REFERENCES RL_MR_FAMILIAS_FORMULARIO(FAM_ID),
                CONSTRAINT CK_RL_MR_VER_EST CHECK (VER_ESTADO IN ('DRAFT','IN_REVIEW','APPROVED','PUBLISHED','RETIRED','ARCHIVED')),
                CONSTRAINT CK_RL_MR_VER_VIG CHECK (VER_VIGENTE IN (0,1))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_VERSIONES_FORMULARIO MODIFY (VER_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_VERSIONES_FORMULARIO DROP CONSTRAINT UQ_RL_MR_VER_FAM_VER");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_VERSIONES_FORMULARIO DROP CONSTRAINT FK_RL_MR_VER_USR");

        await ExecuteIgnoreAsync(connection, @"
            BEGIN
                INSERT INTO RL_MR_FAMILIAS_FORMULARIO (
                    FAM_ID, FAM_CODIGO, FAM_NOMBRE, FAM_DESCRIPCION, FAM_ACTIVO, FAM_PREDETERMINADA
                ) VALUES (
                    1, 'FAM_SEMILLA', 'Familia Semilla', 'Familia semilla para fixtures Oracle XE', 1, 1
                );
            EXCEPTION
                WHEN DUP_VAL_ON_INDEX THEN NULL;
            END;");

        await ExecuteIgnoreAsync(connection, @"
            BEGIN
                INSERT INTO RL_MR_VERSIONES_FORMULARIO (
                    VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, VER_HASH,
                    VER_ESTADO, VER_VIGENTE, VER_FECHA_INICIO, VER_FECHA_CREACION, VER_USR_CREACION
                ) VALUES (
                    1, 1, 'VER_SEMILLA', 1, '{""sections"":[]}', '0000000000000000000000000000000000000000000000000000000000000000',
                    'PUBLISHED', 1, SYSDATE, SYSDATE, 1
                );
            EXCEPTION
                WHEN DUP_VAL_ON_INDEX THEN
                    UPDATE RL_MR_VERSIONES_FORMULARIO
                       SET VER_JSON = '{""sections"":[]}'
                     WHERE VER_ID = 1
                       AND VER_FAMILIA_ID = 1
                       AND VER_CODIGO = 'VER_SEMILLA';
            END;");

        string seedJson = Convert.ToString(await ScalarAsync(
            connection,
            "SELECT VER_JSON FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID = 1 AND VER_FAMILIA_ID = 1 AND VER_CODIGO = 'VER_SEMILLA'")) ?? string.Empty;
        try
        {
            using JsonDocument seedDocument = JsonDocument.Parse(seedJson);
            if (seedDocument.RootElement.ValueKind != JsonValueKind.Object
                || !seedDocument.RootElement.TryGetProperty("sections", out _))
            {
                throw new InvalidOperationException("La semilla Oracle XE VER_ID=1 no cumple el contrato JSON esperado.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("La semilla Oracle XE VER_ID=1 contiene JSON inválido.", ex);
        }

        // Avanzar secuencias para que los tests que llaman a NEXTVAL no colisionen con los IDs semilla (1)
        await ExecuteIgnoreAsync(connection, @"
            DECLARE
                v_num NUMBER;
            BEGIN
                FOR i IN 1..100 LOOP
                    BEGIN
                        SELECT SEQ_RL_MR_FAMILIAS.NEXTVAL INTO v_num FROM DUAL;
                        EXIT WHEN v_num >= 100;
                    EXCEPTION WHEN OTHERS THEN EXIT;
                    END;
                END LOOP;
                FOR i IN 1..100 LOOP
                    BEGIN
                        SELECT SEQ_RL_MR_VERSIONES.NEXTVAL INTO v_num FROM DUAL;
                        EXIT WHEN v_num >= 100;
                    EXCEPTION WHEN OTHERS THEN EXIT;
                    END;
                END LOOP;
            END;");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_CATALOGOS (
                CAT_ID NUMBER(15) NOT NULL,
                CAT_CODIGO VARCHAR2(50) NOT NULL,
                CAT_NOMBRE VARCHAR2(150) NOT NULL,
                CAT_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_CATALOGOS PRIMARY KEY (CAT_ID),
                CONSTRAINT UQ_RL_MR_CAT_COD UNIQUE (CAT_CODIGO),
                CONSTRAINT CK_RL_MR_CAT_ACT CHECK (CAT_ACTIVO IN (0,1))
            )");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_ELEMENTOS_CATALOGO (
                ELE_ID NUMBER(15) NOT NULL,
                ELE_CATALOGO_ID NUMBER(15) NOT NULL,
                ELE_CODIGO VARCHAR2(50) NOT NULL,
                ELE_VALOR VARCHAR2(255) NOT NULL,
                ELE_ORDEN NUMBER(5) DEFAULT 0 NOT NULL,
                ELE_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_ELEMENTOS PRIMARY KEY (ELE_ID),
                CONSTRAINT FK_RL_MR_ELE_CAT FOREIGN KEY (ELE_CATALOGO_ID) REFERENCES RL_MR_CATALOGOS(CAT_ID),
                CONSTRAINT UQ_RL_MR_ELE_COD UNIQUE (ELE_CATALOGO_ID, ELE_CODIGO),
                CONSTRAINT CK_RL_MR_ELE_ACT CHECK (ELE_ACTIVO IN (0,1))
            )");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_REGLAS_CALCULO (
                REG_ID NUMBER(15) NOT NULL,
                REG_CODIGO VARCHAR2(50) NOT NULL,
                REG_VERSION VARCHAR2(20) NOT NULL,
                REG_NOMBRE VARCHAR2(150) NOT NULL,
                REG_ALGORITMO_ID VARCHAR2(100) NOT NULL,
                REG_ACTIVA NUMBER(1) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_REGLAS PRIMARY KEY (REG_ID),
                CONSTRAINT UQ_RL_MR_REG_COD UNIQUE (REG_CODIGO, REG_VERSION),
                CONSTRAINT CK_RL_MR_REG_ACT CHECK (REG_ACTIVA IN (0,1))
            )");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_RIESGOS (
                RIE_ID NUMBER(15) NOT NULL,
                RIE_CODIGO VARCHAR2(30) NOT NULL,
                RIE_NOMBRE VARCHAR2(250) NOT NULL,
                RIE_DESCRIPCION VARCHAR2(2000),
                RIE_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                RIE_USR_CREACION NUMBER(10) DEFAULT 1 NOT NULL,
                RIE_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                CONSTRAINT PK_RL_MR_RIESGOS PRIMARY KEY (RIE_ID),
                CONSTRAINT CK_RL_MR_RIE_ACT CHECK (RIE_ACTIVO IN (0,1))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_RIESGOS MODIFY (RIE_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_RIESGOS DROP CONSTRAINT FK_RL_MR_RIE_USR");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_RIESGOS DROP CONSTRAINT UQ_RL_MR_RIE_COD");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_EVALUACIONES_RIESGO (
                EVA_ID NUMBER(15) NOT NULL,
                EVA_RIESGO_ID NUMBER(15) NOT NULL,
                EVA_VERSION_ID NUMBER(15) NOT NULL,
                EVA_DATOS_JSON CLOB NOT NULL,
                EVA_CALCULOS_JSON CLOB NOT NULL,
                EVA_FECHA_REGISTRO DATE DEFAULT SYSDATE NOT NULL,
                EVA_USR_REGISTRO NUMBER(10) DEFAULT 1 NOT NULL,
                EVA_VERSION_ROW NUMBER(10) DEFAULT 1 NOT NULL,
                EVA_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_EVALUACIONES PRIMARY KEY (EVA_ID),
                CONSTRAINT FK_RL_MR_EVA_RIE FOREIGN KEY (EVA_RIESGO_ID) REFERENCES RL_MR_RIESGOS(RIE_ID),
                CONSTRAINT FK_RL_MR_EVA_VER FOREIGN KEY (EVA_VERSION_ID) REFERENCES RL_MR_VERSIONES_FORMULARIO(VER_ID),
                CONSTRAINT CK_RL_MR_EVA_ACT CHECK (EVA_ACTIVO IN (0,1))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_EVALUACIONES_RIESGO MODIFY (EVA_USR_REGISTRO DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_EVALUACIONES_RIESGO DROP CONSTRAINT FK_RL_MR_EVA_USR");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_PROYECCIONES_EVALUACION (
                PROY_ID NUMBER(15) NOT NULL,
                PROY_EVALUACION_ID NUMBER(15) NOT NULL,
                PROY_CODIGO_RIESGO VARCHAR2(30) NOT NULL,
                PROY_AREA_PRINCIPAL VARCHAR2(100),
                PROY_VRI NUMBER(8,2) DEFAULT 0,
                PROY_VRR NUMBER(8,2) DEFAULT 0,
                PROY_NIVEL_INHERENTE VARCHAR2(20),
                PROY_NIVEL_RESIDUAL VARCHAR2(20),
                PROY_RESPUESTA_RIESGO VARCHAR2(50),
                PROY_ESTADO_EVALUACION VARCHAR2(30),
                PROY_DUENO_RIESGO VARCHAR2(150),
                PROY_FECHA_EVAL DATE DEFAULT SYSDATE,
                CONSTRAINT PK_RL_MR_PROYECCIONES PRIMARY KEY (PROY_ID),
                CONSTRAINT FK_RL_MR_PROY_EVA FOREIGN KEY (PROY_EVALUACION_ID) REFERENCES RL_MR_EVALUACIONES_RIESGO(EVA_ID),
                CONSTRAINT UQ_RL_MR_PROY_EVA UNIQUE (PROY_EVALUACION_ID)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PROYECCIONES_EVALUACION MODIFY (PROY_AREA_PRINCIPAL NULL, PROY_VRI DEFAULT 0, PROY_VRR DEFAULT 0, PROY_NIVEL_INHERENTE NULL, PROY_NIVEL_RESIDUAL NULL, PROY_RESPUESTA_RIESGO NULL, PROY_ESTADO_EVALUACION NULL, PROY_DUENO_RIESGO NULL, PROY_FECHA_EVAL DEFAULT SYSDATE)");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_FLUJOS_EVALUACION (
                FLU_ID NUMBER(15) NOT NULL,
                FLU_EVALUACION_ID NUMBER(15) NOT NULL,
                FLU_ESTADO VARCHAR2(30) NOT NULL,
                FLU_MOTIVO VARCHAR2(1000),
                FLU_USR_ID NUMBER(10) DEFAULT 1 NOT NULL,
                FLU_FECHA DATE DEFAULT SYSDATE NOT NULL,
                CONSTRAINT PK_RL_MR_FLUJOS PRIMARY KEY (FLU_ID),
                CONSTRAINT FK_RL_MR_FLU_EVA FOREIGN KEY (FLU_EVALUACION_ID) REFERENCES RL_MR_EVALUACIONES_RIESGO(EVA_ID),
                CONSTRAINT CK_RL_MR_FLU_EST CHECK (FLU_ESTADO IN ('BORRADOR','EN_REVISION','OBSERVADA','APROBADA','RECHAZADA','CERRADA','DRAFT'))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FLUJOS_EVALUACION MODIFY (FLU_USR_ID DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FLUJOS_EVALUACION DROP CONSTRAINT FK_RL_MR_FLU_USR");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FLUJOS_EVALUACION DROP CONSTRAINT CK_RL_MR_FLU_EST");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FLUJOS_EVALUACION ADD CONSTRAINT CK_RL_MR_FLU_EST CHECK (FLU_ESTADO IN ('BORRADOR','EN_REVISION','OBSERVADA','APROBADA','RECHAZADA','CERRADA','DRAFT'))");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_CONTROLES_RIESGO (
                CON_ID NUMBER(15) NOT NULL,
                CON_EVALUACION_ID NUMBER(15) NOT NULL,
                CON_TIPO VARCHAR2(30) NOT NULL,
                CON_DESCRIPCION VARCHAR2(500) NOT NULL,
                CON_AUTOMATIZACION VARCHAR2(30) DEFAULT 'MANUAL' NOT NULL,
                CON_ESTADO VARCHAR2(20) NOT NULL,
                CON_ESTADO_MONITOREO VARCHAR2(30 CHAR),
                CON_EFECTIVIDAD_MONITOREO NUMBER(5,2),
                CONSTRAINT PK_RL_MR_CONTROLES PRIMARY KEY (CON_ID),
                CONSTRAINT FK_RL_MR_CON_EVA FOREIGN KEY (CON_EVALUACION_ID) REFERENCES RL_MR_EVALUACIONES_RIESGO(EVA_ID),
                CONSTRAINT CK_RL_MR_CON_TIPO CHECK (CON_TIPO IN ('PREVENTIVO','DETECTIVO','CORRECTIVO')),
                CONSTRAINT CK_RL_MR_CON_AUTO CHECK (CON_AUTOMATIZACION IN ('MANUAL','SEMIAUTOMATICO','AUTOMATICO')),
                CONSTRAINT CK_RL_MR_CON_MON_EF CHECK (CON_EFECTIVIDAD_MONITOREO BETWEEN 0 AND 100)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_CONTROLES_RIESGO MODIFY (CON_AUTOMATIZACION DEFAULT 'MANUAL')");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_EVALUACIONES_CONTROL (
                ECO_ID NUMBER(15) NOT NULL,
                ECO_CONTROL_ID NUMBER(15) NOT NULL,
                ECO_EFECTIVIDAD NUMBER(5,2) NOT NULL,
                ECO_COMENTARIO VARCHAR2(500),
                CONSTRAINT PK_RL_MR_EVAL_CONTROLES PRIMARY KEY (ECO_ID),
                CONSTRAINT FK_RL_MR_ECO_CON FOREIGN KEY (ECO_CONTROL_ID) REFERENCES RL_MR_CONTROLES_RIESGO(CON_ID),
                CONSTRAINT CK_RL_MR_ECO_EF CHECK (ECO_EFECTIVIDAD BETWEEN 0 AND 100)
            )");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_PLANES (
                PLA_ID NUMBER(15) NOT NULL,
                PLA_EVALUACION_ID NUMBER(15) NOT NULL,
                PLA_DESCRIPCION VARCHAR2(500) NOT NULL,
                PLA_AVANCE NUMBER(5,2) DEFAULT 0 NOT NULL,
                PLA_PRESUPUESTO NUMBER(15,2) DEFAULT 0 NOT NULL,
                PLA_FECHA_INICIO DATE DEFAULT SYSDATE NOT NULL,
                PLA_FECHA_FIN DATE DEFAULT SYSDATE + 30 NOT NULL,
                PLA_ESTADO VARCHAR2(30) NOT NULL,
                PLA_MONITOREO_SEGUIMIENTO VARCHAR2(1000 CHAR),
                PLA_RESPONSABLES VARCHAR2(1000 CHAR),
                PLA_RECURSOS VARCHAR2(1000 CHAR),
                CONSTRAINT PK_RL_MR_PLANES PRIMARY KEY (PLA_ID),
                CONSTRAINT FK_RL_MR_PLA_EVA FOREIGN KEY (PLA_EVALUACION_ID) REFERENCES RL_MR_EVALUACIONES_RIESGO(EVA_ID)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PLANES MODIFY (PLA_FECHA_INICIO DEFAULT SYSDATE, PLA_FECHA_FIN DEFAULT SYSDATE + 30)");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_ACTIVIDADES (
                ACT_ID NUMBER(15) NOT NULL,
                ACT_PLAN_ID NUMBER(15) NOT NULL,
                ACT_DESCRIPCION VARCHAR2(500) NOT NULL,
                ACT_RESPONSABLE VARCHAR2(150) DEFAULT 'ADMIN' NOT NULL,
                ACT_AVANCE NUMBER(5,2) DEFAULT 0 NOT NULL,
                ACT_FECHA_INICIO DATE DEFAULT SYSDATE NOT NULL,
                ACT_FECHA_FIN DATE DEFAULT SYSDATE + 30 NOT NULL,
                ACT_ESTADO VARCHAR2(30) NOT NULL,
                CONSTRAINT PK_RL_MR_ACTIVIDADES PRIMARY KEY (ACT_ID),
                CONSTRAINT FK_RL_MR_ACT_PLAN FOREIGN KEY (ACT_PLAN_ID) REFERENCES RL_MR_PLANES(PLA_ID)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_ACTIVIDADES MODIFY (ACT_RESPONSABLE DEFAULT 'ADMIN', ACT_FECHA_INICIO DEFAULT SYSDATE, ACT_FECHA_FIN DEFAULT SYSDATE + 30)");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_EVIDENCIAS (
                EVI_ID NUMBER(15) NOT NULL,
                EVI_NOMBRE_ARCHIVO VARCHAR2(255) NOT NULL,
                EVI_EXTENSION VARCHAR2(10) NOT NULL,
                EVI_TAMANO NUMBER(15) NOT NULL,
                EVI_HASH VARCHAR2(64) NOT NULL,
                EVI_RUTA VARCHAR2(500) NOT NULL,
                EVI_USR_CREACION NUMBER(10) DEFAULT 1 NOT NULL,
                EVI_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                CONSTRAINT PK_RL_MR_EVIDENCIAS PRIMARY KEY (EVI_ID)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_EVIDENCIAS MODIFY (EVI_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_EVIDENCIAS DROP CONSTRAINT FK_RL_MR_EVI_USR");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_EVIDENCIAS_VINCULOS (
                EVV_ID NUMBER(15) NOT NULL,
                EVV_EVIDENCIA_ID NUMBER(15) NOT NULL,
                EVV_TIPO_ENTIDAD VARCHAR2(30) NOT NULL,
                EVV_ENTIDAD_ID NUMBER(15) NOT NULL,
                EVV_USR_CREACION NUMBER(10) DEFAULT 1 NOT NULL,
                EVV_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                CONSTRAINT PK_RL_MR_EVI_VINCULOS PRIMARY KEY (EVV_ID),
                CONSTRAINT FK_RL_MR_EVV_EVI FOREIGN KEY (EVV_EVIDENCIA_ID) REFERENCES RL_MR_EVIDENCIAS(EVI_ID),
                CONSTRAINT UQ_RL_MR_EVV_UNICO UNIQUE (EVV_EVIDENCIA_ID, EVV_TIPO_ENTIDAD, EVV_ENTIDAD_ID),
                CONSTRAINT CK_RL_MR_EVV_TIPO CHECK (EVV_TIPO_ENTIDAD IN ('RIESGO','EVALUACION','CONTROL','PLAN','ACTIVIDAD','ALERTA','AUTOMONITOREO'))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_EVIDENCIAS_VINCULOS MODIFY (EVV_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_EVIDENCIAS_VINCULOS DROP CONSTRAINT FK_RL_MR_EVV_USR");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_SENALES_ALERTA (
                ALE_ID NUMBER(15) NOT NULL,
                ALE_EVALUACION_ID NUMBER(15) NOT NULL,
                ALE_CODIGO VARCHAR2(50) NOT NULL,
                ALE_INDICADOR VARCHAR2(150) DEFAULT 'INDICADOR' NOT NULL,
                ALE_ESTADO VARCHAR2(30) DEFAULT 'INACTIVO' NOT NULL,
                ALE_FECHA_DISPARO DATE,
                CONSTRAINT PK_RL_MR_SENALES PRIMARY KEY (ALE_ID),
                CONSTRAINT FK_RL_MR_ALE_EVA FOREIGN KEY (ALE_EVALUACION_ID) REFERENCES RL_MR_EVALUACIONES_RIESGO(EVA_ID),
                CONSTRAINT CK_RL_MR_ALE_EST CHECK (ALE_ESTADO IN ('ACTIVO','INACTIVO','ACTIVA','INACTIVA'))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_SENALES_ALERTA MODIFY (ALE_INDICADOR DEFAULT 'INDICADOR')");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_SENALES_ALERTA DROP CONSTRAINT CK_RL_MR_ALE_EST");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_SENALES_ALERTA ADD CONSTRAINT CK_RL_MR_ALE_EST CHECK (ALE_ESTADO IN ('ACTIVO','INACTIVO','ACTIVA','INACTIVA'))");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_AUTOMONITOREO (
                MON_ID NUMBER(15) NOT NULL,
                MON_EVALUACION_ID NUMBER(15) NOT NULL,
                MON_ESTADO_RIESGO VARCHAR2(30) NOT NULL,
                MON_ESTADO_CONTR VARCHAR2(30) NOT NULL,
                MON_RESULTADO VARCHAR2(1000) DEFAULT 'RESULTADO' NOT NULL,
                MON_USR_ID NUMBER(10) DEFAULT 1 NOT NULL,
                MON_OBSERVACIONES_AREA VARCHAR2(2000),
                MON_OBSERVACIONES_UGR VARCHAR2(2000),
                MON_FECHA DATE DEFAULT SYSDATE NOT NULL,
                CONSTRAINT PK_RL_MR_AUTOMONITOREO PRIMARY KEY (MON_ID),
                CONSTRAINT FK_RL_MR_MON_EVA FOREIGN KEY (MON_EVALUACION_ID) REFERENCES RL_MR_EVALUACIONES_RIESGO(EVA_ID)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_AUTOMONITOREO MODIFY (MON_RESULTADO DEFAULT 'RESULTADO', MON_USR_ID DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_AUTOMONITOREO DROP CONSTRAINT FK_RL_MR_MON_USR");

        // Asegurar columnas de Bloques 4 y 6 en caso de tabla preexistente
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_CONTROLES_RIESGO ADD (CON_ESTADO_MONITOREO VARCHAR2(30 CHAR))");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_CONTROLES_RIESGO ADD (CON_EFECTIVIDAD_MONITOREO NUMBER(5,2))");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PLANES ADD (PLA_MONITOREO_SEGUIMIENTO VARCHAR2(1000 CHAR))");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PLANES ADD (PLA_RESPONSABLES VARCHAR2(1000 CHAR))");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PLANES ADD (PLA_RECURSOS VARCHAR2(1000 CHAR))");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_AUTOMONITOREO ADD (MON_OBSERVACIONES_AREA VARCHAR2(2000 CHAR))");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_AUTOMONITOREO ADD (MON_OBSERVACIONES_UGR VARCHAR2(2000 CHAR))");

        // 5. Crear tablas y secuencias de Configuración de Cálculo (8 tablas y 8 secuencias)
        string[] secuenciasCalculo =
        {
            "SEQ_RL_MR_FORMULAS",
            "SEQ_RL_MR_FORMULA_USOS",
            "SEQ_RL_MR_FORMULA_VERSIONES",
            "SEQ_RL_MR_FUNCION_ARGUMENTOS",
            "SEQ_RL_MR_FUNCIONES",
            "SEQ_RL_MR_FUNCION_VERSIONES",
            "SEQ_RL_MR_PARAMETROS",
            "SEQ_RL_MR_PARAMETRO_VERSIONES"
        };
        foreach (string seq in secuenciasCalculo)
        {
            await ExecuteIgnoreAsync(connection, $"CREATE SEQUENCE {seq} START WITH 1 INCREMENT BY 1 NOCACHE");
        }

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_FORMULAS (
                FOR_ID NUMBER(15) NOT NULL,
                FOR_CODIGO VARCHAR2(80 CHAR) NOT NULL,
                FOR_NOMBRE VARCHAR2(150 CHAR) NOT NULL,
                FOR_DESCRIPCION VARCHAR2(1000 CHAR),
                FOR_ESTADO VARCHAR2(20 CHAR) DEFAULT 'ACTIVE' NOT NULL,
                FOR_METADATA_JSON CLOB,
                FOR_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                FOR_USR_CREACION NUMBER(10) NOT NULL,
                FOR_VERSION_ROW NUMBER(10) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_FORMULAS PRIMARY KEY (FOR_ID),
                CONSTRAINT UQ_RL_MR_FOR_COD UNIQUE (FOR_CODIGO),
                CONSTRAINT CK_RL_MR_FOR_EST CHECK (FOR_ESTADO IN ('ACTIVE','INACTIVE','RETIRED')),
                CONSTRAINT CK_RL_MR_FOR_ROW CHECK (FOR_VERSION_ROW >= 1)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FORMULAS DROP CONSTRAINT FK_RL_MR_FOR_USR");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_FORMULA_VERSIONES (
                FOV_ID NUMBER(15) NOT NULL,
                FOV_FORMULA_ID NUMBER(15) NOT NULL,
                FOV_VERSION NUMBER(5) NOT NULL,
                FOV_EXPRESION CLOB NOT NULL,
                FOV_TIPO_RESULTADO VARCHAR2(20 CHAR) NOT NULL,
                FOV_ESTADO VARCHAR2(20 CHAR) DEFAULT 'DRAFT' NOT NULL,
                FOV_HASH VARCHAR2(64 CHAR) NOT NULL,
                FOV_FECHA_INICIO DATE,
                FOV_FECHA_FIN DATE,
                FOV_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                FOV_USR_CREACION NUMBER(10) NOT NULL,
                FOV_VERSION_ROW NUMBER(10) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_FOV PRIMARY KEY (FOV_ID),
                CONSTRAINT FK_RL_MR_FOV_FOR FOREIGN KEY (FOV_FORMULA_ID) REFERENCES RL_MR_FORMULAS(FOR_ID),
                CONSTRAINT UQ_RL_MR_FOV_VER UNIQUE (FOV_FORMULA_ID, FOV_VERSION),
                CONSTRAINT CK_RL_MR_FOV_VER CHECK (FOV_VERSION >= 1),
                CONSTRAINT CK_RL_MR_FOV_EST CHECK (FOV_ESTADO IN ('DRAFT','IN_REVIEW','APPROVED','PUBLISHED','RETIRED','ARCHIVED')),
                CONSTRAINT CK_RL_MR_FOV_HASH CHECK (REGEXP_LIKE(FOV_HASH,'^[0-9A-Fa-f]{64}$')),
                CONSTRAINT CK_RL_MR_FOV_ROW CHECK (FOV_VERSION_ROW >= 1)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FORMULA_VERSIONES DROP CONSTRAINT FK_RL_MR_FOV_USR");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_FORMULA_USOS (
                FUS_ID NUMBER(15) NOT NULL,
                FUS_VERSION_FORMULARIO_ID NUMBER(15) NOT NULL,
                FUS_CAMPO_CLAVE VARCHAR2(150 CHAR) NOT NULL,
                FUS_FORMULA_VERSION_ID NUMBER(15) NOT NULL,
                FUS_ETAPA VARCHAR2(50 CHAR) DEFAULT 'INHERENTE' NOT NULL,
                FUS_ORDEN NUMBER(5) DEFAULT 1 NOT NULL,
                FUS_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL,
                FUS_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                FUS_USR_CREACION NUMBER(10) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_FORMULA_USOS PRIMARY KEY (FUS_ID),
                CONSTRAINT FK_RL_MR_FUS_VERF FOREIGN KEY (FUS_VERSION_FORMULARIO_ID) REFERENCES RL_MR_VERSIONES_FORMULARIO(VER_ID),
                CONSTRAINT FK_RL_MR_FUS_FOV FOREIGN KEY (FUS_FORMULA_VERSION_ID) REFERENCES RL_MR_FORMULA_VERSIONES(FOV_ID),
                CONSTRAINT UQ_RL_MR_FUS_USO UNIQUE (FUS_VERSION_FORMULARIO_ID, FUS_CAMPO_CLAVE),
                CONSTRAINT CK_RL_MR_FUS_ACT CHECK (FUS_ACTIVO IN (0,1)),
                CONSTRAINT CK_RL_MR_FUS_ORD CHECK (FUS_ORDEN >= 1),
                CONSTRAINT CK_RL_MR_FUS_ETA CHECK (FUS_ETAPA IN ('INHERENTE','CONTROL','RESIDUAL','CALCULO'))
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FORMULA_USOS DROP CONSTRAINT FK_RL_MR_FUS_USR");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FORMULA_USOS MODIFY (FUS_ETAPA DEFAULT 'INHERENTE', FUS_ORDEN DEFAULT 1, FUS_ACTIVO DEFAULT 1, FUS_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FORMULAS MODIFY (FOR_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FORMULA_VERSIONES MODIFY (FOV_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FUNCIONES MODIFY (FUN_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FUNCION_VERSIONES MODIFY (FUV_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PARAMETROS_CALCULO MODIFY (PAC_USR_CREACION DEFAULT 1)");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PARAMETRO_VERSIONES MODIFY (PAV_USR_CREACION DEFAULT 1)");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_FUNCIONES (
                FUN_ID NUMBER(15) NOT NULL,
                FUN_CODIGO VARCHAR2(80 CHAR) NOT NULL,
                FUN_NOMBRE VARCHAR2(150 CHAR) NOT NULL,
                FUN_DESCRIPCION VARCHAR2(1000 CHAR),
                FUN_CATEGORIA VARCHAR2(50 CHAR) NOT NULL,
                FUN_ESTADO VARCHAR2(20 CHAR) DEFAULT 'ACTIVE' NOT NULL,
                FUN_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                FUN_USR_CREACION NUMBER(10) NOT NULL,
                FUN_VERSION_ROW NUMBER(10) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_FUNCIONES PRIMARY KEY (FUN_ID),
                CONSTRAINT UQ_RL_MR_FUN_COD UNIQUE (FUN_CODIGO),
                CONSTRAINT CK_RL_MR_FUN_EST CHECK (FUN_ESTADO IN ('ACTIVE','INACTIVE','RETIRED')),
                CONSTRAINT CK_RL_MR_FUN_ROW CHECK (FUN_VERSION_ROW >= 1)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FUNCIONES DROP CONSTRAINT FK_RL_MR_FUN_USR");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_FUNCION_VERSIONES (
                FUV_ID NUMBER(15) NOT NULL,
                FUV_FUNCION_ID NUMBER(15) NOT NULL,
                FUV_VERSION NUMBER(5) NOT NULL,
                FUV_EXPRESION CLOB NOT NULL,
                FUV_TIPO_RETORNO VARCHAR2(20 CHAR) NOT NULL,
                FUV_ESTADO VARCHAR2(20 CHAR) DEFAULT 'DRAFT' NOT NULL,
                FUV_HASH VARCHAR2(64 CHAR) NOT NULL,
                FUV_FECHA_INICIO DATE,
                FUV_FECHA_FIN DATE,
                FUV_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                FUV_USR_CREACION NUMBER(10) NOT NULL,
                FUV_VERSION_ROW NUMBER(10) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_FUV PRIMARY KEY (FUV_ID),
                CONSTRAINT FK_RL_MR_FUV_FUN FOREIGN KEY (FUV_FUNCION_ID) REFERENCES RL_MR_FUNCIONES(FUN_ID),
                CONSTRAINT UQ_RL_MR_FUV_VER UNIQUE (FUV_FUNCION_ID, FUV_VERSION),
                CONSTRAINT CK_RL_MR_FUV_VER CHECK (FUV_VERSION >= 1),
                CONSTRAINT CK_RL_MR_FUV_EST CHECK (FUV_ESTADO IN ('DRAFT','IN_REVIEW','APPROVED','PUBLISHED','RETIRED','ARCHIVED')),
                CONSTRAINT CK_RL_MR_FUV_HASH CHECK (REGEXP_LIKE(FUV_HASH,'^[0-9A-Fa-f]{64}$')),
                CONSTRAINT CK_RL_MR_FUV_ROW CHECK (FUV_VERSION_ROW >= 1)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_FUNCION_VERSIONES DROP CONSTRAINT FK_RL_MR_FUV_USR");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_FUNCION_ARGUMENTOS (
                FUA_ID NUMBER(15) NOT NULL,
                FUA_FUNCION_VERSION_ID NUMBER(15) NOT NULL,
                FUA_POSICION NUMBER(3) NOT NULL,
                FUA_NOMBRE VARCHAR2(80 CHAR) NOT NULL,
                FUA_TIPO VARCHAR2(20 CHAR) NOT NULL,
                FUA_OPCIONAL NUMBER(1) DEFAULT 0 NOT NULL,
                FUA_VALOR_DEFECTO VARCHAR2(255 CHAR),
                FUA_DESCRIPCION VARCHAR2(500 CHAR),
                CONSTRAINT PK_RL_MR_FUA PRIMARY KEY (FUA_ID),
                CONSTRAINT FK_RL_MR_FUA_FUV FOREIGN KEY (FUA_FUNCION_VERSION_ID) REFERENCES RL_MR_FUNCION_VERSIONES(FUV_ID),
                CONSTRAINT UQ_RL_MR_FUA_POS UNIQUE (FUA_FUNCION_VERSION_ID, FUA_POSICION),
                CONSTRAINT CK_RL_MR_FUA_POS CHECK (FUA_POSICION >= 1),
                CONSTRAINT CK_RL_MR_FUA_OPC CHECK (FUA_OPCIONAL IN (0,1))
            )");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_PARAMETROS_CALCULO (
                PAC_ID NUMBER(15) NOT NULL,
                PAC_CODIGO VARCHAR2(80 CHAR) NOT NULL,
                PAC_NOMBRE VARCHAR2(150 CHAR) NOT NULL,
                PAC_DESCRIPCION VARCHAR2(1000 CHAR),
                PAC_TIPO_DATO VARCHAR2(20 CHAR) NOT NULL,
                PAC_ESTADO VARCHAR2(20 CHAR) DEFAULT 'ACTIVE' NOT NULL,
                PAC_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                PAC_USR_CREACION NUMBER(10) NOT NULL,
                PAC_VERSION_ROW NUMBER(10) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_PAC PRIMARY KEY (PAC_ID),
                CONSTRAINT UQ_RL_MR_PAC_COD UNIQUE (PAC_CODIGO),
                CONSTRAINT CK_RL_MR_PAC_EST CHECK (PAC_ESTADO IN ('ACTIVE','INACTIVE','RETIRED')),
                CONSTRAINT CK_RL_MR_PAC_ROW CHECK (PAC_VERSION_ROW >= 1)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PARAMETROS_CALCULO DROP CONSTRAINT FK_RL_MR_PAC_USR");

        await ExecuteIgnoreAsync(connection, @"
            CREATE TABLE RL_MR_PARAMETRO_VERSIONES (
                PAV_ID NUMBER(15) NOT NULL,
                PAV_PARAMETRO_ID NUMBER(15) NOT NULL,
                PAV_VERSION NUMBER(5) NOT NULL,
                PAV_VALOR_TEXTO VARCHAR2(1000 CHAR),
                PAV_VALOR_NUMERICO NUMBER(18,6),
                PAV_VALOR_JSON CLOB,
                PAV_ESTADO VARCHAR2(20 CHAR) DEFAULT 'DRAFT' NOT NULL,
                PAV_FECHA_INICIO DATE,
                PAV_FECHA_FIN DATE,
                PAV_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL,
                PAV_USR_CREACION NUMBER(10) NOT NULL,
                PAV_VERSION_ROW NUMBER(10) DEFAULT 1 NOT NULL,
                CONSTRAINT PK_RL_MR_PAV PRIMARY KEY (PAV_ID),
                CONSTRAINT FK_RL_MR_PAV_PAC FOREIGN KEY (PAV_PARAMETRO_ID) REFERENCES RL_MR_PARAMETROS_CALCULO(PAC_ID),
                CONSTRAINT UQ_RL_MR_PAV_VER UNIQUE (PAV_PARAMETRO_ID, PAV_VERSION),
                CONSTRAINT CK_RL_MR_PAV_VER CHECK (PAV_VERSION >= 1),
                CONSTRAINT CK_RL_MR_PAV_EST CHECK (PAV_ESTADO IN ('DRAFT','IN_REVIEW','APPROVED','PUBLISHED','RETIRED','ARCHIVED')),
                CONSTRAINT CK_RL_MR_PAV_ROW CHECK (PAV_VERSION_ROW >= 1)
            )");
        await ExecuteIgnoreAsync(connection, "ALTER TABLE RL_MR_PARAMETRO_VERSIONES DROP CONSTRAINT FK_RL_MR_PAV_USR");

        // 6. Crear los 16 índices principales
        string[] indices =
        {
            "CREATE INDEX IDX_RL_MR_VER_VIG ON RL_MR_VERSIONES_FORMULARIO(VER_FAMILIA_ID,VER_VIGENTE,VER_ESTADO)",
            "CREATE INDEX IDX_RL_MR_ELE_CAT ON RL_MR_ELEMENTOS_CATALOGO(ELE_CATALOGO_ID)",
            "CREATE INDEX IDX_RL_MR_EVA_RIE ON RL_MR_EVALUACIONES_RIESGO(EVA_RIESGO_ID)",
            "CREATE INDEX IDX_RL_MR_EVA_VER ON RL_MR_EVALUACIONES_RIESGO(EVA_VERSION_ID)",
            "CREATE INDEX IDX_RL_MR_FLU_EVA_FEC ON RL_MR_FLUJOS_EVALUACION(FLU_EVALUACION_ID,FLU_FECHA)",
            "CREATE INDEX IDX_RL_MR_PROY_BUSQ ON RL_MR_PROYECCIONES_EVALUACION(PROY_ESTADO_EVALUACION,PROY_NIVEL_RESIDUAL,PROY_FECHA_EVAL)",
            "CREATE INDEX IDX_RL_MR_PROY_AREA ON RL_MR_PROYECCIONES_EVALUACION(PROY_AREA_PRINCIPAL)",
            "CREATE INDEX IDX_RL_MR_PROY_DUENO ON RL_MR_PROYECCIONES_EVALUACION(PROY_DUENO_RIESGO)",
            "CREATE INDEX IDX_RL_MR_CON_EVA ON RL_MR_CONTROLES_RIESGO(CON_EVALUACION_ID)",
            "CREATE INDEX IDX_RL_MR_ECO_CON ON RL_MR_EVALUACIONES_CONTROL(ECO_CONTROL_ID)",
            "CREATE INDEX IDX_RL_MR_PLA_EVA ON RL_MR_PLANES(PLA_EVALUACION_ID)",
            "CREATE INDEX IDX_RL_MR_ACT_PLAN ON RL_MR_ACTIVIDADES(ACT_PLAN_ID)",
            "CREATE INDEX IDX_RL_MR_EVV_ENTIDAD ON RL_MR_EVIDENCIAS_VINCULOS(EVV_TIPO_ENTIDAD,EVV_ENTIDAD_ID)",
            "CREATE INDEX IDX_RL_MR_EVV_EVIDENCIA ON RL_MR_EVIDENCIAS_VINCULOS(EVV_EVIDENCIA_ID)",
            "CREATE INDEX IDX_RL_MR_ALE_EVAL ON RL_MR_SENALES_ALERTA(ALE_EVALUACION_ID,ALE_ESTADO)",
            "CREATE INDEX IDX_RL_MR_MON_EVAL_FEC ON RL_MR_AUTOMONITOREO(MON_EVALUACION_ID,MON_FECHA)"
        };
        foreach (string idx in indices)
        {
            await ExecuteIgnoreAsync(connection, idx);
        }

        // 7. Retirar cualquier objeto obsoleto si existiera
        string[] tablasRetiradas =
        {
            "RL_MR_EVI_APROBACION", "RL_MR_EVI_REVISION", "RL_MR_EVI_AUTOMONITOREO",
            "RL_MR_EVI_ALERTA", "RL_MR_EVI_ACTIVIDAD", "RL_MR_EVI_PLAN", "RL_MR_EVI_CONTROL",
            "RL_MR_EVI_EVALUACION", "RL_MR_EVI_RIESGO", "RL_MR_DETALLES_IMPORTACION",
            "RL_MR_LOTES_IMPORTACION", "RL_MR_TRAZAS_CALCULO", "RL_MR_AUDITORIA",
            "RL_MR_PERMISOS_FORMULARIO", "RL_MR_APROBACIONES_FORMULARIO", "RL_MR_CAMPOS_FORMULARIO",
            "RL_MR_RELACIONES_RIESGO", "RL_MR_REVISIONES_EVALUACION"
        };
        foreach (string tabla in tablasRetiradas)
        {
            await ExecuteIgnoreAsync(connection, $"DROP TABLE {tabla} CASCADE CONSTRAINTS PURGE");
        }

        string[] secuenciasRetiradas =
        {
            "SEQ_RL_MR_AUDITORIA", "SEQ_RL_MR_TRAZAS", "SEQ_RL_MR_REVISIONES"
        };
        foreach (string seq in secuenciasRetiradas)
        {
            await ExecuteIgnoreAsync(connection, $"DROP SEQUENCE {seq}");
        }
    }

    private static void ValidarDestinoDisposableXe(string connectionString)
    {
        var builder = new OracleConnectionStringBuilder(connectionString);
        string source = builder.DataSource ?? string.Empty;
        bool isLoopback = source.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                          source.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        bool isXe = source.Contains("SERVICE_NAME=XE", StringComparison.OrdinalIgnoreCase) ||
                    source.Contains("/XE", StringComparison.OrdinalIgnoreCase) ||
                    source.Contains(":XE", StringComparison.OrdinalIgnoreCase);

        if (!isLoopback || !isXe)
        {
            throw new InvalidOperationException("El bootstrap de Oracle solo puede ejecutarse en Oracle XE descartable (127.0.0.1 / localhost:1521/XE).");
        }
    }

    private static async Task ExecuteIgnoreAsync(OracleConnection connection, string sql)
    {
        try
        {
            await using var cmd = new OracleCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (OracleException ex) when (ex.Number is 1 or 942 or 955 or 1418 or 1430 or 1442 or 1451 or 2260 or 2261 or 2264 or 2275 or 2289 or 2441 or 2442 or 2443)
        {
            // Idempotencia segura en Oracle XE descartable:
            // 1: unique constraint violated
            // 942: table or view does not exist (para drops)
            // 955: object already exists
            // 1418: specified index does not exist (para drops)
            // 1430: column being added already exists in table
            // 1442: column being modified to NOT NULL is already NOT NULL
            // 1451: column to be modified to NULL is already NULL
            // 2260: table can have only one primary key
            // 2261: unique or primary key already exists
            // 2264: name already used by an existing constraint
            // 2275: such a referential constraint already exists in the table
            // 2289: sequence does not exist (para drops)
            // 2441: cannot drop nonexistent primary key
            // 2442: cannot drop nonexistent unique key
            // 2443: cannot drop constraint - nonexistent constraint
        }
    }

    private static async Task<object?> ScalarAsync(OracleConnection connection, string sql)
    {
        await using var cmd = new OracleCommand(sql, connection);
        return await cmd.ExecuteScalarAsync();
    }
}
