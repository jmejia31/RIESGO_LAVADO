using System;
using System.IO;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosRepositorySqlContractTests
{
    [Fact]
    public void ActualizarEvaluacion_NoUsaNombreLegacySingularDeFlujos()
    {
        string source = LeerRepositorioMatrices();

        Assert.DoesNotContain("RL_MR_FLUJO_EVALUACION", source, StringComparison.Ordinal);
        Assert.Contains("RL_MR_FLUJOS_EVALUACION", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ActualizarEvaluacion_ObtieneEstadoPersistidoConHelperCanonicoDespuesDelLock()
    {
        string source = LeerRepositorioMatrices();

        Assert.Contains(
            "string estadoPersistido = await ObtenerEstadoActualAsync(conn, trans, dto.EvaId);",
            source,
            StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ConsolidadoNormalizaPaginaAntesDeConstruirOffset()
    {
        string source = LeerRepositorioMatrices();
        int normalizacion = source.IndexOf("PaginacionEvaluacionesHelper.CalcularPaginaEfectiva", StringComparison.Ordinal);
        int offset = source.IndexOf("int offset = (paginaNormalizada - 1) * tamanoPagina;", StringComparison.Ordinal);

        Assert.True(normalizacion >= 0);
        Assert.True(offset >= 0);
        Assert.True(normalizacion < offset);
    }

    [Fact]
    public void ConsolidadoPaginado_ProyectaLaConsultaInternaSinAliasFueraDeAlcance()
    {
        string source = LeerRepositorioMatrices();

        Assert.Contains(
            "SELECT base.*, ROWNUM AS CONSOLIDADO_ROWNUM",
            source,
            StringComparison.Ordinal);
        Assert.Contains(") base", source, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "SELECT {columnas}, ROWNUM AS CONSOLIDADO_ROWNUM",
            source,
            StringComparison.Ordinal);
    }

    private static string LeerRepositorioMatrices()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RIESGO_LAVADO.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string path = Path.Combine(
            directory!.FullName,
            "backend",
            "RL.API",
            "Features",
            "MatricesRiesgos",
            "Persistence",
            "MatricesRiesgosRepository.cs");

        Assert.True(File.Exists(path), $"No se encontró el repositorio de Matrices en {path}.");
        return File.ReadAllText(path);
    }
}
