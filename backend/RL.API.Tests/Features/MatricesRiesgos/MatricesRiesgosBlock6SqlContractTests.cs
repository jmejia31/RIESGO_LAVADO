using System;
using System.IO;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosBlock6SqlContractTests
{
    [Fact]
    public void Block6_MigrationReusesTheFreshInstallCapabilityTableAndIndex()
    {
        string migration = Read("database", "19_matrices_riesgos", "transicion", "51_ddl_bloque6.sql");
        string freshInstall = Read("database", "01_create_tables.sql");

        Assert.Contains("IF v_table_count = 0 THEN", migration, StringComparison.Ordinal);
        Assert.Contains("FROM USER_TAB_COLUMNS", migration, StringComparison.Ordinal);
        Assert.Contains("SCHEMA_CONFLICT: RL_USUARIO_CAPACIDADES existe con columnas incompatibles", migration, StringComparison.Ordinal);
        Assert.Contains("FROM USER_INDEXES", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE INDEX IDX_RL_UCP_CAP ON RL_USUARIO_CAPACIDADES", migration, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE RL_USUARIO_CAPACIDADES", freshInstall, StringComparison.Ordinal);
        Assert.Contains("CREATE INDEX IDX_RL_UCP_CAP ON RL_USUARIO_CAPACIDADES", freshInstall, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE INDEX IDX_RL_UCP_CAP ON RL_USUARIO_CAPACIDADES", migration.Replace("EXECUTE IMMEDIATE 'CREATE INDEX IDX_RL_UCP_CAP ON RL_USUARIO_CAPACIDADES(UCP_CAPACIDAD, UCP_ACTIVO)'", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public void Block6_RollbackPreservesSharedCapabilityTableAndIndex()
    {
        string rollback = Read("database", "19_matrices_riesgos", "transicion", "53_rollback_bloque6.sql");

        Assert.Contains("Preserve this shared table and its index", rollback, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE RL_USUARIO_CAPACIDADES", rollback, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP INDEX IDX_RL_UCP_CAP", rollback, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DROP COLUMN MON_OBSERVACIONES_AREA", rollback, StringComparison.Ordinal);
        Assert.Contains("DROP COLUMN MON_OBSERVACIONES_UGR", rollback, StringComparison.Ordinal);
        Assert.Contains("DROP CONSTRAINT CK_RL_MR_CON_MON_EF", rollback, StringComparison.Ordinal);
    }

    [Fact]
    public void Block6_IncrementalAndRebuildSchemasDeclareTheSameBlock6Columns()
    {
        string migration = Read("database", "19_matrices_riesgos", "transicion", "51_ddl_bloque6.sql");
        string rebuild = Read("database", "19_matrices_riesgos", "transicion", "06_reconstruir_modelo_17_tablas.sql");

        foreach (string column in new[]
        {
            "CON_ESTADO_MONITOREO", "CON_EFECTIVIDAD_MONITOREO",
            "MON_OBSERVACIONES_AREA", "MON_OBSERVACIONES_UGR"
        })
        {
            Assert.Contains(column, migration, StringComparison.Ordinal);
            Assert.Contains(column, rebuild, StringComparison.Ordinal);
        }
        Assert.Contains("CREATE TABLE RL_USUARIO_CAPACIDADES", Read("database", "01_create_tables.sql"), StringComparison.Ordinal);
    }

    [Fact]
    public void FreshInstall_DoesNotCreateASecondIndexForTheUniqueEmailConstraint()
    {
        string source = Read("database", "01_create_tables.sql");
        int rolesStart = source.IndexOf("CREATE TABLE RL_USUARIOS", StringComparison.Ordinal);
        int nextSection = source.IndexOf("CREATE TABLE RL_USUARIO_CAPACIDADES", rolesStart, StringComparison.Ordinal);
        string users = source[rolesStart..nextSection];

        Assert.Contains("CONSTRAINT UQ_RL_USR_EMAIL UNIQUE (USR_EMAIL)", users, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE INDEX IDX_RL_USR_EMAIL ON RL_USUARIOS(USR_EMAIL)", users, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RIESGO_LAVADO.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string path = Path.Combine(directory!.FullName, Path.Combine(parts));
        Assert.True(File.Exists(path), $"No se encontró {path}.");
        return File.ReadAllText(path);
    }
}
