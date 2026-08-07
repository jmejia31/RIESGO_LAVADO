#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Contracts;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Contracts;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;
using Xunit;
using Xunit.Abstractions;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Suite de certificación Oracle del modelo reducido de 17 tablas.
///
/// La ejecución ordinaria NO abre conexiones Oracle. Solo se habilita cuando un
/// operador autorizado configura simultáneamente:
///   RL_ORACLE_INTEGRATION_REQUIRED=true
///   ConnectionStrings__OracleDB mediante variable de entorno o User Secrets.
///
/// La suite no ejecuta DDL, no ejecuta los scripts 05 o 06 y no modifica el
/// esquema. Únicamente certifica objetos existentes y utiliza registros aislados
/// que se eliminan al terminar cada escenario confirmado.
/// </summary>
public sealed class MatricesRiesgosRepositoryIntegrationTests
{
    private const string PrefijoPrueba = "TMR17_";

    internal static readonly string[] TablasModelo17 =
    {
        "RL_MR_FAMILIAS_FORMULARIO",
        "RL_MR_VERSIONES_FORMULARIO",
        "RL_MR_CATALOGOS",
        "RL_MR_ELEMENTOS_CATALOGO",
        "RL_MR_REGLAS_CALCULO",
        "RL_MR_RIESGOS",
        "RL_MR_EVALUACIONES_RIESGO",
        "RL_MR_PROYECCIONES_EVALUACION",
        "RL_MR_FLUJOS_EVALUACION",
        "RL_MR_CONTROLES_RIESGO",
        "RL_MR_EVALUACIONES_CONTROL",
        "RL_MR_PLANES",
        "RL_MR_ACTIVIDADES",
        "RL_MR_EVIDENCIAS",
        "RL_MR_EVIDENCIAS_VINCULOS",
        "RL_MR_SENALES_ALERTA",
        "RL_MR_AUTOMONITOREO"
    };

    internal static readonly string[] SecuenciasModelo17 =
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

    internal static readonly string[] TablasRetiradas =
    {
        "RL_MR_EVI_APROBACION",
        "RL_MR_EVI_REVISION",
        "RL_MR_EVI_AUTOMONITOREO",
        "RL_MR_EVI_ALERTA",
        "RL_MR_EVI_ACTIVIDAD",
        "RL_MR_EVI_PLAN",
        "RL_MR_EVI_CONTROL",
        "RL_MR_EVI_EVALUACION",
        "RL_MR_EVI_RIESGO",
        "RL_MR_DETALLES_IMPORTACION",
        "RL_MR_LOTES_IMPORTACION",
        "RL_MR_TRAZAS_CALCULO_OLD",
        "RL_MR_AUDITORIA",
        "RL_MR_PERMISOS_FORMULARIO",
        "RL_MR_APROBACIONES_FORMULARIO",
        "RL_MR_CAMPOS_FORMULARIO",
        "RL_MR_RELACIONES_RIESGO",
        "RL_MR_REVISIONES_EVALUACION"
    };

    internal static readonly string[] SecuenciasRetiradas =
    {
        "SEQ_RL_MR_AUDITORIA",
        "SEQ_RL_MR_TRAZAS_OLD",
        "SEQ_RL_MR_REVISIONES"
    };

    internal static readonly string[] IndicesPrincipales =
    {
        "IDX_RL_MR_VER_VIG",
        "IDX_RL_MR_ELE_CAT",
        "IDX_RL_MR_EVA_RIE",
        "IDX_RL_MR_EVA_VER",
        "IDX_RL_MR_FLU_EVA_FEC",
        "IDX_RL_MR_PROY_BUSQ",
        "IDX_RL_MR_PROY_AREA",
        "IDX_RL_MR_PROY_DUENO",
        "IDX_RL_MR_CON_EVA",
        "IDX_RL_MR_ECO_CON",
        "IDX_RL_MR_PLA_EVA",
        "IDX_RL_MR_ACT_PLAN",
        "IDX_RL_MR_EVV_ENTIDAD",
        "IDX_RL_MR_EVV_EVIDENCIA",
        "IDX_RL_MR_ALE_EVAL",
        "IDX_RL_MR_MON_EVAL_FEC"
    };

    internal static readonly string[] RestriccionesPrincipales =
    {
        "PK_RL_MR_FAMILIAS",
        "PK_RL_MR_VERSIONES",
        "PK_RL_MR_CATALOGOS",
        "PK_RL_MR_ELEMENTOS",
        "PK_RL_MR_REGLAS",
        "PK_RL_MR_RIESGOS",
        "PK_RL_MR_EVALUACIONES",
        "PK_RL_MR_PROYECCIONES",
        "PK_RL_MR_FLUJOS",
        "PK_RL_MR_CONTROLES",
        "PK_RL_MR_EVAL_CONTROLES",
        "PK_RL_MR_PLANES",
        "PK_RL_MR_ACTIVIDADES",
        "PK_RL_MR_EVIDENCIAS",
        "PK_RL_MR_EVI_VINCULOS",
        "PK_RL_MR_SENALES",
        "PK_RL_MR_AUTOMONITOREO",
        "FK_RL_MR_EVA_RIE",
        "FK_RL_MR_EVA_VER",
        "FK_RL_MR_PROY_EVA",
        "FK_RL_MR_FLU_EVA",
        "FK_RL_MR_EVV_EVI",
        "UQ_RL_MR_PROY_EVA",
        "UQ_RL_MR_EVV_UNICO",
        "CK_RL_MR_FLU_EST",
        "CK_RL_MR_EVV_TIPO"
    };

    private readonly ITestOutputHelper _output;
    private readonly string? _connectionString;
    private readonly bool _integrationRequired;

    public MatricesRiesgosRepositoryIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<MatricesRiesgosRepositoryIntegrationTests>(optional: true)
            .Build();

        _connectionString = configuration.GetConnectionString("OracleDB")
            ?? configuration["ConnectionStrings:OracleDB"];

        _integrationRequired = string.Equals(
            Environment.GetEnvironmentVariable("RL_ORACLE_INTEGRATION_REQUIRED"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task EsquemaModelo17_InventarioIndicesRestriccionesYAusencias_CumplenContrato()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await ValidarContratoFisicoAsync(conn);
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task CicloCompleto_Commit_PersisteFamiliaVersionRiesgoEvaluacionProyeccionFlujoEvidenciaVinculoYAuditoria()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        DatosCiclo datos = await CrearCicloConfirmadoAsync("COMMIT");
        try
        {
            var db = new OracleDbContext(_connectionString!);
            var auditoria = new AuditoriaRepository(db, new HttpContextAccessor());
            var repository = new MatricesRiesgosRepository(db, auditoria);

            bool vinculado = await repository.VincularEvidenciaAsync(
                new VincularEvidenciaDto
                {
                    EvidenciaId = datos.EvidenciaId,
                    TipoEntidad = TipoEntidadEvidencia.Evaluacion,
                    EntidadId = datos.EvaluacionId
                },
                datos.UsuarioId,
                "127.0.0.1");

            Assert.True(vinculado);

            await using OracleConnection conn = CrearConexion();
            await conn.OpenAsync();

            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_FAMILIAS_FORMULARIO", "FAM_ID", datos.FamiliaId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_VERSIONES_FORMULARIO", "VER_ID", datos.VersionId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_RIESGOS", "RIE_ID", datos.RiesgoId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_EVALUACIONES_RIESGO", "EVA_ID", datos.EvaluacionId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_PROYECCIONES_EVALUACION", "PROY_ID", datos.ProyeccionId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_FLUJOS_EVALUACION", "FLU_ID", datos.FlujoId));
            Assert.Equal(1, await ContarPorIdAsync(conn, "RL_MR_EVIDENCIAS", "EVI_ID", datos.EvidenciaId));

            long vinculoId = await ObtenerVinculoIdAsync(conn, datos);
            Assert.True(vinculoId > 0);
            Assert.Equal(1, await ContarAuditoriaAsync(conn, vinculoId));
        }
        finally
        {
            await LimpiarCicloAsync(datos);
        }
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task CicloCompleto_Rollback_NoPersisteRegistrosBase()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();

        DatosCiclo datos = await InsertarCicloAsync(conn, transaction, "ROLLBACK_BASE");
        await transaction.RollbackAsync();

        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_FAMILIAS_FORMULARIO", "FAM_ID", datos.FamiliaId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_VERSIONES_FORMULARIO", "VER_ID", datos.VersionId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_RIESGOS", "RIE_ID", datos.RiesgoId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_EVALUACIONES_RIESGO", "EVA_ID", datos.EvaluacionId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_PROYECCIONES_EVALUACION", "PROY_ID", datos.ProyeccionId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_FLUJOS_EVALUACION", "FLU_ID", datos.FlujoId));
        Assert.Equal(0, await ContarPorIdAsync(conn, "RL_MR_EVIDENCIAS", "EVI_ID", datos.EvidenciaId));
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task VinculoGenericoYAuditoria_FalloPosteriorAInsertarAuditoria_RevierteAmbosRegistros()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        DatosCiclo datos = await CrearCicloConfirmadoAsync("ROLLBACK_AUD");
        try
        {
            var db = new OracleDbContext(_connectionString!);
            var auditoriaReal = new AuditoriaRepository(db, new HttpContextAccessor());
            var auditoriaConFallo = new AuditoriaFallaDespuesDeInsertar(auditoriaReal);
            var repository = new MatricesRiesgosRepository(db, auditoriaConFallo);

            int auditoriasAntes = await ContarAuditoriasVinculoAsync();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                repository.VincularEvidenciaAsync(
                    new VincularEvidenciaDto
                    {
                        EvidenciaId = datos.EvidenciaId,
                        TipoEntidad = TipoEntidadEvidencia.Evaluacion,
                        EntidadId = datos.EvaluacionId
                    },
                    datos.UsuarioId,
                    "127.0.0.1"));

            Assert.Contains("Fallo controlado posterior a la auditoría", error.Message);

            await using OracleConnection conn = CrearConexion();
            await conn.OpenAsync();

            Assert.Equal(0, await ContarVinculosAsync(conn, datos));
            Assert.Equal(auditoriasAntes, await ContarAuditoriasVinculoAsync());
        }
        finally
        {
            await LimpiarCicloAsync(datos);
        }
    }

    private async Task<bool> ValidarEntornoEjecucionAsync()
    {
        if (!_integrationRequired)
        {
            _output.WriteLine(
                "[OMITIDA] Certificación Oracle no habilitada. " +
                "RL_ORACLE_INTEGRATION_REQUIRED debe ser true. " +
                "Este resultado no certifica físicamente el modelo de 17 tablas.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            Assert.Fail(
                "RL_ORACLE_INTEGRATION_REQUIRED=true, pero no existe una cadena Oracle segura en variables de entorno o User Secrets.");
            return false;
        }

        try
        {
            await using OracleConnection conn = CrearConexion();
            await conn.OpenAsync();

            await using var schemaCmd = new OracleCommand(
                "SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM DUAL",
                conn);
            string esquema = Convert.ToString(await schemaCmd.ExecuteScalarAsync()) ?? string.Empty;

            if (!string.Equals(esquema, "RIESGO_LAVADO", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Fail($"Esquema Oracle no autorizado para certificación: {esquema}.");
                return false;
            }

            await ValidarContratoFisicoAsync(conn);
            return true;
        }
        catch (Exception ex)
        {
            Assert.Fail($"No fue posible validar de forma segura el entorno Oracle: {ex.Message}");
            return false;
        }
    }

    private static async Task ValidarContratoFisicoAsync(OracleConnection conn)
    {
        string[] tablasActuales = await ObtenerNombresAsync(
            conn,
            "SELECT TABLE_NAME FROM USER_TABLES WHERE TABLE_NAME LIKE 'RL_MR_%' ORDER BY TABLE_NAME");
        string[] tablasEsperadas = TablasModelo17.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(tablasEsperadas, tablasActuales);

        string[] secuenciasActuales = await ObtenerNombresAsync(
            conn,
            "SELECT SEQUENCE_NAME FROM USER_SEQUENCES WHERE SEQUENCE_NAME LIKE 'SEQ_RL_MR_%' ORDER BY SEQUENCE_NAME");
        string[] secuenciasEsperadas = SecuenciasModelo17.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(secuenciasEsperadas, secuenciasActuales);

        Assert.Equal(1, await ContarObjetoAsync(conn, "USER_TABLES", "TABLE_NAME", "RL_USUARIOS"));
        Assert.Equal(1, await ContarObjetoAsync(conn, "USER_TABLES", "TABLE_NAME", "RL_AUDITORIA"));
        Assert.Equal(1, await ContarObjetoAsync(conn, "USER_SEQUENCES", "SEQUENCE_NAME", "SEQ_RL_AUDITORIA"));

        foreach (string tabla in TablasRetiradas)
        {
            Assert.Equal(0, await ContarObjetoAsync(conn, "USER_TABLES", "TABLE_NAME", tabla));
        }

        foreach (string secuencia in SecuenciasRetiradas)
        {
            Assert.Equal(0, await ContarObjetoAsync(conn, "USER_SEQUENCES", "SEQUENCE_NAME", secuencia));
        }

        foreach (string indice in IndicesPrincipales)
        {
            Assert.Equal(1, await ContarObjetoAsync(conn, "USER_INDEXES", "INDEX_NAME", indice));
        }

        foreach (string restriccion in RestriccionesPrincipales)
        {
            await using var cmd = new OracleCommand(@"
                SELECT COUNT(*)
                  FROM USER_CONSTRAINTS
                 WHERE CONSTRAINT_NAME = :nombre
                   AND STATUS = 'ENABLED'", conn)
            {
                BindByName = true
            };
            cmd.Parameters.Add(new OracleParameter("nombre", restriccion));
            Assert.Equal(1, Convert.ToInt32(await cmd.ExecuteScalarAsync()));
        }
    }

    private async Task<DatosCiclo> CrearCicloConfirmadoAsync(string escenario)
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();

        try
        {
            DatosCiclo datos = await InsertarCicloAsync(conn, transaction, escenario);
            await transaction.CommitAsync();
            return datos;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task<DatosCiclo> InsertarCicloAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        string escenario)
    {
        long usuarioId = await ObtenerUsuarioValidoAsync(conn, transaction);
        long familiaId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_FAMILIAS");
        long versionId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_VERSIONES");
        long riesgoId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_RIESGOS");
        long evaluacionId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_EVALUACIONES");
        long proyeccionId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_PROYECCIONES");
        long flujoId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_FLUJOS");
        long evidenciaId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_EVIDENCIAS");
        string sufijo = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        string codigoBase = PrefijoPrueba + sufijo;

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_FAMILIAS_FORMULARIO (
                  FAM_ID, FAM_CODIGO, FAM_NOMBRE, FAM_DESCRIPCION, FAM_ACTIVO
              ) VALUES (
                  :id, :codigo, :nombre, :descripcion, 1
              )",
            new OracleParameter("id", familiaId),
            new OracleParameter("codigo", codigoBase),
            new OracleParameter("nombre", "Familia certificación " + escenario),
            new OracleParameter("descripcion", "Registro aislado de certificación Oracle"));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_VERSIONES_FORMULARIO (
                  VER_ID, VER_FAMILIA_ID, VER_CODIGO, VER_VERSION, VER_JSON, VER_HASH,
                  VER_ESTADO, VER_VIGENTE, VER_FECHA_INICIO, VER_USR_CREACION
              ) VALUES (
                  :id, :familiaId, :codigo, 1, :json, :hash,
                  'PUBLISHED', 1, SYSDATE, :usuarioId
              )",
            new OracleParameter("id", versionId),
            new OracleParameter("familiaId", familiaId),
            new OracleParameter("codigo", "V_" + sufijo),
            CrearClob("json", "{\"sections\":[]}"),
            new OracleParameter("hash", sufijo.PadRight(64, '0')),
            new OracleParameter("usuarioId", usuarioId));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_RIESGOS (
                  RIE_ID, RIE_CODIGO, RIE_NOMBRE, RIE_DESCRIPCION,
                  RIE_ACTIVO, RIE_USR_CREACION
              ) VALUES (
                  :id, :codigo, :nombre, :descripcion, 1, :usuarioId
              )",
            new OracleParameter("id", riesgoId),
            new OracleParameter("codigo", "R_" + sufijo),
            new OracleParameter("nombre", "Riesgo certificación " + escenario),
            new OracleParameter("descripcion", "Riesgo aislado para validar el modelo de 17 tablas"),
            new OracleParameter("usuarioId", usuarioId));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_EVALUACIONES_RIESGO (
                  EVA_ID, EVA_RIESGO_ID, EVA_VERSION_ID, EVA_DATOS_JSON,
                  EVA_CALCULOS_JSON, EVA_USR_REGISTRO, EVA_VERSION_ROW, EVA_ACTIVO
              ) VALUES (
                  :id, :riesgoId, :versionId, :datosJson,
                  :calculosJson, :usuarioId, 1, 1
              )",
            new OracleParameter("id", evaluacionId),
            new OracleParameter("riesgoId", riesgoId),
            new OracleParameter("versionId", versionId),
            CrearClob("datosJson", "{\"area\":\"PRUEBAS\",\"dueno\":\"CERTIFICACION\"}"),
            CrearClob("calculosJson", "{\"reglaCodigo\":\"CALCULO_VRI_VRR\",\"reglaVersion\":\"1.0\",\"algoritmoId\":\"MATRICES_VRI_ADITIVO_1_9\",\"vri\":7,\"vrr\":4}"),
            new OracleParameter("usuarioId", usuarioId));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_PROYECCIONES_EVALUACION (
                  PROY_ID, PROY_EVALUACION_ID, PROY_CODIGO_RIESGO,
                  PROY_AREA_PRINCIPAL, PROY_VRI, PROY_VRR,
                  PROY_NIVEL_INHERENTE, PROY_NIVEL_RESIDUAL,
                  PROY_RESPUESTA_RIESGO, PROY_ESTADO_EVALUACION,
                  PROY_DUENO_RIESGO, PROY_FECHA_EVAL
              ) VALUES (
                  :id, :evaluacionId, :codigoRiesgo,
                  'PRUEBAS', 7, 4,
                  'ALTO', 'MEDIO',
                  'MITIGAR', 'BORRADOR',
                  'CERTIFICACION', SYSDATE
              )",
            new OracleParameter("id", proyeccionId),
            new OracleParameter("evaluacionId", evaluacionId),
            new OracleParameter("codigoRiesgo", "R_" + sufijo));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_FLUJOS_EVALUACION (
                  FLU_ID, FLU_EVALUACION_ID, FLU_ESTADO, FLU_MOTIVO, FLU_USR_ID
              ) VALUES (
                  :id, :evaluacionId, 'BORRADOR', :motivo, :usuarioId
              )",
            new OracleParameter("id", flujoId),
            new OracleParameter("evaluacionId", evaluacionId),
            new OracleParameter("motivo", "Captura inicial de certificación"),
            new OracleParameter("usuarioId", usuarioId));

        await EjecutarAsync(
            conn,
            transaction,
            @"INSERT INTO RL_MR_EVIDENCIAS (
                  EVI_ID, EVI_NOMBRE_ARCHIVO, EVI_EXTENSION, EVI_TAMANO,
                  EVI_HASH, EVI_RUTA, EVI_USR_CREACION
              ) VALUES (
                  :id, :nombre, 'txt', 1,
                  :hash, :ruta, :usuarioId
              )",
            new OracleParameter("id", evidenciaId),
            new OracleParameter("nombre", codigoBase + ".txt"),
            new OracleParameter("hash", (escenario + sufijo).PadRight(64, '0')[..64]),
            new OracleParameter("ruta", "/certificacion-oracle/" + codigoBase),
            new OracleParameter("usuarioId", usuarioId));

        return new DatosCiclo(
            familiaId,
            versionId,
            riesgoId,
            evaluacionId,
            proyeccionId,
            flujoId,
            evidenciaId,
            usuarioId);
    }

    private async Task LimpiarCicloAsync(DatosCiclo datos)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return;
        }

        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();

        try
        {
            var vinculos = new List<long>();
            await using (var buscarCmd = new OracleCommand(@"
                SELECT EVV_ID
                  FROM RL_MR_EVIDENCIAS_VINCULOS
                 WHERE EVV_EVIDENCIA_ID = :evidenciaId", conn)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                buscarCmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
                await using OracleDataReader reader = await buscarCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    vinculos.Add(reader.GetInt64(0));
                }
            }

            foreach (long vinculoId in vinculos)
            {
                await EjecutarAsync(
                    conn,
                    transaction,
                    "DELETE FROM RL_AUDITORIA WHERE AUD_TABLA = 'RL_MR_EVIDENCIAS_VINCULOS' AND AUD_REGISTRO_ID = :id",
                    new OracleParameter("id", vinculoId.ToString()));
            }

            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_EVIDENCIAS_VINCULOS WHERE EVV_EVIDENCIA_ID = :id",
                new OracleParameter("id", datos.EvidenciaId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id",
                new OracleParameter("id", datos.EvidenciaId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_FLUJOS_EVALUACION WHERE FLU_EVALUACION_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_PROYECCIONES_EVALUACION WHERE PROY_EVALUACION_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_EVALUACIONES_RIESGO WHERE EVA_ID = :id",
                new OracleParameter("id", datos.EvaluacionId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_RIESGOS WHERE RIE_ID = :id",
                new OracleParameter("id", datos.RiesgoId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_VERSIONES_FORMULARIO WHERE VER_ID = :id",
                new OracleParameter("id", datos.VersionId));
            await EjecutarAsync(conn, transaction,
                "DELETE FROM RL_MR_FAMILIAS_FORMULARIO WHERE FAM_ID = :id",
                new OracleParameter("id", datos.FamiliaId));

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _output.WriteLine($"[ADVERTENCIA] Limpieza Oracle incompleta: {ex.Message}");
        }
    }

    private static async Task<string[]> ObtenerNombresAsync(OracleConnection conn, string sql)
    {
        var nombres = new List<string>();
        await using var cmd = new OracleCommand(sql, conn);
        await using OracleDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            nombres.Add(reader.GetString(0));
        }

        return nombres.ToArray();
    }

    private static async Task<int> ContarObjetoAsync(
        OracleConnection conn,
        string vista,
        string columna,
        string nombre)
    {
        if ((vista, columna) is not
            ("USER_TABLES", "TABLE_NAME") and not
            ("USER_SEQUENCES", "SEQUENCE_NAME") and not
            ("USER_INDEXES", "INDEX_NAME"))
        {
            throw new InvalidOperationException("Vista de metadatos no autorizada en la certificación.");
        }

        await using var cmd = new OracleCommand(
            $"SELECT COUNT(*) FROM {vista} WHERE {columna} = :nombre",
            conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("nombre", nombre));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<int> ContarPorIdAsync(
        OracleConnection conn,
        string tabla,
        string columna,
        long id)
    {
        var destinos = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RL_MR_FAMILIAS_FORMULARIO"] = "FAM_ID",
            ["RL_MR_VERSIONES_FORMULARIO"] = "VER_ID",
            ["RL_MR_RIESGOS"] = "RIE_ID",
            ["RL_MR_EVALUACIONES_RIESGO"] = "EVA_ID",
            ["RL_MR_PROYECCIONES_EVALUACION"] = "PROY_ID",
            ["RL_MR_FLUJOS_EVALUACION"] = "FLU_ID",
            ["RL_MR_EVIDENCIAS"] = "EVI_ID"
        };

        if (!destinos.TryGetValue(tabla, out string? columnaPermitida) ||
            !string.Equals(columnaPermitida, columna, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Destino no autorizado en la certificación Oracle.");
        }

        await using var cmd = new OracleCommand(
            $"SELECT COUNT(*) FROM {tabla} WHERE {columna} = :id",
            conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("id", id));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<long> ObtenerVinculoIdAsync(OracleConnection conn, DatosCiclo datos)
    {
        await using var cmd = new OracleCommand(@"
            SELECT EVV_ID
              FROM RL_MR_EVIDENCIAS_VINCULOS
             WHERE EVV_EVIDENCIA_ID = :evidenciaId
               AND EVV_TIPO_ENTIDAD = 'EVALUACION'
               AND EVV_ENTIDAD_ID = :evaluacionId", conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
        cmd.Parameters.Add(new OracleParameter("evaluacionId", datos.EvaluacionId));
        object? resultado = await cmd.ExecuteScalarAsync();
        return resultado is null ? 0 : Convert.ToInt64(resultado);
    }

    private static async Task<int> ContarVinculosAsync(OracleConnection conn, DatosCiclo datos)
    {
        await using var cmd = new OracleCommand(@"
            SELECT COUNT(*)
              FROM RL_MR_EVIDENCIAS_VINCULOS
             WHERE EVV_EVIDENCIA_ID = :evidenciaId
               AND EVV_TIPO_ENTIDAD = 'EVALUACION'
               AND EVV_ENTIDAD_ID = :evaluacionId", conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
        cmd.Parameters.Add(new OracleParameter("evaluacionId", datos.EvaluacionId));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<int> ContarAuditoriaAsync(OracleConnection conn, long vinculoId)
    {
        await using var cmd = new OracleCommand(@"
            SELECT COUNT(*)
              FROM RL_AUDITORIA
             WHERE AUD_TABLA = 'RL_MR_EVIDENCIAS_VINCULOS'
               AND AUD_REGISTRO_ID = :registroId
               AND AUD_ACCION = 'INSERT'", conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("registroId", vinculoId.ToString()));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> ContarAuditoriasVinculoAsync()
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(@"
            SELECT COUNT(*)
              FROM RL_AUDITORIA
             WHERE AUD_TABLA = 'RL_MR_EVIDENCIAS_VINCULOS'
               AND AUD_ACCION = 'VINCULAR_EVIDENCIA'", conn);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<long> ObtenerUsuarioValidoAsync(
        OracleConnection conn,
        OracleTransaction transaction)
    {
        await using var cmd = new OracleCommand(@"
            SELECT USR_ID
              FROM RL_USUARIOS
             WHERE ROWNUM = 1", conn)
        {
            Transaction = transaction
        };
        object? resultado = await cmd.ExecuteScalarAsync();
        return resultado is null
            ? throw new InvalidOperationException("No existe un usuario válido para la certificación Oracle.")
            : Convert.ToInt64(resultado);
    }

    private static async Task<long> SiguienteSecuenciaAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        string secuencia)
    {
        if (!SecuenciasModelo17.Contains(secuencia, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Secuencia no autorizada en la certificación Oracle.");
        }

        await using var cmd = new OracleCommand($"SELECT {secuencia}.NEXTVAL FROM DUAL", conn)
        {
            Transaction = transaction
        };
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private OracleConnection CrearConexion()
    {
        var builder = new OracleConnectionStringBuilder(_connectionString!)
        {
            ConnectionTimeout = 5
        };
        return new OracleConnection(builder.ConnectionString);
    }

    private static OracleParameter CrearClob(string nombre, string valor) =>
        new(nombre, OracleDbType.Clob) { Value = valor };

    private static async Task EjecutarAsync(
        OracleConnection conn,
        OracleTransaction transaction,
        string sql,
        params OracleParameter[] parameters)
    {
        await using var cmd = new OracleCommand(sql, conn)
        {
            BindByName = true,
            Transaction = transaction
        };
        cmd.Parameters.AddRange(parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    private sealed record DatosCiclo(
        long FamiliaId,
        long VersionId,
        long RiesgoId,
        long EvaluacionId,
        long ProyeccionId,
        long FlujoId,
        long EvidenciaId,
        long UsuarioId);

    private sealed class AuditoriaFallaDespuesDeInsertar : IAuditoriaRepository
    {
        private readonly IAuditoriaRepository _inner;

        public AuditoriaFallaDespuesDeInsertar(IAuditoriaRepository inner)
        {
            _inner = inner;
        }

        public Task RegistrarAsync(
            string tabla,
            string registroId,
            string accion,
            string? datosAnt,
            string? datosNvo,
            long? usrId,
            string? email,
            string? ip,
            string? modulo) =>
            _inner.RegistrarAsync(
                tabla,
                registroId,
                accion,
                datosAnt,
                datosNvo,
                usrId,
                email,
                ip,
                modulo);

        public async Task RegistrarAsync(
            OracleConnection connection,
            OracleTransaction? transaction,
            string tabla,
            string registroId,
            string accion,
            string? datosAnt,
            string? datosNvo,
            long? usrId,
            string? email,
            string? ip,
            string? modulo)
        {
            await _inner.RegistrarAsync(
                connection,
                transaction,
                tabla,
                registroId,
                accion,
                datosAnt,
                datosNvo,
                usrId,
                email,
                ip,
                modulo);

            throw new InvalidOperationException("Fallo controlado posterior a la auditoría.");
        }

        public Task<(List<AuditoriaDto> Datos, int Total)> ObtenerBitacoraPaginadaAsync(
            int pagina,
            int limite,
            string? buscar,
            string? accion,
            string? modulo,
            string? tabla,
            DateTime? fechaInicio,
            DateTime? fechaFin) =>
            _inner.ObtenerBitacoraPaginadaAsync(
                pagina,
                limite,
                buscar,
                accion,
                modulo,
                tabla,
                fechaInicio,
                fechaFin);
    }
}
