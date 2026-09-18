#pragma warning disable CA1416
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using RL.API.Features.Auditoria.Persistence;
using RL.API.Features.MatricesRiesgos.Persistence;
using RL.API.Infrastructure.Database;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

/// <summary>
/// Certificación de solo lectura del Bloque 1 de la Fase 11.
/// Solo abre Oracle cuando RL_ORACLE_INTEGRATION_REQUIRED=true y la conexión
/// se suministra mediante variable de entorno o User Secrets.
/// </summary>
[Collection("MatricesRiesgosOracle")]
public sealed class MatricesRiesgosPhase11Block1OracleIntegrationTests
{
    private const string FamiliaCodigo = "MATRIZ_RIESGOS_LAFT";
    private const string HashEsperado = "f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90";

    [Fact]
    [Trait("Category", "OracleIntegration")]
    public async Task SemillasOficiales_ExistenSonIntegrasYElBackendLasDeserializa()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<MatricesRiesgosPhase11Block1OracleIntegrationTests>(optional: true)
            .Build();

        bool requerido = string.Equals(
            Environment.GetEnvironmentVariable("RL_ORACLE_INTEGRATION_REQUIRED"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!requerido)
        {
            return;
        }

        string? connectionString = configuration.GetConnectionString("OracleDB")
            ?? configuration["ConnectionStrings:OracleDB"];
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        // El runner puede encadenar suites Oracle en el mismo proceso. Limpiar
        // el pool antes de abrir evita reutilizar una sesión que el proveedor
        // dejó en estado de conexión pendiente; no altera datos ni configuración.
        OracleConnection.ClearAllPools();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();

        Assert.Equal(
            "RIESGO_LAVADO",
            (await ScalarAsync(connection, "SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') FROM DUAL"))?.ToString());

        Assert.Equal(1, Convert.ToInt32(await ScalarAsync(connection, @"
            SELECT COUNT(*)
              FROM RL_MR_FAMILIAS_FORMULARIO f
              JOIN RL_MR_VERSIONES_FORMULARIO v
                ON v.VER_FAMILIA_ID = f.FAM_ID
             WHERE f.FAM_CODIGO = 'MATRIZ_RIESGOS_LAFT'
               AND f.FAM_ACTIVO = 1
               AND v.VER_CODIGO = 'MATRIZ_RIESGOS_LAFT_V1'
               AND v.VER_VERSION = 1
               AND v.VER_ESTADO = 'PUBLISHED'
               AND v.VER_VIGENTE = 1
               AND LOWER(v.VER_HASH) = 'f2f84f21b6cc46762fd6087bc41df449b31ca87b058c763689bdfb3bba961f90'")));

        Assert.Equal(4, Convert.ToInt32(await ScalarAsync(connection, @"
            SELECT COUNT(*)
              FROM RL_MR_CATALOGOS
             WHERE CAT_CODIGO IN (
                'MR_FRECUENCIA_1_5',
                'MR_IMPACTO_1_5',
                'MR_NIVEL_RIESGO',
                'MR_RESPUESTA_RIESGO'
             )
               AND CAT_ACTIVO = 1")));

        Assert.Equal(18, Convert.ToInt32(await ScalarAsync(connection, @"
            SELECT COUNT(*)
              FROM RL_MR_ELEMENTOS_CATALOGO e
              JOIN RL_MR_CATALOGOS c
                ON c.CAT_ID = e.ELE_CATALOGO_ID
             WHERE c.CAT_CODIGO IN (
                'MR_FRECUENCIA_1_5',
                'MR_IMPACTO_1_5',
                'MR_NIVEL_RIESGO',
                'MR_RESPUESTA_RIESGO'
             )
               AND e.ELE_ACTIVO = 1")));

        Assert.Equal(1, Convert.ToInt32(await ScalarAsync(connection, @"
            SELECT COUNT(*)
              FROM RL_MR_REGLAS_CALCULO
             WHERE REG_CODIGO = 'CALCULO_VRI_VRR'
               AND REG_VERSION = '1.0'
               AND REG_ALGORITMO_ID = 'MATRICES_VRI_ADITIVO_1_9'
               AND REG_ACTIVA = 1")));

        // Las tablas B10_* pertenecen a una arquitectura histórica retirada.
        // El contrato vigente utiliza el modelo operativo RL_MR_* y no debe
        // exigir objetos que ya no forman parte del despliegue aprobado.
        Assert.Equal(0, Convert.ToInt32(await ScalarAsync(connection, @"
            SELECT COUNT(*)
              FROM USER_TABLES
             WHERE TABLE_NAME LIKE 'B10\_%' ESCAPE '\'")));

        var db = new OracleDbContext(connectionString!);
        var auditoria = new AuditoriaRepository(db, new HttpContextAccessor());
        var repository = new MatricesRiesgosRepository(db, auditoria);

        var version = await repository.ObtenerVersionVigenteFormularioAsync(FamiliaCodigo);
        Assert.NotNull(version);
        Assert.Equal(HashEsperado, version!.VerHash, ignoreCase: true);
        Assert.Equal("PUBLISHED", version.VerEstado);
        Assert.True(version.VerVigente);

        var metodologia = await repository.ObtenerMetodologiaDinamicaPorVersionAsync(version.VerId);
        Assert.NotNull(metodologia);
        Assert.Equal("MATRIZ_RIESGOS_LAFT_V1", metodologia!.Codigo);
        Assert.Equal(4, metodologia.Secciones.Count);
        Assert.Equal(4, metodologia.Catalogos.Count);
        Assert.Single(metodologia.Reglas);
        Assert.Equal("CALCULO_VRI_VRR", metodologia.Reglas[0].Codigo);
        Assert.Equal("MATRICES_VRI_ADITIVO_1_9", metodologia.Reglas[0].AlgoritmoId);
    }

    private static async Task<object?> ScalarAsync(OracleConnection connection, string sql)
    {
        await using var command = new OracleCommand(sql, connection) { BindByName = true };
        return await command.ExecuteScalarAsync();
    }
}
