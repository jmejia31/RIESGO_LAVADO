using RL.API.Features.MatricesRiesgos.Persistence;
using Xunit;

namespace RL.API.Tests.Features.MatricesRiesgos;

public sealed class MatricesRiesgosFamiliasDeletionPolicyTests
{
    [Fact]
    public void PoliticaDelete_ConsideraCualquierVersionComoDependencia()
    {
        string source = LeerLifecycle();
        int deleteStart = source.IndexOf("EliminarFamiliaFormularioSeguraAsync", StringComparison.Ordinal);
        int helperStart = source.IndexOf(
            "private async Task<FamiliaBloqueada?> ObtenerFamiliaBloqueadaAsync",
            deleteStart + 1,
            StringComparison.Ordinal);
        Assert.True(helperStart > deleteStart, "No se encontró el límite del método de eliminación.");
        string method = source[deleteStart..helperStart];

        Assert.Contains("SELECT COUNT(*)", method, StringComparison.Ordinal);
        Assert.Contains("WHERE VER_FAMILIA_ID = :famId", method, StringComparison.Ordinal);
        Assert.DoesNotContain("VER_ESTADO =", method, StringComparison.Ordinal);
    }

    private static string LeerLifecycle()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RIESGO_LAVADO.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        return File.ReadAllText(Path.Combine(directory!.FullName, "backend", "RL.API", "Features", "MatricesRiesgos", "Persistence", "FamiliasFormularioLifecycleRepository.cs"));
    }
}
