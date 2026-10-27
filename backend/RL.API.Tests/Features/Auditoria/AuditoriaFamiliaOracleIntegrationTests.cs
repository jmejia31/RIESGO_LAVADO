using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Infrastructure.Database;
using RL.API.Tests.Support;
using Xunit;

namespace RL.API.Tests.Features.Auditoria;

[Collection("MatricesRiesgosOracle")]
public sealed class AuditoriaFamiliaOracleIntegrationTests
{
    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task HistorialFamilia_ResuelveIdYCodigoConPaginacionEstableYAislamiento()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets<AuditoriaFamiliaOracleIntegrationTests>(optional: true)
            .AddUserSecrets<OracleDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        if (!string.Equals(
                configuration[DatabaseProfileResolver.ProfileConfigurationKey],
                DatabaseProfileResolver.LocalXeProfileName,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string connectionString = OracleTestDatabaseProfile.RequireLocalXeConnection(null);
        long familiaId = 0;
        long otraFamiliaId = 0;
        string codigo = $"H02_{Guid.NewGuid():N}"[..30].ToUpperInvariant();
        string otroCodigo = $"H02_OTRA_{Guid.NewGuid():N}"[..30].ToUpperInvariant();

        await using (OracleConnection seed = new(connectionString))
        {
            await seed.OpenAsync();
            familiaId = await NextValueAsync(seed, "SEQ_RL_MR_FAMILIAS");
            otraFamiliaId = await NextValueAsync(seed, "SEQ_RL_MR_FAMILIAS");
            await ExecuteAsync(seed,
                "INSERT INTO RL_MR_FAMILIAS_FORMULARIO (FAM_ID,FAM_CODIGO,FAM_NOMBRE,FAM_ACTIVO,FAM_ARCHIVADA,FAM_FECHA_CREACION) VALUES (:id,:codigo,'H02 familia',1,0,SYSDATE)",
                ("id", familiaId), ("codigo", codigo));
            await ExecuteAsync(seed,
                "INSERT INTO RL_MR_FAMILIAS_FORMULARIO (FAM_ID,FAM_CODIGO,FAM_NOMBRE,FAM_ACTIVO,FAM_ARCHIVADA,FAM_FECHA_CREACION) VALUES (:id,:codigo,'H02 otra familia',1,0,SYSDATE)",
                ("id", otraFamiliaId), ("codigo", otroCodigo));

            for (int index = 0; index < 60; index++)
            {
                string registroId = index % 2 == 0 ? familiaId.ToString() : codigo;
                await ExecuteAsync(seed,
                    "INSERT INTO RL_AUDITORIA (AUD_ID,AUD_TABLA,AUD_REGISTRO_ID,AUD_ACCION,AUD_FECHA,AUD_MODULO) VALUES (SEQ_RL_AUDITORIA.NEXTVAL,'RL_MR_FAMILIAS_FORMULARIO',:registroId,:accion,TO_DATE('2026-10-27 00:00:00','YYYY-MM-DD HH24:MI:SS') - :orden/1440,'MatricesRiesgos')",
                    ("registroId", registroId), ("accion", $"H02_EVENT_{index:00}"), ("orden", index));
            }

            for (int index = 0; index < 5; index++)
            {
                await ExecuteAsync(seed,
                    "INSERT INTO RL_AUDITORIA (AUD_ID,AUD_TABLA,AUD_REGISTRO_ID,AUD_ACCION,AUD_FECHA,AUD_MODULO) VALUES (SEQ_RL_AUDITORIA.NEXTVAL,'RL_MR_FAMILIAS_FORMULARIO',:registroId,:accion,SYSDATE,'MatricesRiesgos')",
                    ("registroId", otraFamiliaId.ToString()), ("accion", $"H02_OTHER_{index:00}"));
            }
        }

        try
        {
            var repository = new AuditoriaRepository(
                new OracleDbContext(connectionString),
                new HttpContextAccessor());

            var page1 = await repository.ObtenerPorFamiliaPaginadaAsync(familiaId, 1, 25);
            var page2 = await repository.ObtenerPorFamiliaPaginadaAsync(familiaId, 2, 25);
            var page3 = await repository.ObtenerPorFamiliaPaginadaAsync(familiaId, 3, 25);
            var all = page1.Datos.Concat(page2.Datos).Concat(page3.Datos).ToList();

            Assert.Equal(60, page1.Total);
            Assert.Equal(60, page2.Total);
            Assert.Equal(60, page3.Total);
            Assert.Equal(25, page1.Datos.Count);
            Assert.Equal(25, page2.Datos.Count);
            Assert.Equal(10, page3.Datos.Count);
            Assert.Equal(60, all.Select(item => item.AudId).Distinct().Count());
            Assert.DoesNotContain(all, item => item.RegistroId == otraFamiliaId.ToString());
            Assert.Contains(all, item => item.RegistroId == familiaId.ToString());
            Assert.Contains(all, item => item.RegistroId == codigo);
            Assert.True(all.Zip(all.Skip(1), (left, right) => left.Fecha >= right.Fecha).All(value => value));
        }
        finally
        {
            await using OracleConnection cleanup = new(connectionString);
            await cleanup.OpenAsync();
            await ExecuteAsync(cleanup,
                "DELETE FROM RL_AUDITORIA WHERE AUD_TABLA='RL_MR_FAMILIAS_FORMULARIO' AND (AUD_REGISTRO_ID=:id OR AUD_REGISTRO_ID=:codigo OR AUD_REGISTRO_ID=:otroId)",
                ("id", familiaId.ToString()), ("codigo", codigo), ("otroId", otraFamiliaId.ToString()));
            await ExecuteAsync(cleanup,
                "DELETE FROM RL_MR_FAMILIAS_FORMULARIO WHERE FAM_ID IN (:id,:otroId)",
                ("id", familiaId), ("otroId", otraFamiliaId));
        }
    }

    private static async Task<long> NextValueAsync(OracleConnection connection, string sequence)
    {
        await using var command = new OracleCommand($"SELECT {sequence}.NEXTVAL FROM DUAL", connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task ExecuteAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = new OracleCommand(sql, connection) { BindByName = true };
        foreach ((string name, object value) in parameters)
            command.Parameters.Add(new OracleParameter(name, value));
        await command.ExecuteNonQueryAsync();
    }
}
