using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosOracleCertificationContractTests
{
    [Fact]
    public void InventarioOracle_DeclaraExactamenteDiecisieteTablasYSecuencias()
    {
        Assert.Equal(17, MatricesRiesgosRepositoryIntegrationTests.TablasModelo17.Length);
        Assert.Equal(17, MatricesRiesgosRepositoryIntegrationTests.SecuenciasModelo17.Length);
        Assert.Equal(
            17,
            MatricesRiesgosRepositoryIntegrationTests.TablasModelo17
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Equal(
            17,
            MatricesRiesgosRepositoryIntegrationTests.SecuenciasModelo17
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public void InventarioOracle_NoMezclaObjetosActivosYRetirados()
    {
        Assert.Empty(
            MatricesRiesgosRepositoryIntegrationTests.TablasModelo17.Intersect(
                MatricesRiesgosRepositoryIntegrationTests.TablasRetiradas,
                StringComparer.Ordinal));
        Assert.Empty(
            MatricesRiesgosRepositoryIntegrationTests.SecuenciasModelo17.Intersect(
                MatricesRiesgosRepositoryIntegrationTests.SecuenciasRetiradas,
                StringComparer.Ordinal));
    }

    [Fact]
    public void SuiteOracle_ConservaEscenariosFisicoCommitRollbackYAuditoria()
    {
        string[] metodos = typeof(MatricesRiesgosRepositoryIntegrationTests)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name)
            .ToArray();

        Assert.Contains(
            "EsquemaModelo17_InventarioIndicesRestriccionesYAusencias_CumplenContrato",
            metodos);
        Assert.Contains(
            "CicloCompleto_Commit_PersisteFamiliaVersionRiesgoEvaluacionProyeccionFlujoEvidenciaVinculoYAuditoria",
            metodos);
        Assert.Contains(
            "CicloCompleto_Rollback_NoPersisteRegistrosBase",
            metodos);
        Assert.Contains(
            "VinculoGenericoYAuditoria_FalloPosteriorAInsertarAuditoria_RevierteAmbosRegistros",
            metodos);
    }

    [Fact]
    public void SuiteOracle_DeclaraIndicesYRestriccionesPrincipalesSinDuplicados()
    {
        Assert.NotEmpty(MatricesRiesgosRepositoryIntegrationTests.IndicesPrincipales);
        Assert.NotEmpty(MatricesRiesgosRepositoryIntegrationTests.RestriccionesPrincipales);
        Assert.Equal(
            MatricesRiesgosRepositoryIntegrationTests.IndicesPrincipales.Length,
            MatricesRiesgosRepositoryIntegrationTests.IndicesPrincipales
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Equal(
            MatricesRiesgosRepositoryIntegrationTests.RestriccionesPrincipales.Length,
            MatricesRiesgosRepositoryIntegrationTests.RestriccionesPrincipales
                .Distinct(StringComparer.Ordinal)
                .Count());
    }
}
