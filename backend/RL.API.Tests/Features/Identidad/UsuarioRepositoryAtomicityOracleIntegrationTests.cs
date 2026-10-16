using Oracle.ManagedDataAccess.Client;
using System.Globalization;
using RL.API.Features.Identidad.Contracts;
using RL.API.Features.Identidad.Persistence;
using RL.API.Infrastructure.Database;
using Xunit;

namespace RL.API.Tests.Features.Identidad;

[Collection("MatricesRiesgosOracle")]
public sealed class UsuarioRepositoryAtomicityOracleIntegrationTests
{
    [P0FOracleRequiredFact]
    [Trait("Category", "P0FOracleIntegration")]
    public async Task UserAndModuleWritesRollbackWhenNthAssignmentFails()
    {
        string connectionString = Environment.GetEnvironmentVariable("P0F_ORACLE_CONNECTION_STRING")
            ?? throw new InvalidOperationException("P0F_ORACLE_CONNECTION_STRING is required when P0-F Oracle integration is mandatory.");
        ValidarDestinoDisposableXe(connectionString);

        OracleConnection.ClearAllPools();
        await using var connection = new OracleConnection(connectionString);
        await connection.OpenAsync();
        Fixture fixture = await EnsureFixtureAsync(connection);
        var repository = new UsuarioRepository(new OracleDbContext(connectionString));
        string failedCreateEmail = $"P0F_CREATE_{Guid.NewGuid():N}@example.invalid";
        string updateEmail = $"P0F_UPDATE_{Guid.NewGuid():N}@example.invalid";
        long? updateUserId = null;

        try
        {
            var createDto = new CrearUsuarioDto
            {
                Nombre = "Atomicidad crear",
                Apellido = "Prueba",
                Email = failedCreateEmail,
                RolId = fixture.RoleId,
                EsUsuarioDominio = 0,
                ModulosIds = [fixture.ModuleId, fixture.ModuleId]
            };

            await Assert.ThrowsAsync<OracleException>(() => repository.CrearAsync(createDto, "synthetic-hash", "BCRYPT"));

            Assert.Equal(0, await ScalarAsync(connection,
                "SELECT COUNT(*) FROM RL_USUARIOS WHERE USR_EMAIL = :email", ("email", failedCreateEmail)));
            Assert.Equal(0, await ScalarAsync(connection,
                "SELECT COUNT(*) FROM RL_USUARIO_MODULOS m JOIN RL_USUARIOS u ON u.USR_ID = m.USM_USR_ID WHERE u.USR_EMAIL = :email",
                ("email", failedCreateEmail)));

            var initialDto = new CrearUsuarioDto
            {
                Nombre = "Nombre original",
                Apellido = "Estado original",
                Email = updateEmail,
                RolId = fixture.RoleId,
                EsUsuarioDominio = 0,
                ModulosIds = [fixture.ModuleId]
            };
            updateUserId = await repository.CrearAsync(initialDto, "synthetic-hash", "BCRYPT");

            var updateDto = new ActualizarUsuarioDto
            {
                Nombre = "Nombre nuevo parcial",
                Apellido = "Estado nuevo parcial",
                Email = updateEmail,
                RolId = fixture.RoleId,
                EsUsuarioDominio = 0,
                ModulosIds = [fixture.ModuleId, fixture.ModuleId]
            };

            await Assert.ThrowsAsync<OracleException>(() => repository.ActualizarAsync(updateUserId.Value, updateDto, null, null));

            Assert.Equal("Nombre original", await ScalarStringAsync(connection,
                "SELECT USR_NOMBRE FROM RL_USUARIOS WHERE USR_ID = :id", ("id", updateUserId.Value)));
            Assert.Equal("Estado original", await ScalarStringAsync(connection,
                "SELECT USR_APELLIDO FROM RL_USUARIOS WHERE USR_ID = :id", ("id", updateUserId.Value)));
            Assert.Equal(1, await ScalarAsync(connection,
                "SELECT COUNT(*) FROM RL_USUARIO_MODULOS WHERE USM_USR_ID = :id AND USM_MOD_ID = :moduleId",
                ("id", updateUserId.Value), ("moduleId", fixture.ModuleId)));
            Assert.Equal(1, await ScalarAsync(connection,
                "SELECT COUNT(*) FROM RL_USUARIO_MODULOS WHERE USM_USR_ID = :id",
                ("id", updateUserId.Value)));

            Console.WriteLine("P0F_CREATE_NTH_MODULE_ROLLBACK=PASS; P0F_UPDATE_NTH_MODULE_ROLLBACK=PASS; P0F_OLD_MODULE_STATE_RESTORED=PASS");
        }
        finally
        {
            await ExecuteAsync(connection,
                "DELETE FROM RL_USUARIO_MODULOS WHERE USM_USR_ID IN (SELECT USR_ID FROM RL_USUARIOS WHERE USR_EMAIL IN (:createEmail,:updateEmail))",
                ("createEmail", failedCreateEmail), ("updateEmail", updateEmail));
            await ExecuteAsync(connection,
                "DELETE FROM RL_USUARIOS WHERE USR_EMAIL IN (:createEmail,:updateEmail)",
                ("createEmail", failedCreateEmail), ("updateEmail", updateEmail));
            if (fixture.CreatedModuleId.HasValue)
                await ExecuteAsync(connection, "DELETE FROM RL_MODULOS WHERE MOD_ID = :id", ("id", fixture.CreatedModuleId.Value));
            await DropFixtureObjectsAsync(connection, fixture);
        }
    }

    private static void ValidarDestinoDisposableXe(string connectionString)
    {
        var builder = new OracleConnectionStringBuilder(connectionString);
        string source = builder.DataSource;
        bool localHost = source.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || source.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
        bool xeService = source.Contains("/XE", StringComparison.OrdinalIgnoreCase)
            || source.Contains("SERVICE_NAME=XE", StringComparison.OrdinalIgnoreCase);
        Assert.True(localHost && xeService, "P0-F integration tests require a disposable loopback Oracle XE service.");
    }

    private static async Task<Fixture> EnsureFixtureAsync(OracleConnection connection)
    {
        bool createdUsers = !await TableExistsAsync(connection, "RL_USUARIOS");
        bool createdModules = !await TableExistsAsync(connection, "RL_MODULOS");
        bool createdAssignments = !await TableExistsAsync(connection, "RL_USUARIO_MODULOS");
        bool createdSequence = !await SequenceExistsAsync(connection, "SEQ_RL_USUARIOS");

        if (createdUsers)
            await ExecuteAsync(connection, "CREATE TABLE RL_USUARIOS (USR_ID NUMBER(10) PRIMARY KEY, USR_NOMBRE VARCHAR2(100) NOT NULL, USR_APELLIDO VARCHAR2(100) NOT NULL, USR_EMAIL VARCHAR2(150) NOT NULL UNIQUE, USR_PASSWORD_HASH VARCHAR2(256) NOT NULL, USR_PASSWORD_SALT VARCHAR2(256) NOT NULL, USR_ROL_ID NUMBER(3) NOT NULL, USR_EMPLEADO_ID VARCHAR2(20), USR_DNI VARCHAR2(25), ES_USUARIO_DOMINIO NUMBER(1), USUARIO_DOMINIO VARCHAR2(500), USR_DOM_ID NUMBER(5), USR_DEBE_CAMBIAR_PASS NUMBER(1), USR_FECHA_CLAVE_TEMP DATE, USR_ACTIVO NUMBER(1) DEFAULT 1 NOT NULL, USR_FECHA_CREACION DATE DEFAULT SYSDATE NOT NULL)");
        if (createdModules)
            await ExecuteAsync(connection, "CREATE TABLE RL_MODULOS (MOD_ID NUMBER(5) PRIMARY KEY)");
        if (createdAssignments)
            await ExecuteAsync(connection, "CREATE TABLE RL_USUARIO_MODULOS (USM_USR_ID NUMBER(10) NOT NULL, USM_MOD_ID NUMBER(5) NOT NULL, CONSTRAINT PK_P0F_USUARIO_MODULOS PRIMARY KEY (USM_USR_ID, USM_MOD_ID), CONSTRAINT FK_P0F_USM_USUARIO FOREIGN KEY (USM_USR_ID) REFERENCES RL_USUARIOS(USR_ID), CONSTRAINT FK_P0F_USM_MODULO FOREIGN KEY (USM_MOD_ID) REFERENCES RL_MODULOS(MOD_ID))");
        if (createdSequence)
            await ExecuteAsync(connection, "CREATE SEQUENCE SEQ_RL_USUARIOS START WITH 9000000000 INCREMENT BY 1 NOCACHE");

        long moduleId = await ScalarAsync(connection, "SELECT NVL(MIN(MOD_ID), 0) FROM RL_MODULOS");
        long? createdModuleId = null;
        if (moduleId == 0)
        {
            moduleId = 99999;
            if (createdModules)
                await ExecuteAsync(connection, "INSERT INTO RL_MODULOS (MOD_ID) VALUES (:id)", ("id", moduleId));
            else
                await ExecuteAsync(connection,
                    "INSERT INTO RL_MODULOS (MOD_ID, MOD_NOMBRE, MOD_DESCRIPCION, MOD_RUTA, MOD_ICONO, MOD_SECCION, MOD_ACTIVO) VALUES (:id,'P0F fixture','P0F disposable test fixture','/p0f-atomicity-fixture','shield','P0F',1)",
                    ("id", moduleId));
            createdModuleId = moduleId;
        }

        int roleId = 1;
        if (await TableExistsAsync(connection, "RL_ROLES"))
        {
            object? roleValue = await ScalarObjectAsync(connection, "SELECT NVL(MIN(ROL_ID), 0) FROM RL_ROLES");
            if (roleValue is not null && Convert.ToInt32(roleValue, CultureInfo.InvariantCulture) > 0)
                roleId = Convert.ToInt32(roleValue, CultureInfo.InvariantCulture);
        }

        return new Fixture(createdUsers, createdModules, createdAssignments, createdSequence, createdModuleId, roleId, checked((int)moduleId));
    }

    private static async Task DropFixtureObjectsAsync(OracleConnection connection, Fixture fixture)
    {
        if (fixture.CreatedAssignments) await ExecuteAsync(connection, "DROP TABLE RL_USUARIO_MODULOS PURGE");
        if (fixture.CreatedModules) await ExecuteAsync(connection, "DROP TABLE RL_MODULOS PURGE");
        if (fixture.CreatedUsers) await ExecuteAsync(connection, "DROP TABLE RL_USUARIOS PURGE");
        if (fixture.CreatedSequence) await ExecuteAsync(connection, "DROP SEQUENCE SEQ_RL_USUARIOS");
    }

    private static async Task<bool> TableExistsAsync(OracleConnection connection, string tableName) =>
        await ScalarAsync(connection, "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME = :name", ("name", tableName)) > 0;

    private static async Task<bool> SequenceExistsAsync(OracleConnection connection, string sequenceName) =>
        await ScalarAsync(connection, "SELECT COUNT(*) FROM USER_SEQUENCES WHERE SEQUENCE_NAME = :name", ("name", sequenceName)) > 0;

    private static async Task<long> ScalarAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        object? value = await ScalarObjectAsync(connection, sql, parameters);
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static async Task<string?> ScalarStringAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters) =>
        Convert.ToString(await ScalarObjectAsync(connection, sql, parameters), CultureInfo.InvariantCulture);

    private static async Task<object?> ScalarObjectAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach ((string name, object value) in parameters)
            command.Parameters.Add(new OracleParameter(name, value));
        return await command.ExecuteScalarAsync();
    }

    private static async Task ExecuteAsync(OracleConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach ((string name, object value) in parameters)
            command.Parameters.Add(new OracleParameter(name, value));
        await command.ExecuteNonQueryAsync();
    }

    private sealed record Fixture(bool CreatedUsers, bool CreatedModules, bool CreatedAssignments, bool CreatedSequence, long? CreatedModuleId, int RoleId, int ModuleId);
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class P0FOracleRequiredFactAttribute : FactAttribute
{
    public P0FOracleRequiredFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RL_P0F_ORACLE_INTEGRATION_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
            Skip = "Requires the disposable Oracle XE P0-F integration step.";
    }
}
