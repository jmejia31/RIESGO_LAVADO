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

    [Fact]
    public void Consolidado_SeleccionaLaEvaluacionVigenteAprobadaMasRecientePorRiesgo()
    {
        string source = LeerRepositorioMatrices();
        int selectorStart = source.IndexOf("ConstruirFuenteConsolidado() =>", StringComparison.Ordinal);
        Assert.True(selectorStart >= 0);
        string selector = source[selectorStart..];

        Assert.Contains("PARTITION BY flujo.FLU_EVALUACION_ID", selector, StringComparison.Ordinal);
        Assert.Contains("ORDER BY flujo.FLU_FECHA DESC, flujo.FLU_ID DESC", selector, StringComparison.Ordinal);
        Assert.Contains("estado_actual.FLU_ESTADO = 'APROBADA'", selector, StringComparison.Ordinal);
        Assert.Contains("f.FLU_ESTADO AS PROY_ESTADO_EVALUACION", selector, StringComparison.Ordinal);
        Assert.Contains("PARTITION BY candidatas.EVA_RIESGO_ID", selector, StringComparison.Ordinal);
        Assert.Contains("candidatas.FLU_FECHA DESC", selector, StringComparison.Ordinal);
        Assert.Contains("candidatas.FLU_ID DESC", selector, StringComparison.Ordinal);
        Assert.Contains("candidatas.PROY_EVALUACION_ID DESC", selector, StringComparison.Ordinal);
        Assert.Contains("seleccion.CONSOLIDADO_RIESGO_RN = 1", selector, StringComparison.Ordinal);
    }

    [Fact]
    public void Consolidado_ListaYExportacionCompartenFuenteOficial()
    {
        string source = LeerRepositorioMatrices();
        Assert.Equal(3, Cuenta(source, "ConstruirFuenteConsolidado()"));
    }

    [Fact]
    public void ResumenOperativo_ReutilizaLaSeleccionOficialDelConsolidado()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RIESGO_LAVADO.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string source = File.ReadAllText(Path.Combine(directory!.FullName, "backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", "MatricesRiesgosMonitoreoRepository.cs"));
        Assert.Contains("WITH CONSOLIDADO_OFICIAL AS {MatricesRiesgosRepository.ConstruirFuenteConsolidado()}", source, StringComparison.Ordinal);
        Assert.Contains("FROM CONSOLIDADO_OFICIAL WHERE UPPER(PROY_NIVEL_RESIDUAL)", source, StringComparison.Ordinal);
    }

    private static int Cuenta(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
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
