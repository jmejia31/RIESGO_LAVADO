using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasSourceContractTests
{
    [Fact]
    public void LifecycleRepository_NoEliminaVersionesParaEliminarFamilia()
    {
        string source = Leer("backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", "FamiliasFormularioLifecycleRepository.cs");
        Assert.DoesNotContain("DELETE FROM RL_MR_VERSIONES_FORMULARIO", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LifecycleRepository_NoContieneCascadeDelete()
    {
        string source = Leer("backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", "FamiliasFormularioLifecycleRepository.cs");
        Assert.DoesNotContain("CASCADE", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SafeRepository_UpdateNoMutaCodigo()
    {
        string source = Leer("backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", "SafeMatricesRiesgosRepository.cs");
        Assert.DoesNotContain("SET FAM_CODIGO", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string Leer(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RIESGO_LAVADO.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string path = Path.Combine(new[] { directory!.FullName }.Concat(segments).ToArray());
        Assert.True(File.Exists(path), $"No se encontró {path}.");
        return File.ReadAllText(path);
    }
}
