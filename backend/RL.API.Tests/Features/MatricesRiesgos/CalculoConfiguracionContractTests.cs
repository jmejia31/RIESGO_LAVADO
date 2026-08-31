using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class CalculoConfiguracionContractTests
{
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath));

    [Fact]
    public void Repository_UsesBoundParametersAndSingleRuntimeEngineBoundary()
    {
        string source = Read("backend/RL.API/Features/MatricesRiesgos/Persistence/CalculoConfiguracionRepository.cs");

        Assert.Contains("BindByName=true", source);
        Assert.Contains("OracleParameter", source);
        Assert.DoesNotContain("FormulaEngineV2", source);
        Assert.DoesNotContain("ExecuteImmediate", source);
        Assert.DoesNotContain("RL_MR_AUDITORIA_CALCULO", source);
    }

    [Fact]
    public void OraclePackage_ContainsOnlyApprovedConfigurationObjectsAndSafeExecutionModes()
    {
        string root = Path.Combine(RepositoryRoot(), "database", "19_matrices_riesgos", "transicion");
        string ddl = File.ReadAllText(Path.Combine(root, "15_ddl_configuracion_calculo_312.sql"));
        string seed = File.ReadAllText(Path.Combine(root, "16_seed_funciones_nativas_312.sql"));

        Assert.DoesNotContain("IDENTITY", ddl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RL_MR_DEPENDENCIAS_CALCULO", ddl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RL_MR_REGLAS_CALCULO_V2", ddl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHENEVER SQLERROR EXIT SQL.SQLCODE", ddl);
        Assert.Equal(7, seed.Split("  seed_native('", StringSplitOptions.None).Length - 1);
        Assert.Contains("COMMIT", seed);
    }

    [Fact]
    public void OracleReadOnlyScripts_UseFailClosedDirectives()
    {
        string root = Path.Combine(RepositoryRoot(), "database", "19_matrices_riesgos", "transicion");
        foreach (string name in new[] { "14_precheck_configuracion_calculo_312_solo_lectura.sql", "17_postcheck_configuracion_calculo_312_solo_lectura.sql" })
        {
            string sql = File.ReadAllText(Path.Combine(root, name));
            Assert.Contains("WHENEVER SQLERROR EXIT SQL.SQLCODE", sql);
            Assert.Contains("WHENEVER OSERROR EXIT FAILURE", sql);
            Assert.DoesNotContain("INSERT INTO", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE ", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE ", sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
