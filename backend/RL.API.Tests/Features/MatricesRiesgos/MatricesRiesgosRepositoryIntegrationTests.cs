#pragma warning disable CA1416
using System;
using System.Collections.Generic;
using System.IO;
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
/// Certificación Oracle pendiente de la Fase 1.2.
///
/// Estas pruebas NO se conectan a Oracle durante la ejecución ordinaria. Solo se
/// habilitan cuando un operador autorizado configura simultáneamente:
///   RL_ORACLE_INTEGRATION_REQUIRED=true
///   ConnectionStrings__OracleDB mediante variable de entorno o User Secrets.
///
/// No ejecutan scripts DDL, no ejecutan el script 05 y trabajan únicamente con
/// registros aislados que se eliminan al finalizar.
/// </summary>
public sealed class MatricesRiesgosRepositoryIntegrationTests
{
    private const string PrefijoPrueba = "TEST_INT_MR_EVI_";

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
    public async Task VinculoGenericoYAuditoria_CommitConjunto_PersistenEnLaMismaOperacion()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        DatosPrueba datos = await CrearDatosPruebaAsync("COMMIT");
        try
        {
            var db = new OracleDbContext(_connectionString!);
            var auditoria = new AuditoriaRepository(db, new HttpContextAccessor());
            var repository = new MatricesRiesgosRepository(db, auditoria);

            bool resultado = await repository.VincularEvidenciaAsync(
                new VincularEvidenciaDto
                {
                    EvidenciaId = datos.EvidenciaId,
                    TipoEntidad = TipoEntidadEvidencia.Riesgo,
                    EntidadId = datos.RiesgoId
                },
                datos.UsuarioId,
                "127.0.0.1");

            Assert.True(resultado);

            await using OracleConnection conn = CrearConexion();
            await conn.OpenAsync();

            long vinculoId = await ObtenerVinculoIdAsync(conn, datos);
            Assert.True(vinculoId > 0);

            await using var auditoriaCmd = new OracleCommand(@"
                SELECT COUNT(*)
                  FROM RL_AUDITORIA
                 WHERE AUD_TABLA = 'RL_MR_EVIDENCIAS_VINCULOS'
                   AND AUD_REGISTRO_ID = :registroId
                   AND AUD_ACCION = 'VINCULAR_EVIDENCIA'", conn)
            {
                BindByName = true
            };
            auditoriaCmd.Parameters.Add(new OracleParameter("registroId", vinculoId.ToString()));

            Assert.Equal(1, Convert.ToInt32(await auditoriaCmd.ExecuteScalarAsync()));
        }
        finally
        {
            await LimpiarDatosPruebaAsync(datos);
        }
    }

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task VinculoGenericoYAuditoria_FalloPosteriorAInsertarAuditoria_RevierteAmbosRegistros()
    {
        if (!await ValidarEntornoEjecucionAsync())
        {
            return;
        }

        DatosPrueba datos = await CrearDatosPruebaAsync("ROLLBACK");
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
                        TipoEntidad = TipoEntidadEvidencia.Riesgo,
                        EntidadId = datos.RiesgoId
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
            await LimpiarDatosPruebaAsync(datos);
        }
    }

    private async Task<bool> ValidarEntornoEjecucionAsync()
    {
        if (!_integrationRequired)
        {
            _output.WriteLine(
                "[OMITIDA] Prueba Oracle no habilitada. " +
                "RL_ORACLE_INTEGRATION_REQUIRED debe ser true. " +
                "Este resultado no certifica la Fase 1.2.");
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

            foreach (string objeto in new[]
            {
                "RL_MR_RIESGOS",
                "RL_MR_EVIDENCIAS",
                "RL_MR_EVIDENCIAS_VINCULOS",
                "RL_AUDITORIA"
            })
            {
                await using var objetoCmd = new OracleCommand(
                    "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = :tabla",
                    conn)
                {
                    BindByName = true
                };
                objetoCmd.Parameters.Add(new OracleParameter("tabla", objeto));

                if (Convert.ToInt32(await objetoCmd.ExecuteScalarAsync()) != 1)
                {
                    Assert.Fail($"La certificación requiere el objeto Oracle {objeto}; no se ejecutará ninguna migración automática.");
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Assert.Fail($"No fue posible validar de forma segura el entorno Oracle: {ex.Message}");
            return false;
        }
    }

    private async Task<DatosPrueba> CrearDatosPruebaAsync(string escenario)
    {
        await using OracleConnection conn = CrearConexion();
        await conn.OpenAsync();
        await using OracleTransaction transaction = conn.BeginTransaction();

        try
        {
            long usuarioId = await ObtenerUsuarioValidoAsync(conn, transaction);
            long riesgoId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_RIESGOS");
            long evidenciaId = await SiguienteSecuenciaAsync(conn, transaction, "SEQ_RL_MR_EVIDENCIAS");
            string sufijo = $"{escenario}_{Guid.NewGuid():N}";

            await using (var riesgoCmd = new OracleCommand(@"
                INSERT INTO RL_MR_RIESGOS (
                    RIE_ID, RIE_CODIGO, RIE_ACTIVO
                ) VALUES (
                    :riesgoId, :codigo, 1
                )", conn)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                riesgoCmd.Parameters.Add(new OracleParameter("riesgoId", riesgoId));
                riesgoCmd.Parameters.Add(new OracleParameter("codigo", PrefijoPrueba + sufijo));
                await riesgoCmd.ExecuteNonQueryAsync();
            }

            await using (var evidenciaCmd = new OracleCommand(@"
                INSERT INTO RL_MR_EVIDENCIAS (
                    EVI_ID,
                    EVI_NOMBRE_ARCHIVO,
                    EVI_EXTENSION,
                    EVI_TAMANO,
                    EVI_HASH,
                    EVI_RUTA,
                    EVI_USR_CREACION
                ) VALUES (
                    :evidenciaId,
                    :nombre,
                    'txt',
                    1,
                    :hash,
                    :ruta,
                    :usuarioId
                )", conn)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                evidenciaCmd.Parameters.Add(new OracleParameter("evidenciaId", evidenciaId));
                evidenciaCmd.Parameters.Add(new OracleParameter("nombre", PrefijoPrueba + sufijo + ".txt"));
                evidenciaCmd.Parameters.Add(new OracleParameter("hash", sufijo.PadRight(64, '0')[..64]));
                evidenciaCmd.Parameters.Add(new OracleParameter("ruta", "/pruebas-integracion/" + sufijo));
                evidenciaCmd.Parameters.Add(new OracleParameter("usuarioId", usuarioId));
                await evidenciaCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return new DatosPrueba(riesgoId, evidenciaId, usuarioId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task LimpiarDatosPruebaAsync(DatosPrueba datos)
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
                 WHERE EVV_EVIDENCIA_ID = :evidenciaId
                   AND EVV_TIPO_ENTIDAD = 'RIESGO'
                   AND EVV_ENTIDAD_ID = :riesgoId", conn)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                buscarCmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
                buscarCmd.Parameters.Add(new OracleParameter("riesgoId", datos.RiesgoId));
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

            await EjecutarAsync(
                conn,
                transaction,
                "DELETE FROM RL_MR_EVIDENCIAS_VINCULOS WHERE EVV_EVIDENCIA_ID = :id",
                new OracleParameter("id", datos.EvidenciaId));
            await EjecutarAsync(
                conn,
                transaction,
                "DELETE FROM RL_MR_EVIDENCIAS WHERE EVI_ID = :id",
                new OracleParameter("id", datos.EvidenciaId));
            await EjecutarAsync(
                conn,
                transaction,
                "DELETE FROM RL_MR_RIESGOS WHERE RIE_ID = :id",
                new OracleParameter("id", datos.RiesgoId));

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _output.WriteLine($"[ADVERTENCIA] Limpieza Oracle incompleta: {ex.Message}");
        }
    }

    private async Task<long> ObtenerVinculoIdAsync(OracleConnection conn, DatosPrueba datos)
    {
        await using var cmd = new OracleCommand(@"
            SELECT EVV_ID
              FROM RL_MR_EVIDENCIAS_VINCULOS
             WHERE EVV_EVIDENCIA_ID = :evidenciaId
               AND EVV_TIPO_ENTIDAD = 'RIESGO'
               AND EVV_ENTIDAD_ID = :riesgoId", conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
        cmd.Parameters.Add(new OracleParameter("riesgoId", datos.RiesgoId));
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }

    private static async Task<int> ContarVinculosAsync(OracleConnection conn, DatosPrueba datos)
    {
        await using var cmd = new OracleCommand(@"
            SELECT COUNT(*)
              FROM RL_MR_EVIDENCIAS_VINCULOS
             WHERE EVV_EVIDENCIA_ID = :evidenciaId
               AND EVV_TIPO_ENTIDAD = 'RIESGO'
               AND EVV_ENTIDAD_ID = :riesgoId", conn)
        {
            BindByName = true
        };
        cmd.Parameters.Add(new OracleParameter("evidenciaId", datos.EvidenciaId));
        cmd.Parameters.Add(new OracleParameter("riesgoId", datos.RiesgoId));
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
        if (secuencia is not ("SEQ_RL_MR_RIESGOS" or "SEQ_RL_MR_EVIDENCIAS"))
        {
            throw new InvalidOperationException("Secuencia no autorizada en la prueba de integración.");
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

    private sealed record DatosPrueba(long RiesgoId, long EvidenciaId, long UsuarioId);

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
