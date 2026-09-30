using System;
using System.IO;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosBlock4SqlContractTests
{
    [Fact]
    public void Block4_ReadModel_AgrupaPlanesYActividadesEnUnaConsultaYDerivaLosConteos()
    {
        string source = Read("backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", "MatricesRiesgosMitigacionRepository.cs");
        int start = source.IndexOf("public async Task<MitigacionBloque4Dto?> ObtenerBloque4Async", StringComparison.Ordinal);
        int end = source.IndexOf("public async Task<long> CrearPlanAsync", start, StringComparison.Ordinal);
        string readModel = source[start..end];

        Assert.Contains("LEFT JOIN RL_MR_PLANES p ON p.PLA_EVALUACION_ID = e.EVA_ID", readModel, StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN RL_MR_ACTIVIDADES a ON a.ACT_PLAN_ID = p.PLA_ID", readModel, StringComparison.Ordinal);
        Assert.Contains("CantidadActividades++", readModel, StringComparison.Ordinal);
        Assert.Contains("CantidadAcciones = planes.Count", readModel, StringComparison.Ordinal);
        Assert.DoesNotContain("RL_MR_AUTOMONITOREO", readModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Block4_WriteDto_NoExponeConteosComoValoresConfiablesDelCliente()
    {
        string source = Read("backend", "RL.API", "Features", "MatricesRiesgos", "Contracts", "PlanesAccion", "MitigacionDtos.cs");
        int start = source.IndexOf("public sealed class PlanMitigacionGuardarDto", StringComparison.Ordinal);
        int end = source.IndexOf("public sealed class ActividadPlanDto", start, StringComparison.Ordinal);
        string writeContract = source[start..end];

        Assert.DoesNotContain("CantidadAcciones", writeContract, StringComparison.Ordinal);
        Assert.DoesNotContain("CantidadActividades", writeContract, StringComparison.Ordinal);
        Assert.DoesNotContain("Actividades {", writeContract, StringComparison.Ordinal);
    }

    [Fact]
    public void Block4_CreateUpdateSelectAndAudit_IncludeTheThreeNullablePlanProperties()
    {
        string source = Read("backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", "MatricesRiesgosMitigacionRepository.cs");

        foreach (string column in new[] { "PLA_MONITOREO_SEGUIMIENTO", "PLA_RESPONSABLES", "PLA_RECURSOS" })
        {
            Assert.Contains(column, source, StringComparison.Ordinal);
        }
        Assert.Contains("INSERT INTO RL_MR_PLANES", source, StringComparison.Ordinal);
        Assert.Contains("UPDATE RL_MR_PLANES", source, StringComparison.Ordinal);
        Assert.Contains("AuditarAsync(conn, tx, \"RL_MR_PLANES\", id, \"INSERT\", dto", source, StringComparison.Ordinal);
        Assert.Contains("AuditarAntesDespuesAsync(conn, tx, \"RL_MR_PLANES\", planId, anterior, dto", source, StringComparison.Ordinal);
        Assert.Contains("BeginTransaction", source, StringComparison.Ordinal);
        Assert.Contains("anterior.PlaEvaluacionId != dto.PlaEvaluacionId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SET PLA_EVALUACION_ID =", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Block4_ActividadNoPermiteCambiarElPlanPadreDuranteActualizacion()
    {
        string source = Read("backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", "MatricesRiesgosMitigacionRepository.cs");
        int start = source.IndexOf("public async Task<bool> ActualizarActividadAsync", StringComparison.Ordinal);
        int end = source.IndexOf("private static void AgregarParametrosPlan", start, StringComparison.Ordinal);
        string update = source[start..end];

        Assert.Contains("SELECT ACT_PLAN_ID FROM RL_MR_ACTIVIDADES WHERE ACT_ID = :id FOR UPDATE", source, StringComparison.Ordinal);
        Assert.Contains("planActual.Value != dto.ActPlanId", update, StringComparison.Ordinal);
        Assert.Contains("WHERE ACT_ID = :id AND ACT_PLAN_ID = :planId", update, StringComparison.Ordinal);
        Assert.DoesNotContain("SET ACT_PLAN_ID =", update, StringComparison.Ordinal);
    }

    [Fact]
    public void Block4_IncrementalAndFreshInstallSchemas_DeclareTheSameNullablePlanColumns()
    {
        string migration = Read("database", "19_matrices_riesgos", "transicion", "47_ddl_bloque4_plan_campos.sql");
        string fresh = Read("database", "19_matrices_riesgos", "transicion", "06_reconstruir_modelo_17_tablas.sql");

        foreach (string column in new[] { "PLA_MONITOREO_SEGUIMIENTO", "PLA_RESPONSABLES", "PLA_RECURSOS" })
        {
            Assert.Contains("agregar_columna('" + column + "')", migration, StringComparison.Ordinal);
            Assert.Contains(column + " VARCHAR2(1000 CHAR)", fresh, StringComparison.Ordinal);
        }
        Assert.Contains("p_columna || ' VARCHAR2(1000 CHAR))'", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE RL_MR_", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Block4_Rollback_ValidaLasTresColumnasAntesDeRetirarSoloElAlcanceAgregado()
    {
        string rollback = Read("database", "19_matrices_riesgos", "transicion", "49_rollback_bloque4_plan_campos.sql");
        int allValidations = rollback.IndexOf("validar_columna('PLA_RECURSOS')", StringComparison.Ordinal);
        int firstDrop = rollback.IndexOf("retirar_columna('PLA_MONITOREO_SEGUIMIENTO')", StringComparison.Ordinal);

        Assert.True(allValidations >= 0 && firstDrop > allValidations);
        Assert.Contains("ALTER TABLE RL_MR_PLANES DROP COLUMN ' || p_columna", rollback, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", rollback, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", rollback, StringComparison.OrdinalIgnoreCase);
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
