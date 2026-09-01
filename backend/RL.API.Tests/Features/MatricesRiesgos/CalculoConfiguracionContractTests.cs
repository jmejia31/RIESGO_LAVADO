using Xunit;
using RL.API.Features.MatricesRiesgos.Domain;
using RL.API.Features.MatricesRiesgos.Persistence;

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

    [Fact]
    public void SequenceResolver_ResolvesEveryApprovedCalculationSequence()
    {
        string[] sequences =
        [
            "SEQ_RL_MR_FORMULAS",
            "SEQ_RL_MR_FORMULA_VERSIONES",
            "SEQ_RL_MR_FORMULA_USOS",
            "SEQ_RL_MR_FUNCIONES",
            "SEQ_RL_MR_FUNCION_VERSIONES",
            "SEQ_RL_MR_FUNCION_ARGUMENTOS",
            "SEQ_RL_MR_PARAMETROS",
            "SEQ_RL_MR_PARAMETRO_VERSIONES"
        ];

        Assert.Equal(8, sequences.Length);
        Assert.All(sequences, sequence =>
        {
            string sql = CalculoConfiguracionRepository.ResolveSequenceSql(sequence);
            Assert.Equal($"SELECT {sequence}.NEXTVAL FROM DUAL", sql);
        });
        Assert.Equal(
            "SELECT SEQ_RL_MR_FORMULA_USOS.NEXTVAL FROM DUAL",
            CalculoConfiguracionRepository.ResolveSequenceSql("SEQ_RL_MR_FORMULA_USOS"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalculoConfiguracionRepository.ResolveSequenceSql("SEQ_USER_SUPPLIED"));
    }

    [Fact]
    public void Repository_UsesFormulaUsageSequenceAndLocksMastersForVersionCreation()
    {
        string source = Read("backend/RL.API/Features/MatricesRiesgos/Persistence/CalculoConfiguracionRepository.cs");

        Assert.Contains("SEQ_RL_MR_FORMULA_USOS", source);
        Assert.Contains("SELECT FOR_ID FROM RL_MR_FORMULAS WHERE FOR_ID=:id AND FOR_ESTADO='ACTIVE' FOR UPDATE", source);
        Assert.Contains("SELECT FUN_ID FROM RL_MR_FUNCIONES WHERE FUN_ID=:id AND FUN_ESTADO='ACTIVE' FOR UPDATE", source);
        Assert.Contains("SELECT PAC_ID FROM RL_MR_PARAMETROS_CALCULO WHERE PAC_ID=:id AND PAC_ESTADO='ACTIVE' FOR UPDATE", source);
        Assert.Contains("WHERE {idColumn}=:id FOR UPDATE", source);
        Assert.Contains("currentState", source);
        Assert.DoesNotContain("AND {stateColumn} IN ('DRAFT','APPROVED','PUBLISHED')", source);
    }

    [Fact]
    public void EncodingPostcheck_UsesOnlyReadOnlyOracle11gCompatibleChecks()
    {
        string sql = Read("database/19_matrices_riesgos/transicion/23_postcheck_codificacion_comentarios_312_solo_lectura.sql");

        Assert.Contains("UNISTR('\\FFFD')", sql);
        Assert.Contains("UNISTR('\\00C3')", sql);
        Assert.Contains("UNISTR('\\00C2')", sql);
        Assert.Contains("SPANISH_DIACRITICS=", sql);
        Assert.DoesNotContain("COUNT(EVA_CALCULOS_JSON)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT INTO", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MERGE ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ALTER ", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP ", sql, StringComparison.OrdinalIgnoreCase);
    }
}
