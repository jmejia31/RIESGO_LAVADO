using Xunit;

namespace RL.API.Tests.Infrastructure;

public sealed class AuditIpPipelineGuardTests
{
    private static readonly string[] AuditWriters =
    [
        "backend/RL.API/Features/Auditoria/AuditoriaController.cs",
        "backend/RL.API/Features/Auditoria/Persistence/AuditoriaRepository.cs",
        "backend/RL.API/Features/Configuracion/ConfiguracionController.cs",
        "backend/RL.API/Features/Identidad/AuthController.cs",
        "backend/RL.API/Features/Listas/ListasController.cs",
        "backend/RL.API/Features/Listas/Persistence/ListasRepository.cs",
        "backend/RL.API/Features/MatricesRiesgos/CalculoConfiguracionController.cs",
        "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosController.cs",
        "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosGestionController.cs",
        "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMitigacionController.cs",
        "backend/RL.API/Features/MatricesRiesgos/MatricesRiesgosMonitoreoController.cs",
        "backend/RL.API/Features/MatricesRiesgos/Persistence/FamiliasFormularioLifecycleRepository.cs",
        "backend/RL.API/Features/MatricesRiesgos/Persistence/SafeMatricesRiesgosRepository.cs"
    ];

    [Fact]
    public void AuditWriters_UseOnlyCanonicalClientIpAccess()
    {
        var root = RepositoryRoot();
        foreach (var relativePath in AuditWriters)
        {
            var source = File.ReadAllText(Path.Combine(root, relativePath));
            Assert.DoesNotContain("RemoteIpAddress", source);
            Assert.DoesNotContain("X-Forwarded-For", source);
            Assert.DoesNotContain("X-Real-IP", source);
        }
    }

    [Fact]
    public void Application_RegistersTrustedForwardedHeadersBeforeRequestConsumers()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/RL.API/Program.cs"));
        Assert.Contains("KnownProxies", source);
        Assert.Contains("app.UseForwardedHeaders();", source);
        Assert.True(source.IndexOf("app.UseForwardedHeaders();", StringComparison.Ordinal)
            < source.IndexOf("app.UseAuthentication();", StringComparison.Ordinal));
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
