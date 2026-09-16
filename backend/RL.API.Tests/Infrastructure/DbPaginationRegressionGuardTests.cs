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
    public void MonitoreoNaturales_UsaFuenteLigeraEquivalenteSinLaVistaPesada()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"));
        var naturalesSql = ExtractSqlBuilder(source, "ConstruirConsultaMonitoreoNaturales");
        var personaFuenteStart = naturalesSql.IndexOf("PERSONA_FUENTE AS (", StringComparison.Ordinal);
        var personaFuenteEnd = naturalesSql.IndexOf("), PERSONA_FUENTE_DISTINCTA AS", personaFuenteStart, StringComparison.Ordinal);

        Assert.True(personaFuenteStart >= 0 && personaFuenteEnd > personaFuenteStart);
        var personaFuente = naturalesSql[personaFuenteStart..personaFuenteEnd];

        // El detalle natural requiere más columnas de la vista; esta guarda cubre sólo el hot path paginado.
        Assert.DoesNotContain("DNP_IHSS.V_SOCIOS_REPRESENTANTES", naturalesSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DNP_IHSS.SOCIOS S", personaFuente, StringComparison.Ordinal);
        Assert.Contains("DNP_IHSS.REPRESENTANTES R", personaFuente, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN REPORTE_IDS I ON I.DNI = S.NUMERO_IDENTIFICACION", personaFuente, StringComparison.Ordinal);
        Assert.Contains("INNER JOIN REPORTE_IDS I ON I.DNI = R.NUMERO_IDENTIFICACION", personaFuente, StringComparison.Ordinal);
        Assert.Contains("SELECT DISTINCT", personaFuente, StringComparison.Ordinal);
        Assert.Contains("UNION", personaFuente, StringComparison.Ordinal);
        Assert.DoesNotContain("UNION ALL", personaFuente, StringComparison.OrdinalIgnoreCase);

        foreach (var requiredFunctionalRelation in new[]
                 {
                     "DNP_IHSS.TIPO_IDENTIFICACION", "DNP_IHSS.TIPO_GENERO", "DNP_IHSS.PAISES_NACIONALIDAD",
                     "DNP_IHSS.DEPARTAMENTOS", "DNP_IHSS.MUNICIPIOS", "DNP_IHSS.TIPO_CONDICION_ACTUA",
                     "DNP_IHSS.TIPO_OCUPACION", "DNP_IHSS.DATOS_EMPRESA", "MMATAMOROS.PATRONOS PA",
                     "PA.NUMEPATRO = DE.NUMERO_PATRONAL"
                 })
            Assert.Contains(requiredFunctionalRelation, personaFuente, StringComparison.Ordinal);

        Assert.DoesNotContain("SELECT (SELECT", naturalesSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MonitoreoJuridicas_UsaFuenteLigeraEquivalenteSinLaVistaPesada()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs"));
        var juridicasSql = ExtractSqlBuilder(source, "ConstruirConsultaMonitoreoJuridicas");
        var reporteJuridicasStart = juridicasSql.IndexOf("REPORTE_JURIDICAS AS (", StringComparison.Ordinal);
        var reporteJuridicasEnd = juridicasSql.IndexOf("), EMPRESA_FUENTE AS", reporteJuridicasStart, StringComparison.Ordinal);
        var empresaFuenteStart = juridicasSql.IndexOf("EMPRESA_FUENTE AS (", StringComparison.Ordinal);
        var empresaFuenteEnd = juridicasSql.IndexOf("), Coincidencias AS", empresaFuenteStart, StringComparison.Ordinal);

        Assert.True(reporteJuridicasStart >= 0 && reporteJuridicasEnd > reporteJuridicasStart);
        Assert.True(empresaFuenteStart >= 0 && empresaFuenteEnd > empresaFuenteStart);
        var reporteJuridicas = juridicasSql[reporteJuridicasStart..reporteJuridicasEnd];
        var empresaFuente = juridicasSql[empresaFuenteStart..empresaFuenteEnd];

        Assert.DoesNotContain("DNP_IHSS.V_DATOS_EMPRESA", juridicasSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("DNP_IHSS.REPORTE_COINCIDENCIAS", reporteJuridicas, StringComparison.Ordinal);
        Assert.Contains("TIPO_CALIFICACION_ID = 1", reporteJuridicas, StringComparison.Ordinal);
        Assert.Contains("NUMERO_PATRONO IS NOT NULL", reporteJuridicas, StringComparison.Ordinal);
        Assert.Contains("FROM REPORTE_JURIDICAS R", empresaFuente, StringComparison.Ordinal);
        Assert.Contains("MMATAMOROS.PATRONOS P ON P.NUMEPATRO = R.NUMERO_PATRONO", empresaFuente, StringComparison.Ordinal);
        Assert.Contains("DNP_IHSS.DATOS_EMPRESA DE ON DE.NUMERO_PATRONAL = P.NUMEPATRO", empresaFuente, StringComparison.Ordinal);
        Assert.Contains("DE.TIPO_EMPRESA_ID = 1", empresaFuente, StringComparison.Ordinal);

        foreach (var requiredFunctionalRelation in new[]
                 {
                     "MMATAMOROS.ACTIECON", "MMATAMOROS.SECTORES", "DNP_IHSS.DEPARTAMENTOS",
                     "DNP_IHSS.MUNICIPIOS", "DNP_IHSS.TIPO_EMPRESA", "DNP_IHSS.TIPO_RIESGO"
                 })
            Assert.Contains(requiredFunctionalRelation, empresaFuente, StringComparison.Ordinal);

        Assert.DoesNotContain("SELECT (SELECT", juridicasSql, StringComparison.OrdinalIgnoreCase);
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

    private static string ExtractSqlBuilder(string source, string builder)
    {
        var start = source.IndexOf("private static string " + builder, StringComparison.Ordinal);
        Assert.True(start >= 0, $"No se encontro el builder {builder}.");
        var end = source.IndexOf("private static string ConstruirConsultaMonitoreo", start + builder.Length, StringComparison.Ordinal);
        return source[start..(end > start ? end : source.Length)];
    }
}
