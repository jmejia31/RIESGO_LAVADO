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
    public void Monitoreo_AplicaFiltroDeEstadoFueraDelNivelQueDefineElAlias()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"));

        Assert.Contains("SELECT f.* FROM (SELECT q.*", source, StringComparison.Ordinal);
        Assert.Contains("AND ESTADO_MONITOREO = :estado", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Monitoreo_UsaPaginaSeparadaMetadataCacheadaYLookupsSetBased()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"));
        var monitoringStart = source.IndexOf("private async Task<MonitoreoPaginadoDto<T>> ObtenerMonitoreoPaginadoAsync", StringComparison.Ordinal);
        var monitoringEnd = source.IndexOf("private sealed record MonitoreoMetadata", monitoringStart, StringComparison.Ordinal);
        Assert.True(monitoringStart >= 0 && monitoringEnd > monitoringStart);
        var monitoring = source[monitoringStart..monitoringEnd];

        Assert.DoesNotContain("COUNT(*) OVER", monitoring, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SUM(CASE", monitoring, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rangeCommand", monitoring, StringComparison.Ordinal);
        Assert.Contains("EjecutarPaginaMonitoreoAsync", monitoring, StringComparison.Ordinal);
        Assert.Contains("ObtenerMetadataMonitoreoAsync", monitoring, StringComparison.Ordinal);
        Assert.Contains("_cache.GetOrCreateAsync", monitoring, StringComparison.Ordinal);
        Assert.Contains("cancellationToken", monitoring, StringComparison.Ordinal);
        Assert.DoesNotContain("ObtenerMonitoreoPaginadoLegacyAsync", source, StringComparison.Ordinal);

        foreach (var builder in new[] { "ConstruirConsultaMonitoreoJuridicas", "ConstruirConsultaMonitoreoNaturales", "ConstruirConsultaMonitoreoEmpleados" })
        {
            var start = source.IndexOf("private static string " + builder, StringComparison.Ordinal);
            Assert.True(start >= 0);
            var end = source.IndexOf("private static string ConstruirConsultaMonitoreo", start + builder.Length, StringComparison.Ordinal);
            var sql = source[start..(end > start ? end : source.Length)];
            Assert.Contains("POSITIVOS_AGG", sql, StringComparison.Ordinal);
            Assert.DoesNotContain("SELECT MIN(lp.", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NVL((SELECT 1 FROM RL_LISTA_POSITIVOS", sql, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void LegacyFullListRoutesAndMethods_NoPermanecenComoSuperficieProductiva()
    {
        var listasController = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Listas/ListasController.cs"));
        var listasRepository = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"));
        var authController = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Identidad/AuthController.cs"));
        var authRepository = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Identidad/Persistence/UsuarioRepository.cs"));
        var matricesController = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs"));
        var gestionController = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosGestionController.cs"));
        var gestionRepository = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/MatricesRiesgos/Persistence/MatricesRiesgosGestionRepository.cs"));

        foreach (var route in new[] { "HttpGet(\"juridicas\")", "HttpGet(\"naturales\")", "HttpGet(\"empleados\")" })
            Assert.DoesNotContain(route, listasController, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpGet(\"usuarios\")", authController, StringComparison.Ordinal);
        Assert.DoesNotContain("ObtenerJuridicasAsync()", listasRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("ObtenerNaturalesAsync()", listasRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("ObtenerEmpleadosAsync()", listasRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("ListarAsync()", authRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpGet(\"familias\")", matricesController, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpGet(\"consolidado\")", matricesController, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpGet]", gestionController, StringComparison.Ordinal);
        Assert.DoesNotContain("ListarRiesgosAsync(bool", gestionRepository, StringComparison.Ordinal);
        Assert.Contains("HttpGet(\"juridicas/paginado\")", listasController, StringComparison.Ordinal);
        Assert.Contains("HttpGet(\"juridicas/exportar\")", listasController, StringComparison.Ordinal);
        Assert.Contains("HttpGet(\"usuarios/paginado\")", authController, StringComparison.Ordinal);
        Assert.Contains("HttpGet(\"familias/paginado\")", matricesController, StringComparison.Ordinal);
        Assert.Contains("HttpGet(\"consolidado/paginado\")", matricesController, StringComparison.Ordinal);
        Assert.Contains("HttpGet(\"paginado\")", gestionController, StringComparison.Ordinal);
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
