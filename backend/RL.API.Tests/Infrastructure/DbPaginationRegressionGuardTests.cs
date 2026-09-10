using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class DbPaginationRegressionGuardTests
{
    [Fact]
    public void CertifiedBusinessRepositories_UseFilteredCountStableOrderAndDatabasePageWindow()
    {
        var root = RepositoryRoot();
        var sources = new[]
        {
            File.ReadAllText(Path.Combine(root, "backend/RL.API/Features/Identidad/Persistence/UsuarioRepository.cs")),
            File.ReadAllText(Path.Combine(root, "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs")),
            File.ReadAllText(Path.Combine(root, "backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosRepository.cs")),
            File.ReadAllText(Path.Combine(root, "backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosGestionRepository.cs"))
        };

        foreach (var source in sources)
        {
            Assert.Contains("COUNT(*)", source, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ROWNUM", source, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ORDER BY", source, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Math.Clamp", source, StringComparison.Ordinal);
            Assert.Contains("BindByName = true", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ListasPaginadas_ExponeConsultasSeparadasParaGrillaYExportacionCompletaFiltrada()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"));

        Assert.Contains("ObtenerMonitoreoPaginadoAsync", source, StringComparison.Ordinal);
        Assert.Contains("ObtenerMonitoreoCompletoAsync", source, StringComparison.Ordinal);
        Assert.Contains("ESTADO_MONITOREO", source, StringComparison.Ordinal);
        Assert.Contains("AppendMonitoringFilters", source, StringComparison.Ordinal);
        Assert.Contains("ORDER BY NOMBRE ASC", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UsuariosPaginados_CargaModulosSoloParaLaPagina()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Identidad/Persistence/UsuarioRepository.cs"));
        var pagedStart = source.IndexOf("ListarPaginadoAsync", StringComparison.Ordinal);

        Assert.True(pagedStart >= 0);
        var pagedSource = source[pagedStart..];
        Assert.Contains("IN (", pagedSource, StringComparison.Ordinal);
        Assert.DoesNotContain("foreach (var user in await ListarAsync", pagedSource, StringComparison.Ordinal);
        Assert.Contains("users.Count > 0", pagedSource, StringComparison.Ordinal);
    }

    [Fact]
    public void AltoVolumen_ConservaLaReglaDePaginaEfectiva()
    {
        Assert.Equal(4_000, (int)Math.Ceiling(100_000d / 25));
        Assert.Equal(4_000, RL.API.Features.MatricesRiesgos.Domain.PaginacionEvaluacionesHelper.CalcularPaginaEfectiva(100_000, 25, 4_001));
        Assert.Equal(25, Math.Clamp(25, 1, 200));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
